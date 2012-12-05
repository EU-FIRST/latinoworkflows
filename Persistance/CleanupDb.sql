-- executed on 22/09/2012
-- remove corpora
delete from Corpora where timeEnd > '2012-09-22 05:30'
-- remove documents that don't hv corpora records
delete from Documents where not exists (select top 1 * from Corpora where Documents.corpusId = Corpora.id)
-- remove corpora that don't hv any documents
delete from Corpora where not exists (select top 1 * from Documents where Documents.corpusId = Corpora.id)
-- remove sources that don't hv document records
delete from Sources where not exists (select top 1 * from Documents join Corpora on Documents.corpusId = Corpora.id and Documents.rejected = Corpora.rejected where Sources.siteId = Corpora.siteId and Sources.docId = Documents.id)
-- remove RSS records that are not referenced from Sources
--delete from RssXml where not exists (select top 1 * from Sources where Sources.xmlHash = RssXml.hash) -- this took too long and was skipped
-- remove text blocks that don't hv document records
delete from TextBlocks where not exists (select top 1 * from Documents where TextBlocks.corpusId = Documents.corpusId and TextBlocks.docId = Documents.id)