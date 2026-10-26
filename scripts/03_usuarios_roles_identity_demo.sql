/*
Script informativo de respaldo.
La aplicación crea estos registros automáticamente mediante Program.cs usando UserManager y RoleManager.
No es obligatorio ejecutarlo si la app inicia correctamente.
*/

SELECT 'Roles esperados' AS Seccion, Name AS Valor FROM AspNetRoles;
SELECT 'Usuarios esperados' AS Seccion, Email AS Valor FROM AspNetUsers;

/* Usuarios demo generados por la app:
admin@rubio.com      / 123456 / Administrador
logistica@rubio.com  / 123456 / JefeLogistica
ventas@rubio.com     / 123456 / Ventas
cliente@rubio.com    / 123456 / Cliente
*/
