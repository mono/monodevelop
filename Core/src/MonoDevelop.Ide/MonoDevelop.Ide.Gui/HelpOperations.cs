//
// HelpOperations.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
//


using System;
using System.Collections;
using Monodoc;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Gui
{
	public class HelpOperations
	{
		HelpViewer helpViewer;

		public void ShowHelp (string topic)
		{
			if (helpViewer == null) {
				helpViewer = new HelpViewer ();
				helpViewer.LoadUrl (topic);
				IdeApp.Workbench.OpenDocument (helpViewer, true);		
				helpViewer.WorkbenchWindow.Closed += new EventHandler (CloseWindowEvent);
			} else {
				helpViewer.LoadUrl (topic);
				helpViewer.WorkbenchWindow.SelectWindow ();
			}
		}

		public void ShowDocs (string text, Node matched_node, string url)
		{
			if (helpViewer == null) {
				helpViewer = new HelpViewer ();
				helpViewer.Render (text, matched_node, url);
				IdeApp.Workbench.OpenDocument (helpViewer, true);
				helpViewer.WorkbenchWindow.Closed += new EventHandler (CloseWindowEvent);
			} else {
				helpViewer.Render (text, matched_node, url);
				helpViewer.WorkbenchWindow.SelectWindow ();
			}
		}

		void CloseWindowEvent (object sender, EventArgs e)
		{
			helpViewer = null;
		}
	}
}
