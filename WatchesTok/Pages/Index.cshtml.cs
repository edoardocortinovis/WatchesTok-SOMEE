using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WatchesTok.Data; // Solo se usi EF Core

namespace WatchesTok.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly AppDbContext _context; // Assumi che tu abbia un DbContext

        // Proprietà per memorizzare i conteggi
        public int NumeroUtenti { get; set; }
        public int NumeroPost { get; set; }

        public IndexModel(ILogger<IndexModel> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public void OnGet()
        {
            try
            {
                // Conta direttamente dal database
                NumeroUtenti = _context.EleUtenti.Count();
                NumeroPost = _context.ElePost.Count();

                _logger.LogInformation($"Conteggi recuperati: {NumeroUtenti} utenti, {NumeroPost} post");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei conteggi");
                NumeroUtenti = 0;
                NumeroPost = 0;
            }
        }
    }
}