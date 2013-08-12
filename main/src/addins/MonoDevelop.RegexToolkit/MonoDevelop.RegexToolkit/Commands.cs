//
// Commands.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.RegexToolkit
{
	enum Commands
	{
		ShowRegexToolkit
	}
	
	class ViewOnlyContent : AbstractViewContent
	{
		Widget widget;
		
		public override Widget Control {
			get {
				return widget;
			}
		}
		
		public ViewOnlyContent (Widget widget, string contentName)
		{
			this.widget = widget;
			this.ContentName = contentName;
			IsViewOnly = true;
		}
		
		public override void Load (string fileName)
		{
			throw new System.NotImplementedException ();
		}
		
	}
	
	class DefaultAttachableViewContent : AbstractAttachableViewContent
	{
		Widget widget;
		
		public override Widget Control {
			get {
				return widget;
			}
		}
		string tabPageLabel;
		public override string TabPageLabel {
			get {
				return tabPageLabel;
			}
		}
		
		public DefaultAttachableViewContent (Widget widget, string contentName)
		{
			this.widget = widget;
			this.tabPageLabel = contentName;
		}
	}
	
	class ShowRegexToolkitHandler : CommandHandler
	{
		protected override void Run ()
		{
			foreach (var document in IdeApp.Workbench.Documents) {
				if (document.Window.ViewContent.Control is RegexToolkitWidget) {
					document.Window.SelectWindow ();
					return;
				}
			}
			var regexToolkit = new RegexToolkitWidget ();
			var newDocument = IdeApp.Workbench.OpenDocument (new ViewOnlyContent (regexToolkit, GettextCatalog.GetString ("Regex Toolkit")), true);
			
			var elementHelp = new ElementHelpWidget (newDocument.Window, regexToolkit);
			
			newDocument.Window.AttachViewContent (new DefaultAttachableViewContent (elementHelp, GettextCatalog.GetString ("Elements")));
		}
	}
	
	
}
