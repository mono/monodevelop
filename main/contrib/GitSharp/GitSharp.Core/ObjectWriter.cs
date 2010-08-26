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
using System.Text;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace GitSharp.Core
{
    public class ObjectWriter
    {
        // Fields
        private static readonly byte[] HAuthor = Constants.encodeASCII("author");
        private static readonly byte[] HCommitter = Constants.encodeASCII("committer");
        private static readonly byte[] HEncoding = Constants.encodeASCII("encoding");
        private static readonly byte[] HParent = Constants.encodeASCII("parent");
        private static readonly byte[] HTree = Constants.encodeASCII("tree");
        private readonly byte[] _buf;
        private readonly MessageDigest _md;
        private readonly Repository _r;

        ///	<summary>
        /// Construct an object writer for the specified repository.
        /// </summary>
        ///	<param name="repo"> </param>
        public ObjectWriter(Repository repo)
        {
            _r = repo;
            _buf = new byte[0x2000];
            _md = Constants.newMessageDigest();
        }

        ///	<summary>
        /// Compute the SHA-1 of a blob without creating an object. This is for
        ///	figuring out if we already have a blob or not.
        ///	</summary>
        ///	<param name="length"> number of bytes to consume.</param>
        ///	<param name="input"> stream for read blob data from.</param>
        ///	<returns>SHA-1 of a looked for blob.</returns>
        ///	<exception cref="IOException"></exception>
        public ObjectId ComputeBlobSha1(long length, Stream input)
        {
            return WriteObject(ObjectType.Blob, length, input, false);
        }

        /// <summary>
        /// Write a blob with the data in the specified file
        /// </summary>
        /// <param name="fileInfo">A file containing blob data.</param>
        ///	<returns>SHA-1 of the blob.</returns>
        ///	<exception cref="IOException"></exception>
        public ObjectId WriteBlob(FileInfo fileInfo)
        {
            using (FileStream stream = fileInfo.OpenRead())
            {
                return WriteBlob(fileInfo.Length, stream);
            }
        }

        ///	<summary>
        /// Write a blob with the specified data.
        ///	</summary>
        ///	<param name="b">Bytes of the blob.</param>
        ///	<returns>SHA-1 of the blob.</returns>
        ///	<exception cref="IOException"></exception>
        public ObjectId WriteBlob(byte[] b)
        {
            return WriteBlob(b.Length, new MemoryStream(b));
        }

        ///	<summary>
        /// Write a blob with data from a stream
        ///	</summary>
        ///	<param name="len">Number of bytes to consume from the stream.</param>
        ///	<param name="input">Stream with blob data.</param>
        ///	<returns>SHA-1 of the blob.</returns>
        ///	<exception cref="IOException"></exception>
        public ObjectId WriteBlob(long len, Stream input)
        {
            return WriteObject(ObjectType.Blob, len, input, true);
        }

        ///	<summary>
        /// Write a canonical tree to the object database.
        /// </summary>
        /// <param name="buffer">The canonical encoding of the tree object.</param>
        ///	<returns>SHA-1 of the tree.</returns>
        ///	<exception cref="IOException"></exception>
        public ObjectId WriteCanonicalTree(byte[] buffer)
        {
            return WriteTree(buffer.Length, new MemoryStream(buffer));
        }

        ///	<summary>
        /// Write a Commit to the object database
        ///	</summary>
        ///	<param name="c">Commit to store.</param>
        ///	<returns>SHA-1 of the commit.</returns>
        ///	<exception cref="IOException"></exception>
        public ObjectId WriteCommit(Commit c)
        {
            Encoding encoding = c.Encoding ?? Constants.CHARSET;
            var output = new MemoryStream();
            var s = new BinaryWriter(output, encoding);
            s.Write(HTree);
            s.Write(' ');
            c.TreeId.CopyTo(s);
            s.Write('\n');
            ObjectId[] parentIds = c.ParentIds;
            for (int i = 0; i < parentIds.Length; i++)
            {
                s.Write(HParent);
                s.Write(' ');
                parentIds[i].CopyTo(s);
                s.Write('\n');
            }
            s.Write(HAuthor);
            s.Write(' ');
            s.Write(c.Author.ToExternalString().ToCharArray());
            s.Write('\n');
            s.Write(HCommitter);
            s.Write(' ');
            s.Write(c.Committer.ToExternalString().ToCharArray());
            s.Write('\n');

            if (encoding != Constants.CHARSET)
            {
                s.Write(HEncoding);
                s.Write(' ');
                s.Write(Constants.encodeASCII(encoding.HeaderName.ToUpperInvariant()));
                s.Write('\n');
            }

            s.Write('\n');
            s.Write(c.Message.ToCharArray());

            return WriteCommit(output.ToArray());
        }

        private ObjectId WriteCommit(byte[] b)
        {
            return WriteCommit(b.Length, new MemoryStream(b));
        }

        private ObjectId WriteCommit(long len, Stream input)
        {
            return WriteObject(ObjectType.Commit, len, input, true);
        }

        internal ObjectId WriteObject(ObjectType type, long len, Stream input, bool store)
        {
            FileInfo info;
            DeflaterOutputStream stream;
            FileStream stream2;
            ObjectId objectId = null;
            Deflater def = null;

            if (store)
            {
                info = _r.ObjectsDirectory.CreateTempFile("noz");
                stream2 = info.OpenWrite();
            }
            else
            {
                info = null;
                stream2 = null;
            }

            _md.Reset();
            if (store)
            {
                def = new Deflater(_r.Config.getCore().getCompression());
                stream = new DeflaterOutputStream(stream2, def);
            }
            else
            {
                stream = null;
            }

            try
            {
                int num;
                byte[] bytes = Codec.EncodedTypeString(type);
                _md.Update(bytes);
                if (stream != null)
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                _md.Update(0x20);
                if (stream != null)
                {
                    stream.WriteByte(0x20);
                }

                bytes = Constants.encodeASCII(len.ToString());
                _md.Update(bytes);
                if (stream != null)
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                _md.Update(0);
                if (stream != null)
                {
                    stream.WriteByte(0);
                }
                while ((len > 0L) && ((num = input.Read(_buf, 0, (int)Math.Min(len, _buf.Length))) > 0))
                {
                    _md.Update(_buf, 0, num);
                    if (stream != null)
                    {
                        stream.Write(_buf, 0, num);
                    }
                    len -= num;
                }

                if (len != 0L)
                {
                    throw new IOException("Input did not match supplied Length. " + len + " bytes are missing.");
                }

                if (stream != null)
                {
                    stream.Close();
                    if (info != null)
                    {
                        info.IsReadOnly = true;
                    }
                }
                objectId = ObjectId.FromRaw(_md.Digest());
            }
            finally
            {
                if ((objectId == null) && (stream != null))
                {
                    try
                    {
                        stream.Close();
                    }
                    finally
                    {
                        info.DeleteFile();
                    }

                    if (def != null)
                    {
                        def.Finish();
                    }
                }
            }
            if (info != null)
            {
                if (_r.HasObject(objectId))
                {
                    // Object is already in the repository so remove
                    // the temporary file.
                    //
                    info.DeleteFile();
                }
                else
                {
                    FileInfo info2 = _r.ToFile(objectId);
                    if (!info.RenameTo(info2.FullName))
                    {
                        // Maybe the directory doesn't exist yet as the object
                        // directories are always lazily created. Note that we
                        // try the rename first as the directory likely does exist.
                        //
                        if (info2.Directory != null)
                        {
                            info2.Directory.Create();
                        }
                        if (!info.RenameTo(info2.FullName) && !_r.HasObject(objectId))
                        {
                            // The object failed to be renamed into its proper
                            // location and it doesn't exist in the repository
                            // either. We really don't know what went wrong, so
                            // fail.
                            //
                            info.DeleteFile();
                            throw new ObjectWritingException("Unable to create new object: " + info2);
                        }
                    }
                }
            }
            return objectId;
        }

        ///	<summary>
        /// Write an annotated Tag to the object database
        ///	</summary>
        ///	<param name="tag">Tag</param>
        ///	<returns>SHA-1 of the tag.</returns>
        ///	<exception cref="IOException"></exception>
        public ObjectId WriteTag(Tag tag)
        {
            using (var output = new MemoryStream())
            using (var s = new BinaryWriter(output))
            {
                s.Write("object ".ToCharArray());
                tag.Id.CopyTo(s);
                s.Write('\n');
                s.Write("type ".ToCharArray());
                s.Write(tag.TagType.ToCharArray());
                s.Write('\n');
                s.Write("tag ".ToCharArray());
                s.Write(tag.TagName.ToCharArray());
                s.Write('\n');
                s.Write("tagger ".ToCharArray());
                s.Write(tag.Author.ToExternalString().ToCharArray());
                s.Write('\n');
                s.Write('\n');
                s.Write(tag.Message.ToCharArray());

                return WriteTag(output.ToArray());
            }
        }

        private ObjectId WriteTag(byte[] b)
        {
            return WriteTag(b.Length, new MemoryStream(b));
        }

        private ObjectId WriteTag(long len, Stream input)
        {
            return WriteObject(ObjectType.Tag, len, input, true);
        }

        public ObjectId WriteTree(Tree t)
        {
            var output = new MemoryStream();
            var writer = new BinaryWriter(output);
            foreach (TreeEntry entry in t.Members)
            {
                ObjectId id = entry.Id;
                if (id == null)
                {
                    throw new ObjectWritingException("object at path \"" + entry.FullName +
                                                     "\" does not have an id assigned.  All object ids must be assigned prior to writing a tree.");
                }
                entry.Mode.CopyTo(output);
                writer.Write((byte)0x20);
                writer.Write(entry.NameUTF8);
                writer.Write((byte)0);
                id.copyRawTo(output);
            }
            return WriteCanonicalTree(output.ToArray());
        }

        private ObjectId WriteTree(long len, Stream input)
        {
            return WriteObject(ObjectType.Tree, len, input, true);
        }
    }
}