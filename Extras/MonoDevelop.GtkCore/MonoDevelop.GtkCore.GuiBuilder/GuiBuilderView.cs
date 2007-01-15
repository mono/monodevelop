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
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;

using Gtk;
using Gdk;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderView : CombinedDesignView
	{
		Stetic.WidgetDesigner designer;
		Stetic.ActionGroupDesigner actionsBox;
		GuiBuilderWindow window;
		
		Gtk.EventBox designerPage;
		VBox actionsPage;
		
		bool actionsButtonVisible;
		
		CodeBinder codeBinder;
		
		public GuiBuilderView (IViewContent content, GuiBuilderWindow window): base (content)
		{
			this.window = window;
			
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (window.Project.Project);
			designer = window.Project.SteticProject.CreateWidgetDesigner (window.RootWidget, false);
			designer.AllowWidgetBinding = (info != null && !info.GeneratePartialClasses);
			
			codeBinder = new CodeBinder (window.Project.Project, new OpenDocumentFileProvider (), designer.RootComponent);
			
			designer.BindField += OnBindWidgetField;
			designer.ModifiedChanged += OnWindowModifiedChanged;
			designer.SignalAdded += OnSignalAdded;
			designer.SignalRemoved += OnSignalRemoved;
			designer.SignalChanged += OnSignalChanged;
			designer.ComponentNameChanged += OnComponentNameChanged;
			designer.RootComponentChanged += OnRootComponentChanged;
			
			// Designer page
			designerPage = new DesignerPage (designer);
			designerPage.Show ();
			designerPage.Add (designer);

			AddButton (GettextCatalog.GetString ("Designer"), designerPage);
			
			// Actions designer
			actionsBox = designer.CreateActionGroupDesigner ();
			actionsBox.AllowActionBinding = (info != null && !info.GeneratePartialClasses);
			actionsBox.BindField += new EventHandler (OnBindActionField);
			actionsBox.ModifiedChanged += new EventHandler (OnActionshanged);
			
			actionsPage = new ActionGroupPage (actionsBox);
			actionsPage.PackStart (actionsBox, true, true, 0);
			actionsPage.ShowAll ();
			
			if (actionsBox.HasData) {
				AddButton (GettextCatalog.GetString ("Actions"), actionsPage);
				actionsButtonVisible = true;
			}
			
			designer.ShowAll ();
			designer.SetActive ();
		}
		
		public GuiBuilderWindow Window {
			get { return window; }
		}
		
		public void SetActive ()
		{
			designer.SetActive ();
		}
		
		public override void Dispose ()
		{
			designer.BindField -= OnBindWidgetField;
			designer.ModifiedChanged -= OnWindowModifiedChanged;
			designer.SignalAdded -= OnSignalAdded;
			designer.SignalRemoved -= OnSignalRemoved;
			designer.SignalChanged -= OnSignalChanged;
			designer.ComponentNameChanged -= OnComponentNameChanged;
			designer.RootComponentChanged -= OnRootComponentChanged;
			
			actionsPage.Dispose ();
			actionsBox.Dispose ();
			actionsBox.Destroy ();
			designerPage.Dispose ();
			actionsBox = null;
			designerPage = null;
			base.Dispose ();
		}
		
		protected override void OnDocumentActivated ()
		{
//			editSession.SetDesignerActive ();
			// FIXME
		}
		
		public override void ShowPage (int npage)
		{
			if (window != null) {
				// At every page switch update the generated code, to make sure code completion works
				// for the generated fields. The call to GenerateSteticCodeStructure will generate
				// the code for the window (only the fields in fact) and update the parser database, it
				// will not save the code to disk.
				GtkDesignInfo info = GtkCoreService.GetGtkInfo (window.Project.Project);
				if (info != null && info.GeneratePartialClasses)
					GuiBuilderService.GenerateSteticCodeStructure ((DotNetProject)window.Project.Project, designer.RootComponent, false, false);
			}
			base.ShowPage (npage);
		}
		
		void OnRootComponentChanged (object s, EventArgs args)
		{
			codeBinder.TargetObject = designer.RootComponent;
		}
		
		void OnComponentNameChanged (object s, Stetic.ComponentNameEventArgs args)
		{
			codeBinder.UpdateField (args.Component, args.OldName);
		}
		
		void OnActionshanged (object s, EventArgs args)
		{
			if (!actionsButtonVisible) {
				actionsButtonVisible = true;
				AddButton (GettextCatalog.GetString ("Actions"), actionsPage);
			}
		}
		
		void OnWindowModifiedChanged (object s, EventArgs args)
		{
			if (IsDirty)
				OnContentChanged (args);
			OnDirtyChanged (args);
		}
		
		void OnBindWidgetField (object o, EventArgs a)
		{
			if (designer.Selection != null)
				codeBinder.BindToField (designer.Selection);
		}
		
		void OnBindActionField (object o, EventArgs a)
		{
			if (actionsBox.SelectedAction != null)
				codeBinder.BindToField (actionsBox.SelectedAction);
		}
		
		void OnSignalAdded (object sender, Stetic.ComponentSignalEventArgs args)
		{
			codeBinder.BindSignal (args.Signal);
		}

		void OnSignalRemoved (object sender, Stetic.ComponentSignalEventArgs args)
		{
		}

		void OnSignalChanged (object sender, Stetic.ComponentSignalEventArgs args)
		{
			codeBinder.UpdateSignal (args.OldSignal, args.Signal);
		}
		
		public override void Save (string fileName)
		{
			base.Save (fileName);
			
			string oldName = window.RootWidget.Name;
			string oldBuildFile = GuiBuilderService.GetBuildCodeFileName (window.Project.Project, window.RootWidget);
 
			codeBinder.UpdateBindings (fileName);
			designer.Save ();
			actionsBox.Save ();
			
			string newBuildFile = GuiBuilderService.GetBuildCodeFileName (window.Project.Project, window.RootWidget);
			
			if (oldBuildFile != newBuildFile)
				Runtime.FileService.MoveFile (oldBuildFile, newBuildFile);
			
			window.Project.Save ();
			
			if (window.RootWidget.Name != oldName) {
				// The name of the component has changed. If this component is being
				// exported by the library, then the component reference also has to
				// be updated in the project configuration
				
				GtkDesignInfo info = GtkCoreService.GetGtkInfo (window.Project.Project);
				if (info.IsExported (oldName)) {
					info.RemoveExportedWidget (oldName);
					info.AddExportedWidget (codeBinder.TargetObject.Name);
					info.UpdateGtkFolder ();
					GtkCoreService.UpdateObjectsFile (window.Project.Project);
					IdeApp.ProjectOperations.SaveProject (window.Project.Project);
				}
			}
		}
		
		public override bool IsDirty {
			get {
				// There is no need to check if the action group designer is modified
				// since changes in the action group are as well changes in the designed widget
				return base.IsDirty || designer.Modified;
			}
			set {
				base.IsDirty = value;
			}
		}
		
		public override void JumpToSignalHandler (Stetic.Signal signal)
		{
			IClass cls = codeBinder.GetClass ();
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
			actionsBox.ActiveGroup = name;
		}
	}
	
	class DesignerPage: Gtk.EventBox
	{
		Stetic.WidgetDesigner designer;
		
		public DesignerPage (Stetic.WidgetDesigner designer)
		{
			this.designer = designer;
		}
		
		[CommandHandler (EditCommands.Delete)]
		protected void OnDelete ()
		{
			designer.DeleteSelection ();
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		protected void OnUpdateDelete (CommandInfo cinfo)
		{
			cinfo.Enabled = designer.Selection != null && designer.Selection.CanCut;
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			designer.CopySelection ();
		}
		
		[CommandUpdateHandler (EditCommands.Copy)]
		protected void OnUpdateCopy (CommandInfo cinfo)
		{
			cinfo.Enabled = designer.Selection != null && designer.Selection.CanCopy;
		}
		
		[CommandHandler (EditCommands.Cut)]
		protected void OnCut ()
		{
			designer.CutSelection ();
		}
		
		[CommandUpdateHandler (EditCommands.Cut)]
		protected void OnUpdateCut (CommandInfo cinfo)
		{
			cinfo.Enabled = designer.Selection != null && designer.Selection.CanCut;
		}
		
		[CommandHandler (EditCommands.Paste)]
		protected void OnPaste ()
		{
			designer.PasteToSelection ();
		}
		
		[CommandHandler (EditCommands.Undo)]
		protected void OnUndo ()
		{
			designer.UndoQueue.Undo ();
		}
		
		[CommandHandler (EditCommands.Redo)]
		protected void OnRedo ()
		{
			designer.UndoQueue.Redo ();
		}
		
		[CommandUpdateHandler (EditCommands.Paste)]
		protected void OnUpdatePaste (CommandInfo cinfo)
		{
			cinfo.Enabled = designer.Selection != null && designer.Selection.CanPaste;
		}
		
		[CommandUpdateHandler (EditCommands.Undo)]
		protected void OnUpdateUndo (CommandInfo cinfo)
		{
			cinfo.Enabled = designer.UndoQueue.CanUndo;
		}
		
		[CommandUpdateHandler (EditCommands.Redo)]
		protected void OnUpdateRedo (CommandInfo cinfo)
		{
			cinfo.Enabled = designer.UndoQueue.CanRedo;
		}
	}
}

