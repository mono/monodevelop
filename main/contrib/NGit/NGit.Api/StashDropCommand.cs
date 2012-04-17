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

using System;
using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Errors;
using NGit.Internal;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit.Api
{
	/// <summary>Command class to delete a stashed commit reference</summary>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-stash.html"
	/// *      >Git documentation about Stash</a></seealso>
	public class StashDropCommand : GitCommand<ObjectId>
	{
		private int stashRefEntry;

		private bool all;

		/// <param name="repo"></param>
		protected internal StashDropCommand(Repository repo) : base(repo)
		{
		}

		/// <summary>Set the stash reference to drop (0-based).</summary>
		/// <remarks>
		/// Set the stash reference to drop (0-based).
		/// <p>
		/// This will default to drop the latest stashed commit (stash@{0}) if
		/// unspecified
		/// </remarks>
		/// <param name="stashRef"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.StashDropCommand SetStashRef(int stashRef)
		{
			if (stashRef < 0)
			{
				throw new ArgumentException();
			}
			stashRefEntry = stashRef;
			return this;
		}

		/// <summary>Set wheter drop all stashed commits</summary>
		/// <param name="all">
		/// true to drop all stashed commits, false to drop only the
		/// stashed commit set via calling
		/// <see cref="SetStashRef(int)">SetStashRef(int)</see>
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.StashDropCommand SetAll(bool all)
		{
			this.all = all;
			return this;
		}

		/// <exception cref="NGit.Api.Errors.GitAPIException"></exception>
		private Ref GetRef()
		{
			try
			{
				return repo.GetRef(Constants.R_STASH);
			}
			catch (IOException e)
			{
				throw new InvalidRefNameException(MessageFormat.Format(JGitText.Get().cannotRead, 
					Constants.R_STASH), e);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private RefUpdate CreateRefUpdate(Ref stashRef)
		{
			RefUpdate update = repo.UpdateRef(Constants.R_STASH);
			update.DisableRefLog();
			update.SetExpectedOldObjectId(stashRef.GetObjectId());
			update.SetForceUpdate(true);
			return update;
		}

		private void DeleteRef(Ref stashRef)
		{
			try
			{
				RefUpdate.Result result = CreateRefUpdate(stashRef).Delete();
				if (RefUpdate.Result.FORCED != result)
				{
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().stashDropDeleteRefFailed
						, result));
				}
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().stashDropFailed, e);
			}
		}

		private void UpdateRef(Ref stashRef, ObjectId newId)
		{
			try
			{
				RefUpdate update = CreateRefUpdate(stashRef);
				update.SetNewObjectId(newId);
				RefUpdate.Result result = update.Update();
				switch (result)
				{
					case RefUpdate.Result.FORCED:
					case RefUpdate.Result.NEW:
					case RefUpdate.Result.NO_CHANGE:
					{
						return;
					}

					default:
					{
						throw new JGitInternalException(MessageFormat.Format(JGitText.Get().updatingRefFailed
							, Constants.R_STASH, newId, result));
					}
				}
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().stashDropFailed, e);
			}
		}

		/// <summary>
		/// Drop the configured entry from the stash reflog and return value of the
		/// stash reference after the drop occurs
		/// </summary>
		/// <returns>commit id of stash reference or null if no more stashed changes</returns>
		/// <exception cref="NGit.Api.Errors.GitAPIException"></exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		public override ObjectId Call()
		{
			CheckCallable();
			Ref stashRef = GetRef();
			if (stashRef == null)
			{
				return null;
			}
			if (all)
			{
				DeleteRef(stashRef);
				return null;
			}
			ReflogReader reader = new ReflogReader(repo, Constants.R_STASH);
			IList<ReflogEntry> entries;
			try
			{
				entries = reader.GetReverseEntries();
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().stashDropFailed, e);
			}
			if (stashRefEntry >= entries.Count)
			{
				throw new JGitInternalException(JGitText.Get().stashDropMissingReflog);
			}
			if (entries.Count == 1)
			{
				DeleteRef(stashRef);
				return null;
			}
			ReflogWriter writer = new ReflogWriter(repo, true);
			string stashLockRef = ReflogWriter.RefLockFor(Constants.R_STASH);
			FilePath stashLockFile = writer.LogFor(stashLockRef);
			FilePath stashFile = writer.LogFor(Constants.R_STASH);
			if (stashLockFile.Exists())
			{
				throw new JGitInternalException(JGitText.Get().stashDropFailed, new LockFailedException
					(stashFile));
			}
			entries.Remove(stashRefEntry);
			ObjectId entryId = ObjectId.ZeroId;
			try
			{
				for (int i = entries.Count - 1; i >= 0; i--)
				{
					ReflogEntry entry = entries[i];
					writer.Log(stashLockRef, entryId, entry.GetNewId(), entry.GetWho(), entry.GetComment
						());
					entryId = entry.GetNewId();
				}
				if (!stashLockFile.RenameTo(stashFile))
				{
					FileUtils.Delete(stashFile);
					if (!stashLockFile.RenameTo(stashFile))
					{
						throw new JGitInternalException(MessageFormat.Format(JGitText.Get().couldNotWriteFile
							, stashLockFile.GetPath(), stashFile.GetPath()));
					}
				}
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().stashDropFailed, e);
			}
			UpdateRef(stashRef, entryId);
			try
			{
				Ref newStashRef = repo.GetRef(Constants.R_STASH);
				return newStashRef != null ? newStashRef.GetObjectId() : null;
			}
			catch (IOException e)
			{
				throw new InvalidRefNameException(MessageFormat.Format(JGitText.Get().cannotRead, 
					Constants.R_STASH), e);
			}
		}
	}
}
