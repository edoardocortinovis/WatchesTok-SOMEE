using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json.Serialization;

namespace WatchesTok.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GeminiAIController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GeminiAIController> _logger;
        private readonly string _geminiApiKey = "AIzaSyDZm5vTo1xQypblKGPTmyyw_Q_fp1yEpN0";
        private readonly string _geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

        public GeminiAIController(
            IHttpClientFactory httpClientFactory,
            ILogger<GeminiAIController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public class WatchRequest
        {
            public string WatchName { get; set; }
        }

        // Classe per la risposta di validazione
        private class ValidationResponse
        {
            [JsonPropertyName("isValid")]
            public bool IsValid { get; set; }

            [JsonPropertyName("reason")]
            public string Reason { get; set; }
        }

        // Classe intermedia per deserializzare la risposta di Gemini
        private class RawWatchContentResponse
        {
            public string Titolo { get; set; }
            public string Descrizione { get; set; }
            [JsonPropertyName("Hashtags")]
            public JsonElement Hashtags { get; set; }
        }

        // Classe per la risposta finale
        public class WatchContentResponse
        {
            public string Titolo { get; set; }
            public string Descrizione { get; set; }
            public string Hashtags { get; set; }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { status = "ok", message = "GeminiAI controller è accessibile", apiKey = _geminiApiKey.Substring(0, 5) + "..." });
        }

        [HttpPost("GetContent")]
        public async Task<IActionResult> GetContent([FromBody] WatchRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.WatchName))
            {
                return BadRequest("Il nome dell'orologio è obbligatorio.");
            }

            try
            {
                // PASSO 1: Verifica prima se l'orologio esiste realmente
                var isValidWatch = await ValidateWatchNameAsync(request.WatchName);

                if (!isValidWatch.IsValid)
                {
                    // Se l'orologio non esiste, ritorna 404 Not Found con un messaggio chiaro
                    return NotFound(new
                    {
                        error = "Orologio non trovato",
                        message = $"'{request.WatchName}' non corrisponde ad alcun marchio o modello di orologio conosciuto.",
                        details = isValidWatch.Reason
                    });
                }

                // PASSO 2: Solo se l'orologio è valido, procedi con la generazione del contenuto
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                var prompt = $@"
                Sei un esperto di orologi di lusso. Genera contenuti per un post social sull'orologio: '{request.WatchName}'.
                So già con certezza che questo è un orologio reale perché ho fatto una verifica preventiva.
                
                Fornisci le seguenti informazioni in formato JSON:
                
                1. Titolo: un titolo accattivante per il post (massimo 50 caratteri)
                2. Descrizione: una descrizione dettagliata dell'orologio (circa 150 parole) che includa caratteristiche tecniche, materiali, movimento
                3. Hashtags: un array di 5-7 hashtag rilevanti (puoi fornirli con o senza il simbolo #)
                
                Rispondi SOLO con un oggetto JSON con i campi 'Titolo', 'Descrizione', 'Hashtags'.";

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

                _logger.LogInformation($"Invio richiesta a Gemini per: {request.WatchName}");

                var response = await client.PostAsync($"{_geminiApiUrl}?key={_geminiApiKey}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Errore API Gemini: {responseContent}");
                    return StatusCode((int)response.StatusCode, $"Errore API Gemini: {response.StatusCode}");
                }

                // Estrai il testo dalla risposta di Gemini
                using JsonDocument doc = JsonDocument.Parse(responseContent);

                string extractedText = "";
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    if (candidates[0].TryGetProperty("content", out var content1) &&
                        content1.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        extractedText = parts[0].GetProperty("text").GetString() ?? "";
                    }
                }

                // Cerca di estrarre il JSON dal testo
                int startIndex = extractedText.IndexOf('{');
                int endIndex = extractedText.LastIndexOf('}');

                if (startIndex >= 0 && endIndex > startIndex)
                {
                    string jsonPart = extractedText.Substring(startIndex, endIndex - startIndex + 1);
                    _logger.LogInformation($"JSON estratto: {jsonPart}");

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    // Prima deserializziamo nella classe intermedia che può gestire diverse forme di hashtags
                    var rawResult = JsonSerializer.Deserialize<RawWatchContentResponse>(jsonPart, options);

                    if (rawResult != null)
                    {
                        // Creiamo la risposta finale
                        var result = new WatchContentResponse
                        {
                            Titolo = rawResult.Titolo,
                            Descrizione = rawResult.Descrizione,
                            Hashtags = ConvertHashtags(rawResult.Hashtags)
                        };

                        return Ok(result);
                    }
                }

                return StatusCode(500, "Impossibile elaborare la risposta di Gemini AI");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Eccezione in GetContent: {ex.Message}");
                return StatusCode(500, $"Errore: {ex.Message}");
            }
        }

        // Nuovo metodo per validare se il nome dell'orologio esiste realmente
        private async Task<ValidationResponse> ValidateWatchNameAsync(string watchName)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                // Prompt di validazione specifico e dettagliato
                var validationPrompt = $@"
                SEI UN VALIDATORE SPECIALIZZATO IN OROLOGI DI LUSSO. Il tuo UNICO compito è determinare se l'input rappresenta un vero marchio o modello di orologi.

                Input: '{watchName}'

                REGOLE RIGIDE:
                1. Verifica SOLO se l'input è un marchio di orologi (es. Rolex, Omega, Patek Philippe, Audemars Piguet) o un modello specifico (es. Submariner, Speedmaster)
                2. Input che sono parole casuali, nomi inventati, o generici NON sono validi
                3. Se hai QUALSIASI dubbio sulla validità, considera l'input NON VALIDO
                4. Parole generiche come 'orologio d'oro', 'orologio digitale', 'smartwatch' NON sono validi
                5. Il nome deve riferirsi a uno SPECIFICO marchio/modello REALE nel mondo dell'orologeria

                Rispondi SOLO con un oggetto JSON con questa struttura esatta:
                {{
                  ""isValid"": true/false,
                  ""reason"": ""breve spiegazione della decisione""
                }}
                
                Se l'input somiglia a un marchio o modello reale ma con errori di ortografia, indicalo nella ragione e considera NON VALIDO.
                ";

                var validationRequestObject = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = validationPrompt
                                }
                            }
                        }
                    }
                };

                var validationRequestJson = JsonSerializer.Serialize(validationRequestObject);
                var validationContent = new StringContent(validationRequestJson, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Verifica validità per: {watchName}");

                var validationResponse = await client.PostAsync($"{_geminiApiUrl}?key={_geminiApiKey}", validationContent);
                var validationResponseContent = await validationResponse.Content.ReadAsStringAsync();

                if (!validationResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"Errore durante la validazione: {validationResponseContent}");
                    return new ValidationResponse { IsValid = false, Reason = "Errore durante la validazione" };
                }

                // Estrai il JSON dalla risposta
                using JsonDocument doc = JsonDocument.Parse(validationResponseContent);

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

                // Trova e estrai il JSON dalla risposta
                int startIndex = extractedText.IndexOf('{');
                int endIndex = extractedText.LastIndexOf('}');

                if (startIndex >= 0 && endIndex > startIndex)
                {
                    string jsonPart = extractedText.Substring(startIndex, endIndex - startIndex + 1);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var result = JsonSerializer.Deserialize<ValidationResponse>(jsonPart, options);

                    if (result != null)
                    {
                        _logger.LogInformation($"Validazione per '{watchName}': {(result.IsValid ? "VALIDO" : "NON VALIDO")} - {result.Reason}");
                        return result;
                    }
                }

                return new ValidationResponse { IsValid = false, Reason = "Impossibile determinare la validità" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Eccezione durante la validazione: {ex.Message}");
                return new ValidationResponse { IsValid = false, Reason = $"Errore durante la validazione: {ex.Message}" };
            }
        }

        // Metodo per convertire hashtags da vari formati a stringa
        private string ConvertHashtags(JsonElement hashtagsElement)
        {
            try
            {
                // Se hashtags è un array di stringhe
                if (hashtagsElement.ValueKind == JsonValueKind.Array)
                {
                    var hashtags = new System.Collections.Generic.List<string>();
                    foreach (var element in hashtagsElement.EnumerateArray())
                    {
                        var hashtag = element.GetString();
                        if (!string.IsNullOrEmpty(hashtag))
                        {
                            // Aggiungi # se non presente
                            if (!hashtag.StartsWith("#"))
                                hashtag = "#" + hashtag;
                            hashtags.Add(hashtag);
                        }
                    }
                    return string.Join(" ", hashtags);
                }
                // Se hashtags è una stringa singola
                else if (hashtagsElement.ValueKind == JsonValueKind.String)
                {
                    var hashtagsString = hashtagsElement.GetString() ?? "";
                    // Assumiamo che sia già formattata come una stringa di hashtag
                    return hashtagsString;
                }
            }
            catch
            {
                _logger.LogWarning("Errore nella conversione degli hashtag");
            }

            return "";
        }
    }
}