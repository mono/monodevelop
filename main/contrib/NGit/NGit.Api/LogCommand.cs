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
using NGit.Errors;
using NGit.Revwalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// A class used to execute a
	/// <code>Log</code>
	/// command. It has setters for all
	/// supported options and arguments of this command and a
	/// <see cref="Call()">Call()</see>
	/// method
	/// to finally execute the command. Each instance of this class should only be
	/// used for one invocation of the command (means: one call to
	/// <see cref="Call()">Call()</see>
	/// )
	/// <p>
	/// This is currently a very basic implementation which takes only one starting
	/// revision as option.
	/// TODO: add more options (revision ranges, sorting, ...)
	/// </summary>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-log.html"
	/// *      >Git documentation about Log</a></seealso>
	public class LogCommand : GitCommand<Iterable<RevCommit>>
	{
		private RevWalk walk;

		private bool startSpecified = false;

		private readonly IList<PathFilter> pathFilters = new AList<PathFilter>();

		/// <param name="repo"></param>
		protected internal LogCommand(Repository repo) : base(repo)
		{
			walk = new RevWalk(repo);
		}

		/// <summary>
		/// Executes the
		/// <code>Log</code>
		/// command with all the options and parameters
		/// collected by the setter methods (e.g.
		/// <see cref="Add(NGit.AnyObjectId)">Add(NGit.AnyObjectId)</see>
		/// ,
		/// <see cref="Not(NGit.AnyObjectId)">Not(NGit.AnyObjectId)</see>
		/// , ..) of this class. Each instance of this class
		/// should only be used for one invocation of the command. Don't call this
		/// method twice on an instance.
		/// </summary>
		/// <returns>an iteration over RevCommits</returns>
		/// <exception cref="NGit.Api.Errors.NoHeadException"></exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		public override Iterable<RevCommit> Call()
		{
			CheckCallable();
			if (pathFilters.Count > 0)
			{
				walk.SetTreeFilter(PathFilterGroup.Create(pathFilters));
			}
			if (!startSpecified)
			{
				try
				{
					ObjectId headId = repo.Resolve(Constants.HEAD);
					if (headId == null)
					{
						throw new NoHeadException(JGitText.Get().noHEADExistsAndNoExplicitStartingRevisionWasSpecified
							);
					}
					Add(headId);
				}
				catch (IOException e)
				{
					// all exceptions thrown by add() shouldn't occur and represent
					// severe low-level exception which are therefore wrapped
					throw new JGitInternalException(JGitText.Get().anExceptionOccurredWhileTryingToAddTheIdOfHEAD
						, e);
				}
			}
			SetCallable(false);
			return walk;
		}

		/// <summary>Mark a commit to start graph traversal from.</summary>
		/// <remarks>Mark a commit to start graph traversal from.</remarks>
		/// <seealso cref="NGit.Revwalk.RevWalk.MarkStart(NGit.Revwalk.RevCommit)">NGit.Revwalk.RevWalk.MarkStart(NGit.Revwalk.RevCommit)
		/// 	</seealso>
		/// <param name="start"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// the commit supplied is not available from the object
		/// database. This usually indicates the supplied commit is
		/// invalid, but the reference was constructed during an earlier
		/// invocation to
		/// <see cref="NGit.Revwalk.RevWalk.LookupCommit(NGit.AnyObjectId)">NGit.Revwalk.RevWalk.LookupCommit(NGit.AnyObjectId)
		/// 	</see>
		/// .
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the object was not parsed yet and it was discovered during
		/// parsing that it is not actually a commit. This usually
		/// indicates the caller supplied a non-commit SHA-1 to
		/// <see cref="NGit.Revwalk.RevWalk.LookupCommit(NGit.AnyObjectId)">NGit.Revwalk.RevWalk.LookupCommit(NGit.AnyObjectId)
		/// 	</see>
		/// .
		/// </exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException">
		/// a low-level exception of JGit has occurred. The original
		/// exception can be retrieved by calling
		/// <see cref="System.Exception.InnerException()">System.Exception.InnerException()</see>
		/// . Expect only
		/// <code>IOException's</code>
		/// to be wrapped. Subclasses of
		/// <see cref="System.IO.IOException">System.IO.IOException</see>
		/// (e.g.
		/// <see cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
		/// 	</see>
		/// ) are
		/// typically not wrapped here but thrown as original exception
		/// </exception>
		public virtual NGit.Api.LogCommand Add(AnyObjectId start)
		{
			return Add(true, start);
		}

		/// <summary>
		/// Same as
		/// <code>--not start</code>
		/// , or
		/// <code>^start</code>
		/// </summary>
		/// <param name="start"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// the commit supplied is not available from the object
		/// database. This usually indicates the supplied commit is
		/// invalid, but the reference was constructed during an earlier
		/// invocation to
		/// <see cref="NGit.Revwalk.RevWalk.LookupCommit(NGit.AnyObjectId)">NGit.Revwalk.RevWalk.LookupCommit(NGit.AnyObjectId)
		/// 	</see>
		/// .
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the object was not parsed yet and it was discovered during
		/// parsing that it is not actually a commit. This usually
		/// indicates the caller supplied a non-commit SHA-1 to
		/// <see cref="NGit.Revwalk.RevWalk.LookupCommit(NGit.AnyObjectId)">NGit.Revwalk.RevWalk.LookupCommit(NGit.AnyObjectId)
		/// 	</see>
		/// .
		/// </exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException">
		/// a low-level exception of JGit has occurred. The original
		/// exception can be retrieved by calling
		/// <see cref="System.Exception.InnerException()">System.Exception.InnerException()</see>
		/// . Expect only
		/// <code>IOException's</code>
		/// to be wrapped. Subclasses of
		/// <see cref="System.IO.IOException">System.IO.IOException</see>
		/// (e.g.
		/// <see cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
		/// 	</see>
		/// ) are
		/// typically not wrapped here but thrown as original exception
		/// </exception>
		public virtual NGit.Api.LogCommand Not(AnyObjectId start)
		{
			return Add(false, start);
		}

		/// <summary>
		/// Adds the range
		/// <code>since..until</code>
		/// </summary>
		/// <param name="since"></param>
		/// <param name="until"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// the commit supplied is not available from the object
		/// database. This usually indicates the supplied commit is
		/// invalid, but the reference was constructed during an earlier
		/// invocation to
		/// <see cref="NGit.Revwalk.RevWalk.LookupCommit(NGit.AnyObjectId)">NGit.Revwalk.RevWalk.LookupCommit(NGit.AnyObjectId)
		/// 	</see>
		/// .
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the object was not parsed yet and it was discovered during
		/// parsing that it is not actually a commit. This usually
		/// indicates the caller supplied a non-commit SHA-1 to
		/// <see cref="NGit.Revwalk.RevWalk.LookupCommit(NGit.AnyObjectId)">NGit.Revwalk.RevWalk.LookupCommit(NGit.AnyObjectId)
		/// 	</see>
		/// .
		/// </exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException">
		/// a low-level exception of JGit has occurred. The original
		/// exception can be retrieved by calling
		/// <see cref="System.Exception.InnerException()">System.Exception.InnerException()</see>
		/// . Expect only
		/// <code>IOException's</code>
		/// to be wrapped. Subclasses of
		/// <see cref="System.IO.IOException">System.IO.IOException</see>
		/// (e.g.
		/// <see cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
		/// 	</see>
		/// ) are
		/// typically not wrapped here but thrown as original exception
		/// </exception>
		public virtual NGit.Api.LogCommand AddRange(AnyObjectId since, AnyObjectId until)
		{
			return Not(since).Add(until);
		}

		/// <summary>Show only commits that affect any of the specified paths.</summary>
		/// <remarks>
		/// Show only commits that affect any of the specified paths. The path must
		/// either name a file or a directory exactly. Note that regex expressions or
		/// wildcards are not supported.
		/// </remarks>
		/// <param name="path">a path is relative to the top level of the repository</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.LogCommand AddPath(string path)
		{
			CheckCallable();
			pathFilters.AddItem(PathFilter.Create(path));
			return this;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		private NGit.Api.LogCommand Add(bool include, AnyObjectId start)
		{
			CheckCallable();
			try
			{
				if (include)
				{
					walk.MarkStart(walk.LookupCommit(start));
					startSpecified = true;
				}
				else
				{
					walk.MarkUninteresting(walk.LookupCommit(start));
				}
				return this;
			}
			catch (MissingObjectException e)
			{
				throw;
			}
			catch (IncorrectObjectTypeException e)
			{
				throw;
			}
			catch (IOException e)
			{
				throw new JGitInternalException(MessageFormat.Format(JGitText.Get().exceptionOccurredDuringAddingOfOptionToALogCommand
					, start), e);
			}
		}
	}
}
