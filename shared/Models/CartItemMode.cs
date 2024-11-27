namespace Shared.Models
{
    public class CartItemModel
    {
        public int CartItemID { get; set; } // PK - Primary Key, ostoskoriin lisätyn tuotteen yksilöllinen tunniste
        public int CartID { get; set; } // FK - Foreign Key, viittaa ostoskoriin (CartID) ShoppingCart-taulussa
        public int PhoneID { get; set; } // FK - Foreign Key, viittaa puhelimeen (PhoneID) Phones-taulussa
        public int Quantity { get; set; }
    }

    public class CartItemWithPhone
    {
        public int CartItemID { get; set; }
        public int CartID { get; set; }
        public int Quantity { get; set; }
        public PhoneModel Phone { get; set; }
    }
}
