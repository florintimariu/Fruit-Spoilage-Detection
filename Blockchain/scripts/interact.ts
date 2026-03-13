import { ethers } from "hardhat";

async function main() {
  // Replace with your deployed contract address
  const contractAddress = "0x6c1975669f94dE615a32EA36E15578d3020d5f18"; 
  
  // Get the contract instance
  const tracker = await ethers.getContractAt("ShipmentTracker", contractAddress);

  // Call logStep
  const tx = await tracker.logStep("SHIP002", "STEP_DEPART", "WARNING");
  await tx.wait();
  console.log("Step logged. Transaction hash:", tx.hash);

  // Optional: Query events from the last 10 blocks only
  const currentBlock = await ethers.provider.getBlockNumber();
  const fromBlock = currentBlock - 9; // last 10 blocks
  const events = await tracker.queryFilter(tracker.filters.StepLogged(), fromBlock, currentBlock);
  
  console.log("Recent events:", events.map(e => ({
    shipmentId: e.args.shipmentId,
    stepId: e.args.stepId,
    aiStatus: e.args.aiStatus,
    timestamp: new Date(Number(e.args.timestamp) * 1000).toISOString()
  })));
}

main().catch(console.error);