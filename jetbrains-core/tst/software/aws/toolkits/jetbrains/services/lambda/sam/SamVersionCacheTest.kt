package software.aws.toolkits.jetbrains.services.lambda.sam

import org.assertj.core.api.Assertions.assertThat
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Assume
import org.junit.Rule
import org.junit.Test

class SamVersionCacheTest {

    @Test
    fun samVersion_EmptyCache() {
        SamVersionCache.evaluate()


    }
}
