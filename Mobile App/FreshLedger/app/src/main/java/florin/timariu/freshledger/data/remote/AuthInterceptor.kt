package florin.timariu.freshledger.data.remote

import com.google.firebase.auth.FirebaseAuth
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.tasks.await
import okhttp3.Interceptor
import okhttp3.Response

class AuthInterceptor : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val original = chain.request()

        // Obtine token-ul curent Firebase (blocant, dar pe thread de networking OkHttp)
        val token = runBlocking {
            try {
                FirebaseAuth.getInstance().currentUser
                    ?.getIdToken(false)?.await()?.token
            } catch (e: Exception) {
                null
            }
        }

        val request = if (token != null) {
            original.newBuilder()
                .addHeader("Authorization", "Bearer $token")
                .build()
        } else {
            original
        }

        return chain.proceed(request)
    }
}