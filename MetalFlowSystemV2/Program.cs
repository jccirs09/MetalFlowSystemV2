using MetalFlowSystemV2.Client.Pages;
using MetalFlowSystemV2.Components;
using MetalFlowSystemV2.Components.Account;
using MetalFlowSystemV2.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddMudServices();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddScoped<MetalFlowSystemV2.Data.Services.Admin.BranchAdminService>();
builder.Services.AddScoped<MetalFlowSystemV2.Data.Services.Admin.ProductionAreaAdminService>();
builder.Services.AddScoped<MetalFlowSystemV2.Data.Services.Admin.ShiftAdminService>();
builder.Services.AddScoped<MetalFlowSystemV2.Data.Services.Admin.TruckAdminService>();
builder.Services.AddScoped<MetalFlowSystemV2.Data.Services.Admin.UserAdminService>();
builder.Services.AddScoped<MetalFlowSystemV2.Data.Services.Admin.ItemService>();
builder.Services.AddScoped<MetalFlowSystemV2.Data.Services.Admin.InventoryService>();
builder.Services.AddScoped<MetalFlowSystemV2.Data.Services.Admin.InventorySnapshotImportService>();
builder.Services.AddScoped<MetalFlowSystemV2.Data.Services.Admin.PackingStationAdminService>();
builder.Services.AddScoped<MetalFlowSystemV2.Data.Services.Admin.UserWorkAssignmentService>();
builder.Services.AddScoped<MetalFlowSystemV2.Data.Services.ShiftInstanceService>();
builder.Services.AddScoped<MetalFlowSystemV2.Data.Services.PickingListParser>();
builder.Services.AddScoped<MetalFlowSystemV2.Data.Services.PickingListService>();

// Remove AddAuthentication and AddIdentityCookies here because AddIdentity adds them.
// But we might need to configure cookies.
// The issue "Scheme already exists: Identity.Application" suggests AddIdentity adds it, and AddAuthentication adds it again.
// AddIdentity adds cookie authentication.
// We should check what AddIdentityCore vs AddIdentity does.
// AddIdentity adds the default cookie schemes.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<ApplicationDbContext>(p =>
    p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MetalFlowSystemV2.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    await MetalFlowSystemV2.Data.Seed.DbInitializer.Initialize(scope.ServiceProvider);
}

app.Run();
