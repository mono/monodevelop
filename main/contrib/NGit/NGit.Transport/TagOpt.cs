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
using NGit;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Specification of annotated tag behavior during fetch.</summary>
	/// <remarks>Specification of annotated tag behavior during fetch.</remarks>
	public class TagOpt
	{
		/// <summary>Automatically follow tags if we fetch the thing they point at.</summary>
		/// <remarks>
		/// Automatically follow tags if we fetch the thing they point at.
		/// <p>
		/// This is the default behavior and tries to balance the benefit of having
		/// an annotated tag against the cost of possibly objects that are only on
		/// branches we care nothing about. Annotated tags are fetched only if we can
		/// prove that we already have (or will have when the fetch completes) the
		/// object the annotated tag peels (dereferences) to.
		/// </remarks>
		public static NGit.Transport.TagOpt AUTO_FOLLOW = new NGit.Transport.TagOpt(string.Empty
			);

		/// <summary>Never fetch tags, even if we have the thing it points at.</summary>
		/// <remarks>
		/// Never fetch tags, even if we have the thing it points at.
		/// <p>
		/// This option must be requested by the user and always avoids fetching
		/// annotated tags. It is most useful if the location you are fetching from
		/// publishes annotated tags, but you are not interested in the tags and only
		/// want their branches.
		/// </remarks>
		public static NGit.Transport.TagOpt NO_TAGS = new NGit.Transport.TagOpt("--no-tags"
			);

		/// <summary>Always fetch tags, even if we do not have the thing it points at.</summary>
		/// <remarks>
		/// Always fetch tags, even if we do not have the thing it points at.
		/// <p>
		/// Unlike
		/// <see cref="AUTO_FOLLOW">AUTO_FOLLOW</see>
		/// the tag is always obtained. This may cause
		/// hundreds of megabytes of objects to be fetched if the receiving
		/// repository does not yet have the necessary dependencies.
		/// </remarks>
		public static NGit.Transport.TagOpt FETCH_TAGS = new NGit.Transport.TagOpt("--tags"
			);

		private readonly string option;

		private TagOpt(string o)
		{
			option = o;
		}

		/// <summary>Get the command line/configuration file text for this value.</summary>
		/// <remarks>Get the command line/configuration file text for this value.</remarks>
		/// <returns>text that appears in the configuration file to activate this.</returns>
		public virtual string Option()
		{
			return option;
		}

		/// <summary>Convert a command line/configuration file text into a value instance.</summary>
		/// <remarks>Convert a command line/configuration file text into a value instance.</remarks>
		/// <param name="o">the configuration file text value.</param>
		/// <returns>the option that matches the passed parameter.</returns>
		public static NGit.Transport.TagOpt FromOption(string o)
		{
			if (o == null || o.Length == 0)
			{
				return AUTO_FOLLOW;
			}
			foreach (NGit.Transport.TagOpt tagopt in Values())
			{
				if (tagopt.Option().Equals(o))
				{
					return tagopt;
				}
			}
			throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidTagOption, 
				o));
		}

		private static NGit.Transport.TagOpt[] Values()
		{
			return new NGit.Transport.TagOpt[] { AUTO_FOLLOW, NO_TAGS, FETCH_TAGS };
		}
	}
}
