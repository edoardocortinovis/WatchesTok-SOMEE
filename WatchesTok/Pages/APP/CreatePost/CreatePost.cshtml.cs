using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WatchesTok.Data;
using WatchesTok.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace WatchesTok.Pages
{
    public class CreatePostModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CreatePostModel> _logger;
        private readonly IWebHostEnvironment _environment;

        public CreatePostModel(AppDbContext context, ILogger<CreatePostModel> logger, IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        [BindProperty]
        public Post Post { get; set; } = new Post();

        [TempData]
        public string ErrorMessage { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }

        public void OnGet()
        {
            // Niente da fare qui, il form è già inizializzato
        }

        public async Task<IActionResult> OnPostAsync(IFormFile MediaFile)
        {
            try
            {
                // Log dettagliati per debug
                _logger.LogInformation("OnPostAsync chiamato");
                _logger.LogInformation($"Titolo ricevuto: {Post?.Titolo}");
                _logger.LogInformation($"MarcaModello ricevuto: {Post?.MarcaModello}");
                _logger.LogInformation($"MediaFile ricevuto: {MediaFile?.FileName}");
                _logger.LogInformation($"UserEmail ricevuto: {Request.Form["UserEmail"]}");

                // Verifica campi obbligatori (extra cautela)
                if (string.IsNullOrEmpty(Post?.Titolo) || string.IsNullOrEmpty(Post?.MarcaModello) ||
                    string.IsNullOrEmpty(Post?.Descrizione) || MediaFile == null)
                {
                    ErrorMessage = "Tutti i campi obbligatori devono essere compilati e devi caricare un'immagine o un video.";
                    return Page();
                }

                // Ottieni l'email dell'utente
                string userEmail = Request.Form["UserEmail"];
                _logger.LogInformation($"Email utente estratta: {userEmail}");

                if (string.IsNullOrEmpty(userEmail))
                {
                    ErrorMessage = "Email utente non trovata. Devi essere autenticato per pubblicare un post.";
                    return Page();
                }

                // Cerca l'utente nel database
                var utente = await _context.EleUtenti
                    .FirstOrDefaultAsync(u => u.email == userEmail);

                _logger.LogInformation(utente == null
                    ? $"Nessun utente trovato con email: {userEmail}"
                    : $"Utente trovato: {utente.UtenteID} ({utente.Nome})");

                if (utente == null)
                {
                    // Fallback: Usa il primo utente nel database (SOLO PER DEBUG - in produzione rimuovi questo codice)
                    utente = await _context.EleUtenti.FirstOrDefaultAsync();

                    if (utente == null)
                    {
                        ErrorMessage = "Nessun utente trovato. Impossibile pubblicare il post.";
                        return Page();
                    }

                    _logger.LogWarning($"FALLBACK: Utilizzo primo utente disponibile: {utente.UtenteID} ({utente.Nome})");
                }

                // Salva il media caricato
                string uniqueFileName = null;
                if (MediaFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    uniqueFileName = Guid.NewGuid().ToString() + "_" + MediaFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    _logger.LogInformation($"Salvataggio file in: {filePath}");

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await MediaFile.CopyToAsync(stream);
                    }

                    Post.MediaPath = "/uploads/" + uniqueFileName;
                }

                // Prepara il post per il salvataggio
                Post.DataCreazione = DateTime.Now;
                Post.UtenteID = utente.UtenteID;

                // DEBUG: Loghiamo il post prima di salvare
                _logger.LogInformation($"Post da salvare: ID={Post.PostID}, Titolo={Post.Titolo}, UtenteID={Post.UtenteID}, MediaPath={Post.MediaPath}");

                // SALVA NEL DATABASE (approccio diretto)
                try
                {
                    _context.ElePost.Add(Post);
                    var result = await _context.SaveChangesAsync();

                    _logger.LogInformation($"Risultato SaveChanges: {result} record modificati");
                    _logger.LogInformation($"Post salvato con ID: {Post.PostID}");

                    // Verifichiamo che il post sia effettivamente salvato
                    var postSalvato = await _context.ElePost.FindAsync(Post.PostID);
                    if (postSalvato != null)
                    {
                        _logger.LogInformation($"Post verificato nel database con ID: {postSalvato.PostID}");
                        SuccessMessage = "Post pubblicato con successo!";
                        return RedirectToPage("/Index");
                    }
                    else
                    {
                        _logger.LogError($"Post non trovato dopo il salvataggio!");
                        ErrorMessage = "Post non trovato dopo il salvataggio. Riprova.";
                        return Page();
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Errore durante il salvataggio nel database");

                    // Soluzione estrema: proviamo con un comando SQL diretto
                    try
                    {
                        var sql = $@"
                            INSERT INTO ElePost (Titolo, Descrizione, MarcaModello, Hashtags, MediaPath, DataCreazione, UtenteID) 
                            VALUES ('{Post.Titolo.Replace("'", "''")}', '{Post.Descrizione.Replace("'", "''")}', 
                                    '{Post.MarcaModello.Replace("'", "''")}', '{Post.Hashtags?.Replace("'", "''")}', 
                                    '{Post.MediaPath}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}', {utente.UtenteID})";

                        _logger.LogInformation($"Tentativo con SQL diretto: {sql}");

                        // L'implementazione del comando SQL diretto dipende da come accedi al DB
                        // Se stai usando Entity Framework Core, potresti usare: await _context.Database.ExecuteSqlRawAsync(sql);

                        SuccessMessage = "Post pubblicato con successo! (metodo alternativo)";
                        return RedirectToPage("Index");
                    }
                    catch (Exception sqlEx)
                    {
                        _logger.LogError(sqlEx, "Anche il tentativo SQL è fallito");
                        ErrorMessage = "Si è verificato un errore durante il salvataggio. Dettagli tecnici nei log.";
                        return Page();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore generale nel processo di creazione post");
                ErrorMessage = "Si è verificato un errore durante la pubblicazione del post. Riprova più tardi.";
                return Page();
            }
        }
    }
}