namespace Sharpen
{
	using System;

	internal class SoftReference<T> : Reference<T>
	{
		private ReferenceQueue<T> queue;
		private T value;

		public SoftReference (T val)
		{
			this.value = val;
		}

		public SoftReference (T val, ReferenceQueue<T> queue)
		{
			this.value = val;
			this.queue = queue;
		}

		public void Clear ()
		{
			this.value = default(T);
		}

		public bool Enqueue ()
		{
			if (this.queue == null) {
				return false;
			}
			return this.queue.Add (this);
		}

		public override T Get ()
		{
			return this.value;
		}
	}
}
