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

using System.Collections.Generic;
using NGit;
using Sharpen;

namespace NGit
{
	/// <summary>
	/// Abstraction of name to
	/// <see cref="ObjectId">ObjectId</see>
	/// mapping.
	/// <p>
	/// A reference database stores a mapping of reference names to
	/// <see cref="ObjectId">ObjectId</see>
	/// .
	/// Every
	/// <see cref="Repository">Repository</see>
	/// has a single reference database, mapping names to
	/// the tips of the object graph contained by the
	/// <see cref="ObjectDatabase">ObjectDatabase</see>
	/// .
	/// </summary>
	public abstract class RefDatabase
	{
		/// <summary>Order of prefixes to search when using non-absolute references.</summary>
		/// <remarks>
		/// Order of prefixes to search when using non-absolute references.
		/// <p>
		/// The implementation's
		/// <see cref="GetRef(string)">GetRef(string)</see>
		/// method must take this search
		/// space into consideration when locating a reference by name. The first
		/// entry in the path is always
		/// <code>""</code>
		/// , ensuring that absolute references
		/// are resolved without further mangling.
		/// </remarks>
		protected internal static readonly string[] SEARCH_PATH = new string[] { string.Empty
			, Constants.R_REFS, Constants.R_TAGS, Constants.R_HEADS, Constants.R_REMOTES };

		/// <summary>
		/// Maximum number of times a
		/// <see cref="SymbolicRef">SymbolicRef</see>
		/// can be traversed.
		/// <p>
		/// If the reference is nested deeper than this depth, the implementation
		/// should either fail, or at least claim the reference does not exist.
		/// </summary>
		protected internal const int MAX_SYMBOLIC_REF_DEPTH = 5;

		/// <summary>
		/// Magic value for
		/// <see cref="GetRefs(string)">GetRefs(string)</see>
		/// to return all references.
		/// </summary>
		public static readonly string ALL = string.Empty;

		//$NON-NLS-1$
		//
		//
		//
		//
		//$NON-NLS-1$
		/// <summary>Initialize a new reference database at this location.</summary>
		/// <remarks>Initialize a new reference database at this location.</remarks>
		/// <exception cref="System.IO.IOException">the database could not be created.</exception>
		public abstract void Create();

		/// <summary>Close any resources held by this database.</summary>
		/// <remarks>Close any resources held by this database.</remarks>
		public abstract void Close();

		/// <summary>Determine if a proposed reference name overlaps with an existing one.</summary>
		/// <remarks>
		/// Determine if a proposed reference name overlaps with an existing one.
		/// <p>
		/// Reference names use '/' as a component separator, and may be stored in a
		/// hierarchical storage such as a directory on the local filesystem.
		/// <p>
		/// If the reference "refs/heads/foo" exists then "refs/heads/foo/bar" must
		/// not exist, as a reference cannot have a value and also be a container for
		/// other references at the same time.
		/// <p>
		/// If the reference "refs/heads/foo/bar" exists than the reference
		/// "refs/heads/foo" cannot exist, for the same reason.
		/// </remarks>
		/// <param name="name">proposed name.</param>
		/// <returns>
		/// true if the name overlaps with an existing reference; false if
		/// using this name right now would be safe.
		/// </returns>
		/// <exception cref="System.IO.IOException">the database could not be read to check for conflicts.
		/// 	</exception>
		public abstract bool IsNameConflicting(string name);

		/// <summary>Create a new update command to create, modify or delete a reference.</summary>
		/// <remarks>Create a new update command to create, modify or delete a reference.</remarks>
		/// <param name="name">the name of the reference.</param>
		/// <param name="detach">
		/// if
		/// <code>true</code>
		/// and
		/// <code>name</code>
		/// is currently a
		/// <see cref="SymbolicRef">SymbolicRef</see>
		/// , the update will replace it with an
		/// <see cref="ObjectIdRef">ObjectIdRef</see>
		/// . Otherwise, the update will recursively
		/// traverse
		/// <see cref="SymbolicRef">SymbolicRef</see>
		/// s and operate on the leaf
		/// <see cref="ObjectIdRef">ObjectIdRef</see>
		/// .
		/// </param>
		/// <returns>a new update for the requested name; never null.</returns>
		/// <exception cref="System.IO.IOException">the reference space cannot be accessed.</exception>
		public abstract RefUpdate NewUpdate(string name, bool detach);

		/// <summary>Create a new update command to rename a reference.</summary>
		/// <remarks>Create a new update command to rename a reference.</remarks>
		/// <param name="fromName">name of reference to rename from</param>
		/// <param name="toName">name of reference to rename to</param>
		/// <returns>an update command that knows how to rename a branch to another.</returns>
		/// <exception cref="System.IO.IOException">the reference space cannot be accessed.</exception>
		public abstract RefRename NewRename(string fromName, string toName);

		/// <summary>Read a single reference.</summary>
		/// <remarks>
		/// Read a single reference.
		/// <p>
		/// Aside from taking advantage of
		/// <see cref="SEARCH_PATH">SEARCH_PATH</see>
		/// , this method may be
		/// able to more quickly resolve a single reference name than obtaining the
		/// complete namespace by
		/// <code>getRefs(ALL).get(name)</code>
		/// .
		/// </remarks>
		/// <param name="name">
		/// the name of the reference. May be a short name which must be
		/// searched for using the standard
		/// <see cref="SEARCH_PATH">SEARCH_PATH</see>
		/// .
		/// </param>
		/// <returns>
		/// the reference (if it exists); else
		/// <code>null</code>
		/// .
		/// </returns>
		/// <exception cref="System.IO.IOException">the reference space cannot be accessed.</exception>
		public abstract Ref GetRef(string name);

		/// <summary>Get a section of the reference namespace.</summary>
		/// <remarks>Get a section of the reference namespace.</remarks>
		/// <param name="prefix">
		/// prefix to search the namespace with; must end with
		/// <code>/</code>
		/// .
		/// If the empty string (
		/// <see cref="ALL">ALL</see>
		/// ), obtain a complete snapshot
		/// of all references.
		/// </param>
		/// <returns>
		/// modifiable map that is a complete snapshot of the current
		/// reference namespace, with
		/// <code>prefix</code>
		/// removed from the start
		/// of each key. The map can be an unsorted map.
		/// </returns>
		/// <exception cref="System.IO.IOException">the reference space cannot be accessed.</exception>
		public abstract IDictionary<string, Ref> GetRefs(string prefix);

		/// <summary>Peel a possibly unpeeled reference by traversing the annotated tags.</summary>
		/// <remarks>
		/// Peel a possibly unpeeled reference by traversing the annotated tags.
		/// <p>
		/// If the reference cannot be peeled (as it does not refer to an annotated
		/// tag) the peeled id stays null, but
		/// <see cref="Ref.IsPeeled()">Ref.IsPeeled()</see>
		/// will be true.
		/// <p>
		/// Implementors should check
		/// <see cref="Ref.IsPeeled()">Ref.IsPeeled()</see>
		/// before performing any
		/// additional work effort.
		/// </remarks>
		/// <param name="ref">The reference to peel</param>
		/// <returns>
		/// 
		/// <code>ref</code>
		/// if
		/// <code>ref.isPeeled()</code>
		/// is true; otherwise a new
		/// Ref object representing the same data as Ref, but isPeeled() will
		/// be true and getPeeledObjectId() will contain the peeled object
		/// (or null).
		/// </returns>
		/// <exception cref="System.IO.IOException">the reference space or object space cannot be accessed.
		/// 	</exception>
		public abstract Ref Peel(Ref @ref);
	}
}
