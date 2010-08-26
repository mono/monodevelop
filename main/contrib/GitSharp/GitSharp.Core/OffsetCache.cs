/*
 * Copyright (C) 2009, Google Inc.
 * Copyrigth (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using GitSharp.Core.Util;
using GitSharp.Core.Util.JavaHelper;

namespace GitSharp.Core
{
	/// <summary>
	/// Least frequently used cache for objects specified by PackFile positions.
	/// <para />
	/// This cache maps a <code>(PackFile, position)</code> tuple to an object.
	/// <para />
	/// This cache is suitable for objects that are "relative expensive" to compute
	/// from the underlying PackFile, given some known position in that file.
	/// <para />
	/// Whenever a cache miss occurs, <see cref="load(PackFile, long)"/> is invoked by
	/// exactly one thread for the given <code>(PackFile,position)</code> key tuple.
	/// This is ensured by an array of _locks, with the tuple hashed to a @lock instance.
	/// <para />
	/// During a miss, older entries are evicted from the cache so long as
	/// <see cref="isFull"/> returns true.
	/// <para />
	/// Its too expensive during object access to be 100% accurate with a least
	/// recently used (LRU) algorithm. Strictly ordering every read is a lot of
	/// overhead that typically doesn't yield a corresponding benefit to the
	/// application.
	/// <para />
	/// This cache : a loose LRU policy by randomly picking a window
	/// comprised of roughly 10% of the cache, and evicting the oldest accessed entry
	/// within that window.
	/// <para />
	/// Entities created by the cache are held under SoftReferences, permitting the
	/// Java runtime's garbage collector to evict entries when heap memory gets low.
	/// Most JREs implement a loose least recently used algorithm for this eviction.
	/// <para />
	/// The internal hash table does not expand at runtime, instead it is fixed in
	/// size at cache creation time. The internal @lock table used to gate load
	/// invocations is also fixed in size.
	/// <para />
	/// The key tuple is passed through to methods as a pair of parameters rather
	/// than as a single object, thus reducing the transient memory allocations of
	/// callers. It is more efficient to avoid the allocation, as we can't be 100%
	/// sure that a JIT would be able to stack-allocate a key tuple.
	/// <para />
	/// This cache has an implementation rule such that:
	/// <list>
	/// <item><see cref="load(PackFile, long)"/> is invoked by at most one thread at a time
	/// for a given <code>(PackFile, position)</code> tuple.
	/// </item><item>For every <code>load()</code> invocation there is exactly one
	/// <see cref="createRef(PackFile, long, V)"/> invocation to wrap a SoftReference
	/// around the cached entity.
	/// </item><item>For every Reference created by <code>createRef()</code> there will be
	/// exactly one call to <see cref="clear(R)"/> to cleanup any resources associated
	/// with the (now expired) cached entity.
	/// </item>
	/// </list>
	/// <para />
	/// Therefore, it is safe to perform resource accounting increments during the
	/// <see cref="load(PackFile, long)"/> or <see cref="createRef(PackFile, long, V)"/>
	/// methods, and matching decrements during <see cref="clear(R)"/>. Implementors may
	/// need to override <see cref="createRef(PackFile, long, V)"/> in order to embed
	/// additional accounting information into an implementation specific
	/// <typeparamref name="V"/> subclass, as the cached entity may have already been
	/// evicted by the JRE's garbage collector.
	/// <para />
	/// To maintain higher concurrency workloads, during eviction only one thread
	/// performs the eviction work, while other threads can continue to insert new
	/// objects in parallel. This means that the cache can be temporarily over limit,
	/// especially if the nominated eviction thread is being starved relative to the
	/// other threads.
	/// </summary>
	/// <typeparam name="V">Type of value stored in the cache.</typeparam>
	/// <typeparam name="R">
	/// Subtype of <typeparamref name="R"/> subclass used by the cache.
	/// </typeparam>
	internal abstract class OffsetCache<V, R>
		where R : OffsetCache<V, R>.Ref<V>
		where V : class
	{
		// [ammachado] .NET Random is not thread safe
		private static readonly Random Rng = new Random();

		/// <summary>
		/// Queue that <see cref="createRef(PackFile, long, V)"/> must use.
		/// </summary>
		internal Queue queue;

		/// <summary>
		/// Number of entries in <see cref="_table"/>.
		/// </summary>
		private readonly int _tableSize;

		/// <summary>
		/// Access clock for loose LRU.
		/// </summary>
		private readonly AtomicLong _clock;

		/// <summary>
		/// Hash bucket directory; entries are chained below.
		/// </summary>
		private readonly AtomicReferenceArray<Entry<V>> _table;

		/// <summary>
		/// Locks to prevent concurrent loads for same (PackFile, position).
		/// </summary>
		private readonly LockTarget[] _locks;

		/// <summary>
		/// Lock to elect the eviction thread after a load occurs.
		/// </summary>
		private readonly AutoResetEvent _evictLock;

		/// <summary>
		/// Number of <see cref="_table"/> buckets to scan for an eviction window.
		/// </summary>
		private readonly int _evictBatch;

		/// <summary>
		/// Create a new cache with a fixed size entry table and @Lock table.
		/// </summary>
		/// <param name="tSize">number of entries in the entry hash table.</param>
		/// <param name="lockCount">
		/// number of entries in the <see cref="LockTarget"/> table. This is the maximum
		/// concurrency rate for creation of new objects through
		/// <see cref="load(PackFile, long)"/> invocations.
		/// </param>
		internal OffsetCache(int tSize, int lockCount)
		{
			if (tSize < 1)
			{
				throw new ArgumentException("tSize must be >= 1");
			}

			if (lockCount < 1)
			{
				throw new ArgumentException("lockCount must be >= 1");
			}

			queue = new Queue();
			_tableSize = tSize;
			_clock = new AtomicLong(1);
			_table = new AtomicReferenceArray<Entry<V>>(_tableSize);
			_locks = new LockTarget[lockCount];

			for (int i = 0; i < _locks.Length; i++)
			{
				_locks[i] = new LockTarget();
			}

			_evictLock = new AutoResetEvent(true);

			var eb = (int)(_tableSize * .1);

			if (64 < eb)
			{
				eb = 64;
			}
			else if (eb < 4)
			{
				eb = 4;
			}

			if (_tableSize < eb)
			{
				eb = _tableSize;
			}

			_evictBatch = eb;
		}

		/// <summary>
		/// Lookup a cached object, creating and loading it if it doesn't exist.
		/// </summary>
		/// <param name="pack">the pack that "contains" the cached object.</param>
		/// <param name="position">offset within <paramref name="pack"/> of the object.</param>
		/// <returns>The object reference.</returns>
		/// <exception cref="Exception">
		/// The object reference was not in the cache and could not be
		/// obtained by <see cref="load(PackFile, long)"/>
		/// </exception>
		internal V getOrLoad(PackFile pack, long position)
		{
			int slot = this.Slot(pack, position);
			Entry<V> e1 = _table.get(slot);
			V v = Scan(e1, pack, position);
			if (v != null)
				return v;

			lock (Lock(pack, position))
			{
				Entry<V> e2 = _table.get(slot);
				if (e2 != e1)
				{
					v = Scan(e2, pack, position);
					if (v != null)
						return v;
				}

				v = load(pack, position);
				Ref<V> @ref = createRef(pack, position, v);
				Hit(@ref);

				while (true)
				{
					var n = new Entry<V>(Clean(e2), @ref);
					if (_table.compareAndSet(slot, e2, n)) break;
					e2 = _table.get(slot);
				}
			}

			if (_evictLock.WaitOne())
			{
				try
				{
					Gc();
					Evict();
				}
				finally
				{
					_evictLock.Set();
				}
			}

			return v;
		}

		private V Scan(Entry<V> n, PackFile pack, long position)
		{
			for (; n != null; n = n.Next)
			{
				Ref<V> r = n.Ref;
				if (r.pack == pack && r.position == position)
				{
					V v = r.get();
					if (v != null)
					{
						Hit(r);
						return v;
					}
					n.Kill();
					break;
				}
			}

			return null;
		}

		private void Hit(Ref<V> r)
		{
			// We don't need to be 100% accurate here. Its sufficient that at least
			// one thread performs the increment. Any other concurrent access at
			// exactly the same time can simply use the same clock value.
			//
			// Consequently we attempt the set, but we don't try to recover should
			// it fail. This is why we don't use getAndIncrement() here.
			//
			long c = _clock.get();
			_clock.compareAndSet(c, c + 1);
			r.lastAccess = c;
		}

        private void Evict()
        {
            while (isFull())
            {
                int ptr = Rng.Next(_tableSize);
                Entry<V> old = null;
                int slot = 0;
                for (int b = _evictBatch - 1; b >= 0; b--, ptr++)
                {
                    if (_tableSize <= ptr)
                        ptr = 0;
                    for (Entry<V> e = _table.get(ptr); e != null; e = e.Next)
                    {
                        if (e.Dead)
                            continue;

                        if (old == null || e.Ref.lastAccess < old.Ref.lastAccess)
                        {
                            old = e;
                            slot = ptr;
                        }
                    }
                }

                if (old != null)
                {
                    old.Kill();
                    Gc();
                    Entry<V> e1 = _table.get(slot);
                    _table.compareAndSet(slot, e1, Clean(e1));
                }
            }
        }

	    /// <summary>
		/// Clear every entry from the cache.
		/// <para />
		/// This is a last-ditch effort to clear out the cache, such as before it
		/// gets replaced by another cache that is configured differently. This
		/// method tries to force every cached entry through <see cref="clear(R)"/> to
		/// ensure that resources are correctly accounted for and cleaned up by the
		/// subclass. A concurrent reader loading entries while this method is
		/// running may cause resource accounting failures.
		/// </summary>
		internal void removeAll()
		{
			for (int s = 0; s < _tableSize; s++)
			{
				Entry<V> e1;
				do
				{
					e1 = _table.get(s);
					for (Entry<V> e = e1; e != null; e = e.Next)
					{
						e.Kill();
					}
				} while (!_table.compareAndSet(s, e1, null));
			}

			Gc();
		}

		/// <summary>
		/// Clear all entries related to a single file.
		/// <para />
		/// Typically this method is invoked during <see cref="PackFile.Close()"/>, when we
		/// know the pack is never going to be useful to us again (for example, it no
		/// longer exists on disk). A concurrent reader loading an entry from this
		/// same pack may cause the pack to become stuck in the cache anyway.
		/// </summary>
		/// <param name="pack">the file to purge all entries of.</param>
		internal void removeAll(PackFile pack)
		{
			for (int s = 0; s < _tableSize; s++)
			{
				Entry<V> e1 = _table.get(s);
				bool hasDead = false;
				for (Entry<V> e = e1; e != null; e = e.Next)
				{
					if (e.Ref.pack == pack)
					{
						e.Kill();
						hasDead = true;
					}
					else if (e.Dead)
						hasDead = true;
				}
				if (hasDead)
					_table.compareAndSet(s, e1, Clean(e1));
			}
			Gc();
		}

		/// <summary>
		/// Materialize an object that doesn't yet exist in the cache.
		/// <para />
		/// This method is invoked by <see cref="getOrLoad(PackFile, long)"/> when the
		/// specified entity does not yet exist in the cache. Internal locking
		/// ensures that at most one thread can call this method for each unique
		/// <code>(pack,position)</code>, but multiple threads can call this method
		/// concurrently for different <code>(pack,position)</code> tuples.
		/// </summary>
		/// <param name="pack">The file to materialize the entry from.</param>
		/// <param name="position">Offset within the file of the entry.</param>
		/// <returns> the materialized object. Must never be null.</returns>
		/// <exception cref="Exception">
		/// The method was unable to materialize the object for this
		/// input pair. The usual reasons would be file corruption, file
		/// not found, out of file descriptors, etc.
		/// </exception>
		internal abstract V load(PackFile pack, long position);

		/// <summary>
		/// Construct a Ref (SoftReference) around a cached entity.
		/// <para />
		/// Implementing this is only necessary if the subclass is performing
		/// resource accounting during <see cref="load(PackFile, long)"/> and
		/// <see cref="clear(R)"/> requires some information to update the accounting.
		/// <para />
		/// Implementors <b>MUST</b> ensure that the returned reference uses the
		/// <see cref="queue">Queue</see>, otherwise <see cref="clear(R)"/> will not be
		/// invoked at the proper time.
		/// </summary>
		/// <param name="pack">The file to materialize the entry from.</param>
		/// <param name="position">Offset within the file of the entry.</param>
		/// <param name="v">
		/// The object returned by <see cref="load(PackFile, long)"/>.
		/// </param>
		/// <returns>
		/// A weak reference subclass wrapped around <typeparamref name="V"/>.
		/// </returns>
		internal virtual R createRef(PackFile pack, long position, V v)
		{
			return (R)new Ref<V>(pack, position, v, queue);
		}

		/// <summary>
		/// Update accounting information now that an object has left the cache.
		/// <para />
		/// This method is invoked exactly once for the combined
		/// <see cref="load(PackFile, long)"/> and
		/// <see cref="createRef(PackFile, long, V)"/> invocation pair that was used
		/// to construct and insert an object into the cache.
		/// </summary>
		/// <param name="ref">
		/// the reference wrapped around the object. Implementations must
		/// be prepared for <code>@ref.get()</code> to return null.
		/// </param>
		internal virtual void clear(R @ref)
		{
			// Do nothing by default.
		}

		/// <summary>
		/// Determine if the cache is full and requires eviction of entries.
		/// <para />
		/// By default this method returns false. Implementors may override to
		/// consult with the accounting updated by <see cref="load(PackFile, long)"/>,
		/// <see cref="createRef(PackFile, long, V)"/> and <see cref="clear(R)"/>.
		/// </summary>
		/// <returns>
		/// True if the cache is still over-limit and requires eviction of
		/// more entries.
		/// </returns>
		internal virtual bool isFull()
		{
			return false;
		}

		private void Gc()
		{
			R r;

			while (queue.Count > 0)
			{
				r = (R)queue.Dequeue();
				// Sun's Java 5 and 6 implementation have a bug where a Reference
				// can be enqueued and dequeued twice on the same reference queue
				// due to a race condition within Queue.enqueue(Reference).
				//
				// http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=6837858
				//
				// We CANNOT permit a Reference to come through us twice, as it will
				// skew the resource counters we maintain. Our canClear() check here
				// provides a way to skip the redundant dequeues, if any.
				//
				if (!r.canClear()) continue;

				clear(r);

				bool found = false;
				int s = Slot(r.pack, r.position);
				Entry<V> e1 = _table.get(s);

				for (Entry<V> n = e1; n != null; n = n.Next)
				{
					if (!n.Ref.Equals(r)) continue;

					//n.Dead = true;
					found = true;
					break;
				}

				if (found)
				{
					_table.compareAndSet(s, e1, Clean(e1));
				}
			}
		}

		/// <summary>
		/// Compute the hash code value for a <code>(PackFile,position)</code> tuple.
		/// <para />
		/// For example, <code>return packHash + (int) (position >>> 4)</code>.
		/// Implementors must override with a suitable hash (for example, a different
		/// right shift on the position).
		/// </summary>
		/// <param name="packHash">hash code for the file being accessed.</param>
		/// <param name="position">position within the file being accessed.</param>
		/// <returns>a reasonable hash code mixing the two values.</returns>
		internal abstract int hash(int packHash, long position);

		private int Slot(PackFile pack, long position)
		{
			return (int)((uint)hash(pack.Hash, position) >> 1) % _tableSize;
		}

		private LockTarget Lock(PackFile pack, long position)
		{
			return _locks[(int)((uint)hash(pack.Hash, position) >> 1) % _locks.Length];
		}

		private static Entry<V> Clean(Entry<V> top)
		{
			while (top != null && top.Dead)
			{
				top.Ref.enqueue();
				top = top.Next;
			}
			if (top == null) return null;

			Entry<V> n = Clean(top.Next);
			return n == top.Next ? top : new Entry<V>(n, top.Ref);
		}

		#region Nested Types

		private class Entry<T>
		{
			/// <summary>
			/// Next entry in the hash table's chain list.
			/// </summary>
			public readonly Entry<T> Next;

			/// <summary>
			/// The referenced object.
			/// </summary>
			public readonly Ref<T> Ref;

		    /// <summary>
		    /// Marked true when <see cref="Ref"/> returns null and the <see cref="Ref"/> 
		    /// is garbage collected.
		    /// <para />
		    /// A true here indicates that the @ref is no longer accessible, and that
		    /// we therefore need to eventually purge this Entry object out of the
		    /// bucket's chain.
		    /// </summary>
		    public bool Dead;

			public Entry(Entry<T> n, Ref<T> r)
			{
				Next = n;
				Ref = r;
			}

			public void Kill()
			{
			    Dead = true;
				Ref.enqueue();
			}
		}

		/// <summary>
		/// A <see cref="WeakReference"/> wrapped around a cached object.
		/// </summary>
		/// <typeparam name="T">Type of the cached object.</typeparam>
		internal class Ref<T> : WeakReference
		{
			private readonly Queue _queue;	
			private Object locker = new Object();

			public Ref(PackFile pack, long position, T v, Queue queue)
				: base(v)
			{
				_queue = queue;
				this.pack = pack;
				this.position = position;
			}

			public PackFile pack;
			public long position;
			public long lastAccess;
			private bool cleared;

			public bool enqueue()
			{
				if (_queue.Contains(this)) return false;
				_queue.Enqueue(this);
				return true;
			}

			public bool canClear()
			{
				lock(locker)
				{
					if (cleared)
	                    return false;
					cleared = true;
					return true;
				}
			}

			public T get()
			{
				return (T)Target;
			}
		}

		private class LockTarget
		{
			// Used only as target for locking
		}

		#endregion
	}
}