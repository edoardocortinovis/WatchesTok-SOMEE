using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using WatchesTok.Data;

namespace WatchesTok.Pages.APP.Account
{
    public class AccountEditModel : PageModel
    {
        private readonly AppDbContext _context;

        [BindProperty]
        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(50, ErrorMessage = "Il nome non può superare i 50 caratteri")]
        public string Nome { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Il cognome è obbligatorio")]
        [StringLength(50, ErrorMessage = "Il cognome non può superare i 50 caratteri")]
        public string Cognome { get; set; }

        [BindProperty]
        public string Email { get; set; }

        public string StatusMessage { get; set; }
        public bool IsSuccess { get; set; }

        public AccountEditModel(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            // Authentication is handled client-side via localStorage
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                StatusMessage = "Per favore correggi gli errori nel form";
                IsSuccess = false;
                return Page();
            }

            try
            {
                var user = await _context.EleUtenti.FirstOrDefaultAsync(u => u.email == Email);
                if (user == null)
                {
                    StatusMessage = "Utente non trovato nel database";
                    IsSuccess = false;
                    return Page();
                }

                // Update user data
                user.Nome = Nome;
                user.Cognome = Cognome;
                await _context.SaveChangesAsync();

                // Prepare data for localStorage update
                var userData = new
                {
                    email = user.email,
                    nome = user.Nome,
                    cognome = user.Cognome
                };

                TempData["UserData"] = JsonSerializer.Serialize(userData);
                StatusMessage = "Profilo aggiornato con successo!";
                IsSuccess = true;

                return Page();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Errore durante il salvataggio: {ex.Message}";
                IsSuccess = false;
                return Page();
            }
        }
    }
}