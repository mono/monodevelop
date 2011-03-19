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
using NGit.Revwalk.Filter;
using NGit.Storage.Pack;
using NGit.Transport;
using NGit.Util.IO;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Implements the server side of a fetch connection, transmitting objects.</summary>
	/// <remarks>Implements the server side of a fetch connection, transmitting objects.</remarks>
	public class UploadPack
	{
		internal static readonly string OPTION_INCLUDE_TAG = BasePackFetchConnection.OPTION_INCLUDE_TAG;

		internal static readonly string OPTION_MULTI_ACK = BasePackFetchConnection.OPTION_MULTI_ACK;

		internal static readonly string OPTION_MULTI_ACK_DETAILED = BasePackFetchConnection
			.OPTION_MULTI_ACK_DETAILED;

		internal static readonly string OPTION_THIN_PACK = BasePackFetchConnection.OPTION_THIN_PACK;

		internal static readonly string OPTION_SIDE_BAND = BasePackFetchConnection.OPTION_SIDE_BAND;

		internal static readonly string OPTION_SIDE_BAND_64K = BasePackFetchConnection.OPTION_SIDE_BAND_64K;

		internal static readonly string OPTION_OFS_DELTA = BasePackFetchConnection.OPTION_OFS_DELTA;

		internal static readonly string OPTION_NO_PROGRESS = BasePackFetchConnection.OPTION_NO_PROGRESS;

		/// <summary>Database we read the objects from.</summary>
		/// <remarks>Database we read the objects from.</remarks>
		private readonly Repository db;

		/// <summary>
		/// Revision traversal support over
		/// <see cref="db">db</see>
		/// .
		/// </summary>
		private readonly RevWalk walk;

		/// <summary>Configuration to pass into the PackWriter.</summary>
		/// <remarks>Configuration to pass into the PackWriter.</remarks>
		private PackConfig packConfig;

		/// <summary>Timeout in seconds to wait for client interaction.</summary>
		/// <remarks>Timeout in seconds to wait for client interaction.</remarks>
		private int timeout;

		/// <summary>
		/// Is the client connection a bi-directional socket or pipe?
		/// <p>
		/// If true, this class assumes it can perform multiple read and write cycles
		/// with the client over the input and output streams.
		/// </summary>
		/// <remarks>
		/// Is the client connection a bi-directional socket or pipe?
		/// <p>
		/// If true, this class assumes it can perform multiple read and write cycles
		/// with the client over the input and output streams. This matches the
		/// functionality available with a standard TCP/IP connection, or a local
		/// operating system or in-memory pipe.
		/// <p>
		/// If false, this class runs in a read everything then output results mode,
		/// making it suitable for single round-trip systems RPCs such as HTTP.
		/// </remarks>
		private bool biDirectionalPipe = true;

		/// <summary>
		/// Timer to manage
		/// <see cref="timeout">timeout</see>
		/// .
		/// </summary>
		private InterruptTimer timer;

		private InputStream rawIn;

		private OutputStream rawOut;

		private PacketLineIn pckIn;

		private PacketLineOut pckOut;

		/// <summary>The refs we advertised as existing at the start of the connection.</summary>
		/// <remarks>The refs we advertised as existing at the start of the connection.</remarks>
		private IDictionary<string, Ref> refs;

		/// <summary>Filter used while advertising the refs to the client.</summary>
		/// <remarks>Filter used while advertising the refs to the client.</remarks>
		private RefFilter refFilter;

		/// <summary>Capabilities requested by the client.</summary>
		/// <remarks>Capabilities requested by the client.</remarks>
		private readonly ICollection<string> options = new HashSet<string>();

		/// <summary>Raw ObjectIds the client has asked for, before validating them.</summary>
		/// <remarks>Raw ObjectIds the client has asked for, before validating them.</remarks>
		private readonly ICollection<ObjectId> wantIds = new HashSet<ObjectId>();

		/// <summary>Objects the client wants to obtain.</summary>
		/// <remarks>Objects the client wants to obtain.</remarks>
		private readonly IList<RevObject> wantAll = new AList<RevObject>();

		/// <summary>Objects on both sides, these don't have to be sent.</summary>
		/// <remarks>Objects on both sides, these don't have to be sent.</remarks>
		private readonly IList<RevObject> commonBase = new AList<RevObject>();

		/// <summary>Commit time of the oldest common commit, in seconds.</summary>
		/// <remarks>Commit time of the oldest common commit, in seconds.</remarks>
		private int oldestTime;

		/// <summary>
		/// null if
		/// <see cref="commonBase">commonBase</see>
		/// should be examined again.
		/// </summary>
		private bool? okToGiveUp;

		/// <summary>Objects we sent in our advertisement list, clients can ask for these.</summary>
		/// <remarks>Objects we sent in our advertisement list, clients can ask for these.</remarks>
		private ICollection<ObjectId> advertised;

		/// <summary>Marked on objects the client has asked us to give them.</summary>
		/// <remarks>Marked on objects the client has asked us to give them.</remarks>
		private readonly RevFlag WANT;

		/// <summary>Marked on objects both we and the client have.</summary>
		/// <remarks>Marked on objects both we and the client have.</remarks>
		private readonly RevFlag PEER_HAS;

		/// <summary>
		/// Marked on objects in
		/// <see cref="commonBase">commonBase</see>
		/// .
		/// </summary>
		private readonly RevFlag COMMON;

		/// <summary>Objects where we found a path from the want list to a common base.</summary>
		/// <remarks>Objects where we found a path from the want list to a common base.</remarks>
		private readonly RevFlag SATISFIED;

		private readonly RevFlagSet SAVE;

		private int multiAck = BasePackFetchConnection.MultiAck.OFF;

		private PackWriter.Statistics statistics;

		private UploadPackLogger logger;

		/// <summary>Create a new pack upload for an open repository.</summary>
		/// <remarks>Create a new pack upload for an open repository.</remarks>
		/// <param name="copyFrom">the source repository.</param>
		public UploadPack(Repository copyFrom)
		{
			db = copyFrom;
			walk = new RevWalk(db);
			walk.SetRetainBody(false);
			WANT = walk.NewFlag("WANT");
			PEER_HAS = walk.NewFlag("PEER_HAS");
			COMMON = walk.NewFlag("COMMON");
			SATISFIED = walk.NewFlag("SATISFIED");
			walk.Carry(PEER_HAS);
			SAVE = new RevFlagSet();
			SAVE.AddItem(WANT);
			SAVE.AddItem(PEER_HAS);
			SAVE.AddItem(COMMON);
			SAVE.AddItem(SATISFIED);
			refFilter = RefFilter.DEFAULT;
		}

		/// <returns>the repository this upload is reading from.</returns>
		public Repository GetRepository()
		{
			return db;
		}

		/// <returns>the RevWalk instance used by this connection.</returns>
		public RevWalk GetRevWalk()
		{
			return walk;
		}

		/// <returns>all refs which were advertised to the client.</returns>
		public IDictionary<string, Ref> GetAdvertisedRefs()
		{
			return refs;
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
		/// before aborting an IO read or write operation with the
		/// connected client.
		/// </param>
		public virtual void SetTimeout(int seconds)
		{
			timeout = seconds;
		}

		/// <returns>
		/// true if this class expects a bi-directional pipe opened between
		/// the client and itself. The default is true.
		/// </returns>
		public virtual bool IsBiDirectionalPipe()
		{
			return biDirectionalPipe;
		}

		/// <param name="twoWay">
		/// if true, this class will assume the socket is a fully
		/// bidirectional pipe between the two peers and takes advantage
		/// of that by first transmitting the known refs, then waiting to
		/// read commands. If false, this class assumes it must read the
		/// commands before writing output and does not perform the
		/// initial advertising.
		/// </param>
		public virtual void SetBiDirectionalPipe(bool twoWay)
		{
			biDirectionalPipe = twoWay;
		}

		/// <returns>the filter used while advertising the refs to the client</returns>
		public virtual RefFilter GetRefFilter()
		{
			return refFilter;
		}

		/// <summary>Set the filter used while advertising the refs to the client.</summary>
		/// <remarks>
		/// Set the filter used while advertising the refs to the client.
		/// <p>
		/// Only refs allowed by this filter will be sent to the client. This can
		/// be used by a server to restrict the list of references the client can
		/// obtain through clone or fetch, effectively limiting the access to only
		/// certain refs.
		/// </remarks>
		/// <param name="refFilter">the filter; may be null to show all refs.</param>
		public virtual void SetRefFilter(RefFilter refFilter)
		{
			this.refFilter = refFilter != null ? refFilter : RefFilter.DEFAULT;
		}

		/// <summary>Set the configuration used by the pack generator.</summary>
		/// <remarks>Set the configuration used by the pack generator.</remarks>
		/// <param name="pc">
		/// configuration controlling packing parameters. If null the
		/// source repository's settings will be used.
		/// </param>
		public virtual void SetPackConfig(PackConfig pc)
		{
			this.packConfig = pc;
		}

		/// <summary>Set the logger.</summary>
		/// <remarks>Set the logger.</remarks>
		/// <param name="logger">the logger instance. If null, no logging occurs.</param>
		public virtual void SetLogger(UploadPackLogger logger)
		{
			this.logger = logger;
		}

		/// <summary>Execute the upload task on the socket.</summary>
		/// <remarks>Execute the upload task on the socket.</remarks>
		/// <param name="input">
		/// raw input to read client commands from. Caller must ensure the
		/// input is buffered, otherwise read performance may suffer.
		/// </param>
		/// <param name="output">
		/// response back to the Git network client, to write the pack
		/// data onto. Caller must ensure the output is buffered,
		/// otherwise write performance may suffer.
		/// </param>
		/// <param name="messages">
		/// secondary "notice" channel to send additional messages out
		/// through. When run over SSH this should be tied back to the
		/// standard error channel of the command execution. For most
		/// other network connections this should be null.
		/// </param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void Upload(InputStream input, OutputStream output, OutputStream messages
			)
		{
			try
			{
				rawIn = input;
				rawOut = output;
				if (timeout > 0)
				{
					Sharpen.Thread caller = Sharpen.Thread.CurrentThread();
					timer = new InterruptTimer(caller.GetName() + "-Timer");
					TimeoutInputStream i = new TimeoutInputStream(rawIn, timer);
					TimeoutOutputStream o = new TimeoutOutputStream(rawOut, timer);
					i.SetTimeout(timeout * 1000);
					o.SetTimeout(timeout * 1000);
					rawIn = i;
					rawOut = o;
				}
				pckIn = new PacketLineIn(rawIn);
				pckOut = new PacketLineOut(rawOut);
				Service();
			}
			finally
			{
				walk.Release();
				if (timer != null)
				{
					try
					{
						timer.Terminate();
					}
					finally
					{
						timer = null;
					}
				}
			}
		}

		/// <summary>Get the PackWriter's statistics if a pack was sent to the client.</summary>
		/// <remarks>Get the PackWriter's statistics if a pack was sent to the client.</remarks>
		/// <returns>
		/// statistics about pack output, if a pack was sent. Null if no pack
		/// was sent, such as during the negotation phase of a smart HTTP
		/// connection, or if the client was already up-to-date.
		/// </returns>
		public virtual PackWriter.Statistics GetPackStatistics()
		{
			return statistics;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Service()
		{
			if (biDirectionalPipe)
			{
				SendAdvertisedRefs(new RefAdvertiser.PacketLineOutRefAdvertiser(pckOut));
			}
			else
			{
				advertised = new HashSet<ObjectId>();
				refs = refFilter.Filter(db.GetAllRefs());
				foreach (Ref @ref in refs.Values)
				{
					if (@ref.GetObjectId() != null)
					{
						advertised.AddItem(@ref.GetObjectId());
					}
				}
			}
			RecvWants();
			if (wantIds.IsEmpty())
			{
				return;
			}
			if (options.Contains(OPTION_MULTI_ACK_DETAILED))
			{
				multiAck = BasePackFetchConnection.MultiAck.DETAILED;
			}
			else
			{
				if (options.Contains(OPTION_MULTI_ACK))
				{
					multiAck = BasePackFetchConnection.MultiAck.CONTINUE;
				}
				else
				{
					multiAck = BasePackFetchConnection.MultiAck.OFF;
				}
			}
			if (Negotiate())
			{
				SendPack();
			}
		}

		/// <summary>Generate an advertisement of available refs and capabilities.</summary>
		/// <remarks>Generate an advertisement of available refs and capabilities.</remarks>
		/// <param name="adv">the advertisement formatter.</param>
		/// <exception cref="System.IO.IOException">the formatter failed to write an advertisement.
		/// 	</exception>
		public virtual void SendAdvertisedRefs(RefAdvertiser adv)
		{
			adv.Init(db);
			adv.AdvertiseCapability(OPTION_INCLUDE_TAG);
			adv.AdvertiseCapability(OPTION_MULTI_ACK_DETAILED);
			adv.AdvertiseCapability(OPTION_MULTI_ACK);
			adv.AdvertiseCapability(OPTION_OFS_DELTA);
			adv.AdvertiseCapability(OPTION_SIDE_BAND);
			adv.AdvertiseCapability(OPTION_SIDE_BAND_64K);
			adv.AdvertiseCapability(OPTION_THIN_PACK);
			adv.AdvertiseCapability(OPTION_NO_PROGRESS);
			adv.SetDerefTags(true);
			refs = refFilter.Filter(db.GetAllRefs());
			advertised = adv.Send(refs);
			adv.End();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void RecvWants()
		{
			bool isFirst = true;
			for (; ; )
			{
				string line;
				try
				{
					line = pckIn.ReadString();
				}
				catch (EOFException eof)
				{
					if (isFirst)
					{
						break;
					}
					throw;
				}
				if (line == PacketLineIn.END)
				{
					break;
				}
				if (!line.StartsWith("want ") || line.Length < 45)
				{
					throw new PackProtocolException(MessageFormat.Format(JGitText.Get().expectedGot, 
						"want", line));
				}
				if (isFirst && line.Length > 45)
				{
					string opt = Sharpen.Runtime.Substring(line, 45);
					if (opt.StartsWith(" "))
					{
						opt = Sharpen.Runtime.Substring(opt, 1);
					}
					foreach (string c in opt.Split(" "))
					{
						options.AddItem(c);
					}
					line = Sharpen.Runtime.Substring(line, 0, 45);
				}
				wantIds.AddItem(ObjectId.FromString(Sharpen.Runtime.Substring(line, 5)));
				isFirst = false;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool Negotiate()
		{
			okToGiveUp = false;
			ObjectId last = ObjectId.ZeroId;
			IList<ObjectId> peerHas = new AList<ObjectId>(64);
			for (; ; )
			{
				string line;
				try
				{
					line = pckIn.ReadString();
				}
				catch (EOFException eof)
				{
					throw;
				}
				if (line == PacketLineIn.END)
				{
					last = ProcessHaveLines(peerHas, last);
					if (commonBase.IsEmpty() || multiAck != BasePackFetchConnection.MultiAck.OFF)
					{
						pckOut.WriteString("NAK\n");
					}
					if (!biDirectionalPipe)
					{
						return false;
					}
					pckOut.Flush();
				}
				else
				{
					if (line.StartsWith("have ") && line.Length == 45)
					{
						peerHas.AddItem(ObjectId.FromString(Sharpen.Runtime.Substring(line, 5)));
					}
					else
					{
						if (line.Equals("done"))
						{
							last = ProcessHaveLines(peerHas, last);
							if (commonBase.IsEmpty())
							{
								pckOut.WriteString("NAK\n");
							}
							else
							{
								if (multiAck != BasePackFetchConnection.MultiAck.OFF)
								{
									pckOut.WriteString("ACK " + last.Name + "\n");
								}
							}
							return true;
						}
						else
						{
							throw new PackProtocolException(MessageFormat.Format(JGitText.Get().expectedGot, 
								"have", line));
						}
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ObjectId ProcessHaveLines(IList<ObjectId> peerHas, ObjectId last)
		{
			if (peerHas.IsEmpty())
			{
				return last;
			}
			IList<ObjectId> toParse = peerHas;
			HashSet<ObjectId> peerHasSet = null;
			bool needMissing = false;
			if (wantAll.IsEmpty() && !wantIds.IsEmpty())
			{
				// We have not yet parsed the want list. Parse it now.
				peerHasSet = new HashSet<ObjectId>(peerHas);
				int cnt = wantIds.Count + peerHasSet.Count;
				toParse = new AList<ObjectId>(cnt);
				Sharpen.Collections.AddAll(toParse, wantIds);
				Sharpen.Collections.AddAll(toParse, peerHasSet);
				needMissing = true;
			}
			AsyncRevObjectQueue q = walk.ParseAny(toParse.AsIterable(), needMissing);
			try
			{
				for (; ; )
				{
					RevObject obj;
					try
					{
						obj = q.Next();
					}
					catch (MissingObjectException notFound)
					{
						ObjectId id = notFound.GetObjectId();
						if (wantIds.Contains(id))
						{
							string msg = MessageFormat.Format(JGitText.Get().wantNotValid, id.Name);
							pckOut.WriteString("ERR " + msg);
							throw new PackProtocolException(msg, notFound);
						}
						continue;
					}
					if (obj == null)
					{
						break;
					}
					// If the object is still found in wantIds, the want
					// list wasn't parsed earlier, and was done in this batch.
					//
					if (wantIds.Remove(obj))
					{
						if (!advertised.Contains(obj))
						{
							string msg = MessageFormat.Format(JGitText.Get().wantNotValid, obj.Name);
							pckOut.WriteString("ERR " + msg);
							throw new PackProtocolException(msg);
						}
						if (!obj.Has(WANT))
						{
							obj.Add(WANT);
							wantAll.AddItem(obj);
						}
						if (!(obj is RevCommit))
						{
							obj.Add(SATISFIED);
						}
						if (obj is RevTag)
						{
							RevObject target = walk.Peel(obj);
							if (target is RevCommit)
							{
								if (!target.Has(WANT))
								{
									target.Add(WANT);
									wantAll.AddItem(target);
								}
							}
						}
						if (!peerHasSet.Contains(obj))
						{
							continue;
						}
					}
					last = obj;
					if (obj is RevCommit)
					{
						RevCommit c = (RevCommit)obj;
						if (oldestTime == 0 || c.CommitTime < oldestTime)
						{
							oldestTime = c.CommitTime;
						}
					}
					if (obj.Has(PEER_HAS))
					{
						continue;
					}
					obj.Add(PEER_HAS);
					if (obj is RevCommit)
					{
						((RevCommit)obj).Carry(PEER_HAS);
					}
					AddCommonBase(obj);
					switch (multiAck)
					{
						case BasePackFetchConnection.MultiAck.OFF:
						{
							// If both sides have the same object; let the client know.
							//
							if (commonBase.Count == 1)
							{
								pckOut.WriteString("ACK " + obj.Name + "\n");
							}
							break;
						}

						case BasePackFetchConnection.MultiAck.CONTINUE:
						{
							pckOut.WriteString("ACK " + obj.Name + " continue\n");
							break;
						}

						case BasePackFetchConnection.MultiAck.DETAILED:
						{
							pckOut.WriteString("ACK " + obj.Name + " common\n");
							break;
						}
					}
				}
			}
			finally
			{
				q.Release();
			}
			// If we don't have one of the objects but we're also willing to
			// create a pack at this point, let the client know so it stops
			// telling us about its history.
			//
			bool didOkToGiveUp = false;
			for (int i = peerHas.Count - 1; i >= 0; i--)
			{
				ObjectId id = peerHas[i];
				if (walk.LookupOrNull(id) == null)
				{
					didOkToGiveUp = true;
					if (OkToGiveUp())
					{
						switch (multiAck)
						{
							case BasePackFetchConnection.MultiAck.OFF:
							{
								break;
							}

							case BasePackFetchConnection.MultiAck.CONTINUE:
							{
								pckOut.WriteString("ACK " + id.Name + " continue\n");
								break;
							}

							case BasePackFetchConnection.MultiAck.DETAILED:
							{
								pckOut.WriteString("ACK " + id.Name + " ready\n");
								break;
							}
						}
					}
					break;
				}
			}
			if (multiAck == BasePackFetchConnection.MultiAck.DETAILED && !didOkToGiveUp && OkToGiveUp
				())
			{
				ObjectId id = peerHas[peerHas.Count - 1];
				pckOut.WriteString("ACK " + id.Name + " ready\n");
			}
			peerHas.Clear();
			return last;
		}

		private void AddCommonBase(RevObject o)
		{
			if (!o.Has(COMMON))
			{
				o.Add(COMMON);
				commonBase.AddItem(o);
				okToGiveUp = null;
			}
		}

		/// <exception cref="NGit.Errors.PackProtocolException"></exception>
		private bool OkToGiveUp()
		{
			if (okToGiveUp == null)
			{
				okToGiveUp = Sharpen.Extensions.ValueOf(OkToGiveUpImp());
			}
			return okToGiveUp.Value;
		}

		/// <exception cref="NGit.Errors.PackProtocolException"></exception>
		private bool OkToGiveUpImp()
		{
			if (commonBase.IsEmpty())
			{
				return false;
			}
			try
			{
				foreach (RevObject obj in wantAll)
				{
					if (!WantSatisfied(obj))
					{
						return false;
					}
				}
				return true;
			}
			catch (IOException e)
			{
				throw new PackProtocolException(JGitText.Get().internalRevisionError, e);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool WantSatisfied(RevObject want)
		{
			if (want.Has(SATISFIED))
			{
				return true;
			}
			walk.ResetRetain(SAVE);
			walk.MarkStart((RevCommit)want);
			if (oldestTime != 0)
			{
				walk.SetRevFilter(CommitTimeRevFilter.After(oldestTime * 1000L));
			}
			for (; ; )
			{
				RevCommit c = walk.Next();
				if (c == null)
				{
					break;
				}
				if (c.Has(PEER_HAS))
				{
					AddCommonBase(c);
					want.Add(SATISFIED);
					return true;
				}
			}
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SendPack()
		{
			bool sideband = options.Contains(OPTION_SIDE_BAND) || options.Contains(OPTION_SIDE_BAND_64K
				);
			ProgressMonitor pm = NullProgressMonitor.INSTANCE;
			OutputStream packOut = rawOut;
			SideBandOutputStream msgOut = null;
			if (sideband)
			{
				int bufsz = SideBandOutputStream.SMALL_BUF;
				if (options.Contains(OPTION_SIDE_BAND_64K))
				{
					bufsz = SideBandOutputStream.MAX_BUF;
				}
				packOut = new SideBandOutputStream(SideBandOutputStream.CH_DATA, bufsz, rawOut);
				if (!options.Contains(OPTION_NO_PROGRESS))
				{
					msgOut = new SideBandOutputStream(SideBandOutputStream.CH_PROGRESS, bufsz, rawOut
						);
					pm = new SideBandProgressMonitor(msgOut);
				}
			}
			PackConfig cfg = packConfig;
			if (cfg == null)
			{
				cfg = new PackConfig(db);
			}
			PackWriter pw = new PackWriter(cfg, walk.GetObjectReader());
			try
			{
				pw.SetUseCachedPacks(true);
				pw.SetReuseDeltaCommits(true);
				pw.SetDeltaBaseAsOffset(options.Contains(OPTION_OFS_DELTA));
				pw.SetThin(options.Contains(OPTION_THIN_PACK));
				pw.SetReuseValidatingObjects(false);
				if (commonBase.IsEmpty())
				{
					ICollection<ObjectId> tagTargets = new HashSet<ObjectId>();
					foreach (Ref @ref in refs.Values)
					{
						if (@ref.GetPeeledObjectId() != null)
						{
							tagTargets.AddItem(@ref.GetPeeledObjectId());
						}
						else
						{
							if (@ref.GetObjectId() == null)
							{
								continue;
							}
							else
							{
								if (@ref.GetName().StartsWith(Constants.R_HEADS))
								{
									tagTargets.AddItem(@ref.GetObjectId());
								}
							}
						}
					}
					pw.SetTagTargets(tagTargets);
				}
				RevWalk rw = walk;
				if (wantAll.IsEmpty())
				{
					pw.PreparePack(pm, wantIds, commonBase);
				}
				else
				{
					walk.Reset();
					ObjectWalk ow = walk.ToObjectWalkWithSameObjects();
					pw.PreparePack(pm, ow, wantAll, commonBase);
					rw = ow;
				}
				if (options.Contains(OPTION_INCLUDE_TAG))
				{
					foreach (Ref vref in refs.Values)
					{
						Ref @ref = vref;
						ObjectId objectId = @ref.GetObjectId();
						// If the object was already requested, skip it.
						if (wantAll.IsEmpty())
						{
							if (wantIds.Contains(objectId))
							{
								continue;
							}
						}
						else
						{
							RevObject obj = rw.LookupOrNull(objectId);
							if (obj != null && obj.Has(WANT))
							{
								continue;
							}
						}
						if (!@ref.IsPeeled())
						{
							@ref = db.Peel(@ref);
						}
						ObjectId peeledId = @ref.GetPeeledObjectId();
						if (peeledId == null)
						{
							continue;
						}
						objectId = @ref.GetObjectId();
						if (pw.WillInclude(peeledId) && !pw.WillInclude(objectId))
						{
							pw.AddObject(rw.ParseAny(objectId));
						}
					}
				}
				pw.WritePack(pm, NullProgressMonitor.INSTANCE, packOut);
				statistics = pw.GetStatistics();
				if (msgOut != null)
				{
					string msg = pw.GetStatistics().GetMessage() + '\n';
					msgOut.Write(Constants.Encode(msg));
					msgOut.Flush();
				}
			}
			finally
			{
				pw.Release();
			}
			if (sideband)
			{
				pckOut.End();
			}
			if (logger != null && statistics != null)
			{
				logger.OnPackStatistics(statistics);
			}
		}
	}
}
