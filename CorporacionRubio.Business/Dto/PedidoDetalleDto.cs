namespace CorporacionRubio.Business.Dto
{
    public class PedidoDetalleDto
    {
        public int IdDetalle { get; set; }
        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }
    }
}
