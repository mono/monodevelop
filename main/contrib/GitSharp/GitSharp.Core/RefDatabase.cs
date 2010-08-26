/*
 * Copyright (C) 2010, Google Inc.
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;

namespace GitSharp.Core
{
    /// <summary>
    /// Abstraction of name to <see cref="ObjectId"/> mapping.
    /// <para/>
    /// A reference database stores a mapping of reference names to <see cref="ObjectId"/>.
    /// Every <see cref="Repository"/> has a single reference database, mapping names to
    /// the tips of the object graph contained by the <see cref="ObjectDatabase"/>.
    /// </summary>
    public abstract class RefDatabase : IDisposable
    {
        /// <summary>
        /// Order of prefixes to search when using non-absolute references.
        /// <para/>
        /// The implementation's <see cref="getRef"/> method must take this search
        /// space into consideration when locating a reference by name. The first
        /// entry in the path is always {@code ""}, ensuring that absolute references
        /// are resolved without further mangling.
        /// </summary>
        protected static string[] SEARCH_PATH = { "", //$NON-NLS-1$
                                                  Constants.R_REFS, //
                                                  Constants.R_TAGS, //
                                                  Constants.R_HEADS, //
                                                  Constants.R_REMOTES //
                                                };


        /// <summary>
        /// Maximum number of times a <see cref="SymbolicRef"/> can be traversed.
        /// <para/>
        /// If the reference is nested deeper than this depth, the implementation
        /// should either fail, or at least claim the reference does not exist.
        /// </summary>
        protected static int MAX_SYMBOLIC_REF_DEPTH = 5;

        /// <summary>
        /// Magic value for <see cref="getRefs"/> to return all references.
        /// </summary>
        public static string ALL = "";//$NON-NLS-1$

        /// <summary>
        /// Initialize a new reference database at this location.
        /// </summary>
        public abstract void create();

        /// <summary>
        /// Close any resources held by this database.
        /// </summary>
        public abstract void close();

        /// <summary>
        /// Determine if a proposed reference name overlaps with an existing one.
        /// <para/>
        /// Reference names use '/' as a component separator, and may be stored in a
        /// hierarchical storage such as a directory on the local filesystem.
        /// <para/>
        /// If the reference "refs/heads/foo" exists then "refs/heads/foo/bar" must
        /// not exist, as a reference cannot have a value and also be a container for
        /// other references at the same time.
        /// <para/>
        /// If the reference "refs/heads/foo/bar" exists than the reference
        /// "refs/heads/foo" cannot exist, for the same reason.
        /// </summary>
        /// <param name="name">proposed name.</param>
        /// <returns>
        /// true if the name overlaps with an existing reference; false if
        /// using this name right now would be safe.
        /// </returns>
        public abstract bool isNameConflicting(string name);

        /// <summary>
        /// Create a new update command to create, modify or delete a reference.
        /// </summary>
        /// <param name="name">the name of the reference.</param>
        /// <param name="detach">
        /// if {@code true} and {@code name} is currently a
        /// <see cref="SymbolicRef"/>, the update will replace it with an
        /// <see cref="ObjectIdRef"/>. Otherwise, the update will recursively
        /// traverse <see cref="SymbolicRef"/>s and operate on the leaf
        /// <see cref="ObjectIdRef"/>.
        /// </param>
        /// <returns>a new update for the requested name; never null.</returns>
        public abstract RefUpdate newUpdate(string name, bool detach);

        /// <summary>
        /// Create a new update command to rename a reference.
        /// </summary>
        /// <param name="fromName">name of reference to rename from</param>
        /// <param name="toName">name of reference to rename to</param>
        /// <returns>an update command that knows how to rename a branch to another.</returns>
        public abstract RefRename newRename(string fromName, string toName);

        /// <summary>
        /// Read a single reference.
        /// <para/>
        /// Aside from taking advantage of <see cref="SEARCH_PATH"/>, this method may be
        /// able to more quickly resolve a single reference name than obtaining the
        /// complete namespace by {@code getRefs(ALL).get(name)}.
        /// </summary>
        /// <param name="name">
        /// the name of the reference. May be a short name which must be
        /// searched for using the standard {@link #SEARCH_PATH}.
        /// </param>
        /// <returns>the reference (if it exists); else {@code null}.</returns>
        public abstract Ref getRef(string name);

        /// <summary>
        /// Get a section of the reference namespace.
        /// </summary>
        /// <param name="prefix">
        /// prefix to search the namespace with; must end with {@code /}.
        /// If the empty string (<see cref="ALL"/>), obtain a complete snapshot
        /// of all references.
        /// </param>
        /// <returns>
        /// modifiable map that is a complete snapshot of the current
        /// reference namespace, with {@code prefix} removed from the start
        /// of each key. The map can be an unsorted map.
        /// </returns>
        public abstract IDictionary<string, Ref> getRefs(string prefix);

        /// <summary>
        /// Peel a possibly unpeeled reference by traversing the annotated tags.
        /// <para/>
        /// If the reference cannot be peeled (as it does not refer to an annotated
        /// tag) the peeled id stays null, but <see cref="Ref.IsPeeled"/> will be true.
        /// <para/>
        /// Implementors should check <see cref="Ref.IsPeeled"/> before performing any
        /// additional work effort.
        /// </summary>
        /// <param name="ref">The reference to peel</param>
        /// <returns>
        /// {@code ref} if {@code ref.isPeeled()} is true; otherwise a new
        /// Ref object representing the same data as Ref, but isPeeled() will
        /// be true and getPeeledObjectId() will contain the peeled object
        /// (or null).
        /// </returns>
        public abstract Ref peel(Ref @ref);

        public virtual void Dispose()
        {
            close();
        }
    }
}


