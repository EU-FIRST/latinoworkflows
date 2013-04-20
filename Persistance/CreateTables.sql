-- DROP TABLES

/****** Object:  Table [dbo].[Corpora] ******/
DROP TABLE [dbo].[Corpora]
GO
/****** Object:  Table [dbo].[Documents] ******/
DROP TABLE [dbo].[Documents]
GO
/****** Object:  Table [dbo].[TextBlocks] ******/
DROP TABLE [dbo].[TextBlocks]
GO
/****** Object:  Table [dbo].[Sources] ******/
DROP TABLE [dbo].[Sources]
GO
/****** Object:  Table [dbo].[RssXml] ******/
DROP TABLE [dbo].[RssXml]
GO

-- CREATE TABLES

/****** Object:  Table [dbo].[TextBlocks] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[TextBlocks](
	[corpusId] [char](32) NOT NULL,
	[docId] [char](32) NOT NULL,
	[hashCodes] [text] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
CREATE NONCLUSTERED INDEX [corpusId_docId] ON [dbo].[TextBlocks] 
(
	[corpusId] ASC,
	[docId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Documents] ******/
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
	[category] [nvarchar](400) NULL,
	[link] [varchar](400) NULL,
	[responseUrl] [varchar](400) NULL,
	[urlKey] [varchar](400) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
	[time] [char](26) NULL,
	[pubDate] [char](26) NULL,
	[mimeType] [varchar](80) NULL,
	[contentType] [varchar](40) NULL,
	[charSet] [varchar](40) NULL,
	[contentLength] [int] NULL,
	[detectedLanguage] [nvarchar](100) NULL,
	[detectedCharRange] [nvarchar](100) NULL,
	[rejected] [bit] NOT NULL,
	[unseenContent] [varchar](3) NULL,
	[domain] [varchar](100) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
	[bpCharCount] [int] NULL,
	[contentCharCount] [int] NULL,
	[unseenContentCharCount] [int] NULL,
	[rev] [int] NULL
) ON [PRIMARY] 
GO
SET ANSI_PADDING OFF
GO
CREATE NONCLUSTERED INDEX [domain_time] ON [dbo].[Documents] 
(
	[time] ASC,
	[domain] DESC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [urlKey_time_rev] ON [dbo].[Documents] 
(
	[urlKey] ASC,
	[time] DESC,
	[rev] DESC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Corpora] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Corpora](
	[id] [char](32) NOT NULL,
	[title] [nvarchar](400) NULL,
	[language] [nvarchar](100) NULL,
	[sourceUrl] [varchar](400) NULL,
	[timeStart] [char](26) NULL,
	[timeEnd] [char](26) NULL,
	[siteId] [nvarchar](100) NULL,
	[rejected] [bit] NOT NULL
) ON [PRIMARY] 
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Sources] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Sources](
	[siteId] [nvarchar](100) NOT NULL,
	[docId] [char](32) NOT NULL,
	[sourceUrl] [varchar](400) NOT NULL,
	[category] [ntext] NULL,
	[entities] [ntext] NULL,
	[xmlHash] [char](32) NOT NULL
	constraint UQ_Sources unique (siteId, docId, sourceUrl) with (ignore_dup_key = on)
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
CREATE NONCLUSTERED INDEX [siteId_docId_sourceUrl] ON [dbo].[Sources] 
(
	[siteId] ASC,
	[docId] ASC,
	[sourceUrl] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RssXml] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[RssXml](
	[hash] [char](32) NOT NULL,
	[xml] [ntext] NOT NULL
	constraint UQ_RssXml unique (hash) with (ignore_dup_key = on)
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
CREATE NONCLUSTERED INDEX [hash] ON [dbo].[RssXml] 
(
	[hash] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO