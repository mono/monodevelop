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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Dircache;
using NGit.Revwalk;
using NGit.Util;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// A class used to execute a
	/// <code>Rebase</code>
	/// command. It has setters for all
	/// supported options and arguments of this command and a
	/// <see cref="Call()">Call()</see>
	/// method
	/// to finally execute the command. Each instance of this class should only be
	/// used for one invocation of the command (means: one call to
	/// <see cref="Call()">Call()</see>
	/// )
	/// <p>
	/// </summary>
	/// <seealso><a
	/// *      href="http://www.kernel.org/pub/software/scm/git/docs/git-rebase.html"
	/// *      >Git documentation about Rebase</a></seealso>
	public class RebaseCommand : GitCommand<RebaseResult>
	{
		/// <summary>The available operations</summary>
		public enum Operation
		{
			BEGIN,
			CONTINUE,
			SKIP,
			ABORT
		}

		private RebaseCommand.Operation operation = RebaseCommand.Operation.BEGIN;

		private RevCommit upstreamCommit;

		private ProgressMonitor monitor = NullProgressMonitor.INSTANCE;

		private readonly RevWalk walk;

		private readonly FilePath rebaseDir;

		/// <param name="repo"></param>
		protected internal RebaseCommand(Repository repo) : base(repo)
		{
			walk = new RevWalk(repo);
			rebaseDir = new FilePath(repo.Directory, "rebase-merge");
		}

		/// <summary>
		/// Executes the
		/// <code>Rebase</code>
		/// command with all the options and parameters
		/// collected by the setter methods of this class. Each instance of this
		/// class should only be used for one invocation of the command. Don't call
		/// this method twice on an instance.
		/// </summary>
		/// <returns>an object describing the result of this command</returns>
		/// <exception cref="NGit.Api.Errors.NoHeadException"></exception>
		/// <exception cref="NGit.Api.Errors.RefNotFoundException"></exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		/// <exception cref="NGit.Api.Errors.GitAPIException"></exception>
		public override RebaseResult Call()
		{
			CheckCallable();
			CheckParameters();
			try
			{
				switch (operation)
				{
					case RebaseCommand.Operation.ABORT:
					{
						try
						{
							return Abort();
						}
						catch (IOException ioe)
						{
							throw new JGitInternalException(ioe.Message, ioe);
						}
						goto case RebaseCommand.Operation.SKIP;
					}

					case RebaseCommand.Operation.SKIP:
					case RebaseCommand.Operation.CONTINUE:
					{
						// fall through
						string upstreamCommitName = ReadFile(rebaseDir, "onto");
						this.upstreamCommit = walk.ParseCommit(repo.Resolve(upstreamCommitName));
						break;
					}

					case RebaseCommand.Operation.BEGIN:
					{
						RebaseResult res = InitFilesAndRewind();
						if (res != null)
						{
							return res;
						}
					break;
					}
				}
				if (monitor.IsCancelled())
				{
					return Abort();
				}
				if (this.operation == RebaseCommand.Operation.CONTINUE)
				{
					throw new NotSupportedException("--continue Not yet implemented");
				}
				if (this.operation == RebaseCommand.Operation.SKIP)
				{
					throw new NotSupportedException("--skip Not yet implemented");
				}
				RevCommit newHead = null;
				IList<RebaseCommand.Step> steps = LoadSteps();
				ObjectReader or = repo.NewObjectReader();
				int stepsToPop = 0;
				foreach (RebaseCommand.Step step in steps)
				{
					if (step.action != RebaseCommand.Action.PICK)
					{
						continue;
					}
					ICollection<ObjectId> ids = or.Resolve(step.commit);
					if (ids.Count != 1)
					{
						throw new JGitInternalException("Could not resolve uniquely the abbreviated object ID"
							);
					}
					RevCommit commitToPick = walk.ParseCommit(ids.Iterator().Next());
					if (monitor.IsCancelled())
					{
						return new RebaseResult(commitToPick);
					}
					monitor.BeginTask(MessageFormat.Format(JGitText.Get().applyingCommit, commitToPick
						.GetShortMessage()), ProgressMonitor.UNKNOWN);
					// TODO if the first parent of commitToPick is the current HEAD,
					// we should fast-forward instead of cherry-pick to avoid
					// unnecessary object rewriting
					newHead = new Git(repo).CherryPick().Include(commitToPick).Call();
					monitor.EndTask();
					if (newHead == null)
					{
						PopSteps(stepsToPop);
						return new RebaseResult(commitToPick);
					}
					stepsToPop++;
				}
				if (newHead != null)
				{
					// point the previous head (if any) to the new commit
					string headName = ReadFile(rebaseDir, "head-name");
					if (headName.StartsWith(Constants.R_REFS))
					{
						RefUpdate rup = repo.UpdateRef(headName);
						rup.SetNewObjectId(newHead);
						rup.ForceUpdate();
						rup = repo.UpdateRef(Constants.HEAD);
						rup.Link(headName);
					}
					DeleteRecursive(rebaseDir);
					return new RebaseResult(RebaseResult.Status.OK);
				}
				return new RebaseResult(RebaseResult.Status.UP_TO_DATE);
			}
			catch (IOException ioe)
			{
				throw new JGitInternalException(ioe.Message, ioe);
			}
		}

		/// <summary>
		/// Removes the number of lines given in the parameter from the
		/// <code>git-rebase-todo</code> file but preserves comments and other lines
		/// that can not be parsed as steps
		/// </summary>
		/// <param name="numSteps"></param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		private void PopSteps(int numSteps)
		{
			if (numSteps == 0)
			{
				return;
			}
			IList<string> lines = new AList<string>();
			FilePath file = new FilePath(rebaseDir, "git-rebase-todo");
			BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(
				file), "UTF-8"));
			int popped = 0;
			try
			{
				// check if the line starts with a action tag (pick, skip...)
				while (popped < numSteps)
				{
					string popCandidate = br.ReadLine();
					if (popCandidate == null)
					{
						break;
					}
					int spaceIndex = popCandidate.IndexOf(' ');
					bool pop = false;
					if (spaceIndex >= 0)
					{
						string actionToken = Sharpen.Runtime.Substring(popCandidate, 0, spaceIndex);
						pop = RebaseCommand.Action.Parse(actionToken) != null;
					}
					if (pop)
					{
						popped++;
					}
					else
					{
						lines.AddItem(popCandidate);
					}
				}
				string readLine = br.ReadLine();
				while (readLine != null)
				{
					lines.AddItem(readLine);
					readLine = br.ReadLine();
				}
			}
			finally
			{
				br.Close();
			}
			BufferedWriter bw = new BufferedWriter(new OutputStreamWriter(new FileOutputStream
				(file), "UTF-8"));
			try
			{
				foreach (string writeLine in lines)
				{
					bw.Write(writeLine);
					bw.NewLine();
				}
			}
			finally
			{
				bw.Close();
			}
		}

		/// <exception cref="NGit.Api.Errors.RefNotFoundException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Api.Errors.NoHeadException"></exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		private RebaseResult InitFilesAndRewind()
		{
			// we need to store everything into files so that we can implement
			// --skip, --continue, and --abort
			// first of all, we determine the commits to be applied
			IList<RevCommit> cherryPickList = new AList<RevCommit>();
			Ref head = repo.GetRef(Constants.HEAD);
			if (head == null || head.GetObjectId() == null)
			{
				throw new RefNotFoundException(MessageFormat.Format(JGitText.Get().refNotResolved
					, Constants.HEAD));
			}
			string headName;
			if (head.IsSymbolic())
			{
				headName = head.GetTarget().GetName();
			}
			else
			{
				headName = "detached HEAD";
			}
			ObjectId headId = head.GetObjectId();
			if (headId == null)
			{
				throw new RefNotFoundException(MessageFormat.Format(JGitText.Get().refNotResolved
					, Constants.HEAD));
			}
			RevCommit headCommit = walk.LookupCommit(headId);
			monitor.BeginTask(JGitText.Get().obtainingCommitsForCherryPick, ProgressMonitor.UNKNOWN
				);
			LogCommand cmd = new Git(repo).Log().AddRange(upstreamCommit, headCommit);
			Iterable<RevCommit> commitsToUse = cmd.Call();
			foreach (RevCommit commit in commitsToUse)
			{
				cherryPickList.AddItem(commit);
			}
			// nothing to do: return with UP_TO_DATE_RESULT
			if (cherryPickList.IsEmpty())
			{
				return RebaseResult.UP_TO_DATE_RESULT;
			}
			Sharpen.Collections.Reverse(cherryPickList);
			// create the folder for the meta information
			rebaseDir.Mkdir();
			CreateFile(repo.Directory, "ORIG_HEAD", headId.Name);
			CreateFile(rebaseDir, "head", headId.Name);
			CreateFile(rebaseDir, "head-name", headName);
			CreateFile(rebaseDir, "onto", upstreamCommit.Name);
			BufferedWriter fw = new BufferedWriter(new OutputStreamWriter(new FileOutputStream
				(new FilePath(rebaseDir, "git-rebase-todo")), "UTF-8"));
			fw.Write("# Created by EGit: rebasing " + upstreamCommit.Name + " onto " + headId
				.Name);
			fw.NewLine();
			try
			{
				StringBuilder sb = new StringBuilder();
				ObjectReader reader = walk.GetObjectReader();
				foreach (RevCommit commit_1 in cherryPickList)
				{
					sb.Length = 0;
					sb.Append(RebaseCommand.Action.PICK.ToToken());
					sb.Append(" ");
					sb.Append(reader.Abbreviate(commit_1).Name);
					sb.Append(" ");
					sb.Append(commit_1.GetShortMessage());
					fw.Write(sb.ToString());
					fw.NewLine();
				}
			}
			finally
			{
				fw.Close();
			}
			monitor.EndTask();
			// we rewind to the upstream commit
			monitor.BeginTask(MessageFormat.Format(JGitText.Get().rewinding, upstreamCommit.GetShortMessage
				()), ProgressMonitor.UNKNOWN);
			CheckoutCommit(upstreamCommit);
			monitor.EndTask();
			return null;
		}

		/// <exception cref="NGit.Api.Errors.WrongRepositoryStateException"></exception>
		private void CheckParameters()
		{
			RepositoryState s = repo.GetRepositoryState();
			if (this.operation != RebaseCommand.Operation.BEGIN)
			{
				// these operations are only possible while in a rebasing state
				if (s != RepositoryState.REBASING && s != RepositoryState.REBASING_INTERACTIVE &&
					 s != RepositoryState.REBASING_MERGE && s != RepositoryState.REBASING_REBASING)
				{
					throw new WrongRepositoryStateException(MessageFormat.Format(JGitText.Get().wrongRepositoryState
						, repo.GetRepositoryState().Name()));
				}
			}
			else
			{
				if (s == RepositoryState.SAFE)
				{
					if (this.upstreamCommit == null)
					{
						throw new JGitInternalException(MessageFormat.Format(JGitText.Get().missingRequiredParameter
							, "upstream"));
					}
					return;
				}
				else
				{
					throw new WrongRepositoryStateException(MessageFormat.Format(JGitText.Get().wrongRepositoryState
						, repo.GetRepositoryState().Name()));
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CreateFile(FilePath parentDir, string name, string content)
		{
			FilePath file = new FilePath(parentDir, name);
			FileOutputStream fos = new FileOutputStream(file);
			try
			{
				fos.Write(Sharpen.Runtime.GetBytesForString(content, "UTF-8"));
				fos.Write('\n');
			}
			finally
			{
				fos.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private RebaseResult Abort()
		{
			try
			{
				string commitId = ReadFile(repo.Directory, "ORIG_HEAD");
				monitor.BeginTask(MessageFormat.Format(JGitText.Get().abortingRebase, commitId), 
					ProgressMonitor.UNKNOWN);
				RevCommit commit = walk.ParseCommit(repo.Resolve(commitId));
				// no head in order to reset --hard
				DirCacheCheckout dco = new DirCacheCheckout(repo, repo.LockDirCache(), commit.Tree
					);
				dco.SetFailOnConflict(false);
				dco.Checkout();
				walk.Release();
			}
			finally
			{
				monitor.EndTask();
			}
			try
			{
				string headName = ReadFile(rebaseDir, "head-name");
				if (headName.StartsWith(Constants.R_REFS))
				{
					monitor.BeginTask(MessageFormat.Format(JGitText.Get().resettingHead, headName), ProgressMonitor
						.UNKNOWN);
					// update the HEAD
					RefUpdate refUpdate = repo.UpdateRef(Constants.HEAD, false);
					RefUpdate.Result res = refUpdate.Link(headName);
					switch (res)
					{
						case RefUpdate.Result.FAST_FORWARD:
						case RefUpdate.Result.FORCED:
						case RefUpdate.Result.NO_CHANGE:
						{
							break;
						}

						default:
						{
							throw new JGitInternalException(JGitText.Get().abortingRebaseFailed);
						}
					}
				}
				// cleanup the files
				DeleteRecursive(rebaseDir);
				return new RebaseResult(RebaseResult.Status.ABORTED);
			}
			finally
			{
				monitor.EndTask();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void DeleteRecursive(FilePath fileOrFolder)
		{
			FilePath[] children = fileOrFolder.ListFiles();
			if (children != null)
			{
				foreach (FilePath child in children)
				{
					DeleteRecursive(child);
				}
			}
			if (!fileOrFolder.Delete())
			{
				throw new IOException("Could not delete " + fileOrFolder.GetPath());
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private string ReadFile(FilePath directory, string fileName)
		{
			byte[] content = IOUtil.ReadFully(new FilePath(directory, fileName));
			// strip off the last LF
			int end = content.Length;
			while (0 < end && content[end - 1] == '\n')
			{
				end--;
			}
			return RawParseUtils.Decode(content, 0, end);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckoutCommit(RevCommit commit)
		{
			try
			{
				RevCommit head = walk.ParseCommit(repo.Resolve(Constants.HEAD));
				DirCacheCheckout dco = new DirCacheCheckout(repo, head.Tree, repo.LockDirCache(), 
					commit.Tree);
				dco.SetFailOnConflict(true);
				dco.Checkout();
				// update the HEAD
				RefUpdate refUpdate = repo.UpdateRef(Constants.HEAD, true);
				refUpdate.SetExpectedOldObjectId(head);
				refUpdate.SetNewObjectId(commit);
				RefUpdate.Result res = refUpdate.ForceUpdate();
				switch (res)
				{
					case RefUpdate.Result.FAST_FORWARD:
					case RefUpdate.Result.NO_CHANGE:
					case RefUpdate.Result.FORCED:
					{
						break;
					}

					default:
					{
						throw new IOException("Could not rewind to upstream commit");
					}
				}
			}
			finally
			{
				walk.Release();
				monitor.EndTask();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private IList<RebaseCommand.Step> LoadSteps()
		{
			byte[] buf = IOUtil.ReadFully(new FilePath(rebaseDir, "git-rebase-todo"));
			int ptr = 0;
			int tokenBegin = 0;
			AList<RebaseCommand.Step> r = new AList<RebaseCommand.Step>();
			while (ptr < buf.Length)
			{
				tokenBegin = ptr;
				ptr = RawParseUtils.NextLF(buf, ptr);
				int nextSpace = 0;
				int tokenCount = 0;
				RebaseCommand.Step current = null;
				while (tokenCount < 3 && nextSpace < ptr)
				{
					switch (tokenCount)
					{
						case 0:
						{
							nextSpace = RawParseUtils.Next(buf, tokenBegin, ' ');
							string actionToken = Sharpen.Extensions.CreateString(buf, tokenBegin, nextSpace -
								 tokenBegin - 1);
							tokenBegin = nextSpace;
							RebaseCommand.Action action = RebaseCommand.Action.Parse(actionToken);
							if (action != null)
							{
								current = new RebaseCommand.Step(RebaseCommand.Action.Parse(actionToken));
							}
							break;
						}

						case 1:
						{
							if (current == null)
							{
								break;
							}
							nextSpace = RawParseUtils.Next(buf, tokenBegin, ' ');
							string commitToken = Sharpen.Extensions.CreateString(buf, tokenBegin, nextSpace -
								 tokenBegin - 1);
							tokenBegin = nextSpace;
							current.commit = AbbreviatedObjectId.FromString(commitToken);
							break;
						}

						case 2:
						{
							if (current == null)
							{
								break;
							}
							nextSpace = ptr;
							int length = ptr - tokenBegin;
							current.shortMessage = new byte[length];
							System.Array.Copy(buf, tokenBegin, current.shortMessage, 0, length);
							r.AddItem(current);
							break;
						}
					}
					tokenCount++;
				}
			}
			return r;
		}

		/// <param name="upstream">the upstream commit</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.RebaseCommand SetUpstream(RevCommit upstream)
		{
			this.upstreamCommit = upstream;
			return this;
		}

		/// <param name="upstream">the upstream branch</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		/// <exception cref="NGit.Api.Errors.RefNotFoundException">NGit.Api.Errors.RefNotFoundException
		/// 	</exception>
		public virtual NGit.Api.RebaseCommand SetUpstream(string upstream)
		{
			try
			{
				ObjectId upstreamId = repo.Resolve(upstream);
				if (upstreamId == null)
				{
					throw new RefNotFoundException(MessageFormat.Format(JGitText.Get().refNotResolved
						, upstream));
				}
				upstreamCommit = walk.ParseCommit(repo.Resolve(upstream));
				return this;
			}
			catch (IOException ioe)
			{
				throw new JGitInternalException(ioe.Message, ioe);
			}
		}

		/// <param name="operation">the operation to perform</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.RebaseCommand SetOperation(RebaseCommand.Operation operation
			)
		{
			this.operation = operation;
			return this;
		}

		/// <param name="monitor">a progress monitor</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.RebaseCommand SetProgressMonitor(ProgressMonitor monitor)
		{
			this.monitor = monitor;
			return this;
		}

		internal class Action
		{
			public static RebaseCommand.Action PICK = new RebaseCommand.Action("pick");

			public readonly string token;

			private Action(string token)
			{
				// later add SQUASH, EDIT, etc.
				this.token = token;
			}

			public virtual string ToToken()
			{
				return this.token;
			}

			internal static RebaseCommand.Action Parse(string token)
			{
				if (token.Equals("pick") || token.Equals("p"))
				{
					return PICK;
				}
				return null;
			}
		}

		internal class Step
		{
			internal RebaseCommand.Action action;

			internal AbbreviatedObjectId commit;

			internal byte[] shortMessage;

			internal Step(RebaseCommand.Action action)
			{
				this.action = action;
			}
		}
	}
}
