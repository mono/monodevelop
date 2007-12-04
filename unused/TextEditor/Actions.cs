//  Actions.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System.Drawing;
using System;

using MonoDevelop.Core.Properties;
using MonoDevelop.DefaultEditor.Gui.Editor;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.TextEditor.Actions;
using MonoDevelop.TextEditor;
using MonoDevelop.TextEditor.Gui.CompletionWindow;

namespace MonoDevelop.DefaultEditor.Actions
{
	public class TemplateCompletion : AbstractEditAction
	{
		public override void Execute(TextArea services)
		{
			CompletionWindow completionWindow = new CompletionWindow(services.MotherTextEditorControl, "a.cs", new TemplateCompletionDataProvider());
			completionWindow.ShowCompletionWindow('\0');
		}
	}
}
