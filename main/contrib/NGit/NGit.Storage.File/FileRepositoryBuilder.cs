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
using NGit.Storage.File;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// Constructs a
	/// <see cref="FileRepository">FileRepository</see>
	/// .
	/// <p>
	/// Applications must set one of
	/// <see cref="NGit.BaseRepositoryBuilder{B, R}.SetGitDir(Sharpen.FilePath)">NGit.BaseRepositoryBuilder&lt;B, R&gt;.SetGitDir(Sharpen.FilePath)
	/// 	</see>
	/// or
	/// <see cref="NGit.BaseRepositoryBuilder{B, R}.SetWorkTree(Sharpen.FilePath)">NGit.BaseRepositoryBuilder&lt;B, R&gt;.SetWorkTree(Sharpen.FilePath)
	/// 	</see>
	/// , or use
	/// <see cref="NGit.BaseRepositoryBuilder{B, R}.ReadEnvironment()">NGit.BaseRepositoryBuilder&lt;B, R&gt;.ReadEnvironment()
	/// 	</see>
	/// or
	/// <see cref="NGit.BaseRepositoryBuilder{B, R}.FindGitDir()">NGit.BaseRepositoryBuilder&lt;B, R&gt;.FindGitDir()
	/// 	</see>
	/// in order to configure the minimum property set
	/// necessary to open a repository.
	/// <p>
	/// Single repository applications trying to be compatible with other Git
	/// implementations are encouraged to use a model such as:
	/// <pre>
	/// new FileRepositoryBuilder() //
	/// .setGitDir(gitDirArgument) // --git-dir if supplied, no-op if null
	/// .readEnviroment() // scan environment GIT_* variables
	/// .findGitDir() // scan up the file system tree
	/// .build()
	/// </pre>
	/// </summary>
	public class FileRepositoryBuilder : BaseRepositoryBuilder<FileRepositoryBuilder, 
		FileRepository>
	{
		/// <summary>Create a repository matching the configuration in this builder.</summary>
		/// <remarks>
		/// Create a repository matching the configuration in this builder.
		/// <p>
		/// If an option was not set, the build method will try to default the option
		/// based on other options. If insufficient information is available, an
		/// exception is thrown to the caller.
		/// </remarks>
		/// <returns>a repository matching this configuration.</returns>
		/// <exception cref="System.ArgumentException">insufficient parameters were set.</exception>
		/// <exception cref="System.IO.IOException">
		/// the repository could not be accessed to configure the rest of
		/// the builder's parameters.
		/// </exception>
		public override FileRepository Build()
		{
			return new FileRepository(Setup());
		}
	}
}
