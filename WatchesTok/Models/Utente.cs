using System.ComponentModel.DataAnnotations;

namespace WatchesTok.Models
{
    public class Utente
    {
        [Key]
        public int UtenteID { get; set; } 

        public  string? Nome {  get; set; }

        public  string? Cognome { get; set; }

        public  string? email { get; set; }

        public  string? password { get; set; }
    }
}
