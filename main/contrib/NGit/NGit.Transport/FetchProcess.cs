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
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Revwalk;
using NGit.Storage.File;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	internal class FetchProcess
	{
		/// <summary>Transport we will fetch over.</summary>
		/// <remarks>Transport we will fetch over.</remarks>
		private readonly NGit.Transport.Transport transport;

		/// <summary>List of things we want to fetch from the remote repository.</summary>
		/// <remarks>List of things we want to fetch from the remote repository.</remarks>
		private readonly ICollection<RefSpec> toFetch;

		/// <summary>Set of refs we will actually wind up asking to obtain.</summary>
		/// <remarks>Set of refs we will actually wind up asking to obtain.</remarks>
		private readonly Dictionary<ObjectId, Ref> askFor = new Dictionary<ObjectId, Ref>
			();

		/// <summary>Objects we know we have locally.</summary>
		/// <remarks>Objects we know we have locally.</remarks>
		private readonly HashSet<ObjectId> have = new HashSet<ObjectId>();

		/// <summary>Updates to local tracking branches (if any).</summary>
		/// <remarks>Updates to local tracking branches (if any).</remarks>
		private readonly AList<TrackingRefUpdate> localUpdates = new AList<TrackingRefUpdate
			>();

		/// <summary>Records to be recorded into FETCH_HEAD.</summary>
		/// <remarks>Records to be recorded into FETCH_HEAD.</remarks>
		private readonly AList<FetchHeadRecord> fetchHeadUpdates = new AList<FetchHeadRecord
			>();

		private readonly AList<PackLock> packLocks = new AList<PackLock>();

		private FetchConnection conn;

		internal FetchProcess(NGit.Transport.Transport t, ICollection<RefSpec> f)
		{
			transport = t;
			toFetch = f;
		}

		/// <exception cref="System.NotSupportedException"></exception>
		/// <exception cref="NGit.Errors.TransportException"></exception>
		internal virtual void Execute(ProgressMonitor monitor, FetchResult result)
		{
			askFor.Clear();
			localUpdates.Clear();
			fetchHeadUpdates.Clear();
			packLocks.Clear();
			try
			{
				ExecuteImp(monitor, result);
			}
			finally
			{
				try
				{
					foreach (PackLock Lock in packLocks)
					{
						Lock.Unlock();
					}
				}
				catch (IOException e)
				{
					throw new TransportException(e.Message, e);
				}
			}
		}

		/// <exception cref="System.NotSupportedException"></exception>
		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void ExecuteImp(ProgressMonitor monitor, FetchResult result)
		{
			conn = transport.OpenFetch();
			try
			{
				result.SetAdvertisedRefs(transport.GetURI(), conn.GetRefsMap());
				ICollection<Ref> matched = new HashSet<Ref>();
				foreach (RefSpec spec in toFetch)
				{
					if (spec.GetSource() == null)
					{
						throw new TransportException(MessageFormat.Format(JGitText.Get().sourceRefNotSpecifiedForRefspec
							, spec));
					}
					if (spec.IsWildcard())
					{
						ExpandWildcard(spec, matched);
					}
					else
					{
						ExpandSingle(spec, matched);
					}
				}
				ICollection<Ref> additionalTags = Sharpen.Collections.EmptyList<Ref>();
				TagOpt tagopt = transport.GetTagOpt();
				if (tagopt == TagOpt.AUTO_FOLLOW)
				{
					additionalTags = ExpandAutoFollowTags();
				}
				else
				{
					if (tagopt == TagOpt.FETCH_TAGS)
					{
						ExpandFetchTags();
					}
				}
				bool includedTags;
				if (!askFor.IsEmpty() && !AskForIsComplete())
				{
					FetchObjects(monitor);
					includedTags = conn.DidFetchIncludeTags();
					// Connection was used for object transfer. If we
					// do another fetch we must open a new connection.
					//
					CloseConnection(result);
				}
				else
				{
					includedTags = false;
				}
				if (tagopt == TagOpt.AUTO_FOLLOW && !additionalTags.IsEmpty())
				{
					// There are more tags that we want to follow, but
					// not all were asked for on the initial request.
					//
					Sharpen.Collections.AddAll(have, askFor.Keys);
					askFor.Clear();
					foreach (Ref r in additionalTags)
					{
						ObjectId id = r.GetPeeledObjectId();
						if (id == null)
						{
							id = r.GetObjectId();
						}
						if (transport.local.HasObject(id))
						{
							WantTag(r);
						}
					}
					if (!askFor.IsEmpty() && (!includedTags || !AskForIsComplete()))
					{
						ReopenConnection();
						if (!askFor.IsEmpty())
						{
							FetchObjects(monitor);
						}
					}
				}
			}
			finally
			{
				CloseConnection(result);
			}
			RevWalk walk = new RevWalk(transport.local);
			try
			{
				if (monitor is BatchingProgressMonitor)
				{
					((BatchingProgressMonitor)monitor).SetDelayStart(250, TimeUnit.MILLISECONDS);
				}
				monitor.BeginTask(JGitText.Get().updatingReferences, localUpdates.Count);
				if (transport.IsRemoveDeletedRefs())
				{
					DeleteStaleTrackingRefs(result, walk);
				}
				foreach (TrackingRefUpdate u in localUpdates)
				{
					try
					{
						monitor.Update(1);
						u.Update(walk);
						result.Add(u);
					}
					catch (IOException err)
					{
						throw new TransportException(MessageFormat.Format(JGitText.Get().failureUpdatingTrackingRef
							, u.GetLocalName(), err.Message), err);
					}
				}
				monitor.EndTask();
			}
			finally
			{
				walk.Release();
			}
			if (!fetchHeadUpdates.IsEmpty())
			{
				try
				{
					UpdateFETCH_HEAD(result);
				}
				catch (IOException err)
				{
					throw new TransportException(MessageFormat.Format(JGitText.Get().failureUpdatingFETCH_HEAD
						, err.Message), err);
				}
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void FetchObjects(ProgressMonitor monitor)
		{
			try
			{
				conn.SetPackLockMessage("jgit fetch " + transport.uri);
				conn.Fetch(monitor, askFor.Values, have);
			}
			finally
			{
				Sharpen.Collections.AddAll(packLocks, conn.GetPackLocks());
			}
			if (transport.IsCheckFetchedObjects() && !conn.DidFetchTestConnectivity() && !AskForIsComplete
				())
			{
				throw new TransportException(transport.GetURI(), JGitText.Get().peerDidNotSupplyACompleteObjectGraph
					);
			}
		}

		private void CloseConnection(FetchResult result)
		{
			if (conn != null)
			{
				conn.Close();
				result.AddMessages(conn.GetMessages());
				conn = null;
			}
		}

		/// <exception cref="System.NotSupportedException"></exception>
		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void ReopenConnection()
		{
			if (conn != null)
			{
				return;
			}
			conn = transport.OpenFetch();
			// Since we opened a new connection we cannot be certain
			// that the system we connected to has the same exact set
			// of objects available (think round-robin DNS and mirrors
			// that aren't updated at the same time).
			//
			// We rebuild our askFor list using only the refs that the
			// new connection has offered to us.
			//
			Dictionary<ObjectId, Ref> avail = new Dictionary<ObjectId, Ref>();
			foreach (Ref r in conn.GetRefs())
			{
				avail.Put(r.GetObjectId(), r);
			}
			ICollection<Ref> wants = new AList<Ref>(askFor.Values);
			askFor.Clear();
			foreach (Ref want in wants)
			{
				Ref newRef = avail.Get(want.GetObjectId());
				if (newRef != null)
				{
					askFor.Put(newRef.GetObjectId(), newRef);
				}
				else
				{
					RemoveFetchHeadRecord(want.GetObjectId());
					RemoveTrackingRefUpdate(want.GetObjectId());
				}
			}
		}

		private void RemoveTrackingRefUpdate(ObjectId want)
		{
			Iterator<TrackingRefUpdate> i = localUpdates.Iterator();
			while (i.HasNext())
			{
				TrackingRefUpdate u = i.Next();
				if (u.GetNewObjectId().Equals(want))
				{
					i.Remove();
				}
			}
		}

		private void RemoveFetchHeadRecord(ObjectId want)
		{
			Iterator<FetchHeadRecord> i = fetchHeadUpdates.Iterator();
			while (i.HasNext())
			{
				FetchHeadRecord fh = i.Next();
				if (fh.newValue.Equals(want))
				{
					i.Remove();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void UpdateFETCH_HEAD(FetchResult result)
		{
			FilePath meta = transport.local.Directory;
			if (meta == null)
			{
				return;
			}
			LockFile Lock = new LockFile(new FilePath(meta, "FETCH_HEAD"), transport.local.FileSystem
				);
			try
			{
				if (Lock.Lock())
				{
					TextWriter w = new OutputStreamWriter(Lock.GetOutputStream());
					try
					{
						foreach (FetchHeadRecord h in fetchHeadUpdates)
						{
							h.Write(w);
							result.Add(h);
						}
					}
					finally
					{
						w.Close();
					}
					Lock.Commit();
				}
			}
			finally
			{
				Lock.Unlock();
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private bool AskForIsComplete()
		{
			try
			{
				ObjectWalk ow = new ObjectWalk(transport.local);
				try
				{
					foreach (ObjectId want in askFor.Keys)
					{
						ow.MarkStart(ow.ParseAny(want));
					}
					foreach (Ref @ref in transport.local.GetAllRefs().Values)
					{
						ow.MarkUninteresting(ow.ParseAny(@ref.GetObjectId()));
					}
					ow.CheckConnectivity();
				}
				finally
				{
					ow.Release();
				}
				return true;
			}
			catch (MissingObjectException)
			{
				return false;
			}
			catch (IOException e)
			{
				throw new TransportException(JGitText.Get().unableToCheckConnectivity, e);
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void ExpandWildcard(RefSpec spec, ICollection<Ref> matched)
		{
			foreach (Ref src in conn.GetRefs())
			{
				if (spec.MatchSource(src) && matched.AddItem(src))
				{
					Want(src, spec.ExpandFromSource(src));
				}
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void ExpandSingle(RefSpec spec, ICollection<Ref> matched)
		{
			Ref src = conn.GetRef(spec.GetSource());
			if (src == null)
			{
				throw new TransportException(MessageFormat.Format(JGitText.Get().remoteDoesNotHaveSpec
					, spec.GetSource()));
			}
			if (matched.AddItem(src))
			{
				Want(src, spec);
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private ICollection<Ref> ExpandAutoFollowTags()
		{
			ICollection<Ref> additionalTags = new AList<Ref>();
			IDictionary<string, Ref> haveRefs = transport.local.GetAllRefs();
			foreach (Ref r in conn.GetRefs())
			{
				if (!IsTag(r))
				{
					continue;
				}
				Ref local = haveRefs.Get(r.GetName());
				ObjectId obj = r.GetObjectId();
				if (r.GetPeeledObjectId() == null)
				{
					if (local != null && obj.Equals(local.GetObjectId()))
					{
						continue;
					}
					if (askFor.ContainsKey(obj) || transport.local.HasObject(obj))
					{
						WantTag(r);
					}
					else
					{
						additionalTags.AddItem(r);
					}
					continue;
				}
				if (local != null)
				{
					if (!obj.Equals(local.GetObjectId()))
					{
						WantTag(r);
					}
				}
				else
				{
					if (askFor.ContainsKey(r.GetPeeledObjectId()) || transport.local.HasObject(r.GetPeeledObjectId
						()))
					{
						WantTag(r);
					}
					else
					{
						additionalTags.AddItem(r);
					}
				}
			}
			return additionalTags;
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void ExpandFetchTags()
		{
			IDictionary<string, Ref> haveRefs = transport.local.GetAllRefs();
			foreach (Ref r in conn.GetRefs())
			{
				if (!IsTag(r))
				{
					continue;
				}
				Ref local = haveRefs.Get(r.GetName());
				if (local == null || !r.GetObjectId().Equals(local.GetObjectId()))
				{
					WantTag(r);
				}
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void WantTag(Ref r)
		{
			Want(r, new RefSpec().SetSource(r.GetName()).SetDestination(r.GetName()));
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void Want(Ref src, RefSpec spec)
		{
			ObjectId newId = src.GetObjectId();
			if (spec.GetDestination() != null)
			{
				try
				{
					TrackingRefUpdate tru = CreateUpdate(spec, newId);
					if (newId.Equals(tru.GetOldObjectId()))
					{
						return;
					}
					localUpdates.AddItem(tru);
				}
				catch (IOException err)
				{
					// Bad symbolic ref? That is the most likely cause.
					//
					throw new TransportException(MessageFormat.Format(JGitText.Get().cannotResolveLocalTrackingRefForUpdating
						, spec.GetDestination()), err);
				}
			}
			askFor.Put(newId, src);
			FetchHeadRecord fhr = new FetchHeadRecord();
			fhr.newValue = newId;
			fhr.notForMerge = spec.GetDestination() != null;
			fhr.sourceName = src.GetName();
			fhr.sourceURI = transport.GetURI();
			fetchHeadUpdates.AddItem(fhr);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private TrackingRefUpdate CreateUpdate(RefSpec spec, ObjectId newId)
		{
			return new TrackingRefUpdate(transport.local, spec, newId, "fetch");
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void DeleteStaleTrackingRefs(FetchResult result, RevWalk walk)
		{
			Repository db = transport.local;
			foreach (Ref @ref in db.GetAllRefs().Values)
			{
				string refname = @ref.GetName();
				foreach (RefSpec spec in toFetch)
				{
					if (spec.MatchDestination(refname))
					{
						RefSpec s = spec.ExpandFromDestination(refname);
						if (result.GetAdvertisedRef(s.GetSource()) == null)
						{
							DeleteTrackingRef(result, db, walk, s, @ref);
						}
					}
				}
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void DeleteTrackingRef(FetchResult result, Repository db, RevWalk walk, RefSpec
			 spec, Ref localRef)
		{
			string name = localRef.GetName();
			try
			{
				TrackingRefUpdate u = new TrackingRefUpdate(db, name, spec.GetSource(), true, ObjectId
					.ZeroId, "deleted");
				result.Add(u);
				if (transport.IsDryRun())
				{
					return;
				}
				u.Delete(walk);
				switch (u.GetResult())
				{
					case RefUpdate.Result.NEW:
					case RefUpdate.Result.NO_CHANGE:
					case RefUpdate.Result.FAST_FORWARD:
					case RefUpdate.Result.FORCED:
					{
						break;
					}

					default:
					{
						throw new TransportException(transport.GetURI(), MessageFormat.Format(JGitText.Get
							().cannotDeleteStaleTrackingRef2, name, u.GetResult().ToString()));
					}
				}
			}
			catch (IOException e)
			{
				throw new TransportException(transport.GetURI(), MessageFormat.Format(JGitText.Get
					().cannotDeleteStaleTrackingRef, name), e);
			}
		}

		private static bool IsTag(Ref r)
		{
			return IsTag(r.GetName());
		}

		private static bool IsTag(string name)
		{
			return name.StartsWith(Constants.R_TAGS);
		}
	}
}
