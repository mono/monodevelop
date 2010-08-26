/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

using System;
using System.IO;
using System.Threading;
using GitSharp.Core.Util;
using System.Diagnostics;

namespace GitSharp.Core
{
    /// <summary>
    /// Git style file locking and replacement.
    /// <para />
    /// To modify a ref file Git tries to use an atomic update approach: we write the
    /// new data into a brand new file, then rename it in place over the old name.
    /// This way we can just delete the temporary file if anything goes wrong, and
    /// nothing has been damaged. To coordinate access from multiple processes at
    /// once Git tries to atomically create the new temporary file under a well-known
    /// name.
    /// </summary>
    public class LockFile : IDisposable
    {
        public static string SUFFIX = ".lock"; //$NON-NLS-1$

        public static Func<string, bool> FILTER = (name) => !name.EndsWith(SUFFIX);

        private readonly FileInfo _refFile;
        private readonly FileInfo _lockFile;
        private FileStream _os;
        private FileLock _fLck;
        private bool _haveLock;
        private readonly string _lockFilePath;


        public long CommitLastModified { get; private set; }
        public bool NeedStatInformation { get; set; }

        /// <summary>
        /// Create a new lock for any file.
        /// </summary>
        /// <param name="file">the file that will be locked.</param>
        public LockFile(FileInfo file)
        {
            _refFile = file;
            _lockFile = PathUtil.CombineFilePath(_refFile.Directory, _refFile.Name + SUFFIX);
            _lockFilePath = _lockFile.FullName;

        }

        /// <summary>
        /// Try to establish the lock.
        /// </summary>
        /// <returns>
        /// True if the lock is now held by the caller; false if it is held
        /// by someone else.
        /// </returns>
        /// <exception cref="IOException">
        /// the temporary output file could not be created. The caller
        /// does not hold the lock.
        /// </exception>
        public bool Lock()
        {
            _lockFile.Directory.Mkdirs();
            if (_lockFile.Exists)
            {
                return false;
            }

            try
            {
                _haveLock = true;
                _os = _lockFile.Create();

                _fLck = FileLock.TryLock(_os, _lockFile);
                if (_fLck == null)
                {
                    // We cannot use unlock() here as this file is not
                    // held by us, but we thought we created it. We must
                    // not delete it, as it belongs to some other process.
                    _haveLock = false;
                    try
                    {
                        _os.Close();
                    }
                    catch (Exception)
                    {
                        // Fail by returning haveLck = false.
                    }
                    _os = null;
                }
            }
            catch (Exception)
            {
                Unlock();
                throw;
            }

            return _haveLock;
        }

        /// <summary>
        /// Try to establish the lock for appending.
        /// </summary>
        /// <returns>
        /// True if the lock is now held by the caller; false if it is held
        /// by someone else.
        /// </returns>
        /// <exception cref="IOException">
        /// The temporary output file could not be created. The caller
        /// does not hold the lock.
        /// </exception>
        public bool LockForAppend()
        {
            if (!Lock())
            {
                return false;
            }

            CopyCurrentContent();

            return true;
        }


        /// <summary>
        /// Copy the current file content into the temporary file.
        /// <para />
        /// This method saves the current file content by inserting it into the
        /// temporary file, so that the caller can safely append rather than replace
        /// the primary file.
        /// <para />
        /// This method does nothing if the current file does not exist, or exists
        /// but is empty.
        /// </summary>
        /// <exception cref="IOException">
        /// The temporary file could not be written, or a read error
        /// occurred while reading from the current file. The lock is
        /// released before throwing the underlying IO exception to the
        /// caller. 
        /// </exception>
        public void CopyCurrentContent()
        {
            RequireLock();
            try
            {
                using (FileStream fis = _refFile.OpenRead())
                {
                    var buf = new byte[2048];
                    int r;
                    while ((r = fis.Read(buf, 0, buf.Length)) >= 0)
                        _os.Write(buf, 0, r);
                }
            }
            catch (FileNotFoundException)
            {
                // Don't worry about a file that doesn't exist yet, it
                // conceptually has no current content to copy.
                //
            }
            catch (Exception)
            {
                Unlock();
                throw;
            }
        }

        /// <summary>
        /// Write an ObjectId and LF to the temporary file.
        /// </summary>
        /// <param name="id">
        /// the id to store in the file. The id will be written in hex,
        /// followed by a sole LF.
        /// </param>
        public void Write(ObjectId id)
        {
            RequireLock();
            try
            {
                using (var b = new BinaryWriter(_os))
                {
                    id.CopyTo(b);
                    b.Write('\n');
                    b.Flush();
                    _fLck.Release();
                }
                _os = null;
            }
            catch (Exception)
            {
                Unlock();
                throw;
            }
        }

        /// <summary>
        /// Write arbitrary data to the temporary file.
        /// </summary>
        /// <param name="content">
        /// the bytes to store in the temporary file. No additional bytes
        /// are added, so if the file must end with an LF it must appear
        /// at the end of the byte array.
        /// </param>
        public void Write(byte[] content)
        {
            RequireLock();
            try
            {
                _os.Write(content, 0, content.Length);
                _os.Flush();
                _fLck.Release();
                _os.Close();
                _os = null;
            }
            catch (Exception)
            {
                Unlock();
                throw;
            }
        }

        /// <summary>
        /// Obtain the direct output stream for this lock.
        /// <para />
        /// The stream may only be accessed once, and only after <see cref="Lock()"/> has
        /// been successfully invoked and returned true. Callers must close the
        /// stream prior to calling <see cref="Commit()"/> to commit the change.
        /// </summary>
        /// <returns>
        /// A stream to write to the new file. The stream is unbuffered.
        /// </returns>
        public Stream GetOutputStream()
        {
            RequireLock();
            return new LockFileOutputStream(this);
        }

        private void RequireLock()
        {
            if (_os == null)
            {
                Unlock();
                throw new InvalidOperationException("Lock on " + _refFile + " not held.");
            }
        }

        /// <summary>
        /// Request that <see cref="Commit"/> remember modification time.
        /// </summary>
        /// <param name="on">true if the commit method must remember the modification time.</param>
        public void setNeedStatInformation(bool on)
        {
            NeedStatInformation = on;
        }

        /// <summary>
        /// Wait until the lock file information differs from the old file.
        /// <para/>
        /// This method tests both the length and the last modification date. If both
        /// are the same, this method sleeps until it can force the new lock file's
        /// modification date to be later than the target file.
        /// </summary>
        public void waitForStatChange()
        {
            _refFile.Refresh();
            _lockFile.Refresh();
            if (_refFile.Length == _lockFile.Length)
            {
                long otime = _refFile.lastModified();
                long ntime = _lockFile.lastModified();
                while (otime == ntime)
                {
                    Thread.Sleep(25 /* milliseconds */);
                    _lockFile.LastWriteTime = DateTime.Now;
                    ntime = _lockFile.lastModified();
                }
            }
        }

        /// <summary>
        /// Commit this change and release the lock.
        /// <para/>
        /// If this method fails (returns false) the lock is still released.
        /// </summary>
        /// <returns>
        /// true if the commit was successful and the file contains the new
        /// data; false if the commit failed and the file remains with the
        /// old data.
        /// </returns>
        public bool Commit()
        {
            if (_os != null)
            {
                Unlock();
                throw new InvalidOperationException("Lock on " + _refFile + " not closed.");
            }

            SaveStatInformation();

            if (_lockFile.RenameTo(_refFile.FullName))
            {
                return true;
            }

            _refFile.Refresh();
            if (!_refFile.Exists || _refFile.DeleteFile())
                if (_lockFile.RenameTo(_refFile.FullName))
                    return true;
            Unlock();
            return false;
        }

        private void SaveStatInformation()
        {
            if (NeedStatInformation)
            {
                _lockFile.Refresh();
                CommitLastModified = _lockFile.lastModified();
            }
        }

        /// <summary>
        /// Unlock this file and abort this change.
        /// <para/>
        /// The temporary file (if created) is deleted before returning.
        /// </summary>
        public void Unlock()
        {
            if (_os != null)
            {
                if (_fLck != null)
                {
                    try
                    {
                        _fLck.Release();
                    }
                    catch (IOException)
                    {
                        // Huh?
                    }
                    _fLck = null;
                }
                try
                {
                    _os.Close();
                }
                catch (IOException)
                {
                    // Ignore this
                }
                _os = null;
            }

            if (_haveLock)
            {
                _haveLock = false;
                File.Delete(_lockFilePath);
            }
        }

        public void Dispose()
        {
            if (_haveLock)
            {
                Unlock();
            }
        }


        #region Nested Types

        public class LockFileOutputStream : Stream
        {
            private readonly LockFile _lockFile;

            public LockFileOutputStream(LockFile lockfile)
            {
                _lockFile = lockfile;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _lockFile._os.Write(buffer, offset, count);
            }

            public void write(byte[] b)
            {
                _lockFile._os.Write(b, 0, b.Length);
            }

            public void write(int b)
            {
                _lockFile._os.WriteByte((byte)b);
            }

            public override void Flush()
            {
                _lockFile._os.Flush();
            }

            public override void Close()
            {
                try
                {
                    _lockFile._os.Flush();
                    _lockFile._fLck.Release();
                    _lockFile._os.Close();
                    _lockFile._os = null;
                }
                catch (Exception)
                {
                    _lockFile.Unlock();
                    throw;
                }
            }

            public override bool CanRead
            {
                get { return false; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override long Length
            {
                get { throw new NotImplementedException(); }
            }

            public override long Position
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Wraps a FileStream and tracks its locking status
        /// </summary>
        public class FileLock : IDisposable
        {
            public FileStream FileStream { get; private set; }
            public bool Locked { get; private set; }
            public string File { get; private set; }

            private FileLock(FileStream fs, string file)
            {
                File = file;
                FileStream = fs;
                FileStream.Lock(0, long.MaxValue);
                Locked = true;
            }

            public static FileLock TryLock(FileStream fs, FileInfo file)
            {
                try
                {
                    return new FileLock(fs, file.FullName);
                }
                catch (IOException e)
                {
                    Debug.WriteLine("Could not lock " + file.FullName);
                    Debug.WriteLine(e.Message);
                    return null;
                }
            }

            public void Dispose()
            {
                Release();
            }

            public void Release()
            {
                if (Locked == false)
                {
                    return;
                }
                try
                {
                    FileStream.Unlock(0, long.MaxValue);
#if DEBUG
                    GC.SuppressFinalize(this); // [henon] disarm lock-release checker
#endif
                }
                catch (IOException)
                {
                    // unlocking went wrong
                    Console.Error.WriteLine(GetType().Name + ": tried to unlock an unlocked filelock " + File);
                    throw;
                }
                Locked = false;
            }

#if DEBUG
            // [henon] this : a debug mode warning if the filelock has not been disposed properly
            ~FileLock()
            {
                Console.Error.WriteLine(GetType().Name + " has not been properly disposed: " + File);
            }
#endif
        }

        #endregion
    }
}
