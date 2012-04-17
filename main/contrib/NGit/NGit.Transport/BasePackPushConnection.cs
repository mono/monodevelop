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
using System.Text;
using NGit;
using NGit.Errors;
using NGit.Internal;
using NGit.Storage.Pack;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Push implementation using the native Git pack transfer service.</summary>
	/// <remarks>
	/// Push implementation using the native Git pack transfer service.
	/// <p>
	/// This is the canonical implementation for transferring objects to the remote
	/// repository from the local repository by talking to the 'git-receive-pack'
	/// service. Objects are packed on the local side into a pack file and then sent
	/// to the remote repository.
	/// <p>
	/// This connection requires only a bi-directional pipe or socket, and thus is
	/// easily wrapped up into a local process pipe, anonymous TCP socket, or a
	/// command executed through an SSH tunnel.
	/// <p>
	/// This implementation honors
	/// <see cref="Transport.IsPushThin()">Transport.IsPushThin()</see>
	/// option.
	/// <p>
	/// Concrete implementations should just call
	/// <see cref="BasePackConnection.Init(Sharpen.InputStream, Sharpen.OutputStream)">BasePackConnection.Init(Sharpen.InputStream, Sharpen.OutputStream)
	/// 	</see>
	/// and
	/// <see cref="BasePackConnection.ReadAdvertisedRefs()">BasePackConnection.ReadAdvertisedRefs()
	/// 	</see>
	/// methods in constructor or before any use. They
	/// should also handle resources releasing in
	/// <see cref="BasePackConnection.Close()">BasePackConnection.Close()</see>
	/// method if needed.
	/// </remarks>
	public abstract class BasePackPushConnection : BasePackConnection, PushConnection
	{
		/// <summary>The client expects a status report after the server processes the pack.</summary>
		/// <remarks>The client expects a status report after the server processes the pack.</remarks>
		public static readonly string CAPABILITY_REPORT_STATUS = "report-status";

		/// <summary>The server supports deleting refs.</summary>
		/// <remarks>The server supports deleting refs.</remarks>
		public static readonly string CAPABILITY_DELETE_REFS = "delete-refs";

		/// <summary>The server supports packs with OFS deltas.</summary>
		/// <remarks>The server supports packs with OFS deltas.</remarks>
		public static readonly string CAPABILITY_OFS_DELTA = "ofs-delta";

		/// <summary>The client supports using the 64K side-band for progress messages.</summary>
		/// <remarks>The client supports using the 64K side-band for progress messages.</remarks>
		public static readonly string CAPABILITY_SIDE_BAND_64K = "side-band-64k";

		private readonly bool thinPack;

		private bool capableDeleteRefs;

		private bool capableReport;

		private bool capableSideBand;

		private bool capableOfsDelta;

		private bool sentCommand;

		private bool writePack;

		/// <summary>Time in milliseconds spent transferring the pack data.</summary>
		/// <remarks>Time in milliseconds spent transferring the pack data.</remarks>
		private long packTransferTime;

		/// <summary>Create a new connection to push using the native git transport.</summary>
		/// <remarks>Create a new connection to push using the native git transport.</remarks>
		/// <param name="packTransport">the transport.</param>
		internal BasePackPushConnection(PackTransport packTransport) : base(packTransport
			)
		{
			thinPack = transport.IsPushThin();
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public virtual void Push(ProgressMonitor monitor, IDictionary<string, RemoteRefUpdate
			> refUpdates)
		{
			MarkStartedOperation();
			DoPush(monitor, refUpdates);
		}

		protected internal override TransportException NoRepository()
		{
			// Sadly we cannot tell the "invalid URI" case from "push not allowed".
			// Opening a fetch connection can help us tell the difference, as any
			// useful repository is going to support fetch if it also would allow
			// push. So if fetch throws NoRemoteRepositoryException we know the
			// URI is wrong. Otherwise we can correctly state push isn't allowed
			// as the fetch connection opened successfully.
			//
			try
			{
				transport.OpenFetch().Close();
			}
			catch (NGit.Errors.NotSupportedException)
			{
			}
			catch (NoRemoteRepositoryException e)
			{
				// Fall through.
				// Fetch concluded the repository doesn't exist.
				//
				return e;
			}
			catch (TransportException)
			{
			}
			// Fall through.
			return new TransportException(uri, JGitText.Get().pushNotPermitted);
		}

		/// <summary>Push one or more objects and update the remote repository.</summary>
		/// <remarks>Push one or more objects and update the remote repository.</remarks>
		/// <param name="monitor">progress monitor to receive status updates.</param>
		/// <param name="refUpdates">update commands to be applied to the remote repository.</param>
		/// <exception cref="NGit.Errors.TransportException">if any exception occurs.</exception>
		protected internal virtual void DoPush(ProgressMonitor monitor, IDictionary<string
			, RemoteRefUpdate> refUpdates)
		{
			try
			{
				WriteCommands(refUpdates.Values, monitor);
				if (writePack)
				{
					WritePack(refUpdates, monitor);
				}
				if (sentCommand)
				{
					if (capableReport)
					{
						ReadStatusReport(refUpdates);
					}
					if (capableSideBand)
					{
						// Ensure the data channel is at EOF, so we know we have
						// read all side-band data from all channels and have a
						// complete copy of the messages (if any) buffered from
						// the other data channels.
						//
						int b = @in.Read();
						if (0 <= b)
						{
							throw new TransportException(uri, MessageFormat.Format(JGitText.Get().expectedEOFReceived
								, (char)b));
						}
					}
				}
			}
			catch (TransportException e)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new TransportException(uri, e.Message, e);
			}
			finally
			{
				Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteCommands(ICollection<RemoteRefUpdate> refUpdates, ProgressMonitor
			 monitor)
		{
			string capabilities = EnableCapabilities(monitor);
			foreach (RemoteRefUpdate rru in refUpdates)
			{
				if (!capableDeleteRefs && rru.IsDelete())
				{
					rru.SetStatus(RemoteRefUpdate.Status.REJECTED_NODELETE);
					continue;
				}
				StringBuilder sb = new StringBuilder();
				Ref advertisedRef = GetRef(rru.GetRemoteName());
				ObjectId oldId = (advertisedRef == null ? ObjectId.ZeroId : advertisedRef.GetObjectId
					());
				sb.Append(oldId.Name);
				sb.Append(' ');
				sb.Append(rru.GetNewObjectId().Name);
				sb.Append(' ');
				sb.Append(rru.GetRemoteName());
				if (!sentCommand)
				{
					sentCommand = true;
					sb.Append(capabilities);
				}
				pckOut.WriteString(sb.ToString());
				rru.SetStatus(RemoteRefUpdate.Status.AWAITING_REPORT);
				if (!rru.IsDelete())
				{
					writePack = true;
				}
			}
			if (monitor.IsCancelled())
			{
				throw new TransportException(uri, JGitText.Get().pushCancelled);
			}
			pckOut.End();
			outNeedsEnd = false;
		}

		private string EnableCapabilities(ProgressMonitor monitor)
		{
			StringBuilder line = new StringBuilder();
			capableReport = WantCapability(line, CAPABILITY_REPORT_STATUS);
			capableDeleteRefs = WantCapability(line, CAPABILITY_DELETE_REFS);
			capableOfsDelta = WantCapability(line, CAPABILITY_OFS_DELTA);
			capableSideBand = WantCapability(line, CAPABILITY_SIDE_BAND_64K);
			if (capableSideBand)
			{
				@in = new SideBandInputStream(@in, monitor, GetMessageWriter());
				pckIn = new PacketLineIn(@in);
			}
			if (line.Length > 0)
			{
				Sharpen.Runtime.SetCharAt(line, 0, '\0');
			}
			return line.ToString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WritePack(IDictionary<string, RemoteRefUpdate> refUpdates, ProgressMonitor
			 monitor)
		{
			ICollection<ObjectId> remoteObjects = new HashSet<ObjectId>();
			ICollection<ObjectId> newObjects = new HashSet<ObjectId>();
			PackWriter writer = new PackWriter(transport.GetPackConfig(), local.NewObjectReader
				());
			try
			{
				foreach (Ref r in GetRefs())
				{
					remoteObjects.AddItem(r.GetObjectId());
				}
				Sharpen.Collections.AddAll(remoteObjects, additionalHaves);
				foreach (RemoteRefUpdate r_1 in refUpdates.Values)
				{
					if (!ObjectId.ZeroId.Equals(r_1.GetNewObjectId()))
					{
						newObjects.AddItem(r_1.GetNewObjectId());
					}
				}
				writer.SetUseCachedPacks(true);
				writer.SetThin(thinPack);
				writer.SetReuseValidatingObjects(false);
				writer.SetDeltaBaseAsOffset(capableOfsDelta);
				writer.PreparePack(monitor, newObjects, remoteObjects);
				writer.WritePack(monitor, monitor, @out);
			}
			finally
			{
				writer.Release();
			}
			packTransferTime = writer.GetStatistics().GetTimeWriting();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadStatusReport(IDictionary<string, RemoteRefUpdate> refUpdates)
		{
			string unpackLine = ReadStringLongTimeout();
			if (!unpackLine.StartsWith("unpack "))
			{
				throw new PackProtocolException(uri, MessageFormat.Format(JGitText.Get().unexpectedReportLine
					, unpackLine));
			}
			string unpackStatus = Sharpen.Runtime.Substring(unpackLine, "unpack ".Length);
			if (!unpackStatus.Equals("ok"))
			{
				throw new TransportException(uri, MessageFormat.Format(JGitText.Get().errorOccurredDuringUnpackingOnTheRemoteEnd
					, unpackStatus));
			}
			string refLine;
			while ((refLine = pckIn.ReadString()) != PacketLineIn.END)
			{
				bool ok = false;
				int refNameEnd = -1;
				if (refLine.StartsWith("ok "))
				{
					ok = true;
					refNameEnd = refLine.Length;
				}
				else
				{
					if (refLine.StartsWith("ng "))
					{
						ok = false;
						refNameEnd = refLine.IndexOf(" ", 3);
					}
				}
				if (refNameEnd == -1)
				{
					throw new PackProtocolException(MessageFormat.Format(JGitText.Get().unexpectedReportLine2
						, uri, refLine));
				}
				string refName = Sharpen.Runtime.Substring(refLine, 3, refNameEnd);
				string message = (ok ? null : Sharpen.Runtime.Substring(refLine, refNameEnd + 1));
				RemoteRefUpdate rru = refUpdates.Get(refName);
				if (rru == null)
				{
					throw new PackProtocolException(MessageFormat.Format(JGitText.Get().unexpectedRefReport
						, uri, refName));
				}
				if (ok)
				{
					rru.SetStatus(RemoteRefUpdate.Status.OK);
				}
				else
				{
					rru.SetStatus(RemoteRefUpdate.Status.REJECTED_OTHER_REASON);
					rru.SetMessage(message);
				}
			}
			foreach (RemoteRefUpdate rru_1 in refUpdates.Values)
			{
				if (rru_1.GetStatus() == RemoteRefUpdate.Status.AWAITING_REPORT)
				{
					throw new PackProtocolException(MessageFormat.Format(JGitText.Get().expectedReportForRefNotReceived
						, uri, rru_1.GetRemoteName()));
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private string ReadStringLongTimeout()
		{
			if (timeoutIn == null)
			{
				return pckIn.ReadString();
			}
			// The remote side may need a lot of time to choke down the pack
			// we just sent them. There may be many deltas that need to be
			// resolved by the remote. Its hard to say how long the other
			// end is going to be silent. Taking 10x the configured timeout
			// or the time spent transferring the pack, whichever is larger,
			// gives the other side some reasonable window to process the data,
			// but this is just a wild guess.
			//
			int oldTimeout = timeoutIn.GetTimeout();
			int sendTime = (int)Math.Min(packTransferTime, 28800000L);
			try
			{
				timeoutIn.SetTimeout(10 * Math.Max(sendTime, oldTimeout));
				return pckIn.ReadString();
			}
			finally
			{
				timeoutIn.SetTimeout(oldTimeout);
			}
		}
	}
}
