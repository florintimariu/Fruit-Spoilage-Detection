package florin.timariu.freshledger.ui.shipments

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.layout.height
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.VerifiedUser
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import florin.timariu.freshledger.data.model.Step
import florin.timariu.freshledger.ui.components.SensorChart
import florin.timariu.freshledger.ui.components.RouteMap

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ShipmentDetailScreen(
    shipmentId: String,
    onBack: () -> Unit,
    viewModel: ShipmentDetailViewModel = viewModel(
        factory = ShipmentDetailViewModelFactory(shipmentId)
    )
) {
    val uiState by viewModel.uiState.collectAsState()
    var showStartStepDialog by remember { mutableStateOf(false) }
    var stepToComplete by remember { mutableStateOf<Step?>(null) }

    val activeStep = uiState.steps.firstOrNull { !it.isCompleted }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(uiState.shipment?.productName ?: "Shipment") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        },
        floatingActionButton = {
            if (activeStep == null && uiState.shipment?.status != "Completed") {
                ExtendedFloatingActionButton(
                    onClick = { showStartStepDialog = true },
                    icon = { Icon(Icons.Default.Add, null) },
                    text = { Text("Start Step") }
                )
            }
        }
    ) { padding ->
        if (uiState.isLoading) {
            Box(Modifier.fillMaxSize().padding(padding), contentAlignment = Alignment.Center) {
                CircularProgressIndicator()
            }
            return@Scaffold
        }

        LazyColumn(
            modifier = Modifier.fillMaxSize().padding(padding),
            contentPadding = PaddingValues(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            // Header cu info shipment
            uiState.shipment?.let { shipment ->
                item {
                    ShipmentInfoCard(shipment.origin, shipment.destination, shipment.status)
                }
            }

            item {
                Text("Steps", style = MaterialTheme.typography.titleLarge)
            }

            if (uiState.steps.isEmpty()) {
                item {
                    Text(
                        "No steps yet. Tap 'Start Step' to begin tracking.",
                        color = MaterialTheme.colorScheme.outline
                    )
                }
            }

            items(uiState.steps) { step ->
                StepCard(
                    step = step,
                    onComplete = { stepToComplete = step },
                    onVerify = { viewModel.verifyStep(step.stepId) },
                    onViewData = { viewModel.loadReadings(step.stepId) },
                    onViewRoute = { viewModel.loadLocations(step.stepId) },
                    isVerifying = uiState.verifyingStepId == step.stepId
                )
            }
        }
    }

    // Dialog start step
    if (showStartStepDialog) {
        StartStepDialog(
            onDismiss = { showStartStepDialog = false },
            onStart = { type, location, operator ->
                viewModel.startStep(type, location, operator)
                showStartStepDialog = false
            }
        )
    }

    // Dialog complete step
    stepToComplete?.let { step ->
        CompleteStepDialog(
            onDismiss = { stepToComplete = null },
            onComplete = { aiStatus ->
                viewModel.completeStep(step.stepId, aiStatus)
                stepToComplete = null
            }
        )
    }

    // Dialog rezultat verificare
    uiState.verificationResult?.let { result ->
        VerificationDialog(
            result = result,
            onDismiss = { viewModel.clearVerification() }
        )
    }

    // Dialog cu grafice readings
    if (uiState.readingsStepId != null) {
        ReadingsDialog(
            isLoading = uiState.loadingReadings,
            readings = uiState.selectedStepReadings,
            onDismiss = { viewModel.closeReadings() }
        )
    }

    // Dialog cu harta
    if (uiState.showMapStepId != null) {
        AlertDialog(
            onDismissRequest = { viewModel.closeMap() },
            title = { Text("GPS Route") },
            text = {
                if (uiState.loadingLocations) {
                    Box(Modifier.fillMaxWidth().height(100.dp), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator()
                    }
                } else if (uiState.locations.isEmpty()) {
                    Text("No GPS data for this step.")
                } else {
                    RouteMap(
                        locations = uiState.locations,
                        modifier = Modifier.fillMaxWidth().height(350.dp)
                    )
                }
            },
            confirmButton = { TextButton(onClick = { viewModel.closeMap() }) { Text("Close") } }
        )
    }
}

@Composable
fun ShipmentInfoCard(origin: String, destination: String, status: String) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Text("Route", style = MaterialTheme.typography.labelMedium)
                StatusChip(status)
            }
            Spacer(Modifier.height(4.dp))
            Text("$origin → $destination", style = MaterialTheme.typography.bodyLarge)
        }
    }
}

@Composable
fun StepCard(
    step: Step,
    onComplete: () -> Unit,
    onVerify: () -> Unit,
    onViewData: () -> Unit,
    onViewRoute: () -> Unit,
    isVerifying: Boolean
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp)) {

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(step.type, style = MaterialTheme.typography.titleMedium)
                if (step.isCompleted) {
                    Icon(
                        Icons.Default.CheckCircle,
                        contentDescription = "Completed",
                        tint = Color(0xFF4CAF50)
                    )
                } else {
                    AssistChip(onClick = {}, label = { Text("Active") })
                }
            }

            Text(
                step.locationName,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.outline
            )
            Text(
                "Operator: ${step.operatorName}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.outline
            )

            Spacer(Modifier.height(8.dp))
            step.avgTemp?.let {
                Text(
                    "Temperature: ${step.minTemp}°C / ${"%.1f".format(it)}°C / ${step.maxTemp}°C (min/avg/max)",
                    style = MaterialTheme.typography.bodySmall
                )
            }
            step.avgHumidity?.let {
                Text(
                    "Humidity: ${step.minHumidity}% / ${"%.1f".format(it)}% / ${step.maxHumidity}%",
                    style = MaterialTheme.typography.bodySmall
                )
            }
            Text("Readings: ${step.readingsCount}", style = MaterialTheme.typography.bodySmall)
            step.aiStatusAtCompletion?.let {
                Text("AI status: $it", style = MaterialTheme.typography.bodySmall)
            }

            if (step.isCompleted) {
                step.transactionHash?.let { tx ->
                    Spacer(Modifier.height(8.dp))
                    Text(
                        "Anchored on-chain",
                        style = MaterialTheme.typography.labelSmall,
                        color = Color(0xFF4CAF50)
                    )
                    Text(
                        tx,
                        style = MaterialTheme.typography.bodySmall,
                        fontFamily = FontFamily.Monospace,
                        color = MaterialTheme.colorScheme.outline,
                        maxLines = 1
                    )
                }
            }

            Spacer(Modifier.height(12.dp))
            HorizontalDivider()
            Spacer(Modifier.height(12.dp))

            OutlinedButton(onClick = onViewData, modifier = Modifier.fillMaxWidth()) {
                Text("View Sensor Data")
            }

            Spacer(Modifier.height(8.dp))
            OutlinedButton(onClick = onViewRoute, modifier = Modifier.fillMaxWidth()) {
                Text("View GPS Route")
            }

            Spacer(Modifier.height(12.dp))
            HorizontalDivider()
            Spacer(Modifier.height(12.dp))

            if (step.isCompleted) {
                Button(
                    onClick = onVerify,
                    enabled = !isVerifying,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    if (isVerifying) {
                        CircularProgressIndicator(Modifier.size(18.dp), strokeWidth = 2.dp)
                    } else {
                        Icon(Icons.Default.VerifiedUser, null, Modifier.size(18.dp))
                        Spacer(Modifier.width(8.dp))
                        Text("Verify Integrity")
                    }
                }
            } else {
                Button(onClick = onComplete, modifier = Modifier.fillMaxWidth()) {
                    Text("Complete Step")
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun StartStepDialog(
    onDismiss: () -> Unit,
    onStart: (String, String, String) -> Unit
) {
    val stepTypes = listOf("Harvest", "Warehouse", "Transport", "Retail")
    var selectedType by remember { mutableStateOf(stepTypes[0]) }
    var expanded by remember { mutableStateOf(false) }
    var location by remember { mutableStateOf("") }
    var operator by remember { mutableStateOf("") }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Start New Step") },
        text = {
            Column {
                ExposedDropdownMenuBox(
                    expanded = expanded,
                    onExpandedChange = { expanded = it }
                ) {
                    OutlinedTextField(
                        value = selectedType,
                        onValueChange = {},
                        readOnly = true,
                        label = { Text("Type") },
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded) },
                        modifier = Modifier.menuAnchor().fillMaxWidth()
                    )
                    ExposedDropdownMenu(
                        expanded = expanded,
                        onDismissRequest = { expanded = false }
                    ) {
                        stepTypes.forEach { type ->
                            DropdownMenuItem(
                                text = { Text(type) },
                                onClick = {
                                    selectedType = type
                                    expanded = false
                                }
                            )
                        }
                    }
                }
                Spacer(Modifier.height(8.dp))
                OutlinedTextField(
                    value = location, onValueChange = { location = it },
                    label = { Text("Location") }, singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                Spacer(Modifier.height(8.dp))
                OutlinedTextField(
                    value = operator, onValueChange = { operator = it },
                    label = { Text("Operator") }, singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
            }
        },
        confirmButton = {
            TextButton(onClick = {
                if (location.isNotBlank() && operator.isNotBlank()) {
                    onStart(selectedType, location, operator)
                }
            }) { Text("Start") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Cancel") } }
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CompleteStepDialog(
    onDismiss: () -> Unit,
    onComplete: (String) -> Unit
) {
    val verdicts = listOf("Fresh", "Warning", "Spoiled")
    var selected by remember { mutableStateOf(verdicts[0]) }
    var expanded by remember { mutableStateOf(false) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Complete Step") },
        text = {
            Column {
                Text("This will anchor the step data on the blockchain.")
                Spacer(Modifier.height(12.dp))
                ExposedDropdownMenuBox(
                    expanded = expanded,
                    onExpandedChange = { expanded = it }
                ) {
                    OutlinedTextField(
                        value = selected,
                        onValueChange = {},
                        readOnly = true,
                        label = { Text("AI Status") },
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded) },
                        modifier = Modifier.menuAnchor().fillMaxWidth()
                    )
                    ExposedDropdownMenu(
                        expanded = expanded,
                        onDismissRequest = { expanded = false }
                    ) {
                        verdicts.forEach { v ->
                            DropdownMenuItem(
                                text = { Text(v) },
                                onClick = { selected = v; expanded = false }
                            )
                        }
                    }
                }
            }
        },
        confirmButton = {
            TextButton(onClick = { onComplete(selected) }) { Text("Complete & Anchor") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Cancel") } }
    )
}

@Composable
fun VerificationDialog(
    result: florin.timariu.freshledger.data.model.VerificationResult,
    onDismiss: () -> Unit
) {
    val color = if (result.isValid) Color(0xFF4CAF50) else Color(0xFFF44336)
    AlertDialog(
        onDismissRequest = onDismiss,
        title = {
            Text(
                if (result.isValid) "✓ Integrity Verified" else "⚠ Integrity Compromised",
                color = color
            )
        },
        text = {
            Column {
                Text(result.message ?: "")
                Spacer(Modifier.height(8.dp))
                Text("Status: ${result.status}", style = MaterialTheme.typography.labelMedium)
            }
        },
        confirmButton = { TextButton(onClick = onDismiss) { Text("Close") } }
    )
}

@Composable
fun ReadingsDialog(
    isLoading: Boolean,
    readings: List<florin.timariu.freshledger.data.model.SensorReading>,
    onDismiss: () -> Unit
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Sensor Data") },
        text = {
            if (isLoading) {
                Box(Modifier.fillMaxWidth().height(100.dp), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator()
                }
            } else if (readings.isEmpty()) {
                Text("No sensor readings for this step.")
            } else {
                Column(modifier = Modifier.verticalScroll(rememberScrollState())) {
                    val tempReadings = readings.filter { it.sensorType == "Temperature" }
                    val humidityReadings = readings.filter { it.sensorType == "Humidity" }

                    if (tempReadings.isNotEmpty()) {
                        SensorChart("Temperature", tempReadings, "°C")
                    }
                    if (humidityReadings.isNotEmpty()) {
                        SensorChart("Humidity", humidityReadings, "%")
                    }
                }
            }
        },
        confirmButton = { TextButton(onClick = onDismiss) { Text("Close") } }
    )
}