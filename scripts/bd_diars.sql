/* 1. Preparar el entorno */
USE [master];
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'GestionLogisticaRubio')
BEGIN
    ALTER DATABASE [GestionLogisticaRubio] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [GestionLogisticaRubio];
END
GO

CREATE DATABASE [GestionLogisticaRubio];
GO

USE [GestionLogisticaRubio];
GO

/* 2. Creación de Tablas */

CREATE TABLE [dbo].[Rol](
    [id_rol] [int] IDENTITY(1,1) PRIMARY KEY,
    [nombre_rol] [varchar](50) NOT NULL
);

CREATE TABLE [dbo].[Usuario](
    [id_usuario] [int] IDENTITY(1,1) PRIMARY KEY,
    [nombre] [varchar](100) NOT NULL,
    [email] [varchar](100) UNIQUE,
    [id_rol] [int] REFERENCES [dbo].[Rol]([id_rol]),
    [clave] [varchar](255)
);

CREATE TABLE [dbo].[Auditoria](
    [id_evento] [int] IDENTITY(1,1) PRIMARY KEY,
    [tabla_afectada] [varchar](50),
    [accion] [varchar](20),
    [fecha_hora] [datetime] DEFAULT (getdate()),
    [id_usuario] [int] REFERENCES [dbo].[Usuario]([id_usuario]),
    [valor_anterior] [varchar](200),
    [valor_nuevo] [varchar](200)
);

CREATE TABLE [dbo].[Categoria](
    [id_categoria] [int] IDENTITY(1,1) PRIMARY KEY,
    [nombre] [varchar](100) NOT NULL
);

CREATE TABLE [dbo].[Proveedor](
    [id_proveedor] [int] IDENTITY(1,1) PRIMARY KEY,
    [nombre_empresa] [varchar](100) NOT NULL,
    [ruc] [varchar](15) UNIQUE
);

CREATE TABLE [dbo].[Producto](
    [id_producto] [int] IDENTITY(1,1) PRIMARY KEY,
    [nombre] [varchar](100) NOT NULL,
    [descripcion] [varchar](255),
    [precio_unitario] [decimal](10, 2),
    [stock_actual] [int] DEFAULT ((0)),
    [id_categoria] [int] REFERENCES [dbo].[Categoria]([id_categoria]),
    [id_proveedor] [int] REFERENCES [dbo].[Proveedor]([id_proveedor])
);

CREATE TABLE [dbo].[Movimiento_Inventario](
    [id_movimiento] [int] IDENTITY(1,1) PRIMARY KEY,
    [id_producto] [int] REFERENCES [dbo].[Producto]([id_producto]),
    [tipo_movimiento] [varchar](20),
    [cantidad] [int],
    [fecha_movimiento] [datetime] DEFAULT (getdate()),
    [id_usuario] [int] REFERENCES [dbo].[Usuario]([id_usuario])
);

CREATE TABLE [dbo].[Pedido](
    [id_pedido] [int] IDENTITY(1,1) PRIMARY KEY,
    [fecha_pedido] [datetime] DEFAULT (getdate()),
    [id_usuario] [int] REFERENCES [dbo].[Usuario]([id_usuario]),
    [estado] [varchar](50)
);

CREATE TABLE [dbo].[Detalle_Pedido](
    [id_detalle] [int] IDENTITY(1,1) PRIMARY KEY,
    [id_pedido] [int] REFERENCES [dbo].[Pedido]([id_pedido]) ON DELETE CASCADE,
    [id_producto] [int] REFERENCES [dbo].[Producto]([id_producto]),
    [cantidad] [int] NOT NULL,
    [subtotal] [decimal](10, 2)
);
GO

/* 3. Inserción de Datos Iniciales */

-- Insertar Roles
INSERT INTO [dbo].[Rol] (nombre_rol) VALUES ('Administrador de Sistema'), ('Cliente B2B'), ('Jefe de Logística');

-- Insertar Usuario Admin (Clave: 123456)
INSERT INTO [dbo].[Usuario] (nombre, email, id_rol, clave) 
VALUES ('Administrador Rubio', 'admin@rubio.com', 1, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92'); -- SHA256 de 123456

-- Insertar Categoría base
INSERT INTO [dbo].[Categoria] (nombre) VALUES ('EPP'), ('Materiales Construcción');
-- 1. Insertar Proveedores
INSERT INTO [dbo].[Proveedor] (nombre_empresa, ruc) 
VALUES ('Seguridad Industrial S.A.', '20100011122'),
       ('Construcciones Globales SAC', '20600022233');

-- 2. Insertar Productos
INSERT INTO [dbo].[Producto] (nombre, descripcion, precio_unitario, stock_actual, id_categoria, id_proveedor)
VALUES ('Casco de Seguridad', 'Casco industrial clase A, color amarillo', 45.00, 150, 1, 1),
       ('Botas de Seguridad', 'Botas con punta de acero, talla 40-44', 120.00, 80, 1, 1),
       ('Cemento Portland', 'Bolsa de 42.5kg', 28.50, 500, 2, 2),
       ('Ladrillo King Kong', 'Ladrillo para muro portante', 1.20, 2000, 2, 2);

-- 3. Insertar Pedido de prueba
INSERT INTO [dbo].[Pedido] (fecha_pedido, id_usuario, estado)
VALUES (GETDATE(), 1, 'Procesado');

-- 4. Insertar Detalle del Pedido
INSERT INTO [dbo].[Detalle_Pedido] (id_pedido, id_producto, cantidad, subtotal)
VALUES (1, 1, 5, 225.00),
       (1, 3, 10, 285.00);

-- 5. Insertar Movimientos de inventario
INSERT INTO [dbo].[Movimiento_Inventario] (id_producto, tipo_movimiento, cantidad, id_usuario)
VALUES (1, 'INGRESO', 150, 1),
       (3, 'INGRESO', 500, 1);

-- 6. Insertar registros de Auditoría
INSERT INTO [dbo].[Auditoria] (tabla_afectada, accion, id_usuario, valor_anterior, valor_nuevo)
VALUES ('Producto', 'INSERT', 1, NULL, 'Nuevo producto Casco registrado'),
       ('Pedido', 'UPDATE', 1, 'Pendiente', 'Procesado');

GO
PRINT 'Base de datos, tablas y datos iniciales cargados correctamente.';

select * from Usuario