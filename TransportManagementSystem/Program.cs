using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<DelayDetectionService>();
builder.Services.AddSingleton<OsrmRoutingService>();
builder.Services.AddScoped<VrpSolverService>();
builder.Services.AddScoped<AnomalyDetectionService>();
builder.Services.AddScoped<IsolationForestService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Services personnalisés
builder.Services.AddSingleton<ETAPredictionService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "assignments",
    pattern: "Assignments",
    defaults: new { controller = "Assignments", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// ⚠️ Entraînement des modèles APRÈS la construction de l'application
using (var scope = app.Services.CreateScope())
{
    var isolationForestService = scope.ServiceProvider.GetRequiredService<IsolationForestService>();
    await isolationForestService.TrainAll();
}

app.Run();