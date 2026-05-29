package florin.timariu.freshledger.ui.shipments

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider

class ShipmentsViewModelFactory(
    private val organizationId: String
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        return ShipmentsViewModel(organizationId) as T
    }
}