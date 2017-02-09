// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.EditorPrimitives.Implementation
{
    using Microsoft.VisualStudio.Text.Editor;

    internal sealed class ViewPrimitives : IViewPrimitives
    {
        private TextView _textView;
        private Selection _selection;
        private Caret _caret;
        private TextBuffer _textBuffer;
        
        #region IViewPrimitives Members

        internal ViewPrimitives(ITextView textView, IViewPrimitivesFactoryService viewPrimitivesFactory)
        {
            _textView = viewPrimitivesFactory.CreateTextView(textView);

            _textBuffer = _textView.TextBuffer;
            _selection = _textView.Selection;
            _caret = _textView.Caret;
        }

        public TextView View
        {
            get { return _textView; }
        }

        public Selection Selection
        {
            get { return _selection; }
        }

        public Caret Caret
        {
            get { return _caret; }
        }

        public TextBuffer Buffer
        {
            get { return _textBuffer; }
        }

        #endregion
    }
}
