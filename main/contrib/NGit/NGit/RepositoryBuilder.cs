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
using Sharpen;

namespace NGit
{
	/// <summary>
	/// Base class to support constructing a
	/// <see cref="Repository">Repository</see>
	/// .
	/// <p>
	/// Applications must set one of
	/// <see cref="BaseRepositoryBuilder{B, R}.SetGitDir(Sharpen.FilePath)">BaseRepositoryBuilder&lt;B, R&gt;.SetGitDir(Sharpen.FilePath)
	/// 	</see>
	/// or
	/// <see cref="BaseRepositoryBuilder{B, R}.SetWorkTree(Sharpen.FilePath)">BaseRepositoryBuilder&lt;B, R&gt;.SetWorkTree(Sharpen.FilePath)
	/// 	</see>
	/// , or use
	/// <see cref="BaseRepositoryBuilder{B, R}.ReadEnvironment()">BaseRepositoryBuilder&lt;B, R&gt;.ReadEnvironment()
	/// 	</see>
	/// or
	/// <see cref="BaseRepositoryBuilder{B, R}.FindGitDir()">BaseRepositoryBuilder&lt;B, R&gt;.FindGitDir()
	/// 	</see>
	/// in order to configure the minimum property set
	/// necessary to open a repository.
	/// <p>
	/// Single repository applications trying to be compatible with other Git
	/// implementations are encouraged to use a model such as:
	/// <pre>
	/// new RepositoryBuilder() //
	/// .setGitDir(gitDirArgument) // --git-dir if supplied, no-op if null
	/// .readEnviroment() // scan environment GIT_* variables
	/// .findGitDir() // scan up the file system tree
	/// .build()
	/// </pre>
	/// </summary>
	/// <seealso cref="NGit.Storage.File.FileRepositoryBuilder">NGit.Storage.File.FileRepositoryBuilder
	/// 	</seealso>
	public class RepositoryBuilder : BaseRepositoryBuilder<RepositoryBuilder, Repository
		>
	{
		// Empty implementation, everything is inherited.
	}
}
