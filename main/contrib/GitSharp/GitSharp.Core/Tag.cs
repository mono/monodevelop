/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

namespace GitSharp.Core
{
    public class Tag
    {
        public Repository Repository { get; internal set; }

		private PersonIdent author;
		private string message;
		private string tagType;
        private byte[] raw;

        /**
         * Construct a new, yet unnamed Tag.
         *
         * @param db
         */
        public Tag(Repository db)
        {
            Repository = db;
        }

        /**
         * Construct a Tag representing an existing with a known name referencing an known object.
         * This could be either a simple or annotated tag.
         *
         * @param db {@link Repository}
         * @param id target id.
         * @param refName tag name or null
         * @param raw data of an annotated tag.
         */
        public Tag(Repository db, ObjectId id, string refName, byte[] raw)
        {
            Repository = db;
            if (raw != null)
            {
                TagId = id;
                Id = ObjectId.FromString(raw, 7);
            }
            else
                Id = id;
            if (refName != null && refName.StartsWith("refs/tags/"))
                refName = refName.Substring(10);
            TagName = refName;
            this.raw = raw;
        }

        /**
         * @return tagger of a annotated tag or null
         */
        public PersonIdent Author
        {
            get
            {
                decode();
                return author;
            }
            set
            {
                author = value;
            }
        }

        /**
         * @return comment of an annotated tag, or null
         */
        public string Message
        {
            get
            {
                decode();
                return message;
            }
            set
            {
                message = value;
            }
        }

        private void decode()
        {
            // FIXME: handle I/O errors
            if (raw == null) return;

            using (var br = new StreamReader(new MemoryStream(raw)))
            {
                string n = br.ReadLine();
                if (n == null || !n.StartsWith("object "))
                {
                    throw new CorruptObjectException(TagId, "no object");
                }
                Id = ObjectId.FromString(n.Substring(7));
                n = br.ReadLine();
                if (n == null || !n.StartsWith("type "))
                {
                    throw new CorruptObjectException(TagId, "no type");
                }
                TagType = n.Substring("type ".Length);
                n = br.ReadLine();

                if (n == null || !n.StartsWith("tag "))
                {
                    throw new CorruptObjectException(TagId, "no tag name");
                }
                TagName = n.Substring("tag ".Length);
                n = br.ReadLine();

                // We should see a "tagger" header here, but some repos have tags
                // without it.
                if (n == null)
                    throw new CorruptObjectException(TagId, "no tagger header");

                if (n.Length > 0)
                    if (n.StartsWith("tagger "))
                        Tagger = new PersonIdent(n.Substring("tagger ".Length));
                    else
                        throw new CorruptObjectException(TagId, "no tagger/bad header");

                // Message should start with an empty line, but
                StringBuilder tempMessage = new StringBuilder();
                char[] readBuf = new char[2048];
                int readLen;
                int readIndex = 0;
                while ((readLen = br.Read(readBuf, readIndex, readBuf.Length)) > 0)
                {
                    //readIndex += readLen;
                    tempMessage.Append(readBuf, 0, readLen);
                }
                message = tempMessage.ToString();
                if (message.StartsWith("\n"))
                    message = message.Substring(1);
            }

            raw = null;
        }


        /**
         * Store a tag.
         * If author, message or type is set make the tag an annotated tag.
         *
         * @
         */
        public void Save()  //renamed from Tag
        {
            if (TagId != null)
                throw new InvalidOperationException("exists " + TagId);
            ObjectId id;

            if (author != null || message != null || tagType != null)
            {
                ObjectId tagid = new ObjectWriter(Repository).WriteTag(this);
                TagId = tagid;
                id = tagid;
            }
            else
            {
                id = Id;
            }

            RefUpdate ru = Repository.UpdateRef(Constants.R_TAGS + TagName);
            ru.NewObjectId = id;
            ru.setRefLogMessage("tagged " + TagName, false);
            if (ru.forceUpdate() == RefUpdate.RefUpdateResult.LOCK_FAILURE)
                throw new ObjectWritingException("Unable to lock tag " + TagName);
        }

        public override string ToString()
        {
            return "tag[" + TagName + TagType + Id + " " + Author + "]";
        }

        public ObjectId TagId { get; set; }

        /**
         * @return creator of this tag.
         */
        public PersonIdent Tagger
        {
            get { return Author; }
            set { Author = value; }
        }


        /**
         * @return tag target type
         */
        
        public string TagType
        {
            get
            {
                decode();
                return tagType;
            }
            set
            {
                tagType = value;
            }
        }

        /// <summary>
        /// the SHA'1 of the object this tag refers to
        /// </summary>
        public string TagName { get; set; }


        /// <summary>Id of the object this tag refers to</summary>
        public ObjectId Id { get; set; }
    }
}
