using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WatchesTok.Data;
using WatchesTok.Models;

namespace WatchesTok.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.EleUtenti.ToListAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Errore durante il recupero degli utenti", error = ex.Message });
            }
        }

        // API per eliminare un utente
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var utente = await _context.EleUtenti.FindAsync(id);
                if (utente == null)
                {
                    return NotFound(new { success = false, message = "Utente non trovato" });
                }

                // Protezione speciale per l'account admin
                if (utente.email == "admin@admin.it")
                {
                    return BadRequest(new { success = false, message = "Non puoi eliminare l'account admin" });
                }

                // Prima elimina eventuali post dell'utente
                var posts = await _context.ElePost.Where(p => p.UtenteID == id).ToListAsync();
                _context.ElePost.RemoveRange(posts);

                // Elimina anche i likes dell'utente
                var likes = await _context.EleLikes.Where(l => l.UtenteID == id).ToListAsync();
                _context.EleLikes.RemoveRange(likes);

                // Infine elimina l'utente
                _context.EleUtenti.Remove(utente);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Utente eliminato con successo" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Errore durante l'eliminazione dell'utente", error = ex.Message });
            }
        }

        // Classe di supporto per aggiornamento utente
        public class UpdateUserRequest
        {
            public string Nome { get; set; }
            public string Cognome { get; set; }
            public string Email { get; set; }
        }

        // API per aggiornare un utente
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var utente = await _context.EleUtenti.FindAsync(id);
                if (utente == null)
                {
                    return NotFound(new { success = false, message = "Utente non trovato" });
                }

                // Controlla se l'email è già in uso da un altro utente
                if (await _context.EleUtenti.AnyAsync(u => u.UtenteID != id && u.email == request.Email))
                {
                    return BadRequest(new { success = false, message = "Email già in uso" });
                }

                // Protezione speciale per l'account admin: non permettere di cambiare l'email di admin@admin.it
                if (utente.email == "admin@admin.it" && request.Email != "admin@admin.it")
                {
                    return BadRequest(new { success = false, message = "Non puoi modificare l'email dell'account admin" });
                }

                // Aggiorna i dati dell'utente
                utente.Nome = request.Nome;
                utente.Cognome = request.Cognome;
                utente.email = request.Email;

                _context.EleUtenti.Update(utente);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Utente aggiornato con successo" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Errore durante l'aggiornamento dell'utente", error = ex.Message });
            }
        }
    }
}