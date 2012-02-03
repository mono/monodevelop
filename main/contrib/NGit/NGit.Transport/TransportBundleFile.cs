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

using System;
using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	internal class TransportBundleFile : NGit.Transport.Transport, TransportBundle
	{
		private sealed class _TransportProtocol_64 : TransportProtocol
		{
			public _TransportProtocol_64()
			{
				this.schemeNames = new string[] { "bundle", "file" };
				this.schemeSet = Sharpen.Collections.UnmodifiableSet(new LinkedHashSet<string>(Arrays
					.AsList(this.schemeNames)));
			}

			private readonly string[] schemeNames;

			private readonly ICollection<string> schemeSet;

			//$NON-NLS-1$ //$NON-NLS-2$
			public override string GetName()
			{
				return JGitText.Get().transportProtoBundleFile;
			}

			public override ICollection<string> GetSchemes()
			{
				return this.schemeSet;
			}

			public override bool CanHandle(URIish uri, Repository local, string remoteName)
			{
				if (uri.GetPath() == null || uri.GetPort() > 0 || uri.GetUser() != null || uri.GetPass
					() != null || uri.GetHost() != null || (uri.GetScheme() != null && !this.GetSchemes
					().Contains(uri.GetScheme())))
				{
					return false;
				}
				return true;
			}

			/// <exception cref="System.NotSupportedException"></exception>
			/// <exception cref="NGit.Errors.TransportException"></exception>
			public override NGit.Transport.Transport Open(URIish uri, Repository local, string
				 remoteName)
			{
				if ("bundle".Equals(uri.GetScheme()))
				{
					FilePath path = local.FileSystem.Resolve(new FilePath("."), uri.GetPath());
					return new NGit.Transport.TransportBundleFile(local, uri, path);
				}
				// This is an ambiguous reference, it could be a bundle file
				// or it could be a Git repository. Allow TransportLocal to
				// resolve the path and figure out which type it is by testing
				// the target.
				//
				return TransportLocal.PROTO_LOCAL.Open(uri, local, remoteName);
			}
		}

		internal static readonly TransportProtocol PROTO_BUNDLE = new _TransportProtocol_64
			();

		private readonly FilePath bundle;

		internal TransportBundleFile(Repository local, URIish uri, FilePath bundlePath) : 
			base(local, uri)
		{
			bundle = bundlePath;
		}

		/// <exception cref="System.NotSupportedException"></exception>
		/// <exception cref="NGit.Errors.TransportException"></exception>
		public override FetchConnection OpenFetch()
		{
			InputStream src;
			try
			{
				src = new FileInputStream(bundle);
			}
			catch (FileNotFoundException)
			{
				throw new TransportException(uri, JGitText.Get().notFound);
			}
			return new BundleFetchConnection(this, src);
		}

		/// <exception cref="System.NotSupportedException"></exception>
		public override PushConnection OpenPush()
		{
			throw new NGit.Errors.NotSupportedException(JGitText.Get().pushIsNotSupportedForBundleTransport
				);
		}

		public override void Close()
		{
		}
		// Resources must be established per-connection.
	}
}
