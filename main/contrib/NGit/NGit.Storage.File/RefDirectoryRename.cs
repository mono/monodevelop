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

using System.IO;
using NGit;
using NGit.Revwalk;
using NGit.Storage.File;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// Rename any reference stored by
	/// <see cref="RefDirectory">RefDirectory</see>
	/// .
	/// <p>
	/// This class works by first renaming the source reference to a temporary name,
	/// then renaming the temporary name to the final destination reference.
	/// <p>
	/// This strategy permits switching a reference like
	/// <code>refs/heads/foo</code>
	/// ,
	/// which is a file, to
	/// <code>refs/heads/foo/bar</code>
	/// , which is stored inside a
	/// directory that happens to match the source name.
	/// </summary>
	internal class RefDirectoryRename : RefRename
	{
		private readonly RefDirectory refdb;

		/// <summary>The value of the source reference at the start of the rename.</summary>
		/// <remarks>
		/// The value of the source reference at the start of the rename.
		/// <p>
		/// At the end of the rename the destination reference must have this same
		/// value, otherwise we have a concurrent update and the rename must fail
		/// without making any changes.
		/// </remarks>
		private ObjectId objId;

		/// <summary>True if HEAD must be moved to the destination reference.</summary>
		/// <remarks>True if HEAD must be moved to the destination reference.</remarks>
		private bool updateHEAD;

		/// <summary>
		/// A reference we backup
		/// <see cref="objId">objId</see>
		/// into during the rename.
		/// </summary>
		private RefDirectoryUpdate tmp;

		internal RefDirectoryRename(RefDirectoryUpdate src, RefDirectoryUpdate dst) : base
			(src, dst)
		{
			refdb = ((RefDirectory)src.GetRefDatabase());
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override RefUpdate.Result DoRename()
		{
			if (source.GetRef().IsSymbolic())
			{
				return RefUpdate.Result.IO_FAILURE;
			}
			// not supported
			objId = source.GetOldObjectId();
			updateHEAD = NeedToUpdateHEAD();
			tmp = refdb.NewTemporaryUpdate();
			RevWalk rw = new RevWalk(refdb.GetRepository());
			try
			{
				// First backup the source so its never unreachable.
				tmp.SetNewObjectId(objId);
				tmp.SetForceUpdate(true);
				tmp.DisableRefLog();
				switch (tmp.Update(rw))
				{
					case RefUpdate.Result.NEW:
					case RefUpdate.Result.FORCED:
					case RefUpdate.Result.NO_CHANGE:
					{
						break;
					}

					default:
					{
						return tmp.GetResult();
						break;
					}
				}
				// Save the source's log under the temporary name, we must do
				// this before we delete the source, otherwise we lose the log.
				if (!RenameLog(source, tmp))
				{
					return RefUpdate.Result.IO_FAILURE;
				}
				// If HEAD has to be updated, link it now to destination.
				// We have to link before we delete, otherwise the delete
				// fails because its the current branch.
				RefUpdate dst = destination;
				if (updateHEAD)
				{
					if (!LinkHEAD(destination))
					{
						RenameLog(tmp, source);
						return RefUpdate.Result.LOCK_FAILURE;
					}
					// Replace the update operation so HEAD will log the rename.
					dst = ((RefDirectoryUpdate)refdb.NewUpdate(Constants.HEAD, false));
					dst.SetRefLogIdent(destination.GetRefLogIdent());
					dst.SetRefLogMessage(destination.GetRefLogMessage(), false);
				}
				// Delete the source name so its path is free for replacement.
				source.SetExpectedOldObjectId(objId);
				source.SetForceUpdate(true);
				source.DisableRefLog();
				if (source.Delete(rw) != RefUpdate.Result.FORCED)
				{
					RenameLog(tmp, source);
					if (updateHEAD)
					{
						LinkHEAD(source);
					}
					return source.GetResult();
				}
				// Move the log to the destination.
				if (!RenameLog(tmp, destination))
				{
					RenameLog(tmp, source);
					source.SetExpectedOldObjectId(ObjectId.ZeroId);
					source.SetNewObjectId(objId);
					source.Update(rw);
					if (updateHEAD)
					{
						LinkHEAD(source);
					}
					return RefUpdate.Result.IO_FAILURE;
				}
				// Create the destination, logging the rename during the creation.
				dst.SetExpectedOldObjectId(ObjectId.ZeroId);
				dst.SetNewObjectId(objId);
				if (dst.Update(rw) != RefUpdate.Result.NEW)
				{
					// If we didn't create the destination we have to undo
					// our work. Put the log back and restore source.
					if (RenameLog(destination, tmp))
					{
						RenameLog(tmp, source);
					}
					source.SetExpectedOldObjectId(ObjectId.ZeroId);
					source.SetNewObjectId(objId);
					source.Update(rw);
					if (updateHEAD)
					{
						LinkHEAD(source);
					}
					return dst.GetResult();
				}
				return RefUpdate.Result.RENAMED;
			}
			finally
			{
				// Always try to free the temporary name.
				try
				{
					refdb.Delete(tmp);
				}
				catch (IOException)
				{
					refdb.FileFor(tmp.GetName()).Delete();
				}
				rw.Release();
			}
		}

		private bool RenameLog(RefUpdate src, RefUpdate dst)
		{
			FilePath srcLog = refdb.LogFor(src.GetName());
			FilePath dstLog = refdb.LogFor(dst.GetName());
			if (!srcLog.Exists())
			{
				return true;
			}
			if (!Rename(srcLog, dstLog))
			{
				return false;
			}
			try
			{
				int levels = RefDirectory.LevelsIn(src.GetName()) - 2;
				RefDirectory.Delete(srcLog, levels);
				return true;
			}
			catch (IOException)
			{
				Rename(dstLog, srcLog);
				return false;
			}
		}

		private static bool Rename(FilePath src, FilePath dst)
		{
			if (src.RenameTo(dst))
			{
				return true;
			}
			FilePath dir = dst.GetParentFile();
			if ((dir.Exists() || !dir.Mkdirs()) && !dir.IsDirectory())
			{
				return false;
			}
			return src.RenameTo(dst);
		}

		private bool LinkHEAD(RefUpdate target)
		{
			try
			{
				RefUpdate u = ((RefDirectoryUpdate)refdb.NewUpdate(Constants.HEAD, false));
				u.DisableRefLog();
				switch (u.Link(target.GetName()))
				{
					case RefUpdate.Result.NEW:
					case RefUpdate.Result.FORCED:
					case RefUpdate.Result.NO_CHANGE:
					{
						return true;
					}

					default:
					{
						return false;
						break;
					}
				}
			}
			catch (IOException)
			{
				return false;
			}
		}
	}
}
