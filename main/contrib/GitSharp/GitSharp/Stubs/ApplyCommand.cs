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
    public class ApplyCommand
        : AbstractCommand
    {

        public ApplyCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Instead of applying the patch, output diffstat for the
        /// input.  Turns off "apply".
        /// 
        /// </summary>
        public bool Stat { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Similar to `--stat`, but shows the number of added and
        /// deleted lines in decimal notation and the pathname without
        /// abbreviation, to make it more machine friendly.  For
        /// binary files, outputs two `-` instead of saying
        /// `0 0`.  Turns off "apply".
        /// 
        /// </summary>
        public bool Numstat { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of applying the patch, output a condensed
        /// summary of information obtained from git diff extended
        /// headers, such as creations, renames and mode changes.
        /// Turns off "apply".
        /// 
        /// </summary>
        public bool Summary { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of applying the patch, see if the patch is
        /// applicable to the current working tree and/or the index
        /// file and detects errors.  Turns off "apply".
        /// 
        /// </summary>
        public bool Check { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When `--check` is in effect, or when applying the patch
        /// (which is the default when none of the options that
        /// disables it is in effect), make sure the patch is
        /// applicable to what the current index file records.  If
        /// the file to be patched in the working tree is not
        /// up-to-date, it is flagged as an error.  This flag also
        /// causes the index file to be updated.
        /// 
        /// </summary>
        public bool Index { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Apply a patch without touching the working tree. Instead take the
        /// cached data, apply the patch, and store the result in the index
        /// without using the working tree. This implies `--index`.
        /// 
        /// </summary>
        public bool Cached { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Newer 'git-diff' output has embedded 'index information'
        /// for each blob to help identify the original version that
        /// the patch applies to.  When this flag is given, and if
        /// the original versions of the blobs are available locally,
        /// builds a temporary index containing those blobs.
        /// +
        /// When a pure mode change is encountered (which has no index information),
        /// the information is read from the current index instead.
        /// 
        /// </summary>
        public string BuildFakeAncestor { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Apply the patch in reverse.
        /// 
        /// </summary>
        public bool Reverse { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// For atomicity, 'git-apply' by default fails the whole patch and
        /// does not touch the working tree when some of the hunks
        /// do not apply.  This option makes it apply
        /// the parts of the patch that are applicable, and leave the
        /// rejected hunks in corresponding *.rej files.
        /// 
        /// </summary>
        public bool Reject { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When `--numstat` has been given, do not munge pathnames,
        /// but use a NUL-terminated machine-readable format.
        /// +
        /// Without this option, each pathname output will have TAB, LF, double quotes,
        /// and backslash characters replaced with `\t`, `\n`, `\"`, and `\\`,
        /// respectively, and the pathname will be enclosed in double quotes if
        /// any of those replacements occurred.
        /// 
        /// </summary>
        public bool Z { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Remove &lt;n&gt; leading slashes from traditional diff paths. The
        /// default is 1.
        /// 
        /// </summary>
        public string P { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Ensure at least &lt;n&gt; lines of surrounding context match before
        /// and after each change. When fewer lines of surrounding
        /// context exist they all must match.  By default no context is
        /// ever ignored.
        /// 
        /// </summary>
        public string C { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// By default, 'git-apply' expects that the patch being
        /// applied is a unified diff with at least one line of context.
        /// This provides good safety measures, but breaks down when
        /// applying a diff generated with `--unified=0`. To bypass these
        /// checks use `--unidiff-zero`.
        /// +
        /// Note, for the reasons stated above usage of context-free patches is
        /// discouraged.
        /// 
        /// </summary>
        public bool UnidiffZero { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// If you use any of the options marked "Turns off
        /// 'apply'" above, 'git-apply' reads and outputs the
        /// requested information without actually applying the
        /// patch.  Give this flag after those flags to also apply
        /// the patch.
        /// 
        /// </summary>
        public bool Apply { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When applying a patch, ignore additions made by the
        /// patch.  This can be used to extract the common part between
        /// two files by first running 'diff' on them and applying
        /// the result with this option, which would apply the
        /// deletion part but not the addition part.
        /// 
        /// </summary>
        public bool NoAdd { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Historically we did not allow binary patch applied
        /// without an explicit permission from the user, and this
        /// flag was the way to do so.  Currently we always allow binary
        /// patch application, so this is a no-op.
        /// 
        /// </summary>
        public bool AllowBinaryReplacement { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Historically we did not allow binary patch applied
        /// without an explicit permission from the user, and this
        /// flag was the way to do so.  Currently we always allow binary
        /// patch application, so this is a no-op.
        /// 
        /// </summary>
        public bool Binary { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Don't apply changes to files matching the given path pattern. This can
        /// be useful when importing patchsets, where you want to exclude certain
        /// files or directories.
        /// 
        /// </summary>
        public string Exclude { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Apply changes to files matching the given path pattern. This can
        /// be useful when importing patchsets, where you want to include certain
        /// files or directories.
        /// +
        /// When `--exclude` and `--include` patterns are used, they are examined in the
        /// order they appear on the command line, and the first match determines if a
        /// patch to each path is used.  A patch to a path that does not match any
        /// on the command line, and ignored if there is any include pattern.
        /// 
        /// </summary>
        public string Include { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When applying a patch, ignore changes in whitespace in context
        /// lines if necessary.
        /// Context lines will preserve their whitespace, and they will not
        /// undergo whitespace fixing regardless of the value of the
        /// `--whitespace` option. New lines will still be fixed, though.
        /// 
        /// </summary>
        public bool IgnoreSpaceChange { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When applying a patch, ignore changes in whitespace in context
        /// lines if necessary.
        /// Context lines will preserve their whitespace, and they will not
        /// undergo whitespace fixing regardless of the value of the
        /// `--whitespace` option. New lines will still be fixed, though.
        /// 
        /// </summary>
        public bool IgnoreWhitespace { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When applying a patch, detect a new or modified line that has
        /// whitespace errors.  What are considered whitespace errors is
        /// controlled by `core.whitespace` configuration.  By default,
        /// trailing whitespaces (including lines that solely consist of
        /// whitespaces) and a space character that is immediately followed
        /// by a tab character inside the initial indent of the line are
        /// considered whitespace errors.
        /// +
        /// By default, the command outputs warning messages but applies the patch.
        /// When `git-apply` is used for statistics and not applying a
        /// patch, it defaults to `nowarn`.
        /// +
        /// You can use different `&lt;action&gt;` values to control this
        /// behavior:
        /// +
        /// * `nowarn` turns off the trailing whitespace warning.
        /// * `warn` outputs warnings for a few such errors, but applies the
        /// </summary>
        public string Whitespace { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
