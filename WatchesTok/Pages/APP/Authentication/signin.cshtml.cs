using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WatchesTok.Data;
using WatchesTok.Models;
using System.ComponentModel.DataAnnotations;

namespace WatchesTok.Pages.APP.log
{
    public class registraModel : PageModel
    {
        private readonly AppDbContext _context;

        public registraModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            public string Nome { get; set; }

            [Required]
            public string Cognome { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public IActionResult OnGet()
        {
            if (Request.Cookies["isAuthenticated"] == "true") // O altra verifica
            {
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var utente = new Utente
            {
                Nome = Input.Nome,
                Cognome = Input.Cognome,
                email = Input.Email,
                password = Input.Password
            };

            _context.EleUtenti.Add(utente);
            await _context.SaveChangesAsync();

            // Pulisci i dati temporanei dal Local Storage
            HttpContext.Response.Cookies.Append("registrationComplete", "true");

            return RedirectToPage("/APP/Authentication/login");
        }
    }
}