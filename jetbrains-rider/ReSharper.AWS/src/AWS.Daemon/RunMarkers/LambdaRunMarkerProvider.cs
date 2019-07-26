using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
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

namespace ReSharper.AWS.RunMarkers
{
    [Language(typeof(CSharpLanguage))]
    public class LambdaRunMarkerProvider : IRunMarkerProvider
    {
        private static readonly IClrTypeName LambdaContextTypeName =
            new ClrTypeName("Amazon.Lambda.Core.ILambdaContext");

        private static readonly List<IClrTypeName> AmazonLambdaEvents =
            new List<IClrTypeName>
            {
                new ClrTypeName("Amazon.Lambda.APIGatewayEvents"),
                new ClrTypeName("Amazon.Lambda.ApplicationLoadBalancerEvents"),
                new ClrTypeName("Amazon.Lambda.CloudWatchLogsEvents"),
                new ClrTypeName("Amazon.Lambda.CognitoEvents"),
                new ClrTypeName("Amazon.Lambda.ConfigEvents"),
                new ClrTypeName("Amazon.Lambda.DynamoDBEvents"),
                new ClrTypeName("Amazon.Lambda.LexEvents"),
                new ClrTypeName("Amazon.Lambda.KinesisAnalyticsEvents"),
                new ClrTypeName("Amazon.Lambda.KinesisEvents"),
                new ClrTypeName("Amazon.Lambda.KinesisFirehoseEvents"),
                new ClrTypeName("Amazon.Lambda.S3Events"),
                new ClrTypeName("Amazon.Lambda.SimpleEmailEvents"),
                new ClrTypeName("Amazon.Lambda.SNSEvents"),
                new ClrTypeName("Amazon.Lambda.SQSEvents")
            };

        private static readonly IClrTypeName StreamTypeName = new ClrTypeName("System.IO.Stream");

        private static readonly HashSet<IClrTypeName> LambdaInterfaces = new HashSet<IClrTypeName>(
            new List<IClrTypeName>
            {
                new ClrTypeName("Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest"),
                new ClrTypeName("Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse")
            });

        private const string HandlerName = "FunctionHandler";








        public double Priority => RunMarkerProviderPriority.DEFAULT;

        public void CollectRunMarkers(IFile file, IContextBoundSettingsStore settings, IHighlightingConsumer consumer)
        {
            if (!(file is ICSharpFile csharpFile)) return;

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

        private bool IsSuitableLambdaMethod(IMethod method)
        {
            return IsPublic(method) &&
                   IsValidInstanceOrStaticMethod(method) &&
                   HasRequiredParameters(method) &&
                   HasRequiredReturnType(method)
        }

        private bool IsPublic(IMethod method)
        {
            return method.GetAccessRights() == AccessRights.PUBLIC;
        }

        private bool IsValidInstanceOrStaticMethod(IMethod method)
        {
            return method.IsStatic && method.ShortName != "Main" ||
                   !method.IsStatic && CanBeInstantiatedByLambda(method.GetContainingType() as IClass);
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
                case 0:
                    return true;

                case 1:
                    return IsAmazonEventType(parameters[0].Type) || IsCustomDataType(parameters[0].Type);

                case 2:
                    return (IsAmazonEventType(parameters[0].Type) || IsCustomDataType(parameters[0].Type))
                           && IsLambdaContextType(parameters[1]);

                default:
                    return false;
            }
        }

        private bool IsAmazonEventType(IType type)
        {
            var clrName = (type as IDeclaredType)?.GetClrName();
            return clrName != null && AmazonLambdaEvents.Any(amazonEventType => amazonEventType.Equals(clrName));
        }

        private bool IsCustomDataType(IType type)
        {
            return type.IsValid();
        }

        private bool IsStreamType(IType type)
        {
            var streamType = new DeclaredTypeFromCLRName(StreamTypeName, new NullableAnnotation(), type.Module);

            return (type as IDeclaredType)?.Equals(streamType) == true ||
                   type.IsSubtypeOf(new DeclaredTypeFromCLRName(StreamTypeName, new NullableAnnotation(), type.Module));
        }

        private bool IsSerializable(IMethod method)
        {

        }

        private bool IsLambdaContextType(IDeclaredElement element)
        {
            var clrName = (element?.Type() as IDeclaredType)?.GetClrName();
            return clrName != null && clrName.Equals(LambdaContextTypeName);
        }

        private bool HasRequiredReturnType(IMethod method)
        {
            var taskReturnType = method.Module.GetPredefinedType().Task;

            return !method.IsAsync && IsCustomDataType(method.ReturnType) ||
                   method.IsAsync && (method.ReturnType.IsVoid() || method.ReturnType.Equals(taskReturnType) ||
                                      method.ReturnType.IsSubtypeOf(taskReturnType));

        }










































































//        /*
//         * this.isPublic &&
//         * this.hasRequiredParameters() &&
//         *
//         * (!this.isStatic || this.name != "main") &&
//         * !this.isConstructor &&
//         * (this.isStatic || parentClass.canBeInstantiatedByLambda()) &&
//         * !(parentClass.implementsLambdaHandlerInterface(file) && this.name == HANDLER_NAME)
//         */
//        private bool IsSuitableLambdaMethod(IMethod method)
//        {
//            return HasRequiredAccessModifiers(method) &&
//                   HasRequiredStaticOrInstanceModifiers(method) &&
//                   HasRequiredParameters(method) &&
//                   !IsConstructor(method) &&
////                   HasRequiredReturnType(method) &&
//                   !(IsImplementLambdaHandlerInterface(method.GetContainingType() as IClass) && method.ShortName == HandlerName);
//        }
//
//        private bool HasRequiredAccessModifiers(IMethod method)
//        {
//            return method.GetAccessRights() == AccessRights.PUBLIC;
//        }
//
//        private bool HasRequiredStaticOrInstanceModifiers(IMethod method)
//        {
//            return (!method.IsStatic || method.ShortName != "Main") &&
//                   (method.IsStatic || CanBeInstantiatedByLambda(method.GetContainingType() as IClass));
//        }
//
//        // this.isPublic && this.isConcrete && this.hasPublicNoArgsConstructor()
//        private bool CanBeInstantiatedByLambda(IClass classElement)
//        {
//            return classElement.GetAccessRights() == AccessRights.PUBLIC &&
//                   !classElement.IsAbstract &&
//                   HasPublicNoArgsConstructor(classElement);
//        }
//
//        // this.constructors.isEmpty() || this.constructors.any { it.hasModifier(JvmModifier.PUBLIC) && it.parameters.isEmpty() }
//        private bool HasPublicNoArgsConstructor(IClass classElement)
//        {
//            return classElement.Constructors.IsEmpty() ||
//                   classElement.Constructors.Any(constructor =>
//                       constructor.GetAccessRights() == AccessRights.PUBLIC && constructor.Parameters.IsEmpty());
//        }
//
//        private bool IsImplementLambdaHandlerInterface(IClass classElement)
//        {
//
//        }
//
//        /**
//         * when (this.parameters.size) {
//            1 -> true
//            2 -> (this.parameterList.parameters[0].isInputStreamParameter() &&
//                    this.parameterList.parameters[1].isOutputStreamParameter()) ||
//                    this.parameterList.parameters[1].isContextParameter()
//            3 -> this.parameterList.parameters[0].isInputStreamParameter() &&
//                    this.parameterList.parameters[1].isOutputStreamParameter() &&
//                    this.parameterList.parameters[2].isContextParameter()
//            else -> false
//         */
//        private bool HasRequiredParameters(IMethod method)
//        {
//            var parameters = method.Parameters;
//            switch (parameters.Count)
//            {
//                case 1:
//                    return true;
//
//                case 2:
//                    return IsInputOutputStream(parameters[0].Type) && IsInputOutputStream(parameters[1].Type) ||
//                           IsLambdaContextParameter(parameters[1].Type);
//
//                case 3:
//                    return IsInputOutputStream(parameters[0].Type) && IsInputOutputStream(parameters[1].Type) &&
//                           IsLambdaContextParameter(parameters[2].Type);
//
//                default:
//                    return false;
//            }
//        }
//
//        private bool HasRequiredReturnType(IMethod method)
//        {
//            return method.IsAsync && method.ReturnType.IsVoid() &&
//                   !method.IsAsync && method.HasAttributeInstance() &&
//        }
//
//        private bool IsInputOutputStream(IType type)
//        {
//            return IsTypesMatches(type, StreamTypeName);
//        }
//
//        private bool IsLambdaContextParameter(IType type)
//        {
//            return IsTypesMatches(type, LambdaContextTypeName);
//        }
//
//        private bool IsConstructor(IMethod method)
//        {
//            return method.GetType().DeclaringMethod?.IsConstructor == true;
//        }
//
//
//
//
//
//
//
//
//
//
//        private bool IsLambdaHandlerModifierValid(IMethod method)
//        {
//            if (!(method is IModifiersOwner methodModifier)) return false;
//            return !(methodModifier.IsAbstract ||
//                     methodModifier.IsExtern ||
//                     methodModifier.IsOverride ||
//                     methodModifier.IsReadonly ||
//                     methodModifier.IsVirtual ||
//                     methodModifier.IsVolatile);
//        }
//
//        private bool IsHandlerParametersValid(IMethod method)
//        {
//            return method.Parameters.Count > 0 &&
//                   method.Parameters.Count <= 2 &&
//                   method.Parameters.Any(parameter =>
//                       (parameter.Type as IDeclaredType)?.GetClrName().FullName == "Amazon.Lambda.Core.ILambdaContext");
//        }
//
//
//
//        private bool IsTypesMatches(IType type, IClrTypeName clrTypeName)
//        {
//            var clrName = (type as IDeclaredType)?.GetClrName();
//            return clrName != null && clrName.Equals(clrTypeName);
//        }

    }
}