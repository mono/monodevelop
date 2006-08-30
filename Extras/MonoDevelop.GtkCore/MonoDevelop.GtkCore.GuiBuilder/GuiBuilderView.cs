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
		Stetic.ActionGroupDesigner actionsBox;
		MonoDevelopWidgetActionBar widgetBar;
		VBox actionsPage;
		
		Gtk.Widget currentDesigner;
		bool actionsButtonVisible;
		
		public GuiBuilderView (IViewContent content, GuiBuilderWindow window): base (content)
		{
			editSession = window.CreateEditSession (new OpenDocumentFileProvider ());
			editSession.ModifiedChanged += new EventHandler (OnWindowChanged);
			
			designerPage = new DesignerPage (editSession);
			designerPage.Show ();
			AddButton (GettextCatalog.GetString ("Designer"), designerPage);
			
			// Actions designer
			
			MonoDevelopActionGroupToolbar groupToolbar = new MonoDevelopActionGroupToolbar (editSession.RootWidget.LocalActionGroups);
			groupToolbar.BindField += new EventHandler (OnBindActionField);
			actionsBox = Stetic.UserInterface.CreateActionGroupDesigner (editSession.SteticProject, groupToolbar);
			actionsPage = new ActionsPage (actionsBox.Editor);
			actionsPage.PackStart (actionsBox, true, true, 0);
			actionsPage.ShowAll ();
			
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
			
			if (editSession.RootWidget.LocalActionGroups.Count > 0) {
				AddButton (GettextCatalog.GetString ("Actions"), actionsPage);
				actionsButtonVisible = true;
			} else {
				editSession.RootWidget.LocalActionGroups.ActionGroupAdded += new Stetic.Wrapper.ActionGroupEventHandler (OnActionGroupAdded);
			}
			
			editSession.RootWidgetChanged += new EventHandler (OnRootWidgetChanged);
		}
		
		public override void Dispose ()
		{
			actionsPage.Dispose ();
			actionsBox.Destroy ();
			widgetBar.Destroy ();
			editSession.Dispose ();
			designerPage.Dispose ();
			editSession = null;
			actionsBox = null;
			widgetBar = null;
			widgetBar = null;
			designerPage = null;
			designerBox = null;
			base.Dispose ();
		}
		
		protected override void OnDocumentActivated ()
		{
			editSession.SetDesignerActive ();
		}
		
		void OnActionGroupAdded (object s, Stetic.Wrapper.ActionGroupEventArgs args)
		{
			AddButton (GettextCatalog.GetString ("Actions"), actionsBox);
			editSession.RootWidget.LocalActionGroups.ActionGroupAdded -= new Stetic.Wrapper.ActionGroupEventHandler (OnActionGroupAdded);
		}
		
		void OnWindowChanged (object s, EventArgs args)
		{
			OnContentChanged (args);
			OnDirtyChanged (args);
		}
		
		void OnRootWidgetChanged (object o, EventArgs a)
		{
			if (!actionsButtonVisible) {
				widgetBar.RootWidget.LocalActionGroups.ActionGroupAdded -= new Stetic.Wrapper.ActionGroupEventHandler (OnActionGroupAdded);
				editSession.RootWidget.LocalActionGroups.ActionGroupAdded += new Stetic.Wrapper.ActionGroupEventHandler (OnActionGroupAdded);
			}
			
			designerBox.Remove (currentDesigner);
			currentDesigner = editSession.WrapperWidget;
			designerBox.PackStart (currentDesigner, true, true, 0);
			widgetBar.RootWidget = editSession.RootWidget;
			editSession.WrapperWidget.ShowAll ();
			actionsBox.Toolbar.ActionGroups = editSession.RootWidget.LocalActionGroups;
		}
		
		void OnBindWidgetField (object o, EventArgs a)
		{
			editSession.BindCurrentWidget ();
		}
		
		void OnBindActionField (object o, EventArgs a)
		{
			if (actionsBox.Editor.SelectedAction != null)
				editSession.BindAction (actionsBox.Editor.SelectedAction);
		}
		
		public GuiBuilderEditSession EditSession {
			get { return editSession; }
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
		
		public void ShowActionDesignerView (string name)
		{
			ShowPage (2);
			foreach (Stetic.Wrapper.ActionGroup grp in editSession.RootWidget.LocalActionGroups) {
				if (grp.Name == name)
					actionsBox.Toolbar.ActiveGroup = grp;
			}
		}
	}
	
	class DesignerPage: Gtk.EventBox
	{
		GuiBuilderEditSession editSession;
		
		public DesignerPage (GuiBuilderEditSession editSession)
		{
			this.editSession = editSession;
		}
		
		[CommandHandler (EditCommands.Delete)]
		protected void OnDelete ()
		{
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (editSession.SteticProject.Selection);
			wrapper.Delete ();
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		protected void OnUpdateDelete (CommandInfo cinfo)
		{
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (editSession.SteticProject.Selection);
			cinfo.Enabled = wrapper != null;
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			Stetic.Clipboard.Copy (editSession.SteticProject.Selection);
		}
		
		[CommandUpdateHandler (EditCommands.Copy)]
		protected void OnUpdateCopy (CommandInfo cinfo)
		{
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (editSession.SteticProject.Selection);
			cinfo.Enabled = wrapper != null;
		}
		
		[CommandHandler (EditCommands.Cut)]
		protected void OnCut ()
		{
			Stetic.Clipboard.Cut (editSession.SteticProject.Selection);
		}
		
		[CommandUpdateHandler (EditCommands.Cut)]
		protected void OnUpdateCut (CommandInfo cinfo)
		{
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (editSession.SteticProject.Selection);
			cinfo.Enabled = wrapper != null;
		}
		
		[CommandHandler (EditCommands.Paste)]
		protected void OnPaste ()
		{
			Stetic.Clipboard.Paste (editSession.SteticProject.Selection as Stetic.Placeholder);
		}
		
		[CommandUpdateHandler (EditCommands.Paste)]
		protected void OnUpdatePaste (CommandInfo cinfo)
		{
			cinfo.Enabled = editSession.SteticProject.Selection is Stetic.Placeholder;
		}
		
		[CommandUpdateHandler (EditCommands.Undo)]
		protected void OnUpdateUndo (CommandInfo cinfo)
		{
			// Not yet supported
			cinfo.Enabled = false;
		}
		
		[CommandUpdateHandler (EditCommands.Redo)]
		protected void OnUpdateRedo (CommandInfo cinfo)
		{
			// Not yet supported
			cinfo.Enabled = false;
		}
	}
	
	class ActionsPage: VBox
	{
		Stetic.Editor.ActionGroupEditor actionsBox;
		
		public ActionsPage (Stetic.Editor.ActionGroupEditor actionsBox)
		{
			this.actionsBox = actionsBox;
		}
		
		[CommandHandler (EditCommands.Delete)]
		protected void OnDelete ()
		{
			actionsBox.Delete ();
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		protected void OnUpdateDelete (CommandInfo cinfo)
		{
			cinfo.Enabled = actionsBox.SelectedAction != null;
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			actionsBox.Copy ();
		}
		
		[CommandUpdateHandler (EditCommands.Copy)]
		protected void OnUpdateCopy (CommandInfo cinfo)
		{
			cinfo.Enabled = actionsBox.SelectedAction != null;
		}
		
		[CommandHandler (EditCommands.Cut)]
		protected void OnCut ()
		{
			actionsBox.Cut ();
		}
		
		[CommandUpdateHandler (EditCommands.Cut)]
		protected void OnUpdateCut (CommandInfo cinfo)
		{
			cinfo.Enabled = actionsBox.SelectedAction != null;
		}
		
		[CommandHandler (EditCommands.Paste)]
		protected void OnPaste ()
		{
			actionsBox.Paste ();
		}
		
		[CommandUpdateHandler (EditCommands.Paste)]
		protected void OnUpdatePaste (CommandInfo cinfo)
		{
			cinfo.Enabled = false;
		}
		
		[CommandUpdateHandler (EditCommands.Undo)]
		protected void OnUpdateUndo (CommandInfo cinfo)
		{
			// Not yet supported
			cinfo.Enabled = false;
		}
		
		[CommandUpdateHandler (EditCommands.Redo)]
		protected void OnUpdateRedo (CommandInfo cinfo)
		{
			// Not yet supported
			cinfo.Enabled = false;
		}
	}
}

