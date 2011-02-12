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

namespace NGit.Util
{
	/// <summary>File Utilities</summary>
	public class FileUtils
	{
		/// <summary>
		/// Option to delete given
		/// <code>File</code>
		/// </summary>
		public const int NONE = 0;

		/// <summary>
		/// Option to recursively delete given
		/// <code>File</code>
		/// </summary>
		public const int RECURSIVE = 1;

		/// <summary>Option to retry deletion if not successful</summary>
		public const int RETRY = 2;

		/// <summary>Option to skip deletion if file doesn't exist</summary>
		public const int SKIP_MISSING = 4;

		/// <summary>Delete file or empty folder</summary>
		/// <param name="f">
		/// <code>File</code>
		/// to be deleted
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// if deletion of
		/// <code>f</code>
		/// fails. This may occur if
		/// <code>f</code>
		/// didn't exist when the method was called. This can therefore
		/// cause IOExceptions during race conditions when multiple
		/// concurrent threads all try to delete the same file.
		/// </exception>
		public static void Delete(FilePath f)
		{
			Delete(f, NONE);
		}

		/// <summary>Delete file or folder</summary>
		/// <param name="f">
		/// <code>File</code>
		/// to be deleted
		/// </param>
		/// <param name="options">
		/// deletion options,
		/// <code>RECURSIVE</code>
		/// for recursive deletion of
		/// a subtree,
		/// <code>RETRY</code>
		/// to retry when deletion failed.
		/// Retrying may help if the underlying file system doesn't allow
		/// deletion of files being read by another thread.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// if deletion of
		/// <code>f</code>
		/// fails. This may occur if
		/// <code>f</code>
		/// didn't exist when the method was called. This can therefore
		/// cause IOExceptions during race conditions when multiple
		/// concurrent threads all try to delete the same file.
		/// </exception>
		public static void Delete(FilePath f, int options)
		{
			if ((options & SKIP_MISSING) != 0 && !f.Exists())
			{
				return;
			}
			if ((options & RECURSIVE) != 0 && f.IsDirectory())
			{
				FilePath[] items = f.ListFiles();
				if (items != null)
				{
					foreach (FilePath c in items)
					{
						Delete(c, options);
					}
				}
			}
			if (!f.Delete())
			{
				if ((options & RETRY) != 0 && f.Exists())
				{
					for (int i = 1; i < 10; i++)
					{
						try
						{
							Sharpen.Thread.Sleep(100);
						}
						catch (Exception)
						{
						}
						// ignore
						if (f.Delete())
						{
							return;
						}
					}
				}
				throw new IOException(MessageFormat.Format(JGitText.Get().deleteFileFailed, f.GetAbsolutePath
					()));
			}
		}

		/// <summary>Creates the directory named by this abstract pathname.</summary>
		/// <remarks>Creates the directory named by this abstract pathname.</remarks>
		/// <param name="d">directory to be created</param>
		/// <exception cref="System.IO.IOException">
		/// if creation of
		/// <code>d</code>
		/// fails. This may occur if
		/// <code>d</code>
		/// did exist when the method was called. This can therefore
		/// cause IOExceptions during race conditions when multiple
		/// concurrent threads all try to create the same directory.
		/// </exception>
		public static void Mkdir(FilePath d)
		{
			Mkdir(d, false);
		}

		/// <summary>Creates the directory named by this abstract pathname.</summary>
		/// <remarks>Creates the directory named by this abstract pathname.</remarks>
		/// <param name="d">directory to be created</param>
		/// <param name="skipExisting">
		/// if
		/// <code>true</code>
		/// skip creation of the given directory if it
		/// already exists in the file system
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// if creation of
		/// <code>d</code>
		/// fails. This may occur if
		/// <code>d</code>
		/// did exist when the method was called. This can therefore
		/// cause IOExceptions during race conditions when multiple
		/// concurrent threads all try to create the same directory.
		/// </exception>
		public static void Mkdir(FilePath d, bool skipExisting)
		{
			if (!d.Mkdir())
			{
				if (skipExisting && d.IsDirectory())
				{
					return;
				}
				throw new IOException(MessageFormat.Format(JGitText.Get().mkDirFailed, d.GetAbsolutePath
					()));
			}
		}

		/// <summary>
		/// Creates the directory named by this abstract pathname, including any
		/// necessary but nonexistent parent directories.
		/// </summary>
		/// <remarks>
		/// Creates the directory named by this abstract pathname, including any
		/// necessary but nonexistent parent directories. Note that if this operation
		/// fails it may have succeeded in creating some of the necessary parent
		/// directories.
		/// </remarks>
		/// <param name="d">directory to be created</param>
		/// <exception cref="System.IO.IOException">
		/// if creation of
		/// <code>d</code>
		/// fails. This may occur if
		/// <code>d</code>
		/// did exist when the method was called. This can therefore
		/// cause IOExceptions during race conditions when multiple
		/// concurrent threads all try to create the same directory.
		/// </exception>
		public static void Mkdirs(FilePath d)
		{
			Mkdirs(d, false);
		}

		/// <summary>
		/// Creates the directory named by this abstract pathname, including any
		/// necessary but nonexistent parent directories.
		/// </summary>
		/// <remarks>
		/// Creates the directory named by this abstract pathname, including any
		/// necessary but nonexistent parent directories. Note that if this operation
		/// fails it may have succeeded in creating some of the necessary parent
		/// directories.
		/// </remarks>
		/// <param name="d">directory to be created</param>
		/// <param name="skipExisting">
		/// if
		/// <code>true</code>
		/// skip creation of the given directory if it
		/// already exists in the file system
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// if creation of
		/// <code>d</code>
		/// fails. This may occur if
		/// <code>d</code>
		/// did exist when the method was called. This can therefore
		/// cause IOExceptions during race conditions when multiple
		/// concurrent threads all try to create the same directory.
		/// </exception>
		public static void Mkdirs(FilePath d, bool skipExisting)
		{
			if (!d.Mkdirs())
			{
				if (skipExisting && d.IsDirectory())
				{
					return;
				}
				throw new IOException(MessageFormat.Format(JGitText.Get().mkDirsFailed, d.GetAbsolutePath
					()));
			}
		}

		/// <summary>
		/// Atomically creates a new, empty file named by this abstract pathname if
		/// and only if a file with this name does not yet exist.
		/// </summary>
		/// <remarks>
		/// Atomically creates a new, empty file named by this abstract pathname if
		/// and only if a file with this name does not yet exist. The check for the
		/// existence of the file and the creation of the file if it does not exist
		/// are a single operation that is atomic with respect to all other
		/// filesystem activities that might affect the file.
		/// <p>
		/// Note: this method should not be used for file-locking, as the resulting
		/// protocol cannot be made to work reliably. The
		/// <see cref="Sharpen.FileLock">Sharpen.FileLock</see>
		/// facility
		/// should be used instead.
		/// </remarks>
		/// <param name="f">the file to be created</param>
		/// <exception cref="System.IO.IOException">if the named file already exists or if an I/O error occurred
		/// 	</exception>
		public static void CreateNewFile(FilePath f)
		{
			if (!f.CreateNewFile())
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().createNewFileFailed, f)
					);
			}
		}
	}
}
