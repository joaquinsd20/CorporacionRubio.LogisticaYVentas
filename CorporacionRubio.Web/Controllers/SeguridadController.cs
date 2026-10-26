using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CorporacionRubio.Data;
using CorporacionRubio.Data.Entities;
using CorporacionRubio.Web.Models;

namespace CorporacionRubio.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class SeguridadController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly RubioDbContext _context;

        public SeguridadController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            RubioDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => r.Name!)
                .ToListAsync();

            var users = await _userManager.Users
                .OrderBy(u => u.NombreCompleto)
                .ThenBy(u => u.Email)
                .ToListAsync();

            var filas = new List<UsuarioSeguridadItemViewModel>();
            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                filas.Add(new UsuarioSeguridadItemViewModel
                {
                    Id = user.Id,
                    NombreCompleto = user.NombreCompleto ?? $"{user.Nombres} {user.Apellidos}".Trim(),
                    Email = user.Email ?? string.Empty,
                    Cargo = user.Cargo ?? "No especificado",
                    Area = user.Area ?? "No especificada",
                    Estado = user.Estado,
                    RolPrincipal = userRoles.FirstOrDefault() ?? "Sin rol",
                    RolesTexto = userRoles.Any() ? string.Join(", ", userRoles) : "Sin rol asignado",
                    Bloqueado = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                    EmailConfirmado = user.EmailConfirmed,
                    IntentosFallidos = user.AccessFailedCount,
                    FechaRegistro = user.FechaRegistro
                });
            }

            return View(new SeguridadIndexViewModel
            {
                Roles = roles,
                Usuarios = filas
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearRol(string nuevoRol)
        {
            nuevoRol = (nuevoRol ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nuevoRol))
            {
                TempData["Error"] = "Ingrese el nombre del rol que desea crear.";
                return RedirectToAction(nameof(Index));
            }

            if (await _roleManager.RoleExistsAsync(nuevoRol))
            {
                TempData["Error"] = "El rol ya existe en ASP.NET Core Identity.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(nuevoRol));
            TempData[result.Succeeded ? "Mensaje" : "Error"] = result.Succeeded
                ? "Rol creado correctamente con RoleManager."
                : string.Join(" ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarRol(string userId, string rol)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "No se encontró el usuario seleccionado.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _roleManager.RoleExistsAsync(rol))
            {
                TempData["Error"] = "El rol seleccionado no existe.";
                return RedirectToAction(nameof(Index));
            }

            var actuales = await _userManager.GetRolesAsync(user);
            if (actuales.Any())
            {
                var remove = await _userManager.RemoveFromRolesAsync(user, actuales);
                if (!remove.Succeeded)
                {
                    TempData["Error"] = string.Join(" ", remove.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Index));
                }
            }

            var add = await _userManager.AddToRoleAsync(user, rol);
            if (!add.Succeeded)
            {
                TempData["Error"] = string.Join(" ", add.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            await SincronizarUsuarioOperativo(user, MapearRolOperativo(rol));
            TempData["Mensaje"] = "Rol actualizado correctamente con UserManager y RoleManager.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(string userId, string estado)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "No se encontró el usuario seleccionado.";
                return RedirectToAction(nameof(Index));
            }

            estado = string.Equals(estado, "Activo", StringComparison.OrdinalIgnoreCase) ? "Activo" : "Inactivo";
            if (string.Equals(user.Email, "admin@rubio.com", StringComparison.OrdinalIgnoreCase) && estado == "Inactivo")
            {
                TempData["Error"] = "No se recomienda desactivar la cuenta principal del administrador.";
                return RedirectToAction(nameof(Index));
            }

            user.Estado = estado;
            user.LockoutEnd = estado == "Activo" ? null : DateTimeOffset.UtcNow.AddYears(100);
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                if (estado == "Activo") await _userManager.ResetAccessFailedCountAsync(user);
                TempData["Mensaje"] = $"Cuenta marcada como {estado.ToLowerInvariant()}.";
            }
            else
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarCuenta(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "No se encontró el usuario seleccionado.";
                return RedirectToAction(nameof(Index));
            }

            user.EmailConfirmed = true;
            user.Estado = "Activo";
            user.LockoutEnd = null;
            var result = await _userManager.UpdateAsync(user);
            TempData[result.Succeeded ? "Mensaje" : "Error"] = result.Succeeded
                ? "Cuenta confirmada y habilitada correctamente."
                : string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerPassword(string userId, string nuevaPassword)
        {
            nuevaPassword = string.IsNullOrWhiteSpace(nuevaPassword) ? "123456" : nuevaPassword.Trim();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "No se encontró el usuario seleccionado.";
                return RedirectToAction(nameof(Index));
            }

            IdentityResult result;
            if (await _userManager.HasPasswordAsync(user))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                result = await _userManager.ResetPasswordAsync(user, token, nuevaPassword);
            }
            else
            {
                result = await _userManager.AddPasswordAsync(user, nuevaPassword);
            }

            if (result.Succeeded)
            {
                user.Estado = "Activo";
                user.EmailConfirmed = true;
                user.LockoutEnd = null;
                await _userManager.UpdateAsync(user);
                await _userManager.ResetAccessFailedCountAsync(user);
                TempData["Mensaje"] = $"Contraseña restablecida correctamente para {user.Email}.";
            }
            else
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerAdministrador()
        {
            await CrearRolSiNoExiste("Administrador");
            var admin = await _userManager.FindByEmailAsync("admin@rubio.com");
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin@rubio.com",
                    Email = "admin@rubio.com",
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
                var create = await _userManager.CreateAsync(admin, "123456");
                if (!create.Succeeded)
                {
                    TempData["Error"] = string.Join(" ", create.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(admin);
                var reset = await _userManager.ResetPasswordAsync(admin, token, "123456");
                if (!reset.Succeeded)
                {
                    TempData["Error"] = string.Join(" ", reset.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Index));
                }

                admin.Estado = "Activo";
                admin.EmailConfirmed = true;
                admin.LockoutEnd = null;
                await _userManager.UpdateAsync(admin);
                await _userManager.ResetAccessFailedCountAsync(admin);
            }

            if (!await _userManager.IsInRoleAsync(admin, "Administrador"))
            {
                await _userManager.AddToRoleAsync(admin, "Administrador");
            }

            await SincronizarUsuarioOperativo(admin, 1);
            TempData["Mensaje"] = "Cuenta administrador restablecida. Correo: admin@rubio.com · Clave: 123456.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CrearRolSiNoExiste(string rol)
        {
            if (!await _roleManager.RoleExistsAsync(rol))
            {
                await _roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        private async Task SincronizarUsuarioOperativo(ApplicationUser user, int? idRol)
        {
            var email = user.Email ?? string.Empty;
            var operativo = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (operativo == null)
            {
                _context.Usuarios.Add(new Usuario
                {
                    Nombre = user.NombreCompleto ?? user.UserName ?? email,
                    Email = email,
                    IdRol = idRol,
                    Contrasena = "IDENTITY"
                });
            }
            else
            {
                operativo.Nombre = user.NombreCompleto ?? operativo.Nombre;
                operativo.IdRol = idRol ?? operativo.IdRol;
                operativo.Contrasena = "IDENTITY";
            }

            await _context.SaveChangesAsync();
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
    }
}
