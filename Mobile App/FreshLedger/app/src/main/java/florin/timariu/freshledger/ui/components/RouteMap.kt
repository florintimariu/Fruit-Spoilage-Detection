package florin.timariu.freshledger.ui.components

import android.graphics.Color as AndroidColor
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.viewinterop.AndroidView
import florin.timariu.freshledger.data.model.LocationPoint
import org.osmdroid.tileprovider.tilesource.TileSourceFactory
import org.osmdroid.util.GeoPoint
import org.osmdroid.views.MapView
import org.osmdroid.views.overlay.Marker
import org.osmdroid.views.overlay.Polyline

@Composable
fun RouteMap(
    locations: List<LocationPoint>,
    modifier: Modifier = Modifier
) {
    AndroidView(
        modifier = modifier,
        factory = { context ->
            MapView(context).apply {
                setTileSource(TileSourceFactory.MAPNIK)
                setMultiTouchControls(true)
                controller.setZoom(6.0)
            }
        },
        update = { mapView ->
            mapView.overlays.clear()

            if (locations.isNotEmpty()) {
                val geoPoints = locations.map { GeoPoint(it.latitude, it.longitude) }

                // Polyline traseu
                val polyline = Polyline().apply {
                    setPoints(geoPoints)
                    outlinePaint.color = AndroidColor.parseColor("#2196F3")
                    outlinePaint.strokeWidth = 8f
                }
                mapView.overlays.add(polyline)

                // Marker start
                Marker(mapView).apply {
                    position = geoPoints.first()
                    setAnchor(Marker.ANCHOR_CENTER, Marker.ANCHOR_BOTTOM)
                    title = "Start"
                    mapView.overlays.add(this)
                }

                // Marker end
                if (geoPoints.size > 1) {
                    Marker(mapView).apply {
                        position = geoPoints.last()
                        setAnchor(Marker.ANCHOR_CENTER, Marker.ANCHOR_BOTTOM)
                        title = "End"
                        mapView.overlays.add(this)
                    }
                }

                // Centreaza pe traseu
                mapView.controller.setCenter(geoPoints.first())
                mapView.controller.setZoom(10.0)
            }

            mapView.invalidate()
        }
    )
}