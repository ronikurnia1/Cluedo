using Blazored.LocalStorage;
using Cluedo.WebApp.Services;
using Sotsera.Blazor.Toaster.Core.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddToaster(options =>
{
    options.PositionClass = Defaults.Classes.Position.BottomRight;
    options.PreventDuplicates = false;
    options.NewestOnTop = true;
});


builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddBlazoredLocalStorage();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
