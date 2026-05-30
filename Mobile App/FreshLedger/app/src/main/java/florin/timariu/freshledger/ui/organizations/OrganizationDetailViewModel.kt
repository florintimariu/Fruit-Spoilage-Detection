package florin.timariu.freshledger.ui.organizations

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.google.firebase.auth.FirebaseAuth
import florin.timariu.freshledger.data.model.Organization
import florin.timariu.freshledger.data.repository.OrganizationRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class OrganizationDetailUiState(
    val isLoading: Boolean = false,
    val organization: Organization? = null,
    val currentUserRole: String? = null,
    val errorMessage: String? = null,
    val actionInProgress: Boolean = false
)

class OrganizationDetailViewModel(
    private val organizationId: String,
    private val repository: OrganizationRepository = OrganizationRepository()
) : ViewModel() {

    private val _uiState = MutableStateFlow(OrganizationDetailUiState())
    val uiState: StateFlow<OrganizationDetailUiState> = _uiState.asStateFlow()

    init {
        load()
    }

    fun load() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            val result = repository.getOrganization(organizationId)
            val org = result.getOrNull()
            val currentUid = FirebaseAuth.getInstance().currentUser?.uid
            val role = org?.members?.firstOrNull { it.userId == currentUid }?.role
            _uiState.value = _uiState.value.copy(
                isLoading = false,
                organization = org,
                currentUserRole = role,
                errorMessage = result.exceptionOrNull()?.message
            )
        }
    }

    fun addMemberByEmail(email: String, role: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(actionInProgress = true, errorMessage = null)
            val result = repository.addMemberByEmail(organizationId, email, role)
            _uiState.value = _uiState.value.copy(actionInProgress = false)
            if (result.isSuccess) load()
            else _uiState.value = _uiState.value.copy(
                errorMessage = result.exceptionOrNull()?.message ?: "Failed to add member"
            )
        }
    }

    fun removeMember(userId: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(actionInProgress = true)
            val result = repository.removeMember(organizationId, userId)
            _uiState.value = _uiState.value.copy(actionInProgress = false)
            if (result.isSuccess) load()
            else _uiState.value = _uiState.value.copy(
                errorMessage = result.exceptionOrNull()?.message
            )
        }
    }
}

class OrganizationDetailViewModelFactory(
    private val organizationId: String
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        return OrganizationDetailViewModel(organizationId) as T
    }
}