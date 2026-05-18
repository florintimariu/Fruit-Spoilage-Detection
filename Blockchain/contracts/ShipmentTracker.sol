// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

contract ShipmentTracker {
    event StepLogged(
        string shipmentId,
        string stepId,
        string aiStatus,
        bytes32 dataHash,
        uint256 timestamp
    );

    function logStep(
        string memory _shipmentId,
        string memory _stepId,
        string memory _aiStatus,
        bytes32 _dataHash
    ) public {
        emit StepLogged(_shipmentId, _stepId, _aiStatus, _dataHash, block.timestamp);
    }
}