using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace WebScraperAPI.Controllers
{
    [Route("api/specs")]
    [ApiController]
    public class AIExtractController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _geminiApiKey = "AIzaSyD_IZfeJ3SK4-FNzIZplcdoWi2KP-O0kCQ"; // Replace with actual API key

        public AIExtractController()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
        }

        // Endpoint to scrape and extract specifications
        [HttpGet("{model}")]
        public async Task<IActionResult> GetSpecs(string model)
        {
            string specsText = await ScrapeSpecifications(model);

            if (string.IsNullOrWhiteSpace(specsText))
                return BadRequest(new { error = "Could not retrieve specifications" });

            var extractedSpecs = await ExtractSpecsWithGemini(specsText);
            return Ok(extractedSpecs);
        }

        // Scrapes specifications from Google search results
        private async Task<string> ScrapeSpecifications(string model)
        {
            string searchUrl = $"https://serpapi.com/search.json?q={model}+specifications&api_key=873ab37c117c76394e5b68c481f1822373aae377ceaccd758cebf8a912c82ec3";


            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(searchUrl);
                if (!response.IsSuccessStatusCode)
                    return null;

                string pageContent = await response.Content.ReadAsStringAsync();
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(pageContent);

                // Extract all visible text from the page
                string specsText = htmlDoc.DocumentNode.InnerText;
                return specsText;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error scraping: " + ex.Message);
                return null;
            }
        }

        // Calls Gemini API to extract structured data
        private async Task<DesktopSpecs> ExtractSpecsWithGemini(string specsText)
        {
            var requestData = new
            {
                contents = new[]
                {
            new
            {
                parts = new[]
                {
                    new { text = $"Extract desktop specifications from this text: \"{specsText}\". Return JSON with keys: Processor, RamType, RamSlot, RamCapacity, StorageCapacity." }
                }
            }
        }
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_geminiApiKey}",
                jsonContent
            );

            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadAsStringAsync();

            try
            {
                // Deserialize the full Gemini response
                var geminiResponse = JsonConvert.DeserializeObject<GeminiResponse>(result);

                // Extract JSON text from response
                string jsonSpecs = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text;

                if (string.IsNullOrWhiteSpace(jsonSpecs))
                    return null;

                // Clean and extract valid JSON from Markdown formatting (if present)
                jsonSpecs = jsonSpecs.Replace("json", "").Trim('`').Trim();

                jsonSpecs = jsonSpecs.Trim().Trim('`');  // Removes markdown-style backticks

                // Deserialize into a dynamic object
                var rawData = JsonConvert.DeserializeObject<dynamic>(jsonSpecs);

                if (rawData == null)
                    return null;

                // Map extracted values into DesktopSpecs model

                var desktopSpecs = new DesktopSpecs
                {
                    Processors = ExtractList(rawData.Processor),
                    RamTypes = ExtractList(rawData.RamType),
                    RamSlots = ExtractList(rawData.RamSlot),
                    RamCapacities = ExtractList(rawData.RamCapacity),
                    StorageCapacities = ExtractList(rawData.StorageCapacity)
                };

                return desktopSpecs;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Gemini response: {ex.Message}");
                return null;
            }
        }

        List<string> ExtractList(dynamic data)
        {
            if (data is JArray array)
            {
                return array.ToObject<List<string>>() ?? new List<string>();
            }
            return data != null ? new List<string> { data.ToString() } : new List<string>();
        }
    }


    public class GeminiResponse
    {
        public List<Candidate> Candidates { get; set; }
    }

    public class Candidate
    {
        public Content Content { get; set; }
    }

    public class Content
    {
        public List<Part> Parts { get; set; }
    }

    public class Part
    {
        public string Text { get; set; }
    }

 

    public class DesktopSpecs
    {
        public List<string> Processors { get; set; }
        public List<string> RamTypes { get; set; }
        public List<string> RamSlots { get; set; }
        public List<string> RamCapacities { get; set; }
        public List<string> StorageCapacities { get; set; }
    }
}
