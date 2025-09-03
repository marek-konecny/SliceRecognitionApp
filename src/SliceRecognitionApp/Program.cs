using Microsoft.AspNetCore.Builder;
using SliceRecognitionApp.Components;
using SliceRecognitionApp.Components.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SampleImageService>();
builder.Services.AddScoped<SliceRecognizeProcessService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<RateLimiterService>(); 

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
