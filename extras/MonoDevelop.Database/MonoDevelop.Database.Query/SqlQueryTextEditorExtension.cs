// 
// SqlQueryTextEditorExtension.cs
//  
// Author:
//       Luciano N. Callero <lnc19@hotmail.com>
// 
// Copyright (c) 2009 Luciano N. Callero
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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
	
namespace MonoDevelop.Database.Query
{
	public class SqlQueryTextEditorExtension:TextEditorExtension
	{
		// public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		// {
		// }
		
	}
	
	internal class QueryInitializer {
		
		public QueryInitializer ()
		{
			IdeApp.CommandService.RegisterGlobalHandler (new DatabaseCommandHandler ());
		}
	}
	
	internal class DatabaseCommandHandler {
		
		[CommandHandler (MonoDevelop.Debugger.DebugCommands.Debug)]
		protected void Run ()
		{
			if (IdeApp.Workbench.ActiveDocument != null && 
			    IdeApp.Workbench.ActiveDocument.ActiveView is ISqlQueryEditorView)
				((ISqlQueryEditorView)IdeApp.Workbench.ActiveDocument.ActiveView).RunQuery ();
		}

		[CommandUpdateHandler (MonoDevelop.Debugger.DebugCommands.Debug)]
		protected void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.ActiveView is ISqlQueryEditorView) {
				info.Icon = "md-db-execute";
				info.Text = "Execute _Query";
				return;
			} else
				info.Bypass = true;
		}
		
	}
}
