package florin.timariu.freshledger.ui.shipments

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import florin.timariu.freshledger.data.model.CreateShipmentRequest
import florin.timariu.freshledger.data.model.OverviewStats
import florin.timariu.freshledger.data.model.Shipment
import florin.timariu.freshledger.data.repository.ShipmentRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class ShipmentsUiState(
    val isLoading: Boolean = false,
    val shipments: List<Shipment> = emptyList(),
    val errorMessage: String? = null,
    val stats: OverviewStats? = null
)

class ShipmentsViewModel(
    private val organizationId: String,
    private val repository: ShipmentRepository = ShipmentRepository()
) : ViewModel() {

    private val _uiState = MutableStateFlow(ShipmentsUiState())
    val uiState: StateFlow<ShipmentsUiState> = _uiState.asStateFlow()

    init {
        loadShipments()
        loadStats()
    }

    fun loadShipments() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            val result = repository.getShipments(organizationId)
            _uiState.value = if (result.isSuccess) {
                _uiState.value.copy(isLoading = false, shipments = result.getOrThrow())
            } else {
                _uiState.value.copy(
                    isLoading = false,
                    errorMessage = result.exceptionOrNull()?.message ?: "Failed to load"
                )
            }
        }
    }

    fun loadStats() {
        viewModelScope.launch {
            val result = repository.getOverview(organizationId, "month")
            if (result.isSuccess) {
                _uiState.value = _uiState.value.copy(stats = result.getOrNull())
            }
        }
    }

    fun createShipment(
        productName: String,
        productDescription: String,
        origin: String,
        destination: String
    ) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true)
            val result = repository.createShipment(
                CreateShipmentRequest(
                    organizationId = organizationId,
                    productName = productName,
                    productDescription = productDescription.ifBlank { null },
                    origin = origin,
                    destination = destination
                )
            )
            if (result.isSuccess) {
                loadShipments()
            } else {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = result.exceptionOrNull()?.message
                )
            }
        }
    }
}