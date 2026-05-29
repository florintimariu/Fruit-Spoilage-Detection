package florin.timariu.freshledger.ui.shipments

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Inventory2
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import florin.timariu.freshledger.data.model.Shipment
import florin.timariu.freshledger.ui.components.StatsDashboard

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ShipmentsScreen(
    organizationId: String,
    onBack: () -> Unit,
    onShipmentClick: (Shipment) -> Unit,
    onManageOrganization: () -> Unit,
    viewModel: ShipmentsViewModel = viewModel(
        factory = ShipmentsViewModelFactory(organizationId)
    )
) {
    val uiState by viewModel.uiState.collectAsState()
    var showCreateDialog by remember { mutableStateOf(false) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Shipments") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
                actions = {
                    IconButton(onClick = onManageOrganization) {
                        Icon(Icons.Default.Settings, contentDescription = "Manage organization")
                    }
                }
            )
        },
        floatingActionButton = {
            FloatingActionButton(onClick = { showCreateDialog = true }) {
                Icon(Icons.Default.Add, contentDescription = "Create shipment")
            }
        }
    ) { padding ->
        Column(modifier = Modifier.fillMaxSize().padding(padding)) {
            // Dashboard mereu sus (daca exista stats)
            uiState.stats?.let { stats ->
                Box(modifier = Modifier.padding(16.dp)) {
                    StatsDashboard(stats)
                }
            }

            Box(modifier = Modifier.fillMaxSize()) {
                when {
                    uiState.isLoading -> {
                        CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
                    }
                    uiState.shipments.isEmpty() -> {
                        Column(
                            modifier = Modifier.align(Alignment.Center),
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Icon(
                                Icons.Default.Inventory2,
                                contentDescription = null,
                                modifier = Modifier.size(64.dp),
                                tint = MaterialTheme.colorScheme.outline
                            )
                            Spacer(Modifier.height(16.dp))
                            Text("No shipments yet", style = MaterialTheme.typography.bodyLarge)
                        }
                    }
                    else -> {
                        LazyColumn(
                            modifier = Modifier.fillMaxSize(),
                            contentPadding = PaddingValues(16.dp),
                            verticalArrangement = Arrangement.spacedBy(12.dp)
                        ) {
                            items(uiState.shipments) { shipment ->
                                ShipmentCard(shipment, onClick = { onShipmentClick(shipment) })
                            }
                        }
                    }
                }
            }
        }
    }

    if (showCreateDialog) {
        CreateShipmentDialog(
            onDismiss = { showCreateDialog = false },
            onCreate = { name, desc, origin, dest ->
                viewModel.createShipment(name, desc, origin, dest)
                showCreateDialog = false
            }
        )
    }
}

@Composable
fun ShipmentCard(shipment: Shipment, onClick: () -> Unit) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(shipment.productName, style = MaterialTheme.typography.titleMedium)
                StatusChip(shipment.status)
            }
            Spacer(Modifier.height(8.dp))
            Text(
                "${shipment.origin} → ${shipment.destination}",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.outline
            )
            if (shipment.productDescription.isNotBlank()) {
                Text(
                    shipment.productDescription,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.outline
                )
            }
        }
    }
}

@Composable
fun StatusChip(status: String) {
    val (bgColor, label) = when (status) {
        "Created" -> Color(0xFF9E9E9E) to "Created"
        "InProgress" -> Color(0xFF2196F3) to "In Progress"
        "Completed" -> Color(0xFF4CAF50) to "Completed"
        "Compromised" -> Color(0xFFF44336) to "Compromised"
        else -> Color(0xFF9E9E9E) to status
    }
    Surface(
        color = bgColor,
        shape = MaterialTheme.shapes.small
    ) {
        Text(
            label,
            color = Color.White,
            style = MaterialTheme.typography.labelSmall,
            modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp)
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CreateShipmentDialog(
    onDismiss: () -> Unit,
    onCreate: (String, String, String, String) -> Unit
) {
    var name by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }
    var origin by remember { mutableStateOf("") }
    var destination by remember { mutableStateOf("") }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("New Shipment") },
        text = {
            Column {
                OutlinedTextField(
                    value = name, onValueChange = { name = it },
                    label = { Text("Product name") }, singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                Spacer(Modifier.height(8.dp))
                OutlinedTextField(
                    value = description, onValueChange = { description = it },
                    label = { Text("Description (optional)") },
                    modifier = Modifier.fillMaxWidth()
                )
                Spacer(Modifier.height(8.dp))
                OutlinedTextField(
                    value = origin, onValueChange = { origin = it },
                    label = { Text("Origin") }, singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                Spacer(Modifier.height(8.dp))
                OutlinedTextField(
                    value = destination, onValueChange = { destination = it },
                    label = { Text("Destination") }, singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
            }
        },
        confirmButton = {
            TextButton(onClick = {
                if (name.isNotBlank() && origin.isNotBlank() && destination.isNotBlank()) {
                    onCreate(name, description, origin, destination)
                }
            }) { Text("Create") }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("Cancel") }
        }
    )
}