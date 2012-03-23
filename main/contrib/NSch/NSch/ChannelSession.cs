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
using NSch;
using Sharpen;

namespace NSch
{
	public class ChannelSession : Channel
	{
		private static byte[] _session = Util.Str2byte("session");

		protected internal bool agent_forwarding = false;

		protected internal bool xforwading = false;

		protected internal Hashtable env = null;

		protected internal bool pty = false;

		protected internal string ttype = "vt100";

		protected internal int tcol = 80;

		protected internal int trow = 24;

		protected internal int twp = 640;

		protected internal int thp = 480;

		protected internal byte[] terminal_mode = null;

		public ChannelSession() : base()
		{
			type = _session;
			io = new IO();
		}

		/// <summary>Enable the agent forwarding.</summary>
		/// <remarks>Enable the agent forwarding.</remarks>
		/// <param name="enable"></param>
		public virtual void SetAgentForwarding(bool enable)
		{
			agent_forwarding = enable;
		}

		/// <summary>Enable the X11 forwarding.</summary>
		/// <remarks>Enable the X11 forwarding.</remarks>
		/// <param name="enable"></param>
		/// <seealso cref="RFC4254">6.3.1. Requesting X11 Forwarding</seealso>
		public override void SetXForwarding(bool enable)
		{
			xforwading = enable;
		}

		/// <seealso cref="SetEnv(string, string)">SetEnv(string, string)</seealso>
		/// <seealso cref="SetEnv(byte[], byte[])">SetEnv(byte[], byte[])</seealso>
		[System.ObsoleteAttribute(@"Use SetEnv(string, string) or SetEnv(byte[], byte[]) instead."
			)]
		public virtual void SetEnv(Hashtable env)
		{
			lock (this)
			{
				this.env = env;
			}
		}

		/// <summary>Set the environment variable.</summary>
		/// <remarks>
		/// Set the environment variable.
		/// If <code>name</code> and <code>value</code> are needed to be passed
		/// to the remote in your faivorite encoding,use
		/// <see cref="SetEnv(byte[], byte[])">SetEnv(byte[], byte[])</see>
		/// .
		/// </remarks>
		/// <param name="name">A name for environment variable.</param>
		/// <param name="value">A value for environment variable.</param>
		/// <seealso cref="RFC4254">6.4 Environment Variable Passing</seealso>
		public virtual void SetEnv(string name, string value)
		{
			SetEnv(Util.Str2byte(name), Util.Str2byte(value));
		}

		/// <summary>Set the environment variable.</summary>
		/// <remarks>Set the environment variable.</remarks>
		/// <param name="name">A name of environment variable.</param>
		/// <param name="value">A value of environment variable.</param>
		/// <seealso cref="SetEnv(string, string)">SetEnv(string, string)</seealso>
		/// <seealso cref="RFC4254">6.4 Environment Variable Passing</seealso>
		public virtual void SetEnv(byte[] name, byte[] value)
		{
			lock (this)
			{
				GetEnv().Put(name, value);
			}
		}

		private Hashtable GetEnv()
		{
			if (env == null)
			{
				env = new Hashtable();
			}
			return env;
		}

		/// <summary>Allocate a Pseudo-Terminal.</summary>
		/// <remarks>Allocate a Pseudo-Terminal.</remarks>
		/// <param name="enable"></param>
		/// <seealso cref="RFC4254">6.2. Requesting a Pseudo-Terminal</seealso>
		public virtual void SetPty(bool enable)
		{
			pty = enable;
		}

		/// <summary>Set the terminal mode.</summary>
		/// <remarks>Set the terminal mode.</remarks>
		/// <param name="terminal_mode"></param>
		public virtual void SetTerminalMode(byte[] terminal_mode)
		{
			this.terminal_mode = terminal_mode;
		}

		/// <summary>Change the window dimension interactively.</summary>
		/// <remarks>Change the window dimension interactively.</remarks>
		/// <param name="col">terminal width, columns</param>
		/// <param name="row">terminal height, rows</param>
		/// <param name="wp">terminal width, pixels</param>
		/// <param name="hp">terminal height, pixels</param>
		/// <seealso cref="RFC4254">6.7. Window Dimension Change Message</seealso>
		public virtual void SetPtySize(int col, int row, int wp, int hp)
		{
			SetPtyType(this.ttype, col, row, wp, hp);
			if (!pty || !IsConnected())
			{
				return;
			}
			try
			{
				RequestWindowChange request = new RequestWindowChange();
				request.SetSize(col, row, wp, hp);
				request.DoRequest(GetSession(), this);
			}
			catch (Exception)
			{
			}
		}

		//System.err.println("ChannelSessio.setPtySize: "+e);
		/// <summary>Set the terminal type.</summary>
		/// <remarks>
		/// Set the terminal type.
		/// This method is not effective after Channel#connect().
		/// </remarks>
		/// <param name="ttype">terminal type(for example, "vt100")</param>
		/// <seealso cref="SetPtyType(string, int, int, int, int)">SetPtyType(string, int, int, int, int)
		/// 	</seealso>
		public virtual void SetPtyType(string ttype)
		{
			SetPtyType(ttype, 80, 24, 640, 480);
		}

		/// <summary>Set the terminal type.</summary>
		/// <remarks>
		/// Set the terminal type.
		/// This method is not effective after Channel#connect().
		/// </remarks>
		/// <param name="ttype">terminal type(for example, "vt100")</param>
		/// <param name="col">terminal width, columns</param>
		/// <param name="row">terminal height, rows</param>
		/// <param name="wp">terminal width, pixels</param>
		/// <param name="hp">terminal height, pixels</param>
		public virtual void SetPtyType(string ttype, int col, int row, int wp, int hp)
		{
			this.ttype = ttype;
			this.tcol = col;
			this.trow = row;
			this.twp = wp;
			this.thp = hp;
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void SendRequests()
		{
			Session _session = GetSession();
			Request request;
			if (agent_forwarding)
			{
				request = new RequestAgentForwarding();
				request.DoRequest(_session, this);
			}
			if (xforwading)
			{
				request = new RequestX11();
				request.DoRequest(_session, this);
			}
			if (pty)
			{
				request = new RequestPtyReq();
				((RequestPtyReq)request).SetTType(ttype);
				((RequestPtyReq)request).SetTSize(tcol, trow, twp, thp);
				if (terminal_mode != null)
				{
					((RequestPtyReq)request).SetTerminalMode(terminal_mode);
				}
				request.DoRequest(_session, this);
			}
			if (env != null)
			{
				foreach (var v in env.Keys)
				{
					object name = v;
					object value = env[name];
					request = new RequestEnv();
					((RequestEnv)request).SetEnv(ToByteArray(name), ToByteArray(value));
					request.DoRequest(_session, this);
				}
			}
		}

		private byte[] ToByteArray(object o)
		{
			if (o is string)
			{
				return Util.Str2byte((string)o);
			}
			return (byte[])o;
		}

		public override void Run()
		{
			//System.err.println(this+":run >");
			Buffer buf = new Buffer(rmpsize);
			Packet packet = new Packet(buf);
			int i = -1;
			try
			{
				while (IsConnected() && thread != null && io != null && io.@in != null)
				{
					i = io.@in.Read(buf.buffer, 14, buf.buffer.Length - 14 - Session.buffer_margin);
					if (i == 0)
					{
						continue;
					}
					if (i == -1)
					{
						Eof();
						break;
					}
					if (close)
					{
						break;
					}
					//System.out.println("write: "+i);
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
			//System.err.println("# ChannelExec.run");
			//e.printStackTrace();
			Sharpen.Thread _thread = thread;
			if (_thread != null)
			{
				lock (_thread)
				{
					Sharpen.Runtime.NotifyAll(_thread);
				}
			}
			thread = null;
		}
		//System.err.println(this+":run <");
	}
}
