/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
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
using System.Text;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{

	/// <summary>
	/// Implements the server side of a push connection, receiving objects.
	/// </summary>
	public class ReceivePack : IDisposable
	{

		/// <summary>
		/// Database we write the stored objects into.
		/// </summary>
		private readonly Repository db;

		/// <summary>
		/// Revision traversal support over <see cref="db"/>.
		/// </summary>
		private readonly RevWalk.RevWalk walk;

		/// <summary>
		/// Is the client connection a bi-directional socket or pipe?
		/// <para/>
		/// If true, this class assumes it can perform multiple read and write cycles
		/// with the client over the input and output streams. This matches the
		/// functionality available with a standard TCP/IP connection, or a local
		/// operating system or in-memory pipe.
		/// <para/>
		/// If false, this class runs in a read everything then output results mode,
		/// making it suitable for single round-trip systems RPCs such as HTTP.
		/// </summary>
		private bool biDirectionalPipe = true;

		/// <summary>
		/// Should an incoming transfer validate objects?
		/// </summary>
		private bool checkReceivedObjects;

		/// <summary>
		/// Should an incoming transfer permit create requests?
		/// </summary>
		private bool allowCreates;

		/// <summary>
		/// Should an incoming transfer permit delete requests?
		/// </summary>
		private bool allowDeletes;

		/// <summary>
		/// Should an incoming transfer permit non-fast-forward requests?
		/// </summary>
		private bool allowNonFastForwards;

		private bool allowOfsDelta;

		/// <summary>
		/// Identity to record action as within the reflog.
		/// </summary>
		private PersonIdent refLogIdent;

		/// <summary>
		/// Filter used while advertising the refs to the client.
		/// </summary>
		private RefFilter refFilter;

		/// <summary>
		/// Hook to validate the update commands before execution.
		/// </summary>
		private IPreReceiveHook preReceive;

		/// <summary>
		/// Hook to report on the commands after execution.
		/// </summary>
		private IPostReceiveHook postReceive;

		private Stream rawInput;
		private Stream rawOutput;

		private PacketLineIn pckIn;
		private PacketLineOut pckOut;
		private StreamWriter msgs;

		private IndexPack indexPack;

		/// <summary>
		/// The refs we advertised as existing at the start of the connection.
		/// </summary>
		private IDictionary<string, Ref> refs;

		/// <summary>
		/// Capabilities requested by the client.
		/// </summary>
		private List<string> enabledCapabilities;

		/// <summary>
		/// Commands to execute, as received by the client.
		/// </summary>
		private List<ReceiveCommand> commands;

		/// <summary>
		/// An exception caught while unpacking and fsck'ing the objects.
		/// </summary>
		private Exception unpackError;

		/// <summary>
		/// If <see cref="BasePackPushConnection.CAPABILITY_REPORT_STATUS"/> is enabled./>
		/// </summary>
		private bool reportStatus;

		/// <summary>
		/// If <see cref="BasePackPushConnection.CAPABILITY_SIDE_BAND_64K"/> is enabled./>
		/// </summary>
		private bool sideBand;

		/// <summary>
		/// Lock around the received pack file, while updating refs.
		/// </summary>
		private PackLock packLock;

		private bool needNewObjectIds;

		private bool needBaseObjectIds;

		/// <summary>
		/// Create a new pack receive for an open repository.
		/// </summary>
		/// <param name="into">the destination repository.</param>
		public ReceivePack(Repository into)
		{
			db = into;
			walk = new RevWalk.RevWalk(db);

			ReceiveConfig cfg = db.Config.get(ReceiveConfig.KEY);
			checkReceivedObjects = cfg._checkReceivedObjects;
			allowCreates = cfg._allowCreates;
			allowDeletes = cfg._allowDeletes;
			allowNonFastForwards = cfg._allowNonFastForwards;
			allowOfsDelta = cfg._allowOfsDelta;
			refFilter = RefFilterContants.DEFAULT;
			preReceive = PreReceiveHook.NULL;
			postReceive = PostReceiveHook.NULL;
		}

		private class ReceiveConfig
		{
			public static Config.SectionParser<ReceiveConfig> KEY = new SectionParser();

			private class SectionParser : Config.SectionParser<ReceiveConfig>
			{
				public ReceiveConfig parse(Config cfg)
				{
					return new ReceiveConfig(cfg);
				}
			}

			public readonly bool _checkReceivedObjects;
			public readonly bool _allowCreates;
			public readonly bool _allowDeletes;
			public readonly bool _allowNonFastForwards;
			public readonly bool _allowOfsDelta;

			ReceiveConfig(Config config)
			{
				_checkReceivedObjects = config.getBoolean("receive", "fsckobjects",
						  false);
				_allowCreates = true;
				_allowDeletes = !config.getBoolean("receive", "denydeletes", false);
				_allowNonFastForwards = !config.getBoolean("receive",
						  "denynonfastforwards", false);
				_allowOfsDelta = config.getBoolean("repack", "usedeltabaseoffset",
						  true);
			}
		}

		/// <summary>
		/// Returns the repository this receive completes into.
		/// </summary>
		/// <returns></returns>
		public Repository getRepository()
		{
			return db;
		}

		/// <summary>
		/// Returns the RevWalk instance used by this connection.
		/// </summary>
		/// <returns></returns>
		public RevWalk.RevWalk getRevWalk()
		{
			return walk;
		}

		/// <summary>
		/// Returns all refs which were advertised to the client.
		/// </summary>
		/// <returns></returns>
		public IDictionary<string, Ref> getAdvertisedRefs()
		{
			return refs;
		}

		/// <summary>
		/// Configure this receive pack instance to keep track of the objects assumed
		/// for delta bases.
		/// <para/>
		/// By default a receive pack doesn't save the objects that were used as
		/// delta bases. Setting this flag to {@code true} will allow the caller to
		/// use <see cref="getBaseObjectIds"/> to retrieve that list.
		/// </summary>
		/// <param name="b"> <code>true</code> to enable keeping track of delta bases.</param>
		public void setNeedBaseObjectIds(bool b)
		{
			this.needBaseObjectIds = b;
		}

		///  <returns> the set of objects the incoming pack assumed for delta purposes</returns>
		public HashSet<ObjectId> getBaseObjectIds()
		{
			return indexPack.getBaseObjectIds();
		}

		/// <summary>
		/// Configure this receive pack instance to keep track of new objects.
		/// <para/>
		/// By default a receive pack doesn't save the new objects that were created
		/// when it was instantiated. Setting this flag to {@code true} allows the
		/// caller to use {@link #getNewObjectIds()} to retrieve that list.
		/// </summary>
		/// <param name="b"><code>true</code> to enable keeping track of new objects.</param> 
		public void setNeedNewObjectIds(bool b)
		{
			this.needNewObjectIds = b;
		}


		/// <returns>the new objects that were sent by the user</returns>  
		public HashSet<ObjectId> getNewObjectIds()
		{
			return indexPack.getNewObjectIds();
		}

		/// <returns>
		/// true if this class expects a bi-directional pipe opened between
		/// the client and itself. The default is true.
		/// </returns>
		public bool isBiDirectionalPipe()
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
		public void setBiDirectionalPipe(bool twoWay)
		{
			biDirectionalPipe = twoWay;
		}

		/// <summary>
		/// Returns true if this instance will verify received objects are formatted correctly. 
		/// Validating objects requires more CPU time on this side of the connection.
		/// </summary>
		/// <returns></returns>
		public bool isCheckReceivedObjects()
		{
			return checkReceivedObjects;
		}

		/// <param name="check">true to enable checking received objects; false to assume all received objects are valid.</param>
		public void setCheckReceivedObjects(bool check)
		{
			checkReceivedObjects = check;
		}

		/// <summary>
		/// Returns true if the client can request refs to be created.
		/// </summary>
		/// <returns></returns>
		public bool isAllowCreates()
		{
			return allowCreates;
		}

		/// <param name="canCreate">true to permit create ref commands to be processed.</param>
		public void setAllowCreates(bool canCreate)
		{
			allowCreates = canCreate;
		}

		/// <summary>
		/// Returns true if the client can request refs to be deleted.
		/// </summary>
		public bool isAllowDeletes()
		{
			return allowDeletes;
		}

		/// <param name="canDelete">true to permit delete ref commands to be processed.</param>
		public void setAllowDeletes(bool canDelete)
		{
			allowDeletes = canDelete;
		}

		/// <summary>
		/// Returns true if the client can request non-fast-forward updates of a ref, possibly making objects unreachable.
		/// </summary>
		public bool isAllowNonFastForwards()
		{
			return allowNonFastForwards;
		}

		/// <param name="canRewind">true to permit the client to ask for non-fast-forward updates of an existing ref.</param>
		public void setAllowNonFastForwards(bool canRewind)
		{
			allowNonFastForwards = canRewind;
		}

		/// <summary>
		/// Returns identity of the user making the changes in the reflog.
		/// </summary>
		public PersonIdent getRefLogIdent()
		{
			return refLogIdent;
		}

		/// <summary>
		/// Set the identity of the user appearing in the affected reflogs.
		/// <para>
		/// The timestamp portion of the identity is ignored. A new identity with the
		/// current timestamp will be created automatically when the updates occur
		/// and the log records are written.
		/// </para>
		/// </summary>
		/// <param name="pi">identity of the user. If null the identity will be
		/// automatically determined based on the repository
		///configuration.</param>
		public void setRefLogIdent(PersonIdent pi)
		{
			refLogIdent = pi;
		}

		/// <returns>the filter used while advertising the refs to the client</returns>
		public RefFilter getRefFilter()
		{
			return refFilter;
		}

		/// <summary>
		/// Set the filter used while advertising the refs to the client.
		/// <para/>
		/// Only refs allowed by this filter will be shown to the client.
		/// Clients may still attempt to create or update a reference hidden
		/// by the configured <see cref="RefFilter"/>. These attempts should be
		/// rejected by a matching <see cref="PreReceiveHook"/>.
		/// </summary>
		/// <param name="refFilter">the filter; may be null to show all refs.</param>
		public void setRefFilter(RefFilter refFilter)
		{
			this.refFilter = refFilter ?? RefFilterContants.DEFAULT;
		}

		/// <returns>the hook invoked before updates occur.</returns>
		public IPreReceiveHook getPreReceiveHook()
		{
			return preReceive;
		}

		/// <summary>
		/// Set the hook which is invoked prior to commands being executed.
		/// <para>
		/// Only valid commands (those which have no obvious errors according to the
		/// received input and this instance's configuration) are passed into the
		/// hook. The hook may mark a command with a result of any value other than
		/// <see cref="ReceiveCommand.Result.NOT_ATTEMPTED"/> to block its execution.
		/// </para><para>
		/// The hook may be called with an empty command collection if the current
		/// set is completely invalid.</para>
		/// </summary>
		/// <param name="h">the hook instance; may be null to disable the hook.</param>
		public void setPreReceiveHook(IPreReceiveHook h)
		{
			preReceive = h ?? PreReceiveHook.NULL;
		}

		/// <returns>the hook invoked after updates occur.</returns>
		public IPostReceiveHook getPostReceiveHook()
		{
			return postReceive;
		}

		/// <summary>
		/// <para>
		/// Only successful commands (type is <see cref="ReceiveCommand.Result.OK"/>) are passed into the
		/// Set the hook which is invoked after commands are executed.
		/// hook. The hook may be called with an empty command collection if the
		/// current set all resulted in an error.
		/// </para>
		/// </summary>
		/// <param name="h">the hook instance; may be null to disable the hook.</param>
		public void setPostReceiveHook(IPostReceiveHook h)
		{
			postReceive = h ?? PostReceiveHook.NULL;
		}

		/// <returns>all of the command received by the current request.</returns>
		public IList<ReceiveCommand> getAllCommands()
		{
			return commands.AsReadOnly();
		}

		/// <summary>
		/// Send an error message to the client, if it supports receiving them.
		/// <para>
		/// If the client doesn't support receiving messages, the message will be
		/// discarded, with no other indication to the caller or to the client.
		/// </para>
		/// <para>
		/// <see cref="PreReceiveHook"/>s should always try to use
		/// <see cref="ReceiveCommand.setResult(GitSharp.Core.Transport.ReceiveCommand.Result,string)"/> with a result status of
		/// <see cref="ReceiveCommand.Result.REJECTED_OTHER_REASON"/> to indicate any reasons for
		/// rejecting an update. Messages attached to a command are much more likely
		/// to be returned to the client.
		/// </para>
		/// </summary>
		/// <param name="what">string describing the problem identified by the hook. The string must not end with an LF, and must not contain an LF.</param>
		public void sendError(string what)
		{
			try
			{
				if (msgs != null)
					msgs.Write("error: " + what + "\n");
			}
			catch (IOException)
			{
				// Ignore write failures.
			}
		}

		/// <summary>
		/// Send a message to the client, if it supports receiving them.
		/// <para>
		/// If the client doesn't support receiving messages, the message will be
		/// discarded, with no other indication to the caller or to the client.
		/// </para>
		/// </summary>
		/// <param name="what">string describing the problem identified by the hook. The string must not end with an LF, and must not contain an LF.</param>
		public void sendMessage(string what)
		{
			try
			{
				if (msgs != null)
					msgs.Write(what + "\n");
			}
			catch (IOException)
			{
				// Ignore write failures.
			}
		}

		/// <summary>
		/// Execute the receive task on the socket.
		/// </summary>
		/// <param name="input">Raw input to read client commands and pack data from. Caller must ensure the input is buffered, otherwise read performance may suffer. Response back to the Git network client. Caller must ensure the output is buffered, otherwise write performance may suffer.</param>
		/// <param name="messages">Secondary "notice" channel to send additional messages out through. When run over SSH this should be tied back to the standard error channel of the command execution. For most other network connections this should be null.</param>
		public void receive(Stream input, Stream output, Stream messages)
		{
			try
			{
				rawInput = input;
				rawOutput = output;

				pckIn = new PacketLineIn(rawInput);
				pckOut = new PacketLineOut(rawOutput);

				// TODO: [henon] port jgit's timeout behavior which is obviously missing here

				if (messages != null)
					msgs = new StreamWriter(messages, Constants.CHARSET);

				enabledCapabilities = new List<string>();
				commands = new List<ReceiveCommand>();

				Service();
			}
			finally
			{
				try
				{
					if (pckOut != null)
						pckOut.Flush();
					if (msgs != null)
						msgs.Flush();
					if (sideBand)
											// If we are using side band, we need to send a final
											// flush-pkt to tell the remote peer the side band is
											// complete and it should stop decoding. We need to
											// use the original output stream as rawOut is now the
											// side band data channel.
											//
						new PacketLineOut(output).End();

				}
				finally
				{
					// TODO : [nulltoken] Aren't we missing some Dispose() love, here ?
					UnlockPack();
					rawInput = null;
					rawOutput = null;
					pckIn = null;
					pckOut = null;
					msgs = null;
					refs = null;
					enabledCapabilities = null;
					commands = null;
				}
			}
		}

		private void Service()
		{
			if (biDirectionalPipe)
				SendAdvertisedRefs(new RefAdvertiser.PacketLineOutRefAdvertiser(pckOut));
			else
				refs = refFilter.filter(db.getAllRefs());

			RecvCommands();
			if (commands.isEmpty()) return;
			EnableCapabilities();

			if (NeedPack())
			{
				try
				{
					receivePack();
					if (isCheckReceivedObjects())
					{
						CheckConnectivity();
					}
					unpackError = null;
				}
				catch (IOException err)
				{
					unpackError = err;
				}
				catch (Exception err)
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
				SendStatusReport(true, new ServiceReporter(pckOut));
				pckOut.End();
			}
			else if (msgs != null)
			{
				SendStatusReport(false, new MessagesReporter(msgs.BaseStream));
			}

			postReceive.OnPostReceive(this, FilterCommands(ReceiveCommand.Result.OK));
		}

		private void UnlockPack()
		{
			if (packLock != null)
			{
				packLock.Unlock();
				packLock = null;
			}
		}

		/// <summary>
		/// Generate an advertisement of available refs and capabilities.
		/// </summary>
		/// <param name="adv">the advertisement formatter.</param>
		public void SendAdvertisedRefs(RefAdvertiser adv)
		{
			RevFlag advertised = walk.newFlag("ADVERTISED");
			adv.init(walk, advertised);
			adv.advertiseCapability(BasePackPushConnection.CAPABILITY_SIDE_BAND_64K);
			adv.advertiseCapability(BasePackPushConnection.CAPABILITY_DELETE_REFS);
			adv.advertiseCapability(BasePackPushConnection.CAPABILITY_REPORT_STATUS);
			if (allowOfsDelta)
				adv.advertiseCapability(BasePackPushConnection.CAPABILITY_OFS_DELTA);
			refs = refFilter.filter(db.getAllRefs());
			Ref head = refs.remove(Constants.HEAD);
			adv.send(refs);
			if (head != null && !head.isSymbolic())
				adv.advertiseHave(head.ObjectId);
			adv.includeAdditionalHaves();
			if (adv.isEmpty())
				adv.advertiseId(ObjectId.ZeroId, "capabilities^{}");
			adv.end();
		}


		private void RecvCommands()
		{
			while (true)
			{
				string line;
				try
				{
					line = pckIn.ReadStringRaw();
				}
				catch (EndOfStreamException)
				{
					if (commands.isEmpty())
					{
						return;
					}
					throw;
				}

				if (line == PacketLineIn.END)
					break;

				if (commands.isEmpty())
				{
					int nul = line.IndexOf('\0');
					if (nul >= 0)
					{
						foreach (string c in line.Substring(nul + 1).Split(' '))
						{
							enabledCapabilities.Add(c);
						}
						line = line.Slice(0, nul);
					}
				}

				if (line.Length < 83)
				{
					string m = "error: invalid protocol: wanted 'old new ref'";
					sendError(m);
					throw new PackProtocolException(m);
				}

				ObjectId oldId = ObjectId.FromString(line.Slice(0, 40));
				ObjectId newId = ObjectId.FromString(line.Slice(41, 81));
				string name = line.Substring(82);
				var cmd = new ReceiveCommand(oldId, newId, name);

				if (name.Equals(Constants.HEAD))
				{
					cmd.setResult(ReceiveCommand.Result.REJECTED_CURRENT_BRANCH);
				}
				else
				{
					cmd.setRef(refs.get(cmd.getRefName()));
				}

				commands.Add(cmd);
			}
		}

		private void EnableCapabilities()
		{
			reportStatus = enabledCapabilities.Contains(BasePackPushConnection.CAPABILITY_REPORT_STATUS);

			sideBand = enabledCapabilities.Contains(BasePackPushConnection.CAPABILITY_SIDE_BAND_64K);
			if (sideBand)
			{
				Stream @out = rawOutput;

				rawOutput = new SideBandOutputStream(SideBandOutputStream.CH_DATA, SideBandOutputStream.MAX_BUF, @out);
				pckOut = new PacketLineOut(rawOutput);
				msgs = new StreamWriter(new SideBandOutputStream(SideBandOutputStream.CH_PROGRESS,
					SideBandOutputStream.MAX_BUF, @out), Constants.CHARSET);
			}
		}

		private bool NeedPack()
		{
			foreach (ReceiveCommand cmd in commands)
			{
				if (cmd.getType() != ReceiveCommand.Type.DELETE) return true;
			}
			return false;
		}

		private void receivePack()
		{
			indexPack = IndexPack.Create(db, rawInput);
			indexPack.setFixThin(true);
			indexPack.setNeedNewObjectIds(needNewObjectIds);
			indexPack.setNeedBaseObjectIds(needBaseObjectIds);
			indexPack.setObjectChecking(isCheckReceivedObjects());
			indexPack.index(NullProgressMonitor.Instance);

			//  TODO: [caytchen] reflect gitsharp
			string lockMsg = "jgit receive-pack";
			if (getRefLogIdent() != null)
				lockMsg += " from " + getRefLogIdent().ToExternalString();
			packLock = indexPack.renameAndOpenPack(lockMsg);
		}

		private void CheckConnectivity()
		{
			using (var ow = new ObjectWalk(db))
			{
				foreach (ReceiveCommand cmd in commands)
				{
					if (cmd.getResult() != ReceiveCommand.Result.NOT_ATTEMPTED) continue;
					if (cmd.getType() == ReceiveCommand.Type.DELETE) continue;
					ow.markStart(ow.parseAny(cmd.getNewId()));
				}
				foreach (Ref @ref in refs.Values)
				{
					ow.markUninteresting(ow.parseAny(@ref.ObjectId));
				}
				ow.checkConnectivity();
			}
		}

		private void ValidateCommands()
		{
			foreach (ReceiveCommand cmd in commands)
			{
				Ref @ref = cmd.getRef();
				if (cmd.getResult() != ReceiveCommand.Result.NOT_ATTEMPTED)
					continue;

				if (cmd.getType() == ReceiveCommand.Type.DELETE && !isAllowDeletes())
				{
					// Deletes are not supported on this repository.
					//
					cmd.setResult(ReceiveCommand.Result.REJECTED_NODELETE);
					continue;
				}

				if (cmd.getType() == ReceiveCommand.Type.CREATE)
				{
					if (!isAllowCreates())
					{
						cmd.setResult(ReceiveCommand.Result.REJECTED_NOCREATE);
						continue;
					}

					if (@ref != null && !isAllowNonFastForwards())
					{
						// Creation over an existing ref is certainly not going
						// to be a fast-forward update. We can reject it early.
						//
						cmd.setResult(ReceiveCommand.Result.REJECTED_NONFASTFORWARD);
						continue;
					}

					if (@ref != null)
					{
						// A well behaved client shouldn't have sent us a
						// create command for a ref we advertised to it.
						//
						cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, "ref exists");
						continue;
					}
				}

				if (cmd.getType() == ReceiveCommand.Type.DELETE && @ref != null && !ObjectId.ZeroId.Equals(cmd.getOldId()) && !@ref.ObjectId.Equals(cmd.getOldId()))
				{
					// Delete commands can be sent with the old id matching our
					// advertised value, *OR* with the old id being 0{40}. Any
					// other requested old id is invalid.
					//
					cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, "invalid old id sent");
					continue;
				}

				if (cmd.getType() == ReceiveCommand.Type.UPDATE)
				{
					if (@ref == null)
					{
						// The ref must have been advertised in order to be updated.
						//
						cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, "no such ref");
						continue;
					}

					if (!@ref.ObjectId.Equals(cmd.getOldId()))
					{
						// A properly functioning client will send the same
						// object id we advertised.
						//
						cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, "invalid old id sent");
						continue;
					}

					// Is this possibly a non-fast-forward style update?
					//
					RevObject oldObj, newObj;
					try
					{
						oldObj = walk.parseAny(cmd.getOldId());
					}
					catch (IOException)
					{
						cmd.setResult(ReceiveCommand.Result.REJECTED_MISSING_OBJECT, cmd.getOldId().Name);
						continue;
					}

					try
					{
						newObj = walk.parseAny(cmd.getNewId());
					}
					catch (IOException)
					{
						cmd.setResult(ReceiveCommand.Result.REJECTED_MISSING_OBJECT, cmd.getNewId().Name);
						continue;
					}

					var oldComm = (oldObj as RevCommit);
					var newComm = (newObj as RevCommit);
					if (oldComm != null && newComm != null)
					{
						try
						{
							if (!walk.isMergedInto(oldComm, newComm))
							{
								cmd.setType(ReceiveCommand.Type.UPDATE_NONFASTFORWARD);
							}
						}
						catch (MissingObjectException e)
						{
							cmd.setResult(ReceiveCommand.Result.REJECTED_MISSING_OBJECT, e.Message);
						}
						catch (IOException)
						{
							cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON);
						}
					}
					else
					{
						cmd.setType(ReceiveCommand.Type.UPDATE_NONFASTFORWARD);
					}
				}

				if (!cmd.getRefName().StartsWith(Constants.R_REFS) || !Repository.IsValidRefName(cmd.getRefName()))
				{
					cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, "funny refname");
				}
			}
		}

		private void ExecuteCommands()
		{
			preReceive.onPreReceive(this, FilterCommands(ReceiveCommand.Result.NOT_ATTEMPTED));
			foreach (ReceiveCommand cmd in FilterCommands(ReceiveCommand.Result.NOT_ATTEMPTED))
			{
				Execute(cmd);
			}
		}

		private void Execute(ReceiveCommand cmd)
		{
			try
			{
				RefUpdate ru = db.UpdateRef(cmd.getRefName());
				ru.setRefLogIdent(getRefLogIdent());
				switch (cmd.getType())
				{
					case ReceiveCommand.Type.DELETE:
						if (!ObjectId.ZeroId.Equals(cmd.getOldId()))
						{
							// We can only do a CAS style delete if the client
							// didn't bork its delete request by sending the
							// wrong zero id rather than the advertised one.
							//
							ru.setExpectedOldObjectId(cmd.getOldId());
						}
						ru.setForceUpdate(true);
						Status(cmd, ru.delete(walk));
						break;

					case ReceiveCommand.Type.CREATE:
					case ReceiveCommand.Type.UPDATE:
					case ReceiveCommand.Type.UPDATE_NONFASTFORWARD:
						ru.setForceUpdate(isAllowNonFastForwards());
						ru.setExpectedOldObjectId(cmd.getOldId());
						ru.setNewObjectId(cmd.getNewId());
						ru.setRefLogMessage("push", true);
						Status(cmd, ru.update(walk));
						break;
				}
			}
			catch (IOException err)
			{
				cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, "lock error: " + err.Message);
			}
		}


		private static void Status(ReceiveCommand cmd, RefUpdate.RefUpdateResult result)
		{
			switch (result)
			{
				case RefUpdate.RefUpdateResult.NOT_ATTEMPTED:
					cmd.setResult(ReceiveCommand.Result.NOT_ATTEMPTED);
					break;

				case RefUpdate.RefUpdateResult.LOCK_FAILURE:
				case RefUpdate.RefUpdateResult.IO_FAILURE:
					cmd.setResult(ReceiveCommand.Result.LOCK_FAILURE);
					break;

				case RefUpdate.RefUpdateResult.NO_CHANGE:
				case RefUpdate.RefUpdateResult.NEW:
				case RefUpdate.RefUpdateResult.FORCED:
				case RefUpdate.RefUpdateResult.FAST_FORWARD:
					cmd.setResult(ReceiveCommand.Result.OK);
					break;

				case RefUpdate.RefUpdateResult.REJECTED:
					cmd.setResult(ReceiveCommand.Result.REJECTED_NONFASTFORWARD);
					break;

				case RefUpdate.RefUpdateResult.REJECTED_CURRENT_BRANCH:
					cmd.setResult(ReceiveCommand.Result.REJECTED_CURRENT_BRANCH);
					break;

				default:
					cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, Enum.GetName(typeof(RefUpdate.RefUpdateResult), result));
					break;
			}
		}

		private List<ReceiveCommand> FilterCommands(ReceiveCommand.Result want)
		{
			var r = new List<ReceiveCommand>(commands.Count);
			foreach (ReceiveCommand cmd in commands)
			{
				if (cmd.getResult() == want)
					r.Add(cmd);
			}
			return r;
		}

		private void SendStatusReport(bool forClient, Reporter rout)
		{
			if (unpackError != null)
			{
				rout.SendString("unpack error " + unpackError.Message);
				if (forClient)
				{
					foreach (ReceiveCommand cmd in commands)
					{
						rout.SendString("ng " + cmd.getRefName() + " n/a (unpacker error)");
					}
				}

				return;
			}

			if (forClient)
			{
				rout.SendString("unpack ok");
			}
			foreach (ReceiveCommand cmd in commands)
			{
				if (cmd.getResult() == ReceiveCommand.Result.OK)
				{
					if (forClient)
						rout.SendString("ok " + cmd.getRefName());
					continue;
				}

				var r = new StringBuilder();
				r.Append("ng ");
				r.Append(cmd.getRefName());
				r.Append(" ");

				switch (cmd.getResult())
				{
					case ReceiveCommand.Result.NOT_ATTEMPTED:
						r.Append("server bug; ref not processed");
						break;

					case ReceiveCommand.Result.REJECTED_NOCREATE:
						r.Append("creation prohibited");
						break;

					case ReceiveCommand.Result.REJECTED_NODELETE:
						r.Append("deletion prohibited");
						break;

					case ReceiveCommand.Result.REJECTED_NONFASTFORWARD:
						r.Append("non-fast forward");
						break;

					case ReceiveCommand.Result.REJECTED_CURRENT_BRANCH:
						r.Append("branch is currently checked out");
						break;

					case ReceiveCommand.Result.REJECTED_MISSING_OBJECT:
						if (cmd.getMessage() == null)
							r.Append("missing object(s)");
						else if (cmd.getMessage().Length == Constants.OBJECT_ID_STRING_LENGTH)
							r.Append("object " + cmd.getMessage() + " missing");
						else
							r.Append(cmd.getMessage());
						break;

					case ReceiveCommand.Result.REJECTED_OTHER_REASON:
						if (cmd.getMessage() == null)
							r.Append("unspecified reason");
						else
							r.Append(cmd.getMessage());
						break;

					case ReceiveCommand.Result.LOCK_FAILURE:
						r.Append("failed to lock");
						break;

					case ReceiveCommand.Result.OK:
						// We shouldn't have reached this case (see 'ok' case above).
						continue;
				}

				rout.SendString(r.ToString());
			}
		}

		public void Dispose()
		{
			walk.Dispose();
			rawInput.Dispose();
			rawOutput.Dispose();
			msgs.Dispose();
		}

		#region Nested Types

		private abstract class Reporter
		{
			public abstract void SendString(string s);
		}

		private class ServiceReporter : Reporter
		{
			private readonly PacketLineOut _pckOut;

			public ServiceReporter(PacketLineOut pck)
			{
				_pckOut = pck;
			}

			public override void SendString(string s)
			{
				_pckOut.WriteString(s + "\n");
			}
		}

		private class MessagesReporter : Reporter
		{
			private readonly Stream _stream;

			public MessagesReporter(Stream ms)
			{
				_stream = ms;
			}

			public override void SendString(string s)
			{
				byte[] data = Constants.encode(s+"\n");
				_stream.Write(data, 0, data.Length); 
			}
		}

		#endregion
	}
}