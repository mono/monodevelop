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
	internal class UserAuthNone : UserAuth
	{
		private const int SSH_MSG_SERVICE_ACCEPT = 6;

		private string methods = null;

		/// <exception cref="System.Exception"></exception>
		public override bool Start(Session session)
		{
			base.Start(session);
			// send
			// byte      SSH_MSG_SERVICE_REQUEST(5)
			// string    service name "ssh-userauth"
			packet.Reset();
			buf.PutByte(unchecked((byte)Session.SSH_MSG_SERVICE_REQUEST));
			buf.PutString(Util.Str2byte("ssh-userauth"));
			session.Write(packet);
			if (JSch.GetLogger().IsEnabled(Logger.INFO))
			{
				JSch.GetLogger().Log(Logger.INFO, "SSH_MSG_SERVICE_REQUEST sent");
			}
			// receive
			// byte      SSH_MSG_SERVICE_ACCEPT(6)
			// string    service name
			buf = session.Read(buf);
			int command = buf.GetCommand();
			bool result = (command == SSH_MSG_SERVICE_ACCEPT);
			if (JSch.GetLogger().IsEnabled(Logger.INFO))
			{
				JSch.GetLogger().Log(Logger.INFO, "SSH_MSG_SERVICE_ACCEPT received");
			}
			if (!result)
			{
				return false;
			}
			byte[] _username = null;
			_username = Util.Str2byte(username);
			// send
			// byte      SSH_MSG_USERAUTH_REQUEST(50)
			// string    user name
			// string    service name ("ssh-connection")
			// string    "none"
			packet.Reset();
			buf.PutByte(unchecked((byte)SSH_MSG_USERAUTH_REQUEST));
			buf.PutString(_username);
			buf.PutString(Util.Str2byte("ssh-connection"));
			buf.PutString(Util.Str2byte("none"));
			session.Write(packet);
			while (true)
			{
				buf = session.Read(buf);
				command = buf.GetCommand() & unchecked((int)(0xff));
				if (command == SSH_MSG_USERAUTH_SUCCESS)
				{
					return true;
				}
				if (command == SSH_MSG_USERAUTH_BANNER)
				{
					buf.GetInt();
					buf.GetByte();
					buf.GetByte();
					byte[] _message = buf.GetString();
					byte[] lang = buf.GetString();
					string message = Util.Byte2str(_message);
					if (userinfo != null)
					{
						try
						{
							userinfo.ShowMessage(message);
						}
						catch (RuntimeException)
						{
						}
					}
					goto loop_continue;
				}
				if (command == SSH_MSG_USERAUTH_FAILURE)
				{
					buf.GetInt();
					buf.GetByte();
					buf.GetByte();
					byte[] foo = buf.GetString();
					int partial_success = buf.GetByte();
					methods = Util.Byte2str(foo);
					//System.err.println("UserAuthNONE: "+methods+
					//		   " partial_success:"+(partial_success!=0));
					//	if(partial_success!=0){
					//	  throw new JSchPartialAuthException(new String(foo));
					//	}
					break;
				}
				else
				{
					//      System.err.println("USERAUTH fail ("+command+")");
					throw new JSchException("USERAUTH fail (" + command + ")");
				}
loop_continue: ;
			}
loop_break: ;
			//throw new JSchException("USERAUTH fail");
			return false;
		}

		internal virtual string GetMethods()
		{
			return methods;
		}
	}
}
