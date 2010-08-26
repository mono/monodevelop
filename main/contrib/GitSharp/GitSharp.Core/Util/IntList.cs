/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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
using System.Text;

namespace GitSharp.Core.Util
{
    /// <summary>
    /// A more efficient <see cref="List{Int32}"/> using a primitive integer array.
    /// </summary>
    public class IntList
    {
        private int[] entries;

        private int count;

        /// <summary>
        /// Create an empty list with a default capacity.
        /// </summary>
        public IntList()
            : this(10)
        {
        }

        /// <summary>
        /// Create an empty list with the specified capacity.
        /// </summary>
        /// <param name="capacity">number of entries the list can initially hold.</param>
        public IntList(int capacity)
        {
            entries = new int[capacity];
        }

        /// <returns>
        /// Number of entries in this list
        /// </returns>
        public int size()
        {
            return count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i">index to Read, must be in the range [0, <see cref="size"/>).</param>
        /// <returns>the number at the specified index</returns>
        public int get(int i)
        {
            if (count <= i)
                throw new IndexOutOfRangeException();
            return entries[i];
        }

        /// <summary>
        /// Empty this list
        /// </summary>
        public void clear()
        {
            count = 0;
        }

        /// <summary>
        /// Add an entry to the end of the list.
        /// </summary>
        /// <param name="n">The nbumber to add</param>
        public void add(int n)
        {
            if (count == entries.Length)
                grow();
            entries[count++] = n;
        }

        /// <summary>
        /// Assign an entry in the list.
        /// </summary>
        /// <param name="index">index to set, must be in the range [0, <see cref="size()"/>).</param>
        /// <param name="n">value to store at the position.</param>
        public void set(int index, int n)
        {
            if (count < index)
                throw new ArgumentOutOfRangeException("index");

            if (count == index)
                add(n);
            else
                entries[index] = n;
        }

        /// <summary>
        /// Pad the list with entries.
        /// </summary>
        /// <param name="toIndex">index position to stop filling at. 0 inserts no filler. 1 ensures the list has a size of 1, adding <code>val</code> if the list is currently empty.</param>
        /// <param name="val">value to insert into padded positions.</param>
        public void fillTo(int toIndex, int val)
        {
            while (count < toIndex)
                add(val);
        }

        private void grow()
        {
            var n = new int[(entries.Length + 16) * 3 / 2];
            Array.Copy(entries, 0, n, 0, count);
            entries = n;
        }

        public string toString()
        {
            var r = new StringBuilder();
            r.Append('[');
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    r.Append(", ");
                r.Append(entries[i]);
            }
            r.Append(']');
            return r.ToString();
        }
    }
}