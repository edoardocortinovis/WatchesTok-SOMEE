using System.Linq;
using Microsoft.AspNetCore.Mvc;
using WatchesTok.Data;

namespace WatchesTok.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserStatsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserStatsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetUserStats(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest(new { success = false, error = "Email non specificata" });

            try
            {
                var utente = _context.EleUtenti.FirstOrDefault(u => u.email == email);

                if (utente == null)
                    return NotFound(new { success = false, error = "Utente non trovato" });

                var postCount = _context.ElePost.Count(p => p.UtenteID == utente.UtenteID);
                var likeCount = _context.EleLikes.Count(l => l.UtenteID == utente.UtenteID);

                return Ok(new { success = true, postCount, likeCount });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}