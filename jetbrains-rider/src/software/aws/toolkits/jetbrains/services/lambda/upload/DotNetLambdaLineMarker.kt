// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

package software.aws.toolkits.jetbrains.services.lambda.upload

import com.intellij.codeInsight.daemon.LineMarkerInfo
import com.intellij.codeInsight.daemon.LineMarkerProviderDescriptor
import com.intellij.psi.PsiElement
import software.aws.toolkits.resources.message
import javax.swing.Icon

/**
 * Rider show gutter marks from backend. Disable all frontend gutter marks logic.
 */
class DotNetLambdaLineMarker : LineMarkerProviderDescriptor() {

    override fun getName(): String? = message("lambda.service_name")
    override fun getIcon(): Icon? = null
    override fun getLineMarkerInfo(element: PsiElement): LineMarkerInfo<*>? = null
    override fun collectSlowLineMarkers(elements: MutableList<PsiElement>, result: MutableCollection<LineMarkerInfo<PsiElement>>) {}
}
