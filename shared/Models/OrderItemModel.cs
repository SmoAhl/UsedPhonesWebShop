namespace Shared.Models
{
    public class OrderItemModel
    {
        public int OrderItemID { get; set; } // PK - Primary Key, tilauserän yksilöllinen tunniste
        public int OrderID { get; set; } // FK - Foreign Key, viittaa tilaukseen (OrderID) Orders-taulussa
        public int PhoneID { get; set; } // FK - Foreign Key, viittaa puhelimeen (PhoneID) Phones-taulussa
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
