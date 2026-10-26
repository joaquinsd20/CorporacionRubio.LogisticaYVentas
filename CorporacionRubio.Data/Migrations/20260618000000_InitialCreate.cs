using CorporacionRubio.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorporacionRubio.Data.Migrations
{
    [DbContext(typeof(RubioDbContext))]
    [Migration("20260618000000_InitialCreate")]
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('[dbo].[Rol]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Rol](
        [id_rol] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [nombre_rol] [varchar](50) NOT NULL
    );
END;

IF OBJECT_ID('[dbo].[Usuario]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Usuario](
        [id_usuario] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [nombre] [varchar](100) NOT NULL,
        [email] [varchar](100) NULL UNIQUE,
        [id_rol] [int] NULL,
        [clave] [varchar](255) NULL,
        CONSTRAINT [FK_Usuario_Rol] FOREIGN KEY([id_rol]) REFERENCES [dbo].[Rol]([id_rol])
    );
END;

IF OBJECT_ID('[dbo].[Auditoria]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Auditoria](
        [id_evento] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [tabla_afectada] [varchar](50) NULL,
        [accion] [varchar](20) NULL,
        [fecha_hora] [datetime] NULL DEFAULT (GETDATE()),
        [id_usuario] [int] NULL,
        [valor_anterior] [varchar](200) NULL,
        [valor_nuevo] [varchar](200) NULL,
        CONSTRAINT [FK_Auditoria_Usuario] FOREIGN KEY([id_usuario]) REFERENCES [dbo].[Usuario]([id_usuario])
    );
END;

IF OBJECT_ID('[dbo].[Categoria]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Categoria](
        [id_categoria] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [nombre] [varchar](100) NOT NULL
    );
END;

IF OBJECT_ID('[dbo].[Proveedor]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Proveedor](
        [id_proveedor] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [nombre_empresa] [varchar](100) NOT NULL,
        [ruc] [varchar](15) NULL UNIQUE
    );
END;

IF OBJECT_ID('[dbo].[Producto]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Producto](
        [id_producto] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [nombre] [varchar](100) NOT NULL,
        [descripcion] [varchar](255) NULL,
        [precio_unitario] [decimal](10,2) NULL,
        [stock_actual] [int] NULL DEFAULT ((0)),
        [id_categoria] [int] NULL,
        [id_proveedor] [int] NULL,
        CONSTRAINT [FK_Producto_Categoria] FOREIGN KEY([id_categoria]) REFERENCES [dbo].[Categoria]([id_categoria]),
        CONSTRAINT [FK_Producto_Proveedor] FOREIGN KEY([id_proveedor]) REFERENCES [dbo].[Proveedor]([id_proveedor])
    );
END;

IF OBJECT_ID('[dbo].[Movimiento_Inventario]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Movimiento_Inventario](
        [id_movimiento] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [id_producto] [int] NOT NULL,
        [tipo_movimiento] [varchar](20) NOT NULL,
        [cantidad] [int] NOT NULL,
        [fecha_movimiento] [datetime] NULL DEFAULT (GETDATE()),
        [id_usuario] [int] NOT NULL,
        CONSTRAINT [FK_Movimiento_Producto] FOREIGN KEY([id_producto]) REFERENCES [dbo].[Producto]([id_producto]),
        CONSTRAINT [FK_Movimiento_Usuario] FOREIGN KEY([id_usuario]) REFERENCES [dbo].[Usuario]([id_usuario])
    );
END;

IF OBJECT_ID('[dbo].[Pedido]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Pedido](
        [id_pedido] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [fecha_pedido] [datetime] NULL DEFAULT (GETDATE()),
        [id_usuario] [int] NOT NULL,
        [estado] [varchar](50) NULL,
        CONSTRAINT [FK_Pedido_Usuario] FOREIGN KEY([id_usuario]) REFERENCES [dbo].[Usuario]([id_usuario])
    );
END;

IF OBJECT_ID('[dbo].[Detalle_Pedido]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Detalle_Pedido](
        [id_detalle] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [id_pedido] [int] NOT NULL,
        [id_producto] [int] NOT NULL,
        [cantidad] [int] NOT NULL,
        [subtotal] [decimal](10,2) NULL,
        CONSTRAINT [FK_Detalle_Pedido] FOREIGN KEY([id_pedido]) REFERENCES [dbo].[Pedido]([id_pedido]) ON DELETE CASCADE,
        CONSTRAINT [FK_Detalle_Producto] FOREIGN KEY([id_producto]) REFERENCES [dbo].[Producto]([id_producto])
    );
END;

IF OBJECT_ID('[dbo].[Comprobante_Pago_Pedido]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Comprobante_Pago_Pedido](
        [id_pago] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [id_pedido] [int] NOT NULL UNIQUE,
        [metodo_pago] [varchar](40) NOT NULL,
        [referencia_operacion] [varchar](80) NOT NULL,
        [tipo_comprobante] [varchar](40) NULL,
        [estado_pago] [varchar](40) NOT NULL DEFAULT('Pendiente de validación'),
        [observacion] [varchar](200) NULL,
        [fecha_registro] [datetime] NOT NULL DEFAULT(GETDATE()),
        [fecha_validacion] [datetime] NULL,
        [id_usuario_validacion] [int] NULL,
        CONSTRAINT [FK_ComprobantePago_Pedido] FOREIGN KEY([id_pedido]) REFERENCES [dbo].[Pedido]([id_pedido]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Rol] WHERE [nombre_rol] = 'Administrador de Sistema')
    INSERT INTO [dbo].[Rol] (nombre_rol) VALUES ('Administrador de Sistema'), ('Cliente B2B'), ('Jefe de Logística');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Usuario] WHERE [email] = 'admin@rubio.com')
    INSERT INTO [dbo].[Usuario] (nombre, email, id_rol, clave)
    VALUES ('Administrador Rubio', 'admin@rubio.com', 1, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categoria])
    INSERT INTO [dbo].[Categoria] (nombre) VALUES ('EPP'), ('Materiales Construcción');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Proveedor])
    INSERT INTO [dbo].[Proveedor] (nombre_empresa, ruc)
    VALUES ('Seguridad Industrial S.A.', '20100011122'),
           ('Construcciones Globales SAC', '20600022233');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Producto])
    INSERT INTO [dbo].[Producto] (nombre, descripcion, precio_unitario, stock_actual, id_categoria, id_proveedor)
    VALUES ('Casco de Seguridad', 'Casco industrial clase A, color amarillo', 45.00, 150, 1, 1),
           ('Botas de Seguridad', 'Botas con punta de acero, talla 40-44', 120.00, 80, 1, 1),
           ('Cemento Portland', 'Bolsa de 42.5kg', 28.50, 500, 2, 2),
           ('Ladrillo King Kong', 'Ladrillo para muro portante', 1.20, 2000, 2, 2);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Pedido])
    INSERT INTO [dbo].[Pedido] (fecha_pedido, id_usuario, estado)
    VALUES (GETDATE(), 1, 'Procesado');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Detalle_Pedido])
    INSERT INTO [dbo].[Detalle_Pedido] (id_pedido, id_producto, cantidad, subtotal)
    VALUES (1, 1, 5, 225.00),
           (1, 3, 10, 285.00);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Movimiento_Inventario])
    INSERT INTO [dbo].[Movimiento_Inventario] (id_producto, tipo_movimiento, cantidad, id_usuario)
    VALUES (1, 'INGRESO', 150, 1),
           (3, 'INGRESO', 500, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Auditoria])
    INSERT INTO [dbo].[Auditoria] (tabla_afectada, accion, id_usuario, valor_anterior, valor_nuevo)
    VALUES ('Producto', 'INSERT', 1, NULL, 'Nuevo producto Casco registrado'),
           ('Pedido', 'UPDATE', 1, 'Pendiente', 'Procesado');
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('[dbo].[Comprobante_Pago_Pedido]', 'U') IS NOT NULL DROP TABLE [dbo].[Comprobante_Pago_Pedido];
IF OBJECT_ID('[dbo].[Detalle_Pedido]', 'U') IS NOT NULL DROP TABLE [dbo].[Detalle_Pedido];
IF OBJECT_ID('[dbo].[Pedido]', 'U') IS NOT NULL DROP TABLE [dbo].[Pedido];
IF OBJECT_ID('[dbo].[Movimiento_Inventario]', 'U') IS NOT NULL DROP TABLE [dbo].[Movimiento_Inventario];
IF OBJECT_ID('[dbo].[Producto]', 'U') IS NOT NULL DROP TABLE [dbo].[Producto];
IF OBJECT_ID('[dbo].[Proveedor]', 'U') IS NOT NULL DROP TABLE [dbo].[Proveedor];
IF OBJECT_ID('[dbo].[Categoria]', 'U') IS NOT NULL DROP TABLE [dbo].[Categoria];
IF OBJECT_ID('[dbo].[Auditoria]', 'U') IS NOT NULL DROP TABLE [dbo].[Auditoria];
IF OBJECT_ID('[dbo].[Usuario]', 'U') IS NOT NULL DROP TABLE [dbo].[Usuario];
IF OBJECT_ID('[dbo].[Rol]', 'U') IS NOT NULL DROP TABLE [dbo].[Rol];
");
        }
    }
}
