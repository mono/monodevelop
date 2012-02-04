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
using System.Reflection;
using System.Security;
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	internal class FS_POSIX_Java6 : FS_POSIX
	{
		private static readonly MethodInfo canExecute;

		private static readonly MethodInfo setExecute;

		static FS_POSIX_Java6()
		{
			canExecute = NeedMethod(typeof(FilePath), "canExecute");
			setExecute = NeedMethod(typeof(FilePath), "setExecutable", typeof(bool));
		}

		internal static bool HasExecute()
		{
			return canExecute != null && setExecute != null;
		}

		private static MethodInfo NeedMethod(Type on, string name, params Type[]
			 args)
		{
			try
			{
				return on.GetMethod(name, args);
			}
			catch (SecurityException)
			{
				return null;
			}
			catch (NoSuchMethodException)
			{
				return null;
			}
		}

		public FS_POSIX_Java6() : base()
		{
		}

		protected internal FS_POSIX_Java6(FS src) : base(src)
		{
		}

		public override FS NewInstance()
		{
			return new NGit.Util.FS_POSIX_Java6(this);
		}

		public override bool SupportsExecute()
		{
			return true;
		}

		public override bool CanExecute(FilePath f)
		{
			try
			{
				object r = canExecute.Invoke(f, (object[])null);
				return ((bool)r);
			}
			catch (ArgumentException e)
			{
				throw new Error(e);
			}
			catch (MemberAccessException e)
			{
				throw new Error(e);
			}
			catch (TargetInvocationException e)
			{
				throw new Error(e);
			}
		}

		public override bool SetExecute(FilePath f, bool canExec)
		{
			try
			{
				object r;
				r = setExecute.Invoke(f, new object[] { Sharpen.Extensions.ValueOf(canExec) });
				return ((bool)r);
			}
			catch (ArgumentException e)
			{
				throw new Error(e);
			}
			catch (MemberAccessException e)
			{
				throw new Error(e);
			}
			catch (TargetInvocationException e)
			{
				throw new Error(e);
			}
		}

		public override bool RetryFailedLockFileCommit()
		{
			return false;
		}
	}
}
