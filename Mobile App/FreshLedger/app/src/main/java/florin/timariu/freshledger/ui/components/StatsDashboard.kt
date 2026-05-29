package florin.timariu.freshledger.ui.components

import androidx.compose.foundation.layout.*
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import florin.timariu.freshledger.data.model.OverviewStats

@Composable
fun StatsDashboard(stats: OverviewStats) {
    Column(modifier = Modifier.fillMaxWidth()) {
        Text(
            "Overview (last 30 days)",
            style = MaterialTheme.typography.titleMedium,
            modifier = Modifier.padding(bottom = 8.dp)
        )
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            StatCard("Total", stats.totalShipments, Color(0xFF607D8B), Modifier.weight(1f))
            StatCard("Active", stats.inProgressShipments, Color(0xFF2196F3), Modifier.weight(1f))
        }
        Spacer(Modifier.height(8.dp))
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            StatCard("Completed", stats.completedShipments, Color(0xFF4CAF50), Modifier.weight(1f))
            StatCard("Compromised", stats.compromisedShipments, Color(0xFFF44336), Modifier.weight(1f))
        }
    }
}

@Composable
private fun StatCard(label: String, value: Int, color: Color, modifier: Modifier = Modifier) {
    Card(modifier = modifier) {
        Column(
            modifier = Modifier.fillMaxWidth().padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                value.toString(),
                style = MaterialTheme.typography.headlineMedium,
                fontWeight = FontWeight.Bold,
                color = color
            )
            Text(label, style = MaterialTheme.typography.bodySmall)
        }
    }
}