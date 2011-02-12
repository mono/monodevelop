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
using Sharpen;

namespace NGit
{
	/// <summary>Mutable builder to construct a commit recording the state of a project.</summary>
	/// <remarks>
	/// Mutable builder to construct a commit recording the state of a project.
	/// Applications should use this object when they need to manually construct a
	/// commit and want precise control over its fields. For a higher level interface
	/// see
	/// <see cref="NGit.Api.CommitCommand">NGit.Api.CommitCommand</see>
	/// .
	/// To read a commit object, construct a
	/// <see cref="NGit.Revwalk.RevWalk">NGit.Revwalk.RevWalk</see>
	/// and obtain a
	/// <see cref="NGit.Revwalk.RevCommit">NGit.Revwalk.RevCommit</see>
	/// instance by calling
	/// <see cref="NGit.Revwalk.RevWalk.ParseCommit(AnyObjectId)">NGit.Revwalk.RevWalk.ParseCommit(AnyObjectId)
	/// 	</see>
	/// .
	/// </remarks>
	public class CommitBuilder
	{
		private static readonly ObjectId[] EMPTY_OBJECTID_LIST = new ObjectId[0];

		private static readonly byte[] htree = Constants.EncodeASCII("tree");

		private static readonly byte[] hparent = Constants.EncodeASCII("parent");

		private static readonly byte[] hauthor = Constants.EncodeASCII("author");

		private static readonly byte[] hcommitter = Constants.EncodeASCII("committer");

		private static readonly byte[] hencoding = Constants.EncodeASCII("encoding");

		private ObjectId treeId;

		private ObjectId[] parentIds;

		private PersonIdent author;

		private PersonIdent committer;

		private string message;

		private System.Text.Encoding encoding;

		/// <summary>Initialize an empty commit.</summary>
		/// <remarks>Initialize an empty commit.</remarks>
		public CommitBuilder()
		{
			parentIds = EMPTY_OBJECTID_LIST;
			encoding = Constants.CHARSET;
		}

		/// <returns>id of the root tree listing this commit's snapshot.</returns>
		/// <summary>Set the tree id for this commit object</summary>
		/// <value>the tree identity.</value>
		public virtual ObjectId TreeId
		{
			get
			{
				return treeId;
			}
			set
			{
				ObjectId id = value;
				treeId = id.Copy();
			}
		}

		/// <returns>the author of this commit (who wrote it).</returns>
		/// <summary>Set the author (name, email address, and date) of who wrote the commit.</summary>
		/// <remarks>Set the author (name, email address, and date) of who wrote the commit.</remarks>
		/// <value>the new author. Should not be null.</value>
		public virtual PersonIdent Author
		{
			get
			{
				return author;
			}
			set
			{
				PersonIdent newAuthor = value;
				author = newAuthor;
			}
		}

		/// <returns>the committer and commit time for this object.</returns>
		/// <summary>Set the committer and commit time for this object</summary>
		/// <value>the committer information. Should not be null.</value>
		public virtual PersonIdent Committer
		{
			get
			{
				return committer;
			}
			set
			{
				PersonIdent newCommitter = value;
				committer = newCommitter;
			}
		}

		/// <returns>the ancestors of this commit. Never null.</returns>
		public virtual ObjectId[] ParentIds
		{
			get
			{
				return parentIds;
			}
		}

		/// <summary>Set the parent of this commit.</summary>
		/// <remarks>Set the parent of this commit.</remarks>
		/// <param name="newParent">the single parent for the commit.</param>
		public virtual void SetParentId(AnyObjectId newParent)
		{
			parentIds = new ObjectId[] { newParent.Copy() };
		}

		/// <summary>Set the parents of this commit.</summary>
		/// <remarks>Set the parents of this commit.</remarks>
		/// <param name="parent1">
		/// the first parent of this commit. Typically this is the current
		/// value of the
		/// <code>HEAD</code>
		/// reference and is thus the current
		/// branch's position in history.
		/// </param>
		/// <param name="parent2">
		/// the second parent of this merge commit. Usually this is the
		/// branch being merged into the current branch.
		/// </param>
		public virtual void SetParentIds(AnyObjectId parent1, AnyObjectId parent2)
		{
			parentIds = new ObjectId[] { parent1.Copy(), parent2.Copy() };
		}

		/// <summary>Set the parents of this commit.</summary>
		/// <remarks>Set the parents of this commit.</remarks>
		/// <param name="newParents">the entire list of parents for this commit.</param>
		public virtual void SetParentIds(params ObjectId[] newParents)
		{
			parentIds = new ObjectId[newParents.Length];
			for (int i = 0; i < newParents.Length; i++)
			{
				parentIds[i] = newParents[i].Copy();
			}
		}

		/// <summary>Set the parents of this commit.</summary>
		/// <remarks>Set the parents of this commit.</remarks>
		/// <param name="newParents">the entire list of parents for this commit.</param>
		public virtual void SetParentIds<_T0>(IList<_T0> newParents) where _T0:AnyObjectId
		{
			parentIds = new ObjectId[newParents.Count];
			for (int i = 0; i < newParents.Count; i++)
			{
				parentIds[i] = newParents[i].Copy();
			}
		}

		/// <summary>Add a parent onto the end of the parent list.</summary>
		/// <remarks>Add a parent onto the end of the parent list.</remarks>
		/// <param name="additionalParent">new parent to add onto the end of the current parent list.
		/// 	</param>
		public virtual void AddParentId(AnyObjectId additionalParent)
		{
			if (parentIds.Length == 0)
			{
				SetParentId(additionalParent);
			}
			else
			{
				ObjectId[] newParents = new ObjectId[parentIds.Length + 1];
				for (int i = 0; i < parentIds.Length; i++)
				{
					newParents[i] = parentIds[i];
				}
				newParents[parentIds.Length] = additionalParent.Copy();
				parentIds = newParents;
			}
		}

		/// <returns>the complete commit message.</returns>
		/// <summary>Set the commit message.</summary>
		/// <remarks>Set the commit message.</remarks>
		/// <value>the commit message. Should not be null.</value>
		public virtual string Message
		{
			get
			{
				return message;
			}
			set
			{
				string newMessage = value;
				message = newMessage;
			}
		}

		/// <summary>Set the encoding for the commit information</summary>
		/// <param name="encodingName">
		/// the encoding name. See
		/// <see cref="Sharpen.Extensions.GetEncoding(string)">Sharpen.Extensions.GetEncoding(string)
		/// 	</see>
		/// .
		/// </param>
		public virtual void SetEncoding(string encodingName)
		{
			encoding = Sharpen.Extensions.GetEncoding(encodingName);
		}

		/// <summary>Set the encoding for the commit information</summary>
		/// <param name="enc">the encoding to use.</param>
		public virtual void SetEncoding(System.Text.Encoding enc)
		{
			encoding = enc;
		}

		/// <returns>the encoding that should be used for the commit message text.</returns>
		public virtual System.Text.Encoding Encoding
		{
			get
			{
				return encoding;
			}
		}

		/// <summary>Format this builder's state as a commit object.</summary>
		/// <remarks>Format this builder's state as a commit object.</remarks>
		/// <returns>
		/// this object in the canonical commit format, suitable for storage
		/// in a repository.
		/// </returns>
		/// <exception cref="Sharpen.UnsupportedEncodingException">
		/// the encoding specified by
		/// <see cref="Encoding()">Encoding()</see>
		/// is not
		/// supported by this Java runtime.
		/// </exception>
		public virtual byte[] Build()
		{
			ByteArrayOutputStream os = new ByteArrayOutputStream();
			OutputStreamWriter w = new OutputStreamWriter(os, Encoding);
			try
			{
				os.Write(htree);
				os.Write(' ');
				TreeId.CopyTo(os);
				os.Write('\n');
				foreach (ObjectId p in ParentIds)
				{
					os.Write(hparent);
					os.Write(' ');
					p.CopyTo(os);
					os.Write('\n');
				}
				os.Write(hauthor);
				os.Write(' ');
				w.Write(Author.ToExternalString());
				w.Flush();
				os.Write('\n');
				os.Write(hcommitter);
				os.Write(' ');
				w.Write(Committer.ToExternalString());
				w.Flush();
				os.Write('\n');
				if (Encoding != Constants.CHARSET)
				{
					os.Write(hencoding);
					os.Write(' ');
					os.Write(Constants.EncodeASCII(Encoding.Name()));
					os.Write('\n');
				}
				os.Write('\n');
				if (Message != null)
				{
					w.Write(Message);
					w.Flush();
				}
			}
			catch (IOException err)
			{
				// This should never occur, the only way to get it above is
				// for the ByteArrayOutputStream to throw, but it doesn't.
				//
				throw new RuntimeException(err);
			}
			return os.ToByteArray();
		}

		/// <summary>Format this builder's state as a commit object.</summary>
		/// <remarks>Format this builder's state as a commit object.</remarks>
		/// <returns>
		/// this object in the canonical commit format, suitable for storage
		/// in a repository.
		/// </returns>
		/// <exception cref="Sharpen.UnsupportedEncodingException">
		/// the encoding specified by
		/// <see cref="Encoding()">Encoding()</see>
		/// is not
		/// supported by this Java runtime.
		/// </exception>
		public virtual byte[] ToByteArray()
		{
			return Build();
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			r.Append("Commit");
			r.Append("={\n");
			r.Append("tree ");
			r.Append(treeId != null ? treeId.Name : "NOT_SET");
			r.Append("\n");
			foreach (ObjectId p in parentIds)
			{
				r.Append("parent ");
				r.Append(p.Name);
				r.Append("\n");
			}
			r.Append("author ");
			r.Append(author != null ? author.ToString() : "NOT_SET");
			r.Append("\n");
			r.Append("committer ");
			r.Append(committer != null ? committer.ToString() : "NOT_SET");
			r.Append("\n");
			if (encoding != null && encoding != Constants.CHARSET)
			{
				r.Append("encoding ");
				r.Append(encoding.Name());
				r.Append("\n");
			}
			r.Append("\n");
			r.Append(message != null ? message : string.Empty);
			r.Append("}");
			return r.ToString();
		}
	}
}
