using System.ComponentModel.DataAnnotations;

namespace MovieRecommendationSystem.Models
{
    public class Country
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public List<Movie> Movies { get; set; } = new();
    }
}