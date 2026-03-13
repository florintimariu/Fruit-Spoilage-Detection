import { ethers } from "hardhat";

async function main() {
  console.log("Deploying ShipmentTracker...");

  // Get the contract factory
  const ShipmentTracker = await ethers.getContractFactory("ShipmentTracker");
  
  // Deploy the contract
  const tracker = await ShipmentTracker.deploy();
  
  // Wait for deployment to finish
  await tracker.waitForDeployment();

  // Get the deployed contract address
  const address = await tracker.getAddress();
  
  console.log("ShipmentTracker deployed to:", address);
}

// Execute deployment
main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});