namespace Shared.Models
{
    public class OrderModel
    {
        public int OrderID { get; set; } // PK - Primary Key, tilauksen yksilöllinen tunniste
        public int UserID { get; set; } // FK - Foreign Key, viittaa käyttäjään (UserID) Users-taulussa
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
    }
}
