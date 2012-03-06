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
using NGit.Transport;
using Sharpen;

namespace NGit
{
	/// <summary>
	/// Inserts objects into an existing
	/// <code>ObjectDatabase</code>
	/// .
	/// <p>
	/// An inserter is not thread-safe. Individual threads should each obtain their
	/// own unique inserter instance, or must arrange for locking at a higher level
	/// to ensure the inserter is in use by no more than one thread at a time.
	/// <p>
	/// Objects written by an inserter may not be immediately visible for reading
	/// after the insert method completes. Callers must invoke either
	/// <see cref="Release()">Release()</see>
	/// or
	/// <see cref="Flush()">Flush()</see>
	/// prior to updating references or
	/// otherwise making the returned ObjectIds visible to other code.
	/// </summary>
	public abstract class ObjectInserter
	{
		/// <summary>An inserter that can be used for formatting and id generation only.</summary>
		/// <remarks>An inserter that can be used for formatting and id generation only.</remarks>
		public class Formatter : ObjectInserter
		{
			/// <exception cref="System.IO.IOException"></exception>
			public override ObjectId Insert(int objectType, long length, InputStream @in)
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override PackParser NewPackParser(InputStream @in)
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Flush()
			{
			}

			// Do nothing.
			public override void Release()
			{
			}
			// Do nothing.
		}

		/// <summary>Digest to compute the name of an object.</summary>
		/// <remarks>Digest to compute the name of an object.</remarks>
		private readonly MessageDigest digest;

		/// <summary>Temporary working buffer for streaming data through.</summary>
		/// <remarks>Temporary working buffer for streaming data through.</remarks>
		private byte[] tempBuffer;

		/// <summary>Create a new inserter for a database.</summary>
		/// <remarks>Create a new inserter for a database.</remarks>
		public ObjectInserter()
		{
			digest = Constants.NewMessageDigest();
		}

		/// <returns>a temporary byte array for use by the caller.</returns>
		protected internal virtual byte[] Buffer()
		{
			if (tempBuffer == null)
			{
				tempBuffer = new byte[8192];
			}
			return tempBuffer;
		}

		private static readonly int tempBufSize;

		static ObjectInserter()
		{
			string s = Runtime.GetProperty("jgit.tempbufmaxsize");
			if (s != null)
			{
				tempBufSize = System.Convert.ToInt32(s);
			}
			else
			{
				tempBufSize = 1000000;
			}
		}

		/// <param name="hintSize"></param>
		/// <returns>a temporary byte array for use by the caller</returns>
		protected internal virtual byte[] Buffer(long hintSize)
		{
			if (hintSize >= tempBufSize)
			{
				tempBuffer = new byte[0];
			}
			else
			{
				if (tempBuffer == null)
				{
					tempBuffer = new byte[(int)hintSize];
				}
				else
				{
					if (tempBuffer.Length < hintSize)
					{
						tempBuffer = new byte[(int)hintSize];
					}
				}
			}
			return tempBuffer;
		}

		/// <returns>digest to help compute an ObjectId</returns>
		protected internal virtual MessageDigest Digest()
		{
			digest.Reset();
			return digest;
		}

		/// <summary>Compute the name of an object, without inserting it.</summary>
		/// <remarks>Compute the name of an object, without inserting it.</remarks>
		/// <param name="type">type code of the object to store.</param>
		/// <param name="data">complete content of the object.</param>
		/// <returns>the name of the object.</returns>
		public virtual ObjectId IdFor(int type, byte[] data)
		{
			return IdFor(type, data, 0, data.Length);
		}

		/// <summary>Compute the name of an object, without inserting it.</summary>
		/// <remarks>Compute the name of an object, without inserting it.</remarks>
		/// <param name="type">type code of the object to store.</param>
		/// <param name="data">complete content of the object.</param>
		/// <param name="off">
		/// first position within
		/// <code>data</code>
		/// .
		/// </param>
		/// <param name="len">
		/// number of bytes to copy from
		/// <code>data</code>
		/// .
		/// </param>
		/// <returns>the name of the object.</returns>
		public virtual ObjectId IdFor(int type, byte[] data, int off, int len)
		{
			MessageDigest md = Digest();
			md.Update(Constants.EncodedTypeString(type));
			md.Update(unchecked((byte)' '));
			md.Update(Constants.EncodeASCII(len));
			md.Update(unchecked((byte)0));
			md.Update(data, off, len);
			return ObjectId.FromRaw(md.Digest());
		}

		/// <summary>Compute the name of an object, without inserting it.</summary>
		/// <remarks>Compute the name of an object, without inserting it.</remarks>
		/// <param name="objectType">type code of the object to store.</param>
		/// <param name="length">
		/// number of bytes to scan from
		/// <code>in</code>
		/// .
		/// </param>
		/// <param name="in">
		/// stream providing the object content. The caller is responsible
		/// for closing the stream.
		/// </param>
		/// <returns>the name of the object.</returns>
		/// <exception cref="System.IO.IOException">the source stream could not be read.</exception>
		public virtual ObjectId IdFor(int objectType, long length, InputStream @in)
		{
			MessageDigest md = Digest();
			md.Update(Constants.EncodedTypeString(objectType));
			md.Update(unchecked((byte)' '));
			md.Update(Constants.EncodeASCII(length));
			md.Update(unchecked((byte)0));
			byte[] buf = Buffer(length);
			while (length > 0)
			{
				int n = @in.Read(buf, 0, (int)Math.Min(length, buf.Length));
				if (n < 0)
				{
					throw new EOFException("Unexpected end of input");
				}
				md.Update(buf, 0, n);
				length -= n;
			}
			return ObjectId.FromRaw(md.Digest());
		}

		/// <summary>Compute the ObjectId for the given tree without inserting it.</summary>
		/// <remarks>Compute the ObjectId for the given tree without inserting it.</remarks>
		/// <param name="formatter"></param>
		/// <returns>the computed ObjectId</returns>
		public virtual ObjectId IdFor(TreeFormatter formatter)
		{
			return formatter.ComputeId(this);
		}

		/// <summary>Insert a single tree into the store, returning its unique name.</summary>
		/// <remarks>Insert a single tree into the store, returning its unique name.</remarks>
		/// <param name="formatter">the formatter containing the proposed tree's data.</param>
		/// <returns>the name of the tree object.</returns>
		/// <exception cref="System.IO.IOException">the object could not be stored.</exception>
		public ObjectId Insert(TreeFormatter formatter)
		{
			// Delegate to the formatter, as then it can pass the raw internal
			// buffer back to this inserter, avoiding unnecessary data copying.
			//
			return formatter.InsertTo(this);
		}

		/// <summary>Insert a single commit into the store, returning its unique name.</summary>
		/// <remarks>Insert a single commit into the store, returning its unique name.</remarks>
		/// <param name="builder">the builder containing the proposed commit's data.</param>
		/// <returns>the name of the commit object.</returns>
		/// <exception cref="System.IO.IOException">the object could not be stored.</exception>
		public ObjectId Insert(NGit.CommitBuilder builder)
		{
			return Insert(Constants.OBJ_COMMIT, builder.Build());
		}

		/// <summary>Insert a single annotated tag into the store, returning its unique name.
		/// 	</summary>
		/// <remarks>Insert a single annotated tag into the store, returning its unique name.
		/// 	</remarks>
		/// <param name="builder">the builder containing the proposed tag's data.</param>
		/// <returns>the name of the tag object.</returns>
		/// <exception cref="System.IO.IOException">the object could not be stored.</exception>
		public ObjectId Insert(TagBuilder builder)
		{
			return Insert(Constants.OBJ_TAG, builder.Build());
		}

		/// <summary>Insert a single object into the store, returning its unique name.</summary>
		/// <remarks>Insert a single object into the store, returning its unique name.</remarks>
		/// <param name="type">type code of the object to store.</param>
		/// <param name="data">complete content of the object.</param>
		/// <returns>the name of the object.</returns>
		/// <exception cref="System.IO.IOException">the object could not be stored.</exception>
		public virtual ObjectId Insert(int type, byte[] data)
		{
			return Insert(type, data, 0, data.Length);
		}

		/// <summary>Insert a single object into the store, returning its unique name.</summary>
		/// <remarks>Insert a single object into the store, returning its unique name.</remarks>
		/// <param name="type">type code of the object to store.</param>
		/// <param name="data">complete content of the object.</param>
		/// <param name="off">
		/// first position within
		/// <code>data</code>
		/// .
		/// </param>
		/// <param name="len">
		/// number of bytes to copy from
		/// <code>data</code>
		/// .
		/// </param>
		/// <returns>the name of the object.</returns>
		/// <exception cref="System.IO.IOException">the object could not be stored.</exception>
		public virtual ObjectId Insert(int type, byte[] data, int off, int len)
		{
			return Insert(type, len, new ByteArrayInputStream(data, off, len));
		}

		/// <summary>Insert a single object into the store, returning its unique name.</summary>
		/// <remarks>Insert a single object into the store, returning its unique name.</remarks>
		/// <param name="objectType">type code of the object to store.</param>
		/// <param name="length">
		/// number of bytes to copy from
		/// <code>in</code>
		/// .
		/// </param>
		/// <param name="in">
		/// stream providing the object content. The caller is responsible
		/// for closing the stream.
		/// </param>
		/// <returns>the name of the object.</returns>
		/// <exception cref="System.IO.IOException">
		/// the object could not be stored, or the source stream could
		/// not be read.
		/// </exception>
		public abstract ObjectId Insert(int objectType, long length, InputStream @in);

		/// <summary>Initialize a parser to read from a pack formatted stream.</summary>
		/// <remarks>Initialize a parser to read from a pack formatted stream.</remarks>
		/// <param name="in">
		/// the input stream. The stream is not closed by the parser, and
		/// must instead be closed by the caller once parsing is complete.
		/// </param>
		/// <returns>the pack parser.</returns>
		/// <exception cref="System.IO.IOException">
		/// the parser instance, which can be configured and then used to
		/// parse objects into the ObjectDatabase.
		/// </exception>
		public abstract PackParser NewPackParser(InputStream @in);

		/// <summary>Make all inserted objects visible.</summary>
		/// <remarks>
		/// Make all inserted objects visible.
		/// <p>
		/// The flush may take some period of time to make the objects available to
		/// other threads.
		/// </remarks>
		/// <exception cref="System.IO.IOException">
		/// the flush could not be completed; objects inserted thus far
		/// are in an indeterminate state.
		/// </exception>
		public abstract void Flush();

		/// <summary>Release any resources used by this inserter.</summary>
		/// <remarks>
		/// Release any resources used by this inserter.
		/// <p>
		/// An inserter that has been released can be used again, but may need to be
		/// released after the subsequent usage.
		/// </remarks>
		public abstract void Release();
	}
}
