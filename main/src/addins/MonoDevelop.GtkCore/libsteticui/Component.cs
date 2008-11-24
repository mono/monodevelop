
using System;
using System.Collections;

namespace Stetic
{
	public class Component: MarshalByRefObject, IObjectFrontend
	{
		protected string name;
		protected ComponentType type;
		protected string typeName;
		protected object backend;
		protected Application app;
		
		public event EventHandler Changed;

		internal Component (Application app, object backend, string name, ComponentType type)
		{
			this.app = app;
			this.backend = backend;
			this.name = name;
			this.type = type;
		}
		
		public virtual void Dispose ()
		{
			System.Runtime.Remoting.RemotingServices.Disconnect (this);
			if (app != null)
				app.DisposeComponent (this);
		}
		
		public SignalCollection GetSignals ()
		{
			if (backend is ObjectWrapper)
				return ((ObjectWrapper)backend).Signals;
			else
				return new SignalCollection ();
		}
		
		public void RemoveSignal (Signal signal)
		{
			if (backend is ObjectWrapper && app != null)
				app.Backend.RemoveWidgetSignal ((ObjectWrapper) backend, signal);
		}
		
		public virtual Component[] GetChildren ()
		{
			return new Component [0];
		}
		
		void IObjectFrontend.NotifyChanged ()
		{
			OnChanged ();
		}
		
		protected virtual void OnChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public virtual string Name {
			get { return name; }
			set { name = value; }
		}

		public virtual ComponentType Type {
			get {
				return type;
			}
		}
		
		public virtual bool GeneratePublic {
			get { return true; }
			set {}
		}
		
		internal object Backend {
			get { return backend; }
		}
		
		internal static MarshalByRefObject GetSafeReference (MarshalByRefObject ob)
		{
			// Make sure we don't leak the wrapper type to the frontend process
			
			if (ob is Wrapper.Window) {
				System.Runtime.Remoting.RemotingServices.Marshal (ob, null, typeof(Wrapper.Window));
			} else if (ob is Wrapper.Container) {
				System.Runtime.Remoting.RemotingServices.Marshal (ob, null, typeof(Wrapper.Container));
			} else if (ob is Wrapper.Widget) {
				System.Runtime.Remoting.RemotingServices.Marshal (ob, null, typeof(Wrapper.Widget));
			} else if (ob is ObjectWrapper) {
				System.Runtime.Remoting.RemotingServices.Marshal (ob, null, typeof(ObjectWrapper));
			}
			return ob;
		}
		
		public override string ToString ()
		{
			return base.ToString() + " " + backend;
		}
		
		public ObjectBindInfo[] GetObjectBindInfo ()
		{
			ObjectWrapper ww = backend as ObjectWrapper;
			if (ww != null)
				return app.Backend.GetBoundComponents (ww);
			else
				return new ObjectBindInfo [0];
		}
	}
}
