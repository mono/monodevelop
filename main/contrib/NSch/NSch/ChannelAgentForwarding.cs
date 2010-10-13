/*
Copyright (c) 2006-2010 ymnk, JCraft,Inc. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

  1. Redistributions of source code must retain the above copyright notice,
     this list of conditions and the following disclaimer.

  2. Redistributions in binary form must reproduce the above copyright 
     notice, this list of conditions and the following disclaimer in 
     the documentation and/or other materials provided with the distribution.

  3. The names of the authors may not be used to endorse or promote products
     derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL JCRAFT,
INC. OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

This code is based on jsch (http://www.jcraft.com/jsch).
All credit should go to the authors of jsch.
*/

using System;
using System.Collections;
using System.IO;
using NSch;
using Sharpen;

namespace NSch
{
	internal class ChannelAgentForwarding : Channel
	{
		private const int LOCAL_WINDOW_SIZE_MAX = unchecked((int)(0x20000));

		private const int LOCAL_MAXIMUM_PACKET_SIZE = unchecked((int)(0x4000));

		private readonly int SSH2_AGENTC_REQUEST_IDENTITIES = 11;

		private readonly int SSH2_AGENT_IDENTITIES_ANSWER = 12;

		private readonly int SSH2_AGENTC_SIGN_REQUEST = 13;

		private readonly int SSH2_AGENT_SIGN_RESPONSE = 14;

		private readonly int SSH2_AGENTC_ADD_IDENTITY = 17;

		private readonly int SSH2_AGENTC_REMOVE_IDENTITY = 18;

		private readonly int SSH2_AGENTC_REMOVE_ALL_IDENTITIES = 19;

		private readonly int SSH2_AGENT_FAILURE = 30;

		internal bool init = true;

		private Buffer rbuf = null;

		private Buffer wbuf = null;

		private Packet packet = null;

		private Buffer mbuf = null;

		public ChannelAgentForwarding() : base()
		{
			SetLocalWindowSizeMax(LOCAL_WINDOW_SIZE_MAX);
			SetLocalWindowSize(LOCAL_WINDOW_SIZE_MAX);
			SetLocalPacketSize(LOCAL_MAXIMUM_PACKET_SIZE);
			type = Util.Str2byte("auth-agent@openssh.com");
			rbuf = new Buffer();
			rbuf.Reset();
			//wbuf=new Buffer(rmpsize);
			//packet=new Packet(wbuf);
			mbuf = new Buffer();
			connected = true;
		}

		public override void Run()
		{
			try
			{
				SendOpenConfirmation();
			}
			catch (Exception)
			{
				close = true;
				Disconnect();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override void Write(byte[] foo, int s, int l)
		{
			if (packet == null)
			{
				wbuf = new Buffer(rmpsize);
				packet = new Packet(wbuf);
			}
			rbuf.Shift();
			if (rbuf.buffer.Length < rbuf.index + l)
			{
				byte[] newbuf = new byte[rbuf.s + l];
				System.Array.Copy(rbuf.buffer, 0, newbuf, 0, rbuf.buffer.Length);
				rbuf.buffer = newbuf;
			}
			rbuf.PutByte(foo, s, l);
			int mlen = rbuf.GetInt();
			if (mlen > rbuf.GetLength())
			{
				rbuf.s -= 4;
				return;
			}
			int typ = rbuf.GetByte();
			Session _session = null;
			try
			{
				_session = GetSession();
			}
			catch (JSchException e)
			{
				throw new IOException(e.ToString());
			}
			ArrayList identities = _session.jsch.identities;
			UserInfo userinfo = _session.GetUserInfo();
			if (typ == SSH2_AGENTC_REQUEST_IDENTITIES)
			{
				mbuf.Reset();
				mbuf.PutByte(unchecked((byte)SSH2_AGENT_IDENTITIES_ANSWER));
				lock (identities)
				{
					int count = 0;
					for (int i = 0; i < identities.Count; i++)
					{
						Identity identity = (Identity)(identities[i]);
						if (identity.GetPublicKeyBlob() != null)
						{
							count++;
						}
					}
					mbuf.PutInt(count);
					for (int i_1 = 0; i_1 < identities.Count; i_1++)
					{
						Identity identity = (Identity)(identities[i_1]);
						byte[] pubkeyblob = identity.GetPublicKeyBlob();
						if (pubkeyblob == null)
						{
							continue;
						}
						mbuf.PutString(pubkeyblob);
						mbuf.PutString(Util.empty);
					}
				}
				byte[] bar = new byte[mbuf.GetLength()];
				mbuf.GetByte(bar);
				Send(bar);
			}
			else
			{
				if (typ == SSH2_AGENTC_SIGN_REQUEST)
				{
					byte[] blob = rbuf.GetString();
					byte[] data = rbuf.GetString();
					int flags = rbuf.GetInt();
					//      if((flags & 1)!=0){ //SSH_AGENT_OLD_SIGNATURE // old OpenSSH 2.0, 2.1
					//        datafellows = SSH_BUG_SIGBLOB;
					//      }
					Identity identity = null;
					lock (identities)
					{
						for (int i = 0; i < identities.Count; i++)
						{
							Identity _identity = (Identity)(identities[i]);
							if (_identity.GetPublicKeyBlob() == null)
							{
								continue;
							}
							if (!Util.Array_equals(blob, _identity.GetPublicKeyBlob()))
							{
								continue;
							}
							if (_identity.IsEncrypted())
							{
								if (userinfo == null)
								{
									continue;
								}
								while (_identity.IsEncrypted())
								{
									if (!userinfo.PromptPassphrase("Passphrase for " + _identity.GetName()))
									{
										break;
									}
									string _passphrase = userinfo.GetPassphrase();
									if (_passphrase == null)
									{
										break;
									}
									byte[] passphrase = Util.Str2byte(_passphrase);
									try
									{
										if (_identity.SetPassphrase(passphrase))
										{
											break;
										}
									}
									catch (JSchException)
									{
										break;
									}
								}
							}
							if (!_identity.IsEncrypted())
							{
								identity = _identity;
								break;
							}
						}
					}
					byte[] signature = null;
					if (identity != null)
					{
						signature = identity.GetSignature(data);
					}
					mbuf.Reset();
					if (signature == null)
					{
						mbuf.PutByte(unchecked((byte)SSH2_AGENT_FAILURE));
					}
					else
					{
						mbuf.PutByte(unchecked((byte)SSH2_AGENT_SIGN_RESPONSE));
						mbuf.PutString(signature);
					}
					byte[] bar = new byte[mbuf.GetLength()];
					mbuf.GetByte(bar);
					Send(bar);
				}
			}
		}

		private void Send(byte[] message)
		{
			packet.Reset();
			wbuf.PutByte(unchecked((byte)Session.SSH_MSG_CHANNEL_DATA));
			wbuf.PutInt(recipient);
			wbuf.PutInt(4 + message.Length);
			wbuf.PutString(message);
			try
			{
				GetSession().Write(packet, this, 4 + message.Length);
			}
			catch (Exception)
			{
			}
		}
	}
}
