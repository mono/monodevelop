////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides information about value changes of all kinds.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class ValueChangedEventArgs<TValue> : EventArgs
    {
        private TValue _oldValue;
        private TValue _newValue;

        /// <summary>
        /// Initializes a new instance of <see cref="ValueChangedEventArgs&lt;T&gt;"/> with the new and old values of a property.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        public ValueChangedEventArgs(TValue oldValue, TValue newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        /// <summary>
        /// Gets the old value.
        /// </summary>
        public TValue OldValue
        { get { return _oldValue; } }

        /// <summary>
        /// Gets the new value.
        /// </summary>
        public TValue NewValue
        { get { return _newValue; } }
    }
}
