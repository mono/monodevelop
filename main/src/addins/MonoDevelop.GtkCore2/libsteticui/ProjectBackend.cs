using Gtk;
using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.CodeDom;
using Mono.Unix;

namespace Stetic {

	internal class ProjectBackend : MarshalByRefObject, IProject, IDisposable 
	{
		List<WidgetData> topLevels;
		bool modified;
		Gtk.Widget selection;
		string id;
		string folderName;
		XmlDocument tempDoc;
		bool loading;
		IResourceProvider resourceProvider;
		
		// Global action groups of the project
		Stetic.Wrapper.ActionGroupCollection actionGroups;
		bool ownedGlobalActionGroups = true;	// It may be false when reusing groups from another project
		
		Stetic.ProjectIconFactory iconFactory;
		Project frontend;
		ArrayList widgetLibraries;
		ArrayList internalLibs;
		ApplicationBackend app;
		AssemblyResolver resolver;
		string imagesRootPath;
		string targetGtkVersion;
		List<string> modifiedTopLevels;
		//During project conversion flag is set
		bool converting;
		
		// The action collection of the last selected widget
		Stetic.Wrapper.ActionGroupCollection oldTopActionCollection;
		
		public event Wrapper.WidgetNameChangedHandler WidgetNameChanged;
		public event Wrapper.WidgetEventHandler WidgetAdded;
		public event Wrapper.WidgetEventHandler WidgetContentsChanged;
		public event ObjectWrapperEventHandler ObjectChanged;
		public event EventHandler ComponentTypesChanged;
		
		public event SignalEventHandler SignalAdded;
		public event SignalEventHandler SignalRemoved;
		public event SignalChangedEventHandler SignalChanged;
		
		public event Wrapper.WidgetEventHandler SelectionChanged;
		public event ProjectChangedEventHandler Changed;
		
		// Fired when the project has been reloaded, due for example to
		// a change in the registry
		public event EventHandler ProjectReloading;
		public event EventHandler ProjectReloaded;

		public ProjectBackend (ApplicationBackend app)
		{
			this.app = app;
			topLevels = new List<WidgetData> ();
			
			ActionGroups = new Stetic.Wrapper.ActionGroupCollection ();

			Registry.RegistryChanging += OnRegistryChanging;
			Registry.RegistryChanged += OnRegistryChanged;
			
			iconFactory = new ProjectIconFactory ();
			widgetLibraries = new ArrayList ();
			internalLibs = new ArrayList ();
			modifiedTopLevels = new List<string> ();
		}
		
		public void Dispose ()
		{
			// First of all, disconnect from the frontend,
			// to avoid sending notifications while disposing
			frontend = null;
			
			if (oldTopActionCollection != null)
				oldTopActionCollection.ActionGroupChanged -= OnComponentTypesChanged;

			Registry.RegistryChanging -= OnRegistryChanging;
			Registry.RegistryChanged -= OnRegistryChanged;
			Close ();
			iconFactory = null;
			ActionGroups = null;
			System.Runtime.Remoting.RemotingServices.Disconnect (this);
		}

		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
		
		
		public string FileName {
			get { throw new Exception("FileName is obsolete"); }
			set { throw new Exception("FileName is obsolete"); }
		}
			
		public string FolderName {
			get { return folderName; }
			set {
				this.folderName = value;
				if (folderName != null )
					Id = System.IO.Path.GetFullPath (folderName);
				else
					Id = null;
			}
		}
		
		internal ArrayList WidgetLibraries {
			get { return widgetLibraries; }
			set { widgetLibraries = value; }
		}
		
		internal ArrayList InternalWidgetLibraries {
			get { return internalLibs; }
			set { internalLibs = value; }
		}
		
		public bool IsInternalLibrary (string lib)
		{
			return internalLibs.Contains (lib);
		}
		
		public bool CanGenerateCode {
			get {
				// It can generate code if all libraries on which depend can generate code
				foreach (string s in widgetLibraries) {
					WidgetLibrary lib = Registry.GetWidgetLibrary (s);
					if (lib != null && !lib.CanGenerateCode)
						return false;
				}
				return true;
			}
		}
		
		public string TargetGtkVersion {
			get { return targetGtkVersion ?? string.Empty; }
			set {
				if (TargetGtkVersion == value)
					return;
				targetGtkVersion = value;
				
//				 Update the project
				OnRegistryChanging (null, null);
				OnRegistryChanged (null, null);
			}
		}

		public string ImagesRootPath {
			get {
				if (string.IsNullOrEmpty (imagesRootPath)) {
					if (string.IsNullOrEmpty (folderName))
						return ".";
					else
						return Path.GetFullPath (folderName);
				}
				else {
					if (Path.IsPathRooted (imagesRootPath))
						return imagesRootPath;
					else if (!string.IsNullOrEmpty (folderName))
						return Path.GetFullPath (Path.Combine (folderName, imagesRootPath));
					else
						return imagesRootPath;
				}
			}
			set { imagesRootPath = value; }
		}

		
		public void AddWidgetLibrary (string lib)
		{
			AddWidgetLibrary (lib, false);
		}
		
		public void AddWidgetLibrary (string lib, bool isInternal)
		{
			if (!widgetLibraries.Contains (lib))
				widgetLibraries.Add (lib);
			if (isInternal) {
				if (!internalLibs.Contains (lib))
					internalLibs.Add (lib);
			}
			else {
				internalLibs.Remove (lib);
			}
		}
		
		public void RemoveWidgetLibrary (string lib)
		{
			widgetLibraries.Remove (lib);
			internalLibs.Remove (lib);
		}
			
		public ArrayList GetComponentTypes ()
		{
			ArrayList list = new ArrayList ();
			foreach (WidgetLibrary lib in app.GetProjectLibraries (this)) {
				// Don't include in the list widgets which are internal (when the library is
				// not internal to the project), widgets not assigned to any category, and deprecated ones.
				bool isInternalLib = IsInternalLibrary (lib.Name);
				if (lib.NeedsReload)
					lib.Reload ();
				foreach (ClassDescriptor cd in lib.AllClasses) {
					if (!cd.Deprecated && cd.Category.Length > 0 && (isInternalLib || !cd.IsInternal) && cd.SupportsGtkVersion (TargetGtkVersion))
						list.Add (cd.Name);
				}
			}
			return list;
		}
		
		public string[] GetWidgetTypes ()
		{
			List<string> list = new List<string> ();
			foreach (WidgetLibrary lib in app.GetProjectLibraries (this)) {
				// Don't include in the list widgets which are internal (when the library is
				// not internal to the project) and deprecated ones.
				bool isInternalLib = IsInternalLibrary (lib.Name);
				foreach (ClassDescriptor cd in lib.AllClasses) {
					if (!cd.Deprecated && (isInternalLib || !cd.IsInternal))
						list.Add (cd.Name);
				}
			}
			return list.ToArray ();
		}
		
		public IResourceProvider ResourceProvider { 
			get { return resourceProvider; }
			set { resourceProvider = value; }
		}
		
		public Stetic.Wrapper.ActionGroupCollection ActionGroups {
			get { return actionGroups; }
			set {
				if (actionGroups != null) {
					actionGroups.ActionGroupAdded -= OnGroupAdded;
					actionGroups.ActionGroupRemoved -= OnGroupRemoved;
					actionGroups.ActionGroupChanged -= OnComponentTypesChanged;
				}
				actionGroups = value;
				if (actionGroups != null) {
					actionGroups.ActionGroupAdded += OnGroupAdded;
					actionGroups.ActionGroupRemoved += OnGroupRemoved;
					actionGroups.ActionGroupChanged += OnComponentTypesChanged;
				}
				ownedGlobalActionGroups = true;
			}
		}
		
		public void AttachActionGroups (Stetic.Wrapper.ActionGroupCollection groups)
		{
			ActionGroups = groups;
			ownedGlobalActionGroups = false;
		}
		
		public Stetic.ProjectIconFactory IconFactory {
			get { return iconFactory; }
			set { iconFactory = value; }
		}
		
		internal void SetFrontend (Project project)
		{
			frontend = project;
		}
		
		public void Close ()
		{	
			if (actionGroups != null && ownedGlobalActionGroups) {
				foreach (Stetic.Wrapper.ActionGroup ag in actionGroups)
					ag.Dispose ();
				actionGroups.Clear ();
			}

			foreach (WidgetData wd in topLevels) {
				if (wd.Widget != null)
					wd.Widget.Destroy ();
			}

			selection = null;
			topLevels.Clear ();
			//widgetLibraries.Clear ();
			

			iconFactory = new ProjectIconFactory ();
		}
		
		public void Load (string folderName)
		{
			this.folderName = folderName;
			XmlDocument doc = new XmlDocument ();
			doc.PreserveWhitespace = true;
			XmlElement toplevel = doc.CreateElement ("stetic-interface");
			doc.AppendChild (toplevel);
			modifiedTopLevels.Clear ();
			
			ReadIconFactory (doc);
			ReadActionGroups (doc);
			ReadTopLevels (doc);
			Read (doc);
			
			Id = System.IO.Path.GetFullPath (folderName);
		}
		
		public void LoadOldVersion (string fileName)
		{
			if (!File.Exists (fileName))
				return;
			
			XmlDocument doc = new XmlDocument ();
			doc.PreserveWhitespace = true;
			doc.Load (fileName);
			
			Read (doc);
			
			Id = System.IO.Path.GetFileName (fileName);
		}
		
		void ReadIconFactory (XmlDocument doc)
		{
			XmlNode node = doc.SelectSingleNode ("/stetic-interface");
			if (node == null)
				return;
			
			string basePath = folderName;
			string xmlfile = Path.Combine(basePath, "IconFactory.gtkx");
			
			if (File.Exists (xmlfile)) {
			
				XmlDocument doc2 = new XmlDocument ();
				doc2.PreserveWhitespace = true;
				doc2.Load (xmlfile);
		
				XmlNode ifnode2 = doc2.SelectSingleNode ("/stetic-interface/icon-factory");
				
				if (ifnode2 != null) {
					XmlNode ifnode = doc.ImportNode (ifnode2, true);
					node.AppendChild (ifnode);
				}
			}
		}
		
		void ReadActionGroups (XmlDocument doc)
		{
			ReadSplitFiles (doc, "action-group", "name");
		}
		
		void ReadTopLevels (XmlDocument doc)
		{
			ReadSplitFiles (doc, "widget", "id");
		}
		
		void ReadSplitFiles (XmlDocument doc, string splitElement, string idAttribute)
		{
			XmlNode node = doc.SelectSingleNode ("/stetic-interface");
			if (node == null)
				return;
			
			foreach (string basePath in frontend.DesignInfo.GetComponentFolders ()) {
				DirectoryInfo dir = new DirectoryInfo (basePath);
				
				foreach (FileInfo file in dir.GetFiles ()) {
					if (file.Extension == ".gtkx") {
						
						XmlDocument wdoc = new XmlDocument ();
						wdoc.PreserveWhitespace = true;
						wdoc.Load (file.FullName);
				
						XmlNode wnode = wdoc.SelectSingleNode ("/stetic-interface");
						
						foreach (XmlElement toplevel in wnode.SelectNodes (splitElement)) {
							string id = toplevel.GetAttribute (idAttribute);
							
							if (frontend.DesignInfo.HasComponentFile (id)) {
								XmlNode imported = doc.ImportNode (toplevel, true);
								node.AppendChild (imported);
							}
						}
					}
				}
			}
		}
		
		void AddActionGroup (XmlElement groupElem)
		{
			ObjectReader reader = new ObjectReader (this, FileFormat.Native);
			
			Wrapper.ActionGroup actionGroup = new Wrapper.ActionGroup ();
			actionGroup.Read (reader, groupElem);
			actionGroups.Add (actionGroup);
		}
		
		void AddWidget (XmlElement toplevel)
		{
			topLevels.Add (new WidgetData (toplevel.GetAttribute ("id"), toplevel, null));
		}
		
		void Read (XmlDocument doc)
		{
			loading = true;

			try {
				Close ();
				
				XmlNode node = doc.SelectSingleNode ("/stetic-interface");
				if (node == null)
					throw new ApplicationException (Catalog.GetString ("Not a Stetic file according to node name."));
				
				// Load the assembly directories
				resolver = new AssemblyResolver (app);
				app.LoadLibraries (resolver, widgetLibraries);
				
				if (ownedGlobalActionGroups) {
					foreach (XmlElement groupElem in node.SelectNodes ("action-group")) 
						AddActionGroup (groupElem);
				}
				
				XmlElement iconsElem = node.SelectSingleNode ("icon-factory") as XmlElement;
				if (iconsElem != null)
					iconFactory.Read (this, iconsElem);
				
				foreach (XmlElement toplevel in node.SelectNodes ("widget"))
					AddWidget (toplevel);
				
			} finally {
				loading = false;
			}
		}
		
		public Gtk.Widget GetWidget (WidgetData data)
		{
			if (data.Widget == null) {
				try {
					loading = true;
					ObjectReader reader = new ObjectReader (this, FileFormat.Native);
					Wrapper.Container wrapper = Stetic.ObjectWrapper.ReadObject (reader, data.XmlData, null) as Wrapper.Container;
					data.Widget = wrapper.Wrapped;
					data.Widget.Destroyed += (s,e) => data.Widget = null;
				} finally {
					loading = false;
				}
			}
			
			return data.Widget;
		}
		
		public bool ReloadTopLevel (string topLevelName)
		{
			XmlElement topLevelElem = ReadDesignerFile (topLevelName, "widget");
			if (topLevelName != null) {
				WidgetData data = GetWidgetData (topLevelName);
			
				if (data != null) {
					//Stetic.Wrapper.Widget ww = Stetic.Wrapper.Widget.Lookup (data.Widget);
					data.SetXmlData (topLevelName, topLevelElem);
					GetWidget (data);
					return true;
				}
			}
			return false;
		}
		
		public void ReloadComponent (string componentName)
		{
			ReloadTopLevel (componentName);
			//TODO: Make reloading works for action groups
		}
			               
		
		public void ReloadActionGroup (string groupName)
		{
			//TODO : Implement method ReloadActionGroup
		}
		
		XmlElement ReadDesignerFile (string componentName, string elementName)
		{
			string gtkxFile = GetDesignerFileName (componentName);
			if (gtkxFile != null) {
				XmlDocument wdoc = new XmlDocument ();
				wdoc.PreserveWhitespace = true;
				wdoc.Load (gtkxFile);
		
				XmlNode wnode = wdoc.SelectSingleNode ("/stetic-interface");
				foreach (XmlElement toplevel in wnode.SelectNodes (elementName)) {
					return toplevel;
				}
				
				string msg = string.Format (@"Cannot find /stetic-interface/{0} element in {1} file.", 
				                             elementName, gtkxFile);
				throw new InvalidOperationException (msg);
			}
			
			return null;
		}
		
		public void ConvertProject (string oldSteticFileName, string newGuiFolderName)
		{
			converting = true;
			try {
				LoadOldVersion (oldSteticFileName);
				Save (newGuiFolderName);
			} finally {
				converting = false;
			}
		}
		
		public void Save (string folderName)
		{
			this.folderName = folderName;
			XmlDocument doc = Write (false);
			
			XmlTextWriter writer = null;
			try {
				WriteIconFactory (doc);
				WriteActionGroups (doc);
				WriteTopLevels (doc);

			} finally {
				if (writer != null)
					writer.Close ();
			}
		}
		
		private void WriteIconFactory (XmlDocument doc)
		{
			XmlNode node = doc.SelectSingleNode ("/stetic-interface");
			if (node == null)
				return;
			
			XmlNode ifnode = node.SelectSingleNode ("icon-factory");
			if (ifnode == null)
				return;
			
			XmlDocument doc2 = new XmlDocument ();
			doc2.PreserveWhitespace = true;

			XmlElement node2 = doc2.CreateElement ("stetic-interface");
			doc2.AppendChild (node2);
		
			XmlNode ifnode2 = doc2.ImportNode (ifnode, true);
			node2.AppendChild (ifnode2);
			
			string basePath = this.folderName;
			string xmlFile = Path.Combine (basePath, "IconFactory.gtkx");
			WriteXmlFile (xmlFile, doc2);
			
			node.RemoveChild (ifnode);
		}
			
		void WriteActionGroups (XmlDocument doc)
		{
			WriteSplitFiles (doc, "action-group", "name");
		}
		
		void WriteTopLevels (XmlDocument doc)
		{
			WriteSplitFiles (doc, "widget", "id");
		}
		
		void WriteSplitFiles (XmlDocument doc, string splitElement, string idAttribute)
		{
			XmlNode node = doc.SelectSingleNode ("/stetic-interface");
			if (node == null)
				return;
			
			foreach (XmlElement toplevel in node.SelectNodes (splitElement)) {
					
					string id = toplevel.GetAttribute (idAttribute);
					if (modifiedTopLevels.Contains (id) || converting) {
						string xmlFile = GetDesignerFileName (id);
						if (xmlFile != null) {
							XmlDocument doc2 = new XmlDocument ();
							doc2.PreserveWhitespace = true;
							                     
							XmlElement node2 = doc2.CreateElement ("stetic-interface");
							doc2.AppendChild (node2);
						
							XmlNode wnode2 = doc2.ImportNode (toplevel, true);
							node2.AppendChild (wnode2);
						
							WriteXmlFile (xmlFile, doc2);
							if (modifiedTopLevels.Contains (id))
								modifiedTopLevels.Remove (id);
						}
					}
			}	
		}
		
		private string GetDesignerFileName (string componentName)
		{
			string componentFile = frontend.DesignInfo.GetComponentFile (componentName);			
			return frontend.DesignInfo.GetDesignerFileFromComponent (componentFile);
		}
		
		void WriteXmlFile (string xmlFile, XmlDocument doc)
		{
			XmlTextWriter writer = null;
			try {
				writer = new XmlTextWriter (xmlFile + "~", System.Text.Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				
				doc.Save (writer);
				writer.Close ();
				
				File.Copy (xmlFile + "~", xmlFile, true);
				File.Delete (xmlFile + "~");
			} finally {
				if (writer != null)
					writer.Close ();
			}
		}
		
		XmlDocument Write (bool includeUndoInfo)
		{
			XmlDocument doc = new XmlDocument ();
			doc.PreserveWhitespace = true;

			XmlElement toplevel = doc.CreateElement ("stetic-interface");
			doc.AppendChild (toplevel);

			ObjectWriter writer = new ObjectWriter (doc, FileFormat.Native);
			writer.CreateUndoInfo = includeUndoInfo;
			if (ownedGlobalActionGroups) {
				foreach (Wrapper.ActionGroup agroup in actionGroups) {
					XmlElement elem = agroup.Write (writer);
					toplevel.AppendChild (elem);
				}
			}
			
			if (iconFactory.Icons.Count > 0)
				toplevel.AppendChild (iconFactory.Write (doc));

			foreach (WidgetData data in topLevels) {
				if (data.Widget != null) {
					Stetic.Wrapper.Container wrapper = Stetic.Wrapper.Container.Lookup (data.Widget);
					if (wrapper == null)
						continue;

					XmlElement elem = wrapper.Write (writer);
					if (elem != null)
						toplevel.AppendChild (elem);
				} else {
					toplevel.AppendChild (doc.ImportNode (data.XmlData, true));
				}
			}
			
			// Remove undo annotations from the xml document
			if (!includeUndoInfo)
				CleanUndoData (doc.DocumentElement);
			
			return doc;
		}
		
		public void ImportGlade (string fileName)
		{
			GladeFiles.Import (this, fileName);
		}
		
		public void ExportGlade (string fileName)
		{
			GladeFiles.Export (this, fileName);
		}
		
		public void EditIcons ()
		{
			using (Stetic.Editor.EditIconFactoryDialog dlg = new Stetic.Editor.EditIconFactoryDialog (null, this, this.IconFactory)) {
				dlg.Run ();
			}
		}
		
//		internal WidgetEditSession CreateWidgetDesignerSession (WidgetDesignerFrontend frontend, string windowName, Stetic.ProjectBackend editingBackend, bool autoCommitChanges)
//		{
//			return new WidgetEditSession (this, frontend, windowName, editingBackend, autoCommitChanges);
//		}
		
		internal WidgetEditSession CreateWidgetDesignerSession (WidgetDesignerFrontend frontend, string windowName)
		{
			return new WidgetEditSession (this, frontend, windowName);
		}
		
		internal ActionGroupEditSession CreateGlobalActionGroupDesignerSession (ActionGroupDesignerFrontend frontend, string groupName, bool autoCommitChanges)
		{
			return new ActionGroupEditSession (frontend, this, null, groupName, autoCommitChanges);
		}
		
		internal ActionGroupEditSession CreateLocalActionGroupDesignerSession (ActionGroupDesignerFrontend frontend, string windowName, bool autoCommitChanges)
		{
			return new ActionGroupEditSession (frontend, this, windowName, null, autoCommitChanges);
		}
		
		public Wrapper.Container GetTopLevelWrapper (string name, bool throwIfNotFound)
		{
			Gtk.Widget w = GetWidget (name);
			if (w != null) {
				Wrapper.Container ww = Wrapper.Container.Lookup (w);
				if (ww != null)
					return (Wrapper.Container) Component.GetSafeReference (ww);
			}
			if (throwIfNotFound)
				throw new InvalidOperationException ("Component not found: " + name);
			return null;
		}
		
		public object AddNewComponent (string fileName)
		{
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true;
			doc.Load (fileName);
			
			XmlElement toplevel = (XmlElement)doc.SelectSingleNode ("/stetic-interface/widget");
			if (toplevel != null)
				return AddNewWidgetFromTemplate (toplevel.OuterXml);
			
			XmlElement groupElem = (XmlElement)doc.SelectSingleNode ("/stetic-interface/action-group");
			if (groupElem != null)
				return AddNewActionGroupFromTemplate (groupElem.OuterXml);
			
			return null;
		}
		
		public object AddNewWidget (string type, string name)
		{
			ClassDescriptor cls = Registry.LookupClassByName (type);
			Gtk.Widget w = (Gtk.Widget) cls.NewInstance (this);
			w.Name = name;
			this.AddWidget (w);
			return Component.GetSafeReference (ObjectWrapper.Lookup (w));
		}
		
		public object AddNewWidgetFromTemplate (string template)
		{
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (template);
			Gtk.Widget widget = Stetic.WidgetUtils.ImportWidget (this, doc.DocumentElement);
			AddWidget (widget);
			
			string name = widget.Name;
			return Component.GetSafeReference (ObjectWrapper.Lookup (widget));
		}
		
		public void RemoveWidget (string name)
		{
			WidgetData data = GetWidgetData (name);
			if (data == null)
				return;
			
			if (frontend != null)
				frontend.NotifyWidgetRemoved (data.Name);
			
			if (modifiedTopLevels.Contains (name))
				modifiedTopLevels.Remove (name);
		}
		
		public Stetic.Wrapper.ActionGroup AddNewActionGroup (string name)
		{
			Stetic.Wrapper.ActionGroup group = new Stetic.Wrapper.ActionGroup ();
			group.Name = name;
			ActionGroups.Add (group);
			this.modifiedTopLevels.Add (name);
			return group;
		}
		
		public Stetic.Wrapper.ActionGroup AddNewActionGroupFromTemplate (string template)
		{
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (template);
			ObjectReader or = new ObjectReader (this, FileFormat.Native);
			Stetic.Wrapper.ActionGroup group = new Stetic.Wrapper.ActionGroup ();
			group.Read (or, doc.DocumentElement);
			ActionGroups.Add (group);
			this.modifiedTopLevels.Add (group.Name);
			return group;
		}
		
		public void RemoveActionGroup (Stetic.Wrapper.ActionGroup group)
		{
			ActionGroups.Remove (group);
			string name = group.Name;
			if (modifiedTopLevels.Contains (name))
				modifiedTopLevels.Remove (name);
		}
		
		public Wrapper.ActionGroup[] GetActionGroups ()
		{
			// Needed since ActionGroupCollection can't be made serializable
			return ActionGroups.ToArray ();
		}
			
		void CleanUndoData (XmlElement elem)
		{
			elem.RemoveAttribute ("undoId");
			foreach (XmlNode cn in elem.ChildNodes) {
				XmlElement ce = cn as XmlElement;
				if (ce != null)
					CleanUndoData (ce);
			}
		}

		void OnRegistryChanging (object o, EventArgs args)
		{
			if (loading) return;
				
			// Store a copy of the current tree. The tree will
			// be recreated once the registry change is completed.
			
			tempDoc = Write (true);
			Selection = null;
		}
		
		void OnRegistryChanged (object o, EventArgs args)
		{
			if (loading) return;
			
			if (tempDoc != null)
				LoadStatus (tempDoc);
		}
		
		public object SaveStatus ()
		{
			return Write (true);
		}
		
		public void LoadStatus (object ob)
		{
			if (frontend != null)
				frontend.NotifyProjectReloading ();
			if (ProjectReloading != null)
				ProjectReloading (this, EventArgs.Empty);
			
			ProjectIconFactory icf = iconFactory;
			
			Read ((XmlDocument)ob);

			// Reuse the same icon factory, since a registry change has no effect to it
			// and it may be inherited from another project
			iconFactory = icf;
			
			if (frontend != null)
				frontend.NotifyProjectReloaded ();
			if (ProjectReloaded != null)
				ProjectReloaded (this, EventArgs.Empty);
			NotifyComponentTypesChanged ();
		}
		
		bool preserveWidgetLibraries;
		
		public void Reload ()
		{
			try {
				preserveWidgetLibraries = true;
				OnRegistryChanging (null, null);
				OnRegistryChanged (null, null);
			}
			finally
			{
				preserveWidgetLibraries = false;
			}
		}

		public string Id {
			get { return id; }
			set { id = value; }
		}
		
		public bool WasModified (string topLevel)
		{
			return modifiedTopLevels.Contains (topLevel);
		}
		
		public bool ComponentNeedsCodeGeneration (string topLevel)
		{
			return frontend.DesignInfo.ComponentNeedsCodeGeneration (topLevel);
		}
		
		public AssemblyResolver Resolver {
			get { return resolver; }
		}
		
		public string ImportFile (string filePath)
		{
			return frontend.ImportFile (filePath);
		}

		public void AddWindow (Gtk.Window window)
		{
			AddWindow (window, false);
		}

		public void AddWindow (Gtk.Window window, bool select)
		{
			AddWidget (window);
			if (select)
				Selection = window;
		}

		public void AddWidget (Gtk.Widget widget)
		{
			if (!typeof(Gtk.Container).IsInstanceOfType (widget))
				throw new System.ArgumentException ("widget", "Only containers can be top level widgets");
			topLevels.Add (new WidgetData (null, null, widget));
			
			if (!loading) {
				Stetic.Wrapper.Widget ww = Stetic.Wrapper.Widget.Lookup (widget);
				
				if (ww == null)
					throw new InvalidOperationException ("Widget not wrapped");
				if (frontend != null)
					frontend.NotifyWidgetAdded (Component.GetSafeReference (ww), widget.Name, ww.ClassDescriptor.Name);
				OnWidgetAdded (new Stetic.Wrapper.WidgetEventArgs (ww));
			}
		}
		
		void IProject.NotifyObjectChanged (ObjectWrapperEventArgs args)
		{
			if (loading)
				return;
			NotifyChanged (args.Wrapper.RootWrapperName);
			if (ObjectChanged != null)
				ObjectChanged (this, args);
		}
		
		void IProject.NotifyNameChanged (Stetic.Wrapper.WidgetNameChangedArgs args)
		{
			if (loading)
				return;
			NotifyChanged (args.WidgetWrapper.RootWrapperName);
			OnWidgetNameChanged (args, args.WidgetWrapper.IsTopLevel);
		}
		
		void IProject.NotifySignalAdded (SignalEventArgs args)
		{
			if (loading)
				return;
			if (frontend != null)
				frontend.NotifySignalAdded (Component.GetSafeReference (args.Wrapper), null, args.Signal);
			OnSignalAdded (args);
		}
		
		void IProject.NotifySignalRemoved (SignalEventArgs args)
		{
			if (loading)
				return;
			if (frontend != null)
				frontend.NotifySignalRemoved (Component.GetSafeReference (args.Wrapper), null, args.Signal);
			OnSignalRemoved (args);
		}
		
		void IProject.NotifySignalChanged (SignalChangedEventArgs args)
		{
			if (loading)
				return;
			OnSignalChanged (args);
		}
		
		void IProject.NotifyWidgetContentsChanged (Wrapper.Widget w)
		{
			if (loading)
				return;
			if (WidgetContentsChanged != null)
				WidgetContentsChanged (this, new Wrapper.WidgetEventArgs (w));
		}
		
		void OnWidgetNameChanged (Stetic.Wrapper.WidgetNameChangedArgs args, bool isTopLevel)
		{
			if (frontend != null)
				frontend.NotifyWidgetNameChanged (Component.GetSafeReference (args.WidgetWrapper), args.OldName, args.NewName, isTopLevel);
			if (args.WidgetWrapper != null && WidgetNameChanged != null)
				WidgetNameChanged (this, args);
			
			if (modifiedTopLevels.Contains (args.OldName))
				modifiedTopLevels.Remove (args.OldName);
			
			if (!modifiedTopLevels.Contains (args.NewName))
				modifiedTopLevels.Add (args.NewName);
		}
		
		void OnSignalAdded (object sender, SignalEventArgs args)
		{
			if (frontend != null)
				frontend.NotifySignalAdded (Component.GetSafeReference (args.Wrapper), null, args.Signal);
			OnSignalAdded (args);
		}

		protected virtual void OnSignalAdded (SignalEventArgs args)
		{
			if (SignalAdded != null)
				SignalAdded (this, args);
		}

		void OnSignalRemoved (object sender, SignalEventArgs args)
		{
			if (frontend != null)
				frontend.NotifySignalRemoved (Component.GetSafeReference (args.Wrapper), null, args.Signal);
			OnSignalRemoved (args);
		}

		protected virtual void OnSignalRemoved (SignalEventArgs args)
		{
			if (SignalRemoved != null)
				SignalRemoved (this, args);
		}

		void OnSignalChanged (object sender, SignalChangedEventArgs args)
		{
			OnSignalChanged (args);
		}

		protected virtual void OnSignalChanged (SignalChangedEventArgs args)
		{
			if (frontend != null)
				frontend.NotifySignalChanged (Component.GetSafeReference (args.Wrapper), null, args.OldSignal, args.Signal);
			if (SignalChanged != null)
				SignalChanged (this, args);
		}

		public Gtk.Widget[] Toplevels {
			get {
				List<Gtk.Widget> list = new List<Gtk.Widget> ();
				foreach (WidgetData w in topLevels)
					list.Add (GetWidget (w));
				return list.ToArray ();
			}
		}
		
		public Gtk.Widget GetWidget (string name)
		{
			WidgetData w = GetWidgetData (name);
			if (w != null)
				return GetWidget (w);
			else
				return null;
		}

		public WidgetData GetWidgetData (string name)
		{
			foreach (WidgetData w in topLevels) {
				if (w.Name == name)
					return w;
			}
			return null;
		}

		// IProject

		public Gtk.Widget Selection {
			get {
				return selection;
			}
			set {
				if (selection == value)
					return;

				Wrapper.ActionGroupCollection newCollection = null;
				
				// FIXME: should there be an IsDestroyed property?
				if (selection != null && selection.Handle != IntPtr.Zero) {
					Stetic.Wrapper.Container parent = Stetic.Wrapper.Container.LookupParent (selection);
					if (parent == null)
						parent = Stetic.Wrapper.Container.Lookup (selection);
					if (parent != null)
						parent.UnSelect (selection);
				}

				selection = value;

				if (selection != null && selection.Handle != IntPtr.Zero) {
					Stetic.Wrapper.Container parent = Stetic.Wrapper.Container.LookupParent (selection);
					if (parent == null)
						parent = Stetic.Wrapper.Container.Lookup (selection);
					if (parent != null)
						parent.Select (selection);
					Wrapper.Widget w = Wrapper.Widget.Lookup (selection);
					if (w != null)
						newCollection = w.LocalActionGroups;
				}

				if (SelectionChanged != null)
					SelectionChanged (this, new Wrapper.WidgetEventArgs (Wrapper.Widget.Lookup (selection)));
				
				if (oldTopActionCollection != newCollection) {
					if (oldTopActionCollection != null)
						oldTopActionCollection.ActionGroupChanged -= OnComponentTypesChanged;
					if (newCollection != null)
						newCollection.ActionGroupChanged += OnComponentTypesChanged;
					oldTopActionCollection = newCollection;
					OnComponentTypesChanged (null, null);
				}
			}
		}

		public void PopupContextMenu (Stetic.Wrapper.Widget wrapper)
		{
			Gtk.Menu m = new ContextMenu (wrapper);
			m.Popup ();
		}

		public void PopupContextMenu (Placeholder ph)
		{
			Gtk.Menu m = new ContextMenu (ph);
			m.Popup ();
		}
		
		internal static string AbsoluteToRelativePath (string baseDirectoryPath, string absPath)
		{
			if (!Path.IsPathRooted (absPath))
				return absPath;
			
			absPath = Path.GetFullPath (absPath);
			baseDirectoryPath = Path.GetFullPath (baseDirectoryPath);
			char[] separators = { Path.DirectorySeparatorChar, Path.VolumeSeparatorChar, Path.AltDirectorySeparatorChar };
			
			string[] bPath = baseDirectoryPath.Split (separators);
			string[] aPath = absPath.Split (separators);
			int indx = 0;
			for(; indx < Math.Min(bPath.Length, aPath.Length); ++indx) {
				if(!bPath[indx].Equals(aPath[indx]))
					break;
			}
			
			if (indx == 0) {
				return absPath;
			}
			
			string erg = "";
			
			if(indx == bPath.Length) {
				erg += "." + Path.DirectorySeparatorChar;
			} else {
				for (int i = indx; i < bPath.Length; ++i) {
					erg += ".." + Path.DirectorySeparatorChar;
				}
			}
			erg += String.Join(Path.DirectorySeparatorChar.ToString(), aPath, indx, aPath.Length-indx);
			
			return erg;
		}
		
		void OnComponentTypesChanged (object s, EventArgs a)
		{
			if (!loading)
				NotifyComponentTypesChanged ();
		}
		
		public void NotifyComponentTypesChanged ()
		{
			if (frontend != null)
				frontend.NotifyComponentTypesChanged ();
			if (ComponentTypesChanged != null)
				ComponentTypesChanged (this, EventArgs.Empty);
		}
		
		void OnGroupAdded (object s, Stetic.Wrapper.ActionGroupEventArgs args)
		{
			args.ActionGroup.SignalAdded += OnSignalAdded;
			args.ActionGroup.SignalRemoved += OnSignalRemoved;
			args.ActionGroup.SignalChanged += OnSignalChanged;
			if (frontend != null)
				frontend.NotifyActionGroupAdded (args.ActionGroup.Name);
			OnComponentTypesChanged (null, null);
		}
		
		void OnGroupRemoved (object s, Stetic.Wrapper.ActionGroupEventArgs args)
		{
			args.ActionGroup.SignalAdded -= OnSignalAdded;
			args.ActionGroup.SignalRemoved -= OnSignalRemoved;
			args.ActionGroup.SignalChanged -= OnSignalChanged;
			if (frontend != null)
				frontend.NotifyActionGroupRemoved (args.ActionGroup.Name);
			OnComponentTypesChanged (null, null);
		}
		
		void NotifyChanged (string rootWidgetName)
		{
			if (!modifiedTopLevels.Contains (rootWidgetName))
				modifiedTopLevels.Add (rootWidgetName);
			if (frontend != null)
				frontend.NotifyChanged (rootWidgetName);
			if (Changed != null)
				Changed (this, new ProjectChangedEventArgs (rootWidgetName));
		}
		
		protected virtual void OnWidgetAdded (Stetic.Wrapper.WidgetEventArgs args)
		{
			NotifyChanged (args.WidgetWrapper.RootWrapperName);
			if (WidgetAdded != null)
				WidgetAdded (this, args);
		}
	}
	
	class WidgetData
	{
		string name;
		
		public WidgetData (string name, XmlElement data, Gtk.Widget widget)
		{
			SetXmlData (name, data);
			Widget = widget;
		}
		
		public void SetXmlData (string name, XmlElement data)
		{
			this.name = name;
			XmlData = data;
			Widget = null;
		}
		
		public XmlElement XmlData;
		public Gtk.Widget Widget;
		
		public string Name {
			get {
				if (Widget != null)
					return Widget.Name;
				else
					return name;
			}
		}
	}
	
	public class ProjectChangedEventArgs : EventArgs
	{
		public string ChangedTopLevelName { get; private set; }
		
		public ProjectChangedEventArgs (string changedTopLevel)
		{
			ChangedTopLevelName = changedTopLevel;
		}
	}
	
	public delegate void ProjectChangedEventHandler (object sender, ProjectChangedEventArgs args);
}
