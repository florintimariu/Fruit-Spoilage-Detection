import { expect } from "chai";
import { ethers } from "hardhat";
import { ShipmentTracker } from "../typechain-types";

describe("ShipmentTracker", function () {
  let tracker: ShipmentTracker;

  beforeEach(async function () {
    const ShipmentTrackerFactory = await ethers.getContractFactory("ShipmentTracker");
    tracker = await ShipmentTrackerFactory.deploy();
    await tracker.waitForDeployment();
  });

  it("should emit StepLogged event with correct parameters", async function () {
    const shipmentId = "SHIP123";
    const stepId = "STEP456";
    const aiStatus = "OK";

    // Call the function and capture the event
    await expect(tracker.logStep(shipmentId, stepId, aiStatus))
      .to.emit(tracker, "StepLogged")
      .withArgs(shipmentId, stepId, aiStatus, (timestamp: bigint) => {
        // timestamp should be a recent block timestamp (within reasonable range)
        expect(timestamp).to.be.closeTo(
          Math.floor(Date.now() / 1000),
          60 // allow 60 seconds difference
        );
        return true;
      });
  });
});