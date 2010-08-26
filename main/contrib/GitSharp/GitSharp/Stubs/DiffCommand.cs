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
    public class DiffCommand
        : AbstractCommand
    {

        public DiffCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// ifdef::git-format-patch[]
        /// Generate plain patches without any diffstats.
        /// endif::git-format-patch[]
        /// 
        /// </summary>
        public bool NoStat { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ifndef::git-format-patch[]
        /// Generate patch (see section on generating patches).
        /// {git-diff? This is the default.}
        /// endif::git-format-patch[]
        /// 
        /// </summary>
        public bool P { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ifndef::git-format-patch[]
        /// Generate patch (see section on generating patches).
        /// {git-diff? This is the default.}
        /// endif::git-format-patch[]
        /// 
        /// </summary>
        public bool U { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Generate diffs with &lt;n&gt; lines of context instead of
        /// the usual three.
        /// ifndef::git-format-patch[]
        /// Implies `-p`.
        /// endif::git-format-patch[]
        /// 
        /// </summary>
        public string Unified { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ifndef::git-format-patch[]
        /// Generate the raw format.
        /// {git-diff-core? This is the default.}
        /// endif::git-format-patch[]
        /// 
        /// </summary>
        public bool Raw { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ifndef::git-format-patch[]
        /// Synonym for `-p --raw`.
        /// endif::git-format-patch[]
        /// 
        /// </summary>
        public bool PatchWithRaw { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Generate a diff using the "patience diff" algorithm.
        /// 
        /// </summary>
        public bool Patience { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Generate a diffstat.  You can override the default
        /// output width for 80-column terminal by `--stat=width`.
        /// The width of the filename part can be controlled by
        /// giving another width to it separated by a comma.
        /// 
        /// </summary>
        public string Stat { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Similar to `\--stat`, but shows number of added and
        /// deleted lines in decimal notation and pathname without
        /// abbreviation, to make it more machine friendly.  For
        /// binary files, outputs two `-` instead of saying
        /// `0 0`.
        /// 
        /// </summary>
        public bool Numstat { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Output only the last line of the `--stat` format containing total
        /// number of modified files, as well as number of added and deleted
        /// lines.
        /// 
        /// </summary>
        public bool Shortstat { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Output the distribution of relative amount of changes (number of lines added or
        /// removed) for each sub-directory. Directories with changes below
        /// a cut-off percent (3% by default) are not shown. The cut-off percent
        /// can be set with `--dirstat=limit`. Changes in a child directory is not
        /// counted for the parent directory, unless `--cumulative` is used.
        /// 
        /// </summary>
        public string Dirstat { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Same as `--dirstat`, but counts changed files instead of lines.
        /// 
        /// </summary>
        public string DirstatByFile { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Output a condensed summary of extended header information
        /// such as creations, renames and mode changes.
        /// 
        /// </summary>
        public bool Summary { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ifndef::git-format-patch[]
        /// Synonym for `-p --stat`.
        /// endif::git-format-patch[]
        /// 
        /// </summary>
        public bool PatchWithStat { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ifdef::git-log[]
        /// Separate the commits with NULs instead of with new newlines.
        /// +
        /// Also, when `--raw` or `--numstat` has been given, do not munge
        /// pathnames and use NULs as output field terminators.
        /// endif::git-log[]
        /// ifndef::git-log[]
        /// When `--raw` or `--numstat` has been given, do not munge
        /// pathnames and use NULs as output field terminators.
        /// endif::git-log[]
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
        /// Show only names of changed files.
        /// 
        /// </summary>
        public bool NameOnly { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show only names and status of changed files. See the description
        /// of the `--diff-filter` option on what the status letters mean.
        /// 
        /// </summary>
        public bool NameStatus { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Chose the output format for submodule differences. &lt;format&gt; can be one of
        /// 'short' and 'log'. 'short' just shows pairs of commit names, this format
        /// is used when this option is not given. 'log' is the default value for this
        /// option and lists the commits in that commit range like the 'summary'
        /// option of linkgit:git-submodule[1] does.
        /// 
        /// </summary>
        public string Submodule { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show colored diff.
        /// 
        /// </summary>
        public bool Color { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Turn off colored diff, even when the configuration file
        /// gives the default to color output.
        /// 
        /// </summary>
        public bool NoColor { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show colored word diff, i.e., color words which have changed.
        /// By default, words are separated by whitespace.
        /// +
        /// When a &lt;regex&gt; is specified, every non-overlapping match of the
        /// considered whitespace and ignored(!) for the purposes of finding
        /// differences.  You may want to append `|[^[:space:]]` to your regular
        /// expression to make sure that it matches all non-whitespace characters.
        /// A match that contains a newline is silently truncated(!) at the
        /// newline.
        /// +
        /// The regex can also be set via a diff driver or configuration option, see
        /// linkgit:gitattributes[1] or linkgit:git-config[1].  Giving it explicitly
        /// overrides any diff driver or configuration setting.  Diff drivers
        /// override configuration settings.
        /// endif::git-format-patch[]
        /// 
        /// </summary>
        public string ColorWords { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Turn off rename detection, even when the configuration
        /// file gives the default to do so.
        /// 
        /// </summary>
        public bool NoRenames { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ifndef::git-format-patch[]
        /// Warn if changes introduce trailing whitespace
        /// or an indent that uses a space before a tab. Exits with
        /// non-zero status if problems are found. Not compatible with
        /// --exit-code.
        /// endif::git-format-patch[]
        /// 
        /// </summary>
        public bool Check { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of the first handful of characters, show the full
        /// pre- and post-image blob object names on the "index"
        /// line when generating patch format output.
        /// 
        /// </summary>
        public bool FullIndex { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// In addition to `--full-index`, output a binary diff that
        /// can be applied with `git-apply`.
        /// 
        /// </summary>
        public bool Binary { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of showing the full 40-byte hexadecimal object
        /// name in diff-raw format output and diff-tree header
        /// lines, show only a partial prefix.  This is
        /// independent of the `--full-index` option above, which controls
        /// the diff-patch output format.  Non default number of
        /// digits can be specified with `--abbrev=&lt;n&gt;`.
        /// 
        /// </summary>
        public string Abbrev { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Break complete rewrite changes into pairs of delete and create.
        /// 
        /// </summary>
        public bool B { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Detect renames.
        /// 
        /// </summary>
        public bool M { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Detect copies as well as renames.  See also `--find-copies-harder`.
        /// 
        /// </summary>
        public bool C { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ifndef::git-format-patch[]
        /// Select only files that are Added (`A`), Copied (`C`),
        /// Deleted (`D`), Modified (`M`), Renamed (`R`), have their
        /// type (i.e. regular file, symlink, submodule, ...) changed (`T`),
        /// are Unmerged (`U`), are
        /// Unknown (`X`), or have had their pairing Broken (`B`).
        /// Any combination of the filter characters may be used.
        /// When `*` (All-or-none) is added to the combination, all
        /// paths are selected if there is any file that matches
        /// other criteria in the comparison; if there is no file
        /// that matches other criteria, nothing is selected.
        /// endif::git-format-patch[]
        /// 
        /// </summary>
        public string DiffFilter { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// For performance reasons, by default, `-C` option finds copies only
        /// if the original file of the copy was modified in the same
        /// changeset.  This flag makes the command
        /// inspect unmodified files as candidates for the source of
        /// copy.  This is a very expensive operation for large
        /// projects, so use it with caution.  Giving more than one
        /// `-C` option has the same effect.
        /// 
        /// </summary>
        public bool FindCopiesHarder { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// The `-M` and `-C` options require O(n^2) processing time where n
        /// is the number of potential rename/copy targets.  This
        /// option prevents rename/copy detection from running if
        /// the number of rename/copy targets exceeds the specified
        /// number.
        /// 
        /// </summary>
        public string L { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ifndef::git-format-patch[]
        /// Look for differences that introduce or remove an instance of
        /// &lt;string&gt;. Note that this is different than the string simply
        /// appearing in diff output; see the 'pickaxe' entry in
        /// linkgit:gitdiffcore[7] for more details.
        /// 
        /// </summary>
        public string S { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When `-S` finds a change, show all the changes in that
        /// changeset, not just the files that contain the change
        /// in &lt;string&gt;.
        /// 
        /// </summary>
        public bool PickaxeAll { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Make the &lt;string&gt; not a plain string but an extended POSIX
        /// regex to match.
        /// endif::git-format-patch[]
        /// 
        /// </summary>
        public string PickaxeRegex { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Output the patch in the order specified in the
        /// &lt;orderfile&gt;, which has one shell glob pattern per line.
        /// 
        /// </summary>
        public string O { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ifndef::git-format-patch[]
        /// Swap two inputs; that is, show differences from index or
        /// on-disk file to tree contents.
        /// 
        /// </summary>
        public bool R { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When run from a subdirectory of the project, it can be
        /// told to exclude changes outside the directory and show
        /// pathnames relative to it with this option.  When you are
        /// not in a subdirectory (e.g. in a bare repository), you
        /// can name which subdirectory to make the output relative
        /// to by giving a &lt;path&gt; as an argument.
        /// endif::git-format-patch[]
        /// 
        /// </summary>
        public string Relative { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Treat all files as text.
        /// 
        /// </summary>
        public bool Text { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Ignore changes in whitespace at EOL.
        /// 
        /// </summary>
        public bool IgnoreSpaceAtEol { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Ignore changes in amount of whitespace.  This ignores whitespace
        /// at line end, and considers all other sequences of one or
        /// more whitespace characters to be equivalent.
        /// 
        /// </summary>
        public bool IgnoreSpaceChange { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Ignore whitespace when comparing lines.  This ignores
        /// differences even if one line has whitespace where the other
        /// line has none.
        /// 
        /// </summary>
        public bool IgnoreAllSpace { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show the context between diff hunks, up to the specified number
        /// of lines, thereby fusing hunks that are close to each other.
        /// 
        /// </summary>
        public string InterHunkContext { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ifndef::git-format-patch[]
        /// Make the program exit with codes similar to diff(1).
        /// That is, it exits with 1 if there were differences and
        /// 0 means no differences.
        /// 
        /// </summary>
        public bool ExitCode { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Disable all output of the program. Implies `--exit-code`.
        /// endif::git-format-patch[]
        /// 
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Allow an external diff helper to be executed. If you set an
        /// external diff driver with linkgit:gitattributes[5], you need
        /// to use this option with linkgit:git-log[1] and friends.
        /// 
        /// </summary>
        public bool ExtDiff { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Disallow external diff drivers.
        /// 
        /// </summary>
        public bool NoExtDiff { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Ignore changes to submodules in the diff generation.
        /// 
        /// </summary>
        public bool IgnoreSubmodules { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show the given source prefix instead of "a/".
        /// 
        /// </summary>
        public string SrcPrefix { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show the given destination prefix instead of "b/".
        /// 
        /// </summary>
        public string DstPrefix { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not show any source or destination prefix.
        /// 
        /// </summary>
        public bool NoPrefix { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
