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
    public class AmCommand
        : AbstractCommand
    {

        public AmCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

         #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Add a `Signed-off-by:` line to the commit message, using
        /// the committer identity of yourself.
        /// 
        /// </summary>
        public bool Signoff { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Pass `-k` flag to 'git-mailinfo' (see linkgit:git-mailinfo[1]).
        /// 
        /// </summary>
        public bool Keep { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Remove everything in body before a scissors line (see
        /// linkgit:git-mailinfo[1]).
        /// 
        /// </summary>
        public bool Scissors { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Ignore scissors lines (see linkgit:git-mailinfo[1]).
        /// 
        /// </summary>
        public bool NoScissors { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Be quiet. Only print error messages.
        /// 
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Pass `-u` flag to 'git-mailinfo' (see linkgit:git-mailinfo[1]).
        /// The proposed commit log message taken from the e-mail
        /// is re-coded into UTF-8 encoding (configuration variable
        /// `i18n.commitencoding` can be used to specify project's
        /// preferred encoding if it is not UTF-8).
        /// +
        /// This was optional in prior versions of git, but now it is the
        /// default.   You can use `--no-utf8` to override this.
        /// 
        /// </summary>
        public bool Utf8 { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Pass `-n` flag to 'git-mailinfo' (see
        /// linkgit:git-mailinfo[1]).
        /// 
        /// </summary>
        public bool NoUtf8 { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When the patch does not apply cleanly, fall back on
        /// 3-way merge if the patch records the identity of blobs
        /// it is supposed to apply to and we have those blobs
        /// available locally.
        /// 
        /// </summary>
        public bool ThreeWay { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// These flags are passed to the 'git-apply' (see linkgit:git-apply[1])
        /// program that applies
        /// the patch.
        /// 
        /// </summary>
        public bool IgnoreSpaceChange { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// These flags are passed to the 'git-apply' (see linkgit:git-apply[1])
        /// program that applies
        /// the patch.
        /// 
        /// </summary>
        public string IgnoreWhitespace { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// These flags are passed to the 'git-apply' (see linkgit:git-apply[1])
        /// program that applies
        /// the patch.
        /// 
        /// </summary>
        public string Whitespace { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// These flags are passed to the 'git-apply' (see linkgit:git-apply[1])
        /// program that applies
        /// the patch.
        /// 
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// These flags are passed to the 'git-apply' (see linkgit:git-apply[1])
        /// program that applies
        /// the patch.
        /// 
        /// </summary>
        public bool Reject { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Run interactively.
        /// 
        /// </summary>
        public bool Interactive { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// By default the command records the date from the e-mail
        /// message as the commit author date, and uses the time of
        /// commit creation as the committer date. This allows the
        /// user to lie about the committer date by using the same
        /// value as the author date.
        /// 
        /// </summary>
        public bool CommitterDateIsAuthorDate { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// By default the command records the date from the e-mail
        /// message as the commit author date, and uses the time of
        /// commit creation as the committer date. This allows the
        /// user to lie about the author date by using the same
        /// value as the committer date.
        /// 
        /// </summary>
        public bool IgnoreDate { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Skip the current patch.  This is only meaningful when
        /// restarting an aborted patch.
        /// 
        /// </summary>
        public bool Skip { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// After a patch failure (e.g. attempting to apply
        /// conflicting patch), the user has applied it by hand and
        /// the index file stores the result of the application.
        /// Make a commit using the authorship and commit log
        /// extracted from the e-mail message and the current index
        /// file, and continue.
        /// 
        /// </summary>
        public bool Resolved { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When a patch failure occurs, &lt;msg&gt; will be printed
        /// to the screen before exiting.  This overrides the
        /// standard message informing you to use `--resolved`
        /// or `--skip` to handle the failure.  This is solely
        /// for internal use between 'git-rebase' and 'git-am'.
        /// 
        /// </summary>
        public string Resolvemsg { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Restore the original branch and abort the patching operation.
        /// </summary>
        public bool Abort { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
