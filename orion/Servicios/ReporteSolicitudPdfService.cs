using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using orion.Models;

namespace orion.Servicios
{
    public class ReporteSolicitudPdfService
    {
        public byte[] GenerarPdfSolicitud(Solicitudes solicitud, List<DetalleSolicitudes> detalles)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter.Landscape()); // Horizontal
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Element(content => ComposeHeader(content, solicitud));
                    page.Content().Element(content => ComposeContent(content, detalles));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        void ComposeHeader(IContainer container, Solicitudes solicitud)
        {
            var logoBytes = CargarImagen("images/soalpro.png");

            container.Column(column =>
            {
                // Fila del logo y número de solicitud
                column.Item().Row(row =>
                {
                    // Logo
                    if (logoBytes != null)
                    {
                        row.ConstantItem(150).Height(45).Image(logoBytes, ImageScaling.FitArea);
                    }

                    row.RelativeItem(); // Espacio central

                    // N° de solicitud
                    row.ConstantItem(150).AlignRight().AlignTop().Row(r =>
                    {
                        r.AutoItem().Text($"N°: {solicitud.Id}").Bold().FontSize(11);
                        
                    });
                });


                // Título
                column.Item().Row(row =>
                {
                    row.RelativeItem().Background(Colors.Red.Darken1).Padding(3)
                        .AlignCenter().AlignMiddle()
                        .Text("SOLICITUD DE COMPRA (BIENES O SERVICIOS)")
                        .FontSize(12)
                        .Bold()
                        .FontColor(Colors.White);
                });
                column.Item().PaddingTop(10);

                // Información de la solicitud - Primera fila
                column.Item().Row(row =>
                {
                    // FECHA
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(60).AlignBottom().Text("FECHA:").Bold().FontSize(9);
                            r.ConstantItem(100).Border(1).BorderColor(Colors.Black)
                                .Padding(3).Text(solicitud.Fecha.ToString("dd/MM/yyyy")).FontSize(9);
                        });
                        c.Item().PaddingTop(5);
                        /*c.Item().Row(r =>
                        {
                            r.ConstantItem(60).AlignBottom().Text("F. REQ.:").Bold().FontSize(9);
                            r.ConstantItem(100).Border(1).BorderColor(Colors.Black)
                                .Padding(3).Text(solicitud.Frequerimiento?.ToString("dd/MM/yyyy") ?? "").FontSize(9);
                        });*/
                    });

                    row.ConstantItem(30); // Espacio

                    // REFERENCIA
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(90).AlignBottom().Text("REFERENCIA:").Bold().FontSize(9);
                            r.ConstantItem(150).Border(1).BorderColor(Colors.Black)
                                .Padding(3).Text(solicitud.Referencia ?? "").FontSize(9);
                        });
                        c.Item().PaddingTop(5);
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(90).AlignBottom().Text("SOLICITANTE:").Bold().FontSize(9);
                            r.ConstantItem(150).Border(1).BorderColor(Colors.Black)
                                .Padding(3).Text(solicitud.Solicitante ?? "").FontSize(9);
                        });
                    });
                });
            });
        }

        void ComposeContent(IContainer container, List<DetalleSolicitudes> detalles)
        {
            container.PaddingTop(10).Column(column =>
            {
                // Tabla de contenido
                column.Item().Table(table =>
                {
                    // Definición de columnas
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.5f);  // Código
                        columns.RelativeColumn(2.5f);     // Descripción
                        columns.RelativeColumn(2.5f);     // Características
                        columns.ConstantColumn(80);    // Proveedor
                        columns.ConstantColumn(60);    // Unidad
                        columns.ConstantColumn(50);    // Fecha Req.
                        columns.ConstantColumn(50);    // Req. Días
                        columns.ConstantColumn(50);    // Ult. Precio 
                        columns.ConstantColumn(55);    // Ult. Compra
                        columns.ConstantColumn(55);    // Cantidad
                        columns.ConstantColumn(55);    // Estado
                        columns.ConstantColumn(55);    // Fecha Aprobado
                    });

                    // Encabezados
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("COD. \nPRODUCTO").Bold();
                        header.Cell().Element(HeaderStyle).Text("DESCRIPCIÓN").Bold();
                        header.Cell().Element(HeaderStyle).Text("CARACTERISTICAS").Bold();
                        header.Cell().Element(HeaderStyle).Text("PROVEEDOR").Bold();
                        header.Cell().Element(HeaderStyle).Text("UNIDAD DE\nCOMPRA").Bold();
                        header.Cell().Element(HeaderStyle).Text("FECHA\nREQ.").Bold();
                        header.Cell().Element(HeaderStyle).Text("REQ.\nDIAS").Bold();
                        header.Cell().Element(HeaderStyle).Text("ULT.\nPRECIO").Bold(); 
                        header.Cell().Element(HeaderStyle).Text("F. ULT.\nCOMPRA").Bold();
                        header.Cell().Element(HeaderStyle).Text("CANTIDAD").Bold();
                        header.Cell().Element(HeaderStyle).Text("ESTADO").Bold();
                        header.Cell().Element(HeaderStyle).Text("FECHA\nAPROBADO").Bold();

                        static IContainer HeaderStyle(IContainer container)
                        {
                            return container.Border(1)
                                .BorderColor(Colors.Black)
                                .Background(Colors.Grey.Lighten3)
                                .Padding(5)
                                .AlignCenter()
                                .AlignMiddle()
                                .DefaultTextStyle(x => x.FontSize(7));
                        }
                    });

                    // Filas de productos
                    foreach (var detalle in detalles)
                    {
                        table.Cell().Element(CellStyle).Text(detalle.Codigo ?? "").FontSize(7);
                        table.Cell().Element(CellStyle).Text(detalle.Descripcion ?? "").FontSize(7);
                        table.Cell().Element(CellStyle).Text(detalle.Caracteristicas ?? "").FontSize(7);
                        table.Cell().Element(CellStyle).Text(detalle.Proveedor ?? "").FontSize(7);
                        table.Cell().Element(CellStyle).AlignCenter().Text(detalle.Unidad ?? "").FontSize(7);
                        table.Cell().Element(CellStyle).AlignCenter().Text(detalle.Frequerimiento.HasValue ? detalle.Frequerimiento.Value.ToString("dd/MM/yyyy") : "").FontSize(7);
                        table.Cell().Element(CellStyle).AlignCenter().Text(detalle.FrequerimientoDias?.ToString() ?? "").FontSize(7);
                        table.Cell().Element(CellStyle).AlignRight().Text(detalle.UltimoPrecio > 0 ? detalle.UltimoPrecio.ToString("N2") : "").FontSize(7);
                        table.Cell().Element(CellStyle).AlignCenter().Text(detalle.FultimoPrecio.HasValue ? detalle.FultimoPrecio.Value.ToString("dd/MM/yyyy") : "").FontSize(7);
                        table.Cell().Element(CellStyle).AlignCenter().Text(detalle.Cantidad.ToString("N2")).FontSize(7);
                        table.Cell().Element(CellStyle).AlignCenter().Text(detalle.Estado ?? "").FontSize(7);
                        table.Cell().Element(CellStyle).AlignCenter()
                            .Text(detalle.Faprobado.HasValue && detalle.Faprobado.Value.Year > 1900
                            ? detalle.Faprobado.Value.ToString("dd/MM/yyyy")
                            : "").FontSize(7);
                    }

                    // Filas vacías para completar la página (mínimo 6 filas totales)
                    int filasVacias = Math.Max(0, 6 - detalles.Count);
                    for (int i = 0; i < filasVacias; i++)
                    {
                        table.Cell().Element(CellStyle).Text("");
                        table.Cell().Element(CellStyle).Text("");
                        table.Cell().Element(CellStyle).Text("");
                        table.Cell().Element(CellStyle).Text("");
                        table.Cell().Element(CellStyle).Text("");
                        table.Cell().Element(CellStyle).Text("");
                        table.Cell().Element(CellStyle).Text("");
                        table.Cell().Element(CellStyle).Text("");
                        table.Cell().Element(CellStyle).Text("");
                        table.Cell().Element(CellStyle).Text("");
                        table.Cell().Element(CellStyle).Text("");
                        table.Cell().Element(CellStyle).Text("");
                    }

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.Border(1)
                            .BorderColor(Colors.Black)
                            .Padding(3)
                            .MinHeight(20);
                    }
                });

                // Espaciador que empuja el footer hacia abajo
                column.Item().ExtendVertical();
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem(); // Espacio izquierdo

                // SOLICITADO POR
                row.ConstantItem(250).Column(column =>
                {
                    column.Item().Border(1).BorderColor(Colors.Black)
                        .Height(80).AlignBottom().AlignCenter()
                        .PaddingBottom(5)
                        .Text("SOLICITADO POR").Bold().FontSize(9);
                });

                row.ConstantItem(50); // Espacio entre firmas

                // AUTORIZADO POR JEFE DE ÁREA
                row.ConstantItem(250).Column(column =>
                {
                    column.Item().Border(1).BorderColor(Colors.Black)
                        .Height(80).AlignBottom().AlignCenter()
                        .PaddingBottom(5)
                        .Text("AUTORIZADO POR JEFE DE ÁREA").Bold().FontSize(9);
                });

                row.RelativeItem(); // Espacio derecho
            });
        }

        private byte[] CargarImagen(string rutaRelativa)
        {
            try
            {
                var rutaCompleta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", rutaRelativa.TrimStart('~', '/'));

                if (File.Exists(rutaCompleta))
                {
                    return File.ReadAllBytes(rutaCompleta);
                }
            }
            catch { }

            return null;
        }
    }
}