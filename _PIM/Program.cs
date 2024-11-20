using _PIM.Data;
using _PIM.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Adiciona os servi�os ao cont�iner.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient<WeatherService>();

// Configura��o do servi�o de banco de dados
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DbPath")));

// Configura��o do Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 0;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Adiciona suporte a sess�es
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tempo de expira��o
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Middleware para cria��o de pap�is e usu�rio admin
async Task CreateRoles(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // Defina os pap�is que queremos garantir que existam
    string[] roleNames = { "Admin", "User" };
    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Criar um usu�rio Admin padr�o, caso n�o exista
    var adminUser = await userManager.FindByEmailAsync("admin@example.com");
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = "admin@example.com",
            Email = "admin@example.com"
        };
        await userManager.CreateAsync(adminUser, "Admin@123");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

// Configura��o do pipeline de requisi��o HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Middleware de autentica��o, autoriza��o e sess�o
app.UseAuthentication();
app.UseAuthorization();
app.UseSession(); // Ativa��o do suporte a sess�es

// Mapeamento de rotas
app.MapControllerRoute(
    name: "loja",
    pattern: "Loja",
    defaults: new { controller = "Home", action = "Index" });

app.MapControllerRoute(
    name: "dashboard_produtos",
    pattern: "Dashboard/Produtos",
    defaults: new { controller = "Produto", action = "Index" });

app.MapControllerRoute(
    name: "dashboard",
    pattern: "Dashboard",
    defaults: new { controller = "Admin", action = "Index" });

app.MapControllerRoute(
    name: "dashboard_sensores",
    pattern: "Dashboard/Sensores",
    defaults: new { controller = "Sensor", action = "Index" });

app.MapControllerRoute(
    name: "entrar",
    pattern: "Entrar",
    defaults: new { controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "criar_conta",
    pattern: "CriarConta",
    defaults: new { controller = "Account", action = "Register" });

app.MapControllerRoute(
    name: "pagamento",
    pattern: "pagamento",
    defaults: new { controller = "Pagamento", action = "Index" });

// Rota padr�o para controllers
app.MapDefaultControllerRoute();

// Executa a cria��o dos pap�is e do usu�rio admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await CreateRoles(services);
}

app.Run();
