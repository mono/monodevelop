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

using NSch;
using Sharpen;

namespace NSch
{
	internal class RequestEnv : Request
	{
		internal byte[] name = new byte[0];

		internal byte[] value = new byte[0];

		internal virtual void SetEnv(byte[] name, byte[] value)
		{
			this.name = name;
			this.value = value;
		}

		/// <exception cref="System.Exception"></exception>
		internal override void DoRequest(Session session, Channel channel)
		{
			base.DoRequest(session, channel);
			Buffer buf = new Buffer();
			Packet packet = new Packet(buf);
			packet.Reset();
			buf.PutByte(unchecked((byte)Session.SSH_MSG_CHANNEL_REQUEST));
			buf.PutInt(channel.GetRecipient());
			buf.PutString(Util.Str2byte("env"));
			buf.PutByte(unchecked((byte)(WaitForReply() ? 1 : 0)));
			buf.PutString(name);
			buf.PutString(value);
			Write(packet);
		}
	}
}
