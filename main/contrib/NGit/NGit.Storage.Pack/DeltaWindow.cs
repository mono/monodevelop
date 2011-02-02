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
using ICSharpCode.SharpZipLib.Zip.Compression;
using NGit;
using NGit.Errors;
using NGit.Storage.Pack;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.Pack
{
	internal class DeltaWindow
	{
		private const int NEXT_RES = 0;

		private const int NEXT_SRC = 1;

		private readonly PackConfig config;

		private readonly DeltaCache deltaCache;

		private readonly ObjectReader reader;

		private readonly DeltaWindowEntry[] window;

		/// <summary>Maximum number of bytes to admit to the window at once.</summary>
		/// <remarks>Maximum number of bytes to admit to the window at once.</remarks>
		private readonly long maxMemory;

		/// <summary>Maximum depth we should create for any delta chain.</summary>
		/// <remarks>Maximum depth we should create for any delta chain.</remarks>
		private readonly int maxDepth;

		/// <summary>Amount of memory we have loaded right now.</summary>
		/// <remarks>Amount of memory we have loaded right now.</remarks>
		private long loaded;

		/// <summary>
		/// Position of
		/// <see cref="res">res</see>
		/// within
		/// <see cref="window">window</see>
		/// array.
		/// </summary>
		private int resSlot;

		/// <summary>Maximum delta chain depth the current object can have.</summary>
		/// <remarks>
		/// Maximum delta chain depth the current object can have.
		/// <p>
		/// This can be smaller than
		/// <see cref="maxDepth">maxDepth</see>
		/// .
		/// </remarks>
		private int resMaxDepth;

		/// <summary>Window entry of the object we are currently considering.</summary>
		/// <remarks>Window entry of the object we are currently considering.</remarks>
		private DeltaWindowEntry res;

		/// <summary>
		/// If we have a delta for
		/// <see cref="res">res</see>
		/// , this is the shortest found yet.
		/// </summary>
		private TemporaryBuffer.Heap bestDelta;

		/// <summary>
		/// If we have
		/// <see cref="bestDelta">bestDelta</see>
		/// , the window position it was created by.
		/// </summary>
		private int bestSlot;

		/// <summary>Used to compress cached deltas.</summary>
		/// <remarks>Used to compress cached deltas.</remarks>
		private ICSharpCode.SharpZipLib.Zip.Compression.Deflater deflater;

		internal DeltaWindow(PackConfig pc, DeltaCache dc, ObjectReader or)
		{
			// The object we are currently considering needs a lot of state:
			config = pc;
			deltaCache = dc;
			reader = or;
			// C Git increases the window size supplied by the user by 1.
			// We don't know why it does this, but if the user asks for
			// window=10, it actually processes with window=11. Because
			// the window size has the largest direct impact on the final
			// pack file size, we match this odd behavior here to give us
			// a better chance of producing a similar sized pack as C Git.
			//
			// We would prefer to directly honor the user's request since
			// PackWriter has a minimum of 2 for the window size, but then
			// users might complain that JGit is creating a bigger pack file.
			//
			window = new DeltaWindowEntry[config.GetDeltaSearchWindowSize() + 1];
			for (int i = 0; i < window.Length; i++)
			{
				window[i] = new DeltaWindowEntry();
			}
			maxMemory = config.GetDeltaSearchMemoryLimit();
			maxDepth = config.GetMaxDeltaDepth();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Search(ProgressMonitor monitor, ObjectToPack[] toSearch, int
			 off, int cnt)
		{
			try
			{
				for (int end = off + cnt; off < end; off++)
				{
					res = window[resSlot];
					if (0 < maxMemory)
					{
						Clear(res);
						int tail = Next(resSlot);
						long need = EstimateSize(toSearch[off]);
						while (maxMemory < loaded + need && tail != resSlot)
						{
							Clear(window[tail]);
							tail = Next(tail);
						}
					}
					res.Set(toSearch[off]);
					if (res.@object.IsEdge())
					{
						// We don't actually want to make a delta for
						// them, just need to push them into the window
						// so they can be read by other objects.
						//
						KeepInWindow();
					}
					else
					{
						// Search for a delta for the current window slot.
						//
						monitor.Update(1);
						Search();
					}
				}
			}
			finally
			{
				if (deflater != null)
				{
					deflater.Finish();
				}
			}
		}

		private static long EstimateSize(ObjectToPack ent)
		{
			return DeltaIndex.EstimateIndexSize(ent.GetWeight());
		}

		private void Clear(DeltaWindowEntry ent)
		{
			if (ent.index != null)
			{
				loaded -= ent.index.GetIndexSize();
			}
			else
			{
				if (res.buffer != null)
				{
					loaded -= ent.buffer.Length;
				}
			}
			ent.Set(null);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Search()
		{
			// TODO(spearce) If the object is used as a base for other
			// objects in this pack we should limit the depth we create
			// for ourselves to be the remainder of our longest dependent
			// chain and the configured maximum depth. This can happen
			// when the dependents are being reused out a pack, but we
			// cannot be because we are near the edge of a thin pack.
			//
			resMaxDepth = maxDepth;
			// Loop through the window backwards, considering every entry.
			// This lets us look at the bigger objects that came before.
			//
			for (int srcSlot = Prior(resSlot); srcSlot != resSlot; srcSlot = Prior(srcSlot))
			{
				DeltaWindowEntry src = window[srcSlot];
				if (src.Empty())
				{
					break;
				}
				if (Delta(src, srcSlot) == NEXT_RES)
				{
					bestDelta = null;
					return;
				}
			}
			// We couldn't find a suitable delta for this object, but it may
			// still be able to act as a base for another one.
			//
			if (bestDelta == null)
			{
				KeepInWindow();
				return;
			}
			// Select this best matching delta as the base for the object.
			//
			ObjectToPack srcObj = window[bestSlot].@object;
			ObjectToPack resObj = res.@object;
			if (srcObj.IsEdge())
			{
				// The source (the delta base) is an edge object outside of the
				// pack. Its part of the common base set that the peer already
				// has on hand, so we don't want to send it. We have to store
				// an ObjectId and *NOT* an ObjectToPack for the base to ensure
				// the base isn't included in the outgoing pack file.
				//
				resObj.SetDeltaBase(srcObj.Copy());
			}
			else
			{
				// The base is part of the pack we are sending, so it should be
				// a direct pointer to the base.
				//
				resObj.SetDeltaBase(srcObj);
			}
			resObj.SetDeltaDepth(srcObj.GetDeltaDepth() + 1);
			resObj.ClearReuseAsIs();
			CacheDelta(srcObj, resObj);
			// Discard the cached best result, otherwise it leaks.
			//
			bestDelta = null;
			// If this should be the end of a chain, don't keep
			// it in the window. Just move on to the next object.
			//
			if (resObj.GetDeltaDepth() == maxDepth)
			{
				return;
			}
			ShuffleBaseUpInPriority();
			KeepInWindow();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int Delta(DeltaWindowEntry src, int srcSlot)
		{
			// Objects must use only the same type as their delta base.
			// If we are looking at something where that isn't true we
			// have exhausted everything of the correct type and should
			// move on to the next thing to examine.
			//
			if (src.Type() != res.Type())
			{
				KeepInWindow();
				return NEXT_RES;
			}
			// Only consider a source with a short enough delta chain.
			if (src.Depth() > resMaxDepth)
			{
				return NEXT_SRC;
			}
			// Estimate a reasonable upper limit on delta size.
			int msz = DeltaSizeLimit(res, resMaxDepth, src);
			if (msz <= 8)
			{
				return NEXT_SRC;
			}
			// If we have to insert a lot to make this work, find another.
			if (res.Size() - src.Size() > msz)
			{
				return NEXT_SRC;
			}
			// If the sizes are radically different, this is a bad pairing.
			if (res.Size() < src.Size() / 16)
			{
				return NEXT_SRC;
			}
			DeltaIndex srcIndex;
			try
			{
				srcIndex = Index(src);
			}
			catch (LargeObjectException)
			{
				// If the source is too big to work on, skip it.
				DropFromWindow(srcSlot);
				return NEXT_SRC;
			}
			catch (IOException notAvailable)
			{
				if (src.@object.IsEdge())
				{
					// This is an edge that is suddenly not available.
					DropFromWindow(srcSlot);
					return NEXT_SRC;
				}
				else
				{
					throw;
				}
			}
			byte[] resBuf;
			try
			{
				resBuf = Buffer(res);
			}
			catch (LargeObjectException)
			{
				// If its too big, move on to another item.
				return NEXT_RES;
			}
			// If we already have a delta for the current object, abort
			// encoding early if this new pairing produces a larger delta.
			if (bestDelta != null && bestDelta.Length() < msz)
			{
				msz = (int)bestDelta.Length();
			}
			TemporaryBuffer.Heap delta = new TemporaryBuffer.Heap(msz);
			try
			{
				if (!srcIndex.Encode(delta, resBuf, msz))
				{
					return NEXT_SRC;
				}
			}
			catch (IOException)
			{
				// This only happens when the heap overflows our limit.
				return NEXT_SRC;
			}
			if (IsBetterDelta(src, delta))
			{
				bestDelta = delta;
				bestSlot = srcSlot;
			}
			return NEXT_SRC;
		}

		private void CacheDelta(ObjectToPack srcObj, ObjectToPack resObj)
		{
			if (int.MaxValue < bestDelta.Length())
			{
				return;
			}
			int rawsz = (int)bestDelta.Length();
			if (deltaCache.CanCache(rawsz, srcObj, resObj))
			{
				try
				{
					byte[] zbuf = new byte[DeflateBound(rawsz)];
					DeltaWindow.ZipStream zs = new DeltaWindow.ZipStream(Deflater(), zbuf);
					bestDelta.WriteTo(zs, null);
					bestDelta = null;
					int len = zs.Finish();
					resObj.SetCachedDelta(deltaCache.Cache(zbuf, len, rawsz));
					resObj.SetCachedSize(rawsz);
				}
				catch (IOException)
				{
					deltaCache.Credit(rawsz);
				}
				catch (OutOfMemoryException)
				{
					deltaCache.Credit(rawsz);
				}
			}
		}

		private static int DeflateBound(int insz)
		{
			return insz + ((insz + 7) >> 3) + ((insz + 63) >> 6) + 11;
		}

		private void ShuffleBaseUpInPriority()
		{
			// Shuffle the entire window so that the best match we just used
			// is at our current index, and our current object is at the index
			// before it. Slide any entries in between to make space.
			//
			window[resSlot] = window[bestSlot];
			DeltaWindowEntry next = res;
			int slot = Prior(resSlot);
			for (; slot != bestSlot; slot = Prior(slot))
			{
				DeltaWindowEntry e = window[slot];
				window[slot] = next;
				next = e;
			}
			window[slot] = next;
		}

		private void KeepInWindow()
		{
			resSlot = Next(resSlot);
		}

		private int Next(int slot)
		{
			if (++slot == window.Length)
			{
				return 0;
			}
			return slot;
		}

		private int Prior(int slot)
		{
			if (slot == 0)
			{
				return window.Length - 1;
			}
			return slot - 1;
		}

		private void DropFromWindow(int srcSlot)
		{
		}

		// We should drop the current source entry from the window,
		// it is somehow invalid for us to work with.
		private bool IsBetterDelta(DeltaWindowEntry src, TemporaryBuffer.Heap resDelta)
		{
			if (bestDelta == null)
			{
				return true;
			}
			// If both delta sequences are the same length, use the one
			// that has a shorter delta chain since it would be faster
			// to access during reads.
			//
			if (resDelta.Length() == bestDelta.Length())
			{
				return src.Depth() < window[bestSlot].Depth();
			}
			return resDelta.Length() < bestDelta.Length();
		}

		private static int DeltaSizeLimit(DeltaWindowEntry res, int maxDepth, DeltaWindowEntry
			 src)
		{
			// Ideally the delta is at least 50% of the original size,
			// but we also want to account for delta header overhead in
			// the pack file (to point to the delta base) so subtract off
			// some of those header bytes from the limit.
			//
			int limit = res.Size() / 2 - 20;
			// Distribute the delta limit over the entire chain length.
			// This is weighted such that deeper items in the chain must
			// be even smaller than if they were earlier in the chain, as
			// they cost significantly more to unpack due to the increased
			// number of recursive unpack calls.
			//
			int remainingDepth = maxDepth - src.Depth();
			return (limit * remainingDepth) / maxDepth;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.LargeObjectException"></exception>
		private DeltaIndex Index(DeltaWindowEntry ent)
		{
			DeltaIndex idx = ent.index;
			if (idx == null)
			{
				try
				{
					idx = new DeltaIndex(Buffer(ent));
				}
				catch (OutOfMemoryException noMemory)
				{
					LargeObjectException.OutOfMemory e;
					e = new LargeObjectException.OutOfMemory(noMemory);
					e.SetObjectId(ent.@object);
					throw e;
				}
				if (0 < maxMemory)
				{
					loaded += idx.GetIndexSize() - idx.GetSourceSize();
				}
				ent.index = idx;
			}
			return idx;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.LargeObjectException"></exception>
		private byte[] Buffer(DeltaWindowEntry ent)
		{
			byte[] buf = ent.buffer;
			if (buf == null)
			{
				buf = PackWriter.Buffer(config, reader, ent.@object);
				if (0 < maxMemory)
				{
					loaded += buf.Length;
				}
				ent.buffer = buf;
			}
			return buf;
		}

		private ICSharpCode.SharpZipLib.Zip.Compression.Deflater Deflater()
		{
			if (deflater == null)
			{
				deflater = new ICSharpCode.SharpZipLib.Zip.Compression.Deflater(config.GetCompressionLevel
					());
			}
			else
			{
				deflater.Reset();
			}
			return deflater;
		}

		internal sealed class ZipStream : OutputStream
		{
			private readonly Deflater deflater;

			private readonly byte[] zbuf;

			private int outPtr;

			internal ZipStream(Deflater deflater, byte[] zbuf)
			{
				this.deflater = deflater;
				this.zbuf = zbuf;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal int Finish()
			{
				deflater.Finish();
				for (; ; )
				{
					if (outPtr == zbuf.Length)
					{
						throw new EOFException();
					}
					int n = deflater.Deflate(zbuf, outPtr, zbuf.Length - outPtr);
					if (n == 0)
					{
						if (deflater.IsFinished)
						{
							return outPtr;
						}
						throw new IOException();
					}
					outPtr += n;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(byte[] b, int off, int len)
			{
				deflater.SetInput(b, off, len);
				for (; ; )
				{
					if (outPtr == zbuf.Length)
					{
						throw new EOFException();
					}
					int n = deflater.Deflate(zbuf, outPtr, zbuf.Length - outPtr);
					if (n == 0)
					{
						if (deflater.IsNeedingInput)
						{
							break;
						}
						throw new IOException();
					}
					outPtr += n;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(int b)
			{
				throw new NotSupportedException();
			}
		}
	}
}
