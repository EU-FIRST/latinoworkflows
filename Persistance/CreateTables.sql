/*USE [DacqPipeDB]
GO*/

/****** Object:  Table [dbo].[Corpora]    Script Date: 02/06/2011 11:08:48 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Corpora]') AND type in (N'U'))
DROP TABLE [dbo].[Corpora]
GO

/****** Object:  Table [dbo].[Corpora]    Script Date: 02/06/2011 11:08:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Corpora](
	[id] [char](32) NOT NULL,
	[xml] [ntext] NOT NULL,
	[title] [nvarchar](4000) NULL,
	[provider] [varchar](4000) NULL,
	[language] [nvarchar](4000) NULL,
	[sourceUrl] [nvarchar](4000) NULL,
	[source] [ntext] NULL,
	[timeStart] [char](30) NULL,
	[timeEnd] [char](30) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[Documents]    Script Date: 02/06/2011 11:08:56 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Documents]') AND type in (N'U'))
DROP TABLE [dbo].[Documents]
GO

/****** Object:  Table [dbo].[Documents]    Script Date: 02/06/2011 11:08:56 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Documents](
	[id] [char](32) NOT NULL,
	[corpusId] [char](32) NOT NULL,
	[name] [nvarchar](4000) NULL,
	[description] [ntext] NULL,
	[text] [ntext] NULL,
	[category] [nvarchar](4000) NULL,
	[link] [nvarchar](4000) NULL,
	[time] [char](30) NULL,
	[pubDate] [char](30) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[History]    Script Date: 04/06/2011 17:18:12 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[History]') AND type in (N'U'))
DROP TABLE [dbo].[History]
GO

/****** Object:  Table [dbo].[History]    Script Date: 04/06/2011 17:18:12 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[History](
	[SiteId] [nvarchar](400) NULL,
	[ItemId] [char](32) NOT NULL,
	[Source] [varchar](900) NOT NULL,
	[Time] [char](23) NOT NULL
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Indices ******/
CREATE INDEX IX_History
ON History (SiteId ASC, Time ASC)
GO