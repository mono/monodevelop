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

using NGit.Storage.Pack;
using Sharpen;

namespace NGit.Storage.Pack
{
	internal class ThreadSafeDeltaCache : DeltaCache
	{
		private readonly ReentrantLock Lock;

		internal ThreadSafeDeltaCache(PackConfig pc) : base(pc)
		{
			Lock = new ReentrantLock();
		}

		internal override bool CanCache(int length, ObjectToPack src, ObjectToPack res)
		{
			Lock.Lock();
			try
			{
				return base.CanCache(length, src, res);
			}
			finally
			{
				Lock.Unlock();
			}
		}

		internal override void Credit(int reservedSize)
		{
			Lock.Lock();
			try
			{
				base.Credit(reservedSize);
			}
			finally
			{
				Lock.Unlock();
			}
		}

		internal override DeltaCache.Ref Cache(byte[] data, int actLen, int reservedSize)
		{
			data = Resize(data, actLen);
			Lock.Lock();
			try
			{
				return base.Cache(data, actLen, reservedSize);
			}
			finally
			{
				Lock.Unlock();
			}
		}
	}
}
