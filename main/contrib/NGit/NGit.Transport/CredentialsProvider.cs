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
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Provide credentials for use in connecting to Git repositories.</summary>
	/// <remarks>
	/// Provide credentials for use in connecting to Git repositories.
	/// Implementors are strongly encouraged to support at least the minimal
	/// <see cref="Username">Username</see>
	/// and
	/// <see cref="Password">Password</see>
	/// items.
	/// More sophisticated implementors may implement additional types, such as
	/// <see cref="StringType">StringType</see>
	/// .
	/// CredentialItems are usually presented in bulk, allowing implementors to
	/// combine them into a single UI widget and streamline the authentication
	/// process for an end-user.
	/// </remarks>
	/// <seealso cref="UsernamePasswordCredentialsProvider">UsernamePasswordCredentialsProvider
	/// 	</seealso>
	public abstract class CredentialsProvider
	{
		private static volatile CredentialsProvider defaultProvider;

		/// <returns>the default credentials provider, or null.</returns>
		public static CredentialsProvider GetDefault()
		{
			return defaultProvider;
		}

		/// <summary>Set the default credentials provider.</summary>
		/// <remarks>Set the default credentials provider.</remarks>
		/// <param name="p">the new default provider, may be null to select no default.</param>
		public static void SetDefault(CredentialsProvider p)
		{
			defaultProvider = p;
		}

		/// <summary>Check if the provider is interactive with the end-user.</summary>
		/// <remarks>
		/// Check if the provider is interactive with the end-user.
		/// An interactive provider may try to open a dialog box, or prompt for input
		/// on the terminal, and will wait for a user response. A non-interactive
		/// provider will either populate CredentialItems, or fail.
		/// </remarks>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if the provider is interactive with the end-user.
		/// </returns>
		public abstract bool IsInteractive();

		/// <summary>
		/// Check if the provider can supply the necessary
		/// <see cref="CredentialItem">CredentialItem</see>
		/// s.
		/// </summary>
		/// <param name="items">the items the application requires to complete authentication.
		/// 	</param>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if this
		/// <see cref="CredentialsProvider">CredentialsProvider</see>
		/// supports all of
		/// the items supplied.
		/// </returns>
		public abstract bool Supports(params CredentialItem[] items);

		/// <summary>Ask for the credential items to be populated.</summary>
		/// <remarks>Ask for the credential items to be populated.</remarks>
		/// <param name="uri">the URI of the remote resource that needs authentication.</param>
		/// <param name="items">the items the application requires to complete authentication.
		/// 	</param>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if the request was successful and values were
		/// supplied;
		/// <code>false</code>
		/// if the user canceled the request and did
		/// not supply all requested values.
		/// </returns>
		/// <exception cref="NGit.Errors.UnsupportedCredentialItem">if one of the items supplied is not supported.
		/// 	</exception>
		public abstract bool Get(URIish uri, params CredentialItem[] items);

		/// <summary>Ask for the credential items to be populated.</summary>
		/// <remarks>Ask for the credential items to be populated.</remarks>
		/// <param name="uri">the URI of the remote resource that needs authentication.</param>
		/// <param name="items">the items the application requires to complete authentication.
		/// 	</param>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if the request was successful and values were
		/// supplied;
		/// <code>false</code>
		/// if the user canceled the request and did
		/// not supply all requested values.
		/// </returns>
		/// <exception cref="NGit.Errors.UnsupportedCredentialItem">if one of the items supplied is not supported.
		/// 	</exception>
		public virtual bool Get(URIish uri, IList<CredentialItem> items)
		{
			return Get(uri, Sharpen.Collections.ToArray(items, new CredentialItem[items.Count
				]));
		}

		/// <summary>Reset the credentials provider for the given URI</summary>
		/// <param name="uri"></param>
		public virtual void Reset(URIish uri)
		{
		}
		// default does nothing
	}
}
