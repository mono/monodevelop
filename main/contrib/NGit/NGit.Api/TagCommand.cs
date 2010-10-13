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
using System.IO;
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// A class used to execute a
	/// <code>Tag</code>
	/// command. It has setters for all
	/// supported options and arguments of this command and a
	/// <see cref="Call()">Call()</see>
	/// method
	/// to finally execute the command.
	/// </summary>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-tag.html"
	/// *      >Git documentation about Tag</a></seealso>
	public class TagCommand : GitCommand<RevTag>
	{
		private RevObject id;

		private string name;

		private string message;

		private PersonIdent tagger;

		private bool signed;

		private bool forceUpdate;

		/// <param name="repo"></param>
		protected internal TagCommand(Repository repo) : base(repo)
		{
		}

		/// <summary>
		/// Executes the
		/// <code>tag</code>
		/// command with all the options and parameters
		/// collected by the setter methods of this class. Each instance of this
		/// class should only be used for one invocation of the command (means: one
		/// call to
		/// <see cref="Call()">Call()</see>
		/// )
		/// </summary>
		/// <returns>
		/// a
		/// <see cref="NGit.Revwalk.RevTag">NGit.Revwalk.RevTag</see>
		/// object representing the successful tag
		/// </returns>
		/// <exception cref="NGit.Api.Errors.NoHeadException">when called on a git repo without a HEAD reference
		/// 	</exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException">
		/// a low-level exception of JGit has occurred. The original
		/// exception can be retrieved by calling
		/// <see cref="System.Exception.InnerException()">System.Exception.InnerException()</see>
		/// . Expect only
		/// <code>IOException's</code>
		/// to be wrapped.
		/// </exception>
		/// <exception cref="NGit.Api.Errors.ConcurrentRefUpdateException"></exception>
		/// <exception cref="NGit.Api.Errors.InvalidTagNameException"></exception>
		public override RevTag Call()
		{
			CheckCallable();
			RepositoryState state = repo.GetRepositoryState();
			ProcessOptions(state);
			try
			{
				// create the tag object
				TagBuilder newTag = new TagBuilder();
				newTag.SetTag(name);
				newTag.SetMessage(message);
				newTag.SetTagger(tagger);
				// if no id is set, we should attempt to use HEAD
				if (id == null)
				{
					ObjectId objectId = repo.Resolve(Constants.HEAD + "^{commit}");
					if (objectId == null)
					{
						throw new NoHeadException(JGitText.Get().tagOnRepoWithoutHEADCurrentlyNotSupported
							);
					}
					newTag.SetObjectId(objectId, Constants.OBJ_COMMIT);
				}
				else
				{
					newTag.SetObjectId(id);
				}
				// write the tag object
				ObjectInserter inserter = repo.NewObjectInserter();
				try
				{
					ObjectId tagId = inserter.Insert(newTag);
					inserter.Flush();
					RevWalk revWalk = new RevWalk(repo);
					try
					{
						RevTag revTag = revWalk.ParseTag(newTag.GetTagId());
						string refName = Constants.R_TAGS + newTag.GetTag();
						RefUpdate tagRef = repo.UpdateRef(refName);
						tagRef.SetNewObjectId(tagId);
						tagRef.SetForceUpdate(forceUpdate);
						tagRef.SetRefLogMessage("tagged " + name, false);
						RefUpdate.Result updateResult = tagRef.Update(revWalk);
						switch (updateResult)
						{
							case RefUpdate.Result.NEW:
							case RefUpdate.Result.FORCED:
							{
								return revTag;
							}

							case RefUpdate.Result.LOCK_FAILURE:
							{
								throw new ConcurrentRefUpdateException(JGitText.Get().couldNotLockHEAD, tagRef.GetRef
									(), updateResult);
							}

							default:
							{
								throw new JGitInternalException(MessageFormat.Format(JGitText.Get().updatingRefFailed
									, refName, newTag.ToString(), updateResult));
							}
						}
					}
					finally
					{
						revWalk.Release();
					}
				}
				finally
				{
					inserter.Release();
				}
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().exceptionCaughtDuringExecutionOfTagCommand
					, e);
			}
		}

		/// <summary>Sets default values for not explicitly specified options.</summary>
		/// <remarks>
		/// Sets default values for not explicitly specified options. Then validates
		/// that all required data has been provided.
		/// </remarks>
		/// <param name="state">the state of the repository we are working on</param>
		/// <exception cref="NGit.Api.Errors.InvalidTagNameException">if the tag name is null or invalid
		/// 	</exception>
		/// <exception cref="System.NotSupportedException">if the tag is signed (not supported yet)
		/// 	</exception>
		private void ProcessOptions(RepositoryState state)
		{
			if (tagger == null)
			{
				tagger = new PersonIdent(repo);
			}
			if (name == null || !Repository.IsValidRefName(Constants.R_TAGS + name))
			{
				throw new InvalidTagNameException(MessageFormat.Format(JGitText.Get().tagNameInvalid
					, name == null ? "<null>" : name));
			}
			if (signed)
			{
				throw new NotSupportedException(JGitText.Get().signingNotSupportedOnTag);
			}
		}

		/// <param name="name">
		/// the tag name used for the
		/// <code>tag</code>
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.TagCommand SetName(string name)
		{
			CheckCallable();
			this.name = name;
			return this;
		}

		/// <returns>the tag name used for the <code>tag</code></returns>
		public virtual string GetName()
		{
			return name;
		}

		/// <returns>the tag message used for the <code>tag</code></returns>
		public virtual string GetMessage()
		{
			return message;
		}

		/// <param name="message">
		/// the tag message used for the
		/// <code>tag</code>
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.TagCommand SetMessage(string message)
		{
			CheckCallable();
			this.message = message;
			return this;
		}

		/// <returns>whether the tag is signed</returns>
		public virtual bool IsSigned()
		{
			return signed;
		}

		/// <summary>If set to true the Tag command creates a signed tag object.</summary>
		/// <remarks>
		/// If set to true the Tag command creates a signed tag object. This
		/// corresponds to the parameter -s on the command line.
		/// </remarks>
		/// <param name="signed"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.TagCommand SetSigned(bool signed)
		{
			this.signed = signed;
			return this;
		}

		/// <summary>Sets the tagger of the tag.</summary>
		/// <remarks>
		/// Sets the tagger of the tag. If the tagger is null, a PersonIdent will be
		/// created from the info in the repository.
		/// </remarks>
		/// <param name="tagger"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.TagCommand SetTagger(PersonIdent tagger)
		{
			this.tagger = tagger;
			return this;
		}

		/// <returns>the tagger of the tag</returns>
		public virtual PersonIdent GetTagger()
		{
			return tagger;
		}

		/// <returns>the object id of the tag</returns>
		public virtual RevObject GetObjectId()
		{
			return id;
		}

		/// <summary>Sets the object id of the tag.</summary>
		/// <remarks>
		/// Sets the object id of the tag. If the object id is null, the commit
		/// pointed to from HEAD will be used.
		/// </remarks>
		/// <param name="id"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.TagCommand SetObjectId(RevObject id)
		{
			this.id = id;
			return this;
		}

		/// <returns>is this a force update</returns>
		public virtual bool IsForceUpdate()
		{
			return forceUpdate;
		}

		/// <summary>If set to true the Tag command may replace an existing tag object.</summary>
		/// <remarks>
		/// If set to true the Tag command may replace an existing tag object. This
		/// corresponds to the parameter -f on the command line.
		/// </remarks>
		/// <param name="forceUpdate"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.TagCommand SetForceUpdate(bool forceUpdate)
		{
			this.forceUpdate = forceUpdate;
			return this;
		}
	}
}
