package florin.timariu.freshledger.data.remote

import florin.timariu.freshledger.data.model.*
import retrofit2.http.*

interface FreshLedgerApi {

    // === User ===
    @GET("api/me")
    suspend fun getMe(): MeResponse

    @POST("api/me/fcm-token")
    suspend fun updateFcmToken(@Body request: UpdateFcmTokenRequest)

    // === Organizations ===
    @GET("api/organizations")
    suspend fun getOrganizations(): List<Organization>

    @GET("api/organizations/{id}")
    suspend fun getOrganization(@Path("id") id: String): Organization

    @POST("api/organizations")
    suspend fun createOrganization(@Body request: CreateOrganizationRequest): Organization

    @POST("api/organizations/{id}/members")
    suspend fun addMember(
        @Path("id") organizationId: String,
        @Body request: AddMemberRequest
    )

    @DELETE("api/organizations/{id}/members/{userId}")
    suspend fun removeMember(
        @Path("id") organizationId: String,
        @Path("userId") userId: String
    )

    // === Stats ===
    @GET("api/organizations/{id}/stats/overview")
    suspend fun getOverview(
        @Path("id") organizationId: String,
        @Query("period") period: String? = null
    ): OverviewStats

    // === Shipments ===
    @GET("api/shipments")
    suspend fun getShipments(@Query("organizationId") organizationId: String): List<Shipment>

    @GET("api/shipments/{id}")
    suspend fun getShipment(@Path("id") id: String): Shipment

    @POST("api/shipments")
    suspend fun createShipment(@Body request: CreateShipmentRequest): Shipment

    // === Steps ===
    @GET("api/shipments/{shipmentId}/steps")
    suspend fun getSteps(@Path("shipmentId") shipmentId: String): List<Step>

    @POST("api/shipments/{shipmentId}/steps")
    suspend fun startStep(
        @Path("shipmentId") shipmentId: String,
        @Body request: StartStepRequest
    ): Step

    @POST("api/shipments/{shipmentId}/steps/{stepId}/complete")
    suspend fun completeStep(
        @Path("shipmentId") shipmentId: String,
        @Path("stepId") stepId: String,
        @Body request: CompleteStepRequest
    ): CompleteStepResponse

    // === Readings ===
    @GET("api/shipments/{shipmentId}/steps/{stepId}/readings")
    suspend fun getReadings(
        @Path("shipmentId") shipmentId: String,
        @Path("stepId") stepId: String
    ): List<SensorReading>

    @GET("api/shipments/{shipmentId}/steps/{stepId}/locations")
    suspend fun getLocations(
        @Path("shipmentId") shipmentId: String,
        @Path("stepId") stepId: String
    ): List<LocationPoint>

    // === Verification ===
    @GET("api/shipments/{shipmentId}/steps/{stepId}/verify")
    suspend fun verifyStep(
        @Path("shipmentId") shipmentId: String,
        @Path("stepId") stepId: String
    ): VerificationResult

    // === Inspections ===
    @GET("api/shipments/{shipmentId}/inspections")
    suspend fun getInspections(@Path("shipmentId") shipmentId: String): List<AiInspection>

    // === Sensors ===
    @GET("api/sensors")
    suspend fun getSensors(): List<Sensor>

    // === Members by mail ===
    @POST("api/organizations/{id}/members/by-email")
    suspend fun addMemberByEmail(
        @Path("id") organizationId: String,
        @Body request: AddMemberByEmailRequest
    ): AddMemberByEmailResponse
}
