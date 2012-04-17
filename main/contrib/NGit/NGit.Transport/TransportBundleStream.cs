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
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Internal;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Single shot fetch from a streamed Git bundle.</summary>
	/// <remarks>
	/// Single shot fetch from a streamed Git bundle.
	/// <p>
	/// The bundle is read from an unbuffered input stream, which limits the
	/// transport to opening at most one FetchConnection before needing to recreate
	/// the transport instance.
	/// </remarks>
	public class TransportBundleStream : NGit.Transport.Transport, TransportBundle
	{
		private InputStream src;

		/// <summary>Create a new transport to fetch objects from a streamed bundle.</summary>
		/// <remarks>
		/// Create a new transport to fetch objects from a streamed bundle.
		/// <p>
		/// The stream can be unbuffered (buffering is automatically provided
		/// internally to smooth out short reads) and unpositionable (the stream is
		/// read from only once, sequentially).
		/// <p>
		/// When the FetchConnection or the this instance is closed the supplied
		/// input stream is also automatically closed. This frees callers from
		/// needing to keep track of the supplied stream.
		/// </remarks>
		/// <param name="db">repository the fetched objects will be loaded into.</param>
		/// <param name="uri">
		/// symbolic name of the source of the stream. The URI can
		/// reference a non-existent resource. It is used only for
		/// exception reporting.
		/// </param>
		/// <param name="in">the stream to read the bundle from.</param>
		public TransportBundleStream(Repository db, URIish uri, InputStream @in) : base(db
			, uri)
		{
			src = @in;
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public override FetchConnection OpenFetch()
		{
			if (src == null)
			{
				throw new TransportException(uri, JGitText.Get().onlyOneFetchSupported);
			}
			try
			{
				return new BundleFetchConnection(this, src);
			}
			finally
			{
				src = null;
			}
		}

		/// <exception cref="System.NotSupportedException"></exception>
		public override PushConnection OpenPush()
		{
			throw new NGit.Errors.NotSupportedException(JGitText.Get().pushIsNotSupportedForBundleTransport
				);
		}

		public override void Close()
		{
			if (src != null)
			{
				try
				{
					src.Close();
				}
				catch (IOException)
				{
				}
				finally
				{
					// Ignore a close error.
					src = null;
				}
			}
		}
	}
}
