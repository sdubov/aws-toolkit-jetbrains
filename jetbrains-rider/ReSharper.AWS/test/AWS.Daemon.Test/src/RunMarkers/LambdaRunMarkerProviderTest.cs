using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using NUnit.Framework;

namespace AWS.Daemon.Test.RunMarkers
{
    class LambdaRunMarkerProviderTest : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"RunMarkers\LambdaRunMarkerProviderTest";

        [Test] public void testClass_FunctionClass_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testClass_FunctionStruct_LambdaNotDetected() { DoNamedTest2(); }
        [Test] public void testClass_FunctionInterface_LambdaNotDetected() { DoNamedTest2(); }
        [Test] public void testClass_FunctionAbstractClass_LambdaNotDetected() { DoNamedTest2(); }
        [Test] public void testParameters_NoParameters_LambdaNotDetected() { DoNamedTest2(); }
        [Test] public void testParameters_SingleCustomDataParameter_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testParameters_SingleAmazonEventParameter_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testParameters_SingleStreamParameter_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testParameters_TwoParametersCustomDataAndContext_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testParameters_TwoParametersAmazonEventAndContext_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testParameters_TwoParametersStreamAndContext_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testParameters_TwoParametersAmazonEventAndNonContext_LambdaNotDetected() { DoNamedTest2(); }
        [Test] public void testReturn_SyncStream_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testReturn_SyncCustomData_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testReturn_SyncAmazonEvent_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testReturn_SyncVoid_LambdaNotDetected() { DoNamedTest2(); }
        [Test] public void testReturn_AsyncVoid_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testReturn_AsyncTask_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testReturn_AsyncCustomData_LambdaNotDetected() { DoNamedTest2(); }
        [Test] public void testMethod_StaticNonMain_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testMethod_StaticMain_LambdaNotDetected() { DoNamedTest2(); }
        [Test] public void testMethod_InstanceMain_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testSerializer_MethodLevel_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testSerializer_MethodLevelInherited_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testSerializer_MethodLevelNonInherited_LambdaNotDetected() { DoNamedTest2(); }
        [Test] public void testSerializer_AssemblyLevel_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testSerializer_AssemblyLevelInherited_LambdaDetected() { DoNamedTest2(); }
        [Test] public void testSerializer_AssemblyLevelNonInherited_LambdaNotDetected() { DoNamedTest2(); }
        [Test] public void testSerializer_NoSerializer_LambdaNotDetected() { DoNamedTest2(); }
    }
}
