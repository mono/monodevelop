//
// GuiBuilderView.cs
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;

using Gtk;
using Gdk;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderView : CombinedDesignView
	{
		GuiBuilderEditSession editSession;
		Gtk.EventBox designerPage;
		VBox designerBox;
		VBox actionsBox;
		Stetic.Editor.ActionGroupEditor agroupEditor;
		MonoDevelopActionGroupToolbar groupToolbar;
		MonoDevelopWidgetActionBar widgetBar;
		
		Gtk.Widget currentDesigner;
		
		public GuiBuilderView (IViewContent content, GuiBuilderWindow window): base (content)
		{
			editSession = window.CreateEditSession (new OpenDocumentFileProvider ());
			editSession.ModifiedChanged += new EventHandler (OnWindowChanged);
			
			designerPage = new Gtk.EventBox ();
			designerPage.Show ();
			AddButton (GettextCatalog.GetString ("Designer"), designerPage);
			
			// Actions designer
			
			actionsBox = new Gtk.VBox ();
			agroupEditor = new Stetic.Editor.ActionGroupEditor ();
			agroupEditor.Project = editSession.SteticProject;
			Gtk.Widget groupDesign = Stetic.EmbedWindow.Wrap (agroupEditor, -1, -1);
			groupToolbar = new MonoDevelopActionGroupToolbar (editSession.RootWidget.LocalActionGroups);
			groupToolbar.Bind (agroupEditor);
			groupToolbar.BindField += new EventHandler (OnBindActionField);
			actionsBox.BorderWidth = 3;
			actionsBox.PackStart (groupToolbar, false, false, 0);
			actionsBox.PackStart (groupDesign, true, true, 3);
			actionsBox.ShowAll ();
			
			AddButton (GettextCatalog.GetString ("Actions"), actionsBox);
			
			// Widget toolbar
			widgetBar = new MonoDevelopWidgetActionBar (editSession.RootWidget);
			widgetBar.BorderWidth = 3;
			widgetBar.BindField += new EventHandler (OnBindWidgetField);
			
			// Designer page
			designerBox = new VBox ();
			designerPage.Add (designerBox);
			designerBox.PackStart (widgetBar, false, false, 0);
			designerBox.ShowAll ();
			
			designerBox.PackStart (editSession.WrapperWidget, true, true, 0);
			
			currentDesigner = editSession.WrapperWidget;
			
			editSession.RootWidgetChanged += new EventHandler (OnRootWidgetChanged);
		}
		
		void OnWindowChanged (object s, EventArgs args)
		{
			OnContentChanged (args);
			OnDirtyChanged (args);
		}
		
		void OnRootWidgetChanged (object o, EventArgs a)
		{
			designerBox.Remove (currentDesigner);
			currentDesigner = editSession.WrapperWidget;
			designerBox.PackStart (currentDesigner, true, true, 0);
			widgetBar.RootWidget = editSession.RootWidget;
			editSession.WrapperWidget.ShowAll ();
			groupToolbar.ActionGroups = editSession.RootWidget.LocalActionGroups;
		}
		
		void OnBindWidgetField (object o, EventArgs a)
		{
			editSession.BindCurrentWidget ();
		}
		
		void OnBindActionField (object o, EventArgs a)
		{
			if (agroupEditor.SelectedAction != null)
				editSession.BindAction (agroupEditor.SelectedAction);
		}
		
		public GuiBuilderEditSession EditSession {
			get { return editSession; }
		}
		
		public override void Dispose ()
		{
			agroupEditor.Dispose ();
			groupToolbar.Dispose ();
			widgetBar.Dispose ();
			designerPage.Remove (editSession.WrapperWidget);
			editSession.Dispose ();
			base.Dispose ();
		}
		
		public override void Save (string fileName)
		{
			base.Save (fileName);
			editSession.UpdateBindings (fileName);
			editSession.Save ();
		}
		
		public override bool IsDirty {
			get {
				return base.IsDirty || editSession.Modified;
			}
			set {
				base.IsDirty = value;
			}
		}
		
		public override void JumpToSignalHandler (Stetic.Signal signal)
		{
			IClass cls = editSession.GetClass ();
			foreach (IMethod met in cls.Methods) {
				if (met.Name == signal.Handler) {
					ShowPage (1);
					JumpTo (met.Region.BeginLine, met.Region.BeginColumn);
					break;
				}
			}
		}
		
		public void ShowDesignerView ()
		{
			ShowPage (1);
		}
		
		/* Commands *********************************/
		
		[CommandHandler (EditCommands.Delete)]
		protected void OnDelete ()
		{
			if (editSession.SteticProject.Selection != null) {
				Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (editSession.SteticProject.Selection);
				wrapper.Delete ();
			}
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			if (editSession.SteticProject.Selection != null)
				Stetic.Clipboard.Copy (editSession.SteticProject.Selection);
		}
		
		[CommandHandler (EditCommands.Cut)]
		protected void OnCut ()
		{
			if (editSession.SteticProject.Selection != null)
				Stetic.Clipboard.Cut (editSession.SteticProject.Selection);
		}
		
		[CommandHandler (EditCommands.Paste)]
		protected void OnPaste ()
		{
			if (editSession.SteticProject.Selection != null)
				Stetic.Clipboard.Cut (editSession.SteticProject.Selection as Stetic.Placeholder);
		}
	}
}

