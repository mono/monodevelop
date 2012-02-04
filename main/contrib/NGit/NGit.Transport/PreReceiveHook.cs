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
	/// Hook invoked by
	/// <see cref="ReceivePack">ReceivePack</see>
	/// before any updates are executed.
	/// <p>
	/// The hook is called with any commands that are deemed valid after parsing them
	/// from the client and applying the standard receive configuration options to
	/// them:
	/// <ul>
	/// <li><code>receive.denyDenyDeletes</code></li>
	/// <li><code>receive.denyNonFastForwards</code></li>
	/// </ul>
	/// This means the hook will not receive a non-fast-forward update command if
	/// denyNonFastForwards is set to true in the configuration file. To get all
	/// commands within the hook, see
	/// <see cref="ReceivePack.GetAllCommands()">ReceivePack.GetAllCommands()</see>
	/// .
	/// <p>
	/// As the hook is invoked prior to the commands being executed, the hook may
	/// choose to block any command by setting its result status with
	/// <see cref="ReceiveCommand.SetResult(Result)">ReceiveCommand.SetResult(Result)</see>
	/// .
	/// <p>
	/// The hook may also choose to perform the command itself (or merely pretend
	/// that it has performed the command), by setting the result status to
	/// <see cref="Result.OK">Result.OK</see>
	/// .
	/// <p>
	/// Hooks should run quickly, as they block the caller thread and the client
	/// process from completing.
	/// <p>
	/// Hooks may send optional messages back to the client via methods on
	/// <see cref="ReceivePack">ReceivePack</see>
	/// . Implementors should be aware that not all network
	/// transports support this output, so some (or all) messages may simply be
	/// discarded. These messages should be advisory only.
	/// </summary>
	public abstract class PreReceiveHook
	{
		private sealed class _PreReceiveHook_80 : PreReceiveHook
		{
			public _PreReceiveHook_80()
			{
			}

			public override void OnPreReceive(ReceivePack rp, ICollection<ReceiveCommand> commands
				)
			{
			}
		}

		/// <summary>A simple no-op hook.</summary>
		/// <remarks>A simple no-op hook.</remarks>
		public static readonly PreReceiveHook NULL = new _PreReceiveHook_80();

		// Do nothing.
		/// <summary>Invoked just before commands are executed.</summary>
		/// <remarks>
		/// Invoked just before commands are executed.
		/// <p>
		/// See the class description for how this method can impact execution.
		/// </remarks>
		/// <param name="rp">
		/// the process handling the current receive. Hooks may obtain
		/// details about the destination repository through this handle.
		/// </param>
		/// <param name="commands">
		/// unmodifiable set of valid commands still pending execution.
		/// May be the empty set.
		/// </param>
		public abstract void OnPreReceive(ReceivePack rp, ICollection<ReceiveCommand> commands
			);
	}
}
