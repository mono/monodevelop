
using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Stetic
{
	public class Project: MarshalByRefObject
	{
		Application app;
		ProjectBackend backend;
		string fileName;
		IResourceProvider resourceProvider;
		Component selection;
		string tmpProjectFile;
		bool reloadRequested;
		List<WidgetInfo> widgets = new List<WidgetInfo> ();
		List<ActionGroupInfo> groups = new List<ActionGroupInfo> ();
		bool modified;
		ImportFileDelegate importFileDelegate;
		
		public event WidgetInfoEventHandler WidgetAdded;
		public event WidgetInfoEventHandler WidgetRemoved;
		public event ComponentNameEventHandler ComponentNameChanged;
		public event EventHandler ComponentTypesChanged;
		
		public event EventHandler ActionGroupsChanged;
		public event EventHandler ModifiedChanged;
		public event EventHandler Changed;

		
		// Internal events
		internal event BackendChangingHandler BackendChanging;
		internal event BackendChangedHandler BackendChanged;
		internal event ComponentSignalEventHandler SignalAdded;
		internal event ComponentSignalEventHandler SignalRemoved;
		internal event ComponentSignalEventHandler SignalChanged;

		public event EventHandler ProjectReloading;
		public event EventHandler ProjectReloaded;

		internal Project (Application app): this (app, null)
		{
		}
		
		internal Project (Application app, ProjectBackend backend)
		{
			this.app = app;
			if (backend != null) {
				this.backend = backend;
				backend.SetFrontend (this);
			}

			if (app is IsolatedApplication) {
				IsolatedApplication iapp = app as IsolatedApplication;
				iapp.BackendChanging += OnBackendChanging;
				iapp.BackendChanged += OnBackendChanged;
			}
		}
		
		internal ProjectBackend ProjectBackend {
			get {
				if (backend == null) {
					backend = app.Backend.CreateProject ();
					backend.SetFrontend (this);
					if (resourceProvider != null)
						backend.ResourceProvider = resourceProvider;
					if (fileName != null)
						backend.Load (fileName);
				}
				return backend; 
			}
		}
		
		internal bool IsBackendLoaded {
			get { return backend != null; }
		}
		
		internal Application App {
			get { return app; }
		}
		
		internal event EventHandler Disposed;

		public void Dispose ()
		{
			if (app is IsolatedApplication) {
				IsolatedApplication iapp = app as IsolatedApplication;
				iapp.BackendChanging -= OnBackendChanging;
				iapp.BackendChanged -= OnBackendChanged;
			}
			
			if (tmpProjectFile != null && File.Exists (tmpProjectFile)) {
				File.Delete (tmpProjectFile);
				tmpProjectFile = null;
			}
			if (backend != null)
				backend.Dispose ();
			if (Disposed != null)
				Disposed (this, EventArgs.Empty);
			System.Runtime.Remoting.RemotingServices.Disconnect (this);
		}

		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}

		public string FileName {
			get { return fileName; }
		}
		
		public IResourceProvider ResourceProvider { 
			get { return resourceProvider; }
			set { 
				resourceProvider = value;
				if (backend != null)
					backend.ResourceProvider = value;
			}
		}
		
		public Stetic.ProjectIconFactory IconFactory {
			get { return ProjectBackend.IconFactory; }
			set { backend.IconFactory = value; }
		}
		
		public ImportFileDelegate ImportFileCallback {
			get { return importFileDelegate; }
			set { importFileDelegate = value; }
		}
		
		public bool CanGenerateCode {
			get { return ProjectBackend.CanGenerateCode; }
		}
		
		public Component Selection {
			get { return selection; }
		}
		
		public string ImagesRootPath {
			get { return ProjectBackend.ImagesRootPath; }
			set { ProjectBackend.ImagesRootPath = value; }
		}
		
		public string TargetGtkVersion {
			get { return ProjectBackend.TargetGtkVersion; }
			set { ProjectBackend.TargetGtkVersion = value; }
		}
		
		public void Close ()
		{
			if (backend != null)
				backend.Close ();
		}
		
		public void Load (string fileName)
		{
			this.fileName = fileName;
			if (backend != null)
				backend.Load (fileName);
			
			using (StreamReader sr = new StreamReader (fileName)) {
				XmlTextReader reader = new XmlTextReader (sr); 
				
				reader.MoveToContent ();
				if (reader.IsEmptyElement)
					return;
				
				reader.ReadStartElement ("stetic-interface");
				if (reader.IsEmptyElement)
					return;
				while (reader.NodeType != XmlNodeType.EndElement) {
					if (reader.NodeType == XmlNodeType.Element) {
						if (reader.LocalName == "widget")
							ReadWidget (reader);
						else if (reader.LocalName == "action-group")
							ReadActionGroup (reader);
						else
							reader.Skip ();
					}
					else {
						reader.Skip ();
					}
					reader.MoveToContent ();
				}
			}
		}
		
		void ReadWidget (XmlTextReader reader)
		{
			WidgetInfo w = new WidgetInfo (this, reader.GetAttribute ("id"), reader.GetAttribute ("class"));
			widgets.Add (w);
			if (reader.IsEmptyElement) {
				reader.Skip ();
				return;
			}
			reader.ReadStartElement ();
			reader.MoveToContent ();
			while (reader.NodeType != XmlNodeType.EndElement) {
				if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "action-group") {
					w.AddGroup (reader.GetAttribute ("name"));
				}
				reader.Skip ();
				reader.MoveToContent ();
			}
			reader.ReadEndElement ();
		}
		
		void ReadActionGroup (XmlTextReader reader)
		{
			groups.Add (new ActionGroupInfo (this, reader.GetAttribute ("name")));
			reader.Skip ();
		}
		
		public void Save (string fileName)
		{
			this.fileName = fileName;
			if (backend != null)
				backend.Save (fileName);
		}
		
		public void ImportGlade (string fileName)
		{
			ProjectBackend.ImportGlade (fileName);
		}
		
		public void ExportGlade (string fileName)
		{
			ProjectBackend.ExportGlade (fileName);
		}
		
		public bool Modified {
			get {
				if (backend != null)
					return backend.Modified; 
				else
					return modified;
			}
			set {
				if (backend != null)
					backend.Modified = value;	
				else
					modified = true;
			}
		}
		
		public IEnumerable<WidgetInfo> Widgets {
			get { return widgets; }
		}
		
		public IEnumerable<ActionGroupInfo> ActionGroups {
			get { return groups; }
		}
		
		public WidgetInfo GetWidget (string name)
		{
			foreach (WidgetInfo w in widgets)
				if (w.Name == name)
					return w;
			return null;
		}
		
		public ActionGroupInfo GetActionGroup (string name)
		{
			foreach (ActionGroupInfo w in groups)
				if (w.Name == name)
					return w;
			return null;
		}
				
		public WidgetDesigner CreateWidgetDesigner (WidgetInfo widgetInfo, bool autoCommitChanges)
		{
			return new WidgetDesigner (this, widgetInfo.Name, autoCommitChanges);
		}
		
		public ActionGroupDesigner CreateActionGroupDesigner (ActionGroupInfo actionGroup, bool autoCommitChanges)
		{
			return new ActionGroupDesigner (this, null, actionGroup.Name, null, autoCommitChanges);
		}

		public WidgetInfo AddNewComponent (ComponentType type, string name)
		{
			object ob = ProjectBackend.AddNewWidget (type.Name, name);
			WidgetComponent wc = (WidgetComponent) App.GetComponent (ob, null, null);
			WidgetInfo wi = GetWidget (wc.Name);
			if (wi == null) {
				wi = new WidgetInfo (this, wc);
				widgets.Add (wi);
			}
			return wi;
		}
		
		public WidgetInfo AddNewComponent (XmlElement template)
		{
			object ob = ProjectBackend.AddNewWidgetFromTemplate (template.OuterXml);
			WidgetComponent wc = (WidgetComponent) App.GetComponent (ob, null, null);
			WidgetInfo wi = GetWidget (wc.Name);
			if (wi == null) {
				wi = new WidgetInfo (this, wc);
				widgets.Add (wi);
			}
			return wi;
		}
		
		public ComponentType[] GetComponentTypes ()
		{
			ArrayList types = new ArrayList ();
			
			ArrayList typeNames = ProjectBackend.GetComponentTypes ();
			for (int n=0; n<typeNames.Count; n++)
				types.Add (app.GetComponentType ((string) typeNames [n]));

			// Global action groups
			foreach (ActionGroupComponent grp in GetActionGroups ()) {
				foreach (ActionComponent ac in grp.GetActions ())
					types.Add (new ComponentType (app, ac));
			}
			
			return (ComponentType[]) types.ToArray (typeof(ComponentType));
		}
		
		// Returns a list of all support widget types (including base classes)
		public string[] GetWidgetTypes ()
		{
			return ProjectBackend.GetWidgetTypes ();
		}
		
		public void RemoveComponent (WidgetInfo component)
		{
			ProjectBackend.RemoveWidget (component.Name);
		}
		
		public WidgetComponent GetComponent (string name)
		{
			object ob = ProjectBackend.GetTopLevelWrapper (name, false);
			if (ob != null)
				return (WidgetComponent) App.GetComponent (ob, name, null);
			else
				return null;
		}
		
		public ActionGroupComponent AddNewActionGroup (string id)
		{
			object ob = ProjectBackend.AddNewActionGroup (id);
			ActionGroupComponent ac = (ActionGroupComponent) App.GetComponent (ob, id, null);
			
			// Don't wait for the group added event to come to update the groups list since
			// it may be too late.
			ActionGroupInfo gi = GetActionGroup (ac.Name);
			if (gi == null) {
				gi = new ActionGroupInfo (this, ac.Name);
				groups.Add (gi);
			}
			return ac;
		}
		
		public ActionGroupComponent AddNewActionGroup (XmlElement template)
		{
			object ob = ProjectBackend.AddNewActionGroupFromTemplate (template.OuterXml);
			ActionGroupComponent ac = (ActionGroupComponent) App.GetComponent (ob, null, null);
			
			// Don't wait for the group added event to come to update the groups list since
			// it may be too late.
			ActionGroupInfo gi = GetActionGroup (ac.Name);
			if (gi == null) {
				gi = new ActionGroupInfo (this, ac.Name);
				groups.Add (gi);
			}
			return ac;
		}
		
		public void RemoveActionGroup (ActionGroupInfo group)
		{
			ActionGroupComponent ac = (ActionGroupComponent) group.Component;
			ProjectBackend.RemoveActionGroup ((Stetic.Wrapper.ActionGroup) ac.Backend);
		}
		
		internal ActionGroupComponent[] GetActionGroups ()
		{
			Wrapper.ActionGroup[] acs = ProjectBackend.GetActionGroups ();
			
			ArrayList comps = new ArrayList (acs.Length);
			for (int n=0; n<acs.Length; n++) {
				ActionGroupComponent ag = (ActionGroupComponent) App.GetComponent (acs[n], null, null);
				if (ag != null)
					comps.Add (ag);
			}
				
			return (ActionGroupComponent[]) comps.ToArray (typeof(ActionGroupComponent));
		}

		public void AddWidgetLibrary (string assemblyPath)
		{
			AddWidgetLibrary (assemblyPath, false);
		}
		
		public void AddWidgetLibrary (string assemblyPath, bool isInternal)
		{
			reloadRequested = false;
			ProjectBackend.AddWidgetLibrary (assemblyPath, isInternal);
			app.UpdateWidgetLibraries (false, false);
			if (!reloadRequested)
				ProjectBackend.Reload ();
		}
		
		public void RemoveWidgetLibrary (string assemblyPath)
		{
			reloadRequested = false;
			ProjectBackend.RemoveWidgetLibrary (assemblyPath);
			app.UpdateWidgetLibraries (false, false);
			if (!reloadRequested)
				ProjectBackend.Reload ();
		}
		
		public string[] WidgetLibraries {
			get {
				return (string[]) ProjectBackend.WidgetLibraries.ToArray (typeof(string));
			}
		}
		
		public void SetWidgetLibraries (string[] libraries, string[] internalLibraries)
		{
			reloadRequested = false;
			
			ArrayList libs = new ArrayList ();
			libs.AddRange (libraries);
			libs.AddRange (internalLibraries);
			ProjectBackend.WidgetLibraries = libs;
			
			libs = new ArrayList ();
			libs.AddRange (internalLibraries);
			ProjectBackend.InternalWidgetLibraries = libs;
			
			app.UpdateWidgetLibraries (false, false);
			if (!reloadRequested)
				ProjectBackend.Reload ();
		}
		
/*		public bool CanCopySelection {
			get { return Selection != null ? Selection.CanCopy : false; }
		}
		
		public bool CanCutSelection {
			get { return Selection != null ? Selection.CanCut : false; }
		}
		
		public bool CanPasteToSelection {
			get { return Selection != null ? Selection.CanPaste : false; }
		}
		
		public void CopySelection ()
		{
			if (Selection != null)
				backend.ClipboardCopySelection ();
		}
		
		public void CutSelection ()
		{
			if (Selection != null)
				backend.ClipboardCutSelection ();
		}
		
		public void PasteToSelection ()
		{
			if (Selection != null)
				backend.ClipboardPaste ();
		}
		
		public void DeleteSelection ()
		{
			backend.DeleteSelection ();
		}
*/
		
		public void EditIcons ()
		{
			ProjectBackend.EditIcons ();
		}
		
		internal void NotifyWidgetAdded (object obj, string name, string typeName)
		{
			GuiDispatch.InvokeSync (
				delegate {
					Component c = App.GetComponent (obj, name, typeName);
					if (c != null) {
						WidgetInfo wi = GetWidget (c.Name);
						if (wi == null) {
							wi = new WidgetInfo (this, c);
							widgets.Add (wi);
						}
						if (WidgetAdded != null)
							WidgetAdded (this, new WidgetInfoEventArgs (this, wi));
					}
				}
			);
		}
		
		internal void NotifyWidgetRemoved (string name)
		{
			GuiDispatch.InvokeSync (
				delegate {
					WidgetInfo wi = GetWidget (name);
					if (wi != null) {
						widgets.Remove (wi);
						if (WidgetRemoved != null)
							WidgetRemoved (this, new WidgetInfoEventArgs (this, wi));
					}
				}
			);
		}
		
		internal void NotifyModifiedChanged ()
		{
			GuiDispatch.InvokeSync (
				delegate {
					if (ModifiedChanged != null)
						ModifiedChanged (this, EventArgs.Empty);
				}
			);
		}
		
		internal void NotifyChanged ()
		{
			GuiDispatch.InvokeSync (
				delegate {
					if (Changed != null)
						Changed (this, EventArgs.Empty);
				
					// TODO: Optimize
					foreach (ProjectItemInfo it in widgets)
						it.NotifyChanged ();
					foreach (ProjectItemInfo it in groups)
						it.NotifyChanged ();
				}
			);
		}
		
		internal void NotifyWidgetNameChanged (object obj, string oldName, string newName, bool isRoot)
		{
			WidgetComponent c = obj != null ? (WidgetComponent) App.GetComponent (obj, null, null) : null;
			if (c != null)
				c.UpdateName (newName);
			
			if (isRoot) {
				WidgetInfo wi = GetWidget (oldName);
				if (wi != null)
					wi.NotifyNameChanged (newName);
			}
			
			GuiDispatch.InvokeSync (
				delegate {
					if (c != null) {
						if (ComponentNameChanged != null)
							ComponentNameChanged (this, new ComponentNameEventArgs (this, c, oldName));
					}
				}
			);
		}
		
		internal void NotifyActionGroupAdded (string group)
		{
			GuiDispatch.InvokeSync (delegate {
				ActionGroupInfo gi = GetActionGroup (group);
				if (gi == null) {
					gi = new ActionGroupInfo (this, group);
					groups.Add (gi);
				}
				if (ActionGroupsChanged != null)
					ActionGroupsChanged (this, EventArgs.Empty);
			});
		}
		
		internal void NotifyActionGroupRemoved (string group)
		{
			GuiDispatch.InvokeSync (delegate {
				ActionGroupInfo gi = GetActionGroup (group);
				if (gi != null) {
					groups.Remove (gi);
					if (ActionGroupsChanged != null)
						ActionGroupsChanged (this, EventArgs.Empty);
				}
			});
		}
		
		internal void NotifySignalAdded (object obj, string name, Signal signal)
		{
			GuiDispatch.InvokeSync (delegate {
				if (SignalAdded != null) {
					Component c = App.GetComponent (obj, name, null);
					if (c != null)
						SignalAdded (this, new ComponentSignalEventArgs (this, c, null, signal));
				}
			});
		}
		
		internal void NotifySignalRemoved (object obj, string name, Signal signal)
		{
			GuiDispatch.InvokeSync (delegate {
				if (SignalRemoved != null) {
					Component c = App.GetComponent (obj, name, null);
					if (c != null)
						SignalRemoved (this, new ComponentSignalEventArgs (this, c, null, signal));
				}
			});
		}
		
		internal void NotifySignalChanged (object obj, string name, Signal oldSignal, Signal signal)
		{
			GuiDispatch.InvokeSync (delegate {
				if (SignalChanged != null) {
					Component c = App.GetComponent (obj, name, null);
					if (c != null)
						SignalChanged (this, new ComponentSignalEventArgs (this, c, oldSignal, signal));
				}
			});
		}
		
		internal void NotifyProjectReloaded ()
		{
			GuiDispatch.InvokeSync (delegate {
				if (ProjectReloaded != null)
					ProjectReloaded (this, EventArgs.Empty);
			});
		}
		
		internal void NotifyProjectReloading ()
		{
			GuiDispatch.InvokeSync (delegate {
				if (ProjectReloading != null)
					ProjectReloading (this, EventArgs.Empty);
			});
		}
		
		internal void NotifyComponentTypesChanged ()
		{
			GuiDispatch.InvokeSync (delegate {
				if (ComponentTypesChanged != null)
					ComponentTypesChanged (this, EventArgs.Empty);
			});
		}
		
		internal string ImportFile (string filePath)
		{
			if (importFileDelegate != null) {
				string res = null;
				GuiDispatch.InvokeSync (delegate {
					res = importFileDelegate (filePath);
				});
				return res;
			}
			else
				return filePath;
		}
		
		internal void NotifyUpdateLibraries ()
		{
			app.UpdateWidgetLibraries (false, false);
		}
		
		void OnBackendChanging ()
		{
			selection = null;
				
			if (BackendChanging != null)
				BackendChanging ();
		}
		
		void OnBackendChanged (ApplicationBackend oldBackend)
		{
			if (oldBackend != null) {
				tmpProjectFile = Path.GetTempFileName ();
				backend.Save (tmpProjectFile);
				backend.Dispose ();
			}
			
			backend = app.Backend.CreateProject ();
			backend.SetFrontend (this);

			if (tmpProjectFile != null && File.Exists (tmpProjectFile)) {
				backend.Load (tmpProjectFile, fileName);
				File.Delete (tmpProjectFile);
				tmpProjectFile = null;
			} else if (fileName != null) {
				backend.Load (fileName);
			}

			if (resourceProvider != null)
				backend.ResourceProvider = resourceProvider;

			if (BackendChanged != null)
				BackendChanged (oldBackend);
				
			if (ProjectReloaded != null)
				ProjectReloaded (this, EventArgs.Empty);
		}
	}

	public abstract class ProjectItemInfo
	{
		string name;
		protected Project project;
		
		public event EventHandler Changed;

		internal ProjectItemInfo (Project project, string name)
		{
			this.name = name;
			this.project = project;
		}
		
		public string Name {
			get { return name; }
			set { Component.Name = value; name = value; }
		}
		
		internal void NotifyNameChanged (string name)
		{
			this.name = name;
			NotifyChanged ();
		}
		
		internal void NotifyChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public abstract Component Component { get; }
	}
		
	public class WidgetInfo: ProjectItemInfo
	{
		string type;
		List<ActionGroupInfo> groups;
		
		internal WidgetInfo (Project project, string name, string type): base (project, name)
		{
			this.type = type;
		}
		
		internal WidgetInfo (Project project, Component c): base (project, c.Name)
		{
			type = c.Type.Name;
		}
		
		public IEnumerable<ActionGroupInfo> ActionGroups {
			get {
				if (groups != null)
					return groups;
				else
					return new ActionGroupInfo [0];
			}
		}
		
		public string Type {
			get { return type; }
		}
		
		public bool IsWindow {
			get { return type == "Gtk.Dialog" || type == "Gtk.Window"; }
		}
		
		public override Component Component {
			get { return project.GetComponent (Name); }
		}
		
		internal void AddGroup (string group)
		{
			if (groups == null)
				groups = new List<ActionGroupInfo> ();
			groups.Add (new ActionGroupInfo (project, group, Name));
		}
	}
	
	public class ActionGroupInfo: ProjectItemInfo
	{
		string widgetName;
		
		internal ActionGroupInfo (Project project, string name): base (project, name)
		{
		}
		
		internal ActionGroupInfo (Project project, string name, string widgetName): base (project, name)
		{
			this.widgetName = widgetName;
		}
		
		public override Component Component {
			get {
				ActionGroupComponent[] ags;
				if (widgetName != null) {
					WidgetComponent c = project.GetComponent (widgetName) as WidgetComponent;
					if (c == null)
						return null;
					ags = c.GetActionGroups ();
				}
				else {
					ags = project.GetActionGroups ();
				}
				foreach (ActionGroupComponent ag in ags)
					if (ag.Name == Name)
						return ag;
				return null;
			}
		}
	}
	
	public delegate string ImportFileDelegate (string fileName);
}
