using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Latino.Workflows.TextMining;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;

namespace Latino.Workflows
{
    class X : StreamDataProducer
    {
        protected override object ProduceData()
        {
            Thread.Sleep(1000);
            return "";
        }
    }

    class A : StreamDataProcessor
    {
        protected override object ProcessData(IDataProducer sender, object data)
        {
            Thread.Sleep(1000);
            return (string)data + "A";
        }
    }

    class B : StreamDataProcessor
    {
        protected override object ProcessData(IDataProducer sender, object data)
        {
            Thread.Sleep(2000);
            return (string)data + "B";
        }
    }

    class C : StreamDataProcessor
    {
        protected override object ProcessData(IDataProducer sender, object data)
        {
            Thread.Sleep(3000);
            return (string)data + "C";
        }
    }

    class D : StreamDataProcessor
    {
        protected override object ProcessData(IDataProducer sender, object data)
        {
            Thread.Sleep(4000);
            return (string)data + "D";
        }
    }

    class Y : StreamDataConsumer
    {
        protected override void ConsumeData(IDataProducer sender, object data)
        {
            Console.WriteLine((string)data);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("hello worlds!");
            //X x = new X();
            //A a = new A();
            //B b = new B();
            //C c = new C();
            //D d = new D();
            //GenericStreamDataConsumer gsdc = new GenericStreamDataConsumer();
            //gsdc.OnConsumeData += delegate(IDataProducer sender, object data) 
            //{
            //    Console.WriteLine((string)data);
            //};
            //Y y = new Y();

            //x.Subscribe(a);
            //a.Subscribe(b);
            //b.Subscribe(gsdc);

            //x.Subscribe(c);
            //c.Subscribe(d);
            //d.Subscribe(gsdc);

            //x.Start();
            //Console.ReadLine();
            //Console.WriteLine("stop");
            //x.GracefulStop();
            //Console.ReadLine();

            //DocumentCorpus corpus = new DocumentCorpus();
            //Document doc = new Document("This is a very short document. This is some boilerplate.");
            //corpus.Add(doc);
            //Annotation annot = new Annotation(0, 29, "content_block");
            ////doc.AddAnnotation(annot);
            //RegexTokenizerComponent tok = new RegexTokenizerComponent();
            //tok.ReceiveData(null, corpus);

            //Regex mCharsetRegex
            //    = new Regex(@"((charset)|(encoding))\s*=\s*(([""'](?<enc>[^""']+)[""'])|((?<enc>[^\s>""']+)))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            //Console.WriteLine(mCharsetRegex.Match(@"<?xml version=""1.0"" encoding=""ISO-8859-1""?>").Success);

            //RssFeedComponent rss = new RssFeedComponent(@"http://feeds.abcnews.com/abcnews/moneyheadlines");
            //rss.Start();

            Document doc = new Document("name", "bla bla");
            Document doc2 = new Document("name2", "bla bla 2");
            doc.AddAnnotation(new Annotation(0, 100, "waka waka"));
            StringWriter sw;
            XmlTextWriter writer = new XmlTextWriter(sw = new StringWriter());
            DocumentCorpus c = new DocumentCorpus();
            c.AddDocument(doc);
            c.AddDocument(doc2);
            c.WriteXml(writer);
            Console.WriteLine(sw);
        }
    }
}
