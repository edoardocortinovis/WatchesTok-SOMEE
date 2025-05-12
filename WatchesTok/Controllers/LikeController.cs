using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchesTok.Data;
using WatchesTok.Models;

namespace WatchesTok.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LikeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LikeController(AppDbContext context)
        {
            _context = context;
        }

        // API per aggiungere/rimuovere un like
        [HttpPost("toggle")]
        public async Task<IActionResult> ToggleLike([FromBody] LikeRequest request)
        {
            try
            {
                var post = await _context.ElePost.FindAsync(request.PostID);
                if (post == null)
                {
                    return NotFound(new { message = "Post non trovato" });
                }

                // Trova l'utente tramite email
                var utente = await _context.EleUtenti.FirstOrDefaultAsync(u => u.email == request.UserEmail);
                if (utente == null)
                {
                    return NotFound(new { message = "Utente non trovato" });
                }

                // Cerca se esiste già un like
                var existingLike = await _context.EleLikes
                    .FirstOrDefaultAsync(l => l.PostID == request.PostID && l.UtenteID == utente.UtenteID);

                // Toggle del like
                if (existingLike != null)
                {
                    // Rimuovi il like
                    _context.EleLikes.Remove(existingLike);
                    await _context.SaveChangesAsync();
                    return Ok(new { liked = false, likeCount = await GetLikeCount(request.PostID) });
                }
                else
                {
                    // Aggiungi il like
                    var like = new Like
                    {
                        PostID = request.PostID,
                        UtenteID = utente.UtenteID,
                        DataCreazione = DateTime.Now
                    };
                    _context.EleLikes.Add(like);
                    await _context.SaveChangesAsync();
                    return Ok(new { liked = true, likeCount = await GetLikeCount(request.PostID) });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Errore durante la gestione del like", error = ex.Message });
            }
        }

        // API per verificare se un post è piaciuto da un utente
        [HttpGet("check")]
        public async Task<IActionResult> CheckLike(int postId, string userEmail)
        {
            try
            {
                var utente = await _context.EleUtenti.FirstOrDefaultAsync(u => u.email == userEmail);
                if (utente == null)
                {
                    return NotFound(new { message = "Utente non trovato" });
                }

                bool hasLiked = await _context.EleLikes
                    .AnyAsync(l => l.PostID == postId && l.UtenteID == utente.UtenteID);

                int likeCount = await GetLikeCount(postId);

                return Ok(new { liked = hasLiked, likeCount = likeCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Errore durante la verifica del like", error = ex.Message });
            }
        }

        // API per recuperare i post piaciuti da un utente
        [HttpGet("user")]
        public async Task<IActionResult> GetUserLikes(string userEmail)
        {
            try
            {
                var utente = await _context.EleUtenti.FirstOrDefaultAsync(u => u.email == userEmail);
                if (utente == null)
                {
                    return NotFound(new { message = "Utente non trovato" });
                }

                var likedPosts = await _context.EleLikes
                    .Where(l => l.UtenteID == utente.UtenteID)
                    .Include(l => l.Post)
                    .ThenInclude(p => p.Utente)
                    .Select(l => new
                    {
                        l.Post.PostID,
                        l.Post.Titolo,
                        l.Post.Descrizione,
                        l.Post.MediaPath,
                        l.Post.DataCreazione,
                        l.Post.MarcaModello,
                        l.Post.Hashtags,
                        Autore = new
                        {
                            l.Post.Utente.UtenteID,
                            l.Post.Utente.Nome,
                            l.Post.Utente.Cognome,
                            l.Post.Utente.email
                        },
                        DataLike = l.DataCreazione
                    })
                    .OrderByDescending(p => p.DataLike)
                    .ToListAsync();

                return Ok(likedPosts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Errore durante l'ottenimento dei post con like", error = ex.Message });
            }
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetPostLikeCount(int postId)
        {
            try
            {
                int count = await GetLikeCount(postId);
                return Ok(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Errore durante l'ottenimento del conteggio dei like", error = ex.Message });
            }
        }

        // Metodo helper per contare i like di un post
        private async Task<int> GetLikeCount(int postId)
        {
            return await _context.EleLikes.CountAsync(l => l.PostID == postId);
        }
    }

    public class LikeRequest
    {
        public int PostID { get; set; }
        public string UserEmail { get; set; }
    }
}