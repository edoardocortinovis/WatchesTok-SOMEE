using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WatchesTok.Data;
using WatchesTok.Models;

namespace WatchesTok.Pages
{
    public class HomepageModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomepageModel> _logger;

        public HomepageModel(AppDbContext context, ILogger<HomepageModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Post> Posts { get; set; }
        public string MarcaFiltro { get; set; }
        public int PostsPerLoad { get; set; } = 10;  // Carica 10 post alla volta per lo scroll infinito
        public int CurrentPage { get; set; } = 1;
        public bool HasMorePosts { get; set; }

        public async Task OnGetAsync(string marca, int? page)
        {
            try
            {
                CurrentPage = page ?? 1;
                MarcaFiltro = marca;

                // Query base includendo l'utente per accedere ai suoi dati
                var query = _context.ElePost
                    .Include(p => p.Utente)
                    .Include(p => p.Likes)
                    .AsQueryable();

                // Applica filtro per marca se specificato
                if (!string.IsNullOrEmpty(marca))
                {
                    query = query.Where(p => p.MarcaModello.Contains(marca));
                }

                // Ottieni i post ordinati per data più recente con paginazione
                var totalPosts = await query.CountAsync();
                Posts = await query
                    .OrderByDescending(p => p.DataCreazione)
                    .Skip((CurrentPage - 1) * PostsPerLoad)
                    .Take(PostsPerLoad)
                    .ToListAsync();

                // Verifica se esistono altri post da caricare
                HasMorePosts = (CurrentPage * PostsPerLoad) < totalPosts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel caricamento dei post");
                Posts = new List<Post>();
            }
        }
    }
}