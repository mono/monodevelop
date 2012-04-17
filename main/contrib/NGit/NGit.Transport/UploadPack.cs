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
using NGit.Internal;
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

		internal static readonly string OPTION_NO_DONE = BasePackFetchConnection.OPTION_NO_DONE;

		internal static readonly string OPTION_SHALLOW = BasePackFetchConnection.OPTION_SHALLOW;

		/// <summary>Policy the server uses to validate client requests</summary>
		public enum RequestPolicy
		{
			ADVERTISED,
			REACHABLE_COMMIT,
			ANY
		}

		/// <summary>Data in the first line of a request, the line itself plus options.</summary>
		/// <remarks>Data in the first line of a request, the line itself plus options.</remarks>
		public class FirstLine
		{
			private readonly string line;

			private readonly ICollection<string> options;

			/// <summary>Parse the first line of a receive-pack request.</summary>
			/// <remarks>Parse the first line of a receive-pack request.</remarks>
			/// <param name="line">line from the client.</param>
			public FirstLine(string line)
			{
				if (line.Length > 45)
				{
					HashSet<string> opts = new HashSet<string>();
					string opt = Sharpen.Runtime.Substring(line, 45);
					if (opt.StartsWith(" "))
					{
						opt = Sharpen.Runtime.Substring(opt, 1);
					}
					foreach (string c in opt.Split(" "))
					{
						opts.AddItem(c);
					}
					this.line = Sharpen.Runtime.Substring(line, 0, 45);
					this.options = Sharpen.Collections.UnmodifiableSet(opts);
				}
				else
				{
					this.line = line;
					this.options = Sharpen.Collections.EmptySet<string>();
				}
			}

			/// <returns>non-capabilities part of the line.</returns>
			public virtual string GetLine()
			{
				return line;
			}

			/// <returns>options parsed from the line.</returns>
			public virtual ICollection<string> GetOptions()
			{
				return options;
			}
		}

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

		/// <summary>Hook used while advertising the refs to the client.</summary>
		/// <remarks>Hook used while advertising the refs to the client.</remarks>
		private AdvertiseRefsHook advertiseRefsHook = AdvertiseRefsHook.DEFAULT;

		/// <summary>Filter used while advertising the refs to the client.</summary>
		/// <remarks>Filter used while advertising the refs to the client.</remarks>
		private RefFilter refFilter = RefFilter.DEFAULT;

		/// <summary>Hook handling the various upload phases.</summary>
		/// <remarks>Hook handling the various upload phases.</remarks>
		private PreUploadHook preUploadHook = PreUploadHook.NULL;

		/// <summary>Capabilities requested by the client.</summary>
		/// <remarks>Capabilities requested by the client.</remarks>
		private ICollection<string> options;

		/// <summary>Raw ObjectIds the client has asked for, before validating them.</summary>
		/// <remarks>Raw ObjectIds the client has asked for, before validating them.</remarks>
		private readonly ICollection<ObjectId> wantIds = new HashSet<ObjectId>();

		/// <summary>Objects the client wants to obtain.</summary>
		/// <remarks>Objects the client wants to obtain.</remarks>
		private readonly ICollection<RevObject> wantAll = new HashSet<RevObject>();

		/// <summary>Objects on both sides, these don't have to be sent.</summary>
		/// <remarks>Objects on both sides, these don't have to be sent.</remarks>
		private readonly ICollection<RevObject> commonBase = new HashSet<RevObject>();

		/// <summary>Shallow commits the client already has.</summary>
		/// <remarks>Shallow commits the client already has.</remarks>
		private readonly ICollection<ObjectId> clientShallowCommits = new HashSet<ObjectId
			>();

		/// <summary>Shallow commits on the client which are now becoming unshallow</summary>
		private readonly IList<ObjectId> unshallowCommits = new AList<ObjectId>();

		/// <summary>Desired depth from the client on a shallow request.</summary>
		/// <remarks>Desired depth from the client on a shallow request.</remarks>
		private int depth;

		/// <summary>Commit time of the oldest common commit, in seconds.</summary>
		/// <remarks>Commit time of the oldest common commit, in seconds.</remarks>
		private int oldestTime;

		/// <summary>
		/// null if
		/// <see cref="commonBase">commonBase</see>
		/// should be examined again.
		/// </summary>
		private bool? okToGiveUp;

		private bool sentReady;

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

		private UploadPack.RequestPolicy requestPolicy = UploadPack.RequestPolicy.ADVERTISED;

		private int multiAck = BasePackFetchConnection.MultiAck.OFF;

		private bool noDone;

		private PackWriter.Statistics statistics;

		private UploadPackLogger logger = UploadPackLogger.NULL;

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

		/// <summary>Get refs which were advertised to the client.</summary>
		/// <remarks>Get refs which were advertised to the client.</remarks>
		/// <returns>
		/// all refs which were advertised to the client, or null if
		/// <see cref="SetAdvertisedRefs(System.Collections.Generic.IDictionary{K, V})">SetAdvertisedRefs(System.Collections.Generic.IDictionary&lt;K, V&gt;)
		/// 	</see>
		/// has not been called yet.
		/// </returns>
		public IDictionary<string, Ref> GetAdvertisedRefs()
		{
			return refs;
		}

		/// <summary>Set the refs advertised by this UploadPack.</summary>
		/// <remarks>
		/// Set the refs advertised by this UploadPack.
		/// <p>
		/// Intended to be called from a
		/// <see cref="PreUploadHook">PreUploadHook</see>
		/// .
		/// </remarks>
		/// <param name="allRefs">
		/// explicit set of references to claim as advertised by this
		/// UploadPack instance. This overrides any references that
		/// may exist in the source repository. The map is passed
		/// to the configured
		/// <see cref="GetRefFilter()">GetRefFilter()</see>
		/// . If null, assumes
		/// all refs were advertised.
		/// </param>
		public virtual void SetAdvertisedRefs(IDictionary<string, Ref> allRefs)
		{
			if (allRefs != null)
			{
				refs = allRefs;
			}
			else
			{
				refs = db.GetAllRefs();
			}
			refs = refFilter.Filter(refs);
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
			if (!biDirectionalPipe && requestPolicy == UploadPack.RequestPolicy.ADVERTISED)
			{
				requestPolicy = UploadPack.RequestPolicy.REACHABLE_COMMIT;
			}
		}

		/// <returns>policy used by the service to validate client requests.</returns>
		public virtual UploadPack.RequestPolicy GetRequestPolicy()
		{
			return requestPolicy;
		}

		/// <param name="policy">
		/// the policy used to enforce validation of a client's want list.
		/// By default the policy is
		/// <see cref="RequestPolicy.ADVERTISED">RequestPolicy.ADVERTISED</see>
		/// ,
		/// which is the Git default requiring clients to only ask for an
		/// object that a reference directly points to. This may be relaxed
		/// to
		/// <see cref="RequestPolicy.REACHABLE_COMMIT">RequestPolicy.REACHABLE_COMMIT</see>
		/// when callers
		/// have
		/// <see cref="SetBiDirectionalPipe(bool)">SetBiDirectionalPipe(bool)</see>
		/// set to false.
		/// </param>
		public virtual void SetRequestPolicy(UploadPack.RequestPolicy policy)
		{
			requestPolicy = policy != null ? policy : UploadPack.RequestPolicy.ADVERTISED;
		}

		/// <returns>the hook used while advertising the refs to the client</returns>
		public virtual AdvertiseRefsHook GetAdvertiseRefsHook()
		{
			return advertiseRefsHook;
		}

		/// <returns>the filter used while advertising the refs to the client</returns>
		public virtual RefFilter GetRefFilter()
		{
			return refFilter;
		}

		/// <summary>Set the hook used while advertising the refs to the client.</summary>
		/// <remarks>
		/// Set the hook used while advertising the refs to the client.
		/// <p>
		/// If the
		/// <see cref="AdvertiseRefsHook">AdvertiseRefsHook</see>
		/// chooses to call
		/// <see cref="SetAdvertisedRefs(System.Collections.Generic.IDictionary{K, V})">SetAdvertisedRefs(System.Collections.Generic.IDictionary&lt;K, V&gt;)
		/// 	</see>
		/// , only refs set by this hook <em>and</em>
		/// selected by the
		/// <see cref="RefFilter">RefFilter</see>
		/// will be shown to the client.
		/// </remarks>
		/// <param name="advertiseRefsHook">the hook; may be null to show all refs.</param>
		public virtual void SetAdvertiseRefsHook(AdvertiseRefsHook advertiseRefsHook)
		{
			if (advertiseRefsHook != null)
			{
				this.advertiseRefsHook = advertiseRefsHook;
			}
			else
			{
				this.advertiseRefsHook = AdvertiseRefsHook.DEFAULT;
			}
		}

		/// <summary>Set the filter used while advertising the refs to the client.</summary>
		/// <remarks>
		/// Set the filter used while advertising the refs to the client.
		/// <p>
		/// Only refs allowed by this filter will be sent to the client.
		/// The filter is run against the refs specified by the
		/// <see cref="AdvertiseRefsHook">AdvertiseRefsHook</see>
		/// (if applicable).
		/// </remarks>
		/// <param name="refFilter">the filter; may be null to show all refs.</param>
		public virtual void SetRefFilter(RefFilter refFilter)
		{
			this.refFilter = refFilter != null ? refFilter : RefFilter.DEFAULT;
		}

		/// <returns>the configured upload hook.</returns>
		public virtual PreUploadHook GetPreUploadHook()
		{
			return preUploadHook;
		}

		/// <summary>Set the hook that controls how this instance will behave.</summary>
		/// <remarks>Set the hook that controls how this instance will behave.</remarks>
		/// <param name="hook">the hook; if null no special actions are taken.</param>
		public virtual void SetPreUploadHook(PreUploadHook hook)
		{
			preUploadHook = hook != null ? hook : PreUploadHook.NULL;
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

		/// <returns>the configured logger.</returns>
		public virtual UploadPackLogger GetLogger()
		{
			return logger;
		}

		/// <summary>Set the logger.</summary>
		/// <remarks>Set the logger.</remarks>
		/// <param name="logger">the logger instance. If null, no logging occurs.</param>
		public virtual void SetLogger(UploadPackLogger logger)
		{
			this.logger = logger;
		}

		/// <summary>Check whether the client expects a side-band stream.</summary>
		/// <remarks>Check whether the client expects a side-band stream.</remarks>
		/// <returns>
		/// true if the client has advertised a side-band capability, false
		/// otherwise.
		/// </returns>
		/// <exception cref="RequestNotYetReadException">
		/// if the client's request has not yet been read from the wire, so
		/// we do not know if they expect side-band. Note that the client
		/// may have already written the request, it just has not been
		/// read.
		/// </exception>
		/// <exception cref="NGit.Transport.RequestNotYetReadException"></exception>
		public virtual bool IsSideBand()
		{
			if (options == null)
			{
				throw new RequestNotYetReadException();
			}
			return (options.Contains(OPTION_SIDE_BAND) || options.Contains(OPTION_SIDE_BAND_64K
				));
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

		private IDictionary<string, Ref> GetAdvertisedOrDefaultRefs()
		{
			if (refs == null)
			{
				SetAdvertisedRefs(null);
			}
			return refs;
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
				if (requestPolicy == UploadPack.RequestPolicy.ANY)
				{
					advertised = Sharpen.Collections.EmptySet<ObjectId>();
				}
				else
				{
					advertised = new HashSet<ObjectId>();
					foreach (Ref @ref in GetAdvertisedOrDefaultRefs().Values)
					{
						if (@ref.GetObjectId() != null)
						{
							advertised.AddItem(@ref.GetObjectId());
						}
					}
				}
			}
			bool sendPack;
			try
			{
				RecvWants();
				if (wantIds.IsEmpty())
				{
					preUploadHook.OnBeginNegotiateRound(this, wantIds, 0);
					preUploadHook.OnEndNegotiateRound(this, wantIds, 0, 0, false);
					return;
				}
				if (options.Contains(OPTION_MULTI_ACK_DETAILED))
				{
					multiAck = BasePackFetchConnection.MultiAck.DETAILED;
					noDone = options.Contains(OPTION_NO_DONE);
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
				if (depth != 0)
				{
					ProcessShallow();
				}
				sendPack = Negotiate();
			}
			catch (PackProtocolException err)
			{
				ReportErrorDuringNegotiate(err.Message);
				throw;
			}
			catch (ServiceMayNotContinueException err)
			{
				if (!err.IsOutput() && err.Message != null)
				{
					try
					{
						pckOut.WriteString("ERR " + err.Message + "\n");
						err.SetOutput();
					}
					catch
					{
					}
				}
				// Ignore this secondary failure (and not mark output).
				throw;
			}
			catch (IOException err)
			{
				ReportErrorDuringNegotiate(JGitText.Get().internalServerError);
				throw;
			}
			catch (RuntimeException err)
			{
				ReportErrorDuringNegotiate(JGitText.Get().internalServerError);
				throw;
			}
			catch (Error err)
			{
				ReportErrorDuringNegotiate(JGitText.Get().internalServerError);
				throw;
			}
			if (sendPack)
			{
				SendPack();
			}
		}

		private void ReportErrorDuringNegotiate(string msg)
		{
			try
			{
				pckOut.WriteString("ERR " + msg + "\n");
			}
			catch
			{
			}
		}

		// Ignore this secondary failure.
		/// <exception cref="System.IO.IOException"></exception>
		private void ProcessShallow()
		{
			var depthWalk = new NGit.Revwalk.Depthwalk.RevWalk(walk.GetObjectReader(), depth
				);
			// Find all the commits which will be shallow
			foreach (ObjectId o in wantIds)
			{
				try
				{
					depthWalk.MarkRoot(depthWalk.ParseCommit(o));
				}
				catch (IncorrectObjectTypeException)
				{
				}
			}
			// Ignore non-commits in this loop.
			RevCommit o_1;
			while ((o_1 = depthWalk.Next()) != null)
			{
				var c = (NGit.Revwalk.Depthwalk.Commit)o_1;
				// Commits at the boundary which aren't already shallow in
				// the client need to be marked as such
				if (c.GetDepth() == depth && !clientShallowCommits.Contains(c))
				{
					pckOut.WriteString("shallow " + o_1.Name);
				}
				// Commits not on the boundary which are shallow in the client
				// need to become unshallowed
				if (c.GetDepth() < depth && clientShallowCommits.Contains(c))
				{
					unshallowCommits.AddItem(c.Copy());
					pckOut.WriteString("unshallow " + c.Name);
				}
			}
			pckOut.End();
		}

		/// <summary>Generate an advertisement of available refs and capabilities.</summary>
		/// <remarks>Generate an advertisement of available refs and capabilities.</remarks>
		/// <param name="adv">the advertisement formatter.</param>
		/// <exception cref="System.IO.IOException">the formatter failed to write an advertisement.
		/// 	</exception>
		/// <exception cref="ServiceMayNotContinueException">the hook denied advertisement.</exception>
		/// <exception cref="NGit.Transport.ServiceMayNotContinueException"></exception>
		public virtual void SendAdvertisedRefs(RefAdvertiser adv)
		{
			try
			{
				advertiseRefsHook.AdvertiseRefs(this);
			}
			catch (ServiceMayNotContinueException fail)
			{
				if (fail.Message != null)
				{
					adv.WriteOne("ERR " + fail.Message);
					fail.SetOutput();
				}
				throw;
			}
			adv.Init(db);
			adv.AdvertiseCapability(OPTION_INCLUDE_TAG);
			adv.AdvertiseCapability(OPTION_MULTI_ACK_DETAILED);
			adv.AdvertiseCapability(OPTION_MULTI_ACK);
			adv.AdvertiseCapability(OPTION_OFS_DELTA);
			adv.AdvertiseCapability(OPTION_SIDE_BAND);
			adv.AdvertiseCapability(OPTION_SIDE_BAND_64K);
			adv.AdvertiseCapability(OPTION_THIN_PACK);
			adv.AdvertiseCapability(OPTION_NO_PROGRESS);
			adv.AdvertiseCapability(OPTION_SHALLOW);
			if (!biDirectionalPipe)
			{
				adv.AdvertiseCapability(OPTION_NO_DONE);
			}
			adv.SetDerefTags(true);
			advertised = adv.Send(GetAdvertisedOrDefaultRefs());
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
				if (line.StartsWith("deepen "))
				{
					depth = System.Convert.ToInt32(Sharpen.Runtime.Substring(line, 7));
					continue;
				}
				if (line.StartsWith("shallow "))
				{
					clientShallowCommits.AddItem(ObjectId.FromString(Sharpen.Runtime.Substring(line, 
						8)));
					continue;
				}
				if (!line.StartsWith("want ") || line.Length < 45)
				{
					throw new PackProtocolException(MessageFormat.Format(JGitText.Get().expectedGot, 
						"want", line));
				}
				if (isFirst && line.Length > 45)
				{
					UploadPack.FirstLine firstLine = new UploadPack.FirstLine(line);
					options = firstLine.GetOptions();
					line = firstLine.GetLine();
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
					// EOF on stateless RPC (aka smart HTTP) and non-shallow request
					// means the client asked for the updated shallow/unshallow data,
					// disconnected, and will try another request with actual want/have.
					// Don't report the EOF here, its a bug in the protocol that the client
					// just disconnects without sending an END.
					if (!biDirectionalPipe && depth > 0)
					{
						return false;
					}
					throw;
				}
				if (line == PacketLineIn.END)
				{
					last = ProcessHaveLines(peerHas, last);
					if (commonBase.IsEmpty() || multiAck != BasePackFetchConnection.MultiAck.OFF)
					{
						pckOut.WriteString("NAK\n");
					}
					if (noDone && sentReady)
					{
						pckOut.WriteString("ACK " + last.Name + "\n");
						return true;
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
			preUploadHook.OnBeginNegotiateRound(this, wantIds, peerHas.Count);
			if (peerHas.IsEmpty())
			{
				return last;
			}
			IList<ObjectId> toParse = peerHas;
			HashSet<ObjectId> peerHasSet = null;
			bool needMissing = false;
			sentReady = false;
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
			ICollection<RevObject> notAdvertisedWants = null;
			int haveCnt = 0;
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
						if (!advertised.Contains(obj) && requestPolicy != UploadPack.RequestPolicy.ANY)
						{
							if (notAdvertisedWants == null)
							{
								notAdvertisedWants = new HashSet<RevObject>();
							}
							notAdvertisedWants.AddItem(obj);
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
					haveCnt++;
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
			// If the client asked for non advertised object, check our policy.
			if (notAdvertisedWants != null && !notAdvertisedWants.IsEmpty())
			{
				switch (requestPolicy)
				{
					case UploadPack.RequestPolicy.ADVERTISED:
					default:
					{
						throw new PackProtocolException(MessageFormat.Format(JGitText.Get().wantNotValid, 
							notAdvertisedWants.Iterator().Next().Name));
					}

					case UploadPack.RequestPolicy.REACHABLE_COMMIT:
					{
						CheckNotAdvertisedWants(notAdvertisedWants);
						break;
					}

					case UploadPack.RequestPolicy.ANY:
					{
						// Allow whatever was asked for.
						break;
					}
				}
			}
			int missCnt = peerHas.Count - haveCnt;
			// If we don't have one of the objects but we're also willing to
			// create a pack at this point, let the client know so it stops
			// telling us about its history.
			//
			bool didOkToGiveUp = false;
			if (0 < missCnt)
			{
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
									sentReady = true;
									break;
								}
							}
						}
						break;
					}
				}
			}
			if (multiAck == BasePackFetchConnection.MultiAck.DETAILED && !didOkToGiveUp && OkToGiveUp
				())
			{
				ObjectId id = peerHas[peerHas.Count - 1];
				sentReady = true;
				pckOut.WriteString("ACK " + id.Name + " ready\n");
				sentReady = true;
			}
			preUploadHook.OnEndNegotiateRound(this, wantAll, haveCnt, missCnt, sentReady);
			peerHas.Clear();
			return last;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void CheckNotAdvertisedWants(ICollection<RevObject> notAdvertisedWants)
		{
			// Walk the requested commits back to the advertised commits.
			// If any commit exists, a branch was deleted or rewound and
			// the repository owner no longer exports that requested item.
			// If the requested commit is merged into an advertised branch
			// it will be marked UNINTERESTING and no commits return.
			foreach (RevObject o in notAdvertisedWants)
			{
				if (!(o is RevCommit))
				{
					throw new PackProtocolException(MessageFormat.Format(JGitText.Get().wantNotValid, 
						notAdvertisedWants.Iterator().Next().Name));
				}
				walk.MarkStart((RevCommit)o);
			}
			foreach (ObjectId id in advertised)
			{
				try
				{
					walk.MarkUninteresting(walk.ParseCommit(id));
				}
				catch (IncorrectObjectTypeException)
				{
					continue;
				}
			}
			RevCommit bad = walk.Next();
			if (bad != null)
			{
				throw new PackProtocolException(MessageFormat.Format(JGitText.Get().wantNotValid, 
					bad.Name));
			}
			walk.Reset();
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
			if (!biDirectionalPipe)
			{
				// Ensure the request was fully consumed. Any remaining input must
				// be a protocol error. If we aren't at EOF the implementation is broken.
				int eof = rawIn.Read();
				if (0 <= eof)
				{
					throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().expectedEOFReceived
						, "\\x" + Sharpen.Extensions.ToHexString(eof)));
				}
			}
			if (sideband)
			{
				try
				{
					SendPack(true);
				}
				catch (ServiceMayNotContinueException noPack)
				{
					// This was already reported on (below).
					throw;
				}
				catch (IOException err)
				{
					if (ReportInternalServerErrorOverSideband())
					{
						throw new UploadPackInternalServerErrorException(err);
					}
					else
					{
						throw;
					}
				}
				catch (RuntimeException err)
				{
					if (ReportInternalServerErrorOverSideband())
					{
						throw new UploadPackInternalServerErrorException(err);
					}
					else
					{
						throw;
					}
				}
				catch (Error err)
				{
					if (ReportInternalServerErrorOverSideband())
					{
						throw new UploadPackInternalServerErrorException(err);
					}
					else
					{
						throw;
					}
				}
			}
			else
			{
				SendPack(false);
			}
		}

		private bool ReportInternalServerErrorOverSideband()
		{
			try
			{
				SideBandOutputStream err = new SideBandOutputStream(SideBandOutputStream.CH_ERROR
					, SideBandOutputStream.SMALL_BUF, rawOut);
				err.Write(Constants.Encode(JGitText.Get().internalServerError));
				err.Flush();
				return true;
			}
			catch
			{
				// Ignore the reason. This is a secondary failure.
				return false;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SendPack(bool sideband)
		{
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
			try
			{
				if (wantAll.IsEmpty())
				{
					preUploadHook.OnSendPack(this, wantIds, commonBase);
				}
				else
				{
					preUploadHook.OnSendPack(this, wantAll, commonBase);
				}
			}
			catch (ServiceMayNotContinueException noPack)
			{
				if (sideband && noPack.Message != null)
				{
					noPack.SetOutput();
					SideBandOutputStream err = new SideBandOutputStream(SideBandOutputStream.CH_ERROR
						, SideBandOutputStream.SMALL_BUF, rawOut);
					err.Write(Constants.Encode(noPack.Message));
					err.Flush();
				}
				throw;
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
				if (commonBase.IsEmpty() && refs != null)
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
				if (depth > 0)
				{
					pw.SetShallowPack(depth, unshallowCommits);
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
				if (options.Contains(OPTION_INCLUDE_TAG) && refs != null)
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
			if (statistics != null)
			{
				logger.OnPackStatistics(statistics);
			}
		}
	}
}
