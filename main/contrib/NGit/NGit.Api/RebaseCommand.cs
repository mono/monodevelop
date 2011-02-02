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
using NGit.Dircache;
using NGit.Revwalk;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
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
		/// <summary>The name of the "rebase-merge" folder</summary>
		public static readonly string REBASE_MERGE = "rebase-merge";

		/// <summary>The name of the "stopped-sha" file</summary>
		public static readonly string STOPPED_SHA = "stopped-sha";

		private static readonly string AUTHOR_SCRIPT = "author-script";

		private static readonly string DONE = "done";

		private static readonly string GIT_AUTHOR_DATE = "GIT_AUTHOR_DATE";

		private static readonly string GIT_AUTHOR_EMAIL = "GIT_AUTHOR_EMAIL";

		private static readonly string GIT_AUTHOR_NAME = "GIT_AUTHOR_NAME";

		private static readonly string GIT_REBASE_TODO = "git-rebase-todo";

		private static readonly string HEAD_NAME = "head-name";

		private static readonly string INTERACTIVE = "interactive";

		private static readonly string MESSAGE = "message";

		private static readonly string ONTO = "onto";

		private static readonly string PATCH = "patch";

		private static readonly string REBASE_HEAD = "head";

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
			rebaseDir = new FilePath(repo.Directory, REBASE_MERGE);
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
			RevCommit newHead = null;
			bool lastStepWasForward = false;
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
						string upstreamCommitName = ReadFile(rebaseDir, ONTO);
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
					newHead = ContinueRebase();
				}
				if (this.operation == RebaseCommand.Operation.SKIP)
				{
					newHead = CheckoutCurrentHead();
				}
				ObjectReader or = repo.NewObjectReader();
				IList<RebaseCommand.Step> steps = LoadSteps();
				foreach (RebaseCommand.Step step in steps)
				{
					PopSteps(1);
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
					// if the first parent of commitToPick is the current HEAD,
					// we do a fast-forward instead of cherry-pick to avoid
					// unnecessary object rewriting
					newHead = TryFastForward(commitToPick);
					lastStepWasForward = newHead != null;
					if (!lastStepWasForward)
					{
						// TODO if the content of this commit is already merged here
						// we should skip this step in order to avoid confusing
						// pseudo-changed
						newHead = new Git(repo).CherryPick().Include(commitToPick).Call();
					}
					monitor.EndTask();
					if (newHead == null)
					{
						return Stop(commitToPick);
					}
				}
				if (newHead != null)
				{
					// point the previous head (if any) to the new commit
					string headName = ReadFile(rebaseDir, HEAD_NAME);
					if (headName.StartsWith(Constants.R_REFS))
					{
						RefUpdate rup = repo.UpdateRef(headName);
						rup.SetNewObjectId(newHead);
						RefUpdate.Result res_1 = rup.ForceUpdate();
						switch (res_1)
						{
							case RefUpdate.Result.FAST_FORWARD:
							case RefUpdate.Result.FORCED:
							case RefUpdate.Result.NO_CHANGE:
							{
								break;
							}

							default:
							{
								throw new JGitInternalException("Updating HEAD failed");
							}
						}
						rup = repo.UpdateRef(Constants.HEAD);
						res_1 = rup.Link(headName);
						switch (res_1)
						{
							case RefUpdate.Result.FAST_FORWARD:
							case RefUpdate.Result.FORCED:
							case RefUpdate.Result.NO_CHANGE:
							{
								break;
							}

							default:
							{
								throw new JGitInternalException("Updating HEAD failed");
							}
						}
					}
					FileUtils.Delete(rebaseDir, FileUtils.RECURSIVE);
					if (lastStepWasForward)
					{
						return new RebaseResult(RebaseResult.Status.FAST_FORWARD);
					}
					return new RebaseResult(RebaseResult.Status.OK);
				}
				return new RebaseResult(RebaseResult.Status.UP_TO_DATE);
			}
			catch (IOException ioe)
			{
				throw new JGitInternalException(ioe.Message, ioe);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Api.Errors.NoHeadException"></exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		private RevCommit CheckoutCurrentHead()
		{
			ObjectId headTree = repo.Resolve(Constants.HEAD + "^{tree}");
			if (headTree == null)
			{
				throw new NoHeadException(JGitText.Get().cannotRebaseWithoutCurrentHead);
			}
			DirCache dc = repo.LockDirCache();
			try
			{
				DirCacheCheckout dco = new DirCacheCheckout(repo, dc, headTree);
				dco.SetFailOnConflict(false);
				bool needsDeleteFiles = dco.Checkout();
				if (needsDeleteFiles)
				{
					IList<string> fileList = dco.GetToBeDeleted();
					foreach (string filePath in fileList)
					{
						FilePath fileToDelete = new FilePath(repo.WorkTree, filePath);
						if (fileToDelete.Exists())
						{
							FileUtils.Delete(fileToDelete, FileUtils.RECURSIVE | FileUtils.RETRY);
						}
					}
				}
			}
			finally
			{
				dc.Unlock();
			}
			RevWalk rw = new RevWalk(repo);
			RevCommit commit = rw.ParseCommit(repo.Resolve(Constants.HEAD));
			rw.Release();
			return commit;
		}

		/// <returns>the commit if we had to do a commit, otherwise null</returns>
		/// <exception cref="NGit.Api.Errors.GitAPIException">NGit.Api.Errors.GitAPIException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		private RevCommit ContinueRebase()
		{
			// if there are still conflicts, we throw a specific Exception
			DirCache dc = repo.ReadDirCache();
			bool hasUnmergedPaths = dc.HasUnmergedPaths();
			if (hasUnmergedPaths)
			{
				throw new UnmergedPathsException();
			}
			// determine whether we need to commit
			TreeWalk treeWalk = new TreeWalk(repo);
			treeWalk.Reset();
			treeWalk.Recursive = true;
			treeWalk.AddTree(new DirCacheIterator(dc));
			ObjectId id = repo.Resolve(Constants.HEAD + "^{tree}");
			if (id == null)
			{
				throw new NoHeadException(JGitText.Get().cannotRebaseWithoutCurrentHead);
			}
			treeWalk.AddTree(id);
			treeWalk.Filter = TreeFilter.ANY_DIFF;
			bool needsCommit = treeWalk.Next();
			treeWalk.Release();
			if (needsCommit)
			{
				CommitCommand commit = new Git(repo).Commit();
				commit.SetMessage(ReadFile(rebaseDir, MESSAGE));
				commit.SetAuthor(ParseAuthor());
				return commit.Call();
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private PersonIdent ParseAuthor()
		{
			FilePath authorScriptFile = new FilePath(rebaseDir, AUTHOR_SCRIPT);
			byte[] raw;
			try
			{
				raw = IOUtil.ReadFully(authorScriptFile);
			}
			catch (FileNotFoundException)
			{
				return null;
			}
			return ParseAuthor(raw);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private RebaseResult Stop(RevCommit commitToPick)
		{
			PersonIdent author = commitToPick.GetAuthorIdent();
			string authorScript = ToAuthorScript(author);
			CreateFile(rebaseDir, AUTHOR_SCRIPT, authorScript);
			CreateFile(rebaseDir, MESSAGE, commitToPick.GetFullMessage());
			ByteArrayOutputStream bos = new ByteArrayOutputStream();
			DiffFormatter df = new DiffFormatter(bos);
			df.SetRepository(repo);
			df.Format(commitToPick.GetParent(0), commitToPick);
			CreateFile(rebaseDir, PATCH, Sharpen.Extensions.CreateString(bos.ToByteArray(), Constants
				.CHARACTER_ENCODING));
			CreateFile(rebaseDir, STOPPED_SHA, repo.NewObjectReader().Abbreviate(commitToPick
				).Name);
			return new RebaseResult(commitToPick);
		}

		internal virtual string ToAuthorScript(PersonIdent author)
		{
			StringBuilder sb = new StringBuilder(100);
			sb.Append(GIT_AUTHOR_NAME);
			sb.Append("='");
			sb.Append(author.GetName());
			sb.Append("'\n");
			sb.Append(GIT_AUTHOR_EMAIL);
			sb.Append("='");
			sb.Append(author.GetEmailAddress());
			sb.Append("'\n");
			// the command line uses the "external String"
			// representation for date and timezone
			sb.Append(GIT_AUTHOR_DATE);
			sb.Append("='");
			string externalString = author.ToExternalString();
			sb.Append(Sharpen.Runtime.Substring(externalString, externalString.LastIndexOf('>'
				) + 2));
			sb.Append("'\n");
			return sb.ToString();
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
			IList<string> todoLines = new AList<string>();
			IList<string> poppedLines = new AList<string>();
			FilePath todoFile = new FilePath(rebaseDir, GIT_REBASE_TODO);
			FilePath doneFile = new FilePath(rebaseDir, DONE);
			BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(
				todoFile), Constants.CHARACTER_ENCODING));
			try
			{
				// check if the line starts with a action tag (pick, skip...)
				while (poppedLines.Count < numSteps)
				{
					string popCandidate = br.ReadLine();
					if (popCandidate == null)
					{
						break;
					}
					if (popCandidate[0] == '#')
					{
						continue;
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
						poppedLines.AddItem(popCandidate);
					}
					else
					{
						todoLines.AddItem(popCandidate);
					}
				}
				string readLine = br.ReadLine();
				while (readLine != null)
				{
					todoLines.AddItem(readLine);
					readLine = br.ReadLine();
				}
			}
			finally
			{
				br.Close();
			}
			BufferedWriter todoWriter = new BufferedWriter(new OutputStreamWriter(new FileOutputStream
				(todoFile), Constants.CHARACTER_ENCODING));
			try
			{
				foreach (string writeLine in todoLines)
				{
					todoWriter.Write(writeLine);
					todoWriter.NewLine();
				}
			}
			finally
			{
				todoWriter.Close();
			}
			if (poppedLines.Count > 0)
			{
				// append here
				BufferedWriter doneWriter = new BufferedWriter(new OutputStreamWriter(new FileOutputStream
					(doneFile, true), Constants.CHARACTER_ENCODING));
				try
				{
					foreach (string writeLine in poppedLines)
					{
						doneWriter.Write(writeLine);
						doneWriter.NewLine();
					}
				}
				finally
				{
					doneWriter.Close();
				}
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
			// if the upstream commit is in a direct line to the current head,
			// the log command will not report any commits; in this case,
			// we create the cherry-pick list ourselves
			if (cherryPickList.IsEmpty())
			{
				Iterable<RevCommit> parents = new Git(repo).Log().Add(upstreamCommit).Call();
				foreach (RevCommit parent in parents)
				{
					if (parent.Equals(headCommit))
					{
						break;
					}
					if (parent.ParentCount != 1)
					{
						throw new JGitInternalException(JGitText.Get().canOnlyCherryPickCommitsWithOneParent
							);
					}
					cherryPickList.AddItem(parent);
				}
			}
			// nothing to do: return with UP_TO_DATE_RESULT
			if (cherryPickList.IsEmpty())
			{
				return RebaseResult.UP_TO_DATE_RESULT;
			}
			Sharpen.Collections.Reverse(cherryPickList);
			// create the folder for the meta information
			FileUtils.Mkdir(rebaseDir);
			CreateFile(repo.Directory, Constants.ORIG_HEAD, headId.Name);
			CreateFile(rebaseDir, REBASE_HEAD, headId.Name);
			CreateFile(rebaseDir, HEAD_NAME, headName);
			CreateFile(rebaseDir, ONTO, upstreamCommit.Name);
			CreateFile(rebaseDir, INTERACTIVE, string.Empty);
			BufferedWriter fw = new BufferedWriter(new OutputStreamWriter(new FileOutputStream
				(new FilePath(rebaseDir, GIT_REBASE_TODO)), Constants.CHARACTER_ENCODING));
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

		/// <summary>checks if we can fast-forward and returns the new head if it is possible
		/// 	</summary>
		/// <param name="newCommit"></param>
		/// <returns>the new head, or null</returns>
		/// <exception cref="NGit.Api.Errors.RefNotFoundException">NGit.Api.Errors.RefNotFoundException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual RevCommit TryFastForward(RevCommit newCommit)
		{
			Ref head = repo.GetRef(Constants.HEAD);
			if (head == null || head.GetObjectId() == null)
			{
				throw new RefNotFoundException(MessageFormat.Format(JGitText.Get().refNotResolved
					, Constants.HEAD));
			}
			ObjectId headId = head.GetObjectId();
			if (headId == null)
			{
				throw new RefNotFoundException(MessageFormat.Format(JGitText.Get().refNotResolved
					, Constants.HEAD));
			}
			RevCommit headCommit = walk.LookupCommit(headId);
			if (walk.IsMergedInto(newCommit, headCommit))
			{
				return newCommit;
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
			return TryFastForward(headName, headCommit, newCommit);
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		private RevCommit TryFastForward(string headName, RevCommit oldCommit, RevCommit 
			newCommit)
		{
			bool tryRebase = false;
			foreach (RevCommit parentCommit in newCommit.Parents)
			{
				if (parentCommit.Equals(oldCommit))
				{
					tryRebase = true;
				}
			}
			if (!tryRebase)
			{
				return null;
			}
			CheckoutCommand co = new CheckoutCommand(repo);
			try
			{
				co.SetName(newCommit.Name).Call();
				if (headName.StartsWith(Constants.R_HEADS))
				{
					RefUpdate rup = repo.UpdateRef(headName);
					rup.SetExpectedOldObjectId(oldCommit);
					rup.SetNewObjectId(newCommit);
					rup.SetRefLogMessage("Fast-foward from " + oldCommit.Name + " to " + newCommit.Name
						, false);
					RefUpdate.Result res = rup.Update(walk);
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
							throw new IOException("Could not fast-forward");
						}
					}
				}
				return newCommit;
			}
			catch (RefAlreadyExistsException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			catch (RefNotFoundException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			catch (InvalidRefNameException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
		}

		/// <exception cref="NGit.Api.Errors.WrongRepositoryStateException"></exception>
		private void CheckParameters()
		{
			RepositoryState s = repo.GetRepositoryState();
			if (this.operation != RebaseCommand.Operation.BEGIN)
			{
				// these operations are only possible while in a rebasing state
				if (repo.GetRepositoryState() != RepositoryState.REBASING_INTERACTIVE)
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
				fos.Write(Sharpen.Runtime.GetBytesForString(content, Constants.CHARACTER_ENCODING
					));
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
				string commitId = ReadFile(repo.Directory, Constants.ORIG_HEAD);
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
				string headName = ReadFile(rebaseDir, HEAD_NAME);
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
				FileUtils.Delete(rebaseDir, FileUtils.RECURSIVE);
				return new RebaseResult(RebaseResult.Status.ABORTED);
			}
			finally
			{
				monitor.EndTask();
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
			byte[] buf = IOUtil.ReadFully(new FilePath(rebaseDir, GIT_REBASE_TODO));
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
							if (actionToken[0] == '#')
							{
								tokenCount = 3;
								break;
							}
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

		/// <param name="upstream">id of the upstream commit</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.RebaseCommand SetUpstream(AnyObjectId upstream)
		{
			try
			{
				this.upstreamCommit = walk.ParseCommit(upstream);
			}
			catch (IOException e)
			{
				throw new JGitInternalException(MessageFormat.Format(JGitText.Get().couldNotReadObjectWhileParsingCommit
					, upstream.Name), e);
			}
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
				throw new JGitInternalException(MessageFormat.Format("Unknown or unsupported command \"{0}\", only  \"pick\" is allowed"
					, token));
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

		internal virtual PersonIdent ParseAuthor(byte[] raw)
		{
			if (raw.Length == 0)
			{
				return null;
			}
			IDictionary<string, string> keyValueMap = new Dictionary<string, string>();
			for (int p = 0; p < raw.Length; )
			{
				int end = RawParseUtils.NextLF(raw, p);
				if (end == p)
				{
					break;
				}
				int equalsIndex = RawParseUtils.Next(raw, p, '=');
				if (equalsIndex == end)
				{
					break;
				}
				string key = RawParseUtils.Decode(raw, p, equalsIndex - 1);
				string value = RawParseUtils.Decode(raw, equalsIndex + 1, end - 2);
				p = end;
				keyValueMap.Put(key, value);
			}
			string name = keyValueMap.Get(GIT_AUTHOR_NAME);
			string email = keyValueMap.Get(GIT_AUTHOR_EMAIL);
			string time = keyValueMap.Get(GIT_AUTHOR_DATE);
			// the time is saved as <seconds since 1970> <timezone offset>
			long when = long.Parse(Sharpen.Runtime.Substring(time, 0, time.IndexOf(' '))) * 1000;
			string tzOffsetString = Sharpen.Runtime.Substring(time, time.IndexOf(' ') + 1);
			int multiplier = -1;
			if (tzOffsetString[0] == '+')
			{
				multiplier = 1;
			}
			int hours = System.Convert.ToInt32(Sharpen.Runtime.Substring(tzOffsetString, 1, 3
				));
			int minutes = System.Convert.ToInt32(Sharpen.Runtime.Substring(tzOffsetString, 3, 
				5));
			// this is in format (+/-)HHMM (hours and minutes)
			// we need to convert into minutes
			int tz = (hours * 60 + minutes) * multiplier;
			if (name != null && email != null)
			{
				return new PersonIdent(name, email, when, tz);
			}
			return null;
		}
	}
}
