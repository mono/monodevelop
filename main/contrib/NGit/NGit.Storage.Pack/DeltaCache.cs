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

using NGit.Storage.Pack;
using Sharpen;

namespace NGit.Storage.Pack
{
	internal class DeltaCache
	{
		private readonly long size;

		private readonly int entryLimit;

		private readonly ReferenceQueue<byte[]> queue;

		private long used;

		internal DeltaCache(PackConfig pc)
		{
			size = pc.GetDeltaCacheSize();
			entryLimit = pc.GetDeltaCacheLimit();
			queue = new ReferenceQueue<byte[]>();
		}

		internal virtual bool CanCache(int length, ObjectToPack src, ObjectToPack res)
		{
			// If the cache would overflow, don't store.
			//
			if (0 < size && size < used + length)
			{
				CheckForGarbageCollectedObjects();
				if (0 < size && size < used + length)
				{
					return false;
				}
			}
			if (length < entryLimit)
			{
				used += length;
				return true;
			}
			// If the combined source files are multiple megabytes but the delta
			// is on the order of a kilobyte or two, this was likely costly to
			// construct. Cache it anyway, even though its over the limit.
			//
			if (length >> 10 < (src.GetWeight() >> 20) + (res.GetWeight() >> 21))
			{
				used += length;
				return true;
			}
			return false;
		}

		internal virtual void Credit(int reservedSize)
		{
			used -= reservedSize;
		}

		internal virtual DeltaCache.Ref Cache(byte[] data, int actLen, int reservedSize)
		{
			// The caller may have had to allocate more space than is
			// required. If we are about to waste anything, shrink it.
			//
			data = Resize(data, actLen);
			// When we reserved space for this item we did it for the
			// inflated size of the delta, but we were just given the
			// compressed version. Adjust the cache cost to match.
			//
			if (reservedSize != data.Length)
			{
				used -= reservedSize;
				used += data.Length;
			}
			return new DeltaCache.Ref(data, queue);
		}

		internal virtual byte[] Resize(byte[] data, int actLen)
		{
			if (data.Length != actLen)
			{
				byte[] nbuf = new byte[actLen];
				System.Array.Copy(data, 0, nbuf, 0, actLen);
				data = nbuf;
			}
			return data;
		}

		private void CheckForGarbageCollectedObjects()
		{
			DeltaCache.Ref r;
			while ((r = (DeltaCache.Ref)queue.Poll()) != null)
			{
				used -= r.cost;
			}
		}

		internal class Ref : SoftReference<byte[]>
		{
			internal readonly int cost;

			internal Ref(byte[] array, ReferenceQueue<byte[]> queue) : base(array, queue)
			{
				cost = array.Length;
			}
		}
	}
}
