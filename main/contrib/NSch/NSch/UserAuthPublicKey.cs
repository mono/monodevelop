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

using System.Collections;
using NSch;
using Sharpen;

namespace NSch
{
	internal class UserAuthPublicKey : UserAuth
	{
		/// <exception cref="System.Exception"></exception>
		public override bool Start(Session session)
		{
			base.Start(session);
			ArrayList identities = session.jsch.GetIdentityRepository().GetIdentities();
			byte[] passphrase = null;
			byte[] _username = null;
			int command;
			lock (identities)
			{
				if (identities.Count <= 0)
				{
					return false;
				}
				_username = Util.Str2byte(username);
				for (int i = 0; i < identities.Count; i++)
				{
					if (session.auth_failures >= session.max_auth_tries)
					{
						return false;
					}
					Identity identity = (Identity)(identities[i]);
					byte[] pubkeyblob = identity.GetPublicKeyBlob();
					//System.err.println("UserAuthPublicKey: "+identity+" "+pubkeyblob);
					if (pubkeyblob != null)
					{
						// send
						// byte      SSH_MSG_USERAUTH_REQUEST(50)
						// string    user name
						// string    service name ("ssh-connection")
						// string    "publickey"
						// boolen    FALSE
						// string    plaintext password (ISO-10646 UTF-8)
						packet.Reset();
						buf.PutByte(unchecked((byte)SSH_MSG_USERAUTH_REQUEST));
						buf.PutString(_username);
						buf.PutString(Util.Str2byte("ssh-connection"));
						buf.PutString(Util.Str2byte("publickey"));
						buf.PutByte(unchecked((byte)0));
						buf.PutString(Util.Str2byte(identity.GetAlgName()));
						buf.PutString(pubkeyblob);
						session.Write(packet);
						while (true)
						{
							buf = session.Read(buf);
							command = buf.GetCommand() & unchecked((int)(0xff));
							if (command == SSH_MSG_USERAUTH_PK_OK)
							{
								break;
							}
							else
							{
								if (command == SSH_MSG_USERAUTH_FAILURE)
								{
									break;
								}
								else
								{
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
										goto loop1_continue;
									}
									else
									{
										//System.err.println("USERAUTH fail ("+command+")");
										//throw new JSchException("USERAUTH fail ("+command+")");
										break;
									}
								}
							}
loop1_continue: ;
						}
loop1_break: ;
						if (command != SSH_MSG_USERAUTH_PK_OK)
						{
							continue;
						}
					}
					//System.err.println("UserAuthPublicKey: identity.isEncrypted()="+identity.isEncrypted());
					int count = 5;
					while (true)
					{
						if ((identity.IsEncrypted() && passphrase == null))
						{
							if (userinfo == null)
							{
								throw new JSchException("USERAUTH fail");
							}
							if (identity.IsEncrypted() && !userinfo.PromptPassphrase("Passphrase for " + identity
								.GetName()))
							{
								throw new JSchAuthCancelException("publickey");
							}
							//throw new JSchException("USERAUTH cancel");
							//break;
							string _passphrase = userinfo.GetPassphrase();
							if (_passphrase != null)
							{
								passphrase = Util.Str2byte(_passphrase);
							}
						}
						if (!identity.IsEncrypted() || passphrase != null)
						{
							if (identity.SetPassphrase(passphrase))
							{
								break;
							}
						}
						Util.Bzero(passphrase);
						passphrase = null;
						count--;
						if (count == 0)
						{
							break;
						}
					}
					Util.Bzero(passphrase);
					passphrase = null;
					//System.err.println("UserAuthPublicKey: identity.isEncrypted()="+identity.isEncrypted());
					if (identity.IsEncrypted())
					{
						continue;
					}
					if (pubkeyblob == null)
					{
						pubkeyblob = identity.GetPublicKeyBlob();
					}
					//System.err.println("UserAuthPublicKey: pubkeyblob="+pubkeyblob);
					if (pubkeyblob == null)
					{
						continue;
					}
					// send
					// byte      SSH_MSG_USERAUTH_REQUEST(50)
					// string    user name
					// string    service name ("ssh-connection")
					// string    "publickey"
					// boolen    TRUE
					// string    plaintext password (ISO-10646 UTF-8)
					packet.Reset();
					buf.PutByte(unchecked((byte)SSH_MSG_USERAUTH_REQUEST));
					buf.PutString(_username);
					buf.PutString(Util.Str2byte("ssh-connection"));
					buf.PutString(Util.Str2byte("publickey"));
					buf.PutByte(unchecked((byte)1));
					buf.PutString(Util.Str2byte(identity.GetAlgName()));
					buf.PutString(pubkeyblob);
					//      byte[] tmp=new byte[buf.index-5];
					//      System.arraycopy(buf.buffer, 5, tmp, 0, tmp.length);
					//      buf.putString(signature);
					byte[] sid = session.GetSessionId();
					int sidlen = sid.Length;
					byte[] tmp = new byte[4 + sidlen + buf.index - 5];
					tmp[0] = unchecked((byte)((int)(((uint)sidlen) >> 24)));
					tmp[1] = unchecked((byte)((int)(((uint)sidlen) >> 16)));
					tmp[2] = unchecked((byte)((int)(((uint)sidlen) >> 8)));
					tmp[3] = unchecked((byte)(sidlen));
					System.Array.Copy(sid, 0, tmp, 4, sidlen);
					System.Array.Copy(buf.buffer, 5, tmp, 4 + sidlen, buf.index - 5);
					byte[] signature = identity.GetSignature(tmp);
					if (signature == null)
					{
						// for example, too long key length.
						break;
					}
					buf.PutString(signature);
					session.Write(packet);
					while (true)
					{
						buf = session.Read(buf);
						command = buf.GetCommand() & unchecked((int)(0xff));
						if (command == SSH_MSG_USERAUTH_SUCCESS)
						{
							return true;
						}
						else
						{
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
								goto loop2_continue;
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
									//                   " partial_success:"+(partial_success!=0));
									if (partial_success != 0)
									{
										throw new JSchPartialAuthException(Util.Byte2str(foo));
									}
									session.auth_failures++;
									break;
								}
							}
						}
						//System.err.println("USERAUTH fail ("+command+")");
						//throw new JSchException("USERAUTH fail ("+command+")");
						break;
loop2_continue: ;
					}
loop2_break: ;
				}
			}
			return false;
		}
	}
}
