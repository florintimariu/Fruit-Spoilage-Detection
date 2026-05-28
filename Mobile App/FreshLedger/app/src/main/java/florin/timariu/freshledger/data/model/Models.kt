package florin.timariu.freshledger.data.model

import kotlinx.serialization.Serializable

@Serializable
data class Organization(
    val organizationId: String = "",
    val name: String = "",
    val description: String = "",
    val createdByUserId: String = "",
    val members: List<Member> = emptyList()
)

@Serializable
data class Member(
    val userId: String = "",
    val role: String = "Viewer"
)

@Serializable
data class Shipment(
    val shipmentId: String = "",
    val organizationId: String = "",
    val productName: String = "",
    val productDescription: String = "",
    val origin: String = "",
    val destination: String = "",
    val status: String = "Created",
    val currentStepId: String? = null,
    val createdByUserId: String = ""
)

@Serializable
data class Step(
    val stepId: String = "",
    val shipmentId: String = "",
    val type: String = "",
    val locationName: String = "",
    val operatorName: String = "",
    val isCompleted: Boolean = false,
    val minTemp: Double? = null,
    val maxTemp: Double? = null,
    val avgTemp: Double? = null,
    val minHumidity: Double? = null,
    val maxHumidity: Double? = null,
    val avgHumidity: Double? = null,
    val readingsCount: Int = 0,
    val aiStatusAtCompletion: String? = null,
    val dataHash: String? = null,
    val transactionHash: String? = null
)

@Serializable
data class SensorReading(
    val readingId: String = "",
    val shipmentId: String = "",
    val stepId: String = "",
    val sensorIeee: String = "",
    val sensorLogicalId: String = "",
    val sensorType: String = "",
    val value: Double = 0.0,
    val unit: String = ""
)

@Serializable
data class LocationPoint(
    val locationId: String = "",
    val latitude: Double = 0.0,
    val longitude: Double = 0.0,
    val accuracy: Double? = null,
    val speed: Double? = null
)

@Serializable
data class AiInspection(
    val inspectionId: String = "",
    val shipmentId: String = "",
    val stepId: String? = null,
    val imageUrl: String = "",
    val maskUrl: String = "",
    val verdict: String = "",
    val spoilagePercent: Double = 0.0,
    val spoilageDetected: Boolean = false,
    val triggerType: String = ""
)

@Serializable
data class Sensor(
    val ieee: String = "",
    val logicalId: String = "",
    val displayName: String = "",
    val sensorType: String = "",
    val unit: String = "",
    val assignedShipmentId: String? = null,
    val status: String = "Pending"
)

@Serializable
data class OverviewStats(
    val totalShipments: Int = 0,
    val completedShipments: Int = 0,
    val inProgressShipments: Int = 0,
    val compromisedShipments: Int = 0,
    val createdShipments: Int = 0
)

@Serializable
data class VerificationResult(
    val isValid: Boolean = false,
    val status: String = "",
    val storedHash: String? = null,
    val onChainHash: String? = null,
    val transactionHash: String? = null,
    val message: String? = null
)