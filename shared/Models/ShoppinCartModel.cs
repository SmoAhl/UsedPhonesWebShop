namespace Shared.Models
{
    public class ShoppingCartModel
    {
        public int CartID { get; set; } // PK - Primary Key, ostoskorin yksilöllinen tunniste
        public int UserID { get; set; } // FK - Foreign Key, viittaa käyttäjään (UserID) Users-taulussa, yksi käyttäjä = yksi ostoskori
    }
}
