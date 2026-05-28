package florin.timariu.freshledger.data.repository

import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.messaging.FirebaseMessaging
import florin.timariu.freshledger.data.model.UpdateFcmTokenRequest
import florin.timariu.freshledger.data.remote.ApiClient
import kotlinx.coroutines.tasks.await

class AuthRepository {
    private val auth = FirebaseAuth.getInstance()

    val currentUserId: String?
        get() = auth.currentUser?.uid

    val isLoggedIn: Boolean
        get() = auth.currentUser != null

    suspend fun login(email: String, password: String): Result<Unit> {
        return try {
            auth.signInWithEmailAndPassword(email, password).await()
            Result.success(Unit)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun register(email: String, password: String): Result<Unit> {
        return try {
            auth.createUserWithEmailAndPassword(email, password).await()
            Result.success(Unit)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    fun logout() {
        auth.signOut()
    }

    suspend fun registerFcmToken() {
        try {
            val token = FirebaseMessaging.getInstance().token.await()
            ApiClient.api.updateFcmToken(UpdateFcmTokenRequest(token))
        } catch (_: Exception) {
            // nu blocheaza login-ul daca FCM esueaza
        }
    }
}