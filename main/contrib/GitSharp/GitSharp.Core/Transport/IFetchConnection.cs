/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using GitSharp.Core.Exceptions;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Lists known refs from the remote and copies objects of selected refs.
    /// <para/>
    /// A fetch connection typically connects to the <code>git-upload-pack</code>
    /// service running where the remote repository is stored. This provides a
    /// one-way object transfer service to copy objects from the remote repository
    /// into this local repository.
    /// <para/>
    /// Instances of a FetchConnection must be created by a <see cref="Transport"/> that
    /// implements a specific object transfer protocol that both sides of the
    /// connection understand.
    /// <para/>
    /// FetchConnection instances are not thread safe and may be accessed by only one
    /// thread at a time.
    /// </summary>
    public interface IFetchConnection : IConnection
    {
        /// <summary>
        /// Fetch objects we don't have but that are reachable from advertised refs.
        /// <para/>
        /// Only one call per connection is allowed. Subsequent calls will result in
        /// <see cref="TransportException"/>.
        /// <para/>
        /// Implementations are free to use network connections as necessary to
        /// efficiently (for both client and server) transfer objects from the remote
        /// repository into this repository. When possible implementations should
        /// avoid replacing/overwriting/duplicating an object already available in
        /// the local destination repository. Locally available objects and packs
        /// should always be preferred over remotely available objects and packs.
        /// <see cref="Transport.get_FetchThin"/> should be honored if applicable.
        /// </summary>
        /// <param name="monitor">
        /// progress monitor to inform the end-user about the amount of
        /// work completed, or to indicate cancellation. Implementations
        /// should poll the monitor at regular intervals to look for
        /// cancellation requests from the user.
        /// </param>
        /// <param name="want">
        /// one or more refs advertised by this connection that the caller
        /// wants to store locally.
        /// </param>
        /// <param name="have">
        /// additional objects known to exist in the destination
        /// repository, especially if they aren't yet reachable by the ref
        /// database. Connections should take this set as an addition to
        /// what is reachable through all Refs, not in replace of it.
        /// </param>
        void Fetch(ProgressMonitor monitor, ICollection<Ref> want, IList<ObjectId> have);

        /// <summary>
        /// Did the last <see cref="Fetch"/> get tags?
        /// <para/>
        /// Some Git aware transports are able to implicitly grab an annotated tag if
        /// <see cref="TagOpt.AUTO_FOLLOW"/> or <see cref="TagOpt.FETCH_TAGS"/> was selected and
        /// the object the tag peels to (references) was transferred as part of the
        /// last <see cref="Fetch"/> call. If it is
        /// possible for such tags to have been included in the transfer this method
        /// returns true, allowing the caller to attempt tag discovery.
        /// <para/>
        /// By returning only true/false (and not the actual list of tags obtained)
        /// the transport itself does not need to be aware of whether or not tags
        /// were included in the transfer.
        /// <para/>
        /// Returns true if the last fetch call implicitly included tag objects;
        /// false if tags were not implicitly obtained.
        /// </summary>
        bool DidFetchIncludeTags { get; }

        /// <summary>
        /// Did the last <see cref="Fetch"/> validate
        /// graph?
        /// <para/>
        /// Some transports walk the object graph on the client side, with the client
        /// looking for what objects it is missing and requesting them individually
        /// from the remote peer. By virtue of completing the fetch call the client
        /// implicitly tested the object connectivity, as every object in the graph
        /// was either already local or was requested successfully from the peer. In
        /// such transports this method returns true.
        /// <para/>
        /// Some transports assume the remote peer knows the Git object graph and is
        /// able to supply a fully connected graph to the client (although it may
        /// only be transferring the parts the client does not yet have). Its faster
        /// to assume such remote peers are well behaved and send the correct
        /// response to the client. In such transports this method returns false.
        /// <para/>
        /// Returns true if the last fetch had to perform a connectivity check on the
        /// client side in order to succeed; false if the last fetch assumed
        /// the remote peer supplied a complete graph.
        /// </summary>
        bool DidFetchTestConnectivity { get; }

        /// <summary>
        /// Set the lock message used when holding a pack out of garbage collection.
        /// <para/>
        /// Callers that set a lock message <b>must</b> ensure they call
        /// <see cref="PackLocks"/> after
        /// <see cref="Fetch"/>, even if an exception
        /// was thrown, and release the locks that are held.
        /// </summary>
        /// <param name="message">message to use when holding a pack in place.</param>
        void SetPackLockMessage(string message);

        /// <summary>
        /// All locks created by the last <see cref="Fetch"/> call.
        /// <para/>
        /// Returns collection (possibly empty) of locks created by the last call to
        /// fetch. The caller must release these after refs are updated in
        /// order to safely permit garbage collection.
        /// </summary>
        List<PackLock> PackLocks { get; }
    }
}