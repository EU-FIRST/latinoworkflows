/****** Object:  Table [dbo].[Corpora]    Script Date: 04/08/2011 19:46:53 ******/
DROP TABLE [dbo].[Corpora]
GO
/****** Object:  Table [dbo].[Documents]    Script Date: 04/08/2011 19:46:53 ******/
DROP TABLE [dbo].[Documents]
GO
/****** Object:  Table [dbo].[TextBlocks]    Script Date: 04/02/2012 19:39:36 ******/
DROP TABLE [dbo].[TextBlocks]
GO
/****** Object:  Table [dbo].[TextBlocks]    Script Date: 04/02/2012 19:39:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[TextBlocks](
	[id] [char](32) NOT NULL,
	[hashCodes] [text] NOT NULL,
	[queue] [tinyint] NOT NULL,
	[time] [char](26) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
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
	[category] [nvarchar](400) NULL,
	[link] [varchar](400) NULL,
	[responseUrl] [varchar](400) NULL,
	[urlKey] [varchar](400) NULL,
	[time] [char](26) NULL,
	[pubDate] [char](26) NULL,
	[mimeType] [varchar](80) NULL,
	[contentType] [varchar](40) NULL,
	[charSet] [varchar](40) NULL,
	[contentLength] [bigint] NULL,
	[detectedLanguage] [nvarchar](400) NULL,
	[dump] [bit] NOT NULL
) ON [PRIMARY] 
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
	[title] [nvarchar](400) NULL,
	[language] [nvarchar](400) NULL,
	[sourceUrl] [varchar](400) NULL,
	[timeStart] [char](26) NULL,
	[timeEnd] [char](26) NULL,
	[siteId] [nvarchar](400) NULL,
	[dump] [bit] NOT NULL
) ON [PRIMARY] 
GO
SET ANSI_PADDING OFF
GO