#!/bin/bash

# Çıkış yapıldığında arka plandaki node'u da kapat
trap "echo 'Hardhat Node kapatılıyor...'; kill 0" SIGINT SIGTERM EXIT

echo "Hardhat Node başlatılıyor..."
# Hardhat node'u arka planda başlatıyoruz
npx hardhat node &
NODE_PID=$!

echo "Node'un ayağa kalkması bekleniyor (3 saniye)..."
sleep 3

echo "Akıllı Sözleşme (Smart Contract) deploy ediliyor..."
npx hardhat run scripts/deploy.js --network localhost

echo "Veritabanı ile Blockchain senkronize ediliyor..."
curl -s http://localhost:5233/Product/SyncBlockchain
echo ""

echo "✅ Sistem başarıyla senkronize edildi."
echo "🟢 Hardhat Node çalışmaya devam ediyor..."
echo "❌ Node'u kapatmak için CTRL+C tuşlarına basın."

# Script'in kapanmaması için node sürecini bekliyoruz
wait $NODE_PID
