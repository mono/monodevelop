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
using NGit;
using NGit.Api;
using NGit.Merge;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// Encapsulates the result of a
	/// <see cref="CherryPickCommand">CherryPickCommand</see>
	/// .
	/// </summary>
	public class CherryPickResult
	{
		/// <summary>The cherry-pick status</summary>
		public enum CherryPickStatus
		{
			OK,
			FAILED,
			CONFLICTING
		}

		private readonly CherryPickResult.CherryPickStatus status;

		private readonly RevCommit newHead;

		private readonly IList<Ref> cherryPickedRefs;

		private readonly IDictionary<string, ResolveMerger.MergeFailureReason> failingPaths;

		/// <param name="newHead">commit the head points at after this cherry-pick</param>
		/// <param name="cherryPickedRefs">list of successfully cherry-picked <code>Ref</code>'s
		/// 	</param>
		public CherryPickResult(RevCommit newHead, IList<Ref> cherryPickedRefs)
		{
			this.status = CherryPickResult.CherryPickStatus.OK;
			this.newHead = newHead;
			this.cherryPickedRefs = cherryPickedRefs;
			this.failingPaths = null;
		}

		/// <param name="failingPaths">
		/// list of paths causing this cherry-pick to fail (see
		/// <see cref="NGit.Merge.ResolveMerger.GetFailingPaths()">NGit.Merge.ResolveMerger.GetFailingPaths()
		/// 	</see>
		/// for details)
		/// </param>
		public CherryPickResult(IDictionary<string, ResolveMerger.MergeFailureReason> failingPaths
			)
		{
			this.status = CherryPickResult.CherryPickStatus.FAILED;
			this.newHead = null;
			this.cherryPickedRefs = null;
			this.failingPaths = failingPaths;
		}

		private CherryPickResult(CherryPickResult.CherryPickStatus status)
		{
			this.status = status;
			this.newHead = null;
			this.cherryPickedRefs = null;
			this.failingPaths = null;
		}

		/// <summary>
		/// A <code>CherryPickResult</code> with status
		/// <see cref="CherryPickStatus.CONFLICTING">CherryPickStatus.CONFLICTING</see>
		/// </summary>
		public static NGit.Api.CherryPickResult CONFLICT = new NGit.Api.CherryPickResult(
			CherryPickResult.CherryPickStatus.CONFLICTING);

		/// <returns>the status this cherry-pick resulted in</returns>
		public virtual CherryPickResult.CherryPickStatus GetStatus()
		{
			return status;
		}

		/// <returns>
		/// the commit the head points at after this cherry-pick,
		/// <code>null</code> if
		/// <see cref="GetStatus()">GetStatus()</see>
		/// is not
		/// <see cref="CherryPickStatus.OK">CherryPickStatus.OK</see>
		/// </returns>
		public virtual RevCommit GetNewHead()
		{
			return newHead;
		}

		/// <returns>
		/// the list of successfully cherry-picked <code>Ref</code>'s,
		/// <code>null</code> if
		/// <see cref="GetStatus()">GetStatus()</see>
		/// is not
		/// <see cref="CherryPickStatus.OK">CherryPickStatus.OK</see>
		/// </returns>
		public virtual IList<Ref> GetCherryPickedRefs()
		{
			return cherryPickedRefs;
		}

		/// <returns>
		/// the list of paths causing this cherry-pick to fail (see
		/// <see cref="NGit.Merge.ResolveMerger.GetFailingPaths()">NGit.Merge.ResolveMerger.GetFailingPaths()
		/// 	</see>
		/// for details),
		/// <code>null</code> if
		/// <see cref="GetStatus()">GetStatus()</see>
		/// is not
		/// <see cref="CherryPickStatus.FAILED">CherryPickStatus.FAILED</see>
		/// </returns>
		public virtual IDictionary<string, ResolveMerger.MergeFailureReason> GetFailingPaths
			()
		{
			return failingPaths;
		}
	}
}
