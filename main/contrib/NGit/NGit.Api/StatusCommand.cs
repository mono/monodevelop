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
using NGit.Api;
using NGit.Treewalk;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// A class used to execute a
	/// <code>Status</code>
	/// command. It has setters for all
	/// supported options and arguments of this command and a
	/// <see cref="Call()">Call()</see>
	/// method
	/// to finally execute the command. Each instance of this class should only be
	/// used for one invocation of the command (means: one call to
	/// <see cref="Call()">Call()</see>
	/// )
	/// </summary>
	/// <seealso><a
	/// *      href="http://www.kernel.org/pub/software/scm/git/docs/git-status.html"
	/// *      >Git documentation about Status</a></seealso>
	public class StatusCommand : GitCommand<Status>
	{
		private WorkingTreeIterator workingTreeIt;

		/// <param name="repo"></param>
		protected internal StatusCommand(Repository repo) : base(repo)
		{
		}

		/// <summary>
		/// Executes the
		/// <code>Status</code>
		/// command with all the options and parameters
		/// collected by the setter methods of this class. Each instance of this
		/// class should only be used for one invocation of the command. Don't call
		/// this method twice on an instance.
		/// </summary>
		/// <returns>
		/// a
		/// <see cref="Status">Status</see>
		/// object telling about each path where working
		/// tree, index or HEAD differ from each other.
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.NoWorkTreeException"></exception>
		public override Status Call()
		{
			if (workingTreeIt == null)
			{
				workingTreeIt = new FileTreeIterator(repo);
			}
			IndexDiff diff = new IndexDiff(repo, Constants.HEAD, workingTreeIt);
			diff.Diff();
			return new Status(diff);
		}

		/// <summary>
		/// To set the
		/// <see cref="NGit.Treewalk.WorkingTreeIterator">NGit.Treewalk.WorkingTreeIterator</see>
		/// which should be used. If this
		/// method is not called a standard
		/// <see cref="NGit.Treewalk.FileTreeIterator">NGit.Treewalk.FileTreeIterator</see>
		/// is used.
		/// </summary>
		/// <param name="workingTreeIt">a working tree iterator</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.StatusCommand SetWorkingTreeIt(WorkingTreeIterator workingTreeIt
			)
		{
			this.workingTreeIt = workingTreeIt;
			return this;
		}
	}
}
