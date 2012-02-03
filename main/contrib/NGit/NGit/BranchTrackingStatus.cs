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
using NGit.Revwalk;
using NGit.Revwalk.Filter;
using Sharpen;

namespace NGit
{
	/// <summary>Status of a branch's relation to its remote-tracking branch.</summary>
	/// <remarks>Status of a branch's relation to its remote-tracking branch.</remarks>
	public class BranchTrackingStatus
	{
		/// <summary>
		/// Compute the tracking status for the <code>branchName</code> in
		/// <code>repository</code>.
		/// </summary>
		/// <remarks>
		/// Compute the tracking status for the <code>branchName</code> in
		/// <code>repository</code>.
		/// </remarks>
		/// <param name="repository">the git repository to compute the status from</param>
		/// <param name="branchName">the local branch</param>
		/// <returns>the tracking status, or null if it is not known</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public static NGit.BranchTrackingStatus Of(Repository repository, string branchName
			)
		{
			BranchConfig branchConfig = new BranchConfig(repository.GetConfig(), branchName);
			string remoteTrackingBranch = branchConfig.GetRemoteTrackingBranch();
			if (remoteTrackingBranch == null)
			{
				return null;
			}
			Ref tracking = repository.GetRef(remoteTrackingBranch);
			if (tracking == null)
			{
				return null;
			}
			Ref local = repository.GetRef(branchName);
			if (local == null)
			{
				return null;
			}
			RevWalk walk = new RevWalk(repository);
			RevCommit localCommit = walk.ParseCommit(local.GetObjectId());
			RevCommit trackingCommit = walk.ParseCommit(tracking.GetObjectId());
			walk.SetRevFilter(RevFilter.MERGE_BASE);
			walk.MarkStart(localCommit);
			walk.MarkStart(trackingCommit);
			RevCommit mergeBase = walk.Next();
			walk.Reset();
			walk.SetRevFilter(RevFilter.ALL);
			int aheadCount = RevWalkUtils.Count(walk, localCommit, mergeBase);
			int behindCount = RevWalkUtils.Count(walk, trackingCommit, mergeBase);
			return new NGit.BranchTrackingStatus(remoteTrackingBranch, aheadCount, behindCount
				);
		}

		private readonly string remoteTrackingBranch;

		private readonly int aheadCount;

		private readonly int behindCount;

		private BranchTrackingStatus(string remoteTrackingBranch, int aheadCount, int behindCount
			)
		{
			this.remoteTrackingBranch = remoteTrackingBranch;
			this.aheadCount = aheadCount;
			this.behindCount = behindCount;
		}

		/// <returns>full remote-tracking branch name</returns>
		public virtual string GetRemoteTrackingBranch()
		{
			return remoteTrackingBranch;
		}

		/// <returns>
		/// number of commits that the local branch is ahead of the
		/// remote-tracking branch
		/// </returns>
		public virtual int GetAheadCount()
		{
			return aheadCount;
		}

		/// <returns>
		/// number of commits that the local branch is behind of the
		/// remote-tracking branch
		/// </returns>
		public virtual int GetBehindCount()
		{
			return behindCount;
		}
	}
}
