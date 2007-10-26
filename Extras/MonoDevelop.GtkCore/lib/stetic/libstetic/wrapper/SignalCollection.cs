using System;
using System.Collections;

namespace Stetic
{
	[Serializable]
	public class SignalCollection: CollectionBase
	{
		[NonSerialized]
		ObjectWrapper owner;
		
		[NonSerialized]
		Signal[] clearedData;
		
		public SignalCollection ()
		{
		}
		
		internal SignalCollection (ObjectWrapper owner)
		{
			this.owner = owner;
		}
		
		public int Add (Signal signal)
		{
			return List.Add (signal);
		}
		
		public Signal this [int n] {
			get { return (Signal) List [n]; }
		}
		
		public void Remove (Signal signal)
		{
			List.Remove (signal);
		}
		
		public void CopyTo (Signal[] signals, int index)
		{
			List.CopyTo (signals, index);
		}
		
		protected override void OnClear ()
		{
			if (owner != null) {
				clearedData = new Signal [Count];
				List.CopyTo (clearedData, 0);
			}
		}
		
		protected override void OnClearComplete ()
		{
			if (owner != null) {
				Signal[] data = clearedData;
				clearedData = null;
				foreach (Signal s in data) {
					s.Owner = null;
					owner.OnSignalRemoved (new SignalEventArgs (owner, s));
				}
			}
		}
		
		protected override void OnInsertComplete (int index, object value)
		{
			if (owner != null) {
				((Signal)value).Owner = owner;
				owner.OnSignalAdded (new SignalEventArgs (owner, (Signal) value));
			}
		}
		
		protected override void OnRemoveComplete (int index, object value)
		{
			if (owner != null) {
				((Signal)value).Owner = null;
				owner.OnSignalRemoved (new SignalEventArgs (owner, (Signal) value));
			}
		}
		
		protected override void OnSetComplete (int index, object oldValue, object newValue)
		{
			if (owner != null) {
				((Signal)oldValue).Owner = null;
				owner.OnSignalRemoved (new SignalEventArgs (owner, (Signal) oldValue));
				((Signal)newValue).Owner = owner;
				owner.OnSignalAdded (new SignalEventArgs (owner, (Signal) newValue));
			}
		}
	}
}
