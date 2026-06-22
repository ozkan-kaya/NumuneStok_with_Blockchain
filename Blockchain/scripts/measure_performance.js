const hre = require("hardhat");

async function timed(label, action) {
  const startedAt = Date.now();
  const tx = await action();
  const receipt = await tx.wait();
  const durationMs = Date.now() - startedAt;

  return {
    label,
    txHash: tx.hash,
    gasUsed: receipt.gasUsed.toString(),
    durationMs,
  };
}

async function main() {
  const [admin, producer, warehouse, laboratory] = await hre.ethers.getSigners();
  const SupplyChainLedger = await hre.ethers.getContractFactory("SupplyChainLedger");
  const ledger = await SupplyChainLedger.deploy();
  await ledger.waitForDeployment();

  await ledger.connect(admin).setActorRole(producer.address, 1);
  await ledger.connect(admin).setActorRole(warehouse.address, 2);
  await ledger.connect(admin).setActorRole(laboratory.address, 3);

  const lot = `MEASURE-${Date.now()}`;
  const rows = [];

  rows.push(await timed("Produced", () =>
    ledger.connect(producer).logAction(lot, 2, 100, "Tedarikci", "Uretim Hatti")
  ));

  rows.push(await timed("Shipped", () =>
    ledger.connect(producer).logAction(lot, 3, 100, "Uretim Hatti", "Lojistik")
  ));

  rows.push(await timed("Received", () =>
    ledger.connect(warehouse).logAction(lot, 4, 100, "Lojistik", "Merkez Depo")
  ));

  rows.push(await timed("Transferred", () =>
    ledger.connect(warehouse).logAction(lot, 5, 25, "Merkez Depo", "Laboratuvar")
  ));

  rows.push(await timed("Consumed", () =>
    ledger.connect(laboratory).logAction(lot, 6, 25, "Laboratuvar", "Tuketildi")
  ));

  const batchLot = `BATCH-${Date.now()}`;
  rows.push(await timed("Batch genesis x3", () =>
    ledger.connect(admin).logActions(
      [`${batchLot}-1`, `${batchLot}-2`, `${batchLot}-3`],
      [7, 7, 7],
      [10, 20, 30],
      ["Baslangic", "Baslangic", "Baslangic"],
      ["Merkez Depo", "Merkez Depo", "Merkez Depo"]
    )
  ));

  const historyStartedAt = Date.now();
  const history = await ledger.getHistory(lot);
  const historyReadDurationMs = Date.now() - historyStartedAt;

  console.log("\nSupplyChainLedger local Hardhat olcumleri");
  console.table(rows.map(({ label, gasUsed, durationMs }) => ({ label, gasUsed, durationMs })));
  console.log(`History read count: ${history.length}`);
  console.log(`History read durationMs: ${historyReadDurationMs}`);
  console.log(`Contract address: ${await ledger.getAddress()}`);
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
