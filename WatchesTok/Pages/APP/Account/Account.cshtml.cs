using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WatchesTok.Data;
using WatchesTok.Models;

namespace WatchesTok.Pages.APP.Account
{
    public class AccountModel : PageModel
    {
        private readonly AppDbContext _context;

        public AccountModel(AppDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            // Non carichiamo dati qui, utilizziamo API e localStorage lato client
        }

        // Metodo per aggiornare i dati dell'utente
        public async Task<IActionResult> OnPostAsync(string email, string nome, string cognome)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return new JsonResult(new { success = false, error = "Email non specificata" });
                }

                // Cerca l'utente nel database per email
                var utente = _context.EleUtenti.FirstOrDefault(u => u.email == email);
                if (utente == null)
                {
                    return new JsonResult(new { success = false, error = "Utente non trovato con questa email" });
                }

                // Aggiorna i dati dell'utente
                utente.Nome = nome;
                utente.Cognome = cognome;
                // Non aggiorniamo l'email perché la usiamo come identificativo

                // Salva i cambiamenti nel database
                await _context.SaveChangesAsync();

                // Restituisci successo
                return new JsonResult(new
                {
                    success = true,
                    message = "Dati aggiornati con successo"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiornamento: {ex.Message}");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }
    }
}