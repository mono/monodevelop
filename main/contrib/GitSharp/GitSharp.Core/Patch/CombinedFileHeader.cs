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

using System.Collections.Generic;
using System.Text;
using GitSharp.Core.Util;

namespace GitSharp.Core.Patch
{
    /**
     * A file in the Git "diff --cc" or "diff --combined" format.
     * <para />
     * A combined diff shows an n-way comparison between two or more ancestors and
     * the final revision. Its primary function is to perform code reviews on a
     * merge which introduces changes not in any ancestor.
     */
    public class CombinedFileHeader : FileHeader
    {
        private static readonly byte[] Mode = Constants.encodeASCII("mode ");

        private AbbreviatedObjectId[] _oldIds;
        private FileMode[] _oldModes;

        public CombinedFileHeader(byte[] b, int offset)
            : base(b, offset)
        {
        }

        /// <summary>
		/// Number of ancestor revisions mentioned in this diff.
        /// </summary>
    	public override int ParentCount
    	{
    		get { return _oldIds.Length; }
    	}

    	/// <summary>
    	/// Get the file mode of the first parent.
    	///  </summary>
    	public override FileMode GetOldMode()
    	{
    		return getOldMode(0);
    	}

    	/**
         * Get the file mode of the nth ancestor
         *
         * @param nthParent
         *            the ancestor to get the mode of
         * @return the mode of the requested ancestor.
         */
        public FileMode getOldMode(int nthParent)
        {
            return _oldModes[nthParent];
        }

        /** @return get the object id of the first parent. */
        public override AbbreviatedObjectId getOldId()
        {
            return getOldId(0);
        }

        /**
         * Get the ObjectId of the nth ancestor
         *
         * @param nthParent
         *            the ancestor to get the object id of
         * @return the id of the requested ancestor.
         */
        public AbbreviatedObjectId getOldId(int nthParent)
        {
            return _oldIds[nthParent];
        }

        public override string getScriptText(Encoding oldCharset, Encoding newCharset)
        {
            var cs = new Encoding[ParentCount + 1];
            for (int i = 0; i < cs.Length; i++)
            {
            	cs[i] = oldCharset;
            }
            cs[ParentCount] = newCharset;
            return getScriptText(cs);
        }
        
        public override int parseGitHeaders(int ptr, int end)
        {
            while (ptr < end)
            {
                int eol = RawParseUtils.nextLF(Buffer, ptr);
				if (isHunkHdr(Buffer, ptr, end) >= 1)
                {
                    // First hunk header; break out and parse them later.
                    break;
                }

				if (RawParseUtils.match(Buffer, ptr, OLD_NAME) >= 0)
                {
                    ParseOldName(ptr, eol);

                }
				else if (RawParseUtils.match(Buffer, ptr, NEW_NAME) >= 0)
                {
                    ParseNewName(ptr, eol);

                }
				else if (RawParseUtils.match(Buffer, ptr, Index) >= 0)
                {
                    ParseIndexLine(ptr + Index.Length, eol);

                }
				else if (RawParseUtils.match(Buffer, ptr, Mode) >= 0)
                {
                    parseModeLine(ptr + Mode.Length, eol);

                }
				else if (RawParseUtils.match(Buffer, ptr, NewFileMode) >= 0)
                {
                    ParseNewFileMode(ptr, eol);

                }
				else if (RawParseUtils.match(Buffer, ptr, DeletedFileMode) >= 0)
                {
                    parseDeletedFileMode(ptr + DeletedFileMode.Length, eol);

                }
                else
                {
                    // Probably an empty patch (stat dirty).
                    break;
                }

                ptr = eol;
            }
            return ptr;
        }

    	protected override void ParseIndexLine(int ptr, int end)
        {
            // "index $asha1,$bsha1..$csha1"
            //
            var ids = new List<AbbreviatedObjectId>();
            while (ptr < end)
            {
				int comma = RawParseUtils.nextLF(Buffer, ptr, (byte)',');
                if (end <= comma) break;
				ids.Add(AbbreviatedObjectId.FromString(Buffer, ptr, comma - 1));
                ptr = comma;
            }

            _oldIds = new AbbreviatedObjectId[ids.Count + 1];
            ids.CopyTo(_oldIds);
			int dot2 = RawParseUtils.nextLF(Buffer, ptr, (byte)'.');
			_oldIds[_oldIds.Length - 1] = AbbreviatedObjectId.FromString(Buffer, ptr, dot2 - 1);
			NewId = AbbreviatedObjectId.FromString(Buffer, dot2 + 1, end - 1);
            _oldModes = new FileMode[_oldIds.Length];
        }

    	protected override void ParseNewFileMode(int ptr, int eol)
        {
            for (int i = 0; i < _oldModes.Length; i++)
            {
            	_oldModes[i] = FileMode.Missing;
            }
            base.ParseNewFileMode(ptr, eol);
        }

        public override HunkHeader newHunkHeader(int offset)
        {
            return new CombinedHunkHeader(this, offset);
        }

        private void parseModeLine(int ptr, int eol)
        {
            // "mode $amode,$bmode..$cmode"
            //
            int n = 0;
            while (ptr < eol)
            {
				int comma = RawParseUtils.nextLF(Buffer, ptr, (byte)',');
                if (eol <= comma)
                    break;
                _oldModes[n++] = ParseFileMode(ptr, comma);
                ptr = comma;
            }

			int dot2 = RawParseUtils.nextLF(Buffer, ptr, (byte)'.');
            _oldModes[n] = ParseFileMode(ptr, dot2);
            NewMode = ParseFileMode(dot2 + 1, eol);
        }

        private void parseDeletedFileMode(int ptr, int eol)
        {
            // "deleted file mode $amode,$bmode"
            //
            ChangeType = ChangeTypeEnum.DELETE;
            int n = 0;
            while (ptr < eol)
            {
				int comma = RawParseUtils.nextLF(Buffer, ptr, (byte)',');
                if (eol <= comma)
                    break;
                _oldModes[n++] = ParseFileMode(ptr, comma);
                ptr = comma;
            }

            _oldModes[n] = ParseFileMode(ptr, eol);
            NewMode = FileMode.Missing;
        }
    }
}