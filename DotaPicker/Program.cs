

using Microsoft.EntityFrameworkCore;
using webhook_gateway.DB;
using webhook_gateway.Interfaces;
using webhook_gateway.Services;
using webhook_gateway.Singleton;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


var stratzToken = builder.Configuration["Stratz:Token"] ?? throw new NullReferenceException("Stratz token is null");

builder.Services.AddHttpClient("StratzClient", client =>
{
    client.BaseAddress = new Uri("https://api.stratz.com/graphql");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {stratzToken}");
    client.DefaultRequestHeaders.Add("User-Agent", "STRATZ_API");
});
    


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=dota_meta.db"));




builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddSignalR();
builder.Services.AddScoped<IPickerAnalyzer, PickerAnalyzer>();
builder.Services.AddSingleton<IMetaCache, InMemoryDotaMetaCache>();
builder.Services.AddHostedService<BackgroundDataFetcher>();
builder.Services.AddTransient<IMetaDataFetcher, MetaDataFetcher>();
var app = builder.Build();




// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI();


app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.UseDefaultFiles(); 
app.UseStaticFiles();  

app.MapControllers();

app.Run();