-- =====================================================================
-- Setup del módulo "Solicitudes" (tiketera simple) para ABM.
-- Ejecutar contra la base de datos de la conexión "CadenaSQL"
-- (appsettings.json -> ConnectionStrings:CadenaSQL).
-- =====================================================================

-- 1) Tabla de solicitudes -------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ftc_solicitudes')
BEGIN
    CREATE TABLE ftc_solicitudes (
        idSolicitud               INT IDENTITY(1,1) PRIMARY KEY,
        idUsuarioCreador          INT NOT NULL,
        asunto                    NVARCHAR(200) NOT NULL,
        mensaje                   NVARCHAR(MAX) NOT NULL,
        estado                    NVARCHAR(30) NOT NULL DEFAULT ('Pendiente'),
        FechaCreacion             DATETIME NOT NULL DEFAULT (GETDATE()),
        FechaCambioEstado         DATETIME NULL,
        idUsuarioActualizoEstado  INT NULL,
        CONSTRAINT FK_solicitudes_creador   FOREIGN KEY (idUsuarioCreador)         REFERENCES ftc_usuario(idUsuario),
        CONSTRAINT FK_solicitudes_actualizo FOREIGN KEY (idUsuarioActualizoEstado) REFERENCES ftc_usuario(idUsuario)
    );
END
GO

-- 2) Entrada de menú para "Administrar Solicitudes" ----------------------
-- Sólo aparece en el sidebar de los roles a los que se les otorgue el
-- permiso (Mantenedores > Roles > Editar Rol > tildar "Administrar Solicitudes").
-- "Mis Solicitudes" NO usa este mecanismo: es un link fijo visible para
-- cualquier usuario logueado (agregado directamente en _Layout.cshtml).
IF NOT EXISTS (SELECT 1 FROM ftc_MENU WHERE CONTROLADOR = 'Solicitudes' AND VISTA = 'Admin')
BEGIN
    -- ID_Menu no es IDENTITY en esta tabla: hay que calcular el próximo valor a mano.
    DECLARE @NuevoIdMenu INT = (SELECT ISNULL(MAX(ID_Menu), 0) + 1 FROM ftc_MENU);

    INSERT INTO ftc_MENU (ID_Menu, NOMBRE_MENU, ICONO, VISTA, CONTROLADOR, Estado)
    VALUES (@NuevoIdMenu, 'Administrar Solicitudes', 'fa-solid fa-ticket', 'Admin', 'Solicitudes', 1);
END
GO

-- NOTA: si ftc_MENU tiene alguna otra columna NOT NULL sin default (por
-- ejemplo un campo de orden), el INSERT de arriba puede fallar: en ese
-- caso agregá esa columna al INSERT con el valor que corresponda.

-- 3) (Opcional) Otorgar el permiso a un rol puntual manualmente ----------
-- Lo normal es hacerlo desde Mantenedores > Editar Rol en la app, pero si
-- preferís hacerlo por SQL directamente, reemplazá @IdRolAdmin:
--
-- DECLARE @IdRolAdmin INT = 1; -- <-- idRol que debe administrar solicitudes
-- INSERT INTO ftc_PermisosMenu (idRol, COD_Menu, PERMITIDO, FECHA_CREACION)
-- SELECT @IdRolAdmin, ID_Menu, 1, GETDATE()
-- FROM ftc_MENU
-- WHERE CONTROLADOR = 'Solicitudes' AND VISTA = 'Admin'
--   AND NOT EXISTS (
--       SELECT 1 FROM ftc_PermisosMenu
--       WHERE idRol = @IdRolAdmin AND COD_Menu = ftc_MENU.ID_Menu
--   );
