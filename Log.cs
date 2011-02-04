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

// TODO: per-instance level, per-instance type...

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
            Console = 1,
            File    = 2
        }

        public enum Level
        { 
            Debug    = 0,
            Info     = 1,
            Warn     = 2,
            Critical = 3,
            Off      = 4
        }

        private string mObjName;
        private static Level mLevel
            = Level.Debug; //Level.Info;
        private static string mFileName
            = null;
        private static Type mType
            = Type.Console;

        private static object mLogLock
            = new object();

        public Log(string objName)
        {
            Utils.ThrowException(objName == null ? new ArgumentNullException("objName") : null);
            mObjName = objName;
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

        public static Level LogLevel
        {
            get { return mLevel; }
            set { mLevel = value; }
        }

        // *** public interface ***

        public void Debug(string funcName, string message, params object[] args)
        {
            Utils.ThrowException(funcName == null ? new ArgumentNullException("funcName") : null); 
            Utils.ThrowException(message == null ? new ArgumentNullException("message") : null); 
            if (mLevel == Level.Debug)
            {
                lock (mLogLock)
                {
                    if ((mType & Type.Console) != 0)
                    {
                        DebugConsole(funcName, string.Format(message, args));
                    }
                }
            }
        }

        public void Info(string funcName, string message, params object[] args)
        {
            Utils.ThrowException(funcName == null ? new ArgumentNullException("funcName") : null);
            Utils.ThrowException(message == null ? new ArgumentNullException("message") : null); 
            if (mLevel <= Level.Info)
            {
                lock (mLogLock)
                {
                    if ((mType & Type.Console) != 0)
                    {
                        InfoConsole(funcName, string.Format(message, args));
                    }
                }
            }
        }

        public void Warning(string funcName, string message, params object[] args)
        {
            Utils.ThrowException(funcName == null ? new ArgumentNullException("funcName") : null);
            Utils.ThrowException(message == null ? new ArgumentNullException("message") : null); 
            if (mLevel <= Level.Warn)
            {
                lock (mLogLock)
                {
                    if ((mType & Type.Console) != 0)
                    {
                        WarningConsole(funcName, string.Format(message, args));
                    }
                }
            }
        }

        public void Warning(string funcName, Exception e)
        {
            Utils.ThrowException(funcName == null ? new ArgumentNullException("funcName") : null);
            Utils.ThrowException(e == null ? new ArgumentNullException("e") : null);
            Utils.ThrowException(e.Message == null ? new ArgumentNullException("e.Message") : null);
            if (mLevel <= Level.Warn)
            {
                lock (mLogLock)
                {
                    if ((mType & Type.Console) != 0)
                    {
                        WarningConsole(funcName, /*message=*/null, e);
                    }
                }
            }
        }

        public void Critical(string funcName, string message, params object[] args)
        {
            Utils.ThrowException(funcName == null ? new ArgumentNullException("funcName") : null);
            Utils.ThrowException(message == null ? new ArgumentNullException("message") : null); 
            if (mLevel <= Level.Critical)
            {
                lock (mLogLock)
                {
                    if ((mType & Type.Console) != 0)
                    {
                        CriticalConsole(funcName, string.Format(message, args));
                    }
                }
            }
        }

        public void Critical(string funcName, Exception e)
        {
            Utils.ThrowException(funcName == null ? new ArgumentNullException("funcName") : null);
            Utils.ThrowException(e == null ? new ArgumentNullException("e") : null);
            Utils.ThrowException(e.Message == null ? new ArgumentNullException("e.Message") : null);
            if (mLevel <= Level.Critical)
            {
                lock (mLogLock)
                {
                    if ((mType & Type.Console) != 0)
                    {
                        CriticalConsole(funcName, /*message=*/null, e);
                    }
                }
            }
        }

        // *** console and file output ***

        private void DebugConsole(string funcName, string message)
        {
            Console.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1} {2}", DateTime.Now, mObjName, funcName);
            Console.WriteLine("DEBUG: {0}", message);
        }

        private void InfoConsole(string funcName, string message)
        {
            Console.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1} {2}", DateTime.Now, mObjName, funcName);
            Console.WriteLine("INFO: {0}", message);
        }

        private void WarningConsole(string funcName, string message)
        {
            Console.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1} {2}", DateTime.Now, mObjName, funcName);
            Console.WriteLine("WARN: {0}", message);
        }

        private void WarningConsole(string funcName, string message, Exception e)
        {
            Console.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1} {2}", DateTime.Now, mObjName, funcName);
            Console.WriteLine("WARN: {0}", message == null ? e.Message : message);
            if (e.StackTrace != null) { Console.WriteLine(e.StackTrace); }
        }

        private void CriticalConsole(string funcName, string message)
        {
            Console.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1} {2}", DateTime.Now, mObjName, funcName);
            Console.WriteLine("CRITICAL: {0}", message);
        }

        private void CriticalConsole(string funcName, string message, Exception e)
        {
            Console.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1} {2}", DateTime.Now, mObjName, funcName);
            Console.WriteLine("CRITICAL: {0}", message == null ? e.Message : message);
            if (e.StackTrace != null) { Console.WriteLine(e.StackTrace); }
        }
    }
}
