////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Microsoft.VisualStudio.Utilities;
#if TARGET_VS
using System.Windows.Media;
#endif

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an item in a completion set. 
    /// </summary>
    public class Completion : IPropertyOwner
    {
        // unfortunately this public class, intended to be inherited from, was declared with 
        // copious private state that is never used by the predominant completion implementation.
        // Given the high frequency with which these objects are created, it makes sense to
        // allocate that rarely (never?) used state only on demand.
        private class CompletionState
        {
            public string displayText;
            public string insertionText;
            public string description;
#if TARGET_VS
            public ImageSource iconSource;
#endif            
            public string iconAutomationText;
            public PropertyCollection properties;
        }

        private CompletionState _state; // rarely if ever used with shim completion

        /// <summary>
        /// Initializes a new instance of <see cref="Completion"/>.
        /// </summary>
        public Completion()
        { 
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Completion"/> with the specified text and description.
        /// </summary>
        /// <param name="displayText">The text that is to be displayed by an IntelliSense presenter.</param>
        public Completion(string displayText)
        {
            _state = new CompletionState();
            _state.displayText = displayText;
            _state.insertionText = displayText;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Completion"/> with the specified text and description.
        /// </summary>
        /// <param name="displayText">The text that is to be displayed by an IntelliSense presenter.</param>
        /// <param name="insertionText">The text that is to be inserted into the buffer if this completion is committed.</param>
        /// <param name="description">A description that could be displayed with the display text of the completion.</param>
        /// <param name="iconSource">The icon to describe the completion item.</param>
        /// <param name="iconAutomationText">The automation name for the icon.</param>
        public Completion(string displayText,
                          string insertionText,
                          string description,
#if TARGET_VS
                          ImageSource iconSource,
#endif
                          string iconAutomationText)
        {
            _state = new CompletionState();
            _state.displayText = displayText;
            _state.insertionText = insertionText;
            _state.description = description;
#if TARGET_VS
            _state.iconSource = iconSource;
#endif
            _state.iconAutomationText = iconAutomationText;
        }

        /// <summary>
        /// Gets/Sets the text that is to be displayed by an IntelliSense presenter.
        /// </summary>
        public virtual string DisplayText
        {
            get { return _state != null ? _state.displayText : null; }
            set
            {
                EnsureState();
                _state.displayText = value;
            }
        }

        /// <summary>
        /// Gets/Sets the text that is to be inserted into the buffer if this completion is committed.
        /// </summary>
        public virtual string InsertionText
        {
            get { return _state != null ? _state.insertionText : null; }
            set
            {
                EnsureState();
                _state.insertionText = value;
            }
        }

        /// <summary>
        /// Gets/Sets a description that could be displayed with the display text of the completion.
        /// </summary>
        public virtual string Description
        {
            get { return _state != null ? _state.description : null; }
            set
            {
                EnsureState();
                _state.description = value;
            }
        }

#if TARGET_VS
        /// <summary>
        /// Gets/Sets an icon that could be used to describe the completion.
        /// </summary>
        public virtual ImageSource IconSource
        {
            get { return _state != null ? _state.iconSource : null; }
            set 
            {
                EnsureState();
                _state.iconSource = value; 
            }
        }
#endif

        /// <summary>
        /// Gets/Sets the text to be used as the automation name for the icon when it's displayed.
        /// </summary>
        public virtual string IconAutomationText
        {
            get { return _state != null ? _state.iconAutomationText : null; }
            set 
            {
                EnsureState();
                _state.iconAutomationText = value; 
            }
        }

        /// <summary>
        /// Gets the properties of the completion.
        /// </summary>
        public virtual PropertyCollection Properties
        {
            get
            {
                EnsureState();
                if (_state.properties == null)
                {
                    _state.properties = new PropertyCollection();
                }

                return _state.properties;
            }
        }

        private void EnsureState()
        {
            if (_state == null)
            {
                _state = new CompletionState();
            }
        }
    }
}
