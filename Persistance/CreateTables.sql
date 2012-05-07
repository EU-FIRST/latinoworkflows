/****** Object:  Table [dbo].[Corpora] ******/
DROP TABLE [dbo].[Corpora]
GO
/****** Object:  Table [dbo].[Documents] ******/
DROP TABLE [dbo].[Documents]
GO
/****** Object:  Table [dbo].[TextBlocks] ******/
DROP TABLE [dbo].[TextBlocks]
GO
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
	[urlKey] [varchar](400) NULL,
	[time] [char](26) NULL,
	[pubDate] [char](26) NULL,
	[mimeType] [varchar](80) NULL,
	[contentType] [varchar](40) NULL,
	[charSet] [varchar](40) NULL,
	[contentLength] [int] NULL,
	[detectedLanguage] [nvarchar](100) NULL,
	[detectedCharRange] [nvarchar](100) NULL,
	[rejected] [bit] NOT NULL,
	[duplicate] [varchar](10) NULL,
	[domain] [varchar](100) NULL,
	[bpCharCount] [int] NULL,
	[contentCharCount] [int] NULL,
	[modifiedContentCharCount] [int] NULL
) ON [PRIMARY] 
GO
SET ANSI_PADDING OFF
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