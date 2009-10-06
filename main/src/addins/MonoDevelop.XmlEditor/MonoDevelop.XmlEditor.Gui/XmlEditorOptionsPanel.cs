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

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;

using System;
using Gtk;

namespace MonoDevelop.XmlEditor.Gui
{
	/// <summary>
	/// Configuration settings for the xml editor.
	/// </summary>
	public class XmlEditorOptionsPanel : OptionsPanel
	{
		XmlEditorOptionsPanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			widget = new XmlEditorOptionsPanelWidget();
			widget.AutoCompleteElements = XmlEditorAddInOptions.AutoCompleteElements;
			widget.ShowSchemaAnnotation = XmlEditorAddInOptions.ShowSchemaAnnotation;
			return widget;
		}
		
		public override void ApplyChanges ()
		{
			XmlEditorAddInOptions.AutoCompleteElements = widget.AutoCompleteElements;
			XmlEditorAddInOptions.ShowSchemaAnnotation = widget.ShowSchemaAnnotation;
		}
	}
}
