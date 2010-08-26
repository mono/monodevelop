/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
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

using System;
using System.Collections.Generic;
using System.IO;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Connects two Git repositories together and copies objects between them.
    /// <para />
    /// A transport can be used for either fetching (copying objects into the
    /// caller's repository from the remote repository) or pushing (copying objects
    /// into the remote repository from the caller's repository). Each transport
    /// implementation is responsible for the details associated with establishing
    /// the network connection(s) necessary for the copy, as well as actually
    /// shuffling data back and forth.
    /// <para />
    /// Transport instances and the connections they Create are not thread-safe.
    /// Callers must ensure a transport is accessed by only one thread at a time.
    /// </summary>
    public abstract class Transport : IDisposable
    {
        /// <summary>
        /// Type of operation a Transport is being opened for.
        /// </summary>
        public enum Operation
        {
            /// <summary>
            /// Transport is to fetch objects locally.
            /// </summary>
            FETCH,

            /// <summary>
            /// Transport is to push objects remotely.
            /// </summary>
            PUSH
        }

        /// <summary>
        /// Open a new transport instance to connect two repositories.
        /// <para/>
        /// This method assumes <see cref="Operation.FETCH"/>.
        /// </summary>
        /// <param name="local">existing local repository.</param>
        /// <param name="remote">
        /// location of the remote repository - may be URI or remote
        /// configuration name.
        /// </param>
        /// <returns>
        /// the new transport instance. Never null. In case of multiple URIs
        /// in remote configuration, only the first is chosen.
        /// </returns>
        public static Transport open(Repository local, string remote)
        {
            return open(local, remote, Operation.FETCH);
        }

        /// <summary>
        /// Open a new transport instance to connect two repositories.
        /// </summary>
        /// <param name="local">existing local repository.</param>
        /// <param name="remote">location of the remote repository - may be URI or remote configuration name.</param>
        /// <param name="op">
        /// planned use of the returned Transport; the URI may differ
        /// based on the type of connection desired.
        /// </param>
        /// <returns>
        /// the new transport instance. Never null. In case of multiple URIs
        /// in remote configuration, only the first is chosen.
        /// </returns>
        public static Transport open(Repository local, string remote,
                Operation op)
        {
            var cfg = new RemoteConfig(local.Config, remote);
            if (doesNotExist(cfg))
                return open(local, new URIish(remote));
            return open(local, cfg, op);
        }

        /// <summary>
        /// Open new transport instances to connect two repositories.
        /// <para/>
        /// This method assumes <see cref="Operation.FETCH"/>.
        /// </summary>
        /// <param name="local">existing local repository.</param>
        /// <param name="remote">
        /// location of the remote repository - may be URI or remote
        /// configuration name.
        /// </param>
        /// <returns>
        /// the list of new transport instances for every URI in remote
        /// configuration.
        /// </returns>
        public static List<Transport> openAll(Repository local, string remote)
        {
            return openAll(local, remote, Operation.FETCH);
        }

        /// <summary>
        /// Open new transport instances to connect two repositories.
        /// </summary>
        /// <param name="local">existing local repository.</param>
        /// <param name="remote">
        /// location of the remote repository - may be URI or remote
        /// configuration name.
        /// </param>
        /// <param name="op">
        /// planned use of the returned Transport; the URI may differ
        /// based on the type of connection desired.
        /// </param>
        /// <returns>
        /// the list of new transport instances for every URI in remote
        /// configuration.
        /// </returns>
        public static List<Transport> openAll(Repository local,
                string remote, Operation op)
        {
            var cfg = new RemoteConfig(local.Config, remote);
            if (doesNotExist(cfg))
            {
                var transports = new List<Transport>(1);
                transports.Add(open(local, new URIish(remote)));
                return transports;
            }
            return openAll(local, cfg, op);
        }
        /// <summary>
        /// Open a new transport instance to connect two repositories.
        /// <para/>
        /// This method assumes <see cref="Operation.FETCH"/>.
        /// </summary>
        /// <param name="local">existing local repository.</param>
        /// <param name="cfg">
        /// configuration describing how to connect to the remote
        /// repository.
        /// </param>
        /// <returns>
        /// the new transport instance. Never null. In case of multiple URIs
        /// in remote configuration, only the first is chosen.
        /// </returns>
        public static Transport open(Repository local, RemoteConfig cfg)
        {
            return open(local, cfg, Operation.FETCH);
        }

        /// <summary>
        /// Open a new transport instance to connect two repositories.
        /// </summary>
        /// <param name="local">existing local repository.</param>
        /// <param name="cfg">
        /// configuration describing how to connect to the remote
        /// repository.
        /// </param>
        /// <param name="op">
        /// planned use of the returned Transport; the URI may differ
        /// based on the type of connection desired.
        /// </param>
        /// <returns></returns>
        public static Transport open(Repository local,
                RemoteConfig cfg, Operation op)
        {
            List<URIish> uris = getURIs(cfg, op);
            if (uris.isEmpty())
                throw new ArgumentException(
                        "Remote config \""
                        + cfg.Name + "\" has no URIs associated");
            Transport tn = open(local, uris[0]);
            tn.ApplyConfig(cfg);
            return tn;
        }

        /// <summary>
        /// Open a new transport instance to connect two repositories.
        /// <para/>
        /// This method assumes <see cref="Operation.FETCH"/>.
        /// </summary>
        /// <param name="local">existing local repository.</param>
        /// <param name="cfg">
        /// configuration describing how to connect to the remote
        /// repository.
        /// </param>
        /// <returns>
        /// the list of new transport instances for every URI in remote
        /// configuration.
        /// </returns>
        public static List<Transport> openAll(Repository local, RemoteConfig cfg)
        {
            return openAll(local, cfg, Operation.FETCH);
        }

        /// <summary>
        /// Open new transport instances to connect two repositories.
        /// </summary>
        /// <param name="local">existing local repository.</param>
        /// <param name="cfg">
        /// configuration describing how to connect to the remote
        /// repository.
        /// </param>
        /// <param name="op">
        /// planned use of the returned Transport; the URI may differ
        /// based on the type of connection desired.
        /// </param>
        /// <returns>
        /// the list of new transport instances for every URI in remote
        /// configuration.
        /// </returns>
        public static List<Transport> openAll(Repository local,
                RemoteConfig cfg, Operation op)
        {
            List<URIish> uris = getURIs(cfg, op);
            var transports = new List<Transport>(uris.Count);
            foreach (URIish uri in uris)
            {
                Transport tn = open(local, uri);
                tn.ApplyConfig(cfg);
                transports.Add(tn);
            }
            return transports;
        }

        private static List<URIish> getURIs(RemoteConfig cfg, Operation op)
        {
            switch (op)
            {
                case Operation.FETCH:
                    return cfg.URIs;

                case Operation.PUSH:
                    List<URIish> uris = cfg.PushURIs;
                    if (uris.isEmpty())
                    {
                        uris = cfg.URIs;
                    }
                    return uris;

                default:
                    throw new ArgumentException(op.ToString());
            }
        }

        private static bool doesNotExist(RemoteConfig cfg)
        {
            return cfg.URIs.isEmpty() && cfg.PushURIs.isEmpty();
        }

        /// <summary>
        /// Open a new transport instance to connect two repositories.
        /// </summary>
        /// <param name="local">existing local repository.</param>
        /// <param name="remote">location of the remote repository.</param>
        /// <returns>the new transport instance. Never null.</returns>
        public static Transport open(Repository local, URIish remote)
        {
            if (TransportGitSsh.canHandle(remote))
                return new TransportGitSsh(local, remote);

            if (TransportHttp.canHandle(remote))
                return new TransportHttp(local, remote);

            if (TransportSftp.canHandle(remote))
                return new TransportSftp(local, remote);

            if (TransportGitAnon.canHandle(remote))
                return new TransportGitAnon(local, remote);

            if (TransportAmazonS3.canHandle(remote))
                return new TransportAmazonS3(local, remote);

            if (TransportBundleFile.canHandle(remote))
                return new TransportBundleFile(local, remote);

            if (TransportLocal.canHandle(remote))
                return new TransportLocal(local, remote);

            throw new NotSupportedException("URI not supported: " + remote);
        }

        /// <summary>
        /// Convert push remote refs update specification from <see cref="RefSpec"/> form
        /// to <see cref="RemoteRefUpdate"/>. Conversion expands wildcards by matching
        /// source part to local refs. expectedOldObjectId in RemoteRefUpdate is
        /// always set as null. Tracking branch is configured if RefSpec destination
        /// matches source of any fetch ref spec for this transport remote
        /// configuration.
        /// </summary>
        /// <param name="db">local database.</param>
        /// <param name="specs">collection of RefSpec to convert.</param>
        /// <param name="fetchSpecs">
        /// fetch specifications used for finding localtracking refs. May
        /// be null or empty collection.
        /// </param>
        /// <returns>collection of set up <see cref="RemoteRefUpdate"/>.</returns>
        public static ICollection<RemoteRefUpdate> findRemoteRefUpdatesFor(Repository db, List<RefSpec> specs, List<RefSpec> fetchSpecs)
        {
            if (fetchSpecs == null)
            {
                fetchSpecs = new List<RefSpec>();
            }

            ICollection<RemoteRefUpdate> result = new List<RemoteRefUpdate>();
            ICollection<RefSpec> procRefs = ExpandPushWildcardsFor(db, specs);

            foreach (RefSpec spec in procRefs)
            {
                string srcSpec = spec.Source;
                Ref srcRef = db.getRef(srcSpec);
                if (srcRef != null)
                {
                    srcSpec = srcRef.Name;
                }

                String destSpec = spec.Destination;
                if (destSpec == null)
                {
                    // No destination (no-colon in ref-spec), DWIMery assumes src
                    //
                    destSpec = srcSpec;
                }

                if (srcRef != null && !destSpec.StartsWith(Constants.R_REFS))
                {
                    // Assume the same kind of ref at the destination, e.g.
                    // "refs/heads/foo:master", DWIMery assumes master is also
                    // under "refs/heads/".
                    //
                    string n = srcRef.Name;
                    int kindEnd = n.IndexOf('/', Constants.R_REFS.Length);
                    destSpec = n.Slice(0, kindEnd + 1) + destSpec;
                }

                bool forceUpdate = spec.Force;
                string localName = FindTrackingRefName(destSpec, fetchSpecs);
                var rru = new RemoteRefUpdate(db, srcSpec, destSpec, forceUpdate, localName, null);
                result.Add(rru);
            }
            return result;
        }

        private static ICollection<RefSpec> ExpandPushWildcardsFor(Repository db, IEnumerable<RefSpec> specs)
        {
            IDictionary<string, Ref> localRefs = db.getAllRefs();
            var procRefs = new HashSet<RefSpec>();

            foreach (RefSpec spec in specs)
            {
                if (spec.Wildcard)
                {
                    foreach (Ref localRef in localRefs.Values)
                    {
                        if (spec.MatchSource(localRef))
                        {
                            procRefs.Add(spec.ExpandFromSource(localRef));
                        }
                    }
                }
                else
                {
                    procRefs.Add(spec);
                }
            }

            return procRefs;
        }

        private static string FindTrackingRefName(string remoteName, IEnumerable<RefSpec> fetchSpecs)
        {
            // try to find matching tracking refs
            foreach (RefSpec fetchSpec in fetchSpecs)
            {
                if (fetchSpec.MatchSource(remoteName))
                {
                    if (fetchSpec.Wildcard)
                        return fetchSpec.ExpandFromSource(remoteName).Destination;

                    return fetchSpec.Destination;
                }
            }
            return null;
        }

        /// <summary>
        /// Default setting for <see cref="get_FetchThin"/> option.
        /// </summary>
        public const bool DEFAULT_FETCH_THIN = true;

        /// <summary>
        /// Default setting for <see cref="get_PushThin"/> option.
        /// </summary>
        public const bool DEFAULT_PUSH_THIN = false;

        /// <summary>
        /// Specification for fetch or push operations, to fetch or push all tags.
        /// Acts as --tags.
        /// </summary>
        public static readonly RefSpec REFSPEC_TAGS = new RefSpec("refs/tags/*:refs/tags/*");

        /// <summary>
        /// Specification for push operation, to push all refs under refs/heads. Acts
        /// as --all
        /// </summary>
        public static readonly RefSpec REFSPEC_PUSH_ALL = new RefSpec("refs/heads/*:refs/heads/*");

        /// <summary>
        /// The repository this transport fetches into, or pushes out of.
        /// </summary>
        private readonly Repository _local;

        /// <summary>
        /// The URI used to create this transport. 
        /// </summary>
        protected readonly URIish _uri;

        /// <summary>
        /// Name of the upload pack program, if it must be executed.
        /// </summary>
        private string _optionUploadPack = RemoteConfig.DEFAULT_UPLOAD_PACK;

        /// <summary>
        /// Specifications to apply during fetch.
        /// </summary>
        private List<RefSpec> _fetch = new List<RefSpec>();

        /// <summary>
        /// How <see cref="fetch"/> should handle tags.
        /// <para/>
        /// We default to <see cref="Core.Transport.TagOpt.NO_TAGS"/> so as to avoid fetching annotated
        /// tags during one-shot fetches used for later merges. This prevents
        /// dragging down tags from repositories that we do not have established
        /// tracking branches for. If we do not track the source repository, we most
        /// likely do not care about any tags it publishes.
        /// </summary>
        private TagOpt _tagopt = TagOpt.NO_TAGS;

        /// <summary>
        /// Should fetch request thin-pack if remote repository can produce it.
        /// </summary>
        private bool _fetchThin = DEFAULT_FETCH_THIN;

        /// <summary>
        /// Name of the receive pack program, if it must be executed.
        /// </summary>
        private string _optionReceivePack = RemoteConfig.DEFAULT_RECEIVE_PACK;

        /// <summary>
        /// Specifications to apply during push.
        /// </summary>
        private List<RefSpec> _push = new List<RefSpec>();

        /// <summary>
        /// Should push produce thin-pack when sending objects to remote repository.
        /// </summary>
        private bool _pushThin = DEFAULT_PUSH_THIN;

        /// <summary>
        /// Should push just check for operation result, not really push.
        /// </summary>
        private bool _dryRun;

        /// <summary>
        /// Should an incoming (fetch) transfer validate objects?
        /// </summary>
        private bool _checkFetchedObjects;

        /// <summary>
        /// Should refs no longer on the source be pruned from the destination?
        /// </summary>
        private bool _removeDeletedRefs;

        /// <summary>
        /// Timeout in seconds to wait before aborting an IO read or write.
        /// </summary>
        private int _timeout;

        public Repository Local { get { return _local; } }

        /// <summary>
        /// Create a new transport instance.
        /// </summary>
        /// <param name="local">
        /// the repository this instance will fetch into, or push out of.
        /// This must be the repository passed to
        /// <see cref="open(GitSharp.Core.Repository,GitSharp.Core.Transport.URIish)"/>.
        /// </param>
        /// <param name="uri">
        /// the URI used to access the remote repository. This must be the
        /// URI passed to <see cref="open(GitSharp.Core.Repository,GitSharp.Core.Transport.URIish)"/>.
        /// </param>
        protected Transport(Repository local, URIish uri)
        {
            TransferConfig tc = local.Config.getTransfer();
            _local = local;
            _uri = uri;
            _checkFetchedObjects = tc.isFsckObjects();
        }

        /// <summary>
        /// Get the URI this transport connects to.
        /// <para/>
        /// Each transport instance connects to at most one URI at any point in time.
        /// <para/>
        /// Returns the URI describing the location of the remote repository.
        /// </summary>
        public URIish Uri
        {
            get { return _uri; }
        }

        /// <summary>
        /// name of the remote executable providing upload-pack service (typically "git-upload-pack").
        /// </summary>
        public string OptionUploadPack
        {
            get { return _optionUploadPack; }
            set { _optionUploadPack = string.IsNullOrEmpty(value) ? RemoteConfig.DEFAULT_UPLOAD_PACK : value; }
        }

        /// <summary>
        /// description of how annotated tags should be treated during fetch.
        /// </summary>
        public TagOpt TagOpt
        {
            get { return _tagopt; }
            set { _tagopt = value ?? TagOpt.AUTO_FOLLOW; }
        }

        /// <summary>
        /// thin-pack preference for fetch operation. Default setting is: <see cref="DEFAULT_FETCH_THIN"/>.
        /// </summary>
        public bool FetchThin
        {
            get { return _fetchThin; }
            set { _fetchThin = value; }
        }

        /// <summary>
        /// true to enable checking received objects; false to assume all received objects are valid.
        /// </summary>
        public bool CheckFetchedObjects
        {
            get { return _checkFetchedObjects; }
            set { _checkFetchedObjects = value; }
        }

        /// <summary>
        /// remote executable providing receive-pack service for pack transports.
        /// Default setting is: <see cref="RemoteConfig.DEFAULT_RECEIVE_PACK"/>
        /// </summary>
        public string OptionReceivePack
        {
            get { return _optionReceivePack; }
            set { _optionReceivePack = string.IsNullOrEmpty(value) ? RemoteConfig.DEFAULT_RECEIVE_PACK : value; }
        }

        /// <summary>
        /// thin-pack preference for push operation. Default setting is: <see cref="DEFAULT_PUSH_THIN"/>.
        /// true when push should produce thin-pack in pack transports; false when it shouldn't.
        /// </summary>
        public bool PushThin
        {
            get { return _pushThin; }
            set { _pushThin = value; }
        }

        /// <summary>
        /// Whether or not to remove refs which no longer exist in the source.
        /// <para/>
        /// If true, refs at the destination repository (local for fetch, remote for
        /// push) are deleted if they no longer exist on the source side (remote for
        /// fetch, local for push).
        /// <para/>
        /// False by default, as this may cause data to become unreachable, and
        /// eventually be deleted on the next GC.
        /// </summary>
        public bool RemoveDeletedRefs
        {
            get { return _removeDeletedRefs; }
            set { _removeDeletedRefs = value; }
        }

        /// <summary>
        /// Apply provided remote configuration on this transport.
        /// </summary>
        /// <param name="cfg">configuration to apply on this transport.</param>
        public void ApplyConfig(RemoteConfig cfg)
        {
            if (cfg == null)
                throw new ArgumentNullException("cfg");
            OptionUploadPack = cfg.UploadPack;
            OptionReceivePack = cfg.ReceivePack;
            TagOpt = cfg.TagOpt;
            _fetch = cfg.Fetch;
            _push = cfg.Push;
            _timeout = cfg.Timeout;
        }

        /// <summary>
        /// true if push operation should just check for possible result and
        /// not really update remote refs, false otherwise - when push should
        /// act normally.
        /// </summary>
        public bool DryRun
        {
            get { return _dryRun; }
            set { _dryRun = value; }
        }

        /// <summary>
        /// number of seconds to wait (with no data transfer occurring) 
        /// before aborting an IO read or write operation with this 
        /// remote. 
        /// </summary>
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        /// <summary>
        /// Fetch objects and refs from the remote repository to the local one.
        /// <para/>
        /// This is a utility function providing standard fetch behavior. Local
        /// tracking refs associated with the remote repository are automatically
        /// updated if this transport was created from a <see cref="RemoteConfig"/> with
        /// fetch RefSpecs defined.
        /// </summary>
        /// <param name="monitor">
        /// progress monitor to inform the user about our processing
        /// activity. Must not be null. Use <see cref="NullProgressMonitor"/> if
        /// progress updates are not interesting or necessary.
        /// </param>
        /// <param name="toFetch">
        /// specification of refs to fetch locally. May be null or the
        /// empty collection to use the specifications from the
        /// RemoteConfig. Source for each RefSpec can't be null.
        /// </param>
        /// <returns>information describing the tracking refs updated.</returns>
        public FetchResult fetch(ProgressMonitor monitor, List<RefSpec> toFetch)
        {
            if (toFetch == null || toFetch.isEmpty())
            {
                // If the caller did not ask for anything use the defaults.
                //
                if (_fetch.isEmpty())
                {
                    throw new TransportException("Nothing to fetch.");
                }
                toFetch = _fetch;
            }
            else if (!_fetch.isEmpty())
            {
                // If the caller asked for something specific without giving
                // us the local tracking branch see if we can update any of
                // the local tracking branches without incurring additional
                // object transfer overheads.
                //
                var tmp = new List<RefSpec>(toFetch);
                foreach (RefSpec requested in toFetch)
                {
                    string reqSrc = requested.Source;
                    foreach (RefSpec configured in _fetch)
                    {
                        string cfgSrc = configured.Source;
                        string cfgDst = configured.Destination;
                        if (cfgSrc.Equals(reqSrc) && cfgDst != null)
                        {
                            tmp.Add(configured);
                            break;
                        }
                    }
                }
                toFetch = tmp;
            }

            var result = new FetchResult();
            new FetchProcess(this, toFetch).execute(monitor, result);
            return result;
        }

        /// <summary>
        /// Push objects and refs from the local repository to the remote one.
        /// <para/>
        /// This is a utility function providing standard push behavior. It updates
        /// remote refs and send there necessary objects according to remote ref
        /// update specification. After successful remote ref update, associated
        /// locally stored tracking branch is updated if set up accordingly. Detailed
        /// operation result is provided after execution.
        /// <para/>
        /// For setting up remote ref update specification from ref spec, see helper
        /// method <see cref="findRemoteRefUpdatesFor(System.Collections.Generic.List{GitSharp.Core.Transport.RefSpec})"/>, predefined refspecs
        /// (<see cref="REFSPEC_TAGS"/>, <see cref="REFSPEC_PUSH_ALL"/>) or consider using
        /// directly <see cref="RemoteRefUpdate"/> for more possibilities.
        /// <para/>
        /// When <see cref="get_DryRun"/> is true, result of this operation is just
        /// estimation of real operation result, no real action is performed.
        /// </summary>
        /// <param name="monitor">
        /// progress monitor to inform the user about our processing
        /// activity. Must not be null. Use <see cref="NullProgressMonitor"/> if
        /// progress updates are not interesting or necessary.
        /// </param>
        /// <param name="toPush">
        /// specification of refs to push. May be null or the empty
        /// collection to use the specifications from the RemoteConfig
        /// converted by <see cref="findRemoteRefUpdatesFor(System.Collections.Generic.List{GitSharp.Core.Transport.RefSpec})"/>. No
        /// more than 1 RemoteRefUpdate with the same remoteName is
        /// allowed. These objects are modified during this call.
        /// </param>
        /// <returns>
        /// information about results of remote refs updates, tracking refs
        /// updates and refs advertised by remote repository.
        /// </returns>
        public PushResult push(ProgressMonitor monitor, ICollection<RemoteRefUpdate> toPush)
        {
            if (toPush == null || toPush.isEmpty())
            {
                // If the caller did not ask for anything use the defaults.
                try
                {
                    toPush = findRemoteRefUpdatesFor(_push);
                }
                catch (IOException e)
                {
                    throw new TransportException("Problem with resolving push ref specs locally: " + e.Message, e);
                }

                if (toPush.isEmpty())
                {
                    throw new TransportException("Nothing to push");
                }
            }

            var pushProcess = new PushProcess(this, toPush);
            return pushProcess.execute(monitor);
        }

        /// <summary>
        /// Convert push remote refs update specification from <see cref="RefSpec"/> form
        /// to <see cref="RemoteRefUpdate"/>. Conversion expands wildcards by matching
        /// source part to local refs. expectedOldObjectId in RemoteRefUpdate is
        /// always set as null. Tracking branch is configured if RefSpec destination
        /// matches source of any fetch ref spec for this transport remote
        /// configuration.
        /// <para/>
        /// Conversion is performed for context of this transport (database, fetch
        /// specifications).                                                        
        /// </summary>
        /// <param name="specs">collection of RefSpec to convert.</param>
        /// <returns>collection of set up <see cref="RemoteRefUpdate"/>.</returns>
        public ICollection<RemoteRefUpdate> findRemoteRefUpdatesFor(List<RefSpec> specs)
        {
            return findRemoteRefUpdatesFor(_local, specs, _fetch);
        }

        /// <summary>
        /// Begins a new connection for fetching from the remote repository.
        /// </summary>
        /// <returns>a fresh connection to fetch from the remote repository.</returns>
        public abstract IFetchConnection openFetch();

        /// <summary>
        /// Begins a new connection for pushing into the remote repository.
        /// </summary>
        /// <returns>a fresh connection to push into the remote repository.</returns>
        public abstract IPushConnection openPush();

        /// <summary>
        /// Close any resources used by this transport.
        /// <para/>
        /// If the remote repository is contacted by a network socket this method
        /// must close that network socket, disconnecting the two peers. If the
        /// remote repository is actually local (same system) this method must close
        /// any open file handles used to read the "remote" repository.
        /// </summary>
        public abstract void close();

        public virtual void Dispose()
        {
            close();
        }
    }
}