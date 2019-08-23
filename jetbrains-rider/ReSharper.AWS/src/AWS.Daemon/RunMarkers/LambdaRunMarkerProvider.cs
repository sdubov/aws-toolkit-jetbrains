using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.ProjectsHost.Dependencies;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Features.RunMarkers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Logging;
using ReSharper.AWS.Lambda;
using IMethodDeclaration = JetBrains.ReSharper.Psi.CSharp.Tree.IMethodDeclaration;

namespace ReSharper.AWS.RunMarkers
{
    [Language(typeof(CSharpLanguage))]
    public class LambdaRunMarkerProvider : IRunMarkerProvider
    {
        private ILogger myLogger = Logger.GetLogger<LambdaRunMarkerProvider>();

        public double Priority => RunMarkerProviderPriority.DEFAULT;

        public void CollectRunMarkers(IFile file, IContextBoundSettingsStore settings, IHighlightingConsumer consumer)
        {
            if (!IsLambdaProjectType(file.GetProject())) return;
            if (!(file is ICSharpFile csharpFile)) return;

            foreach (var declaration in CachedDeclarationsCollector.Run<IMethodDeclaration>(csharpFile))
            {
                if (!(declaration.DeclaredElement is IMethod method)) continue;
                if (!LambdaFinder.IsSuitableLambdaMethod(method, myLogger)) continue;

                var range = declaration.GetNameDocumentRange();

                var highlighting = new RunMarkerHighlighting(declaration,
                    LambdaRunMarkerAttributeIds.LAMBDA_RUN_METHOD_MARKER_ID, range,
                    file.GetPsiModule().TargetFrameworkId);

                consumer.AddHighlighting(highlighting, range);
            }
        }

        private bool IsLambdaProjectType([CanBeNull] IProject project)
        {
            if (project == null) return false;
            if (!project.IsDotNetCoreProject()) return false;

            var dependencyManager = project.GetSolution().GetComponent<ProjectDependenciesManager>();
            var descriptor = dependencyManager.GetDescriptor(project);
            if (descriptor != null && descriptor.RootDependencies.Any(dependency => dependency.Name.Contains("Amazon.Lambda.Core")))
                return true;

            return project
                .FindProjectItemsByLocation(
                    project.ProjectFileLocation.Parent.Combine("aws-lambda-tools-defaults.json")).Any();
        }
    }
}
