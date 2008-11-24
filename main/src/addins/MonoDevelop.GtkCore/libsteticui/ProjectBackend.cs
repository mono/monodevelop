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
		string fileName;
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
		public event EventHandler ModifiedChanged;
		public event EventHandler Changed;
		
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
			get { return fileName; }
			set {
				this.fileName = value;
				if (fileName != null)
					Id = System.IO.Path.GetFileName (fileName);
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
			get { return targetGtkVersion != null ? targetGtkVersion : "2.4"; }
			set {
				if (TargetGtkVersion == value)
					return;
				targetGtkVersion = value;
				
				// Update the project
				OnRegistryChanging (null, null);
				OnRegistryChanged (null, null);
			}
		}
		
		public string ImagesRootPath {
			get {
				if (string.IsNullOrEmpty (imagesRootPath)) {
					if (string.IsNullOrEmpty (fileName))
						return ".";
					else
						return Path.GetDirectoryName (fileName);
				}
				else {
					if (Path.IsPathRooted (imagesRootPath))
						return imagesRootPath;
					else if (!string.IsNullOrEmpty (fileName))
						return Path.GetFullPath (Path.Combine (Path.GetDirectoryName (fileName), imagesRootPath));
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
		
		internal void SetFileName (string fileName)
		{
			this.fileName = fileName;
		}
		
		internal void SetFrontend (Project project)
		{
			frontend = project;
		}
		
		public void Close ()
		{
			fileName = null;
			
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
			widgetLibraries.Clear ();

			iconFactory = new ProjectIconFactory ();
		}
		
		public void Load (string fileName)
		{
			Load (fileName, fileName);
		}
		
		public void Load (string xmlFile, string fileName)
		{
			this.fileName = fileName;
			XmlDocument doc = new XmlDocument ();
			doc.PreserveWhitespace = true;
			doc.Load (xmlFile);
			Read (doc);
			
			Id = System.IO.Path.GetFileName (fileName);
		}
		
		void Read (XmlDocument doc)
		{
			loading = true;
			string basePath = fileName != null ? Path.GetDirectoryName (fileName) : null;
			
			try {
				string fn = fileName;
				Close ();
				fileName = fn;
				
				XmlNode node = doc.SelectSingleNode ("/stetic-interface");
				if (node == null)
					throw new ApplicationException (Catalog.GetString ("Not a Stetic file according to node name."));
				
				// Load configuration options
				foreach (XmlNode configNode in node.SelectNodes ("configuration/*")) {
					XmlElement config = configNode as XmlElement;
					if (config == null) continue;
					
					if (config.LocalName == "images-root-path")
						imagesRootPath = config.InnerText;
					else if (config.LocalName == "target-gtk-version")
						targetGtkVersion = config.InnerText;
				}
				
				// Load the assembly directories
				resolver = new AssemblyResolver (app);
				foreach (XmlElement libElem in node.SelectNodes ("import/assembly-directory")) {
					string dir = libElem.GetAttribute ("path");
					if (dir.Length > 0) {
						if (basePath != null && !Path.IsPathRooted (dir)) {
							dir = Path.Combine (basePath, dir);
							if (Directory.Exists (dir))
								dir = Path.GetFullPath (dir);
						}
						resolver.Directories.Add (dir);
					}
				}
				
				// Import the referenced libraries
				foreach (XmlElement libElem in node.SelectNodes ("import/widget-library")) {
					string libname = libElem.GetAttribute ("name");
					if (libname.EndsWith (".dll") || libname.EndsWith (".exe")) {
						if (basePath != null && !Path.IsPathRooted (libname)) {
							libname = Path.Combine (basePath, libname);
							if (File.Exists (libname))
								libname = Path.GetFullPath (libname);
						}
					}
					widgetLibraries.Add (libname);
					if (libElem.GetAttribute ("internal") == "true")
						internalLibs.Add (libname);
				}
				
				app.LoadLibraries (resolver, widgetLibraries);
				
				ObjectReader reader = new ObjectReader (this, FileFormat.Native);
				
				if (ownedGlobalActionGroups) {
					foreach (XmlElement groupElem in node.SelectNodes ("action-group")) {
						Wrapper.ActionGroup actionGroup = new Wrapper.ActionGroup ();
						actionGroup.Read (reader, groupElem);
						actionGroups.Add (actionGroup);
					}
				}
				
				XmlElement iconsElem = node.SelectSingleNode ("icon-factory") as XmlElement;
				if (iconsElem != null)
					iconFactory.Read (this, iconsElem);
				
				foreach (XmlElement toplevel in node.SelectNodes ("widget")) {
					topLevels.Add (new WidgetData (toplevel.GetAttribute ("id"), toplevel, null));
				}
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
					Wrapper.Container wrapper = Stetic.ObjectWrapper.ReadObject (reader, data.XmlData) as Wrapper.Container;
					data.Widget = wrapper.Wrapped;
				} finally {
					loading = false;
				}
			}
			
			return data.Widget;
		}
		
		public void Save (string fileName)
		{
			this.fileName = fileName;
			XmlDocument doc = Write (false);
			
			XmlTextWriter writer = null;
			try {
				// Write to a temporary file first, just in case something fails
				writer = new XmlTextWriter (fileName + "~", System.Text.Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				doc.Save (writer);
				writer.Close ();
				
				File.Copy (fileName + "~", fileName, true);
				File.Delete (fileName + "~");
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

			XmlElement config = doc.CreateElement ("configuration");
			if (!string.IsNullOrEmpty (imagesRootPath)) {
				XmlElement iroot = doc.CreateElement ("images-root-path");
				iroot.InnerText = imagesRootPath;
				config.AppendChild (iroot);
			}
			if (!string.IsNullOrEmpty (targetGtkVersion)) {
				XmlElement iroot = doc.CreateElement ("target-gtk-version");
				iroot.InnerText = targetGtkVersion;
				config.AppendChild (iroot);
			}
			
			if (config.ChildNodes.Count > 0)
				toplevel.AppendChild (config);
			
			if (widgetLibraries.Count > 0 || (resolver != null && resolver.Directories.Count > 0)) {
				XmlElement importElem = doc.CreateElement ("import");
				toplevel.AppendChild (importElem);
				string basePath = Path.GetDirectoryName (fileName);
				
				if (resolver != null && resolver.Directories.Count > 0) {
					foreach (string dir in resolver.Directories) {
						XmlElement dirElem = doc.CreateElement ("assembly-directory");
						if (basePath != null)
							dirElem.SetAttribute ("path", AbsoluteToRelativePath (basePath, dir));
						else
							dirElem.SetAttribute ("path", dir);
						toplevel.AppendChild (dirElem);
					}
				}
				
				foreach (string wlib in widgetLibraries) {
					string libName = wlib;
					XmlElement libElem = doc.CreateElement ("widget-library");
					if (wlib.EndsWith (".dll") || wlib.EndsWith (".exe")) {
						if (basePath != null)
							libName = AbsoluteToRelativePath (basePath, wlib);
					}

					libElem.SetAttribute ("name", libName);
					if (IsInternalLibrary (wlib))
						libElem.SetAttribute ("internal", "true");
					importElem.AppendChild (libElem);
				}
			}

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
		
		internal WidgetEditSession CreateWidgetDesignerSession (WidgetDesignerFrontend frontend, string windowName, Stetic.ProjectBackend editingBackend, bool autoCommitChanges)
		{
			return new WidgetEditSession (this, frontend, windowName, editingBackend, autoCommitChanges);
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
			Gtk.Widget w = GetTopLevel (name);
			if (w != null) {
				Wrapper.Container ww = Wrapper.Container.Lookup (w);
				if (ww != null)
					return (Wrapper.Container) Component.GetSafeReference (ww);
			}
			if (throwIfNotFound)
				throw new InvalidOperationException ("Component not found: " + name);
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
			return Component.GetSafeReference (ObjectWrapper.Lookup (widget));
		}
		
		public void RemoveWidget (string name)
		{
			WidgetData data = GetWidgetData (name);
			if (data == null)
				return;
			
			if (frontend != null)
				frontend.NotifyWidgetRemoved (data.Name);
		
			topLevels.Remove (data);
			if (data.Widget != null)
				data.Widget.Destroy ();
		}
		
		public Stetic.Wrapper.ActionGroup AddNewActionGroup (string name)
		{
			Stetic.Wrapper.ActionGroup group = new Stetic.Wrapper.ActionGroup ();
			group.Name = name;
			ActionGroups.Add (group);
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
			return group;
		}
		
		public void RemoveActionGroup (Stetic.Wrapper.ActionGroup group)
		{
			ActionGroups.Remove (group);
		}
		
		public Wrapper.ActionGroup[] GetActionGroups ()
		{
			// Needed since ActionGroupCollection can't be made serializable
			return ActionGroups.ToArray ();
		}
				
		public void CopyWidgetToProject (string name, ProjectBackend other, string replacedName)
		{
			WidgetData wdata = GetWidgetData (name);
			if (name == null)
				throw new InvalidOperationException ("Component not found: " + name);
			
			XmlElement data;
			if (wdata.Widget != null)
				data = Stetic.WidgetUtils.ExportWidget (wdata.Widget);
			else
				data = (XmlElement) wdata.XmlData.Clone ();
			
			// If widget already exist, replace it
			wdata = other.GetWidgetData (replacedName);
			if (wdata == null) {
				wdata = new WidgetData (name, data, null);
				other.topLevels.Add (wdata);
			} else {
				if (wdata.Widget != null) {
					// If a widget instance already exist, load the new data on it
					Wrapper.Widget sw = Wrapper.Widget.Lookup (wdata.Widget);
					sw.Read (new ObjectReader (other, FileFormat.Native), data);
					sw.NotifyChanged ();
					if (name != replacedName)
						other.OnWidgetNameChanged (new Wrapper.WidgetNameChangedArgs (sw, replacedName, name), true);
				} else {
					wdata.SetXmlData (name, data);
					if (name != replacedName)
						other.OnWidgetNameChanged (new Wrapper.WidgetNameChangedArgs (null, replacedName, name), true);
				}
			}
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
			
			if (tempDoc != null) {
				if (frontend != null)
					frontend.NotifyProjectReloading ();
				if (ProjectReloading != null)
					ProjectReloading (this, EventArgs.Empty);
				
				ProjectIconFactory icf = iconFactory;
				
				Read (tempDoc);

				// Reuse the same icon factory, since a registry change has no effect to it
				// and it may be inherited from another project
				iconFactory = icf;
				
				tempDoc = null;
				if (frontend != null)
					frontend.NotifyProjectReloaded ();
				if (ProjectReloaded != null)
					ProjectReloaded (this, EventArgs.Empty);
				NotifyComponentTypesChanged ();
			}
		}
		
		public void Reload ()
		{
			OnRegistryChanging (null, null);
			OnRegistryChanged (null, null);
		}

		public string Id {
			get { return id; }
			set { id = value; }
		}
		
		public bool Modified {
			get { return modified; }
			set {
				if (modified != value) {
					modified = value;
					if (frontend != null)
						frontend.NotifyModifiedChanged ();
					OnModifiedChanged (EventArgs.Empty);
				}
			}
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
			NotifyChanged ();
			if (ObjectChanged != null)
				ObjectChanged (this, args);
		}
		
		void IProject.NotifyNameChanged (Stetic.Wrapper.WidgetNameChangedArgs args)
		{
			if (loading)
				return;
			NotifyChanged ();
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
		
		public Gtk.Widget GetTopLevel (string name)
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
		
		void NotifyChanged ()
		{
			Modified = true;
			if (frontend != null)
				frontend.NotifyChanged ();
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		protected virtual void OnModifiedChanged (EventArgs args)
		{
			if (ModifiedChanged != null)
				ModifiedChanged (this, args);
		}
		
		protected virtual void OnWidgetAdded (Stetic.Wrapper.WidgetEventArgs args)
		{
			NotifyChanged ();
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
}
