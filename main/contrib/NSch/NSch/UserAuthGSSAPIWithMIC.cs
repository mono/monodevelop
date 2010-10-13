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
	public class UserAuthGSSAPIWithMIC : UserAuth
	{
		private const int SSH_MSG_USERAUTH_GSSAPI_RESPONSE = 60;

		private const int SSH_MSG_USERAUTH_GSSAPI_TOKEN = 61;

		private const int SSH_MSG_USERAUTH_GSSAPI_EXCHANGE_COMPLETE = 63;

		private const int SSH_MSG_USERAUTH_GSSAPI_ERROR = 64;

		private const int SSH_MSG_USERAUTH_GSSAPI_ERRTOK = 65;

		private const int SSH_MSG_USERAUTH_GSSAPI_MIC = 66;

		private static readonly byte[][] supported_oid = new byte[][] { new byte[] { unchecked(
			(byte)unchecked((int)(0x6))), unchecked((byte)unchecked((int)(0x9))), unchecked(
			(byte)unchecked((int)(0x2a))), unchecked((byte)unchecked((int)(0x86))), unchecked(
			(byte)unchecked((int)(0x48))), unchecked((byte)unchecked((int)(0x86))), unchecked(
			(byte)unchecked((int)(0xf7))), unchecked((byte)unchecked((int)(0x12))), unchecked(
			(byte)unchecked((int)(0x1))), unchecked((byte)unchecked((int)(0x2))), unchecked(
			(byte)unchecked((int)(0x2))) } };

		private static readonly string[] supported_method = new string[] { "gssapi-with-mic.krb5"
			 };

		// OID 1.2.840.113554.1.2.2 in DER
		/// <exception cref="System.Exception"></exception>
		public override bool Start(Session session)
		{
			base.Start(session);
			byte[] _username = Util.Str2byte(username);
			packet.Reset();
			// byte            SSH_MSG_USERAUTH_REQUEST(50)
			// string          user name(in ISO-10646 UTF-8 encoding)
			// string          service name(in US-ASCII)
			// string          "gssapi"(US-ASCII)
			// uint32          n, the number of OIDs client supports
			// string[n]       mechanism OIDS
			buf.PutByte(unchecked((byte)SSH_MSG_USERAUTH_REQUEST));
			buf.PutString(_username);
			buf.PutString(Util.Str2byte("ssh-connection"));
			buf.PutString(Util.Str2byte("gssapi-with-mic"));
			buf.PutInt(supported_oid.Length);
			for (int i = 0; i < supported_oid.Length; i++)
			{
				buf.PutString(supported_oid[i]);
			}
			session.Write(packet);
			string method = null;
			int command;
			while (true)
			{
				buf = session.Read(buf);
				command = buf.GetCommand() & unchecked((int)(0xff));
				if (command == SSH_MSG_USERAUTH_FAILURE)
				{
					return false;
				}
				if (command == SSH_MSG_USERAUTH_GSSAPI_RESPONSE)
				{
					buf.GetInt();
					buf.GetByte();
					buf.GetByte();
					byte[] message = buf.GetString();
					for (int i_1 = 0; i_1 < supported_oid.Length; i_1++)
					{
						if (Util.Array_equals(message, supported_oid[i_1]))
						{
							method = supported_method[i_1];
							break;
						}
					}
					if (method == null)
					{
						return false;
					}
					break;
				}
				// success
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
						userinfo.ShowMessage(message);
					}
					continue;
				}
				return false;
			}
			NSch.GSSContext context = null;
			try
			{
				Type c = Sharpen.Runtime.GetType(session.GetConfig(method));
				context = (NSch.GSSContext)(System.Activator.CreateInstance(c));
			}
			catch (Exception)
			{
				return false;
			}
			try
			{
				context.Create(username, session.host);
			}
			catch (JSchException)
			{
				return false;
			}
			byte[] token = new byte[0];
			while (!context.IsEstablished())
			{
				try
				{
					token = context.Init(token, 0, token.Length);
				}
				catch (JSchException)
				{
					// TODO
					// ERRTOK should be sent?
					// byte        SSH_MSG_USERAUTH_GSSAPI_ERRTOK
					// string      error token
					return false;
				}
				if (token != null)
				{
					packet.Reset();
					buf.PutByte(unchecked((byte)SSH_MSG_USERAUTH_GSSAPI_TOKEN));
					buf.PutString(token);
					session.Write(packet);
				}
				if (!context.IsEstablished())
				{
					buf = session.Read(buf);
					command = buf.GetCommand() & unchecked((int)(0xff));
					if (command == SSH_MSG_USERAUTH_GSSAPI_ERROR)
					{
						// uint32    major_status
						// uint32    minor_status
						// string    message
						// string    language tag
						buf = session.Read(buf);
						command = buf.GetCommand() & unchecked((int)(0xff));
					}
					else
					{
						//return false;
						if (command == SSH_MSG_USERAUTH_GSSAPI_ERRTOK)
						{
							// string error token
							buf = session.Read(buf);
							command = buf.GetCommand() & unchecked((int)(0xff));
						}
					}
					//return false;
					if (command == SSH_MSG_USERAUTH_FAILURE)
					{
						return false;
					}
					buf.GetInt();
					buf.GetByte();
					buf.GetByte();
					token = buf.GetString();
				}
			}
			Buffer mbuf = new Buffer();
			// string    session identifier
			// byte      SSH_MSG_USERAUTH_REQUEST
			// string    user name
			// string    service
			// string    "gssapi-with-mic"
			mbuf.PutString(session.GetSessionId());
			mbuf.PutByte(unchecked((byte)SSH_MSG_USERAUTH_REQUEST));
			mbuf.PutString(_username);
			mbuf.PutString(Util.Str2byte("ssh-connection"));
			mbuf.PutString(Util.Str2byte("gssapi-with-mic"));
			byte[] mic = context.GetMIC(mbuf.buffer, 0, mbuf.GetLength());
			if (mic == null)
			{
				return false;
			}
			packet.Reset();
			buf.PutByte(unchecked((byte)SSH_MSG_USERAUTH_GSSAPI_MIC));
			buf.PutString(mic);
			session.Write(packet);
			context.Dispose();
			buf = session.Read(buf);
			command = buf.GetCommand() & unchecked((int)(0xff));
			if (command == SSH_MSG_USERAUTH_SUCCESS)
			{
				return true;
			}
			else
			{
				if (command == SSH_MSG_USERAUTH_FAILURE)
				{
					buf.GetInt();
					buf.GetByte();
					buf.GetByte();
					byte[] foo = buf.GetString();
					int partial_success = buf.GetByte();
					//System.err.println(new String(foo)+
					//		 " partial_success:"+(partial_success!=0));
					if (partial_success != 0)
					{
						throw new JSchPartialAuthException(Util.Byte2str(foo));
					}
				}
			}
			return false;
		}
	}
}
