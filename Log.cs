/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    Log.cs
 *  Desc:    Logging utility
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Text;

// TODO: allow nulls in logging functions (rationale: must not crash because of logging)

namespace Latino.Workflows
{
    /* .-----------------------------------------------------------------------
       |
       |  Class Log
       |
       '-----------------------------------------------------------------------
    */
    public class Log
    {
        public enum Type 
        { 
            Console   = 1,
            LogWriter = 2
        }

        public enum Level
        { 
            Debug    = 0,
            Info     = 1,
            Warn     = 2,
            Critical = 3,
            Off      = 4
        }

        private static object mConsoleLock
            = new object();

        private static Level mDefaultLogLevel
            = Level.Info;
        private static Type mDefaultLogType
            = Type.Console;
        private static TextWriter mDefaultLogWriter
            = null;

        private string mObjName;
        private Level mLogLevel;
        private Type mLogType;
        private TextWriter mLogWriter;

        public Log(string objName)
        {
            Utils.ThrowException(objName == null ? new ArgumentNullException("objName") : null);
            mObjName = objName;            
            mLogLevel = mDefaultLogLevel;
            mLogType = mDefaultLogType;
            mLogWriter = mDefaultLogWriter;
        }

        public static Level DefaultLogLevel
        {
            get { return mDefaultLogLevel; }
            set { mDefaultLogLevel = value; }
        }

        public Level LogLevel
        {
            get { return mLogLevel; }
            set { mLogLevel = value; }
        }

        public static Type DefaultLogType
        {
            get { return mDefaultLogType; }
            set { mDefaultLogType = value; }
        }

        public Type LogType
        {
            get { return mLogType; }
            set { mLogType = value; }
        }

        public static TextWriter DefaultLogWriter
        {
            get { return mDefaultLogWriter; }
            set { mDefaultLogWriter = value; }
        }

        public TextWriter LogWriter
        {
            get { return mLogWriter; }
            set { mLogWriter = value; }
        }

        public string ObjectName
        {
            get { return mObjName; }
            set
            {
                Utils.ThrowException(value == null ? new ArgumentNullException("ObjectName") : null);
                mObjName = value;
            }
        }

        // *** public interface ***

        public void Debug(string funcName, string message, params object[] args)
        {
            Utils.ThrowException(funcName == null ? new ArgumentNullException("funcName") : null); 
            Utils.ThrowException(message == null ? new ArgumentNullException("message") : null); 
            if (mLogLevel == Level.Debug)
            {
                if ((mLogType & Type.Console) != 0)
                {
                    lock (mConsoleLock)
                    {                     
                        Debug(Console.Out, funcName, string.Format(message, args));
                    }
                }
                if ((mLogType & Type.LogWriter) != 0 && mLogWriter != null)
                {
                    lock (mLogWriter)
                    {
                        Debug(mLogWriter, funcName, string.Format(message, args));
                    }
                }
            }
        }

        public void Info(string funcName, string message, params object[] args)
        {
            Utils.ThrowException(funcName == null ? new ArgumentNullException("funcName") : null);
            Utils.ThrowException(message == null ? new ArgumentNullException("message") : null); 
            if (mLogLevel <= Level.Info)
            {
                if ((mLogType & Type.Console) != 0)
                {
                    lock (mConsoleLock)
                    {
                        Info(Console.Out, funcName, string.Format(message, args));
                    }
                }
                if ((mLogType & Type.LogWriter) != 0 && mLogWriter != null)
                {
                    lock (mLogWriter)
                    {
                        Info(mLogWriter, funcName, string.Format(message, args));
                    }
                }
            }
        }

        public void Warning(string funcName, string message, params object[] args)
        {
            Utils.ThrowException(funcName == null ? new ArgumentNullException("funcName") : null);
            Utils.ThrowException(message == null ? new ArgumentNullException("message") : null); 
            if (mLogLevel <= Level.Warn)
            {
                if ((mLogType & Type.Console) != 0)
                {
                    lock (mConsoleLock)
                    {
                        Warning(Console.Out, funcName, string.Format(message, args));
                    }
                }
                if ((mLogType & Type.LogWriter) != 0 && mLogWriter != null)
                {
                    lock (mLogWriter)
                    {
                        Warning(mLogWriter, funcName, string.Format(message, args));
                    }
                }
            }
        }

        public void Warning(string funcName, Exception e)
        {
            Utils.ThrowException(funcName == null ? new ArgumentNullException("funcName") : null);
            Utils.ThrowException(e == null ? new ArgumentNullException("e") : null);
            Utils.ThrowException(e.Message == null ? new ArgumentNullException("e.Message") : null);
            if (mLogLevel <= Level.Warn)
            {
                if ((mLogType & Type.Console) != 0)
                {
                    lock (mConsoleLock)
                    {
                        Warning(Console.Out, funcName, /*message=*/null, e);
                    }
                }
                if ((mLogType & Type.LogWriter) != 0 && mLogWriter != null)
                {
                    lock (mLogWriter)
                    {
                        Warning(mLogWriter, funcName, /*message=*/null, e);
                    }
                }
            }
        }

        public void Critical(string funcName, string message, params object[] args)
        {
            Utils.ThrowException(funcName == null ? new ArgumentNullException("funcName") : null);
            Utils.ThrowException(message == null ? new ArgumentNullException("message") : null); 
            if (mLogLevel <= Level.Critical)
            {
                if ((mLogType & Type.Console) != 0)
                {
                    lock (mConsoleLock)
                    {
                        Critical(Console.Out, funcName, string.Format(message, args));
                    }
                }
                if ((mLogType & Type.LogWriter) != 0 && mLogWriter != null)
                {
                    lock (mLogWriter)
                    {
                        Critical(mLogWriter, funcName, string.Format(message, args));
                    }
                }
            }
        }

        public void Critical(string funcName, Exception e)
        {
            Utils.ThrowException(funcName == null ? new ArgumentNullException("funcName") : null);
            Utils.ThrowException(e == null ? new ArgumentNullException("e") : null);
            Utils.ThrowException(e.Message == null ? new ArgumentNullException("e.Message") : null);
            if (mLogLevel <= Level.Critical)
            {
                if ((mLogType & Type.Console) != 0)
                {
                    lock (mConsoleLock)
                    {
                        Critical(Console.Out, funcName, /*message=*/null, e);
                    }
                }
                if ((mLogType & Type.LogWriter) != 0 && mLogWriter != null)
                {
                    lock (mLogWriter)
                    {
                        Critical(mLogWriter, funcName, /*message=*/null, e);
                    }
                }
            }
        }

        // *** logging functions ***

        private void Debug(TextWriter writer, string funcName, string message)
        {
            writer.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1} {2}", DateTime.Now, mObjName, funcName);
            writer.WriteLine("DEBUG: {0}", message);
            writer.Flush();
        }

        private void Info(TextWriter writer, string funcName, string message)
        {
            writer.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1} {2}", DateTime.Now, mObjName, funcName);
            writer.WriteLine("INFO: {0}", message);
            writer.Flush();
        }

        private void Warning(TextWriter writer, string funcName, string message)
        {
            writer.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1} {2}", DateTime.Now, mObjName, funcName);
            writer.WriteLine("WARN: {0}", message);
            writer.Flush();
        }

        private void Warning(TextWriter writer, string funcName, string message, Exception e)
        {
            writer.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1} {2}", DateTime.Now, mObjName, funcName);
            writer.WriteLine("WARN: {0}", message == null ? e.Message : message);
            if (e.StackTrace != null) { writer.WriteLine(e.StackTrace); }
            writer.Flush();
        }

        private void Critical(TextWriter writer, string funcName, string message)
        {
            writer.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1} {2}", DateTime.Now, mObjName, funcName);
            writer.WriteLine("CRITICAL: {0}", message);
            writer.Flush();
        }

        private void Critical(TextWriter writer, string funcName, string message, Exception e)
        {
            writer.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1} {2}", DateTime.Now, mObjName, funcName);
            writer.WriteLine("CRITICAL: {0}", message == null ? e.Message : message);
            if (e.StackTrace != null) { writer.WriteLine(e.StackTrace); }
            writer.Flush();
        }
    }
}
