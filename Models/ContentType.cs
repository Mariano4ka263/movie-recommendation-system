using System.ComponentModel.DataAnnotations;

namespace MovieRecommendationSystem.Models
{
    public enum ContentType
    {
        [Display(Name = "Фільм")]
        Movie = 1,

        [Display(Name = "Серіал")]
        Series = 2
    }
}