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
using System.Text;
using NGit;
using NGit.Errors;
using NGit.Revwalk;
using NGit.Revwalk.Filter;
using NGit.Storage.File;
using NGit.Transport;
using NGit.Util;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Fetch implementation using the native Git pack transfer service.</summary>
	/// <remarks>
	/// Fetch implementation using the native Git pack transfer service.
	/// <p>
	/// This is the canonical implementation for transferring objects from the remote
	/// repository to the local repository by talking to the 'git-upload-pack'
	/// service. Objects are packed on the remote side into a pack file and then sent
	/// down the pipe to us.
	/// <p>
	/// This connection requires only a bi-directional pipe or socket, and thus is
	/// easily wrapped up into a local process pipe, anonymous TCP socket, or a
	/// command executed through an SSH tunnel.
	/// <p>
	/// If
	/// <see cref="BasePackConnection.statelessRPC">BasePackConnection.statelessRPC</see>
	/// is
	/// <code>true</code>
	/// , this connection
	/// can be tunneled over a request-response style RPC system like HTTP.  The RPC
	/// call boundary is determined by this class switching from writing to the
	/// OutputStream to reading from the InputStream.
	/// <p>
	/// Concrete implementations should just call
	/// <see cref="BasePackConnection.Init(Sharpen.InputStream, Sharpen.OutputStream)">BasePackConnection.Init(Sharpen.InputStream, Sharpen.OutputStream)
	/// 	</see>
	/// and
	/// <see cref="BasePackConnection.ReadAdvertisedRefs()">BasePackConnection.ReadAdvertisedRefs()
	/// 	</see>
	/// methods in constructor or before any use. They
	/// should also handle resources releasing in
	/// <see cref="Close()">Close()</see>
	/// method if needed.
	/// </remarks>
	public abstract class BasePackFetchConnection : BasePackConnection, FetchConnection
	{
		/// <summary>Maximum number of 'have' lines to send before giving up.</summary>
		/// <remarks>
		/// Maximum number of 'have' lines to send before giving up.
		/// <p>
		/// During
		/// <see cref="Negotiate(NGit.ProgressMonitor)">Negotiate(NGit.ProgressMonitor)</see>
		/// we send at most this many
		/// commits to the remote peer as 'have' lines without an ACK response before
		/// we give up.
		/// </remarks>
		private const int MAX_HAVES = 256;

		/// <summary>Amount of data the client sends before starting to read.</summary>
		/// <remarks>
		/// Amount of data the client sends before starting to read.
		/// <p>
		/// Any output stream given to the client must be able to buffer this many
		/// bytes before the client will stop writing and start reading from the
		/// input stream. If the output stream blocks before this many bytes are in
		/// the send queue, the system will deadlock.
		/// </remarks>
		protected internal const int MIN_CLIENT_BUFFER = 2 * 32 * 46 + 8;

		internal static readonly string OPTION_INCLUDE_TAG = "include-tag";

		internal static readonly string OPTION_MULTI_ACK = "multi_ack";

		internal static readonly string OPTION_MULTI_ACK_DETAILED = "multi_ack_detailed";

		internal static readonly string OPTION_THIN_PACK = "thin-pack";

		internal static readonly string OPTION_SIDE_BAND = "side-band";

		internal static readonly string OPTION_SIDE_BAND_64K = "side-band-64k";

		internal static readonly string OPTION_OFS_DELTA = "ofs-delta";

		internal static readonly string OPTION_SHALLOW = "shallow";

		internal static readonly string OPTION_NO_PROGRESS = "no-progress";

		internal class MultiAck
		{
			public const int OFF = 0;

			public const int CONTINUE = 1;

			public const int DETAILED = 2;
		}

		private readonly RevWalk walk;

		/// <summary>All commits that are immediately reachable by a local ref.</summary>
		/// <remarks>All commits that are immediately reachable by a local ref.</remarks>
		private RevCommitList<RevCommit> reachableCommits;

		/// <summary>Marks an object as having all its dependencies.</summary>
		/// <remarks>Marks an object as having all its dependencies.</remarks>
		internal readonly RevFlag REACHABLE;

		/// <summary>Marks a commit known to both sides of the connection.</summary>
		/// <remarks>Marks a commit known to both sides of the connection.</remarks>
		internal readonly RevFlag COMMON;

		/// <summary>
		/// Like
		/// <see cref="COMMON">COMMON</see>
		/// but means its also in
		/// <see cref="pckState">pckState</see>
		/// .
		/// </summary>
		private readonly RevFlag STATE;

		/// <summary>Marks a commit listed in the advertised refs.</summary>
		/// <remarks>Marks a commit listed in the advertised refs.</remarks>
		internal readonly RevFlag ADVERTISED;

		private int multiAck = BasePackFetchConnection.MultiAck.OFF;

		private bool thinPack;

		private bool sideband;

		private bool includeTags;

		private bool allowOfsDelta;

		private string lockMessage;

		private PackLock packLock;

		/// <summary>
		/// RPC state, if
		/// <see cref="BasePackConnection.statelessRPC">BasePackConnection.statelessRPC</see>
		/// is true.
		/// </summary>
		private TemporaryBuffer.Heap state;

		private PacketLineOut pckState;

		/// <summary>Create a new connection to fetch using the native git transport.</summary>
		/// <remarks>Create a new connection to fetch using the native git transport.</remarks>
		/// <param name="packTransport">the transport.</param>
		internal BasePackFetchConnection(PackTransport packTransport) : base(packTransport
			)
		{
			BasePackFetchConnection.FetchConfig cfg = local.GetConfig().Get(BasePackFetchConnection.FetchConfig
				.KEY);
			includeTags = transport.GetTagOpt() != TagOpt.NO_TAGS;
			thinPack = transport.IsFetchThin();
			allowOfsDelta = cfg.allowOfsDelta;
			walk = new RevWalk(local);
			reachableCommits = new RevCommitList<RevCommit>();
			REACHABLE = walk.NewFlag("REACHABLE");
			COMMON = walk.NewFlag("COMMON");
			STATE = walk.NewFlag("STATE");
			ADVERTISED = walk.NewFlag("ADVERTISED");
			walk.Carry(COMMON);
			walk.Carry(REACHABLE);
			walk.Carry(ADVERTISED);
		}

		private class FetchConfig
		{
			private sealed class _SectionParser_211 : Config.SectionParser<BasePackFetchConnection.FetchConfig
				>
			{
				public _SectionParser_211()
				{
				}

				public BasePackFetchConnection.FetchConfig Parse(Config cfg)
				{
					return new BasePackFetchConnection.FetchConfig(cfg);
				}
			}

			internal static readonly Config.SectionParser<BasePackFetchConnection.FetchConfig
				> KEY = new _SectionParser_211();

			internal readonly bool allowOfsDelta;

			internal FetchConfig(Config c)
			{
				allowOfsDelta = c.GetBoolean("repack", "usedeltabaseoffset", true);
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public void Fetch(ProgressMonitor monitor, ICollection<Ref> want, ICollection<ObjectId
			> have)
		{
			MarkStartedOperation();
			DoFetch(monitor, want, have);
		}

		public virtual bool DidFetchIncludeTags()
		{
			return false;
		}

		public virtual bool DidFetchTestConnectivity()
		{
			return false;
		}

		public virtual void SetPackLockMessage(string message)
		{
			lockMessage = message;
		}

		public virtual ICollection<PackLock> GetPackLocks()
		{
			if (packLock != null)
			{
				return Sharpen.Collections.Singleton(packLock);
			}
			return Sharpen.Collections.EmptyList<PackLock>();
		}

		/// <summary>Execute common ancestor negotiation and fetch the objects.</summary>
		/// <remarks>Execute common ancestor negotiation and fetch the objects.</remarks>
		/// <param name="monitor">progress monitor to receive status updates.</param>
		/// <param name="want">the advertised remote references the caller wants to fetch.</param>
		/// <param name="have">
		/// additional objects to assume that already exist locally. This
		/// will be added to the set of objects reachable from the
		/// destination repository's references.
		/// </param>
		/// <exception cref="NGit.Errors.TransportException">if any exception occurs.</exception>
		protected internal virtual void DoFetch(ProgressMonitor monitor, ICollection<Ref>
			 want, ICollection<ObjectId> have)
		{
			try
			{
				MarkRefsAdvertised();
				MarkReachable(have, MaxTimeWanted(want));
				if (statelessRPC)
				{
					state = new TemporaryBuffer.Heap(int.MaxValue);
					pckState = new PacketLineOut(state);
				}
				if (SendWants(want))
				{
					Negotiate(monitor);
					walk.Dispose();
					reachableCommits = null;
					state = null;
					pckState = null;
					ReceivePack(monitor);
				}
			}
			catch (BasePackFetchConnection.CancelledException)
			{
				Close();
				return;
			}
			catch (IOException err)
			{
				// Caller should test (or just know) this themselves.
				Close();
				throw new TransportException(err.Message, err);
			}
			catch (RuntimeException err)
			{
				Close();
				throw new TransportException(err.Message, err);
			}
		}

		public override void Close()
		{
			walk.Release();
			base.Close();
		}

		private int MaxTimeWanted(ICollection<Ref> wants)
		{
			int maxTime = 0;
			foreach (Ref r in wants)
			{
				try
				{
					RevObject obj = walk.ParseAny(r.GetObjectId());
					if (obj is RevCommit)
					{
						int cTime = ((RevCommit)obj).CommitTime;
						if (maxTime < cTime)
						{
							maxTime = cTime;
						}
					}
				}
				catch (IOException)
				{
				}
			}
			// We don't have it, but we want to fetch (thus fixing error).
			return maxTime;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void MarkReachable(ICollection<ObjectId> have, int maxTime)
		{
			foreach (Ref r in local.GetAllRefs().Values)
			{
				try
				{
					RevCommit o = walk.ParseCommit(r.GetObjectId());
					o.Add(REACHABLE);
					reachableCommits.AddItem(o);
				}
				catch (IOException)
				{
				}
			}
			// If we cannot read the value of the ref skip it.
			foreach (ObjectId id in have)
			{
				try
				{
					RevCommit o = walk.ParseCommit(id);
					o.Add(REACHABLE);
					reachableCommits.AddItem(o);
				}
				catch (IOException)
				{
				}
			}
			// If we cannot read the value of the ref skip it.
			if (maxTime > 0)
			{
				// Mark reachable commits until we reach maxTime. These may
				// wind up later matching up against things we want and we
				// can avoid asking for something we already happen to have.
				//
				DateTime maxWhen = Sharpen.Extensions.CreateDate(maxTime * 1000L);
				walk.Sort(RevSort.COMMIT_TIME_DESC);
				walk.MarkStart(reachableCommits);
				walk.SetRevFilter(NGit.Revwalk.Filter.CommitTimeRevFilter.AfterFilter(maxWhen));
				for (; ; )
				{
					RevCommit c = walk.Next();
					if (c == null)
					{
						break;
					}
					if (c.Has(ADVERTISED) && !c.Has(COMMON))
					{
						// This is actually going to be a common commit, but
						// our peer doesn't know that fact yet.
						//
						c.Add(COMMON);
						c.Carry(COMMON);
						reachableCommits.AddItem(c);
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool SendWants(ICollection<Ref> want)
		{
			PacketLineOut p = statelessRPC ? pckState : pckOut;
			bool first = true;
			foreach (Ref r in want)
			{
				try
				{
					if (walk.ParseAny(r.GetObjectId()).Has(REACHABLE))
					{
						// We already have this object. Asking for it is
						// not a very good idea.
						//
						continue;
					}
				}
				catch (IOException)
				{
				}
				// Its OK, we don't have it, but we want to fix that
				// by fetching the object from the other side.
				StringBuilder line = new StringBuilder(46);
				line.Append("want ");
				line.Append(r.GetObjectId().Name);
				if (first)
				{
					line.Append(EnableCapabilities());
					first = false;
				}
				line.Append('\n');
				p.WriteString(line.ToString());
			}
			if (first)
			{
				return false;
			}
			p.End();
			outNeedsEnd = false;
			return true;
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private string EnableCapabilities()
		{
			StringBuilder line = new StringBuilder();
			if (includeTags)
			{
				includeTags = WantCapability(line, OPTION_INCLUDE_TAG);
			}
			if (allowOfsDelta)
			{
				WantCapability(line, OPTION_OFS_DELTA);
			}
			if (WantCapability(line, OPTION_MULTI_ACK_DETAILED))
			{
				multiAck = BasePackFetchConnection.MultiAck.DETAILED;
			}
			else
			{
				if (WantCapability(line, OPTION_MULTI_ACK))
				{
					multiAck = BasePackFetchConnection.MultiAck.CONTINUE;
				}
				else
				{
					multiAck = BasePackFetchConnection.MultiAck.OFF;
				}
			}
			if (thinPack)
			{
				thinPack = WantCapability(line, OPTION_THIN_PACK);
			}
			if (WantCapability(line, OPTION_SIDE_BAND_64K))
			{
				sideband = true;
			}
			else
			{
				if (WantCapability(line, OPTION_SIDE_BAND))
				{
					sideband = true;
				}
			}
			if (statelessRPC && multiAck != BasePackFetchConnection.MultiAck.DETAILED)
			{
				// Our stateless RPC implementation relies upon the detailed
				// ACK status to tell us common objects for reuse in future
				// requests.  If its not enabled, we can't talk to the peer.
				//
				throw new PackProtocolException(uri, MessageFormat.Format(JGitText.Get().statelessRPCRequiresOptionToBeEnabled
					, OPTION_MULTI_ACK_DETAILED));
			}
			return line.ToString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Transport.BasePackFetchConnection.CancelledException"></exception>
		private void Negotiate(ProgressMonitor monitor)
		{
			MutableObjectId ackId = new MutableObjectId();
			int resultsPending = 0;
			int havesSent = 0;
			int havesSinceLastContinue = 0;
			bool receivedContinue = false;
			bool receivedAck = false;
			if (statelessRPC)
			{
				state.WriteTo(@out, null);
			}
			NegotiateBegin();
			for (; ; )
			{
				RevCommit c = walk.Next();
				if (c == null)
				{
					goto SEND_HAVES_break;
				}
				pckOut.WriteString("have " + c.Id.Name + "\n");
				havesSent++;
				havesSinceLastContinue++;
				if ((31 & havesSent) != 0)
				{
					// We group the have lines into blocks of 32, each marked
					// with a flush (aka end). This one is within a block so
					// continue with another have line.
					//
					continue;
				}
				if (monitor.IsCancelled())
				{
					throw new BasePackFetchConnection.CancelledException();
				}
				pckOut.End();
				resultsPending++;
				// Each end will cause a result to come back.
				if (havesSent == 32 && !statelessRPC)
				{
					// On the first block we race ahead and try to send
					// more of the second block while waiting for the
					// remote to respond to our first block request.
					// This keeps us one block ahead of the peer.
					//
					continue;
				}
				for (; ; )
				{
					PacketLineIn.AckNackResult anr = pckIn.ReadACK(ackId);
					switch (anr)
					{
						case PacketLineIn.AckNackResult.NAK:
						{
							// More have lines are necessary to compute the
							// pack on the remote side. Keep doing that.
							//
							resultsPending--;
							goto READ_RESULT_break;
						}

						case PacketLineIn.AckNackResult.ACK:
						{
							// The remote side is happy and knows exactly what
							// to send us. There is no further negotiation and
							// we can break out immediately.
							//
							multiAck = BasePackFetchConnection.MultiAck.OFF;
							resultsPending = 0;
							receivedAck = true;
							if (statelessRPC)
							{
								state.WriteTo(@out, null);
							}
							goto SEND_HAVES_break;
						}

						case PacketLineIn.AckNackResult.ACK_CONTINUE:
						case PacketLineIn.AckNackResult.ACK_COMMON:
						case PacketLineIn.AckNackResult.ACK_READY:
						{
							// The server knows this commit (ackId). We don't
							// need to send any further along its ancestry, but
							// we need to continue to talk about other parts of
							// our local history.
							//
							MarkCommon(walk.ParseAny(ackId), anr);
							receivedAck = true;
							receivedContinue = true;
							havesSinceLastContinue = 0;
							break;
						}
					}
					if (monitor.IsCancelled())
					{
						throw new BasePackFetchConnection.CancelledException();
					}
READ_RESULT_continue: ;
				}
READ_RESULT_break: ;
				if (statelessRPC)
				{
					state.WriteTo(@out, null);
				}
				if (receivedContinue && havesSinceLastContinue > MAX_HAVES)
				{
					// Our history must be really different from the remote's.
					// We just sent a whole slew of have lines, and it did not
					// recognize any of them. Avoid sending our entire history
					// to them by giving up early.
					//
					goto SEND_HAVES_break;
				}
SEND_HAVES_continue: ;
			}
SEND_HAVES_break: ;
			// Tell the remote side we have run out of things to talk about.
			//
			if (monitor.IsCancelled())
			{
				throw new BasePackFetchConnection.CancelledException();
			}
			// When statelessRPC is true we should always leave SEND_HAVES
			// loop above while in the middle of a request. This allows us
			// to just write done immediately.
			//
			pckOut.WriteString("done\n");
			pckOut.Flush();
			if (!receivedAck)
			{
				// Apparently if we have never received an ACK earlier
				// there is one more result expected from the done we
				// just sent to the remote.
				//
				multiAck = BasePackFetchConnection.MultiAck.OFF;
				resultsPending++;
			}
			while (resultsPending > 0 || multiAck != BasePackFetchConnection.MultiAck.OFF)
			{
				PacketLineIn.AckNackResult anr = pckIn.ReadACK(ackId);
				resultsPending--;
				switch (anr)
				{
					case PacketLineIn.AckNackResult.NAK:
					{
						// A NAK is a response to an end we queued earlier
						// we eat it and look for another ACK/NAK message.
						//
						break;
					}

					case PacketLineIn.AckNackResult.ACK:
					{
						// A solitary ACK at this point means the remote won't
						// speak anymore, but is going to send us a pack now.
						//
						goto READ_RESULT_break2;
					}

					case PacketLineIn.AckNackResult.ACK_CONTINUE:
					case PacketLineIn.AckNackResult.ACK_COMMON:
					case PacketLineIn.AckNackResult.ACK_READY:
					{
						// We will expect a normal ACK to break out of the loop.
						//
						multiAck = BasePackFetchConnection.MultiAck.CONTINUE;
						break;
					}
				}
				if (monitor.IsCancelled())
				{
					throw new BasePackFetchConnection.CancelledException();
				}
READ_RESULT_continue: ;
			}
READ_RESULT_break2: ;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void NegotiateBegin()
		{
			walk.ResetRetain(REACHABLE, ADVERTISED);
			walk.MarkStart(reachableCommits);
			walk.Sort(RevSort.COMMIT_TIME_DESC);
			walk.SetRevFilter(new _RevFilter_586(this));
		}

		private sealed class _RevFilter_586 : RevFilter
		{
			public _RevFilter_586(BasePackFetchConnection _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override RevFilter Clone()
			{
				return this;
			}

			public override bool Include(RevWalk walker, RevCommit c)
			{
				bool remoteKnowsIsCommon = c.Has(this._enclosing.COMMON);
				if (c.Has(this._enclosing.ADVERTISED))
				{
					// Remote advertised this, and we have it, hence common.
					// Whether or not the remote knows that fact is tested
					// before we added the flag. If the remote doesn't know
					// we have to still send them this object.
					//
					c.Add(this._enclosing.COMMON);
				}
				return !remoteKnowsIsCommon;
			}

			private readonly BasePackFetchConnection _enclosing;
		}

		private void MarkRefsAdvertised()
		{
			foreach (Ref r in GetRefs())
			{
				MarkAdvertised(r.GetObjectId());
				if (r.GetPeeledObjectId() != null)
				{
					MarkAdvertised(r.GetPeeledObjectId());
				}
			}
		}

		private void MarkAdvertised(AnyObjectId id)
		{
			try
			{
				walk.ParseAny(id).Add(ADVERTISED);
			}
			catch (IOException)
			{
			}
		}

		// We probably just do not have this object locally.
		/// <exception cref="System.IO.IOException"></exception>
		private void MarkCommon(RevObject obj, PacketLineIn.AckNackResult anr)
		{
			if (statelessRPC && anr == PacketLineIn.AckNackResult.ACK_COMMON && !obj.Has(STATE
				))
			{
				StringBuilder s;
				s = new StringBuilder(6 + Constants.OBJECT_ID_STRING_LENGTH);
				s.Append("have ");
				//$NON-NLS-1$
				s.Append(obj.Name);
				s.Append('\n');
				pckState.WriteString(s.ToString());
				obj.Add(STATE);
			}
			obj.Add(COMMON);
			if (obj is RevCommit)
			{
				((RevCommit)obj).Carry(COMMON);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReceivePack(ProgressMonitor monitor)
		{
			IndexPack ip;
			InputStream input = @in;
			if (sideband)
			{
				input = new SideBandInputStream(input, monitor, GetMessageWriter());
			}
			ip = IndexPack.Create(local, input);
			ip.SetFixThin(thinPack);
			ip.SetObjectChecking(transport.IsCheckFetchedObjects());
			ip.Index(monitor);
			packLock = ip.RenameAndOpenPack(lockMessage);
		}

		[System.Serializable]
		private class CancelledException : Exception
		{
			private const long serialVersionUID = 1L;
		}
	}
}
