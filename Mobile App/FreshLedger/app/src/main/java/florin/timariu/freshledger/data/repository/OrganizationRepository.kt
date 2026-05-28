package florin.timariu.freshledger.data.repository

import florin.timariu.freshledger.data.model.AddMemberByEmailRequest
import florin.timariu.freshledger.data.model.AddMemberRequest
import florin.timariu.freshledger.data.model.CreateOrganizationRequest
import florin.timariu.freshledger.data.model.Organization
import florin.timariu.freshledger.data.remote.ApiClient

class OrganizationRepository {
    private val api = ApiClient.api

    suspend fun getOrganizations(): Result<List<Organization>> = try {
        Result.success(api.getOrganizations())
    } catch (e: Exception) {
        Result.failure(e)
    }

    suspend fun createOrganization(name: String, description: String?): Result<Organization> = try {
        Result.success(api.createOrganization(CreateOrganizationRequest(name, description)))
    } catch (e: Exception) {
        Result.failure(e)
    }

    suspend fun getOrganization(id: String): Result<Organization> = try {
        Result.success(api.getOrganization(id))
    } catch (e: Exception) {
        Result.failure(e)
    }

    suspend fun addMemberByEmail(organizationId: String, email: String, role: String): Result<String> = try {
        val response = api.addMemberByEmail(organizationId, AddMemberByEmailRequest(email, role))
        Result.success(response.email)
    } catch (e: Exception) {
        Result.failure(e)
    }

    suspend fun removeMember(organizationId: String, userId: String): Result<Unit> = try {
        api.removeMember(organizationId, userId)
        Result.success(Unit)
    } catch (e: Exception) {
        Result.failure(e)
    }
}