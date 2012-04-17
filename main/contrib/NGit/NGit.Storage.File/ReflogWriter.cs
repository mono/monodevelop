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
using System.Text;
using NGit;
using NGit.Internal;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>Utility for writing reflog entries</summary>
	public class ReflogWriter
	{
		/// <summary>Get the ref name to be used for when locking a ref's log for rewriting</summary>
		/// <param name="name">
		/// name of the ref, relative to the Git repository top level
		/// directory (so typically starts with refs/).
		/// </param>
		/// <returns>the name of the ref's lock ref</returns>
		public static string RefLockFor(string name)
		{
			return name + LockFile.SUFFIX;
		}

		private readonly Repository parent;

		private readonly FilePath logsDir;

		private readonly FilePath logsRefsDir;

		private readonly bool forceWrite;

		/// <summary>Create write for repository</summary>
		/// <param name="repository"></param>
		public ReflogWriter(Repository repository) : this(repository, false)
		{
		}

		/// <summary>Create write for repository</summary>
		/// <param name="repository"></param>
		/// <param name="forceWrite">
		/// true to write to disk all entries logged, false to respect the
		/// repository's config and current log file status
		/// </param>
		public ReflogWriter(Repository repository, bool forceWrite)
		{
			FS fs = repository.FileSystem;
			parent = repository;
			FilePath gitDir = repository.Directory;
			logsDir = fs.Resolve(gitDir, Constants.LOGS);
			logsRefsDir = fs.Resolve(gitDir, Constants.LOGS + '/' + Constants.R_REFS);
			this.forceWrite = forceWrite;
		}

		/// <summary>Get repository that reflog is being written for</summary>
		/// <returns>file repository</returns>
		public virtual Repository GetRepository()
		{
			return parent;
		}

		/// <summary>Create the log directories</summary>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		/// <returns>this writer</returns>
		public virtual NGit.Storage.File.ReflogWriter Create()
		{
			FileUtils.Mkdir(logsDir);
			FileUtils.Mkdir(logsRefsDir);
			FileUtils.Mkdir(new FilePath(logsRefsDir, Sharpen.Runtime.Substring(Constants.R_HEADS
				, Constants.R_REFS.Length)));
			return this;
		}

		/// <summary>Locate the log file on disk for a single reference name.</summary>
		/// <remarks>Locate the log file on disk for a single reference name.</remarks>
		/// <param name="name">
		/// name of the ref, relative to the Git repository top level
		/// directory (so typically starts with refs/).
		/// </param>
		/// <returns>the log file location.</returns>
		public virtual FilePath LogFor(string name)
		{
			if (name.StartsWith(Constants.R_REFS))
			{
				name = Sharpen.Runtime.Substring(name, Constants.R_REFS.Length);
				return new FilePath(logsRefsDir, name);
			}
			return new FilePath(logsDir, name);
		}

		/// <summary>
		/// Write the given
		/// <see cref="ReflogEntry">ReflogEntry</see>
		/// entry to the ref's log
		/// </summary>
		/// <param name="refName"></param>
		/// <param name="entry"></param>
		/// <returns>this writer</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual NGit.Storage.File.ReflogWriter Log(string refName, ReflogEntry entry
			)
		{
			return Log(refName, entry.GetOldId(), entry.GetNewId(), entry.GetWho(), entry.GetComment
				());
		}

		/// <summary>Write the given entry information to the ref's log</summary>
		/// <param name="refName"></param>
		/// <param name="oldId"></param>
		/// <param name="newId"></param>
		/// <param name="ident"></param>
		/// <param name="message"></param>
		/// <returns>this writer</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual NGit.Storage.File.ReflogWriter Log(string refName, ObjectId oldId, 
			ObjectId newId, PersonIdent ident, string message)
		{
			byte[] encoded = Encode(oldId, newId, ident, message);
			return Log(refName, encoded);
		}

		/// <summary>Write the given ref update to the ref's log</summary>
		/// <param name="update"></param>
		/// <param name="msg"></param>
		/// <param name="deref"></param>
		/// <returns>this writer</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual NGit.Storage.File.ReflogWriter Log(RefUpdate update, string msg, bool
			 deref)
		{
			ObjectId oldId = update.GetOldObjectId();
			ObjectId newId = update.GetNewObjectId();
			Ref @ref = update.GetRef();
			PersonIdent ident = update.GetRefLogIdent();
			if (ident == null)
			{
				ident = new PersonIdent(parent);
			}
			else
			{
				ident = new PersonIdent(ident);
			}
			byte[] rec = Encode(oldId, newId, ident, msg);
			if (deref && @ref.IsSymbolic())
			{
				Log(@ref.GetName(), rec);
				Log(@ref.GetLeaf().GetName(), rec);
			}
			else
			{
				Log(@ref.GetName(), rec);
			}
			return this;
		}

		private byte[] Encode(ObjectId oldId, ObjectId newId, PersonIdent ident, string message
			)
		{
			StringBuilder r = new StringBuilder();
			r.Append(ObjectId.ToString(oldId));
			r.Append(' ');
			r.Append(ObjectId.ToString(newId));
			r.Append(' ');
			r.Append(ident.ToExternalString());
			r.Append('\t');
			r.Append(message);
			r.Append('\n');
			return Constants.Encode(r.ToString());
		}

		/// <exception cref="System.IO.IOException"></exception>
		private NGit.Storage.File.ReflogWriter Log(string refName, byte[] rec)
		{
			FilePath log = LogFor(refName);
			bool write = forceWrite || (IsLogAllRefUpdates() && ShouldAutoCreateLog(refName))
				 || log.IsFile();
			if (!write)
			{
				return this;
			}
			WriteConfig wc = GetRepository().GetConfig().Get(WriteConfig.KEY);
			FileOutputStream @out;
			try
			{
				@out = new FileOutputStream(log, true);
			}
			catch (FileNotFoundException err)
			{
				FilePath dir = log.GetParentFile();
				if (dir.Exists())
				{
					throw;
				}
				if (!dir.Mkdirs() && !dir.IsDirectory())
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().cannotCreateDirectory, 
						dir));
				}
				@out = new FileOutputStream(log, true);
			}
			try
			{
				if (wc.GetFSyncRefFiles())
				{
					FileChannel fc = @out.GetChannel();
					ByteBuffer buf = ByteBuffer.Wrap(rec);
					while (0 < buf.Remaining())
					{
						fc.Write(buf);
					}
					fc.Force(true);
				}
				else
				{
					@out.Write(rec);
				}
			}
			finally
			{
				@out.Close();
			}
			return this;
		}

		private bool IsLogAllRefUpdates()
		{
			return parent.GetConfig().Get(CoreConfig.KEY).IsLogAllRefUpdates();
		}

		private bool ShouldAutoCreateLog(string refName)
		{
			return refName.Equals(Constants.HEAD) || refName.StartsWith(Constants.R_HEADS) ||
				 refName.StartsWith(Constants.R_REMOTES) || refName.Equals(Constants.R_STASH);
		}
		//
		//
		//
	}
}
