using Microsoft.EntityFrameworkCore;
using MovieRecommendationSystem.Models;

namespace MovieRecommendationSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Rating> Ratings { get; set; }

        public DbSet<Genre> Genres { get; set; }
        public DbSet<Actor> Actors { get; set; }
        public DbSet<Director> Directors { get; set; }
        public DbSet<Country> Countries { get; set; }

        public DbSet<MovieGenre> MovieGenres { get; set; }
        public DbSet<MovieActor> MovieActors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Rating>()
                .HasIndex(r => new { r.UserId, r.MovieId })
                .IsUnique();

            modelBuilder.Entity<Movie>()
                .HasIndex(m => new { m.Title, m.ReleaseYear, m.ContentType })
                .IsUnique();

            modelBuilder.Entity<Genre>()
                .HasIndex(g => g.Name)
                .IsUnique();

            modelBuilder.Entity<Actor>()
                .HasIndex(a => a.FullName)
                .IsUnique();

            modelBuilder.Entity<Director>()
                .HasIndex(d => d.FullName)
                .IsUnique();

            modelBuilder.Entity<Country>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<MovieGenre>()
                .HasKey(mg => new { mg.MovieId, mg.GenreId });

            modelBuilder.Entity<MovieGenre>()
                .HasOne(mg => mg.Movie)
                .WithMany(m => m.MovieGenres)
                .HasForeignKey(mg => mg.MovieId);

            modelBuilder.Entity<MovieGenre>()
                .HasOne(mg => mg.Genre)
                .WithMany(g => g.MovieGenres)
                .HasForeignKey(mg => mg.GenreId);

            modelBuilder.Entity<MovieActor>()
                .HasKey(ma => new { ma.MovieId, ma.ActorId });

            modelBuilder.Entity<MovieActor>()
                .HasOne(ma => ma.Movie)
                .WithMany(m => m.MovieActors)
                .HasForeignKey(ma => ma.MovieId);

            modelBuilder.Entity<MovieActor>()
                .HasOne(ma => ma.Actor)
                .WithMany(a => a.MovieActors)
                .HasForeignKey(ma => ma.ActorId);

            modelBuilder.Entity<Genre>().HasData(
                new Genre { Id = 1, Name = "Фантастика" },
                new Genre { Id = 2, Name = "Драма" },
                new Genre { Id = 3, Name = "Трилер" },
                new Genre { Id = 4, Name = "Детектив" },
                new Genre { Id = 5, Name = "Комедія" },
                new Genre { Id = 6, Name = "Бойовик" },
                new Genre { Id = 7, Name = "Жахи" },
                new Genre { Id = 8, Name = "Романтика" },
                new Genre { Id = 9, Name = "Пригоди" },
                new Genre { Id = 10, Name = "Анімація" },
                new Genre { Id = 11, Name = "Документальний" }
            );
        }
    }
}