using System;
using System.Collections.Generic;

namespace NumuneStok.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string ReferenceNumber { get; set; }
        public string? Barcode { get; set; }
        public string? Location { get; set; }
        public int Quantity { get; set; }  // Toplam adet
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public int? Order { get; set; }
        public int? Critical { get; set; }
        public int MultiplicationValue { get; set; } = 1;
        public int MultipliedTotalQuantity => TotalQuantity * MultiplicationValue;

        // Alt ürünlerle ilişki
        public List<ChildProduct> ChildProducts { get; set; }

        // Toplam adeti hesaplayan bir özellik
        public int TotalQuantity => ChildProducts != null ? ChildProducts.Sum(cp => cp.Quantity) : 0;
    }
}
