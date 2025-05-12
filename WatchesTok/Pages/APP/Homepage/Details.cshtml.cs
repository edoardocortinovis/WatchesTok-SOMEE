using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WatchesTok.Data;
using WatchesTok.Models;

namespace WatchesTok.Pages.Homepage
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(AppDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Post Post { get; set; }
        public bool IsVideo { get; set; }
        public bool IsCurrentUserOwner { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                // Log che stiamo recuperando il post
                _logger.LogInformation("Recupero post con ID: {PostId}", id);

                // Carica il post con tutte le relazioni necessarie
                Post = await _context.ElePost
                    .Include(p => p.Utente)
                    .Include(p => p.Likes)
                    .FirstOrDefaultAsync(p => p.PostID == id);

                if (Post == null)
                {
                    _logger.LogWarning("Post non trovato con ID: {PostId}", id);
                    return Page();
                }

                _logger.LogInformation("Post trovato: {PostTitle}", Post.Titolo);

                // Determina se il media è un video
                IsVideo = IsVideoFile(Post.MediaPath);

                // Determina se l'utente corrente è il proprietario
                var currentUserEmail = User.Identity.IsAuthenticated ? User.Identity.Name : null;
                IsCurrentUserOwner = currentUserEmail != null && Post.Utente != null &&
                                    currentUserEmail.Equals(Post.Utente.email, StringComparison.OrdinalIgnoreCase);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel caricamento del post con ID {PostId}", id);
                return RedirectToPage("/Error");
            }
        }

        // Metodo di utility per determinare se un file è un video
        private bool IsVideoFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            var extensions = new[] { ".mp4", ".mov", ".webm", ".avi", ".wmv", ".mkv" };
            return extensions.Any(ext => path.ToLower().EndsWith(ext));
        }
    }
}