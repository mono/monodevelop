/*
 * Copyright (C) 2008, 2009, Google Inc.
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
using GitSharp.Core.RevWalk;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Support for the start of <see cref="UploadPack"/> and <see cref="ReceivePack"/>.
    /// </summary>
    public abstract class RefAdvertiser : IDisposable
    {
        /// <summary>
        /// Advertiser which frames lines in a {@link PacketLineOut} format.
        /// </summary>
        public class PacketLineOutRefAdvertiser : RefAdvertiser
        {
            private PacketLineOut pckOut;

            /// <summary>
            /// Create a new advertiser for the supplied stream.
            /// </summary>
            /// <param name="out">the output stream.</param>
            public PacketLineOutRefAdvertiser(PacketLineOut @out)
            {
                pckOut = @out;
            }

            protected override void writeOne(string line)
            {
                pckOut.WriteString(line.ToString());
            }

            public override void end()
            {
                pckOut.End();
            }
        }
        private RevWalk.RevWalk _walk;
        private RevFlag ADVERTISED;
        private readonly StringBuilder _tmpLine = new StringBuilder(100);
        private readonly char[] _tmpId = new char[Constants.OBJECT_ID_STRING_LENGTH];
        private readonly List<string> _capabilities = new List<string>();
        private bool _derefTags;
        private bool _first = true;

        /// <summary>
        /// Initialize a new advertisement formatter.
        /// </summary>
        /// <param name="protoWalk">the RevWalk used to parse objects that are advertised.</param>
        /// <param name="advertisedFlag">
        /// flag marked on any advertised objects parsed out of the
        /// <paramref name="protoWalk"/>'s object pool, permitting the caller to
        /// later quickly determine if an object was advertised (or not).
        /// </param>
        public void init(RevWalk.RevWalk protoWalk, RevFlag advertisedFlag)
        {
            _walk = protoWalk;
            ADVERTISED = advertisedFlag;
        }

        /// <summary>
        /// Toggle tag peeling.
        /// <para/>
        /// This method must be invoked prior to any of the following:
        /// <see cref="send"/>, <see cref="advertiseHave"/>, <see cref="includeAdditionalHaves"/>.
        /// </summary>
        /// <param name="deref">
        /// true to show the dereferenced value of a tag as the special
        /// ref <code>$tag^{}</code> ; false to omit it from the output.
        /// </param>
        public void setDerefTags(bool deref)
        {
            _derefTags = deref;
        }

        /// <summary>
        /// Add one protocol capability to the initial advertisement.
        /// <para/>
        /// This method must be invoked prior to any of the following:
        /// <see cref="send"/>, <see cref="advertiseHave"/>, <see cref="includeAdditionalHaves"/>.
        /// </summary>
        /// <param name="name">
        /// the name of a single protocol capability supported by the
        /// caller. The set of capabilities are sent to the client in the
        /// advertisement, allowing the client to later selectively enable
        /// features it recognizes.
        /// </param>
        public void advertiseCapability(string name)
        {
            _capabilities.Add(name);
        }

        /// <summary>
        /// Format an advertisement for the supplied refs.
        /// </summary>
        /// <param name="refs">
        /// zero or more refs to format for the client. The collection is
        /// sorted before display if necessary, and therefore may appear
        /// in any order.
        /// </param>
        public void send(IDictionary<string, Ref> refs)
        {
            foreach (Ref r in getSortedRefs(refs))
            {
                RevObject obj = parseAnyOrNull(r.ObjectId);
                if (obj != null)
                {
                    advertiseAny(obj, r.Name);
                    RevTag rt = (obj as RevTag);
                    if (_derefTags && rt != null)
                    {
                        advertiseTag(rt, r.Name + "^{}");
                    }
                }
            }
        }

        private IEnumerable<Ref> getSortedRefs(IDictionary<string, Ref> all)
        {
            if (all is RefMap
                    || (all is SortedDictionary<string, Ref>))
                return all.Values;
            return RefComparator.Sort(all.Values);
        }
        
        /// <summary>
        /// Advertise one object is available using the magic <code>.have</code>.
        /// <para/>
        /// The magic <code>.have</code> advertisement is not available for fetching by a
        /// client, but can be used by a client when considering a delta base
        /// candidate before transferring data in a push. Within the record created
        /// by this method the ref name is simply the invalid string <code>.have</code>.
        /// </summary>
        /// <param name="id">
        /// identity of the object that is assumed to exist.
        /// </param>
        public void advertiseHave(AnyObjectId id)
        {
            RevObject obj = parseAnyOrNull(id);
            if (obj != null)
            {
                advertiseAnyOnce(obj, ".have");
            }

            RevTag rt = (obj as RevTag);
            if (rt != null)
            {
                advertiseAnyOnce(rt.getObject(), ".have");
            }
        }

        /// <summary>
        /// Include references of alternate repositories as {@code .have} lines.
        /// </summary>
        public void includeAdditionalHaves()
        {
            additionalHaves(_walk.Repository.ObjectDatabase);
        }

        private void additionalHaves(ObjectDatabase db)
        {
            AlternateRepositoryDatabase b = (db as AlternateRepositoryDatabase);
            if (b != null)
            {
                additionalHaves(b.getRepository());
            }

            foreach (ObjectDatabase alt in db.getAlternates())
            {
                additionalHaves(alt);
            }
        }

        private void additionalHaves(Repository alt)
        {
            foreach (Ref r in alt.getAllRefs().Values)
            {
                advertiseHave(r.ObjectId);
            }
        }

        /// <returns>true if no advertisements have been sent yet.</returns>
        public bool isEmpty()
        {
            return _first;
        }

        private RevObject parseAnyOrNull(AnyObjectId id)
        {
            if (id == null) return null;

            try
            {
                return _walk.parseAny(id);
            }
            catch (IOException)
            {
                return null;
            }
        }

        private void advertiseAnyOnce(RevObject obj, string refName)
        {
            if (!obj.has(ADVERTISED))
            {
                advertiseAny(obj, refName);
            }
        }

        private void advertiseAny(RevObject obj, string refName)
        {
            obj.add(ADVERTISED);
            advertiseId(obj, refName);
        }

        private void advertiseTag(RevTag tag, string refName)
        {
            RevObject o = tag;
            do
            {
                // Fully unwrap here so later on we have these already parsed.
                RevObject target = (((RevTag)o).getObject());
                try
                {
                    _walk.parseHeaders(target);
                }
                catch (IOException)
                {
                    return;
                }
                target.add(ADVERTISED);
                o = target;
            } while (o is RevTag);

            advertiseAny(tag.getObject(), refName);
        }

        /// <summary>
        /// Advertise one object under a specific name.
        /// <para/>
        /// If the advertised object is a tag, this method does not advertise the
        /// peeled version of it.
        /// </summary>
        /// <param name="id">
        /// the object to advertise.
        /// </param>
        /// <param name="refName">
        /// name of the reference to advertise the object as, can be any
        /// string not including the NUL byte.
        /// </param>
        public void advertiseId(AnyObjectId id, string refName)
        {
            _tmpLine.Length = 0;
            id.CopyTo(_tmpId, _tmpLine);
            _tmpLine.Append(' ');
            _tmpLine.Append(refName);

            if (_first)
            {
                _first = false;
                if (_capabilities.Count > 0)
                {
                    _tmpLine.Append('\0');
                    foreach (string capName in _capabilities)
                    {
                        _tmpLine.Append(' ');
                        _tmpLine.Append(capName);
                    }
                    _tmpLine.Append(' ');
                }
            }

            _tmpLine.Append('\n');
            writeOne(_tmpLine.ToString());
        }

        public void Dispose()
        {
            _walk.Dispose();
            ADVERTISED.Dispose();
        }

        /// <summary>
        /// Write a single advertisement line.
        /// </summary>
        /// <param name="line">
        /// the advertisement line to be written. The line always ends
        /// with LF. Never null or the empty string.
        /// </param>
        protected abstract void writeOne(string line);

        /// <summary>
        /// Mark the end of the advertisements.
        /// </summary>
        public abstract void end();
    }
}