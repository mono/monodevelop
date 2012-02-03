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

using NGit;
using Sharpen;

namespace NGit
{
	/// <summary>Queue to open objects asynchronously.</summary>
	/// <remarks>
	/// Queue to open objects asynchronously.
	/// A queue may perform background decompression of objects and supply them
	/// (possibly out-of-order) to the application.
	/// </remarks>
	/// <?></?>
	public interface AsyncObjectLoaderQueue<T> : AsyncOperation where T:ObjectId
	{
		/// <summary>Position this queue onto the next available result.</summary>
		/// <remarks>
		/// Position this queue onto the next available result.
		/// Even if this method returns true,
		/// <see cref="AsyncObjectLoaderQueue{T}.Open()">AsyncObjectLoaderQueue&lt;T&gt;.Open()
		/// 	</see>
		/// may still throw
		/// <see cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
		/// 	</see>
		/// if the underlying object database was
		/// concurrently modified and the current object is no longer available.
		/// </remarks>
		/// <returns>
		/// true if there is a result available; false if the queue has
		/// finished its input iteration.
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// the object does not exist. If the implementation is retaining
		/// the application's objects
		/// <see cref="AsyncObjectLoaderQueue{T}.GetCurrent()">AsyncObjectLoaderQueue&lt;T&gt;.GetCurrent()
		/// 	</see>
		/// will be the
		/// current object that is missing. There may be more results
		/// still available, so the caller should continue invoking next
		/// to examine another result.
		/// </exception>
		/// <exception cref="System.IO.IOException">the object store cannot be accessed.</exception>
		bool Next();

		/// <returns>
		/// the current object, null if the implementation lost track.
		/// Implementations may for performance reasons discard the caller's
		/// ObjectId and provider their own through
		/// <see cref="AsyncObjectLoaderQueue{T}.GetObjectId()">AsyncObjectLoaderQueue&lt;T&gt;.GetObjectId()
		/// 	</see>
		/// .
		/// </returns>
		T GetCurrent();

		/// <returns>the ObjectId of the current object. Never null.</returns>
		ObjectId GetObjectId();

		/// <summary>Obtain a loader to read the object.</summary>
		/// <remarks>
		/// Obtain a loader to read the object.
		/// This method can only be invoked once per result
		/// Due to race conditions with a concurrent modification of the underlying
		/// object database, an object may be unavailable when this method is
		/// invoked, even though next returned successfully.
		/// </remarks>
		/// <returns>the ObjectLoader to read this object. Never null.</returns>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// the object does not exist. If the implementation is retaining
		/// the application's objects
		/// <see cref="AsyncObjectLoaderQueue{T}.GetCurrent()">AsyncObjectLoaderQueue&lt;T&gt;.GetCurrent()
		/// 	</see>
		/// will be the
		/// current object that is missing. There may be more results
		/// still available, so the caller should continue invoking next
		/// to examine another result.
		/// </exception>
		/// <exception cref="System.IO.IOException">the object store cannot be accessed.</exception>
		ObjectLoader Open();
	}
}
