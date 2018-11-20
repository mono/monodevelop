//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    public interface ITextEditorInitializationService
    {
        ITextView CreateTextView (MonoDevelop.Ide.Editor.TextEditor textEditor);
    }
}
