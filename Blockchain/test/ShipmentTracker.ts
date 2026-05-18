import { expect } from "chai";
import { ethers } from "hardhat";
import { ShipmentTracker } from "../typechain-types";
import { time } from "@nomicfoundation/hardhat-network-helpers";

describe("ShipmentTracker", function () {
  let tracker: ShipmentTracker;

  beforeEach(async function () {
    const ShipmentTrackerFactory = await ethers.getContractFactory("ShipmentTracker");
    tracker = await ShipmentTrackerFactory.deploy();
    await tracker.waitForDeployment();
  });

  describe("logStep", function () {
    it("should emit StepLogged event with correct parameters", async function () {
      const shipmentId = "SHIP-001";
      const stepId = "STEP-ARRIVAL";
      const aiStatus = "OK";

      // Call the function and capture the event
      await expect(tracker.logStep(shipmentId, stepId, aiStatus))
        .to.emit(tracker, "StepLogged")
        .withArgs(shipmentId, stepId, aiStatus, (timestamp: bigint) => {
          // timestamp should be a recent block timestamp (within reasonable range)
          const now = Math.floor(Date.now() / 1000);
          expect(timestamp).to.be.closeTo(now, 60); // allow 60 seconds difference
          return true;
        });
    });

    it("should allow multiple log entries with different data", async function () {
      // Log first step
      const tx1 = await tracker.logStep("SHIP-001", "STEP-DEPART", "OK");
      await tx1.wait();

      // Log second step
      const tx2 = await tracker.logStep("SHIP-001", "STEP-ARRIVAL", "WARNING");
      await tx2.wait();

      // Query events to verify both exist
      const filter = tracker.filters.StepLogged();
      const events = await tracker.queryFilter(filter);

      expect(events.length).to.equal(2);
      expect(events[0].args.shipmentId).to.equal("SHIP-001");
      expect(events[0].args.stepId).to.equal("STEP-DEPART");
      expect(events[0].args.aiStatus).to.equal("OK");
      
      expect(events[1].args.shipmentId).to.equal("SHIP-001");
      expect(events[1].args.stepId).to.equal("STEP-ARRIVAL");
      expect(events[1].args.aiStatus).to.equal("WARNING");
    });

    it("should handle empty strings", async function () {
      await expect(tracker.logStep("", "", ""))
        .to.emit(tracker, "StepLogged")
        .withArgs("", "", "", (timestamp: bigint) => true);
    });

    it("should include a valid timestamp", async function () {
      // Get current block timestamp before transaction
      const blockNumBefore = await ethers.provider.getBlockNumber();
      const blockBefore = await ethers.provider.getBlock(blockNumBefore);
      const timestampBefore = blockBefore?.timestamp || 0;

      // Send transaction
      const tx = await tracker.logStep("SHIP-002", "STEP-TEST", "OK");
      const receipt = await tx.wait();

      // Get timestamp of block where transaction was mined
      const blockAfter = await ethers.provider.getBlock(receipt!.blockNumber);
      const timestampAfter = blockAfter?.timestamp || 0;

      // Query the event and check its timestamp
      const events = await tracker.queryFilter(tracker.filters.StepLogged());
      const event = events[events.length - 1];
      
      expect(event.args.timestamp).to.equal(timestampAfter);
      expect(event.args.timestamp).to.be.at.least(timestampBefore);
    });
  });
});