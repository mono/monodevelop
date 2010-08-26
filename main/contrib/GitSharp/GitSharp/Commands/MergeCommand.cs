/*
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Core.Merge;

namespace GitSharp.Commands
{
	public static class MergeCommand
	{
		public static MergeResult Execute(MergeOptions options)
		{
			options.Validate();

			var merger = SelectMerger(options);
			bool success = merger.Merge(options.Commits.Select(c => ((Core.Commit)c).CommitId).ToArray());
			var result = new MergeResult { Success = success };
			result.Tree = new Tree(options.Repository, merger.GetResultTreeId());
			if (options.NoCommit)
			{

			}
			else
			{
				if (string.IsNullOrEmpty(options.Message))
				{
					options.Message = FormatMergeMessage(options);
				}
				var author = Author.GetDefaultAuthor(options.Repository);
				result.Commit = Commit.Create(options.Message, options.Commits, result.Tree, author, author, DateTimeOffset.Now);
				if (options.Branches.Length >= 1 && options.Branches[0] is Branch)
					Ref.Update("refs/heads/" + options.Branches[0].Name, result.Commit);
			}
			return result;
		}

		private static string FormatMergeMessage(MergeOptions options)
		{
			if (options.Branches.Length > 0 && options.Branches[0] is Branch)
				return string.Format("Merge branch '{0}' into {1}", options.Branches[1].Name, options.Branches[0].Name);
			else
				return "Merge commits: " + string.Join(", ", options.Commits.Select(c => c.Hash).ToArray()); // todo: replace this fallback message with something sensible.
		}

		private static Merger SelectMerger(MergeOptions options)
		{
			switch (options.MergeStrategy)
			{
				case MergeStrategy.Ours:
					return Core.Merge.MergeStrategy.Ours.NewMerger(options.Repository);
				case MergeStrategy.Theirs:
					return Core.Merge.MergeStrategy.Theirs.NewMerger(options.Repository);
				case MergeStrategy.Recursive:
					return Core.Merge.MergeStrategy.SimpleTwoWayInCore.NewMerger(options.Repository);
			}
			throw new ArgumentException("Invalid merge option: "+options.MergeStrategy);
		}

	}


	public enum MergeStrategy { Ours, Theirs, Recursive }

	public class MergeOptions
	{
		public MergeOptions()
		{
			NoCommit = false;
			NoFastForward = false;
		}

		internal Repository Repository { get; set; }

		/// <summary>
		/// Commit message of the merge. If left empty or null a good default message will be provided by the merge command.
		/// </summary>
		public string Message { get; set; }

		public MergeStrategy MergeStrategy { get; set; }

		private Ref[] _branches;

		/// <summary>
		/// The branches to merge. This automatically sets the Commits property.
		/// </summary>
		public Ref[] Branches
		{
			get { return _branches; }
			set
			{
				_branches = value;
				if (value != null)
					Commits = value.Select(b => b.Target as Commit).ToArray();
				if (_branches != null && _branches.Length > 0 && _branches[0] != null)
					Repository = _branches[0]._repo;
			}
		}

		/// <summary>
		/// The commits to merge, set this only if you can not specify the branches.
		/// </summary>
		public Commit[] Commits { get; set; }

		/// <summary>
		/// With NoCommit=true MergeCommand performs the merge but pretends the merge failed and does not autocommit, to give the user a chance to inspect and further tweak the merge result before committing.
		/// By default MergeCommand performs the merge and committs the result (the default value is false).
		/// </summary>
		public bool NoCommit { get; set; }

		/// <summary>
		/// When true Generate a merge commit even if the merge resolved as a fast-forward. 
		/// MergeCommand by default does not generate a merge commit if the merge resolved as a fast-forward, only updates the branch pointer (the default value is false).
		/// </summary>
		public bool NoFastForward { get; set; }


		public bool Log { get; set; }

		public void Validate()
		{
			if (Repository == null)
				throw new ArgumentException("Repository must not be null");
			if (Commits.Count() < 2)
				throw new ArgumentException("Need at least two commits to merge");
		}
	}

	public class MergeResult
	{
		/// <summary>
		/// True if the merge was sucessful. In case of conflicts or the strategy not being able to conduct the merge this is false.
		/// </summary>
		public bool Success { get; set; }

		/// <summary>
		/// Result object of the merge command. If MergeOptions.NoCommit == true this is null.
		/// </summary>
		public Commit Commit { get; set; }

		/// <summary>
		/// Resulting tree. This property is especially useful when merging with option NoCommit == true.
		/// </summary>
		public Tree Tree { get; set; }

	}
}
