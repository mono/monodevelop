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
	public abstract class Request
	{
		private bool reply = false;

		private Session session = null;

		private Channel channel = null;

		/// <exception cref="System.Exception"></exception>
		internal virtual void DoRequest(Session session, Channel channel)
		{
			this.session = session;
			this.channel = channel;
			if (channel.connectTimeout > 0)
			{
				SetReply(true);
			}
		}

		internal virtual bool WaitForReply()
		{
			return reply;
		}

		internal virtual void SetReply(bool reply)
		{
			this.reply = reply;
		}

		/// <exception cref="System.Exception"></exception>
		internal virtual void Write(Packet packet)
		{
			if (reply)
			{
				channel.reply = -1;
			}
			session.Write(packet);
			if (reply)
			{
				long start = Runtime.CurrentTimeMillis();
				long timeout = channel.connectTimeout;
				while (channel.IsConnected() && channel.reply == -1)
				{
					try
					{
						Sharpen.Thread.Sleep(10);
					}
					catch (Exception)
					{
					}
					if (timeout > 0L && (Runtime.CurrentTimeMillis() - start) > timeout)
					{
						channel.reply = 0;
						throw new JSchException("channel request: timeout");
					}
				}
				if (channel.reply == 0)
				{
					throw new JSchException("failed to send channel request");
				}
			}
		}
	}
}
