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
	/// <summary>
	/// Pairing of a name and the
	/// <see cref="ObjectId">ObjectId</see>
	/// it currently has.
	/// <p>
	/// A ref in Git is (more or less) a variable that holds a single object
	/// identifier. The object identifier can be any valid Git object (blob, tree,
	/// commit, annotated tag, ...).
	/// <p>
	/// The ref name has the attributes of the ref that was asked for as well as the
	/// ref it was resolved to for symbolic refs plus the object id it points to and
	/// (for tags) the peeled target object id, i.e. the tag resolved recursively
	/// until a non-tag object is referenced.
	/// </summary>
	public interface Ref
	{
		/// <summary>What this ref is called within the repository.</summary>
		/// <remarks>What this ref is called within the repository.</remarks>
		/// <returns>name of this ref.</returns>
		string GetName();

		/// <summary>Test if this reference is a symbolic reference.</summary>
		/// <remarks>
		/// Test if this reference is a symbolic reference.
		/// <p>
		/// A symbolic reference does not have its own
		/// <see cref="ObjectId">ObjectId</see>
		/// value, but
		/// instead points to another
		/// <code>Ref</code>
		/// in the same database and always
		/// uses that other reference's value as its own.
		/// </remarks>
		/// <returns>
		/// true if this is a symbolic reference; false if this reference
		/// contains its own ObjectId.
		/// </returns>
		bool IsSymbolic();

		/// <summary>
		/// Traverse target references until
		/// <see cref="IsSymbolic()">IsSymbolic()</see>
		/// is false.
		/// <p>
		/// If
		/// <see cref="IsSymbolic()">IsSymbolic()</see>
		/// is false, returns
		/// <code>this</code>
		/// .
		/// <p>
		/// If
		/// <see cref="IsSymbolic()">IsSymbolic()</see>
		/// is true, this method recursively traverses
		/// <see cref="GetTarget()">GetTarget()</see>
		/// until
		/// <see cref="IsSymbolic()">IsSymbolic()</see>
		/// returns false.
		/// <p>
		/// This method is effectively
		/// <pre>
		/// return isSymbolic() ? getTarget().getLeaf() : this;
		/// </pre>
		/// </summary>
		/// <returns>the reference that actually stores the ObjectId value.</returns>
		Ref GetLeaf();

		/// <summary>
		/// Get the reference this reference points to, or
		/// <code>this</code>
		/// .
		/// <p>
		/// If
		/// <see cref="IsSymbolic()">IsSymbolic()</see>
		/// is true this method returns the reference it
		/// directly names, which might not be the leaf reference, but could be
		/// another symbolic reference.
		/// <p>
		/// If this is a leaf level reference that contains its own ObjectId,this
		/// method returns
		/// <code>this</code>
		/// .
		/// </summary>
		/// <returns>
		/// the target reference, or
		/// <code>this</code>
		/// .
		/// </returns>
		Ref GetTarget();

		/// <summary>Cached value of this ref.</summary>
		/// <remarks>Cached value of this ref.</remarks>
		/// <returns>the value of this ref at the last time we read it.</returns>
		ObjectId GetObjectId();

		/// <summary>Cached value of <code>ref^{}</code> (the ref peeled to commit).</summary>
		/// <remarks>Cached value of <code>ref^{}</code> (the ref peeled to commit).</remarks>
		/// <returns>
		/// if this ref is an annotated tag the id of the commit (or tree or
		/// blob) that the annotated tag refers to; null if this ref does not
		/// refer to an annotated tag.
		/// </returns>
		ObjectId GetPeeledObjectId();

		/// <returns>whether the Ref represents a peeled tag</returns>
		bool IsPeeled();

		/// <summary>
		/// How was this ref obtained?
		/// <p>
		/// The current storage model of a Ref may influence how the ref must be
		/// updated or deleted from the repository.
		/// </summary>
		/// <remarks>
		/// How was this ref obtained?
		/// <p>
		/// The current storage model of a Ref may influence how the ref must be
		/// updated or deleted from the repository.
		/// </remarks>
		/// <returns>type of ref.</returns>
		RefStorage GetStorage();
	}

	/// <summary>
	/// Location where a
	/// <see cref="Ref">Ref</see>
	/// is stored.
	/// </summary>
	public class RefStorage
	{
		/// <summary>The ref does not exist yet, updating it may create it.</summary>
		/// <remarks>
		/// The ref does not exist yet, updating it may create it.
		/// <p>
		/// Creation is likely to choose
		/// <see cref="LOOSE">LOOSE</see>
		/// storage.
		/// </remarks>
		public static RefStorage NEW = new RefStorage(true, false);

		/// <summary>The ref is stored in a file by itself.</summary>
		/// <remarks>
		/// The ref is stored in a file by itself.
		/// <p>
		/// Updating this ref affects only this ref.
		/// </remarks>
		public static RefStorage LOOSE = new RefStorage(true, false);

		/// <summary>The ref is stored in the <code>packed-refs</code> file, with others.</summary>
		/// <remarks>
		/// The ref is stored in the <code>packed-refs</code> file, with others.
		/// <p>
		/// Updating this ref requires rewriting the file, with perhaps many
		/// other refs being included at the same time.
		/// </remarks>
		public static RefStorage PACKED = new RefStorage(false, true);

		/// <summary>
		/// The ref is both
		/// <see cref="LOOSE">LOOSE</see>
		/// and
		/// <see cref="PACKED">PACKED</see>
		/// .
		/// <p>
		/// Updating this ref requires only updating the loose file, but deletion
		/// requires updating both the loose file and the packed refs file.
		/// </summary>
		public static RefStorage LOOSE_PACKED = new RefStorage(true, true);

		/// <summary>The ref came from a network advertisement and storage is unknown.</summary>
		/// <remarks>
		/// The ref came from a network advertisement and storage is unknown.
		/// <p>
		/// This ref cannot be updated without Git-aware support on the remote
		/// side, as Git-aware code consolidate the remote refs and reported them
		/// to this process.
		/// </remarks>
		public static RefStorage NETWORK = new RefStorage(false, false);

		private readonly bool loose;

		private readonly bool packed;

		private RefStorage(bool l, bool p)
		{
			loose = l;
			packed = p;
		}

		/// <returns>true if this storage has a loose file.</returns>
		public virtual bool IsLoose()
		{
			return loose;
		}

		/// <returns>true if this storage is inside the packed file.</returns>
		public virtual bool IsPacked()
		{
			return packed;
		}
	}
}
