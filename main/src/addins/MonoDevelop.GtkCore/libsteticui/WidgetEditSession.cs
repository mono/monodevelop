//
// WidgetEditSession.cs
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
using System.Xml;
using System.Reflection;
using System.Collections;
using System.CodeDom;
using Mono.Unix;

namespace Stetic {
    
	internal class WidgetEditSession: MarshalByRefObject, IDisposable
	{
		string sourceWidget;
		Stetic.ProjectBackend sourceProject;
		
		Stetic.ProjectBackend gproject;
		Stetic.Wrapper.Container rootWidget;
		Stetic.WidgetDesignerBackend widget;
		Gtk.VBox designer;
		Gtk.Plug plug;
		bool autoCommitChanges;
		WidgetActionBar toolbar;
		WidgetDesignerFrontend frontend;
		bool allowBinding;
		bool disposed;
		
		ContainerUndoRedoManager undoManager;
		UndoQueue undoQueue;
		
		public event EventHandler ModifiedChanged;
		public event EventHandler RootWidgetChanged;
		public event Stetic.Wrapper.WidgetEventHandler SelectionChanged;
		
		public WidgetEditSession (ProjectBackend sourceProject, WidgetDesignerFrontend frontend, string windowName, Stetic.ProjectBackend editingBackend, bool autoCommitChanges)
		{
			this.frontend = frontend;
			this.autoCommitChanges = autoCommitChanges;
			undoManager = new ContainerUndoRedoManager ();
			undoQueue = new UndoQueue ();
			undoManager.UndoQueue = undoQueue;
			
			sourceWidget = windowName;
			this.sourceProject = sourceProject;
			
			if (!autoCommitChanges) {
				// Reuse the action groups and icon factory of the main project
				gproject = editingBackend;
				
				// Attach will prevent the destruction of the action group list by gproject
				gproject.AttachActionGroups (sourceProject.ActionGroups);
				
				gproject.IconFactory = sourceProject.IconFactory;
				gproject.FileName = sourceProject.FileName;
				gproject.ImagesRootPath = sourceProject.ImagesRootPath;
				gproject.ResourceProvider = sourceProject.ResourceProvider;
				gproject.WidgetLibraries = (ArrayList) sourceProject.WidgetLibraries.Clone ();
				gproject.InternalWidgetLibraries = (ArrayList) sourceProject.InternalWidgetLibraries.Clone ();
				gproject.TargetGtkVersion = sourceProject.TargetGtkVersion;
				sourceProject.ComponentTypesChanged += OnSourceProjectLibsChanged;
				sourceProject.ProjectReloaded += OnSourceProjectReloaded;
				
				rootWidget = editingBackend.GetTopLevelWrapper (sourceWidget, false);
				if (rootWidget == null) {
					// Copy the widget to edit from the source project
					// When saving the file, this project will be merged with the main project.
					sourceProject.CopyWidgetToProject (windowName, gproject, windowName);
					rootWidget = gproject.GetTopLevelWrapper (windowName, true);
				}
				
				gproject.Modified = false;
			}
			else {
				rootWidget = sourceProject.GetTopLevelWrapper (windowName, true);
				gproject = sourceProject;
			}
			
			rootWidget.Select ();
			undoManager.RootObject = rootWidget;
			
			gproject.ModifiedChanged += new EventHandler (OnModifiedChanged);
			gproject.Changed += new EventHandler (OnChanged);
			gproject.ProjectReloaded += new EventHandler (OnProjectReloaded);
			gproject.ProjectReloading += new EventHandler (OnProjectReloading);
//			gproject.WidgetMemberNameChanged += new Stetic.Wrapper.WidgetNameChangedHandler (OnWidgetNameChanged);
		}
		
		public bool AllowWidgetBinding {
			get { return allowBinding; }
			set {
				allowBinding = value;
				if (toolbar != null)
					toolbar.AllowWidgetBinding = allowBinding;
			}
		}
		
		public Stetic.Wrapper.Widget GladeWidget {
			get { return rootWidget; }
		}
		
		public Stetic.Wrapper.Container RootWidget {
			get { return (Wrapper.Container) Component.GetSafeReference (rootWidget); }
		}
		
		public Gtk.Widget WrapperWidget {
			get {
				if (designer == null) {
					if (rootWidget == null)
						return widget;
					Gtk.Container wc = rootWidget.Wrapped as Gtk.Container;
					if (widget == null)
						widget = Stetic.UserInterface.CreateWidgetDesigner (wc, rootWidget.DesignWidth, rootWidget.DesignHeight);
					
					toolbar = new WidgetActionBar (frontend, rootWidget);
					toolbar.AllowWidgetBinding = allowBinding;
					designer = new Gtk.VBox ();
					designer.BorderWidth = 3;
					designer.PackStart (toolbar, false, false, 0);
					designer.PackStart (widget, true, true, 3);
					widget.DesignArea.SetSelection (gproject.Selection, gproject.Selection, false);
					widget.SelectionChanged += OnSelectionChanged;
				
				}
				return designer; 
			}
		}
		
		[NoGuiDispatch]
		public void CreateWrapperWidgetPlug (uint socketId)
		{
			Gdk.Threads.Enter ();
			plug = new Gtk.Plug (socketId);
			plug.Add (WrapperWidget);
			plug.Decorated = false;
			plug.ShowAll ();
			Gdk.Threads.Leave ();
		}
		
		public void DestroyWrapperWidgetPlug ()
		{
			if (designer != null) {
				Gtk.Plug plug = (Gtk.Plug) WrapperWidget.Parent;
				plug.Remove (WrapperWidget);
				plug.Destroy ();
			}
		}
		
		public void Save ()
		{
			if (!autoCommitChanges) {
				gproject.CopyWidgetToProject (rootWidget.Wrapped.Name, sourceProject, sourceWidget);
				sourceWidget = rootWidget.Wrapped.Name;
				gproject.Modified = false;
			}
		}
		
		public ProjectBackend EditingBackend {
			get { return gproject; }
		}
		
		public void Dispose ()
		{
			sourceProject.ComponentTypesChanged -= OnSourceProjectLibsChanged;
			sourceProject.ProjectReloaded -= OnSourceProjectReloaded;
			
			gproject.ModifiedChanged -= new EventHandler (OnModifiedChanged);
			gproject.Changed -= new EventHandler (OnChanged);
			gproject.ProjectReloaded -= OnProjectReloaded;
			gproject.ProjectReloading -= OnProjectReloading;
//			gproject.WidgetMemberNameChanged -= new Stetic.Wrapper.WidgetNameChangedHandler (OnWidgetNameChanged);
			
			if (!autoCommitChanges) {
				// Don't dispose the project here! it will be disposed by the frontend
				if (widget != null) {
					widget.SelectionChanged -= OnSelectionChanged;
					// Don't dispose the widget. It will be disposed when destroyed together
					// with the container
					widget = null;
				}
			}
			
			if (plug != null)
				plug.Destroy ();
			gproject = null;
			rootWidget = null;
			frontend = null;
			System.Runtime.Remoting.RemotingServices.Disconnect (this);
			disposed = true;
		}
		
		public bool Disposed {
			get { return disposed; }
		}

		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
		
		public void SetDesignerActive ()
		{
			widget.UpdateObjectViewers ();
		}
		
		public bool Modified {
			get { return gproject.Modified; }
		}
		
		public UndoQueue UndoQueue {
			get { 
				if (undoQueue != null)
					return undoQueue;
				else
					return UndoQueue.Empty;
			}
		}
		
		void OnModifiedChanged (object s, EventArgs a)
		{
			if (frontend != null)
				frontend.NotifyModifiedChanged ();
		}
		
		void OnChanged (object s, EventArgs a)
		{
			if (frontend != null)
				frontend.NotifyChanged ();
		}
		
		void OnSourceProjectReloaded (object s, EventArgs a)
		{
			// Propagate gtk version change
			if (sourceProject.TargetGtkVersion != gproject.TargetGtkVersion)
				gproject.TargetGtkVersion = sourceProject.TargetGtkVersion;
		}
		
		void OnSourceProjectLibsChanged (object s, EventArgs a)
		{
			// If component types have changed in the source project, they must also change
			// in this project.
			gproject.WidgetLibraries = (ArrayList) sourceProject.WidgetLibraries.Clone ();
			gproject.InternalWidgetLibraries = (ArrayList) sourceProject.InternalWidgetLibraries.Clone ();
			gproject.NotifyComponentTypesChanged ();
		}
		
		void OnProjectReloading (object s, EventArgs a)
		{
			if (frontend != null)
				frontend.NotifyRootWidgetChanging ();
		}
		
		void OnProjectReloaded (object s, EventArgs a)
		{
			// Update the actions group list
			if (!autoCommitChanges) {
				gproject.AttachActionGroups (sourceProject.ActionGroups);
				gproject.WidgetLibraries = (ArrayList) sourceProject.WidgetLibraries.Clone ();
				gproject.InternalWidgetLibraries = (ArrayList) sourceProject.InternalWidgetLibraries.Clone ();
			}
			
			Gtk.Widget[] tops = gproject.Toplevels;
			if (tops.Length > 0) {
				rootWidget = Stetic.Wrapper.Container.Lookup (tops[0]);
				undoManager.RootObject = rootWidget;
				if (rootWidget != null) {
					Gtk.Widget oldWidget = designer;
					if (widget != null) {
						widget.SelectionChanged -= OnSelectionChanged;
						widget = null;
					}
					OnRootWidgetChanged ();
					if (oldWidget != null) {
						// Delay the destruction of the old widget, so the designer has time to
						// show the new widget. This avoids flickering.
						GLib.Timeout.Add (500, delegate {
							oldWidget.Destroy ();
							return false;
						});
					}
						
					gproject.NotifyComponentTypesChanged ();
					return;
				}
			}
			SetErrorMode ();
		}
		
		void SetErrorMode ()
		{
			Gtk.Label lab = new Gtk.Label ();
			lab.Markup = "<b>" + Catalog.GetString ("The form designer could not be loaded") + "</b>";
			Gtk.EventBox box = new Gtk.EventBox ();
			box.Add (lab);
			
			widget = Stetic.UserInterface.CreateWidgetDesigner (box, 100, 100);
			rootWidget = null;
			
			OnRootWidgetChanged ();
		}
		
		void OnRootWidgetChanged ()
		{
			if (designer != null) {
				if (designer.Parent is Gtk.Plug)
					((Gtk.Plug)designer.Parent).Remove (designer);
				designer = null;
			}
			
			if (plug != null) {
				Gdk.Threads.Enter ();
				plug.Add (WrapperWidget);
				plug.ShowAll ();
				Gdk.Threads.Leave ();
			}
			
			if (frontend != null)
				frontend.NotifyRootWidgetChanged ();
			if (RootWidgetChanged != null)
				RootWidgetChanged (this, EventArgs.Empty);
		}
		
		void OnSelectionChanged (object ob, EventArgs a)
		{
			if (frontend != null) {
				bool canCut, canCopy, canPaste, canDelete;
				ObjectWrapper obj = ObjectWrapper.Lookup (widget.Selection);
				Stetic.Wrapper.Widget wrapper = obj as Stetic.Wrapper.Widget;
				IEditableObject editable = widget.Selection as IEditableObject;
				if (editable == null)
					editable = obj as IEditableObject;
				if (editable != null) {
					canCut = editable.CanCut;
					canCopy = editable.CanCopy;
					canPaste = editable.CanPaste;
					canDelete = editable.CanDelete;
				}
				else {
					canCut = canCopy = canPaste = canDelete = false;
				}
				
				frontend.NotifySelectionChanged (Component.GetSafeReference (obj), canCut, canCopy, canPaste, canDelete);
				if (SelectionChanged != null)
					SelectionChanged (this, new Stetic.Wrapper.WidgetEventArgs (wrapper));
			}
		}
		
		public object SaveState ()
		{
			return null;
		}
		
		public void RestoreState (object sessionData)
		{
		}
		
		internal void ClipboardCopySelection ()
		{
			IEditableObject editable = widget.Selection as IEditableObject;
			if (editable == null)
				editable = ObjectWrapper.Lookup (widget.Selection) as IEditableObject;
			if (editable != null)
				editable.Copy ();
		}
		
		public void ClipboardCutSelection ()
		{
			IEditableObject editable = widget.Selection as IEditableObject;
			if (editable == null)
				editable = ObjectWrapper.Lookup (widget.Selection) as IEditableObject;
			if (editable != null)
				editable.Cut ();
		}
		
		public void ClipboardPaste ()
		{
			IEditableObject editable = widget.Selection as IEditableObject;
			if (editable == null)
				editable = ObjectWrapper.Lookup (widget.Selection) as IEditableObject;
			if (editable != null)
				editable.Paste ();
		}
		
		public void DeleteSelection ()
		{
			IEditableObject editable = widget.Selection as IEditableObject;
			if (editable == null)
				editable = ObjectWrapper.Lookup (widget.Selection) as IEditableObject;
			if (editable != null)
				editable.Delete ();
		}
	}
}
