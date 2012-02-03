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
using System.Text;
using NGit;
using NGit.Merge;
using NGit.Util;
using Sharpen;

namespace NGit.Merge
{
	/// <summary>Formatter for constructing the commit message for a merge commit.</summary>
	/// <remarks>
	/// Formatter for constructing the commit message for a merge commit.
	/// <p>
	/// The format should be the same as C Git does it, for compatibility.
	/// </remarks>
	public class MergeMessageFormatter
	{
		/// <summary>Construct the merge commit message.</summary>
		/// <remarks>Construct the merge commit message.</remarks>
		/// <param name="refsToMerge">the refs which will be merged</param>
		/// <param name="target">the branch ref which will be merged into</param>
		/// <returns>merge commit message</returns>
		public virtual string Format(IList<Ref> refsToMerge, Ref target)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("Merge ");
			IList<string> branches = new AList<string>();
			IList<string> remoteBranches = new AList<string>();
			IList<string> tags = new AList<string>();
			IList<string> commits = new AList<string>();
			IList<string> others = new AList<string>();
			foreach (Ref @ref in refsToMerge)
			{
				if (@ref.GetName().StartsWith(Constants.R_HEADS))
				{
					branches.AddItem("'" + Repository.ShortenRefName(@ref.GetName()) + "'");
				}
				else
				{
					if (@ref.GetName().StartsWith(Constants.R_REMOTES))
					{
						remoteBranches.AddItem("'" + Repository.ShortenRefName(@ref.GetName()) + "'");
					}
					else
					{
						if (@ref.GetName().StartsWith(Constants.R_TAGS))
						{
							tags.AddItem("'" + Repository.ShortenRefName(@ref.GetName()) + "'");
						}
						else
						{
							if (@ref.GetName().Equals(@ref.GetObjectId().GetName()))
							{
								commits.AddItem("'" + @ref.GetName() + "'");
							}
							else
							{
								others.AddItem(@ref.GetName());
							}
						}
					}
				}
			}
			IList<string> listings = new AList<string>();
			if (!branches.IsEmpty())
			{
				listings.AddItem(JoinNames(branches, "branch", "branches"));
			}
			if (!remoteBranches.IsEmpty())
			{
				listings.AddItem(JoinNames(remoteBranches, "remote-tracking branch", "remote-tracking branches"
					));
			}
			if (!tags.IsEmpty())
			{
				listings.AddItem(JoinNames(tags, "tag", "tags"));
			}
			if (!commits.IsEmpty())
			{
				listings.AddItem(JoinNames(commits, "commit", "commits"));
			}
			if (!others.IsEmpty())
			{
				listings.AddItem(StringUtils.Join(others, ", ", " and "));
			}
			sb.Append(StringUtils.Join(listings, ", "));
			string targetName = target.GetLeaf().GetName();
			if (!targetName.Equals(Constants.R_HEADS + Constants.MASTER))
			{
				string targetShortName = Repository.ShortenRefName(targetName);
				sb.Append(" into " + targetShortName);
			}
			return sb.ToString();
		}

		/// <summary>Add section with conflicting paths to merge message.</summary>
		/// <remarks>Add section with conflicting paths to merge message.</remarks>
		/// <param name="message">the original merge message</param>
		/// <param name="conflictingPaths">the paths with conflicts</param>
		/// <returns>merge message with conflicting paths added</returns>
		public virtual string FormatWithConflicts(string message, IList<string> conflictingPaths
			)
		{
			StringBuilder sb = new StringBuilder(message);
			if (!message.EndsWith("\n"))
			{
				sb.Append("\n");
			}
			sb.Append("\n");
			sb.Append("Conflicts:\n");
			foreach (string conflictingPath in conflictingPaths)
			{
				sb.Append('\t').Append(conflictingPath).Append('\n');
			}
			return sb.ToString();
		}

		private static string JoinNames(IList<string> names, string singular, string plural
			)
		{
			if (names.Count == 1)
			{
				return singular + " " + names[0];
			}
			else
			{
				return plural + " " + StringUtils.Join(names, ", ", " and ");
			}
		}
	}
}
