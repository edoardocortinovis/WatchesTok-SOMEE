using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WatchesTok.Data;
using WatchesTok.Models;

namespace WatchesTok.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PostController> _logger;

        public PostController(AppDbContext context, ILogger<PostController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeletePost([FromBody] DeletePostRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.PostId.ToString()) || string.IsNullOrEmpty(request.UserEmail))
            {
                return BadRequest(new { success = false, message = "Dati non validi" });
            }

            try
            {
                // Trova il post nel database
                var post = await _context.ElePost
                    .FirstOrDefaultAsync(p => p.PostID == request.PostId);

                if (post == null)
                {
                    return NotFound(new { success = false, message = "Post non trovato" });
                }

                // Verifica che l'utente sia il proprietario del post
                var user = await _context.EleUtenti
                    .FirstOrDefaultAsync(u => u.email == request.UserEmail);

                if (user == null)
                {
                    return BadRequest(new { success = false, message = "Utente non trovato" });
                }

                if (post.UtenteID != user.UtenteID)
                {
                    return Unauthorized(new { success = false, message = "Non sei autorizzato a eliminare questo post" });
                }

                // Elimina prima i like associati al post
                var likes = await _context.EleLikes.Where(l => l.PostID == post.PostID).ToListAsync();
                _context.EleLikes.RemoveRange(likes);

                

                // Elimina il post
                _context.ElePost.Remove(post);
                await _context.SaveChangesAsync();

                // Se necessario, elimina anche il file media associato
                string mediaPath = post.MediaPath;
                if (!string.IsNullOrEmpty(mediaPath))
                {
                    // Ottenere il percorso fisico del file
                    string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string filePath = Path.Combine(webRootPath, mediaPath.TrimStart('/'));

                    if (System.IO.File.Exists(filePath))
                    {
                        try
                        {
                            System.IO.File.Delete(filePath);
                            _logger.LogInformation($"File eliminato con successo: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Errore durante l'eliminazione del file: {filePath}");
                            // Continuiamo comunque poiché il post è stato eliminato dal database
                        }
                    }
                }

                return Ok(new { success = true, message = "Post eliminato con successo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'eliminazione del post");
                return StatusCode(500, new { success = false, message = "Errore del server durante l'eliminazione del post" });
            }
        }
    }

    public class DeletePostRequest
    {
        public int PostId { get; set; }
        public string UserEmail { get; set; }
    }
}