namespace Shared.Models
{
    public class PhoneModel
    {
        public int PhoneID { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string Condition { get; set; }
        public int StockQuantity { get; set; }
    }
}
