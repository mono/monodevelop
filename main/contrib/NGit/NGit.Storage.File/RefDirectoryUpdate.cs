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
using NGit.Storage.File;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// Updates any reference stored by
	/// <see cref="RefDirectory">RefDirectory</see>
	/// .
	/// </summary>
	internal class RefDirectoryUpdate : RefUpdate
	{
		private readonly RefDirectory database;

		private LockFile Lock;

		internal RefDirectoryUpdate(RefDirectory r, Ref @ref) : base(@ref)
		{
			database = r;
		}

		protected internal override RefDatabase GetRefDatabase()
		{
			return database;
		}

		protected internal override Repository GetRepository()
		{
			return database.GetRepository();
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override bool TryLock(bool deref)
		{
			Ref dst = GetRef();
			if (deref)
			{
				dst = dst.GetLeaf();
			}
			string name = dst.GetName();
			Lock = new LockFile(database.FileFor(name), GetRepository().FileSystem);
			if (Lock.Lock())
			{
				dst = database.GetRef(name);
				SetOldObjectId(dst != null ? dst.GetObjectId() : null);
				return true;
			}
			else
			{
				return false;
			}
		}

		protected internal override void Unlock()
		{
			if (Lock != null)
			{
				Lock.Unlock();
				Lock = null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override RefUpdate.Result DoUpdate(RefUpdate.Result status)
		{
			Lock.SetNeedStatInformation(true);
			Lock.Write(GetNewObjectId());
			string msg = GetRefLogMessage();
			if (msg != null)
			{
				if (IsRefLogIncludingResult())
				{
					string strResult = ToResultString(status);
					if (strResult != null)
					{
						if (msg.Length > 0)
						{
							msg = msg + ": " + strResult;
						}
						else
						{
							msg = strResult;
						}
					}
				}
				database.Log(this, msg, true);
			}
			if (!Lock.Commit())
			{
				return RefUpdate.Result.LOCK_FAILURE;
			}
			database.Stored(this, Lock.GetCommitLastModified());
			return status;
		}

		private string ToResultString(RefUpdate.Result status)
		{
			switch (status)
			{
				case RefUpdate.Result.FORCED:
				{
					return "forced-update";
				}

				case RefUpdate.Result.FAST_FORWARD:
				{
					return "fast forward";
				}

				case RefUpdate.Result.NEW:
				{
					return "created";
				}

				default:
				{
					return null;
					break;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override RefUpdate.Result DoDelete(RefUpdate.Result status)
		{
			if (GetRef().GetLeaf().GetStorage() != RefStorage.NEW)
			{
				database.Delete(this);
			}
			return status;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override RefUpdate.Result DoLink(string target)
		{
			Lock.SetNeedStatInformation(true);
			Lock.Write(Constants.Encode(RefDirectory.SYMREF + target + '\n'));
			string msg = GetRefLogMessage();
			if (msg != null)
			{
				database.Log(this, msg, false);
			}
			if (!Lock.Commit())
			{
				return RefUpdate.Result.LOCK_FAILURE;
			}
			database.StoredSymbolicRef(this, Lock.GetCommitLastModified(), target);
			if (GetRef().GetStorage() == RefStorage.NEW)
			{
				return RefUpdate.Result.NEW;
			}
			return RefUpdate.Result.FORCED;
		}
	}
}
