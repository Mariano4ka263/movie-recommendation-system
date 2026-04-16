using System.ComponentModel.DataAnnotations;

namespace MovieRecommendationSystem.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введіть ім'я")]
        [Display(Name = "Ім'я")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть email")]
        [EmailAddress(ErrorMessage = "Некоректний email")]
        [Display(Name = "Email")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "User";

        public List<Rating> Ratings { get; set; } = new();
    }
}