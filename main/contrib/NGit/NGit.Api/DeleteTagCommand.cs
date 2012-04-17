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
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Internal;
using Sharpen;

namespace NGit.Api
{
	/// <summary>Used to delete one or several tags.</summary>
	/// <remarks>
	/// Used to delete one or several tags.
	/// The result of
	/// <see cref="Call()">Call()</see>
	/// is a list with the (full) names of the deleted
	/// tags.
	/// </remarks>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-tag.html"
	/// *      >Git documentation about Tag</a></seealso>
	public class DeleteTagCommand : GitCommand<IList<string>>
	{
		private readonly ICollection<string> tags = new HashSet<string>();

		/// <param name="repo"></param>
		protected internal DeleteTagCommand(Repository repo) : base(repo)
		{
		}

		/// <exception cref="NGit.Api.Errors.JGitInternalException">when trying to delete a tag that doesn't exist
		/// 	</exception>
		/// <returns>the list with the full names of the deleted tags</returns>
		public override IList<string> Call()
		{
			CheckCallable();
			IList<string> result = new AList<string>();
			if (tags.IsEmpty())
			{
				return result;
			}
			try
			{
				SetCallable(false);
				foreach (string tagName in tags)
				{
					if (tagName == null)
					{
						continue;
					}
					Ref currentRef = repo.GetRef(tagName);
					if (currentRef == null)
					{
						continue;
					}
					string fullName = currentRef.GetName();
					RefUpdate update = repo.UpdateRef(fullName);
					update.SetForceUpdate(true);
					RefUpdate.Result deleteResult = update.Delete();
					bool ok = true;
					switch (deleteResult)
					{
						case RefUpdate.Result.IO_FAILURE:
						case RefUpdate.Result.LOCK_FAILURE:
						case RefUpdate.Result.REJECTED:
						{
							ok = false;
							break;
						}

						default:
						{
							break;
							break;
						}
					}
					if (ok)
					{
						result.AddItem(fullName);
					}
					else
					{
						throw new JGitInternalException(MessageFormat.Format(JGitText.Get().deleteTagUnexpectedResult
							, deleteResult.ToString()));
					}
				}
				return result;
			}
			catch (IOException ioe)
			{
				throw new JGitInternalException(ioe.Message, ioe);
			}
		}

		/// <param name="tags">
		/// the names of the tags to delete; if not set, this will do
		/// nothing; invalid tag names will simply be ignored
		/// </param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.DeleteTagCommand SetTags(params string[] tags)
		{
			CheckCallable();
			this.tags.Clear();
			foreach (string tagName in tags)
			{
				this.tags.AddItem(tagName);
			}
			return this;
		}
	}
}
