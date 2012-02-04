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

using System.Text;
using NGit;
using NGit.Revwalk;
using NGit.Storage.Pack;
using NGit.Transport;
using Sharpen;

namespace NGit.Storage.Pack
{
	/// <summary>
	/// Per-object state used by
	/// <see cref="PackWriter">PackWriter</see>
	/// .
	/// <p>
	/// <code>PackWriter</code>
	/// uses this class to track the things it needs to include in
	/// the newly generated pack file, and how to efficiently obtain the raw data for
	/// each object as they are written to the output stream.
	/// </summary>
	[System.Serializable]
	public class ObjectToPack : PackedObjectInfo
	{
		private const int WANT_WRITE = 1 << 0;

		private const int REUSE_AS_IS = 1 << 1;

		private const int DO_NOT_DELTA = 1 << 2;

		private const int EDGE = 1 << 3;

		private const int TYPE_SHIFT = 5;

		private const int EXT_SHIFT = 8;

		private const int EXT_MASK = unchecked((int)(0xf));

		private const int DELTA_SHIFT = 12;

		private const int NON_EXT_MASK = ~(EXT_MASK << EXT_SHIFT);

		private const int NON_DELTA_MASK = unchecked((int)(0xfff));

		/// <summary>Other object being packed that this will delta against.</summary>
		/// <remarks>Other object being packed that this will delta against.</remarks>
		private ObjectId deltaBase;

		/// <summary>
		/// Bit field, from bit 0 to bit 31:
		/// <ul>
		/// <li>1 bit: wantWrite</li>
		/// <li>1 bit: canReuseAsIs</li>
		/// <li>1 bit: doNotDelta</li>
		/// <li>1 bit: edgeObject</li>
		/// <li>1 bit: unused</li>
		/// <li>3 bits: type</li>
		/// <li>4 bits: subclass flags (if any)</li>
		/// <li>--</li>
		/// <li>20 bits: deltaDepth</li>
		/// </ul>
		/// </summary>
		private int flags;

		/// <summary>Hash of the object's tree path.</summary>
		/// <remarks>Hash of the object's tree path.</remarks>
		private int pathHash;

		/// <summary>If present, deflated delta instruction stream for this object.</summary>
		/// <remarks>If present, deflated delta instruction stream for this object.</remarks>
		private DeltaCache.Ref cachedDelta;

		/// <summary>Construct for the specified object id.</summary>
		/// <remarks>Construct for the specified object id.</remarks>
		/// <param name="src">object id of object for packing</param>
		/// <param name="type">real type code of the object, not its in-pack type.</param>
		public ObjectToPack(AnyObjectId src, int type) : base(src)
		{
			flags = type << TYPE_SHIFT;
		}

		/// <summary>Construct for the specified object.</summary>
		/// <remarks>Construct for the specified object.</remarks>
		/// <param name="obj">
		/// identity of the object that will be packed. The object's
		/// parsed status is undefined here. Implementers must not rely on
		/// the object being parsed.
		/// </param>
		public ObjectToPack(RevObject obj) : this(obj, obj.Type)
		{
		}

		/// <returns>
		/// delta base object id if object is going to be packed in delta
		/// representation; null otherwise - if going to be packed as a
		/// whole object.
		/// </returns>
		public virtual ObjectId GetDeltaBaseId()
		{
			return deltaBase;
		}

		/// <returns>
		/// delta base object to pack if object is going to be packed in
		/// delta representation and delta is specified as object to
		/// pack; null otherwise - if going to be packed as a whole
		/// object or delta base is specified only as id.
		/// </returns>
		public virtual NGit.Storage.Pack.ObjectToPack GetDeltaBase()
		{
			if (deltaBase is NGit.Storage.Pack.ObjectToPack)
			{
				return (NGit.Storage.Pack.ObjectToPack)deltaBase;
			}
			return null;
		}

		/// <summary>Set delta base for the object.</summary>
		/// <remarks>
		/// Set delta base for the object. Delta base set by this method is used
		/// by
		/// <see cref="PackWriter">PackWriter</see>
		/// to write object - determines its representation
		/// in a created pack.
		/// </remarks>
		/// <param name="deltaBase">
		/// delta base object or null if object should be packed as a
		/// whole object.
		/// </param>
		internal virtual void SetDeltaBase(ObjectId deltaBase)
		{
			this.deltaBase = deltaBase;
		}

		internal virtual void SetCachedDelta(DeltaCache.Ref data)
		{
			cachedDelta = data;
		}

		internal virtual DeltaCache.Ref PopCachedDelta()
		{
			DeltaCache.Ref r = cachedDelta;
			if (r != null)
			{
				cachedDelta = null;
			}
			return r;
		}

		internal virtual void ClearDeltaBase()
		{
			this.deltaBase = null;
			if (cachedDelta != null)
			{
				cachedDelta.Clear();
				cachedDelta.Enqueue();
				cachedDelta = null;
			}
		}

		/// <returns>
		/// true if object is going to be written as delta; false
		/// otherwise.
		/// </returns>
		public virtual bool IsDeltaRepresentation()
		{
			return deltaBase != null;
		}

		/// <summary>Check if object is already written in a pack.</summary>
		/// <remarks>
		/// Check if object is already written in a pack. This information is
		/// used to achieve delta-base precedence in a pack file.
		/// </remarks>
		/// <returns>true if object is already written; false otherwise.</returns>
		public virtual bool IsWritten()
		{
			return GetOffset() != 0;
		}

		/// <returns>the type of this object.</returns>
		public virtual int GetType()
		{
			return (flags >> TYPE_SHIFT) & unchecked((int)(0x7));
		}

		internal virtual int GetDeltaDepth()
		{
			return (int)(((uint)flags) >> DELTA_SHIFT);
		}

		internal virtual void SetDeltaDepth(int d)
		{
			flags = (d << DELTA_SHIFT) | (flags & NON_DELTA_MASK);
		}

		internal virtual bool WantWrite()
		{
			return (flags & WANT_WRITE) != 0;
		}

		internal virtual void MarkWantWrite()
		{
			flags |= WANT_WRITE;
		}

		/// <returns>
		/// true if an existing representation was selected to be reused
		/// as-is into the pack stream.
		/// </returns>
		public virtual bool IsReuseAsIs()
		{
			return (flags & REUSE_AS_IS) != 0;
		}

		internal virtual void SetReuseAsIs()
		{
			flags |= REUSE_AS_IS;
		}

		/// <summary>Forget the reuse information previously stored.</summary>
		/// <remarks>
		/// Forget the reuse information previously stored.
		/// <p>
		/// Implementations may subclass this method, but they must also invoke the
		/// super version with
		/// <code>super.clearReuseAsIs()</code>
		/// to ensure the flag is
		/// properly cleared for the writer.
		/// </remarks>
		protected internal virtual void ClearReuseAsIs()
		{
			flags &= ~REUSE_AS_IS;
		}

		internal virtual bool IsDoNotDelta()
		{
			return (flags & DO_NOT_DELTA) != 0;
		}

		internal virtual void SetDoNotDelta(bool noDelta)
		{
			if (noDelta)
			{
				flags |= DO_NOT_DELTA;
			}
			else
			{
				flags &= ~DO_NOT_DELTA;
			}
		}

		internal virtual bool IsEdge()
		{
			return (flags & EDGE) != 0;
		}

		internal virtual void SetEdge()
		{
			flags |= EDGE;
		}

		/// <returns>the extended flags on this object, in the range [0x0, 0xf].</returns>
		protected internal virtual int GetExtendedFlags()
		{
			return ((int)(((uint)flags) >> EXT_SHIFT)) & EXT_MASK;
		}

		/// <summary>Determine if a particular extended flag bit has been set.</summary>
		/// <remarks>
		/// Determine if a particular extended flag bit has been set.
		/// This implementation may be faster than calling
		/// <see cref="GetExtendedFlags()">GetExtendedFlags()</see>
		/// and testing the result.
		/// </remarks>
		/// <param name="flag">the flag mask to test, must be between 0x0 and 0xf.</param>
		/// <returns>true if any of the bits matching the mask are non-zero.</returns>
		protected internal virtual bool IsExtendedFlag(int flag)
		{
			return (flags & (flag << EXT_SHIFT)) != 0;
		}

		/// <summary>Set an extended flag bit.</summary>
		/// <remarks>
		/// Set an extended flag bit.
		/// This implementation is more efficient than getting the extended flags,
		/// adding the bit, and setting them all back.
		/// </remarks>
		/// <param name="flag">the bits to set, must be between 0x0 and 0xf.</param>
		protected internal virtual void SetExtendedFlag(int flag)
		{
			flags |= (flag & EXT_MASK) << EXT_SHIFT;
		}

		/// <summary>Clear an extended flag bit.</summary>
		/// <remarks>
		/// Clear an extended flag bit.
		/// This implementation is more efficient than getting the extended flags,
		/// removing the bit, and setting them all back.
		/// </remarks>
		/// <param name="flag">the bits to clear, must be between 0x0 and 0xf.</param>
		protected internal virtual void ClearExtendedFlag(int flag)
		{
			flags &= ~((flag & EXT_MASK) << EXT_SHIFT);
		}

		/// <summary>Set the extended flags used by the subclass.</summary>
		/// <remarks>
		/// Set the extended flags used by the subclass.
		/// Subclass implementations may store up to 4 bits of information inside of
		/// the internal flags field already used by the base ObjectToPack instance.
		/// </remarks>
		/// <param name="extFlags">
		/// additional flag bits to store in the flags field. Due to space
		/// constraints only values [0x0, 0xf] are permitted.
		/// </param>
		protected internal virtual void SetExtendedFlags(int extFlags)
		{
			flags = ((extFlags & EXT_MASK) << EXT_SHIFT) | (flags & NON_EXT_MASK);
		}

		internal virtual int GetFormat()
		{
			if (IsReuseAsIs())
			{
				if (IsDeltaRepresentation())
				{
					return StoredObjectRepresentation.PACK_DELTA;
				}
				return StoredObjectRepresentation.PACK_WHOLE;
			}
			return StoredObjectRepresentation.FORMAT_OTHER;
		}

		// Overload weight into CRC since we don't need them at the same time.
		internal virtual int GetWeight()
		{
			return GetCRC();
		}

		internal virtual void SetWeight(int weight)
		{
			SetCRC(weight);
		}

		internal virtual int GetPathHash()
		{
			return pathHash;
		}

		internal virtual void SetPathHash(int hc)
		{
			pathHash = hc;
		}

		internal virtual int GetCachedSize()
		{
			return pathHash;
		}

		internal virtual void SetCachedSize(int sz)
		{
			pathHash = sz;
		}

		/// <summary>Remember a specific representation for reuse at a later time.</summary>
		/// <remarks>
		/// Remember a specific representation for reuse at a later time.
		/// <p>
		/// Implementers should remember the representation chosen, so it can be
		/// reused at a later time.
		/// <see cref="PackWriter">PackWriter</see>
		/// may invoke this method
		/// multiple times for the same object, each time saving the current best
		/// representation found.
		/// </remarks>
		/// <param name="ref">the object representation.</param>
		public virtual void Select(StoredObjectRepresentation @ref)
		{
		}

		// Empty by default.
		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			buf.Append("ObjectToPack[");
			buf.Append(Constants.TypeString(GetType()));
			buf.Append(" ");
			buf.Append(Name);
			if (WantWrite())
			{
				buf.Append(" wantWrite");
			}
			if (IsReuseAsIs())
			{
				buf.Append(" reuseAsIs");
			}
			if (IsDoNotDelta())
			{
				buf.Append(" doNotDelta");
			}
			if (IsEdge())
			{
				buf.Append(" edge");
			}
			if (GetDeltaDepth() > 0)
			{
				buf.Append(" depth=" + GetDeltaDepth());
			}
			if (IsDeltaRepresentation())
			{
				if (GetDeltaBase() != null)
				{
					buf.Append(" base=inpack:" + GetDeltaBase().Name);
				}
				else
				{
					buf.Append(" base=edge:" + GetDeltaBaseId().Name);
				}
			}
			if (IsWritten())
			{
				buf.Append(" offset=" + GetOffset());
			}
			buf.Append("]");
			return buf.ToString();
		}
	}
}
