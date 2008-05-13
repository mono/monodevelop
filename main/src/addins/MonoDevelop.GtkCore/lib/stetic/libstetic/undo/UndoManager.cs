
using System;
using System.Xml;
using System.Collections;
using Stetic.Wrapper;

namespace Stetic
{
	// This class holds an xml tree which describes the whole widget structure being designed.
	// It is used by the Undo/Redo infrastructure to keep track of changes in widgets.
	public class UndoManager
	{
		Hashtable elements = new Hashtable ();
		XmlDocument doc;
		ObjectWrapper root;
		AtomicChangeTracker atomicChangeTracker;
		bool isDefaultManager;
		
		public event UndoCheckpointHandler UndoCheckpoint;
		
		public UndoManager ()
		{
			atomicChangeTracker = new AtomicChangeTracker ();
			atomicChangeTracker.undoManager = this;
		}
		
		internal UndoManager (bool isDefaultManager): this ()
		{
			this.isDefaultManager = isDefaultManager;
		}
		
		public void SetRoot (ObjectWrapper wrapper)
		{
			root = wrapper;
			wrapper.UndoManager = this;
			elements.Clear ();
			
			doc = new XmlDocument ();
			UndoWriter writer = new UndoWriter (doc, this);
			writer.WriteObject (wrapper);
		}
		
		internal bool CanNotifyChanged (ObjectWrapper wrapper)
		{
			if (!InAtomicChange) {
				if (IsRegistered (wrapper) && UndoCheckpoint != null)
					UndoCheckpoint (this, new UndoCheckpointEventArgs (new ObjectWrapper[] { wrapper }));
				return true;
			} else
				return atomicChangeTracker.ProcessChange (wrapper);
		}
		
		public IAtomicChange AtomicChange {
			get {
				atomicChangeTracker.Count++;
				return atomicChangeTracker;
			}
		}
		
		public bool InAtomicChange {
			get { return atomicChangeTracker.InAtomicChange; }
		}
		
		// This method can be called by containers to register new objects in the tree.
		// Unless an object is registered in this way, no status will be tracked for it.
		// The provided status element must be a direct or indirect child of the parent status.
		internal void RegisterObject (ObjectWrapper w, XmlElement status)
		{
			VerifyManager ();
				
			if (IsRegistered (w))
				throw new InvalidOperationException ("Object already registered: " + w.GetType ());

			elements [w] = GetLocalElement (status);
			
			w.Disposed += OnObjectDisposed;
		}
		
		void OnObjectDisposed (object s, EventArgs a)
		{
			ObjectWrapper w = (ObjectWrapper) s;
			UnregisterObject (w);
			w.Disposed -= OnObjectDisposed;
		}
		
		// This method can be called to update the xml tree, for example when a change in the
		// object is detected.
		internal void UpdateObjectStatus (ObjectWrapper w, XmlElement status)
		{
			VerifyManager ();
			
			XmlElement oldElem = (XmlElement) elements [w];
			if (oldElem == null)
				throw new InvalidOperationException ("Could not update unregistered object of type " + w.GetType ());

			if (oldElem != status) {
				XmlElement newElem = GetLocalElement (status);
				if (oldElem.ParentNode != null) {
					oldElem.ParentNode.ReplaceChild (newElem, oldElem);
					elements [w] = newElem;
				} else {
					if (w != root)
						throw new InvalidOperationException ("Root element does not match the root widget: " + w.GetType ());
					elements [w] = newElem;
				}
			}
		}
		
		// Returns the xml that describes the specified widget (including information for all
		// children of the widget).
		internal XmlElement GetObjectStatus (ObjectWrapper w)
		{
			VerifyManager ();
				
			XmlElement elem = (XmlElement) elements [w];
			if (elem == null)
				throw new InvalidOperationException ("No status found for object of type " + w.GetType ());
			return elem;
		}
		
		internal bool IsRegistered (ObjectWrapper w)
		{
			return elements.ContainsKey (w);
		}
		
		internal void UnregisterObject (ObjectWrapper w)
		{
			VerifyManager ();
			elements.Remove (w);
		}
		
		void VerifyManager ()
		{
			if (isDefaultManager)
				throw new InvalidOperationException ("The default UndoManager can't track changes in objects.");
		}
		
		XmlElement GetLocalElement (XmlElement elem)
		{
			if (elem.OwnerDocument != doc)
				throw new InvalidOperationException ("Invalid document owner.");
			return elem;
		}
		
		internal void NotifyUndoCheckpoint (ObjectWrapper[] obs)
		{
			if (UndoCheckpoint != null)
				UndoCheckpoint (this, new UndoCheckpointEventArgs (obs));
		}
		
		internal void Dump ()
		{
			Console.WriteLine ("--------------------------------------");
			Console.WriteLine ("UNDO STATUS:");
			Console.WriteLine (GetObjectStatus (root).OuterXml);
			Console.WriteLine ("--------------------------------------");
		}
	}
	
	public delegate void UndoCheckpointHandler (object sender, UndoCheckpointEventArgs args);
	
	public class UndoCheckpointEventArgs: EventArgs
	{
		ObjectWrapper[] objects;
		
		internal UndoCheckpointEventArgs (ObjectWrapper[] objects)
		{
			this.objects = objects;
		}
		
		public ObjectWrapper[] ModifiedObjects {
			get { return objects; }
		}
	}
	
	// This is a special writer use to generate status info from widgets.
	// This writer won't recurse through objects which are already registered
	// in the provided UndoManager.
	class UndoWriter: ObjectWriter
	{
		UndoManager undoManager;
		bool allowMarkers = true;
		
		public UndoWriter (XmlDocument doc, UndoManager undoManager): base (doc, FileFormat.Native)
		{
			this.undoManager = undoManager;
			CreateUndoInfo = true;
		}
		
		public override XmlElement WriteObject (ObjectWrapper wrapper)
		{
			Wrapper.Widget ww = wrapper as Wrapper.Widget;
			
			// If the object is already registered, skip it (just create a dummy object)
			if (allowMarkers && ww != null && undoManager.IsRegistered (ww) && !ww.RequiresUndoStatusUpdate) {
				XmlElement marker = XmlDocument.CreateElement ("widget");
				marker.SetAttribute ("unchanged_marker","yes");
				return marker;
			}

			// Don't allow markers in indirect children, since those are not checked
			// when creating the diff
			bool oldAllow = allowMarkers;
			allowMarkers = false;
			XmlElement elem = base.WriteObject (wrapper);
			allowMarkers = oldAllow;
			
			if (ww != null) {
				ww.RequiresUndoStatusUpdate = false;
			}

			// Register the object, so it is correctly bound to this xml element
			if (undoManager.IsRegistered (wrapper))
				undoManager.UnregisterObject (wrapper);
			undoManager.RegisterObject (wrapper, elem);
			
			return elem;
		}
	}
	
	class UndoReader: ObjectReader
	{
		UndoManager undoManager;
		
		public UndoReader (IProject proj, FileFormat format, UndoManager undoManager): base (proj, format)
		{
			this.undoManager = undoManager;
		}
		
		public override ObjectWrapper ReadObject (XmlElement elem)
		{
			ObjectWrapper ww = base.ReadObject (elem);
			if (ww is Widget)
				undoManager.RegisterObject ((Widget)ww, elem);
			return ww;
		}
		
		public override void ReadObject (ObjectWrapper wrapper, XmlElement elem)
		{
			base.ReadObject (wrapper, elem);
			if (wrapper is Widget)
				undoManager.RegisterObject ((Widget)wrapper, elem);
		}
	}
	
	public interface IAtomicChange: IDisposable
	{
		void Delay ();
	}
	
	class AtomicChangeTracker: IAtomicChange
	{
		public int Count;
		public ArrayList ChangeEventPending = new ArrayList ();
		public UndoManager undoManager;
		bool delayed;
		
		public bool InAtomicChange {
			get { return Count > 0; }
		}
		
		public bool ProcessChange (ObjectWrapper wrapper)
		{
			if (!ChangeEventPending.Contains (wrapper)) {
				ChangeEventPending.Add (wrapper);
				delayed = false;
			}
			return false;
		}
		
		public void Delay ()
		{
			delayed = true;
		}

		public void Dispose ()
		{
			if (Count == 0)
				return;

			if (Count == 1) {
				// The change events fired here may generate changes in other
				// objects. Those changes will also be included in the transaction.
				// So, the ChangeEventPending array may grow while calling NotifyChanged,
				// and that's ok.
				
				for (int n=0; n < ChangeEventPending.Count; n++) {
					((ObjectWrapper)ChangeEventPending[n]).FireObjectChangedEvent ();
				}
				
				// Remove from the list the widgets that have been disposed. It means that
				// they have been deleted. That change will be recorded by their parents.
				// Remove as well wrappers that are not registered, since there won't be
				// status information for them.
				for (int n=0; n<ChangeEventPending.Count; n++) {
					ObjectWrapper w = (ObjectWrapper)ChangeEventPending[n]; 
					if (w.IsDisposed || !undoManager.IsRegistered (w)) {
						ChangeEventPending.RemoveAt (n);
						n--;
					}
				}
				
				ObjectWrapper[] obs = (ObjectWrapper[]) ChangeEventPending.ToArray (typeof(ObjectWrapper));
				ChangeEventPending.Clear ();
				Count = 0;
				
				if (!delayed)
					undoManager.NotifyUndoCheckpoint (obs);
				delayed = false;
			}
			else
				Count--;
		}
	}
}
