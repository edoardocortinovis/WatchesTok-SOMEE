using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WatchesTok.Data;
using WatchesTok.Models;

namespace WatchesTok.Pages.APP.Account
{
    public class AccountPostsModel : PageModel
    {
        private readonly AppDbContext _context;

        public AccountPostsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Post> MyPosts { get; set; } = new List<Post>();
        public List<Post> LikedPosts { get; set; } = new List<Post>();
        public string CurrentTab { get; set; } = "my-posts";

        public async Task<IActionResult> OnGetAsync(string tab, string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return new JsonResult(new { success = false, error = "Utente non specificato" });
            }

            // Imposta tab attuale
            if (!string.IsNullOrEmpty(tab))
            {
                CurrentTab = tab;
            }

            var utente = await _context.EleUtenti
                .FirstOrDefaultAsync(u => u.email == email);

            if (utente == null)
            {
                return new JsonResult(new { success = false, error = "Utente non trovato" });
            }

            // Carica i post dell'utente
            MyPosts = await _context.ElePost
                .Where(p => p.UtenteID == utente.UtenteID)
                .OrderByDescending(p => p.DataCreazione)
                .ToListAsync();

            // Carica i post a cui l'utente ha messo like
            LikedPosts = await _context.EleLikes
                .Where(l => l.UtenteID == utente.UtenteID)
                .Include(l => l.Post)
                .Select(l => l.Post)
                .OrderByDescending(p => p.DataCreazione)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostToggleLikeAsync(int postId, string email)
        {
            try
            {
                // Trova l'utente
                var utente = await _context.EleUtenti
                    .FirstOrDefaultAsync(u => u.email == email);

                if (utente == null)
                {
                    return new JsonResult(new { success = false, error = "Utente non trovato" });
                }

                // Controlla se il post esiste
                var post = await _context.ElePost.FindAsync(postId);

                if (post == null)
                {
                    return new JsonResult(new { success = false, error = "Post non trovato" });
                }

                // Controlla se l'utente ha già messo like
                var existingLike = await _context.EleLikes
                    .FirstOrDefaultAsync(l => l.PostID == postId && l.UtenteID == utente.UtenteID);

                if (existingLike != null)
                {
                    // Toglie il like
                    _context.EleLikes.Remove(existingLike);
                    await _context.SaveChangesAsync();
                    return new JsonResult(new { success = true, liked = false });
                }
                else
                {
                    // Aggiunge il like
                    var like = new Like
                    {
                        PostID = postId,
                        UtenteID = utente.UtenteID,
                        DataCreazione = DateTime.Now
                    };

                    _context.EleLikes.Add(like);
                    await _context.SaveChangesAsync();
                    return new JsonResult(new { success = true, liked = true });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }
    }
}