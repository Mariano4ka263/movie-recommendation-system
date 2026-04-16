using System.ComponentModel.DataAnnotations;

namespace MovieRecommendationSystem.Models
{
    public class Director
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        public List<Movie> Movies { get; set; } = new();
    }
}