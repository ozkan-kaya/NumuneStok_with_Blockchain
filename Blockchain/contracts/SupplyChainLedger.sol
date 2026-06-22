// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract SupplyChainLedger {

    enum ActionType { 
        Added,        // 0 - Stoğa eklendi (eski yöntem, artık kullanılmıyor)
        Deducted,     // 1 - Stoktan düşüldü
        Produced,     // 2 — Üretici tarafından üretildi
        Shipped,      // 3 — Sevkiyata çıkarıldı
        Received,     // 4 — Depoda teslim alındı
        Transferred,  // 5 — Başka bir noktaya transfer edildi
        Consumed,     // 6 — Laboratuvarda tüketildi
        Genesis       // 7 - Sistem açılışındaki mevcut stok başlangıç kaydı
    }

    enum ActorRole {
        None,
        Producer,
        Warehouse,
        Laboratory,
        Admin
    }

    enum LotState {
        None,
        Produced,
        Shipped,
        Received,
        Transferred,
        Consumed
    }

    struct Record {
        string lotNumber;
        ActionType action;
        uint256 quantity;
        uint256 timestamp;
        address user;
        string fromLocation;  // Nereden (ör: "Üretici - Abbott")
        string toLocation;    // Nereye (ör: "Merkez Depo")
    }

    struct LotStatus {
        bool exists;
        LotState state;
        uint256 onChainQuantity;
        uint256 pendingQuantity;
    }

    address public owner;

    mapping(address => ActorRole) public actorRoles;
    mapping(string => Record[]) private lotHistory;
    mapping(string => LotStatus) private lotStatuses;

    event StateChanged(
        string indexed lotNumber, 
        ActionType action, 
        uint256 quantity, 
        uint256 timestamp, 
        address indexed user,
        string fromLocation,
        string toLocation
    );

    event ActorRoleChanged(address indexed actor, ActorRole role);

    constructor() {
        owner = msg.sender;
        actorRoles[msg.sender] = ActorRole.Admin;
        emit ActorRoleChanged(msg.sender, ActorRole.Admin);
    }

    modifier onlyAdmin() {
        require(_isAdmin(msg.sender), "Only admin can manage actors");
        _;
    }

    function setActorRole(address _actor, ActorRole _role) external onlyAdmin {
        require(_actor != address(0), "Actor address is required");
        actorRoles[_actor] = _role;
        emit ActorRoleChanged(_actor, _role);
    }

    function logAction(
        string memory _lotNumber, 
        ActionType _action, 
        uint256 _quantity,
        string memory _fromLocation,
        string memory _toLocation
    ) public {
        _logAction(_lotNumber, _action, _quantity, _fromLocation, _toLocation);
    }

    function logActions(
        string[] memory _lotNumbers,
        ActionType[] memory _actions,
        uint256[] memory _quantities,
        string[] memory _fromLocations,
        string[] memory _toLocations
    ) public {
        uint256 actionCount = _lotNumbers.length;
        require(
            actionCount == _actions.length &&
            actionCount == _quantities.length &&
            actionCount == _fromLocations.length &&
            actionCount == _toLocations.length,
            "Batch array lengths must match"
        );

        for (uint256 i = 0; i < actionCount; i++) {
            _logAction(
                _lotNumbers[i],
                _actions[i],
                _quantities[i],
                _fromLocations[i],
                _toLocations[i]
            );
        }
    }

    function getHistory(string memory _lotNumber) public view returns (Record[] memory) {
        return lotHistory[_lotNumber];
    }

    function getLotStatus(string memory _lotNumber) public view returns (
        bool exists,
        LotState state,
        uint256 onChainQuantity,
        uint256 pendingQuantity
    ) {
        LotStatus memory status = lotStatuses[_lotNumber];
        return (status.exists, status.state, status.onChainQuantity, status.pendingQuantity);
    }

    function _logAction(
        string memory _lotNumber, 
        ActionType _action, 
        uint256 _quantity,
        string memory _fromLocation,
        string memory _toLocation
    ) internal {
        require(bytes(_lotNumber).length > 0, "Lot number is required");
        require(_quantity > 0, "Quantity must be greater than zero");

        LotStatus storage status = lotStatuses[_lotNumber];
        _applyBusinessRules(status, _action, _quantity);

        Record memory newRecord = Record({
            lotNumber: _lotNumber,
            action: _action,
            quantity: _quantity,
            timestamp: block.timestamp,
            user: msg.sender,
            fromLocation: _fromLocation,
            toLocation: _toLocation
        });
        
        lotHistory[_lotNumber].push(newRecord);
        
        emit StateChanged(_lotNumber, _action, _quantity, block.timestamp, msg.sender, _fromLocation, _toLocation);
    }

    function _applyBusinessRules(
        LotStatus storage status,
        ActionType action,
        uint256 quantity
    ) internal {
        if (action == ActionType.Genesis) {
            _requireAdmin();
            require(!status.exists, "Genesis already exists for this lot");
            status.exists = true;
            status.state = LotState.Received;
            status.onChainQuantity = quantity;
            status.pendingQuantity = 0;
            return;
        }

        if (action == ActionType.Added) {
            _requireRole(ActorRole.Warehouse);
            status.exists = true;
            status.onChainQuantity += quantity;
            if (status.state == LotState.None || status.state == LotState.Consumed) {
                status.state = LotState.Received;
            }
            return;
        }

        if (action == ActionType.Produced) {
            _requireRole(ActorRole.Producer);
            require(status.state != LotState.Produced && status.state != LotState.Shipped, "Lot already has an active shipment");
            status.exists = true;
            status.state = LotState.Produced;
            status.pendingQuantity = quantity;
            return;
        }

        if (action == ActionType.Shipped) {
            _requireRole(ActorRole.Producer);
            require(status.exists && status.state == LotState.Produced, "Lot must be produced before shipment");
            require(status.pendingQuantity == quantity, "Shipment quantity must match produced quantity");
            status.state = LotState.Shipped;
            return;
        }

        if (action == ActionType.Received) {
            _requireRole(ActorRole.Warehouse);
            require(status.exists && status.state == LotState.Shipped, "Lot must be shipped before receipt");
            require(status.pendingQuantity == quantity, "Receipt quantity must match shipped quantity");
            status.state = LotState.Received;
            status.onChainQuantity += quantity;
            status.pendingQuantity = 0;
            return;
        }

        if (action == ActionType.Transferred) {
            _requireRole(ActorRole.Warehouse);
            require(status.exists && (status.state == LotState.Received || status.state == LotState.Transferred), "Lot must be in stock before transfer");
            require(status.onChainQuantity >= quantity, "Transfer exceeds on-chain stock");
            status.state = LotState.Transferred;
            return;
        }

        if (action == ActionType.Consumed) {
            _requireRole(ActorRole.Laboratory);
            require(status.exists && (status.state == LotState.Transferred || status.state == LotState.Received), "Lot must be available before consumption");
            _decreaseStock(status, quantity);
            return;
        }

        if (action == ActionType.Deducted) {
            _requireAnyRole(ActorRole.Warehouse, ActorRole.Laboratory);
            require(status.exists, "Lot must exist before deduction");
            _decreaseStock(status, quantity);
            return;
        }

        revert("Unsupported action");
    }

    function _decreaseStock(LotStatus storage status, uint256 quantity) internal {
        require(status.onChainQuantity >= quantity, "Action exceeds on-chain stock");
        status.onChainQuantity -= quantity;
        status.pendingQuantity = 0;

        if (status.onChainQuantity == 0) {
            status.state = LotState.Consumed;
        }
    }

    function _isAdmin(address actor) internal view returns (bool) {
        return actor == owner || actorRoles[actor] == ActorRole.Admin;
    }

    function _requireAdmin() internal view {
        require(_isAdmin(msg.sender), "Admin role is required");
    }

    function _requireRole(ActorRole role) internal view {
        require(_isAdmin(msg.sender) || actorRoles[msg.sender] == role, "Actor is not authorized for this action");
    }

    function _requireAnyRole(ActorRole firstRole, ActorRole secondRole) internal view {
        require(
            _isAdmin(msg.sender) ||
            actorRoles[msg.sender] == firstRole ||
            actorRoles[msg.sender] == secondRole,
            "Actor is not authorized for this action"
        );
    }
}
