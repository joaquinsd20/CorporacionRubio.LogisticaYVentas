using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CorporacionRubio.Data;
using CorporacionRubio.Data.Entities;
using CorporacionRubio.Business;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RubioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConexionRubio")));

// ASP.NET Core Identity: autenticación, autorización, roles y sesiones seguras.
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<RubioDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(45);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = "Rubio.Admin.Identity";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PanelAdministrativo", policy =>
        policy.RequireRole("Administrador", "JefeLogistica"));
});

builder.Services.AddScoped<LogisticaService>();

// Sesión auxiliar para mostrar datos del usuario y conservar compatibilidad con el módulo existente.
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews();

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
app.UseAuthentication();
app.UseAuthorization();

await EnsureIdentitySchemaAsync(app);
await SeedIdentityAsync(app);
await SeedCatalogoCorporativoAsync(app);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


static async Task EnsureIdentitySchemaAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RubioDbContext>();

    // Corrección para bases creadas previamente con el script SQL original:
    // si las tablas de ASP.NET Core Identity todavía no existen, se crean antes de sembrar roles y usuarios.
    await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID('[dbo].[AspNetRoles]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetRoles](
        [Id] [nvarchar](450) NOT NULL,
        [Name] [nvarchar](256) NULL,
        [NormalizedName] [nvarchar](256) NULL,
        [ConcurrencyStamp] [nvarchar](max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID('[dbo].[AspNetUsers]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUsers](
        [Id] [nvarchar](450) NOT NULL,
        [UserName] [nvarchar](256) NULL,
        [NormalizedUserName] [nvarchar](256) NULL,
        [Email] [nvarchar](256) NULL,
        [NormalizedEmail] [nvarchar](256) NULL,
        [EmailConfirmed] [bit] NOT NULL,
        [PasswordHash] [nvarchar](max) NULL,
        [SecurityStamp] [nvarchar](max) NULL,
        [ConcurrencyStamp] [nvarchar](max) NULL,
        [PhoneNumber] [nvarchar](max) NULL,
        [PhoneNumberConfirmed] [bit] NOT NULL,
        [TwoFactorEnabled] [bit] NOT NULL,
        [LockoutEnd] [datetimeoffset](7) NULL,
        [LockoutEnabled] [bit] NOT NULL,
        [AccessFailedCount] [int] NOT NULL,
        [Nombres] [nvarchar](100) NULL,
        [Apellidos] [nvarchar](100) NULL,
        [NombreCompleto] [nvarchar](120) NULL,
        [Cargo] [nvarchar](80) NULL,
        [Area] [nvarchar](80) NULL,
        [Direccion] [nvarchar](120) NULL,
        [Ciudad] [nvarchar](60) NULL,
        [Pais] [nvarchar](60) NULL,
        [Estado] [nvarchar](30) NOT NULL CONSTRAINT [DF_AspNetUsers_Estado] DEFAULT(N'Activo'),
        [FechaRegistro] [datetime2](7) NOT NULL CONSTRAINT [DF_AspNetUsers_FechaRegistro] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID('[dbo].[AspNetRoleClaims]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetRoleClaims](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [RoleId] [nvarchar](450) NOT NULL,
        [ClaimType] [nvarchar](max) NULL,
        [ClaimValue] [nvarchar](max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID('[dbo].[AspNetUserClaims]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserClaims](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [nvarchar](450) NOT NULL,
        [ClaimType] [nvarchar](max) NULL,
        [ClaimValue] [nvarchar](max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID('[dbo].[AspNetUserLogins]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserLogins](
        [LoginProvider] [nvarchar](450) NOT NULL,
        [ProviderKey] [nvarchar](450) NOT NULL,
        [ProviderDisplayName] [nvarchar](max) NULL,
        [UserId] [nvarchar](450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID('[dbo].[AspNetUserRoles]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserRoles](
        [UserId] [nvarchar](450) NOT NULL,
        [RoleId] [nvarchar](450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID('[dbo].[AspNetUserTokens]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserTokens](
        [UserId] [nvarchar](450) NOT NULL,
        [LoginProvider] [nvarchar](450) NOT NULL,
        [Name] [nvarchar](450) NOT NULL,
        [Value] [nvarchar](max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'RoleNameIndex')
    CREATE UNIQUE INDEX [RoleNameIndex] ON [dbo].[AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'EmailIndex')
    CREATE INDEX [EmailIndex] ON [dbo].[AspNetUsers] ([NormalizedEmail]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UserNameIndex')
    CREATE UNIQUE INDEX [UserNameIndex] ON [dbo].[AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetRoleClaims_RoleId')
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims] ([RoleId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserClaims_UserId')
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserLogins_UserId')
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserRoles_RoleId')
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles] ([RoleId]);


IF OBJECT_ID('[dbo].[Comprobante_Pago_Pedido]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Comprobante_Pago_Pedido](
        [id_pago] [int] IDENTITY(1,1) NOT NULL,
        [id_pedido] [int] NOT NULL,
        [metodo_pago] [varchar](40) NOT NULL,
        [referencia_operacion] [varchar](80) NOT NULL,
        [tipo_comprobante] [varchar](40) NULL,
        [estado_pago] [varchar](40) NOT NULL CONSTRAINT [DF_ComprobantePago_Estado] DEFAULT('Pendiente de validación'),
        [observacion] [varchar](200) NULL,
        [fecha_registro] [datetime] NOT NULL CONSTRAINT [DF_ComprobantePago_Fecha] DEFAULT(GETDATE()),
        [fecha_validacion] [datetime] NULL,
        [id_usuario_validacion] [int] NULL,
        CONSTRAINT [PK_Comprobante_Pago_Pedido] PRIMARY KEY ([id_pago]),
        CONSTRAINT [FK_ComprobantePago_Pedido] FOREIGN KEY([id_pedido]) REFERENCES [dbo].[Pedido]([id_pedido]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ComprobantePago_Pedido')
    CREATE UNIQUE INDEX [IX_ComprobantePago_Pedido] ON [dbo].[Comprobante_Pago_Pedido] ([id_pedido]);
");
}

static async Task SeedIdentityAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Administrador", "JefeLogistica", "Ventas", "Cliente" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminEmail = "admin@rubio.com";
    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            Nombres = "Administrador",
            Apellidos = "Rubio",
            NombreCompleto = "Administrador Rubio",
            Cargo = "Administrador del sistema",
            Area = "Logística y Ventas",
            Ciudad = "Trujillo",
            Pais = "Perú",
            Estado = "Activo",
            FechaRegistro = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, "123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Administrador");
        }
    }
    else
    {
        admin.Estado = "Activo";
        admin.EmailConfirmed = true;
        admin.LockoutEnd = null;
        admin.NombreCompleto ??= "Administrador Rubio";
        admin.Nombres ??= "Administrador";
        admin.Apellidos ??= "Rubio";
        await userManager.UpdateAsync(admin);

        if (!await userManager.IsInRoleAsync(admin, "Administrador"))
        {
            await userManager.AddToRoleAsync(admin, "Administrador");
        }
    }

    // Usuarios demo para evidenciar autorización por roles en el caso práctico.
    await CrearUsuarioDemoAsync(userManager, "logistica@rubio.com", "Jefe", "Logística", "Jefe de logística", "Operaciones", "JefeLogistica");
    await CrearUsuarioDemoAsync(userManager, "ventas@rubio.com", "Asesor", "Ventas", "Ejecutivo comercial", "Ventas", "Ventas");
    await CrearUsuarioDemoAsync(userManager, "cliente@rubio.com", "Cliente", "Corporativo", "Cliente B2B", "Compras", "Cliente");
}

static async Task CrearUsuarioDemoAsync(
    UserManager<ApplicationUser> userManager,
    string email,
    string nombres,
    string apellidos,
    string cargo,
    string area,
    string rol)
{
    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Nombres = nombres,
            Apellidos = apellidos,
            NombreCompleto = $"{nombres} {apellidos}",
            Cargo = cargo,
            Area = area,
            Ciudad = "Trujillo",
            Pais = "Perú",
            Estado = "Activo",
            FechaRegistro = DateTime.UtcNow,
            LockoutEnabled = true
        };

        var create = await userManager.CreateAsync(user, "123456");
        if (!create.Succeeded) return;
    }

    user.Estado = "Activo";
    user.EmailConfirmed = true;
    user.LockoutEnd = null;
    await userManager.UpdateAsync(user);

    if (!await userManager.IsInRoleAsync(user, rol))
    {
        await userManager.AddToRoleAsync(user, rol);
    }
}


static async Task SeedCatalogoCorporativoAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RubioDbContext>();

    async Task<Categoria> CategoriaAsync(string nombre)
    {
        var categoria = await db.Categorias.FirstOrDefaultAsync(c => c.Nombre == nombre);
        if (categoria == null)
        {
            categoria = new Categoria { Nombre = nombre };
            db.Categorias.Add(categoria);
            await db.SaveChangesAsync();
        }
        return categoria;
    }

    async Task<Proveedor> ProveedorAsync(string nombre, string ruc)
    {
        var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.NombreEmpresa == nombre || p.Ruc == ruc);
        if (proveedor == null)
        {
            proveedor = new Proveedor { NombreEmpresa = nombre, Ruc = ruc };
            db.Proveedores.Add(proveedor);
            await db.SaveChangesAsync();
        }
        return proveedor;
    }

    var catEpp = await CategoriaAsync("EPP");
    var catHerramientas = await CategoriaAsync("Herramientas y accesorios");
    var catSeguridad = await CategoriaAsync("Seguridad vial y señalización");
    var catMineria = await CategoriaAsync("Productos de minería");
    var catBio = await CategoriaAsync("Bioseguridad y desinfección");
    var catRopa = await CategoriaAsync("Ropa industrial y reflectivos");

    var provRubio = await ProveedorAsync("Corporación Rubio SRL", "20490011122");
    var provSeguridad = await ProveedorAsync("Seguridad Industrial S.A.", "20100011122");
    var provHerramientas = await ProveedorAsync("Herramientas Globales SAC", "20600022233");
    var provMineria = await ProveedorAsync("Equipos Mineros del Norte SAC", "20550033344");

    var productos = new[]
    {
        new { Nombre = "Casco de Seguridad Industrial", Descripcion = "Casco industrial tipo jockey con suspensión regulable para obra, almacén y planta.", Precio = 45.00m, Stock = 150, Categoria = catEpp, Proveedor = provSeguridad },
        new { Nombre = "Botas de Seguridad Punta de Acero", Descripcion = "Calzado industrial con puntera de acero, suela antideslizante y protección para trabajo pesado.", Precio = 120.00m, Stock = 80, Categoria = catEpp, Proveedor = provSeguridad },
        new { Nombre = "Respirador Media Cara", Descripcion = "Respirador reutilizable para protección respiratoria en ambientes con polvo y vapores.", Precio = 95.00m, Stock = 60, Categoria = catEpp, Proveedor = provSeguridad },
        new { Nombre = "Filtros para Respirador", Descripcion = "Par de filtros compatibles para respirador industrial en actividades de pintura y mantenimiento.", Precio = 38.00m, Stock = 110, Categoria = catEpp, Proveedor = provSeguridad },
        new { Nombre = "Guantes de Nitrilo Industrial", Descripcion = "Guantes resistentes para manipulación, limpieza, almacén y protección de manos.", Precio = 18.50m, Stock = 240, Categoria = catEpp, Proveedor = provRubio },
        new { Nombre = "Guantes de Cuero para Soldador", Descripcion = "Guantes largos de cuero para soldadura y trabajos de alta temperatura.", Precio = 32.00m, Stock = 95, Categoria = catEpp, Proveedor = provRubio },
        new { Nombre = "Lentes de Seguridad Transparentes", Descripcion = "Lentes de protección visual contra partículas y salpicaduras leves.", Precio = 12.00m, Stock = 180, Categoria = catEpp, Proveedor = provSeguridad },
        new { Nombre = "Careta Facial de Protección", Descripcion = "Protector facial con visor transparente para actividades industriales y sanitarias.", Precio = 28.00m, Stock = 70, Categoria = catEpp, Proveedor = provRubio },
        new { Nombre = "Orejeras de Protección Auditiva", Descripcion = "Orejeras para reducción de ruido en ambientes de planta, construcción y mantenimiento.", Precio = 42.00m, Stock = 55, Categoria = catEpp, Proveedor = provSeguridad },
        new { Nombre = "Chaleco Reflectivo de Seguridad", Descripcion = "Chaleco reflectivo de alta visibilidad para obra, transporte, almacén y seguridad vial.", Precio = 25.00m, Stock = 130, Categoria = catRopa, Proveedor = provRubio },
        new { Nombre = "Arnés de Seguridad de Cuerpo Completo", Descripcion = "Arnés para trabajos en altura con puntos de anclaje y correas regulables.", Precio = 185.00m, Stock = 35, Categoria = catEpp, Proveedor = provSeguridad },
        new { Nombre = "Cinta de Peligro Amarilla", Descripcion = "Cinta de señalización para delimitación de zonas de riesgo y seguridad vial.", Precio = 16.00m, Stock = 200, Categoria = catSeguridad, Proveedor = provRubio },
        new { Nombre = "Cono de Seguridad Vial", Descripcion = "Cono flexible con banda reflectiva para señalización temporal y control de tránsito.", Precio = 35.00m, Stock = 90, Categoria = catSeguridad, Proveedor = provRubio },
        new { Nombre = "Compresora de Aire 2 HP 100 Litros", Descripcion = "Equipo para soplado, pintado y trabajos de taller con estanque de alta capacidad.", Precio = 1290.00m, Stock = 8, Categoria = catHerramientas, Proveedor = provHerramientas },
        new { Nombre = "Gata de Botella 4 Toneladas", Descripcion = "Gata hidráulica para elevación en mantenimiento automotriz, obra y taller.", Precio = 115.00m, Stock = 25, Categoria = catHerramientas, Proveedor = provHerramientas },
        new { Nombre = "Wincha Industrial 5 Metros", Descripcion = "Cinta métrica resistente para medición en obra, almacén y mantenimiento.", Precio = 19.90m, Stock = 75, Categoria = catHerramientas, Proveedor = provHerramientas },
        new { Nombre = "Alicate Universal Profesional", Descripcion = "Herramienta manual para corte, sujeción y tareas eléctricas básicas.", Precio = 24.50m, Stock = 50, Categoria = catHerramientas, Proveedor = provHerramientas },
        new { Nombre = "Martillo de Uña Industrial", Descripcion = "Martillo de acero con mango ergonómico para mantenimiento y construcción.", Precio = 29.90m, Stock = 42, Categoria = catHerramientas, Proveedor = provHerramientas },
        new { Nombre = "Brocas para Concreto", Descripcion = "Juego de brocas para perforación en concreto y materiales de construcción.", Precio = 36.00m, Stock = 65, Categoria = catHerramientas, Proveedor = provHerramientas },
        new { Nombre = "Lámpara Minera Recargable", Descripcion = "Lámpara portátil para operaciones mineras, inspección y trabajo de baja iluminación.", Precio = 210.00m, Stock = 30, Categoria = catMineria, Proveedor = provMineria },
        new { Nombre = "Fumigador Manual 20 Litros", Descripcion = "Equipo de desinfección y fumigación para limpieza industrial y sanitaria.", Precio = 145.00m, Stock = 22, Categoria = catBio, Proveedor = provRubio }
    };

    foreach (var item in productos)
    {
        var producto = await db.Productos.FirstOrDefaultAsync(p => p.Nombre == item.Nombre ||
                                                                  (item.Nombre.Contains("Casco") && p.Nombre.Contains("Casco")) ||
                                                                  (item.Nombre.Contains("Botas") && p.Nombre.Contains("Botas")));
        if (producto == null)
        {
            producto = new Producto
            {
                Nombre = item.Nombre,
                Descripcion = item.Descripcion,
                PrecioUnitario = item.Precio,
                StockActual = item.Stock,
                IdCategoria = item.Categoria.IdCategoria,
                IdProveedor = item.Proveedor.IdProveedor
            };
            db.Productos.Add(producto);
        }
        else
        {
            producto.Descripcion = item.Descripcion;
            producto.PrecioUnitario = item.Precio;
            producto.IdCategoria = item.Categoria.IdCategoria;
            producto.IdProveedor = item.Proveedor.IdProveedor;
            if ((producto.StockActual ?? 0) <= 0)
            {
                producto.StockActual = item.Stock;
            }
        }
    }

    await db.SaveChangesAsync();
}
