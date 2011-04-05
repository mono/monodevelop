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

using System.IO;
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using Sharpen;

namespace NGit.Api
{
	/// <summary>Create an empty git repository or reinitalize an existing one</summary>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-init.html"
	/// *      >Git documentation about init</a></seealso>
	public class InitCommand : Callable<Git>
	{
		private FilePath directory;

		private bool bare;

		/// <summary>
		/// Executes the
		/// <code>Init</code>
		/// command.
		/// </summary>
		/// <exception cref="NGit.Api.Errors.JGitInternalException">if the repository can't be created
		/// 	</exception>
		/// <returns>
		/// the newly created
		/// <code>Git</code>
		/// object with associated repository
		/// </returns>
		public virtual Git Call()
		{
			try
			{
				RepositoryBuilder builder = new RepositoryBuilder();
				if (bare)
				{
					builder.SetBare();
				}
				builder.ReadEnvironment();
				if (directory != null)
				{
					FilePath d = directory;
					if (!bare)
					{
						d = new FilePath(d, Constants.DOT_GIT);
					}
					builder.SetGitDir(d);
				}
				else
				{
					if (builder.GetGitDir() == null)
					{
						FilePath d = new FilePath(".");
						if (d.GetParentFile() != null)
						{
							d = d.GetParentFile();
						}
						if (!bare)
						{
							d = new FilePath(d, Constants.DOT_GIT);
						}
						builder.SetGitDir(d);
					}
				}
				Repository repository = builder.Build();
				if (!repository.ObjectDatabase.Exists())
				{
					repository.Create(bare);
				}
				return new Git(repository);
			}
			catch (IOException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
		}

		/// <summary>The optional directory associated with the init operation.</summary>
		/// <remarks>
		/// The optional directory associated with the init operation. If no
		/// directory is set, we'll use the current directory
		/// </remarks>
		/// <param name="directory">the directory to init to</param>
		/// <returns>this instance</returns>
		public virtual InitCommand SetDirectory(FilePath directory)
		{
			this.directory = directory;
			return this;
		}

		/// <param name="bare">whether the repository is bare or not</param>
		/// <returns>this instance</returns>
		public virtual InitCommand SetBare(bool bare)
		{
			this.bare = bare;
			return this;
		}
	}
}
