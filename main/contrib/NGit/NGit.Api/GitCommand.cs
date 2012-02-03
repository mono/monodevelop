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

namespace NGit.Api
{
	public interface GitCommand
	{
	}
	
	/// <summary>
	/// Common superclass of all commands in the package
	/// <code>org.eclipse.jgit.api</code>
	/// <p>
	/// This class ensures that all commands fulfill the
	/// <see cref="Sharpen.Callable{V}">Sharpen.Callable&lt;V&gt;</see>
	/// interface.
	/// It also has a property
	/// <see cref="GitCommand{T}.repo">GitCommand&lt;T&gt;.repo</see>
	/// holding a reference to the git
	/// <see cref="NGit.Repository">NGit.Repository</see>
	/// this command should work with.
	/// <p>
	/// Finally this class stores a state telling whether it is allowed to call
	/// <see cref="Sharpen.Callable{V}.Call()">Sharpen.Callable&lt;V&gt;.Call()</see>
	/// on this instance. Instances of
	/// <see cref="GitCommand{T}">GitCommand&lt;T&gt;</see>
	/// can only be
	/// used for one single successful call to
	/// <see cref="Sharpen.Callable{V}.Call()">Sharpen.Callable&lt;V&gt;.Call()</see>
	/// . Afterwards this
	/// instance may not be used anymore to set/modify any properties or to call
	/// <see cref="Sharpen.Callable{V}.Call()">Sharpen.Callable&lt;V&gt;.Call()</see>
	/// again. This is achieved by setting the
	/// <see cref="GitCommand{T}.callable">GitCommand&lt;T&gt;.callable</see>
	/// property to false after the successful execution of
	/// <see cref="Sharpen.Callable{V}.Call()">Sharpen.Callable&lt;V&gt;.Call()</see>
	/// and to
	/// check the state (by calling
	/// <see cref="GitCommand{T}.CheckCallable()">GitCommand&lt;T&gt;.CheckCallable()</see>
	/// ) before setting of
	/// properties and inside
	/// <see cref="Sharpen.Callable{V}.Call()">Sharpen.Callable&lt;V&gt;.Call()</see>
	/// .
	/// </summary>
	/// <?></?>
	public abstract class GitCommand<T> : Callable<T>, GitCommand
	{
		/// <summary>The repository this command is working with</summary>
		protected internal readonly Repository repo;

		/// <summary>
		/// a state which tells whether it is allowed to call
		/// <see cref="Sharpen.Callable{V}.Call()">Sharpen.Callable&lt;V&gt;.Call()</see>
		/// on this
		/// instance.
		/// </summary>
		private bool callable = true;

		/// <summary>Creates a new command which interacts with a single repository</summary>
		/// <param name="repo">
		/// the
		/// <see cref="NGit.Repository">NGit.Repository</see>
		/// this command should interact with
		/// </param>
		protected internal GitCommand(Repository repo)
		{
			this.repo = repo;
		}

		/// <returns>
		/// the
		/// <see cref="NGit.Repository">NGit.Repository</see>
		/// this command is interacting with
		/// </returns>
		public virtual Repository GetRepository()
		{
			return repo;
		}

		/// <summary>
		/// Set's the state which tells whether it is allowed to call
		/// <see cref="Sharpen.Callable{V}.Call()">Sharpen.Callable&lt;V&gt;.Call()</see>
		/// on this instance.
		/// <see cref="GitCommand{T}.CheckCallable()">GitCommand&lt;T&gt;.CheckCallable()</see>
		/// will throw an exception when
		/// called and this property is set to
		/// <code>false</code>
		/// </summary>
		/// <param name="callable">
		/// if <code>true</code> it is allowed to call
		/// <see cref="Sharpen.Callable{V}.Call()">Sharpen.Callable&lt;V&gt;.Call()</see>
		/// on
		/// this instance.
		/// </param>
		protected internal virtual void SetCallable(bool callable)
		{
			this.callable = callable;
		}

		/// <summary>
		/// Checks that the property
		/// <see cref="GitCommand{T}.callable">GitCommand&lt;T&gt;.callable</see>
		/// is
		/// <code>true</code>
		/// . If not then
		/// an
		/// <see cref="System.InvalidOperationException">System.InvalidOperationException</see>
		/// is thrown
		/// </summary>
		/// <exception cref="System.InvalidOperationException">
		/// when this method is called and the property
		/// <see cref="GitCommand{T}.callable">GitCommand&lt;T&gt;.callable</see>
		/// is
		/// <code>false</code>
		/// </exception>
		protected internal virtual void CheckCallable()
		{
			if (!callable)
			{
				throw new InvalidOperationException(MessageFormat.Format(JGitText.Get().commandWasCalledInTheWrongState
					, this.GetType().FullName));
			}
		}

		public abstract T Call();
	}
}
