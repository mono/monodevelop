
using System;
using System.Xml;
using System.Collections;

namespace Stetic
{
	public class UndoQueue: MarshalByRefObject
	{
		ArrayList changeList = new ArrayList ();
		int undoListCount = 0;
		static UndoQueue empty = new UndoQueue ();
		
		public void AddChange (UndoRedoChange change)
		{
			if (undoListCount < changeList.Count) {
				// Destroy all undone changes
				changeList.RemoveRange (undoListCount, changeList.Count - undoListCount);
			}
			changeList.Add (change);
			undoListCount = changeList.Count;
		}
		
		public static UndoQueue Empty {
			get { return empty; }
		}
		
		public bool CanUndo {
			get { return undoListCount > 0; }
		}
		
		public bool CanRedo {
			get { return undoListCount < changeList.Count; }
		}
		
		public void Undo ()
		{
			if (undoListCount == 0)
				return;

			UndoRedoChange change = (UndoRedoChange) changeList [--undoListCount];
			if (change.CheckValid ()) {
				object res = change.ApplyChange ();
				if (res != null)
					changeList [undoListCount] = res;
				else
					// Undo failed
					changeList.RemoveAt (undoListCount);
			} else {
				changeList.RemoveAt (undoListCount);
				Undo ();
			}
		}
		
		public void Redo ()
		{
			if (undoListCount == changeList.Count)
				return;
			
			UndoRedoChange change = (UndoRedoChange) changeList [undoListCount++];
			if (change.CheckValid ()) {
				object res = change.ApplyChange ();
				if (res != null)
					changeList [undoListCount - 1] = res;
				else {
					// Redo failed
					undoListCount--;
					changeList.RemoveAt (undoListCount);
				}
			}
			else {
				changeList.RemoveAt (--undoListCount);
				Redo ();
			}
		}
		
		public void Purge ()
		{
			for (int n=0; n<changeList.Count; n++) {
				UndoRedoChange change = (UndoRedoChange) changeList [n];
				if (!change.CheckValid()) {
					changeList.RemoveAt (n);
					if (n < undoListCount)
						undoListCount--;
				}
			}
		}
	}
	
	public abstract class UndoRedoChange: MarshalByRefObject
	{
		public abstract UndoRedoChange ApplyChange ();

		public virtual bool CheckValid ()
		{
			return true;
		}
	}
	
	
	class ObjectWrapperUndoRedoChange: UndoRedoChange
	{
		UndoRedoManager manager;
		public string TargetObject;
		public object Diff;
		public ObjectWrapperUndoRedoChange Next;
		
		public ObjectWrapperUndoRedoChange (UndoRedoManager manager, string targetObject, object diff)
		{
			this.manager = manager;
			this.TargetObject = targetObject;
			this.Diff = diff;
		}
			
		public override UndoRedoChange ApplyChange ()
		{
			return manager.ApplyChange (this);
		}
		
		public override bool CheckValid ()
		{
			return manager.CheckValid ();
		}
	}
	
	class UndoRedoManager: IDisposable
	{
		UndoQueue queue;
		ObjectWrapper rootObject;
		bool updating;
		UndoManager undoManager = new UndoManager ();
		
		public UndoRedoManager ()
		{
			undoManager.UndoCheckpoint += OnUndoCheckpoint;
		}
		
		public ObjectWrapper RootObject {
			get { return rootObject; }
			set {
				rootObject = value;
				undoManager.SetRoot (rootObject);
			}
		}
		
		public UndoQueue UndoQueue {
			get { return queue; }
			set { queue = value; }
		}
		
		internal UndoManager UndoManager {
			get { return undoManager; }
		}
		
		void OnUndoCheckpoint (object sender, UndoCheckpointEventArgs args)
		{
			AddChange (args.ModifiedObjects);
		}
		
		void AddChange (ObjectWrapper[] obs)
		{
			if (updating || queue == null)
				return;

			ObjectWrapperUndoRedoChange firstChange = null;
			ObjectWrapperUndoRedoChange lastChange = null;
			
//			Console.WriteLine ("** UNDO CHECKPOINT: {0} objects", obs.Length);
			
			foreach (ObjectWrapper ob in obs) {
			
				// Get the diff for going from the new status to the old status
				object diff = GetDiff (ob);
				
				if (diff == null)	// No differences
					continue;
				
//				Console.WriteLine ("ADDCHANGE " + ob + " uid:" + ob.UndoId);
//				PrintPatch (diff);
				
				if (ob.UndoId == null || ob.UndoId.Length == 0)
					throw new InvalidOperationException ("Object of type '" + ob.GetType () + "' does not have an undo id.");

				ObjectWrapperUndoRedoChange change = new ObjectWrapperUndoRedoChange (this, ob.UndoId, diff);
				if (lastChange == null)
					lastChange = firstChange = change;
				else {
					lastChange.Next = change;
					lastChange = change;
				}
			}
			if (firstChange != null)
				queue.AddChange (firstChange);
		}
		
		protected virtual object GetDiff (ObjectWrapper w)
		{
			return w.GetUndoDiff ();
		}
		
		public UndoRedoChange ApplyChange (ObjectWrapperUndoRedoChange first)
		{
			updating = true;
			
			try {
				ObjectWrapperUndoRedoChange change = first;
				ObjectWrapperUndoRedoChange lastRedo = null;
				while (change != null) {
					ObjectWrapperUndoRedoChange redo = ApplyDiff (change.TargetObject, change.Diff);
					if (redo != null) {
						redo.Next = lastRedo;
						lastRedo = redo;
					}
					change = change.Next;
				}
				return lastRedo;
			} catch (Exception ex) {
				Console.WriteLine (ex);
				return null;
			} finally {
				updating = false;
			}
		}
		
		ObjectWrapperUndoRedoChange ApplyDiff (string id, object diff)
		{
//			Console.WriteLine ("** APPLYING DIFF: uid:" + id);
//			PrintPatch (diff);
			
			ObjectWrapper ww = rootObject.FindObjectByUndoId (id);
			if (ww == null) {
				Console.WriteLine ("Object with undo id '{0}' not found", id);
				return null;
			}
			
			object reverseDiff = ww.ApplyUndoRedoDiff (diff);
		
			if (reverseDiff != null) {
//				Console.WriteLine ("** REVERSE DIFF:");
//				PrintPatch (reverseDiff);
				
				ObjectWrapperUndoRedoChange change = new ObjectWrapperUndoRedoChange (this, id, reverseDiff);
				return change;
			} else
				return null;
		}
		
		internal bool CheckValid ()
		{
			return rootObject != null;
		}
		
		public void Dispose ()
		{
			rootObject = null;
			if (queue != null)
				queue.Purge ();
		}
		
		internal void PrintPatch (object diff)
		{
			if (diff is Array) {
				foreach (object ob in (Array)diff)
					if (ob != null) PrintPatch (ob);
			} else if (diff is XmlElement)
				Console.WriteLine (((XmlElement)diff).OuterXml);
			else
				Console.WriteLine (diff.ToString ());
		}
	}
}

