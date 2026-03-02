    using System;
    using System.Collections.Generic;
    using MathNet.Numerics.Distributions;
    using Microsoft.EntityFrameworkCore;

    namespace orion.Models;

    public partial class OrionContext : DbContext
    {
        public OrionContext()
        {
        }

        public OrionContext(DbContextOptions<OrionContext> options)
        : base(options)
        {
        }




        public virtual DbSet<Usuario> Usuarios { get; set; }
        public virtual DbSet<Solicitudes> Solicitudes { get; set; }
        public virtual DbSet<DetalleSolicitudes> DetalleSolicitudes { get; set; }
        public virtual DbSet<ProveProduc> ProveProduc { get; set; }
        public virtual DbSet<OrdenCompra> OrdenCompra { get; set; }
        public virtual DbSet<SolicitudPrecio> SolicitudPrecio { get; set; }
        public virtual DbSet<DetalleSolicitudPrecio> DetalleSolicitudPrecio { get; set; }
        public virtual DbSet<EstadosOrden> EstadosOrden { get; set; }
        public virtual DbSet<HistorialEstadoOrden> HistorialEstadoOrden { get; set; }
        public virtual DbSet<TipoCambioFecha> TipoCambioFecha { get; set; }
        public virtual DbSet<AreaCorrespondencia> AreasCorrespondencia { get; set; }
        public virtual DbSet<ArchivoOrden> ArchivosOrden { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
            => optionsBuilder.UseSqlServer("Data Source=192.168.1.1;Initial Catalog=orion;User=sa1;Password=1237890;TrustServerCertificate=true;");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            
            modelBuilder.Entity<Usuario>(entity =>
            {

                entity.HasKey(e => e.Id); // Clave primaria
                entity.ToTable("usuario");

                entity.Property(e => e.Contraseña)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("contraseña");
                entity.Property(e => e.Estado)
                    .HasMaxLength(1)
                    .IsUnicode(false)
                    .HasColumnName("estado");
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("id");
                entity.Property(e => e.IdTipo)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("id_tipo");
                entity.Property(e => e.Idusuario)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("idusuario");
                entity.Property(e => e.Nombre)
                    .HasMaxLength(300)
                    .IsUnicode(false);
                entity.Property(e => e.NomCompleto)
                    .HasMaxLength(350)
                    .IsUnicode(false)
                    .HasColumnName("nom_completo");
                entity.Property(e => e.Area)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("area");
            });
       
        // Configuración para Solicitudes
        modelBuilder.Entity<Solicitudes>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("solicitudes");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Fecha).HasColumnName("fcreacion");
            entity.Property(e => e.Frequerimiento).HasColumnName("frequerimiento");
            entity.Property(e => e.Referencia)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("referencia");
            entity.Property(e => e.Solicitante)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("solicitante");
        });

        // Configuración para Detalle Solicitudes
        modelBuilder.Entity<DetalleSolicitudes>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("detalle_solicitudes");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdSolicitud).HasColumnName("id_solicitud");
            entity.Property(e => e.Codigo)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("codigo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("descripcion");
            entity.Property(e => e.Proveedor)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("proveedor");
            entity.Property(e => e.Caracteristicas)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("caracteristicas");
            entity.Property(e => e.Unidad)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("unidad");
            entity.Property(e => e.Cantidad)
                    .HasColumnType("decimal(20, 4)")
                    .HasColumnName("cantidad");
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("estado");
            entity.Property(e => e.CodProveedor)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("cod_proveedor");
            entity.Property(e => e.FrequerimientoDias)
                .HasColumnName("frequerimiento_dias");
            entity.Property(e => e.Faprobado).HasColumnName("faprovado");
            entity.Property(e => e.UltimoPrecio)
                    .HasColumnType("decimal(20, 2)")
                    .HasColumnName("ultimo_precio");
            entity.Property(e => e.FultimoPrecio).HasColumnName("f_ultimo_precio");
        });

        // Configuración para Proveedores y Productos
        modelBuilder.Entity<ProveProduc>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("prove_produc");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CodProveedor)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("cod_proveedor");
            entity.Property(e => e.NomProveedor)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("nom_proveedor");
            entity.Property(e => e.CodItem)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("cod_item");
            entity.Property(e => e.NomItem)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("nom_item");
            entity.Property(e => e.Unidad)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("unidad");
            entity.Property(e => e.Precio)
                    .HasColumnType("decimal(20, 2)")
                    .HasColumnName("precio");
            entity.Property(e => e.FultimaCompra).HasColumnName("f_ultima_compra");
            entity.Property(e => e.Contacto)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("contacto");
            entity.Property(e => e.Telefono)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("telefono");
            entity.Property(e => e.Telefono2)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("telefono2");
            entity.Property(e => e.Correo)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("correo");
            entity.Property(e => e.Cuenta)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("cuenta");
            entity.Property(e => e.Banco)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("banco");
            entity.Property(e => e.NomCuenta)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("nom_cuenta");
            entity.Property(e => e.LeadTime)
                .HasColumnName("lead_time");
        });
        // Configuración para OrdenCompra
        modelBuilder.Entity<OrdenCompra>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("orden_compra");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.IdSolicitudPrecio).HasColumnName("id_solicitud_precio");
            entity.Property(e => e.IdEstadoSolicitud).HasColumnName("id_estados_solicitudes");

            entity.Property(e => e.TipoCambio)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("tipo_cambio");

            entity.Property(e => e.Solicitante)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("solicitante");

            entity.Property(e => e.Referencia)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("referencia");

            entity.Property(e => e.Observacion)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("observacion");

            entity.Property(e => e.FormaPago)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("forma_pago");

            entity.Property(e => e.MedioTransporte)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("medio_transporte");

            entity.Property(e => e.ResponsableRecepcion)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("responsable_recepcion");

            entity.Property(e => e.FechaEntrega).HasColumnName("fecha_entrega");

            entity.Property(e => e.LugarEntrega)
                .HasMaxLength(350)
                .IsUnicode(false)
                .HasColumnName("lugar_entrega");

            entity.Property(e => e.FechaAnticipo).HasColumnName("fecha_anticipo");

            entity.Property(e => e.MontoAnticipo)
                .HasColumnType("decimal(20,4)")
                .HasColumnName("monto_anticipo");

            entity.Property(e => e.FechaPagoFinal).HasColumnName("fecha_pago_final");

            entity.Property(e => e.MontoPagoFinal)
                .HasColumnType("decimal(20,4)")
                .HasColumnName("monto_pago_final");

            entity.Property(e => e.Banco)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("banco");

            entity.Property(e => e.Cuenta)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("cuenta");

            entity.Property(e => e.NombreCuentaBancaria)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("nombre_cuenta_bancaria");

            entity.Property(e => e.CodigoSwift)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("codigo_swift");

            entity.Property(e => e.Incoterm)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("incoterm");

            entity.Property(e => e.RazonSocial)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("razon_social");

            entity.Property(e => e.Nit)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("nit");

            entity.Property(e => e.EsImportacion).HasColumnName("es_importacion");

            entity.Property(e => e.Telefono)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("telefono");

            entity.Property(e => e.NomContacto)
                .HasMaxLength(350)
                .IsUnicode(false)
                .HasColumnName("nom_contacto");

            entity.Property(e => e.Aprobador)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("aprobador");

            entity.Property(e => e.IdAreaCorrespondencia).HasColumnName("id_area_correspondencia");

            entity.Property(e => e.CorrespondeAsc)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("corresponde_asc");

            // Relaciones
            entity.HasOne(e => e.SolicitudPrecio)
                .WithMany(s => s.OrdenesCompra)
                .HasForeignKey(e => e.IdSolicitudPrecio)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Estado)
                .WithMany(es => es.OrdenesCompra)
                .HasForeignKey(e => e.IdEstadoSolicitud)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AreaCorrespondencia)
                .WithMany(a => a.OrdenesCompra)
                .HasForeignKey(e => e.IdAreaCorrespondencia)
                .OnDelete(DeleteBehavior.Restrict);

        });

        modelBuilder.Entity<AreaCorrespondencia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("areas_correspondencia");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("nombre");

            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("estado");
        });

        // Configuración para SolicitudPrecio
        modelBuilder.Entity<SolicitudPrecio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("solicitud_precio");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.IdDetalleSolicitud)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("id_detalle_solicitud");
            entity.Property(e => e.IdSolicitudPrecio)
                .HasColumnName("id_solicitud_precio");
            entity.Property(e => e.Precio)
                .HasColumnType("decimal(20,4)")
                .HasColumnName("precio");
            entity.Property(e => e.Cantidad)
                .HasColumnType("decimal(20,4)")
                .HasColumnName("cantidad");
            entity.Property(e => e.EsStock)
                .HasColumnName("es_stock");
        });

        // Configuración para DetalleSolicitudPrecio
        modelBuilder.Entity<DetalleSolicitudPrecio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("detalle_solicitud_precio");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdSolicitudPrecio).HasColumnName("id_solicitud_precio");
            entity.Property(e => e.IdDetalleSolicitud).HasColumnName("id_detalle_solicitud");

            entity.Property(e => e.Precio)
                .HasColumnType("decimal(20,4)")
                .HasColumnName("precio");

            // Relaciones
            entity.HasOne(e => e.SolicitudPrecio)
                .WithMany(s => s.DetallesPrecios)
                .HasForeignKey(e => e.IdSolicitudPrecio)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.DetalleSolicitud)
                .WithMany()
                .HasForeignKey(e => e.IdDetalleSolicitud)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración para EstadosOrden
        modelBuilder.Entity<EstadosOrden>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("estados_ordenes");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Estado)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("estado");

            entity.Property(e => e.Detalle)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("detalle");
        });
        // Configuración para HistorialEstadoOrden
        modelBuilder.Entity<HistorialEstadoOrden>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("historial_estado_orden");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdOrden).HasColumnName("id_orden");
            entity.Property(e => e.IdEstadoAnterior).HasColumnName("id_estado_anterior");
            entity.Property(e => e.IdEstadoNuevo).HasColumnName("id_estado_nuevo");

            entity.Property(e => e.Usuario)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("usuario");

            entity.Property(e => e.FechaCambio)
                .HasColumnName("fecha_cambio")
                .HasDefaultValueSql("GETDATE()");

            // Relaciones
            entity.HasOne(e => e.Orden)
                .WithMany()
                .HasForeignKey(e => e.IdOrden)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.EstadoAnterior)
                .WithMany()
                .HasForeignKey(e => e.IdEstadoAnterior)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EstadoNuevo)
                .WithMany()
                .HasForeignKey(e => e.IdEstadoNuevo)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TipoCambioFecha>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("tipo_cambio_fecha");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.Valor)
                .HasColumnType("decimal(20,4)")
                .HasColumnName("valor");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("estado");
        });

        modelBuilder.Entity<ArchivoOrden>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("archivos_orden");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdOrden).HasColumnName("id_orden");
            entity.Property(e => e.NombreOriginal)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("nombre_original");
            entity.Property(e => e.NombreGuardado)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("nombre_guardado");
            entity.Property(e => e.RutaRelativa)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("ruta_relativa");
            entity.Property(e => e.Extension)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("extension");
            entity.Property(e => e.TamanoBytes).HasColumnName("tamano_bytes");
            entity.Property(e => e.FechaCreacion)
                .HasColumnName("fecha_creacion")
                .HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Usuario)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("usuario");

            entity.HasOne(e => e.Orden)
                .WithMany(o => o.Archivos)
                .HasForeignKey(e => e.IdOrden)
                .OnDelete(DeleteBehavior.Cascade);
        });
     

        OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
