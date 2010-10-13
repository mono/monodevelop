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
using System.Text;
using NGit;
using NGit.Errors;
using NGit.Transport;
using NGit.Util.IO;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Base helper class for pack-based operations implementations.</summary>
	/// <remarks>
	/// Base helper class for pack-based operations implementations. Provides partial
	/// implementation of pack-protocol - refs advertising and capabilities support,
	/// and some other helper methods.
	/// </remarks>
	/// <seealso cref="BasePackFetchConnection">BasePackFetchConnection</seealso>
	/// <seealso cref="BasePackPushConnection">BasePackPushConnection</seealso>
	public abstract class BasePackConnection : BaseConnection
	{
		/// <summary>The repository this transport fetches into, or pushes out of.</summary>
		/// <remarks>The repository this transport fetches into, or pushes out of.</remarks>
		protected internal readonly Repository local;

		/// <summary>Remote repository location.</summary>
		/// <remarks>Remote repository location.</remarks>
		protected internal readonly URIish uri;

		/// <summary>
		/// A transport connected to
		/// <see cref="uri">uri</see>
		/// .
		/// </summary>
		protected internal readonly NGit.Transport.Transport transport;

		/// <summary>Low-level input stream, if a timeout was configured.</summary>
		/// <remarks>Low-level input stream, if a timeout was configured.</remarks>
		protected internal TimeoutInputStream timeoutIn;

		/// <summary>Low-level output stream, if a timeout was configured.</summary>
		/// <remarks>Low-level output stream, if a timeout was configured.</remarks>
		protected internal TimeoutOutputStream timeoutOut;

		/// <summary>
		/// Timer to manage
		/// <see cref="timeoutIn">timeoutIn</see>
		/// and
		/// <see cref="timeoutOut">timeoutOut</see>
		/// .
		/// </summary>
		private InterruptTimer myTimer;

		/// <summary>Input stream reading from the remote.</summary>
		/// <remarks>Input stream reading from the remote.</remarks>
		protected internal InputStream @in;

		/// <summary>Output stream sending to the remote.</summary>
		/// <remarks>Output stream sending to the remote.</remarks>
		protected internal OutputStream @out;

		/// <summary>
		/// Packet line decoder around
		/// <see cref="@in">@in</see>
		/// .
		/// </summary>
		protected internal PacketLineIn pckIn;

		/// <summary>
		/// Packet line encoder around
		/// <see cref="@out">@out</see>
		/// .
		/// </summary>
		protected internal PacketLineOut pckOut;

		/// <summary>
		/// Send
		/// <see cref="PacketLineOut.End()">PacketLineOut.End()</see>
		/// before closing
		/// <see cref="@out">@out</see>
		/// ?
		/// </summary>
		protected internal bool outNeedsEnd;

		/// <summary>True if this is a stateless RPC connection.</summary>
		/// <remarks>True if this is a stateless RPC connection.</remarks>
		protected internal bool statelessRPC;

		/// <summary>Capability tokens advertised by the remote side.</summary>
		/// <remarks>Capability tokens advertised by the remote side.</remarks>
		private readonly ICollection<string> remoteCapablities = new HashSet<string>();

		/// <summary>Extra objects the remote has, but which aren't offered as refs.</summary>
		/// <remarks>Extra objects the remote has, but which aren't offered as refs.</remarks>
		protected internal readonly ICollection<ObjectId> additionalHaves = new HashSet<ObjectId
			>();

		internal BasePackConnection(PackTransport packTransport)
		{
			transport = (NGit.Transport.Transport)packTransport;
			local = transport.local;
			uri = transport.uri;
		}

		/// <summary>Configure this connection with the directional pipes.</summary>
		/// <remarks>Configure this connection with the directional pipes.</remarks>
		/// <param name="myIn">
		/// input stream to receive data from the peer. Caller must ensure
		/// the input is buffered, otherwise read performance may suffer.
		/// </param>
		/// <param name="myOut">
		/// output stream to transmit data to the peer. Caller must ensure
		/// the output is buffered, otherwise write performance may
		/// suffer.
		/// </param>
		protected internal void Init(InputStream myIn, OutputStream myOut)
		{
			int timeout = transport.GetTimeout();
			if (timeout > 0)
			{
				Sharpen.Thread caller = Sharpen.Thread.CurrentThread();
				myTimer = new InterruptTimer(caller.GetName() + "-Timer");
				timeoutIn = new TimeoutInputStream(myIn, myTimer);
				timeoutOut = new TimeoutOutputStream(myOut, myTimer);
				timeoutIn.SetTimeout(timeout * 1000);
				timeoutOut.SetTimeout(timeout * 1000);
				myIn = timeoutIn;
				myOut = timeoutOut;
			}
			@in = myIn;
			@out = myOut;
			pckIn = new PacketLineIn(@in);
			pckOut = new PacketLineOut(@out);
			outNeedsEnd = true;
		}

		/// <summary>Reads the advertised references through the initialized stream.</summary>
		/// <remarks>
		/// Reads the advertised references through the initialized stream.
		/// <p>
		/// Subclass implementations may call this method only after setting up the
		/// input and output streams with
		/// <see cref="Init(Sharpen.InputStream, Sharpen.OutputStream)">Init(Sharpen.InputStream, Sharpen.OutputStream)
		/// 	</see>
		/// .
		/// <p>
		/// If any errors occur, this connection is automatically closed by invoking
		/// <see cref="Close()">Close()</see>
		/// and the exception is wrapped (if necessary) and thrown
		/// as a
		/// <see cref="NGit.Errors.TransportException">NGit.Errors.TransportException</see>
		/// .
		/// </remarks>
		/// <exception cref="NGit.Errors.TransportException">the reference list could not be scanned.
		/// 	</exception>
		protected internal virtual void ReadAdvertisedRefs()
		{
			try
			{
				ReadAdvertisedRefsImpl();
			}
			catch (TransportException err)
			{
				Close();
				throw;
			}
			catch (IOException err)
			{
				Close();
				throw new TransportException(err.Message, err);
			}
			catch (RuntimeException err)
			{
				Close();
				throw new TransportException(err.Message, err);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadAdvertisedRefsImpl()
		{
			LinkedHashMap<string, Ref> avail = new LinkedHashMap<string, Ref>();
			for (; ; )
			{
				string line;
				try
				{
					line = pckIn.ReadString();
				}
				catch (EOFException eof)
				{
					if (avail.IsEmpty())
					{
						throw NoRepository();
					}
					throw;
				}
				if (line == PacketLineIn.END)
				{
					break;
				}
				if (line.StartsWith("ERR "))
				{
					// This is a customized remote service error.
					// Users should be informed about it.
					throw new RemoteRepositoryException(uri, Sharpen.Runtime.Substring(line, 4));
				}
				if (avail.IsEmpty())
				{
					int nul = line.IndexOf('\0');
					if (nul >= 0)
					{
						// The first line (if any) may contain "hidden"
						// capability values after a NUL byte.
						foreach (string c in Sharpen.Runtime.Substring(line, nul + 1).Split(" "))
						{
							remoteCapablities.AddItem(c);
						}
						line = Sharpen.Runtime.Substring(line, 0, nul);
					}
				}
				string name = Sharpen.Runtime.Substring(line, 41, line.Length);
				if (avail.IsEmpty() && name.Equals("capabilities^{}"))
				{
					// special line from git-receive-pack to show
					// capabilities when there are no refs to advertise
					continue;
				}
				ObjectId id = ObjectId.FromString(Sharpen.Runtime.Substring(line, 0, 40));
				if (name.Equals(".have"))
				{
					additionalHaves.AddItem(id);
				}
				else
				{
					if (name.EndsWith("^{}"))
					{
						name = Sharpen.Runtime.Substring(name, 0, name.Length - 3);
						Ref prior = avail.Get(name);
						if (prior == null)
						{
							throw new PackProtocolException(uri, MessageFormat.Format(JGitText.Get().advertisementCameBefore
								, name, name));
						}
						if (prior.GetPeeledObjectId() != null)
						{
							throw DuplicateAdvertisement(name + "^{}");
						}
						avail.Put(name, new ObjectIdRef.PeeledTag(RefStorage.NETWORK, name, prior.GetObjectId
							(), id));
					}
					else
					{
						Ref prior = avail.Put(name, new ObjectIdRef.PeeledNonTag(RefStorage.NETWORK, name
							, id));
						if (prior != null)
						{
							throw DuplicateAdvertisement(name);
						}
					}
				}
			}
			Available(avail);
		}

		/// <summary>Create an exception to indicate problems finding a remote repository.</summary>
		/// <remarks>
		/// Create an exception to indicate problems finding a remote repository. The
		/// caller is expected to throw the returned exception.
		/// Subclasses may override this method to provide better diagnostics.
		/// </remarks>
		/// <returns>
		/// a TransportException saying a repository cannot be found and
		/// possibly why.
		/// </returns>
		protected internal virtual TransportException NoRepository()
		{
			return new NoRemoteRepositoryException(uri, JGitText.Get().notFound);
		}

		protected internal virtual bool IsCapableOf(string option)
		{
			return remoteCapablities.Contains(option);
		}

		protected internal virtual bool WantCapability(StringBuilder b, string option)
		{
			if (!IsCapableOf(option))
			{
				return false;
			}
			b.Append(' ');
			b.Append(option);
			return true;
		}

		private PackProtocolException DuplicateAdvertisement(string name)
		{
			return new PackProtocolException(uri, MessageFormat.Format(JGitText.Get().duplicateAdvertisementsOf
				, name));
		}

		public override void Close()
		{
			if (@out != null)
			{
				try
				{
					if (outNeedsEnd)
					{
						outNeedsEnd = false;
						pckOut.End();
					}
					@out.Close();
				}
				catch (IOException)
				{
				}
				finally
				{
					// Ignore any close errors.
					@out = null;
					pckOut = null;
				}
			}
			if (@in != null)
			{
				try
				{
					@in.Close();
				}
				catch (IOException)
				{
				}
				finally
				{
					// Ignore any close errors.
					@in = null;
					pckIn = null;
				}
			}
			if (myTimer != null)
			{
				try
				{
					myTimer.Terminate();
				}
				finally
				{
					myTimer = null;
					timeoutIn = null;
					timeoutOut = null;
				}
			}
		}

		/// <summary>Tell the peer we are disconnecting, if it cares to know.</summary>
		/// <remarks>Tell the peer we are disconnecting, if it cares to know.</remarks>
		protected internal virtual void EndOut()
		{
			if (outNeedsEnd && @out != null)
			{
				try
				{
					outNeedsEnd = false;
					pckOut.End();
				}
				catch (IOException)
				{
					try
					{
						@out.Close();
					}
					catch (IOException)
					{
					}
					finally
					{
						// Ignore any close errors.
						@out = null;
						pckOut = null;
					}
				}
			}
		}
	}
}
