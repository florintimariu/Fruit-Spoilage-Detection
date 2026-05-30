package florin.timariu.freshledger.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.google.firebase.auth.FirebaseAuth
import florin.timariu.freshledger.ui.auth.LoginScreen
import florin.timariu.freshledger.ui.organizations.OrganizationsScreen

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Text
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.navigation.NavType
import androidx.navigation.navArgument
import florin.timariu.freshledger.ui.organizations.OrganizationDetailScreen
import florin.timariu.freshledger.ui.shipments.ShipmentDetailScreen
import florin.timariu.freshledger.ui.shipments.ShipmentsScreen

@Composable
fun AppNavigation() {
    val navController = rememberNavController()
    val startDestination = if (FirebaseAuth.getInstance().currentUser != null) {
        Routes.ORGANIZATIONS
    } else {
        Routes.LOGIN
    }

    NavHost(navController = navController, startDestination = startDestination) {

        composable(Routes.LOGIN) {
            LoginScreen(
                onLoginSuccess = {
                    navController.navigate(Routes.ORGANIZATIONS) {
                        popUpTo(Routes.LOGIN) { inclusive = true }
                    }
                }
            )
        }

        composable(Routes.ORGANIZATIONS) {
            OrganizationsScreen(
                onOrganizationClick = { org ->
                    navController.navigate(Routes.shipments(org.organizationId))
                },
                onLogout = {
                    navController.navigate(Routes.LOGIN) {
                        popUpTo(0) { inclusive = true }
                    }
                }
            )
        }

        composable(
            route = Routes.SHIPMENTS,
            arguments = listOf(navArgument("organizationId") { type = NavType.StringType })
        ) { backStackEntry ->
            val organizationId = backStackEntry.arguments?.getString("organizationId") ?: ""
            ShipmentsScreen(
                organizationId = organizationId,
                onBack = { navController.popBackStack() },
                onShipmentClick = { shipment ->
                    navController.navigate(Routes.shipmentDetail(shipment.shipmentId))
                },
                onManageOrganization = {
                    navController.navigate(Routes.organizationDetail(organizationId))
                }
            )
        }

        composable(
            route = Routes.SHIPMENT_DETAIL,
            arguments = listOf(navArgument("shipmentId") { type = NavType.StringType })
        ) { backStackEntry ->
            val shipmentId = backStackEntry.arguments?.getString("shipmentId") ?: ""
            ShipmentDetailScreen(
                shipmentId = shipmentId,
                onBack = { navController.popBackStack() }
            )
        }

        composable(
            route = Routes.ORGANIZATION_DETAIL,
            arguments = listOf(navArgument("organizationId") { type = NavType.StringType })
        ) { backStackEntry ->
            val orgId = backStackEntry.arguments?.getString("organizationId") ?: ""
            OrganizationDetailScreen(
                organizationId = orgId,
                onBack = { navController.popBackStack() }
            )
        }
    }
}