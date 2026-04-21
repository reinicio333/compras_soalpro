using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using orion.Controllers;

namespace orion.Servicios
{
    public class ReporteOrdenCompraExcelService
    {
        public byte[] GenerarExcelOrdenCompra(OrdenCompraDto datos)
        {
            IWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("OrdenCompra");

            var estiloTitulo = workbook.CreateCellStyle();
            var fuenteTitulo = workbook.CreateFont();
            fuenteTitulo.IsBold = true;
            fuenteTitulo.FontHeightInPoints = 14;
            estiloTitulo.SetFont(fuenteTitulo);
            estiloTitulo.Alignment = HorizontalAlignment.Center;

            var estiloHeader = workbook.CreateCellStyle();
            var fuenteHeader = workbook.CreateFont();
            fuenteHeader.IsBold = true;
            estiloHeader.SetFont(fuenteHeader);
            estiloHeader.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            estiloHeader.FillPattern = FillPattern.SolidForeground;
            estiloHeader.BorderBottom = BorderStyle.Thin;
            estiloHeader.BorderTop = BorderStyle.Thin;
            estiloHeader.BorderLeft = BorderStyle.Thin;
            estiloHeader.BorderRight = BorderStyle.Thin;

            var estiloMoneda = workbook.CreateCellStyle();
            estiloMoneda.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");

            var rowIndex = 0;
            var rowTitulo = sheet.CreateRow(rowIndex++);
            rowTitulo.CreateCell(0).SetCellValue($"ORDEN DE COMPRA #{datos.Id}");
            rowTitulo.GetCell(0).CellStyle = estiloTitulo;
            sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, 10));

            rowIndex++;

            void AddDato(string etiqueta, string? valor)
            {
                var row = sheet.CreateRow(rowIndex++);
                row.CreateCell(0).SetCellValue(etiqueta);
                row.CreateCell(1).SetCellValue(valor ?? string.Empty);
            }

            AddDato("Fecha", datos.Fecha);
            AddDato("Proveedor", datos.Proveedor);
            AddDato("Solicitante", datos.Solicitante);
            AddDato("Referencia", datos.Referencia);
            AddDato("Tipo Cambio", datos.Tc);
            AddDato("Observación", datos.Cabecera?.Observacion);

            rowIndex++;
            var header = sheet.CreateRow(rowIndex++);
            string[] columnas = { "Código", "Nro", "Descripción", "F. Entrega", "Características", "Unidad", "Cantidad", "Precio", "Total" };
            for (var i = 0; i < columnas.Length; i++)
            {
                var cell = header.CreateCell(i);
                cell.SetCellValue(columnas[i]);
                cell.CellStyle = estiloHeader;
            }

            decimal totalGeneral = 0;
            foreach (var producto in datos.Productos ?? new List<ProductoOrdenDto>())
            {
                var row = sheet.CreateRow(rowIndex++);
                row.CreateCell(0).SetCellValue(producto.Codigo ?? "");
                row.CreateCell(1).SetCellValue(producto.Nro ?? "");
                row.CreateCell(2).SetCellValue(producto.Descripcion ?? "");
                row.CreateCell(3).SetCellValue(producto.FechaEntrega ?? "");
                row.CreateCell(4).SetCellValue(producto.Caracteristicas ?? "");
                row.CreateCell(5).SetCellValue(producto.Unidad ?? "");
                row.CreateCell(6).SetCellValue(Convert.ToDouble(producto.Cantidad));

                var precio = Convert.ToDouble(producto.Precio);
                row.CreateCell(7).SetCellValue(precio);
                row.GetCell(7).CellStyle = estiloMoneda;

                var total = precio * Convert.ToDouble(producto.Cantidad);
                row.CreateCell(8).SetCellValue(total);
                row.GetCell(8).CellStyle = estiloMoneda;

                totalGeneral += (producto.Precio) * (producto.Cantidad);
            }

            var rowTotal = sheet.CreateRow(rowIndex);
            rowTotal.CreateCell(7).SetCellValue("TOTAL");
            rowTotal.GetCell(7).CellStyle = estiloHeader;
            rowTotal.CreateCell(8).SetCellValue(Convert.ToDouble(totalGeneral));
            rowTotal.GetCell(8).CellStyle = estiloMoneda;

            for (var i = 0; i <= 8; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            using var stream = new MemoryStream();
            workbook.Write(stream);
            return stream.ToArray();
        }

        public byte[] GenerarExcelReporteGeneral(List<ReporteGeneralOrdenDetalleDto> ordenes)
        {
            IWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("ReporteGeneral");

            var estiloHeader = workbook.CreateCellStyle();
            var fuenteHeader = workbook.CreateFont();
            fuenteHeader.IsBold = true;
            estiloHeader.SetFont(fuenteHeader);
            estiloHeader.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            estiloHeader.FillPattern = FillPattern.SolidForeground;

            var rowHeader = sheet.CreateRow(0);
            string[] columnas =
            {
                "ID",
                "Fecha",
                "Id Solicitud Precio",
                "Id Solicitud",
                "Solicitudes Vinculadas",
                "Referencias Solicitudes",
                "Solicitantes Solicitudes",
                "Proveedor Item",
                "Nombre Item",
                "Codigo Item",
                "Cantidad Item",
                "Precio Item",
                "Referencia OC",
                "Solicitante OC",
                "Proveedor",
                "Tipo",
                "Estado",
                "Id Estado",
                "Fecha Estado",
                "Tipo Cambio",
                "Observacion",
                "Forma Pago",
                "Medio Transporte",
                "Responsable Recepcion",
                "Fecha Entrega",
                "Lugar Entrega",
                "Fecha Anticipo",
                "Monto Anticipo",
                "Fecha Pago Final",
                "Monto Pago Final",
                "Banco",
                "Cuenta",
                "Nombre Cuenta Bancaria",
                "Codigo Swift",
                "Incoterm",
                "Razon Social",
                "NIT",
                "Telefono",
                "Nombre Contacto",
                "Aprobador",
                "Id Area Correspondencia",
                "Corresponde ASC",
                "Recepcion Tipo",
                "Observacion Recepcion",
                "Adjuntos (JSON)"
            };
            for (var i = 0; i < columnas.Length; i++)
            {
                var cell = rowHeader.CreateCell(i);
                cell.SetCellValue(columnas[i]);
                cell.CellStyle = estiloHeader;
            }

            var rowIndex = 1;
            foreach (var orden in ordenes)
            {
                var row = sheet.CreateRow(rowIndex++);
                row.CreateCell(0).SetCellValue(orden.Id);
                row.CreateCell(1).SetCellValue(orden.Fecha?.ToString("dd/MM/yyyy") ?? "");
                row.CreateCell(2).SetCellValue(orden.IdSolicitudPrecio?.ToString() ?? "");
                row.CreateCell(3).SetCellValue(orden.IdSolicitud?.ToString() ?? "");
                row.CreateCell(4).SetCellValue(orden.SolicitudesVinculadas ?? "");
                row.CreateCell(5).SetCellValue(orden.ReferenciasSolicitudesVinculadas ?? "");
                row.CreateCell(6).SetCellValue(orden.SolicitantesSolicitudesVinculadas ?? "");
                row.CreateCell(7).SetCellValue(orden.ProveedorItem ?? "");
                row.CreateCell(8).SetCellValue(orden.NombreItem ?? "");
                row.CreateCell(9).SetCellValue(orden.CodigoItem ?? "");
                row.CreateCell(10).SetCellValue(orden.CantidadItem?.ToString() ?? "");
                row.CreateCell(11).SetCellValue(orden.PrecioItem?.ToString() ?? "");
                row.CreateCell(12).SetCellValue(orden.Referencia ?? "");
                row.CreateCell(13).SetCellValue(orden.Solicitante ?? "");
                row.CreateCell(14).SetCellValue(orden.Proveedor ?? "");
                row.CreateCell(15).SetCellValue(orden.EsImportacion ? "IMPORTACION" : "NACIONAL");
                row.CreateCell(16).SetCellValue(orden.Estado ?? "Sin Estado");
                row.CreateCell(17).SetCellValue(orden.IdEstado?.ToString() ?? "");
                row.CreateCell(18).SetCellValue(orden.FechaEstado?.ToString("dd/MM/yyyy HH:mm") ?? "");
                row.CreateCell(19).SetCellValue(orden.TipoCambio ?? "");
                row.CreateCell(20).SetCellValue(orden.Observacion ?? "");
                row.CreateCell(21).SetCellValue(orden.FormaPago ?? "");
                row.CreateCell(22).SetCellValue(orden.MedioTransporte ?? "");
                row.CreateCell(23).SetCellValue(orden.ResponsableRecepcion ?? "");
                row.CreateCell(24).SetCellValue(orden.FechaEntrega?.ToString("dd/MM/yyyy") ?? "");
                row.CreateCell(25).SetCellValue(orden.LugarEntrega ?? "");
                row.CreateCell(26).SetCellValue(orden.FechaAnticipo?.ToString("dd/MM/yyyy") ?? "");
                row.CreateCell(27).SetCellValue(orden.MontoAnticipo?.ToString() ?? "");
                row.CreateCell(28).SetCellValue(orden.FechaPagoFinal?.ToString("dd/MM/yyyy") ?? "");
                row.CreateCell(29).SetCellValue(orden.MontoPagoFinal?.ToString() ?? "");
                row.CreateCell(30).SetCellValue(orden.Banco ?? "");
                row.CreateCell(31).SetCellValue(orden.Cuenta ?? "");
                row.CreateCell(32).SetCellValue(orden.NombreCuentaBancaria ?? "");
                row.CreateCell(33).SetCellValue(orden.CodigoSwift ?? "");
                row.CreateCell(34).SetCellValue(orden.Incoterm ?? "");
                row.CreateCell(35).SetCellValue(orden.RazonSocial ?? "");
                row.CreateCell(36).SetCellValue(orden.Nit ?? "");
                row.CreateCell(37).SetCellValue(orden.Telefono ?? "");
                row.CreateCell(38).SetCellValue(orden.NomContacto ?? "");
                row.CreateCell(39).SetCellValue(orden.Aprobador ?? "");
                row.CreateCell(40).SetCellValue(orden.IdAreaCorrespondencia?.ToString() ?? "");
                row.CreateCell(41).SetCellValue(orden.CorrespondeAsc ?? "");
                row.CreateCell(42).SetCellValue(orden.RecepcionTipo ?? "");
                row.CreateCell(43).SetCellValue(orden.ObservacionRecepcion ?? "");
                row.CreateCell(44).SetCellValue(orden.RutasArchivos ?? "");
            }

            for (var i = 0; i < columnas.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            using var stream = new MemoryStream();
            workbook.Write(stream);
            return stream.ToArray();
        }
    }
}
