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
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Errors;
using NGit.Internal;
using NGit.Transport;
using Sharpen;

namespace NGit.Api
{
	/// <summary>The ls-remote command</summary>
	/// <seealso><a
	/// *      href="http://www.kernel.org/pub/software/scm/git/docs/git-ls-remote.html"
	/// *      >Git documentation about ls-remote</a></seealso>
	public class LsRemoteCommand : TransportCommand<NGit.Api.LsRemoteCommand, ICollection
		<Ref>>
	{
		private string remote = Constants.DEFAULT_REMOTE_NAME;

		private bool heads;

		private bool tags;

		private string uploadPack;

		/// <param name="repo"></param>
		protected internal LsRemoteCommand(Repository repo) : base(repo)
		{
		}

		/// <summary>The remote (uri or name) used for the fetch operation.</summary>
		/// <remarks>
		/// The remote (uri or name) used for the fetch operation. If no remote is
		/// set, the default value of <code>Constants.DEFAULT_REMOTE_NAME</code> will
		/// be used.
		/// </remarks>
		/// <seealso cref="NGit.Constants.DEFAULT_REMOTE_NAME">NGit.Constants.DEFAULT_REMOTE_NAME
		/// 	</seealso>
		/// <param name="remote"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.LsRemoteCommand SetRemote(string remote)
		{
			CheckCallable();
			this.remote = remote;
			return this;
		}

		/// <summary>Include refs/heads in references results</summary>
		/// <param name="heads"></param>
		public virtual void SetHeads(bool heads)
		{
			this.heads = heads;
		}

		/// <summary>Include refs/tags in references results</summary>
		/// <param name="tags"></param>
		public virtual void SetTags(bool tags)
		{
			this.tags = tags;
		}

		/// <summary>The full path of git-upload-pack on the remote host</summary>
		/// <param name="uploadPack"></param>
		public virtual void SetUploadPack(string uploadPack)
		{
			this.uploadPack = uploadPack;
		}

		/// <summary>
		/// Executes the
		/// <code>LsRemote</code>
		/// command with all the options and parameters
		/// collected by the setter methods (e.g.
		/// <see cref="SetHeads(bool)">SetHeads(bool)</see>
		/// ) of this
		/// class. Each instance of this class should only be used for one invocation
		/// of the command. Don't call this method twice on an instance.
		/// </summary>
		/// <returns>a collection of references in the remote repository</returns>
		/// <exception cref="NGit.Api.Errors.InvalidRemoteException">when called with an invalid remote uri
		/// 	</exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException">
		/// a low-level exception of JGit has occurred. The original
		/// exception can be retrieved by calling
		/// <see cref="System.Exception.InnerException()">System.Exception.InnerException()</see>
		/// .
		/// </exception>
		/// <exception cref="NGit.Api.Errors.GitAPIException"></exception>
		public override ICollection<Ref> Call()
		{
			CheckCallable();
			NGit.Transport.Transport transport = null;
			FetchConnection fc = null;
			try
			{
				transport = NGit.Transport.Transport.Open(repo, remote);
				transport.SetOptionUploadPack(uploadPack);
				Configure(transport);
				ICollection<RefSpec> refSpecs = new AList<RefSpec>(1);
				if (tags)
				{
					refSpecs.AddItem(new RefSpec("refs/tags/*:refs/remotes/origin/tags/*"));
				}
				if (heads)
				{
					refSpecs.AddItem(new RefSpec("refs/heads/*:refs/remotes/origin/*"));
				}
				ICollection<Ref> refs;
				IDictionary<string, Ref> refmap = new Dictionary<string, Ref>();
				fc = transport.OpenFetch();
				refs = fc.GetRefs();
				if (refSpecs.IsEmpty())
				{
					foreach (Ref r in refs)
					{
						refmap.Put(r.GetName(), r);
					}
				}
				else
				{
					foreach (Ref r_1 in refs)
					{
						foreach (RefSpec rs in refSpecs)
						{
							if (rs.MatchSource(r_1))
							{
								refmap.Put(r_1.GetName(), r_1);
								break;
							}
						}
					}
				}
				return refmap.Values;
			}
			catch (URISyntaxException)
			{
				throw new InvalidRemoteException(MessageFormat.Format(JGitText.Get().invalidRemote
					, remote));
			}
			catch (NGit.Errors.NotSupportedException e)
			{
				throw new JGitInternalException(JGitText.Get().exceptionCaughtDuringExecutionOfLsRemoteCommand
					, e);
			}
			catch (TransportException e)
			{
				throw new TransportException(JGitText.Get().exceptionCaughtDuringExecutionOfLsRemoteCommand
					, e);
			}
			finally
			{
				if (fc != null)
				{
					fc.Close();
				}
				if (transport != null)
				{
					transport.Close();
				}
			}
		}
	}
}
