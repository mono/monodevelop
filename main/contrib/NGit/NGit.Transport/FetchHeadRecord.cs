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
using NGit;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	internal class FetchHeadRecord
	{
		internal ObjectId newValue;

		internal bool notForMerge;

		internal string sourceName;

		internal URIish sourceURI;

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Write(TextWriter pw)
		{
			string type;
			string name;
			if (sourceName.StartsWith(Constants.R_HEADS))
			{
				type = "branch";
				name = Sharpen.Runtime.Substring(sourceName, Constants.R_HEADS.Length);
			}
			else
			{
				if (sourceName.StartsWith(Constants.R_TAGS))
				{
					type = "tag";
					name = Sharpen.Runtime.Substring(sourceName, Constants.R_TAGS.Length);
				}
				else
				{
					if (sourceName.StartsWith(Constants.R_REMOTES))
					{
						type = "remote branch";
						name = Sharpen.Runtime.Substring(sourceName, Constants.R_REMOTES.Length);
					}
					else
					{
						type = string.Empty;
						name = sourceName;
					}
				}
			}
			pw.Write(newValue.Name);
			pw.Write('\t');
			if (notForMerge)
			{
				pw.Write("not-for-merge");
			}
			pw.Write('\t');
			pw.Write(type);
			pw.Write(" '");
			pw.Write(name);
			pw.Write("' of ");
			pw.Write(sourceURI.ToString());
			pw.Write("\n");
		}
	}
}
