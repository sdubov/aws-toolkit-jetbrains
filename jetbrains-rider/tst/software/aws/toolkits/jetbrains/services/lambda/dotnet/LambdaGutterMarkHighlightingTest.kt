// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

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
    fun testClass_FunctionClass_Detected() = verifyLambdaGutterMark()

    @Test
    fun testClass_FunctionStruct_NotDetected() = verifyLambdaGutterMark()

    @Test
    fun testClass_FunctionInterface_NotDetected() = verifyLambdaGutterMark()

    @Test
    fun testClass_FunctionAbstractClass_NotDetected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_NoParameters_NotDetected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterAmazonEvent_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterTypeInheritedFromAmazonEvent_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterStream_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterInheritedFromStream_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterCustomDataPrimitive_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterCustomDataArray_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterCustomDataIList_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterCustomDataGenericIList_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterCustomDataIEnumerable_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterCustomDataGenericIEnumerable_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterCustomDataIDictionary_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterCustomDataGenericIDictionary_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterCustomDataWithoutDefaultConstructor_NotDetected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_SingleParameterCustomDataTypeSubstitution_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_TwoParametersCustomDataAndContext_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_TwoParametersAmazonEventAndContext_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_TwoParametersStreamAndContext_Detected() = verifyLambdaGutterMark()

    @Test
    fun testParameters_TwoParametersAmazonEventAndNonContext_NotDetected() = verifyLambdaGutterMark()

    @Test
    fun testReturn_SyncStream_Detected() = verifyLambdaGutterMark()

    @Test
    fun testReturn_SyncCustomData_Detected() = verifyLambdaGutterMark()

    @Test
    fun testReturn_SyncAmazonEvent_Detected() = verifyLambdaGutterMark()

    @Test
    fun testReturn_SyncVoid_NotDetected() = verifyLambdaGutterMark()

    @Test
    fun testReturn_AsyncVoid_Detected() = verifyLambdaGutterMark()

    @Test
    fun testReturn_AsyncTask_Detected() = verifyLambdaGutterMark()

    @Test
    fun testReturn_AsyncTaskGeneric_Detected() = verifyLambdaGutterMark()

    @Test
    fun testMethod_StaticNonMain_Detected() = verifyLambdaGutterMark()

    @Test
    fun testMethod_StaticMain_NotDetected() = verifyLambdaGutterMark()

    @Test
    fun testMethod_StaticMainWithSerializer_Detected() = verifyLambdaGutterMark()

    @Test
    fun testSerializer_MethodLevel_Detected() = verifyLambdaGutterMark()

    @Test
    fun testSerializer_MethodLevelInherited_Detected() = verifyLambdaGutterMark()

    @Test
    fun testSerializer_MethodLevelNonInherited_NotDetected() = verifyLambdaGutterMark()

    @Test
    fun testSerializer_AssemblyLevel_Detected() = verifyLambdaGutterMark()

    @Test
    fun testSerializer_AssemblyLevelInherited_Detected() = verifyLambdaGutterMark()

    @Test
    fun testSerializer_AssemblyLevelNonInherited_NotDetected() = verifyLambdaGutterMark()

    @Test
    fun testSerializer_NoSerializer_NotDetected() = verifyLambdaGutterMark()

    private fun verifyLambdaGutterMark() {
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
