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
using System.Net;
using System.Net.Sockets;
using NSch;
using Sharpen;

namespace NSch
{
	public class ProxySOCKS4 : Proxy
	{
		private static int DEFAULTPORT = 1080;

		private string proxy_host;

		private int proxy_port;

		private InputStream @in;

		private OutputStream @out;

		private Socket socket;

		private string user;

		private string passwd;

		public ProxySOCKS4(string proxy_host)
		{
			int port = DEFAULTPORT;
			string host = proxy_host;
			if (proxy_host.IndexOf(':') != -1)
			{
				try
				{
					host = Sharpen.Runtime.Substring(proxy_host, 0, proxy_host.IndexOf(':'));
					port = System.Convert.ToInt32(Sharpen.Runtime.Substring(proxy_host, proxy_host.IndexOf
						(':') + 1));
				}
				catch (Exception)
				{
				}
			}
			this.proxy_host = host;
			this.proxy_port = port;
		}

		public ProxySOCKS4(string proxy_host, int proxy_port)
		{
			this.proxy_host = proxy_host;
			this.proxy_port = proxy_port;
		}

		public virtual void SetUserPasswd(string user, string passwd)
		{
			this.user = user;
			this.passwd = passwd;
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void Connect(SocketFactory socket_factory, string host, int port, 
			int timeout)
		{
			try
			{
				if (socket_factory == null)
				{
					socket = Util.CreateSocket(proxy_host, proxy_port, timeout);
					//socket=new Socket(proxy_host, proxy_port);    
					@in = socket.GetInputStream();
					@out = socket.GetOutputStream();
				}
				else
				{
					socket = socket_factory.CreateSocket(proxy_host, proxy_port);
					@in = socket_factory.GetInputStream(socket);
					@out = socket_factory.GetOutputStream(socket);
				}
				if (timeout > 0)
				{
					socket.ReceiveTimeout = timeout;
				}
				socket.NoDelay = true;
				byte[] buf = new byte[1024];
				int index = 0;
				index = 0;
				buf[index++] = 4;
				buf[index++] = 1;
				buf[index++] = unchecked((byte)((int)(((uint)port) >> 8)));
				buf[index++] = unchecked((byte)(port & unchecked((int)(0xff))));
				try
				{
					IPAddress addr = Sharpen.Extensions.GetAddressByName(host);
					byte[] byteAddress = addr.GetAddressBytes();
					for (int i = 0; i < byteAddress.Length; i++)
					{
						buf[index++] = byteAddress[i];
					}
				}
				catch (UnknownHostException uhe)
				{
					throw new JSchException("ProxySOCKS4: " + uhe.ToString(), uhe);
				}
				if (user != null)
				{
					System.Array.Copy(Util.Str2byte(user), 0, buf, index, user.Length);
					index += user.Length;
				}
				buf[index++] = 0;
				@out.Write(buf, 0, index);
				int len = 8;
				int s = 0;
				while (s < len)
				{
					int i = @in.Read(buf, s, len - s);
					if (i <= 0)
					{
						throw new JSchException("ProxySOCKS4: stream is closed");
					}
					s += i;
				}
				if (buf[0] != 0)
				{
					throw new JSchException("ProxySOCKS4: server returns VN " + buf[0]);
				}
				if (buf[1] != 90)
				{
					try
					{
						socket.Close();
					}
					catch (Exception)
					{
					}
					string message = "ProxySOCKS4: server returns CD " + buf[1];
					throw new JSchException(message);
				}
			}
			catch (RuntimeException e)
			{
				throw;
			}
			catch (Exception e)
			{
				try
				{
					if (socket != null)
					{
						socket.Close();
					}
				}
				catch (Exception)
				{
				}
				throw new JSchException("ProxySOCKS4: " + e.ToString());
			}
		}

		public virtual InputStream GetInputStream()
		{
			return @in;
		}

		public virtual OutputStream GetOutputStream()
		{
			return @out;
		}

		public virtual Socket GetSocket()
		{
			return socket;
		}

		public virtual void Close()
		{
			try
			{
				if (@in != null)
				{
					@in.Close();
				}
				if (@out != null)
				{
					@out.Close();
				}
				if (socket != null)
				{
					socket.Close();
				}
			}
			catch (Exception)
			{
			}
			@in = null;
			@out = null;
			socket = null;
		}

		public static int GetDefaultPort()
		{
			return DEFAULTPORT;
		}
	}
}
