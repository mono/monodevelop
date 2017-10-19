//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Platform
{
    public static class PlatformExtensions
    {
        public static ITextBuffer GetPlatformTextBuffer(this MonoDevelop.Ide.Editor.TextEditor textEditor)
        {
            return textEditor.TextView.TextBuffer;
        }

        public static ITextView GetPlatformTextView(this MonoDevelop.Ide.Editor.TextEditor textEditor)
        {
            return textEditor.TextView;
        }

        public static MonoDevelop.Ide.Editor.ITextDocument GetTextEditor(this ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetProperty<MonoDevelop.Ide.Editor.ITextDocument>(typeof(MonoDevelop.Ide.Editor.ITextDocument));
        }

        public static MonoDevelop.Ide.Editor.ITextDocument GetTextEditor (this ITextView textView)
        {
            return textView.Properties.GetProperty<MonoDevelop.Ide.Editor.TextEditor>(typeof(MonoDevelop.Ide.Editor.TextEditor));
        }
    }
}