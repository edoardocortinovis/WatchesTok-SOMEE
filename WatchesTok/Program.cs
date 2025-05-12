using Microsoft.EntityFrameworkCore;
using WatchesTok.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

//GIUSTISSIMOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO

// Configura il DbContext con SQL Server invece di SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("workstation id=Orologi.mssql.somee.com;packet size=4096;user id=edocorti_SQLLogin_1;pwd=p9qa65jbmj;data source=Orologi.mssql.somee.com;persist security info=False;initial catalog=Orologi;TrustServerCertificate=True"));


// Aggiungi i servizi di Razor Pages, Controllers, HttpClient
builder.Services.AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        // Configura le opzioni di Razor Pages
        options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute());
    });

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddHttpClient();

// Configura CORS per risolvere problemi cross-origin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
    loggingBuilder.SetMinimumLevel(LogLevel.Warning);
});

var app = builder.Build();

// Inizializzazione del database - Assicurati che il metodo SeedData.Initialize sia compatibile con SQL Server
using (var scope = app.Services.CreateScope())
{
    var dbcontext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Commenta la riga sotto se hai problemi con il seeding durante il primo deploy
    SeedData.Initialize(dbcontext);
}

// Configurazione del middleware
/*if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}*/

app.UseDeveloperExceptionPage();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Abilita CORS
app.UseCors("AllowAll");

app.MapRazorPages();
app.MapControllers();

app.Run();