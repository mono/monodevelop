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
using NGit;
using NGit.Errors;
using NGit.Revwalk;
using NGit.Storage.File;
using NGit.Transport;
using NGit.Treewalk;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Generic fetch support for dumb transport protocols.</summary>
	/// <remarks>
	/// Generic fetch support for dumb transport protocols.
	/// <p>
	/// Since there are no Git-specific smarts on the remote side of the connection
	/// the client side must determine which objects it needs to copy in order to
	/// completely fetch the requested refs and their history. The generic walk
	/// support in this class parses each individual object (once it has been copied
	/// to the local repository) and examines the list of objects that must also be
	/// copied to create a complete history. Objects which are already available
	/// locally are retained (and not copied), saving bandwidth for incremental
	/// fetches. Pack files are copied from the remote repository only as a last
	/// resort, as the entire pack must be copied locally in order to access any
	/// single object.
	/// <p>
	/// This fetch connection does not actually perform the object data transfer.
	/// Instead it delegates the transfer to a
	/// <see cref="WalkRemoteObjectDatabase">WalkRemoteObjectDatabase</see>
	/// ,
	/// which knows how to read individual files from the remote repository and
	/// supply the data as a standard Java InputStream.
	/// </remarks>
	/// <seealso cref="WalkRemoteObjectDatabase">WalkRemoteObjectDatabase</seealso>
	internal class WalkFetchConnection : BaseFetchConnection
	{
		/// <summary>The repository this transport fetches into, or pushes out of.</summary>
		/// <remarks>The repository this transport fetches into, or pushes out of.</remarks>
		private readonly Repository local;

		/// <summary>If not null the validator for received objects.</summary>
		/// <remarks>If not null the validator for received objects.</remarks>
		private readonly ObjectChecker objCheck;

		/// <summary>List of all remote repositories we may need to get objects out of.</summary>
		/// <remarks>
		/// List of all remote repositories we may need to get objects out of.
		/// <p>
		/// The first repository in the list is the one we were asked to fetch from;
		/// the remaining repositories point to the alternate locations we can fetch
		/// objects through.
		/// </remarks>
		private readonly IList<WalkRemoteObjectDatabase> remotes;

		/// <summary>
		/// Most recently used item in
		/// <see cref="remotes">remotes</see>
		/// .
		/// </summary>
		private int lastRemoteIdx;

		private readonly RevWalk revWalk;

		private readonly TreeWalk treeWalk;

		/// <summary>Objects whose direct dependents we know we have (or will have).</summary>
		/// <remarks>Objects whose direct dependents we know we have (or will have).</remarks>
		private readonly RevFlag COMPLETE;

		/// <summary>
		/// Objects that have already entered
		/// <see cref="workQueue">workQueue</see>
		/// .
		/// </summary>
		private readonly RevFlag IN_WORK_QUEUE;

		/// <summary>
		/// Commits that have already entered
		/// <see cref="localCommitQueue">localCommitQueue</see>
		/// .
		/// </summary>
		private readonly RevFlag LOCALLY_SEEN;

		/// <summary>Commits already reachable from all local refs.</summary>
		/// <remarks>Commits already reachable from all local refs.</remarks>
		private readonly DateRevQueue localCommitQueue;

		/// <summary>Objects we need to copy from the remote repository.</summary>
		/// <remarks>Objects we need to copy from the remote repository.</remarks>
		private List<ObjectId> workQueue;

		/// <summary>Databases we have not yet obtained the list of packs from.</summary>
		/// <remarks>Databases we have not yet obtained the list of packs from.</remarks>
		private readonly List<WalkRemoteObjectDatabase> noPacksYet;

		/// <summary>Databases we have not yet obtained the alternates from.</summary>
		/// <remarks>Databases we have not yet obtained the alternates from.</remarks>
		private readonly List<WalkRemoteObjectDatabase> noAlternatesYet;

		/// <summary>Packs we have discovered, but have not yet fetched locally.</summary>
		/// <remarks>Packs we have discovered, but have not yet fetched locally.</remarks>
		private readonly List<WalkFetchConnection.RemotePack> unfetchedPacks;

		/// <summary>
		/// Packs whose indexes we have looked at in
		/// <see cref="unfetchedPacks">unfetchedPacks</see>
		/// .
		/// <p>
		/// We try to avoid getting duplicate copies of the same pack through
		/// multiple alternates by only looking at packs whose names are not yet in
		/// this collection.
		/// </summary>
		private readonly ICollection<string> packsConsidered;

		private readonly MutableObjectId idBuffer = new MutableObjectId();

		/// <summary>Errors received while trying to obtain an object.</summary>
		/// <remarks>
		/// Errors received while trying to obtain an object.
		/// <p>
		/// If the fetch winds up failing because we cannot locate a specific object
		/// then we need to report all errors related to that object back to the
		/// caller as there may be cascading failures.
		/// </remarks>
		private readonly Dictionary<ObjectId, IList<Exception>> fetchErrors;

		private string lockMessage;

		private readonly IList<PackLock> packLocks;

		/// <summary>
		/// Inserter to write objects onto
		/// <see cref="local">local</see>
		/// .
		/// </summary>
		private readonly ObjectInserter inserter;

		/// <summary>
		/// Inserter to read objects from
		/// <see cref="local">local</see>
		/// .
		/// </summary>
		private readonly ObjectReader reader;

		internal WalkFetchConnection(WalkTransport t, WalkRemoteObjectDatabase w)
		{
			NGit.Transport.Transport wt = (NGit.Transport.Transport)t;
			local = wt.local;
			objCheck = wt.IsCheckFetchedObjects() ? new ObjectChecker() : null;
			inserter = local.NewObjectInserter();
			reader = local.NewObjectReader();
			remotes = new AList<WalkRemoteObjectDatabase>();
			remotes.AddItem(w);
			unfetchedPacks = new List<WalkFetchConnection.RemotePack>();
			packsConsidered = new HashSet<string>();
			noPacksYet = new List<WalkRemoteObjectDatabase>();
			noPacksYet.AddItem(w);
			noAlternatesYet = new List<WalkRemoteObjectDatabase>();
			noAlternatesYet.AddItem(w);
			fetchErrors = new Dictionary<ObjectId, IList<Exception>>();
			packLocks = new AList<PackLock>(4);
			revWalk = new RevWalk(reader);
			revWalk.SetRetainBody(false);
			treeWalk = new TreeWalk(reader);
			COMPLETE = revWalk.NewFlag("COMPLETE");
			IN_WORK_QUEUE = revWalk.NewFlag("IN_WORK_QUEUE");
			LOCALLY_SEEN = revWalk.NewFlag("LOCALLY_SEEN");
			localCommitQueue = new DateRevQueue();
			workQueue = new List<ObjectId>();
		}

		public override bool DidFetchTestConnectivity()
		{
			return true;
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		protected internal override void DoFetch(ProgressMonitor monitor, ICollection<Ref
			> want, ICollection<ObjectId> have)
		{
			MarkLocalRefsComplete(have);
			QueueWants(want);
			while (!monitor.IsCancelled() && !workQueue.IsEmpty())
			{
				ObjectId id = workQueue.RemoveFirst();
				if (!(id is RevObject) || !((RevObject)id).Has(COMPLETE))
				{
					DownloadObject(monitor, id);
				}
				Process(id);
			}
		}

		public override ICollection<PackLock> GetPackLocks()
		{
			return packLocks;
		}

		public override void SetPackLockMessage(string message)
		{
			lockMessage = message;
		}

		public override void Close()
		{
			inserter.Release();
			reader.Release();
			foreach (WalkFetchConnection.RemotePack p in unfetchedPacks)
			{
				if (p.tmpIdx != null)
				{
					p.tmpIdx.Delete();
				}
			}
			foreach (WalkRemoteObjectDatabase r in remotes)
			{
				r.Close();
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void QueueWants(ICollection<Ref> want)
		{
			HashSet<ObjectId> inWorkQueue = new HashSet<ObjectId>();
			foreach (Ref r in want)
			{
				ObjectId id = r.GetObjectId();
				try
				{
					RevObject obj = revWalk.ParseAny(id);
					if (obj.Has(COMPLETE))
					{
						continue;
					}
					if (inWorkQueue.AddItem(id))
					{
						obj.Add(IN_WORK_QUEUE);
						workQueue.AddItem(obj);
					}
				}
				catch (MissingObjectException)
				{
					if (inWorkQueue.AddItem(id))
					{
						workQueue.AddItem(id);
					}
				}
				catch (IOException e)
				{
					throw new TransportException(MessageFormat.Format(JGitText.Get().cannotRead, id.Name
						), e);
				}
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void Process(ObjectId id)
		{
			RevObject obj;
			try
			{
				if (id is RevObject)
				{
					obj = (RevObject)id;
					if (obj.Has(COMPLETE))
					{
						return;
					}
					revWalk.ParseHeaders(obj);
				}
				else
				{
					obj = revWalk.ParseAny(id);
					if (obj.Has(COMPLETE))
					{
						return;
					}
				}
			}
			catch (IOException e)
			{
				throw new TransportException(MessageFormat.Format(JGitText.Get().cannotRead, id.Name
					), e);
			}
			switch (obj.Type)
			{
				case Constants.OBJ_BLOB:
				{
					ProcessBlob(obj);
					break;
				}

				case Constants.OBJ_TREE:
				{
					ProcessTree(obj);
					break;
				}

				case Constants.OBJ_COMMIT:
				{
					ProcessCommit(obj);
					break;
				}

				case Constants.OBJ_TAG:
				{
					ProcessTag(obj);
					break;
				}

				default:
				{
					throw new TransportException(MessageFormat.Format(JGitText.Get().unknownObjectType
						, id.Name));
				}
			}
			// If we had any prior errors fetching this object they are
			// now resolved, as the object was parsed successfully.
			//
			Sharpen.Collections.Remove(fetchErrors, id);
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void ProcessBlob(RevObject obj)
		{
			try
			{
				if (reader.Has(obj, Constants.OBJ_BLOB))
				{
					obj.Add(COMPLETE);
				}
				else
				{
					throw new TransportException(MessageFormat.Format(JGitText.Get().cannotReadBlob, 
						obj.Name), new MissingObjectException(obj, Constants.TYPE_BLOB));
				}
			}
			catch (IOException error)
			{
				throw new TransportException(MessageFormat.Format(JGitText.Get().cannotReadBlob, 
					obj.Name), error);
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void ProcessTree(RevObject obj)
		{
			try
			{
				treeWalk.Reset(obj);
				while (treeWalk.Next())
				{
					FileMode mode = treeWalk.GetFileMode(0);
					int sType = mode.GetObjectType();
					switch (sType)
					{
						case Constants.OBJ_BLOB:
						case Constants.OBJ_TREE:
						{
							treeWalk.GetObjectId(idBuffer, 0);
							Needs(revWalk.LookupAny(idBuffer, sType));
							continue;
							goto default;
						}

						default:
						{
							if (FileMode.GITLINK.Equals(mode))
							{
								continue;
							}
							treeWalk.GetObjectId(idBuffer, 0);
							throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().invalidModeFor
								, mode, idBuffer.Name, treeWalk.PathString, obj.Id.Name));
						}
					}
				}
			}
			catch (IOException ioe)
			{
				throw new TransportException(MessageFormat.Format(JGitText.Get().cannotReadTree, 
					obj.Name), ioe);
			}
			obj.Add(COMPLETE);
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void ProcessCommit(RevObject obj)
		{
			RevCommit commit = (RevCommit)obj;
			MarkLocalCommitsComplete(commit.CommitTime);
			Needs(commit.Tree);
			foreach (RevCommit p in commit.Parents)
			{
				Needs(p);
			}
			obj.Add(COMPLETE);
		}

		private void ProcessTag(RevObject obj)
		{
			RevTag tag = (RevTag)obj;
			Needs(tag.GetObject());
			obj.Add(COMPLETE);
		}

		private void Needs(RevObject obj)
		{
			if (obj.Has(COMPLETE))
			{
				return;
			}
			if (!obj.Has(IN_WORK_QUEUE))
			{
				obj.Add(IN_WORK_QUEUE);
				workQueue.AddItem(obj);
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void DownloadObject(ProgressMonitor pm, AnyObjectId id)
		{
			if (AlreadyHave(id))
			{
				return;
			}
			for (; ; )
			{
				// Try a pack file we know about, but don't have yet. Odds are
				// that if it has this object, it has others related to it so
				// getting the pack is a good bet.
				//
				if (DownloadPackedObject(pm, id))
				{
					return;
				}
				// Search for a loose object over all alternates, starting
				// from the one we last successfully located an object through.
				//
				string idStr = id.Name;
				string subdir = Sharpen.Runtime.Substring(idStr, 0, 2);
				string file = Sharpen.Runtime.Substring(idStr, 2);
				string looseName = subdir + "/" + file;
				for (int i = lastRemoteIdx; i < remotes.Count; i++)
				{
					if (DownloadLooseObject(id, looseName, remotes[i]))
					{
						lastRemoteIdx = i;
						return;
					}
				}
				for (int i_1 = 0; i_1 < lastRemoteIdx; i_1++)
				{
					if (DownloadLooseObject(id, looseName, remotes[i_1]))
					{
						lastRemoteIdx = i_1;
						return;
					}
				}
				// Try to obtain more pack information and search those.
				//
				while (!noPacksYet.IsEmpty())
				{
					WalkRemoteObjectDatabase wrr = noPacksYet.RemoveFirst();
					ICollection<string> packNameList;
					try
					{
						pm.BeginTask("Listing packs", ProgressMonitor.UNKNOWN);
						packNameList = wrr.GetPackNames();
					}
					catch (IOException e)
					{
						// Try another repository.
						//
						RecordError(id, e);
						continue;
					}
					finally
					{
						pm.EndTask();
					}
					if (packNameList == null || packNameList.IsEmpty())
					{
						continue;
					}
					foreach (string packName in packNameList)
					{
						if (packsConsidered.AddItem(packName))
						{
							unfetchedPacks.AddItem(new WalkFetchConnection.RemotePack(this, wrr, packName));
						}
					}
					if (DownloadPackedObject(pm, id))
					{
						return;
					}
				}
				// Try to expand the first alternate we haven't expanded yet.
				//
				ICollection<WalkRemoteObjectDatabase> al = ExpandOneAlternate(id, pm);
				if (al != null && !al.IsEmpty())
				{
					foreach (WalkRemoteObjectDatabase alt in al)
					{
						remotes.AddItem(alt);
						noPacksYet.AddItem(alt);
						noAlternatesYet.AddItem(alt);
					}
					continue;
				}
				// We could not obtain the object. There may be reasons why.
				//
				IList<Exception> failures = fetchErrors.Get((ObjectId)id);
				TransportException te;
				te = new TransportException(MessageFormat.Format(JGitText.Get().cannotGet, id.Name
					));
				if (failures != null && !failures.IsEmpty())
				{
					if (failures.Count == 1)
					{
						Sharpen.Extensions.InitCause(te, failures[0]);
					}
					else
					{
						Sharpen.Extensions.InitCause(te, new CompoundException(failures));
					}
				}
				throw te;
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private bool AlreadyHave(AnyObjectId id)
		{
			try
			{
				return reader.Has(id);
			}
			catch (IOException error)
			{
				throw new TransportException(MessageFormat.Format(JGitText.Get().cannotReadObject
					, id.Name), error);
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private bool DownloadPackedObject(ProgressMonitor monitor, AnyObjectId id)
		{
			// Search for the object in a remote pack whose index we have,
			// but whose pack we do not yet have.
			//
			Iterator<WalkFetchConnection.RemotePack> packItr = unfetchedPacks.Iterator();
			while (packItr.HasNext() && !monitor.IsCancelled())
			{
				WalkFetchConnection.RemotePack pack = packItr.Next();
				try
				{
					pack.OpenIndex(monitor);
				}
				catch (IOException err)
				{
					// If the index won't open its either not found or
					// its a format we don't recognize. In either case
					// we may still be able to obtain the object from
					// another source, so don't consider it a failure.
					//
					RecordError(id, err);
					packItr.Remove();
					continue;
				}
				if (monitor.IsCancelled())
				{
					// If we were cancelled while the index was opening
					// the open may have aborted. We can't search an
					// unopen index.
					//
					return false;
				}
				if (!pack.index.HasObject(id))
				{
					// Not in this pack? Try another.
					//
					continue;
				}
				// It should be in the associated pack. Download that
				// and attach it to the local repository so we can use
				// all of the contained objects.
				//
				try
				{
					pack.DownloadPack(monitor);
				}
				catch (IOException err)
				{
					// If the pack failed to download, index correctly,
					// or open in the local repository we may still be
					// able to obtain this object from another pack or
					// an alternate.
					//
					RecordError(id, err);
					continue;
				}
				finally
				{
					// If the pack was good its in the local repository
					// and Repository.hasObject(id) will succeed in the
					// future, so we do not need this data anymore. If
					// it failed the index and pack are unusable and we
					// shouldn't consult them again.
					//
					if (pack.tmpIdx != null)
					{
						pack.tmpIdx.Delete();
					}
					packItr.Remove();
				}
				if (!AlreadyHave(id))
				{
					// What the hell? This pack claimed to have
					// the object, but after indexing we didn't
					// actually find it in the pack.
					//
					RecordError(id, new FileNotFoundException(MessageFormat.Format(JGitText.Get().objectNotFoundIn
						, id.Name, pack.packName)));
					continue;
				}
				// Complete any other objects that we can.
				//
				Iterator<ObjectId> pending = SwapFetchQueue();
				while (pending.HasNext())
				{
					ObjectId p = pending.Next();
					if (pack.index.HasObject(p))
					{
						pending.Remove();
						Process(p);
					}
					else
					{
						workQueue.AddItem(p);
					}
				}
				return true;
			}
			return false;
		}

		private Iterator<ObjectId> SwapFetchQueue()
		{
			Iterator<ObjectId> r = workQueue.Iterator();
			workQueue = new List<ObjectId>();
			return r;
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private bool DownloadLooseObject(AnyObjectId id, string looseName, WalkRemoteObjectDatabase
			 remote)
		{
			try
			{
				byte[] compressed = remote.Open(looseName).ToArray();
				VerifyAndInsertLooseObject(id, compressed);
				return true;
			}
			catch (FileNotFoundException e)
			{
				// Not available in a loose format from this alternate?
				// Try another strategy to get the object.
				//
				RecordError(id, e);
				return false;
			}
			catch (IOException e)
			{
				throw new TransportException(MessageFormat.Format(JGitText.Get().cannotDownload, 
					id.Name), e);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void VerifyAndInsertLooseObject(AnyObjectId id, byte[] compressed)
		{
			ObjectLoader uol;
			try
			{
				uol = UnpackedObject.Parse(compressed, id);
			}
			catch (CorruptObjectException parsingError)
			{
				// Some HTTP servers send back a "200 OK" status with an HTML
				// page that explains the requested file could not be found.
				// These servers are most certainly misconfigured, but many
				// of them exist in the world, and many of those are hosting
				// Git repositories.
				//
				// Since an HTML page is unlikely to hash to one of our loose
				// objects we treat this condition as a FileNotFoundException
				// and attempt to recover by getting the object from another
				// source.
				//
				FileNotFoundException e;
				e = new FileNotFoundException(id.Name);
				Sharpen.Extensions.InitCause(e, parsingError);
				throw e;
			}
			int type = uol.GetType();
			byte[] raw = uol.GetCachedBytes();
			if (objCheck != null)
			{
				try
				{
					objCheck.Check(type, raw);
				}
				catch (CorruptObjectException e)
				{
					throw new TransportException(MessageFormat.Format(JGitText.Get().transportExceptionInvalid
						, Constants.TypeString(type), id.Name, e.Message));
				}
			}
			ObjectId act = inserter.Insert(type, raw);
			if (!AnyObjectId.Equals(id, act))
			{
				throw new TransportException(MessageFormat.Format(JGitText.Get().incorrectHashFor
					, id.Name, act.Name, Constants.TypeString(type), compressed.Length));
			}
			inserter.Flush();
		}

		private ICollection<WalkRemoteObjectDatabase> ExpandOneAlternate(AnyObjectId id, 
			ProgressMonitor pm)
		{
			while (!noAlternatesYet.IsEmpty())
			{
				WalkRemoteObjectDatabase wrr = noAlternatesYet.RemoveFirst();
				try
				{
					pm.BeginTask(JGitText.Get().listingAlternates, ProgressMonitor.UNKNOWN);
					ICollection<WalkRemoteObjectDatabase> altList = wrr.GetAlternates();
					if (altList != null && !altList.IsEmpty())
					{
						return altList;
					}
				}
				catch (IOException e)
				{
					// Try another repository.
					//
					RecordError(id, e);
				}
				finally
				{
					pm.EndTask();
				}
			}
			return null;
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void MarkLocalRefsComplete(ICollection<ObjectId> have)
		{
			foreach (Ref r in local.GetAllRefs().Values)
			{
				try
				{
					MarkLocalObjComplete(revWalk.ParseAny(r.GetObjectId()));
				}
				catch (IOException readError)
				{
					throw new TransportException(MessageFormat.Format(JGitText.Get().localRefIsMissingObjects
						, r.GetName()), readError);
				}
			}
			foreach (ObjectId id in have)
			{
				try
				{
					MarkLocalObjComplete(revWalk.ParseAny(id));
				}
				catch (IOException readError)
				{
					throw new TransportException(MessageFormat.Format(JGitText.Get().transportExceptionMissingAssumed
						, id.Name), readError);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void MarkLocalObjComplete(RevObject obj)
		{
			while (obj.Type == Constants.OBJ_TAG)
			{
				obj.Add(COMPLETE);
				obj = ((RevTag)obj).GetObject();
				revWalk.ParseHeaders(obj);
			}
			switch (obj.Type)
			{
				case Constants.OBJ_BLOB:
				{
					obj.Add(COMPLETE);
					break;
				}

				case Constants.OBJ_COMMIT:
				{
					PushLocalCommit((RevCommit)obj);
					break;
				}

				case Constants.OBJ_TREE:
				{
					MarkTreeComplete((RevTree)obj);
					break;
				}
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void MarkLocalCommitsComplete(int until)
		{
			try
			{
				for (; ; )
				{
					RevCommit c = localCommitQueue.Peek();
					if (c == null || c.CommitTime < until)
					{
						return;
					}
					localCommitQueue.Next();
					MarkTreeComplete(c.Tree);
					foreach (RevCommit p in c.Parents)
					{
						PushLocalCommit(p);
					}
				}
			}
			catch (IOException err)
			{
				throw new TransportException(JGitText.Get().localObjectsIncomplete, err);
			}
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void PushLocalCommit(RevCommit p)
		{
			if (p.Has(LOCALLY_SEEN))
			{
				return;
			}
			revWalk.ParseHeaders(p);
			p.Add(LOCALLY_SEEN);
			p.Add(COMPLETE);
			p.Carry(COMPLETE);
			localCommitQueue.Add(p);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void MarkTreeComplete(RevTree tree)
		{
			if (tree.Has(COMPLETE))
			{
				return;
			}
			tree.Add(COMPLETE);
			treeWalk.Reset(tree);
			while (treeWalk.Next())
			{
				FileMode mode = treeWalk.GetFileMode(0);
				int sType = mode.GetObjectType();
				switch (sType)
				{
					case Constants.OBJ_BLOB:
					{
						treeWalk.GetObjectId(idBuffer, 0);
						revWalk.LookupAny(idBuffer, sType).Add(COMPLETE);
						continue;
						goto case Constants.OBJ_TREE;
					}

					case Constants.OBJ_TREE:
					{
						treeWalk.GetObjectId(idBuffer, 0);
						RevObject o = revWalk.LookupAny(idBuffer, sType);
						if (!o.Has(COMPLETE))
						{
							o.Add(COMPLETE);
							treeWalk.EnterSubtree();
						}
						continue;
						goto default;
					}

					default:
					{
						if (FileMode.GITLINK.Equals(mode))
						{
							continue;
						}
						treeWalk.GetObjectId(idBuffer, 0);
						throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().corruptObjectInvalidMode3
							, mode, idBuffer.Name, treeWalk.PathString, tree.Name));
					}
				}
			}
		}

		private void RecordError(AnyObjectId id, Exception what)
		{
			ObjectId objId = id.Copy();
			IList<Exception> errors = fetchErrors.Get(objId);
			if (errors == null)
			{
				errors = new AList<Exception>(2);
				fetchErrors.Put(objId, errors);
			}
			errors.AddItem(what);
		}

		private class RemotePack
		{
			internal readonly WalkRemoteObjectDatabase connection;

			internal readonly string packName;

			internal readonly string idxName;

			internal FilePath tmpIdx;

			internal PackIndex index;

			internal RemotePack(WalkFetchConnection _enclosing, WalkRemoteObjectDatabase c, string
				 pn)
			{
				this._enclosing = _enclosing;
				this.connection = c;
				this.packName = pn;
				this.idxName = Sharpen.Runtime.Substring(this.packName, 0, this.packName.Length -
					 5) + ".idx";
				string tn = this.idxName;
				if (tn.StartsWith("pack-"))
				{
					tn = Sharpen.Runtime.Substring(tn, 5);
				}
				if (tn.EndsWith(".idx"))
				{
					tn = Sharpen.Runtime.Substring(tn, 0, tn.Length - 4);
				}
				if (this._enclosing.local.ObjectDatabase is ObjectDirectory)
				{
					this.tmpIdx = new FilePath(((ObjectDirectory)this._enclosing.local.ObjectDatabase
						).GetDirectory(), "walk-" + tn + ".walkidx");
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal virtual void OpenIndex(ProgressMonitor pm)
			{
				if (this.index != null)
				{
					return;
				}
				if (this.tmpIdx == null)
				{
					this.tmpIdx = FilePath.CreateTempFile("jgit-walk-", ".idx");
				}
				else
				{
					if (this.tmpIdx.IsFile())
					{
						try
						{
							this.index = PackIndex.Open(this.tmpIdx);
							return;
						}
						catch (FileNotFoundException)
						{
						}
					}
				}
				// Fall through and get the file.
				WalkRemoteObjectDatabase.FileStream s;
				s = this.connection.Open("pack/" + this.idxName);
				pm.BeginTask("Get " + Sharpen.Runtime.Substring(this.idxName, 0, 12) + "..idx", s
					.length < 0 ? ProgressMonitor.UNKNOWN : (int)(s.length / 1024));
				try
				{
					FileOutputStream fos = new FileOutputStream(this.tmpIdx);
					try
					{
						byte[] buf = new byte[2048];
						int cnt;
						while (!pm.IsCancelled() && (cnt = s.@in.Read(buf)) >= 0)
						{
							fos.Write(buf, 0, cnt);
							pm.Update(cnt / 1024);
						}
					}
					finally
					{
						fos.Close();
					}
				}
				catch (IOException err)
				{
					this.tmpIdx.Delete();
					throw;
				}
				finally
				{
					s.@in.Close();
				}
				pm.EndTask();
				if (pm.IsCancelled())
				{
					this.tmpIdx.Delete();
					return;
				}
				try
				{
					this.index = PackIndex.Open(this.tmpIdx);
				}
				catch (IOException e)
				{
					this.tmpIdx.Delete();
					throw;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal virtual void DownloadPack(ProgressMonitor monitor)
			{
				WalkRemoteObjectDatabase.FileStream s;
				IndexPack ip;
				s = this.connection.Open("pack/" + this.packName);
				ip = IndexPack.Create(this._enclosing.local, s.@in);
				ip.SetFixThin(false);
				ip.SetObjectChecker(this._enclosing.objCheck);
				ip.Index(monitor);
				PackLock keep = ip.RenameAndOpenPack(this._enclosing.lockMessage);
				if (keep != null)
				{
					this._enclosing.packLocks.AddItem(keep);
				}
			}

			private readonly WalkFetchConnection _enclosing;
		}
	}
}
