/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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
using System.Collections.ObjectModel;
using System.Linq;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Class holding result of operation on remote repository. This includes refs
    /// advertised by remote repo and local tracking refs updates.
    /// </summary>
    public abstract class OperationResult
    {
        private IDictionary<string, Ref> _advertisedRefs = new Dictionary<string, Ref>();
        private URIish _uri;
        private readonly SortedDictionary<string, TrackingRefUpdate> _updates = new SortedDictionary<string, TrackingRefUpdate>();

        /// <summary>
        /// Get the URI this result came from.
        /// <para/>
        /// Each transport instance connects to at most one URI at any point in time.
        /// <para/>
        /// Returns the URI describing the location of the remote repository.
        /// </summary>
        public URIish URI
        {
            get
            {
                return _uri;
            }
        }

        /// <summary>
        /// Get the complete list of refs advertised by the remote.
        /// <para/>
        /// The returned refs may appear in any order. If the caller needs these to
        /// be sorted, they should be copied into a new array or List and then sorted
        /// by the caller as necessary.
        /// <para/>
        /// Returns available/advertised refs. Never null. Not modifiable. The
        /// collection can be empty if the remote side has no refs (it is an
        /// empty/newly created repository).
        /// </summary>
        public ICollection<Ref> AdvertisedRefs
        {
            get
            {
                return new ReadOnlyCollection<Ref>(_advertisedRefs.Values.ToList());
            }
        }

        /// <summary>
        /// Get a single advertised ref by name.
        /// <para/>
        /// The name supplied should be valid ref name. To get a peeled value for a
        /// ref (aka <code>refs/tags/v1.0^{}</code>) use the base name (without
        /// the <code>^{}</code> suffix) and look at the peeled object id.
        /// </summary>
        /// <param name="name">name of the ref to obtain.</param>
        /// <returns>the requested ref; null if the remote did not advertise this ref.</returns>
        public Ref GetAdvertisedRef(string name)
        {
            return _advertisedRefs.get(name);
        }

        /// <summary>
        /// Get the status of all local tracking refs that were updated.
        /// </summary>
        /// <returns>
        /// unmodifiable collection of local updates. Never null. Empty if
        /// there were no local tracking refs updated.
        /// </returns>
        public ICollection<TrackingRefUpdate> TrackingRefUpdates
        {
            get
            {
                return new ReadOnlyCollection<TrackingRefUpdate>(_updates.Values.ToList());
            }
        }

        /// <summary>
        /// Get the status for a specific local tracking ref update.
        /// </summary>
        /// <param name="localName">name of the local ref (e.g. "refs/remotes/origin/master").</param>
        /// <returns>
        /// status of the local ref; null if this local ref was not touched
        /// during this operation.
        /// </returns>
        public TrackingRefUpdate GetTrackingRefUpdate(string localName)
        {
            return _updates.get(localName);
        }

        public void SetAdvertisedRefs(URIish u, IDictionary<string, Ref> ar)
        {
            _uri = u;
            _advertisedRefs = ar;
        }

        public void Add(TrackingRefUpdate u)
        {
            _updates.put(u.LocalName, u);
        }
    }

}