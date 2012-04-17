/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.IO;
using NGit.Internal;
using NGit.Storage.File;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// Caches slices of a
	/// <see cref="PackFile">PackFile</see>
	/// in memory for faster read access.
	/// <p>
	/// The WindowCache serves as a Java based "buffer cache", loading segments of a
	/// PackFile into the JVM heap prior to use. As JGit often wants to do reads of
	/// only tiny slices of a file, the WindowCache tries to smooth out these tiny
	/// reads into larger block-sized IO operations.
	/// <p>
	/// Whenever a cache miss occurs,
	/// <see cref="Load(PackFile, long)">Load(PackFile, long)</see>
	/// is invoked by
	/// exactly one thread for the given <code>(PackFile,position)</code> key tuple.
	/// This is ensured by an array of locks, with the tuple hashed to a lock
	/// instance.
	/// <p>
	/// During a miss, older entries are evicted from the cache so long as
	/// <see cref="IsFull()">IsFull()</see>
	/// returns true.
	/// <p>
	/// Its too expensive during object access to be 100% accurate with a least
	/// recently used (LRU) algorithm. Strictly ordering every read is a lot of
	/// overhead that typically doesn't yield a corresponding benefit to the
	/// application.
	/// <p>
	/// This cache implements a loose LRU policy by randomly picking a window
	/// comprised of roughly 10% of the cache, and evicting the oldest accessed entry
	/// within that window.
	/// <p>
	/// Entities created by the cache are held under SoftReferences, permitting the
	/// Java runtime's garbage collector to evict entries when heap memory gets low.
	/// Most JREs implement a loose least recently used algorithm for this eviction.
	/// <p>
	/// The internal hash table does not expand at runtime, instead it is fixed in
	/// size at cache creation time. The internal lock table used to gate load
	/// invocations is also fixed in size.
	/// <p>
	/// The key tuple is passed through to methods as a pair of parameters rather
	/// than as a single Object, thus reducing the transient memory allocations of
	/// callers. It is more efficient to avoid the allocation, as we can't be 100%
	/// sure that a JIT would be able to stack-allocate a key tuple.
	/// <p>
	/// This cache has an implementation rule such that:
	/// <ul>
	/// <li>
	/// <see cref="Load(PackFile, long)">Load(PackFile, long)</see>
	/// is invoked by at most one thread at a time
	/// for a given <code>(PackFile,position)</code> tuple.</li>
	/// <li>For every <code>load()</code> invocation there is exactly one
	/// <see cref="CreateRef(PackFile, long, ByteWindow)">CreateRef(PackFile, long, ByteWindow)
	/// 	</see>
	/// invocation to wrap a
	/// SoftReference around the cached entity.</li>
	/// <li>For every Reference created by <code>createRef()</code> there will be
	/// exactly one call to
	/// <see cref="Clear(Ref)">Clear(Ref)</see>
	/// to cleanup any resources associated
	/// with the (now expired) cached entity.</li>
	/// </ul>
	/// <p>
	/// Therefore, it is safe to perform resource accounting increments during the
	/// <see cref="Load(PackFile, long)">Load(PackFile, long)</see>
	/// or
	/// <see cref="CreateRef(PackFile, long, ByteWindow)">CreateRef(PackFile, long, ByteWindow)
	/// 	</see>
	/// methods, and matching
	/// decrements during
	/// <see cref="Clear(Ref)">Clear(Ref)</see>
	/// . Implementors may need to override
	/// <see cref="CreateRef(PackFile, long, ByteWindow)">CreateRef(PackFile, long, ByteWindow)
	/// 	</see>
	/// in order to embed additional
	/// accounting information into an implementation specific
	/// <see cref="Ref">Ref</see>
	/// subclass,
	/// as the cached entity may have already been evicted by the JRE's garbage
	/// collector.
	/// <p>
	/// To maintain higher concurrency workloads, during eviction only one thread
	/// performs the eviction work, while other threads can continue to insert new
	/// objects in parallel. This means that the cache can be temporarily over limit,
	/// especially if the nominated eviction thread is being starved relative to the
	/// other threads.
	/// </summary>
	public class WindowCache
	{
		private static int Bits(int newSize)
		{
			if (newSize < 4096)
			{
				throw new ArgumentException(JGitText.Get().invalidWindowSize);
			}
			if (Sharpen.Extensions.BitCount(newSize) != 1)
			{
				throw new ArgumentException(JGitText.Get().windowSizeMustBePowerOf2);
			}
			return Sharpen.Extensions.NumberOfTrailingZeros(newSize);
		}

		private static readonly Random rng = new Random();

		private static volatile NGit.Storage.File.WindowCache cache;

		private static volatile int streamFileThreshold;

		static WindowCache()
		{
			Reconfigure(new WindowCacheConfig());
		}

		/// <summary>Modify the configuration of the window cache.</summary>
		/// <remarks>
		/// Modify the configuration of the window cache.
		/// <p>
		/// The new configuration is applied immediately. If the new limits are
		/// smaller than what what is currently cached, older entries will be purged
		/// as soon as possible to allow the cache to meet the new limit.
		/// </remarks>
		/// <param name="packedGitLimit">maximum number of bytes to hold within this instance.
		/// 	</param>
		/// <param name="packedGitWindowSize">number of bytes per window within the cache.</param>
		/// <param name="packedGitMMAP">true to enable use of mmap when creating windows.</param>
		/// <param name="deltaBaseCacheLimit">number of bytes to hold in the delta base cache.
		/// 	</param>
		[System.ObsoleteAttribute(@"Use WindowCacheConfig instead.")]
		public static void Reconfigure(int packedGitLimit, int packedGitWindowSize, bool 
			packedGitMMAP, int deltaBaseCacheLimit)
		{
			WindowCacheConfig c = new WindowCacheConfig();
			c.SetPackedGitLimit(packedGitLimit);
			c.SetPackedGitWindowSize(packedGitWindowSize);
			c.SetPackedGitMMAP(packedGitMMAP);
			c.SetDeltaBaseCacheLimit(deltaBaseCacheLimit);
			Reconfigure(c);
		}

		/// <summary>Modify the configuration of the window cache.</summary>
		/// <remarks>
		/// Modify the configuration of the window cache.
		/// <p>
		/// The new configuration is applied immediately. If the new limits are
		/// smaller than what what is currently cached, older entries will be purged
		/// as soon as possible to allow the cache to meet the new limit.
		/// </remarks>
		/// <param name="cfg">the new window cache configuration.</param>
		/// <exception cref="System.ArgumentException">
		/// the cache configuration contains one or more invalid
		/// settings, usually too low of a limit.
		/// </exception>
		public static void Reconfigure(WindowCacheConfig cfg)
		{
			NGit.Storage.File.WindowCache nc = new NGit.Storage.File.WindowCache(cfg);
			NGit.Storage.File.WindowCache oc = cache;
			if (oc != null)
			{
				oc.RemoveAll();
			}
			cache = nc;
			streamFileThreshold = cfg.GetStreamFileThreshold();
			DeltaBaseCache.Reconfigure(cfg);
		}

		internal static int GetStreamFileThreshold()
		{
			return streamFileThreshold;
		}

		internal static NGit.Storage.File.WindowCache GetInstance()
		{
			return cache;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static ByteWindow Get(PackFile pack, long offset)
		{
			NGit.Storage.File.WindowCache c = cache;
			ByteWindow r = c.GetOrLoad(pack, c.ToStart(offset));
			if (c != cache)
			{
				// The cache was reconfigured while we were using the old one
				// to load this window. The window is still valid, but our
				// cache may think its still live. Ensure the window is removed
				// from the old cache so resources can be released.
				//
				c.RemoveAll();
			}
			return r;
		}

		internal static void Purge(PackFile pack)
		{
			cache.RemoveAll(pack);
		}

		/// <summary>ReferenceQueue to cleanup released and garbage collected windows.</summary>
		/// <remarks>ReferenceQueue to cleanup released and garbage collected windows.</remarks>
		private readonly ReferenceQueue<ByteWindow> queue;

		/// <summary>
		/// Number of entries in
		/// <see cref="table">table</see>
		/// .
		/// </summary>
		private readonly int tableSize;

		/// <summary>Access clock for loose LRU.</summary>
		/// <remarks>Access clock for loose LRU.</remarks>
		private readonly AtomicLong clock;

		/// <summary>Hash bucket directory; entries are chained below.</summary>
		/// <remarks>Hash bucket directory; entries are chained below.</remarks>
		private readonly AtomicReferenceArray<WindowCache.Entry> table;

		/// <summary>Locks to prevent concurrent loads for same (PackFile,position).</summary>
		/// <remarks>Locks to prevent concurrent loads for same (PackFile,position).</remarks>
		private readonly WindowCache.Lock[] locks;

		/// <summary>Lock to elect the eviction thread after a load occurs.</summary>
		/// <remarks>Lock to elect the eviction thread after a load occurs.</remarks>
		private readonly ReentrantLock evictLock;

		/// <summary>
		/// Number of
		/// <see cref="table">table</see>
		/// buckets to scan for an eviction window.
		/// </summary>
		private readonly int evictBatch;

		private readonly int maxFiles;

		private readonly long maxBytes;

		private readonly bool mmap;

		private readonly int windowSizeShift;

		private readonly int windowSize;

		private readonly AtomicInteger openFiles;

		private readonly AtomicLong openBytes;

		private WindowCache(WindowCacheConfig cfg)
		{
			tableSize = TableSize(cfg);
			int lockCount = LockCount(cfg);
			if (tableSize < 1)
			{
				throw new ArgumentException(JGitText.Get().tSizeMustBeGreaterOrEqual1);
			}
			if (lockCount < 1)
			{
				throw new ArgumentException(JGitText.Get().lockCountMustBeGreaterOrEqual1);
			}
			queue = new ReferenceQueue<ByteWindow>();
			clock = new AtomicLong(1);
			table = new AtomicReferenceArray<WindowCache.Entry>(tableSize);
			locks = new WindowCache.Lock[lockCount];
			for (int i = 0; i < locks.Length; i++)
			{
				locks[i] = new WindowCache.Lock();
			}
			evictLock = new ReentrantLock();
			int eb = (int)(tableSize * .1);
			if (64 < eb)
			{
				eb = 64;
			}
			else
			{
				if (eb < 4)
				{
					eb = 4;
				}
			}
			if (tableSize < eb)
			{
				eb = tableSize;
			}
			evictBatch = eb;
			maxFiles = cfg.GetPackedGitOpenFiles();
			maxBytes = cfg.GetPackedGitLimit();
			mmap = cfg.IsPackedGitMMAP();
			windowSizeShift = Bits(cfg.GetPackedGitWindowSize());
			windowSize = 1 << windowSizeShift;
			openFiles = new AtomicInteger();
			openBytes = new AtomicLong();
			if (maxFiles < 1)
			{
				throw new ArgumentException(JGitText.Get().openFilesMustBeAtLeast1);
			}
			if (maxBytes < windowSize)
			{
				throw new ArgumentException(JGitText.Get().windowSizeMustBeLesserThanLimit);
			}
		}

		internal virtual int GetOpenFiles()
		{
			return openFiles.Get();
		}

		internal virtual long GetOpenBytes()
		{
			return openBytes.Get();
		}

		private int Hash(int packHash, long off)
		{
			return packHash + (int)((long)(((ulong)off) >> windowSizeShift));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ByteWindow Load(PackFile pack, long offset)
		{
			if (pack.BeginWindowCache())
			{
				openFiles.IncrementAndGet();
			}
			try
			{
				if (mmap)
				{
					return pack.Mmap(offset, windowSize);
				}
				return pack.Read(offset, windowSize);
			}
			catch (IOException e)
			{
				Close(pack);
				throw;
			}
			catch (RuntimeException e)
			{
				Close(pack);
				throw;
			}
			catch (Error e)
			{
				Close(pack);
				throw;
			}
		}

		private WindowCache.Ref CreateRef(PackFile p, long o, ByteWindow v)
		{
			WindowCache.Ref @ref = new WindowCache.Ref(p, o, v, queue);
			openBytes.AddAndGet(@ref.size);
			return @ref;
		}

		private void Clear(WindowCache.Ref @ref)
		{
			openBytes.AddAndGet(-@ref.size);
			Close(@ref.pack);
		}

		private void Close(PackFile pack)
		{
			if (pack.EndWindowCache())
			{
				openFiles.DecrementAndGet();
			}
		}

		private bool IsFull()
		{
			return maxFiles < openFiles.Get() || maxBytes < openBytes.Get();
		}

		private long ToStart(long offset)
		{
			return ((long)(((ulong)offset) >> windowSizeShift)) << windowSizeShift;
		}

		private static int TableSize(WindowCacheConfig cfg)
		{
			int wsz = cfg.GetPackedGitWindowSize();
			long limit = cfg.GetPackedGitLimit();
			if (wsz <= 0)
			{
				throw new ArgumentException(JGitText.Get().invalidWindowSize);
			}
			if (limit < wsz)
			{
				throw new ArgumentException(JGitText.Get().windowSizeMustBeLesserThanLimit);
			}
			return (int)Math.Min(5 * (limit / wsz) / 2, 2000000000);
		}

		private static int LockCount(WindowCacheConfig cfg)
		{
			return Math.Max(cfg.GetPackedGitOpenFiles(), 32);
		}

		/// <summary>Lookup a cached object, creating and loading it if it doesn't exist.</summary>
		/// <remarks>Lookup a cached object, creating and loading it if it doesn't exist.</remarks>
		/// <param name="pack">the pack that "contains" the cached object.</param>
		/// <param name="position">offset within <code>pack</code> of the object.</param>
		/// <returns>the object reference.</returns>
		/// <exception cref="System.IO.IOException">
		/// the object reference was not in the cache and could not be
		/// obtained by
		/// <see cref="Load(PackFile, long)">Load(PackFile, long)</see>
		/// .
		/// </exception>
		private ByteWindow GetOrLoad(PackFile pack, long position)
		{
			int slot = Slot(pack, position);
			WindowCache.Entry e1 = table.Get(slot);
			ByteWindow v = Scan(e1, pack, position);
			if (v != null)
			{
				return v;
			}
			lock (LockCache(pack, position))
			{
				WindowCache.Entry e2 = table.Get(slot);
				if (e2 != e1)
				{
					v = Scan(e2, pack, position);
					if (v != null)
					{
						return v;
					}
				}
				v = Load(pack, position);
				WindowCache.Ref @ref = CreateRef(pack, position, v);
				Hit(@ref);
				for (; ; )
				{
					WindowCache.Entry n = new WindowCache.Entry(Clean(e2), @ref);
					if (table.CompareAndSet(slot, e2, n))
					{
						break;
					}
					e2 = table.Get(slot);
				}
			}
			if (evictLock.TryLock())
			{
				try
				{
					Gc();
					Evict();
				}
				finally
				{
					evictLock.Unlock();
				}
			}
			return v;
		}

		private ByteWindow Scan(WindowCache.Entry n, PackFile pack, long position)
		{
			for (; n != null; n = n.next)
			{
				WindowCache.Ref r = n.@ref;
				if (r.pack == pack && r.position == position)
				{
					ByteWindow v = r.Get();
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

		private void Hit(WindowCache.Ref r)
		{
			// We don't need to be 100% accurate here. Its sufficient that at least
			// one thread performs the increment. Any other concurrent access at
			// exactly the same time can simply use the same clock value.
			//
			// Consequently we attempt the set, but we don't try to recover should
			// it fail. This is why we don't use getAndIncrement() here.
			//
			long c = clock.Get();
			clock.CompareAndSet(c, c + 1);
			r.lastAccess = c;
		}

		private void Evict()
		{
			while (IsFull())
			{
				int ptr = rng.Next(tableSize);
				WindowCache.Entry old = null;
				int slot = 0;
				for (int b = evictBatch - 1; b >= 0; b--, ptr++)
				{
					if (tableSize <= ptr)
					{
						ptr = 0;
					}
					for (WindowCache.Entry e = table.Get(ptr); e != null; e = e.next)
					{
						if (e.dead)
						{
							continue;
						}
						if (old == null || e.@ref.lastAccess < old.@ref.lastAccess)
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
					WindowCache.Entry e1 = table.Get(slot);
					table.CompareAndSet(slot, e1, Clean(e1));
				}
			}
		}

		/// <summary>Clear every entry from the cache.</summary>
		/// <remarks>
		/// Clear every entry from the cache.
		/// <p>
		/// This is a last-ditch effort to clear out the cache, such as before it
		/// gets replaced by another cache that is configured differently. This
		/// method tries to force every cached entry through
		/// <see cref="Clear(Ref)">Clear(Ref)</see>
		/// to
		/// ensure that resources are correctly accounted for and cleaned up by the
		/// subclass. A concurrent reader loading entries while this method is
		/// running may cause resource accounting failures.
		/// </remarks>
		private void RemoveAll()
		{
			for (int s = 0; s < tableSize; s++)
			{
				WindowCache.Entry e1;
				do
				{
					e1 = table.Get(s);
					for (WindowCache.Entry e = e1; e != null; e = e.next)
					{
						e.Kill();
					}
				}
				while (!table.CompareAndSet(s, e1, null));
			}
			Gc();
		}

		/// <summary>Clear all entries related to a single file.</summary>
		/// <remarks>
		/// Clear all entries related to a single file.
		/// <p>
		/// Typically this method is invoked during
		/// <see cref="PackFile.Close()">PackFile.Close()</see>
		/// , when we
		/// know the pack is never going to be useful to us again (for example, it no
		/// longer exists on disk). A concurrent reader loading an entry from this
		/// same pack may cause the pack to become stuck in the cache anyway.
		/// </remarks>
		/// <param name="pack">the file to purge all entries of.</param>
		private void RemoveAll(PackFile pack)
		{
			for (int s = 0; s < tableSize; s++)
			{
				WindowCache.Entry e1 = table.Get(s);
				bool hasDead = false;
				for (WindowCache.Entry e = e1; e != null; e = e.next)
				{
					if (e.@ref.pack == pack)
					{
						e.Kill();
						hasDead = true;
					}
					else
					{
						if (e.dead)
						{
							hasDead = true;
						}
					}
				}
				if (hasDead)
				{
					table.CompareAndSet(s, e1, Clean(e1));
				}
			}
			Gc();
		}

		private void Gc()
		{
			WindowCache.Ref r;
			while ((r = (WindowCache.Ref)queue.Poll()) != null)
			{
				// Sun's Java 5 and 6 implementation have a bug where a Reference
				// can be enqueued and dequeued twice on the same reference queue
				// due to a race condition within ReferenceQueue.enqueue(Reference).
				//
				// http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=6837858
				//
				// We CANNOT permit a Reference to come through us twice, as it will
				// skew the resource counters we maintain. Our canClear() check here
				// provides a way to skip the redundant dequeues, if any.
				//
				if (r.CanClear())
				{
					Clear(r);
					bool found = false;
					int s = Slot(r.pack, r.position);
					WindowCache.Entry e1 = table.Get(s);
					for (WindowCache.Entry n = e1; n != null; n = n.next)
					{
						if (n.@ref == r)
						{
							n.dead = true;
							found = true;
							break;
						}
					}
					if (found)
					{
						table.CompareAndSet(s, e1, Clean(e1));
					}
				}
			}
		}

		private int Slot(PackFile pack, long position)
		{
			return ((int)(((uint)Hash(pack.hash, position)) >> 1)) % tableSize;
		}

		private WindowCache.Lock LockCache(PackFile pack, long position)
		{
			return locks[((int)(((uint)Hash(pack.hash, position)) >> 1)) % locks.Length];
		}

		private static WindowCache.Entry Clean(WindowCache.Entry top)
		{
			while (top != null && top.dead)
			{
				top.@ref.Enqueue();
				top = top.next;
			}
			if (top == null)
			{
				return null;
			}
			WindowCache.Entry n = Clean(top.next);
			return n == top.next ? top : new WindowCache.Entry(n, top.@ref);
		}

		private class Entry
		{
			/// <summary>Next entry in the hash table's chain list.</summary>
			/// <remarks>Next entry in the hash table's chain list.</remarks>
			internal readonly WindowCache.Entry next;

			/// <summary>The referenced object.</summary>
			/// <remarks>The referenced object.</remarks>
			internal readonly WindowCache.Ref @ref;

			/// <summary>Marked true when ref.get() returns null and the ref is dead.</summary>
			/// <remarks>
			/// Marked true when ref.get() returns null and the ref is dead.
			/// <p>
			/// A true here indicates that the ref is no longer accessible, and that
			/// we therefore need to eventually purge this Entry object out of the
			/// bucket's chain.
			/// </remarks>
			internal volatile bool dead;

			internal Entry(WindowCache.Entry n, WindowCache.Ref r)
			{
				next = n;
				@ref = r;
			}

			internal void Kill()
			{
				dead = true;
				@ref.Enqueue();
			}
		}

		/// <summary>A soft reference wrapped around a cached object.</summary>
		/// <remarks>A soft reference wrapped around a cached object.</remarks>
		private class Ref : SoftReference<ByteWindow>
		{
			internal readonly PackFile pack;

			internal readonly long position;

			internal readonly int size;

			internal long lastAccess;

			private bool cleared;

			protected internal Ref(PackFile pack, long position, ByteWindow v, ReferenceQueue
				<ByteWindow> queue) : base(v, queue)
			{
				this.pack = pack;
				this.position = position;
				this.size = v.Size();
			}

			internal bool CanClear()
			{
				lock (this)
				{
					if (cleared)
					{
						return false;
					}
					cleared = true;
					return true;
				}
			}
		}

		private sealed class Lock
		{
			// Used only for its implicit monitor.
		}
	}
}
