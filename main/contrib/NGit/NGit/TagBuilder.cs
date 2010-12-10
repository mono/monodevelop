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

using System.IO;
using System.Text;
using NGit;
using NGit.Revwalk;
using Sharpen;

namespace NGit
{
	/// <summary>Mutable builder to construct an annotated tag recording a project state.
	/// 	</summary>
	/// <remarks>
	/// Mutable builder to construct an annotated tag recording a project state.
	/// Applications should use this object when they need to manually construct a
	/// tag and want precise control over its fields.
	/// To read a tag object, construct a
	/// <see cref="NGit.Revwalk.RevWalk">NGit.Revwalk.RevWalk</see>
	/// and obtain a
	/// <see cref="NGit.Revwalk.RevTag">NGit.Revwalk.RevTag</see>
	/// instance by calling
	/// <see cref="NGit.Revwalk.RevWalk.ParseTag(AnyObjectId)">NGit.Revwalk.RevWalk.ParseTag(AnyObjectId)
	/// 	</see>
	/// .
	/// </remarks>
	public class TagBuilder
	{
		private ObjectId @object;

		private int type = Constants.OBJ_BAD;

		private string tag;

		private PersonIdent tagger;

		private string message;

		/// <returns>the type of object this tag refers to.</returns>
		public virtual int GetObjectType()
		{
			return type;
		}

		/// <returns>the object this tag refers to.</returns>
		public virtual ObjectId GetObjectId()
		{
			return @object;
		}

		/// <summary>Set the object this tag refers to, and its type.</summary>
		/// <remarks>Set the object this tag refers to, and its type.</remarks>
		/// <param name="obj">the object.</param>
		/// <param name="objType">
		/// the type of
		/// <code>obj</code>
		/// . Must be a valid type code.
		/// </param>
		public virtual void SetObjectId(AnyObjectId obj, int objType)
		{
			@object = obj.Copy();
			type = objType;
		}

		/// <summary>Set the object this tag refers to, and infer its type.</summary>
		/// <remarks>Set the object this tag refers to, and infer its type.</remarks>
		/// <param name="obj">the object the tag will refer to.</param>
		public virtual void SetObjectId(RevObject obj)
		{
			SetObjectId(obj, obj.Type);
		}

		/// <returns>
		/// short name of the tag (no
		/// <code>refs/tags/</code>
		/// prefix).
		/// </returns>
		public virtual string GetTag()
		{
			return tag;
		}

		/// <summary>Set the name of this tag.</summary>
		/// <remarks>Set the name of this tag.</remarks>
		/// <param name="shortName">
		/// new short name of the tag. This short name should not start
		/// with
		/// <code>refs/</code>
		/// as typically a tag is stored under the
		/// reference derived from
		/// <code>"refs/tags/" + getTag()</code>
		/// .
		/// </param>
		public virtual void SetTag(string shortName)
		{
			this.tag = shortName;
		}

		/// <returns>creator of this tag. May be null.</returns>
		public virtual PersonIdent GetTagger()
		{
			return tagger;
		}

		/// <summary>Set the creator of this tag.</summary>
		/// <remarks>Set the creator of this tag.</remarks>
		/// <param name="taggerIdent">the creator. May be null.</param>
		public virtual void SetTagger(PersonIdent taggerIdent)
		{
			tagger = taggerIdent;
		}

		/// <returns>the complete commit message.</returns>
		public virtual string GetMessage()
		{
			return message;
		}

		/// <summary>Set the tag's message.</summary>
		/// <remarks>Set the tag's message.</remarks>
		/// <param name="newMessage">the tag's message.</param>
		public virtual void SetMessage(string newMessage)
		{
			message = newMessage;
		}

		/// <summary>Format this builder's state as an annotated tag object.</summary>
		/// <remarks>Format this builder's state as an annotated tag object.</remarks>
		/// <returns>
		/// this object in the canonical annotated tag format, suitable for
		/// storage in a repository.
		/// </returns>
		public virtual byte[] Build()
		{
			ByteArrayOutputStream os = new ByteArrayOutputStream();
			OutputStreamWriter w = new OutputStreamWriter(os, Constants.CHARSET);
			try
			{
				w.Write("object ");
				GetObjectId().CopyTo(w);
				w.Write('\n');
				w.Write("type ");
				w.Write(Constants.TypeString(GetObjectType()));
				w.Write("\n");
				w.Write("tag ");
				w.Write(GetTag());
				w.Write("\n");
				if (GetTagger() != null)
				{
					w.Write("tagger ");
					w.Write(GetTagger().ToExternalString());
					w.Write('\n');
				}
				w.Write('\n');
				if (GetMessage() != null)
				{
					w.Write(GetMessage());
				}
				w.Close();
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

		/// <summary>Format this builder's state as an annotated tag object.</summary>
		/// <remarks>Format this builder's state as an annotated tag object.</remarks>
		/// <returns>
		/// this object in the canonical annotated tag format, suitable for
		/// storage in a repository.
		/// </returns>
		public virtual byte[] ToByteArray()
		{
			return Build();
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			r.Append("Tag");
			r.Append("={\n");
			r.Append("object ");
			r.Append(@object != null ? @object.Name : "NOT_SET");
			r.Append("\n");
			r.Append("type ");
			r.Append(@object != null ? Constants.TypeString(type) : "NOT_SET");
			r.Append("\n");
			r.Append("tag ");
			r.Append(tag != null ? tag : "NOT_SET");
			r.Append("\n");
			if (tagger != null)
			{
				r.Append("tagger ");
				r.Append(tagger);
				r.Append("\n");
			}
			r.Append("\n");
			r.Append(message != null ? message : string.Empty);
			r.Append("}");
			return r.ToString();
		}
	}
}
