/*
 * Copyright (C) 2010, Dominique van de Vorle <dvdvorle@gmail.com>
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
using System.Linq;
using System.Text;

namespace GitSharp.Commands
{
    public class MailinfoCommand
        : AbstractCommand
    {

        public MailinfoCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Usually the program 'cleans up' the Subject: header line
        /// to extract the title line for the commit log message,
        /// among which (1) remove 'Re:' or 're:', (2) leading
        /// whitespaces, (3) '[' up to ']', typically '[PATCH]', and
        /// then prepends "[PATCH] ".  This flag forbids this
        /// munging, and is most useful when used to read back
        /// 'git-format-patch -k' output.
        /// 
        /// </summary>
        public bool K { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When -k is not in effect, all leading strings bracketed with '['
        /// and ']' pairs are stripped.  This option limits the stripping to
        /// only the pairs whose bracketed string contains the word "PATCH".
        /// 
        /// </summary>
        public bool B { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// The commit log message, author name and author email are
        /// taken from the e-mail, and after minimally decoding MIME
        /// transfer encoding, re-coded in UTF-8 by transliterating
        /// them.  This used to be optional but now it is the default.
        /// +
        /// Note that the patch is always used as-is without charset
        /// conversion, even with this flag.
        /// 
        /// </summary>
        public bool U { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Similar to -u but if the local convention is different
        /// from what is specified by i18n.commitencoding, this flag
        /// can be used to override it.
        /// 
        /// </summary>
        public string Encoding { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Disable all charset re-coding of the metadata.
        /// 
        /// </summary>
        public bool N { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Remove everything in body before a scissors line.  A line that
        /// mainly consists of scissors (either "&gt;8" or "8&lt;") and perforation
        /// (dash "-") marks is called a scissors line, and is used to request
        /// the reader to cut the message at that line.  If such a line
        /// appears in the body of the message before the patch, everything
        /// before it (including the scissors line itself) is ignored when
        /// this option is used.
        /// +
        /// This is useful if you want to begin your message in a discussion thread
        /// with comments and suggestions on the message you are responding to, and to
        /// conclude it with a patch submission, separating the discussion and the
        /// beginning of the proposed commit log message with a scissors line.
        /// +
        /// This can enabled by default with the configuration option mailinfo.scissors.
        /// 
        /// </summary>
        public bool Scissors { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Ignore scissors lines. Useful for overriding mailinfo.scissors settings.
        /// 
        /// </summary>
        public bool NoScissors { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
