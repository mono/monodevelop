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
using System.IO;
using NGit;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>Git style file locking and replacement.</summary>
	/// <remarks>
	/// Git style file locking and replacement.
	/// <p>
	/// To modify a ref file Git tries to use an atomic update approach: we write the
	/// new data into a brand new file, then rename it in place over the old name.
	/// This way we can just delete the temporary file if anything goes wrong, and
	/// nothing has been damaged. To coordinate access from multiple processes at
	/// once Git tries to atomically create the new temporary file under a well-known
	/// name.
	/// </remarks>
	public class LockFile
	{
		internal static readonly string SUFFIX = ".lock";

		private sealed class _FilenameFilter_78 : FilenameFilter
		{
			public _FilenameFilter_78()
			{
			}

			//$NON-NLS-1$
			public bool Accept(FilePath dir, string name)
			{
				return !name.EndsWith(NGit.Storage.File.LockFile.SUFFIX);
			}
		}

		/// <summary>Filter to skip over active lock files when listing a directory.</summary>
		/// <remarks>Filter to skip over active lock files when listing a directory.</remarks>
		internal static readonly FilenameFilter FILTER = new _FilenameFilter_78();

		private readonly FilePath @ref;

		private readonly FilePath lck;

		private FileLock fLck;

		private bool haveLck;

		private FileOutputStream os;

		private bool needStatInformation;

		private long commitLastModified;

		private readonly FS fs;

		/// <summary>Create a new lock for any file.</summary>
		/// <remarks>Create a new lock for any file.</remarks>
		/// <param name="f">the file that will be locked.</param>
		/// <param name="fs">
		/// the file system abstraction which will be necessary to perform
		/// certain file system operations.
		/// </param>
		public LockFile(FilePath f, FS fs)
		{
			@ref = f;
			lck = new FilePath(@ref.GetParentFile(), @ref.GetName() + SUFFIX);
			this.fs = fs;
		}

		/// <summary>Try to establish the lock.</summary>
		/// <remarks>Try to establish the lock.</remarks>
		/// <returns>
		/// true if the lock is now held by the caller; false if it is held
		/// by someone else.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// the temporary output file could not be created. The caller
		/// does not hold the lock.
		/// </exception>
		public virtual bool Lock()
		{
			lck.GetParentFile().Mkdirs();
			if (lck.CreateNewFile())
			{
				haveLck = true;
				try
				{
					os = new FileOutputStream(lck);
					try
					{
						fLck = os.GetChannel().TryLock();
						if (fLck == null)
						{
							throw new OverlappingFileLockException();
						}
					}
					catch (OverlappingFileLockException)
					{
						// We cannot use unlock() here as this file is not
						// held by us, but we thought we created it. We must
						// not delete it, as it belongs to some other process.
						//
						haveLck = false;
						try
						{
							os.Close();
						}
						catch (IOException)
						{
						}
						// Fail by returning haveLck = false.
						os = null;
					}
				}
				catch (IOException ioe)
				{
					Unlock();
					throw;
				}
			}
			return haveLck;
		}

		/// <summary>Try to establish the lock for appending.</summary>
		/// <remarks>Try to establish the lock for appending.</remarks>
		/// <returns>
		/// true if the lock is now held by the caller; false if it is held
		/// by someone else.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// the temporary output file could not be created. The caller
		/// does not hold the lock.
		/// </exception>
		public virtual bool LockForAppend()
		{
			if (!Lock())
			{
				return false;
			}
			CopyCurrentContent();
			return true;
		}

		/// <summary>Copy the current file content into the temporary file.</summary>
		/// <remarks>
		/// Copy the current file content into the temporary file.
		/// <p>
		/// This method saves the current file content by inserting it into the
		/// temporary file, so that the caller can safely append rather than replace
		/// the primary file.
		/// <p>
		/// This method does nothing if the current file does not exist, or exists
		/// but is empty.
		/// </remarks>
		/// <exception cref="System.IO.IOException">
		/// the temporary file could not be written, or a read error
		/// occurred while reading from the current file. The lock is
		/// released before throwing the underlying IO exception to the
		/// caller.
		/// </exception>
		/// <exception cref="Sharpen.RuntimeException">
		/// the temporary file could not be written. The lock is released
		/// before throwing the underlying exception to the caller.
		/// </exception>
		public virtual void CopyCurrentContent()
		{
			RequireLock();
			try
			{
				FileInputStream fis = new FileInputStream(@ref);
				try
				{
					byte[] buf = new byte[2048];
					int r;
					while ((r = fis.Read(buf)) >= 0)
					{
						os.Write(buf, 0, r);
					}
				}
				finally
				{
					fis.Close();
				}
			}
			catch (FileNotFoundException)
			{
			}
			catch (IOException ioe)
			{
				// Don't worry about a file that doesn't exist yet, it
				// conceptually has no current content to copy.
				//
				Unlock();
				throw;
			}
			catch (RuntimeException ioe)
			{
				Unlock();
				throw;
			}
			catch (Error ioe)
			{
				Unlock();
				throw;
			}
		}

		/// <summary>Write an ObjectId and LF to the temporary file.</summary>
		/// <remarks>Write an ObjectId and LF to the temporary file.</remarks>
		/// <param name="id">
		/// the id to store in the file. The id will be written in hex,
		/// followed by a sole LF.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// the temporary file could not be written. The lock is released
		/// before throwing the underlying IO exception to the caller.
		/// </exception>
		/// <exception cref="Sharpen.RuntimeException">
		/// the temporary file could not be written. The lock is released
		/// before throwing the underlying exception to the caller.
		/// </exception>
		public virtual void Write(ObjectId id)
		{
			RequireLock();
			try
			{
				BufferedOutputStream b;
				b = new BufferedOutputStream(os, Constants.OBJECT_ID_STRING_LENGTH + 1);
				id.CopyTo(b);
				b.Write('\n');
				b.Flush();
				fLck.Release();
				b.Close();
				os = null;
			}
			catch (IOException ioe)
			{
				Unlock();
				throw;
			}
			catch (RuntimeException ioe)
			{
				Unlock();
				throw;
			}
			catch (Error ioe)
			{
				Unlock();
				throw;
			}
		}

		/// <summary>Write arbitrary data to the temporary file.</summary>
		/// <remarks>Write arbitrary data to the temporary file.</remarks>
		/// <param name="content">
		/// the bytes to store in the temporary file. No additional bytes
		/// are added, so if the file must end with an LF it must appear
		/// at the end of the byte array.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// the temporary file could not be written. The lock is released
		/// before throwing the underlying IO exception to the caller.
		/// </exception>
		/// <exception cref="Sharpen.RuntimeException">
		/// the temporary file could not be written. The lock is released
		/// before throwing the underlying exception to the caller.
		/// </exception>
		public virtual void Write(byte[] content)
		{
			RequireLock();
			try
			{
				os.Write(content);
				os.Flush();
				fLck.Release();
				os.Close();
				os = null;
			}
			catch (IOException ioe)
			{
				Unlock();
				throw;
			}
			catch (RuntimeException ioe)
			{
				Unlock();
				throw;
			}
			catch (Error ioe)
			{
				Unlock();
				throw;
			}
		}

		/// <summary>Obtain the direct output stream for this lock.</summary>
		/// <remarks>
		/// Obtain the direct output stream for this lock.
		/// <p>
		/// The stream may only be accessed once, and only after
		/// <see cref="Lock()">Lock()</see>
		/// has
		/// been successfully invoked and returned true. Callers must close the
		/// stream prior to calling
		/// <see cref="Commit()">Commit()</see>
		/// to commit the change.
		/// </remarks>
		/// <returns>a stream to write to the new file. The stream is unbuffered.</returns>
		public virtual OutputStream GetOutputStream()
		{
			RequireLock();
			return new _OutputStream_299(this);
		}

		private sealed class _OutputStream_299 : OutputStream
		{
			public _OutputStream_299(LockFile _enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(byte[] b, int o, int n)
			{
				this._enclosing.os.Write(b, o, n);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(byte[] b)
			{
				this._enclosing.os.Write(b);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(int b)
			{
				this._enclosing.os.Write(b);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Flush()
			{
				this._enclosing.os.Flush();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				try
				{
					this._enclosing.os.Flush();
					this._enclosing.fLck.Release();
					this._enclosing.os.Close();
					this._enclosing.os = null;
				}
				catch (IOException ioe)
				{
					this._enclosing.Unlock();
					throw;
				}
				catch (RuntimeException ioe)
				{
					this._enclosing.Unlock();
					throw;
				}
				catch (Error ioe)
				{
					this._enclosing.Unlock();
					throw;
				}
			}

			private readonly LockFile _enclosing;
		}

		private void RequireLock()
		{
			if (os == null)
			{
				Unlock();
				throw new InvalidOperationException(MessageFormat.Format(JGitText.Get().lockOnNotHeld
					, @ref));
			}
		}

		/// <summary>
		/// Request that
		/// <see cref="Commit()">Commit()</see>
		/// remember modification time.
		/// </summary>
		/// <param name="on">true if the commit method must remember the modification time.</param>
		public virtual void SetNeedStatInformation(bool on)
		{
			needStatInformation = on;
		}

		/// <summary>Wait until the lock file information differs from the old file.</summary>
		/// <remarks>
		/// Wait until the lock file information differs from the old file.
		/// <p>
		/// This method tests both the length and the last modification date. If both
		/// are the same, this method sleeps until it can force the new lock file's
		/// modification date to be later than the target file.
		/// </remarks>
		/// <exception cref="System.Exception">
		/// the thread was interrupted before the last modified date of
		/// the lock file was different from the last modified date of
		/// the target file.
		/// </exception>
		public virtual void WaitForStatChange()
		{
			if (@ref.Length() == lck.Length())
			{
				long otime = @ref.LastModified();
				long ntime = lck.LastModified();
				while (otime == ntime)
				{
					Sharpen.Thread.Sleep(25);
					lck.SetLastModified(Runtime.CurrentTimeMillis());
					ntime = lck.LastModified();
				}
			}
		}

		/// <summary>Commit this change and release the lock.</summary>
		/// <remarks>
		/// Commit this change and release the lock.
		/// <p>
		/// If this method fails (returns false) the lock is still released.
		/// </remarks>
		/// <returns>
		/// true if the commit was successful and the file contains the new
		/// data; false if the commit failed and the file remains with the
		/// old data.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">the lock is not held.</exception>
		public virtual bool Commit()
		{
			if (os != null)
			{
				Unlock();
				throw new InvalidOperationException(MessageFormat.Format(JGitText.Get().lockOnNotClosed
					, @ref));
			}
			SaveStatInformation();
			if (lck.RenameTo(@ref))
			{
				return true;
			}
			if (!@ref.Exists() || DeleteRef())
			{
				if (lck.RenameTo(@ref))
				{
					return true;
				}
			}
			Unlock();
			return false;
		}

		private bool DeleteRef()
		{
			if (!fs.RetryFailedLockFileCommit())
			{
				return @ref.Delete();
			}
			// File deletion fails on windows if another thread is
			// concurrently reading the same file. So try a few times.
			//
			for (int attempts = 0; attempts < 10; attempts++)
			{
				if (@ref.Delete())
				{
					return true;
				}
				try
				{
					Sharpen.Thread.Sleep(100);
				}
				catch (Exception)
				{
					return false;
				}
			}
			return false;
		}

		private void SaveStatInformation()
		{
			if (needStatInformation)
			{
				commitLastModified = lck.LastModified();
			}
		}

		/// <summary>Get the modification time of the output file when it was committed.</summary>
		/// <remarks>Get the modification time of the output file when it was committed.</remarks>
		/// <returns>modification time of the lock file right before we committed it.</returns>
		public virtual long GetCommitLastModified()
		{
			return commitLastModified;
		}

		/// <summary>Unlock this file and abort this change.</summary>
		/// <remarks>
		/// Unlock this file and abort this change.
		/// <p>
		/// The temporary file (if created) is deleted before returning.
		/// </remarks>
		public virtual void Unlock()
		{
			if (os != null)
			{
				if (fLck != null)
				{
					try
					{
						fLck.Release();
					}
					catch (IOException)
					{
					}
					// Huh?
					fLck = null;
				}
				try
				{
					os.Close();
				}
				catch (IOException)
				{
				}
				// Ignore this
				os = null;
			}
			if (haveLck)
			{
				haveLck = false;
				lck.Delete();
			}
		}

		public override string ToString()
		{
			return "LockFile[" + lck + ", haveLck=" + haveLck + "]";
		}
	}
}
