using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

//app.UseHttpsRedirection();



var httpClient = new HttpClient();

DotNetEnv.Env.Load();
app.MapGet("/specs", async ([FromQuery] string model) =>
{
    if (string.IsNullOrEmpty(model))
    {
        return Results.BadRequest(new { error = "Model parameter is required." });
    }

    // ✅ Get API Key securely from Environment Variables
    var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.StatusCode(500);
    }

    string geminiPrompt = $"Extract and format the specifications of the computer model '{model}' in JSON with keys: model, configurations[]. Example format: {{\"model\": \"Acer Veriton X4710G\", \"configurations\": [{{\"specs\": \"Intel Core i7-13700\", \"ramType\": \"DDR4\", \"ramStorage\": \"16GB\", \"ramSlot\": \"up to 128GB\", \"storageCapacity\": \"512GB/1TB SSD\"}}]}}.";

    var requestBody = new
    {
        contents = new[]
        {
            new { parts = new[] { new { text = geminiPrompt } } }
        }
    };

    string requestJson = JsonSerializer.Serialize(requestBody);
    var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

    string geminiUrl = $"https://generativelanguage.googleapis.com/v1/models/gemini-pro:generateContent?key={apiKey}";

    try
    {
        HttpResponseMessage response = await httpClient.PostAsync(geminiUrl, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            return Results.StatusCode((int)response.StatusCode);
        }

        string responseContent = await response.Content.ReadAsStringAsync();

        // ✅ Handle unexpected JSON parsing errors
        try
        {
            var jsonResponse = JsonSerializer.Deserialize<object>(responseContent);
            return Results.Ok(jsonResponse);
        }
        catch (JsonException)
        {
            return Results.StatusCode(500);
        }
    }
    catch (Exception ex)
    {
        return Results.StatusCode(500);
    }
});
app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
