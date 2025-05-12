using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WatchesTok.Models
{
    public class Post
    {
        [Key]
        public int PostID { get; set; }

        [Required]
        public int UtenteID { get; set; }

        [Required]
        [StringLength(100)]
        public string? Titolo { get; set; }

        [StringLength(1000)]
        public string? Descrizione { get; set; }

        [StringLength(200)]
        public string? Hashtags { get; set; }

        [StringLength(100)]
        public string? MarcaModello { get; set; }

        [Required]
        [StringLength(255)]
        public string? MediaPath { get; set; }

        [Required]
        public DateTime DataCreazione { get; set; } = DateTime.Now;

        // Rimosso OrologioID e proprietà di navigazione Orologio

        // Proprietà di navigazione per utente
        [ForeignKey("UtenteID")]
        public virtual Utente? Utente { get; set; }

        // Collezione di like per questo post
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

        // Metodo di utility per contare i like
        [NotMapped]
        public int LikeCount => Likes?.Count ?? 0;
    }
}