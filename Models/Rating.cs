using System.ComponentModel.DataAnnotations;

namespace MovieRecommendationSystem.Models
{
    public class Rating
    {
        public int Id { get; set; }

        [Display(Name = "Користувач")]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Display(Name = "Фільм / серіал")]
        public int MovieId { get; set; }
        public Movie? Movie { get; set; }

        [Required(ErrorMessage = "Оберіть оцінку")]
        [Range(1, 10, ErrorMessage = "Оцінка повинна бути від 1 до 10")]
        [Display(Name = "Оцінка")]
        public int Score { get; set; }
    }
}