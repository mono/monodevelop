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
using NSch;
using Sharpen;

namespace NSch
{
	public class ChannelExec : ChannelSession
	{
		internal byte[] command = new byte[0];

		/// <exception cref="NSch.JSchException"></exception>
		public override void Start()
		{
			Session _session = GetSession();
			try
			{
				SendRequests();
				Request request = new RequestExec(command);
				request.DoRequest(_session, this);
			}
			catch (Exception e)
			{
				if (e is JSchException)
				{
					throw (JSchException)e;
				}
				if (e is Exception)
				{
					throw new JSchException("ChannelExec", (Exception)e);
				}
				throw new JSchException("ChannelExec");
			}
			if (io.@in != null)
			{
				thread = new Sharpen.Thread(this);
				thread.SetName("Exec thread " + _session.GetHost());
				if (_session.daemon_thread)
				{
					thread.SetDaemon(_session.daemon_thread);
				}
				thread.Start();
			}
		}

		public virtual void SetCommand(string command)
		{
			this.command = Util.Str2byte(command);
		}

		public virtual void SetCommand(byte[] command)
		{
			this.command = command;
		}

		/// <exception cref="NSch.JSchException"></exception>
		internal override void Init()
		{
			io.SetInputStream(GetSession().@in);
			io.SetOutputStream(GetSession().@out);
		}

		public virtual void SetErrStream(OutputStream @out)
		{
			SetExtOutputStream(@out);
		}

		public virtual void SetErrStream(OutputStream @out, bool dontclose)
		{
			SetExtOutputStream(@out, dontclose);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual InputStream GetErrStream()
		{
			return GetExtInputStream();
		}
	}
}
