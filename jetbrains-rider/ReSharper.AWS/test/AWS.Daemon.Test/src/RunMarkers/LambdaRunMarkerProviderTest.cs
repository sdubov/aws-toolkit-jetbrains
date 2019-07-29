using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using NUnit.Framework;

namespace AWS.Daemon.Test.RunMarkers
{
    [TestFixture, TestProjectName("CSharpHighlightingTest")]
    class LambdaRunMarkerProviderTest : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"RunMarkers\LambdaRunMarkerProviderTest";

        [Test]
        public void testCollectRunMarkers_StructFunctionClass_FunctionNotDetected() { DoNamedTest2(); }
    }
}
