//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;

namespace MonoDevelop.Xml.Editor
{
	/// <summary>
	/// Configuration settings for the xml editor.
	/// </summary>
	class XmlEditorOptionsPanel : OptionsPanel
	{
		XmlEditorOptionsPanelWidget widget;
		
		public override Control CreatePanelWidget ()
		{
			widget = new XmlEditorOptionsPanelWidget();
			widget.AutoCompleteElements = XmlEditorOptions.AutoCompleteElements;
			widget.AutoInsertFragments = XmlEditorOptions.AutoInsertFragments;
			widget.ShowSchemaAnnotation = XmlEditorOptions.ShowSchemaAnnotation;
			widget.AutoShowCodeCompletion = XmlEditorOptions.AutoShowCodeCompletion;
			return widget;
		}
		
		public override void ApplyChanges ()
		{
			XmlEditorOptions.AutoCompleteElements = widget.AutoCompleteElements;
			XmlEditorOptions.AutoInsertFragments = widget.AutoInsertFragments;
			XmlEditorOptions.ShowSchemaAnnotation = widget.ShowSchemaAnnotation;
			XmlEditorOptions.AutoShowCodeCompletion = widget.AutoShowCodeCompletion;
		}
	}
}
