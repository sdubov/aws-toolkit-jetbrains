using System.Threading;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Host.Env;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;

[assembly: RequiresThread(ApartmentState.STA)]

namespace AWS.Daemon.Test
{
    [ZoneDefinition]
    public interface IAWSTestZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>, IRequire<IRiderPlatformZone>
    {
    }

    [SetUpFixture]
    public sealed class TestEnvironment : AWSExtensionTestEnvironmentAssembly<IAWSTestZone>
    {
    }
}