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
		Stetic.ProjectBackend project;
		
		Stetic.Wrapper.Container rootWidget;
		Stetic.WidgetDesignerBackend widget;
		Gtk.VBox designer;
		Gtk.Plug plug;
		WidgetActionBar toolbar;
		WidgetDesignerFrontend frontend;
		bool allowBinding;
		bool disposed;
		
		ContainerUndoRedoManager undoManager;
		UndoQueue undoQueue;
		
		public event EventHandler RootWidgetChanged;
		public event Stetic.Wrapper.WidgetEventHandler SelectionChanged;
		
		public WidgetEditSession (ProjectBackend sourceProject, WidgetDesignerFrontend frontend, string windowName)
		{
			this.frontend = frontend;
			undoManager = new ContainerUndoRedoManager ();
			undoQueue = new UndoQueue ();
			undoManager.UndoQueue = undoQueue;
			
			sourceWidget = windowName;
			this.project = sourceProject;
						
			rootWidget = sourceProject.GetTopLevelWrapper (windowName, true);
			rootWidget.Select ();
			undoManager.RootObject = rootWidget;
			
			this.project.Changed += new ProjectChangedEventHandler (OnChanged);
			this.project.ProjectReloaded += new EventHandler (OnProjectReloaded);
			this.project.ProjectReloading += new EventHandler (OnProjectReloading);
//			this.project.WidgetMemberNameChanged += new Stetic.Wrapper.WidgetNameChangedHandler (OnWidgetNameChanged);
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
					widget.DesignArea.SetSelection (project.Selection, project.Selection, false);
					
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
		}
		
		public ProjectBackend EditingBackend {
			get { return project; }
		}
		
		public void Dispose ()
		{
			project.ComponentTypesChanged -= OnSourceProjectLibsChanged;
			project.ProjectReloaded -= OnSourceProjectReloaded;
			project.Changed -= new ProjectChangedEventHandler (OnChanged);
			project.ProjectReloaded -= OnProjectReloaded;
			project.ProjectReloading -= OnProjectReloading;
//			project.WidgetMemberNameChanged -= new Stetic.Wrapper.WidgetNameChangedHandler (OnWidgetNameChanged);
			
			if (plug != null)
				plug.Destroy ();
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
			get { 
				if (project == null || RootWidget == null)
					return false;
				
				return project.WasModified (RootWidget.Name); 
			}
		}
		
		public UndoQueue UndoQueue {
			get { 
				if (undoQueue != null)
					return undoQueue;
				else
					return UndoQueue.Empty;
			}
		}
		
		void OnChanged (object s, ProjectChangedEventArgs a)
		{
			if (a.ChangedTopLevelName != RootWidget.Name)
				return;
			
			if (frontend != null)
				frontend.NotifyChanged ();
		}
		
		void OnSourceProjectReloaded (object s, EventArgs a)
		{
			
		}
		
		void OnSourceProjectLibsChanged (object s, EventArgs a)
		{
		}
		
		void OnProjectReloading (object s, EventArgs a)
		{
			if (frontend != null)
				frontend.NotifyRootWidgetChanging ();
		}
		
		void OnProjectReloaded (object s, EventArgs a)
		{
			Gtk.Widget topWidget = project.GetWidget (sourceWidget);
			
			if (topWidget != null) {
				rootWidget = Stetic.Wrapper.Container.Lookup (topWidget);
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
									
					project.NotifyComponentTypesChanged ();
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
			return new object[] {
				project.SaveStatus (),
				undoQueue
			};
		}
		
		public void RestoreState (object sessionData)
		{
			object[] status = (object[]) sessionData;
			project.LoadStatus (status [0]);
			undoQueue = (UndoQueue) status [1];
			foreach (UndoRedoChange ch in undoQueue.Changes) {
				ObjectWrapperUndoRedoChange och = ch as ObjectWrapperUndoRedoChange;
				if (och != null)
					och.Manager = undoManager;
			}
			undoManager.UndoQueue = undoQueue;
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
