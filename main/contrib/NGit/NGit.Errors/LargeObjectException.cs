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
using Sharpen;

namespace NGit.Errors
{
	/// <summary>An object is too big to load into memory as a single byte array.</summary>
	/// <remarks>An object is too big to load into memory as a single byte array.</remarks>
	[System.Serializable]
	public class LargeObjectException : RuntimeException
	{
		private const long serialVersionUID = 1L;

		private ObjectId objectId;

		/// <summary>Create a large object exception, where the object isn't known.</summary>
		/// <remarks>Create a large object exception, where the object isn't known.</remarks>
		public LargeObjectException()
		{
		}

		/// <summary>Create a large object exception, naming the object that is too big.</summary>
		/// <remarks>Create a large object exception, naming the object that is too big.</remarks>
		/// <param name="id">
		/// identity of the object that is too big to be loaded as a byte
		/// array in this JVM.
		/// </param>
		public LargeObjectException(AnyObjectId id)
		{
			// Do nothing.
			SetObjectId(id);
		}

		/// <returns>identity of the object that is too large; may be null.</returns>
		public virtual ObjectId GetObjectId()
		{
			return objectId;
		}

		/// <returns>either the hex encoded name of the object, or 'unknown object'.</returns>
		protected internal virtual string GetObjectName()
		{
			if (GetObjectId() != null)
			{
				return GetObjectId().Name;
			}
			return JGitText.Get().unknownObject;
		}

		/// <summary>Set the identity of the object, if its not already set.</summary>
		/// <remarks>Set the identity of the object, if its not already set.</remarks>
		/// <param name="id">the id of the object that is too large to process.</param>
		public virtual void SetObjectId(AnyObjectId id)
		{
			if (objectId == null)
			{
				objectId = id.Copy();
			}
		}

		public override string Message
		{
			get
			{
				return MessageFormat.Format(JGitText.Get().largeObjectException, GetObjectName());
			}
		}

		/// <summary>An error caused by the JVM being out of heap space.</summary>
		/// <remarks>An error caused by the JVM being out of heap space.</remarks>
		[System.Serializable]
		public class OutOfMemory : LargeObjectException
		{
			private const long serialVersionUID = 1L;

			/// <summary>Construct a wrapper around the original OutOfMemoryError.</summary>
			/// <remarks>Construct a wrapper around the original OutOfMemoryError.</remarks>
			/// <param name="cause">the original root cause.</param>
			public OutOfMemory(OutOfMemoryException cause)
			{
				Sharpen.Extensions.InitCause(this, cause);
			}

			public override string Message
			{
				get
				{
					return MessageFormat.Format(JGitText.Get().largeObjectOutOfMemory, GetObjectName(
						));
				}
			}
		}

		/// <summary>Object size exceeds JVM limit of 2 GiB per byte array.</summary>
		/// <remarks>Object size exceeds JVM limit of 2 GiB per byte array.</remarks>
		[System.Serializable]
		public class ExceedsByteArrayLimit : LargeObjectException
		{
			private const long serialVersionUID = 1L;

			public override string Message
			{
				get
				{
					return MessageFormat.Format(JGitText.Get().largeObjectExceedsByteArray, GetObjectName
						());
				}
			}
		}

		/// <summary>Object size exceeds the caller's upper limit.</summary>
		/// <remarks>Object size exceeds the caller's upper limit.</remarks>
		[System.Serializable]
		public class ExceedsLimit : LargeObjectException
		{
			private const long serialVersionUID = 1L;

			private readonly long limit;

			private readonly long size;

			/// <summary>Construct an exception for a particular size being exceeded.</summary>
			/// <remarks>Construct an exception for a particular size being exceeded.</remarks>
			/// <param name="limit">the limit the caller imposed on the object.</param>
			/// <param name="size">the actual size of the object.</param>
			public ExceedsLimit(long limit, long size)
			{
				this.limit = limit;
				this.size = size;
			}

			public override string Message
			{
				get
				{
					return MessageFormat.Format(JGitText.Get().largeObjectExceedsLimit, GetObjectName
						(), limit, size);
				}
			}
		}
	}
}
