#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BLOCKCHAIN_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ROOT_DIR="$(cd "$BLOCKCHAIN_DIR/.." && pwd)"
NODE_PID=""

trap 'echo "Hardhat Node kapatılıyor..."; if [ -n "$NODE_PID" ]; then kill "$NODE_PID" 2>/dev/null || true; fi' SIGINT SIGTERM EXIT

cd "$BLOCKCHAIN_DIR"

echo "Hardhat Node başlatılıyor..."
npx hardhat node &
NODE_PID=$!

echo "Node'un ayağa kalkması bekleniyor (3 saniye)..."
sleep 3

echo "Akıllı Sözleşme (Smart Contract) deploy ediliyor..."
DEPLOY_OUTPUT="$(npx hardhat run scripts/deploy.js --network localhost)"
echo "$DEPLOY_OUTPUT"

CONTRACT_ADDRESS="$(printf "%s\n" "$DEPLOY_OUTPUT" | awk '/SupplyChainLedger deployed to:/ {print $NF}' | tail -n 1)"
if [ -z "$CONTRACT_ADDRESS" ]; then
    echo "❌ Deploy edilen contract address okunamadı."
    exit 1
fi

echo "Başlangıç stokları blockchain'e senkronize ediliyor..."
cd "$ROOT_DIR"
BLOCKCHAIN_SYNC_SOURCE="start_and_sync" Blockchain__ContractAddress="$CONTRACT_ADDRESS" dotnet run --project NumuneStok/NumuneStok.csproj -- --sync-blockchain

echo "✅ Blockchain node, akıllı sözleşme ve başlangıç stok sync hazır."
echo "🟢 Hardhat Node çalışmaya devam ediyor..."
echo "❌ Node'u kapatmak için CTRL+C tuşlarına basın."

wait "$NODE_PID"
