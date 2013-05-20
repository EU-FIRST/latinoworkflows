/****** Object:  Table [dbo].[Documents]    Script Date: 05/09/2013 13:30:57 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Documents]') AND type in (N'U'))
DROP TABLE [dbo].[Documents]
GO

/****** Object:  Table [dbo].[Documents]    Script Date: 05/09/2013 13:30:57 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Documents](
	[id] [uniqueidentifier] NULL,
	[name] [nvarchar](400) NULL,
	[description] [nvarchar](400) NULL,
	[category] [nvarchar](400) NULL,
	[link] [varchar](400) NULL,
	[responseUrl] [varchar](400) NULL,
	[urlKey] [varchar](400) NULL,
	[acqTime] [datetime] NULL,
	[pubTimeStr] [char](100) NULL,
	[mimeType] [varchar](80) NULL,
	[charSet] [varchar](40) NULL,
	[contentLength] [int] NULL,
	[domain] [varchar](100) NULL,
	[bpCharCount] [int] NULL,
	[contentCharCount] [int] NULL,
	[unseenContentCharCount] [int] NULL,
	[rev] [int] NULL,
	[fileName] [varchar](100) NULL,
	[oldIdCorpus] [uniqueidentifier] NULL,
	[oldIdDocument] [uniqueidentifier] NULL,
	CONSTRAINT [UQ_Documents_id] UNIQUE NONCLUSTERED 
	(
		[id] ASC
	) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = ON, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO