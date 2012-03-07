/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System.Collections.Generic;
using System.IO;
using System.Text;
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Diff;
using NGit.Patch;
using NGit.Util;
using Sharpen;

namespace NGit.Api
{
	/// <summary>Apply a patch to files and/or to the index.</summary>
	/// <remarks>Apply a patch to files and/or to the index.</remarks>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-apply.html"
	/// *      >Git documentation about apply</a></seealso>
	public class ApplyCommand : GitCommand<ApplyResult>
	{
		private InputStream @in;

		/// <summary>Constructs the command if the patch is to be applied to the index.</summary>
		/// <remarks>Constructs the command if the patch is to be applied to the index.</remarks>
		/// <param name="repo"></param>
		protected internal ApplyCommand(Repository repo) : base(repo)
		{
		}

		/// <param name="in">the patch to apply</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.ApplyCommand SetPatch(InputStream @in)
		{
			CheckCallable();
			this.@in = @in;
			return this;
		}

		/// <summary>
		/// Executes the
		/// <code>ApplyCommand</code>
		/// command with all the options and
		/// parameters collected by the setter methods (e.g.
		/// <see cref="SetPatch(Sharpen.InputStream)">SetPatch(Sharpen.InputStream)</see>
		/// of this class. Each instance of this class
		/// should only be used for one invocation of the command. Don't call this
		/// method twice on an instance.
		/// </summary>
		/// <returns>
		/// an
		/// <see cref="ApplyResult">ApplyResult</see>
		/// object representing the command result
		/// </returns>
		/// <exception cref="NGit.Api.Errors.GitAPIException"></exception>
		public override ApplyResult Call()
		{
			CheckCallable();
			ApplyResult r = new ApplyResult();
			try
			{
				NGit.Patch.Patch p = new NGit.Patch.Patch();
				try
				{
					p.Parse(@in);
				}
				finally
				{
					@in.Close();
				}
				if (!p.GetErrors().IsEmpty())
				{
					throw new PatchFormatException(p.GetErrors());
				}
				foreach (FileHeader fh in p.GetFiles())
				{
					DiffEntry.ChangeType type = fh.GetChangeType();
					FilePath f = null;
					switch (type)
					{
						case DiffEntry.ChangeType.ADD:
						{
							f = GetFile(fh.GetNewPath(), true);
							Apply(f, fh);
							break;
						}

						case DiffEntry.ChangeType.MODIFY:
						{
							f = GetFile(fh.GetOldPath(), false);
							Apply(f, fh);
							break;
						}

						case DiffEntry.ChangeType.DELETE:
						{
							f = GetFile(fh.GetOldPath(), false);
							if (!f.Delete())
							{
								throw new PatchApplyException(MessageFormat.Format(JGitText.Get().cannotDeleteFile
									, f));
							}
							break;
						}

						case DiffEntry.ChangeType.RENAME:
						{
							f = GetFile(fh.GetOldPath(), false);
							FilePath dest = GetFile(fh.GetNewPath(), false);
							if (!f.RenameTo(dest))
							{
								throw new PatchApplyException(MessageFormat.Format(JGitText.Get().renameFileFailed
									, f, dest));
							}
							break;
						}

						case DiffEntry.ChangeType.COPY:
						{
							f = GetFile(fh.GetOldPath(), false);
							byte[] bs = IOUtil.ReadFully(f);
							FileWriter fw = new FileWriter(GetFile(fh.GetNewPath(), true));
							fw.Write(Sharpen.Runtime.GetStringForBytes(bs));
							fw.Close();
							break;
						}
					}
					r.AddUpdatedFile(f);
				}
			}
			catch (IOException e)
			{
				throw new PatchApplyException(MessageFormat.Format(JGitText.Get().patchApplyException
					, e.Message), e);
			}
			SetCallable(false);
			return r;
		}

		/// <exception cref="NGit.Api.Errors.PatchApplyException"></exception>
		private FilePath GetFile(string path, bool create)
		{
			FilePath f = new FilePath(GetRepository().WorkTree, path);
			if (create)
			{
				try
				{
					FileUtils.CreateNewFile(f);
				}
				catch (IOException e)
				{
					throw new PatchApplyException(MessageFormat.Format(JGitText.Get().createNewFileFailed
						, f), e);
				}
			}
			return f;
		}

		/// <param name="f"></param>
		/// <param name="fh"></param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		/// <exception cref="NGit.Api.Errors.PatchApplyException">NGit.Api.Errors.PatchApplyException
		/// 	</exception>
		private void Apply(FilePath f, FileHeader fh)
		{
			RawText rt = new RawText(f);
			IList<string> oldLines = new AList<string>(rt.Size());
			for (int i = 0; i < rt.Size(); i++)
			{
				oldLines.AddItem(rt.GetString(i));
			}
			IList<string> newLines = new AList<string>(oldLines);
			foreach (HunkHeader hh in fh.GetHunks())
			{
				StringBuilder hunk = new StringBuilder();
				for (int j = hh.GetStartOffset(); j < hh.GetEndOffset(); j++)
				{
					hunk.Append((char)hh.GetBuffer()[j]);
				}
				RawText hrt = new RawText(Sharpen.Runtime.GetBytesForString(hunk.ToString()));
				IList<string> hunkLines = new AList<string>(hrt.Size());
				for (int i_1 = 0; i_1 < hrt.Size(); i_1++)
				{
					hunkLines.AddItem(hrt.GetString(i_1));
				}
				int pos = 0;
				for (int j_1 = 1; j_1 < hunkLines.Count; j_1++)
				{
					string hunkLine = hunkLines[j_1];
					switch (hunkLine[0])
					{
						case ' ':
						{
							if (!newLines[hh.GetNewStartLine() - 1 + pos].Equals(Sharpen.Runtime.Substring(hunkLine
								, 1)))
							{
								throw new PatchApplyException(MessageFormat.Format(JGitText.Get().patchApplyException
									, hh));
							}
							pos++;
							break;
						}

						case '-':
						{
							if (!newLines[hh.GetNewStartLine() - 1 + pos].Equals(Sharpen.Runtime.Substring(hunkLine
								, 1)))
							{
								throw new PatchApplyException(MessageFormat.Format(JGitText.Get().patchApplyException
									, hh));
							}
							newLines.Remove(hh.GetNewStartLine() - 1 + pos);
							break;
						}

						case '+':
						{
							newLines.Add(hh.GetNewStartLine() - 1 + pos, Sharpen.Runtime.Substring(hunkLine, 
								1));
							pos++;
							break;
						}
					}
				}
			}
			if (!IsNoNewlineAtEndOfFile(fh))
			{
				newLines.AddItem(string.Empty);
			}
			if (!rt.IsMissingNewlineAtEnd())
			{
				oldLines.AddItem(string.Empty);
			}
			if (!IsChanged(oldLines, newLines))
			{
				return;
			}
			// don't touch the file
			StringBuilder sb = new StringBuilder();
			string eol = rt.Size() == 0 || (rt.Size() == 1 && rt.IsMissingNewlineAtEnd()) ? "\n"
				 : rt.GetLineDelimiter();
			foreach (string l in newLines)
			{
				sb.Append(l);
				if (eol != null)
				{
					sb.Append(eol);
				}
			}
			Sharpen.Runtime.DeleteCharAt(sb, sb.Length - 1);
			FileWriter fw = new FileWriter(f);
			fw.Write(sb.ToString());
			fw.Close();
		}

		private bool IsChanged(IList<string> ol, IList<string> nl)
		{
			if (ol.Count != nl.Count)
			{
				return true;
			}
			for (int i = 0; i < ol.Count; i++)
			{
				if (!ol[i].Equals(nl[i]))
				{
					return true;
				}
			}
			return false;
		}

		private bool IsNoNewlineAtEndOfFile(FileHeader fh)
		{
			HunkHeader lastHunk = fh.GetHunks()[fh.GetHunks().Count - 1];
			RawText lhrt = new RawText(lastHunk.GetBuffer());
			return lhrt.GetString(lhrt.Size() - 1).Equals("\\ No newline at end of file");
		}
		//$NON-NLS-1$
	}
}
