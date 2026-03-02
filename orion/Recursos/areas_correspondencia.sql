CREATE TABLE areas_correspondencia (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(150) NOT NULL,
    estado CHAR(1) NULL
);

ALTER TABLE orden_compra
    ADD id_area_correspondencia INT NULL,
        corresponde_asc VARCHAR(150) NULL;

ALTER TABLE orden_compra
    ADD CONSTRAINT FK_orden_compra_areas_correspondencia
    FOREIGN KEY (id_area_correspondencia) REFERENCES areas_correspondencia(id);

-- Datos iniciales de ejemplo
INSERT INTO areas_correspondencia (nombre, estado)
VALUES
('SISTEMAS', 'A'),
('CONTABILIDAD', 'A'),
('PLANTA ALAMO', 'A');
