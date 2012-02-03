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
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib;
using NGit;
using NGit.Errors;
using NGit.Revwalk;
using NGit.Storage.File;
using NGit.Storage.Pack;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>Active handle to a ByteWindow.</summary>
	/// <remarks>Active handle to a ByteWindow.</remarks>
	internal sealed class WindowCursor : ObjectReader, ObjectReuseAsIs
	{
		/// <summary>Temporary buffer large enough for at least one raw object id.</summary>
		/// <remarks>Temporary buffer large enough for at least one raw object id.</remarks>
		internal readonly byte[] tempId = new byte[Constants.OBJECT_ID_LENGTH];

		private ICSharpCode.SharpZipLib.Zip.Compression.Inflater inf;

		private ByteWindow window;

		private DeltaBaseCache baseCache;

		internal readonly FileObjectDatabase db;

		internal WindowCursor(FileObjectDatabase db)
		{
			this.db = db;
		}

		internal DeltaBaseCache GetDeltaBaseCache()
		{
			if (baseCache == null)
			{
				baseCache = new DeltaBaseCache();
			}
			return baseCache;
		}

		public override ObjectReader NewReader()
		{
			return new NGit.Storage.File.WindowCursor(db);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override ICollection<ObjectId> Resolve(AbbreviatedObjectId id)
		{
			if (id.IsComplete)
			{
				return Sharpen.Collections.Singleton(id.ToObjectId());
			}
			HashSet<ObjectId> matches = new HashSet<ObjectId>();
			db.Resolve(matches, id);
			return matches;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool Has(AnyObjectId objectId)
		{
			return db.Has(objectId);
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override ObjectLoader Open(AnyObjectId objectId, int typeHint)
		{
			ObjectLoader ldr = db.OpenObject(this, objectId);
			if (ldr == null)
			{
				if (typeHint == OBJ_ANY)
				{
					throw new MissingObjectException(objectId.Copy(), "unknown");
				}
				throw new MissingObjectException(objectId.Copy(), typeHint);
			}
			if (typeHint != OBJ_ANY && ldr.GetType() != typeHint)
			{
				throw new IncorrectObjectTypeException(objectId.Copy(), typeHint);
			}
			return ldr;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override long GetObjectSize(AnyObjectId objectId, int typeHint)
		{
			long sz = db.GetObjectSize(this, objectId);
			if (sz < 0)
			{
				if (typeHint == OBJ_ANY)
				{
					throw new MissingObjectException(objectId.Copy(), "unknown");
				}
				throw new MissingObjectException(objectId.Copy(), typeHint);
			}
			return sz;
		}

		public ObjectToPack NewObjectToPack(RevObject obj)
		{
			return new LocalObjectToPack(obj);
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		public void SelectObjectRepresentation(PackWriter packer, ProgressMonitor monitor
			, Iterable<ObjectToPack> objects)
		{
			foreach (ObjectToPack otp in objects)
			{
				db.SelectObjectRepresentation(packer, otp, this);
				monitor.Update(1);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.StoredObjectRepresentationNotAvailableException"></exception>
		public void CopyObjectAsIs(PackOutputStream @out, ObjectToPack otp, bool validate
			)
		{
			LocalObjectToPack src = (LocalObjectToPack)otp;
			src.pack.CopyAsIs(@out, src, validate, this);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public void WriteObjects(PackOutputStream @out, IList<ObjectToPack> list)
		{
			foreach (ObjectToPack otp in list)
			{
				@out.WriteObject(otp);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public ICollection<CachedPack> GetCachedPacks()
		{
			return (ICollection<CachedPack>)db.GetCachedPacks();
		}

		/// <summary>Copy bytes from the window to a caller supplied buffer.</summary>
		/// <remarks>Copy bytes from the window to a caller supplied buffer.</remarks>
		/// <param name="pack">the file the desired window is stored within.</param>
		/// <param name="position">position within the file to read from.</param>
		/// <param name="dstbuf">destination buffer to copy into.</param>
		/// <param name="dstoff">offset within <code>dstbuf</code> to start copying into.</param>
		/// <param name="cnt">
		/// number of bytes to copy. This value may exceed the number of
		/// bytes remaining in the window starting at offset
		/// <code>pos</code>.
		/// </param>
		/// <returns>
		/// number of bytes actually copied; this may be less than
		/// <code>cnt</code> if <code>cnt</code> exceeded the number of bytes
		/// available.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// this cursor does not match the provider or id and the proper
		/// window could not be acquired through the provider's cache.
		/// </exception>
		internal int Copy(PackFile pack, long position, byte[] dstbuf, int dstoff, int cnt
			)
		{
			long length = pack.length;
			int need = cnt;
			while (need > 0 && position < length)
			{
				Pin(pack, position);
				int r = window.Copy(position, dstbuf, dstoff, need);
				position += r;
				dstoff += r;
				need -= r;
			}
			return cnt - need;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public void CopyPackAsIs(PackOutputStream @out, CachedPack pack, bool validate)
		{
			((LocalCachedPack)pack).CopyAsIs(@out, validate, this);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal void CopyPackAsIs(PackFile pack, long length, bool validate, PackOutputStream
			 @out)
		{
			MessageDigest md = null;
			if (validate)
			{
				md = Constants.NewMessageDigest();
				byte[] buf = @out.GetCopyBuffer();
				Pin(pack, 0);
				if (window.Copy(0, buf, 0, 12) != 12)
				{
					pack.SetInvalid();
					throw new IOException(JGitText.Get().packfileIsTruncated);
				}
				md.Update(buf, 0, 12);
			}
			long position = 12;
			long remaining = length - (12 + 20);
			while (0 < remaining)
			{
				Pin(pack, position);
				int ptr = (int)(position - window.start);
				int n = (int)Math.Min(window.Size() - ptr, remaining);
				window.Write(@out, position, n, md);
				position += n;
				remaining -= n;
			}
			if (md != null)
			{
				byte[] buf = new byte[20];
				byte[] actHash = md.Digest();
				Pin(pack, position);
				if (window.Copy(position, buf, 0, 20) != 20)
				{
					pack.SetInvalid();
					throw new IOException(JGitText.Get().packfileIsTruncated);
				}
				if (!Arrays.Equals(actHash, buf))
				{
					pack.SetInvalid();
					throw new IOException(MessageFormat.Format(JGitText.Get().packfileCorruptionDetected
						, pack.GetPackFile().GetPath()));
				}
			}
		}

		/// <summary>
		/// Inflate a region of the pack starting at
		/// <code>position</code>
		/// .
		/// </summary>
		/// <param name="pack">the file the desired window is stored within.</param>
		/// <param name="position">position within the file to read from.</param>
		/// <param name="dstbuf">
		/// destination buffer the inflater should output decompressed
		/// data to.
		/// </param>
		/// <param name="dstoff">current offset within <code>dstbuf</code> to inflate into.</param>
		/// <returns>
		/// updated <code>dstoff</code> based on the number of bytes
		/// successfully inflated into <code>dstbuf</code>.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// this cursor does not match the provider or id and the proper
		/// window could not be acquired through the provider's cache.
		/// </exception>
		/// <exception cref="ICSharpCode.SharpZipLib.SharpZipBaseException">
		/// the inflater encountered an invalid chunk of data. Data
		/// stream corruption is likely.
		/// </exception>
		internal int Inflate(PackFile pack, long position, byte[] dstbuf, int dstoff)
		{
			PrepareInflater();
			Pin(pack, position);
			position += window.SetInput(position, inf);
			do
			{
				int n = inf.Inflate(dstbuf, dstoff, dstbuf.Length - dstoff);
				if (n == 0)
				{
					if (inf.IsNeedingInput)
					{
						Pin(pack, position);
						position += window.SetInput(position, inf);
					}
					else
					{
						if (inf.IsFinished || (dstbuf.Length - dstoff) == 0)
						{
							return dstoff;
						}
						else
						{
							throw new SharpZipBaseException();
						}
					}
				}
				dstoff += n;
			}
			while (dstoff < dstbuf.Length);
			return dstoff;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal ByteArrayWindow QuickCopy(PackFile p, long pos, long cnt)
		{
			Pin(p, pos);
			if (window is ByteArrayWindow && window.Contains(p, pos + (cnt - 1)))
			{
				return (ByteArrayWindow)window;
			}
			return null;
		}

		internal ICSharpCode.SharpZipLib.Zip.Compression.Inflater Inflater()
		{
			PrepareInflater();
			return inf;
		}

		private void PrepareInflater()
		{
			if (inf == null)
			{
				inf = InflaterCache.Get();
			}
			else
			{
				inf.Reset();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal void Pin(PackFile pack, long position)
		{
			ByteWindow w = window;
			if (w == null || !w.Contains(pack, position))
			{
				// If memory is low, we may need what is in our window field to
				// be cleaned up by the GC during the get for the next window.
				// So we always clear it, even though we are just going to set
				// it again.
				//
				window = null;
				window = WindowCache.Get(pack, position);
			}
		}

		internal int GetStreamFileThreshold()
		{
			return WindowCache.GetStreamFileThreshold();
		}

		/// <summary>Release the current window cursor.</summary>
		/// <remarks>Release the current window cursor.</remarks>
		public override void Release()
		{
			window = null;
			baseCache = null;
			try
			{
				InflaterCache.Release(inf);
			}
			finally
			{
				inf = null;
			}
		}
	}
}
