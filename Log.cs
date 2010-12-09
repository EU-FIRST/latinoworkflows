/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    Log.cs
 *  Desc:    Common logging utilities
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;

namespace Latino.Workflows
{
    /* .-----------------------------------------------------------------------
       |
       |  Class Log
       |
       '-----------------------------------------------------------------------
    */
    public static class Log
    {
        public static void Info(string message)
        {
            Console.WriteLine(message);
        }

        public static void Critical(Exception exc)
        {
            Console.WriteLine(exc);
        }
    }
}
