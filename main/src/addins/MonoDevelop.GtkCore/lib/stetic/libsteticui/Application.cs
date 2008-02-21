
using System;
using System.Threading;
using System.Collections;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Diagnostics;
using Mono.Remoting.Channels.Unix;
using Mono.Cecil;

namespace Stetic
{
	public enum IsolationMode
	{
		None,
		ProcessTcp,
		ProcessUnix
	}
	
	public delegate string AssemblyResolverCallback (string assemblyName);
	
	public class Application: MarshalByRefObject, IDisposable
	{
		ApplicationBackend backend;
		bool externalBackend;
		string channelId;
		ApplicationBackendController backendController;
		
		Hashtable components = new Hashtable ();
		Hashtable types = new Hashtable ();
		ArrayList widgetLibraries = new ArrayList ();
		ArrayList projects = new ArrayList ();
		Project activeProject;
		Designer activeDesigner;
		
		WidgetPropertyTree propertiesWidget;
		Palette paletteWidget;
		WidgetTree projectWidget;
		SignalsEditor signalsWidget;
		bool allowInProcLibraries = true;
		bool disposed;
		
		static Hashtable libraryCheckCache; 
		
		internal event BackendChangingHandler BackendChanging;
		internal event BackendChangedHandler BackendChanged;
		internal event EventHandler Disposing;
		
		public Application (IsolationMode mode)
		{
			if (mode == IsolationMode.None) {
				backend = new ApplicationBackend (this);
				externalBackend = false;
			} else {
				externalBackend = true;
				channelId = RegisterRemotingChannel (mode);
				backendController = new ApplicationBackendController (this, channelId);
				backendController.StartBackend ();
				OnBackendChanged (false);
			}
		}
		
		public AssemblyResolverCallback WidgetLibraryResolver {
			get { return Backend.WidgetLibraryResolver; }
			set { Backend.WidgetLibraryResolver = value; }
		}
		
		public bool ShowNonContainerWarning {
			get { return backend.ShowNonContainerWarning; }
			set { backend.ShowNonContainerWarning = value; }
		}
		
		// Loads the libraries registered in the projects or in the application.
		// It will reload the libraries if they have changed. Libraries won't be
		// unloaded unless forceUnload is set to true
		public void UpdateWidgetLibraries (bool forceUnload)
		{
			UpdateWidgetLibraries (true, forceUnload);
		}
		
		internal void UpdateWidgetLibraries (bool allowBackendRestart, bool forceUnload)
		{
			// Collect libraries from the project and from the application
			
			ArrayList assemblies = new ArrayList ();
			assemblies.AddRange (widgetLibraries);
			
			ArrayList projectBackends = new ArrayList ();
			foreach (Project p in projects)
				if (p.IsBackendLoaded)
					projectBackends.Add (p.ProjectBackend);
			
			if (!Backend.UpdateLibraries (assemblies, projectBackends, allowBackendRestart, forceUnload))
			{
				// The backend process needs to be restarted.
				// This is done in background.
				
				ThreadPool.QueueUserWorkItem (delegate {
					try {
						// Start the new backend
						ApplicationBackendController newController = new ApplicationBackendController (this, channelId);
						newController.StartBackend ();
						Gtk.Application.Invoke (newController, EventArgs.Empty, OnNewBackendStarted);
					} catch {
						// FIXME: show an error message
					}
				});
			}
		}
		
		void OnNewBackendStarted (object ob, EventArgs args)
		{
			// The new backend is running, just do the switch
			
			OnBackendChanging ();
			
			ApplicationBackendController oldBackend = backendController;
			backendController = (ApplicationBackendController) ob;
			
			OnBackendChanged (true);

			// The old backend can now be safely stopped
			oldBackend.StopBackend (false);
		}
		
		void OnBackendStopped (object ob, EventArgs args)
		{
			// The backend process crashed, try to restart it
			backend = null;
			backendController = new ApplicationBackendController (this, channelId);
			backendController.StartBackend ();
			OnBackendChanged (true);
		}
		
		void OnBackendChanged (bool notify)
		{
			ApplicationBackend oldBackend = backend;
			
			backend = backendController.Backend;
			backendController.Stopped += OnBackendStopped;
			UpdateWidgetLibraries (false, false);
			lock (types) {
				types.Clear ();
			}
			
			Component[] comps;
			lock (components) {
				// All components should have been cleared by the backend,
				// just make sure it did
				comps = new Component [components.Count];
				components.Values.CopyTo (comps, 0);
				components.Clear ();
			}
			
			foreach (Component c in comps) {
				c.Dispose ();
			}

			if (notify && BackendChanged != null)
				BackendChanged (oldBackend);

			backend.ActiveProject = activeProject != null ? activeProject.ProjectBackend : null;
		}
		
		void OnBackendChanging ()
		{
			backend.GlobalWidgetLibraries = widgetLibraries;
			if (BackendChanging != null)
				BackendChanging ();
		}
		
		internal string RegisterRemotingChannel (IsolationMode mode)
		{
			string remotingChannel;
			if (mode == IsolationMode.ProcessTcp) {
				remotingChannel = "tcp";
				IChannel ch = ChannelServices.GetChannel ("tcp");
				if (ch == null) {
					ChannelServices.RegisterChannel (new TcpChannel (0));
				}
			} else {
				remotingChannel = "unix";
				IChannel ch = ChannelServices.GetChannel ("unix");
				if (ch == null) {
					string unixRemotingFile = Path.GetTempFileName ();
					ChannelServices.RegisterChannel (new UnixChannel (unixRemotingFile));
				}
			}
			return remotingChannel;
		}
		
		public virtual void Dispose ()
		{
			if (disposed)
				return;
				
			disposed = true;
			if (Disposing != null)
				Disposing (this, EventArgs.Empty);
			if (externalBackend) {
				backendController.StopBackend (true);
			} else {
				backend.Dispose ();
			}
			System.Runtime.Remoting.RemotingServices.Disconnect (this);
		}
		
		internal bool Disposed {
			get { return disposed; }
		}

		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
		
		internal ApplicationBackend Backend {
			get {
				if (disposed)
					throw new InvalidOperationException ("Application has been disposed");
				return backend;
			}
		}
		
		public Project LoadProject (string path)
		{
			Project p = new Project (this);
			p.Load (path);
			projects.Add (p);
			return p;
		}
		
		public Project CreateProject ()
		{
			Project p = new Project (this);
			projects.Add (p);
			return p;
		}
		
		public ComponentType[] GetComponentTypes (string fileName)
		{
			if (IsWidgetLibrary (fileName))
				return CecilWidgetLibrary.GetComponentTypes (this, fileName).ToArray ();
			else
				return new ComponentType [0];
		}
		
		public void AddWidgetLibrary (string assemblyPath)
		{
			if (!widgetLibraries.Contains (assemblyPath)) {
				widgetLibraries.Add (assemblyPath);
				Backend.GlobalWidgetLibraries = widgetLibraries; 
				UpdateWidgetLibraries (false, false);
			}
		}
		
		public void RemoveWidgetLibrary (string assemblyPath)
		{
			widgetLibraries.Remove (assemblyPath);
			Backend.GlobalWidgetLibraries = widgetLibraries; 
			UpdateWidgetLibraries (false, false);
		}
		
		public string[] GetWidgetLibraries ()
		{
			return (string[]) widgetLibraries.ToArray (typeof(string));
		}
		
		public bool IsWidgetLibrary (string assemblyRef)
		{
			ImportContext ic = new ImportContext ();
			ic.App = this.Backend;
			return Application.InternalIsWidgetLibrary (ic, assemblyRef);
		}
		
		internal static bool InternalIsWidgetLibrary (ImportContext ic, string assemblyRef)
		{
			string path;

			if (assemblyRef.EndsWith (".dll") || assemblyRef.EndsWith (".exe")) {
				if (!File.Exists (assemblyRef))
					return false;
				path = assemblyRef;
			}
			else {
				path = CecilWidgetLibrary.FindAssembly (ic, assemblyRef, null);
				if (path == null)
					return false;
			}
			
			LibraryData data = GetLibraryCacheData (path);
			if (data == null) {
				// There is no info about this library, it has to be checked
				bool isLib = CecilWidgetLibrary.IsWidgetLibrary (path);
				SetLibraryCacheData (path, isLib);
				return isLib;
			} else
				return data.IsLibrary;
		}
		
		internal void DisposeProject (Project p)
		{
			projects.Remove (p);
		}
		
		public CodeGenerationResult GenerateProjectCode (string file, string namespaceName, CodeDomProvider provider, GenerationOptions options, params Project[] projects)
		{
			ArrayList files = new ArrayList ();
			CodeGenerationResult res = GenerateProjectCode (options, projects);
			
			ICodeGenerator gen = provider.CreateGenerator ();
			string basePath = Path.GetDirectoryName (file);
			string ext = Path.GetExtension (file);
			
			foreach (SteticCompilationUnit unit in res.Units) {
				string fname;
				if (unit.Name.Length == 0)
					fname = file;
				else
					fname = Path.Combine (basePath, unit.Name) + ext;
				files.Add (fname);
				unit.Name = fname;
				StreamWriter fileStream = new StreamWriter (fname);
				try {
					gen.GenerateCodeFromCompileUnit (unit, fileStream, new CodeGeneratorOptions ());
				} finally {
					fileStream.Close ();
				}
			}
			return res;
		}
		
		public CodeGenerationResult GenerateProjectCode (GenerationOptions options, params Project[] projects)
		{
			ProjectBackend[] pbs = new ProjectBackend [projects.Length];
			for (int n=0; n<projects.Length; n++)
				pbs [n] = projects [n].ProjectBackend;
				
			return Backend.GenerateProjectCode (options, pbs);
		}
		
		public Designer ActiveDesigner {
			get {
				return activeDesigner; 
			}
			set {
				if (activeDesigner != value) {
					activeDesigner = value;
					if (activeDesigner != null)
						activeDesigner.SetActive ();
					else
						ActiveProject = null;
				}
			}
		}
		
		internal bool UseExternalBackend {
			get { return externalBackend; }
		}
			
		internal Project ActiveProject {
			get { return activeProject; }
			set { 
				activeProject = value;
				Backend.ActiveProject = value != null ? value.ProjectBackend : null;
				Backend.SetActiveDesignSession (null);
			}
		}
		
		internal void SetActiveDesignSession (Project p, WidgetEditSession session)
		{
			activeProject = p;
			Backend.ActiveProject = p != null ? p.ProjectBackend : null;
			Backend.SetActiveDesignSession (session);
		}
		
		public WidgetPropertyTree PropertiesWidget {
			get {
				if (propertiesWidget == null)
					propertiesWidget = new WidgetPropertyTree (this);
				return propertiesWidget;
			}
		}
		
		public Palette PaletteWidget {
			get {
				if (paletteWidget == null)
					paletteWidget = new Palette (this);
				return paletteWidget;
			}
		}
		
		public WidgetTree WidgetTreeWidget {
			get {
				if (projectWidget == null)
					projectWidget = new WidgetTree (this);
				return projectWidget;
			}
		}
		
		public SignalsEditor SignalsWidget {
			get {
				if (signalsWidget == null)
					signalsWidget = new SignalsEditor (this);
				return signalsWidget;
			}
		}
		
		public bool AllowInProcLibraries {
			get { return allowInProcLibraries; }
			set {
				allowInProcLibraries = value;
				if (backend != null)
					backend.AllowInProcLibraries = value;
			}
		}
		
		internal void NotifyLibraryUnloaded (string name)
		{
			// Remove cached types from the unloaded library
			ArrayList toDelete = new ArrayList ();
			lock (types) {
				foreach (ComponentType ct in types.Values) {
					if (ct.Library == name)
						toDelete.Add (ct.Name);
				}
				foreach (string s in toDelete)
					types.Remove (s);
			}
		}
		
		internal ComponentType GetComponentType (string typeName)
		{
			lock (types) {
				ComponentType t = (ComponentType) types [typeName];
				if (t != null) return t;
				
				if (typeName == "Gtk.Action" || typeName == "Gtk.ActionGroup") {
					t = new ComponentType (this, typeName, "", typeName, "Actions", "2.4", null, ComponentType.Unknown.Icon);
					types [typeName] = t;
					return t;
				}
				
				string desc = null, className = null, category = null, targetGtkVersion = null, library = null;
				Gdk.Pixbuf px = null;
				
				if (externalBackend) {
					byte[] icon;
					
					if (Backend.GetClassDescriptorInfo (typeName, out desc, out className, out category, out targetGtkVersion, out library, out icon)) {
						if (icon != null)
							px = new Gdk.Pixbuf (icon);
					}
					
					if (px == null) {
						px = ComponentType.Unknown.Icon;
					}
					
					if (desc == null)
						desc = typeName;
				} else {
					ClassDescriptor cls = Registry.LookupClassByName (typeName);
					if (cls != null) {
						desc = cls.Label;
						className = cls.WrappedTypeName;
						category = cls.Category;
						targetGtkVersion = cls.TargetGtkVersion;
						px = cls.Icon;
						library = cls.Library.Name;
					}
				}
				
				t = new ComponentType (this, typeName, desc, className, category, targetGtkVersion, library, px);
				types [typeName] = t;
				return t;
			}
		}
		
		internal Component GetComponent (object cbackend, string name, string type)
		{
			try {
				lock (components) {
					Component c = (Component) components [cbackend];
					if (c != null)
						return c;

					// If the remote object is already disposed, don't try to create a
					// local component.
					if (cbackend is ObjectWrapper && ((ObjectWrapper)cbackend).IsDisposed)
						return null;
					
					if (cbackend is Wrapper.Action) {
						c = new ActionComponent (this, cbackend, name);
						((ObjectWrapper)cbackend).Frontend = c;
					} else if (cbackend is Wrapper.ActionGroup) {
						c = new ActionGroupComponent (this, cbackend, name);
						((Wrapper.ActionGroup)cbackend).Frontend = c;
					} else if (cbackend is ObjectWrapper) {
						c = new WidgetComponent (this, cbackend, name, type != null ? GetComponentType (type) : null);
						((ObjectWrapper)cbackend).Frontend = c;
					} else if (cbackend == null)
						throw new System.ArgumentNullException ("cbackend");
					else
						throw new System.InvalidOperationException ("Invalid component type: " + cbackend.GetType ());

					components [cbackend] = c;
					return c;
				}
			}
			catch (System.Runtime.Remoting.RemotingException)
			{
				// There may be a remoting exception if the remote wrapper
				// has already been disconnected when trying to create the
				// component frontend. This may happen since calls are
				// dispatched in the GUI thread, so they may be delayed.
				return null;
			}
		}
		
		internal void DisposeComponent (Component c)
		{
			lock (components) {
				components.Remove (c.Backend);
			}
		}
		
		static LibraryData GetLibraryCacheData (string path)
		{
			if (libraryCheckCache == null)
				LoadLibraryCheckCache ();
			LibraryData data = (LibraryData) libraryCheckCache [path];
			if (data == null)
				return null;

			DateTime lastWrite = File.GetLastWriteTime (path);
			if (data.LastCheck == lastWrite)
				return data;
			else
				// Data not valid anymore
				return null;
		}
		
		static void SetLibraryCacheData (string path, bool isLibrary)
		{
			if (libraryCheckCache == null)
				LoadLibraryCheckCache ();

			LibraryData data = (LibraryData) libraryCheckCache [path];
			if (data == null) {
				data = new LibraryData ();
				libraryCheckCache [path] = data;
			}
			data.IsLibrary = isLibrary;
			data.LastCheck = File.GetLastWriteTime (path);
			SaveLibraryCheckCache ();
		}
		
		static void LoadLibraryCheckCache ()
		{
			bool needsSave = false;;
			libraryCheckCache = new Hashtable ();
			string cacheFile = Path.Combine (ConfigDir, "assembly-check-cache");
			if (!File.Exists (cacheFile))
				return;
			
			try {
				XmlDocument doc = new XmlDocument ();
				doc.Load (cacheFile);
				foreach (XmlElement elem in doc.SelectNodes ("assembly-check-cache/assembly")) {
					string file = elem.GetAttribute ("path");
					if (File.Exists (file)) {
						LibraryData data = new LibraryData ();
						if (elem.GetAttribute ("isLibrary") == "yes")
							data.IsLibrary = true;
						data.LastCheck = XmlConvert.ToDateTime (elem.GetAttribute ("timestamp"), XmlDateTimeSerializationMode.Local);
					} else
						needsSave = true;
				}
			} catch {
				// If there is an error, just ignore the cached data
				needsSave = true;
			}
			
			if (needsSave)
				SaveLibraryCheckCache ();
		}
		
		static void SaveLibraryCheckCache ()
		{
			if (libraryCheckCache == null)
				return;
			
			try {
				if (!Directory.Exists (ConfigDir))
					Directory.CreateDirectory (ConfigDir);
					
				XmlDocument doc = new XmlDocument ();
				XmlElement delem = doc.CreateElement ("assembly-check-cache");
				doc.AppendChild (delem);
					
				foreach (DictionaryEntry e in libraryCheckCache) {
					LibraryData data = (LibraryData) e.Value;
					XmlElement elem = doc.CreateElement ("assembly");
					elem.SetAttribute ("path", (string) e.Key);
					if (data.IsLibrary)
						elem.SetAttribute ("isLibrary", "yes");
					elem.SetAttribute ("timestamp", XmlConvert.ToString (data.LastCheck, XmlDateTimeSerializationMode.Local));
					delem.AppendChild (elem);
				}
				
				doc.Save (Path.Combine (ConfigDir, "assembly-check-cache"));
			}
			catch {
				// If something goes wrong, just ignore the cached info
			}
		}
		
		static string ConfigDir {
			get { 
				string file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".config");
				return Path.Combine (file, "stetic");
			}
		}
	}

	internal delegate void BackendChangingHandler ();
	internal delegate void BackendChangedHandler (ApplicationBackend oldBackend);
	
	class LibraryData
	{
		public DateTime LastCheck;
		public bool IsLibrary;
	}
}
