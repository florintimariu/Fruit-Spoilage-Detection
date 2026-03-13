// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

contract ShipmentTracker {
    event StepLogged(
        string shipmentId,
        string stepId,
        string aiStatus,
        uint256 timestamp
    );

    function logStep(
        string memory _shipmentId,
        string memory _stepId,
        string memory _aiStatus
    ) public {
        emit StepLogged(_shipmentId, _stepId, _aiStatus, block.timestamp);
    }
}