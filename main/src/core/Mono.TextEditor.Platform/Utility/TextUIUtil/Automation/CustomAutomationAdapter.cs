// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities.Automation
{

    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;

    /// <summary>
    /// This class provides the set of basic automation properties that an object needs to provide.
    /// Classes other than <see cref="UIElement"/> and its derivative that wish to provide automation
    /// can use this. It's expected that this class is extended to provide customized automation behavior
    /// tailored to the specific class being automated.
    /// </summary>
    public class CustomAutomationAdapter<T> : AutomationPeer, IAutomationAdapter<T> where T : class
    {

        #region Private Attributes and Construction

        /// <summary>
        /// Provides the basic set of properties that every automated object has to provide.
        /// </summary>
        private AutomationProperties _properties;

        /// <summary>
        /// Stores a reference to the item for which automation is being provided.
        /// </summary>
        private T _coreElement;

        /// <summary>
        /// Creates an automation peer for any UIElement
        /// </summary>
        public CustomAutomationAdapter(T coreElement, AutomationProperties properties)
        {
            _coreElement = coreElement;
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
                return _coreElement;
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
        protected override sealed string GetClassNameCore()
        {
            return AutomationProperties.ClassName;
        }

        /// <summary>
        /// Returns a friendly name for the automated object
        /// </summary>
        protected override sealed string GetNameCore()
        {
            return AutomationProperties.Name;
        }

        /// <summary>
        /// Gets the Automation Control type that the control resembles
        /// </summary>
        protected override sealed AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationProperties.ControlType;
        }

        /// <summary>
        /// Gets the automation ID for the automated object
        /// </summary>
        protected override sealed string GetAutomationIdCore()
        {
            return AutomationProperties.AutomationId;
        }

        /// <summary>
        /// Returns an object implementing the pattern requested. If the object implements the pattern, the implemented pattern is
        /// returned, otherwise the base's GetPattern is called which might return null
        /// </summary>
        public override sealed object GetPattern(PatternInterface patternInterface)
        {
            return AutomationProperties.GetPatternProvider(patternInterface);
        }

        /// <summary>
        /// Returns a rectangle specifying the layout of the automated element on the screen
        /// </summary>
        /// <remarks>Extenders of this class are encouraged to provide their own implementation of this method.</remarks>
        protected override Rect GetBoundingRectangleCore()
        {
            return new Rect(0,0,0,0);
        }

        /// <summary>
        /// Returns a help text for the automated element.
        /// </summary>
        protected sealed override string GetHelpTextCore()
        {
            if (!string.IsNullOrEmpty(AutomationProperties.HelpText))
            {
                return AutomationProperties.HelpText;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the element that is associated with this automation peer currently has keyboard focus.
        /// </summary>
        protected override bool HasKeyboardFocusCore()
        {
            return false;
        }

        /// <summary>
        /// Gets a value that indicates whether the element that is associated with this automation peer contains data that is presented to the user.
        /// </summary>
        /// <remarks>Extenders of this class are encouraged to provider their own implementation of this method</remarks>
        protected override bool IsContentElementCore()
        {
            return true;
        }

        /// <summary>
        /// Gets a value that indicates whether the element is understood by the user as interactive or as contributing to the logical structure of the control in the GUI.
        /// </summary>
        /// <remarks>Extenders of this class are encouraged to provider their own implementation of this method</remarks>
        protected override bool IsControlElementCore()
        {
            return true;
        }

        /// <summary>
        /// Gets a value that indicates whether the element associated with this automation peer supports interaction.
        /// </summary>
        /// <returns>Always returns true for a text view line</returns>
        protected override bool IsEnabledCore()
        {
            return true;
        }

        /// <summary>
        /// Gets a value that indicates whether the element can accept keyboard focus.
        /// </summary>
        protected override bool IsKeyboardFocusableCore()
        {
            return false;
        }

        /// <summary>
        /// Gets a value that indicates whether an element is off the screen.
        /// </summary>
        protected override bool IsOffscreenCore()
        {
            return true;
        }

        /// <summary>
        /// Gets a value that indicates whether the element contains sensitive content.
        /// </summary>
        protected override bool IsPasswordCore()
        {
            return false;
        }

        /// <summary>
        /// Gets a value that indicates whether the element that is associated with this peer must be completed on a form.
        /// </summary>
        protected override bool IsRequiredForFormCore()
        {
            return false;
        }

        /// <summary>
        /// Sets the keyboard focus on the element that is associated with this automation peer. This should never be called because
        /// IsKeyboardFocusableCore returns false
        /// </summary>
        protected override void SetFocusCore()
        {
        }

        /// <summary>
        /// Gets the collection of GetChildren elements that are represented in the UI Automation tree as immediate child elements of the automation peer.
        /// </summary>
        /// <remarks>Extenders of this class are encouraged to provide their own implementation for this method.</remarks>
        protected override System.Collections.Generic.List<AutomationPeer> GetChildrenCore()
        {
            return null;
        }

        /// <summary>
        /// Gets a Point on the element that is associated with the automation peer that responds to a mouse click.
        /// </summary>
        protected override Point GetClickablePointCore()
        {
            return new Point(0,0);
        }

        /// <summary>
        /// Gets text that conveys the visual status of the element that is associated with this automation peer.
        /// </summary>
        protected override string GetItemStatusCore()
        {
            return string.Empty;
        }

        /// <summary>
        /// Gets a string that describes what kind of item an object represents.
        /// </summary>
        protected override string GetItemTypeCore()
        {
            return string.Empty;
        }

        /// <summary>
        /// Gets the AutomationPeer for the Label that is targeted to the element.
        /// </summary>
        protected override AutomationPeer GetLabeledByCore()
        {
            return null;
        }

        /// <summary>
        /// Gets a value that indicates the explicit control orientation, if any.
        /// </summary>
        protected override AutomationOrientation GetOrientationCore()
        {
            return AutomationOrientation.None;
        }

        /// <summary>
        /// Gets the accelerator key combinations for the element that is associated with the UI Automation peer. 
        /// </summary>
        protected override string GetAcceleratorKeyCore()
        {
            return null;
        }

        /// <summary>
        /// Gets the access key for the element that is associated with the automation peer.
        /// </summary>
        protected override string GetAccessKeyCore()
        {
            return null;
        }

        #endregion //AutomationPeer elements
    }
}
