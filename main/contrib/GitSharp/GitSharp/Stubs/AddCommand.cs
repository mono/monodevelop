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
using GitSharp.Core.FnMatch;
using GitSharp.Core;

namespace GitSharp.Commands
{
    public class AddCommand
        : AbstractCommand
    {

        public AddCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        ///         Don't actually add the file(s), just show if they exist.
        /// 
        /// </summary>
        public bool DryRun { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        ///         Be verbose.
        /// 
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Allow adding otherwise ignored files.
        /// 
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Add modified contents in the working tree interactively to
        /// the index. Optional path arguments may be supplied to limit
        /// operation to a subset of the working tree. See ``Interactive
        /// mode'' for details.
        /// 
        /// </summary>
        public bool Interactive { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Interactively choose hunks of patch between the index and the
        /// work tree and add them to the index. This gives the user a chance
        /// to review the difference before adding modified contents to the
        /// index.
        /// +
        /// This effectively runs `add --interactive`, but bypasses the
        /// initial command menu and directly jumps to the `patch` subcommand.
        /// See ``Interactive mode'' for details.
        /// 
        /// </summary>
        public bool Patch { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Open the diff vs. the index in an editor and let the user
        /// edit it.  After the editor was closed, adjust the hunk headers
        /// and apply the patch to the index.
        /// +
        /// *NOTE*: Obviously, if you change anything else than the first character
        /// on lines beginning with a space or a minus, the patch will no longer
        /// apply.
        /// 
        /// </summary>
        public bool E { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Update only files that git already knows about, staging modified
        /// content for commit and marking deleted files for removal. This
        /// is similar
        /// to what "git commit -a" does in preparation for making a commit,
        /// except that the update is limited to paths specified on the
        /// command line. If no paths are specified, all tracked files in the
        /// current directory and its subdirectories are updated.
        /// 
        /// </summary>
        public bool Update { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Update files that git already knows about (same as '\--update')
        /// and add all untracked files that are not ignored by '.gitignore'
        /// mechanism.
        /// 
        /// </summary>
        public bool All { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Record only the fact that the path will be added later. An entry
        /// for the path is placed in the index with no content. This is
        /// useful for, among other things, showing the unstaged content of
        /// such files with 'git diff' and committing them with 'git commit
        /// -a'.
        /// 
        /// </summary>
        public bool IntentToAdd { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Don't add the file(s), but only refresh their stat()
        /// information in the index.
        /// 
        /// </summary>
        public bool Refresh { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// If some files could not be added because of errors indexing
        /// them, do not abort the operation, but continue adding the
        /// others. The command shall still exit with non-zero status.
        /// 
        /// </summary>
        public bool IgnoreErrors { get; set; }

        #endregion

        public override void Execute()
        {
            foreach (string arg in Arguments)
            {   
                //Todo: Add FileNameMatcher support. To be added when fnmatch is completed.
                //For now, pattern matching is not allowed. Please specify the files only.               
                
                //Gain access to the Git index using the repository determined before command execution
                Index index = new Index(Repository);
                
                //Use full paths only to eliminate platform-based directory differences
                string path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), arg));

                //Perform the validity tests outside of the index to handle the error messages
                if ((new FileInfo(path).Exists) || (new DirectoryInfo(path).Exists))
                    index.Add(path);
                else
                    OutputStream.WriteLine(path + " does not exist.");
            }
        }
    }
}
