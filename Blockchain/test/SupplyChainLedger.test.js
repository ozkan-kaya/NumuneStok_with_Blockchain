const { expect } = require("chai");
const { ethers } = require("hardhat");

describe("SupplyChainLedger", function () {
  let ledger;
  let owner;
  let producer;
  let warehouse;
  let laboratory;
  let outsider;

  beforeEach(async function () {
    [owner, producer, warehouse, laboratory, outsider] = await ethers.getSigners();

    const SupplyChainLedger = await ethers.getContractFactory("SupplyChainLedger");
    ledger = await SupplyChainLedger.deploy();
    await ledger.waitForDeployment();

    await ledger.setActorRole(producer.address, 1);
    await ledger.setActorRole(warehouse.address, 2);
    await ledger.setActorRole(laboratory.address, 3);
  });

  it("records a complete lot journey from production to consumption", async function () {
    const lot = "LOT-SC-001";

    await ledger.connect(producer).logAction(lot, 2, 10, "Tedarikci", "Uretim Hatti");
    await ledger.connect(producer).logAction(lot, 3, 10, "Uretim Hatti", "Lojistik");
    await ledger.connect(warehouse).logAction(lot, 4, 10, "Lojistik", "Merkez Depo");
    await ledger.connect(warehouse).logAction(lot, 5, 4, "Merkez Depo", "Laboratuvar");
    await ledger.connect(laboratory).logAction(lot, 6, 4, "Laboratuvar", "Tuketildi");

    const history = await ledger.getHistory(lot);
    expect(history).to.have.lengthOf(5);

    const status = await ledger.getLotStatus(lot);
    expect(status[0]).to.equal(true);
    expect(status[2]).to.equal(6n);
  });

  it("rejects shipment before production", async function () {
    await expect(
      ledger.connect(producer).logAction("LOT-SC-002", 3, 5, "Uretim Hatti", "Lojistik")
    ).to.be.revertedWith("Lot must be produced before shipment");
  });

  it("rejects receipt before shipment", async function () {
    const lot = "LOT-SC-003";

    await ledger.connect(producer).logAction(lot, 2, 5, "Tedarikci", "Uretim Hatti");

    await expect(
      ledger.connect(warehouse).logAction(lot, 4, 5, "Lojistik", "Merkez Depo")
    ).to.be.revertedWith("Lot must be shipped before receipt");
  });

  it("rejects transfer before the warehouse has received the shipment", async function () {
    const lot = "LOT-SC-003-TRANSFER";

    await ledger.connect(producer).logAction(lot, 2, 5, "Tedarikci", "Uretim Hatti");
    await ledger.connect(producer).logAction(lot, 3, 5, "Uretim Hatti", "Lojistik");

    await expect(
      ledger.connect(warehouse).logAction(lot, 5, 2, "Merkez Depo", "Laboratuvar")
    ).to.be.revertedWith("Lot must be in stock before transfer");
  });

  it("rejects shipment when shipped quantity differs from produced quantity", async function () {
    const lot = "LOT-SC-003-SHIP-QTY";

    await ledger.connect(producer).logAction(lot, 2, 10, "Tedarikci", "Uretim Hatti");

    await expect(
      ledger.connect(producer).logAction(lot, 3, 8, "Uretim Hatti", "Lojistik")
    ).to.be.revertedWith("Shipment quantity must match produced quantity");
  });

  it("rejects receipt when received quantity differs from shipped quantity", async function () {
    const lot = "LOT-SC-003-RECEIVE-QTY";

    await ledger.connect(producer).logAction(lot, 2, 10, "Tedarikci", "Uretim Hatti");
    await ledger.connect(producer).logAction(lot, 3, 10, "Uretim Hatti", "Lojistik");

    await expect(
      ledger.connect(warehouse).logAction(lot, 4, 9, "Lojistik", "Merkez Depo")
    ).to.be.revertedWith("Receipt quantity must match shipped quantity");
  });

  it("rejects consumption that exceeds on-chain stock", async function () {
    const lot = "LOT-SC-004";

    await ledger.connect(producer).logAction(lot, 2, 5, "Tedarikci", "Uretim Hatti");
    await ledger.connect(producer).logAction(lot, 3, 5, "Uretim Hatti", "Lojistik");
    await ledger.connect(warehouse).logAction(lot, 4, 5, "Lojistik", "Merkez Depo");

    await expect(
      ledger.connect(laboratory).logAction(lot, 6, 6, "Laboratuvar", "Tuketildi")
    ).to.be.revertedWith("Action exceeds on-chain stock");
  });

  it("rejects duplicate genesis records for the same lot", async function () {
    const lot = "LOT-SC-005";

    await ledger.logAction(lot, 7, 20, "Baslangic Envanteri", "Merkez Depo");

    await expect(
      ledger.logAction(lot, 7, 20, "Baslangic Envanteri", "Merkez Depo")
    ).to.be.revertedWith("Genesis already exists for this lot");
  });

  it("rejects actions from unauthorized actors", async function () {
    await expect(
      ledger.connect(outsider).logAction("LOT-SC-006", 2, 3, "Tedarikci", "Uretim Hatti")
    ).to.be.revertedWith("Actor is not authorized for this action");
  });

  it("rejects batch actions with mismatched array lengths", async function () {
    await expect(
      ledger.logActions(
        ["LOT-SC-007", "LOT-SC-008"],
        [7],
        [1, 1],
        ["Baslangic", "Baslangic"],
        ["Depo", "Depo"]
      )
    ).to.be.revertedWith("Batch array lengths must match");
  });

  it("deducts stock from a genesis lot and updates on-chain quantity", async function () {
    const lot = "LOT-SC-009";

    await ledger.logAction(lot, 7, 30, "Baslangic Envanteri", "Merkez Depo");
    await ledger.connect(warehouse).logAction(lot, 1, 7, "Merkez Depo", "Laboratuvar");

    const status = await ledger.getLotStatus(lot);
    expect(status[0]).to.equal(true);
    expect(status[1]).to.equal(3n); // Received
    expect(status[2]).to.equal(23n);
    expect(status[3]).to.equal(0n);
  });

  it("allows consumption after warehouse transfer and preserves remaining quantity", async function () {
    const lot = "LOT-SC-010";

    await ledger.connect(producer).logAction(lot, 2, 12, "Tedarikci", "Uretim Hatti");
    await ledger.connect(producer).logAction(lot, 3, 12, "Uretim Hatti", "Lojistik");
    await ledger.connect(warehouse).logAction(lot, 4, 12, "Lojistik", "Merkez Depo");
    await ledger.connect(warehouse).logAction(lot, 5, 5, "Merkez Depo", "Laboratuvar");
    await ledger.connect(laboratory).logAction(lot, 6, 5, "Laboratuvar", "Tuketildi");

    const history = await ledger.getHistory(lot);
    const status = await ledger.getLotStatus(lot);

    expect(history).to.have.lengthOf(5);
    expect(status[1]).to.equal(4n); // Transferred, remaining stock still exists
    expect(status[2]).to.equal(7n);
  });

  it("marks the lot consumed when all on-chain quantity is consumed", async function () {
    const lot = "LOT-SC-011";

    await ledger.connect(producer).logAction(lot, 2, 8, "Tedarikci", "Uretim Hatti");
    await ledger.connect(producer).logAction(lot, 3, 8, "Uretim Hatti", "Lojistik");
    await ledger.connect(warehouse).logAction(lot, 4, 8, "Lojistik", "Merkez Depo");
    await ledger.connect(warehouse).logAction(lot, 5, 8, "Merkez Depo", "Laboratuvar");
    await ledger.connect(laboratory).logAction(lot, 6, 8, "Laboratuvar", "Tuketildi");

    const status = await ledger.getLotStatus(lot);
    expect(status[1]).to.equal(5n); // Consumed
    expect(status[2]).to.equal(0n);
    expect(status[3]).to.equal(0n);
  });
});
