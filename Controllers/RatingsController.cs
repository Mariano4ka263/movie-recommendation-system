using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieRecommendationSystem.Data;
using MovieRecommendationSystem.Models;
using MovieRecommendationSystem.ViewModels;

namespace MovieRecommendationSystem.Controllers
{
    [Authorize]
    public class RatingsController : Controller
    {
        private readonly AppDbContext _context;

        public RatingsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(userIdValue!);
        }

        public async Task<IActionResult> Index()
        {
            int currentUserId = GetCurrentUserId();

            var ratingsQuery = _context.Ratings
                .Include(r => r.Movie)
                .Include(r => r.User)
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                ratingsQuery = ratingsQuery.Where(r => r.UserId == currentUserId);
            }

            var ratings = await ratingsQuery
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return View(ratings);
        }

        public async Task<IActionResult> Create(int? movieId)
        {
            if (movieId == null)
            {
                return NotFound();
            }

            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == movieId.Value);
            if (movie == null)
            {
                return NotFound();
            }

            int currentUserId = GetCurrentUserId();

            bool alreadyExists = await _context.Ratings
                .AnyAsync(r => r.UserId == currentUserId && r.MovieId == movieId.Value);

            if (alreadyExists)
            {
                TempData["RatingMessage"] = "Ви вже оцінили цей фільм.";
                return RedirectToAction("Details", "Movies", new { id = movieId.Value });
            }

            var model = new CreateRatingViewModel
            {
                MovieId = movie.Id,
                MovieTitle = movie.Title,
                Score = 10
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRatingViewModel model)
        {
            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == model.MovieId);
            if (movie == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.MovieTitle = movie.Title;
                return View(model);
            }

            int currentUserId = GetCurrentUserId();

            bool alreadyExists = await _context.Ratings
                .AnyAsync(r => r.UserId == currentUserId && r.MovieId == model.MovieId);

            if (alreadyExists)
            {
                TempData["RatingMessage"] = "Ви вже оцінили цей фільм.";
                return RedirectToAction("Details", "Movies", new { id = model.MovieId });
            }

            var rating = new Rating
            {
                UserId = currentUserId,
                MovieId = model.MovieId,
                Score = model.Score
            };

            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();

            TempData["RatingMessage"] = "Оцінку успішно додано.";
            return RedirectToAction("Details", "Movies", new { id = model.MovieId });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rating = await _context.Ratings
                .Include(r => r.Movie)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rating == null)
            {
                return NotFound();
            }

            int currentUserId = GetCurrentUserId();

            if (!User.IsInRole("Admin") && rating.UserId != currentUserId)
            {
                return Forbid();
            }

            var model = new CreateRatingViewModel
            {
                MovieId = rating.MovieId,
                MovieTitle = rating.Movie?.Title ?? string.Empty,
                Score = rating.Score
            };

            ViewBag.RatingId = rating.Id;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateRatingViewModel model)
        {
            var rating = await _context.Ratings
                .Include(r => r.Movie)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rating == null)
            {
                return NotFound();
            }

            int currentUserId = GetCurrentUserId();

            if (!User.IsInRole("Admin") && rating.UserId != currentUserId)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                model.MovieTitle = rating.Movie?.Title ?? string.Empty;
                ViewBag.RatingId = id;
                return View(model);
            }

            rating.Score = model.Score;

            _context.Ratings.Update(rating);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rating = await _context.Ratings
                .Include(r => r.Movie)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rating == null)
            {
                return NotFound();
            }

            int currentUserId = GetCurrentUserId();

            if (!User.IsInRole("Admin") && rating.UserId != currentUserId)
            {
                return Forbid();
            }

            return View(rating);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rating = await _context.Ratings.FindAsync(id);
            if (rating == null)
            {
                return NotFound();
            }

            int currentUserId = GetCurrentUserId();

            if (!User.IsInRole("Admin") && rating.UserId != currentUserId)
            {
                return Forbid();
            }

            _context.Ratings.Remove(rating);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}