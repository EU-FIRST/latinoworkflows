using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

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
            X x = new X();
            A a = new A();
            B b = new B();
            C c = new C();
            D d = new D();
            Y y = new Y();

            x.Subscribe(a);
            a.Subscribe(b);
            b.Subscribe(y);

            x.Subscribe(c);
            c.Subscribe(d);
            d.Subscribe(y);

            x.Start();
            Console.ReadLine();
            Console.WriteLine("stop");
            x.GracefulStop();
            Console.ReadLine();
        }
    }
}
