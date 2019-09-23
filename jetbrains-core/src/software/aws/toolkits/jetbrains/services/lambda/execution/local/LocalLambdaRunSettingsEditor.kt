// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

package software.aws.toolkits.jetbrains.services.lambda.execution.local

import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import software.aws.toolkits.jetbrains.services.lambda.LambdaBuilder
import software.aws.toolkits.jetbrains.services.lambda.RuntimeGroup
import software.aws.toolkits.jetbrains.services.lambda.completion.HandlerCompletionProvider
import software.aws.toolkits.jetbrains.utils.ui.selected
import javax.swing.JComponent

class LocalLambdaRunSettingsEditor(project: Project) : SettingsEditor<LocalLambdaRunConfiguration>() {
    private val view = LocalLambdaRunSettingsEditorPanel(project, HandlerCompletionProvider(project))

    init {
        val supported = LambdaBuilder.supportedRuntimeGroups.flatMap { it.runtimes }.sorted()
        val selected = RuntimeGroup.determineRuntime(project)?.let { if (it in supported) it else null }

        view.setRuntimes(supported)
        view.runtime.selectedItem = selected
    }

    override fun createEditor(): JComponent = view.panel

    override fun resetEditorFrom(configuration: LocalLambdaRunConfiguration) {
        view.useTemplate.isSelected = configuration.isUsingTemplate()
        if (configuration.isUsingTemplate()) {
            view.runtime.isEnabled = false
            view.setTemplateFile(configuration.templateFile())
            view.selectFunction(configuration.logicalId())
        } else {
            view.setTemplateFile(null) // Also clears the functions selector
            view.runtime.model.selectedItem = configuration.runtime()
            view.handler.text = configuration.handler() ?: ""
        }

        view.timeoutSlider.value = configuration.timeout()
        view.memorySlider.value = configuration.memorySize()
        view.environmentVariables.envVars = configuration.environmentVariables()

        if (configuration.isUsingInputFile()) {
            view.lambdaInput.inputFile = configuration.inputSource()
        } else {
            view.lambdaInput.inputText = configuration.inputSource()
        }
    }

    override fun applyEditorTo(configuration: LocalLambdaRunConfiguration) {
        if (view.useTemplate.isSelected) {
            configuration.useTemplate(view.templateFile.text, view.function.selected()?.logicalName)
        } else {
            configuration.useHandler(view.runtime.selected(), view.handler.text)
        }

        configuration.timeout(view.timeoutSlider.value)
        configuration.memorySize(view.memorySlider.value)
        configuration.environmentVariables(view.environmentVariables.envVars)
        if (view.lambdaInput.isUsingFile) {
            configuration.useInputFile(view.lambdaInput.inputFile)
        } else {
            configuration.useInputText(view.lambdaInput.inputText)
        }
    }
}
