// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities.Automation
{

    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
    
    /// <summary>
    /// This class provides standard automation support for a WPF object (a <see cref="UIElement"/> or a derivative thereof).
    /// All UIElement classes should use this class to provide automation support. If customizations
    /// are necessary, this class can be extended.
    /// </summary>
    public class WpfAutomationAdapter<T> : UIElementAutomationPeer, IAutomationAdapter<T> where T : UIElement
    {

        #region Private Attributes and Construction

        /// <summary>
        /// Holds a reference to the <see cref="AutomationProperties"/> for this
        /// object.
        /// </summary>
        private AutomationProperties _properties;

        /// <summary>
        /// Creates an automation peer for any UIElement
        /// </summary>
        public WpfAutomationAdapter(T coreElement, AutomationProperties properties)
            : base(coreElement)
        {
            _properties = properties;
        }

        #endregion //Private Attributes and Construction

        #region IAutomationAdapter<T> members

        /// <summary>
        /// Returns the element that's being automated.
        /// </summary>
        public T CoreElement
        {
            get 
            { 
                return (T)base.Owner; 
            }
        }

        /// <summary>
        /// Returns an <see cref="IRawElementProviderSimple"/> for a child element of this automatable.
        /// </summary>
        public IRawElementProviderSimple GetAutomationProviderForChild(AutomationPeer child)
        {
            return this.ProviderFromPeer(child);
        }

        /// <summary>
        /// Returns an <see cref="AutomationPeer"/> for the element being automated. <see cref="AutomationPeer"/>s are used
        /// by WPF UIAutomation to access properties of the automated elements.
        /// </summary>
        public AutomationPeer AutomationPeer
        {
            get 
            { 
                return this; 
            }
        }

        /// <summary>
        /// Returns a corresponding <see cref="IRawElementProviderSimple"/> proxy for the automated element. Clients using
        /// the Microsoft Active Accessibility standard access automated elements through <see cref="IRawElementProviderSimple"/>
        /// proxies.
        /// </summary>
        public IRawElementProviderSimple AutomationProvider
        {
            get 
            {
                return this.ProviderFromPeer(this); 
            }
        }

        /// <summary>
        /// Returns the <see cref="AutomationProperties"/> associated with the automated element. The <see cref="AutomationProperties"/>
        /// holds core properties about the element being automated.
        /// </summary>
        public AutomationProperties AutomationProperties
        {
            get 
            { 
                return _properties; 
            }
        }

        #endregion //IAutomatable<T> members

        #region AutomationPeer elements

        /// <summary>
        /// Returns the Class Name for this automation peer
        /// </summary>
        protected override string GetClassNameCore()
        {
            return AutomationProperties.ClassName;
        }

        /// <summary>
        /// Returns a friendly name for the automated object
        /// </summary>
        protected override string GetNameCore()
        {
            return AutomationProperties.Name;
        }

        /// <summary>
        /// Gets the Automation Control type that the control resembles
        /// </summary>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationProperties.ControlType;
        }

        /// <summary>
        /// Gets the automation ID for the automated object
        /// </summary>
        protected override string GetAutomationIdCore()
        {
            return AutomationProperties.AutomationId;
        }

        /// <summary>
        /// Returns an object implementing the pattern requested. If the object implements the pattern, the implemented pattern is
        /// returned, otherwise the base's GetPattern is called which might return null
        /// </summary>
        public override object GetPattern(PatternInterface patternInterface)
        {
            return AutomationProperties.GetPatternProvider(patternInterface) ?? base.GetPattern(patternInterface);
        }

        /// <summary>
        /// Returns a rectangle specifying the layout of the automated element on the screen
        /// </summary>
        protected override Rect GetBoundingRectangleCore()
        {
            if ((Owner.Visibility == Visibility.Visible) && Owner.IsVisible)
            {
                Point translatedCoordinate = Owner.PointToScreen(new Point(0,0)); //find x and y coordinates of element on screen
                return new Rect(translatedCoordinate.X, translatedCoordinate.Y, Owner.RenderSize.Width, Owner.RenderSize.Height);
            }
            else
            {
                return base.GetBoundingRectangleCore();
            }
        }

        /// <summary>
        /// Returns a help text for the automated element.
        /// </summary>
        protected override string GetHelpTextCore()
        {
            if (!string.IsNullOrEmpty(AutomationProperties.HelpText))
            {
                return AutomationProperties.HelpText;
            }
            else
            {
                return base.GetHelpTextCore();
            }
        }

        #endregion //AutomationPeer elements
    }
}
