// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract SupplyChainLedger {
    
    enum ActionType { Added, Deducted }
    
    struct Record {
        string lotNumber;
        ActionType action;
        uint256 quantity;
        uint256 timestamp;
        address user;
    }
    
    // Mapping from lotNumber to an array of its history records
    mapping(string => Record[]) private lotHistory;
    
    // Event to be emitted whenever a new record is added
    event StateChanged(string indexed lotNumber, ActionType action, uint256 quantity, uint256 timestamp, address indexed user);
    
    // Function to log an action (Added or Deducted) for a specific lot number
    function logAction(string memory _lotNumber, ActionType _action, uint256 _quantity) public {
        Record memory newRecord = Record({
            lotNumber: _lotNumber,
            action: _action,
            quantity: _quantity,
            timestamp: block.timestamp,
            user: msg.sender
        });
        
        lotHistory[_lotNumber].push(newRecord);
        
        emit StateChanged(_lotNumber, _action, _quantity, block.timestamp, msg.sender);
    }
    
    // Function to retrieve the complete history of a specific lot number
    function getHistory(string memory _lotNumber) public view returns (Record[] memory) {
        return lotHistory[_lotNumber];
    }
}
