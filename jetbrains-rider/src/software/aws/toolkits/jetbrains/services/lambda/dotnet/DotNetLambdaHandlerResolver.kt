// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

package software.aws.toolkits.jetbrains.services.lambda.dotnet

import com.intellij.openapi.progress.ProgressManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.psi.NavigatablePsiElement
import com.intellij.psi.PsiElement
import com.intellij.psi.search.GlobalSearchScope
import com.intellij.psi.util.contextOfType
import com.intellij.psi.util.parentOfType
import com.jetbrains.rd.framework.impl.RpcTimeouts
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.threading.SpinWait
import com.jetbrains.rider.ideaInterop.fileTypes.csharp.CSharpLanguage
import com.jetbrains.rider.model.MethodExistingRequest
import com.jetbrains.rider.model.RdMethodInfo
import com.jetbrains.rider.model.backendPsiHelperModel
import com.jetbrains.rider.model.publishableProjectsModel
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.configurations.method.getProjectModeId
import com.jetbrains.rider.util.idea.application
import software.aws.toolkits.jetbrains.services.lambda.LambdaHandlerResolver
import software.aws.toolkits.jetbrains.services.lambda.dotnet.element.RiderLambdaHandlerFakePsiElement
import java.time.Duration

class DotNetLambdaHandlerResolver : LambdaHandlerResolver {

    companion object {
        private const val handlerValidationTimeoutMs = 2000L
    }

    override fun version(): Int = 1

    override fun findPsiElements(
        project: Project,
        handler: String,
        searchScope: GlobalSearchScope
    ): Array<NavigatablePsiElement> {

        val handlerParts = handler.split("::")
        if (handlerParts.size != 3) return emptyArray()

        val assemblyName = handlerParts[0]
        val type = handlerParts[1]
        val methodName = handlerParts[2]

        val projectModelViewHost = ProjectModelViewHost.getInstance(project)
        val publishableProjects = project.solution.publishableProjectsModel.publishableProjects.values.toList()
        val projectToProcess = publishableProjects.find { it.projectName == assemblyName } ?: return emptyArray()

        val methodForHandler: RdMethodInfo? = project.solution.backendPsiHelperModel.findPublicMethod
                .sync(MethodExistingRequest(
                        className = type,
                        methodName = methodName,
                        targetFramework = "",
                        projectId = projectModelViewHost.getProjectModeId(projectToProcess.projectFilePath)
                ), RpcTimeouts.default)

        val fieldId = methodForHandler?.fileId ?: return emptyArray()

        return arrayOf(RiderLambdaHandlerFakePsiElement(project, "$assemblyName::$type::$methodName", fieldId))
    }

    /**
     * public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
     *
     * returnType handler-name(inputType input, ILambdaContext context) {
     *
     * HelloWorld::HelloWorld.Function::FunctionHandler
     */
    override fun determineHandler(element: PsiElement): String? {
        if (element.language != CSharpLanguage) return null
        if (element.text != "ILambdaContext") return null

        val assemblyName = element.containingFile.name
        val type = "HelloWorld.Function"
        val methodName = "FunctionHandler"
        return "$assemblyName::$type::$methodName"
    }

    override fun determineHandlers(element: PsiElement, file: VirtualFile): Set<String> {
        val handler = determineHandler(element) ?: return emptySet()
        return setOf(handler)
    }

    override fun isHandlerValid(project: Project, handler: String): Boolean {
        val handlerParts = handler.split("::")
        if (handlerParts.size != 3) return false

        val assemblyName = handlerParts[0]
        val type = handlerParts[1]
        val methodName = handlerParts[2]

        var isMethodExists = false

        if (application.isDispatchThread) {
            isMethodExists = isMethodExists(project, assemblyName, type, methodName)
        } else {
            var isCompleted = false
            application.invokeLater {
                isMethodExists = isMethodExists(project, assemblyName, type, methodName)
                isCompleted = true
            }

            SpinWait.spinUntil(Lifetime.Eternal, Duration.ofMillis(handlerValidationTimeoutMs)) {
                ProgressManager.checkCanceled()
                isCompleted
            }
        }

        return isMethodExists
    }

    /**
     * Check if we should show a line marker to run Lambda based on
     */
    override fun shouldShowLineMarker(handler: String): Boolean {
        return handler.split("::").size == 3
    }

    private fun isMethodExists(project: Project, assemblyName: String, type: String, methodName: String): Boolean {
        val projectModelViewHost = ProjectModelViewHost.getInstance(project)
        val projects = project.solution.publishableProjectsModel.publishableProjects.values.toList()
        val projectToProcess = projects.find { it.projectName == assemblyName } ?: return false

        return project.solution.backendPsiHelperModel.isMethodExists
                .sync(MethodExistingRequest(
                        className = type,
                        methodName = methodName,
                        targetFramework = "",
                        projectId = projectModelViewHost.getProjectModeId(projectToProcess.projectFilePath)
                ), RpcTimeouts.default)
    }
}