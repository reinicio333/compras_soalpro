using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using orion.Controllers;

namespace orion.Servicios
{
    public class ReporteOrdenCompraExcelService
    {


        public byte[] GenerarExcelReporteGeneral(List<ReporteGeneralOrdenDetalleDto> ordenes)
        {
            IWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("ReporteGeneral");

            var fuenteHeader = workbook.CreateFont();
            fuenteHeader.IsBold = true;
            fuenteHeader.Color = IndexedColors.White.Index;
            fuenteHeader.FontHeightInPoints = 10;

            var fuenteTitulo = workbook.CreateFont();
            fuenteTitulo.IsBold = true;
            fuenteTitulo.FontHeightInPoints = 14;
            fuenteTitulo.Color = IndexedColors.White.Index;

            var fuenteDato = workbook.CreateFont();
            fuenteDato.FontHeightInPoints = 10;

            var fuenteIdGrupo = workbook.CreateFont();
            fuenteIdGrupo.IsBold = true;
            fuenteIdGrupo.FontHeightInPoints = 10;

            IDataFormat fmt = workbook.CreateDataFormat();

            byte[][] coloresGrupo = {
        new byte[] { 189, 215, 238 },
        new byte[] { 198, 239, 206 },
    };
            byte[][] coloresGrupoOscuro = {
        new byte[] { 155, 194, 230 },
        new byte[] { 169, 208, 142 },
    };

            var bgEstado = new byte[] { 255, 230, 153 }; // amarillo para estado alcanzado
            var bgEstadoVacio = new byte[] { 242, 242, 242 }; // gris para "—"

            XSSFColor Rgb(byte[] rgb) { var c = new XSSFColor(); c.SetRgb(rgb); return c; }

            XSSFCellStyle MakeStyle(byte[] bgRgb, bool negrita = false, bool numero = false, bool centrado = false)
            {
                var s = (XSSFCellStyle)workbook.CreateCellStyle();
                s.SetFont(negrita ? fuenteIdGrupo : fuenteDato);
                s.SetFillForegroundColor(Rgb(bgRgb));
                s.FillPattern = FillPattern.SolidForeground;
                s.BorderTop = BorderStyle.Thin;
                s.BorderBottom = BorderStyle.Thin;
                s.BorderLeft = BorderStyle.Thin;
                s.BorderRight = BorderStyle.Thin;
                s.TopBorderColor = IndexedColors.Grey50Percent.Index;
                s.BottomBorderColor = IndexedColors.Grey50Percent.Index;
                s.LeftBorderColor = IndexedColors.Grey50Percent.Index;
                s.RightBorderColor = IndexedColors.Grey50Percent.Index;
                s.VerticalAlignment = VerticalAlignment.Center;
                if (numero) s.DataFormat = fmt.GetFormat("#,##0.00");
                if (centrado) s.Alignment = HorizontalAlignment.Center;
                return s;
            }

            XSSFCellStyle MakeEstadoStyle(bool alcanzado)
            {
                var s = (XSSFCellStyle)workbook.CreateCellStyle();
                s.SetFont(fuenteDato);
                s.SetFillForegroundColor(Rgb(alcanzado ? bgEstado : bgEstadoVacio));
                s.FillPattern = FillPattern.SolidForeground;
                s.BorderTop = BorderStyle.Thin;
                s.BorderBottom = BorderStyle.Thin;
                s.BorderLeft = BorderStyle.Thin;
                s.BorderRight = BorderStyle.Thin;
                s.TopBorderColor = IndexedColors.Grey50Percent.Index;
                s.BottomBorderColor = IndexedColors.Grey50Percent.Index;
                s.LeftBorderColor = IndexedColors.Grey50Percent.Index;
                s.RightBorderColor = IndexedColors.Grey50Percent.Index;
                s.VerticalAlignment = VerticalAlignment.Center;
                s.Alignment = HorizontalAlignment.Center;
                return s;
            }

            var estiloTitulo = (XSSFCellStyle)workbook.CreateCellStyle();
            estiloTitulo.SetFont(fuenteTitulo);
            estiloTitulo.SetFillForegroundColor(Rgb(new byte[] { 31, 73, 125 }));
            estiloTitulo.FillPattern = FillPattern.SolidForeground;
            estiloTitulo.Alignment = HorizontalAlignment.Center;
            estiloTitulo.VerticalAlignment = VerticalAlignment.Center;

            var estiloHeader = (XSSFCellStyle)workbook.CreateCellStyle();
            estiloHeader.SetFont(fuenteHeader);
            estiloHeader.SetFillForegroundColor(Rgb(new byte[] { 31, 73, 125 }));
            estiloHeader.FillPattern = FillPattern.SolidForeground;
            estiloHeader.BorderTop = BorderStyle.Medium;
            estiloHeader.BorderBottom = BorderStyle.Medium;
            estiloHeader.BorderLeft = BorderStyle.Thin;
            estiloHeader.BorderRight = BorderStyle.Thin;
            estiloHeader.Alignment = HorizontalAlignment.Center;
            estiloHeader.VerticalAlignment = VerticalAlignment.Center;

            var estadosColumnas = new (int Id, string Nombre)[]
            {
        (1,  "Pedido"),
        (2,  "Pre autorización"),
        (3,  "Aprobación OC"),
        (4,  "En tránsito extranjero"),
        (5,  "En aduana"),
        (6,  "En senasag"),
        (7,  "En tránsito nacional"),
        (8,  "Enviado a Proveedor"),
        (9,  "Recepción almacenes"),
        (10, "Costeado en SAP"),
        (11, "Rechazado"),
            };

            string[] columnaBase =
            {
        "Nº OC",                   // 0  ← merge por grupo
        "Fecha Creación OC",       // 1
        "Nº Solicitud",            // 2
        "Solicitantes",            // 3
        "Fecha Requerimiento",     // 4
        "Proveedor Item",          // 5
        "Nombre Item",             // 6
        "Codigo Item",             // 7
        "Cantidad",                // 8
        "Precio",                  // 9
        "Tipo",                    // 10
    };

            int colBase = columnaBase.Length;               // 11
            int totalCols = colBase + estadosColumnas.Length; // 11 + 11 = 22

            // ── Fila 0: Título ───────────────────────────────────────────
            var rowTitulo = sheet.CreateRow(0);
            rowTitulo.HeightInPoints = 26;
            for (int c = 0; c < totalCols; c++)
            {
                var tmp = rowTitulo.CreateCell(c);
                tmp.CellStyle = estiloTitulo;
                if (c == 0) tmp.SetCellValue("REPORTE GENERAL DE ÓRDENES DE COMPRA");
            }
            sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, totalCols - 1));

            // ── Fila 1: Headers ──────────────────────────────────────────
            var rowHeader = sheet.CreateRow(1);
            rowHeader.HeightInPoints = 22;
            for (int i = 0; i < colBase; i++)
            {
                var cell = rowHeader.CreateCell(i);
                cell.SetCellValue(columnaBase[i]);
                cell.CellStyle = estiloHeader;
            }
            for (int i = 0; i < estadosColumnas.Length; i++)
            {
                var cell = rowHeader.CreateCell(colBase + i);
                cell.SetCellValue(estadosColumnas[i].Nombre);
                cell.CellStyle = estiloHeader;
            }

            // ── Datos agrupados por ID ───────────────────────────────────
            var grupos = ordenes.GroupBy(o => o.Id).ToList();
            var rowIndex = 2;
            var grupoIdx = 0;

            foreach (var grupo in grupos)
            {
                var items = grupo.ToList();
                int colorIdx = grupoIdx % 2;

                var sTexto = MakeStyle(coloresGrupo[colorIdx]);
                var sNum = MakeStyle(coloresGrupo[colorIdx], numero: true);
                var sId = MakeStyle(coloresGrupoOscuro[colorIdx], negrita: true, centrado: true);

                var historial = items[0].HistorialEstados ?? new Dictionary<int, DateTime>();

                int filaInicio = rowIndex;

                for (int itemIdx = 0; itemIdx < items.Count; itemIdx++)
                {
                    var orden = items[itemIdx];
                    var row = sheet.CreateRow(rowIndex);
                    row.HeightInPoints = 16;

                    void T(int col, string? val) { var cc = row.CreateCell(col); cc.CellStyle = sTexto; cc.SetCellValue(val ?? ""); }
                    void N(int col, double val) { var cc = row.CreateCell(col); cc.CellStyle = sNum; cc.SetCellValue(val); }

                    // Col 0: Nº OC — valor solo en primera fila del grupo
                    var cellId = row.CreateCell(0);
                    cellId.CellStyle = sId;
                    if (itemIdx == 0) cellId.SetCellValue(orden.Id.ToString());

                    T(1, orden.Fecha?.ToString("dd/MM/yyyy") ?? "");
                    T(2, orden.IdSolicitud?.ToString() ?? "");
                    T(3, orden.SolicitantesSolicitudesVinculadas ?? "");
                    T(4, orden.Frequerimiento?.ToString("dd/MM/yyyy") ?? "");
                    T(5, orden.ProveedorItem ?? "");
                    T(6, orden.NombreItem ?? "");
                    T(7, orden.CodigoItem ?? "");

                    if (orden.CantidadItem.HasValue) N(8, Convert.ToDouble(orden.CantidadItem.Value));
                    else T(8, "");
                    if (orden.PrecioItem.HasValue) N(9, Convert.ToDouble(orden.PrecioItem.Value));
                    else T(9, "");

                    T(10, orden.EsImportacion ? "IMPORTACION" : "NACIONAL");

                    // Columnas de estado: solo en primera fila del grupo
                    if (itemIdx == 0)
                    {
                        for (int e = 0; e < estadosColumnas.Length; e++)
                        {
                            int estadoId = estadosColumnas[e].Id;
                            var cc = row.CreateCell(colBase + e);
                            bool alcanzado = historial.TryGetValue(estadoId, out var fechaEstado);
                            cc.CellStyle = MakeEstadoStyle(alcanzado);
                            cc.SetCellValue(alcanzado ? fechaEstado.ToString("dd/MM/yyyy HH:mm") : "—");
                        }
                    }
                    else
                    {
                        for (int e = 0; e < estadosColumnas.Length; e++)
                        {
                            var cc = row.CreateCell(colBase + e);
                            cc.CellStyle = MakeEstadoStyle(false);
                            cc.SetCellValue("");
                        }
                    }

                    rowIndex++;
                }

                // Merge Nº OC y columnas de estado si hay más de 1 ítem
                if (items.Count > 1)
                {
                    sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(filaInicio, rowIndex - 1, 0, 0));
                    for (int e = 0; e < estadosColumnas.Length; e++)
                    {
                        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(
                            filaInicio, rowIndex - 1, colBase + e, colBase + e));
                    }
                }

                grupoIdx++;
            }

            // ── AutoSize con ancho mínimo ────────────────────────────────
            for (var i = 0; i < totalCols; i++)
            {
                sheet.AutoSizeColumn(i);
                if (sheet.GetColumnWidth(i) < 10 * 256)
                    sheet.SetColumnWidth(i, 10 * 256);
            }

            sheet.CreateFreezePane(0, 2);
            sheet.SetAutoFilter(new NPOI.SS.Util.CellRangeAddress(1, 1, 0, totalCols - 1));

            using var stream = new MemoryStream();
            workbook.Write(stream);
            return stream.ToArray();
        }

        private void T(int v, object value)
        {
            throw new NotImplementedException();
        }
    }
}
