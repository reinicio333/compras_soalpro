CREATE TABLE tipo_cambio_fecha (
    id INT IDENTITY(1,1) PRIMARY KEY,
    fecha_inicio DATE NOT NULL,
    fecha_fin DATE NOT NULL,
    valor DECIMAL(20,4) NOT NULL,
    estado VARCHAR(1) NOT NULL DEFAULT '1'
);

INSERT INTO tipo_cambio_fecha (fecha_inicio, fecha_fin, valor, estado)
VALUES ('1900-01-01', '1900-01-01', 6.9600, '0');
