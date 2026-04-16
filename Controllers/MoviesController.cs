using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MovieRecommendationSystem.Data;
using MovieRecommendationSystem.Models;
using MovieRecommendationSystem.Services;
using MovieRecommendationSystem.ViewModels;

namespace MovieRecommendationSystem.Controllers
{
    public class MoviesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRecommendationService _recommendationService;

        public MoviesController(AppDbContext context, IRecommendationService recommendationService)
        {
            _context = context;
            _recommendationService = recommendationService;
        }

        private async Task SetMovieListsAsync()
        {
            var genres = await _context.Genres
                .OrderBy(g => g.Name)
                .ToListAsync();

            ViewBag.GenreList = genres;

            ViewBag.FilterGenres = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Усі жанри --" }
            }
            .Concat(genres.Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Name
            }))
            .ToList();

            ViewBag.ContentTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = ContentType.Movie.ToString(), Text = "Фільм" },
                new SelectListItem { Value = ContentType.Series.ToString(), Text = "Серіал" }
            };

            ViewBag.Statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Оберіть статус --" },
                new SelectListItem { Value = MovieStatus.Announced.ToString(), Text = "Анонсований" },
                new SelectListItem { Value = MovieStatus.Ongoing.ToString(), Text = "Продовжується" },
                new SelectListItem { Value = MovieStatus.Finished.ToString(), Text = "Завершений" },
                new SelectListItem { Value = MovieStatus.Cancelled.ToString(), Text = "Скасований" }
            };
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchString, int? genreId, string? sortOrder)
        {
            var moviesQuery = _context.Movies
                .Include(m => m.Ratings)
                .Include(m => m.Country)
                .Include(m => m.Director)
                .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                .Include(m => m.MovieActors)
                    .ThenInclude(ma => ma.Actor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                moviesQuery = moviesQuery.Where(m => m.Title.Contains(searchString));
            }

            if (genreId.HasValue)
            {
                moviesQuery = moviesQuery.Where(m => m.MovieGenres.Any(mg => mg.GenreId == genreId.Value));
            }

            var movies = await moviesQuery.ToListAsync();

            movies = sortOrder switch
            {
                "rating_desc" => movies.OrderByDescending(m => m.AverageRating).ToList(),
                "year_desc" => movies.OrderByDescending(m => m.ReleaseYear).ToList(),
                "year_asc" => movies.OrderBy(m => m.ReleaseYear).ToList(),
                _ => movies.OrderBy(m => m.Title).ToList()
            };

            ViewBag.SearchString = searchString;
            ViewBag.SelectedGenreId = genreId;
            ViewBag.SortOrder = sortOrder;

            await SetMovieListsAsync();
            return View(movies);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var movie = await _context.Movies
                .Include(m => m.Ratings)
                .Include(m => m.Country)
                .Include(m => m.Director)
                .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                .Include(m => m.MovieActors)
                    .ThenInclude(ma => ma.Actor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
                return NotFound();

            return View(movie);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await SetMovieListsAsync();

            var model = new MovieFormViewModel
            {
                ReleaseYear = DateTime.Now.Year
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFormViewModel model)
        {
            if (model.SelectedGenreIds == null || !model.SelectedGenreIds.Any())
            {
                ModelState.AddModelError(nameof(model.SelectedGenreIds), "Оберіть хоча б один жанр");
            }

            var duplicateExists = await _context.Movies.AnyAsync(m =>
                m.Title == model.Title &&
                m.ReleaseYear == model.ReleaseYear &&
                m.ContentType == model.ContentType);

            if (duplicateExists)
            {
                ModelState.AddModelError(nameof(model.Title), "Такий фільм або серіал уже існує");
            }

            if (!ModelState.IsValid)
            {
                await SetMovieListsAsync();
                return View(model);
            }

            try
            {
                var country = await GetOrCreateCountryAsync(model.CountryName);
                var director = await GetOrCreateDirectorAsync(model.DirectorName);
                var actors = await GetOrCreateActorsAsync(model.ActorsInput);

                var movie = new Movie
                {
                    Title = model.Title?.Trim() ?? string.Empty,
                    Description = model.Description?.Trim() ?? string.Empty,
                    ReleaseYear = model.ReleaseYear,
                    ContentType = model.ContentType,
                    CountryId = country?.Id,
                    DirectorId = director?.Id,
                    Status = model.Status,
                    PosterUrl = model.PosterUrl?.Trim() ?? string.Empty,
                    TrailerUrl = model.TrailerUrl?.Trim() ?? string.Empty
                };

                var selectedGenreIds = model.SelectedGenreIds ?? new List<int>();

                movie.MovieGenres = selectedGenreIds
                    .Distinct()
                    .Select(id => new MovieGenre { GenreId = id })
                    .ToList();

                movie.MovieActors = actors
                    .Select(actor => new MovieActor { ActorId = actor.Id })
                    .ToList();

                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Фільм успішно додано.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Помилка під час збереження: " + ex.Message);
                await SetMovieListsAsync();
                return View(model);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var movie = await _context.Movies
                .Include(m => m.Country)
                .Include(m => m.Director)
                .Include(m => m.MovieGenres)
                .Include(m => m.MovieActors)
                    .ThenInclude(ma => ma.Actor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                return RedirectToAction(nameof(Index));
            }

            await SetMovieListsAsync();

            var model = new MovieFormViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                SelectedGenreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToList(),
                Description = movie.Description,
                ReleaseYear = movie.ReleaseYear,
                ContentType = movie.ContentType,
                CountryName = movie.Country?.Name ?? string.Empty,
                DirectorName = movie.Director?.FullName ?? string.Empty,
                ActorsInput = string.Join(", ", movie.MovieActors
                    .Where(ma => ma.Actor != null)
                    .Select(ma => ma.Actor!.FullName)),
                Status = movie.Status,
                PosterUrl = movie.PosterUrl,
                TrailerUrl = movie.TrailerUrl
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MovieFormViewModel model)
        {
            if (id != model.Id)
            {
                return RedirectToAction(nameof(Index));
            }

            if (model.SelectedGenreIds == null || !model.SelectedGenreIds.Any())
            {
                ModelState.AddModelError(nameof(model.SelectedGenreIds), "Оберіть хоча б один жанр");
            }

            var duplicateExists = await _context.Movies.AnyAsync(m =>
                m.Id != model.Id &&
                m.Title == model.Title &&
                m.ReleaseYear == model.ReleaseYear &&
                m.ContentType == model.ContentType);

            if (duplicateExists)
            {
                ModelState.AddModelError(nameof(model.Title), "Такий фільм або серіал уже існує");
            }

            if (!ModelState.IsValid)
            {
                await SetMovieListsAsync();
                return View(model);
            }

            try
            {
                var movie = await _context.Movies
                    .Include(m => m.MovieGenres)
                    .Include(m => m.MovieActors)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (movie == null)
                {
                    return RedirectToAction(nameof(Index));
                }

                var country = await GetOrCreateCountryAsync(model.CountryName);
                var director = await GetOrCreateDirectorAsync(model.DirectorName);
                var actors = await GetOrCreateActorsAsync(model.ActorsInput);

                movie.Title = model.Title?.Trim() ?? string.Empty;
                movie.Description = model.Description?.Trim() ?? string.Empty;
                movie.ReleaseYear = model.ReleaseYear;
                movie.ContentType = model.ContentType;
                movie.CountryId = country?.Id;
                movie.DirectorId = director?.Id;
                movie.Status = model.Status;
                movie.PosterUrl = model.PosterUrl?.Trim() ?? string.Empty;
                movie.TrailerUrl = model.TrailerUrl?.Trim() ?? string.Empty;

                var selectedGenreIds = model.SelectedGenreIds ?? new List<int>();

                movie.MovieGenres.Clear();
                foreach (var genreId in selectedGenreIds.Distinct())
                {
                    movie.MovieGenres.Add(new MovieGenre
                    {
                        MovieId = movie.Id,
                        GenreId = genreId
                    });
                }

                movie.MovieActors.Clear();
                foreach (var actor in actors)
                {
                    movie.MovieActors.Add(new MovieActor
                    {
                        MovieId = movie.Id,
                        ActorId = actor.Id
                    });
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Фільм успішно оновлено.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Помилка під час збереження: " + ex.Message);
                await SetMovieListsAsync();
                return View(model);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var movie = await _context.Movies
                .Include(m => m.Country)
                .Include(m => m.Director)
                .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
                return NotFound();

            return View(movie);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public async Task<IActionResult> Recommendations(int? selectedUserId, int? selectedGenreId)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated && !User.IsInRole("Admin"))
            {
                var currentUserIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(currentUserIdValue, out int currentUserId))
                {
                    selectedUserId = currentUserId;
                }
            }

            var model = await _recommendationService.BuildRecommendationsAsync(selectedUserId, selectedGenreId);
            return View(model);
        }

        private async Task<Country?> GetOrCreateCountryAsync(string? countryName)
        {
            if (string.IsNullOrWhiteSpace(countryName))
                return null;

            var normalized = countryName.Trim();

            var country = await _context.Countries
                .FirstOrDefaultAsync(c => c.Name == normalized);

            if (country != null)
                return country;

            country = new Country { Name = normalized };
            _context.Countries.Add(country);
            await _context.SaveChangesAsync();
            return country;
        }

        private async Task<Director?> GetOrCreateDirectorAsync(string? directorName)
        {
            if (string.IsNullOrWhiteSpace(directorName))
                return null;

            var normalized = directorName.Trim();

            var director = await _context.Directors
                .FirstOrDefaultAsync(d => d.FullName == normalized);

            if (director != null)
                return director;

            director = new Director { FullName = normalized };
            _context.Directors.Add(director);
            await _context.SaveChangesAsync();
            return director;
        }

        private async Task<List<Actor>> GetOrCreateActorsAsync(string? actorsInput)
        {
            var result = new List<Actor>();

            if (string.IsNullOrWhiteSpace(actorsInput))
                return result;

            var names = actorsInput
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct()
                .ToList();

            foreach (var name in names)
            {
                var actor = await _context.Actors.FirstOrDefaultAsync(a => a.FullName == name);

                if (actor == null)
                {
                    actor = new Actor { FullName = name };
                    _context.Actors.Add(actor);
                    await _context.SaveChangesAsync();
                }

                result.Add(actor);
            }

            return result;
        }
    }
}