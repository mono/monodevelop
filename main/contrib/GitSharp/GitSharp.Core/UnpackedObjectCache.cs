/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using GitSharp.Core.Util;
using System.Runtime.CompilerServices;

namespace GitSharp.Core
{
    public static class UnpackedObjectCache
    {
        private const int CacheSize = 1024;

        private static readonly WeakReference<Entry> Dead;
        private static int _maxByteCount;
        private static readonly Slot[] Cache;
        private static Slot _lruHead;
        private static Slot _lruTail;
        private static int _openByteCount;
		
		private static Object locker = new Object();

        private static int Hash(long position)
        {
            return (int)((uint)(((int)position) << 22) >> 22);
        }

        static UnpackedObjectCache()
        {
            Dead = new WeakReference<Entry>(null);
            _maxByteCount = new WindowCacheConfig().DeltaBaseCacheLimit;

            Cache = new Slot[CacheSize];
            for (int i = 0; i < CacheSize; i++)
            {
                Cache[i] = new Slot();
            }
        }

        public static void Reconfigure(WindowCacheConfig cfg)
        {
			if (cfg == null)
				throw new ArgumentNullException ("cfg");
			lock(locker)
			{
	            int dbLimit = cfg.DeltaBaseCacheLimit;
	            if (_maxByteCount != dbLimit)
	            {
	                _maxByteCount = dbLimit;
	                ReleaseMemory();
	            }
			}
        }

        public static Entry get(PackFile pack, long position)
        {
			lock(locker)
			{
	            Slot e = Cache[Hash(position)];
	            if (e.provider == pack && e.position == position)
	            {
	                Entry buf = e.data.get();
	                if (buf != null)
	                {
	                    MoveToHead(e);
	                    return buf;
	                }
	            }
	            return null;
			}
        }

        public static void store(PackFile pack, long position,
                 byte[] data, int objectType)
        {
			if (data==null)
				throw new ArgumentNullException("data");
			lock(locker)
			{
	            if (data.Length > _maxByteCount)
	                return; // Too large to cache.
	
	            Slot e = Cache[Hash(position)];
	            ClearEntry(e);
	
	            _openByteCount += data.Length;
	            ReleaseMemory();
	
	            e.provider = pack;
	            e.position = position;
	            e.sz = data.Length;
	            e.data = new WeakReference<Entry>(new Entry(data, objectType));
	            MoveToHead(e);
			}
        }

        private static void ReleaseMemory()
        {
            while (_openByteCount > _maxByteCount && _lruTail != null)
            {
                Slot currOldest = _lruTail;
                Slot nextOldest = currOldest.lruPrev;

                ClearEntry(currOldest);
                currOldest.lruPrev = null;
                currOldest.lruNext = null;

                if (nextOldest == null)
                {
                    _lruHead = null;
                }
                else
                {
                    nextOldest.lruNext = null;
                }

                _lruTail = nextOldest;
            }
        }

        public static void purge(PackFile file)
        {
			lock(locker)
			{
	            foreach (Slot e in Cache)
	            {
	                if (e.provider == file)
	                {
	                    ClearEntry(e);
	                    Unlink(e);
	                }
	            }
			}
        }

        private static void MoveToHead(Slot e)
        {
            Unlink(e);
            e.lruPrev = null;
            e.lruNext = _lruHead;
            if (_lruHead != null)
            {
                _lruHead.lruPrev = e;
            }
            else
            {
                _lruTail = e;
            }
            _lruHead = e;
        }

        private static void Unlink(Slot e)
        {
            Slot prev = e.lruPrev;
            Slot next = e.lruNext;

            if (prev != null)
            {
                prev.lruNext = next;
            }
            if (next != null)
            {
                next.lruPrev = prev;
            }
        }

        private static void ClearEntry(Slot e)
        {
            _openByteCount -= e.sz;
            e.provider = null;
            e.data = Dead;
            e.sz = 0;
        }

        #region Nested Types

        public class Entry
        {
            public byte[] data;

            public int type;

            public Entry(byte[] aData, int aType)
            {
                data = aData;
                type = aType;
            }
        }

        private class Slot : IDisposable
        {
            public Slot lruPrev;

            public Slot lruNext;

            public PackFile provider;

            public long position;

            public int sz;

            public WeakReference<Entry> data = Dead;
			
			public void Dispose ()
			{
				provider.Dispose();
				lruNext.Dispose();
				lruPrev.Dispose();
			}
			
        }

        #endregion
    }
}
