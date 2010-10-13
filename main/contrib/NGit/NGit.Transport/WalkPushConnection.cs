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
using NGit.Storage.Pack;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Generic push support for dumb transport protocols.</summary>
	/// <remarks>
	/// Generic push support for dumb transport protocols.
	/// <p>
	/// Since there are no Git-specific smarts on the remote side of the connection
	/// the client side must handle everything on its own. The generic push support
	/// requires being able to delete, create and overwrite files on the remote side,
	/// as well as create any missing directories (if necessary). Typically this can
	/// be handled through an FTP style protocol.
	/// <p>
	/// Objects not on the remote side are uploaded as pack files, using one pack
	/// file per invocation. This simplifies the implementation as only two data
	/// files need to be written to the remote repository.
	/// <p>
	/// Push support supplied by this class is not multiuser safe. Concurrent pushes
	/// to the same repository may yield an inconsistent reference database which may
	/// confuse fetch clients.
	/// <p>
	/// A single push is concurrently safe with multiple fetch requests, due to the
	/// careful order of operations used to update the repository. Clients fetching
	/// may receive transient failures due to short reads on certain files if the
	/// protocol does not support atomic file replacement.
	/// </remarks>
	/// <seealso cref="WalkRemoteObjectDatabase">WalkRemoteObjectDatabase</seealso>
	internal class WalkPushConnection : BaseConnection, PushConnection
	{
		/// <summary>The repository this transport pushes out of.</summary>
		/// <remarks>The repository this transport pushes out of.</remarks>
		private readonly Repository local;

		/// <summary>Location of the remote repository we are writing to.</summary>
		/// <remarks>Location of the remote repository we are writing to.</remarks>
		private readonly URIish uri;

		/// <summary>Database connection to the remote repository.</summary>
		/// <remarks>Database connection to the remote repository.</remarks>
		private readonly WalkRemoteObjectDatabase dest;

		/// <summary>The configured transport we were constructed by.</summary>
		/// <remarks>The configured transport we were constructed by.</remarks>
		private readonly NGit.Transport.Transport transport;

		/// <summary>Packs already known to reside in the remote repository.</summary>
		/// <remarks>
		/// Packs already known to reside in the remote repository.
		/// <p>
		/// This is a LinkedHashMap to maintain the original order.
		/// </remarks>
		private LinkedHashMap<string, string> packNames;

		/// <summary>Complete listing of refs the remote will have after our push.</summary>
		/// <remarks>Complete listing of refs the remote will have after our push.</remarks>
		private IDictionary<string, Ref> newRefs;

		/// <summary>Updates which require altering the packed-refs file to complete.</summary>
		/// <remarks>
		/// Updates which require altering the packed-refs file to complete.
		/// <p>
		/// If this collection is non-empty then any refs listed in
		/// <see cref="newRefs">newRefs</see>
		/// with a storage class of
		/// <see cref="NGit.RefStorage.PACKED">NGit.RefStorage.PACKED</see>
		/// will be written.
		/// </remarks>
		private ICollection<RemoteRefUpdate> packedRefUpdates;

		internal WalkPushConnection(WalkTransport walkTransport, WalkRemoteObjectDatabase
			 w)
		{
			transport = (NGit.Transport.Transport)walkTransport;
			local = transport.local;
			uri = transport.GetURI();
			dest = w;
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public virtual void Push(ProgressMonitor monitor, IDictionary<string, RemoteRefUpdate
			> refUpdates)
		{
			MarkStartedOperation();
			packNames = null;
			newRefs = new SortedDictionary<string, Ref>(GetRefsMap());
			packedRefUpdates = new AList<RemoteRefUpdate>(refUpdates.Count);
			// Filter the commands and issue all deletes first. This way we
			// can correctly handle a directory being cleared out and a new
			// ref using the directory name being created.
			//
			IList<RemoteRefUpdate> updates = new AList<RemoteRefUpdate>();
			foreach (RemoteRefUpdate u in refUpdates.Values)
			{
				string n = u.GetRemoteName();
				if (!n.StartsWith("refs/") || !Repository.IsValidRefName(n))
				{
					u.SetStatus(RemoteRefUpdate.Status.REJECTED_OTHER_REASON);
					u.SetMessage(JGitText.Get().funnyRefname);
					continue;
				}
				if (AnyObjectId.Equals(ObjectId.ZeroId, u.GetNewObjectId()))
				{
					DeleteCommand(u);
				}
				else
				{
					updates.AddItem(u);
				}
			}
			// If we have any updates we need to upload the objects first, to
			// prevent creating refs pointing at non-existent data. Then we
			// can update the refs, and the info-refs file for dumb transports.
			//
			if (!updates.IsEmpty())
			{
				Sendpack(updates, monitor);
			}
			foreach (RemoteRefUpdate u_1 in updates)
			{
				UpdateCommand(u_1);
			}
			// Is this a new repository? If so we should create additional
			// metadata files so it is properly initialized during the push.
			//
			if (!updates.IsEmpty() && IsNewRepository())
			{
				CreateNewRepository(updates);
			}
			RefWriter refWriter = new _RefWriter_177(this, newRefs.Values);
			if (!packedRefUpdates.IsEmpty())
			{
				try
				{
					refWriter.WritePackedRefs();
					foreach (RemoteRefUpdate u_2 in packedRefUpdates)
					{
						u_2.SetStatus(RemoteRefUpdate.Status.OK);
					}
				}
				catch (IOException err)
				{
					foreach (RemoteRefUpdate u_2 in packedRefUpdates)
					{
						u_2.SetStatus(RemoteRefUpdate.Status.REJECTED_OTHER_REASON);
						u_2.SetMessage(err.Message);
					}
					throw new TransportException(uri, JGitText.Get().failedUpdatingRefs, err);
				}
			}
			try
			{
				refWriter.WriteInfoRefs();
			}
			catch (IOException err)
			{
				throw new TransportException(uri, JGitText.Get().failedUpdatingRefs, err);
			}
		}

		private sealed class _RefWriter_177 : RefWriter
		{
			public _RefWriter_177(WalkPushConnection _enclosing, ICollection<Ref> baseArg1) : 
				base(baseArg1)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void WriteFile(string file, byte[] content)
			{
				this._enclosing.dest.WriteFile(WalkRemoteObjectDatabase.ROOT_DIR + file, content);
			}

			private readonly WalkPushConnection _enclosing;
		}

		public override void Close()
		{
			dest.Close();
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void Sendpack(IList<RemoteRefUpdate> updates, ProgressMonitor monitor)
		{
			string pathPack = null;
			string pathIdx = null;
			PackWriter writer = new PackWriter(transport.GetPackConfig(), local.NewObjectReader
				());
			try
			{
				IList<ObjectId> need = new AList<ObjectId>();
				IList<ObjectId> have = new AList<ObjectId>();
				foreach (RemoteRefUpdate r in updates)
				{
					need.AddItem(r.GetNewObjectId());
				}
				foreach (Ref r_1 in GetRefs())
				{
					have.AddItem(r_1.GetObjectId());
					if (r_1.GetPeeledObjectId() != null)
					{
						have.AddItem(r_1.GetPeeledObjectId());
					}
				}
				writer.PreparePack(monitor, need, have);
				// We don't have to continue further if the pack will
				// be an empty pack, as the remote has all objects it
				// needs to complete this change.
				//
				if (writer.GetObjectsNumber() == 0)
				{
					return;
				}
				packNames = new LinkedHashMap<string, string>();
				foreach (string n in dest.GetPackNames())
				{
					packNames.Put(n, n);
				}
				string @base = "pack-" + writer.ComputeName().Name;
				string packName = @base + ".pack";
				pathPack = "pack/" + packName;
				pathIdx = "pack/" + @base + ".idx";
				if (Sharpen.Collections.Remove(packNames, packName) != null)
				{
					// The remote already contains this pack. We should
					// remove the index before overwriting to prevent bad
					// offsets from appearing to clients.
					//
					dest.WriteInfoPacks(packNames.Keys);
					dest.DeleteFile(pathIdx);
				}
				// Write the pack file, then the index, as readers look the
				// other direction (index, then pack file).
				//
				string wt = "Put " + Sharpen.Runtime.Substring(@base, 0, 12);
				OutputStream os = dest.WriteFile(pathPack, monitor, wt + "..pack");
				try
				{
					os = new BufferedOutputStream(os);
					writer.WritePack(monitor, monitor, os);
				}
				finally
				{
					os.Close();
				}
				os = dest.WriteFile(pathIdx, monitor, wt + "..idx");
				try
				{
					os = new BufferedOutputStream(os);
					writer.WriteIndex(os);
				}
				finally
				{
					os.Close();
				}
				// Record the pack at the start of the pack info list. This
				// way clients are likely to consult the newest pack first,
				// and discover the most recent objects there.
				//
				AList<string> infoPacks = new AList<string>();
				infoPacks.AddItem(packName);
				Sharpen.Collections.AddAll(infoPacks, packNames.Keys);
				dest.WriteInfoPacks(infoPacks);
			}
			catch (IOException err)
			{
				SafeDelete(pathIdx);
				SafeDelete(pathPack);
				throw new TransportException(uri, JGitText.Get().cannotStoreObjects, err);
			}
			finally
			{
				writer.Release();
			}
		}

		private void SafeDelete(string path)
		{
			if (path != null)
			{
				try
				{
					dest.DeleteFile(path);
				}
				catch (IOException)
				{
				}
			}
		}

		// Ignore the deletion failure. We probably are
		// already failing and were just trying to pick
		// up after ourselves.
		private void DeleteCommand(RemoteRefUpdate u)
		{
			Ref r = Sharpen.Collections.Remove(newRefs, u.GetRemoteName());
			if (r == null)
			{
				// Already gone.
				//
				u.SetStatus(RemoteRefUpdate.Status.OK);
				return;
			}
			if (r.GetStorage().IsPacked())
			{
				packedRefUpdates.AddItem(u);
			}
			if (r.GetStorage().IsLoose())
			{
				try
				{
					dest.DeleteRef(u.GetRemoteName());
					u.SetStatus(RemoteRefUpdate.Status.OK);
				}
				catch (IOException e)
				{
					u.SetStatus(RemoteRefUpdate.Status.REJECTED_OTHER_REASON);
					u.SetMessage(e.Message);
				}
			}
			try
			{
				dest.DeleteRefLog(u.GetRemoteName());
			}
			catch (IOException e)
			{
				u.SetStatus(RemoteRefUpdate.Status.REJECTED_OTHER_REASON);
				u.SetMessage(e.Message);
			}
		}

		private void UpdateCommand(RemoteRefUpdate u)
		{
			try
			{
				dest.WriteRef(u.GetRemoteName(), u.GetNewObjectId());
				newRefs.Put(u.GetRemoteName(), new ObjectIdRef.Unpeeled(RefStorage.LOOSE, u.GetRemoteName
					(), u.GetNewObjectId()));
				u.SetStatus(RemoteRefUpdate.Status.OK);
			}
			catch (IOException e)
			{
				u.SetStatus(RemoteRefUpdate.Status.REJECTED_OTHER_REASON);
				u.SetMessage(e.Message);
			}
		}

		private bool IsNewRepository()
		{
			return GetRefsMap().IsEmpty() && packNames != null && packNames.IsEmpty();
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void CreateNewRepository(IList<RemoteRefUpdate> updates)
		{
			try
			{
				string @ref = "ref: " + PickHEAD(updates) + "\n";
				byte[] bytes = Constants.Encode(@ref);
				dest.WriteFile(WalkRemoteObjectDatabase.ROOT_DIR + Constants.HEAD, bytes);
			}
			catch (IOException e)
			{
				throw new TransportException(uri, JGitText.Get().cannotCreateHEAD, e);
			}
			try
			{
				string config = "[core]\n" + "\trepositoryformatversion = 0\n";
				byte[] bytes = Constants.Encode(config);
				dest.WriteFile(WalkRemoteObjectDatabase.ROOT_DIR + "config", bytes);
			}
			catch (IOException e)
			{
				throw new TransportException(uri, JGitText.Get().cannotCreateConfig, e);
			}
		}

		private static string PickHEAD(IList<RemoteRefUpdate> updates)
		{
			// Try to use master if the user is pushing that, it is the
			// default branch and is likely what they want to remain as
			// the default on the new remote.
			//
			foreach (RemoteRefUpdate u in updates)
			{
				string n = u.GetRemoteName();
				if (n.Equals(Constants.R_HEADS + Constants.MASTER))
				{
					return n;
				}
			}
			// Pick any branch, under the assumption the user pushed only
			// one to the remote side.
			//
			foreach (RemoteRefUpdate u_1 in updates)
			{
				string n = u_1.GetRemoteName();
				if (n.StartsWith(Constants.R_HEADS))
				{
					return n;
				}
			}
			return updates[0].GetRemoteName();
		}
	}
}
