/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2008, Google Inc
 * Copyright (C) 2008, Caytchen 
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
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
using GitSharp.Core.Exceptions;
using GitSharp.Core.Transport;
using GitSharp.Core;

namespace GitSharp.Commands
{
	/// <summary>
	/// Represents git's clone command line interface command.
	/// </summary>
	public class CloneCommand
		: AbstractFetchCommand
	{

		public CloneCommand()
		{
			Quiet = true;
		}

		// note: the naming of command parameters may not follow .NET conventions in favour of git command line parameter naming conventions.

		/// <summary>
        /// The name of a new directory to clone into.
		/// </summary>
		public string Directory { get; set; }

		/// <summary>
		/// The (possibly remote) repository to clone from.
		/// </summary>
		public string Source { get; set; }


		/// <summary>
		/// Not implemented
		/// 
		/// When the repository to clone from is on a local machine, this flag bypasses normal "git aware" transport mechanism and clones the repository 
		/// by making a copy of HEAD and everything under objects and refs directories. The files under .git/objects/ directory are hardlinked to save 
		/// space when possible. This is now the default when the source repository is specified with /path/to/repo  syntax, so it essentially is a no-op 
		/// option. To force copying instead of hardlinking (which may be desirable if you are trying to make a back-up of your repository), but still avoid 
		/// the usual "git aware" transport mechanism, --no-hardlinks can be used. 
		/// </summary>
		public bool Local { get; set; }

		/// <summary>
		/// Not implemented
		/// 
		/// Optimize the cloning process from a repository on a local filesystem by copying files under .git/objects  directory. 
		/// </summary>
		public bool NoHardLinks { get; set; }

		/// <summary>
		/// Not implemented
		/// 
		/// When the repository to clone is on the local machine, instead of using hard links, automatically setup .git/objects/info/alternates to share the objects 
		/// with the source repository. The resulting repository starts out without any object of its own. 
		///      
		/// NOTE: this is a possibly dangerous operation; do not use it unless you understand what it does. If you clone your repository using this option and then 
		/// delete branches (or use any other git command that makes any existing commit unreferenced) in the source repository, some objects may become 
		/// unreferenced (or dangling). These objects may be removed by normal git operations (such as git-commit) which automatically call git gc --auto. 
		/// (See git-gc(1).) If these objects are removed and were referenced by the cloned repository, then the cloned repository will become corrupt.
		/// </summary>
		public bool Shared { get; set; }

		/// <summary>
		/// Not implemented
		/// 
		/// If the reference repository is on the local machine automatically setup .git/objects/info/alternates to obtain objects from the reference repository. Using 
		/// an already existing repository as an alternate will require fewer objects to be copied from the repository being cloned, reducing network and local storage costs.
		/// 
		/// NOTE: see NOTE to --shared option.
		/// </summary>
		public string ReferenceRepository { get; set; }

		/// <summary>
		/// Operate quietly. This flag is also passed to the `rsync' command when given.
		/// </summary>
		public bool Quiet { get; set; }

		/// <summary>
		/// Display the progressbar, even in case the standard output is not a terminal. 
		/// </summary>
		public bool Verbose { get; set; }

		/// <summary>
		/// No checkout of HEAD is performed after the clone is complete. 
		/// </summary>
		public bool NoCheckout { get; set; }

		/// <summary>
		/// Make a bare GIT repository. That is, instead of creating "directory" and placing the administrative files in "directory"/.git, make the "directory"  itself the $GIT_DIR. 
		/// This obviously implies the -n  because there is nowhere to check out the working tree. Also the branch heads at the remote are copied directly to corresponding local 
		/// branch heads, without mapping them to refs/remotes/origin/. When this option is used, neither remote-tracking branches nor the related configuration variables are created. 
		/// </summary>
		public bool Bare { get; set; }

		/// <summary>
		/// Set up a mirror of the remote repository. This implies --bare. 
		/// </summary>
		public bool Mirror { get; set; }

		/// <summary>
		/// Instead of using the remote name origin to keep track of the upstream repository, use "name". 
		/// </summary>
		public string OriginName { get; set; }

		/// <summary>
		/// Not implemented.
		/// 
		/// When given, and the repository to clone from is accessed via ssh, this specifies a non-default path for the command run on the other end. 
		/// </summary>
		public string UploadPack { get; set; }

		/// <summary>
		/// Not implemented.
		/// 
		/// Specify the directory from which templates will be used; if unset the templates are taken from the installation defined default, typically /usr/share/git-core/templates. 
		/// </summary>
		public string TemplateDirectory { get; set; }

		/// <summary>
		/// Not implemented.
		/// 
		/// Create a shallow clone with a history truncated to the specified number of revisions. A shallow repository has a number of limitations (you cannot clone or fetch from it, 
		/// nor push from nor into it), but is adequate if you are only interested in the recent history of a large project with a long history, and would want to send in fixes as patches. 
		/// </summary>
		public int Depth { get; set; }

		/// <summary>
		/// Do it.
		/// </summary>
		public override void Execute()
		{
			if (Source.Length <= 0)
			{
                throw new ArgumentException("fatal: You must specify a repository to clone.");
			}

            if (Directory != null && GitDirectory != null)
            {
                throw new ArgumentException("conflicting usage of --git-dir and arguments");
            }

			var source = new URIish(Source);

            if (Directory == null)
            {
                try
                {
                    Directory = source.getHumanishName();
                }
                catch (InvalidOperationException e)
                {
                    throw new ArgumentException("cannot guess local name from " + source, e);
                }
            }

            if (GitDirectory == null)
            {
                GitDirectory = Path.Combine(Directory, Constants.DOT_GIT);
            }

			if (Mirror)
				Bare = true;
			if (Bare)
			{
				if (OriginName != null)
					throw new ArgumentException("Bare+Origin", "--bare and --origin " + OriginName + " options are incompatible.");
				NoCheckout = true;
			}
			if (OriginName == null)
				OriginName = Constants.DEFAULT_REMOTE_NAME;

			if (System.IO.Directory.Exists(Directory) && System.IO.Directory.GetFileSystemEntries(Directory).Length != 0)
			{
                throw new InvalidOperationException(string.Format("destination path '{0}' already exists and is not an empty directory.", new DirectoryInfo(Directory).FullName));
			}
            
            var repo = new Core.Repository(new DirectoryInfo(GitDirectory));
			repo.Create(Bare);
			repo.Config.setBoolean("core", null, "bare", Bare);
			repo.Config.save();
			Repository = new Repository(repo);
			if (!Quiet)
			{
				OutputStream.WriteLine("Initialized empty Git repository in " + repo.Directory.FullName);
				OutputStream.Flush();
			}

			saveRemote(source);

            FetchResult r;
		    try
		    {
		        r = runFetch();
		    }
		    catch (NoRemoteRepositoryException)
		    {
		        Repository.Dispose();
		        throw;
		    }
			GitSharp.Core.Ref branch = guessHEAD(r);
			if (!NoCheckout)
				doCheckout(branch);
		}

		private void saveRemote(URIish uri)
		{
			var repo = Repository._internal_repo;
			RemoteConfig rc = new RemoteConfig(repo.Config, OriginName);
			rc.AddURI(uri);
			rc.AddFetchRefSpec(new RefSpec().SetForce(true).SetSourceDestination(Constants.R_HEADS + "*",
				Constants.R_REMOTES + OriginName + "/*"));
			rc.Update(repo.Config);
			repo.Config.save();
		}

		private FetchResult runFetch()
		{
			Transport tn = Transport.open(Repository._internal_repo, OriginName);
			FetchResult r;

			try
			{
				if (!Quiet)
					r = tn.fetch(new TextProgressMonitor(OutputStream), null);
				else
					r = tn.fetch(new NullProgressMonitor(), null);
			}
			finally
			{
				tn.Dispose();
			}

			showFetchResult(tn, r);
			return r;
		}

		private static GitSharp.Core.Ref guessHEAD(FetchResult result)
		{
			GitSharp.Core.Ref idHEAD = result.GetAdvertisedRef(Constants.HEAD);
			List<GitSharp.Core.Ref> availableRefs = new List<GitSharp.Core.Ref>();
			GitSharp.Core.Ref head = null;

			foreach (GitSharp.Core.Ref r in result.AdvertisedRefs)
			{
				string n = r.Name;
				if (!n.StartsWith(Constants.R_HEADS))
					continue;
				availableRefs.Add(r);
				if (idHEAD == null || head != null)
					continue;

				if (r.ObjectId.Equals(idHEAD.ObjectId))
					head = r;
			}
			availableRefs.Sort(RefComparator.INSTANCE);
			if (idHEAD != null && head == null)
				head = idHEAD;
			return head;
		}

		private void doCheckout(GitSharp.Core.Ref branch)
		{
			if (branch == null)
				throw new ArgumentNullException("branch", "Cannot checkout; no HEAD advertised by remote");
			var repo = Repository._internal_repo;

			if (!Constants.HEAD.Equals(branch.getName()))
			{
				RefUpdate u1 = repo.UpdateRef(Constants.HEAD);
				u1.disableRefLog();
				u1.link(branch.getName());
			}

			GitSharp.Core.Commit commit = repo.MapCommit(branch.ObjectId);
			RefUpdate u = repo.UpdateRef(Constants.HEAD);
			u.NewObjectId = commit.CommitId;
			u.forceUpdate();
			GitIndex index = new GitIndex(repo);
			GitSharp.Core.Tree tree = commit.TreeEntry;
			WorkDirCheckout co = new WorkDirCheckout(repo, repo.WorkingDirectory, index, tree);
			co.checkout();
			index.write();
		}
	}
}