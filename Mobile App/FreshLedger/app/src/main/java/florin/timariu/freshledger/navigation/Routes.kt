package florin.timariu.freshledger.navigation

object Routes {
    const val LOGIN = "login"
    const val ORGANIZATIONS = "organizations"
    const val SHIPMENTS = "shipments/{organizationId}"
    const val SHIPMENT_DETAIL = "shipment/{shipmentId}"

    const val ORGANIZATION_DETAIL = "organization/{organizationId}"
    fun shipments(organizationId: String) = "shipments/$organizationId"
    fun shipmentDetail(shipmentId: String) = "shipment/$shipmentId"

    fun organizationDetail(organizationId: String) = "organization/$organizationId"
}