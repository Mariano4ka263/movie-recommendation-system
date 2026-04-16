using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MovieRecommendationSystem.Models
{
    public class Movie
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введіть назву")]
        [Display(Name = "Назва")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Опис")]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть рік випуску")]
        [Display(Name = "Рік випуску")]
        [Range(1900, 2100, ErrorMessage = "Введіть коректний рік")]
        public int ReleaseYear { get; set; }

        [Required(ErrorMessage = "Оберіть тип контенту")]
        [Display(Name = "Тип контенту")]
        public ContentType ContentType { get; set; }

        [Display(Name = "Країна")]
        public int? CountryId { get; set; }
        public Country? Country { get; set; }

        [Display(Name = "Режисер")]
        public int? DirectorId { get; set; }
        public Director? Director { get; set; }

        [Display(Name = "Статус")]
        public MovieStatus? Status { get; set; }

        [Display(Name = "Постер")]
        [StringLength(300)]
        public string PosterUrl { get; set; } = string.Empty;

        [Display(Name = "Український трейлер")]
        [StringLength(500)]
        public string TrailerUrl { get; set; } = string.Empty;

        public List<Rating> Ratings { get; set; } = new();
        public List<MovieGenre> MovieGenres { get; set; } = new();
        public List<MovieActor> MovieActors { get; set; } = new();

        [NotMapped]
        public string PosterUrlOrDefault =>
            string.IsNullOrWhiteSpace(PosterUrl)
                ? "/images/Zaglushka1.png"
                : PosterUrl;

        [NotMapped]
        [Display(Name = "Середній рейтинг")]
        public double AverageRating =>
            Ratings == null || Ratings.Count == 0 ? 0 : Ratings.Average(r => r.Score);

        [NotMapped]
        public string GenresText =>
            string.Join(", ", MovieGenres
                .Where(mg => mg.Genre != null)
                .Select(mg => mg.Genre!.Name));

        [NotMapped]
        public string ActorsText =>
            string.Join(", ", MovieActors
                .Where(ma => ma.Actor != null)
                .Select(ma => ma.Actor!.FullName));
    }
}