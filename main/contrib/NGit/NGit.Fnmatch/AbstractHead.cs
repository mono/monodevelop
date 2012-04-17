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
using NGit.Fnmatch;
using NGit.Internal;
using Sharpen;

namespace NGit.Fnmatch
{
	internal abstract class AbstractHead : Head
	{
		private IList<Head> newHeads = null;

		private readonly bool star;

		protected internal abstract bool Matches(char c);

		internal AbstractHead(bool star)
		{
			this.star = star;
		}

		/// <param name="newHeads">
		/// a list of
		/// <see cref="Head">Head</see>
		/// s which will not be modified.
		/// </param>
		public void SetNewHeads(IList<Head> newHeads)
		{
			if (this.newHeads != null)
			{
				throw new InvalidOperationException(JGitText.Get().propertyIsAlreadyNonNull);
			}
			this.newHeads = newHeads;
		}

		public virtual IList<Head> GetNextHeads(char c)
		{
			if (Matches(c))
			{
				return newHeads;
			}
			else
			{
				return FileNameMatcher.EMPTY_HEAD_LIST;
			}
		}

		internal virtual bool IsStar()
		{
			return star;
		}
	}
}
