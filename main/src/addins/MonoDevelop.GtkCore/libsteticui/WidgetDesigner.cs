
using System;
using System.Collections;

namespace Stetic
{
	public delegate void ComponentDropCallback ();
		
	public class WidgetDesigner: Designer
	{
		WidgetEditSession session;
		WidgetDesignerFrontend frontend;
		Component selection;
		Component rootWidget;
		
		Project project;
		Project editedProject;
		int reloadCount;
		
		string componentName;
		bool autoCommitChanges;
		bool disposed;
		
		bool canCut, canCopy, canPaste, canDelete;
		
		public event EventHandler BindField;
		public event EventHandler ModifiedChanged;
		public event EventHandler Changed;
		public event EventHandler SelectionChanged;
		public event EventHandler RootComponentChanged;
		public event ComponentSignalEventHandler SignalAdded;
		public event ComponentSignalEventHandler SignalRemoved;
		public event ComponentSignalEventHandler SignalChanged;
		public event ComponentNameEventHandler ComponentNameChanged;
		public event EventHandler ComponentTypesChanged;
		
		internal WidgetDesigner (Project project, string componentName, bool autoCommitChanges): base (project.App)
		{
			this.componentName = componentName;
			this.autoCommitChanges = autoCommitChanges;
			this.project = project;
			frontend = new WidgetDesignerFrontend (this);
			
			if (autoCommitChanges)
				editedProject = project;
			else
				editedProject = new Project (project.App);
			
			editedProject.SignalAdded += OnSignalAdded;
			editedProject.SignalRemoved += OnSignalRemoved;
			editedProject.SignalChanged += OnSignalChanged;
			editedProject.ComponentNameChanged += OnComponentNameChanged;
			editedProject.ComponentTypesChanged += OnComponentTypesChanged;
			
			project.BackendChanged += OnProjectBackendChanged;
			editedProject.BackendChanged += OnProjectBackendChanged;
			
			CreateSession ();
		}
		
		public Component RootComponent {
			get { return rootWidget; }
		}
		
		public override ProjectItemInfo ProjectItem {
			get { return project.GetWidget (rootWidget.Name); }
		}
		
		public ComponentType[] GetComponentTypes ()
		{
			if (!disposed) {
				ArrayList types = new ArrayList ();
				types.AddRange (editedProject.GetComponentTypes ());
				
				// Add actions from the local action groups
				
				WidgetComponent c = rootWidget as WidgetComponent;
				if (c != null) {
					foreach (ActionGroupComponent grp in c.GetActionGroups ()) {
						foreach (ActionComponent ac in grp.GetActions ())
							types.Add (new ComponentType (app, ac));
					}
				}
				return (ComponentType[]) types.ToArray (typeof(ComponentType)); 
			}
			else
				return new ComponentType[0];
		}
		
		public void BeginComponentDrag (ComponentType type, Gtk.Widget source, Gdk.DragContext ctx)
		{
			Stetic.ObjectWrapper wrapper = type.Action != null ? (Stetic.ObjectWrapper) type.Action.Backend : null;
			app.Backend.BeginComponentDrag (editedProject.ProjectBackend, type.Description, type.ClassName, wrapper, source, ctx, null);
		}
		
		public void BeginComponentDrag (string title, string className, Gtk.Widget source, Gdk.DragContext ctx, ComponentDropCallback callback)
		{
			app.Backend.BeginComponentDrag (editedProject.ProjectBackend, title, className, null, source, ctx, callback);
		}
		
		// Creates an action group designer for the widget being edited by this widget designer
		public ActionGroupDesigner CreateActionGroupDesigner ()
		{
			if (disposed)
				throw new ObjectDisposedException ("WidgetDesigner");
			return new ActionGroupDesigner (editedProject, componentName, null, this, true);
		}
		
		public bool Modified {
			get { return session != null && session.Modified; }
		}
		
		internal override void SetActive ()
		{
			if (!disposed)
				project.App.SetActiveDesignSession (editedProject, session);
		}
		
		public bool AllowWidgetBinding {
			get { return session != null && session.AllowWidgetBinding; }
			set {
				if (session != null)
					session.AllowWidgetBinding = value;
			}
		}
		
		public ImportFileDelegate ImportFileCallback {
			get { return editedProject.ImportFileCallback; }
			set { editedProject.ImportFileCallback = value; }
		}
		
		void CreateSession ()
		{
			try {
				session = project.ProjectBackend.CreateWidgetDesignerSession (frontend, componentName, editedProject.ProjectBackend, autoCommitChanges);
				ResetCustomWidget ();
				rootWidget = app.GetComponent (session.RootWidget, null, null);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				Gtk.Label lab = new Gtk.Label ();
				lab.Text = Mono.Unix.Catalog.GetString ("The designer could not be loaded.") + "\n\n" + ex.Message;
				lab.Wrap = true;
				lab.WidthRequest = 400;
				AddCustomWidget (lab);
				session = null;
			}
		}
		
		protected override void OnCreatePlug (uint socketId)
		{
			session.CreateWrapperWidgetPlug (socketId);
		}
		
		protected override void OnDestroyPlug (uint socketId)
		{
			session.DestroyWrapperWidgetPlug ();
		}
		
		protected override Gtk.Widget OnCreateWidget ()
		{
			return session.WrapperWidget;
		}
		
		public Component Selection {
			get {
				return selection;
			}
		}
		
		public void CopySelection ()
		{
			if (session != null)
				session.ClipboardCopySelection ();
		}
		
		public void CutSelection ()
		{
			if (session != null)
				session.ClipboardCutSelection ();
		}
		
		public void PasteToSelection ()
		{
			if (session != null)
				session.ClipboardPaste ();
		}
		
		public void DeleteSelection ()
		{
			if (session != null)
				session.DeleteSelection ();
		}
		
		public bool CanDeleteSelection {
			get { return canDelete; }
		}
		
		public bool CanCutSelection {
			get { return canCut; }
		}
		
		public bool CanCopySelection {
			get { return canCopy; }
		}
		
		public bool CanPasteToSelection {
			get { return canPaste; }
		}
		
		public UndoQueue UndoQueue {
			get {
				if (session != null)
					return session.UndoQueue;
				else
					return UndoQueue.Empty;
			}
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			
			if (disposed)
				return;
			
			if (project.App.ActiveProject == editedProject)
				project.App.ActiveProject = null;
			
			disposed = true;
			frontend.disposed = true;
			editedProject.SignalAdded -= OnSignalAdded;
			editedProject.SignalRemoved -= OnSignalRemoved;
			editedProject.SignalChanged -= OnSignalChanged;
			editedProject.ComponentNameChanged -= OnComponentNameChanged;
			editedProject.BackendChanged -= OnProjectBackendChanged;
			editedProject.ComponentTypesChanged -= OnComponentTypesChanged;
			project.BackendChanged -= OnProjectBackendChanged;
			
			if (session != null) {
				session.Dispose ();
				session = null;
			}
				
			if (!autoCommitChanges)
				editedProject.Dispose ();
				
			System.Runtime.Remoting.RemotingServices.Disconnect (frontend);
			frontend = null;
			rootWidget = null;
			selection = null;
			base.Dispose ();
		}
		
		public void Save ()
		{
			if (session != null)
				session.Save ();
		}
		
		void OnComponentNameChanged (object s, ComponentNameEventArgs args)
		{
			if (!disposed && ComponentNameChanged != null)
				ComponentNameChanged (this, args);
		}
		
		internal void NotifyRootWidgetChanging ()
		{
			PrepareUpdateWidget ();
		}
		
		internal void NotifyRootWidgetChanged ()
		{
			object rw = session.RootWidget;
			if (rw != null)
				rootWidget = app.GetComponent (session.RootWidget, null, null);
			else
				rootWidget = null;

			UpdateWidget ();
			if (RootComponentChanged != null)
				RootComponentChanged (this, EventArgs.Empty);
		}
		
		internal override void OnBackendChanging ()
		{
			reloadCount = 0;
			base.OnBackendChanging ();
		}
		
		internal override void OnBackendChanged (ApplicationBackend oldBackend)
		{
			// Can't do anything until the projects are reloaded
			// This is checked in OnProjectBackendChanged.
		}
		
		void OnProjectBackendChanged (ApplicationBackend oldBackend)
		{
			if (++reloadCount == 2) {
				object sessionData = null;
				
				if (oldBackend != null && !autoCommitChanges) {
					sessionData = session.SaveState ();
					session.DestroyWrapperWidgetPlug ();
				}

				// Don't dispose the session here, since it will dispose
				// the underlying project, and we can't do it because
				// it may need to process the OnBackendChanging event
				// as well.
			
				CreateSession ();
				
				if (sessionData != null && session != null)
					session.RestoreState (sessionData);

				base.OnBackendChanged (oldBackend);
				NotifyRootWidgetChanged ();
			}
		}
		
		void OnComponentTypesChanged (object s, EventArgs a)
		{
			if (ComponentTypesChanged != null)
				ComponentTypesChanged (this, a);
		}
		
		void OnSignalAdded (object sender, Stetic.ComponentSignalEventArgs args)
		{
			if (SignalAdded != null)
				SignalAdded (this, args);
		}

		void OnSignalRemoved (object sender, Stetic.ComponentSignalEventArgs args)
		{
			if (SignalRemoved != null)
				SignalRemoved (this, args);
		}

		void OnSignalChanged (object sender, Stetic.ComponentSignalEventArgs args)
		{
			if (SignalChanged != null)
				SignalChanged (this, args);
		}
		
		internal void NotifyBindField ()
		{
			if (BindField != null)
				BindField (this, EventArgs.Empty);
		}
		
		internal void NotifyModifiedChanged ()
		{
			if (ModifiedChanged != null)
				ModifiedChanged (this, EventArgs.Empty);
		}
		
		internal void NotifyChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		internal void NotifySelectionChanged (object ob, bool canCut, bool canCopy, bool canPaste, bool canDelete)
		{
			this.canCut = canCut;
			this.canCopy = canCopy;
			this.canPaste = canPaste;
			this.canDelete = canDelete;
			
			if (ob != null)
				selection = app.GetComponent (ob, null, null);
			else
				selection = null;

			if (SelectionChanged != null)
				SelectionChanged (this, EventArgs.Empty);
		}
	}
	
	internal class WidgetDesignerFrontend: MarshalByRefObject
	{
		WidgetDesigner designer;
		internal bool disposed;
		
		public WidgetDesignerFrontend (WidgetDesigner designer)
		{
			this.designer = designer;
		}
		
		public void NotifyBindField ()
		{
			GuiDispatch.InvokeSync (
				delegate { if (!disposed) designer.NotifyBindField (); }
			);
		}
		
		public void NotifyModifiedChanged ()
		{
			GuiDispatch.InvokeSync (
				delegate { if (!disposed) designer.NotifyModifiedChanged (); }
			);
		}
		
		public void NotifyChanged ()
		{
			GuiDispatch.InvokeSync (
				delegate { if (!disposed) designer.NotifyChanged (); }
			);
		}
		
		internal void NotifySelectionChanged (object ob, bool canCut, bool canCopy, bool canPaste, bool canDelete)
		{
			GuiDispatch.InvokeSync (
				delegate { if (!disposed) designer.NotifySelectionChanged (ob, canCut, canCopy, canPaste, canDelete); }
			);
		}

		public void NotifyRootWidgetChanged ()
		{
			GuiDispatch.InvokeSync (
				delegate { if (!disposed) designer.NotifyRootWidgetChanged (); }
			);
		}

		public void NotifyRootWidgetChanging ()
		{
			GuiDispatch.InvokeSync (
				delegate { if (!disposed) designer.NotifyRootWidgetChanging (); }
			);
		}
		
		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
	}
}
