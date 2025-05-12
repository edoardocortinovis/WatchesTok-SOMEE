using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WatchesTok.Models
{
    public class Like
    {
        [Key]
        public int LikeID { get; set; }

        [Required]
        public int PostID { get; set; }

        [Required]
        public int UtenteID { get; set; }

        public DateTime DataCreazione { get; set; }

        // Proprietà di navigazione
        [ForeignKey("PostID")]
        public virtual Post? Post { get; set; }

        [ForeignKey("UtenteID")]
        public virtual Utente? Utente { get; set; }
    }
}