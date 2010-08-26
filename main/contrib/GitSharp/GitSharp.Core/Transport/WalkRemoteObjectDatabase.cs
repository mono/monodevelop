/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Transfers object data through a dumb transport.
    /// <para/>
    /// Implementations are responsible for resolving path names relative to the
    /// <code>objects/</code> subdirectory of a single remote Git repository or
    /// naked object database and make the content available as a Java input stream
    /// for reading during fetch. The actual object traversal logic to determine the
    /// names of files to retrieve is handled through the generic, protocol
    /// independent <see cref="WalkFetchConnection"/>.
    /// </summary>
    public abstract class WalkRemoteObjectDatabase : IDisposable
    {
        public const string ROOT_DIR = "../";
        public const string INFO_PACKS = "info/packs";
        public const string INFO_ALTERNATES = "info/alternates";
        public const string INFO_HTTP_ALTERNATES = "info/http-alternates";
        public static string INFO_REFS = ROOT_DIR + Constants.INFO_REFS;

        public abstract URIish getURI();

        /// <summary>
        /// Obtain the list of available packs (if any).
        /// <para/>
        /// Pack names should be the file name in the packs directory, that is
        /// <code>pack-035760ab452d6eebd123add421f253ce7682355a.pack</code>. Index
        /// names should not be included in the returned collection.
        /// </summary>
        /// <returns>list of pack names; null or empty list if none are available.</returns>
        public abstract ICollection<string> getPackNames();

        /// <summary>
        /// Obtain alternate connections to alternate object databases (if any).
        /// <para/>
        /// Alternates are typically read from the file <see cref="INFO_ALTERNATES"/> or
        /// <see cref="INFO_HTTP_ALTERNATES"/>. The content of each line must be resolved
        /// by the implementation and a new database reference should be returned to
        /// represent the additional location.
        /// <para/>
        /// Alternates may reuse the same network connection handle, however the
        /// fetch connection will <see cref="close"/> each created alternate.
        /// </summary>
        /// <returns>
        /// list of additional object databases the caller could fetch from;
        /// null or empty list if none are configured.
        /// </returns>
        public abstract ICollection<WalkRemoteObjectDatabase> getAlternates();

        /// <summary>
        /// Open a single file for reading.
        /// <para/>
        /// Implementors should make every attempt possible to ensure
        /// {@link FileNotFoundException} is used when the remote object does not
        /// exist. However when fetching over HTTP some misconfigured servers may
        /// generate a 200 OK status message (rather than a 404 Not Found) with an
        /// HTML formatted message explaining the requested resource does not exist.
        /// Callers such as <see cref="WalkFetchConnection"/> are prepared to handle this
        /// by validating the content received, and assuming content that fails to
        /// match its hash is an incorrectly phrased FileNotFoundException.
        /// </summary>
        /// <param name="path">
        /// location of the file to read, relative to this objects
        /// directory (e.g.
        /// <code>cb/95df6ab7ae9e57571511ef451cf33767c26dd2</code> or
        /// <code>pack/pack-035760ab452d6eebd123add421f253ce7682355a.pack</code>).
        /// </param>
        /// <returns>a stream to read from the file. Never null.</returns>
        public abstract Stream open(string path);

        /// <summary>
        /// Create a new connection for a discovered alternate object database
        /// <para/>
        /// This method is typically called by <see cref="readAlternates"/> when
        /// subclasses us the generic alternate parsing logic for their
        /// implementation of <see cref="getAlternates"/>.
        /// </summary>
        /// <param name="location">
        /// the location of the new alternate, relative to the current
        /// object database.
        /// </param>
        /// <returns>
        /// a new database connection that can read from the specified
        /// alternate.
        /// </returns>
        public abstract WalkRemoteObjectDatabase openAlternate(string location);

        /// <summary>
        /// Close any resources used by this connection.
        /// <para/>
        /// If the remote repository is contacted by a network socket this method
        /// must close that network socket, disconnecting the two peers. If the
        /// remote repository is actually local (same system) this method must close
        /// any open file handles used to read the "remote" repository.
        /// </summary>
        public abstract void close();

        public virtual void Dispose()
        {
            close();
        }

        /// <summary>
        /// Delete a file from the object database.
        /// <para/>
        /// Path may start with <code>../</code> to request deletion of a file that
        /// resides in the repository itself.
        /// <para/>
        /// When possible empty directories must be removed, up to but not includin
        /// the current object database directory itself.
        /// <para/>
        /// This method does not support deletion of directories.
        /// </summary>
        /// <param name="path">
        /// name of the item to be removed, relative to the current object
        /// database.
        /// </param>
        public virtual void deleteFile(string path)
        {
            throw new IOException("Deleting '" + path + "' not supported");
        }

        /// <summary>
        /// Open a remote file for writing.
        /// <para/>
        /// Path may start with <code>../</code> to request writing of a file that
        /// resides in the repository itself.
        /// <para/>
        /// The requested path may or may not exist. If the path already exists as a
        /// file the file should be truncated and completely replaced.
        /// <para/>
        /// This method creates any missing parent directories, if necessary.
        /// </summary>
        /// <param name="path">
        /// name of the file to write, relative to the current object
        /// database.
        /// </param>
        /// <param name="monitor">
        /// (optional) progress monitor to post write completion to during
        /// the stream's close method.
        /// </param>
        /// <param name="monitorTask">
        /// (optional) task name to display during the close method.
        /// </param>
        /// <returns>
        /// stream to write into this file. Caller must close the stream to
        /// complete the write request. The stream is not buffered and each
        /// write may cause a network request/response so callers should
        /// buffer to smooth out small writes.
        /// </returns>
        public virtual Stream writeFile(string path, ProgressMonitor monitor, string monitorTask)
        {
            throw new IOException("Writing of '" + path + "' not supported.");
        }

        /// <summary>
        /// Atomically write a remote file.
        /// <para/>
        /// This method attempts to perform as atomic of an update as it can,
        /// reducing (or eliminating) the time that clients might be able to see
        /// partial file content. This method is not suitable for very large
        /// transfers as the complete content must be passed as an argument.
        /// <para/>
        /// Path may start with <code>../</code> to request writing of a file that
        /// resides in the repository itself.
        /// <para/>
        /// The requested path may or may not exist. If the path already exists as a
        /// file the file should be truncated and completely replaced.
        /// <para/>
        /// This method creates any missing parent directories, if necessary.
        /// </summary>
        /// <param name="path">
        /// name of the file to write, relative to the current object
        /// database.
        /// </param>
        /// <param name="data">complete new content of the file.</param>
        public virtual void writeFile(string path, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            using (Stream fs = writeFile(path, null, null))
            {
                fs.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Delete a loose ref from the remote repository.
        /// </summary>
        /// <param name="name">
        /// name of the ref within the ref space, for example
        /// <code>refs/heads/pu</code>.
        /// </param>
        public void deleteRef(string name)
        {
            deleteFile(ROOT_DIR + name);
        }

        /// <summary>
        /// Delete a reflog from the remote repository.
        /// </summary>
        /// <param name="name">
        /// name of the ref within the ref space, for example
        /// <code>refs/heads/pu</code>.
        /// </param>
        public void deleteRefLog(string name)
        {
            deleteFile(ROOT_DIR + Constants.LOGS + "/" + name);
        }

        /// <summary>
        /// Overwrite (or create) a loose ref in the remote repository.
        /// <para/>
        /// This method creates any missing parent directories, if necessary. 
        /// </summary>
        /// <param name="name">
        /// name of the ref within the ref space, for example
        /// <code>refs/heads/pu</code>.
        /// </param>
        /// <param name="value">new value to store in this ref. Must not be null.</param>
        public void writeRef(string name, ObjectId value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            using (var m = new MemoryStream(Constants.OBJECT_ID_STRING_LENGTH + 1))
            using (var b = new BinaryWriter(m))
            {
                value.CopyTo(b);
                b.Write('\n');
                b.Flush();

                writeFile(ROOT_DIR + name, m.ToArray());
            }
        }

        /// <summary>
        /// Rebuild the <see cref="INFO_PACKS"/> for dumb transport clients.
        /// <para/>
        /// This method rebuilds the contents of the <see cref="INFO_PACKS"/> file to
        /// match the passed list of pack names.
        /// </summary>
        /// <param name="packNames">
        /// names of available pack files, in the order they should appear
        /// in the file. Valid pack name strings are of the form
        /// <code>pack-035760ab452d6eebd123add421f253ce7682355a.pack</code>.
        /// </param>
        public void writeInfoPacks(ICollection<string> packNames)
        {
            if (packNames == null)
                throw new ArgumentNullException("packNames");

            var w = new StringBuilder();
            foreach (string n in packNames)
            {
                w.Append("P ");
                w.Append(n);
                w.Append('\n');
            }

            writeFile(INFO_PACKS, Constants.encodeASCII(w.ToString()));
        }

        /// <summary>
        /// Open a buffered reader around a file.
        /// <para/>
        /// This is shorthand for calling <see cref="open"/> and then wrapping it
        /// in a reader suitable for line oriented files like the alternates list.
        /// </summary>
        /// <param name="path">
        /// location of the file to read, relative to this objects
        /// directory (e.g. <code>info/packs</code>).
        /// </param>
        /// <returns>a stream to read from the file. Never null.</returns>
        public StreamReader openReader(string path)
        {
            Stream s = open(path);
            // StreamReader buffers itself
            return new StreamReader(s, Constants.CHARSET);
        }

        /// <summary>
        /// Read a standard Git alternates file to discover other object databases.
        /// <para/>
        /// This method is suitable for reading the standard formats of the
        /// alternates file, such as found in <code>objects/info/alternates</code>
        /// or <code>objects/info/http-alternates</code> within a Git repository.
        /// <para/>
        /// Alternates appear one per line, with paths expressed relative to this
        /// object database.
        /// </summary>
        /// <param name="listPath">
        /// location of the alternate file to read, relative to this
        /// object database (e.g. <code>info/alternates</code>).
        /// </param>
        /// <returns>
        /// the list of discovered alternates. Empty list if the file exists,
        /// but no entries were discovered.
        /// </returns>
        public ICollection<WalkRemoteObjectDatabase> readAlternates(string listPath)
        {
            using (StreamReader sr = openReader(listPath))
            {
                var alts = new List<WalkRemoteObjectDatabase>();
                for (; ; )
                {
                    string line = sr.ReadLine();
                    if (line == null) break;
                    if (!line.EndsWith("/"))
                        line += "/";
                    alts.Add(openAlternate(line));
                }
                return alts;
            }
        }

        /// <summary>
        /// Read a standard Git packed-refs file to discover known references.
        /// </summary>
        /// <param name="avail">
        /// return collection of references. Any existing entries will be
        /// replaced if they are found in the packed-refs file.
        /// </param>
        public void readPackedRefs(Dictionary<string, Ref> avail)
        {
            try
            {
                using (StreamReader sr = openReader(ROOT_DIR + Constants.PACKED_REFS))
                {
                    readPackedRefsImpl(avail, sr);
                }
            }
            catch (FileNotFoundException)
            {
                // Perhaps it wasn't worthwhile, or is just an older repository.
            }
            catch (IOException e)
            {
                throw new TransportException(getURI(), "error in packed-refs", e);
            }
        }

        private static void readPackedRefsImpl(Dictionary<string, Ref> avail, StreamReader sr)
        {
            Ref last = null;
            bool peeled = false;
            for (; ; )
            {
                string line = sr.ReadLine();

                if (line == null)
                    break;
                if (line[0] == '#')
                {
                    if (line.StartsWith(RefDirectory.PACKED_REFS_HEADER))
                    {
                        line = line.Substring(RefDirectory.PACKED_REFS_HEADER.Length);
                        peeled = line.Contains(RefDirectory.PACKED_REFS_PEELED);
                    }
                    continue;
                }

                if (line[0] == '^')
                {
                    if (last == null)
                        throw new TransportException("Peeled line before ref");
                    ObjectId pid = ObjectId.FromString(line.Substring(1));
                    last = new PeeledTag(Storage.Packed, last.Name, last.ObjectId, pid);
                    avail.put(last.Name, last);
                    continue;
                }

                int sp = line.IndexOf(' ');
                if (sp < 0)
                    throw new TransportException("Unrecognized ref: " + line);
                ObjectId id = ObjectId.FromString(line.Slice(0, sp));
                string name = line.Substring(sp + 1);
                if (peeled)
                    last = new PeeledNonTag(Storage.Packed, name, id);
                else
                    last = new Unpeeled(Storage.Packed, name, id);

                avail.put(last.Name, last);
            }
        }
    }
}