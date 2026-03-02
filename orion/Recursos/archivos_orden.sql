-- Tabla para relacionar archivos adjuntos con una orden de compra
CREATE TABLE archivos_orden (
    id INT IDENTITY(1,1) PRIMARY KEY,
    id_orden INT NOT NULL,
    nombre_original VARCHAR(255) NOT NULL,
    nombre_guardado VARCHAR(255) NOT NULL,
    ruta_relativa VARCHAR(500) NOT NULL,
    extension VARCHAR(20) NOT NULL,
    tamano_bytes BIGINT NOT NULL,
    fecha_creacion DATETIME NOT NULL DEFAULT GETDATE(),
    usuario VARCHAR(250) NULL,
    CONSTRAINT FK_archivos_orden_orden_compra FOREIGN KEY (id_orden)
        REFERENCES orden_compra(id) ON DELETE CASCADE
);

CREATE INDEX IX_archivos_orden_id_orden ON archivos_orden(id_orden);
