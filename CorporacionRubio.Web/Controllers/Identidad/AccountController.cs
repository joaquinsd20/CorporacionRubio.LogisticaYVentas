using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CorporacionRubio.Data;
using CorporacionRubio.Data.Entities;
using CorporacionRubio.Web.Models;

namespace CorporacionRubio.Web.Controllers
{
    public class AccountController : Controller
    {
        private const string AdminEmail = "admin@rubio.com";
        private const string CodigoRestablecimientoAdmin = "RUBIO-2026";

        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly RubioDbContext _context;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            RubioDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Acceso()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null, string? perfil = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["PerfilAcceso"] = NormalizarPerfilAcceso(perfil);
            return View("Account", new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, string? perfil = null)
        {
            var perfilAcceso = NormalizarPerfilAcceso(perfil);
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["PerfilAcceso"] = perfilAcceso;

            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Complete los datos solicitados para iniciar sesión.";
                return View("Account", model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null || !string.Equals(user.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "La cuenta no existe o no se encuentra activa.";
                return View("Account", model);
            }

            var rolesUsuario = await _userManager.GetRolesAsync(user);
            if (!PerfilCoincideConRoles(perfilAcceso, rolesUsuario))
            {
                ViewBag.Error = perfilAcceso == "cliente"
                    ? "Esta cuenta no corresponde al portal cliente. Use una cuenta de cliente corporativo o seleccione acceso interno."
                    : "Esta cuenta pertenece al portal cliente. Seleccione Portal cliente para continuar.";
                return View("Account", model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.Recordarme, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                await CargarSesionOperativa(user);

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return await RedireccionarPorRolAsync(user);
            }

            if (result.IsLockedOut)
            {
                ViewBag.Error = "La cuenta fue bloqueada temporalmente por intentos fallidos.";
            }
            else
            {
                ViewBag.Error = "Correo o contraseña incorrectos.";
            }

            return View("Account", model);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);

            // En producción se enviaría por correo. En el proyecto académico se muestra el enlace para evidencia.
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var link = Url.Action("ResetPassword", "Account", new { email, token }, Request.Scheme);
                TempData["ResetLink"] = link;
            }

            TempData["Mensaje"] = "Si el correo está registrado, se generó un enlace de restablecimiento seguro con token de Identity.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(string? email = null, string? token = null)
        {
            return View(new ResetPasswordViewModel
            {
                Email = email ?? string.Empty,
                Token = token ?? string.Empty
            });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "No se encontró una cuenta asociada al correo ingresado.");
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                user.Estado = "Activo";
                user.EmailConfirmed = true;
                user.LockoutEnd = null;
                await _userManager.UpdateAsync(user);
                await _userManager.ResetAccessFailedCountAsync(user);
                await SincronizarUsuarioOperativo(user, EsAdministrador(user.Email) ? 1 : null);

                TempData["MensajeLogin"] = "Contraseña actualizada correctamente. Ya puede iniciar sesión.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult RestablecerAdministrador()
        {
            return View(new RestablecerAdminViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerAdministrador(RestablecerAdminViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (!string.Equals(model.Codigo?.Trim(), CodigoRestablecimientoAdmin, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.Codigo), "Código de restablecimiento incorrecto.");
                return View(model);
            }

            var result = await RestablecerCuentaAdministradorAsync(model.NuevaPassword.Trim());
            if (result.Succeeded)
            {
                TempData["MensajeLogin"] = "Cuenta administradora restablecida correctamente. Use admin@rubio.com con la nueva contraseña.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegistroViewModel());
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistroViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync(model.Rol))
            {
                ModelState.AddModelError(nameof(model.Rol), "El perfil seleccionado no existe.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                EmailConfirmed = true,
                Nombres = model.Nombres.Trim(),
                Apellidos = model.Apellidos.Trim(),
                NombreCompleto = $"{model.Nombres.Trim()} {model.Apellidos.Trim()}",
                Cargo = model.Cargo?.Trim(),
                Area = model.Area?.Trim(),
                Estado = "Activo",
                FechaRegistro = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Rol);
                await RegistrarUsuarioOperativo(model, user.Email!);
                TempData["Mensaje"] = "Usuario creado correctamente con ASP.NET Core Identity.";
                return RedirectToAction("Index", "Seguridad");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Se mantiene una ruta GET para compatibilidad con botones antiguos del layout.
        [HttpGet]
        public async Task<IActionResult> LogoutGet()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        private async Task<IActionResult> RedireccionarPorRolAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Administrador"))
            {
                return RedirectToAction("Index", "Administracion");
            }

            if (roles.Contains("JefeLogistica"))
            {
                return RedirectToAction("Index", "PanelLogistico");
            }

            if (roles.Contains("Ventas"))
            {
                return RedirectToAction("Index", "PanelVentas");
            }

            if (roles.Contains("Cliente"))
            {
                return RedirectToAction("Index", "PortalCliente");
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task CargarSesionOperativa(ApplicationUser identityUser)
        {
            var email = identityUser.Email ?? string.Empty;
            var usuarioOperativo = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (usuarioOperativo == null)
            {
                usuarioOperativo = new Usuario
                {
                    Nombre = identityUser.NombreCompleto ?? identityUser.UserName ?? "Usuario",
                    Email = email,
                    IdRol = await ObtenerIdRolOperativo(identityUser),
                    Contrasena = "IDENTITY"
                };

                _context.Usuarios.Add(usuarioOperativo);
                await _context.SaveChangesAsync();
            }

            var roles = await _userManager.GetRolesAsync(identityUser);
            HttpContext.Session.SetString("UsuarioId", usuarioOperativo.IdUsuario.ToString());
            HttpContext.Session.SetString("UserName", identityUser.NombreCompleto ?? usuarioOperativo.Nombre ?? identityUser.Email ?? "Usuario");
            HttpContext.Session.SetString("UserRol", roles.FirstOrDefault() ?? "Usuario");
        }

        private async Task RegistrarUsuarioOperativo(RegistroViewModel model, string email)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Email == email)) return;

            _context.Usuarios.Add(new Usuario
            {
                Nombre = $"{model.Nombres.Trim()} {model.Apellidos.Trim()}",
                Email = email,
                IdRol = MapearRolOperativo(model.Rol),
                Contrasena = "IDENTITY"
            });

            await _context.SaveChangesAsync();
        }

        private async Task SincronizarUsuarioOperativo(ApplicationUser user, int? idRolForzado = null)
        {
            var email = user.Email ?? string.Empty;
            var operativo = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (operativo == null)
            {
                operativo = new Usuario
                {
                    Nombre = user.NombreCompleto ?? user.UserName ?? email,
                    Email = email,
                    IdRol = idRolForzado ?? await ObtenerIdRolOperativo(user),
                    Contrasena = "IDENTITY"
                };
                _context.Usuarios.Add(operativo);
            }
            else
            {
                operativo.Nombre = user.NombreCompleto ?? operativo.Nombre;
                operativo.IdRol = idRolForzado ?? await ObtenerIdRolOperativo(user);
                operativo.Contrasena = "IDENTITY";
            }

            await _context.SaveChangesAsync();
        }

        private async Task<int?> ObtenerIdRolOperativo(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return MapearRolOperativo(roles.FirstOrDefault() ?? "Cliente");
        }

        private static int? MapearRolOperativo(string rol)
        {
            return rol switch
            {
                "Administrador" => 1,
                "Cliente" => 2,
                "JefeLogistica" => 3,
                "Ventas" => 2,
                _ => 2
            };
        }

        private async Task<IdentityResult> RestablecerCuentaAdministradorAsync(string nuevaPassword)
        {
            await CrearRolSiNoExiste("Administrador");
            await CrearRolSiNoExiste("JefeLogistica");
            await CrearRolSiNoExiste("Ventas");
            await CrearRolSiNoExiste("Cliente");

            var admin = await _userManager.FindByEmailAsync(AdminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    EmailConfirmed = true,
                    Nombres = "Administrador",
                    Apellidos = "Rubio",
                    NombreCompleto = "Administrador Rubio",
                    Cargo = "Administrador del sistema",
                    Area = "Logística y Ventas",
                    Ciudad = "Trujillo",
                    Pais = "Perú",
                    Estado = "Activo",
                    FechaRegistro = DateTime.UtcNow,
                    LockoutEnabled = true
                };

                var creado = await _userManager.CreateAsync(admin, nuevaPassword);
                if (!creado.Succeeded) return creado;
            }
            else
            {
                admin.UserName = AdminEmail;
                admin.Email = AdminEmail;
                admin.EmailConfirmed = true;
                admin.Estado = "Activo";
                admin.LockoutEnd = null;
                admin.NombreCompleto ??= "Administrador Rubio";
                admin.Nombres ??= "Administrador";
                admin.Apellidos ??= "Rubio";
                admin.Cargo ??= "Administrador del sistema";
                admin.Area ??= "Logística y Ventas";

                var actualizado = await _userManager.UpdateAsync(admin);
                if (!actualizado.Succeeded) return actualizado;

                var tienePassword = await _userManager.HasPasswordAsync(admin);
                if (tienePassword)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(admin);
                    var reset = await _userManager.ResetPasswordAsync(admin, token, nuevaPassword);
                    if (!reset.Succeeded) return reset;
                }
                else
                {
                    var addPassword = await _userManager.AddPasswordAsync(admin, nuevaPassword);
                    if (!addPassword.Succeeded) return addPassword;
                }
            }

            if (!await _userManager.IsInRoleAsync(admin, "Administrador"))
            {
                var addRole = await _userManager.AddToRoleAsync(admin, "Administrador");
                if (!addRole.Succeeded) return addRole;
            }

            await _userManager.ResetAccessFailedCountAsync(admin);
            await SincronizarUsuarioOperativo(admin, 1);
            return IdentityResult.Success;
        }

        private async Task CrearRolSiNoExiste(string rol)
        {
            if (!await _roleManager.RoleExistsAsync(rol))
            {
                await _roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        private static string? NormalizarPerfilAcceso(string? perfil)
        {
            if (string.IsNullOrWhiteSpace(perfil)) return null;
            perfil = perfil.Trim().ToLowerInvariant();
            return perfil switch
            {
                "cliente" => "cliente",
                "interno" => "interno",
                "admin" => "interno",
                "administrativo" => "interno",
                _ => null
            };
        }

        private static bool PerfilCoincideConRoles(string? perfilAcceso, IEnumerable<string> roles)
        {
            if (string.IsNullOrWhiteSpace(perfilAcceso)) return true;
            var esCliente = roles.Contains("Cliente");
            return perfilAcceso == "cliente" ? esCliente : !esCliente;
        }

        private static bool EsAdministrador(string? email) =>
            string.Equals(email, AdminEmail, StringComparison.OrdinalIgnoreCase);
    }
}
