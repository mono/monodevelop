/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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

using System.Collections.Generic;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Represent connection for operation on a remote repository.
    /// <para/>
    /// Currently all operations on remote repository (fetch and push) provide
    /// information about remote refs. Every connection is able to be closed and
    /// should be closed - this is a connection client responsibility.
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Get the complete map of refs advertised as available for fetching or
        /// pushing.
        /// <para/>
        /// Returns available/advertised refs: map of refname to ref. Never null. Not
        /// modifiable. The collection can be empty if the remote side has no
        /// refs (it is an empty/newly created repository).
        /// </summary>
        IDictionary<string, Ref> RefsMap { get; }

        /// <summary>
        /// Get the complete list of refs advertised as available for fetching or
        /// pushing.
        /// <para/>
        /// The returned refs may appear in any order. If the caller needs these to
        /// be sorted, they should be copied into a new array or List and then sorted
        /// by the caller as necessary.
        /// 
        /// Returns available/advertised refs. Never null. Not modifiable. The
        /// collection can be empty if the remote side has no refs (it is an
        /// empty/newly created repository).
        /// </summary>
        ICollection<Ref> Refs { get; }

        /// <summary>
        /// Get a single advertised ref by name.
        /// <para/>
        /// The name supplied should be valid ref name. To get a peeled value for a
        /// ref (aka <code>refs/tags/v1.0^{}</code>) use the base name (without
        /// the <code>^{}</code> suffix) and look at the peeled object id.
        /// </summary>
        /// <param name="name">name of the ref to obtain.</param>
        /// <returns>the requested ref; null if the remote did not advertise this ref.</returns>
        Ref GetRef(string name);

        /// <summary>
        /// Close any resources used by this connection.
        /// <para/>
        /// If the remote repository is contacted by a network socket this method
        /// must close that network socket, disconnecting the two peers. If the
        /// remote repository is actually local (same system) this method must close
        /// any open file handles used to read the "remote" repository.</summary>
        void Close();
    }
}