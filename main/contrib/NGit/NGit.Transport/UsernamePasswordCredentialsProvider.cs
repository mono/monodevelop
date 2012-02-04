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

using NGit.Errors;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>
	/// Simple
	/// <see cref="CredentialsProvider">CredentialsProvider</see>
	/// that always uses the same information.
	/// </summary>
	public class UsernamePasswordCredentialsProvider : CredentialsProvider
	{
		private string username;

		private char[] password;

		/// <summary>Initialize the provider with a single username and password.</summary>
		/// <remarks>Initialize the provider with a single username and password.</remarks>
		/// <param name="username"></param>
		/// <param name="password"></param>
		public UsernamePasswordCredentialsProvider(string username, string password) : this
			(username, password.ToCharArray())
		{
		}

		/// <summary>Initialize the provider with a single username and password.</summary>
		/// <remarks>Initialize the provider with a single username and password.</remarks>
		/// <param name="username"></param>
		/// <param name="password"></param>
		public UsernamePasswordCredentialsProvider(string username, char[] password)
		{
			this.username = username;
			this.password = password;
		}

		public override bool IsInteractive()
		{
			return false;
		}

		public override bool Supports(params CredentialItem[] items)
		{
			foreach (CredentialItem i in items)
			{
				if (i is CredentialItem.Username)
				{
					continue;
				}
				else
				{
					if (i is CredentialItem.Password)
					{
						continue;
					}
					else
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <exception cref="NGit.Errors.UnsupportedCredentialItem"></exception>
		public override bool Get(URIish uri, params CredentialItem[] items)
		{
			foreach (CredentialItem i in items)
			{
				if (i is CredentialItem.Username)
				{
					((CredentialItem.Username)i).SetValue(username);
				}
				else
				{
					if (i is CredentialItem.Password)
					{
						((CredentialItem.Password)i).SetValue(password);
					}
					else
					{
						throw new UnsupportedCredentialItem(uri, i.GetPromptText());
					}
				}
			}
			return true;
		}

		/// <summary>Destroy the saved username and password..</summary>
		/// <remarks>Destroy the saved username and password..</remarks>
		public virtual void Clear()
		{
			username = null;
			if (password != null)
			{
				Arrays.Fill(password, (char)0);
				password = null;
			}
		}
	}
}
