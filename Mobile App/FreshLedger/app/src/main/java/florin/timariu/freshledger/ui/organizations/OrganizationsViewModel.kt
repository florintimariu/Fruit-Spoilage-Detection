package florin.timariu.freshledger.ui.organizations

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import florin.timariu.freshledger.data.model.Organization
import florin.timariu.freshledger.data.repository.OrganizationRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class OrganizationsUiState(
    val isLoading: Boolean = false,
    val organizations: List<Organization> = emptyList(),
    val errorMessage: String? = null
)

class OrganizationsViewModel(
    private val repository: OrganizationRepository = OrganizationRepository()
) : ViewModel() {

    private val _uiState = MutableStateFlow(OrganizationsUiState())
    val uiState: StateFlow<OrganizationsUiState> = _uiState.asStateFlow()

    init {
        loadOrganizations()
    }

    fun loadOrganizations() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            val result = repository.getOrganizations()
            _uiState.value = if (result.isSuccess) {
                _uiState.value.copy(isLoading = false, organizations = result.getOrThrow())
            } else {
                _uiState.value.copy(
                    isLoading = false,
                    errorMessage = result.exceptionOrNull()?.message ?: "Failed to load"
                )
            }
        }
    }

    fun createOrganization(name: String, description: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true)
            val result = repository.createOrganization(name, description.ifBlank { null })
            if (result.isSuccess) {
                loadOrganizations()
            } else {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = result.exceptionOrNull()?.message
                )
            }
        }
    }
}