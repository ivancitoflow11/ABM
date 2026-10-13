-- =====================================================================
-- Stored procedures usados por la búsqueda de "Consulta Usuario" en ABM.
-- Base de datos: ABM_FTC (servidor DESKTOP-QE2DJ87)
-- Extraídos con: SELECT OBJECT_DEFINITION(OBJECT_ID('<nombre_sp>'))
-- =====================================================================

-- =====================================================================
-- 1) CSS_DatosBasicosUsuario
-- Uso: búsqueda principal por RUT / correo / nombre (LIKE '%input%').
-- Combina 2 fuentes con UNION ALL: alertas de finiquitados en AD (sólo
-- Chile) y la vista general de activos agrupados (v_agrupa_activos).
-- =====================================================================
CREATE PROCEDURE [dbo].[CSS_DatosBasicosUsuario]
    @input VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rutdni, dv, nombreusuario, mailusuario, cargospr, ESTADO, pais, negocio, sistema, NegocioPais
    FROM (
        SELECT
            [rut] COLLATE DATABASE_DEFAULT AS rutdni,
            '' COLLATE DATABASE_DEFAULT AS dv,
            [NOMBRE] COLLATE DATABASE_DEFAULT AS nombreusuario,
            mail COLLATE DATABASE_DEFAULT AS mailusuario,
            '' COLLATE DATABASE_DEFAULT AS cargospr,
            'FINIQUITADO' COLLATE DATABASE_DEFAULT AS ESTADO,
            [pais] COLLATE DATABASE_DEFAULT AS pais,
            a.[negocio] COLLATE DATABASE_DEFAULT AS negocio,
            'ACTIVE DIRECTORY' COLLATE DATABASE_DEFAULT AS sistema,
            (a.[negocio] + ' - ' + pais) COLLATE DATABASE_DEFAULT AS NegocioPais
        FROM [Abm_APP].dbo.alerta_sistemas_ad a
        LEFT OUTER JOIN [Abm_APP].dbo.negocio_2 b ON a.negocio = b.negocio
        WHERE pais = 'chile'
          AND tipo = 'usuario'
          AND finiquitado = 1
          AND vertical <> 'FIF'
          AND (
              rut LIKE '%' + @input + '%'
              OR mail LIKE '%' + @input + '%'
              OR nombre COLLATE Latin1_General_CI_AI LIKE '%' + @input + '%' COLLATE Latin1_General_CI_AI
          )

        UNION ALL

        SELECT
            A.rutdni COLLATE DATABASE_DEFAULT,
            A.dv COLLATE DATABASE_DEFAULT,
            A.nombreusuario COLLATE DATABASE_DEFAULT,
            A.mailusuario COLLATE DATABASE_DEFAULT,
            A.cargospr COLLATE DATABASE_DEFAULT,
            ISNULL(A.estado, 'N/A') COLLATE DATABASE_DEFAULT AS estado,
            B.pais COLLATE DATABASE_DEFAULT,
            B.negocio COLLATE DATABASE_DEFAULT,
            ISNULL(B.sistema, 'Sin Sistema') COLLATE DATABASE_DEFAULT AS sistema,
            (B.negocio + ' - ' + B.pais) COLLATE DATABASE_DEFAULT AS NegocioPais
        FROM v_agrupa_activos A
        INNER JOIN [ABM_FTC].[dbo].[ftc_pns_2] B ON A.idPaisNegocioSistema = B.idPaisNegocioSistema
        WHERE (
            A.rutdni LIKE '%' + @input + '%'
            OR A.mailusuario LIKE '%' + @input + '%'
            OR A.nombreusuario COLLATE Latin1_General_CI_AI LIKE '%' + @input + '%' COLLATE Latin1_General_CI_AI
        )
    ) z
END
GO

-- =====================================================================
-- 2) CSS_DatosEstadoActivo
-- Uso: determina si el usuario está activo como SPR o Empleado Central.
-- =====================================================================
CREATE PROCEDURE [dbo].[CSS_DatosEstadoActivo]
    @input VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        RUT,
        NOMBRES + ' ' + APEPATERNO + ' ' + APEMATERNO AS NOMBRE,
        CORREO,
        CASE WHEN CCOSTO = 'Empleado Central' THEN 'Empleado Central' ELSE 'SPR' END AS ORIGEN
    FROM ftc_activos_falanet
    WHERE RUT = @input
       OR CORREO = @input
       OR ISNULL(ISNULL(CORREO, RUT), NOMBRES + ' ' + APEPATERNO + ' ' + APEMATERNO) LIKE '%' + @input + '%';
END
GO

-- =====================================================================
-- 3) CSS_DatosEstadoFiniquitado
-- Uso: obtiene la fecha de finiquito de un RUT.
-- =====================================================================
CREATE PROCEDURE [dbo].[CSS_DatosEstadoFiniquitado]
    @input NVARCHAR(100)
AS
BEGIN
    SELECT TOP 1
        RUTDNI,
        fecfiniquito
    FROM ftc_finiquitados_general
    WHERE RUTDNI LIKE '%' + @input + '%'
END
GO

-- =====================================================================
-- 4) CSS_DatosSistemas
-- Uso: lista de sistemas/negocios asignados al usuario (match exacto
-- por RUT o correo; LIKE sólo si se busca por nombre).
-- =====================================================================
CREATE PROCEDURE [dbo].[CSS_DatosSistemas]
    @input VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        A.rutdni,
        A.dv,
        A.nombreusuario,
        A.mailusuario,
        A.cargospr,
        ISNULL(A.estado, 'N/A') AS estado,
        B.pais,
        B.negocio,
        ISNULL(B.sistema, 'Sin Sistema') AS sistema,
        (B.negocio + ' - ' + B.pais) AS NegocioPais
    FROM v_agrupa_activos A
    INNER JOIN [ABM_FTC].[dbo].[ftc_pns_2] B ON A.idPaisNegocioSistema = B.idPaisNegocioSistema
    WHERE (
        A.rutdni = @input
        OR A.mailusuario = @input
        OR A.nombreusuario LIKE '%' + @input + '%'
    )
END
GO

-- =====================================================================
-- 5) CSS_DatosUltimoLoginAD
-- Uso: último login en Active Directory (match exacto por correo o rut).
-- =====================================================================
CREATE PROCEDURE [dbo].[CSS_DatosUltimoLoginAD]
    @correo NVARCHAR(100),
    @rut NVARCHAR(100)
AS
BEGIN
    SELECT TOP 1
        dbo.fn_FormatoTiempo(ISNULL(CAST(lastLogon AS BIGINT), 0), 0) AS ultimo_login
    FROM ftc_ad
    WHERE (MAIL = @correo AND @correo IS NOT NULL AND @correo <> '')
       OR (employeeID = @rut AND @rut IS NOT NULL AND @rut <> '')
END
GO
