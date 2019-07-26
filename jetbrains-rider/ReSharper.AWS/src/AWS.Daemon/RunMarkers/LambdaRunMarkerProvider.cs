using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Features.RunMarkers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Impl.Types;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Logging;
using IMethodDeclaration = JetBrains.ReSharper.Psi.CSharp.Tree.IMethodDeclaration;

namespace ReSharper.AWS.RunMarkers
{
    [Language(typeof(CSharpLanguage))]
    public class LambdaRunMarkerProvider : IRunMarkerProvider
    {
        private ILogger myLogger = Logger.GetLogger<LambdaRunMarkerProvider>();

        private static readonly IClrTypeName LambdaContextTypeName =
            new ClrTypeName("Amazon.Lambda.Core.ILambdaContext");

        private static readonly List<string> AmazonLambdaEventNamespaces =
            new List<string>
            {
                "Amazon.Lambda.APIGatewayEvents",
                "Amazon.Lambda.ApplicationLoadBalancerEvents",
                "Amazon.Lambda.CloudWatchLogsEvents",
                "Amazon.Lambda.CognitoEvents",
                "Amazon.Lambda.ConfigEvents",
                "Amazon.Lambda.DynamoDBEvents",
                "Amazon.Lambda.LexEvents",
                "Amazon.Lambda.KinesisAnalyticsEvents",
                "Amazon.Lambda.KinesisEvents",
                "Amazon.Lambda.KinesisFirehoseEvents",
                "Amazon.Lambda.S3Events",
                "Amazon.Lambda.SimpleEmailEvents",
                "Amazon.Lambda.SNSEvents",
                "Amazon.Lambda.SQSEvents"
            };

        private static readonly IClrTypeName StreamTypeName = new ClrTypeName("System.IO.Stream");
        private static readonly IClrTypeName AmazonSerializerType = new ClrTypeName("Amazon.Lambda.Core.ILambdaSerializer");
        private static readonly IClrTypeName AmazonAttributeType = new ClrTypeName("Amazon.Lambda.Core.LambdaSerializerAttribute");

        public double Priority => RunMarkerProviderPriority.DEFAULT;

        public void CollectRunMarkers(IFile file, IContextBoundSettingsStore settings, IHighlightingConsumer consumer)
        {
            if (!(file is ICSharpFile csharpFile)) return;
            if (!IsLambdaProjectType(csharpFile.GetProject())) return;

            foreach (var declaration in CachedDeclarationsCollector.Run<IMethodDeclaration>(csharpFile))
            {
                if (!(declaration.DeclaredElement is IMethod method)) continue;
                if (!IsSuitableLambdaMethod(method)) continue;

                var range = declaration.GetNameDocumentRange();

                var highlighting = new RunMarkerHighlighting(declaration,
                    LambdaRunMarkerAttributeIds.LAMBDA_RUN_METHOD_MARKER_ID, range,
                    file.GetPsiModule().TargetFrameworkId);

                consumer.AddHighlighting(highlighting, range);
            }
        }

        private bool IsLambdaProjectType(IProject project)
        {
            var isDotNetCore = project.IsDotNetCoreProject();
            var amazonPackageReference = project.GetPackagesReference("Amazon.Lambda.Core");
            var awsDefaultsJsonFiles =
                project.GetAllProjectFiles(file => file.Name == "aws-lambda-tools-defaults.json");

            return isDotNetCore && (amazonPackageReference != null || !awsDefaultsJsonFiles.IsEmpty());
        }

        private bool IsSuitableLambdaMethod(IMethod method)
        {
            return method != null &&
                   IsPublicMethod(method) &&
                   !HasSuperTypes(method) &&
                   IsValidInstanceOrStaticMethod(method) &&
                   HasRequiredParameters(method) &&
                   HasRequiredReturnType(method);
        }

        private bool IsPublicMethod(IMethod method)
        {
            return method.GetAccessRights() == AccessRights.PUBLIC;
        }

        private bool HasSuperTypes(IMethod method)
        {
            return method.GetContainingType() is IClass classElement && classElement.GetSuperTypes().Count > 1;
        }

        private bool IsValidInstanceOrStaticMethod(IMethod method)
        {
            var isStaticValid = method.IsStatic && method.ShortName != "Main";
            if (isStaticValid) return true;

            var classElement = method.GetContainingType() as IClass;
            if (classElement == null)
            {
                myLogger.Warn("Expected IClass element, but got: " + method.GetContainingType()?.GetClrName());
                return false;
            }

            return !method.IsStatic && CanBeInstantiatedByLambda(classElement);
        }

        private bool CanBeInstantiatedByLambda(IClass classElement)
        {
            return classElement.GetAccessRights() == AccessRights.PUBLIC &&
                   !classElement.IsAbstract &&
                   HasPublicNoArgsConstructor(classElement);
        }

        private bool HasPublicNoArgsConstructor(IClass classElement)
        {
            return classElement.Constructors.IsEmpty() ||
                   classElement.Constructors.Any(constructor =>
                       constructor.GetAccessRights() == AccessRights.PUBLIC && constructor.Parameters.IsEmpty());
        }

        private bool HasRequiredParameters(IMethod method)
        {
            var parameters = method.Parameters;

            switch (parameters.Count)
            {
                case 1:
                    return IsStreamType(parameters[0].Type) ||
                           (IsAmazonEventType(parameters[0].Type) || IsCustomDataType(parameters[0].Type)) && IsSerializerDefined(method);

                case 2:
                    return IsStreamType(parameters[0].Type) ||
                           (IsAmazonEventType(parameters[0].Type) || IsCustomDataType(parameters[0].Type)) && IsSerializerDefined(method) &&
                            IsLambdaContextType(parameters[1]);

                default:
                    return false;
            }
        }

        private bool IsCustomDataType(IType type)
        {
            return type.IsValid() && !type.IsVoid();
        }

        private bool IsAmazonEventType(IType type)
        {
            var clrName = (type as IDeclaredType)?.GetClrName();
            if (clrName == null) return false;

            var symbolCache = type.GetPsiServices().Symbols;
            var symbolScope = symbolCache.GetSymbolScope(type.Module, true, true);

            foreach (var amazonEventNamespaceName in AmazonLambdaEventNamespaces)
            {
                var namespaceElement = symbolScope.GetNamespace(amazonEventNamespaceName);
                var amazonEventClasses = namespaceElement?.GetNestedTypeElements(symbolScope).Where(element => element is IClass);

                if (amazonEventClasses == null) continue;

                if (amazonEventClasses.Any(eventClass =>
                {
                    var eventClassType = eventClass.Type();
                    return eventClassType != null && type.IsSubtypeOf(eventClassType);
                }))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsStreamType(IType type)
        {
            var streamType = new DeclaredTypeFromCLRName(StreamTypeName, new NullableAnnotation(), type.Module);
            return type.IsSubtypeOf(streamType);
        }

        private bool IsSerializerDefined(IMethod method)
        {
            var psiModule = method.Module;

            var amazonSerializerType =
                TypeFactory.CreateTypeByCLRName(AmazonSerializerType, NullableAnnotation.Unknown, psiModule);

            var methodAttributes = method.GetAttributeInstances(AmazonAttributeType, true);
            if (!methodAttributes.IsEmpty())
            {
                if (methodAttributes.Any(attribute =>
                    attribute.PositionParameters().Any(parameter =>
                        parameter.TypeValue?.IsSubtypeOf(amazonSerializerType) == true)))
                {
                    return true;
                }
            }

            var symbolCache = psiModule.GetPsiServices().Symbols;
            var assemblyAttributes = symbolCache.GetModuleAttributes(psiModule).GetAttributeInstances(AmazonAttributeType, true);

            return !assemblyAttributes.IsEmpty() && assemblyAttributes.Any(attribute =>
                       attribute.PositionParameters().Any(parameter =>
                           parameter.TypeValue?.IsSubtypeOf(amazonSerializerType) == true));
        }

        private bool IsLambdaContextType(IDeclaredElement element)
        {
            var clrName = (element?.Type() as IDeclaredType)?.GetClrName();
            return clrName != null && clrName.Equals(LambdaContextTypeName);
        }

        private bool HasRequiredReturnType(IMethod method)
        {
            var taskReturnType = method.Module.GetPredefinedType().Task;
            var returnType = method.ReturnType;

            return !method.IsAsync && (IsStreamType(returnType) || (IsAmazonEventType(returnType) || IsCustomDataType(returnType)) && IsSerializerDefined(method)) ||
                   method.IsAsync && (method.ReturnType.IsVoid() || returnType.IsSubtypeOf(taskReturnType));
        }
    }
}
