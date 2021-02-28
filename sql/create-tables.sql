USE [SteamScrapper]
GO
/****** Object:  Table [dbo].[Apps]    Script Date: 2021-02-23 20:42:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Apps](
	[Id] [int] NOT NULL,
	[UtcDateTimeRecorded] [datetime2](7) NOT NULL,
	[UtcDateTimeLastModified] [datetime2](7) NOT NULL,
	[Title] [nvarchar](max) NOT NULL,
	[BannerUrl] [nvarchar](2048) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Bundles]    Script Date: 2021-02-23 20:42:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Bundles](
	[Id] [int] NOT NULL,
	[UtcDateTimeRecorded] [datetime2](7) NOT NULL,
	[UtcDateTimeLastModified] [datetime2](7) NOT NULL,
	[Title] [nvarchar](max) NOT NULL,
	[BannerUrl] [nvarchar](2048) NULL,
 CONSTRAINT [PK_Bundles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Subs]    Script Date: 2021-02-23 20:42:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Subs](
	[Id] [int] NOT NULL,
	[UtcDateTimeRecorded] [datetime2](7) NOT NULL,
	[UtcDateTimeLastModified] [datetime2](7) NOT NULL,
	[Title] [nvarchar](max) NULL,
 CONSTRAINT [PK_Subs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
