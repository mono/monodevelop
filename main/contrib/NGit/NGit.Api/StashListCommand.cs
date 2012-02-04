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
using System.IO;
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Revwalk;
using NGit.Storage.File;
using Sharpen;

namespace NGit.Api
{
	/// <summary>Command class to list the stashed commits in a repository.</summary>
	/// <remarks>Command class to list the stashed commits in a repository.</remarks>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-stash.html"
	/// *      >Git documentation about Stash</a></seealso>
	public class StashListCommand : GitCommand<ICollection<RevCommit>>
	{
		/// <summary>Create a new stash list command</summary>
		/// <param name="repo"></param>
		protected internal StashListCommand(Repository repo) : base(repo)
		{
		}

		/// <exception cref="System.Exception"></exception>
		public override ICollection<RevCommit> Call()
		{
			CheckCallable();
			try
			{
				if (repo.GetRef(Constants.R_STASH) == null)
				{
					return Sharpen.Collections.EmptyList<RevCommit>();
				}
			}
			catch (IOException e)
			{
				throw new InvalidRefNameException(MessageFormat.Format(JGitText.Get().cannotRead, 
					Constants.R_STASH), e);
			}
			ReflogCommand refLog = new ReflogCommand(repo);
			refLog.SetRef(Constants.R_STASH);
			ICollection<ReflogEntry> stashEntries = refLog.Call();
			if (stashEntries.IsEmpty())
			{
				return Sharpen.Collections.EmptyList<RevCommit>();
			}
			IList<RevCommit> stashCommits = new AList<RevCommit>(stashEntries.Count);
			RevWalk walk = new RevWalk(repo);
			walk.SetRetainBody(true);
			try
			{
				foreach (ReflogEntry entry in stashEntries)
				{
					try
					{
						stashCommits.AddItem(walk.ParseCommit(entry.GetNewId()));
					}
					catch (IOException e)
					{
						throw new JGitInternalException(MessageFormat.Format(JGitText.Get().cannotReadCommit
							, entry.GetNewId()), e);
					}
				}
			}
			finally
			{
				walk.Dispose();
			}
			return stashCommits;
		}
	}
}
