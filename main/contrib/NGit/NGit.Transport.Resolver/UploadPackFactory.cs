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
using NGit.Transport;
using NGit.Transport.Resolver;
using Sharpen;

namespace NGit.Transport.Resolver
{
	/// <summary>
	/// Create and configure
	/// <see cref="NGit.Transport.UploadPack">NGit.Transport.UploadPack</see>
	/// service instance.
	/// </summary>
	/// <?></?>
	public abstract class UploadPackFactory<C>
	{
		private sealed class _UploadPackFactory_57 : UploadPackFactory<C>
		{
			public _UploadPackFactory_57()
			{
			}

			/// <exception cref="NGit.Transport.Resolver.ServiceNotEnabledException"></exception>
			public override UploadPack Create(C req, Repository db)
			{
				throw new ServiceNotEnabledException();
			}
		}

		/// <summary>A factory disabling the UploadPack service for all repositories.</summary>
		/// <remarks>A factory disabling the UploadPack service for all repositories.</remarks>
		public static readonly UploadPackFactory<C> DISABLED = new _UploadPackFactory_57();

		/// <summary>Create and configure a new UploadPack instance for a repository.</summary>
		/// <remarks>Create and configure a new UploadPack instance for a repository.</remarks>
		/// <param name="req">
		/// current request, in case information from the request may help
		/// configure the UploadPack instance.
		/// </param>
		/// <param name="db">the repository the upload would read from.</param>
		/// <returns>the newly configured UploadPack instance, must not be null.</returns>
		/// <exception cref="ServiceNotEnabledException">
		/// this factory refuses to create the instance because it is not
		/// allowed on the target repository, by any user.
		/// </exception>
		/// <exception cref="ServiceNotAuthorizedException">
		/// this factory refuses to create the instance for this HTTP
		/// request and repository, such as due to a permission error.
		/// </exception>
		/// <exception cref="NGit.Transport.Resolver.ServiceNotEnabledException"></exception>
		/// <exception cref="NGit.Transport.Resolver.ServiceNotAuthorizedException"></exception>
		public abstract UploadPack Create(C req, Repository db);
	}
}
