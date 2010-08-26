/*
 * Copyright (C) 2009, Robin Rosenberg
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
using GitSharp.Core.Util;

namespace GitSharp.Core
{
	/// <summary>
	/// Utility for reading reflog entries.
	/// </summary>
    public class ReflogReader
    {
		#region Nested Types

        public class Entry
        {
			private readonly ObjectId oldId;
			private readonly ObjectId newId;
			private readonly PersonIdent who;
			private readonly string comment;

            public Entry(byte[] raw, int pos)
            {
                oldId = ObjectId.FromString(raw, pos);
                pos += Constants.OBJECT_ID_STRING_LENGTH;
                if (raw[pos++] != ' ')
                    throw new ArgumentException("Raw log message does not parse as log entry");
                newId = ObjectId.FromString(raw, pos);
                pos += Constants.OBJECT_ID_STRING_LENGTH;
                if (raw[pos++] != ' ')
                    throw new ArgumentException("Raw log message does not parse as log entry");
                who = RawParseUtils.parsePersonIdentOnly(raw, pos);
                int p0 = RawParseUtils.next(raw, pos, (byte)'\t');

                if (p0 == -1)
                    throw new ArgumentException("Raw log message does not parse as log entry");

                int p1 = RawParseUtils.nextLF(raw, p0);
                if (p1 == -1)
                    throw new ArgumentException("Raw log message does not parse as log entry");

                comment = RawParseUtils.decode(raw, p0, p1 - 1);
            }

			/// <summary>
			/// Gets the commit id before the change.
			/// </summary>
            public ObjectId getOldId()
            {
                return oldId;
            }

			/// <summary>
			/// Gets the commit id after the change.
			/// </summary>
            public ObjectId getNewId()
            {
                return newId;
            }

			/// <summary>
			/// Gets the user performing the change.
			/// </summary>
            public PersonIdent getWho()
            {
                return who;
            }

			/// <summary>
			/// Gets the textual description of the change.
			/// </summary>
            public string getComment()
            {
                return comment;
            }

            public override string ToString()
            {
                return "Entry[" + oldId.Name + ", " + newId.Name + ", " + getWho() + ", " + getComment() + "]";
            }
        }

		#endregion

		private readonly FileInfo _logName;

		///	<summary>
		/// Parsed reflog entry.
		/// </summary>
		public ReflogReader(Repository db, string refName)
		{
			if (db == null)
				throw new ArgumentNullException ("db");
			_logName = new FileInfo(
				Path.Combine(
					db.Directory.FullName,
						Path.Combine("logs", refName)).Replace('/', Path.DirectorySeparatorChar));
        }

		///	<summary>
		/// Get the last entry in the reflog.
		/// </summary>
		/// <returns>The latest reflog entry, or null if no log.</returns>
		/// <exception cref="IOException"></exception>
        public Entry getLastEntry()
        {
            var entries = getReverseEntries(1);
            return entries.Count > 0 ? entries[0] : null;
        }

		/// <summary></summary>
		/// <returns> all reflog entries in reverse order.
		/// </returns>
		/// <exception cref="IOException"></exception>
		public IList<Entry> getReverseEntries()
		{
			return getReverseEntries(int.MaxValue);
		}

		///	<param name="max">Max number of entries to read.</param>
		///	<returns>All reflog entries in reverse order.</returns>
		///	<exception cref="IOException"></exception>
		public IList<Entry> getReverseEntries(int max)
        {
			byte[] log;

			try
			{
				log = IO.ReadFully(_logName);
			}
			catch (DirectoryNotFoundException)
			{
				return new List<Entry>();
			}
			catch (FileNotFoundException)
			{
				return new List<Entry>();
			}
            
            int rs = RawParseUtils.prevLF(log, log.Length);
            var ret = new List<Entry>();
            while (rs >= 0 && max-- > 0)
            {
                rs = RawParseUtils.prevLF(log, rs);
                Entry entry = new Entry(log, rs < 0 ? 0 : rs + 2);
                ret.Add(entry);
            }

            return ret;
        }
    }
}