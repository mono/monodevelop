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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NGit;
using NGit.Errors;
using NGit.Storage.Pack;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Connects two Git repositories together and copies objects between them.</summary>
	/// <remarks>
	/// Connects two Git repositories together and copies objects between them.
	/// <p>
	/// A transport can be used for either fetching (copying objects into the
	/// caller's repository from the remote repository) or pushing (copying objects
	/// into the remote repository from the caller's repository). Each transport
	/// implementation is responsible for the details associated with establishing
	/// the network connection(s) necessary for the copy, as well as actually
	/// shuffling data back and forth.
	/// <p>
	/// Transport instances and the connections they create are not thread-safe.
	/// Callers must ensure a transport is accessed by only one thread at a time.
	/// </remarks>
	public abstract class Transport
	{
		/// <summary>Type of operation a Transport is being opened for.</summary>
		/// <remarks>Type of operation a Transport is being opened for.</remarks>
		public enum Operation
		{
			FETCH,
			PUSH
		}

		private static readonly IList<WeakReference<TransportProtocol>> protocols = new CopyOnWriteArrayList
			<WeakReference<TransportProtocol>>();

		static Transport()
		{
			// Registration goes backwards in order of priority.
			Register(TransportLocal.PROTO_LOCAL);
			Register(TransportBundleFile.PROTO_BUNDLE);
//			Register(TransportAmazonS3.PROTO_S3);
			Register(TransportGitAnon.PROTO_GIT);
			Register(TransportSftp.PROTO_SFTP);
			Register(TransportHttp.PROTO_FTP);
			Register(TransportHttp.PROTO_HTTP);
			Register(TransportGitSsh.PROTO_SSH);
//			RegisterByService();
		}

/*		private static void RegisterByService()
		{
			ClassLoader ldr = Sharpen.Thread.CurrentThread().GetContextClassLoader();
			if (ldr == null)
			{
				ldr = typeof(NGit.Transport.Transport).GetClassLoader();
			}
			Enumeration<Uri> catalogs = Catalogs(ldr);
			while (catalogs.MoveNext())
			{
				Scan(ldr, catalogs.Current);
			}
		}

		private static Enumeration<Uri> Catalogs(ClassLoader ldr)
		{
			try
			{
				string prefix = "META-INF/services/";
				string name = prefix + typeof(NGit.Transport.Transport).FullName;
				return ldr.GetResources(name);
			}
			catch (IOException)
			{
				return new Vector<Uri>().GetEnumerator();
			}
		}

		private static void Scan(ClassLoader ldr, Uri url)
		{
			BufferedReader br;
			try
			{
				InputStream urlIn = url.OpenStream();
				br = new BufferedReader(new InputStreamReader(urlIn, "UTF-8"));
			}
			catch (IOException)
			{
				// If we cannot read from the service list, go to the next.
				//
				return;
			}
			try
			{
				string line;
				while ((line = br.ReadLine()) != null)
				{
					if (line.Length > 0 && !line.StartsWith("#"))
					{
						Load(ldr, line);
					}
				}
			}
			catch (IOException)
			{
			}
			finally
			{
				// If we failed during a read, ignore the error.
				//
				try
				{
					br.Close();
				}
				catch (IOException)
				{
				}
			}
		}

		// Ignore the close error; we are only reading.
		private static void Load(ClassLoader ldr, string cn)
		{
			Type clazz;
			try
			{
				clazz = Sharpen.Runtime.GetType(cn, false, ldr);
			}
			catch (TypeLoadException)
			{
				// Doesn't exist, even though the service entry is present.
				//
				return;
			}
			foreach (FieldInfo f in Sharpen.Runtime.GetDeclaredFields(clazz))
			{
				if ((f.GetModifiers() & Modifier.STATIC) == Modifier.STATIC && typeof(TransportProtocol
					).IsAssignableFrom(f.FieldType))
				{
					TransportProtocol proto;
					try
					{
						proto = (TransportProtocol)f.GetValue(null);
					}
					catch (ArgumentException)
					{
						// If we cannot access the field, don't.
						continue;
					}
					catch (MemberAccessException)
					{
						// If we cannot access the field, don't.
						continue;
					}
					if (proto != null)
					{
						Register(proto);
					}
				}
			}
		}
		*/

		/// <summary>Register a TransportProtocol instance for use during open.</summary>
		/// <remarks>
		/// Register a TransportProtocol instance for use during open.
		/// <p>
		/// Protocol definitions are held by WeakReference, allowing them to be
		/// garbage collected when the calling application drops all strongly held
		/// references to the TransportProtocol. Therefore applications should use a
		/// singleton pattern as described in
		/// <see cref="TransportProtocol">TransportProtocol</see>
		/// 's class
		/// documentation to ensure their protocol does not get disabled by garbage
		/// collection earlier than expected.
		/// <p>
		/// The new protocol is registered in front of all earlier protocols, giving
		/// it higher priority than the built-in protocol definitions.
		/// </remarks>
		/// <param name="proto">the protocol definition. Must not be null.</param>
		public static void Register(TransportProtocol proto)
		{
			protocols.Add(0, new WeakReference<TransportProtocol>(proto));
		}

		/// <summary>Unregister a TransportProtocol instance.</summary>
		/// <remarks>
		/// Unregister a TransportProtocol instance.
		/// <p>
		/// Unregistering a protocol usually isn't necessary, as protocols are held
		/// by weak references and will automatically clear when they are garbage
		/// collected by the JVM. Matching is handled by reference equality, so the
		/// exact reference given to
		/// <see cref="Register(TransportProtocol)">Register(TransportProtocol)</see>
		/// must be
		/// used.
		/// </remarks>
		/// <param name="proto">the exact object previously given to register.</param>
		public static void Unregister(TransportProtocol proto)
		{
			foreach (WeakReference<TransportProtocol> @ref in protocols)
			{
				TransportProtocol refProto = @ref.Get();
				if (refProto == null || refProto == proto)
				{
					protocols.Remove(@ref);
				}
			}
		}

		/// <summary>Obtain a copy of the registered protocols.</summary>
		/// <remarks>Obtain a copy of the registered protocols.</remarks>
		/// <returns>an immutable copy of the currently registered protocols.</returns>
		public static IList<TransportProtocol> GetTransportProtocols()
		{
			int cnt = protocols.Count;
			IList<TransportProtocol> res = new AList<TransportProtocol>(cnt);
			foreach (WeakReference<TransportProtocol> @ref in protocols)
			{
				TransportProtocol proto = @ref.Get();
				if (proto != null)
				{
					res.AddItem(proto);
				}
				else
				{
					protocols.Remove(@ref);
				}
			}
			return Sharpen.Collections.UnmodifiableList(res);
		}

		/// <summary>Open a new transport instance to connect two repositories.</summary>
		/// <remarks>
		/// Open a new transport instance to connect two repositories.
		/// <p>
		/// This method assumes
		/// <see cref="Operation.FETCH">Operation.FETCH</see>
		/// .
		/// </remarks>
		/// <param name="local">existing local repository.</param>
		/// <param name="remote">
		/// location of the remote repository - may be URI or remote
		/// configuration name.
		/// </param>
		/// <returns>
		/// the new transport instance. Never null. In case of multiple URIs
		/// in remote configuration, only the first is chosen.
		/// </returns>
		/// <exception cref="Sharpen.URISyntaxException">
		/// the location is not a remote defined in the configuration
		/// file and is not a well-formed URL.
		/// </exception>
		/// <exception cref="System.NotSupportedException">the protocol specified is not supported.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">the transport cannot open this URI.
		/// 	</exception>
		public static NGit.Transport.Transport Open(Repository local, string remote)
		{
			return Open(local, remote, Transport.Operation.FETCH);
		}

		/// <summary>Open a new transport instance to connect two repositories.</summary>
		/// <remarks>Open a new transport instance to connect two repositories.</remarks>
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
		/// the new transport instance. Never null. In case of multiple URIs
		/// in remote configuration, only the first is chosen.
		/// </returns>
		/// <exception cref="Sharpen.URISyntaxException">
		/// the location is not a remote defined in the configuration
		/// file and is not a well-formed URL.
		/// </exception>
		/// <exception cref="System.NotSupportedException">the protocol specified is not supported.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">the transport cannot open this URI.
		/// 	</exception>
		public static NGit.Transport.Transport Open(Repository local, string remote, Transport.Operation
			 op)
		{
			RemoteConfig cfg = new RemoteConfig(local.GetConfig(), remote);
			if (DoesNotExist(cfg))
			{
				return Open(local, new URIish(remote), null);
			}
			return Open(local, cfg, op);
		}

		/// <summary>Open new transport instances to connect two repositories.</summary>
		/// <remarks>
		/// Open new transport instances to connect two repositories.
		/// <p>
		/// This method assumes
		/// <see cref="Operation.FETCH">Operation.FETCH</see>
		/// .
		/// </remarks>
		/// <param name="local">existing local repository.</param>
		/// <param name="remote">
		/// location of the remote repository - may be URI or remote
		/// configuration name.
		/// </param>
		/// <returns>
		/// the list of new transport instances for every URI in remote
		/// configuration.
		/// </returns>
		/// <exception cref="Sharpen.URISyntaxException">
		/// the location is not a remote defined in the configuration
		/// file and is not a well-formed URL.
		/// </exception>
		/// <exception cref="System.NotSupportedException">the protocol specified is not supported.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">the transport cannot open this URI.
		/// 	</exception>
		public static IList<NGit.Transport.Transport> OpenAll(Repository local, string remote
			)
		{
			return OpenAll(local, remote, Transport.Operation.FETCH);
		}

		/// <summary>Open new transport instances to connect two repositories.</summary>
		/// <remarks>Open new transport instances to connect two repositories.</remarks>
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
		/// <exception cref="Sharpen.URISyntaxException">
		/// the location is not a remote defined in the configuration
		/// file and is not a well-formed URL.
		/// </exception>
		/// <exception cref="System.NotSupportedException">the protocol specified is not supported.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">the transport cannot open this URI.
		/// 	</exception>
		public static IList<NGit.Transport.Transport> OpenAll(Repository local, string remote
			, Transport.Operation op)
		{
			RemoteConfig cfg = new RemoteConfig(local.GetConfig(), remote);
			if (DoesNotExist(cfg))
			{
				AList<NGit.Transport.Transport> transports = new AList<NGit.Transport.Transport>(
					1);
				transports.AddItem(Open(local, new URIish(remote), null));
				return transports;
			}
			return OpenAll(local, cfg, op);
		}

		/// <summary>Open a new transport instance to connect two repositories.</summary>
		/// <remarks>
		/// Open a new transport instance to connect two repositories.
		/// <p>
		/// This method assumes
		/// <see cref="Operation.FETCH">Operation.FETCH</see>
		/// .
		/// </remarks>
		/// <param name="local">existing local repository.</param>
		/// <param name="cfg">
		/// configuration describing how to connect to the remote
		/// repository.
		/// </param>
		/// <returns>
		/// the new transport instance. Never null. In case of multiple URIs
		/// in remote configuration, only the first is chosen.
		/// </returns>
		/// <exception cref="System.NotSupportedException">the protocol specified is not supported.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">the transport cannot open this URI.
		/// 	</exception>
		/// <exception cref="System.ArgumentException">
		/// if provided remote configuration doesn't have any URI
		/// associated.
		/// </exception>
		public static NGit.Transport.Transport Open(Repository local, RemoteConfig cfg)
		{
			return Open(local, cfg, Transport.Operation.FETCH);
		}

		/// <summary>Open a new transport instance to connect two repositories.</summary>
		/// <remarks>Open a new transport instance to connect two repositories.</remarks>
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
		/// the new transport instance. Never null. In case of multiple URIs
		/// in remote configuration, only the first is chosen.
		/// </returns>
		/// <exception cref="System.NotSupportedException">the protocol specified is not supported.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">the transport cannot open this URI.
		/// 	</exception>
		/// <exception cref="System.ArgumentException">
		/// if provided remote configuration doesn't have any URI
		/// associated.
		/// </exception>
		public static NGit.Transport.Transport Open(Repository local, RemoteConfig cfg, Transport.Operation
			 op)
		{
			IList<URIish> uris = GetURIs(cfg, op);
			if (uris.IsEmpty())
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().remoteConfigHasNoURIAssociated
					, cfg.Name));
			}
			NGit.Transport.Transport tn = Open(local, uris[0], cfg.Name);
			tn.ApplyConfig(cfg);
			return tn;
		}

		/// <summary>Open new transport instances to connect two repositories.</summary>
		/// <remarks>
		/// Open new transport instances to connect two repositories.
		/// <p>
		/// This method assumes
		/// <see cref="Operation.FETCH">Operation.FETCH</see>
		/// .
		/// </remarks>
		/// <param name="local">existing local repository.</param>
		/// <param name="cfg">
		/// configuration describing how to connect to the remote
		/// repository.
		/// </param>
		/// <returns>
		/// the list of new transport instances for every URI in remote
		/// configuration.
		/// </returns>
		/// <exception cref="System.NotSupportedException">the protocol specified is not supported.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">the transport cannot open this URI.
		/// 	</exception>
		public static IList<NGit.Transport.Transport> OpenAll(Repository local, RemoteConfig
			 cfg)
		{
			return OpenAll(local, cfg, Transport.Operation.FETCH);
		}

		/// <summary>Open new transport instances to connect two repositories.</summary>
		/// <remarks>Open new transport instances to connect two repositories.</remarks>
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
		/// <exception cref="System.NotSupportedException">the protocol specified is not supported.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">the transport cannot open this URI.
		/// 	</exception>
		public static IList<NGit.Transport.Transport> OpenAll(Repository local, RemoteConfig
			 cfg, Transport.Operation op)
		{
			IList<URIish> uris = GetURIs(cfg, op);
			IList<NGit.Transport.Transport> transports = new AList<NGit.Transport.Transport>(
				uris.Count);
			foreach (URIish uri in uris)
			{
				NGit.Transport.Transport tn = Open(local, uri, cfg.Name);
				tn.ApplyConfig(cfg);
				transports.AddItem(tn);
			}
			return transports;
		}

		private static IList<URIish> GetURIs(RemoteConfig cfg, Transport.Operation op)
		{
			switch (op)
			{
				case Transport.Operation.FETCH:
				{
					return cfg.URIs;
				}

				case Transport.Operation.PUSH:
				{
					IList<URIish> uris = cfg.PushURIs;
					if (uris.IsEmpty())
					{
						uris = cfg.URIs;
					}
					return uris;
				}

				default:
				{
					throw new ArgumentException(op.ToString());
				}
			}
		}

		private static bool DoesNotExist(RemoteConfig cfg)
		{
			return cfg.URIs.IsEmpty() && cfg.PushURIs.IsEmpty();
		}

		/// <summary>Open a new transport instance to connect two repositories.</summary>
		/// <remarks>Open a new transport instance to connect two repositories.</remarks>
		/// <param name="local">existing local repository.</param>
		/// <param name="uri">location of the remote repository.</param>
		/// <returns>the new transport instance. Never null.</returns>
		/// <exception cref="System.NotSupportedException">the protocol specified is not supported.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">the transport cannot open this URI.
		/// 	</exception>
		public static NGit.Transport.Transport Open(Repository local, URIish uri)
		{
			return Open(local, uri, null);
		}

		/// <summary>Open a new transport instance to connect two repositories.</summary>
		/// <remarks>Open a new transport instance to connect two repositories.</remarks>
		/// <param name="local">existing local repository.</param>
		/// <param name="uri">location of the remote repository.</param>
		/// <param name="remoteName">
		/// name of the remote, if the remote as configured in
		/// <code>local</code>
		/// ; otherwise null.
		/// </param>
		/// <returns>the new transport instance. Never null.</returns>
		/// <exception cref="System.NotSupportedException">the protocol specified is not supported.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">the transport cannot open this URI.
		/// 	</exception>
		public static NGit.Transport.Transport Open(Repository local, URIish uri, string 
			remoteName)
		{
			foreach (WeakReference<TransportProtocol> @ref in protocols)
			{
				TransportProtocol proto = @ref.Get();
				if (proto == null)
				{
					protocols.Remove(@ref);
					continue;
				}
				if (proto.CanHandle(uri, local, remoteName))
				{
					return proto.Open(uri, local, remoteName);
				}
			}
			throw new NotSupportedException(MessageFormat.Format(JGitText.Get().URINotSupported
				, uri));
		}

		/// <summary>
		/// Convert push remote refs update specification from
		/// <see cref="RefSpec">RefSpec</see>
		/// form
		/// to
		/// <see cref="RemoteRefUpdate">RemoteRefUpdate</see>
		/// . Conversion expands wildcards by matching
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
		/// <returns>
		/// collection of set up
		/// <see cref="RemoteRefUpdate">RemoteRefUpdate</see>
		/// .
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// when problem occurred during conversion or specification set
		/// up: most probably, missing objects or refs.
		/// </exception>
		public static ICollection<RemoteRefUpdate> FindRemoteRefUpdatesFor(Repository db, 
			ICollection<RefSpec> specs, ICollection<RefSpec> fetchSpecs)
		{
			if (fetchSpecs == null)
			{
				fetchSpecs = Sharpen.Collections.EmptyList<RefSpec>();
			}
			IList<RemoteRefUpdate> result = new List<RemoteRefUpdate>();
			ICollection<RefSpec> procRefs = ExpandPushWildcardsFor(db, specs);
			foreach (RefSpec spec in procRefs)
			{
				string srcSpec = spec.GetSource();
				Ref srcRef = db.GetRef(srcSpec);
				if (srcRef != null)
				{
					srcSpec = srcRef.GetName();
				}
				string destSpec = spec.GetDestination();
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
					string n = srcRef.GetName();
					int kindEnd = n.IndexOf('/', Constants.R_REFS.Length);
					destSpec = Sharpen.Runtime.Substring(n, 0, kindEnd + 1) + destSpec;
				}
				bool forceUpdate = spec.IsForceUpdate();
				string localName = FindTrackingRefName(destSpec, fetchSpecs);
				RemoteRefUpdate rru = new RemoteRefUpdate(db, srcSpec, destSpec, forceUpdate, localName
					, null);
				result.AddItem(rru);
			}
			return result;
		}

		private static ICollection<RefSpec> ExpandPushWildcardsFor(Repository db, ICollection
			<RefSpec> specs)
		{
			IDictionary<string, Ref> localRefs = db.GetAllRefs();
			ICollection<RefSpec> procRefs = new HashSet<RefSpec>();
			foreach (RefSpec spec in specs)
			{
				if (spec.IsWildcard())
				{
					foreach (Ref localRef in localRefs.Values)
					{
						if (spec.MatchSource(localRef))
						{
							procRefs.AddItem(spec.ExpandFromSource(localRef));
						}
					}
				}
				else
				{
					procRefs.AddItem(spec);
				}
			}
			return procRefs;
		}

		private static string FindTrackingRefName(string remoteName, ICollection<RefSpec>
			 fetchSpecs)
		{
			// try to find matching tracking refs
			foreach (RefSpec fetchSpec in fetchSpecs)
			{
				if (fetchSpec.MatchSource(remoteName))
				{
					if (fetchSpec.IsWildcard())
					{
						return fetchSpec.ExpandFromSource(remoteName).GetDestination();
					}
					else
					{
						return fetchSpec.GetDestination();
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Default setting for
		/// <see cref="fetchThin">fetchThin</see>
		/// option.
		/// </summary>
		public const bool DEFAULT_FETCH_THIN = true;

		/// <summary>
		/// Default setting for
		/// <see cref="pushThin">pushThin</see>
		/// option.
		/// </summary>
		public const bool DEFAULT_PUSH_THIN = false;

		/// <summary>Specification for fetch or push operations, to fetch or push all tags.</summary>
		/// <remarks>
		/// Specification for fetch or push operations, to fetch or push all tags.
		/// Acts as --tags.
		/// </remarks>
		public static readonly RefSpec REFSPEC_TAGS = new RefSpec("refs/tags/*:refs/tags/*"
			);

		/// <summary>Specification for push operation, to push all refs under refs/heads.</summary>
		/// <remarks>
		/// Specification for push operation, to push all refs under refs/heads. Acts
		/// as --all.
		/// </remarks>
		public static readonly RefSpec REFSPEC_PUSH_ALL = new RefSpec("refs/heads/*:refs/heads/*"
			);

		/// <summary>The repository this transport fetches into, or pushes out of.</summary>
		/// <remarks>The repository this transport fetches into, or pushes out of.</remarks>
		protected internal readonly Repository local;

		/// <summary>The URI used to create this transport.</summary>
		/// <remarks>The URI used to create this transport.</remarks>
		protected internal readonly URIish uri;

		/// <summary>Name of the upload pack program, if it must be executed.</summary>
		/// <remarks>Name of the upload pack program, if it must be executed.</remarks>
		private string optionUploadPack = RemoteConfig.DEFAULT_UPLOAD_PACK;

		/// <summary>Specifications to apply during fetch.</summary>
		/// <remarks>Specifications to apply during fetch.</remarks>
		private IList<RefSpec> fetch = Sharpen.Collections.EmptyList<RefSpec>();

		/// <summary>
		/// How
		/// <see cref="Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E})
		/// 	">Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;)</see>
		/// should handle tags.
		/// <p>
		/// We default to
		/// <see cref="TagOpt.NO_TAGS">TagOpt.NO_TAGS</see>
		/// so as to avoid fetching annotated
		/// tags during one-shot fetches used for later merges. This prevents
		/// dragging down tags from repositories that we do not have established
		/// tracking branches for. If we do not track the source repository, we most
		/// likely do not care about any tags it publishes.
		/// </summary>
		private TagOpt tagopt = TagOpt.NO_TAGS;

		/// <summary>Should fetch request thin-pack if remote repository can produce it.</summary>
		/// <remarks>Should fetch request thin-pack if remote repository can produce it.</remarks>
		private bool fetchThin = DEFAULT_FETCH_THIN;

		/// <summary>Name of the receive pack program, if it must be executed.</summary>
		/// <remarks>Name of the receive pack program, if it must be executed.</remarks>
		private string optionReceivePack = RemoteConfig.DEFAULT_RECEIVE_PACK;

		/// <summary>Specifications to apply during push.</summary>
		/// <remarks>Specifications to apply during push.</remarks>
		private IList<RefSpec> push = Sharpen.Collections.EmptyList<RefSpec>();

		/// <summary>Should push produce thin-pack when sending objects to remote repository.
		/// 	</summary>
		/// <remarks>Should push produce thin-pack when sending objects to remote repository.
		/// 	</remarks>
		private bool pushThin = DEFAULT_PUSH_THIN;

		/// <summary>Should push just check for operation result, not really push.</summary>
		/// <remarks>Should push just check for operation result, not really push.</remarks>
		private bool dryRun;

		/// <summary>Should an incoming (fetch) transfer validate objects?</summary>
		private bool checkFetchedObjects;

		/// <summary>Should refs no longer on the source be pruned from the destination?</summary>
		private bool removeDeletedRefs;

		/// <summary>Timeout in seconds to wait before aborting an IO read or write.</summary>
		/// <remarks>Timeout in seconds to wait before aborting an IO read or write.</remarks>
		private int timeout;

		/// <summary>Pack configuration used by this transport to make pack file.</summary>
		/// <remarks>Pack configuration used by this transport to make pack file.</remarks>
		private PackConfig packConfig;

		/// <summary>Assists with authentication the connection.</summary>
		/// <remarks>Assists with authentication the connection.</remarks>
		private CredentialsProvider credentialsProvider;

		/// <summary>Create a new transport instance.</summary>
		/// <remarks>Create a new transport instance.</remarks>
		/// <param name="local">
		/// the repository this instance will fetch into, or push out of.
		/// This must be the repository passed to
		/// <see cref="Open(NGit.Repository, URIish)">Open(NGit.Repository, URIish)</see>
		/// .
		/// </param>
		/// <param name="uri">
		/// the URI used to access the remote repository. This must be the
		/// URI passed to
		/// <see cref="Open(NGit.Repository, URIish)">Open(NGit.Repository, URIish)</see>
		/// .
		/// </param>
		protected internal Transport(Repository local, URIish uri)
		{
			TransferConfig tc = local.GetConfig().Get(TransferConfig.KEY);
			this.local = local;
			this.uri = uri;
			this.checkFetchedObjects = tc.IsFsckObjects();
			this.credentialsProvider = CredentialsProvider.GetDefault();
		}

		/// <summary>Get the URI this transport connects to.</summary>
		/// <remarks>
		/// Get the URI this transport connects to.
		/// <p>
		/// Each transport instance connects to at most one URI at any point in time.
		/// </remarks>
		/// <returns>the URI describing the location of the remote repository.</returns>
		public virtual URIish GetURI()
		{
			return uri;
		}

		/// <summary>Get the name of the remote executable providing upload-pack service.</summary>
		/// <remarks>Get the name of the remote executable providing upload-pack service.</remarks>
		/// <returns>typically "git-upload-pack".</returns>
		public virtual string GetOptionUploadPack()
		{
			return optionUploadPack;
		}

		/// <summary>Set the name of the remote executable providing upload-pack services.</summary>
		/// <remarks>Set the name of the remote executable providing upload-pack services.</remarks>
		/// <param name="where">name of the executable.</param>
		public virtual void SetOptionUploadPack(string where)
		{
			if (where != null && where.Length > 0)
			{
				optionUploadPack = where;
			}
			else
			{
				optionUploadPack = RemoteConfig.DEFAULT_UPLOAD_PACK;
			}
		}

		/// <summary>Get the description of how annotated tags should be treated during fetch.
		/// 	</summary>
		/// <remarks>Get the description of how annotated tags should be treated during fetch.
		/// 	</remarks>
		/// <returns>option indicating the behavior of annotated tags in fetch.</returns>
		public virtual TagOpt GetTagOpt()
		{
			return tagopt;
		}

		/// <summary>Set the description of how annotated tags should be treated on fetch.</summary>
		/// <remarks>Set the description of how annotated tags should be treated on fetch.</remarks>
		/// <param name="option">method to use when handling annotated tags.</param>
		public virtual void SetTagOpt(TagOpt option)
		{
			tagopt = option != null ? option : TagOpt.AUTO_FOLLOW;
		}

		/// <summary>
		/// Default setting is:
		/// <see cref="DEFAULT_FETCH_THIN">DEFAULT_FETCH_THIN</see>
		/// </summary>
		/// <returns>
		/// true if fetch should request thin-pack when possible; false
		/// otherwise
		/// </returns>
		/// <seealso cref="PackTransport">PackTransport</seealso>
		public virtual bool IsFetchThin()
		{
			return fetchThin;
		}

		/// <summary>Set the thin-pack preference for fetch operation.</summary>
		/// <remarks>
		/// Set the thin-pack preference for fetch operation. Default setting is:
		/// <see cref="DEFAULT_FETCH_THIN">DEFAULT_FETCH_THIN</see>
		/// </remarks>
		/// <param name="fetchThin">
		/// true when fetch should request thin-pack when possible; false
		/// when it shouldn't
		/// </param>
		/// <seealso cref="PackTransport">PackTransport</seealso>
		public virtual void SetFetchThin(bool fetchThin)
		{
			this.fetchThin = fetchThin;
		}

		/// <returns>
		/// true if fetch will verify received objects are formatted
		/// correctly. Validating objects requires more CPU time on the
		/// client side of the connection.
		/// </returns>
		public virtual bool IsCheckFetchedObjects()
		{
			return checkFetchedObjects;
		}

		/// <param name="check">
		/// true to enable checking received objects; false to assume all
		/// received objects are valid.
		/// </param>
		public virtual void SetCheckFetchedObjects(bool check)
		{
			checkFetchedObjects = check;
		}

		/// <summary>
		/// Default setting is:
		/// <see cref="RemoteConfig.DEFAULT_RECEIVE_PACK">RemoteConfig.DEFAULT_RECEIVE_PACK</see>
		/// </summary>
		/// <returns>
		/// remote executable providing receive-pack service for pack
		/// transports.
		/// </returns>
		/// <seealso cref="PackTransport">PackTransport</seealso>
		public virtual string GetOptionReceivePack()
		{
			return optionReceivePack;
		}

		/// <summary>Set remote executable providing receive-pack service for pack transports.
		/// 	</summary>
		/// <remarks>
		/// Set remote executable providing receive-pack service for pack transports.
		/// Default setting is:
		/// <see cref="RemoteConfig.DEFAULT_RECEIVE_PACK">RemoteConfig.DEFAULT_RECEIVE_PACK</see>
		/// </remarks>
		/// <param name="optionReceivePack">remote executable, if null or empty default one is set;
		/// 	</param>
		public virtual void SetOptionReceivePack(string optionReceivePack)
		{
			if (optionReceivePack != null && optionReceivePack.Length > 0)
			{
				this.optionReceivePack = optionReceivePack;
			}
			else
			{
				this.optionReceivePack = RemoteConfig.DEFAULT_RECEIVE_PACK;
			}
		}

		/// <summary>
		/// Default setting is:
		/// <value>#DEFAULT_PUSH_THIN</value>
		/// </summary>
		/// <returns>true if push should produce thin-pack in pack transports</returns>
		/// <seealso cref="PackTransport">PackTransport</seealso>
		public virtual bool IsPushThin()
		{
			return pushThin;
		}

		/// <summary>Set thin-pack preference for push operation.</summary>
		/// <remarks>
		/// Set thin-pack preference for push operation. Default setting is:
		/// <value>#DEFAULT_PUSH_THIN</value>
		/// </remarks>
		/// <param name="pushThin">
		/// true when push should produce thin-pack in pack transports;
		/// false when it shouldn't
		/// </param>
		/// <seealso cref="PackTransport">PackTransport</seealso>
		public virtual void SetPushThin(bool pushThin)
		{
			this.pushThin = pushThin;
		}

		/// <returns>
		/// true if destination refs should be removed if they no longer
		/// exist at the source repository.
		/// </returns>
		public virtual bool IsRemoveDeletedRefs()
		{
			return removeDeletedRefs;
		}

		/// <summary>Set whether or not to remove refs which no longer exist in the source.</summary>
		/// <remarks>
		/// Set whether or not to remove refs which no longer exist in the source.
		/// <p>
		/// If true, refs at the destination repository (local for fetch, remote for
		/// push) are deleted if they no longer exist on the source side (remote for
		/// fetch, local for push).
		/// <p>
		/// False by default, as this may cause data to become unreachable, and
		/// eventually be deleted on the next GC.
		/// </remarks>
		/// <param name="remove">true to remove refs that no longer exist.</param>
		public virtual void SetRemoveDeletedRefs(bool remove)
		{
			removeDeletedRefs = remove;
		}

		/// <summary>Apply provided remote configuration on this transport.</summary>
		/// <remarks>Apply provided remote configuration on this transport.</remarks>
		/// <param name="cfg">configuration to apply on this transport.</param>
		public virtual void ApplyConfig(RemoteConfig cfg)
		{
			SetOptionUploadPack(cfg.UploadPack);
			SetOptionReceivePack(cfg.ReceivePack);
			SetTagOpt(cfg.TagOpt);
			fetch = cfg.FetchRefSpecs;
			push = cfg.PushRefSpecs;
			timeout = cfg.Timeout;
		}

		/// <returns>
		/// true if push operation should just check for possible result and
		/// not really update remote refs, false otherwise - when push should
		/// act normally.
		/// </returns>
		public virtual bool IsDryRun()
		{
			return dryRun;
		}

		/// <summary>Set dry run option for push operation.</summary>
		/// <remarks>Set dry run option for push operation.</remarks>
		/// <param name="dryRun">
		/// true if push operation should just check for possible result
		/// and not really update remote refs, false otherwise - when push
		/// should act normally.
		/// </param>
		public virtual void SetDryRun(bool dryRun)
		{
			this.dryRun = dryRun;
		}

		/// <returns>timeout (in seconds) before aborting an IO operation.</returns>
		public virtual int GetTimeout()
		{
			return timeout;
		}

		/// <summary>Set the timeout before willing to abort an IO call.</summary>
		/// <remarks>Set the timeout before willing to abort an IO call.</remarks>
		/// <param name="seconds">
		/// number of seconds to wait (with no data transfer occurring)
		/// before aborting an IO read or write operation with this
		/// remote.
		/// </param>
		public virtual void SetTimeout(int seconds)
		{
			timeout = seconds;
		}

		/// <summary>Get the configuration used by the pack generator to make packs.</summary>
		/// <remarks>
		/// Get the configuration used by the pack generator to make packs.
		/// If
		/// <see cref="SetPackConfig(NGit.Storage.Pack.PackConfig)">SetPackConfig(NGit.Storage.Pack.PackConfig)
		/// 	</see>
		/// was previously given null a new
		/// PackConfig is created on demand by this method using the source
		/// repository's settings.
		/// </remarks>
		/// <returns>the pack configuration. Never null.</returns>
		public virtual PackConfig GetPackConfig()
		{
			if (packConfig == null)
			{
				packConfig = new PackConfig(local);
			}
			return packConfig;
		}

		/// <summary>Set the configuration used by the pack generator.</summary>
		/// <remarks>Set the configuration used by the pack generator.</remarks>
		/// <param name="pc">
		/// configuration controlling packing parameters. If null the
		/// source repository's settings will be used.
		/// </param>
		public virtual void SetPackConfig(PackConfig pc)
		{
			packConfig = pc;
		}

		/// <summary>A credentials provider to assist with authentication connections..</summary>
		/// <remarks>A credentials provider to assist with authentication connections..</remarks>
		/// <param name="credentialsProvider">the credentials provider, or null if there is none
		/// 	</param>
		public virtual void SetCredentialsProvider(CredentialsProvider credentialsProvider
			)
		{
			this.credentialsProvider = credentialsProvider;
		}

		/// <summary>The configured credentials provider.</summary>
		/// <remarks>The configured credentials provider.</remarks>
		/// <returns>
		/// the credentials provider, or null if no credentials provider is
		/// associated with this transport.
		/// </returns>
		public virtual CredentialsProvider GetCredentialsProvider()
		{
			return credentialsProvider;
		}

		/// <summary>Fetch objects and refs from the remote repository to the local one.</summary>
		/// <remarks>
		/// Fetch objects and refs from the remote repository to the local one.
		/// <p>
		/// This is a utility function providing standard fetch behavior. Local
		/// tracking refs associated with the remote repository are automatically
		/// updated if this transport was created from a
		/// <see cref="RemoteConfig">RemoteConfig</see>
		/// with
		/// fetch RefSpecs defined.
		/// </remarks>
		/// <param name="monitor">
		/// progress monitor to inform the user about our processing
		/// activity. Must not be null. Use
		/// <see cref="NGit.NullProgressMonitor">NGit.NullProgressMonitor</see>
		/// if
		/// progress updates are not interesting or necessary.
		/// </param>
		/// <param name="toFetch">
		/// specification of refs to fetch locally. May be null or the
		/// empty collection to use the specifications from the
		/// RemoteConfig. Source for each RefSpec can't be null.
		/// </param>
		/// <returns>information describing the tracking refs updated.</returns>
		/// <exception cref="System.NotSupportedException">
		/// this transport implementation does not support fetching
		/// objects.
		/// </exception>
		/// <exception cref="NGit.Errors.TransportException">
		/// the remote connection could not be established or object
		/// copying (if necessary) failed or update specification was
		/// incorrect.
		/// </exception>
		public virtual FetchResult Fetch(ProgressMonitor monitor, ICollection<RefSpec> toFetch
			)
		{
			if (toFetch == null || toFetch.IsEmpty())
			{
				// If the caller did not ask for anything use the defaults.
				//
				if (fetch.IsEmpty())
				{
					throw new TransportException(JGitText.Get().nothingToFetch);
				}
				toFetch = fetch;
			}
			else
			{
				if (!fetch.IsEmpty())
				{
					// If the caller asked for something specific without giving
					// us the local tracking branch see if we can update any of
					// the local tracking branches without incurring additional
					// object transfer overheads.
					//
					ICollection<RefSpec> tmp = new AList<RefSpec>(toFetch);
					foreach (RefSpec requested in toFetch)
					{
						string reqSrc = requested.GetSource();
						foreach (RefSpec configured in fetch)
						{
							string cfgSrc = configured.GetSource();
							string cfgDst = configured.GetDestination();
							if (cfgSrc.Equals(reqSrc) && cfgDst != null)
							{
								tmp.AddItem(configured);
								break;
							}
						}
					}
					toFetch = tmp;
				}
			}
			FetchResult result = new FetchResult();
			new FetchProcess(this, toFetch).Execute(monitor, result);
			return result;
		}

		/// <summary>Push objects and refs from the local repository to the remote one.</summary>
		/// <remarks>
		/// Push objects and refs from the local repository to the remote one.
		/// <p>
		/// This is a utility function providing standard push behavior. It updates
		/// remote refs and send there necessary objects according to remote ref
		/// update specification. After successful remote ref update, associated
		/// locally stored tracking branch is updated if set up accordingly. Detailed
		/// operation result is provided after execution.
		/// <p>
		/// For setting up remote ref update specification from ref spec, see helper
		/// method
		/// <see cref="FindRemoteRefUpdatesFor(System.Collections.Generic.ICollection{E})">FindRemoteRefUpdatesFor(System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// , predefined refspecs
		/// (
		/// <see cref="REFSPEC_TAGS">REFSPEC_TAGS</see>
		/// ,
		/// <see cref="REFSPEC_PUSH_ALL">REFSPEC_PUSH_ALL</see>
		/// ) or consider using
		/// directly
		/// <see cref="RemoteRefUpdate">RemoteRefUpdate</see>
		/// for more possibilities.
		/// <p>
		/// When
		/// <see cref="IsDryRun()">IsDryRun()</see>
		/// is true, result of this operation is just
		/// estimation of real operation result, no real action is performed.
		/// </remarks>
		/// <seealso cref="RemoteRefUpdate">RemoteRefUpdate</seealso>
		/// <param name="monitor">
		/// progress monitor to inform the user about our processing
		/// activity. Must not be null. Use
		/// <see cref="NGit.NullProgressMonitor">NGit.NullProgressMonitor</see>
		/// if
		/// progress updates are not interesting or necessary.
		/// </param>
		/// <param name="toPush">
		/// specification of refs to push. May be null or the empty
		/// collection to use the specifications from the RemoteConfig
		/// converted by
		/// <see cref="FindRemoteRefUpdatesFor(System.Collections.Generic.ICollection{E})">FindRemoteRefUpdatesFor(System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// . No
		/// more than 1 RemoteRefUpdate with the same remoteName is
		/// allowed. These objects are modified during this call.
		/// </param>
		/// <returns>
		/// information about results of remote refs updates, tracking refs
		/// updates and refs advertised by remote repository.
		/// </returns>
		/// <exception cref="System.NotSupportedException">
		/// this transport implementation does not support pushing
		/// objects.
		/// </exception>
		/// <exception cref="NGit.Errors.TransportException">
		/// the remote connection could not be established or object
		/// copying (if necessary) failed at I/O or protocol level or
		/// update specification was incorrect.
		/// </exception>
		public virtual PushResult Push(ProgressMonitor monitor, ICollection<RemoteRefUpdate
			> toPush)
		{
			if (toPush == null || toPush.IsEmpty())
			{
				// If the caller did not ask for anything use the defaults.
				try
				{
					toPush = FindRemoteRefUpdatesFor(push);
				}
				catch (IOException e)
				{
					throw new TransportException(MessageFormat.Format(JGitText.Get().problemWithResolvingPushRefSpecsLocally
						, e.Message), e);
				}
				if (toPush.IsEmpty())
				{
					throw new TransportException(JGitText.Get().nothingToPush);
				}
			}
			PushProcess pushProcess = new PushProcess(this, toPush);
			return pushProcess.Execute(monitor);
		}

		/// <summary>
		/// Convert push remote refs update specification from
		/// <see cref="RefSpec">RefSpec</see>
		/// form
		/// to
		/// <see cref="RemoteRefUpdate">RemoteRefUpdate</see>
		/// . Conversion expands wildcards by matching
		/// source part to local refs. expectedOldObjectId in RemoteRefUpdate is
		/// always set as null. Tracking branch is configured if RefSpec destination
		/// matches source of any fetch ref spec for this transport remote
		/// configuration.
		/// <p>
		/// Conversion is performed for context of this transport (database, fetch
		/// specifications).
		/// </summary>
		/// <param name="specs">collection of RefSpec to convert.</param>
		/// <returns>
		/// collection of set up
		/// <see cref="RemoteRefUpdate">RemoteRefUpdate</see>
		/// .
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// when problem occurred during conversion or specification set
		/// up: most probably, missing objects or refs.
		/// </exception>
		public virtual ICollection<RemoteRefUpdate> FindRemoteRefUpdatesFor(ICollection<RefSpec
			> specs)
		{
			return FindRemoteRefUpdatesFor(local, specs, fetch);
		}

		/// <summary>Begins a new connection for fetching from the remote repository.</summary>
		/// <remarks>Begins a new connection for fetching from the remote repository.</remarks>
		/// <returns>a fresh connection to fetch from the remote repository.</returns>
		/// <exception cref="System.NotSupportedException">the implementation does not support fetching.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">the remote connection could not be established.
		/// 	</exception>
		public abstract FetchConnection OpenFetch();

		/// <summary>Begins a new connection for pushing into the remote repository.</summary>
		/// <remarks>Begins a new connection for pushing into the remote repository.</remarks>
		/// <returns>a fresh connection to push into the remote repository.</returns>
		/// <exception cref="System.NotSupportedException">the implementation does not support pushing.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">the remote connection could not be established
		/// 	</exception>
		public abstract PushConnection OpenPush();

		/// <summary>Close any resources used by this transport.</summary>
		/// <remarks>
		/// Close any resources used by this transport.
		/// <p>
		/// If the remote repository is contacted by a network socket this method
		/// must close that network socket, disconnecting the two peers. If the
		/// remote repository is actually local (same system) this method must close
		/// any open file handles used to read the "remote" repository.
		/// </remarks>
		public abstract void Close();
	}
}
