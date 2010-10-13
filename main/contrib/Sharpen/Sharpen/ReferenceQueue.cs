namespace Sharpen
{
	using System;
	using System.Collections.Generic;

	internal class ReferenceQueue<T>
	{
		private Queue<Reference<T>> queue;

		public ReferenceQueue ()
		{
			this.queue = new Queue<Reference<T>> ();
		}

		internal bool Add (Reference<T> t)
		{
			Queue<Reference<T>> queue = this.queue;
			lock (queue) {
				if (this.queue.Contains (t)) {
					return false;
				}
				this.queue.Enqueue (t);
				return true;
			}
		}

		public Reference<T> Poll ()
		{
			Queue<Reference<T>> queue = this.queue;
			lock (queue) {
				if (this.queue.Count > 0) {
					return this.queue.Dequeue ();
				}
				return null;
			}
		}
	}
}
