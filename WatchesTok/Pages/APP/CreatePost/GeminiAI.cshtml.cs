using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WatchesTok.Pages
{
    public class GeminiAIModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiAIModel> _logger;
        private readonly string _geminiApiKey;
        private readonly string _geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";

        public GeminiAIModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<GeminiAIModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _geminiApiKey = _configuration["Gemini:ApiKey"];
        }

        public class WatchRequest
        {
            public string WatchName { get; set; }
        }

        public class WatchContentResponse
        {
            public string Titolo { get; set; }
            public string Descrizione { get; set; }
            public string Hashtags { get; set; }
        }

        public async Task<IActionResult> OnPostGenerateContentAsync([FromBody] WatchRequest request)
        {
            if (string.IsNullOrEmpty(request?.WatchName))
            {
                return BadRequest("Il nome dell'orologio è obbligatorio.");
            }

            try
            {
                // Risposta di default in caso di errore
                var defaultResponse = new WatchContentResponse
                {
                    Titolo = $"Il colosso {request.WatchName}",
                    Descrizione = $"Il {request.WatchName} è un orologio che rappresenta l'eccellenza dell'orologeria. " +
                                  "Con materiali pregiati e un design intramontabile, è l'accessorio perfetto per ogni occasione.",
                    Hashtags = $"#{request.WatchName.Replace(" ", "")} #watches #luxury #timepiece"
                };

                // Se non abbiamo una chiave API, restituisci il contenuto di default
                if (string.IsNullOrEmpty(_geminiApiKey))
                {
                    _logger.LogWarning("Chiave API Gemini non configurata, utilizzo risposta predefinita");
                    return new JsonResult(defaultResponse);
                }

                var client = _httpClientFactory.CreateClient();

                // Costruisci il prompt per Gemini
                var prompt = $@"

                Tu sei un validatore di nomi di orologi di lusso. Data la stringa '{{request.WatchName}}', segui queste istruzioni in modo PRECISO:

STEP 1: VALUTAZIONE DEL NOME
Verifica se '{{request.WatchName}}' rappresenta UN OROLOGIO REALE nel mondo dell'orologeria.
Deve essere:
- Un marchio specifico di orologi (esempio: Rolex, Omega, Patek Philippe)
- Un modello specifico di orologio (esempio: Submariner, Speedmaster, Royal Oak)
- Una combinazione precisa marca-modello (esempio: Rolex Daytona)

STEP 2: DECISIONE RIGIDA
Se '{{request.WatchName}}' è UNO QUALSIASI di questi:
- Sequenza casuale di caratteri
- Nome inventato
- Termine generico (come ""orologio digitale"", ""orologio d'oro"")
- Qualsiasi input che non sia SPECIFICAMENTE un orologio reale e riconosciuto
- Qualsiasi cosa per cui hai IL MINIMO DUBBIO che sia un orologio reale

ALLORA DEVI PRODURRE ESATTAMENTE QUESTO JSON:
{{
  ""Titolo"": ""errore"",
  ""Descrizione"": ""errore"",
  ""Hashtags"": ""errore""
}}

STEP 3: SOLO SE ASSOLUTAMENTE CERTO che '{{request.WatchName}}' è un orologio reale:
Produci un JSON con:
- ""Titolo"": titolo accattivante (max 50 caratteri)
- ""Descrizione"": descrizione dettagliata dell'orologio (150-200 parole)
- ""Hashtags"": 5-7 hashtag rilevanti

RICORDA: In caso di QUALSIASI INCERTEZZA, rispondi con il JSON di errore. È MEGLIO dare errore su un orologio reale che fornire informazioni su uno non esistente.

Rispondi SOLO con un oggetto JSON valido senza altro testo.                

";
                // Prepara la richiesta per Gemini
                var geminiRequestObject = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = prompt
                                }
                            }
                        }
                    }
                };

                var geminiRequestJson = JsonSerializer.Serialize(geminiRequestObject);
                var content = new StringContent(geminiRequestJson, Encoding.UTF8, "application/json");

                // Invia la richiesta a Gemini
                var response = await client.PostAsync($"{_geminiApiUrl}?key={_geminiApiKey}", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Risposta ricevuta: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Errore nella chiamata a Gemini API: {responseContent}");
                    return new JsonResult(defaultResponse);
                }

                // Tenta di estrarre il contenuto JSON dalla risposta
                var result = ExtractContentFromResponse(responseContent);
                if (result == null)
                {
                    return new JsonResult(defaultResponse);
                }

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella generazione dei contenuti con Gemini AI");
                return StatusCode(500, "Si è verificato un errore interno durante la generazione dei contenuti");
            }
        }

        private WatchContentResponse ExtractContentFromResponse(string responseContent)
        {
            try
            {
                // Estrarre il testo dalla risposta
                using JsonDocument doc = JsonDocument.Parse(responseContent);

                string extractedText = "";
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    if (candidates[0].TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        extractedText = parts[0].GetProperty("text").GetString() ?? "";
                    }
                }

                // Cercare di estrarre il JSON dal testo
                int startIndex = extractedText.IndexOf('{');
                int endIndex = extractedText.LastIndexOf('}');

                if (startIndex >= 0 && endIndex > startIndex)
                {
                    string jsonPart = extractedText.Substring(startIndex, endIndex - startIndex + 1);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var result = JsonSerializer.Deserialize<WatchContentResponse>(jsonPart, options);
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'estrazione del contenuto dalla risposta");
                return null;
            }
        }
    }
}