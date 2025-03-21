using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/specs")]
   
        public class GeminiController : ControllerBase
        {
            private readonly HttpClient _httpClient;

            public GeminiController(HttpClient httpClient)
            {
                _httpClient = httpClient;
            }

        [HttpGet]
        public async Task<IActionResult> GetSpecs([FromQuery] string model)
        {
            if (string.IsNullOrEmpty(model))
            {
                return BadRequest(new { error = "Model parameter is required." });
            }

            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                return StatusCode(500, new { error = "API key is missing or not configured." });
            }

            var requestBody = new
            {
                contents = new[]
                {
            new
            {
                parts = new[]
                {
                    new
                    {
                        text = $"Extract and format the specifications of the computer model '{model}'  in JSON with keys: model, configurations[]. Example format: {{\"model\": \"Acer Veriton X4710G\", \"configurations\": [{{\"specs\": \"Intel Core i7-13700\", \"ramType\": \"DDR4\", \"ramStorage\": \"16GB\", \"ramSlot\": \"up to 128GB\", \"storageCapacity\": \"512GB/1TB SSD\"}}]}}. Search it's specification. Thank you!."
                    }
                }
            }
        }
            };

            string requestJson = JsonSerializer.Serialize(requestBody);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            string geminiUrl = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.0-flash:generateContent?key={apiKey}";

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(geminiUrl, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { error = "Failed to fetch data from Gemini API." });
                }

                string responseContent = await response.Content.ReadAsStringAsync();

                try
                {
                    // ✅ Extract JSON text from Gemini response
                    var jsonResponse = JsonDocument.Parse(responseContent);
                    string extractedJson = jsonResponse.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    // ✅ Remove Markdown JSON formatting
                    extractedJson = extractedJson.Replace("```json", "").Replace("```", "").Trim();

                    // ✅ Deserialize the JSON into a strongly typed model
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // Allows deserialization even if key casing differs
                    };

                    var computerData = JsonSerializer.Deserialize<ComputerResponse>(extractedJson, options);

                    if (computerData?.Configurations == null || computerData.Configurations.Length == 0)
                    {
                        return BadRequest(new { error = "No valid configurations found." });
                    }

                    // ✅ Extract only required properties
                    var extractedSpecs = computerData.Configurations.Select(config => new
                    {
                        config.Specs,
                        config.RamType,
                        config.RamStorage,
                        config.RamSlot,
                        config.StorageCapacity
                    });

                    return Ok(new { Model = computerData.Model, Configurations = extractedSpecs });
                }
                catch (JsonException ex)
                {
                    return StatusCode(500, new { error = "Invalid JSON response from Gemini API.", details = ex.Message });
                }
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { error = "Request to Gemini API failed.", details = ex.Message });
            }
        }

    }
    }

    public class ComputerResponse
    {
        public string Model { get; set; }
        public ComputerSpecs[] Configurations { get; set; }
    }


    public class ComputerSpecs
    {
        public string Specs { get; set; }
        public string RamType { get; set; }
        public string RamStorage { get; set; }
        public string RamSlot { get; set; }
        public string StorageCapacity { get; set; }

    [JsonIgnore] public string Graphics { get; set; }
    [JsonIgnore] public string Ports { get; set; }
    [JsonIgnore] public string Os { get; set; }
    [JsonIgnore] public string PowerSupply { get; set; }
}






//$"Extract and format the specifications of the computer model '{model}'  in JSON with keys: model, configurations[]. Example format: {{\"model\": \"Acer Veriton X4710G\", \"configurations\": [{{\"specs\": \"Intel Core i7-13700\", \"ramType\": \"DDR4\", \"ramStorage\": \"16GB\", \"ramSlot\": \"up to 128GB\", \"storageCapacity\": \"512GB/1TB SSD\"}}]}}. Search it's specification. Thank you!" 