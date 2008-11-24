using System;
using System.Collections;
using System.CodeDom;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;

namespace Stetic {

	public enum FileFormat {
		Native,
		Glade
	}

	public abstract class ObjectWrapper : MarshalByRefObject, IDisposable
	{
		static Hashtable wrappers = new Hashtable ();

		protected IProject proj;
		protected object wrapped;
		protected ClassDescriptor classDescriptor;
		SignalCollection signals;
		internal Hashtable translationInfo;
		Hashtable extendedData;
		bool loading;
		IObjectFrontend frontend;
		bool disposed;
		
		// This id is used by the undo methods to identify an object.
		// This id is not stored, since it's used only while the widget is being
		// edited in the designer
		string undoId = WidgetUtils.GetUndoId ();
		
		UndoManager undoManager;
		static UndoManager defaultUndoManager = new UndoManager (true);
		
		public ObjectWrapper ()
		{
			undoManager = defaultUndoManager;
		}
		
		public SignalCollection Signals {
			get {
				if (signals == null)
					signals = new SignalCollection (this);
				return signals;
			}
		}
		
		public UndoManager UndoManager {
			get { return GetUndoManagerInternal (); }
			internal set { undoManager = value; }
		}
		
		// Called the get a diff of changes since the last call to GetUndoDiff().
		// The returned object can be used to restore the object status by calling
		// ApplyUndoRedoDiff. The implementation can use the UndoManager object to
		// store status information.
		public virtual object GetUndoDiff ()
		{
			return null;
		}
		
		// Called to apply a set of changes to the object. It returns
		// a set of changes which can be used to reverse the operation.
		public virtual object ApplyUndoRedoDiff (object diff)
		{
			return null;
		}
		
		public string UndoId {
			get { return undoId; }
			internal set { undoId = value; }
		}
		
		public virtual ObjectWrapper FindObjectByUndoId (string id)
		{
			if (undoId == id)
				return this;
			else
				return null;
		}
		
		internal virtual UndoManager GetUndoManagerInternal ()
		{
			return undoManager;
		}
		
		internal protected bool Loading {
			get { return loading; }
			set { loading = value; }
		}
		
		public IObjectFrontend Frontend {
			[NoGuiDispatch]
			get { return frontend; }
			[NoGuiDispatch]
			set {
				if (disposed)
					throw new InvalidOperationException ("Can't bind component to disposed wrapper");
				if (frontend != null)
					frontend.Dispose ();
				frontend = value;
			}
		}
		
		public IDictionary ExtendedData {
			get {
				if (extendedData == null)
					extendedData = new Hashtable ();
				return extendedData;
			}
		}
		
		public void AttachDesigner (IDesignArea designer)
		{
			OnDesignerAttach (designer);
		}
		
		public void DetachDesigner (IDesignArea designer)
		{
			OnDesignerDetach (designer);
		}
		
		public virtual void Wrap (object obj, bool initialized)
		{
			this.wrapped = obj;
			wrappers [GetIndentityObject (obj)] = this;
		}

		public virtual void Dispose ()
		{
			if (IsDisposed)
				return;

			if (Disposed != null)
				Disposed (this, EventArgs.Empty);
			disposed = true;
			if (frontend != null)
				frontend.Dispose ();
			frontend = null;
			if (wrapped != null)
				wrappers.Remove (GetIndentityObject (wrapped));
			System.Runtime.Remoting.RemotingServices.Disconnect (this);
		}
		
		public bool IsDisposed {
			[NoGuiDispatch]
			get { return disposed; }
		}

		public static ObjectWrapper Create (IProject proj, object wrapped)
		{
			ClassDescriptor klass = Registry.LookupClassByName (wrapped.GetType ().FullName);
			ObjectWrapper wrapper = klass.CreateWrapper ();
			wrapper.Loading = true;
			wrapper.proj = proj;
			wrapper.classDescriptor = klass;
			wrapper.Wrap (wrapped, true);
			wrapper.OnWrapped ();
			wrapper.Loading = false;
			return wrapper;
		}

		internal static void Bind (IProject proj, ClassDescriptor klass, ObjectWrapper wrapper, object wrapped, bool initialized)
		{
			wrapper.proj = proj;
			wrapper.classDescriptor = klass;
			wrapper.Wrap (wrapped, initialized);
			wrapper.OnWrapped ();
		}
		
		public virtual void Read (ObjectReader reader, XmlElement element)
		{
			throw new System.NotImplementedException ();
		}
		
		public virtual XmlElement Write (ObjectWriter writer)
		{
			throw new System.NotImplementedException ();
		}

		public static ObjectWrapper ReadObject (ObjectReader reader, XmlElement elem)
		{
			string className = elem.GetAttribute ("class");
			ClassDescriptor klass;
			if (reader.Format == FileFormat.Native)
				klass = Registry.LookupClassByName (className);
			else
				klass = Registry.LookupClassByCName (className);
			
			if (klass == null) {
				ErrorWidget we = new ErrorWidget (className, elem.GetAttribute ("id"));
				ErrorWidgetWrapper wrap = (ErrorWidgetWrapper) Create (reader.Project, we);
				wrap.Read (reader, elem);
				return wrap;
			}
			if (!klass.SupportsGtkVersion (reader.Project.TargetGtkVersion)) {
				ErrorWidget we = new ErrorWidget (className, klass.TargetGtkVersion, reader.Project.TargetGtkVersion, elem.GetAttribute ("id"));
				ErrorWidgetWrapper wrap = (ErrorWidgetWrapper) Create (reader.Project, we);
				wrap.Read (reader, elem);
				return wrap;
			}

			ObjectWrapper wrapper = klass.CreateWrapper ();
			wrapper.classDescriptor = klass;
			wrapper.proj = reader.Project;
			try {
				wrapper.OnBeginRead (reader.Format);
				wrapper.Read (reader, elem);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				ErrorWidget we = new ErrorWidget (ex, elem.GetAttribute ("id"));
				ErrorWidgetWrapper wrap = (ErrorWidgetWrapper) Create (reader.Project, we);
				wrap.Read (reader, elem);
				return wrap;
			} finally {
				wrapper.OnEndRead (reader.Format);
			}
			return wrapper;
		}
		
		internal void GenerateInitCode (GeneratorContext ctx, CodeExpression var)
		{
			// Set the value for initialization properties. The value for those properties is
			// usually set in the constructor, but top levels are created by the user, so
			// those properties need to be explicitely set in the Gui.Build method.
			foreach (PropertyDescriptor prop in ClassDescriptor.InitializationProperties) {
				GeneratePropertySet (ctx, var, prop);
			}
		}
		
		internal protected virtual void GenerateBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			// Write the widget properties
			foreach (ItemGroup group in ClassDescriptor.ItemGroups) {
				foreach (ItemDescriptor item in group) {
					if (!item.SupportsGtkVersion (Project.TargetGtkVersion))
						continue;
					PropertyDescriptor prop = item as PropertyDescriptor;
					if (prop == null || !prop.IsRuntimeProperty)
						continue;
					if (ClassDescriptor.InitializationProperties != null && Array.IndexOf (ClassDescriptor.InitializationProperties, prop) != -1)
						continue;
					GeneratePropertySet (ctx, var, prop);
				}
			}
		}
		
		internal protected virtual void GeneratePostBuildCode (GeneratorContext ctx, CodeExpression var)
		{
		}
		
		internal protected virtual CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			if (ClassDescriptor.InitializationProperties != null) {
				CodeExpression[] paramters = new CodeExpression [ClassDescriptor.InitializationProperties.Length];
				for (int n=0; n < paramters.Length; n++) {
					PropertyDescriptor prop = ClassDescriptor.InitializationProperties [n];
					paramters [n] = ctx.GenerateValue (prop.GetValue (Wrapped), prop.RuntimePropertyType, prop.Translatable && prop.IsTranslated (Wrapped));
				}
				return new CodeObjectCreateExpression (WrappedTypeName, paramters);
			} else
				return new CodeObjectCreateExpression (WrappedTypeName);
		}
		
		protected virtual void GeneratePropertySet (GeneratorContext ctx, CodeExpression var, PropertyDescriptor prop)
		{
			object oval = prop.GetValue (Wrapped);
			if (oval == null || (prop.HasDefault && prop.IsDefaultValue (oval)))
				return;
				
			CodeExpression val = ctx.GenerateValue (oval, prop.RuntimePropertyType, prop.Translatable && prop.IsTranslated (Wrapped));
			CodeExpression cprop;
			
			TypedPropertyDescriptor tprop = prop as TypedPropertyDescriptor;
			if (tprop == null || tprop.GladeProperty == prop) {
				cprop = new CodePropertyReferenceExpression (var, prop.Name);
			} else {
				cprop = new CodePropertyReferenceExpression (var, tprop.GladeProperty.Name);
				cprop = new CodePropertyReferenceExpression (cprop, prop.Name);
			}
			ctx.Statements.Add (new CodeAssignStatement (cprop, val));
		}
		
		public static ObjectWrapper Lookup (object obj)
		{
			if (obj == null)
				return null;
			else
				return wrappers [GetIndentityObject (obj)] as Stetic.ObjectWrapper;
		}

		public object Wrapped {
			get {
				return wrapped;
			}
		}

		public IProject Project {
			get {
				return proj;
			}
		}

		public ClassDescriptor ClassDescriptor {
			get { return classDescriptor; }
		}
		
		public virtual string WrappedTypeName {
			get { return classDescriptor.WrappedTypeName; }
		}
		
		public void NotifyChanged ()
		{
			if (UndoManager.CanNotifyChanged (this))
				OnObjectChanged (new ObjectWrapperEventArgs (this));
		}
		
		internal void FireObjectChangedEvent ()
		{
			OnObjectChanged (new ObjectWrapperEventArgs (this));
		}
		
		static object GetIndentityObject (object ob)
		{
			if (ob is Gtk.Container.ContainerChild) {
				// We handle ContainerChild in a special way here since
				// the Gtk.Container indexer always returns a new ContainerChild
				// instance. We register its wrapper using ContainerChildHashItem
				// to make sure that two different instance of the same ContainerChild
				// can be found equal.
				ContainerChildHashItem p = new ContainerChildHashItem ();
				p.ContainerChild = (Gtk.Container.ContainerChild) ob;
				return p;
			}
			else
				return ob;
		}
		
		public delegate void WrapperNotificationDelegate (object obj, string propertyName);
		
		public event WrapperNotificationDelegate Notify;
		public event SignalEventHandler SignalAdded;
		public event SignalEventHandler SignalRemoved;
		public event SignalChangedEventHandler SignalChanged;
		public event EventHandler Disposed;
		
		// Fired when any information of the object changes.
		public event ObjectWrapperEventHandler ObjectChanged;
		
		protected virtual void OnBeginRead (FileFormat format)
		{
			loading = true;
		}

		protected virtual void OnEndRead (FileFormat format)
		{
			loading = false;
		}

		internal protected virtual void OnObjectChanged (ObjectWrapperEventArgs args)
		{
			if (frontend != null)
				frontend.NotifyChanged ();
			if (!Loading) {
				if (proj != null)
					proj.NotifyObjectChanged (args);
				if (ObjectChanged != null)
					ObjectChanged (this, args);
			}
		}
		
		protected virtual void EmitNotify (string propertyName)
		{
			if (!Loading) {
				NotifyChanged ();
				if (!Loading && Notify != null)
					Notify (this, propertyName);
			}
		}

		internal protected virtual void OnSignalAdded (SignalEventArgs args)
		{
			OnObjectChanged (args);
			if (!Loading) {
				if (proj != null)
					proj.NotifySignalAdded (args);
				if (SignalAdded != null)
					SignalAdded (this, args);
			}
		}
		
		internal protected virtual void OnSignalRemoved (SignalEventArgs args)
		{
			OnObjectChanged (args);
			if (!Loading) {
				if (proj != null)
					proj.NotifySignalRemoved (args);
				if (SignalRemoved != null)
					SignalRemoved (this, args);
			}
		}
		
		internal protected virtual void OnSignalChanged (SignalChangedEventArgs args)
		{
			OnObjectChanged (args);
			if (!Loading) {
				if (proj != null)
					proj.NotifySignalChanged (args);
				if (SignalChanged != null)
					SignalChanged (this, args);
			}
		}
		
		internal protected virtual void OnDesignerAttach (IDesignArea designer)
		{
		}
		
		internal protected virtual void OnDesignerDetach (IDesignArea designer)
		{
		}
		
		internal protected virtual void OnWrapped ()
		{
		}
		
		internal protected virtual void DropObject (string data, Gtk.Widget obj)
		{
			// Called by DND.Drop
		}

		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
	}
	
	// Wraps a ContainerChild, and properly implements GetHashCode() and Equals()
	struct ContainerChildHashItem
	{
		public Gtk.Container.ContainerChild ContainerChild;
		
		public override int GetHashCode ()
		{
			return ContainerChild.Parent.GetHashCode () + ContainerChild.Child.GetHashCode ();
		}
		
		public override bool Equals (object ob)
		{
			if (!(ob is ContainerChildHashItem))
				return false;
			ContainerChildHashItem ot = (ContainerChildHashItem) ob;
			return ot.ContainerChild.Child == ContainerChild.Child && ot.ContainerChild.Parent == ContainerChild.Parent;
		}
	}
	
	public interface IObjectFrontend: IDisposable
	{
		void NotifyChanged ();
	}
}
