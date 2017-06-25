
using System;

namespace Stetic
{
	public class SignalsEditor: PluggableWidget
	{
		SignalsEditorFrontend frontend;
		SignalsEditorEditSession session;
		
		public event EventHandler SignalActivated;
		
		internal SignalsEditor (Application app): base (app)
		{
			frontend = new SignalsEditorFrontend (this);
		}
		
		public Signal SelectedSignal {
			get {
				if (session != null)
					return session.SelectedSignal;
				else
					return null; 
			}
		}
		
		protected override void OnCreatePlug (uint socketId)
		{
			session = app.Backend.CreateSignalsWidgetPlug (frontend, socketId);
		}
		
		protected override void OnDestroyPlug (uint socketId)
		{
			app.Backend.DestroySignalsWidgetPlug ();
		}
		
		protected override Gtk.Widget OnCreateWidget ()
		{
			session = app.Backend.GetSignalsWidget (frontend);
			return session.Editor;
		}
		
		bool disposed = false;

		public override void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;

			if (session != null)
				session.Dispose ();
			frontend.disposed = true;
			System.Runtime.Remoting.RemotingServices.Disconnect (frontend);
			base.Dispose ();
		}
		
		internal void NotifySignalActivated ()
		{
			if (SignalActivated != null)
				SignalActivated (this, EventArgs.Empty);
		}
	}
	
	internal class SignalsEditorFrontend: MarshalByRefObject
	{
		SignalsEditor editor;
		internal bool disposed;
		
		public SignalsEditorFrontend (SignalsEditor editor)
		{
			this.editor = editor;
		}
		
		public void NotifySignalActivated ()
		{
			Gtk.Application.Invoke (
				(o, args) => {
					if (!disposed) editor.NotifySignalActivated ();
				}
			);
		}

		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
	}
}
