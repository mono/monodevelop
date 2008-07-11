
using System;
using System.Xml;
using System.Collections;

namespace Stetic
{
	internal class ActionGroupEditSession: MarshalByRefObject, IDisposable
	{
		ActionGroupDesignerBackend designer;
		Gtk.Plug plug;
		ActionGroupDesignerFrontend frontend;
		bool autoCommitChanges;
		string groupToEdit;
		string containerName;
		ProjectBackend project;
		bool modified;
		bool allowActionBinding;
		bool designerRequested;
		ActionGroupToolbar groupToolbar;
			
		Stetic.Wrapper.ActionGroup groupCopy;
		Stetic.Wrapper.ActionGroup group;
		Hashtable actionCopyMap = new Hashtable ();
		
		UndoRedoManager undoManager;
		UndoQueue undoQueue;
		
		public ActionGroupEditSession (ActionGroupDesignerFrontend frontend, ProjectBackend project, string containerName, string groupToEdit, bool autoCommitChanges)
		{
			this.groupToEdit = groupToEdit;
			this.containerName = containerName;
			this.frontend = frontend;
			this.project = project;
			this.autoCommitChanges = autoCommitChanges;
			
			if (groupToEdit != null) {
				group = project.ActionGroups [groupToEdit];
				if (group == null)
					throw new InvalidOperationException ("Unknown action group: " + groupToEdit);
				Load (group);
				undoManager = new UndoRedoManager ();
				undoQueue = new UndoQueue ();
				undoManager.UndoQueue = undoQueue;
				undoManager.RootObject = groupCopy;
				
				groupToolbar = new ActionGroupToolbar (frontend, groupCopy);
			}
			else {
				if (!autoCommitChanges)
					throw new System.NotSupportedException ();
				
				Stetic.Wrapper.Container container = project.GetTopLevelWrapper (containerName, true);
				groupToolbar = new ActionGroupToolbar (frontend, container.LocalActionGroups);
			}
			
			// Don't delay the creation of the designer because when used in combination with the
			// widget designer, change events are subscribed since the begining
			
			designer = UserInterface.CreateActionGroupDesigner (project, groupToolbar);
			designer.Editor.GroupModified += OnModified;
			designer.Toolbar.AllowActionBinding = allowActionBinding;
			designer.Destroyed += delegate { designer = null; Dispose (); };
		}
		
		public Wrapper.ActionGroup EditedActionGroup {
			get { return groupCopy; }
		}
		
		void Load (Stetic.Wrapper.ActionGroup group)
		{
			if (autoCommitChanges) {
				groupCopy = group;
			}
			else {
				actionCopyMap.Clear ();
					
				groupCopy = new Stetic.Wrapper.ActionGroup ();
				groupCopy.Name = group.Name;
				
				foreach (Stetic.Wrapper.Action action in group.Actions) {
					Stetic.Wrapper.Action dupaction = action.Clone ();
					groupCopy.Actions.Add (dupaction);
					actionCopyMap [dupaction] = action;
				}
				groupCopy.SignalAdded += new Stetic.SignalEventHandler (OnSignalAdded);
				groupCopy.SignalChanged += new Stetic.SignalChangedEventHandler (OnSignalChanged);
			}
		}
		
		public void Save ()
		{
			if (autoCommitChanges)
				return;

			if (group.Name != groupCopy.Name)
				group.Name = groupCopy.Name;
			
			foreach (Stetic.Wrapper.Action actionCopy in groupCopy.Actions) {
				Stetic.Wrapper.Action action = (Stetic.Wrapper.Action) actionCopyMap [actionCopy];
				if (action != null)
					action.CopyFrom (actionCopy);
				else {
					action = actionCopy.Clone ();
					actionCopyMap [actionCopy] = action;
					group.Actions.Add (action);
				}
			}
			
			ArrayList todelete = new ArrayList ();
			foreach (Stetic.Wrapper.Action actionCopy in actionCopyMap.Keys) {
				if (!groupCopy.Actions.Contains (actionCopy))
					todelete.Add (actionCopy);
			}
			
			foreach (Stetic.Wrapper.Action actionCopy in todelete) {
				Stetic.Wrapper.Action action = (Stetic.Wrapper.Action) actionCopyMap [actionCopy];
				group.Actions.Remove (action);
				actionCopyMap.Remove (actionCopy);
			}
			Modified = false;
		}
		
		public UndoQueue UndoQueue {
			get { 
				if (undoQueue != null)
					return undoQueue;
				else
					return UndoQueue.Empty;
			}
		}
		
		public void CopySelection ()
		{
			if (designer != null)
				designer.Editor.Copy ();
		}
		
		public void CutSelection ()
		{
			if (designer != null)
				designer.Editor.Cut ();
		}
		
		public void PasteToSelection ()
		{
			if (designer != null)
				designer.Editor.Paste ();
		}
		
		public void DeleteSelection ()
		{
			if (designer != null)
				designer.Editor.Delete ();
		}
		
		void OnProjectReloaded (object s, EventArgs a)
		{
			// Called when the underlying project has changed. Object references need to be updated.
			if (autoCommitChanges) {
				if (designer != null) {
					if (groupToEdit != null) {
						groupToolbar.ActiveGroup = project.ActionGroups [groupToEdit];
					} else {
						Stetic.Wrapper.Container container = project.GetTopLevelWrapper (containerName, true);
						groupToolbar.ActionGroups = container.LocalActionGroups;
					}
				}
			} else {
				// We only need to remap the actions
				group = project.ActionGroups [groupToEdit];
				actionCopyMap.Clear ();
				foreach (Wrapper.Action dupac in groupCopy.Actions) {
					Wrapper.Action ac = group.GetAction (dupac.Name);
					if (ac != null)
						actionCopyMap [dupac] = ac;
				}
			}
		}
		
		void OnModified (object ob, EventArgs args)
		{
			modified = true;
			frontend.NotifyModified ();
		}
		
		public bool HasData {
			get {
				if (groupToEdit != null)
					return true;
				Stetic.Wrapper.Container container = project.GetTopLevelWrapper (containerName, true);
				return container.LocalActionGroups.Count > 0;
			}
		}
		
		public bool Modified {
			get { return modified; }
			set { modified = value; frontend.NotifyModified (); }
		}
		
		public string ActiveGroup {
			get {
				if (designer != null) {
					Wrapper.ActionGroup grp = designer.Toolbar.ActiveGroup;
					if (grp != null)
						return grp.Name;
				}
				return null;
			}
			set {
				if (value != null || designer != null) {
					// No need to create the designer if the active group is being set to null.
					Wrapper.ActionGroup grp = Backend.Toolbar.ActionGroups [value];
					Backend.Toolbar.ActiveGroup = grp;
				}
			}
		}
		
		public void SetSelectedAction (Wrapper.Action action)
		{
			// No need to create the designer if the active action is being set to null.
			if (action != null || designer != null)
				Backend.Editor.SelectedAction = action;
		}
		
		public void GetSelectedAction (out Wrapper.Action action, out string name)
		{
			if (designer != null) {
				action = designer.Editor.SelectedAction;
				if (action != null)
					name = action.Name;
				else
					name = null;
			} else {
				action = null;
				name = null;
			}
		}
		
		public ActionGroupDesignerBackend Backend {
			get {
				designerRequested = true;
				return designer;
			}
		}
		
		[NoGuiDispatch]
		public void CreateBackendWidgetPlug (uint socketId)
		{
			Gdk.Threads.Enter ();
			plug = new Gtk.Plug (socketId);
			plug.Add (Backend);
			plug.Decorated = false;
			plug.ShowAll ();
			Gdk.Threads.Leave ();
		}
		
		public void DestroyBackendWidgetPlug ()
		{
			if (designer != null) {
				Gtk.Plug plug = (Gtk.Plug) designer.Parent;
				plug.Remove (designer);
				plug.Destroy ();
			}
		}
		
		public bool AllowActionBinding {
			get {
				return allowActionBinding;
			}
			set {
				allowActionBinding = value;
				if (designer != null)
					designer.Toolbar.AllowActionBinding = value;
			}
		}
		
		public void Dispose ()
		{
			if (designer != null) {
				designer.Editor.GroupModified -= OnModified;
				if (!designerRequested)
					designer.Destroy ();
			}
			
			project.ProjectReloaded -= OnProjectReloaded;
			
			if (plug != null)
				plug.Destroy ();

			System.Runtime.Remoting.RemotingServices.Disconnect (this);
		}

		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
		
		public object[] SaveState ()
		{
			if (autoCommitChanges)
				return null;

			XmlDocument doc = new XmlDocument ();
			doc.AppendChild (groupCopy.Write (new ObjectWriter (doc, FileFormat.Native)));
			
			Hashtable nameMap = new Hashtable ();
			foreach (DictionaryEntry e in actionCopyMap)
				nameMap [((Wrapper.Action)e.Key).Name] = ((Wrapper.Action)e.Value).Name;
			
			return new object[] { groupCopy.Name, nameMap, doc.OuterXml };
		}
		
		public void RestoreState (object[] data)
		{
			if (data == null)
				return;
			
			groupCopy = new Stetic.Wrapper.ActionGroup ();
			groupCopy.Name = (string) data [0];
			
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml ((string) data [2]);
			
			// Create the map which links edited action with source actions
			actionCopyMap.Clear ();
			foreach (DictionaryEntry e in (Hashtable) data [1]) {
				Wrapper.Action dupaction = groupCopy.GetAction ((string)e.Key);
				Wrapper.Action action = group.GetAction ((string)e.Value);
				actionCopyMap [dupaction] = action;
			}
			
			groupCopy.SignalAdded += new Stetic.SignalEventHandler (OnSignalAdded);
			groupCopy.SignalChanged += new Stetic.SignalChangedEventHandler (OnSignalChanged);
		}
		
		void OnSignalAdded (object s, Stetic.SignalEventArgs a)
		{
			Wrapper.Action action = (Wrapper.Action) a.Wrapper;
			frontend.NotifySignalAdded (action, action.Name, a.Signal);
		}
		
		void OnSignalChanged (object s, Stetic.SignalChangedEventArgs a)
		{
			Wrapper.Action action = (Wrapper.Action) a.Wrapper;
			frontend.NotifySignalChanged (action, action.Name, a.OldSignal, a.Signal);
		}
	}
}
