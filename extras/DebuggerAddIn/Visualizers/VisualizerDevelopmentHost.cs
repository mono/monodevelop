using System;
using System.IO;

#if WITH_SWF
using System.Windows.Forms;
#endif
#if WITH_GTK
using Gtk;
#endif

namespace MonoDevelop.DebuggerVisualizers
{
	public class VisualizerDevelopmentHost
	{
		class HostObjectProvider : IVisualizerObjectProvider
		{
			object obj;

			public HostObjectProvider (object obj)
			{
				this.obj = obj;
			}

			public bool IsObjectReplaceable
			{
				get { return false; }
			}

			public Stream GetData()
			{
				return null;
			}

			public object GetObject()
			{
				return obj;
			}

			public void ReplaceData (Stream newObjectData)
			{
			}

			public void ReplaceObject (object newObject)
			{
			}

			public Stream TransferData (Stream outgoingData)
			{
				return null;
			}

			public object TransferObject (object outgoingObject)
			{
				throw new InvalidOperationException ();
			}

		}

		object objectToVisualize;
		Type visualizerType;
		Type proxyType;
		bool replacementOK;

		public VisualizerDevelopmentHost (object objectToVisualize, Type visualizerType, Type proxyType, bool replacementOK)
		{
			this.objectToVisualize = objectToVisualize;
			this.visualizerType = visualizerType;
			this.proxyType = proxyType;
			this.replacementOK = replacementOK;
		}

		public VisualizerDevelopmentHost (object objectToVisualize, Type visualizerType, Type proxyType)
		  : this (objectToVisualize, visualizerType, proxyType, false)
		{
		}

		public VisualizerDevelopmentHost (object objectToVisualize, Type visualizerType)
		  : this (objectToVisualize, visualizerType, null, false)
		{
		}

		public object DebuggeeObject {
			set {
				objectToVisualize = value;
			}

			get {
				return objectToVisualize;
			}
		}

#if WITH_SWF
		public void ShowVisualizer(IWin32Window parentWindow)
		{
			throw new NotImplementedException ();
		}

		public void ShowVisualizer(Control parentControl)
		{
			throw new NotImplementedException ();
		}

#endif
#if WITH_GTK
		public void ShowVisualizer (Window parentWindow)
		{
			throw new NotImplementedException ();
		}

		public void ShowVisualizer (Widget parentWidget)
		{
			throw new NotImplementedException ();
		}
#endif

#if WITH_GTK
		public bool IdleShow ()
		{
			DialogDebuggerVisualizer vis = (DialogDebuggerVisualizer)Activator.CreateInstance (visualizerType);

			vis.Show (null, new HostObjectProvider (objectToVisualize));

			Application.Quit ();

			return false;
		}
#endif
		public void ShowVisualizer()
		{
#if WITH_GTK
			Gtk.Application.Init ();

			GLib.Idle.Add (new GLib.IdleHandler (IdleShow));
			
			Gtk.Application.Run ();
#endif
		}
	}
}
