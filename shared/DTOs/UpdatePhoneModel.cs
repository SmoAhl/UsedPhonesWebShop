// DTO-luokka, joka mahdollistaa osittaiset päivitykset
// Tämä luokka mahdollistaa vain tiettyjen kenttien päivittämisen puhelimen tiedoissa.
// Esimerkiksi, jos käyttäjä haluaa päivittää vain puhelimen hinnan, hän voi tehdä sen ilman, että muut tiedot muuttuvat.
namespace Shared.DTOs
{
    public class UpdatePhoneModel
    {
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        public string? Condition { get; set; }
        public int? StockQuantity { get; set; }
    }
}