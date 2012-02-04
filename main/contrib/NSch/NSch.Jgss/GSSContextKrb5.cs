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
using System.Security;
using NSch;
using NSch.Jgss;
using Sharpen;

namespace NSch.Jgss
{
	public class GSSContextKrb5 : NSch.GSSContext
	{
		private static readonly string pUseSubjectCredsOnly = "javax.security.auth.useSubjectCredsOnly";

		private static string useSubjectCredsOnly = GetSystemProperty(pUseSubjectCredsOnly
			);

		private Sharpen.GSSContext context = null;

		/// <exception cref="NSch.JSchException"></exception>
		public virtual void Create(string user, string host)
		{
			try
			{
				// RFC 1964
				Oid krb5 = new Oid("1.2.840.113554.1.2.2");
				// Kerberos Principal Name Form
				Oid principalName = new Oid("1.2.840.113554.1.2.2.1");
				GSSManager mgr = GSSManager.GetInstance();
				GSSCredential crd = null;
				string cname = host;
				try
				{
					cname = Sharpen.Extensions.GetAddressByName(cname).ToString();
				}
				catch (UnknownHostException)
				{
				}
				GSSName _host = mgr.CreateName("host/" + cname, principalName);
				context = mgr.CreateContext(_host, krb5, crd, Sharpen.GSSContext.DEFAULT_LIFETIME
					);
				// RFC4462  3.4.  GSS-API Session
				//
				// When calling GSS_Init_sec_context(), the client MUST set
				// integ_req_flag to "true" to request that per-message integrity
				// protection be supported for this context.  In addition,
				// deleg_req_flag MAY be set to "true" to request access delegation, if
				// requested by the user.
				//
				// Since the user authentication process by its nature authenticates
				// only the client, the setting of mutual_req_flag is not needed for
				// this process.  This flag SHOULD be set to "false".
				// TODO: OpenSSH's sshd does accepts 'false' for mutual_req_flag
				//context.requestMutualAuth(false);
				context.RequestMutualAuth(true);
				context.RequestConf(true);
				context.RequestInteg(true);
				// for MIC
				context.RequestCredDeleg(true);
				context.RequestAnonymity(false);
				return;
			}
			catch (GSSException ex)
			{
				throw new JSchException(ex.ToString());
			}
		}

		public virtual bool IsEstablished()
		{
			return context.IsEstablished();
		}

		/// <exception cref="NSch.JSchException"></exception>
		public virtual byte[] Init(byte[] token, int s, int l)
		{
			try
			{
				// Without setting "javax.security.auth.useSubjectCredsOnly" to "false",
				// Sun's JVM for Un*x will show messages to stderr in
				// processing context.initSecContext().
				// This hack is not thread safe ;-<.
				// If that property is explicitly given as "true" or "false",
				// this hack must not be invoked.
				if (useSubjectCredsOnly == null)
				{
					SetSystemProperty(pUseSubjectCredsOnly, "false");
				}
				return context.InitSecContext(token, 0, l);
			}
			catch (GSSException ex)
			{
				throw new JSchException(ex.ToString());
			}
			catch (SecurityException ex)
			{
				throw new JSchException(ex.ToString());
			}
			finally
			{
				if (useSubjectCredsOnly == null)
				{
					// By the default, it must be "true".
					SetSystemProperty(pUseSubjectCredsOnly, "true");
				}
			}
		}

		public virtual byte[] GetMIC(byte[] message, int s, int l)
		{
			try
			{
				MessageProp prop = new MessageProp(0, true);
				return context.GetMIC(message, s, l, prop);
			}
			catch (GSSException)
			{
				return null;
			}
		}

		public virtual void Dispose()
		{
			try
			{
				context.Dispose();
			}
			catch (GSSException)
			{
			}
		}

		private static string GetSystemProperty(string key)
		{
			try
			{
				return Runtime.GetProperty(key);
			}
			catch (Exception)
			{
				// We are not allowed to get the System properties.
				return null;
			}
		}

		private static void SetSystemProperty(string key, string value)
		{
			try
			{
				Runtime.SetProperty(key, value);
			}
			catch (Exception)
			{
			}
		}
		// We are not allowed to set the System properties.
	}
}
