// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities.Automation
{
    /// <summary>
    /// This interface must be implemented by any element that is automated. It provides a reference to the <see cref="IAutomationAdapter"/>
    /// that's used by the object to provide automation.
    /// </summary>
    public interface IAutomatedElement
    {
        /// <summary>
        /// A reference to the automation adapter used by this automated element. The <see cref="IAutomationAdapter"/>
        /// provides access to automation properties of the automated object.
        /// </summary>
        IAutomationAdapter AutomationAdapter { get; }
    }

}
