using Microsoft.EntityFrameworkCore;
using WatchesTok.Models;

namespace WatchesTok.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<Post> ElePost { get; set; }
        public DbSet<Utente> EleUtenti { get; set; }
        public DbSet<Like> EleLikes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configura la relazione tra Post e Utente
            modelBuilder.Entity<Post>()
                .HasOne(p => p.Utente)
                .WithMany()
                .HasForeignKey(p => p.UtenteID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configura la relazione tra Like e Post
            modelBuilder.Entity<Like>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configura la relazione tra Like e Utente (NON a cascata)
            modelBuilder.Entity<Like>()
                .HasOne(l => l.Utente)
                .WithMany()
                .HasForeignKey(l => l.UtenteID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}