// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities.Automation
{

    using System.Collections.Generic;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;

    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// This class provides the very core properties needed for an automated object. Its purpose is to gather the commonalities of 
    /// objects that provide automation. It's also used to enforce every automated object to implement the set of basic
    /// properties defined in this class.
    /// </summary>
    public class AutomationProperties
    {

        #region Private Attributes and Construction

        /// <summary>
        /// Holds a dictionary of patterns that an automated object implementes. The patterns correspond to the values
        /// defiend in <see cref="PatternInterface"/>. Each automated element is expected to define a subset of
        /// these patterns to make itself accessible through automation.
        /// </summary>
        private IDictionary<PatternInterface, object> _patterns;

        /// <summary>
        /// Creates an automation helper for any given element
        /// </summary>
        /// <param name="className">The class name we want the automation helper to return.</param>
        /// <param name="automationId">The automation id we want the automation helper to return.</param>
        /// <param name="name">Specifies a friendly name for the end user to see for this element</param>
        /// <param name="helpText">The help text associated with the element that's being automated, this should describe the purpose of the element.</param>
        /// <param name="controlType">The type of the control for which the automation helper is being created.</param>
        /// <param name="wpfTextView">The <see cref="IWpfTextView"/> on which the automated element resides</param>
        public AutomationProperties(string className, string automationId, string name, string helpText, AutomationControlType controlType, IWpfTextView wpfTextView)
        {
            ClassName = className;
            AutomationId = automationId;
            ControlType = controlType;
            HelpText = helpText;
            Name = name;
            TextView = wpfTextView;
            _patterns = null;
        }

        /// <summary>
        /// Creates an automation helper for any given element
        /// </summary>
        /// <param name="className">The class name we want the automation helper to return.</param>
        /// <param name="automationId">The automation id we want the automation helper to return.</param>
        /// <param name="name">Specifies a friendly name for the end user to see for this element</param>
        /// <param name="helpText">The help text associated with the element that's being automated, this should describe the purpose of the element.</param>
        /// <param name="wpfTextView">The <see cref="IWpfTextView"/> on which the automated element resides</param>
        /// <remarks>Assumes a default Pane control type.</remarks>
        public AutomationProperties(string className, string automationId, string name, string helpText, IWpfTextView wpfTextView) :
            this(className, automationId, name, helpText, AutomationControlType.Pane, wpfTextView)
        {}

        #endregion //Private Attributes and Construction

        /// <summary>
        /// Adds a new pattern implementation for this automation helper.
        /// </summary>
        /// <param name="patternInterface">Specifies which of the predefined pattern interfaces this handler is being added.</param>
        /// <param name="patternHandler">Specifies a handler for the pattern interface being added.</param>
        /// <remarks>Duplicate handlers for the same pattern interface are not allowed. Each pattern interface can have 0 or 1 handler.</remarks>
        public void AddPattern(PatternInterface patternInterface, object patternHandler)
        {
            //initialize the pattern implementation dictionary lazily
            if (_patterns == null)
            {
                _patterns = new Dictionary<PatternInterface, object>();
            }
            //replace any existing implementation for a pattern with the new one
            if (_patterns.ContainsKey(patternInterface))
            {
                _patterns.Remove(patternInterface);
            }
            _patterns.Add(patternInterface, patternHandler);
        }

        /// <summary>
        /// Returns the pattern implementation for a given <see cref="PatternInterface"/>. Returns null if the 
        /// pattern is not supported by the automated object.
        /// </summary>
        public object GetPatternProvider(PatternInterface patternInterface)
        {
            if (_patterns == null || !_patterns.ContainsKey(patternInterface))
            {
                return null;
            }
            else
            {
                return _patterns[patternInterface];
            }
        }

        /// <summary>
        /// AutomationId is used such that automation clients can distinguish among various automated elements available.
        /// </summary>
        public string AutomationId { get; set; }

        /// <summary>
        /// ClassName provides the name of the programmatic class that is being automated.
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// HelpText is intended for end users to provide them general information about the automated object.
        /// </summary>
        public string HelpText { get; set; }

        /// <summary>
        /// Name acts as a friendly identifier for the end users to identify the automated element.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Returns the <see cref="AutomationControlType"/> of the element being automated.
        /// </summary>
        public AutomationControlType ControlType { get; private set; }

        /// <summary>
        /// Returns an instance of the <see cref="IWpfTextView"/> on which automation is being provided
        /// </summary>
        public IWpfTextView TextView { get; private set; }

    }
}
