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
                row.CreateCell(6).SetCellValue(Convert.ToDouble(producto.Cantidad ?? 0));

                var precio = Convert.ToDouble(producto.Precio ?? 0);
                row.CreateCell(7).SetCellValue(precio);
                row.GetCell(7).CellStyle = estiloMoneda;

                var total = precio * Convert.ToDouble(producto.Cantidad ?? 0);
                row.CreateCell(8).SetCellValue(total);
                row.GetCell(8).CellStyle = estiloMoneda;

                totalGeneral += (producto.Precio ?? 0) * (producto.Cantidad ?? 0);
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

        public byte[] GenerarExcelReporteGeneral(List<ReporteGeneralOrdenDto> ordenes)
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
            string[] columnas = { "ID", "Fecha", "Referencia", "Solicitante", "Proveedor", "Tipo", "Estado", "Fecha Estado" };
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
                row.CreateCell(2).SetCellValue(orden.Referencia ?? "");
                row.CreateCell(3).SetCellValue(orden.Solicitante ?? "");
                row.CreateCell(4).SetCellValue(orden.Proveedor ?? "");
                row.CreateCell(5).SetCellValue(orden.EsImportacion ? "IMPORTACION" : "NACIONAL");
                row.CreateCell(6).SetCellValue(orden.Estado ?? "Sin Estado");
                row.CreateCell(7).SetCellValue(orden.FechaEstado?.ToString("dd/MM/yyyy HH:mm") ?? "");
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
