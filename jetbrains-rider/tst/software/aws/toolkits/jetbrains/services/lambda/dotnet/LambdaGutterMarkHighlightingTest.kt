package software.aws.toolkits.jetbrains.services.lambda.dotnet

import com.jetbrains.rdclient.daemon.util.attributeId
import com.jetbrains.rdclient.testFramework.waitForDaemon
import com.jetbrains.rider.daemon.util.isBackendGutterMark
import com.jetbrains.rider.test.base.BaseTestWithMarkup
import org.testng.annotations.Test

class LambdaGutterMarkHighlightingTest : BaseTestWithMarkup() {

    companion object {
        private const val LAMBDA_RUN_MARKER_ATTRIBUTE_ID = "AWS Lambda Run Method Gutter Mark"
    }

    override fun getSolutionDirectoryName(): String = "SamHelloWorldApp"

    @Test
    fun testClass_FunctionClass_Detected() = checkLambdaGutterMark()

    @Test
    fun testClass_FunctionStruct_NotDetected() = checkLambdaGutterMark()

    @Test
    fun testClass_FunctionInterface_NotDetected() = checkLambdaGutterMark()

    @Test
    fun testClass_FunctionAbstractClass_NotDetected() = checkLambdaGutterMark()

    @Test
    fun testParameters_NoParameters_NotDetected() = checkLambdaGutterMark()

    @Test
    fun testParameters_SingleCustomDataParameter_Detected() = checkLambdaGutterMark()

    @Test
    fun testParameters_SingleAmazonEventParameter_Detected() = checkLambdaGutterMark()

    @Test
    fun testParameters_SingleTypeInheritedFromAmazonEventParameter_Detected() = checkLambdaGutterMark()

    @Test
    fun testParameters_SingleStreamParameter_Detected() = checkLambdaGutterMark()

    @Test
    fun testParameters_SingleInheritedFromStreamParameter_Detected() = checkLambdaGutterMark()

    @Test
    fun testParameters_TwoParametersCustomDataAndContext_Detected() = checkLambdaGutterMark()

    @Test
    fun testParameters_TwoParametersAmazonEventAndContext_Detected() = checkLambdaGutterMark()

    @Test
    fun testParameters_TwoParametersStreamAndContext_Detected() = checkLambdaGutterMark()

    @Test
    fun testParameters_TwoParametersAmazonEventAndNonContext_NotDetected() = checkLambdaGutterMark()

    @Test
    fun testReturn_SyncStream_Detected() = checkLambdaGutterMark()

    @Test
    fun testReturn_SyncCustomData_Detected() = checkLambdaGutterMark()

    @Test
    fun testReturn_SyncAmazonEvent_Detected() = checkLambdaGutterMark()

    @Test
    fun testReturn_SyncVoid_NotDetected() = checkLambdaGutterMark()

    @Test
    fun testReturn_AsyncVoid_Detected() = checkLambdaGutterMark()

    @Test
    fun testReturn_AsyncTask_Detected() = checkLambdaGutterMark()

    @Test
    fun testReturn_AsyncTaskGeneric_Detected() = checkLambdaGutterMark()

    @Test
    fun testMethod_StaticNonMain_Detected() = checkLambdaGutterMark()

    @Test
    fun testMethod_StaticMain_NotDetected() = checkLambdaGutterMark()

    @Test
    fun testSerializer_MethodLevel_Detected() = checkLambdaGutterMark()

    @Test
    fun testSerializer_MethodLevelInherited_Detected() = checkLambdaGutterMark()

    @Test
    fun testSerializer_MethodLevelNonInherited_NotDetected() = checkLambdaGutterMark()

    @Test
    fun testSerializer_AssemblyLevel_Detected() = checkLambdaGutterMark()

    @Test
    fun testSerializer_AssemblyLevelInherited_Detected() = checkLambdaGutterMark()

    @Test
    fun testSerializer_AssemblyLevelNonInherited_NotDetected() = checkLambdaGutterMark()

    @Test
    fun testSerializer_NoSerializer_NotDetected() = checkLambdaGutterMark()

    private fun checkLambdaGutterMark() {
        doTestWithMarkupModel(
            testFilePath = "src/HelloWorld/Function.cs",
            sourceFileName = "Function.cs",
            goldFileName = "Function.gold"
        ) {
            waitForDaemon()
            dumpHighlightersTree(
                valueFilter = { highlighter ->
                    highlighter.isBackendGutterMark && highlighter.attributeId.contains(LAMBDA_RUN_MARKER_ATTRIBUTE_ID)
                }
            )
        }
    }
}
