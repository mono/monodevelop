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
using NGit.Internal;
using Sharpen;

namespace NGit
{
	/// <summary>
	/// Important state of the repository that affects what can and cannot bed
	/// done.
	/// </summary>
	/// <remarks>
	/// Important state of the repository that affects what can and cannot bed
	/// done. This is things like unhandled conflicted merges and unfinished rebase.
	/// The granularity and set of states are somewhat arbitrary. The methods
	/// on the state are the only supported means of deciding what to do.
	/// </remarks>
	public abstract class RepositoryState
	{
		/// <summary>Has no work tree and cannot be used for normal editing.</summary>
		/// <remarks>Has no work tree and cannot be used for normal editing.</remarks>
		public static RepositoryState BARE = new RepositoryState.BARE_Class();

		internal class BARE_Class : RepositoryState
		{
			public override bool CanCheckout()
			{
				return false;
			}

			public override bool CanResetHead()
			{
				return false;
			}

			public override bool CanCommit()
			{
				return false;
			}

			public override bool CanAmend()
			{
				return false;
			}

			public override string GetDescription()
			{
				return "Bare";
			}

			public override string Name()
			{
				return "BARE";
			}
		}

		/// <summary>A safe state for working normally</summary>
		public static RepositoryState SAFE = new RepositoryState.SAFE_Class();

		internal class SAFE_Class : RepositoryState
		{
			public override bool CanCheckout()
			{
				return true;
			}

			public override bool CanResetHead()
			{
				return true;
			}

			public override bool CanCommit()
			{
				return true;
			}

			public override bool CanAmend()
			{
				return true;
			}

			public override string GetDescription()
			{
				return JGitText.Get().repositoryState_normal;
			}

			public override string Name()
			{
				return "SAFE";
			}
		}

		/// <summary>An unfinished merge.</summary>
		/// <remarks>An unfinished merge. Must resolve or reset before continuing normally</remarks>
		public static RepositoryState MERGING = new RepositoryState.MERGING_Class();

		internal class MERGING_Class : RepositoryState
		{
			public override bool CanCheckout()
			{
				return false;
			}

			public override bool CanResetHead()
			{
				return true;
			}

			public override bool CanCommit()
			{
				return false;
			}

			public override bool CanAmend()
			{
				return false;
			}

			public override string GetDescription()
			{
				return JGitText.Get().repositoryState_conflicts;
			}

			public override string Name()
			{
				return "MERGING";
			}
		}

		/// <summary>An merge where all conflicts have been resolved.</summary>
		/// <remarks>
		/// An merge where all conflicts have been resolved. The index does not
		/// contain any unmerged paths.
		/// </remarks>
		public static RepositoryState MERGING_RESOLVED = new RepositoryState.MERGING_RESOLVED_Class
			();

		internal class MERGING_RESOLVED_Class : RepositoryState
		{
			public override bool CanCheckout()
			{
				return true;
			}

			public override bool CanResetHead()
			{
				return true;
			}

			public override bool CanCommit()
			{
				return true;
			}

			public override bool CanAmend()
			{
				return false;
			}

			public override string GetDescription()
			{
				return JGitText.Get().repositoryState_merged;
			}

			public override string Name()
			{
				return "MERGING_RESOLVED";
			}
		}

		/// <summary>An unfinished cherry-pick.</summary>
		/// <remarks>An unfinished cherry-pick. Must resolve or reset before continuing normally
		/// 	</remarks>
		public static RepositoryState CHERRY_PICKING = new RepositoryState.CHERRY_PICKING_Class
			();

		internal class CHERRY_PICKING_Class : RepositoryState
		{
			public override bool CanCheckout()
			{
				return false;
			}

			public override bool CanResetHead()
			{
				return true;
			}

			public override bool CanCommit()
			{
				return false;
			}

			public override bool CanAmend()
			{
				return false;
			}

			public override string GetDescription()
			{
				return JGitText.Get().repositoryState_conflicts;
			}

			public override string Name()
			{
				return "CHERRY_PICKING";
			}
		}

		/// <summary>A cherry-pick where all conflicts have been resolved.</summary>
		/// <remarks>
		/// A cherry-pick where all conflicts have been resolved. The index does not
		/// contain any unmerged paths.
		/// </remarks>
		public static RepositoryState CHERRY_PICKING_RESOLVED = new RepositoryState.CHERRY_PICKING_RESOLVED_Class
			();

		internal class CHERRY_PICKING_RESOLVED_Class : RepositoryState
		{
			public override bool CanCheckout()
			{
				return true;
			}

			public override bool CanResetHead()
			{
				return true;
			}

			public override bool CanCommit()
			{
				return true;
			}

			public override bool CanAmend()
			{
				return false;
			}

			public override string GetDescription()
			{
				return JGitText.Get().repositoryState_merged;
			}

			public override string Name()
			{
				return "CHERRY_PICKING_RESOLVED";
			}
		}

		/// <summary>An unfinished rebase or am.</summary>
		/// <remarks>An unfinished rebase or am. Must resolve, skip or abort before normal work can take place
		/// 	</remarks>
		public static RepositoryState REBASING = new RepositoryState.REBASING_Class();

		internal class REBASING_Class : RepositoryState
		{
			public override bool CanCheckout()
			{
				return false;
			}

			public override bool CanResetHead()
			{
				return false;
			}

			public override bool CanCommit()
			{
				return true;
			}

			public override bool CanAmend()
			{
				return true;
			}

			public override string GetDescription()
			{
				return JGitText.Get().repositoryState_rebaseOrApplyMailbox;
			}

			public override string Name()
			{
				return "REBASING";
			}
		}

		/// <summary>An unfinished rebase.</summary>
		/// <remarks>An unfinished rebase. Must resolve, skip or abort before normal work can take place
		/// 	</remarks>
		public static RepositoryState REBASING_REBASING = new RepositoryState.REBASING_REBASING_Class
			();

		internal class REBASING_REBASING_Class : RepositoryState
		{
			public override bool CanCheckout()
			{
				return false;
			}

			public override bool CanResetHead()
			{
				return false;
			}

			public override bool CanCommit()
			{
				return true;
			}

			public override bool CanAmend()
			{
				return true;
			}

			public override string GetDescription()
			{
				return JGitText.Get().repositoryState_rebase;
			}

			public override string Name()
			{
				return "REBASING_REBASING";
			}
		}

		/// <summary>An unfinished apply.</summary>
		/// <remarks>An unfinished apply. Must resolve, skip or abort before normal work can take place
		/// 	</remarks>
		public static RepositoryState APPLY = new RepositoryState.APPLY_Class();

		internal class APPLY_Class : RepositoryState
		{
			public override bool CanCheckout()
			{
				return false;
			}

			public override bool CanResetHead()
			{
				return false;
			}

			public override bool CanCommit()
			{
				return true;
			}

			public override bool CanAmend()
			{
				return true;
			}

			public override string GetDescription()
			{
				return JGitText.Get().repositoryState_applyMailbox;
			}

			public override string Name()
			{
				return "APPLY";
			}
		}

		/// <summary>An unfinished rebase with merge.</summary>
		/// <remarks>An unfinished rebase with merge. Must resolve, skip or abort before normal work can take place
		/// 	</remarks>
		public static RepositoryState REBASING_MERGE = new RepositoryState.REBASING_MERGE_Class
			();

		internal class REBASING_MERGE_Class : RepositoryState
		{
			public override bool CanCheckout()
			{
				return false;
			}

			public override bool CanResetHead()
			{
				return false;
			}

			public override bool CanCommit()
			{
				return true;
			}

			public override bool CanAmend()
			{
				return true;
			}

			public override string GetDescription()
			{
				return JGitText.Get().repositoryState_rebaseWithMerge;
			}

			public override string Name()
			{
				return "REBASING_MERGE";
			}
		}

		/// <summary>An unfinished interactive rebase.</summary>
		/// <remarks>An unfinished interactive rebase. Must resolve, skip or abort before normal work can take place
		/// 	</remarks>
		public static RepositoryState REBASING_INTERACTIVE = new RepositoryState.REBASING_INTERACTIVE_Class
			();

		internal class REBASING_INTERACTIVE_Class : RepositoryState
		{
			public override bool CanCheckout()
			{
				return false;
			}

			public override bool CanResetHead()
			{
				return false;
			}

			public override bool CanCommit()
			{
				return true;
			}

			public override bool CanAmend()
			{
				return true;
			}

			public override string GetDescription()
			{
				return JGitText.Get().repositoryState_rebaseInteractive;
			}

			public override string Name()
			{
				return "REBASING_INTERACTIVE";
			}
		}

		/// <summary>Bisecting being done.</summary>
		/// <remarks>Bisecting being done. Normal work may continue but is discouraged</remarks>
		public static RepositoryState BISECTING = new RepositoryState.BISECTING_Class();

		internal class BISECTING_Class : RepositoryState
		{
			public override bool CanCheckout()
			{
				return true;
			}

			public override bool CanResetHead()
			{
				return false;
			}

			public override bool CanCommit()
			{
				return true;
			}

			public override bool CanAmend()
			{
				return false;
			}

			public override string GetDescription()
			{
				return JGitText.Get().repositoryState_bisecting;
			}

			public override string Name()
			{
				return "BISECTING";
			}
		}

		/// <returns>true if changing HEAD is sane.</returns>
		public abstract bool CanCheckout();

		/// <returns>true if we can commit</returns>
		public abstract bool CanCommit();

		/// <returns>true if reset to another HEAD is considered SAFE</returns>
		public abstract bool CanResetHead();

		/// <returns>true if amending is considered SAFE</returns>
		public abstract bool CanAmend();

		/// <returns>a human readable description of the state.</returns>
		public abstract string GetDescription();

		public abstract string Name();
	}
}
