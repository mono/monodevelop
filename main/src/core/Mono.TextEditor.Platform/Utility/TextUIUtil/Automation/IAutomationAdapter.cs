// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities.Automation
{

    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;

    /// <summary>
    /// Defines the core properties necessary for an object to be automated.
    /// All objects willing to provide automation support must provide an implementation of this interface (this is usually done
    /// by implementing <see cref="IAutomatedElement"/>).
    /// </summary> 
    public interface IAutomationAdapter
    {
        /// <summary>
        /// Returns an <see cref="IRawElementProviderSimple"/> for a child element of this automatable.
        /// </summary>
        IRawElementProviderSimple GetAutomationProviderForChild(AutomationPeer child);

        /// <summary>
        /// Returns an <see cref="AutomationPeer"/> for the element being automated. <see cref="AutomationPeer"/>s are used
        /// by WPF UIAutomation to access properties of the automated elements.
        /// </summary>
        AutomationPeer AutomationPeer { get; }

        /// <summary>
        /// Returns a corresponding <see cref="IRawElementProviderSimple"/> proxy for the automated element. Clients using
        /// the Microsoft Active Accessibility standard access automated elements through <see cref="IRawElementProviderSimple"/>
        /// proxies.
        /// </summary>
        IRawElementProviderSimple AutomationProvider { get; }

        /// <summary>
        /// Returns the <see cref="AutomationProperties"/> associated with the automated element. The <see cref="AutomationProperties"/>
        /// holds core properties about the element being automated.
        /// </summary>
        AutomationProperties AutomationProperties { get; }
    }


    /// <summary>
    /// Defines the core properties necessary for an object to be automated.
    /// All objects willing to provide automation support must provide an implementation of this interface
    /// (this is usually done by implementing <see cref="IAutomatedElement"/>).
    /// The templated interface, in addition to <see cref="IAutomationAdapter"/> holds a reference
    /// to the object being automated.
    /// </summary> 
    public interface IAutomationAdapter<T> : IAutomationAdapter where T : class
    {
        /// <summary>
        /// Returns the element that's being automated.
        /// </summary>
        T CoreElement { get; }
    }

}
