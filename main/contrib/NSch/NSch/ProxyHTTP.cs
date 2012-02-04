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
using System.IO;
using System.Net.Sockets;
using System.Text;
using NSch;
using Sharpen;

namespace NSch
{
	public class ProxyHTTP : Proxy
	{
		private static int DEFAULTPORT = 80;

		private string proxy_host;

		private int proxy_port;

		private InputStream @in;

		private OutputStream @out;

		private Socket socket;

		private string user;

		private string passwd;

		public ProxyHTTP(string proxy_host)
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

		public ProxyHTTP(string proxy_host, int proxy_port)
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
				@out.Write(Util.Str2byte("CONNECT " + host + ":" + port + " HTTP/1.0\r\n"));
				if (user != null && passwd != null)
				{
					byte[] code = Util.Str2byte(user + ":" + passwd);
					code = Util.ToBase64(code, 0, code.Length);
					@out.Write(Util.Str2byte("Proxy-Authorization: Basic "));
					@out.Write(code);
					@out.Write(Util.Str2byte("\r\n"));
				}
				@out.Write(Util.Str2byte("\r\n"));
				@out.Flush();
				int foo = 0;
				StringBuilder sb = new StringBuilder();
				while (foo >= 0)
				{
					foo = @in.Read();
					if (foo != 13)
					{
						sb.Append((char)foo);
						continue;
					}
					foo = @in.Read();
					if (foo != 10)
					{
						continue;
					}
					break;
				}
				if (foo < 0)
				{
					throw new IOException();
				}
				string response = sb.ToString();
				string reason = "Unknow reason";
				int code_1 = -1;
				try
				{
					foo = response.IndexOf(' ');
					int bar = response.IndexOf(' ', foo + 1);
					code_1 = System.Convert.ToInt32(Sharpen.Runtime.Substring(response, foo + 1, bar)
						);
					reason = Sharpen.Runtime.Substring(response, bar + 1);
				}
				catch (Exception)
				{
				}
				if (code_1 != 200)
				{
					throw new IOException("proxy error: " + reason);
				}
				int count = 0;
				while (true)
				{
					count = 0;
					while (foo >= 0)
					{
						foo = @in.Read();
						if (foo != 13)
						{
							count++;
							continue;
						}
						foo = @in.Read();
						if (foo != 10)
						{
							continue;
						}
						break;
					}
					if (foo < 0)
					{
						throw new IOException();
					}
					if (count == 0)
					{
						break;
					}
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
				string message = "ProxyHTTP: " + e.ToString();
				if (e is Exception)
				{
					throw new JSchException(message, (Exception)e);
				}
				throw new JSchException(message);
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
