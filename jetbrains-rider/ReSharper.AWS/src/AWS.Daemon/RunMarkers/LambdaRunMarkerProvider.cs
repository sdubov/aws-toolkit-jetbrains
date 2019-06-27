using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Features.RunMarkers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharper.AWS.RunMarkers
{
    [Language(typeof(CSharpLanguage))]
    public class LambdaRunMarkerProvider : IRunMarkerProvider
    {
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
            return method.GetAccessRights() == AccessRights.PUBLIC &&
                   IsLambdaHandlerModifierValid(method) &&
                   IsHandlerParametersValid(method);
        }

        private bool IsLambdaHandlerModifierValid(IMethod method)
        {
            if (!(method is IModifiersOwner methodModifier)) return false;
            return !(methodModifier.IsAbstract ||
                     methodModifier.IsExtern ||
                     methodModifier.IsOverride ||
                     methodModifier.IsReadonly ||
                     methodModifier.IsVirtual ||
                     methodModifier.IsVolatile);
        }

        private bool IsHandlerParametersValid(IMethod method)
        {
            return method.Parameters.Count > 0 &&
                   method.Parameters.Count <= 2 &&
                   method.Parameters.Any(parameter =>
                       (parameter.Type as IDeclaredType)?.GetClrName().FullName == "Amazon.Lambda.Core.ILambdaContext");
        }
    }
}
