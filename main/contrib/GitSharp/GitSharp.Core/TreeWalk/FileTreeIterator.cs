/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

using System.IO;
using GitSharp.Core.Util;

namespace GitSharp.Core.TreeWalk
{
    /// <summary>
    /// Working directory iterator for standard Java IO.
    /// 
    /// This iterator uses the standard <code>java.io</code> package to Read the
    /// specified working directory as part of a <see cref="TreeWalk"/>.
    /// </summary>
    public class FileTreeIterator : WorkingTreeIterator
    {
        private readonly DirectoryInfo _directory;

        /// <summary>
        /// Create a new iterator to traverse the given directory and its children.
        /// </summary>
        /// <param name="root">
        /// The starting directory. This directory should correspond to
        /// the root of the repository.
        /// </param>
        public FileTreeIterator(DirectoryInfo root)
        {
            _directory = root;
            Init(Entries);
        }

        /// <summary>
        /// Create a new iterator to traverse a subdirectory.
        /// </summary>
        /// <param name="p">
        /// The parent iterator we were created from.
        /// </param>
        /// <param name="root">
        /// The subdirectory. This should be a directory contained within
        /// the parent directory.
        /// </param>
        public FileTreeIterator(WorkingTreeIterator p, DirectoryInfo root)
            : base(p)
        {
            _directory = root;
            Init(Entries);
        }

        public override AbstractTreeIterator createSubtreeIterator(Repository repo)
        {
            return new FileTreeIterator(this, ((FileEntry)Current).File as DirectoryInfo);
        }

        private Entry[] Entries
        {
			get
			{
				FileSystemInfo[] all = null;

				try
				{
					_directory.Refresh();
					all = _directory.GetFileSystemInfos();
				}
				catch (DirectoryNotFoundException)
				{
				}
				catch (IOException)
				{
				}

				if (all == null) return Eof;

				var r = new Entry[all.Length];

				for (int i = 0; i < r.Length; i++)
				{
					r[i] = new FileEntry(all[i]);
				}

				return r;
			}
        }

        /// <summary>
        /// Wrapper for a standard file
        /// </summary>
        public class FileEntry : Entry
        {
            private readonly FileSystemInfo _file;
            private readonly FileMode _mode;
            private long _fileLength = -1;
            private long _lastModified;

            private readonly bool _isADirectory = false;

            public FileEntry(FileSystemInfo f)
            {
				_file = f;
                
                if (f.IsDirectory())
                {
                    _isADirectory = true;
                    if (PathUtil.CombineDirectoryPath((DirectoryInfo)f, Constants.DOT_GIT).IsDirectory())
                        _mode = FileMode.GitLink;
                    else
                        _mode = FileMode.Tree;
                }
                else if (FS.canExecute(_file))
                    _mode = FileMode.ExecutableFile;
                else
                    _mode = FileMode.RegularFile;
            }

            public override FileMode Mode
            {
                get { return _mode; }
            }

            public override string Name
            {
                get { return _file.Name; }
            }

            public override long Length
            {
                get
                {
                    if (_file.IsDirectory()) return 0;

                    if (_fileLength < 0)
                    {
                        _fileLength = new FileInfo(_file.FullName).Length;
                    }

                    return _fileLength;
                }
            }

            public override long LastModified
            {
                get
                {
                    if (_lastModified == 0)
                    {
                        if (_isADirectory)                        {
                            _lastModified = ((DirectoryInfo)_file).lastModified();
                        }                        else
                        {
                            _lastModified = ((FileInfo)_file).lastModified();                        }
                    }

                    return _lastModified;
                }
            }

            public override FileStream OpenInputStream()
            {
                return new FileStream(_file.FullName, System.IO.FileMode.Open, FileAccess.Read);
            }

            /// <summary>
            /// Get the underlying file of this entry.
            /// </summary>
            public FileSystemInfo File
            {
                get { return _file; }
            }
        }
    }
}