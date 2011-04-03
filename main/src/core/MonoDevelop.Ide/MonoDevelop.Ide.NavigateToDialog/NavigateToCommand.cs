// 
// NavigateToCommand.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.NavigateToDialog
{
	public enum Commands {
		NavigateTo
	}
	
	class GotoTypeHandler : CommandHandler
	{
		protected override void Run ()
		{
			var dialog = new NavigateToDialog (NavigateToType.Types, false) {
				Title = GettextCatalog.GetString ("Go to Type"),
			};
			IEnumerable<NavigateToDialog.OpenLocation> locations = null;
			try {
				if (MessageService.RunCustomDialog (dialog, MessageService.RootWindow) == (int)ResponseType.Ok) {
					dialog.Sensitive = false;
					locations = dialog.Locations;
				}
			} finally {
				dialog.Destroy ();
			}
			if (locations != null) {
				foreach (var loc in locations)
					IdeApp.Workbench.OpenDocument (loc.Filename, loc.Line, loc.Column);
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workspace.IsOpen || IdeApp.Workbench.Documents.Count != 0;
		}
	}
	
	class GotoFileHandler : CommandHandler
	{
		protected override void Run ()
		{
			var dialog = new NavigateToDialog (NavigateToType.Files, false) {
				Title = GettextCatalog.GetString ("Go to File"),
			};
			IEnumerable<NavigateToDialog.OpenLocation> locations = null;
			try {
				if (MessageService.RunCustomDialog (dialog, MessageService.RootWindow) == (int)ResponseType.Ok) {
					dialog.Sensitive = false;
					locations = dialog.Locations;
				}
			} finally {
				dialog.Destroy ();
			}
			if (locations != null) {
				foreach (var loc in locations)
					IdeApp.Workbench.OpenDocument (loc.Filename, loc.Line, loc.Column);
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workspace.IsOpen || IdeApp.Workbench.Documents.Count != 0;
		}
	}	
	
	class NavigateToHandler : CommandHandler
	{
		protected override void Run ()
		{
			var dialog = new NavigateToDialog (NavigateToType.All, true);
			IEnumerable<NavigateToDialog.OpenLocation> locations = null;
			try {
				if (MessageService.RunCustomDialog (dialog, MessageService.RootWindow) == (int)ResponseType.Ok) {
					dialog.Sensitive = false;
					locations = dialog.Locations;
				}
			} finally {
				dialog.Destroy ();
			}
			if (locations != null) {
				foreach (var loc in locations)
					IdeApp.Workbench.OpenDocument (loc.Filename, loc.Line, loc.Column);
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workspace.IsOpen || IdeApp.Workbench.Documents.Count != 0;
		}
	}
}

