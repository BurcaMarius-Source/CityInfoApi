using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CityInfoApi.Models
{
    public class City
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string State { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        [Range(1, 5)]
        public int TouristRating { get; set; }
        public DateTime DateEstablished { get; set; }
        public long EstimatedPopulation { get; set; }
    }
}
