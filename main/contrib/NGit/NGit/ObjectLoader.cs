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
using NGit;
using NGit.Errors;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>Base class for a set of loaders for different representations of Git objects.
	/// 	</summary>
	/// <remarks>
	/// Base class for a set of loaders for different representations of Git objects.
	/// New loaders are constructed for every object.
	/// </remarks>
	public abstract class ObjectLoader
	{
		/// <returns>
		/// Git in pack object type, see
		/// <see cref="Constants">Constants</see>
		/// .
		/// </returns>
		public abstract int GetType();

		/// <returns>size of object in bytes</returns>
		public abstract long GetSize();

		/// <returns>
		/// true if this object is too large to obtain as a byte array.
		/// Objects over a certain threshold should be accessed only by their
		/// <see cref="OpenStream()">OpenStream()</see>
		/// to prevent overflowing the JVM heap.
		/// </returns>
		public virtual bool IsLarge()
		{
			try
			{
				GetCachedBytes();
				return false;
			}
			catch (LargeObjectException)
			{
				return true;
			}
		}

		/// <summary>Obtain a copy of the bytes of this object.</summary>
		/// <remarks>
		/// Obtain a copy of the bytes of this object.
		/// <p>
		/// Unlike
		/// <see cref="GetCachedBytes()">GetCachedBytes()</see>
		/// this method returns an array that might
		/// be modified by the caller.
		/// </remarks>
		/// <returns>the bytes of this object.</returns>
		/// <exception cref="NGit.Errors.LargeObjectException">
		/// if the object won't fit into a byte array, because
		/// <see cref="IsLarge()">IsLarge()</see>
		/// returns true. Callers should use
		/// <see cref="OpenStream()">OpenStream()</see>
		/// instead to access the contents.
		/// </exception>
		public byte[] GetBytes()
		{
			return CloneArray(GetCachedBytes());
		}

		/// <summary>Obtain a copy of the bytes of this object.</summary>
		/// <remarks>
		/// Obtain a copy of the bytes of this object.
		/// If the object size is less than or equal to
		/// <code>sizeLimit</code>
		/// this method
		/// will provide it as a byte array, even if
		/// <see cref="IsLarge()">IsLarge()</see>
		/// is true. This
		/// utility is useful for application code that absolutely must have the
		/// object as a single contiguous byte array in memory.
		/// Unlike
		/// <see cref="GetCachedBytes(int)">GetCachedBytes(int)</see>
		/// this method returns an array that
		/// might be modified by the caller.
		/// </remarks>
		/// <param name="sizeLimit">
		/// maximum number of bytes to return. If the object is larger
		/// than this limit,
		/// <see cref="NGit.Errors.LargeObjectException">NGit.Errors.LargeObjectException</see>
		/// will be thrown.
		/// </param>
		/// <returns>the bytes of this object.</returns>
		/// <exception cref="NGit.Errors.LargeObjectException">
		/// if the object is bigger than
		/// <code>sizeLimit</code>
		/// , or if
		/// <see cref="System.OutOfMemoryException">System.OutOfMemoryException</see>
		/// occurs during allocation of the
		/// result array. Callers should use
		/// <see cref="OpenStream()">OpenStream()</see>
		/// instead to access the contents.
		/// </exception>
		/// <exception cref="NGit.Errors.MissingObjectException">the object is large, and it no longer exists.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">the object store cannot be accessed.</exception>
		public byte[] GetBytes(int sizeLimit)
		{
			byte[] cached = GetCachedBytes(sizeLimit);
			try
			{
				return CloneArray(cached);
			}
			catch (OutOfMemoryException tooBig)
			{
				throw new LargeObjectException.OutOfMemory(tooBig);
			}
		}

		/// <summary>Obtain a reference to the (possibly cached) bytes of this object.</summary>
		/// <remarks>
		/// Obtain a reference to the (possibly cached) bytes of this object.
		/// <p>
		/// This method offers direct access to the internal caches, potentially
		/// saving on data copies between the internal cache and higher level code.
		/// Callers who receive this reference <b>must not</b> modify its contents.
		/// Changes (if made) will affect the cache but not the repository itself.
		/// </remarks>
		/// <returns>the cached bytes of this object. Do not modify it.</returns>
		/// <exception cref="NGit.Errors.LargeObjectException">
		/// if the object won't fit into a byte array, because
		/// <see cref="IsLarge()">IsLarge()</see>
		/// returns true. Callers should use
		/// <see cref="OpenStream()">OpenStream()</see>
		/// instead to access the contents.
		/// </exception>
		public abstract byte[] GetCachedBytes();

		/// <summary>Obtain a reference to the (possibly cached) bytes of this object.</summary>
		/// <remarks>
		/// Obtain a reference to the (possibly cached) bytes of this object.
		/// If the object size is less than or equal to
		/// <code>sizeLimit</code>
		/// this method
		/// will provide it as a byte array, even if
		/// <see cref="IsLarge()">IsLarge()</see>
		/// is true. This
		/// utility is useful for application code that absolutely must have the
		/// object as a single contiguous byte array in memory.
		/// This method offers direct access to the internal caches, potentially
		/// saving on data copies between the internal cache and higher level code.
		/// Callers who receive this reference <b>must not</b> modify its contents.
		/// Changes (if made) will affect the cache but not the repository itself.
		/// </remarks>
		/// <param name="sizeLimit">
		/// maximum number of bytes to return. If the object size is
		/// larger than this limit and
		/// <see cref="IsLarge()">IsLarge()</see>
		/// is true,
		/// <see cref="NGit.Errors.LargeObjectException">NGit.Errors.LargeObjectException</see>
		/// will be thrown.
		/// </param>
		/// <returns>the cached bytes of this object. Do not modify it.</returns>
		/// <exception cref="NGit.Errors.LargeObjectException">
		/// if the object is bigger than
		/// <code>sizeLimit</code>
		/// , or if
		/// <see cref="System.OutOfMemoryException">System.OutOfMemoryException</see>
		/// occurs during allocation of the
		/// result array. Callers should use
		/// <see cref="OpenStream()">OpenStream()</see>
		/// instead to access the contents.
		/// </exception>
		/// <exception cref="NGit.Errors.MissingObjectException">the object is large, and it no longer exists.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">the object store cannot be accessed.</exception>
		public virtual byte[] GetCachedBytes(int sizeLimit)
		{
			if (!IsLarge())
			{
				return GetCachedBytes();
			}
			ObjectStream @in = OpenStream();
			try
			{
				long sz = @in.GetSize();
				if (sizeLimit < sz)
				{
					throw new LargeObjectException.ExceedsLimit(sizeLimit, sz);
				}
				if (int.MaxValue < sz)
				{
					throw new LargeObjectException.ExceedsByteArrayLimit();
				}
				byte[] buf;
				try
				{
					buf = new byte[(int)sz];
				}
				catch (OutOfMemoryException notEnoughHeap)
				{
					throw new LargeObjectException.OutOfMemory(notEnoughHeap);
				}
				IOUtil.ReadFully(@in, buf, 0, buf.Length);
				return buf;
			}
			finally
			{
				@in.Close();
			}
		}

		/// <summary>Obtain an input stream to read this object's data.</summary>
		/// <remarks>Obtain an input stream to read this object's data.</remarks>
		/// <returns>
		/// a stream of this object's data. Caller must close the stream when
		/// through with it. The returned stream is buffered with a
		/// reasonable buffer size.
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">the object no longer exists.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">the object store cannot be accessed.</exception>
		public abstract ObjectStream OpenStream();

		/// <summary>Copy this object to the output stream.</summary>
		/// <remarks>
		/// Copy this object to the output stream.
		/// <p>
		/// For some object store implementations, this method may be more efficient
		/// than reading from
		/// <see cref="OpenStream()">OpenStream()</see>
		/// into a temporary byte array, then
		/// writing to the destination stream.
		/// <p>
		/// The default implementation of this method is to copy with a temporary
		/// byte array for large objects, or to pass through the cached byte array
		/// for small objects.
		/// </remarks>
		/// <param name="out">
		/// stream to receive the complete copy of this object's data.
		/// Caller is responsible for flushing or closing this stream
		/// after this method returns.
		/// </param>
		/// <exception cref="NGit.Errors.MissingObjectException">the object no longer exists.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">
		/// the object store cannot be accessed, or the stream cannot be
		/// written to.
		/// </exception>
		public virtual void CopyTo(OutputStream @out)
		{
			if (IsLarge())
			{
				ObjectStream @in = OpenStream();
				try
				{
					long sz = @in.GetSize();
					byte[] tmp = new byte[8192];
					long copied = 0;
					while (copied < sz)
					{
						int n = @in.Read(tmp);
						if (n < 0)
						{
							throw new EOFException();
						}
						@out.Write(tmp, 0, n);
						copied += n;
					}
					if (0 <= @in.Read())
					{
						throw new EOFException();
					}
				}
				finally
				{
					@in.Close();
				}
			}
			else
			{
				@out.Write(GetCachedBytes());
			}
		}

		private static byte[] CloneArray(byte[] data)
		{
			byte[] copy = new byte[data.Length];
			System.Array.Copy(data, 0, copy, 0, data.Length);
			return copy;
		}

		/// <summary>Simple loader around the cached byte array.</summary>
		/// <remarks>
		/// Simple loader around the cached byte array.
		/// <p>
		/// ObjectReader implementations can use this stream type when the object's
		/// content is small enough to be accessed as a single byte array.
		/// </remarks>
		public class SmallObject : ObjectLoader
		{
			private readonly int type;

			private readonly byte[] data;

			/// <summary>Construct a small object loader.</summary>
			/// <remarks>Construct a small object loader.</remarks>
			/// <param name="type">type of the object.</param>
			/// <param name="data">
			/// the object's data array. This array will be returned as-is
			/// for the
			/// <see cref="GetCachedBytes()">GetCachedBytes()</see>
			/// method.
			/// </param>
			public SmallObject(int type, byte[] data)
			{
				this.type = type;
				this.data = data;
			}

			public override int GetType()
			{
				return type;
			}

			public override long GetSize()
			{
				return GetCachedBytes().Length;
			}

			public override bool IsLarge()
			{
				return false;
			}

			public override byte[] GetCachedBytes()
			{
				return data;
			}

			public override ObjectStream OpenStream()
			{
				return new ObjectStream.SmallStream(this);
			}
		}
	}
}
