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
		public abstract class Status
		{
			public static RebaseResult.Status OK = new RebaseResult.Status.OK_Class();

			public static RebaseResult.Status ABORTED = new RebaseResult.Status.ABORTED_Class
				();

			public static RebaseResult.Status STOPPED = new RebaseResult.Status.STOPPED_Class
				();

			public static RebaseResult.Status FAILED = new RebaseResult.Status.FAILED_Class();

			public static RebaseResult.Status UP_TO_DATE = new RebaseResult.Status.UP_TO_DATE_Class
				();

			public static RebaseResult.Status FAST_FORWARD = new RebaseResult.Status.FAST_FORWARD_Class
				();

			public static RebaseResult.Status NOTHING_TO_COMMIT = new RebaseResult.Status.NOTHING_TO_COMMIT_Class
				();

			internal class OK_Class : RebaseResult.Status
			{
				public override bool IsSuccessful()
				{
					return true;
				}
			}

			internal class ABORTED_Class : RebaseResult.Status
			{
				public override bool IsSuccessful()
				{
					return false;
				}
			}

			internal class STOPPED_Class : RebaseResult.Status
			{
				public override bool IsSuccessful()
				{
					return false;
				}
			}

			internal class FAILED_Class : RebaseResult.Status
			{
				public override bool IsSuccessful()
				{
					return false;
				}
			}

			internal class UP_TO_DATE_Class : RebaseResult.Status
			{
				public override bool IsSuccessful()
				{
					return true;
				}
			}

			internal class FAST_FORWARD_Class : RebaseResult.Status
			{
				public override bool IsSuccessful()
				{
					return true;
				}
			}

			internal class NOTHING_TO_COMMIT_Class : RebaseResult.Status
			{
				public override bool IsSuccessful()
				{
					return false;
				}
			}

			public abstract bool IsSuccessful();
		}

		internal static readonly RebaseResult OK_RESULT = new RebaseResult(RebaseResult.Status
			.OK);

		internal static readonly RebaseResult ABORTED_RESULT = new RebaseResult(RebaseResult.Status
			.ABORTED);

		internal static readonly RebaseResult UP_TO_DATE_RESULT = new RebaseResult(RebaseResult.Status
			.UP_TO_DATE);

		internal static readonly RebaseResult FAST_FORWARD_RESULT = new RebaseResult(RebaseResult.Status
			.FAST_FORWARD);

		internal static readonly RebaseResult NOTHING_TO_COMMIT_RESULT = new RebaseResult
			(RebaseResult.Status.NOTHING_TO_COMMIT);

		private readonly RebaseResult.Status status;

		private readonly RevCommit currentCommit;

		private IDictionary<string, ResolveMerger.MergeFailureReason> failingPaths;

		private RebaseResult(RebaseResult.Status status)
		{
			this.status = status;
			currentCommit = null;
		}

		/// <summary>
		/// Create <code>RebaseResult</code> with status
		/// <see cref="Status.STOPPED">Status.STOPPED</see>
		/// </summary>
		/// <param name="commit">current commit</param>
		internal RebaseResult(RevCommit commit)
		{
			status = RebaseResult.Status.STOPPED;
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
			status = RebaseResult.Status.FAILED;
			currentCommit = null;
			this.failingPaths = failingPaths;
		}

		/// <returns>the overall status</returns>
		public virtual RebaseResult.Status GetStatus()
		{
			return status;
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
