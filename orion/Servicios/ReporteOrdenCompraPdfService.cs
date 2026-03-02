using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using orion.Models;
using orion.Controllers;
using NPOI.SS.Formula.Functions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace orion.Servicios
{
    public class ReporteOrdenCompraPdfService
    {
        public byte[] GenerarPdfOrdenCompra(Solicitudes solicitud, OrdenCompraDto datos)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter.Landscape());
                    page.Margin(25);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Element(content => ComposeHeader(content, solicitud, datos));

                    page.Content().Column(column =>
                    {
                        column.Item().Element(content => ComposeContent(content, datos));
                        column.Spacing(10); 
                    });

                    page.Footer().AlignBottom().Element(content => ComposeFooter(content, datos));
                });
            });

            return document.GeneratePdf();
        }

        void ComposeHeader(IContainer container, Solicitudes solicitud, OrdenCompraDto datos)
        {
            var razon = datos.Facturacion?.Razon?.ToUpper() ?? "";

            string logoFile;
            string logoColor;
            string logoFondo;
            string logoLetra;

            if (razon.Contains("CARSA"))
            {
                logoFile = "images/carsa.png";
                logoColor = "#24346c"; // azul oscuro océano
                logoFondo = "#24346c";
                logoLetra = "#ffffff";

            }
            else if (razon.Contains("TECALIM"))
            {
                logoFile = "images/tecalim.png";
                logoColor = "#2d7a2d";
                logoFondo = "#ffffff";
                logoLetra = "#ffffff";

            }
            else
            {
                logoFile = "images/soalpro.png";
                logoColor = "#ffcccc"; // rojo soalpro
                logoFondo = "#e20d16";
                logoLetra = "#000000";


            }

            var logoBytes = CargarImagen(logoFile);

            container.Column(column =>
            {
                // Logo y numero
                column.Item().Row(row =>
                {
                    if (logoBytes != null)
                    {
                        row.ConstantItem(180).Border(1).BorderColor(Colors.Black)
                            .Background(logoFondo)
                            .AlignCenter().AlignMiddle()
                            .Height(26).Image(logoBytes, ImageScaling.FitArea);
                    }

                    row.RelativeItem().Border(1).BorderColor(Colors.Black)
                        .AlignCenter().AlignMiddle()
                        .Text("REGISTRO").FontSize(9).FontFamily("Cambria");

                    row.ConstantItem(180).Border(1).BorderColor(Colors.Black)
                        .Padding(3).AlignLeft().AlignMiddle().Column(c =>
                        {
                            c.Item().AlignLeft().Text("COM – PRO – 177 – REG – 03").FontSize(9).FontFamily("Cambria");
                            c.Item().AlignLeft().Text("Versión: 006").FontSize(9).FontFamily("Cambria");
                            c.Item().AlignLeft().Text("Página 1 de 1").FontSize(9).FontFamily("Cambria");
                        });
                });

                column.Item().PaddingTop(1);

                column.Item().Padding(3).PaddingRight(75).AlignRight().Row(row =>
                {
                    row.AutoItem().AlignBottom().PaddingRight(10).Text("NÚMERO ")
                        .FontSize(9).FontFamily("Cambria");

                    row.AutoItem().Border(1).PaddingHorizontal(35).PaddingVertical(2).AlignMiddle().Text($"{datos.Id}/2026")
                        .FontSize(9).FontFamily("Cambria");
                });



                // Título
                column.Item().Background(logoColor).Padding(1)
                    .AlignCenter().Text("ORDEN DE COMPRA").FontFamily("Cambria")
                    .FontSize(9).FontColor(logoLetra);    

                column.Item().PaddingTop(5);

                // Tabla de información
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(110);
                        columns.RelativeColumn();    
                        columns.ConstantColumn(80); 
                        columns.ConstantColumn(120); 
                        columns.RelativeColumn();    
                    });

                    // Fila 1
                    table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                        .Text("FECHA:").FontFamily("Cambria").FontSize(8);
                    table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(10)
                        .AlignMiddle().AlignCenter().Text(datos.Fecha).FontFamily("Cambria").FontSize(8);
                    table.Cell().PaddingBottom(5);
                    table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                        .Text("T.C.").FontFamily("Cambria").FontSize(8);
                    table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(10)
                        .AlignMiddle().AlignCenter().Text(datos.Tc).FontFamily("Cambria").FontSize(8);

                    // Fila 2
                    table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                        .Text("PROVEEDOR").FontFamily("Cambria").FontSize(8);
                    table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(10).AlignMiddle().AlignCenter()
                        .AlignMiddle().Text(datos.Proveedor ?? "").FontFamily("Cambria").FontSize(8);
                    table.Cell().PaddingBottom(5);
                    table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                        .Text("SOLICITANTE").FontFamily("Cambria").FontSize(8);
                    table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(10).AlignCenter()
                        .AlignMiddle().Text(datos.Solicitante ?? "").FontFamily("Cambria").FontSize(8).WrapAnywhere();
    
                    // Fila 3
                    table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                        .Text("TELEF./FAX PROVEEDOR").FontFamily("Cambria").FontSize(8);
                    table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(10)
                        .AlignMiddle().AlignCenter().Text(datos.Telefono ?? "").FontFamily("Cambria").FontSize(8);
                    table.Cell().PaddingBottom(5);
                    table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                        .Text("CORRESPONDE A.S.C.").FontFamily("Cambria").FontSize(8);
                    table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(10).AlignCenter()
                        .AlignMiddle().Text(datos.CorrespondeAsc ?? "").FontFamily("Cambria").FontSize(8);

                    // Fila 4
                    table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                        .Text("NOMBRE CONTACTO").FontFamily("Cambria").FontSize(8);
                    table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(10).AlignMiddle().AlignCenter()
                        .AlignMiddle().Text(datos.NomContacto ?? "").FontFamily("Cambria").FontSize(8);
                    table.Cell().PaddingBottom(5);
                    table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                        .Text("REFERENCIA").FontFamily("Cambria").FontSize(8);
                    table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(10).AlignCenter()
                        .AlignMiddle().Text(datos.Referencia ?? "").FontFamily("Cambria").FontSize(8);
                });






            });

            IContainer CellHeaderStyle(IContainer c) =>
                c.Border(1).BorderColor("#999999").Background("#f3f3f3")
                .Padding(3).AlignMiddle().DefaultTextStyle(x => x.Bold());

            IContainer CellStyle(IContainer c) =>
                c.Border(1).BorderColor("#999999").Padding(3).AlignMiddle();
        }

        void ComposeContent(IContainer container, OrdenCompraDto datos)
        {
            container.PaddingTop(5).Column(column =>
            {
                // Tabla de productos
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(60);     // Código
                        columns.ConstantColumn(40);     // Nro
                        columns.RelativeColumn(2);      // Descripción
                        columns.ConstantColumn(85);     // F. Entrega
                        columns.RelativeColumn(1.5f);   // Características
                        columns.ConstantColumn(50);     // Unidad
                        columns.ConstantColumn(55);     // Cantidad
                        columns.ConstantColumn(55);     // Últ. Precio
                        columns.ConstantColumn(65);     // Últ. Compra
                        columns.ConstantColumn(60);     // Precio
                        columns.ConstantColumn(70);     // Total
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("CÓDIGO DE PRODUCTO").FontFamily("Cambria").AlignCenter();
                        header.Cell().Element(HeaderStyle).Text("Nro.").FontFamily("Cambria").AlignCenter();
                        header.Cell().Element(HeaderStyle).Text("DESCRIPCION").FontFamily("Cambria").AlignCenter();
                        header.Cell().Element(HeaderStyle).Text("FECHA DE ENTREGA REQUERIDA").FontFamily("Cambria").AlignCenter();
                        header.Cell().Element(HeaderStyle).Text("CARACTERÍSTICAS").FontFamily("Cambria").AlignCenter();
                        header.Cell().Element(HeaderStyle).Text("UNIDAD").FontFamily("Cambria").AlignCenter();
                        header.Cell().Element(HeaderStyle).Text("CANTIDAD").FontFamily("Cambria").AlignCenter();
                        header.Cell().Element(HeaderStyle).Text("ULT.\nPRECIO").FontFamily("Cambria").AlignCenter();
                        header.Cell().Element(HeaderStyle).Text("F. ULT.\nCOMPRA").FontFamily("Cambria").AlignCenter();
                        header.Cell().Element(HeaderStyle).Text("PRECIO UNITARIO").FontFamily("Cambria").AlignCenter();
                        header.Cell().Element(HeaderStyle).Text("TOTAL BS").FontFamily("Cambria").AlignCenter();
                    });

                    // Productos
                    decimal totalGeneral = 0;
                    foreach (var producto in datos.Productos)
                    {
                        var total = producto.Cantidad * producto.Precio;
                        totalGeneral += total;

                        table.Cell().Element(CellStyle).AlignMiddle().Text(producto.Codigo ?? "").FontSize(7).FontFamily("Cambria").AlignCenter();
                        table.Cell().Element(CellStyle).AlignMiddle().AlignCenter().Text(producto.Nro ?? "").FontSize(7).FontFamily("Cambria");
                        table.Cell().Element(CellStyle).AlignMiddle().Text(producto.Descripcion ?? "").FontSize(7);
                        table.Cell().Background("#ffff00").Element(CellStyle).AlignMiddle().AlignCenter().Text(producto.FechaEntrega ?? "").FontSize(7).FontFamily("Cambria");
                        table.Cell().Element(CellStyle).AlignMiddle().Text(producto.Caracteristicas ?? "").FontSize(7).FontFamily("Cambria").AlignCenter();
                        table.Cell().Element(CellStyle).AlignMiddle().AlignCenter().Text(producto.Unidad ?? "").FontSize(7).FontFamily("Cambria");
                        table.Cell().Element(CellStyle).AlignMiddle().AlignCenter().Text(producto.Cantidad.ToString("N2")).FontSize(7).FontFamily("Cambria");
                        table.Cell().Element(CellStyle).AlignMiddle().AlignRight().Text(producto.UltimoPrecio.HasValue && producto.UltimoPrecio > 0 ? producto.UltimoPrecio.Value.ToString("N2") : "").FontSize(7).FontFamily("Cambria");
                        table.Cell().Element(CellStyle).AlignMiddle().AlignCenter().Text(producto.FultimoPrecio.HasValue ? producto.FultimoPrecio.Value.ToString("dd/MM/yyyy") : "").FontSize(7).FontFamily("Cambria");
                        table.Cell().Element(CellStyle).AlignMiddle().AlignRight().Text(producto.Precio.ToString("N2")).FontSize(7).FontFamily("Cambria");
                        table.Cell().Element(CellStyle).AlignMiddle().AlignRight().Text(total.ToString("N2")).FontSize(7).Bold().FontFamily("Cambria");
                    }

                    // Total
                    
                    table.Cell().ColumnSpan(7); 

                    table.Cell()
                        .Element(c => c.Border(1).BorderColor("#999999").Padding(3).AlignRight().AlignMiddle())
                        .Text("TOTAL Bs")
                        .FontSize(7)
                        .Bold()
                        .FontFamily("Cambria");

                    table.Cell().Element(c => c.Border(1).BorderColor("#999999").Padding(3).AlignRight().AlignMiddle());

                    table.Cell().Element(c => c.Border(1).BorderColor("#999999").Padding(3).AlignRight().AlignMiddle()); 

                    table.Cell()
                        .Element(c => c.Border(1).Background("#ffcccc").BorderColor("#999999").Padding(3).AlignRight().AlignMiddle())
                        .Text(totalGeneral.ToString("N2"))
                        .FontSize(7)
                        .Bold()
                        .FontFamily("Cambria");

                    IContainer HeaderStyle(IContainer c) =>
                        c.Border(1).BorderColor("#999999").Background("#e0e0e0")
                        .Padding(3).AlignCenter().AlignMiddle().DefaultTextStyle(x => x.FontSize(8));
                    


                    IContainer CellStyle(IContainer c) =>
                        c.Border(1).BorderColor("#999999").MinHeight(15).Padding(3);

                    IContainer TotalHeaderStyle(IContainer c) =>
                        c.Border(1).BorderColor("#999999").Background("#f3f3f3")
                        .Padding(3).AlignRight().AlignMiddle().DefaultTextStyle(x => x.Bold());

                    IContainer TotalStyle(IContainer c) =>
                        c.Border(1).BorderColor("#999999").Background("#f3f3f3")
                        .Padding(3).AlignRight().AlignMiddle();
                });

                column.Item().PaddingTop(8);
                column.Item().Row(r =>
                {
                    r.AutoItem().AlignMiddle().PaddingRight(15).Text("OBSERVACION: ").FontSize(8).FontFamily("Cambria");
                    r.RelativeItem().Border(0.6f).MinHeight(18).Padding(3)
                        .Text(datos.Cabecera?.Observacion ?? "").FontSize(8).FontFamily("Cambria");
                });
                column.Item().PaddingTop(8);

                // FORMA DE PAGO y tabla combinada
                column.Item().Row(mainRow =>
                {
                    // Columna izquierda: FORMA DE PAGO
                    mainRow.ConstantItem(350).Column(leftCol =>
                    {
                        leftCol.Item().Row(r =>
                        {
                            r.AutoItem().AlignMiddle().PaddingRight(4)
                                .Text("FORMA DE PAGO:").FontSize(7).FontFamily("Cambria");

                            // EFECTIVO
                            r.ConstantItem(20).Height(14).PaddingRight(3)
                                .Background("#ffff00").Border(1).BorderColor(Colors.Black)
                                .AlignCenter().AlignMiddle()
                                .Text(datos.Cabecera?.FormaPago == "Efectivo" ? "X" : "").FontSize(9);
                            r.AutoItem().PaddingRight(4).AlignMiddle()
                                .Text("EFECTIVO").FontSize(7).FontFamily("Cambria");

                            // TRANSFERENCIA
                            r.ConstantItem(20).Height(14).PaddingRight(3)
                                .Background("#ffff00").Border(1).BorderColor(Colors.Black)
                                .AlignCenter().AlignMiddle()
                                .Text(datos.Cabecera?.FormaPago == "Transferencia" ? "X" : "").FontSize(9);
                            r.AutoItem().PaddingRight(4).AlignMiddle()
                                .Text("TRANSFERENCIA").FontSize(7).FontFamily("Cambria");

                            // CHEQUE
                            r.ConstantItem(20).Height(14).PaddingRight(3)
                                .Background("#ffff00").Border(1).BorderColor(Colors.Black)
                                .AlignCenter().AlignMiddle()
                                .Text(datos.Cabecera?.FormaPago == "Cheque" ? "X" : "").FontSize(9);
                            r.AutoItem().PaddingRight(4).AlignMiddle()
                                .Text("CHEQUE").FontSize(7).FontFamily("Cambria");

                            // QR
                            r.ConstantItem(20).Height(14).PaddingRight(3)
                                .Background("#ffff00").Border(1).BorderColor(Colors.Black)
                                .AlignCenter().AlignMiddle()
                                .Text(datos.Cabecera?.FormaPago == "QR" ? "X" : "").FontSize(9);
                            r.AutoItem().AlignMiddle()
                                .Text("QR").FontSize(7).FontFamily("Cambria");
                        });




                        leftCol.Item().PaddingTop(10);

                        // INFORMACIÓN DE ENTREGA
                        leftCol.Item().Padding(3).AlignCenter().Text("INFORMACIÓN DE ENTREGA:").FontSize(8).FontFamily("Cambria");
                        leftCol.Item().PaddingTop(10);
                        leftCol.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(120);
                                columns.RelativeColumn();
                            });

                            table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                                .Text("MEDIO DE TRANSPORTE").FontFamily("Cambria").FontSize(8);
                            table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(20)
                                .AlignMiddle().AlignCenter().Text(datos.Entrega?.MedioTransporte ?? "").FontFamily("Cambria").FontSize(8);

                            table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                                .Text("RESPONSABLE DE RECEPCIÓN").FontFamily("Cambria").FontSize(8);
                            table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(20)
                                .AlignMiddle().AlignCenter().Text(datos.Entrega?.ResponsableRecepcion ?? "").FontFamily("Cambria").FontSize(8);

                            table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                                .Text("FECHA DE ENTREGA").FontFamily("Cambria").FontSize(8);
                            table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(10)
                                .AlignMiddle().AlignCenter().Text(datos.Entrega?.FechaEntrega ?? "").FontFamily("Cambria").FontSize(8);

                            table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                                .Text("LUGAR DE ENTREGA").FontFamily("Cambria").FontSize(8);
                            table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(20)
                                .AlignMiddle().AlignCenter().Text(datos.Entrega?.LugarEntrega ?? "").FontFamily("Cambria").FontSize(8);
                        });
                    });

                    mainRow.ConstantItem(60); // espacio entre columnas

                    // Columna derecha: INFORMACIÓN DE PAGO
                    mainRow.RelativeItem().Column(rightCol =>
                    {
                        rightCol.Item().Padding(3).AlignCenter().Text("INFORMACIÓN DE PAGO:").FontSize(8).FontFamily("Cambria");
                        rightCol.Item().PaddingTop(10);
                        rightCol.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(125);
                                columns.RelativeColumn();
                            });

                            // FECHA Y MONTO DE ANTICIPO
                            table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                                .Text("FECHA Y MONTO DE ANTICIPO").FontFamily("Cambria").FontSize(8);

                            table.Cell().PaddingBottom(5).Row(row =>
                            {
                                row.ConstantItem(65).Border(1).PaddingVertical(2).PaddingHorizontal(6)
                                    .AlignMiddle().AlignCenter()
                                    .Text(datos.Pago?.AnticipoF ?? "").FontFamily("Cambria").FontSize(8);

                                row.Spacing(10);

                                row.RelativeItem().Border(1).PaddingVertical(2).PaddingHorizontal(6)
                                    .AlignMiddle().AlignCenter()
                                    .Text(datos.Pago?.AnticipoM ?? "").FontFamily("Cambria").FontSize(8);
                            });

                            // FECHA Y MONTO DE PAGO FINAL
                            table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                                .Text("FECHA Y MONTO DE PAGO FINAL").FontFamily("Cambria").FontSize(8);

                            table.Cell().PaddingBottom(5).Row(row =>
                            {
                                row.ConstantItem(65).Border(1).PaddingVertical(2).PaddingHorizontal(6)
                                    .AlignMiddle().AlignCenter()
                                    .Text(datos.Pago?.FinalF ?? "").FontFamily("Cambria").FontSize(8);

                                row.Spacing(10);

                                row.RelativeItem().Border(1).PaddingVertical(2).PaddingHorizontal(6)
                                    .AlignMiddle().AlignCenter()
                                    .Text(datos.Pago?.FinalM ?? "").FontFamily("Cambria").FontSize(8);
                            });

                            // BANCO Y CUENTA BANCARIA
                            table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                                .Text("BANCO Y CUENTA BANCARIA").FontFamily("Cambria").FontSize(8);

                            table.Cell().PaddingBottom(5).Row(row =>
                            {
                                row.ConstantItem(65).Border(1).PaddingVertical(2).PaddingHorizontal(6)
                                    .AlignMiddle().AlignCenter()
                                    .Text(datos.Pago?.Banco ?? "").FontFamily("Cambria").FontSize(8);

                                row.Spacing(10);

                                row.RelativeItem().Border(1).PaddingVertical(2).PaddingHorizontal(6)
                                    .AlignMiddle().AlignCenter()
                                    .Text(datos.Pago?.Cuenta ?? "").FontFamily("Cambria").FontSize(8);
                            });

                            // NOMBRE CUENTA BANCARIA
                            table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                                .Text("NOMBRE CUENTA BANCARIA").FontFamily("Cambria").FontSize(8);
                            table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(10)
                                .AlignMiddle().AlignCenter().Text(datos.Pago?.NombreCuenta ?? "").FontFamily("Cambria").FontSize(8);

                            // CÓDIGO SWIFT DEL BANCO
                            table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                                .Text("CÓDIGO SWIFT DEL BANCO").FontFamily("Cambria").FontSize(8);
                            table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(10)
                                .AlignMiddle().AlignCenter().Text(datos.Pago?.Swift ?? "").FontFamily("Cambria").FontSize(8);

                            // INCOTERM
                            table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                                .Text("INCOTERM").FontFamily("Cambria").FontSize(8);
                            table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(10)
                                .AlignMiddle().AlignCenter().Text(datos.Pago?.Incoterm ?? "").FontFamily("Cambria").FontSize(8);
                        });
                    });
                });

                column.Item().PaddingTop(5);

                // Facturación
                column.Item().Background("#e0e0e0").Padding(2)
                    .AlignCenter().Text("FACTURACIÓN").FontFamily("Cambria")
                    .FontSize(9);
                
                column.Item().PaddingTop(10);
                column.Item().Row(mainRow =>
                {
                    mainRow.ConstantItem(350).Column(leftCol =>
                    {
                        
                       
                        leftCol.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(120);
                                columns.RelativeColumn();
                            });

                            table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                                .Text("RAZON SOCIAL").FontFamily("Cambria").FontSize(8);
                            table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(20)
                                .AlignMiddle().AlignCenter().Text((datos.Facturacion?.Razon ?? "").ToUpper()).FontFamily("Cambria").FontSize(8);

                        });
                    });

                    mainRow.ConstantItem(60); 

                    mainRow.RelativeItem().Column(rightCol =>
                    {
                        
                        rightCol.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(125);
                                columns.RelativeColumn();
                            });

                            table.Cell().PaddingBottom(5).PaddingRight(6).AlignMiddle()
                                .Text("NIT").FontFamily("Cambria").FontSize(8);
                            table.Cell().PaddingBottom(5).Border(1).PaddingVertical(2).PaddingHorizontal(10)
                                .AlignMiddle().AlignCenter().Text(datos.Facturacion?.Nit ?? "").FontFamily("Cambria").FontSize(8);

                        });
                    });
                });
            });

            
        }

        void ComposeFooter(IContainer container, OrdenCompraDto datos)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignCenter().Element(e =>
                    {
                        e.Width(200).Height(60).Border(1).BorderColor(Colors.Black)
                         .AlignCenter().AlignMiddle()
                         .Text(datos.ElaboradoPor ?? "").FontSize(8).FontFamily("Cambria");
                    });
                    col.Item().Height(5);
                    col.Item().AlignCenter()
                        .Text("ELABORADO POR").FontSize(9).FontFamily("Cambria");
                });

                row.ConstantItem(30);

                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignCenter().Element(e =>
                    {
                        e.Width(200).Height(60).Border(1).BorderColor(Colors.Black)
                         .AlignCenter().AlignMiddle()
                         .Text(datos.RevisadoPor ?? "").FontSize(8).FontFamily("Cambria");
                    });
                    col.Item().Height(5);
                    col.Item().AlignCenter()
                        .Text("REVISADO POR").FontSize(9).FontFamily("Cambria");
                });

                row.ConstantItem(30);

                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignCenter().Element(e =>
                    {
                        e.Width(200).Height(60).Border(1).BorderColor(Colors.Black)
                         .AlignCenter().AlignMiddle()
                         .Text(datos.AutorizadoPor ?? "").FontSize(8).FontFamily("Cambria");
                    });
                    col.Item().Height(5);
                    col.Item().AlignCenter()
                        .Text("AUTORIZADO POR").FontSize(9).FontFamily("Cambria");
                });
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