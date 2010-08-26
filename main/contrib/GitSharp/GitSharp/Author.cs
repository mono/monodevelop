/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.Linq;
using System.Text;

namespace GitSharp
{
	/// <summary>
	/// Represents the Author or Committer of a Commit.
	/// </summary>
	public class Author
	{

		/// <summary>
		/// Creates an uninitialized Author. You may use the object initializer syntax with this constructor, i.e. new Author { Name="henon", EmailAddress="henon@gitsharp.com" }
		/// </summary>
		public Author() { }

		/// <summary>
		/// Creates an Author.
		/// </summary>
		public Author(string name, string email)
		{
			Name = name;
			EmailAddress = email;
		}

		public string Name { get; set; }

		public string EmailAddress { get; set; }

		/// <summary>
		/// Preconfigured anonymous Author, which may be used by GitSharp if no Author has been configured.
		/// </summary>
		public static Author Anonymous
		{
			get
			{
				return new Author("anonymous", "anonymous@(none).com");
			}
		}

		public static Author GetDefaultAuthor(Repository repo)
		{
			return new Author(repo.Config["user.name"], repo.Config["user.email"]);
		}
	}
}
