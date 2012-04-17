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

using NGit;
using NGit.Errors;
using NGit.Transport.Resolver;
using Sharpen;

namespace NGit.Transport.Resolver
{
	/// <summary>
	/// Locate a Git
	/// <see cref="NGit.Repository">NGit.Repository</see>
	/// by name from the URL.
	/// </summary>
	/// <?></?>
	public abstract class RepositoryResolver<C>
	{
		private sealed class _RepositoryResolver_58 : RepositoryResolver<C>
		{
			public _RepositoryResolver_58()
			{
			}

			/// <exception cref="NGit.Errors.RepositoryNotFoundException"></exception>
			public override Repository Open(C req, string name)
			{
				throw new RepositoryNotFoundException(name);
			}
		}

		/// <summary>Resolver configured to open nothing.</summary>
		/// <remarks>Resolver configured to open nothing.</remarks>
		public static readonly RepositoryResolver<C> NONE = new _RepositoryResolver_58();

		/// <summary>
		/// Locate and open a reference to a
		/// <see cref="NGit.Repository">NGit.Repository</see>
		/// .
		/// <p>
		/// The caller is responsible for closing the returned Repository.
		/// </summary>
		/// <param name="req">
		/// the current request, may be used to inspect session state
		/// including cookies or user authentication.
		/// </param>
		/// <param name="name">name of the repository, as parsed out of the URL.</param>
		/// <returns>the opened repository instance, never null.</returns>
		/// <exception cref="NGit.Errors.RepositoryNotFoundException">
		/// the repository does not exist or the name is incorrectly
		/// formatted as a repository name.
		/// </exception>
		/// <exception cref="ServiceNotAuthorizedException">
		/// the repository may exist, but HTTP access is not allowed
		/// without authentication, i.e. this corresponds to an HTTP 401
		/// Unauthorized.
		/// </exception>
		/// <exception cref="ServiceNotEnabledException">
		/// the repository may exist, but HTTP access is not allowed on the
		/// target repository, for the current user.
		/// </exception>
		/// <exception cref="NGit.Transport.ServiceMayNotContinueException">
		/// the repository may exist, but HTTP access is not allowed for
		/// the current request. The exception message contains a detailed
		/// message that should be shown to the user.
		/// </exception>
		/// <exception cref="NGit.Transport.Resolver.ServiceNotAuthorizedException"></exception>
		/// <exception cref="NGit.Transport.Resolver.ServiceNotEnabledException"></exception>
		public abstract Repository Open(C req, string name);
	}
}
