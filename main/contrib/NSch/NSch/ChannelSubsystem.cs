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
	class ChannelSubsystem : ChannelSession
	{
		internal bool xforwading = false;

		internal bool pty = false;

		internal bool want_reply = true;

		internal string subsystem = string.Empty;

		public override void SetXForwarding(bool foo)
		{
			xforwading = foo;
		}

		public override void SetPty(bool foo)
		{
			pty = foo;
		}

		public virtual void SetWantReply(bool foo)
		{
			want_reply = foo;
		}

		public virtual void SetSubsystem(string foo)
		{
			subsystem = foo;
		}

		/// <exception cref="NSch.JSchException"></exception>
		public override void Start()
		{
			Session _session = GetSession();
			try
			{
				Request request;
				if (xforwading)
				{
					request = new RequestX11();
					request.DoRequest(_session, this);
				}
				if (pty)
				{
					request = new RequestPtyReq();
					request.DoRequest(_session, this);
				}
				request = new RequestSubsystem();
				((RequestSubsystem)request).Request(_session, this, subsystem, want_reply);
			}
			catch (Exception e)
			{
				if (e is JSchException)
				{
					throw (JSchException)e;
				}
				if (e is Exception)
				{
					throw new JSchException("ChannelSubsystem", (Exception)e);
				}
				throw new JSchException("ChannelSubsystem");
			}
			if (io.@in != null)
			{
				thread = new Sharpen.Thread(this);
				thread.SetName("Subsystem for " + _session.host);
				if (_session.daemon_thread)
				{
					thread.SetDaemon(_session.daemon_thread);
				}
				thread.Start();
			}
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

		/// <exception cref="System.IO.IOException"></exception>
		public virtual InputStream GetErrStream()
		{
			return GetExtInputStream();
		}
	}
}
