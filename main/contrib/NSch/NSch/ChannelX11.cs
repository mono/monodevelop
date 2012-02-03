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
using System.Net.Sockets;
using NSch;
using Sharpen;

namespace NSch
{
	internal class ChannelX11 : Channel
	{
		private const int LOCAL_WINDOW_SIZE_MAX = unchecked((int)(0x20000));

		private const int LOCAL_MAXIMUM_PACKET_SIZE = unchecked((int)(0x4000));

		private const int TIMEOUT = 10 * 1000;

		private static string host = "127.0.0.1";

		private static int port = 6000;

		private bool init = true;

		internal static byte[] cookie = null;

		private static byte[] cookie_hex = null;

		private static Hashtable faked_cookie_pool = new Hashtable();

		private static Hashtable faked_cookie_hex_pool = new Hashtable();

		private static byte[] table = new byte[] { unchecked((int)(0x30)), unchecked((int
			)(0x31)), unchecked((int)(0x32)), unchecked((int)(0x33)), unchecked((int)(0x34))
			, unchecked((int)(0x35)), unchecked((int)(0x36)), unchecked((int)(0x37)), unchecked(
			(int)(0x38)), unchecked((int)(0x39)), unchecked((int)(0x61)), unchecked((int)(0x62
			)), unchecked((int)(0x63)), unchecked((int)(0x64)), unchecked((int)(0x65)), unchecked(
			(int)(0x66)) };

		private Socket socket = null;

		internal static int Revtable(byte foo)
		{
			for (int i = 0; i < table.Length; i++)
			{
				if (table[i] == foo)
				{
					return i;
				}
			}
			return 0;
		}

		internal static void SetCookie(string foo)
		{
			cookie_hex = Util.Str2byte(foo);
			cookie = new byte[16];
			for (int i = 0; i < 16; i++)
			{
				cookie[i] = unchecked((byte)(((Revtable(cookie_hex[i * 2]) << 4) & unchecked((int
					)(0xf0))) | ((Revtable(cookie_hex[i * 2 + 1])) & unchecked((int)(0xf)))));
			}
		}

		internal static void SetHost(string foo)
		{
			host = foo;
		}

		internal static void SetPort(int foo)
		{
			port = foo;
		}

		internal static byte[] GetFakedCookie(Session session)
		{
			lock (faked_cookie_hex_pool)
			{
				byte[] foo = (byte[])faked_cookie_hex_pool[session];
				if (foo == null)
				{
					Random random = Session.random;
					foo = new byte[16];
					lock (random)
					{
						random.Fill(foo, 0, 16);
					}
					faked_cookie_pool.Put(session, foo);
					byte[] bar = new byte[32];
					for (int i = 0; i < 16; i++)
					{
						bar[2 * i] = table[(foo[i] >> 4) & unchecked((int)(0xf))];
						bar[2 * i + 1] = table[(foo[i]) & unchecked((int)(0xf))];
					}
					faked_cookie_hex_pool.Put(session, bar);
					foo = bar;
				}
				return foo;
			}
		}

		public ChannelX11() : base()
		{
			SetLocalWindowSizeMax(LOCAL_WINDOW_SIZE_MAX);
			SetLocalWindowSize(LOCAL_WINDOW_SIZE_MAX);
			SetLocalPacketSize(LOCAL_MAXIMUM_PACKET_SIZE);
			type = Util.Str2byte("x11");
			connected = true;
		}

		public override void Run()
		{
			try
			{
				socket = Util.CreateSocket(host, port, TIMEOUT);
				socket.NoDelay = true;
				io = new IO();
				io.SetInputStream(socket.GetInputStream());
				io.SetOutputStream(socket.GetOutputStream());
				SendOpenConfirmation();
			}
			catch (Exception)
			{
				SendOpenFailure(SSH_OPEN_ADMINISTRATIVELY_PROHIBITED);
				close = true;
				Disconnect();
				return;
			}
			thread = Sharpen.Thread.CurrentThread();
			Buffer buf = new Buffer(rmpsize);
			Packet packet = new Packet(buf);
			int i = 0;
			try
			{
				while (thread != null && io != null && io.@in != null)
				{
					i = io.@in.Read(buf.buffer, 14, buf.buffer.Length - 14 - 32 - 20);
					// padding and mac
					if (i <= 0)
					{
						Eof();
						break;
					}
					if (close)
					{
						break;
					}
					packet.Reset();
					buf.PutByte(unchecked((byte)Session.SSH_MSG_CHANNEL_DATA));
					buf.PutInt(recipient);
					buf.PutInt(i);
					buf.Skip(i);
					GetSession().Write(packet, this, i);
				}
			}
			catch (Exception)
			{
			}
			//System.err.println(e);
			Disconnect();
		}

		private byte[] cache = new byte[0];

		private byte[] AddCache(byte[] foo, int s, int l)
		{
			byte[] bar = new byte[cache.Length + l];
			System.Array.Copy(foo, s, bar, cache.Length, l);
			if (cache.Length > 0)
			{
				System.Array.Copy(cache, 0, bar, 0, cache.Length);
			}
			cache = bar;
			return cache;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override void Write(byte[] foo, int s, int l)
		{
			//if(eof_local)return;
			if (init)
			{
				Session _session = null;
				try
				{
					_session = GetSession();
				}
				catch (JSchException e)
				{
					throw new IOException(e.ToString());
				}
				foo = AddCache(foo, s, l);
				s = 0;
				l = foo.Length;
				if (l < 9)
				{
					return;
				}
				int plen = (foo[s + 6] & unchecked((int)(0xff))) * 256 + (foo[s + 7] & unchecked(
					(int)(0xff)));
				int dlen = (foo[s + 8] & unchecked((int)(0xff))) * 256 + (foo[s + 9] & unchecked(
					(int)(0xff)));
				if ((foo[s] & unchecked((int)(0xff))) == unchecked((int)(0x42)))
				{
				}
				else
				{
					if ((foo[s] & unchecked((int)(0xff))) == unchecked((int)(0x6c)))
					{
						plen = (((int)(((uint)plen) >> 8)) & unchecked((int)(0xff))) | ((plen << 8) & unchecked(
							(int)(0xff00)));
						dlen = (((int)(((uint)dlen) >> 8)) & unchecked((int)(0xff))) | ((dlen << 8) & unchecked(
							(int)(0xff00)));
					}
				}
				// ??
				if (l < 12 + plen + ((-plen) & 3) + dlen)
				{
					return;
				}
				byte[] bar = new byte[dlen];
				System.Array.Copy(foo, s + 12 + plen + ((-plen) & 3), bar, 0, dlen);
				byte[] faked_cookie = null;
				lock (faked_cookie_pool)
				{
					faked_cookie = (byte[])faked_cookie_pool[_session];
				}
				if (Equals(bar, faked_cookie))
				{
					if (cookie != null)
					{
						System.Array.Copy(cookie, 0, foo, s + 12 + plen + ((-plen) & 3), dlen);
					}
				}
				else
				{
					//System.err.println("wrong cookie");
					thread = null;
					Eof();
					io.Close();
					Disconnect();
				}
				init = false;
				io.Put(foo, s, l);
				cache = null;
				return;
			}
			io.Put(foo, s, l);
		}

		private static bool Equals(byte[] foo, byte[] bar)
		{
			if (foo.Length != bar.Length)
			{
				return false;
			}
			for (int i = 0; i < foo.Length; i++)
			{
				if (foo[i] != bar[i])
				{
					return false;
				}
			}
			return true;
		}
	}
}
