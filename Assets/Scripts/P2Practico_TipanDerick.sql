------------------------------------------------------------------------
-- UNIVERSIDAD DE LAS AMÉRICAS
-- FACULTAD DE INGENIERÍA Y CIENCIAS APLICADAS
-- EVALUACIÓN PRÁCTICA - PROGRESO II (BBDD II)
-- NRC: 5482
-- Elaborado por: Derick Vinicio Tipan Lema
-- ID BANNER: A00XXXXXX -- Por favor reemplace las X con su ID de Banner real
-- Dirigido a: Ing. Geovanni Aucancela Soliz
------------------------------------------------------------------------

USE RentingPC5482;
GO

------------------------------------------------------------------------
-- PREGUNTA 1: STORED PROCEDURE DE CONSULTA DE PRÉSTAMOS
------------------------------------------------------------------------
-- Objetivo: Crear un Stored Procedure que muestre los equipos arrendados por un Usuario.
-- 1. Nombre: Reportes.spconsultaprestamo_equipo
-- 2. Recibe como parámetro: @IDUsuario INT
-- 3. Muestra la información relacionada de las 3 tablas.
-- 4. Utiliza los alias: IDUsuario, Nombre, Apellido, IDPrestamo, Fecha, Equipo, Ndias, Valor.

-- Asegurar que el esquema "Reportes" exista en la base de datos
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Reportes')
BEGIN
    EXEC('CREATE SCHEMA Reportes');
END;
GO

-- Crear o modificar el Stored Procedure
CREATE OR ALTER PROCEDURE Reportes.spconsultaprestamo_equipo
    @IDUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Consulta relacionando GENERAL.Usuarios, TRANSACCION.Prestamos y GENERAL.Equipos
    SELECT 
        u.IDUsuario AS IDUsuario,
        u.NombreUsuario AS Nombre,
        u.ApellidoUsuario AS Apellido,
        p.IDPrestamo AS IDPrestamo,
        p.FechaPrestamo AS Fecha,
        e.NombreEquipo AS Equipo,
        p.NumeroDias AS Ndias,
        p.ValorTotal AS Valor
    FROM TRANSACCION.Prestamos p
    INNER JOIN GENERAL.Usuarios u ON p.IDUsuario = u.IDUsuario
    INNER JOIN GENERAL.Equipos e ON p.IDEquipo = e.IDEquipo
    WHERE u.IDUsuario = @IDUsuario;
END;
GO

-- 1.2 Ejecución del Stored Procedure de prueba para el IDUsuario 1
-- Muestra la información formateada y con alias correctos para Juan Pérez.
EXEC Reportes.spconsultaprestamo_equipo @IDUsuario = 1;
GO


------------------------------------------------------------------------
-- PREGUNTA 2: CONTROL DE ACCESO A DATOS (DCL)
------------------------------------------------------------------------

-- 2.1 Crear un login Administrador de Base de Datos con acceso total (equivalente a 'sa')
-- Nombre: saadmin5482, Clave: ApellidosEstudiante (TipanLema)
USE master;
GO

-- Si ya existe, se limpia para evitar conflictos
IF EXISTS (SELECT * FROM sys.server_principals WHERE name = 'saadmin5482')
BEGIN
    DROP LOGIN saadmin5482;
END;
GO

-- Creamos el login. CHECK_POLICY = OFF desactiva reglas locales de complejidad
CREATE LOGIN saadmin5482 WITH PASSWORD = 'TipanLema', CHECK_EXPIRATION = OFF, CHECK_POLICY = OFF;
GO
-- Asignar el rol sysadmin para otorgar acceso y control total (igual a sa)
ALTER SERVER ROLE sysadmin ADD MEMBER saadmin5482;
GO


-- 2.2 Crear un Login con permisos por defecto y su usuario de base de datos
-- Nombre: user5482, Clave: ApellidosEstudiante (TipanLema)
IF EXISTS (SELECT * FROM sys.server_principals WHERE name = 'user5482')
BEGIN
    USE RentingPC5482;
    IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'user5482')
    BEGIN
        DROP USER user5482;
    END;
    USE master;
    DROP LOGIN user5482;
END;
GO

CREATE LOGIN user5482 WITH PASSWORD = 'TipanLema', CHECK_EXPIRATION = OFF, CHECK_POLICY = OFF;
GO

USE RentingPC5482;
GO
CREATE USER user5482 FOR LOGIN user5482;
GO


-- 2.3 Asignación de Permisos al usuario "user5482"
-- Permiso de ejecución al Stored Procedure creado en la Pregunta 1
GRANT EXECUTE ON Reportes.spconsultaprestamo_equipo TO user5482;
GO

-- Permiso de INSERT, UPDATE, DELETE a la tabla GENERAL.Equipos
GRANT INSERT, UPDATE, DELETE ON GENERAL.Equipos TO user5482;
GO

-- VALIDACIÓN: Prueba de restricción de consulta SELECT
-- El usuario "user5482" solo tiene permisos de INSERT, UPDATE y DELETE en GENERAL.Equipos,
-- pero no tiene permisos de SELECT. Se realiza impersonación para validar el fallo de permisos.
EXECUTE AS USER = 'user5482';
GO

-- Esta sentencia debe fallar con error: "The SELECT permission was denied on the object 'Equipos'..."
SELECT * FROM GENERAL.Equipos;
GO

-- Revertir la impersonación de seguridad para regresar a la sesión original
REVERT;
GO


------------------------------------------------------------------------
-- PREGUNTA 3: GENERAR ARCHIVO JSON ANIDADO DESDE SQL SERVER
------------------------------------------------------------------------

-- 3.1 Consulta SQL con FOR JSON PATH que genera los Equipos alquilados por cada usuario
-- Estructura anidada: ID, Nombre, Apellido y la colección de Equipos con sus préstamos.
SELECT 
    u.IDUsuario AS ID,
    u.NombreUsuario AS Nombre,
    u.ApellidoUsuario AS Apellido,
    (
        SELECT 
            p.IDPrestamo AS IDPrestamo,
            p.FechaPrestamo AS Fecha,
            e.NombreEquipo AS Equipo,
            p.NumeroDias AS Ndias,
            p.ValorTotal AS Valor
        FROM TRANSACCION.Prestamos p
        INNER JOIN GENERAL.Equipos e ON p.IDEquipo = e.IDEquipo
        WHERE p.IDUsuario = u.IDUsuario
        FOR JSON PATH
    ) AS Equipos
FROM GENERAL.Usuarios u
WHERE EXISTS (
    SELECT 1 FROM TRANSACCION.Prestamos p WHERE p.IDUsuario = u.IDUsuario
)
FOR JSON PATH;
GO
