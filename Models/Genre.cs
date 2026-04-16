using System.ComponentModel.DataAnnotations;

namespace MovieRecommendationSystem.Models
{
    public class Genre
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public List<MovieGenre> MovieGenres { get; set; } = new();
    }
}