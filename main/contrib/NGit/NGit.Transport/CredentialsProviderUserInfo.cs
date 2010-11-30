/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System.Collections.Generic;
using NGit.Transport;
using NSch;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>
	/// A JSch
	/// <see cref="NSch.UserInfo">NSch.UserInfo</see>
	/// adapter for a
	/// <see cref="CredentialsProvider">CredentialsProvider</see>
	/// .
	/// </summary>
	public class CredentialsProviderUserInfo : UserInfo, UIKeyboardInteractive
	{
		private readonly URIish uri;

		private readonly CredentialsProvider provider;

		private string password;

		private string passphrase;

		/// <summary>Wrap a CredentialsProvider to make it suitable for use with JSch.</summary>
		/// <remarks>Wrap a CredentialsProvider to make it suitable for use with JSch.</remarks>
		/// <param name="session">the JSch session this UserInfo will support authentication on.
		/// 	</param>
		/// <param name="credentialsProvider">the provider that will perform the authentication.
		/// 	</param>
		public CredentialsProviderUserInfo(Session session, CredentialsProvider credentialsProvider
			)
		{
			this.uri = CreateURI(session);
			this.provider = credentialsProvider;
		}

		private static URIish CreateURI(Session session)
		{
			URIish uri = new URIish();
			uri = uri.SetScheme("ssh");
			uri = uri.SetUser(session.GetUserName());
			uri = uri.SetHost(session.GetHost());
			uri = uri.SetPort(session.GetPort());
			return uri;
		}

		public virtual string GetPassword()
		{
			return password;
		}

		public virtual string GetPassphrase()
		{
			return passphrase;
		}

		public virtual bool PromptPassphrase(string msg)
		{
			CredentialItem.StringType v = NewPrompt(msg);
			if (provider.Get(uri, v))
			{
				passphrase = v.GetValue();
				return true;
			}
			else
			{
				passphrase = null;
				return false;
			}
		}

		public virtual bool PromptPassword(string msg)
		{
			CredentialItem.StringType v = NewPrompt(msg);
			if (provider.Get(uri, v))
			{
				password = v.GetValue();
				return true;
			}
			else
			{
				password = null;
				return false;
			}
		}

		private CredentialItem.StringType NewPrompt(string msg)
		{
			return new CredentialItem.StringType(msg, true);
		}

		public virtual bool PromptYesNo(string msg)
		{
			CredentialItem.YesNoType v = new CredentialItem.YesNoType(msg);
			return provider.Get(uri, v) && v.GetValue();
		}

		public virtual void ShowMessage(string msg)
		{
			provider.Get(uri, new CredentialItem.InformationalMessage(msg));
		}

		public virtual string[] PromptKeyboardInteractive(string destination, string name
			, string instruction, string[] prompt, bool[] echo)
		{
			CredentialItem.StringType[] v = new CredentialItem.StringType[prompt.Length];
			for (int i = 0; i < prompt.Length; i++)
			{
				v[i] = new CredentialItem.StringType(prompt[i], !echo[i]);
			}
			IList<CredentialItem> items = new AList<CredentialItem>();
			if (instruction != null && instruction.Length > 0)
			{
				items.AddItem(new CredentialItem.InformationalMessage(instruction));
			}
			Sharpen.Collections.AddAll(items, Arrays.AsList(v));
			if (!provider.Get(uri, items))
			{
				return null;
			}
			// cancel
			string[] result = new string[v.Length];
			for (int i_1 = 0; i_1 < v.Length; i_1++)
			{
				result[i_1] = v[i_1].GetValue();
			}
			return result;
		}
	}
}
