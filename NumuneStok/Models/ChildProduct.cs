using System;

namespace NumuneStok.Models
{
    public class ChildProduct
    {
        public int Id { get; set; }  // Otomatik artan olmalı
        public int ProductId { get; set; }  // Ana ürün ID'si
        public Product Product { get; set; }  // Ana ürün ile ilişki
        public string LotNumber { get; set; }
        public DateTime ProductionDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public int Quantity { get; set; }
    }
}
