package florin.timariu.freshledger.data.repository

import florin.timariu.freshledger.data.model.CompleteStepRequest
import florin.timariu.freshledger.data.model.CompleteStepResponse
import florin.timariu.freshledger.data.model.CreateShipmentRequest
import florin.timariu.freshledger.data.model.LocationPoint
import florin.timariu.freshledger.data.model.OverviewStats
import florin.timariu.freshledger.data.model.SensorReading
import florin.timariu.freshledger.data.model.Shipment
import florin.timariu.freshledger.data.model.StartStepRequest
import florin.timariu.freshledger.data.model.Step
import florin.timariu.freshledger.data.model.VerificationResult
import florin.timariu.freshledger.data.remote.ApiClient

class ShipmentRepository {
    private val api = ApiClient.api

    suspend fun getShipments(organizationId: String): Result<List<Shipment>> = try {
        Result.success(api.getShipments(organizationId))
    } catch (e: Exception) {
        Result.failure(e)
    }

    suspend fun getShipment(shipmentId: String): Result<Shipment> = try {
        Result.success(api.getShipment(shipmentId))
    } catch (e: Exception) {
        Result.failure(e)
    }

    suspend fun createShipment(request: CreateShipmentRequest): Result<Shipment> = try {
        Result.success(api.createShipment(request))
    } catch (e: Exception) {
        Result.failure(e)
    }
    suspend fun getSteps(shipmentId: String): Result<List<Step>> = try {
        Result.success(api.getSteps(shipmentId))
    } catch (e: Exception) {
        Result.failure(e)
    }

    suspend fun startStep(shipmentId: String, request: StartStepRequest): Result<Step> = try {
        Result.success(api.startStep(shipmentId, request))
    } catch (e: Exception) {
        Result.failure(e)
    }

    suspend fun completeStep(
        shipmentId: String,
        stepId: String,
        aiStatus: String
    ): Result<CompleteStepResponse> = try {
        Result.success(api.completeStep(shipmentId, stepId, CompleteStepRequest(aiStatus)))
    } catch (e: Exception) {
        Result.failure(e)
    }

    suspend fun getReadings(shipmentId: String, stepId: String): Result<List<SensorReading>> = try {
        Result.success(api.getReadings(shipmentId, stepId))
    } catch (e: Exception) {
        Result.failure(e)
    }

    suspend fun verifyStep(shipmentId: String, stepId: String): Result<VerificationResult> = try {
        Result.success(api.verifyStep(shipmentId, stepId))
    } catch (e: Exception) {
        Result.failure(e)
    }

    suspend fun getLocations(shipmentId: String, stepId: String): Result<List<LocationPoint>> = try {
        Result.success(api.getLocations(shipmentId, stepId))
    } catch (e: Exception) {
        Result.failure(e)
    }

    suspend fun getOverview(organizationId: String, period: String): Result<OverviewStats> = try {
        Result.success(api.getOverview(organizationId, period))
    } catch (e: Exception) {
        Result.failure(e)
    }
}