using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.RiderTutorials.Utils;
using JetBrains.Util;

namespace ReSharper.AWS.Lambda
{
    [SolutionComponent]
    public class LambdaHost
    {
        private readonly LambdaModel myModel;
        private readonly ISymbolCache mySymbolCache;
        private readonly PsiIconManager myPsiIconManager;
        private readonly IconHost myIconHost;
        private readonly ILogger myLogger;

        public LambdaHost(ISolution solution, ISymbolCache symbolCache, PsiIconManager psiIconManager, IconHost iconHost, ILogger logger)
        {
            myModel = solution.GetProtocolSolution().GetLambdaModel();
            mySymbolCache = symbolCache;
            myPsiIconManager = psiIconManager;
            myIconHost = iconHost;
            myLogger = logger;

            myModel.DetermineHandlers.Set((lifetime, unit) =>
            {
                var task = new RdTask<List<HandlerCompletionItem>>();
                task.Set(DetermineHandlers(solution));
                return task;
            });
        }

        public void RunLambda(string methodName, string handler)
        {
            myModel.RunLambda(new LambdaRequest(methodName, handler));
        }

        public void DebugLambda(string methodName, string handler)
        {
            myModel.DebugLambda(new LambdaRequest(methodName, handler));
        }

        public void CreateNewLambda(string methodName, string handler)
        {
            myModel.CreateNewLambda(new LambdaRequest(methodName, handler));
        }

        private List<HandlerCompletionItem> DetermineHandlers(ISolution solution)
        {
            var handlers = new List<HandlerCompletionItem>();

            using (ReadLockCookie.Create())
            {
                var projects = solution.GetAllProjects();

                foreach (var project in projects)
                {
                    var psiModules = project.GetPsiModules();

                    foreach (var psiModule in psiModules)
                    {
                        using (CompilationContextCookie.OverrideOrCreate(psiModule.GetContextFromModule()))
                        {
                            var scope = mySymbolCache.GetSymbolScope(psiModule, false, true);
                            var namespaces = scope.GlobalNamespace.GetNestedNamespaces(scope);

                            foreach (var @namespace in namespaces)
                            {
                                var classes = @namespace.GetNestedElements(scope).OfType<IClass>();
                                foreach (var @class in classes)
                                {
                                    var methods = @class.Methods;
                                    foreach (var method in methods)
                                    {
                                        if (LambdaFinder.IsSuitableLambdaMethod(method, myLogger))
                                        {
                                            var handlerString = ComposeHandlerString(project, method);
                                            var iconId = myPsiIconManager.GetImage(method.GetElementType());
                                            var iconModel = myIconHost.Transform(iconId);
                                            handlers.Add(new HandlerCompletionItem(handlerString, iconModel));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return handlers;
        }

        [NotNull]
        private static string ComposeHandlerString(IProject project, [NotNull] IMethod method)
        {
            if (project == null) return "";

            var assemblyName = project.GetOutputAssemblyName(project.GetCurrentTargetFrameworkId());

            var containingType = method.GetContainingType();
            if (containingType == null) return "";

            var typeString = containingType.GetFullClrName();

            var methodName = method.ShortName;

            return $"{assemblyName}::{typeString}::{methodName}";
        }
    }
}