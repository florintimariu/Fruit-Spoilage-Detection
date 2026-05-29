package florin.timariu.freshledger.ui.shipments

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import florin.timariu.freshledger.data.model.*
import florin.timariu.freshledger.data.repository.ShipmentRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class ShipmentDetailUiState(
    val isLoading: Boolean = false,
    val shipment: Shipment? = null,
    val steps: List<Step> = emptyList(),
    val errorMessage: String? = null,
    val actionInProgress: Boolean = false,
    val verificationResult: VerificationResult? = null,
    val verifyingStepId: String? = null,
    val selectedStepReadings: List<SensorReading> = emptyList(),
    val loadingReadings: Boolean = false,
    val readingsStepId: String? = null,
    val locations: List<LocationPoint> = emptyList(),
    val loadingLocations: Boolean = false,
    val showMapStepId: String? = null
)

class ShipmentDetailViewModel(
    private val shipmentId: String,
    private val repository: ShipmentRepository = ShipmentRepository()
) : ViewModel() {

    private val _uiState = MutableStateFlow(ShipmentDetailUiState())
    val uiState: StateFlow<ShipmentDetailUiState> = _uiState.asStateFlow()

    init {
        load()
    }

    fun load() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)

            val shipmentResult = repository.getShipment(shipmentId)
            val stepsResult = repository.getSteps(shipmentId)

            _uiState.value = _uiState.value.copy(
                isLoading = false,
                shipment = shipmentResult.getOrNull(),
                steps = stepsResult.getOrNull()?.sortedBy { it.isCompleted } ?: emptyList(),
                errorMessage = shipmentResult.exceptionOrNull()?.message
            )
        }
    }

    fun startStep(type: String, locationName: String, operatorName: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(actionInProgress = true)
            val result = repository.startStep(
                shipmentId,
                StartStepRequest(type, locationName, operatorName)
            )
            _uiState.value = _uiState.value.copy(actionInProgress = false)
            if (result.isSuccess) load()
            else _uiState.value = _uiState.value.copy(
                errorMessage = result.exceptionOrNull()?.message
            )
        }
    }

    fun completeStep(stepId: String, aiStatus: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(actionInProgress = true)
            val result = repository.completeStep(shipmentId, stepId, aiStatus)
            _uiState.value = _uiState.value.copy(actionInProgress = false)
            if (result.isSuccess) load()
            else _uiState.value = _uiState.value.copy(
                errorMessage = result.exceptionOrNull()?.message
            )
        }
    }

    fun verifyStep(stepId: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(verifyingStepId = stepId, verificationResult = null)
            val result = repository.verifyStep(shipmentId, stepId)
            _uiState.value = _uiState.value.copy(
                verifyingStepId = null,
                verificationResult = result.getOrNull()
            )
        }
    }

    fun clearVerification() {
        _uiState.value = _uiState.value.copy(verificationResult = null)
    }

    fun loadReadings(stepId: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(
                loadingReadings = true,
                readingsStepId = stepId,
                selectedStepReadings = emptyList()
            )
            val result = repository.getReadings(shipmentId, stepId)
            _uiState.value = _uiState.value.copy(
                loadingReadings = false,
                selectedStepReadings = result.getOrNull() ?: emptyList()
            )
        }
    }

    fun closeReadings() {
        _uiState.value = _uiState.value.copy(
            readingsStepId = null,
            selectedStepReadings = emptyList()
        )
    }

    fun loadLocations(stepId: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(
                loadingLocations = true,
                showMapStepId = stepId,
                locations = emptyList()
            )
            val result = repository.getLocations(shipmentId, stepId)
            _uiState.value = _uiState.value.copy(
                loadingLocations = false,
                locations = result.getOrNull() ?: emptyList()
            )
        }
    }

    fun closeMap() {
        _uiState.value = _uiState.value.copy(showMapStepId = null, locations = emptyList())
    }
}

class ShipmentDetailViewModelFactory(
    private val shipmentId: String
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        return ShipmentDetailViewModel(shipmentId) as T
    }
}