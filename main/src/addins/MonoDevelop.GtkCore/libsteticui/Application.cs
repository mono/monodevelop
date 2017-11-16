
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
	
	public static class ApplicationFactory {

		public static Application CreateApplication (IsolationMode mode)
		{
			switch (mode) {
			case IsolationMode.None:
				return new LocalApplication ();
			case IsolationMode.ProcessTcp:
			case IsolationMode.ProcessUnix:
				return new IsolatedApplication (mode);
			default:
				throw new ArgumentException ("mode");
			}
		}
	}

	internal class IsolatedApplication : Application {

		string channelId;
		ApplicationBackendController backendController;

		string RegisterRemotingChannel (IsolationMode mode)
		{
			string remotingChannel;
			if (mode == IsolationMode.ProcessTcp) {
				remotingChannel = "tcp";
				IChannel ch = ChannelServices.GetChannel ("tcp");
				if (ch == null) {
					ChannelServices.RegisterChannel (new TcpChannel (0), false);
				}
			} else {
				remotingChannel = "unix";
				IChannel ch = ChannelServices.GetChannel ("unix");
				if (ch == null) {
					string unixRemotingFile = Path.GetTempFileName ();
					ChannelServices.RegisterChannel (new UnixChannel (unixRemotingFile), false);
				}
			}
			return remotingChannel;
		}

		public IsolatedApplication (IsolationMode mode)
		{
			if (mode == IsolationMode.None)
				throw new ArgumentException ("mode");
			channelId = RegisterRemotingChannel (mode);
			backendController = new ApplicationBackendController (this, channelId);
			backendController.StartBackend ();
			OnBackendChanged (false);
		}

		public override void Dispose ()
		{
			base.Dispose ();
			backendController.StopBackend (true);
		}

		public event BackendChangingHandler BackendChanging;
		public event BackendChangedHandler BackendChanged;
		
		void OnNewBackendStarted (object ob, EventArgs args)
		{
			OnBackendChanging ();
			ApplicationBackendController oldBackend = backendController;
			backendController = (ApplicationBackendController) ob;
			OnBackendChanged (true);
			oldBackend.StopBackend (false);
		}
		
		void OnBackendStopped (object ob, EventArgs args)
		{
			// The backend process crashed, try to restart it
			Backend = null;
			backendController = new ApplicationBackendController (this, channelId);
			backendController.StartBackend ();
			OnBackendChanged (true);
		}
		
		void OnBackendChanged (bool notify)
		{
			ApplicationBackend oldBackend = Backend;
			
			Backend = backendController.Backend;
			backendController.Stopped += OnBackendStopped;
			UpdateWidgetLibraries (false, false);
			ClearCollections ();
			if (notify && BackendChanged != null)
				BackendChanged (oldBackend);

			Backend.ActiveProject = ActiveProject != null ? ActiveProject.ProjectBackend : null;
		}
		
		void OnBackendChanging ()
		{
			Backend.GlobalWidgetLibraries = widgetLibraries;
			if (BackendChanging != null)
				BackendChanging ();
		}

		internal override void RestartBackend ()
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

		internal override ComponentType CreateComponentType (string typeName)
		{
			string desc = null, className = null, category = null, targetGtkVersion = null, library = null;
			Gdk.Pixbuf px = null;
				
			byte[] icon;
					
			if (Backend.GetClassDescriptorInfo (typeName, out desc, out className, out category, out targetGtkVersion, out library, out icon) && icon != null)
				px = new Gdk.Pixbuf (icon);
					
			if (px == null)
				px = ComponentType.Unknown.Icon;
					
			if (desc == null)
				desc = typeName;

			return new ComponentType (this, typeName, desc, className, category, targetGtkVersion, library, px);
		}
	}

	internal class LocalApplication : Application {

		public LocalApplication ()
		{
			Backend = new ApplicationBackend (this);
		}

		public override void Dispose ()
		{
			base.Dispose ();
			Backend.Dispose ();
		}

		internal override ComponentType CreateComponentType (string typeName)
		{
			ClassDescriptor cls = Registry.LookupClassByName (typeName);
			if (cls == null)
				return null;
				
			return new ComponentType (this, typeName, cls.Label, cls.WrappedTypeName, cls.Category, cls.TargetGtkVersion, cls.Library.Name, cls.Icon);
		}
	}

	public abstract class Application : MarshalByRefObject, IDisposable {

		ApplicationBackend backend;

		Hashtable components = new Hashtable ();
		Hashtable types = new Hashtable ();
		internal ArrayList widgetLibraries = new ArrayList ();
		ArrayList projects = new ArrayList ();
		Project activeProject;
		Designer activeDesigner;
		
		WidgetPropertyTree propertiesWidget;
		Palette paletteWidget;
		WidgetTree projectWidget;
		SignalsEditor signalsWidget;
		bool allowInProcLibraries = true;
		bool disposed;
		
		public AssemblyResolverCallback WidgetLibraryResolver {
			get { return Backend.WidgetLibraryResolver; }
			set { Backend.WidgetLibraryResolver = value; }
		}
		
		public MimeResolverDelegate MimeResolver {
			set { Backend.MimeResolver = value; }
		}
		
		public Editor.ShowUrlDelegate ShowUrl {
			set { Backend.ShowUrl = value; }
		}
		
		public bool ShowNonContainerWarning {
			get { return Backend.ShowNonContainerWarning; }
			set { Backend.ShowNonContainerWarning = value; }
		}
		
		// Loads the libraries registered in the projects or in the application.
		// It will reload the libraries if they have changed. Libraries won't be
		// unloaded unless forceUnload is set to true
		public void UpdateWidgetLibraries (bool forceUnload)
		{
			UpdateWidgetLibraries (true, forceUnload);
		}
		
		internal virtual void RestartBackend ()
		{
		}

		protected void ClearCollections ()
		{
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
				RestartBackend ();
		}
		
		public virtual void Dispose ()
		{
			if (disposed)
				return;
				
			disposed = true;
			ClearCollections ();
			ArrayList copy = (ArrayList) projects.Clone ();
			foreach (Project p in copy)
				p.Dispose ();
			if (propertiesWidget != null)
				 propertiesWidget.Destroy ();
			if (paletteWidget != null)
				 paletteWidget.Destroy ();
			if (projectWidget != null)
				 projectWidget.Destroy ();
			if (signalsWidget != null)
				 signalsWidget.Destroy ();
			widgetLibraries.Clear ();
			System.Runtime.Remoting.RemotingServices.Disconnect (this);
		}
		
		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
		
		internal ApplicationBackend Backend { 
			get { return backend; }
			set { backend = value; }
		}
		
		void ProjectDisposed (object sender, EventArgs args)
		{
			projects.Remove (sender as Project);
			if (!disposed)
				UpdateWidgetLibraries (false, false);
		}
		
		public Project LoadProject (string path)
		{
			Project p = new Project (this);
			p.Load (path);
			projects.Add (p);
			p.Disposed += ProjectDisposed;
			return p;
		}
		
		public Project CreateProject ()
		{
			Project p = new Project (this);
			projects.Add (p);
			p.Disposed += ProjectDisposed;
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
			AssemblyResolver resolver = new AssemblyResolver (Backend);
			return InternalIsWidgetLibrary (resolver, assemblyRef);
		}
		
		internal static bool InternalIsWidgetLibrary (AssemblyResolver resolver, string assemblyRef)
		{
			string path;

			if (assemblyRef.EndsWith (".dll") || assemblyRef.EndsWith (".exe")) {
				if (!File.Exists (assemblyRef))
					return false;
				path = assemblyRef;
			}
			else {
				path = resolver.Resolve (assemblyRef, null);
				if (path == null)
					return false;
			}

			return CecilWidgetLibrary.IsWidgetLibrary (path);
		}
		
		public CodeGenerationResult GenerateProjectCode (string file, string namespaceName, CodeDomProvider provider, GenerationOptions options, params Project[] projects)
		{
			ArrayList files = new ArrayList ();
			CodeGenerationResult res = GenerateProjectCode (options, projects);
			
			string basePath = Path.GetDirectoryName (file);
			string ext = Path.GetExtension (file);
			
			foreach (SteticCompilationUnit unit in res.Units) {
				string fname;
				if (unit.Name.Length == 0) {
					if (provider is Microsoft.CSharp.CSharpCodeProvider && unit.Namespaces.Count > 0) {
						var types = unit.Namespaces [0].Types;
						if (types.Count > 0) {
							types [0].Members.Insert (0, new CodeSnippetTypeMember ("#pragma warning disable 436"));
						}
					}
					fname = file;
				} else
					fname = Path.Combine (basePath, unit.Name) + ext;
				files.Add (fname);
				unit.Name = fname;
				StreamWriter fileStream = new StreamWriter (fname);
				try {
					provider.GenerateCodeFromCompileUnit (unit, fileStream, new CodeGeneratorOptions ());
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

		public CodeGenerationResult GenerateWidgetCode (GenerationOptions options, Component component)
		{
			var b = (ObjectWrapper)component.Backend;
			return Backend.GenerateWidgetCode (options, b);
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
				if (propertiesWidget == null) {
					propertiesWidget = new WidgetPropertyTree (this);
					propertiesWidget.Destroyed += delegate { propertiesWidget = null; };
				}
				return propertiesWidget;
			}
		}
		
		public Palette PaletteWidget {
			get {
				if (paletteWidget == null) {
					paletteWidget = new Palette (this);
					paletteWidget.Destroyed += delegate { paletteWidget = null; };
				}
				return paletteWidget;
			}
		}
		
		public WidgetTree WidgetTreeWidget {
			get {
				if (projectWidget == null) {
					projectWidget = new WidgetTree (this);
					projectWidget.Destroyed += delegate { projectWidget = null; };
				}
				return projectWidget;
			}
		}
		
		public SignalsEditor SignalsWidget {
			get {
				if (signalsWidget == null) {
					signalsWidget = new SignalsEditor (this);
					signalsWidget.Destroyed += delegate { signalsWidget = null; };
				}
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
		
		internal abstract ComponentType CreateComponentType (string typeName);

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
				
				t = CreateComponentType (typeName);
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
	}

	internal delegate void BackendChangingHandler ();
	internal delegate void BackendChangedHandler (ApplicationBackend oldBackend);
}
