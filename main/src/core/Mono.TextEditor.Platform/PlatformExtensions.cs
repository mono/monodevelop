//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Platform
{
    public static class PlatformExtensions
    {
        public static ITextBuffer GetPlatformTextBuffer(this MonoDevelop.Ide.Editor.TextEditor textEditor)
        {
            return textEditor.GetContent<Mono.TextEditor.ITextEditorDataProvider>().GetTextEditorData().Document.TextBuffer;
        }

        public static MonoDevelop.Ide.Editor.ITextDocument GetTextEditor(this ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetProperty<MonoDevelop.Ide.Editor.ITextDocument>(typeof(MonoDevelop.Ide.Editor.ITextDocument));
        }
    }
}