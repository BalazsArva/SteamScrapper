USE [SteamScrapper]
GO
SELECT
	SUM([All].[AppCount]) AS [AppCount],
	SUM([All].[SubCount]) AS [SubCount],
	SUM([All].[BundleCount]) AS [BundleCount]
FROM (
	SELECT COUNT([A].[Id]) AS [AppCount],               0 AS [SubCount],               0 AS [BundleCount] FROM [dbo].[Apps] AS [A] UNION
	SELECT               0 AS [AppCount], COUNT([S].[Id]) AS [SubCount],               0 AS [BundleCount] FROM [dbo].[Subs] AS [S] UNION
	SELECT               0 AS [AppCount],               0 AS [SubCount], COUNT([B].[Id]) AS [BundleCount] FROM [dbo].[Bundles] AS [B]
) AS [All]