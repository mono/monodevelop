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
using NGit.Storage.File;
using NGit.Storage.Pack;
using Sharpen;

namespace NGit.Storage.File
{
	internal class LocalCachedPack : CachedPack
	{
		private readonly ObjectDirectory odb;

		private readonly ICollection<ObjectId> tips;

		private readonly string[] packNames;

		internal LocalCachedPack(ObjectDirectory odb, ICollection<ObjectId> tips, IList<string
			> packNames)
		{
			this.odb = odb;
			if (tips.Count == 1)
			{
				this.tips = Sharpen.Collections.Singleton(tips.Iterator().Next());
			}
			else
			{
				this.tips = Sharpen.Collections.UnmodifiableSet(tips);
			}
			this.packNames = Sharpen.Collections.ToArray(packNames, new string[packNames.Count
				]);
		}

		public override ICollection<ObjectId> GetTips()
		{
			return tips;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override long GetObjectCount()
		{
			long cnt = 0;
			foreach (string packName in packNames)
			{
				cnt += GetPackFile(packName).GetObjectCount();
			}
			return cnt;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void CopyAsIs(PackOutputStream @out, bool validate, WindowCursor
			 wc)
		{
			foreach (string packName in packNames)
			{
				GetPackFile(packName).CopyPackAsIs(@out, validate, wc);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override ICollection<ObjectId> HasObject<T>(Iterable<T> toFind)
		{
			PackFile[] packs = new PackFile[packNames.Length];
			for (int i = 0; i < packNames.Length; i++)
			{
				packs[i] = GetPackFile(packNames[i]);
			}
			ICollection<ObjectId> have = new HashSet<ObjectId>();
			foreach (ObjectId id in toFind)
			{
				foreach (PackFile pack in packs)
				{
					if (pack.HasObject(id))
					{
						have.AddItem(id);
						break;
					}
				}
			}
			return have;
		}

		/// <exception cref="System.IO.FileNotFoundException"></exception>
		private PackFile GetPackFile(string packName)
		{
			foreach (PackFile pack in odb.GetPacks())
			{
				if (packName.Equals(pack.GetPackName()))
				{
					return pack;
				}
			}
			throw new FileNotFoundException(GetPackFilePath(packName));
		}

		private string GetPackFilePath(string packName)
		{
			FilePath packDir = new FilePath(odb.GetDirectory(), "pack");
			return new FilePath(packDir, "pack-" + packName + ".pack").GetPath();
		}
	}
}
