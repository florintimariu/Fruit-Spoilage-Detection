package florin.timariu.freshledger.data.model

import kotlinx.serialization.Serializable

@Serializable
data class CreateOrganizationRequest(
    val name: String,
    val description: String? = null
)

@Serializable
data class AddMemberRequest(
    val userId: String,
    val role: String
)

@Serializable
data class CreateShipmentRequest(
    val organizationId: String,
    val productName: String,
    val productDescription: String? = null,
    val origin: String,
    val destination: String
)

@Serializable
data class StartStepRequest(
    val type: String,
    val locationName: String,
    val operatorName: String
)

@Serializable
data class CompleteStepRequest(
    val aiStatus: String
)

@Serializable
data class UpdateFcmTokenRequest(
    val fcmToken: String
)

@Serializable
data class MeResponse(
    val userId: String = ""
)

@Serializable
data class CompleteStepResponse(
    val step: Step? = null,
    val transactionHash: String? = null,
    val anchoringSucceeded: Boolean = false,
    val errorMessage: String? = null
)
@Serializable
data class AddMemberByEmailRequest(
    val email: String,
    val role: String
)

@Serializable
data class AddMemberByEmailResponse(
    val message: String = "",
    val userId: String = "",
    val email: String = ""
)