/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2007, Shawn O. Pearce <spearce@spearce.org>
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
using System.Text;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
    public abstract class TreeEntry : IComparable, IComparable<TreeEntry>
    {
        // Fields
        public static int CONCURRENT_MODIFICATION = 4;
        public static int LOADED_ONLY = 2;
        public static int MODIFIED_ONLY = 1;
        private ObjectId _id;

        // Methods
        protected TreeEntry(Tree myParent, ObjectId id, byte[] nameUTF8)
        {
            NameUTF8 = nameUTF8;
            Parent = myParent;
            _id = id;
        }

        // Properties
        public string FullName
        {
            get
            {
                var r = new StringBuilder();
                AppendFullName(r);
                return r.ToString();
            }
        }

        public byte[] FullNameUTF8
        {
            get { return Constants.CHARSET.GetBytes(FullName); }
        }

        public ObjectId Id
        {
            get { return _id; }
            set
            {
                Tree parent = Parent;
                if (((parent != null) && (_id != value)) &&
                    !((((_id != null) || (value == null)) && ((_id == null) || (value != null))) &&
                      _id.Equals(value)))
                {
                    parent.Id = null;
                }
                _id = value;
            }
        }

        public bool IsBlob
        {
            get { return (Mode.ObjectType == ObjectType.Blob); }
        }

        public bool IsCommit
        {
            get { return (Mode.ObjectType == ObjectType.Commit); }
        }

        public bool IsModified
        {
            get { return (_id == null); }
        }

        public bool IsTag
        {
            get { return (Mode.ObjectType == ObjectType.Tag); }
        }

        public bool IsTree
        {
            get { return (Mode.ObjectType == ObjectType.Tree); }
        }

        public abstract FileMode Mode { get; }

        public string Name
        {
            get
            {
                if (NameUTF8 != null)
                    return RawParseUtils.decode(NameUTF8);
                return null;
            }
        }

        public byte[] NameUTF8 { get; private set; }

        public Tree Parent { get; private set; }

        public virtual Repository Repository
        {
            get { return Parent.Repository; }
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (this == obj)
            {
                return 0;
            }
            return CompareTo(obj as TreeEntry);
        }

        #endregion

        #region IComparable<TreeEntry> Members

        public int CompareTo(TreeEntry other)
        {
            if (other != null)
            {
                return Tree.CompareNames(NameUTF8, other.NameUTF8, LastChar(this), LastChar(other));
            }
            return -1;
        }

        #endregion

        public void Accept(TreeVisitor tv)
        {
            Accept(tv, 0);
        }

        public abstract void Accept(TreeVisitor tv, int flags);

        private void AppendFullName(StringBuilder r)
        {
            TreeEntry parent = Parent;
            string name = Name;
            if (parent != null)
            {
                parent.AppendFullName(r);
                if (r.Length > 0)
                {
                    r.Append('/');
                }
            }
            if (name != null)
            {
                r.Append(name);
            }
        }

        public void AttachParent(Tree p)
        {
            Parent = p;
        }

        public void Delete()
        {
            Parent.RemoveEntry(this);
            DetachParent();
        }

        public void DetachParent()
        {
            Parent = null;
        }

        public static int LastChar(GitIndex.Entry i)
        {
            return (FileMode.Tree.Equals(i.getModeBits()) ? 0x2f : 0);
        }

        public static int LastChar(TreeEntry treeEntry)
        {
            if (!(treeEntry is Tree))
            {
                return Convert.ToInt32('\0');
            }
            return Convert.ToInt32('/');
        }

        public void Rename(string n)
        {
            Rename(Constants.encode(n));
        }

        public void Rename(byte[] n)
        {
            Tree parent = Parent;
            if (parent != null)
            {
                Delete();
            }
            NameUTF8 = n;
            if (parent != null)
            {
                parent.AddEntry(this);
            }
        }

        public void SetModified()
        {
            Id = null;
        }
    }
}