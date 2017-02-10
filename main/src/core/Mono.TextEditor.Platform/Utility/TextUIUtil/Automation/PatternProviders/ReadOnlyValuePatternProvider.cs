// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities.Automation
{
    using System;
    using System.Windows.Automation.Provider;

    /// <summary>
    /// Custom provider for read-only values of controls
    /// </summary>
    /// <returns>value of the control</returns>
    public delegate string CustomReadOnlyValueProvider();

    /// <summary>
    /// Implements <see cref="IValueProvider"/> for read-only controls. 
    /// </summary>
    public class ReadOnlyValuePatternProvider : IValueProvider
    {
        private CustomReadOnlyValueProvider _customValueProvider;

        /// <summary>
        /// Creates an instance of the <see cref="ReadOnlyValuePatternProvider"/>. Uses the custom value provider 
        /// to return the value of the control. 
        /// </summary>
        /// <param name="customValueProvider">The custom provider for the value</param>
        /// <exception cref="ArgumentNullException">ArgumentNullException is thrown if a null customValueProvider is given.</exception>
        public ReadOnlyValuePatternProvider(CustomReadOnlyValueProvider customValueProvider)
        {
            if (customValueProvider == null)
                throw new ArgumentNullException("customValueProvider");

            _customValueProvider = customValueProvider;
        }

        #region IValueProvider Members

        /// <summary>
        /// Gets a value that specifies whether the value of the control is read-only. <see cref="ReadOnlyValuePatternProvider"/> always returns true. 
        /// </summary>
        public bool IsReadOnly
        {
            get 
            {
                return true;
            }
        }

        /// <summary>
        /// Sets the value of the control. For the <see cref="ReadOnlyValuePatternProvider"/> nothing will be set. 
        /// </summary>
        /// <param name="value">Value to be set</param>
        /// <exception cref="InvalidOperationException">Cannot set a value for the ReadOnlyValuePatternProvider</exception>
        public void SetValue(string value)
        {
            // pattern is read-only. 
            throw new InvalidOperationException("Cannot set value for a read only value pattern");
        }

        /// <summary>
        /// Get the value of the control
        /// </summary>
        public string Value
        {
            get 
            {
                // invoke the custom provider
                return _customValueProvider();
            }
        }

        #endregion //IValueProvider Members
    }
}
