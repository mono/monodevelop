//
// GuiBuilderProjectPad.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderProjectPad: AbstractPadContent
	{
		Stetic.ProjectView pview;
		
		public GuiBuilderProjectPad ()
		{
			DefaultPlacement = "MonoDevelop.GtkCore.GuiBuilder.GuiBuilderPropertiesPad/center; bottom";
			pview = GuiBuilderService.SteticApp.ProjectWidget;
			pview.ShowAll ();
		}
		
		public override Gtk.Widget Control {
			get { return pview; }
		}
		
		public void Fill (object o)
		{
		}
		
		[CommandHandler (EditCommands.Undo)]
		protected void OnUndo ()
		{
//			GuiBuilderService.App.CommandUndo ();
		}
		
		[CommandHandler (EditCommands.Redo)]
		protected void OnRedo ()
		{
//			GuiBuilderService.App.CommandRedo ();
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
//			GuiBuilderService.App.CommandCopy ();
		}
		
		[CommandHandler (EditCommands.Cut)]
		protected void OnCut ()
		{
//			GuiBuilderService.App.CommandCut ();
		}
		
		[CommandHandler (EditCommands.Paste)]
		protected void OnPaste ()
		{
//			GuiBuilderService.App.CommandPaste ();
		}
		
		[CommandHandler (EditCommands.Delete)]
		protected void OnDelete ()
		{
//			GuiBuilderService.App.CommandDelete ();
		}
	}
}
