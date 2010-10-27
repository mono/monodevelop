//
// GuiBuilderView.cs
//
// Author:
//   Lluis Sanchez Gual
//   Krzysztof Marecki
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
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.DesignerSupport.Toolbox; 
using MonoDevelop.DesignerSupport;

using Gtk;
using Gdk;
using MonoDevelop.Ide;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderView : CombinedDesignView, IToolboxConsumer, MonoDevelop.DesignerSupport.IOutlinedDocument, ISupportsProjectReload
	{
		Stetic.WidgetDesigner designer;
		Stetic.ActionGroupDesigner actionsBox;
		GuiBuilderWindow window;
		
		DesignerPage designerPage;
		ActionGroupPage actionsPage;
		
		
		CodeBinder codeBinder;
		GuiBuilderProject gproject;
		string rootName;
		object designerStatus;
		
		public GuiBuilderView (IViewContent content, GuiBuilderWindow window): base (content)
		{
			rootName = window.Name;
			
			designerPage = new DesignerPage ();
			designerPage.Show ();
			AddButton (GettextCatalog.GetString ("Designer"), designerPage);
			
			actionsPage = new ActionGroupPage ();
			actionsPage.Show ();
			
			AttachWindow (window);
		}
		
		void AttachWindow (GuiBuilderWindow window)
		{
			gproject = window.Project;
			GtkDesignInfo info = GtkDesignInfo.FromProject (gproject.Project);
			gproject.SteticProject.ImagesRootPath = FileService.AbsoluteToRelativePath (info.SteticFolder, gproject.Project.BaseDirectory);
			gproject.UpdateLibraries ();
			LoadDesigner ();
		}
		
		ProjectReloadCapability ISupportsProjectReload.ProjectReloadCapability {
			get {
				return ProjectReloadCapability.Full;
			}
		}
		
		void ISupportsProjectReload.Update (Project project)
		{
			if (gproject != null && gproject.Project == project)
				return;
			
			ReloadDesigner (project);
		}
		
		void LoadDesigner ()
		{
			this.window = gproject.GetWindow (rootName);
			if (window == null) {
				// The window doesn't exist anymore
				return;
			}
			
			gproject.Unloaded += OnDisposeProject;
			
			designer = gproject.SteticProject.CreateWidgetDesigner (window.RootWidget);
			
			// Designer page
			designerPage.ClearChild ();
			designerPage.Add (designer);
			
			if (designer.RootComponent == null) {
				// Something went wrong while creating the designer. Show it, but don't do aything else.
				designer.ShowAll ();
				return;
			}

			designer.AllowWidgetBinding = !gproject.Project.UsePartialTypes;
			
			codeBinder = new CodeBinder (gproject.Project, new OpenDocumentFileProvider (), designer.RootComponent);
			
			designer.BindField += OnBindWidgetField;
			designer.Changed += OnChanged;
			designer.SignalAdded += OnSignalAdded;
			designer.SignalRemoved += OnSignalRemoved;
			designer.SignalChanged += OnSignalChanged;
			designer.ComponentNameChanged += OnComponentNameChanged;
			designer.RootComponentChanged += OnRootComponentChanged;
			designer.ComponentTypesChanged += OnComponentTypesChanged;
			designer.ImportFileCallback = ImportFile;
			
			// Actions designer
			actionsBox = designer.CreateActionGroupDesigner ();
			actionsBox.AllowActionBinding = !gproject.Project.UsePartialTypes;
			actionsBox.BindField += new EventHandler (OnBindActionField);
			actionsBox.ModifiedChanged += new EventHandler (OnActionshanged);
			
			actionsPage.ClearChild ();
			actionsPage.PackStart (actionsBox, true, true, 0);
			actionsPage.ShowAll ();
			
			if (actionsBox.HasData) {
				if (!HasPage (actionsPage))
					AddButton (GettextCatalog.GetString ("Actions"), actionsPage);
			} else {
				RemoveButton (actionsPage);
			}
			
			designer.ShowAll ();
			GuiBuilderService.SteticApp.ActiveDesigner = designer;
		}
		
		public void ReloadDesigner (Project project) 
		{
			if (designer != null)
				designerStatus = designer.SaveStatus ();
			
			CloseDesigner ();
			CloseProject ();
			if (project != null) {
				GuiBuilderWindow w = GuiBuilderDisplayBinding.GetWindow (this.ContentName);
				if (w != null) {
					AttachWindow (w);
					if (designerStatus != null)
						designer.LoadStatus (designerStatus);
					designerStatus = null;
				}
			}
		}
		
		public override Stetic.Designer Designer {
			get { return designer; }
		}
		
		void OnDisposeProject (object s, EventArgs args)
		{
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
			designer.Changed -= OnChanged;
			designer.SignalAdded -= OnSignalAdded;
			designer.SignalRemoved -= OnSignalRemoved;
			designer.SignalChanged -= OnSignalChanged;
			designer.ComponentNameChanged -= OnComponentNameChanged;
			designer.RootComponentChanged -= OnRootComponentChanged;
			designer.ComponentTypesChanged -= OnComponentTypesChanged;
			
			if (actionsBox != null) {
				actionsBox.BindField -= OnBindActionField;
				actionsBox.ModifiedChanged -= OnActionshanged;
				actionsBox = null;
			}

			actionsPage.ClearChild ();
			designerPage.ClearChild ();
			
			designerPage.Add (CreateDesignerNotAvailableWidget ());
			actionsPage.Add (CreateDesignerNotAvailableWidget ());
			
			designer = null;
			
			gproject.Reloaded += OnReloadProject;
		}
		
		void CloseProject ()
		{
			gproject.Reloaded -= OnReloadProject;
		}
		
		public override void Dispose ()
		{
			CloseDesigner ();
			CloseProject ();
			codeBinder = null;
			base.Dispose ();
		}
		
		Gtk.Widget CreateDesignerNotAvailableWidget ()
		{
			Gtk.Label label = new Gtk.Label (GettextCatalog.GetString ("Designer not available"));
			label.Show ();
			return label;
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
			if (designer != null && !HasPage (actionsPage) && !ErrorMode)
				AddButton (GettextCatalog.GetString ("Actions"), actionsPage);
		}
		
		void OnChanged (object s, EventArgs args)
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
			
			if (oldBuildFile != newBuildFile && oldBuildFile != null && newBuildFile != null) {
				if (System.IO.File.Exists (newBuildFile))
					FileService.DeleteFile (newBuildFile);
				if (System.IO.File.Exists (oldBuildFile))
					FileService.MoveFile (oldBuildFile, newBuildFile);
			}
			
			gproject.Save (true);
			OnDirtyChanged (EventArgs.Empty);
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
				if (asm == "gtk-sharp")
					// If we are adding a widget to a window, the project must already have a gtk# reference
					return;
				
				asm = gproject.Project.AssemblyContext.GetAssemblyFullName (asm, gproject.Project.TargetFramework);
				if (asm == null)
					return;
				if (gproject.Project.AssemblyContext.GetPackagesFromFullName (asm).Length > 0) {
					pref = new ProjectReference (ReferenceType.Gac, asm);
				} else {
					asm = gproject.Project.AssemblyContext.GetAssemblyLocation (asm, gproject.Project.TargetFramework);
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

		TargetEntry[] IToolboxConsumer.DragTargets {
			get { return Stetic.DND.Targets; }
		}
	}
	
	class DesignerPage: Gtk.EventBox, ICustomPropertyPadProvider
	{
		public DesignerPage ()
		{
		}
		
		Gtk.Widget ICustomPropertyPadProvider.GetCustomPropertyWidget ()
		{
			return PropertiesWidget.Instance;
		}
		
		void ICustomPropertyPadProvider.DisposeCustomPropertyWidget ()
		{
		}
		
		Stetic.WidgetDesigner Designer {
			get {
				return Child as Stetic.WidgetDesigner;
			}
		}
		
		public void ClearChild ()
		{
			if (Child != null) {
				Gtk.Widget w = Child;
				Remove (w);
				w.Destroy ();
			}
		}
		
		[CommandHandler (EditCommands.Delete)]
		protected void OnDelete ()
		{
			Designer.DeleteSelection ();
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		protected void OnUpdateDelete (CommandInfo cinfo)
		{
			cinfo.Bypass = Designer != null && !Designer.CanDeleteSelection;
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			Designer.CopySelection ();
		}
		
		[CommandUpdateHandler (EditCommands.Copy)]
		protected void OnUpdateCopy (CommandInfo cinfo)
		{
			cinfo.Enabled = Designer != null && Designer.CanCopySelection;
		}
		
		[CommandHandler (EditCommands.Cut)]
		protected void OnCut ()
		{
			Designer.CutSelection ();
		}
		
		[CommandUpdateHandler (EditCommands.Cut)]
		protected void OnUpdateCut (CommandInfo cinfo)
		{
			cinfo.Enabled = Designer != null && Designer.CanCutSelection;
		}
		
		[CommandHandler (EditCommands.Paste)]
		protected void OnPaste ()
		{
			Designer.PasteToSelection ();
		}
		
		[CommandHandler (EditCommands.Undo)]
		protected void OnUndo ()
		{
			Designer.UndoQueue.Undo ();
		}
		
		[CommandHandler (EditCommands.Redo)]
		protected void OnRedo ()
		{
			Designer.UndoQueue.Redo ();
		}
		
		[CommandUpdateHandler (EditCommands.Paste)]
		protected void OnUpdatePaste (CommandInfo cinfo)
		{
			cinfo.Enabled = Designer != null && Designer.CanPasteToSelection;
		}
		
		[CommandUpdateHandler (EditCommands.Undo)]
		protected void OnUpdateUndo (CommandInfo cinfo)
		{
			cinfo.Enabled = Designer != null && Designer.UndoQueue.CanUndo;
		}
		
		[CommandUpdateHandler (EditCommands.Redo)]
		protected void OnUpdateRedo (CommandInfo cinfo)
		{
			cinfo.Enabled = Designer != null && Designer.UndoQueue.CanRedo;
		}
	}
}

