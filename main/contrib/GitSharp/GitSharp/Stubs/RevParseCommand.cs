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
    public class RevParseCommand
        : AbstractCommand
    {

        public RevParseCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Use 'git-rev-parse' in option parsing mode (see PARSEOPT section below).
        /// 
        /// </summary>
        public bool Parseopt { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only meaningful in `--parseopt` mode. Tells the option parser to echo
        /// out the first `--` met instead of skipping it.
        /// 
        /// </summary>
        public bool KeepDashdash { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only meaningful in `--parseopt` mode.  Lets the option parser stop at
        /// the first non-option argument.  This can be used to parse sub-commands
        /// that take options themself.
        /// 
        /// </summary>
        public bool StopAtNonOption { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use 'git-rev-parse' in shell quoting mode (see SQ-QUOTE
        /// section below). In contrast to the `--sq` option below, this
        /// mode does only quoting. Nothing else is done to command input.
        /// 
        /// </summary>
        public bool SqQuote { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not output flags and parameters not meant for
        /// 'git-rev-list' command.
        /// 
        /// </summary>
        public bool RevsOnly { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not output flags and parameters meant for
        /// 'git-rev-list' command.
        /// 
        /// </summary>
        public bool NoRevs { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not output non-flag parameters.
        /// 
        /// </summary>
        public bool Flags { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not output flag parameters.
        /// 
        /// </summary>
        public bool NoFlags { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// If there is no parameter given by the user, use `&lt;arg&gt;`
        /// instead.
        /// 
        /// </summary>
        public string Default { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// The parameter given must be usable as a single, valid
        /// object name.  Otherwise barf and abort.
        /// 
        /// </summary>
        public bool Verify { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only meaningful in `--verify` mode. Do not output an error
        /// message if the first argument is not a valid object name;
        /// instead exit with non-zero status silently.
        /// 
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Usually the output is made one line per flag and
        /// parameter.  This option makes output a single line,
        /// properly quoted for consumption by shell.  Useful when
        /// you expect your parameter to contain whitespaces and
        /// newlines (e.g. when using pickaxe `-S` with
        /// 'git-diff-\*'). In contrast to the `--sq-quote` option,
        /// the command input is still interpreted as usual.
        /// 
        /// </summary>
        public bool Sq { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When showing object names, prefix them with '{caret}' and
        /// strip '{caret}' prefix from the object names that already have
        /// one.
        /// 
        /// </summary>
        public bool Not { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Usually the object names are output in SHA1 form (with
        /// possible '{caret}' prefix); this option makes them output in a
        /// form as close to the original input as possible.
        /// 
        /// </summary>
        public bool Symbolic { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This is similar to \--symbolic, but it omits input that
        /// are not refs (i.e. branch or tag names; or more
        /// explicitly disambiguating "heads/master" form, when you
        /// want to name the "master" branch when there is an
        /// unfortunately named tag "master"), and show them as full
        /// refnames (e.g. "refs/heads/master").
        /// 
        /// </summary>
        public bool SymbolicFullName { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// A non-ambiguous short name of the objects name.
        /// The option core.warnAmbiguousRefs is used to select the strict
        /// abbreviation mode.
        /// 
        /// </summary>
        public string AbbrevRef { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show all refs found in `$GIT_DIR/refs`.
        /// 
        /// </summary>
        public bool All { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show branch refs found in `$GIT_DIR/refs/heads`.
        /// 
        /// </summary>
        public bool Branches { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show tag refs found in `$GIT_DIR/refs/tags`.
        /// 
        /// </summary>
        public bool Tags { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show tag refs found in `$GIT_DIR/refs/remotes`.
        /// 
        /// </summary>
        public bool Remotes { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When the command is invoked from a subdirectory, show the
        /// path of the current directory relative to the top-level
        /// directory.
        /// 
        /// </summary>
        public bool ShowPrefix { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When the command is invoked from a subdirectory, show the
        /// path of the top-level directory relative to the current
        /// directory (typically a sequence of "../", or an empty string).
        /// 
        /// </summary>
        public bool ShowCdup { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show `$GIT_DIR` if defined else show the path to the .git directory.
        /// 
        /// </summary>
        public bool GitDir { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When the current working directory is below the repository
        /// directory print "true", otherwise "false".
        /// 
        /// </summary>
        public bool IsInsideGitDir { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When the current working directory is inside the work tree of the
        /// repository print "true", otherwise "false".
        /// 
        /// </summary>
        public bool IsInsideWorkTree { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When the repository is bare print "true", otherwise "false".
        /// 
        /// </summary>
        public bool IsBareRepository { get; set; }

        ///// <summary>
        ///// Not implemented
        ///// 
        ///// Instead of outputting the full SHA1 values of object names try to
        ///// abbreviate them to a shorter unique name. When no length is specified
        ///// 7 is used. The minimum length is 4.
        ///// 
        ///// </summary>
        //public bool Short { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of outputting the full SHA1 values of object names try to
        /// abbreviate them to a shorter unique name. When no length is specified
        /// 7 is used. The minimum length is 4.
        /// 
        /// </summary>
        public string Short { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Parse the date string, and output the corresponding
        /// --max-age= parameter for 'git-rev-list'.
        /// 
        /// </summary>
        public string Since { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Parse the date string, and output the corresponding
        /// --max-age= parameter for 'git-rev-list'.
        /// 
        /// </summary>
        public string After { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Parse the date string, and output the corresponding
        /// --min-age= parameter for 'git-rev-list'.
        /// 
        /// </summary>
        public string Until { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Parse the date string, and output the corresponding
        /// --min-age= parameter for 'git-rev-list'.
        /// 
        /// </summary>
        public string Before { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
