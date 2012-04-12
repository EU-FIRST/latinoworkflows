/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    IWorkflowComponent.cs
 *  Desc:    LATINO workflow component interface
 *  Created: Apr-2011
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;

namespace Latino.Workflows
{
    /* .-----------------------------------------------------------------------
       |
       |  Interface IWorkflowComponent
       |
       '-----------------------------------------------------------------------
    */
    public interface IWorkflowComponent : IDisposable
    {
        void Start();
        void Stop();
        bool IsRunning { get; }
    }
}
