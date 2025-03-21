var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpClient();  // ✅ Registers HttpClient for DI
DotNetEnv.Env.Load();
var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapControllers();  // ✅ Maps all controllers


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
