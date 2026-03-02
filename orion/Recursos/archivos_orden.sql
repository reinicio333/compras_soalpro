-- En este enfoque NO se usa tabla hija.
-- Se agrega una columna JSON a orden_compra para almacenar array de adjuntos.
IF COL_LENGTH('orden_compra', 'rutas_archivos') IS NULL
BEGIN
    ALTER TABLE orden_compra
    ADD rutas_archivos NVARCHAR(MAX) NULL;
END
