/*USE [DacqPipeTmp]
GO*/
/****** Object:  Table [dbo].[Corpora]    Script Date: 04/08/2011 19:46:53 ******/
DROP TABLE [dbo].[Corpora]
GO
/****** Object:  Table [dbo].[Documents]    Script Date: 04/08/2011 19:46:53 ******/
DROP TABLE [dbo].[Documents]
GO
/****** Object:  Table [dbo].[History]    Script Date: 04/08/2011 19:46:53 ******/
DROP TABLE [dbo].[History]
GO
/****** Object:  Table [dbo].[History]    Script Date: 04/08/2011 19:46:53 ******/
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
CREATE NONCLUSTERED INDEX [IX_History] ON [dbo].[History] 
(
	[SiteId] ASC,
	[Time] DESC
) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Documents]    Script Date: 04/08/2011 19:46:53 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Documents](
	[id] [char](32) NOT NULL,
	[corpusId] [char](32) NOT NULL,
	[name] [nvarchar](400) NULL,
	[description] [nvarchar](400) NULL,
/*	[text] [ntext] NULL,*/
	[category] [nvarchar](400) NULL,
	[link] [varchar](400) NULL,
	[time] [char](26) NULL,
	[pubDate] [char](26) NULL,
	[mimeType] [varchar](80) NULL,
	[contentType] [varchar](40) NULL,
	[charSet] [varchar](40) NULL,
	[contentLength] [bigint] NULL
) ON [PRIMARY] /*TEXTIMAGE_ON [PRIMARY]*/
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Corpora]    Script Date: 04/08/2011 19:46:53 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Corpora](
	[id] [char](32) NOT NULL,
/*	[xml] [ntext] NOT NULL,*/
	[title] [nvarchar](400) NULL,
/*	[provider] [varchar](400) NULL,*/
	[language] [nvarchar](400) NULL,
	[sourceUrl] [varchar](400) NULL,
/*	[source] [ntext] NULL,*/
	[timeStart] [char](26) NULL,
	[timeEnd] [char](26) NULL
) ON [PRIMARY] /*TEXTIMAGE_ON [PRIMARY]*/
GO
SET ANSI_PADDING OFF
GO