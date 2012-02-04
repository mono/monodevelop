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
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>
	/// <see cref="PreReceiveHook">PreReceiveHook</see>
	/// that delegates to a list of other hooks.
	/// <p>
	/// Hooks are run in the order passed to the constructor.
	/// </summary>
	public class PreReceiveHookChain : PreReceiveHook
	{
		private readonly PreReceiveHook[] hooks;

		private readonly int count;

		/// <summary>Create a new hook chaining the given hooks together.</summary>
		/// <remarks>Create a new hook chaining the given hooks together.</remarks>
		/// <param name="hooks">hooks to execute, in order.</param>
		/// <returns>a new hook chain of the given hooks.</returns>
		public static PreReceiveHook NewChain<_T0>(IList<_T0> hooks) where _T0:PreReceiveHook
		{
			PreReceiveHook[] newHooks = new PreReceiveHook[hooks.Count];
			int i = 0;
			foreach (PreReceiveHook hook in hooks)
			{
				if (hook != PreReceiveHook.NULL)
				{
					newHooks[i++] = hook;
				}
			}
			if (i == 0)
			{
				return PreReceiveHook.NULL;
			}
			else
			{
				if (i == 1)
				{
					return newHooks[0];
				}
				else
				{
					return new NGit.Transport.PreReceiveHookChain(newHooks, i);
				}
			}
		}

		public override void OnPreReceive(ReceivePack rp, ICollection<ReceiveCommand> commands
			)
		{
			for (int i = 0; i < count; i++)
			{
				hooks[i].OnPreReceive(rp, commands);
			}
		}

		private PreReceiveHookChain(PreReceiveHook[] hooks, int count)
		{
			this.hooks = hooks;
			this.count = count;
		}
	}
}
