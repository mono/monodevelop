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
using NGit.Api;
using NGit.Merge;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// The result of a
	/// <see cref="RebaseCommand">RebaseCommand</see>
	/// execution
	/// </summary>
	public class RebaseResult
	{
		/// <summary>The overall status</summary>
		public enum Status
		{
			OK,
			ABORTED,
			STOPPED,
			FAILED,
			UP_TO_DATE,
			FAST_FORWARD
		}

		internal static readonly NGit.Api.RebaseResult UP_TO_DATE_RESULT = new NGit.Api.RebaseResult
			(RebaseResult.Status.UP_TO_DATE);

		private readonly RebaseResult.Status mySatus;

		private readonly RevCommit currentCommit;

		private IDictionary<string, ResolveMerger.MergeFailureReason> failingPaths;

		internal RebaseResult(RebaseResult.Status status)
		{
			this.mySatus = status;
			currentCommit = null;
		}

		/// <summary>
		/// Create <code>RebaseResult</code> with status
		/// <see cref="Status.STOPPED">Status.STOPPED</see>
		/// </summary>
		/// <param name="commit">current commit</param>
		internal RebaseResult(RevCommit commit)
		{
			mySatus = RebaseResult.Status.STOPPED;
			currentCommit = commit;
		}

		/// <summary>
		/// Create <code>RebaseResult</code> with status
		/// <see cref="Status.FAILED">Status.FAILED</see>
		/// </summary>
		/// <param name="failingPaths">list of paths causing this rebase to fail</param>
		internal RebaseResult(IDictionary<string, ResolveMerger.MergeFailureReason> failingPaths
			)
		{
			mySatus = RebaseResult.Status.FAILED;
			currentCommit = null;
			this.failingPaths = failingPaths;
		}

		/// <returns>the overall status</returns>
		public virtual RebaseResult.Status GetStatus()
		{
			return mySatus;
		}

		/// <returns>
		/// the current commit if status is
		/// <see cref="Status.STOPPED">Status.STOPPED</see>
		/// , otherwise
		/// <code>null</code>
		/// </returns>
		public virtual RevCommit GetCurrentCommit()
		{
			return currentCommit;
		}

		/// <returns>
		/// the list of paths causing this rebase to fail (see
		/// <see cref="NGit.Merge.ResolveMerger.GetFailingPaths()">NGit.Merge.ResolveMerger.GetFailingPaths()
		/// 	</see>
		/// for details) if status is
		/// <see cref="Status.FAILED">Status.FAILED</see>
		/// , otherwise <code>null</code>
		/// </returns>
		public virtual IDictionary<string, ResolveMerger.MergeFailureReason> GetFailingPaths
			()
		{
			return failingPaths;
		}
	}
}
