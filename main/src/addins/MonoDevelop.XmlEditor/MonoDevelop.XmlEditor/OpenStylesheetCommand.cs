//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2006 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using System;
namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Opens the stylesheet associated with the active XML document.
	/// </summary>
	public class OpenStylesheetCommand : CommandHandler
	{
		protected override void Run()
		{
			XmlEditorViewContent view = XmlEditorService.GetActiveView();
			if (view != null) {
				if (view.StylesheetFileName != null) {
					try {
						IdeApp.Workbench.OpenDocument(view.StylesheetFileName);
					} catch (Exception ex) {
						MonoDevelop.Core.LoggingService.LogError ("Could not open document.", ex);
						MonoDevelop.Ide.MessageService.ShowError ("Could not open document.", ex.ToString());
					}
				}
			}
		}
		
		protected override void Update(CommandInfo info)
		{
			XmlEditorViewContent view = XmlEditorService.GetActiveView();
			if (view != null && view.StylesheetFileName != null) {
				info.Enabled = true;
			} else {
				info.Enabled = false;
			}
		}
	}
}
