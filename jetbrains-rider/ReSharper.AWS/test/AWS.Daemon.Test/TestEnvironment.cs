using System.Threading;
using JetBrains.TestFramework;
using NUnit.Framework;

[assembly: RequiresThread(ApartmentState.STA)]

namespace AWS.Daemon.Test
{
    [SetUpFixture]
    public sealed class TestEnvironment : ExtensionTestEnvironmentAssembly<AWSTestZone>
    {
    }
}