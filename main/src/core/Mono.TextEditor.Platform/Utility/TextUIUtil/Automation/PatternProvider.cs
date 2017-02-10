// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities.Automation
{

    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Provides a base for any pattern provider that wishes to provide some pattern
    /// for an <see cref="IWpfTextView"/>.
    /// </summary>
    public abstract class PatternProvider
    {

        /// <summary>
        /// Keeps a reference to the <see cref="IWpfTextView"/> for which
        /// a pattern is being provided
        /// </summary>
        private IWpfTextView _wpfTextView;
        
        /// <summary>
        /// Constructs a PatternProvider given an <see cref="IWpfTextView"/>.
        /// </summary>
        internal PatternProvider(IWpfTextView wpfTextView)
        {
            if (wpfTextView == null) 
                throw new System.ArgumentNullException("wpfTextView");

            _wpfTextView = wpfTextView;
        }

        /// <summary>
        /// Protected accessor for all subclasses
        /// </summary>
        protected IWpfTextView TextView
        {
            get
            {
                return _wpfTextView;
            }
        }

    }
}
