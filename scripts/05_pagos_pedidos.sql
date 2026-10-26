-- Tabla de validación de pagos por pedido.
-- Ejecutar si la base ya existe y no se quiere recrear todo el esquema.
IF OBJECT_ID('[dbo].[Comprobante_Pago_Pedido]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Comprobante_Pago_Pedido](
        [id_pago] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [id_pedido] [int] NOT NULL,
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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ComprobantePago_Pedido')
    CREATE UNIQUE INDEX [IX_ComprobantePago_Pedido] ON [dbo].[Comprobante_Pago_Pedido] ([id_pedido]);
