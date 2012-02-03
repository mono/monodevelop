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
using NGit;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Describes a way to connect to another Git repository.</summary>
	/// <remarks>
	/// Describes a way to connect to another Git repository.
	/// <p>
	/// Implementations of this class are typically immutable singletons held by
	/// static class members, for example:
	/// <pre>
	/// package com.example.my_transport;
	/// class MyTransport extends Transport {
	/// public static final TransportProtocol PROTO = new TransportProtocol() {
	/// public String getName() {
	/// return &quot;My Protocol&quot;;
	/// }
	/// };
	/// }
	/// </pre>
	/// <p>
	/// Applications may register additional protocols for use by JGit by calling
	/// <see cref="Transport.Register(TransportProtocol)">Transport.Register(TransportProtocol)
	/// 	</see>
	/// . Because that API holds onto
	/// the protocol object by a WeakReference, applications must ensure their own
	/// ClassLoader retains the TransportProtocol for the life of the application.
	/// Using a static singleton pattern as above will ensure the protocol is valid
	/// so long as the ClassLoader that defines it remains valid.
	/// <p>
	/// Applications may automatically register additional protocols by filling in
	/// the names of their TransportProtocol defining classes using the services file
	/// <code>META-INF/services/org.eclipse.jgit.transport.Transport</code>
	/// . For each
	/// class name listed in the services file, any static fields of type
	/// <code>TransportProtocol</code>
	/// will be automatically registered. For the above
	/// example the string
	/// <code>com.example.my_transport.MyTransport</code>
	/// should be
	/// listed in the file, as that is the name of the class that defines the static
	/// PROTO singleton.
	/// </remarks>
	public abstract class TransportProtocol
	{
		/// <summary>
		/// Fields within a
		/// <see cref="URIish">URIish</see>
		/// that a transport uses.
		/// </summary>
		public enum URIishField
		{
			USER,
			PASS,
			HOST,
			PORT,
			PATH
		}

		/// <returns>text name of the protocol suitable for display to a user.</returns>
		public abstract string GetName();

		/// <returns>immutable set of schemes supported by this protocol.</returns>
		public virtual ICollection<string> GetSchemes()
		{
			return Sharpen.Collections.EmptySet<string>();
		}

		/// <returns>immutable set of URIishFields that must be filled in.</returns>
		public virtual ICollection<TransportProtocol.URIishField> GetRequiredFields()
		{
			return Sharpen.Collections.UnmodifiableSet(EnumSet.Of(TransportProtocol.URIishField
				.PATH));
		}

		/// <returns>immutable set of URIishFields that may be filled in.</returns>
		public virtual ICollection<TransportProtocol.URIishField> GetOptionalFields()
		{
			return Sharpen.Collections.EmptySet<TransportProtocol.URIishField>();
		}

		/// <returns>if a port is supported, the default port, else -1.</returns>
		public virtual int GetDefaultPort()
		{
			return -1;
		}

		/// <summary>Determine if this protocol can handle a particular URI.</summary>
		/// <remarks>
		/// Determine if this protocol can handle a particular URI.
		/// <p>
		/// Implementations should try to avoid looking at the local filesystem, but
		/// may look at implementation specific configuration options in the remote
		/// block of
		/// <code>local.getConfig()</code>
		/// using
		/// <code>remoteName</code>
		/// if the name
		/// is non-null.
		/// <p>
		/// The default implementation of this method matches the scheme against
		/// <see cref="GetSchemes()">GetSchemes()</see>
		/// , required fields against
		/// <see cref="GetRequiredFields()">GetRequiredFields()</see>
		/// , and optional fields against
		/// <see cref="GetOptionalFields()">GetOptionalFields()</see>
		/// , returning true only if all of the fields
		/// match the specification.
		/// </remarks>
		/// <param name="uri">address of the Git repository; never null.</param>
		/// <returns>true if this protocol can handle this URI; false otherwise.</returns>
		public virtual bool CanHandle(URIish uri)
		{
			return CanHandle(uri, null, null);
		}

		/// <summary>Determine if this protocol can handle a particular URI.</summary>
		/// <remarks>
		/// Determine if this protocol can handle a particular URI.
		/// <p>
		/// Implementations should try to avoid looking at the local filesystem, but
		/// may look at implementation specific configuration options in the remote
		/// block of
		/// <code>local.getConfig()</code>
		/// using
		/// <code>remoteName</code>
		/// if the name
		/// is non-null.
		/// <p>
		/// The default implementation of this method matches the scheme against
		/// <see cref="GetSchemes()">GetSchemes()</see>
		/// , required fields against
		/// <see cref="GetRequiredFields()">GetRequiredFields()</see>
		/// , and optional fields against
		/// <see cref="GetOptionalFields()">GetOptionalFields()</see>
		/// , returning true only if all of the fields
		/// match the specification.
		/// </remarks>
		/// <param name="uri">address of the Git repository; never null.</param>
		/// <param name="local">
		/// the local repository that will communicate with the other Git
		/// repository. May be null if the caller is only asking about a
		/// specific URI and does not have a local Repository.
		/// </param>
		/// <param name="remoteName">
		/// name of the remote, if the remote as configured in
		/// <code>local</code>
		/// ; otherwise null.
		/// </param>
		/// <returns>true if this protocol can handle this URI; false otherwise.</returns>
		public virtual bool CanHandle(URIish uri, Repository local, string remoteName)
		{
			if (!GetSchemes().IsEmpty() && !GetSchemes().Contains(uri.GetScheme()))
			{
				return false;
			}
			foreach (TransportProtocol.URIishField field in GetRequiredFields())
			{
				switch (field)
				{
					case TransportProtocol.URIishField.USER:
					{
						if (uri.GetUser() == null || uri.GetUser().Length == 0)
						{
							return false;
						}
						break;
					}

					case TransportProtocol.URIishField.PASS:
					{
						if (uri.GetPass() == null || uri.GetPass().Length == 0)
						{
							return false;
						}
						break;
					}

					case TransportProtocol.URIishField.HOST:
					{
						if (uri.GetHost() == null || uri.GetHost().Length == 0)
						{
							return false;
						}
						break;
					}

					case TransportProtocol.URIishField.PORT:
					{
						if (uri.GetPort() <= 0)
						{
							return false;
						}
						break;
					}

					case TransportProtocol.URIishField.PATH:
					{
						if (uri.GetPath() == null || uri.GetPath().Length == 0)
						{
							return false;
						}
						break;
					}

					default:
					{
						return false;
						break;
					}
				}
			}
			ICollection<TransportProtocol.URIishField> canHave = EnumSet.CopyOf(GetRequiredFields
				());
			Sharpen.Collections.AddAll(canHave, GetOptionalFields());
			if (uri.GetUser() != null && !canHave.Contains(TransportProtocol.URIishField.USER
				))
			{
				return false;
			}
			if (uri.GetPass() != null && !canHave.Contains(TransportProtocol.URIishField.PASS
				))
			{
				return false;
			}
			if (uri.GetHost() != null && !canHave.Contains(TransportProtocol.URIishField.HOST
				))
			{
				return false;
			}
			if (uri.GetPort() > 0 && !canHave.Contains(TransportProtocol.URIishField.PORT))
			{
				return false;
			}
			if (uri.GetPath() != null && !canHave.Contains(TransportProtocol.URIishField.PATH
				))
			{
				return false;
			}
			return true;
		}

		/// <summary>Open a Transport instance to the other repository.</summary>
		/// <remarks>
		/// Open a Transport instance to the other repository.
		/// <p>
		/// Implementations should avoid making remote connections until an operation
		/// on the returned Transport is invoked, however they may fail fast here if
		/// they know a connection is impossible, such as when using the local
		/// filesystem and the target path does not exist.
		/// <p>
		/// Implementations may access implementation-specific configuration options
		/// within
		/// <code>local.getConfig()</code>
		/// using the remote block named by the
		/// <code>remoteName</code>
		/// , if the name is non-null.
		/// </remarks>
		/// <param name="uri">address of the Git repository.</param>
		/// <param name="local">
		/// the local repository that will communicate with the other Git
		/// repository.
		/// </param>
		/// <param name="remoteName">
		/// name of the remote, if the remote as configured in
		/// <code>local</code>
		/// ; otherwise null.
		/// </param>
		/// <returns>the transport.</returns>
		/// <exception cref="System.NotSupportedException">this protocol does not support the URI.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">the transport cannot open this URI.
		/// 	</exception>
		public abstract NGit.Transport.Transport Open(URIish uri, Repository local, string
			 remoteName);
	}
}
