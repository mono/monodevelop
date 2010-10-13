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
using NGit;
using NGit.Ignore;
using Sharpen;

namespace NGit.Ignore
{
	/// <summary>Represents a bundle of ignore rules inherited from a base directory.</summary>
	/// <remarks>
	/// Represents a bundle of ignore rules inherited from a base directory.
	/// This class is not thread safe, it maintains state about the last match.
	/// </remarks>
	public class IgnoreNode
	{
		/// <summary>
		/// Result from
		/// <see cref="IsIgnored(string, bool)">IsIgnored(string, bool)</see>
		/// .
		/// </summary>
		public enum MatchResult
		{
			NOT_IGNORED,
			IGNORED,
			CHECK_PARENT
		}

		/// <summary>The rules that have been parsed into this node.</summary>
		/// <remarks>The rules that have been parsed into this node.</remarks>
		private readonly IList<IgnoreRule> rules;

		/// <summary>Create an empty ignore node with no rules.</summary>
		/// <remarks>Create an empty ignore node with no rules.</remarks>
		public IgnoreNode()
		{
			rules = new AList<IgnoreRule>();
		}

		/// <summary>Create an ignore node with given rules.</summary>
		/// <remarks>Create an ignore node with given rules.</remarks>
		/// <param name="rules">list of rules.</param>
		public IgnoreNode(IList<IgnoreRule> rules)
		{
			this.rules = rules;
		}

		/// <summary>Parse files according to gitignore standards.</summary>
		/// <remarks>Parse files according to gitignore standards.</remarks>
		/// <param name="in">
		/// input stream holding the standard ignore format. The caller is
		/// responsible for closing the stream.
		/// </param>
		/// <exception cref="System.IO.IOException">Error thrown when reading an ignore file.
		/// 	</exception>
		public virtual void Parse(InputStream @in)
		{
			BufferedReader br = AsReader(@in);
			string txt;
			while ((txt = br.ReadLine()) != null)
			{
				txt = txt.Trim();
				if (txt.Length > 0 && !txt.StartsWith("#"))
				{
					rules.AddItem(new IgnoreRule(txt));
				}
			}
		}

		private static BufferedReader AsReader(InputStream @in)
		{
			return new BufferedReader(new InputStreamReader(@in, Constants.CHARSET));
		}

		/// <returns>list of all ignore rules held by this node.</returns>
		public virtual IList<IgnoreRule> GetRules()
		{
			return Sharpen.Collections.UnmodifiableList(rules);
		}

		/// <summary>Determine if an entry path matches an ignore rule.</summary>
		/// <remarks>Determine if an entry path matches an ignore rule.</remarks>
		/// <param name="entryPath">
		/// the path to test. The path must be relative to this ignore
		/// node's own repository path, and in repository path format
		/// (uses '/' and not '\').
		/// </param>
		/// <param name="isDirectory">true if the target item is a directory.</param>
		/// <returns>status of the path.</returns>
		public virtual IgnoreNode.MatchResult IsIgnored(string entryPath, bool isDirectory
			)
		{
			if (rules.IsEmpty())
			{
				return IgnoreNode.MatchResult.CHECK_PARENT;
			}
			// Parse rules in the reverse order that they were read
			for (int i = rules.Count - 1; i > -1; i--)
			{
				IgnoreRule rule = rules[i];
				if (rule.IsMatch(entryPath, isDirectory))
				{
					if (rule.GetResult())
					{
						return IgnoreNode.MatchResult.IGNORED;
					}
					else
					{
						return IgnoreNode.MatchResult.NOT_IGNORED;
					}
				}
			}
			return IgnoreNode.MatchResult.CHECK_PARENT;
		}
	}
}
