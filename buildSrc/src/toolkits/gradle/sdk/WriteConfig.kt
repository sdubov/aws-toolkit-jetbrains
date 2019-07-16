package toolkits.gradle.sdk

import org.gradle.api.DefaultTask
import org.gradle.api.tasks.TaskAction
import java.io.File

open class WriteConfig : DefaultTask() {

    lateinit var configFile: File
    lateinit var configText: String

    @TaskAction
    fun generate() {
        configFile.writeTextIfChanged(configText)
    }

    private fun File.writeTextIfChanged(content: String) {
        val bytes = content.toByteArray()

        if (!this.exists() || this.readBytes().toHexString() != bytes.toHexString()) {
            println("Writing ${this.path}")
            this.writeBytes(bytes)
        }
    }

    private fun ByteArray.toHexString() = joinToString("") { "%02x".format(it) }

}