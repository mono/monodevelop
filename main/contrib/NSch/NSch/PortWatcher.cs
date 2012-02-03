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
using System.Net;
using System.Net.Sockets;
using NSch;
using Sharpen;

namespace NSch
{
	internal class PortWatcher : Runnable
	{
		private static ArrayList pool = new ArrayList();

		private static IPAddress anyLocalAddress = null;

		static PortWatcher()
		{
			// 0.0.0.0
			try
			{
				anyLocalAddress = Sharpen.Extensions.GetAddressByName("0.0.0.0");
			}
			catch (UnknownHostException)
			{
			}
		}

		internal Session session;

		internal int lport;

		internal int rport;

		internal string host;

		internal IPAddress boundaddress;

		internal Runnable thread;

		internal Socket ss;

		internal static string[] GetPortForwarding(Session session)
		{
			ArrayList foo = new ArrayList();
			lock (pool)
			{
				for (int i = 0; i < pool.Count; i++)
				{
					NSch.PortWatcher p = (NSch.PortWatcher)(pool[i]);
					if (p.session == session)
					{
						foo.Add(p.lport + ":" + p.host + ":" + p.rport);
					}
				}
			}
			string[] bar = new string[foo.Count];
			for (int i_1 = 0; i_1 < foo.Count; i_1++)
			{
				bar[i_1] = (string)(foo[i_1]);
			}
			return bar;
		}

		/// <exception cref="NSch.JSchException"></exception>
		internal static NSch.PortWatcher GetPort(Session session, string address, int lport
			)
		{
			IPAddress addr;
			try
			{
				addr = Sharpen.Extensions.GetAddressByName(address);
			}
			catch (UnknownHostException uhe)
			{
				throw new JSchException("PortForwardingL: invalid address " + address + " specified."
					, uhe);
			}
			lock (pool)
			{
				for (int i = 0; i < pool.Count; i++)
				{
					NSch.PortWatcher p = (NSch.PortWatcher)(pool[i]);
					if (p.session == session && p.lport == lport)
					{
						if ((anyLocalAddress != null && p.boundaddress.Equals(anyLocalAddress)) || p.boundaddress
							.Equals(addr))
						{
							return p;
						}
					}
				}
				return null;
			}
		}

		/// <exception cref="NSch.JSchException"></exception>
		internal static NSch.PortWatcher AddPort(Session session, string address, int lport
			, string host, int rport, ServerSocketFactory ssf)
		{
			if (GetPort(session, address, lport) != null)
			{
				throw new JSchException("PortForwardingL: local port " + address + ":" + lport + 
					" is already registered.");
			}
			NSch.PortWatcher pw = new NSch.PortWatcher(session, address, lport, host, rport, 
				ssf);
			pool.Add(pw);
			return pw;
		}

		/// <exception cref="NSch.JSchException"></exception>
		internal static void DelPort(Session session, string address, int lport)
		{
			NSch.PortWatcher pw = GetPort(session, address, lport);
			if (pw == null)
			{
				throw new JSchException("PortForwardingL: local port " + address + ":" + lport + 
					" is not registered.");
			}
			pw.Delete();
			pool.RemoveElement(pw);
		}

		internal static void DelPort(Session session)
		{
			lock (pool)
			{
				NSch.PortWatcher[] foo = new NSch.PortWatcher[pool.Count];
				int count = 0;
				for (int i = 0; i < pool.Count; i++)
				{
					NSch.PortWatcher p = (NSch.PortWatcher)(pool[i]);
					if (p.session == session)
					{
						p.Delete();
						foo[count++] = p;
					}
				}
				for (int i_1 = 0; i_1 < count; i_1++)
				{
					NSch.PortWatcher p = foo[i_1];
					pool.RemoveElement(p);
				}
			}
		}

		/// <exception cref="NSch.JSchException"></exception>
		internal PortWatcher(Session session, string address, int lport, string host, int
			 rport, ServerSocketFactory factory)
		{
			this.session = session;
			this.lport = lport;
			this.host = host;
			this.rport = rport;
			try
			{
				boundaddress = Sharpen.Extensions.GetAddressByName(address);
				ss = (factory == null) ? Sharpen.Extensions.CreateServerSocket(lport, 0, boundaddress
					) : factory.CreateServerSocket(lport, 0, boundaddress);
			}
			catch (Exception e)
			{
				//System.err.println(e);
				string message = "PortForwardingL: local port " + address + ":" + lport + " cannot be bound.";
				if (e is Exception)
				{
					throw new JSchException(message, (Exception)e);
				}
				throw new JSchException(message);
			}
			if (lport == 0)
			{
				int assigned = ss.GetLocalPort();
				if (assigned != -1)
				{
					this.lport = assigned;
				}
			}
		}

		public virtual void Run()
		{
			thread = this;
			try
			{
				while (thread != null)
				{
					Socket socket = ss.Accept();
					socket.NoDelay = true;
					InputStream @in = socket.GetInputStream();
					OutputStream @out = socket.GetOutputStream();
					ChannelDirectTCPIP channel = new ChannelDirectTCPIP();
					channel.Init();
					channel.SetInputStream(@in);
					channel.SetOutputStream(@out);
					session.AddChannel(channel);
					((ChannelDirectTCPIP)channel).SetHost(host);
					((ChannelDirectTCPIP)channel).SetPort(rport);
					((ChannelDirectTCPIP)channel).SetOrgIPAddress(socket.GetInetAddress().GetHostAddress
						());
					((ChannelDirectTCPIP)channel).SetOrgPort(socket.GetPort());
					channel.Connect();
					if (channel.exitstatus != -1)
					{
					}
				}
			}
			catch (Exception)
			{
			}
			//System.err.println("! "+e);
			Delete();
		}

		internal virtual void Delete()
		{
			thread = null;
			try
			{
				if (ss != null)
				{
					ss.Close();
				}
				ss = null;
			}
			catch (Exception)
			{
			}
		}
	}
}
