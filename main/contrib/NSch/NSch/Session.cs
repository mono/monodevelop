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
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using NSch;
using Sharpen;

namespace NSch
{
	public class Session : Runnable
	{
		internal const int SSH_MSG_DISCONNECT = 1;

		internal const int SSH_MSG_IGNORE = 2;

		internal const int SSH_MSG_UNIMPLEMENTED = 3;

		internal const int SSH_MSG_DEBUG = 4;

		internal const int SSH_MSG_SERVICE_REQUEST = 5;

		internal const int SSH_MSG_SERVICE_ACCEPT = 6;

		internal const int SSH_MSG_KEXINIT = 20;

		internal const int SSH_MSG_NEWKEYS = 21;

		internal const int SSH_MSG_KEXDH_INIT = 30;

		internal const int SSH_MSG_KEXDH_REPLY = 31;

		internal const int SSH_MSG_KEX_DH_GEX_GROUP = 31;

		internal const int SSH_MSG_KEX_DH_GEX_INIT = 32;

		internal const int SSH_MSG_KEX_DH_GEX_REPLY = 33;

		internal const int SSH_MSG_KEX_DH_GEX_REQUEST = 34;

		internal const int SSH_MSG_GLOBAL_REQUEST = 80;

		internal const int SSH_MSG_REQUEST_SUCCESS = 81;

		internal const int SSH_MSG_REQUEST_FAILURE = 82;

		internal const int SSH_MSG_CHANNEL_OPEN = 90;

		internal const int SSH_MSG_CHANNEL_OPEN_CONFIRMATION = 91;

		internal const int SSH_MSG_CHANNEL_OPEN_FAILURE = 92;

		internal const int SSH_MSG_CHANNEL_WINDOW_ADJUST = 93;

		internal const int SSH_MSG_CHANNEL_DATA = 94;

		internal const int SSH_MSG_CHANNEL_EXTENDED_DATA = 95;

		internal const int SSH_MSG_CHANNEL_EOF = 96;

		internal const int SSH_MSG_CHANNEL_CLOSE = 97;

		internal const int SSH_MSG_CHANNEL_REQUEST = 98;

		internal const int SSH_MSG_CHANNEL_SUCCESS = 99;

		internal const int SSH_MSG_CHANNEL_FAILURE = 100;

		private const int PACKET_MAX_SIZE = 256 * 1024;

		private byte[] V_S;

		private byte[] V_C = Util.Str2byte("SSH-2.0-JSCH-" + JSch.VERSION);

		private byte[] I_C;

		private byte[] I_S;

		private byte[] K_S;

		private byte[] session_id;

		private byte[] IVc2s;

		private byte[] IVs2c;

		private byte[] Ec2s;

		private byte[] Es2c;

		private byte[] MACc2s;

		private byte[] MACs2c;

		private int seqi = 0;

		private int seqo = 0;

		internal string[] guess = null;

		private NSch.Cipher s2ccipher;

		private NSch.Cipher c2scipher;

		private MAC s2cmac;

		private MAC c2smac;

		private byte[] s2cmac_result1;

		private byte[] s2cmac_result2;

		private Compression deflater;

		private Compression inflater;

		private IO io;

		private Socket socket;

		private int timeout = 0;

		private volatile bool isConnected = false;

		private bool isAuthed = false;

		private Sharpen.Thread connectThread = null;

		private object Lock = new object();

		internal bool x11_forwarding = false;

		internal bool agent_forwarding = false;

		internal InputStream @in = null;

		internal OutputStream @out = null;

		internal static Random random;

		internal Buffer buf;

		internal Packet packet;

		internal SocketFactory socket_factory = null;

		internal const int buffer_margin = 32 + 20 + 32;

		private Hashtable config = null;

		private Proxy proxy = null;

		private UserInfo userinfo;

		private string hostKeyAlias = null;

		private int serverAliveInterval = 0;

		private int serverAliveCountMax = 1;

		protected internal bool daemon_thread = false;

		private long kex_start_time = 0L;

		internal int max_auth_tries = 6;

		internal int auth_failures = 0;

		internal string host = "127.0.0.1";

		internal int port = 22;

		internal string username = null;

		internal byte[] password = null;

		internal JSch jsch;

		/// <exception cref="NSch.JSchException"></exception>
		internal Session(JSch jsch) : base()
		{
			grr = new Session.GlobalRequestReply(this);
			// http://ietf.org/internet-drafts/draft-ietf-secsh-assignednumbers-01.txt
			// server version
			// client version
			// the payload of the client's SSH_MSG_KEXINIT
			// the payload of the server's SSH_MSG_KEXINIT
			// the host key
			//private byte[] mac_buf;
			// maximum padding length
			// maximum mac length
			// margin for deflater; deflater may inflate data
			this.jsch = jsch;
			buf = new Buffer();
			packet = new Packet(buf);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void Connect()
		{
			Connect(timeout);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void Connect(int connectTimeout)
		{
			if (isConnected)
			{
				throw new JSchException("session is already connected");
			}
			io = new IO();
			if (random == null)
			{
				try
				{
					Type c = Sharpen.Runtime.GetType(GetConfig("random"));
					random = (Random)(System.Activator.CreateInstance(c));
				}
				catch (Exception e)
				{
					throw new JSchException(e.ToString(), e);
				}
			}
			Packet.SetRandom(random);
			if (JSch.GetLogger().IsEnabled(Logger.INFO))
			{
				JSch.GetLogger().Log(Logger.INFO, "Connecting to " + host + " port " + port);
			}
			try
			{
				int i;
				int j;
				if (proxy == null)
				{
					InputStream @in;
					OutputStream @out;
					if (socket_factory == null)
					{
						socket = Util.CreateSocket(host, port, connectTimeout);
						@in = socket.GetInputStream();
						@out = socket.GetOutputStream();
					}
					else
					{
						socket = socket_factory.CreateSocket(host, port);
						@in = socket_factory.GetInputStream(socket);
						@out = socket_factory.GetOutputStream(socket);
					}
					//if(timeout>0){ socket.setSoTimeout(timeout); }
					socket.NoDelay = true;
					io.SetInputStream(@in);
					io.SetOutputStream(@out);
				}
				else
				{
					lock (proxy)
					{
						proxy.Connect(socket_factory, host, port, connectTimeout);
						io.SetInputStream(proxy.GetInputStream());
						io.SetOutputStream(proxy.GetOutputStream());
						socket = proxy.GetSocket();
					}
				}
				if (connectTimeout > 0 && socket != null)
				{
					socket.ReceiveTimeout = connectTimeout;
				}
				isConnected = true;
				if (JSch.GetLogger().IsEnabled(Logger.INFO))
				{
					JSch.GetLogger().Log(Logger.INFO, "Connection established");
				}
				jsch.AddSession(this);
				{
					// Some Cisco devices will miss to read '\n' if it is sent separately.
					byte[] foo = new byte[V_C.Length + 1];
					System.Array.Copy(V_C, 0, foo, 0, V_C.Length);
					foo[foo.Length - 1] = unchecked((byte)(byte)('\n'));
					io.Put(foo, 0, foo.Length);
				}
				while (true)
				{
					i = 0;
					j = 0;
					while (i < buf.buffer.Length)
					{
						j = io.GetByte();
						if (j < 0)
						{
							break;
						}
						buf.buffer[i] = unchecked((byte)j);
						i++;
						if (j == 10)
						{
							break;
						}
					}
					if (j < 0)
					{
						throw new JSchException("connection is closed by foreign host");
					}
					if (buf.buffer[i - 1] == 10)
					{
						// 0x0a
						i--;
						if (i > 0 && buf.buffer[i - 1] == 13)
						{
							// 0x0d
							i--;
						}
					}
					if (i <= 3 || ((i != buf.buffer.Length) && (buf.buffer[0] != 'S' || buf.buffer[1]
						 != 'S' || buf.buffer[2] != 'H' || buf.buffer[3] != '-')))
					{
						// It must not start with 'SSH-'
						//System.err.println(new String(buf.buffer, 0, i);
						continue;
					}
					if (i == buf.buffer.Length || i < 7 || (buf.buffer[4] == '1' && buf.buffer[6] != 
						'9'))
					{
						// SSH-1.99 or SSH-2.0
						// SSH-1.5
						throw new JSchException("invalid server's version string");
					}
					break;
				}
				V_S = new byte[i];
				System.Array.Copy(buf.buffer, 0, V_S, 0, i);
				//System.err.println("V_S: ("+i+") ["+new String(V_S)+"]");
				if (JSch.GetLogger().IsEnabled(Logger.INFO))
				{
					JSch.GetLogger().Log(Logger.INFO, "Remote version string: " + Util.Byte2str(V_S));
					JSch.GetLogger().Log(Logger.INFO, "Local version string: " + Util.Byte2str(V_C));
				}
				Send_kexinit();
				buf = Read(buf);
				if (buf.GetCommand() != SSH_MSG_KEXINIT)
				{
					in_kex = false;
					throw new JSchException("invalid protocol: " + buf.GetCommand());
				}
				if (JSch.GetLogger().IsEnabled(Logger.INFO))
				{
					JSch.GetLogger().Log(Logger.INFO, "SSH_MSG_KEXINIT received");
				}
				KeyExchange kex = Receive_kexinit(buf);
				while (true)
				{
					buf = Read(buf);
					if (kex.GetState() == buf.GetCommand())
					{
						kex_start_time = Runtime.CurrentTimeMillis();
						bool result = kex.Next(buf);
						if (!result)
						{
							//System.err.println("verify: "+result);
							in_kex = false;
							throw new JSchException("verify: " + result);
						}
					}
					else
					{
						in_kex = false;
						throw new JSchException("invalid protocol(kex): " + buf.GetCommand());
					}
					if (kex.GetState() == KeyExchange.STATE_END)
					{
						break;
					}
				}
				try
				{
					CheckHost(host, port, kex);
				}
				catch (JSchException ee)
				{
					in_kex = false;
					throw;
				}
				Send_newkeys();
				// receive SSH_MSG_NEWKEYS(21)
				buf = Read(buf);
				//System.err.println("read: 21 ? "+buf.getCommand());
				if (buf.GetCommand() == SSH_MSG_NEWKEYS)
				{
					if (JSch.GetLogger().IsEnabled(Logger.INFO))
					{
						JSch.GetLogger().Log(Logger.INFO, "SSH_MSG_NEWKEYS received");
					}
					Receive_newkeys(buf, kex);
				}
				else
				{
					in_kex = false;
					throw new JSchException("invalid protocol(newkyes): " + buf.GetCommand());
				}
				try
				{
					string s = GetConfig("MaxAuthTries");
					if (s != null)
					{
						max_auth_tries = System.Convert.ToInt32(s);
					}
				}
				catch (FormatException e)
				{
					throw new JSchException("MaxAuthTries: " + GetConfig("MaxAuthTries"), e);
				}
				bool auth = false;
				bool auth_cancel = false;
				UserAuth ua = null;
				try
				{
					Type c = Sharpen.Runtime.GetType(GetConfig("userauth.none"));
					ua = (UserAuth)(System.Activator.CreateInstance(c));
				}
				catch (Exception e)
				{
					throw new JSchException(e.ToString(), e);
				}
				auth = ua.Start(this);
				string cmethods = GetConfig("PreferredAuthentications");
				string[] cmethoda = Util.Split(cmethods, ",");
				string smethods = null;
				if (!auth)
				{
					smethods = ((UserAuthNone)ua).GetMethods();
					if (smethods != null)
					{
						smethods = smethods.ToLower();
					}
					else
					{
						// methods: publickey,password,keyboard-interactive
						//smethods="publickey,password,keyboard-interactive";
						smethods = cmethods;
					}
				}
				string[] smethoda = Util.Split(smethods, ",");
				int methodi = 0;
				while (true)
				{
					while (!auth && cmethoda != null && methodi < cmethoda.Length)
					{
						string method = cmethoda[methodi++];
						bool acceptable = false;
						for (int k = 0; k < smethoda.Length; k++)
						{
							if (smethoda[k].Equals(method))
							{
								acceptable = true;
								break;
							}
						}
						if (!acceptable)
						{
							continue;
						}
						//System.err.println("  method: "+method);
						if (JSch.GetLogger().IsEnabled(Logger.INFO))
						{
							string str = "Authentications that can continue: ";
							for (int k_1 = methodi - 1; k_1 < cmethoda.Length; k_1++)
							{
								str += cmethoda[k_1];
								if (k_1 + 1 < cmethoda.Length)
								{
									str += ",";
								}
							}
							JSch.GetLogger().Log(Logger.INFO, str);
							JSch.GetLogger().Log(Logger.INFO, "Next authentication method: " + method);
						}
						ua = null;
						try
						{
							Type c = null;
							if (GetConfig("userauth." + method) != null)
							{
								c = Sharpen.Runtime.GetType(GetConfig("userauth." + method));
								ua = (UserAuth)(System.Activator.CreateInstance(c));
							}
						}
						catch (Exception)
						{
							if (JSch.GetLogger().IsEnabled(Logger.WARN))
							{
								JSch.GetLogger().Log(Logger.WARN, "failed to load " + method + " method");
							}
						}
						if (ua != null)
						{
							auth_cancel = false;
							try
							{
								auth = ua.Start(this);
								if (auth && JSch.GetLogger().IsEnabled(Logger.INFO))
								{
									JSch.GetLogger().Log(Logger.INFO, "Authentication succeeded (" + method + ").");
								}
							}
							catch (JSchAuthCancelException)
							{
								auth_cancel = true;
							}
							catch (JSchPartialAuthException ee)
							{
								string tmp = smethods;
								smethods = ee.GetMethods();
								smethoda = Util.Split(smethods, ",");
								if (!tmp.Equals(smethods))
								{
									methodi = 0;
								}
								//System.err.println("PartialAuth: "+methods);
								auth_cancel = false;
								goto loop_continue;
							}
							catch (RuntimeException ee)
							{
								throw;
							}
							catch (Exception)
							{
								//System.err.println("ee: "+ee); // SSH_MSG_DISCONNECT: 2 Too many authentication failures
								goto loop_break;
							}
						}
					}
					break;
loop_continue: ;
				}
loop_break: ;
				if (!auth)
				{
					if (auth_failures >= max_auth_tries)
					{
						if (JSch.GetLogger().IsEnabled(Logger.INFO))
						{
							JSch.GetLogger().Log(Logger.INFO, "Login trials exceeds " + max_auth_tries);
						}
					}
					if (auth_cancel)
					{
						throw new JSchException("Auth cancel");
					}
					throw new JSchException("Auth fail");
				}
				if (connectTimeout > 0 || timeout > 0)
				{
					socket.ReceiveTimeout = timeout;
				}
				isAuthed = true;
				lock (Lock)
				{
					if (isConnected)
					{
						connectThread = new Sharpen.Thread(this);
						connectThread.SetName("Connect thread " + host + " session");
						if (daemon_thread)
						{
							connectThread.SetDaemon(daemon_thread);
						}
						connectThread.Start();
					}
				}
			}
			catch (Exception e)
			{
				// The session has been already down and
				// we don't have to start new thread.
				in_kex = false;
				if (isConnected)
				{
					try
					{
						packet.Reset();
						buf.PutByte(unchecked((byte)SSH_MSG_DISCONNECT));
						buf.PutInt(3);
						buf.PutString(Util.Str2byte(e.ToString()));
						buf.PutString(Util.Str2byte("en"));
						Write(packet);
						Disconnect();
					}
					catch (Exception)
					{
					}
				}
				isConnected = false;
				//e.printStackTrace();
				if (e is RuntimeException)
				{
					throw (RuntimeException)e;
				}
				if (e is JSchException)
				{
					throw (JSchException)e;
				}
				throw new JSchException("Session.connect: " + e);
			}
			finally
			{
				Util.Bzero(this.password);
				this.password = null;
			}
		}

		/// <exception cref="System.Exception"></exception>
		private KeyExchange Receive_kexinit(Buffer buf)
		{
			int j = buf.GetInt();
			if (j != buf.GetLength())
			{
				// packet was compressed and
				buf.GetByte();
				// j is the size of deflated packet.
				I_S = new byte[buf.index - 5];
			}
			else
			{
				I_S = new byte[j - 1 - buf.GetByte()];
			}
			System.Array.Copy(buf.buffer, buf.s, I_S, 0, I_S.Length);
			if (!in_kex)
			{
				// We are in rekeying activated by the remote!
				Send_kexinit();
			}
			guess = KeyExchange.Guess(I_S, I_C);
			if (guess == null)
			{
				throw new JSchException("Algorithm negotiation fail");
			}
			if (!isAuthed && (guess[KeyExchange.PROPOSAL_ENC_ALGS_CTOS].Equals("none") || (guess
				[KeyExchange.PROPOSAL_ENC_ALGS_STOC].Equals("none"))))
			{
				throw new JSchException("NONE Cipher should not be chosen before authentification is successed."
					);
			}
			KeyExchange kex = null;
			try
			{
				Type c = Sharpen.Runtime.GetType(GetConfig(guess[KeyExchange.PROPOSAL_KEX_ALGS]));
				kex = (KeyExchange)(System.Activator.CreateInstance(c));
			}
			catch (Exception e)
			{
				throw new JSchException(e.ToString(), e);
			}
			kex.Init(this, V_S, V_C, I_S, I_C);
			return kex;
		}

		private bool in_kex = false;

		/// <exception cref="System.Exception"></exception>
		public virtual void Rekey()
		{
			Send_kexinit();
		}

		/// <exception cref="System.Exception"></exception>
		private void Send_kexinit()
		{
			if (in_kex)
			{
				return;
			}
			string cipherc2s = GetConfig("cipher.c2s");
			string ciphers2c = GetConfig("cipher.s2c");
			string[] not_available_ciphers = CheckCiphers(GetConfig("CheckCiphers"));
			if (not_available_ciphers != null && not_available_ciphers.Length > 0)
			{
				cipherc2s = Util.DiffString(cipherc2s, not_available_ciphers);
				ciphers2c = Util.DiffString(ciphers2c, not_available_ciphers);
				if (cipherc2s == null || ciphers2c == null)
				{
					throw new JSchException("There are not any available ciphers.");
				}
			}
			string kex = GetConfig("kex");
			string[] not_available_kexes = CheckKexes(GetConfig("CheckKexes"));
			if (not_available_kexes != null && not_available_kexes.Length > 0)
			{
				kex = Util.DiffString(kex, not_available_kexes);
				if (kex == null)
				{
					throw new JSchException("There are not any available kexes.");
				}
			}
			in_kex = true;
			kex_start_time = Runtime.CurrentTimeMillis();
			// byte      SSH_MSG_KEXINIT(20)
			// byte[16]  cookie (random bytes)
			// string    kex_algorithms
			// string    server_host_key_algorithms
			// string    encryption_algorithms_client_to_server
			// string    encryption_algorithms_server_to_client
			// string    mac_algorithms_client_to_server
			// string    mac_algorithms_server_to_client
			// string    compression_algorithms_client_to_server
			// string    compression_algorithms_server_to_client
			// string    languages_client_to_server
			// string    languages_server_to_client
			Buffer buf = new Buffer();
			// send_kexinit may be invoked
			Packet packet = new Packet(buf);
			// by user thread.
			packet.Reset();
			buf.PutByte(unchecked((byte)SSH_MSG_KEXINIT));
			lock (random)
			{
				random.Fill(buf.buffer, buf.index, 16);
				buf.Skip(16);
			}
			buf.PutString(Util.Str2byte(kex));
			buf.PutString(Util.Str2byte(GetConfig("server_host_key")));
			buf.PutString(Util.Str2byte(cipherc2s));
			buf.PutString(Util.Str2byte(ciphers2c));
			buf.PutString(Util.Str2byte(GetConfig("mac.c2s")));
			buf.PutString(Util.Str2byte(GetConfig("mac.s2c")));
			buf.PutString(Util.Str2byte(GetConfig("compression.c2s")));
			buf.PutString(Util.Str2byte(GetConfig("compression.s2c")));
			buf.PutString(Util.Str2byte(GetConfig("lang.c2s")));
			buf.PutString(Util.Str2byte(GetConfig("lang.s2c")));
			buf.PutByte(unchecked((byte)0));
			buf.PutInt(0);
			buf.SetOffSet(5);
			I_C = new byte[buf.GetLength()];
			buf.GetByte(I_C);
			Write(packet);
			if (JSch.GetLogger().IsEnabled(Logger.INFO))
			{
				JSch.GetLogger().Log(Logger.INFO, "SSH_MSG_KEXINIT sent");
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void Send_newkeys()
		{
			// send SSH_MSG_NEWKEYS(21)
			packet.Reset();
			buf.PutByte(unchecked((byte)SSH_MSG_NEWKEYS));
			Write(packet);
			if (JSch.GetLogger().IsEnabled(Logger.INFO))
			{
				JSch.GetLogger().Log(Logger.INFO, "SSH_MSG_NEWKEYS sent");
			}
		}

		/// <exception cref="NSch.JSchException"></exception>
		private void CheckHost(string chost, int port, KeyExchange kex)
		{
			string shkc = GetConfig("StrictHostKeyChecking");
			if (hostKeyAlias != null)
			{
				chost = hostKeyAlias;
			}
			//System.err.println("shkc: "+shkc);
			byte[] K_S = kex.GetHostKey();
			string key_type = kex.GetKeyType();
			string key_fprint = kex.GetFingerPrint();
			if (hostKeyAlias == null && port != 22)
			{
				chost = ("[" + chost + "]:" + port);
			}
			//    hostkey=new HostKey(chost, K_S);
			HostKeyRepository hkr = jsch.GetHostKeyRepository();
			int i = 0;
			lock (hkr)
			{
				i = hkr.Check(chost, K_S);
			}
			bool insert = false;
			if ((shkc.Equals("ask") || shkc.Equals("yes")) && i == HostKeyRepository.CHANGED)
			{
				string file = null;
				lock (hkr)
				{
					file = hkr.GetKnownHostsRepositoryID();
				}
				if (file == null)
				{
					file = "known_hosts";
				}
				bool b = false;
				if (userinfo != null)
				{
					string message = "WARNING: REMOTE HOST IDENTIFICATION HAS CHANGED!\n" + "IT IS POSSIBLE THAT SOMEONE IS DOING SOMETHING NASTY!\n"
						 + "Someone could be eavesdropping on you right now (man-in-the-middle attack)!\n"
						 + "It is also possible that the " + key_type + " host key has just been changed.\n"
						 + "The fingerprint for the " + key_type + " key sent by the remote host is\n" +
						 key_fprint + ".\n" + "Please contact your system administrator.\n" + "Add correct host key in "
						 + file + " to get rid of this message.";
					if (shkc.Equals("ask"))
					{
						b = userinfo.PromptYesNo(message + "\nDo you want to delete the old key and insert the new key?"
							);
					}
					else
					{
						// shkc.equals("yes")
						userinfo.ShowMessage(message);
					}
				}
				if (!b)
				{
					throw new JSchException("HostKey has been changed: " + chost);
				}
				lock (hkr)
				{
					hkr.Remove(chost, (key_type.Equals("DSA") ? "ssh-dss" : "ssh-rsa"), null);
					insert = true;
				}
			}
			if ((shkc.Equals("ask") || shkc.Equals("yes")) && (i != HostKeyRepository.OK) && 
				!insert)
			{
				if (shkc.Equals("yes"))
				{
					throw new JSchException("reject HostKey: " + host);
				}
				//System.err.println("finger-print: "+key_fprint);
				if (userinfo != null)
				{
					bool foo = userinfo.PromptYesNo("The authenticity of host '" + host + "' can't be established.\n"
						 + key_type + " key fingerprint is " + key_fprint + ".\n" + "Are you sure you want to continue connecting?"
						);
					if (!foo)
					{
						throw new JSchException("reject HostKey: " + host);
					}
					insert = true;
				}
				else
				{
					if (i == HostKeyRepository.NOT_INCLUDED)
					{
						throw new JSchException("UnknownHostKey: " + host + ". " + key_type + " key fingerprint is "
							 + key_fprint);
					}
					else
					{
						throw new JSchException("HostKey has been changed: " + host);
					}
				}
			}
			if (shkc.Equals("no") && HostKeyRepository.NOT_INCLUDED == i)
			{
				insert = true;
			}
			if (i == HostKeyRepository.OK && JSch.GetLogger().IsEnabled(Logger.INFO))
			{
				JSch.GetLogger().Log(Logger.INFO, "Host '" + host + "' is known and mathces the "
					 + key_type + " host key");
			}
			if (insert && JSch.GetLogger().IsEnabled(Logger.WARN))
			{
				JSch.GetLogger().Log(Logger.WARN, "Permanently added '" + host + "' (" + key_type
					 + ") to the list of known hosts.");
			}
			string hkh = GetConfig("HashKnownHosts");
			if (hkh.Equals("yes") && (hkr is KnownHosts))
			{
				hostkey = ((KnownHosts)hkr).CreateHashedHostKey(chost, K_S);
			}
			else
			{
				hostkey = new HostKey(chost, K_S);
			}
			if (insert)
			{
				lock (hkr)
				{
					hkr.Add(hostkey, userinfo);
				}
			}
		}

		//public void start(){ (new Thread(this)).start();  }
		/// <exception cref="NSch.JSchException"></exception>
		public virtual Channel OpenChannel(string type)
		{
			if (!isConnected)
			{
				throw new JSchException("session is down");
			}
			try
			{
				Channel channel = Channel.GetChannel(type);
				AddChannel(channel);
				channel.Init();
				return channel;
			}
			catch (Exception)
			{
			}
			//e.printStackTrace();
			return null;
		}

		// encode will bin invoked in write with synchronization.
		/// <exception cref="System.Exception"></exception>
		public virtual void Encode(Packet packet)
		{
			//System.err.println("encode: "+packet.buffer.getCommand());
			//System.err.println("        "+packet.buffer.index);
			//if(packet.buffer.getCommand()==96){
			//Thread.dumpStack();
			//}
			if (deflater != null)
			{
				compress_len[0] = packet.buffer.index;
				packet.buffer.buffer = deflater.Compress(packet.buffer.buffer, 5, compress_len);
				packet.buffer.index = compress_len[0];
			}
			if (c2scipher != null)
			{
				//packet.padding(c2scipher.getIVSize());
				packet.Padding(c2scipher_size);
				int pad = packet.buffer.buffer[4];
				lock (random)
				{
					random.Fill(packet.buffer.buffer, packet.buffer.index - pad, pad);
				}
			}
			else
			{
				packet.Padding(8);
			}
			if (c2smac != null)
			{
				c2smac.Update(seqo);
				c2smac.Update(packet.buffer.buffer, 0, packet.buffer.index);
				c2smac.DoFinal(packet.buffer.buffer, packet.buffer.index);
			}
			if (c2scipher != null)
			{
				byte[] buf = packet.buffer.buffer;
				c2scipher.Update(buf, 0, packet.buffer.index, buf, 0);
			}
			if (c2smac != null)
			{
				packet.buffer.Skip(c2smac.GetBlockSize());
			}
		}

		internal int[] uncompress_len = new int[1];

		internal int[] compress_len = new int[1];

		private int s2ccipher_size = 8;

		private int c2scipher_size = 8;

		/// <exception cref="System.Exception"></exception>
		public virtual Buffer Read(Buffer buf)
		{
			int j = 0;
			while (true)
			{
				buf.Reset();
				io.GetByte(buf.buffer, buf.index, s2ccipher_size);
				buf.index += s2ccipher_size;
				if (s2ccipher != null)
				{
					s2ccipher.Update(buf.buffer, 0, s2ccipher_size, buf.buffer, 0);
				}
				j = ((buf.buffer[0] << 24) & unchecked((int)(0xff000000))) | ((buf.buffer[1] << 16
					) & unchecked((int)(0x00ff0000))) | ((buf.buffer[2] << 8) & unchecked((int)(0x0000ff00
					))) | ((buf.buffer[3]) & unchecked((int)(0x000000ff)));
				// RFC 4253 6.1. Maximum Packet Length
				if (j < 5 || j > PACKET_MAX_SIZE)
				{
					Start_discard(buf, s2ccipher, s2cmac, j, PACKET_MAX_SIZE);
				}
				int need = j + 4 - s2ccipher_size;
				//if(need<0){
				//  throw new IOException("invalid data");
				//}
				if ((buf.index + need) > buf.buffer.Length)
				{
					byte[] foo = new byte[buf.index + need];
					System.Array.Copy(buf.buffer, 0, foo, 0, buf.index);
					buf.buffer = foo;
				}
				if ((need % s2ccipher_size) != 0)
				{
					string message = "Bad packet length " + need;
					if (JSch.GetLogger().IsEnabled(Logger.FATAL))
					{
						JSch.GetLogger().Log(Logger.FATAL, message);
					}
					Start_discard(buf, s2ccipher, s2cmac, j, PACKET_MAX_SIZE - s2ccipher_size);
				}
				if (need > 0)
				{
					io.GetByte(buf.buffer, buf.index, need);
					buf.index += (need);
					if (s2ccipher != null)
					{
						s2ccipher.Update(buf.buffer, s2ccipher_size, need, buf.buffer, s2ccipher_size);
					}
				}
				if (s2cmac != null)
				{
					s2cmac.Update(seqi);
					s2cmac.Update(buf.buffer, 0, buf.index);
					s2cmac.DoFinal(s2cmac_result1, 0);
					io.GetByte(s2cmac_result2, 0, s2cmac_result2.Length);
					if (!Arrays.Equals(s2cmac_result1, s2cmac_result2))
					{
						if (need > PACKET_MAX_SIZE)
						{
							throw new IOException("MAC Error");
						}
						Start_discard(buf, s2ccipher, s2cmac, j, PACKET_MAX_SIZE - need);
						continue;
					}
				}
				seqi++;
				if (inflater != null)
				{
					//inflater.uncompress(buf);
					int pad = buf.buffer[4];
					uncompress_len[0] = buf.index - 5 - pad;
					byte[] foo = inflater.Uncompress(buf.buffer, 5, uncompress_len);
					if (foo != null)
					{
						buf.buffer = foo;
						buf.index = 5 + uncompress_len[0];
					}
					else
					{
						System.Console.Error.WriteLine("fail in inflater");
						break;
					}
				}
				int type = buf.GetCommand() & unchecked((int)(0xff));
				//System.err.println("read: "+type);
				if (type == SSH_MSG_DISCONNECT)
				{
					buf.Rewind();
					buf.GetInt();
					buf.GetShort();
					int reason_code = buf.GetInt();
					byte[] description = buf.GetString();
					byte[] language_tag = buf.GetString();
					throw new JSchException("SSH_MSG_DISCONNECT: " + reason_code + " " + Util.Byte2str
						(description) + " " + Util.Byte2str(language_tag));
				}
				else
				{
					//break;
					if (type == SSH_MSG_IGNORE)
					{
					}
					else
					{
						if (type == SSH_MSG_UNIMPLEMENTED)
						{
							buf.Rewind();
							buf.GetInt();
							buf.GetShort();
							int reason_id = buf.GetInt();
							if (JSch.GetLogger().IsEnabled(Logger.INFO))
							{
								JSch.GetLogger().Log(Logger.INFO, "Received SSH_MSG_UNIMPLEMENTED for " + reason_id
									);
							}
						}
						else
						{
							if (type == SSH_MSG_DEBUG)
							{
								buf.Rewind();
								buf.GetInt();
								buf.GetShort();
							}
							else
							{
								if (type == SSH_MSG_CHANNEL_WINDOW_ADJUST)
								{
									buf.Rewind();
									buf.GetInt();
									buf.GetShort();
									Channel c = Channel.GetChannel(buf.GetInt(), this);
									if (c == null)
									{
									}
									else
									{
										c.AddRemoteWindowSize(buf.GetInt());
									}
								}
								else
								{
									if (type == UserAuth.SSH_MSG_USERAUTH_SUCCESS)
									{
										isAuthed = true;
										if (inflater == null && deflater == null)
										{
											string method;
											method = guess[KeyExchange.PROPOSAL_COMP_ALGS_CTOS];
											InitDeflater(method);
											method = guess[KeyExchange.PROPOSAL_COMP_ALGS_STOC];
											InitInflater(method);
										}
										break;
									}
									else
									{
										break;
									}
								}
							}
						}
					}
				}
			}
			buf.Rewind();
			return buf;
		}

		/// <exception cref="NSch.JSchException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void Start_discard(Buffer buf, NSch.Cipher cipher, MAC mac, int packet_length
			, int discard)
		{
			MAC discard_mac = null;
			if (!cipher.IsCBC())
			{
				throw new JSchException("Packet corrupt");
			}
			if (packet_length != PACKET_MAX_SIZE && mac != null)
			{
				discard_mac = mac;
			}
			discard -= buf.index;
			while (discard > 0)
			{
				buf.Reset();
				int len = discard > buf.buffer.Length ? buf.buffer.Length : discard;
				io.GetByte(buf.buffer, 0, len);
				if (discard_mac != null)
				{
					discard_mac.Update(buf.buffer, 0, len);
				}
				discard -= len;
			}
			if (discard_mac != null)
			{
				discard_mac.DoFinal(buf.buffer, 0);
			}
			throw new JSchException("Packet corrupt");
		}

		internal virtual byte[] GetSessionId()
		{
			return session_id;
		}

		/// <exception cref="System.Exception"></exception>
		private void Receive_newkeys(Buffer buf, KeyExchange kex)
		{
			UpdateKeys(kex);
			in_kex = false;
		}

		/// <exception cref="System.Exception"></exception>
		private void UpdateKeys(KeyExchange kex)
		{
			byte[] K = kex.GetK();
			byte[] H = kex.GetH();
			HASH hash = kex.GetHash();
			//    String[] guess=kex.guess;
			if (session_id == null)
			{
				session_id = new byte[H.Length];
				System.Array.Copy(H, 0, session_id, 0, H.Length);
			}
			buf.Reset();
			buf.PutMPInt(K);
			buf.PutByte(H);
			buf.PutByte(unchecked((byte)unchecked((int)(0x41))));
			buf.PutByte(session_id);
			hash.Update(buf.buffer, 0, buf.index);
			IVc2s = hash.Digest();
			int j = buf.index - session_id.Length - 1;
			buf.buffer[j]++;
			hash.Update(buf.buffer, 0, buf.index);
			IVs2c = hash.Digest();
			buf.buffer[j]++;
			hash.Update(buf.buffer, 0, buf.index);
			Ec2s = hash.Digest();
			buf.buffer[j]++;
			hash.Update(buf.buffer, 0, buf.index);
			Es2c = hash.Digest();
			buf.buffer[j]++;
			hash.Update(buf.buffer, 0, buf.index);
			MACc2s = hash.Digest();
			buf.buffer[j]++;
			hash.Update(buf.buffer, 0, buf.index);
			MACs2c = hash.Digest();
			try
			{
				Type c;
				string method;
				method = guess[KeyExchange.PROPOSAL_ENC_ALGS_STOC];
				c = Sharpen.Runtime.GetType(GetConfig(method));
				s2ccipher = (NSch.Cipher)(System.Activator.CreateInstance(c));
				while (s2ccipher.GetBlockSize() > Es2c.Length)
				{
					buf.Reset();
					buf.PutMPInt(K);
					buf.PutByte(H);
					buf.PutByte(Es2c);
					hash.Update(buf.buffer, 0, buf.index);
					byte[] foo = hash.Digest();
					byte[] bar = new byte[Es2c.Length + foo.Length];
					System.Array.Copy(Es2c, 0, bar, 0, Es2c.Length);
					System.Array.Copy(foo, 0, bar, Es2c.Length, foo.Length);
					Es2c = bar;
				}
				s2ccipher.Init(NSch.Cipher.DECRYPT_MODE, Es2c, IVs2c);
				s2ccipher_size = s2ccipher.GetIVSize();
				method = guess[KeyExchange.PROPOSAL_MAC_ALGS_STOC];
				c = Sharpen.Runtime.GetType(GetConfig(method));
				s2cmac = (MAC)(System.Activator.CreateInstance(c));
				s2cmac.Init(MACs2c);
				//mac_buf=new byte[s2cmac.getBlockSize()];
				s2cmac_result1 = new byte[s2cmac.GetBlockSize()];
				s2cmac_result2 = new byte[s2cmac.GetBlockSize()];
				method = guess[KeyExchange.PROPOSAL_ENC_ALGS_CTOS];
				c = Sharpen.Runtime.GetType(GetConfig(method));
				c2scipher = (NSch.Cipher)(System.Activator.CreateInstance(c));
				while (c2scipher.GetBlockSize() > Ec2s.Length)
				{
					buf.Reset();
					buf.PutMPInt(K);
					buf.PutByte(H);
					buf.PutByte(Ec2s);
					hash.Update(buf.buffer, 0, buf.index);
					byte[] foo = hash.Digest();
					byte[] bar = new byte[Ec2s.Length + foo.Length];
					System.Array.Copy(Ec2s, 0, bar, 0, Ec2s.Length);
					System.Array.Copy(foo, 0, bar, Ec2s.Length, foo.Length);
					Ec2s = bar;
				}
				c2scipher.Init(NSch.Cipher.ENCRYPT_MODE, Ec2s, IVc2s);
				c2scipher_size = c2scipher.GetIVSize();
				method = guess[KeyExchange.PROPOSAL_MAC_ALGS_CTOS];
				c = Sharpen.Runtime.GetType(GetConfig(method));
				c2smac = (MAC)(System.Activator.CreateInstance(c));
				c2smac.Init(MACc2s);
				method = guess[KeyExchange.PROPOSAL_COMP_ALGS_CTOS];
				InitDeflater(method);
				method = guess[KeyExchange.PROPOSAL_COMP_ALGS_STOC];
				InitInflater(method);
			}
			catch (Exception e)
			{
				if (e is JSchException)
				{
					throw;
				}
				throw new JSchException(e.ToString(), e);
			}
		}

		//System.err.println("updatekeys: "+e); 
		/// <exception cref="System.Exception"></exception>
		internal virtual void Write(Packet packet, Channel c, int length)
		{
			long t = GetTimeout();
			while (true)
			{
				if (in_kex)
				{
					if (t > 0L && (Runtime.CurrentTimeMillis() - kex_start_time) > t)
					{
						throw new JSchException("timeout in wating for rekeying process.");
					}
					try
					{
						Sharpen.Thread.Sleep(10);
					}
					catch (Exception)
					{
					}
					continue;
				}
				lock (c)
				{
					if (c.rwsize < length)
					{
						try
						{
							c.notifyme++;
							Sharpen.Runtime.Wait(c, 100);
						}
						catch (Exception)
						{
						}
						finally
						{
							c.notifyme--;
						}
					}
					if (c.rwsize >= length)
					{
						c.rwsize -= length;
						break;
					}
				}
				if (c.close || !c.IsConnected())
				{
					throw new IOException("channel is broken");
				}
				bool sendit = false;
				int s = 0;
				byte command = 0;
				int recipient = -1;
				lock (c)
				{
					if (c.rwsize > 0)
					{
						long len = c.rwsize;
						if (len > length)
						{
							len = length;
						}
						if (len != length)
						{
							s = packet.Shift((int)len, (c2scipher != null ? c2scipher_size : 8), (c2smac != null
								 ? c2smac.GetBlockSize() : 0));
						}
						command = packet.buffer.GetCommand();
						recipient = c.GetRecipient();
						length -= (int)len;
						c.rwsize -= len;
						sendit = true;
					}
				}
				if (sendit)
				{
					_write(packet);
					if (length == 0)
					{
						return;
					}
					packet.Unshift(command, recipient, s, length);
				}
				lock (c)
				{
					if (in_kex)
					{
						continue;
					}
					if (c.rwsize >= length)
					{
						c.rwsize -= length;
						break;
					}
				}
			}
			//try{ 
			//System.out.println("1wait: "+c.rwsize);
			//  c.notifyme++;
			//  c.wait(100); 
			//}
			//catch(java.lang.InterruptedException e){
			//}
			//finally{
			//  c.notifyme--;
			//}
			_write(packet);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void Write(Packet packet)
		{
			// System.err.println("in_kex="+in_kex+" "+(packet.buffer.getCommand()));
			long t = GetTimeout();
			while (in_kex)
			{
				if (t > 0L && (Runtime.CurrentTimeMillis() - kex_start_time) > t)
				{
					throw new JSchException("timeout in wating for rekeying process.");
				}
				byte command = packet.buffer.GetCommand();
				//System.err.println("command: "+command);
				if (command == SSH_MSG_KEXINIT || command == SSH_MSG_NEWKEYS || command == SSH_MSG_KEXDH_INIT
					 || command == SSH_MSG_KEXDH_REPLY || command == SSH_MSG_KEX_DH_GEX_GROUP || command
					 == SSH_MSG_KEX_DH_GEX_INIT || command == SSH_MSG_KEX_DH_GEX_REPLY || command ==
					 SSH_MSG_KEX_DH_GEX_REQUEST || command == SSH_MSG_DISCONNECT)
				{
					break;
				}
				try
				{
					Sharpen.Thread.Sleep(10);
				}
				catch (Exception)
				{
				}
			}
			_write(packet);
		}

		/// <exception cref="System.Exception"></exception>
		private void _write(Packet packet)
		{
			lock (Lock)
			{
				Encode(packet);
				if (io != null)
				{
					io.Put(packet);
					seqo++;
				}
			}
		}

		internal Runnable thread;

		public virtual void Run()
		{
			thread = this;
			byte[] foo;
			Buffer buf = new Buffer();
			Packet packet = new Packet(buf);
			int i = 0;
			Channel channel;
			int[] start = new int[1];
			int[] length = new int[1];
			KeyExchange kex = null;
			int stimeout = 0;
			try
			{
				while (isConnected && thread != null)
				{
					try
					{
						buf = Read(buf);
						stimeout = 0;
					}
					catch (ThreadInterruptedException ee)
					{
						if (!in_kex && stimeout < serverAliveCountMax)
						{
							SendKeepAliveMsg();
							stimeout++;
							continue;
						}
						else
						{
							if (in_kex && stimeout < serverAliveCountMax)
							{
								stimeout++;
								continue;
							}
						}
						throw;
					}
					int msgType = buf.GetCommand() & unchecked((int)(0xff));
					if (kex != null && kex.GetState() == msgType)
					{
						kex_start_time = Runtime.CurrentTimeMillis();
						bool result = kex.Next(buf);
						if (!result)
						{
							throw new JSchException("verify: " + result);
						}
						continue;
					}
					switch (msgType)
					{
						case SSH_MSG_KEXINIT:
						{
							//System.err.println("KEXINIT");
							kex = Receive_kexinit(buf);
							break;
						}

						case SSH_MSG_NEWKEYS:
						{
							//System.err.println("NEWKEYS");
							Send_newkeys();
							Receive_newkeys(buf, kex);
							kex = null;
							break;
						}

						case SSH_MSG_CHANNEL_DATA:
						{
							buf.GetInt();
							buf.GetByte();
							buf.GetByte();
							i = buf.GetInt();
							channel = Channel.GetChannel(i, this);
							foo = buf.GetString(start, length);
							if (channel == null)
							{
								break;
							}
							if (length[0] == 0)
							{
								break;
							}
							try
							{
								channel.Write(foo, start[0], length[0]);
							}
							catch (Exception)
							{
								//System.err.println(e);
								try
								{
									channel.Disconnect();
								}
								catch (Exception)
								{
								}
								break;
							}
							int len = length[0];
							channel.SetLocalWindowSize(channel.lwsize - len);
							if (channel.lwsize < channel.lwsize_max / 2)
							{
								packet.Reset();
								buf.PutByte(unchecked((byte)SSH_MSG_CHANNEL_WINDOW_ADJUST));
								buf.PutInt(channel.GetRecipient());
								buf.PutInt(channel.lwsize_max - channel.lwsize);
								lock (channel)
								{
									if (!channel.close)
									{
										Write(packet);
									}
								}
								channel.SetLocalWindowSize(channel.lwsize_max);
							}
							break;
						}

						case SSH_MSG_CHANNEL_EXTENDED_DATA:
						{
							buf.GetInt();
							buf.GetShort();
							i = buf.GetInt();
							channel = Channel.GetChannel(i, this);
							buf.GetInt();
							// data_type_code == 1
							foo = buf.GetString(start, length);
							//System.err.println("stderr: "+new String(foo,start[0],length[0]));
							if (channel == null)
							{
								break;
							}
							if (length[0] == 0)
							{
								break;
							}
							channel.Write_ext(foo, start[0], length[0]);
							int len = length[0];
							channel.SetLocalWindowSize(channel.lwsize - len);
							if (channel.lwsize < channel.lwsize_max / 2)
							{
								packet.Reset();
								buf.PutByte(unchecked((byte)SSH_MSG_CHANNEL_WINDOW_ADJUST));
								buf.PutInt(channel.GetRecipient());
								buf.PutInt(channel.lwsize_max - channel.lwsize);
								lock (channel)
								{
									if (!channel.close)
									{
										Write(packet);
									}
								}
								channel.SetLocalWindowSize(channel.lwsize_max);
							}
							break;
						}

						case SSH_MSG_CHANNEL_WINDOW_ADJUST:
						{
							buf.GetInt();
							buf.GetShort();
							i = buf.GetInt();
							channel = Channel.GetChannel(i, this);
							if (channel == null)
							{
								break;
							}
							channel.AddRemoteWindowSize(buf.GetInt());
							break;
						}

						case SSH_MSG_CHANNEL_EOF:
						{
							buf.GetInt();
							buf.GetShort();
							i = buf.GetInt();
							channel = Channel.GetChannel(i, this);
							if (channel != null)
							{
								//channel.eof_remote=true;
								//channel.eof();
								channel.Eof_remote();
							}
							break;
						}

						case SSH_MSG_CHANNEL_CLOSE:
						{
							buf.GetInt();
							buf.GetShort();
							i = buf.GetInt();
							channel = Channel.GetChannel(i, this);
							if (channel != null)
							{
								//	      channel.close();
								channel.Disconnect();
							}
							break;
						}

						case SSH_MSG_CHANNEL_OPEN_CONFIRMATION:
						{
							buf.GetInt();
							buf.GetShort();
							i = buf.GetInt();
							channel = Channel.GetChannel(i, this);
							if (channel == null)
							{
							}
							//break;
							int r = buf.GetInt();
							long rws = buf.GetUInt();
							int rps = buf.GetInt();
							channel.SetRemoteWindowSize(rws);
							channel.SetRemotePacketSize(rps);
							channel.open_confirmation = true;
							channel.SetRecipient(r);
							break;
						}

						case SSH_MSG_CHANNEL_OPEN_FAILURE:
						{
							buf.GetInt();
							buf.GetShort();
							i = buf.GetInt();
							channel = Channel.GetChannel(i, this);
							if (channel == null)
							{
							}
							//break;
							int reason_code = buf.GetInt();
							//foo=buf.getString();  // additional textual information
							//foo=buf.getString();  // language tag 
							channel.SetExitStatus(reason_code);
							channel.close = true;
							channel.eof_remote = true;
							channel.SetRecipient(0);
							break;
						}

						case SSH_MSG_CHANNEL_REQUEST:
						{
							buf.GetInt();
							buf.GetShort();
							i = buf.GetInt();
							foo = buf.GetString();
							bool reply = (buf.GetByte() != 0);
							channel = Channel.GetChannel(i, this);
							if (channel != null)
							{
								byte reply_type = unchecked((byte)SSH_MSG_CHANNEL_FAILURE);
								if ((Util.Byte2str(foo)).Equals("exit-status"))
								{
									i = buf.GetInt();
									// exit-status
									channel.SetExitStatus(i);
									reply_type = unchecked((byte)SSH_MSG_CHANNEL_SUCCESS);
								}
								if (reply)
								{
									packet.Reset();
									buf.PutByte(reply_type);
									buf.PutInt(channel.GetRecipient());
									Write(packet);
								}
							}
							break;
						}

						case SSH_MSG_CHANNEL_OPEN:
						{
							buf.GetInt();
							buf.GetShort();
							foo = buf.GetString();
							string ctyp = Util.Byte2str(foo);
							if (!"forwarded-tcpip".Equals(ctyp) && !("x11".Equals(ctyp) && x11_forwarding) &&
								 !("auth-agent@openssh.com".Equals(ctyp) && agent_forwarding))
							{
								//System.err.println("Session.run: CHANNEL OPEN "+ctyp); 
								//throw new IOException("Session.run: CHANNEL OPEN "+ctyp);
								packet.Reset();
								buf.PutByte(unchecked((byte)SSH_MSG_CHANNEL_OPEN_FAILURE));
								buf.PutInt(buf.GetInt());
								buf.PutInt(Channel.SSH_OPEN_ADMINISTRATIVELY_PROHIBITED);
								buf.PutString(Util.empty);
								buf.PutString(Util.empty);
								Write(packet);
							}
							else
							{
								channel = Channel.GetChannel(ctyp);
								AddChannel(channel);
								channel.GetData(buf);
								channel.Init();
								Sharpen.Thread tmp = new Sharpen.Thread(channel);
								tmp.SetName("Channel " + ctyp + " " + host);
								if (daemon_thread)
								{
									tmp.SetDaemon(daemon_thread);
								}
								tmp.Start();
								break;
							}
							goto case SSH_MSG_CHANNEL_SUCCESS;
						}

						case SSH_MSG_CHANNEL_SUCCESS:
						{
							buf.GetInt();
							buf.GetShort();
							i = buf.GetInt();
							channel = Channel.GetChannel(i, this);
							if (channel == null)
							{
								break;
							}
							channel.reply = 1;
							break;
						}

						case SSH_MSG_CHANNEL_FAILURE:
						{
							buf.GetInt();
							buf.GetShort();
							i = buf.GetInt();
							channel = Channel.GetChannel(i, this);
							if (channel == null)
							{
								break;
							}
							channel.reply = 0;
							break;
						}

						case SSH_MSG_GLOBAL_REQUEST:
						{
							buf.GetInt();
							buf.GetShort();
							foo = buf.GetString();
							// request name
							bool reply = (buf.GetByte() != 0);
							if (reply)
							{
								packet.Reset();
								buf.PutByte(unchecked((byte)SSH_MSG_REQUEST_FAILURE));
								Write(packet);
							}
							break;
						}

						case SSH_MSG_REQUEST_FAILURE:
						case SSH_MSG_REQUEST_SUCCESS:
						{
							Sharpen.Thread t = grr.GetThread();
							if (t != null)
							{
								grr.SetReply(msgType == SSH_MSG_REQUEST_SUCCESS ? 1 : 0);
								t.Interrupt();
							}
							break;
						}

						default:
						{
							//System.err.println("Session.run: unsupported type "+msgType); 
							throw new IOException("Unknown SSH message type " + msgType);
						}
					}
				}
			}
			catch (Exception e)
			{
				in_kex = false;
				if (JSch.GetLogger().IsEnabled(Logger.INFO))
				{
					JSch.GetLogger().Log(Logger.INFO, "Caught an exception, leaving main loop due to "
						 + e.Message);
				}
			}
			//System.err.println("# Session.run");
			//e.printStackTrace();
			try
			{
				Disconnect();
			}
			catch (ArgumentNullException)
			{
			}
			catch (Exception)
			{
			}
			//System.err.println("@1");
			//e.printStackTrace();
			//System.err.println("@2");
			//e.printStackTrace();
			isConnected = false;
		}

		public virtual void Disconnect()
		{
			if (!isConnected)
			{
				return;
			}
			//System.err.println(this+": disconnect");
			//Thread.dumpStack();
			if (JSch.GetLogger().IsEnabled(Logger.INFO))
			{
				JSch.GetLogger().Log(Logger.INFO, "Disconnecting from " + host + " port " + port);
			}
			Channel.Disconnect(this);
			isConnected = false;
			PortWatcher.DelPort(this);
			ChannelForwardedTCPIP.DelPort(this);
			ChannelX11.RemoveFakedCookie(this);
			lock (Lock)
			{
				if (connectThread != null)
				{
					Sharpen.Thread.Yield();
					connectThread.Interrupt();
					connectThread = null;
				}
			}
			thread = null;
			try
			{
				if (io != null)
				{
					if (io.@in != null)
					{
						io.@in.Close();
					}
					if (io.@out != null)
					{
						io.@out.Close();
					}
					if (io.out_ext != null)
					{
						io.out_ext.Close();
					}
				}
				if (proxy == null)
				{
					if (socket != null)
					{
						socket.Close();
					}
				}
				else
				{
					lock (proxy)
					{
						proxy.Close();
					}
					proxy = null;
				}
			}
			catch (Exception)
			{
			}
			//      e.printStackTrace();
			io = null;
			socket = null;
			//    synchronized(jsch.pool){
			//      jsch.pool.removeElement(this);
			//    }
			jsch.RemoveSession(this);
		}

		//System.gc();
		/// <exception cref="NSch.JSchException"></exception>
		public virtual int SetPortForwardingL(int lport, string host, int rport)
		{
			return SetPortForwardingL("127.0.0.1", lport, host, rport);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual int SetPortForwardingL(string boundaddress, int lport, string host
			, int rport)
		{
			return SetPortForwardingL(boundaddress, lport, host, rport, null);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual int SetPortForwardingL(string boundaddress, int lport, string host
			, int rport, ServerSocketFactory ssf)
		{
			PortWatcher pw = PortWatcher.AddPort(this, boundaddress, lport, host, rport, ssf);
			Sharpen.Thread tmp = new Sharpen.Thread(pw);
			tmp.SetName("PortWatcher Thread for " + host);
			if (daemon_thread)
			{
				tmp.SetDaemon(daemon_thread);
			}
			tmp.Start();
			return pw.lport;
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void DelPortForwardingL(int lport)
		{
			DelPortForwardingL("127.0.0.1", lport);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void DelPortForwardingL(string boundaddress, int lport)
		{
			PortWatcher.DelPort(this, boundaddress, lport);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual string[] GetPortForwardingL()
		{
			return PortWatcher.GetPortForwarding(this);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void SetPortForwardingR(int rport, string host, int lport)
		{
			SetPortForwardingR(null, rport, host, lport, (SocketFactory)null);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void SetPortForwardingR(string bind_address, int rport, string host
			, int lport)
		{
			SetPortForwardingR(bind_address, rport, host, lport, (SocketFactory)null);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void SetPortForwardingR(int rport, string host, int lport, SocketFactory
			 sf)
		{
			SetPortForwardingR(null, rport, host, lport, sf);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void SetPortForwardingR(string bind_address, int rport, string host
			, int lport, SocketFactory sf)
		{
			ChannelForwardedTCPIP.AddPort(this, bind_address, rport, host, lport, sf);
			SetPortForwarding(bind_address, rport);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void SetPortForwardingR(int rport, string daemon)
		{
			SetPortForwardingR(null, rport, daemon, null);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void SetPortForwardingR(int rport, string daemon, object[] arg)
		{
			SetPortForwardingR(null, rport, daemon, arg);
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void SetPortForwardingR(string bind_address, int rport, string daemon
			, object[] arg)
		{
			ChannelForwardedTCPIP.AddPort(this, bind_address, rport, daemon, arg);
			SetPortForwarding(bind_address, rport);
		}

		private class GlobalRequestReply
		{
			private Sharpen.Thread thread = null;

			private int reply = -1;

			internal virtual void SetThread(Sharpen.Thread thread)
			{
				this.thread = thread;
				this.reply = -1;
			}

			internal virtual Sharpen.Thread GetThread()
			{
				return this.thread;
			}

			internal virtual void SetReply(int reply)
			{
				this.reply = reply;
			}

			internal virtual int GetReply()
			{
				return this.reply;
			}

			internal GlobalRequestReply(Session _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly Session _enclosing;
		}

		private Session.GlobalRequestReply grr;

		/// <exception cref="NSch.JSchException"></exception>
		private void SetPortForwarding(string bind_address, int rport)
		{
			lock (grr)
			{
				Buffer buf = new Buffer(100);
				// ??
				Packet packet = new Packet(buf);
				string address_to_bind = ChannelForwardedTCPIP.Normalize(bind_address);
				grr.SetThread(Sharpen.Thread.CurrentThread());
				try
				{
					// byte SSH_MSG_GLOBAL_REQUEST 80
					// string "tcpip-forward"
					// boolean want_reply
					// string  address_to_bind
					// uint32  port number to bind
					packet.Reset();
					buf.PutByte(unchecked((byte)SSH_MSG_GLOBAL_REQUEST));
					buf.PutString(Util.Str2byte("tcpip-forward"));
					buf.PutByte(unchecked((byte)1));
					buf.PutString(Util.Str2byte(address_to_bind));
					buf.PutInt(rport);
					Write(packet);
				}
				catch (Exception e)
				{
					grr.SetThread(null);
					if (e is Exception)
					{
						throw new JSchException(e.ToString(), (Exception)e);
					}
					throw new JSchException(e.ToString());
				}
				int count = 0;
				int reply = grr.GetReply();
				while (count < 10 && reply == -1)
				{
					try
					{
						Sharpen.Thread.Sleep(1000);
					}
					catch (Exception)
					{
					}
					count++;
					reply = grr.GetReply();
				}
				grr.SetThread(null);
				if (reply != 1)
				{
					throw new JSchException("remote port forwarding failed for listen port " + rport);
				}
			}
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void DelPortForwardingR(int rport)
		{
			ChannelForwardedTCPIP.DelPort(this, rport);
		}

		/// <exception cref="NSch.JSchException"></exception>
		private void InitDeflater(string method)
		{
			if (method.Equals("none"))
			{
				deflater = null;
				return;
			}
			string foo = GetConfig(method);
			if (foo != null)
			{
				if (method.Equals("zlib") || (isAuthed && method.Equals("zlib@openssh.com")))
				{
					try
					{
						Type c = Sharpen.Runtime.GetType(foo);
						deflater = (Compression)(System.Activator.CreateInstance(c));
						int level = 6;
						try
						{
							level = System.Convert.ToInt32(GetConfig("compression_level"));
						}
						catch (Exception)
						{
						}
						deflater.Init(Compression.DEFLATER, level);
					}
					catch (Exception ee)
					{
						throw new JSchException(ee.ToString(), ee);
					}
				}
			}
		}

		//System.err.println(foo+" isn't accessible.");
		/// <exception cref="NSch.JSchException"></exception>
		private void InitInflater(string method)
		{
			if (method.Equals("none"))
			{
				inflater = null;
				return;
			}
			string foo = GetConfig(method);
			if (foo != null)
			{
				if (method.Equals("zlib") || (isAuthed && method.Equals("zlib@openssh.com")))
				{
					try
					{
						Type c = Sharpen.Runtime.GetType(foo);
						inflater = (Compression)(System.Activator.CreateInstance(c));
						inflater.Init(Compression.INFLATER, 0);
					}
					catch (Exception ee)
					{
						throw new JSchException(ee.ToString(), ee);
					}
				}
			}
		}

		//System.err.println(foo+" isn't accessible.");
		internal virtual void AddChannel(Channel channel)
		{
			channel.SetSession(this);
		}

		public virtual void SetProxy(Proxy proxy)
		{
			this.proxy = proxy;
		}

		public virtual void SetHost(string host)
		{
			this.host = host;
		}

		public virtual void SetPort(int port)
		{
			this.port = port;
		}

		internal virtual void SetUserName(string username)
		{
			this.username = username;
		}

		public virtual void SetUserInfo(UserInfo userinfo)
		{
			this.userinfo = userinfo;
		}

		public virtual UserInfo GetUserInfo()
		{
			return userinfo;
		}

		public virtual void SetInputStream(InputStream @in)
		{
			this.@in = @in;
		}

		public virtual void SetOutputStream(OutputStream @out)
		{
			this.@out = @out;
		}

		public virtual void SetX11Host(string host)
		{
			ChannelX11.SetHost(host);
		}

		public virtual void SetX11Port(int port)
		{
			ChannelX11.SetPort(port);
		}

		public virtual void SetX11Cookie(string cookie)
		{
			ChannelX11.SetCookie(cookie);
		}

		public virtual void SetPassword(string password)
		{
			if (password != null)
			{
				this.password = Util.Str2byte(password);
			}
		}

		public virtual void SetPassword(byte[] password)
		{
			if (password != null)
			{
				this.password = new byte[password.Length];
				System.Array.Copy(password, 0, this.password, 0, password.Length);
			}
		}

		public virtual void SetConfig(Properties newconf)
		{
			SetConfig((Hashtable)newconf);
		}

		public virtual void SetConfig(Hashtable newconf)
		{
			lock (Lock)
			{
				if (config == null)
				{
					config = new Hashtable();
				}
				foreach (var e in newconf.Keys)
				{
					string key = (string)(e);
					config.Put(key, (string)(newconf[key]));
				}
			}
		}

		public virtual void SetConfig(string key, string value)
		{
			lock (Lock)
			{
				if (config == null)
				{
					config = new Hashtable();
				}
				config.Put(key, value);
			}
		}

		public virtual string GetConfig(string key)
		{
			object foo = null;
			if (config != null)
			{
				foo = config[key];
				if (foo is string)
				{
					return (string)foo;
				}
			}
			foo = JSch.GetConfig(key);
			if (foo is string)
			{
				return (string)foo;
			}
			return null;
		}

		public virtual void SetSocketFactory(SocketFactory sfactory)
		{
			socket_factory = sfactory;
		}

		public virtual bool IsConnected()
		{
			return isConnected;
		}

		public virtual int GetTimeout()
		{
			return timeout;
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void SetTimeout(int timeout)
		{
			if (socket == null)
			{
				if (timeout < 0)
				{
					throw new JSchException("invalid timeout value");
				}
				this.timeout = timeout;
				return;
			}
			try
			{
				socket.ReceiveTimeout = timeout;
				this.timeout = timeout;
			}
			catch (Exception e)
			{
				if (e is Exception)
				{
					throw new JSchException(e.ToString(), (Exception)e);
				}
				throw new JSchException(e.ToString());
			}
		}

		public virtual string GetServerVersion()
		{
			return Util.Byte2str(V_S);
		}

		public virtual string GetClientVersion()
		{
			return Util.Byte2str(V_C);
		}

		public virtual void SetClientVersion(string cv)
		{
			V_C = Util.Str2byte(cv);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void SendIgnore()
		{
			Buffer buf = new Buffer();
			Packet packet = new Packet(buf);
			packet.Reset();
			buf.PutByte(unchecked((byte)SSH_MSG_IGNORE));
			Write(packet);
		}

		private static readonly byte[] keepalivemsg = Util.Str2byte("keepalive@jcraft.com"
			);

		/// <exception cref="System.Exception"></exception>
		public virtual void SendKeepAliveMsg()
		{
			Buffer buf = new Buffer();
			Packet packet = new Packet(buf);
			packet.Reset();
			buf.PutByte(unchecked((byte)SSH_MSG_GLOBAL_REQUEST));
			buf.PutString(keepalivemsg);
			buf.PutByte(unchecked((byte)1));
			Write(packet);
		}

		private HostKey hostkey = null;

		public virtual HostKey GetHostKey()
		{
			return hostkey;
		}

		public virtual string GetHost()
		{
			return host;
		}

		public virtual string GetUserName()
		{
			return username;
		}

		public virtual int GetPort()
		{
			return port;
		}

		public virtual void SetHostKeyAlias(string hostKeyAlias)
		{
			this.hostKeyAlias = hostKeyAlias;
		}

		public virtual string GetHostKeyAlias()
		{
			return hostKeyAlias;
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void SetServerAliveInterval(int interval)
		{
			SetTimeout(interval);
			this.serverAliveInterval = interval;
		}

		public virtual void SetServerAliveCountMax(int count)
		{
			this.serverAliveCountMax = count;
		}

		public virtual int GetServerAliveInterval()
		{
			return this.serverAliveInterval;
		}

		public virtual int GetServerAliveCountMax()
		{
			return this.serverAliveCountMax;
		}

		public virtual void SetDaemonThread(bool enable)
		{
			this.daemon_thread = enable;
		}

		private string[] CheckCiphers(string ciphers)
		{
			if (ciphers == null || ciphers.Length == 0)
			{
				return null;
			}
			if (JSch.GetLogger().IsEnabled(Logger.INFO))
			{
				JSch.GetLogger().Log(Logger.INFO, "CheckCiphers: " + ciphers);
			}
			ArrayList result = new ArrayList();
			string[] _ciphers = Util.Split(ciphers, ",");
			for (int i = 0; i < _ciphers.Length; i++)
			{
				if (!CheckCipher(GetConfig(_ciphers[i])))
				{
					result.Add(_ciphers[i]);
				}
			}
			if (result.Count == 0)
			{
				return null;
			}
			string[] foo = new string[result.Count];
			System.Array.Copy(Sharpen.Collections.ToArray(result), 0, foo, 0, result.Count);
			if (JSch.GetLogger().IsEnabled(Logger.INFO))
			{
				for (int i_1 = 0; i_1 < foo.Length; i_1++)
				{
					JSch.GetLogger().Log(Logger.INFO, foo[i_1] + " is not available.");
				}
			}
			return foo;
		}

		internal static bool CheckCipher(string cipher)
		{
			try
			{
				Type c = Sharpen.Runtime.GetType(cipher);
				NSch.Cipher _c = (NSch.Cipher)(System.Activator.CreateInstance(c));
				_c.Init(NSch.Cipher.ENCRYPT_MODE, new byte[_c.GetBlockSize()], new byte[_c.GetIVSize
					()]);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private string[] CheckKexes(string kexes)
		{
			if (kexes == null || kexes.Length == 0)
			{
				return null;
			}
			if (JSch.GetLogger().IsEnabled(Logger.INFO))
			{
				JSch.GetLogger().Log(Logger.INFO, "CheckKexes: " + kexes);
			}
			ArrayList result = new ArrayList();
			string[] _kexes = Util.Split(kexes, ",");
			for (int i = 0; i < _kexes.Length; i++)
			{
				if (!CheckKex(this, GetConfig(_kexes[i])))
				{
					result.Add(_kexes[i]);
				}
			}
			if (result.Count == 0)
			{
				return null;
			}
			string[] foo = new string[result.Count];
			System.Array.Copy(Sharpen.Collections.ToArray(result), 0, foo, 0, result.Count);
			if (JSch.GetLogger().IsEnabled(Logger.INFO))
			{
				for (int i_1 = 0; i_1 < foo.Length; i_1++)
				{
					JSch.GetLogger().Log(Logger.INFO, foo[i_1] + " is not available.");
				}
			}
			return foo;
		}

		internal static bool CheckKex(Session s, string kex)
		{
			try
			{
				Type c = Sharpen.Runtime.GetType(kex);
				KeyExchange _c = (KeyExchange)(System.Activator.CreateInstance(c));
				_c.Init(s, null, null, null, null);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
