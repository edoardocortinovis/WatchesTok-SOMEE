using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using WatchesTok.Models;
using WatchesTok.Data;
using System.Threading.Tasks;
using System;

namespace WatchesTok.Pages.APP.log
{
    public class loginModel : PageModel
    {
        private readonly AppDbContext _context;
        public loginModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            public string Email { get; set; }
            [Required]
            public string Password { get; set; }
        }

        public class GoogleInputModel
        {
            public string Email { get; set; }
            public string Nome { get; set; }
            public string Cognome { get; set; }
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostLogin()
        {
            try
            {
                // Verifica se il modello è valido
                if (!ModelState.IsValid)
                {
                    return new JsonResult(null);
                }

                // Usa direttamente il modello Input invece di Request.Form
                var email = Input.Email;
                var password = Input.Password;

                var utente = _context.EleUtenti.FirstOrDefault(u => u.email == email && u.password == password);
                if (utente == null)
                {
                    return new JsonResult(null);
                }

                return new JsonResult(utente);
            }
            catch (Exception ex)
            {
                // Logging dell'errore
                Console.WriteLine($"Errore nel login: {ex.Message}");
                return StatusCode(500, "Si è verificato un errore durante il login");
            }
        }

        [IgnoreAntiforgeryToken] // Importante per il POST con contenuti JSON
        public async Task<IActionResult> OnPostGoogleAuthAsync([FromBody] GoogleInputModel input)
        {
            try
            {
                if (input == null || string.IsNullOrEmpty(input.Email))
                {
                    return BadRequest("Dati di autenticazione Google non validi");
                }

                // Controllo se l'utente esiste già
                var utente = _context.EleUtenti.FirstOrDefault(u => u.email == input.Email);

                if (utente == null)
                {
                    // Se l'utente non esiste, lo creo
                    utente = new Utente
                    {
                        Nome = input.Nome ?? "Utente",
                        Cognome = input.Cognome ?? "Google",
                        email = input.Email,
                        password = "GoogleAuth_" + Guid.NewGuid().ToString("N") // Password casuale per gli account Google
                        // Aggiungi qui altri campi necessari per un nuovo utente
                    };

                    _context.EleUtenti.Add(utente);
                    await _context.SaveChangesAsync();
                }

                // Restituisco l'utente per il salvataggio nel localStorage
                return new JsonResult(utente);
            }
            catch (Exception ex)
            {
                // Logging dell'errore
                Console.WriteLine($"Errore nell'autenticazione Google: {ex.Message}");
                return StatusCode(500, "Si è verificato un errore durante l'autenticazione con Google");
            }
        }
    }
}