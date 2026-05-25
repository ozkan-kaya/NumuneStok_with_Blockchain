const hre = require("hardhat");

async function main() {
  const SupplyChainLedger = await hre.ethers.getContractFactory("SupplyChainLedger");
  const ledger = await SupplyChainLedger.deploy();

  await ledger.waitForDeployment();

  console.log("SupplyChainLedger deployed to:", await ledger.getAddress());
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
