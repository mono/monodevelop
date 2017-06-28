
using System;

namespace Stetic
{
	public class ActionGroupDesigner: Designer
	{
		ActionGroupEditSession editSession;
		ActionGroupDesignerFrontend frontend;
		Project project;
		string componentName;
		string actionGroupName;
		bool autoCommitChanges;
		WidgetDesigner relatedWidgetDesigner;
		
		public event EventHandler BindField;
		public event EventHandler ModifiedChanged;
		public event EventHandler RootComponentChanged;
		public event ComponentSignalEventHandler SignalAdded;
		public event ComponentSignalEventHandler SignalChanged;
		
		internal ActionGroupDesigner (Project project, string componentName, string actionGroupName, WidgetDesigner relatedWidgetDesigner, bool autoCommitChanges): base (project.App)
		{
			this.componentName = componentName;
			this.relatedWidgetDesigner = relatedWidgetDesigner;
			
			if (actionGroupName != null)
				this.actionGroupName = actionGroupName;
			this.autoCommitChanges = autoCommitChanges;
			this.project = project;

			frontend = new ActionGroupDesignerFrontend (this);

			CreateSession ();
		}
		
		internal override void SetActive ()
		{
			app.ActiveProject = project;
		}
		
		public override ProjectItemInfo ProjectItem {
			get { return project.GetWidget (RootComponent.Name); }
		}
		
		public Component RootComponent {
			get { return app.GetComponent (editSession.EditedActionGroup, null, null); }
		}
		
		public bool AllowActionBinding {
			get {
				return editSession != null && editSession.AllowActionBinding; 
			}
			set {
				if (editSession != null)
					editSession.AllowActionBinding = value; 
			}
		}
		
		public UndoQueue UndoQueue {
			get {
				if (relatedWidgetDesigner != null)
					return relatedWidgetDesigner.UndoQueue;
				else if (editSession != null)
					return editSession.UndoQueue;
				else
					return UndoQueue.Empty;
			}
		}
		
		public ActionComponent SelectedAction {
			get {
				if (editSession == null)
					return null;
				Wrapper.Action act;
				string an;
				editSession.GetSelectedAction (out act, out an);
				if (act != null)
					return (ActionComponent) app.GetComponent (act, an, null);
				else
					return null;
			}
			set {
				editSession.SetSelectedAction ((Wrapper.Action)value.Backend);
			}
		}
		
		public bool HasData {
			get { return editSession != null && editSession.HasData; }
		}
		
		public bool Modified {
			get { return editSession != null && editSession.Modified; }
			set {
				if (editSession != null)
					editSession.Modified = value;
			}
		}
		
		public string ActiveGroup {
			get {
				if (editSession == null)
					return null;
				return editSession.ActiveGroup; 
			}
			set {
				if (editSession != null)
					editSession.ActiveGroup = value;
			}
		}
		
		public void Save ()
		{
			if (editSession != null) {
				editSession.Save ();
				if (!autoCommitChanges && componentName != null)
					componentName = editSession.ActiveGroup;
			}
		}
		
		public void CopySelection ()
		{
			if (editSession != null)
				editSession.CopySelection ();
		}
		
		public void CutSelection ()
		{
			if (editSession != null)
				editSession.CutSelection ();
		}
		
		public void PasteToSelection ()
		{
			if (editSession != null)
				editSession.PasteToSelection ();
		}
		
		public void DeleteSelection ()
		{
			if (editSession != null)
				editSession.DeleteSelection ();
		}
		
		protected override void OnCreatePlug (uint socketId)
		{
			editSession.CreateBackendWidgetPlug (socketId);
		}
		
		protected override void OnDestroyPlug (uint socketId)
		{
			editSession.DestroyBackendWidgetPlug ();
		}
		
		protected override Gtk.Widget OnCreateWidget ()
		{
			return editSession.Backend;
		}
		
		void CreateSession ()
		{
			try {
				if (actionGroupName != null)
					editSession = project.ProjectBackend.CreateGlobalActionGroupDesignerSession (frontend, actionGroupName, autoCommitChanges);
				else
					editSession = project.ProjectBackend.CreateLocalActionGroupDesignerSession (frontend, componentName, autoCommitChanges);
				ResetCustomWidget ();
			} catch (Exception ex) {
				editSession = null;
				Console.WriteLine (ex);
				AddCustomWidget (new Gtk.Label (ex.Message));
			}
		}
		
		protected override void OnDestroyed ()
		{
			frontend.disposed = true;
			System.Runtime.Remoting.RemotingServices.Disconnect (frontend);
			if (editSession != null)
				editSession.Dispose ();
			editSession = null;
			base.OnDestroyed ();
		}
		
		internal override void OnBackendChanged (ApplicationBackend oldBackend)
		{
			object[] sessionData = null;
			
			if (oldBackend != null) {
				if (!autoCommitChanges)
					sessionData = editSession.SaveState ();
				if (editSession != null)
					editSession.Dispose ();
			}
		
			CreateSession ();

			if (sessionData != null && editSession != null)
				editSession.RestoreState (sessionData);
			
			base.OnBackendChanged (oldBackend);
			
			if (RootComponentChanged != null)
				RootComponentChanged (this, EventArgs.Empty);
		}
		
		internal void NotifyBindField ()
		{
			if (BindField != null)
				BindField (this, EventArgs.Empty);
		}
		
		internal void NotifyModified ()
		{
			if (ModifiedChanged != null)
				ModifiedChanged (this, EventArgs.Empty);
		}
		
		internal void NotifySignalAdded (Wrapper.Action action, string name, Signal signal)
		{
			ActionComponent c = (ActionComponent) app.GetComponent (action, name, null);
			if (c != null && SignalAdded != null)
				SignalAdded (this, new ComponentSignalEventArgs (project, c, null, signal));
		}
		
		internal void NotifySignalChanged (Wrapper.Action action, string name, Signal oldSignal, Signal signal)
		{
			ActionComponent c = (ActionComponent) app.GetComponent (action, name, null);
			if (c != null && SignalChanged != null)
				SignalChanged (this, new ComponentSignalEventArgs (project, c, oldSignal, signal));
		}
	}
	
	internal class ActionGroupDesignerFrontend: MarshalByRefObject
	{
		ActionGroupDesigner designer;
		internal bool disposed;
		
		public ActionGroupDesignerFrontend (ActionGroupDesigner designer)
		{
			this.designer = designer;
		}
		
		public void NotifyBindField ()
		{
			Gtk.Application.Invoke (
				(o, args) => { if (!disposed) designer.NotifyBindField (); }
			);
		}
		
		public void NotifyModified ()
		{
			Gtk.Application.Invoke (
				(o, args) => { if (!disposed) designer.NotifyModified (); }
			);
		}

		public void NotifySignalAdded (Wrapper.Action action, string name, Signal signal)
		{
			Gtk.Application.Invoke (
				(o, args) => { if (!disposed) designer.NotifySignalAdded (action, name, signal); }
			);
		}
		
		public void NotifySignalChanged (Wrapper.Action action, string name, Signal oldSignal, Signal signal)
		{
			Gtk.Application.Invoke (
				(o, args) => { if (!disposed) designer.NotifySignalChanged (action, name, oldSignal, signal); }
			);
		}
		
		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
	}
}
