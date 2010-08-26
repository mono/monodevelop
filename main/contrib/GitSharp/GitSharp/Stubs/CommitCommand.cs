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
    public class CommitCommand
        : AbstractCommand
    {

        public CommitCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Tell the command to automatically stage files that have
        /// been modified and deleted, but new files you have not
        /// told git about are not affected.
        /// 
        /// </summary>
        public bool All { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Take an existing commit object, and reuse the log message
        /// and the authorship information (including the timestamp)
        /// when creating the commit.
        /// 
        /// </summary>
        public string ReuseMessage { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Like '-C', but with '-c' the editor is invoked, so that
        /// the user can further edit the commit message.
        /// 
        /// </summary>
        public string ReeditMessage { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When used with -C/-c/--amend options, declare that the
        /// authorship of the resulting commit now belongs of the committer.
        /// This also renews the author timestamp.
        /// 
        /// </summary>
        public bool ResetAuthor { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Take the commit message from the given file.  Use '-' to
        /// read the message from the standard input.
        /// 
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Override the author name used in the commit.  You can use the
        /// standard `A U Thor &lt;author@example.com&gt;` format.  Otherwise,
        /// an existing commit that matches the given string and its author
        /// name is used.
        /// 
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use the given &lt;msg&gt; as the commit message.
        /// 
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use the contents of the given file as the initial version
        /// of the commit message. The editor is invoked and you can
        /// make subsequent changes. If a message is specified using
        /// the `-m` or `-F` options, this option has no effect. This
        /// overrides the `commit.template` configuration variable.
        /// 
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Add Signed-off-by line by the committer at the end of the commit
        /// log message.
        /// 
        /// </summary>
        public bool Signoff { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This option bypasses the pre-commit and commit-msg hooks.
        /// See also linkgit:githooks[5].
        /// 
        /// </summary>
        public bool NoVerify { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Usually recording a commit that has the exact same tree as its
        /// sole parent commit is a mistake, and the command prevents you
        /// from making such a commit.  This option bypasses the safety, and
        /// is primarily for use by foreign scm interface scripts.
        /// 
        /// </summary>
        public bool AllowEmpty { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This option sets how the commit message is cleaned up.
        /// The  '&lt;mode&gt;' can be one of 'verbatim', 'whitespace', 'strip',
        /// and 'default'. The 'default' mode will strip leading and
        /// trailing empty lines and #commentary from the commit message
        /// only if the message is to be edited. Otherwise only whitespace
        /// removed. The 'verbatim' mode does not change message at all,
        /// 'whitespace' removes just leading/trailing whitespace lines
        /// and 'strip' removes both whitespace and commentary.
        /// 
        /// </summary>
        public string Cleanup { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// The message taken from file with `-F`, command line with
        /// `-m`, and from file with `-C` are usually used as the
        /// commit log message unmodified.  This option lets you
        /// further edit the message taken from these sources.
        /// 
        /// </summary>
        public bool Edit { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Used to amend the tip of the current branch. Prepare the tree
        /// object you would want to replace the latest commit as usual
        /// (this includes the usual -i/-o and explicit paths), and the
        /// commit log editor is seeded with the commit message from the
        /// tip of the current branch. The commit you create replaces the
        /// current tip -- if it was a merge, it will have the parents of
        /// the current tip as parents -- so the current top commit is
        /// discarded.
        /// +
        /// </summary>
        public bool Amend { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
