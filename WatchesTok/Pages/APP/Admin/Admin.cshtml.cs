using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WatchesTok.Pages
{
    public class AdminModel : PageModel
    {
        public void OnGet()
        {
            // La logica di controllo accesso � gestita lato client tramite JavaScript
            // Ci� consente di usare le informazioni di sessione memorizzate dal client
        }
    }
}