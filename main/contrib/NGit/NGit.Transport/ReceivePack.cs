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
using NGit.Storage.File;
using NGit.Transport;
using NGit.Util.IO;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Implements the server side of a push connection, receiving objects.</summary>
	/// <remarks>Implements the server side of a push connection, receiving objects.</remarks>
	public class ReceivePack
	{
		/// <summary>Database we write the stored objects into.</summary>
		/// <remarks>Database we write the stored objects into.</remarks>
		private readonly Repository db;

		/// <summary>
		/// Revision traversal support over
		/// <see cref="db">db</see>
		/// .
		/// </summary>
		private readonly RevWalk walk;

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

		/// <summary>Should an incoming transfer validate objects?</summary>
		private bool checkReceivedObjects;

		/// <summary>Should an incoming transfer permit create requests?</summary>
		private bool allowCreates;

		/// <summary>Should an incoming transfer permit delete requests?</summary>
		private bool allowDeletes;

		/// <summary>Should an incoming transfer permit non-fast-forward requests?</summary>
		private bool allowNonFastForwards;

		private bool allowOfsDelta;

		/// <summary>Identity to record action as within the reflog.</summary>
		/// <remarks>Identity to record action as within the reflog.</remarks>
		private PersonIdent refLogIdent;

		/// <summary>Filter used while advertising the refs to the client.</summary>
		/// <remarks>Filter used while advertising the refs to the client.</remarks>
		private RefFilter refFilter;

		/// <summary>Hook to validate the update commands before execution.</summary>
		/// <remarks>Hook to validate the update commands before execution.</remarks>
		private PreReceiveHook preReceive;

		/// <summary>Hook to report on the commands after execution.</summary>
		/// <remarks>Hook to report on the commands after execution.</remarks>
		private PostReceiveHook postReceive;

		/// <summary>Timeout in seconds to wait for client interaction.</summary>
		/// <remarks>Timeout in seconds to wait for client interaction.</remarks>
		private int timeout;

		/// <summary>
		/// Timer to manage
		/// <see cref="timeout">timeout</see>
		/// .
		/// </summary>
		private InterruptTimer timer;

		private TimeoutInputStream timeoutIn;

		private InputStream rawIn;

		private OutputStream rawOut;

		private OutputStream msgOut;

		private PacketLineIn pckIn;

		private PacketLineOut pckOut;

		private PackParser parser;

		/// <summary>The refs we advertised as existing at the start of the connection.</summary>
		/// <remarks>The refs we advertised as existing at the start of the connection.</remarks>
		private IDictionary<string, Ref> refs;

		/// <summary>All SHA-1s shown to the client, which can be possible edges.</summary>
		/// <remarks>All SHA-1s shown to the client, which can be possible edges.</remarks>
		private ICollection<ObjectId> advertisedHaves;

		/// <summary>Capabilities requested by the client.</summary>
		/// <remarks>Capabilities requested by the client.</remarks>
		private ICollection<string> enabledCapablities;

		/// <summary>Commands to execute, as received by the client.</summary>
		/// <remarks>Commands to execute, as received by the client.</remarks>
		private IList<ReceiveCommand> commands;

		/// <summary>Error to display instead of advertising the references.</summary>
		/// <remarks>Error to display instead of advertising the references.</remarks>
		private StringBuilder advertiseError;

		/// <summary>An exception caught while unpacking and fsck'ing the objects.</summary>
		/// <remarks>An exception caught while unpacking and fsck'ing the objects.</remarks>
		private Exception unpackError;

		/// <summary>
		/// If
		/// <see cref="BasePackPushConnection.CAPABILITY_REPORT_STATUS">BasePackPushConnection.CAPABILITY_REPORT_STATUS
		/// 	</see>
		/// is enabled.
		/// </summary>
		private bool reportStatus;

		/// <summary>
		/// If
		/// <see cref="BasePackPushConnection.CAPABILITY_SIDE_BAND_64K">BasePackPushConnection.CAPABILITY_SIDE_BAND_64K
		/// 	</see>
		/// is enabled.
		/// </summary>
		private bool sideBand;

		/// <summary>Lock around the received pack file, while updating refs.</summary>
		/// <remarks>Lock around the received pack file, while updating refs.</remarks>
		private PackLock packLock;

		private bool checkReferencedIsReachable;

		/// <summary>Git object size limit</summary>
		private long maxObjectSizeLimit;

		/// <summary>Create a new pack receive for an open repository.</summary>
		/// <remarks>Create a new pack receive for an open repository.</remarks>
		/// <param name="into">the destination repository.</param>
		public ReceivePack(Repository into)
		{
			db = into;
			walk = new RevWalk(db);
			ReceivePack.ReceiveConfig cfg = db.GetConfig().Get(ReceivePack.ReceiveConfig.KEY);
			checkReceivedObjects = cfg.checkReceivedObjects;
			allowCreates = cfg.allowCreates;
			allowDeletes = cfg.allowDeletes;
			allowNonFastForwards = cfg.allowNonFastForwards;
			allowOfsDelta = cfg.allowOfsDelta;
			refFilter = RefFilter.DEFAULT;
			preReceive = PreReceiveHook.NULL;
			postReceive = PostReceiveHook.NULL;
			advertisedHaves = new HashSet<ObjectId>();
		}

		private class ReceiveConfig
		{
			private sealed class _SectionParser_222 : Config.SectionParser<ReceivePack.ReceiveConfig
				>
			{
				public _SectionParser_222()
				{
				}

				public ReceivePack.ReceiveConfig Parse(Config cfg)
				{
					return new ReceivePack.ReceiveConfig(cfg);
				}
			}

			internal static readonly Config.SectionParser<ReceivePack.ReceiveConfig> KEY = new 
				_SectionParser_222();

			internal readonly bool checkReceivedObjects;

			internal readonly bool allowCreates;

			internal readonly bool allowDeletes;

			internal readonly bool allowNonFastForwards;

			internal readonly bool allowOfsDelta;

			internal ReceiveConfig(Config config)
			{
				checkReceivedObjects = config.GetBoolean("receive", "fsckobjects", false);
				allowCreates = true;
				allowDeletes = !config.GetBoolean("receive", "denydeletes", false);
				allowNonFastForwards = !config.GetBoolean("receive", "denynonfastforwards", false
					);
				allowOfsDelta = config.GetBoolean("repack", "usedeltabaseoffset", true);
			}
		}

		/// <returns>the repository this receive completes into.</returns>
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
			if (refs == null)
			{
				refs = refFilter.Filter(db.GetAllRefs());
				Ref head = refs.Get(Constants.HEAD);
				if (head != null && head.IsSymbolic())
				{
					Sharpen.Collections.Remove(refs, Constants.HEAD);
				}
				foreach (Ref @ref in refs.Values)
				{
					if (@ref.GetObjectId() != null)
					{
						advertisedHaves.AddItem(@ref.GetObjectId());
					}
				}
				Sharpen.Collections.AddAll(advertisedHaves, db.GetAdditionalHaves());
			}
			return refs;
		}

		/// <returns>the set of objects advertised as present in this repository.</returns>
		public ICollection<ObjectId> GetAdvertisedObjects()
		{
			GetAdvertisedRefs();
			return advertisedHaves;
		}

		/// <returns>
		/// true if this instance will validate all referenced, but not
		/// supplied by the client, objects are reachable from another
		/// reference.
		/// </returns>
		public virtual bool IsCheckReferencedObjectsAreReachable()
		{
			return checkReferencedIsReachable;
		}

		/// <summary>Validate all referenced but not supplied objects are reachable.</summary>
		/// <remarks>
		/// Validate all referenced but not supplied objects are reachable.
		/// <p>
		/// If enabled, this instance will verify that references to objects not
		/// contained within the received pack are already reachable through at least
		/// one other reference selected by the
		/// <see cref="GetRefFilter()">GetRefFilter()</see>
		/// and displayed
		/// as part of
		/// <see cref="GetAdvertisedRefs()">GetAdvertisedRefs()</see>
		/// .
		/// <p>
		/// This feature is useful when the application doesn't trust the client to
		/// not provide a forged SHA-1 reference to an object, in an attempt to
		/// access parts of the DAG that they aren't allowed to see and which have
		/// been hidden from them via the configured
		/// <see cref="RefFilter">RefFilter</see>
		/// .
		/// <p>
		/// Enabling this feature may imply at least some, if not all, of the same
		/// functionality performed by
		/// <see cref="SetCheckReceivedObjects(bool)">SetCheckReceivedObjects(bool)</see>
		/// .
		/// Applications are encouraged to enable both features, if desired.
		/// </remarks>
		/// <param name="b">
		/// <code>true</code>
		/// to enable the additional check.
		/// </param>
		public virtual void SetCheckReferencedObjectsAreReachable(bool b)
		{
			this.checkReferencedIsReachable = b;
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

		/// <returns>
		/// true if this instance will verify received objects are formatted
		/// correctly. Validating objects requires more CPU time on this side
		/// of the connection.
		/// </returns>
		public virtual bool IsCheckReceivedObjects()
		{
			return checkReceivedObjects;
		}

		/// <param name="check">
		/// true to enable checking received objects; false to assume all
		/// received objects are valid.
		/// </param>
		public virtual void SetCheckReceivedObjects(bool check)
		{
			checkReceivedObjects = check;
		}

		/// <returns>true if the client can request refs to be created.</returns>
		public virtual bool IsAllowCreates()
		{
			return allowCreates;
		}

		/// <param name="canCreate">true to permit create ref commands to be processed.</param>
		public virtual void SetAllowCreates(bool canCreate)
		{
			allowCreates = canCreate;
		}

		/// <returns>true if the client can request refs to be deleted.</returns>
		public virtual bool IsAllowDeletes()
		{
			return allowDeletes;
		}

		/// <param name="canDelete">true to permit delete ref commands to be processed.</param>
		public virtual void SetAllowDeletes(bool canDelete)
		{
			allowDeletes = canDelete;
		}

		/// <returns>
		/// true if the client can request non-fast-forward updates of a ref,
		/// possibly making objects unreachable.
		/// </returns>
		public virtual bool IsAllowNonFastForwards()
		{
			return allowNonFastForwards;
		}

		/// <param name="canRewind">
		/// true to permit the client to ask for non-fast-forward updates
		/// of an existing ref.
		/// </param>
		public virtual void SetAllowNonFastForwards(bool canRewind)
		{
			allowNonFastForwards = canRewind;
		}

		/// <returns>identity of the user making the changes in the reflog.</returns>
		public virtual PersonIdent GetRefLogIdent()
		{
			return refLogIdent;
		}

		/// <summary>Set the identity of the user appearing in the affected reflogs.</summary>
		/// <remarks>
		/// Set the identity of the user appearing in the affected reflogs.
		/// <p>
		/// The timestamp portion of the identity is ignored. A new identity with the
		/// current timestamp will be created automatically when the updates occur
		/// and the log records are written.
		/// </remarks>
		/// <param name="pi">
		/// identity of the user. If null the identity will be
		/// automatically determined based on the repository
		/// configuration.
		/// </param>
		public virtual void SetRefLogIdent(PersonIdent pi)
		{
			refLogIdent = pi;
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
		/// Only refs allowed by this filter will be shown to the client.
		/// Clients may still attempt to create or update a reference hidden
		/// by the configured
		/// <see cref="RefFilter">RefFilter</see>
		/// . These attempts should be
		/// rejected by a matching
		/// <see cref="PreReceiveHook">PreReceiveHook</see>
		/// .
		/// </remarks>
		/// <param name="refFilter">the filter; may be null to show all refs.</param>
		public virtual void SetRefFilter(RefFilter refFilter)
		{
			this.refFilter = refFilter != null ? refFilter : RefFilter.DEFAULT;
		}

		/// <returns>get the hook invoked before updates occur.</returns>
		public virtual PreReceiveHook GetPreReceiveHook()
		{
			return preReceive;
		}

		/// <summary>Set the hook which is invoked prior to commands being executed.</summary>
		/// <remarks>
		/// Set the hook which is invoked prior to commands being executed.
		/// <p>
		/// Only valid commands (those which have no obvious errors according to the
		/// received input and this instance's configuration) are passed into the
		/// hook. The hook may mark a command with a result of any value other than
		/// <see cref="Result.NOT_ATTEMPTED">Result.NOT_ATTEMPTED</see>
		/// to block its execution.
		/// <p>
		/// The hook may be called with an empty command collection if the current
		/// set is completely invalid.
		/// </remarks>
		/// <param name="h">the hook instance; may be null to disable the hook.</param>
		public virtual void SetPreReceiveHook(PreReceiveHook h)
		{
			preReceive = h != null ? h : PreReceiveHook.NULL;
		}

		/// <returns>get the hook invoked after updates occur.</returns>
		public virtual PostReceiveHook GetPostReceiveHook()
		{
			return postReceive;
		}

		/// <summary>Set the hook which is invoked after commands are executed.</summary>
		/// <remarks>
		/// Set the hook which is invoked after commands are executed.
		/// <p>
		/// Only successful commands (type is
		/// <see cref="Result.OK">Result.OK</see>
		/// ) are passed into the
		/// hook. The hook may be called with an empty command collection if the
		/// current set all resulted in an error.
		/// </remarks>
		/// <param name="h">the hook instance; may be null to disable the hook.</param>
		public virtual void SetPostReceiveHook(PostReceiveHook h)
		{
			postReceive = h != null ? h : PostReceiveHook.NULL;
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

		/// <summary>Set the maximum allowed Git object size.</summary>
		/// <remarks>
		/// Set the maximum allowed Git object size.
		/// <p>
		/// If an object is larger than the given size the pack-parsing will throw an
		/// exception aborting the receive-pack operation.
		/// </remarks>
		/// <param name="limit">the Git object size limit. If zero then there is not limit.</param>
		public virtual void SetMaxObjectSizeLimit(long limit)
		{
			maxObjectSizeLimit = limit;
		}

		/// <returns>all of the command received by the current request.</returns>
		public virtual IList<ReceiveCommand> GetAllCommands()
		{
			return Sharpen.Collections.UnmodifiableList(commands);
		}

		/// <summary>Send an error message to the client.</summary>
		/// <remarks>
		/// Send an error message to the client.
		/// <p>
		/// If any error messages are sent before the references are advertised to
		/// the client, the errors will be sent instead of the advertisement and the
		/// receive operation will be aborted. All clients should receive and display
		/// such early stage errors.
		/// <p>
		/// If the reference advertisements have already been sent, messages are sent
		/// in a side channel. If the client doesn't support receiving messages, the
		/// message will be discarded, with no other indication to the caller or to
		/// the client.
		/// <p>
		/// <see cref="PreReceiveHook">PreReceiveHook</see>
		/// s should always try to use
		/// <see cref="ReceiveCommand.SetResult(Result, string)">ReceiveCommand.SetResult(Result, string)
		/// 	</see>
		/// with a result status of
		/// <see cref="Result.REJECTED_OTHER_REASON">Result.REJECTED_OTHER_REASON</see>
		/// to indicate any reasons for
		/// rejecting an update. Messages attached to a command are much more likely
		/// to be returned to the client.
		/// </remarks>
		/// <param name="what">
		/// string describing the problem identified by the hook. The
		/// string must not end with an LF, and must not contain an LF.
		/// </param>
		public virtual void SendError(string what)
		{
			if (refs == null)
			{
				if (advertiseError == null)
				{
					advertiseError = new StringBuilder();
				}
				advertiseError.Append(what).Append('\n');
			}
			else
			{
				try
				{
					if (msgOut != null)
					{
						msgOut.Write(Constants.Encode("error: " + what + "\n"));
					}
				}
				catch (IOException)
				{
				}
			}
		}

		// Ignore write failures.
		/// <summary>Send a message to the client, if it supports receiving them.</summary>
		/// <remarks>
		/// Send a message to the client, if it supports receiving them.
		/// <p>
		/// If the client doesn't support receiving messages, the message will be
		/// discarded, with no other indication to the caller or to the client.
		/// </remarks>
		/// <param name="what">
		/// string describing the problem identified by the hook. The
		/// string must not end with an LF, and must not contain an LF.
		/// </param>
		public virtual void SendMessage(string what)
		{
			try
			{
				if (msgOut != null)
				{
					msgOut.Write(Constants.Encode(what + "\n"));
				}
			}
			catch (IOException)
			{
			}
		}

		// Ignore write failures.
		/// <summary>Execute the receive task on the socket.</summary>
		/// <remarks>Execute the receive task on the socket.</remarks>
		/// <param name="input">
		/// raw input to read client commands and pack data from. Caller
		/// must ensure the input is buffered, otherwise read performance
		/// may suffer.
		/// </param>
		/// <param name="output">
		/// response back to the Git network client. Caller must ensure
		/// the output is buffered, otherwise write performance may
		/// suffer.
		/// </param>
		/// <param name="messages">
		/// secondary "notice" channel to send additional messages out
		/// through. When run over SSH this should be tied back to the
		/// standard error channel of the command execution. For most
		/// other network connections this should be null.
		/// </param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void Receive(InputStream input, OutputStream output, OutputStream 
			messages)
		{
			try
			{
				rawIn = input;
				rawOut = output;
				msgOut = messages;
				if (timeout > 0)
				{
					Sharpen.Thread caller = Sharpen.Thread.CurrentThread();
					timer = new InterruptTimer(caller.GetName() + "-Timer");
					timeoutIn = new TimeoutInputStream(rawIn, timer);
					TimeoutOutputStream o = new TimeoutOutputStream(rawOut, timer);
					timeoutIn.SetTimeout(timeout * 1000);
					o.SetTimeout(timeout * 1000);
					rawIn = timeoutIn;
					rawOut = o;
				}
				pckIn = new PacketLineIn(rawIn);
				pckOut = new PacketLineOut(rawOut);
				pckOut.SetFlushOnEnd(false);
				enabledCapablities = new HashSet<string>();
				commands = new AList<ReceiveCommand>();
				Service();
			}
			finally
			{
				walk.Release();
				try
				{
					if (sideBand)
					{
						// If we are using side band, we need to send a final
						// flush-pkt to tell the remote peer the side band is
						// complete and it should stop decoding. We need to
						// use the original output stream as rawOut is now the
						// side band data channel.
						//
						((SideBandOutputStream)msgOut).FlushBuffer();
						((SideBandOutputStream)rawOut).FlushBuffer();
						PacketLineOut plo = new PacketLineOut(output);
						plo.SetFlushOnEnd(false);
						plo.End();
					}
					if (biDirectionalPipe)
					{
						// If this was a native git connection, flush the pipe for
						// the caller. For smart HTTP we don't do this flush and
						// instead let the higher level HTTP servlet code do it.
						//
						if (!sideBand && msgOut != null)
						{
							msgOut.Flush();
						}
						rawOut.Flush();
					}
				}
				finally
				{
					UnlockPack();
					timeoutIn = null;
					rawIn = null;
					rawOut = null;
					msgOut = null;
					pckIn = null;
					pckOut = null;
					refs = null;
					enabledCapablities = null;
					commands = null;
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
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Service()
		{
			if (biDirectionalPipe)
			{
				SendAdvertisedRefs(new RefAdvertiser.PacketLineOutRefAdvertiser(pckOut));
				pckOut.Flush();
			}
			else
			{
				GetAdvertisedRefs();
			}
			if (advertiseError != null)
			{
				return;
			}
			RecvCommands();
			if (!commands.IsEmpty())
			{
				EnableCapabilities();
				if (NeedPack())
				{
					try
					{
						DoReceivePack();
						if (NeedCheckConnectivity())
						{
							CheckConnectivity();
						}
						parser = null;
						unpackError = null;
					}
					catch (IOException err)
					{
						unpackError = err;
					}
					catch (RuntimeException err)
					{
						unpackError = err;
					}
					catch (Error err)
					{
						unpackError = err;
					}
				}
				if (unpackError == null)
				{
					ValidateCommands();
					ExecuteCommands();
				}
				UnlockPack();
				if (reportStatus)
				{
					SendStatusReport(true, new _Reporter_702(this));
					pckOut.End();
				}
				else
				{
					if (msgOut != null)
					{
						SendStatusReport(false, new _Reporter_709(this));
					}
				}
				postReceive.OnPostReceive(this, FilterCommands(ReceiveCommand.Result.OK));
				if (unpackError != null)
				{
					throw new UnpackException(unpackError);
				}
			}
		}

		private sealed class _Reporter_702 : ReceivePack.Reporter
		{
			public _Reporter_702(ReceivePack _enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override void SendString(string s)
			{
				this._enclosing.pckOut.WriteString(s + "\n");
			}

			private readonly ReceivePack _enclosing;
		}

		private sealed class _Reporter_709 : ReceivePack.Reporter
		{
			public _Reporter_709(ReceivePack _enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override void SendString(string s)
			{
				this._enclosing.msgOut.Write(Constants.Encode(s + "\n"));
			}

			private readonly ReceivePack _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void UnlockPack()
		{
			if (packLock != null)
			{
				packLock.Unlock();
				packLock = null;
			}
		}

		/// <summary>Generate an advertisement of available refs and capabilities.</summary>
		/// <remarks>Generate an advertisement of available refs and capabilities.</remarks>
		/// <param name="adv">the advertisement formatter.</param>
		/// <exception cref="System.IO.IOException">the formatter failed to write an advertisement.
		/// 	</exception>
		public virtual void SendAdvertisedRefs(RefAdvertiser adv)
		{
			if (advertiseError != null)
			{
				adv.WriteOne("ERR " + advertiseError);
				return;
			}
			adv.Init(db);
			adv.AdvertiseCapability(BasePackPushConnection.CAPABILITY_SIDE_BAND_64K);
			adv.AdvertiseCapability(BasePackPushConnection.CAPABILITY_DELETE_REFS);
			adv.AdvertiseCapability(BasePackPushConnection.CAPABILITY_REPORT_STATUS);
			if (allowOfsDelta)
			{
				adv.AdvertiseCapability(BasePackPushConnection.CAPABILITY_OFS_DELTA);
			}
			adv.Send(GetAdvertisedRefs());
			foreach (ObjectId obj in advertisedHaves)
			{
				adv.AdvertiseHave(obj);
			}
			if (adv.IsEmpty())
			{
				adv.AdvertiseId(ObjectId.ZeroId, "capabilities^{}");
			}
			adv.End();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void RecvCommands()
		{
			for (; ; )
			{
				string line;
				try
				{
					line = pckIn.ReadStringRaw();
				}
				catch (EOFException eof)
				{
					if (commands.IsEmpty())
					{
						return;
					}
					throw;
				}
				if (line == PacketLineIn.END)
				{
					break;
				}
				if (commands.IsEmpty())
				{
					int nul = line.IndexOf('\0');
					if (nul >= 0)
					{
						foreach (string c in Sharpen.Runtime.Substring(line, nul + 1).Split(" "))
						{
							enabledCapablities.AddItem(c);
						}
						line = Sharpen.Runtime.Substring(line, 0, nul);
					}
				}
				if (line.Length < 83)
				{
					string m = JGitText.Get().errorInvalidProtocolWantedOldNewRef;
					SendError(m);
					throw new PackProtocolException(m);
				}
				ObjectId oldId = ObjectId.FromString(Sharpen.Runtime.Substring(line, 0, 40));
				ObjectId newId = ObjectId.FromString(Sharpen.Runtime.Substring(line, 41, 81));
				string name = Sharpen.Runtime.Substring(line, 82);
				ReceiveCommand cmd = new ReceiveCommand(oldId, newId, name);
				if (name.Equals(Constants.HEAD))
				{
					cmd.SetResult(ReceiveCommand.Result.REJECTED_CURRENT_BRANCH);
				}
				else
				{
					cmd.SetRef(refs.Get(cmd.GetRefName()));
				}
				commands.AddItem(cmd);
			}
		}

		private void EnableCapabilities()
		{
			reportStatus = enabledCapablities.Contains(BasePackPushConnection.CAPABILITY_REPORT_STATUS
				);
			sideBand = enabledCapablities.Contains(BasePackPushConnection.CAPABILITY_SIDE_BAND_64K
				);
			if (sideBand)
			{
				OutputStream @out = rawOut;
				rawOut = new SideBandOutputStream(SideBandOutputStream.CH_DATA, SideBandOutputStream
					.MAX_BUF, @out);
				msgOut = new SideBandOutputStream(SideBandOutputStream.CH_PROGRESS, SideBandOutputStream
					.MAX_BUF, @out);
				pckOut = new PacketLineOut(rawOut);
				pckOut.SetFlushOnEnd(false);
			}
		}

		private bool NeedPack()
		{
			foreach (ReceiveCommand cmd in commands)
			{
				if (cmd.GetType() != ReceiveCommand.Type.DELETE)
				{
					return true;
				}
			}
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void DoReceivePack()
		{
			// It might take the client a while to pack the objects it needs
			// to send to us.  We should increase our timeout so we don't
			// abort while the client is computing.
			//
			if (timeoutIn != null)
			{
				timeoutIn.SetTimeout(10 * timeout * 1000);
			}
			ProgressMonitor receiving = NullProgressMonitor.INSTANCE;
			ProgressMonitor resolving = NullProgressMonitor.INSTANCE;
			if (sideBand)
			{
				resolving = new SideBandProgressMonitor(msgOut);
			}
			ObjectInserter ins = db.NewObjectInserter();
			try
			{
				string lockMsg = "jgit receive-pack";
				if (GetRefLogIdent() != null)
				{
					lockMsg += " from " + GetRefLogIdent().ToExternalString();
				}
				parser = ins.NewPackParser(rawIn);
				parser.SetAllowThin(true);
				parser.SetNeedNewObjectIds(checkReferencedIsReachable);
				parser.SetNeedBaseObjectIds(checkReferencedIsReachable);
				parser.SetCheckEofAfterPackFooter(!biDirectionalPipe);
				parser.SetObjectChecking(IsCheckReceivedObjects());
				parser.SetLockMessage(lockMsg);
				parser.SetMaxObjectSizeLimit(maxObjectSizeLimit);
				packLock = parser.Parse(receiving, resolving);
				ins.Flush();
			}
			finally
			{
				ins.Release();
			}
			if (timeoutIn != null)
			{
				timeoutIn.SetTimeout(timeout * 1000);
			}
		}

		private bool NeedCheckConnectivity()
		{
			return IsCheckReceivedObjects() || IsCheckReferencedObjectsAreReachable();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckConnectivity()
		{
			ObjectIdSubclassMap<ObjectId> baseObjects = null;
			ObjectIdSubclassMap<ObjectId> providedObjects = null;
			if (checkReferencedIsReachable)
			{
				baseObjects = parser.GetBaseObjectIds();
				providedObjects = parser.GetNewObjectIds();
			}
			parser = null;
			ObjectWalk ow = new ObjectWalk(db);
			ow.SetRetainBody(false);
			if (checkReferencedIsReachable)
			{
				ow.Sort(RevSort.TOPO);
				if (!baseObjects.IsEmpty())
				{
					ow.Sort(RevSort.BOUNDARY, true);
				}
			}
			foreach (ReceiveCommand cmd in commands)
			{
				if (cmd.GetResult() != ReceiveCommand.Result.NOT_ATTEMPTED)
				{
					continue;
				}
				if (cmd.GetType() == ReceiveCommand.Type.DELETE)
				{
					continue;
				}
				ow.MarkStart(ow.ParseAny(cmd.GetNewId()));
			}
			foreach (ObjectId have in advertisedHaves)
			{
				RevObject o = ow.ParseAny(have);
				ow.MarkUninteresting(o);
				if (checkReferencedIsReachable && !baseObjects.IsEmpty())
				{
					o = ow.Peel(o);
					if (o is RevCommit)
					{
						o = ((RevCommit)o).Tree;
					}
					if (o is RevTree)
					{
						ow.MarkUninteresting(o);
					}
				}
			}
			RevCommit c;
			while ((c = ow.Next()) != null)
			{
				if (checkReferencedIsReachable && !c.Has(RevFlag.UNINTERESTING) && !providedObjects
					.Contains(c))
				{
					//
					//
					throw new MissingObjectException(c, Constants.TYPE_COMMIT);
				}
			}
			RevObject o_1;
			while ((o_1 = ow.NextObject()) != null)
			{
				if (o_1.Has(RevFlag.UNINTERESTING))
				{
					continue;
				}
				if (checkReferencedIsReachable)
				{
					if (providedObjects.Contains(o_1))
					{
						continue;
					}
					else
					{
						throw new MissingObjectException(o_1, o_1.Type);
					}
				}
				if (o_1 is RevBlob && !db.HasObject(o_1))
				{
					throw new MissingObjectException(o_1, Constants.TYPE_BLOB);
				}
			}
			if (checkReferencedIsReachable)
			{
				foreach (ObjectId id in baseObjects)
				{
					o_1 = ow.ParseAny(id);
					if (!o_1.Has(RevFlag.UNINTERESTING))
					{
						throw new MissingObjectException(o_1, o_1.Type);
					}
				}
			}
		}

		private void ValidateCommands()
		{
			foreach (ReceiveCommand cmd in commands)
			{
				Ref @ref = cmd.GetRef();
				if (cmd.GetResult() != ReceiveCommand.Result.NOT_ATTEMPTED)
				{
					continue;
				}
				if (cmd.GetType() == ReceiveCommand.Type.DELETE && !IsAllowDeletes())
				{
					// Deletes are not supported on this repository.
					//
					cmd.SetResult(ReceiveCommand.Result.REJECTED_NODELETE);
					continue;
				}
				if (cmd.GetType() == ReceiveCommand.Type.CREATE)
				{
					if (!IsAllowCreates())
					{
						cmd.SetResult(ReceiveCommand.Result.REJECTED_NOCREATE);
						continue;
					}
					if (@ref != null && !IsAllowNonFastForwards())
					{
						// Creation over an existing ref is certainly not going
						// to be a fast-forward update. We can reject it early.
						//
						cmd.SetResult(ReceiveCommand.Result.REJECTED_NONFASTFORWARD);
						continue;
					}
					if (@ref != null)
					{
						// A well behaved client shouldn't have sent us a
						// create command for a ref we advertised to it.
						//
						cmd.SetResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, MessageFormat.Format(JGitText
							.Get().refAlreadyExists, @ref));
						continue;
					}
				}
				if (cmd.GetType() == ReceiveCommand.Type.DELETE && @ref != null && !ObjectId.ZeroId
					.Equals(cmd.GetOldId()) && !@ref.GetObjectId().Equals(cmd.GetOldId()))
				{
					// Delete commands can be sent with the old id matching our
					// advertised value, *OR* with the old id being 0{40}. Any
					// other requested old id is invalid.
					//
					cmd.SetResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, JGitText.Get().invalidOldIdSent
						);
					continue;
				}
				if (cmd.GetType() == ReceiveCommand.Type.UPDATE)
				{
					if (@ref == null)
					{
						// The ref must have been advertised in order to be updated.
						//
						cmd.SetResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, JGitText.Get().noSuchRef
							);
						continue;
					}
					if (!@ref.GetObjectId().Equals(cmd.GetOldId()))
					{
						// A properly functioning client will send the same
						// object id we advertised.
						//
						cmd.SetResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, JGitText.Get().invalidOldIdSent
							);
						continue;
					}
					// Is this possibly a non-fast-forward style update?
					//
					RevObject oldObj;
					RevObject newObj;
					try
					{
						oldObj = walk.ParseAny(cmd.GetOldId());
					}
					catch (IOException)
					{
						cmd.SetResult(ReceiveCommand.Result.REJECTED_MISSING_OBJECT, cmd.GetOldId().Name);
						continue;
					}
					try
					{
						newObj = walk.ParseAny(cmd.GetNewId());
					}
					catch (IOException)
					{
						cmd.SetResult(ReceiveCommand.Result.REJECTED_MISSING_OBJECT, cmd.GetNewId().Name);
						continue;
					}
					if (oldObj is RevCommit && newObj is RevCommit)
					{
						try
						{
							if (!walk.IsMergedInto((RevCommit)oldObj, (RevCommit)newObj))
							{
								cmd.SetType(ReceiveCommand.Type.UPDATE_NONFASTFORWARD);
							}
						}
						catch (MissingObjectException e)
						{
							cmd.SetResult(ReceiveCommand.Result.REJECTED_MISSING_OBJECT, e.Message);
						}
						catch (IOException)
						{
							cmd.SetResult(ReceiveCommand.Result.REJECTED_OTHER_REASON);
						}
					}
					else
					{
						cmd.SetType(ReceiveCommand.Type.UPDATE_NONFASTFORWARD);
					}
				}
				if (!cmd.GetRefName().StartsWith(Constants.R_REFS) || !Repository.IsValidRefName(
					cmd.GetRefName()))
				{
					cmd.SetResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, JGitText.Get().funnyRefname
						);
				}
			}
		}

		private void ExecuteCommands()
		{
			preReceive.OnPreReceive(this, FilterCommands(ReceiveCommand.Result.NOT_ATTEMPTED)
				);
			IList<ReceiveCommand> toApply = FilterCommands(ReceiveCommand.Result.NOT_ATTEMPTED
				);
			ProgressMonitor updating = NullProgressMonitor.INSTANCE;
			if (sideBand)
			{
				SideBandProgressMonitor pm = new SideBandProgressMonitor(msgOut);
				pm.SetDelayStart(250, TimeUnit.MILLISECONDS);
				updating = pm;
			}
			updating.BeginTask(JGitText.Get().updatingReferences, toApply.Count);
			foreach (ReceiveCommand cmd in toApply)
			{
				updating.Update(1);
				Execute(cmd);
			}
			updating.EndTask();
		}

		private void Execute(ReceiveCommand cmd)
		{
			try
			{
				RefUpdate ru = db.UpdateRef(cmd.GetRefName());
				ru.SetRefLogIdent(GetRefLogIdent());
				switch (cmd.GetType())
				{
					case ReceiveCommand.Type.DELETE:
					{
						if (!ObjectId.ZeroId.Equals(cmd.GetOldId()))
						{
							// We can only do a CAS style delete if the client
							// didn't bork its delete request by sending the
							// wrong zero id rather than the advertised one.
							//
							ru.SetExpectedOldObjectId(cmd.GetOldId());
						}
						ru.SetForceUpdate(true);
						Status(cmd, ru.Delete(walk));
						break;
					}

					case ReceiveCommand.Type.CREATE:
					case ReceiveCommand.Type.UPDATE:
					case ReceiveCommand.Type.UPDATE_NONFASTFORWARD:
					{
						ru.SetForceUpdate(IsAllowNonFastForwards());
						ru.SetExpectedOldObjectId(cmd.GetOldId());
						ru.SetNewObjectId(cmd.GetNewId());
						ru.SetRefLogMessage("push", true);
						Status(cmd, ru.Update(walk));
						break;
					}
				}
			}
			catch (IOException err)
			{
				cmd.SetResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, MessageFormat.Format(JGitText
					.Get().lockError, err.Message));
			}
		}

		private void Status(ReceiveCommand cmd, RefUpdate.Result result)
		{
			switch (result)
			{
				case RefUpdate.Result.NOT_ATTEMPTED:
				{
					cmd.SetResult(ReceiveCommand.Result.NOT_ATTEMPTED);
					break;
				}

				case RefUpdate.Result.LOCK_FAILURE:
				case RefUpdate.Result.IO_FAILURE:
				{
					cmd.SetResult(ReceiveCommand.Result.LOCK_FAILURE);
					break;
				}

				case RefUpdate.Result.NO_CHANGE:
				case RefUpdate.Result.NEW:
				case RefUpdate.Result.FORCED:
				case RefUpdate.Result.FAST_FORWARD:
				{
					cmd.SetResult(ReceiveCommand.Result.OK);
					break;
				}

				case RefUpdate.Result.REJECTED:
				{
					cmd.SetResult(ReceiveCommand.Result.REJECTED_NONFASTFORWARD);
					break;
				}

				case RefUpdate.Result.REJECTED_CURRENT_BRANCH:
				{
					cmd.SetResult(ReceiveCommand.Result.REJECTED_CURRENT_BRANCH);
					break;
				}

				default:
				{
					cmd.SetResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, result.ToString());
					break;
					break;
				}
			}
		}

		private IList<ReceiveCommand> FilterCommands(ReceiveCommand.Result want)
		{
			IList<ReceiveCommand> r = new AList<ReceiveCommand>(commands.Count);
			foreach (ReceiveCommand cmd in commands)
			{
				if (cmd.GetResult() == want)
				{
					r.AddItem(cmd);
				}
			}
			return r;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SendStatusReport(bool forClient, ReceivePack.Reporter @out)
		{
			if (unpackError != null)
			{
				@out.SendString("unpack error " + unpackError.Message);
				if (forClient)
				{
					foreach (ReceiveCommand cmd in commands)
					{
						@out.SendString("ng " + cmd.GetRefName() + " n/a (unpacker error)");
					}
				}
				return;
			}
			if (forClient)
			{
				@out.SendString("unpack ok");
			}
			foreach (ReceiveCommand cmd_1 in commands)
			{
				if (cmd_1.GetResult() == ReceiveCommand.Result.OK)
				{
					if (forClient)
					{
						@out.SendString("ok " + cmd_1.GetRefName());
					}
					continue;
				}
				StringBuilder r = new StringBuilder();
				r.Append("ng ");
				r.Append(cmd_1.GetRefName());
				r.Append(" ");
				switch (cmd_1.GetResult())
				{
					case ReceiveCommand.Result.NOT_ATTEMPTED:
					{
						r.Append("server bug; ref not processed");
						break;
					}

					case ReceiveCommand.Result.REJECTED_NOCREATE:
					{
						r.Append("creation prohibited");
						break;
					}

					case ReceiveCommand.Result.REJECTED_NODELETE:
					{
						r.Append("deletion prohibited");
						break;
					}

					case ReceiveCommand.Result.REJECTED_NONFASTFORWARD:
					{
						r.Append("non-fast forward");
						break;
					}

					case ReceiveCommand.Result.REJECTED_CURRENT_BRANCH:
					{
						r.Append("branch is currently checked out");
						break;
					}

					case ReceiveCommand.Result.REJECTED_MISSING_OBJECT:
					{
						if (cmd_1.GetMessage() == null)
						{
							r.Append("missing object(s)");
						}
						else
						{
							if (cmd_1.GetMessage().Length == Constants.OBJECT_ID_STRING_LENGTH)
							{
								r.Append("object " + cmd_1.GetMessage() + " missing");
							}
							else
							{
								r.Append(cmd_1.GetMessage());
							}
						}
						break;
					}

					case ReceiveCommand.Result.REJECTED_OTHER_REASON:
					{
						if (cmd_1.GetMessage() == null)
						{
							r.Append("unspecified reason");
						}
						else
						{
							r.Append(cmd_1.GetMessage());
						}
						break;
					}

					case ReceiveCommand.Result.LOCK_FAILURE:
					{
						r.Append("failed to lock");
						break;
					}

					case ReceiveCommand.Result.OK:
					{
						// We shouldn't have reached this case (see 'ok' case above).
						continue;
					}
				}
				@out.SendString(r.ToString());
			}
		}

		internal abstract class Reporter
		{
			/// <exception cref="System.IO.IOException"></exception>
			internal abstract void SendString(string s);
		}
	}
}
