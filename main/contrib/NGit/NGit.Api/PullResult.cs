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

using System.Text;
using NGit.Api;
using NGit.Transport;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// Encapsulates the result of a
	/// <see cref="PullCommand">PullCommand</see>
	/// </summary>
	public class PullResult
	{
		private readonly FetchResult fetchResult;

		private readonly MergeCommandResult mergeResult;

		private readonly RebaseResult rebaseResult;

		private readonly string fetchedFrom;

		internal PullResult(FetchResult fetchResult, string fetchedFrom, MergeCommandResult
			 mergeResult)
		{
			this.fetchResult = fetchResult;
			this.fetchedFrom = fetchedFrom;
			this.mergeResult = mergeResult;
			this.rebaseResult = null;
		}

		internal PullResult(FetchResult fetchResult, string fetchedFrom, RebaseResult rebaseResult
			)
		{
			this.fetchResult = fetchResult;
			this.fetchedFrom = fetchedFrom;
			this.mergeResult = null;
			this.rebaseResult = rebaseResult;
		}

		/// <returns>the fetch result, or <code>null</code></returns>
		public virtual FetchResult GetFetchResult()
		{
			return this.fetchResult;
		}

		/// <returns>the merge result, or <code>null</code></returns>
		public virtual MergeCommandResult GetMergeResult()
		{
			return this.mergeResult;
		}

		/// <returns>the rebase result, or <code>null</code></returns>
		public virtual RebaseResult GetRebaseResult()
		{
			return this.rebaseResult;
		}

		/// <returns>
		/// the name of the remote configuration from which fetch was tried,
		/// or <code>null</code>
		/// </returns>
		public virtual string GetFetchedFrom()
		{
			return this.fetchedFrom;
		}

		/// <returns>whether the pull was successful</returns>
		public virtual bool IsSuccessful()
		{
			if (mergeResult != null)
			{
				return mergeResult.GetMergeStatus().IsSuccessful();
			}
			else
			{
				if (rebaseResult != null)
				{
					return rebaseResult.GetStatus().IsSuccessful();
				}
			}
			return true;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			if (fetchResult != null)
			{
				sb.Append(fetchResult.ToString());
			}
			else
			{
				sb.Append("No fetch result");
			}
			sb.Append("\n");
			if (mergeResult != null)
			{
				sb.Append(mergeResult.ToString());
			}
			else
			{
				if (rebaseResult != null)
				{
					sb.Append(rebaseResult.ToString());
				}
				else
				{
					sb.Append("No update result");
				}
			}
			return sb.ToString();
		}
	}
}
