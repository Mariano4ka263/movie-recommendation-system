using System.ComponentModel.DataAnnotations;

namespace MovieRecommendationSystem.Models
{
    public class Actor
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        public List<MovieActor> MovieActors { get; set; } = new();
    }
}