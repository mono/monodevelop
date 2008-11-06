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
using System.ComponentModel;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.DesignerSupport.Toolbox; 
using MonoDevelop.DesignerSupport.PropertyGrid;
using MonoDevelop.Deployment;

using Gtk;
using Gdk;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderView : CombinedDesignView, IToolboxConsumer, MonoDevelop.DesignerSupport.IOutlinedDocument
	{
		Stetic.WidgetDesigner designer;
		Stetic.ActionGroupDesigner actionsBox;
		GuiBuilderWindow window;
		
		Gtk.EventBox designerPage;
		VBox actionsPage;
		
		bool actionsButtonVisible;
		
		CodeBinder codeBinder;
		GuiBuilderProject gproject;
		string rootName;
		
		public GuiBuilderView (IViewContent content, GuiBuilderWindow window): base (content)
		{
			rootName = window.Name;
			gproject = window.Project;
			GtkDesignInfo info = GtkDesignInfo.FromProject (gproject.Project);
			gproject.SteticProject.ImagesRootPath = FileService.AbsoluteToRelativePath (info.GtkGuiFolder, gproject.Project.BaseDirectory);
			gproject.UpdateLibraries ();
			LoadDesigner ();
		}
		
		void LoadDesigner ()
		{
			this.window = gproject.GetWindow (rootName);
			if (window == null) {
				// The window doesn't exist anymore
				return;
			}
			
			gproject.Unloaded += OnDisposeProject;
			
			designer = gproject.SteticProject.CreateWidgetDesigner (window.RootWidget, false);
			if (designer.RootComponent == null) {
				// Something went wrong while creating the designer. Show it, but don't do aything else.
				AddButton (GettextCatalog.GetString ("Designer"), designer);
				designer.ShowAll ();
				return;
			}

			designer.AllowWidgetBinding = !gproject.Project.UsePartialTypes;
			
			codeBinder = new CodeBinder (gproject.Project, new OpenDocumentFileProvider (), designer.RootComponent);
			
			designer.BindField += OnBindWidgetField;
			designer.ModifiedChanged += OnWindowModifiedChanged;
			designer.SignalAdded += OnSignalAdded;
			designer.SignalRemoved += OnSignalRemoved;
			designer.SignalChanged += OnSignalChanged;
			designer.ComponentNameChanged += OnComponentNameChanged;
			designer.RootComponentChanged += OnRootComponentChanged;
			designer.ComponentTypesChanged += OnComponentTypesChanged;
			designer.ImportFileCallback = ImportFile;
			
			// Designer page
			designerPage = new DesignerPage (designer);
			designerPage.Show ();
			designerPage.Add (designer);

			AddButton (GettextCatalog.GetString ("Designer"), designerPage);
			
			// Actions designer
			actionsBox = designer.CreateActionGroupDesigner ();
			actionsBox.AllowActionBinding = !gproject.Project.UsePartialTypes;
			actionsBox.BindField += new EventHandler (OnBindActionField);
			actionsBox.ModifiedChanged += new EventHandler (OnActionshanged);
			
			actionsPage = new ActionGroupPage (actionsBox);
			actionsPage.PackStart (actionsBox, true, true, 0);
			actionsPage.ShowAll ();
			
			if (actionsBox.HasData) {
				AddButton (GettextCatalog.GetString ("Actions"), actionsPage);
				actionsButtonVisible = true;
			} else
				actionsButtonVisible = false;
			
			designer.ShowAll ();
			GuiBuilderService.SteticApp.ActiveDesigner = designer;
		}
		
		public override Stetic.Designer Designer {
			get { return designer; }
		}
		
		void OnDisposeProject (object s, EventArgs args)
		{
			if (actionsButtonVisible)
				RemoveButton (2);
			RemoveButton (1);
			CloseDesigner ();
		}
		
		void OnReloadProject (object s, EventArgs args)
		{
			if (designer == null)
				LoadDesigner ();
		}
		
		public GuiBuilderWindow Window {
			get { return window; }
		}
		
		void CloseDesigner ()
		{
			if (designer == null)
				return;
			gproject.Unloaded -= OnDisposeProject;
			designer.BindField -= OnBindWidgetField;
			designer.ModifiedChanged -= OnWindowModifiedChanged;
			designer.SignalAdded -= OnSignalAdded;
			designer.SignalRemoved -= OnSignalRemoved;
			designer.SignalChanged -= OnSignalChanged;
			designer.ComponentNameChanged -= OnComponentNameChanged;
			designer.RootComponentChanged -= OnRootComponentChanged;
			designer.ComponentTypesChanged -= OnComponentTypesChanged;
			
			if (designerPage != null)
				designerPage = null;
			
			if (actionsPage != null) {
				actionsBox.BindField -= OnBindActionField;
				actionsBox.ModifiedChanged -= OnActionshanged;
				actionsBox = null;
				actionsPage = null;
			}
			designer.Destroy ();
			designer = null;
			gproject.Reloaded += OnReloadProject;
		}
		
		public override void Dispose ()
		{
			CloseDesigner ();
			gproject.Reloaded -= OnReloadProject;
			codeBinder = null;
			base.Dispose ();
		}
		
		public override void ShowPage (int npage)
		{
			if (npage == 0 && designer != null && window != null && !ErrorMode) {
				// At every page switch update the generated code, to make sure code completion works
				// for the generated fields. The call to GenerateSteticCodeStructure will generate
				// the code for the window (only the fields in fact) and update the parser database, it
				// will not save the code to disk.
				if (gproject.Project.UsePartialTypes)
					GuiBuilderService.GenerateSteticCodeStructure ((DotNetProject)gproject.Project, designer.RootComponent, null, false, false);
			}
			base.ShowPage (npage);
		}
		
		string ImportFile (string file)
		{
			return GuiBuilderService.ImportFile (gproject.Project, file);
		}
		
		void OnRootComponentChanged (object s, EventArgs args)
		{
			codeBinder.TargetObject = designer.RootComponent;
		}
		
		void OnComponentNameChanged (object s, Stetic.ComponentNameEventArgs args)
		{
			try {
				// Make sure the fields in the partial class are up to date.
				// Provide the args parameter to GenerateSteticCodeStructure, in this
				// way the component that has been renamed will be generated with the
				// old name, and UpdateField will be able to find it (to rename the
				// references to the field, it needs to have the old name).
				if (gproject.Project.UsePartialTypes)
					GuiBuilderService.GenerateSteticCodeStructure ((DotNetProject)gproject.Project, designer.RootComponent, args, false, false);
				
				codeBinder.UpdateField (args.Component, args.OldName);
			}
			catch (Exception ex) {
				MessageService.ShowException (ex);
			}
		}
		
		void OnComponentTypesChanged (object s, EventArgs a)
		{
			if (ToolboxProvider.Instance != null)
				ToolboxProvider.Instance.NotifyItemsChanged ();
		}
		
		void OnActionshanged (object s, EventArgs args)
		{
			if (designer != null && !actionsButtonVisible && !ErrorMode) {
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
			
			if (designer == null)
				return;
			
			string oldBuildFile = GuiBuilderService.GetBuildCodeFileName (gproject.Project, window.RootWidget.Name);
			
			codeBinder.UpdateBindings (fileName);
			if (!ErrorMode) {
				if (designer != null)
					designer.Save ();
				if (actionsBox != null)
					actionsBox.Save ();
			}
			
			string newBuildFile = GuiBuilderService.GetBuildCodeFileName (gproject.Project, window.RootWidget.Name);
			
			if (oldBuildFile != newBuildFile) {
				if (System.IO.File.Exists (newBuildFile))
					FileService.DeleteFile (newBuildFile);
				FileService.MoveFile (oldBuildFile, newBuildFile);
			}
			
			gproject.Save (true);
		}
		
		public override bool IsDirty {
			get {
				// There is no need to check if the action group designer is modified
				// since changes in the action group are as well changes in the designed widget
				return base.IsDirty || (designer != null && designer.Modified);
			}
			set {
				base.IsDirty = value;
			}
		}
		
		public override void JumpToSignalHandler (Stetic.Signal signal)
		{
			IType cls = codeBinder.GetClass ();
			if (cls == null)
				return;
			foreach (IMethod met in cls.Methods) {
				if (met.Name == signal.Handler) {
					ShowPage (0);
					JumpTo (met.Location.Line, met.Location.Column);
					break;
				}
			}
		}
		
		public void ShowDesignerView ()
		{
			if (designer != null)
				ShowPage (1);
		}
		
		public void ShowActionDesignerView (string name)
		{
			if (designer != null) {
				ShowPage (2);
				if (!ErrorMode)
					actionsBox.ActiveGroup = name;
			}
		}
		
		bool ErrorMode {
			get { return designer.RootComponent == null; }
		}
		
		public Stetic.ComponentType[] GetComponentTypes ()
		{
			if (designer != null)
				return designer.GetComponentTypes ();
			else
				return null;
		}
		
		void IToolboxConsumer.ConsumeItem (ItemToolboxNode item)
		{
		}
		
		//Toolbox service uses this to filter toolbox items.
		ToolboxItemFilterAttribute[] IToolboxConsumer.ToolboxFilterAttributes {
			get {
				return new ToolboxItemFilterAttribute [] {
					new ToolboxItemFilterAttribute ("gtk-sharp", ToolboxItemFilterType.Custom)
				};
			}
		}
		
		//Used if ToolboxItemFilterAttribute demands ToolboxItemFilterType.Custom
		//If not expecting it, should just return false
		bool IToolboxConsumer.CustomFilterSupports (ItemToolboxNode item)
		{
			ComponentToolboxNode cnode = item as ComponentToolboxNode;
			if (cnode != null && gproject.SteticProject != null) {
				if (cnode.GtkVersion == null || Mono.Addins.Addin.CompareVersions (gproject.SteticProject.TargetGtkVersion, cnode.GtkVersion) <= 0)
					return true;
			}
			return false;
		}
		
		string IToolboxConsumer.DefaultItemDomain {
			get { return ComponentToolboxNode.GtkWidgetDomain; }
		}
			
		void IToolboxConsumer.DragItem (ItemToolboxNode item, Gtk.Widget source, Gdk.DragContext ctx)
		{
			if (designer != null) {
				ComponentToolboxNode node = item as ComponentToolboxNode;
				if (node != null) {
					if (node.Reference == null)
						designer.BeginComponentDrag (node.ComponentType, source, ctx);
					else
						designer.BeginComponentDrag (node.Name, node.ClassName, source, ctx, delegate { CheckReference (node); });
				}
			}
		}
			
		void CheckReference (ComponentToolboxNode node)
		{
				if (node.Reference == null)
					return;
				
				ProjectReference pref;
				
				// If the class name includes an assembly name it means that the
				// widget is implemented in another assembly, not in the one that
				// has the objects.xml file.
				int i = node.ClassName.IndexOf (',');
				if (i != -1) {
					string asm = node.ClassName.Substring (i+1).Trim ();
					asm = Runtime.SystemAssemblyService.GetAssemblyFullName (asm);
					if (asm == null)
						return;
					if (Runtime.SystemAssemblyService.GetPackageFromFullName (asm) != null) {
						pref = new ProjectReference (ReferenceType.Gac, asm);
					} else {
						asm = Runtime.SystemAssemblyService.GetAssemblyLocation (asm);
						pref = new ProjectReference (ReferenceType.Assembly, asm);
					}
				}
				else
					pref = new ProjectReference (node.ReferenceType, node.Reference);
				
				foreach (ProjectReference pr in gproject.Project.References) {
					if (pr.Reference == pref.Reference)
						return;
				}
				gproject.Project.References.Add (pref);
		}
		
		Widget MonoDevelop.DesignerSupport.IOutlinedDocument.GetOutlineWidget ()
		{
			return GuiBuilderDocumentOutline.Instance;
		}

		void MonoDevelop.DesignerSupport.IOutlinedDocument.ReleaseOutlineWidget ()
		{
			//Do nothing. We keep the instance to avoid creation cost when switching documents.
		}

		Gtk.TargetEntry[] targets = new Gtk.TargetEntry[] {
			new Gtk.TargetEntry ("application/x-stetic-widget", 0, 0)
		};
			
		TargetEntry[] IToolboxConsumer.DragTargets {
			get { return targets; }
		}
	}
	
	class DesignerPage: Gtk.EventBox, ICustomPropertyPadProvider
	{
		Stetic.WidgetDesigner designer;
		
		public DesignerPage (Stetic.WidgetDesigner designer)
		{
			this.designer = designer;
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
			designer.DeleteSelection ();
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		protected void OnUpdateDelete (CommandInfo cinfo)
		{
			cinfo.Bypass = !designer.CanDeleteSelection;
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			designer.CopySelection ();
		}
		
		[CommandUpdateHandler (EditCommands.Copy)]
		protected void OnUpdateCopy (CommandInfo cinfo)
		{
			cinfo.Enabled = designer.CanCopySelection;
		}
		
		[CommandHandler (EditCommands.Cut)]
		protected void OnCut ()
		{
			designer.CutSelection ();
		}
		
		[CommandUpdateHandler (EditCommands.Cut)]
		protected void OnUpdateCut (CommandInfo cinfo)
		{
			cinfo.Enabled = designer.CanCutSelection;
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
			cinfo.Enabled = designer.CanPasteToSelection;
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

