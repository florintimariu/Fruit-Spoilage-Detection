package florin.timariu.freshledger.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.*
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.unit.dp
import florin.timariu.freshledger.data.model.SensorReading

@Composable
fun SensorChart(
    title: String,
    readings: List<SensorReading>,
    unit: String,
    lineColor: Color = MaterialTheme.colorScheme.primary
) {
    if (readings.isEmpty()) return

    val values = readings.map { it.value }
    val minVal = values.min()
    val maxVal = values.max()
    val range = (maxVal - minVal).takeIf { it > 0 } ?: 1.0

    Column(modifier = Modifier.fillMaxWidth().padding(vertical = 8.dp)) {
        Text(title, style = MaterialTheme.typography.titleSmall)
        Spacer(Modifier.height(8.dp))

        Canvas(
            modifier = Modifier
                .fillMaxWidth()
                .height(180.dp)
                .padding(vertical = 8.dp)
        ) {
            val w = size.width
            val h = size.height
            val stepX = if (values.size > 1) w / (values.size - 1) else w

            // Linie grafic
            val path = Path()
            values.forEachIndexed { i, value ->
                val x = stepX * i
                val normalized = ((value - minVal) / range).toFloat()
                val y = h - (normalized * h)
                if (i == 0) path.moveTo(x, y) else path.lineTo(x, y)
            }
            drawPath(path, color = lineColor, style = Stroke(width = 4f))

            // Puncte
            values.forEachIndexed { i, value ->
                val x = stepX * i
                val normalized = ((value - minVal) / range).toFloat()
                val y = h - (normalized * h)
                drawCircle(color = lineColor, radius = 5f, center = Offset(x, y))
            }
        }

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text("Min: ${"%.1f".format(minVal)}$unit",
                style = MaterialTheme.typography.bodySmall)
            Text("Avg: ${"%.1f".format(values.average())}$unit",
                style = MaterialTheme.typography.bodySmall)
            Text("Max: ${"%.1f".format(maxVal)}$unit",
                style = MaterialTheme.typography.bodySmall)
        }
    }
}