namespace Sharpen
{
	using System;
	using System.Threading;

	internal class InheritableThreadLocal<T> where T : class
	{
		private static object nullMarker;
		private LocalDataStoreSlot slot;

		static InheritableThreadLocal ()
		{
			InheritableThreadLocal<T>.nullMarker = new object ();
		}

		public InheritableThreadLocal ()
		{
			this.slot = System.Threading.Thread.AllocateDataSlot ();
		}

		public T Get ()
		{
			object data = System.Threading.Thread.GetData (this.slot);
			if (data == nullMarker) {
				return null;
			}
			if (data == null) {
				data = InitialValue ();
				Set ((T)data);
			}
			return (T)data;
		}

		protected virtual T InitialValue ()
		{
			return null;
		}

		public void Set (T val)
		{
			if (val == null) {
				System.Threading.Thread.SetData (slot, nullMarker);
			} else {
				System.Threading.Thread.SetData (slot, val);
			}
		}
	}
}
