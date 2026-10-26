-- Catálogo corporativo conectado al inventario real.
-- Este script es opcional: el proyecto también siembra estos productos automáticamente al iniciar.
-- Sirve como respaldo si el docente desea revisar los productos usados en landing, ventas, logística y cliente.

IF NOT EXISTS (SELECT 1 FROM Categoria WHERE nombre = 'EPP') INSERT INTO Categoria(nombre) VALUES ('EPP');
IF NOT EXISTS (SELECT 1 FROM Categoria WHERE nombre = 'Herramientas y accesorios') INSERT INTO Categoria(nombre) VALUES ('Herramientas y accesorios');
IF NOT EXISTS (SELECT 1 FROM Categoria WHERE nombre = 'Seguridad vial y señalización') INSERT INTO Categoria(nombre) VALUES ('Seguridad vial y señalización');
IF NOT EXISTS (SELECT 1 FROM Categoria WHERE nombre = 'Productos de minería') INSERT INTO Categoria(nombre) VALUES ('Productos de minería');
IF NOT EXISTS (SELECT 1 FROM Categoria WHERE nombre = 'Bioseguridad y desinfección') INSERT INTO Categoria(nombre) VALUES ('Bioseguridad y desinfección');
IF NOT EXISTS (SELECT 1 FROM Categoria WHERE nombre = 'Ropa industrial y reflectivos') INSERT INTO Categoria(nombre) VALUES ('Ropa industrial y reflectivos');

IF NOT EXISTS (SELECT 1 FROM Proveedor WHERE nombre_empresa = 'Corporación Rubio SRL') INSERT INTO Proveedor(nombre_empresa, ruc) VALUES ('Corporación Rubio SRL', '20490011122');
IF NOT EXISTS (SELECT 1 FROM Proveedor WHERE nombre_empresa = 'Seguridad Industrial S.A.') INSERT INTO Proveedor(nombre_empresa, ruc) VALUES ('Seguridad Industrial S.A.', '20100011122');
IF NOT EXISTS (SELECT 1 FROM Proveedor WHERE nombre_empresa = 'Herramientas Globales SAC') INSERT INTO Proveedor(nombre_empresa, ruc) VALUES ('Herramientas Globales SAC', '20600022233');
IF NOT EXISTS (SELECT 1 FROM Proveedor WHERE nombre_empresa = 'Equipos Mineros del Norte SAC') INSERT INTO Proveedor(nombre_empresa, ruc) VALUES ('Equipos Mineros del Norte SAC', '20550033344');

DECLARE @EPP INT = (SELECT id_categoria FROM Categoria WHERE nombre = 'EPP');
DECLARE @HER INT = (SELECT id_categoria FROM Categoria WHERE nombre = 'Herramientas y accesorios');
DECLARE @SEG INT = (SELECT id_categoria FROM Categoria WHERE nombre = 'Seguridad vial y señalización');
DECLARE @MIN INT = (SELECT id_categoria FROM Categoria WHERE nombre = 'Productos de minería');
DECLARE @BIO INT = (SELECT id_categoria FROM Categoria WHERE nombre = 'Bioseguridad y desinfección');
DECLARE @ROPA INT = (SELECT id_categoria FROM Categoria WHERE nombre = 'Ropa industrial y reflectivos');
DECLARE @RUBIO INT = (SELECT id_proveedor FROM Proveedor WHERE nombre_empresa = 'Corporación Rubio SRL');
DECLARE @PROVSEG INT = (SELECT id_proveedor FROM Proveedor WHERE nombre_empresa = 'Seguridad Industrial S.A.');
DECLARE @PROVHER INT = (SELECT id_proveedor FROM Proveedor WHERE nombre_empresa = 'Herramientas Globales SAC');
DECLARE @PROVMIN INT = (SELECT id_proveedor FROM Proveedor WHERE nombre_empresa = 'Equipos Mineros del Norte SAC');

IF NOT EXISTS (SELECT 1 FROM Producto WHERE nombre = 'Respirador Media Cara')
INSERT INTO Producto(nombre, descripcion, precio_unitario, stock_actual, id_categoria, id_proveedor)
VALUES ('Respirador Media Cara', 'Respirador reutilizable para protección respiratoria en ambientes con polvo y vapores.', 95.00, 60, @EPP, @PROVSEG);

IF NOT EXISTS (SELECT 1 FROM Producto WHERE nombre = 'Guantes de Nitrilo Industrial')
INSERT INTO Producto(nombre, descripcion, precio_unitario, stock_actual, id_categoria, id_proveedor)
VALUES ('Guantes de Nitrilo Industrial', 'Guantes resistentes para manipulación, limpieza, almacén y protección de manos.', 18.50, 240, @EPP, @RUBIO);

IF NOT EXISTS (SELECT 1 FROM Producto WHERE nombre = 'Lentes de Seguridad Transparentes')
INSERT INTO Producto(nombre, descripcion, precio_unitario, stock_actual, id_categoria, id_proveedor)
VALUES ('Lentes de Seguridad Transparentes', 'Lentes de protección visual contra partículas y salpicaduras leves.', 12.00, 180, @EPP, @PROVSEG);

IF NOT EXISTS (SELECT 1 FROM Producto WHERE nombre = 'Chaleco Reflectivo de Seguridad')
INSERT INTO Producto(nombre, descripcion, precio_unitario, stock_actual, id_categoria, id_proveedor)
VALUES ('Chaleco Reflectivo de Seguridad', 'Chaleco reflectivo de alta visibilidad para obra, transporte, almacén y seguridad vial.', 25.00, 130, @ROPA, @RUBIO);

IF NOT EXISTS (SELECT 1 FROM Producto WHERE nombre = 'Compresora de Aire 2 HP 100 Litros')
INSERT INTO Producto(nombre, descripcion, precio_unitario, stock_actual, id_categoria, id_proveedor)
VALUES ('Compresora de Aire 2 HP 100 Litros', 'Equipo para soplado, pintado y trabajos de taller con estanque de alta capacidad.', 1290.00, 8, @HER, @PROVHER);

IF NOT EXISTS (SELECT 1 FROM Producto WHERE nombre = 'Gata de Botella 4 Toneladas')
INSERT INTO Producto(nombre, descripcion, precio_unitario, stock_actual, id_categoria, id_proveedor)
VALUES ('Gata de Botella 4 Toneladas', 'Gata hidráulica para elevación en mantenimiento automotriz, obra y taller.', 115.00, 25, @HER, @PROVHER);

IF NOT EXISTS (SELECT 1 FROM Producto WHERE nombre = 'Cono de Seguridad Vial')
INSERT INTO Producto(nombre, descripcion, precio_unitario, stock_actual, id_categoria, id_proveedor)
VALUES ('Cono de Seguridad Vial', 'Cono flexible con banda reflectiva para señalización temporal y control de tránsito.', 35.00, 90, @SEG, @RUBIO);

IF NOT EXISTS (SELECT 1 FROM Producto WHERE nombre = 'Lámpara Minera Recargable')
INSERT INTO Producto(nombre, descripcion, precio_unitario, stock_actual, id_categoria, id_proveedor)
VALUES ('Lámpara Minera Recargable', 'Lámpara portátil para operaciones mineras, inspección y trabajo de baja iluminación.', 210.00, 30, @MIN, @PROVMIN);
