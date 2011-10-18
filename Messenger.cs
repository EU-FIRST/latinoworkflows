using System;
using System.Collections.Generic;

using System.Text;
using ZMQ;
using System.Threading;
using System.Collections;
using System.Configuration;
using log4net;
using log4net.Config;

using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using System.IO;

namespace Messaging
{
    public class Messenger
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(Messenger));

        //queue variables
        private BlockingQueue<String> zeromqQueue; //keeps messages will be sent via zeromq
        private BlockingQueue<String> loggerQueue; //keeps messages will be sent via zeromq
        private BlockingQueue<String> brokerQueue; //keeps messages will be sent via broker
        private BlockingQueue<String> fileQueue; //keeps file names

        //file storage variables
        private int fileNum = 1; //keeps file counter
        private String fileStorageAddress; //file storage folder
        private int maxFileStorageNum; //max number of messages can be stored in files

        //zeromq variables
        private Socket zeromqSender; //connection to send messages
        private Socket zeromqWP4Subscriber; //connection to receive overflow messages
        private Context zeromqContext;

        //channel for logging output (DB_LOGGING)
        private Socket loggerEmitter;

        //activemq variables
        private IMessageProducer activemqSender;
        private Apache.NMS.IConnection activeMQconnection;

        //zeromq balancing commands
        private static String WAIT_COMMAND = "WP3_WAIT";
        private static String FINISH_COMMAND = "WP4_FINISH";
        private static String CONTINUE_COMMAND = "WP3_CONTINUE";
        private static String MESSAGE_REQUEST = "WP4_R";

        //zeromq pipeline or request & reply
        private static int MESSAGING_TYPE = 1;
        private const int PIPELINE = 0;
        private const int REQ_REP = 1;

        private static int IGNORE_QUEUE_OVERFLOW = 1;
        private static int MAX_QUEUE_SIZE = 10;
        private static int MAX_BROKER_QUEUE_SIZE = 4;

        //broker variables
        private static int BROKER = 0;
        private const int NONE = 0;
        private const int ACTIVEMQ = 1;

        private const int ON = 1;
        private const int OFF = 0;

        private Thread zerommqThread;
        private Thread loggerThread;
        private Thread brokerThread;
        private Thread fileThread;

        private bool messagingFinished = false;

        //logging flag
        private static bool DB_LOGGING = true;

        public Messenger()
        {
            initLogger();

            //zeromq opening connections
            zeromqQueue = new BlockingQueue<String>();
            brokerQueue = new BlockingQueue<String>();
            fileQueue = new BlockingQueue<String>();
            loggerQueue = new BlockingQueue<String>();
            zeromqContext = new Context(1);
            //reading parameters from the configuration file
            MESSAGING_TYPE = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MessagingType"));
            MAX_QUEUE_SIZE = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MAX_QUEUE_SIZE"));
            IGNORE_QUEUE_OVERFLOW = Convert.ToInt32(ConfigurationManager.AppSettings.Get("IGNORE_QUEUE_OVERFLOW"));
            BROKER = Convert.ToInt32(ConfigurationManager.AppSettings.Get("Broker"));
            String addressSend = ConfigurationManager.AppSettings.Get("WP4MessageAddress");
            fileStorageAddress = ConfigurationManager.AppSettings.Get("FileStorageAddress");
            maxFileStorageNum = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MAX_FILE_STORAGE_SIZE"));
            WAIT_COMMAND = ConfigurationManager.AppSettings.Get("WAIT_COMMAND");
            FINISH_COMMAND = ConfigurationManager.AppSettings.Get("FINISH_COMMAND");
            CONTINUE_COMMAND = ConfigurationManager.AppSettings.Get("CONTINUE_COMMAND");
            MESSAGE_REQUEST = ConfigurationManager.AppSettings.Get("MESSAGE_REQUEST");

            DB_LOGGING = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("DB_LOGGING"));
            //logging receiving socket
            if (DB_LOGGING == true)
            {
                String loggingDestinationPort = ConfigurationManager.AppSettings.Get("DBLoggingReceiver");
                loggerEmitter = zeromqContext.Socket(SocketType.PUSH);
                loggerEmitter.Bind(loggingDestinationPort);
                loggerThread = new Thread(new ThreadStart(this.LoggerThreadRun));
                loggerThread.Start();
            }

            switch (MESSAGING_TYPE)
            {
                case PIPELINE:
                    //  Socket to send messages on
                    zeromqSender = zeromqContext.Socket(SocketType.PUSH);
                    //HWM doesnt work to limit zeromq queue size
                    //sender.SetSockOpt(SocketOpt.HWM, 10);
                    zeromqSender.Bind(addressSend);
                    // to receive continue and wait messages from WP4
                    String wp4SubscriberAddress = ConfigurationManager.AppSettings.Get("WP4SubscriberAddress");
                    zeromqWP4Subscriber = zeromqContext.Socket(SocketType.SUB);
                    zeromqWP4Subscriber.Bind(wp4SubscriberAddress);
                    zeromqWP4Subscriber.Subscribe(ConfigurationManager.AppSettings.Get("WP4_COMMAND_FILTER"), Encoding.UTF8);
                    break;
                case REQ_REP:
                    //  Socket to send messages on
                    zeromqSender = zeromqContext.Socket(SocketType.REP);
                    zeromqSender.Bind(addressSend);
                    break;
            }

            //starts zeromq messaging thread
            zerommqThread = new Thread(new ThreadStart(this.ZeromqThreadRun));
            zerommqThread.Start();

            if (IGNORE_QUEUE_OVERFLOW == OFF)
            {
                //Enables file storage for handling data peaks
                fileThread = new Thread(new ThreadStart(this.FileThreadRun));
                //Reads previously written messages if they are not sent to WP4
                readOldMessageFiles();
                fileThread.Start();
            }
            if (BROKER == ACTIVEMQ)
            {
                try
                {
                    //activemq opening connections
                    Apache.NMS.ActiveMQ.ConnectionFactory factory = new Apache.NMS.ActiveMQ.ConnectionFactory(ConfigurationManager.AppSettings.Get("ACTIVEMQ"));
                    activeMQconnection = factory.CreateConnection();
                    Session session = activeMQconnection.CreateSession(AcknowledgementMode.AutoAcknowledge) as Session;
                    IDestination bqueue = session.GetQueue(ConfigurationManager.AppSettings.Get("QueueName"));
                    activemqSender = session.CreateProducer(bqueue);

                    brokerThread = new Thread(new ThreadStart(this.ActiveMQBrokerThreadRun));
                    brokerThread.Start();
                }
                catch (System.Exception e)
                {
                    //     IGNORE_QUEUE_OVERFLOW = 1;
                    BROKER = NONE;
                    logger.Error(e);
                }
            }
        }

        private void initLogger()
        {
            DOMConfigurator.Configure();
        }

        /*
         * method finishes messaging
         */
        public void stopMessaging()
        {
            messagingFinished = true;
        }

        /*
         * methods gets messages from WP3 and puts into messaging queue
         */
        public void sendMessage(String message)
        {
            //sends message with zeromq
            if (zeromqQueue.Count < Messenger.MAX_QUEUE_SIZE)
            {
                zeromqQueue.Enqueue(message);
            }
            else if (IGNORE_QUEUE_OVERFLOW == OFF)
            {
                //sends message with the broker
                if (BROKER != NONE)
                {
                    try
                    {
                        if (brokerQueue.Count < Messenger.MAX_BROKER_QUEUE_SIZE)
                        {
                            brokerQueue.Enqueue(message);
                        }
                        else
                        {
                            logger.Debug("Message ignored");
                        }
                        //   producer.Send(message);
                    }
                    catch (System.Exception e)
                    {
                        //disables broker type messaging
                        IGNORE_QUEUE_OVERFLOW = ON;
                        BROKER = NONE;
                        logger.Error(e);
                    }
                }
                else
                {
                    writeMessageToFile(message);

                    //keeps WP3
                    //lock (zeromqQueue)
                    //{
                    //    while (zeromqQueue.Count > Messenger.MAX_QUEUE_SIZE)
                    //    {
                    //        Monitor.Wait(zeromqQueue);
                    //    }
                    //    zeromqQueue.Enqueue(message);
                    //}
                }

            }
            //ignore the message
            else
            {
                logger.Debug("Message ignored");
            }

            Thread.Sleep(1);
        }

        /*
         * Activemq messaging thread
         */
        public void ActiveMQBrokerThreadRun()
        {
            while (true)
            {
                try
                {
                    String value = (String)brokerQueue.Dequeue();
                    //activemqSender.Send(value);
                    logger.Debug("Message is sent with activemq");
                    Thread.Sleep(1);
                    if (messagingFinished && brokerQueue.Count == 0)
                    {
                        activeMQconnection.Close();
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    //disables broker type messaging
                    BROKER = NONE;
                    logger.Error(e);
                    brokerQueue.Clear();
                    activeMQconnection.Close();
                    return;
                }
            }
        }

        /*
        * Logger thread
        */
        public void LoggerThreadRun()
        {
            while (true)
            {
                try
                {
                    String value = (String)loggerQueue.Dequeue();
                    loggerEmitter.Send(value, Encoding.UTF8, SendRecvOpt.NONE);
                    logger.Debug("Message is sent for logging");
                    Thread.Sleep(1);
                    if (messagingFinished && loggerQueue.Count == 0)
                    {
                        loggerEmitter.Send(FINISH_COMMAND, Encoding.UTF8, SendRecvOpt.NONE);
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    //disables logging type messaging
                    DB_LOGGING = false;
                    loggerQueue.Clear();
                    logger.Error(e);
                    IGNORE_QUEUE_OVERFLOW = ON;
                    return;
                }
            }
        }

        /*
         * File storage thread
         */
        public void FileThreadRun()
        {
            while (true)
            {
                try
                {
                    //if there are no new messages in the queue, reads messages from the file storage
                    if (zeromqQueue.Count == 0 && fileQueue.Count > 0)
                    {
                        String message = readMessageFile();
                        zeromqQueue.Enqueue(message);
                        Thread.Sleep(1);
                    }
                    else
                    {
                        Thread.Sleep(zeromqQueue.Count * 10);
                    }

                    if (messagingFinished && fileQueue.Count == 0)
                    {
                        return;
                    }

                }
                catch (System.Exception e)
                {
                    logger.Error(e);
                    fileQueue.Clear();
                    return;
                }
            }
        }

        /*
         * Messaging thread function handles communication between WP4
         * 
         */
        public void ZeromqThreadRun()
        {
            logger.Info("messaging type: " + MESSAGING_TYPE);
            double messageNum = 0;
            switch (MESSAGING_TYPE)
            {
                case PIPELINE:
                    while (true)
                    {
                        try
                        {
                            //When a wait command is received, waits until the continue message
                            byte[] command = zeromqWP4Subscriber.Recv(SendRecvOpt.NOBLOCK);
                            if (command != null)
                            {
                                string commandString = System.Text.Encoding.UTF8.GetString(command);
                                if (commandString.Equals(WAIT_COMMAND))
                                {
                                    logger.Info("wait message is received: ");
                                    Thread.Sleep(1);
                                    String continueMessage = "";
                                    while (!continueMessage.Equals(CONTINUE_COMMAND))
                                    {
                                        continueMessage = zeromqWP4Subscriber.Recv(Encoding.UTF8);
                                        logger.Info("continue message is received: " + continueMessage);
                                    }
                                }
                            }
                            //Gets message from the queue added by the WP3
                            if (zeromqQueue.Count > 0)
                            {
                                String value = (String)zeromqQueue.Dequeue();
                                //Sends the message
                                zeromqSender.Send(value, Encoding.UTF8, SendRecvOpt.NONE);
                                logger.Debug("message is sent over network: " + messageNum++);
                                //if enabled, send a copy to DB_logging component
                                if (DB_LOGGING == true)
                                {
                                    loggerQueue.Enqueue(value);
                                }
                            }
                            //Terminates the thread if finish message is retrived from WP3
                            if (messagingFinished && zeromqQueue.Count == 0 && fileQueue.Count == 0 && brokerQueue.Count == 0 && loggerQueue.Count == 0)
                            {
                                zeromqSender.Send(FINISH_COMMAND, Encoding.UTF8, SendRecvOpt.NONE);
                                logger.Info("Finish command is received");
                                return;
                            }

                        }
                        catch (ThreadStateException e)
                        {
                            logger.Error(e);

                        }
                        catch (System.Exception e)
                        {
                            logger.Error(e);
                        }
                    }

                case REQ_REP:
                    while (true)
                    {
                        try
                        {
                            //Waits request message from WP4
                            String message = zeromqSender.Recv(Encoding.UTF8);
                            while (!message.Equals(MESSAGE_REQUEST))
                            {
                                message = zeromqSender.Recv(Encoding.UTF8);
                            }
                            logger.Debug("Received request: {0}" + message);
                            // Sends WP3 message to WP4
                            if (zeromqQueue.Count > 0)
                            {
                                String value = (String)zeromqQueue.Dequeue();
                                zeromqSender.Send(value, Encoding.UTF8);
                                logger.Debug("message is sent over network: " + messageNum++);
                                //if enabled, send a copy to DB_logging component
                                if (DB_LOGGING == true)
                                {
                                    loggerQueue.Enqueue(value);
                                }
                            }
                            //Terminates the thread if finish message is retrived from WP3
                            if (messagingFinished && zeromqQueue.Count == 0 && fileQueue.Count == 0 && brokerQueue.Count == 0 && loggerQueue.Count == 0)
                            {
                                zeromqSender.Send(FINISH_COMMAND, Encoding.UTF8, SendRecvOpt.NONE);
                                logger.Info("Finish command is received");
                                return;
                            }
                        }
                        catch (ThreadStateException e)
                        {
                            logger.Error(e);

                        }
                        catch (System.Exception e)
                        {
                            logger.Error(e);
                        }
                    }
            }

        }
        /*
         * Writes a message to a file
         */
        private void writeMessageToFile(String content)
        {
            if (!fileQueue.Contains(fileNum.ToString()))
            {
                // create a writer and open the file
                TextWriter tw = new StreamWriter(fileStorageAddress + "\\" + fileNum);
                // write a line of text to the file
                tw.Write(content);
                // close the stream
                tw.Close();

                fileQueue.Enqueue(fileNum.ToString());
                //reset counter
                if (fileNum >= maxFileStorageNum)
                {
                    fileNum = 1;
                }
                else
                {
                    fileNum++;
                }
            }
        }

        /*
         * Reads previously written messages if they are not sent to WP4 
         */
        private void readOldMessageFiles()
        {
            // check folder exists if not then create
            if (!Directory.Exists(fileStorageAddress))
            {
                Directory.CreateDirectory(fileStorageAddress);
            }
            //reads all file names and adds into the file queue
            string[] fileEntries = Directory.GetFiles(fileStorageAddress);
            foreach (string fileName in fileEntries)
            {
                String file = fileName.Substring(fileStorageAddress.Length + 1);
                fileQueue.Enqueue(file);
                int num = Convert.ToInt32(file);
                if (num >= fileNum)
                {
                    fileNum = num + 1;
                }

            }
            if (fileNum >= maxFileStorageNum)
            {
                fileNum = 1;
            }
            else
            {
                fileNum++;
            }

        }
        /*
         * Reads a message file 
         */
        private String readMessageFile()
        {
            String fileName = (String)fileQueue.Peek();
            StreamReader file = new StreamReader(fileStorageAddress + "\\" + fileName);
            String content = file.ReadToEnd();
            file.Close();
            System.IO.File.Delete(fileStorageAddress + "\\" + fileName);
            fileQueue.Dequeue();
            return content;
        }


    }

    /*
     * Queue class 
     */
    class BlockingQueue<T> : IEnumerable<T>
    {
        private int _count = 0;

        public int Count
        {
            get { return _count; }
            set { _count = value; }
        }
        private Queue<T> _queue = new Queue<T>();

        public T Dequeue()
        {
            lock (this)
            {
                while (_count <= 0)
                    Monitor.Wait(this);
                _count--;

                Monitor.Pulse(this);
                return _queue.Dequeue();
            }
        }

        public void Enqueue(T data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            lock (this)
            {
                _queue.Enqueue(data);
                _count++;
                Monitor.Pulse(this);

            }
        }

        public T Peek()
        {
            return _queue.Peek();
        }

        public void Clear()
        {
            _queue.Clear();
        }

        public bool Contains(T data)
        {
            lock (this)
            {
                return _queue.Contains(data);

            }
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            while (true)
                yield return Dequeue();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}