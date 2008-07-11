//
// ActionGroupView.cs
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
using System.Collections;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.DesignerSupport.PropertyGrid;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class ActionGroupView: CombinedDesignView
	{
		Stetic.ActionGroupDesigner designer;
		CodeBinder codeBinder;
		GuiBuilderProject project;
		Stetic.ActionGroupComponent group;
		Stetic.ActionGroupInfo groupInfo;
		string groupName;
		
		public ActionGroupView (IViewContent content, Stetic.ActionGroupInfo group, GuiBuilderProject project): base (content)
		{
			groupName = group.Name;
			this.project = project;
			LoadDesigner ();
		}
		
		void LoadDesigner ()
		{
			groupInfo = project.GetActionGroup (groupName);
			if (groupInfo == null)
				// Group not found
				return;
			
			group = (Stetic.ActionGroupComponent) groupInfo.Component;
			project.Unloaded += OnDisposeProject;
			
			designer = project.SteticProject.CreateActionGroupDesigner (groupInfo, false);
			designer.AllowActionBinding = project.Project.UsePartialTypes;
			designer.BindField += new EventHandler (OnBindField);
			
			ActionGroupPage actionsPage = new ActionGroupPage (designer);
			actionsPage.PackStart (designer, true, true, 0);
			actionsPage.ShowAll ();
			
			AddButton (GettextCatalog.GetString ("Actions"), actionsPage);
			
			designer.ModifiedChanged += OnGroupModified;
			designer.SignalAdded += OnSignalAdded;
			designer.SignalChanged += OnSignalChanged;
			designer.RootComponentChanged += OnRootComponentChanged;

			codeBinder = new CodeBinder (project.Project, new OpenDocumentFileProvider (), designer.RootComponent);
		}
		
		public void CloseDesigner ()
		{
			if (designer == null)
				return;
			project.Unloaded -= OnDisposeProject;
			designer.BindField -= OnBindField;
			designer.RootComponentChanged -= OnRootComponentChanged;
			designer.ModifiedChanged -= OnGroupModified;
			designer.SignalAdded -= OnSignalAdded;
			designer.SignalChanged -= OnSignalChanged;
			designer.Destroy ();
			designer = null;
			
			project.Reloaded += OnReloadProject;
		}
		
		public override Stetic.Designer Designer {
			get { return designer; }
		}
		
		void OnDisposeProject (object s, EventArgs args)
		{
			RemoveButton (1);
			CloseDesigner ();
		}
		
		void OnReloadProject (object s, EventArgs args)
		{
			if (designer == null)
				LoadDesigner ();
		}
		
		public Stetic.ActionGroupComponent ActionGroup {
			get { return group; }
			set { Load (value.Name); }
		}
		
		public override void ShowPage (int npage)
		{
			if (designer != null && group != null) {
				// At every page switch update the generated code, to make sure code completion works
				// for the generated fields. The call to GenerateSteticCodeStructure will generate
				// the code for the window (only the fields in fact) and update the parser database, it
				// will not save the code to disk.
				if (project.Project.UsePartialTypes)
					GuiBuilderService.GenerateSteticCodeStructure ((DotNetProject)project.Project, designer.RootComponent, null, false, false);
			}
			base.ShowPage (npage);
		}
		
		void OnRootComponentChanged (object s, EventArgs args)
		{
			codeBinder.TargetObject = designer.RootComponent;
		}
		
		public override void Save (string fileName)
		{
			string oldBuildFile = GuiBuilderService.GetBuildCodeFileName (project.Project, groupInfo.Name);
			
			base.Save (fileName);
			if (designer == null)
				return;

			codeBinder.UpdateBindings (fileName);
			
			designer.Save ();
			
			string newBuildFile = GuiBuilderService.GetBuildCodeFileName (project.Project, groupInfo.Name);
			if (oldBuildFile != newBuildFile)
				FileService.MoveFile (oldBuildFile, newBuildFile);

			project.Save (true);
		}
		
		public override void Dispose ()
		{
			CloseDesigner ();
			project.Reloaded -= OnReloadProject;
			base.Dispose ();
		}
		
		public void ShowDesignerView ()
		{
			ShowPage (1);
		}
		
		public void SelectAction (Stetic.ActionComponent action)
		{
			if (designer != null)
				designer.SelectedAction = action;
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
		
		void OnGroupModified (object s, EventArgs a)
		{
			if (designer.Modified)
				OnContentChanged (a);
			IsDirty = designer.Modified;
		}
		
		void OnSignalAdded (object s, Stetic.ComponentSignalEventArgs a)
		{
			codeBinder.BindSignal (a.Signal);
		}
		
		void OnSignalChanged (object s, Stetic.ComponentSignalEventArgs a)
		{
			codeBinder.UpdateSignal (a.OldSignal, a.Signal);
		}
		
		void OnBindField (object s, EventArgs args)
		{
			if (designer.SelectedAction != null) {
				codeBinder.BindToField (designer.SelectedAction);
			}
		}
	}
	
	class ActionGroupPage: Gtk.VBox, ICustomPropertyPadProvider
	{
		Stetic.ActionGroupDesigner actionsBox;
		
		public ActionGroupPage (Stetic.ActionGroupDesigner actionsBox)
		{
			this.actionsBox = actionsBox;
		}
		
		Gtk.Widget ICustomPropertyPadProvider.GetCustomPropertyWidget ()
		{
			return PropertiesWidget.Instance;
		}
		
		void ICustomPropertyPadProvider.DisposeCustomPropertyWidget ()
		{
		}
		
		[CommandHandler (EditCommands.Delete)]
		protected void OnDelete ()
		{
			actionsBox.DeleteSelection ();
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		protected void OnUpdateDelete (CommandInfo cinfo)
		{
			cinfo.Enabled = actionsBox.SelectedAction != null;
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			actionsBox.CopySelection ();
		}
		
		[CommandUpdateHandler (EditCommands.Copy)]
		protected void OnUpdateCopy (CommandInfo cinfo)
		{
			cinfo.Enabled = actionsBox.SelectedAction != null;
		}
		
		[CommandHandler (EditCommands.Cut)]
		protected void OnCut ()
		{
			actionsBox.CutSelection ();
		}
		
		[CommandUpdateHandler (EditCommands.Cut)]
		protected void OnUpdateCut (CommandInfo cinfo)
		{
			cinfo.Enabled = actionsBox.SelectedAction != null;
		}
		
		[CommandHandler (EditCommands.Paste)]
		protected void OnPaste ()
		{
			actionsBox.PasteToSelection ();
		}
		
		[CommandUpdateHandler (EditCommands.Paste)]
		protected void OnUpdatePaste (CommandInfo cinfo)
		{
			cinfo.Enabled = false;
		}
		
		[CommandHandler (EditCommands.Undo)]
		protected void OnUndo ()
		{
			actionsBox.UndoQueue.Undo ();
		}
		
		[CommandHandler (EditCommands.Redo)]
		protected void OnRedo ()
		{
			actionsBox.UndoQueue.Redo ();
		}
		
		[CommandUpdateHandler (EditCommands.Undo)]
		protected void OnUpdateUndo (CommandInfo cinfo)
		{
			cinfo.Enabled = actionsBox.UndoQueue.CanUndo;
		}
		
		[CommandUpdateHandler (EditCommands.Redo)]
		protected void OnUpdateRedo (CommandInfo cinfo)
		{
			cinfo.Enabled = actionsBox.UndoQueue.CanRedo;
		}
	}
}
