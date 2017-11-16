
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using Mono.Remoting.Channels.Unix;

namespace Stetic
{
	internal class ApplicationBackend: MarshalByRefObject, IDisposable, IObjectViewer
	{
		PaletteBackend paletteWidget;
		WidgetPropertyTreeBackend propertiesWidget;
		SignalsEditorEditSession signalsWidget;
		ProjectViewBackend projectWidget;
		ArrayList globalWidgetLibraries = new ArrayList ();
		AssemblyResolverCallback widgetLibraryResolver;
		object targetViewerObject;
		bool allowInProcLibraries = true;
		Application appFrontend;
		string coreLibrary;
		
		ProjectBackend activeProject;
		WidgetEditSession activeDesignSession;
		static ApplicationBackendController controller;
		
		public ApplicationBackend (Application app)
		{
			appFrontend = app;
			coreLibrary = Path.GetFullPath (typeof(Registry).Assembly.Location);
			Registry.Initialize (new AssemblyWidgetLibrary (coreLibrary, typeof(Registry).Assembly));
			WidgetDesignerBackend.DefaultObjectViewer = this;
		}
		
		public static void Main ()
		{
			Gdk.Threads.Init ();
			Gtk.Application.Init ();
			
			System.Threading.Thread.CurrentThread.Name = "gui-thread";
			
			// Read the remoting channel to use
			
			string channel = Console.In.ReadLine ();
			
			IServerChannelSinkProvider formatterSink = new BinaryServerFormatterSinkProvider ();
			formatterSink.Next = new GuiDispatchServerSinkProvider ();
				
			string unixPath = null;
			if (channel == "unix") {
				unixPath = System.IO.Path.GetTempFileName ();
				Hashtable props = new Hashtable ();
				props ["path"] = unixPath;
				props ["name"] = "__internal_unix";
				ChannelServices.RegisterChannel (new UnixChannel (props, null, formatterSink), false);
			} else {
				Hashtable props = new Hashtable ();
				props ["port"] = 0;
				props ["name"] = "__internal_tcp";
				ChannelServices.RegisterChannel (new TcpChannel (props, null, formatterSink), false);
			}
			
			// Read the reference to the application
			
			string sref = Console.In.ReadLine ();
			byte[] data = Convert.FromBase64String (sref);
			MemoryStream ms = new MemoryStream (data);
			BinaryFormatter bf = new BinaryFormatter ();
			
			controller = (ApplicationBackendController) bf.Deserialize (ms);
			ApplicationBackend backend = new ApplicationBackend (controller.Application);
			
			controller.Connect (backend);
			
			Gdk.Threads.Enter ();
			Gtk.Application.Run ();
			Gdk.Threads.Leave ();
			
			controller.Disconnect (backend);
		}
		
		public virtual void Dispose ()
		{
			if (controller != null) {
				Gtk.Application.Quit ();
			}
			System.Runtime.Remoting.RemotingServices.Disconnect (this);
		}

		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
		
		public AssemblyResolverCallback WidgetLibraryResolver {
			get { return widgetLibraryResolver; }
			set { widgetLibraryResolver = value; }
		}
		
		public MimeResolverDelegate MimeResolver {
			set { ResourceInfo.MimeResolver = value; }
		}

		public Editor.ShowUrlDelegate ShowUrl {
			set { Editor.NonContainerWarningDialog.ShowUrl = value; }
		}

		public bool ShowNonContainerWarning {
			get { return Wrapper.Container.ShowNonContainerWarning; }
			set { Wrapper.Container.ShowNonContainerWarning = value; }
		}
		
		public ProjectBackend LoadProject (string path)
		{
			ProjectBackend p = new ProjectBackend (this);
			
			if (System.IO.Path.GetExtension (path) == ".glade") {
				GladeFiles.Import (p, path);
			} else {
				p.Load (path);
			}
			return p;
		}
		
		public CodeGenerationResult GenerateProjectCode (GenerationOptions options, ProjectBackend[] projects)
		{
			return CodeGenerator.GenerateProjectCode (options, projects);
		}

		public CodeGenerationResult GenerateWidgetCode (GenerationOptions options, ObjectWrapper wrapper)
		{
			return CodeGenerator.GenerateWidgetCode (options, wrapper);
		}
		
		public ArrayList GlobalWidgetLibraries {
			get { return globalWidgetLibraries; }
			set { globalWidgetLibraries = value; }
		}
		
		public bool AllowInProcLibraries {
			get { return allowInProcLibraries; }
			set { allowInProcLibraries = value; }
		}
		
		public bool UpdateLibraries (ArrayList libraries, ArrayList projects, bool allowBackendRestart, bool forceUnload)
		{
			try {
				Registry.BeginChangeSet ();
				
				libraries.Add (Registry.CoreWidgetLibrary.Name);
				
				// Notify libraries that need to be unloaded and loaded again
				
				foreach (WidgetLibrary lib in Registry.RegisteredWidgetLibraries) {
					if (lib.NeedsReload)
						appFrontend.NotifyLibraryUnloaded (lib.Name);
				}
				
				if (!Registry.ReloadWidgetLibraries () && allowBackendRestart)
					return false;
				
				// Store a list of registered libraries, used later to know which
				// ones need to be unloaded
				
				ArrayList loaded = new ArrayList ();
				foreach (WidgetLibrary alib in Registry.RegisteredWidgetLibraries)
					loaded.Add (alib.Name);
				
				// Load required libraries
				
				Hashtable visited = new Hashtable ();
				LoadLibraries (new AssemblyResolver (this), visited, libraries);
				
				foreach (ProjectBackend project in projects)
					LoadLibraries (project.Resolver, visited, project.WidgetLibraries);
				
				// Unload libraries which are not required
				
				foreach (string name in loaded) {
					if (!visited.Contains (name)) {
						if (forceUnload && allowBackendRestart)
							return false;
						appFrontend.NotifyLibraryUnloaded (name);
						Registry.UnregisterWidgetLibrary (Registry.GetWidgetLibrary (name));
					}
				}
				
				return true;
			}
			finally {
				Registry.EndChangeSet ();
			}
		}
		
		internal void LoadLibraries (AssemblyResolver resolver, IEnumerable libraries)
		{
			try {
				Registry.BeginChangeSet ();
				Hashtable visited = new Hashtable ();
				LoadLibraries (resolver, visited, libraries);
			} finally {
				Registry.EndChangeSet ();
			}
		}
		
		void LoadLibraries (AssemblyResolver resolver, Hashtable visited, IEnumerable libraries)
		{
			// Convert all assembly names to assembly paths before registering the libraries.
			// The registry and the library cache will only handle full paths.
			foreach (string s in libraries) {
				string sr = resolver.Resolve (s, null);
				if (sr != null)
					AddLibrary (resolver, visited, sr);
			}
			Registry.ReloadWidgetLibraries ();
		}
		
		WidgetLibrary AddLibrary (AssemblyResolver resolver, Hashtable visited, string s)
		{
			if (Registry.IsRegistered (s)) {
				WidgetLibrary lib = Registry.GetWidgetLibrary (s);
				CheckDependencies (resolver, visited, lib);
				return lib;
			}
			
			// Avoid registering direct references of libstetic
			if (Path.GetFileName (s) == "libstetic.dll" && s != coreLibrary)
				return null;

			WidgetLibrary alib = CreateLibrary (resolver, s);
			if (alib == null)
				return null;
				
			RegisterLibrary (resolver, visited, alib);
			return alib;
		}
		
		void RegisterLibrary (AssemblyResolver resolver, Hashtable visited, WidgetLibrary lib)
		{
			if (lib == null || visited.Contains (lib.Name))
				return;
				
			visited [lib.Name] = lib;

			foreach (string s in lib.GetLibraryDependencies ()) {
				if (!Application.InternalIsWidgetLibrary (resolver, s))
					continue;
				AddLibrary (resolver, visited, s);
			}
			
			try {
				Registry.RegisterWidgetLibrary (lib);
			} catch (Exception ex) {
				// Catch errors when loading a library to avoid aborting
				// the whole update method. After all, that's not a fatal
				// error (some widgets just won't be shown).
				// FIXME: return the error somewhere
				Console.WriteLine (ex);
			}
		}
		
		WidgetLibrary CreateLibrary (AssemblyResolver resolver, string name)
		{
			try {
				if (allowInProcLibraries)
					return new AssemblyWidgetLibrary (resolver, name);
				else
					return new CecilWidgetLibrary (resolver, name);
			} catch (Exception) {
				// FIXME: handle the error, but keep loading.
				return null;
			}
		}
		
		void CheckDependencies (AssemblyResolver resolver, Hashtable visited, WidgetLibrary lib)
		{
			if (visited.Contains (lib.Name) || lib.Name == coreLibrary)
				return;
				
			visited [lib.Name] = lib;
			
			foreach (string dep in lib.GetLibraryDependencies ()) {
				WidgetLibrary depLib = Registry.GetWidgetLibrary (dep);
				if (depLib == null)
					AddLibrary (resolver, visited, dep);
				else
					CheckDependencies (resolver, visited, depLib);
			}
		}
		
		public ProjectBackend ActiveProject {
			get { return activeProject; }
			set {
				if (activeProject == value)
					return;
				activeProject = value;
				if (paletteWidget != null) {
					paletteWidget.ProjectBackend = activeProject;
					paletteWidget.WidgetLibraries = GetActiveLibraries ();
				}
				if (propertiesWidget != null)
					propertiesWidget.ProjectBackend = activeProject;
				if (signalsWidget != null)
					signalsWidget.ProjectBackend = activeProject;
			}
		}
		
		public void SetActiveDesignSession (WidgetEditSession session)
		{
			activeDesignSession = session;
			if (projectWidget != null)
				projectWidget.Bind (session);
		}
		
		public WidgetLibrary[] GetActiveLibraries ()
		{
			return GetProjectLibraries (activeProject);
		}
		
		public WidgetLibrary[] GetProjectLibraries (ProjectBackend project)
		{
			ArrayList projectLibs = new ArrayList ();
			if (project != null)
				projectLibs.AddRange (project.WidgetLibraries);
				
			ArrayList list = new ArrayList ();
			list.Add (Registry.CoreWidgetLibrary);
			foreach (WidgetLibrary alib in Registry.RegisteredWidgetLibraries) {
				if (project != null && !alib.SupportsGtkVersion (project.TargetGtkVersion))
					continue;
				string aname = alib.Name;
				if (projectLibs.Contains (aname) || globalWidgetLibraries.Contains (aname))
					list.Add (alib);
			}
			return (WidgetLibrary[]) list.ToArray (typeof(WidgetLibrary));
		}
		
		public ProjectBackend CreateProject ()
		{
			return new ProjectBackend (this);
		}
		
		object IObjectViewer.TargetObject {
			get { return targetViewerObject; }
			set {
				targetViewerObject = value;
				if (propertiesWidget != null)
					propertiesWidget.TargetObject = targetViewerObject;
				if (signalsWidget != null)
					signalsWidget.Editor.TargetObject = targetViewerObject;
			}
		}
		
		public PaletteBackend GetPaletteWidget ()
		{
			if (paletteWidget == null) {
				paletteWidget = new PaletteBackend (this);
				paletteWidget.ProjectBackend = activeProject;
				paletteWidget.WidgetLibraries = GetActiveLibraries ();
			}
			return paletteWidget;
		}
		
		public void CreatePaletteWidgetPlug (uint socketId)
		{
			Gtk.Plug plug = new Gtk.Plug (socketId);
			plug.Decorated = false;
//			Gtk.Window plug = new Gtk.Window ("");
			plug.Add (GetPaletteWidget ());
			plug.Show ();
		}
		
		public void DestroyPaletteWidgetPlug ()
		{
			if (paletteWidget != null) {
				Gtk.Plug plug = (Gtk.Plug)paletteWidget.Parent;
				plug.Remove (paletteWidget);
				plug.Destroy ();
			}
		}
		
		public WidgetPropertyTreeBackend GetPropertiesWidget ()
		{
			if (propertiesWidget == null) {
				propertiesWidget = new WidgetPropertyTreeBackend ();
				propertiesWidget.ProjectBackend = activeProject;
				propertiesWidget.TargetObject = targetViewerObject;
			}
			return propertiesWidget;
		}
		
		public void CreatePropertiesWidgetPlug (uint socketId)
		{
			Gtk.Plug plug = new Gtk.Plug (socketId);
			plug.Decorated = false;
//			Gtk.Window plug = new Gtk.Window ("");
			plug.Add (GetPropertiesWidget ());
			plug.Show ();
		}
		
		public void DestroyPropertiesWidgetPlug ()
		{
			if (propertiesWidget != null) {
				Gtk.Plug plug = (Gtk.Plug) propertiesWidget.Parent;
				plug.Remove (propertiesWidget);
				plug.Destroy ();
			}
		}
		
		public SignalsEditorEditSession GetSignalsWidget (SignalsEditorFrontend frontend)
		{
			if (signalsWidget == null) {
				signalsWidget = new SignalsEditorEditSession (frontend);
				signalsWidget.ProjectBackend = activeProject;
				signalsWidget.Editor.TargetObject = targetViewerObject;
			}
			return signalsWidget;
		}
		
		public SignalsEditorEditSession CreateSignalsWidgetPlug (SignalsEditorFrontend frontend, uint socketId)
		{
			Gtk.Plug plug = new Gtk.Plug (socketId);
			plug.Decorated = false;
//			Gtk.Window plug = new Gtk.Window ("");
			SignalsEditorEditSession session = GetSignalsWidget (frontend);
			plug.Add (session.Editor);
			plug.Show ();
			return session;
		}
		
		public void DestroySignalsWidgetPlug ()
		{
			if (signalsWidget != null) {
				Gtk.Plug plug = (Gtk.Plug) signalsWidget.Editor.Parent;
				plug.Remove (signalsWidget.Editor);
				plug.Destroy ();
			}
		}
		
		public ProjectViewBackend GetProjectWidget (ProjectViewFrontend frontend)
		{
			if (projectWidget == null) {
				projectWidget = new ProjectViewBackend (frontend);
				projectWidget.Bind (activeDesignSession);
			}
			return projectWidget;
		}
		
		public void CreateProjectWidgetPlug (ProjectViewFrontend frontend, uint socketId)
		{
			Gtk.Plug plug = new Gtk.Plug (socketId);
			plug.Decorated = false;
//			Gtk.Window plug = new Gtk.Window ("");
			plug.Add (GetProjectWidget (frontend));
			plug.Show ();
		}
		
		public void DestroyProjectWidgetPlug ()
		{
			if (projectWidget != null) {
				Gtk.Plug plug = (Gtk.Plug)projectWidget.Parent;
				plug.Remove (projectWidget);
				plug.Destroy ();
			}
		}
		
		public ArrayList GetComponentTypes ()
		{
			ArrayList list = new ArrayList ();
			foreach (ClassDescriptor cd in Registry.AllClasses)
				list.Add (cd.Name);
			return list;
		}
		
		public bool GetClassDescriptorInfo (string name, out string desc, out string className, out string category, out string targetGtkVersion, out string library, out byte[] icon)
		{
			ClassDescriptor cls = Registry.LookupClassByName (name);
			if (cls == null) {
				icon = null;
				desc = null;
				className = null;
				category = null;
				targetGtkVersion = null;
				library = null;
				return false;
			}
			desc = cls.Label;
			className = cls.WrappedTypeName;
			category = cls.Category;
			targetGtkVersion = cls.TargetGtkVersion;
			library = cls.Library.Name;
			icon = cls.Icon != null ? cls.Icon.SaveToBuffer ("png") : null;
			return true;
		}
		
		public object[] GetClassDescriptorInitializationValues (string name)
		{
			ClassDescriptor cls = Registry.LookupClassByName (name);
			ArrayList list = new ArrayList ();
			
			foreach (Stetic.PropertyDescriptor prop in cls.InitializationProperties)
			{
				if (prop.PropertyType.IsValueType) {
					// Avoid sending to the main process types which should not be loaded there
					if (prop.PropertyType.Assembly == typeof(object).Assembly ||
					    prop.PropertyType.Assembly == typeof(Gtk.Widget).Assembly ||
					    prop.PropertyType.Assembly == typeof(Gdk.Window).Assembly ||
					    prop.PropertyType.Assembly == typeof(GLib.Object).Assembly) {
					    
						list.Add (Activator.CreateInstance (prop.PropertyType));
					} else
						return new object [0];
				} else
					list.Add (null);
			}
			return list.ToArray ();
		}
		
		public void ShowPaletteGroup (string name, string label)
		{
			GetPaletteWidget ().ShowGroup (name, label);
		}
		
		public void HidePaletteGroup (string name)
		{
			GetPaletteWidget ().HideGroup (name);
		}
		
		internal void GetClipboardOperations (object obj, out bool canCut, out bool canCopy, out bool canPaste)
		{
			Stetic.Wrapper.Widget wrapper = obj as Stetic.Wrapper.Widget;
			if (wrapper != null) {
				canCut = wrapper.InternalChildProperty == null && !wrapper.IsTopLevel;
				canCopy = !wrapper.IsTopLevel;
				canPaste = false;
			}
			else if (obj is Placeholder) {
				// FIXME: make it work for placeholders
				canCut = canCopy = false;
				canPaste = true;
			}
			else {
				canCut = canCopy = canPaste = false;
			}
		}
		
		internal void GetComponentInfo (object obj, out string name, out string type)
		{
			Stetic.Wrapper.Widget wrapper = obj as Stetic.Wrapper.Widget;
			name = wrapper.Wrapped.Name;
			type = wrapper.ClassDescriptor.Name;
		}
		
		internal void RenameWidget (Wrapper.Widget w, string newName)
		{
			w.Wrapped.Name = newName;
		}
		
		internal byte[] GetActionIcon (Wrapper.Action ac)
		{
			Gdk.Pixbuf pix = ac.RenderIcon (Gtk.IconSize.Menu);
			if (pix == null)
				return null;
			return pix.SaveToBuffer ("png");
		}
		
		internal ArrayList GetWidgetChildren (Wrapper.Widget ww)
		{
			Stetic.Wrapper.Container cw = ww as Stetic.Wrapper.Container;
			if (cw == null)
				return null;
				
			ArrayList list = new ArrayList ();
			foreach (object ob in cw.RealChildren) {
				ObjectWrapper ow = ObjectWrapper.Lookup (ob);
				if (ow != null)
					list.Add (Component.GetSafeReference (ow));
			}
			return list;
		}
		
		internal void RemoveWidgetSignal (ObjectWrapper wrapper, Signal signal)
		{
			foreach (Signal s in wrapper.Signals) {
				if (s.Handler == signal.Handler && s.SignalDescriptor.Name == signal.SignalDescriptor.Name) {
					wrapper.Signals.Remove (s);
					return;
				}
			}
		}
		
		internal Wrapper.ActionGroup[] GetActionGroups (Wrapper.Widget widget)
		{
			return widget.LocalActionGroups.ToArray ();
		}
		
		internal ObjectBindInfo[] GetBoundComponents (ObjectWrapper wrapper)
		{
			return CodeGenerator.GetFieldsToBind (wrapper).ToArray ();
		}
		
		internal ObjectWrapper GetPropertyTreeTarget ()
		{
			return targetViewerObject as ObjectWrapper;
		}
		
		internal void BeginComponentDrag (ProjectBackend project, string desc, string className, ObjectWrapper wrapper, Gtk.Widget source, Gdk.DragContext ctx, ComponentDropCallback callback)
		{
			if (wrapper != null) {
				Stetic.Wrapper.ActionPaletteItem it = new Stetic.Wrapper.ActionPaletteItem (Gtk.UIManagerItemType.Menuitem, null, (Wrapper.Action) wrapper);
				DND.Drag (source, ctx, it);
			}
			else if (callback != null) {
				DND.Drag (source, ctx, delegate () {
					callback ();
					
					// If the class name has an assembly name, remove it now
					int i = className.IndexOf (',');
					if (i != -1)
						className = className.Substring (0, i);
					
					ClassDescriptor cls = Registry.LookupClassByName (className);
					if (cls != null)
						return cls.NewInstance (project) as Gtk.Widget;
					else {
						// Class not found, show an error
						string msg = string.Format ("The widget '{0}' could not be found.", className);
						Gtk.MessageDialog dlg = new Gtk.MessageDialog (null, Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Close, msg);
						dlg.Run ();
						dlg.Destroy ();
						return null;
					}
				},
				(desc != null && desc.Length > 0) ? desc : className
				);
			}
			else {
				ClassDescriptor cls = Registry.LookupClassByName (className);
				DND.Drag (source, ctx, cls.NewInstance (project) as Gtk.Widget);
			}
		}
		
		public string ResolveAssembly (string assemblyName)
		{
			if (widgetLibraryResolver != null)
				return widgetLibraryResolver (assemblyName);
			else
				return null;
		}
	}
}
