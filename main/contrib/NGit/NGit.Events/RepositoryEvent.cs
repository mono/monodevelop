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
using NGit.Events;
using Sharpen;

namespace NGit.Events
{
	/// <summary>Describes a modification made to a repository.</summary>
	/// <remarks>Describes a modification made to a repository.</remarks>
	/// <?></?>
	public abstract class RepositoryEvent<T> : RepositoryEvent where T:RepositoryListener
	{
		private Repository repository;

		/// <summary>Set the repository this event occurred on.</summary>
		/// <remarks>
		/// Set the repository this event occurred on.
		/// <p>
		/// This method should only be invoked once on each event object, and is
		/// automatically set by
		/// <see cref="NGit.Repository.FireEvent(RepositoryEvent{T})">NGit.Repository.FireEvent(RepositoryEvent&lt;T&gt;)
		/// 	</see>
		/// .
		/// </remarks>
		/// <param name="r">the repository.</param>
		public virtual void SetRepository(Repository r)
		{
			if (repository == null)
			{
				repository = r;
			}
		}

		/// <returns>the repository that was changed.</returns>
		public virtual Repository GetRepository()
		{
			return repository;
		}

		/// <returns>type of listener this event dispatches to.</returns>
		public abstract Type GetListenerType();

		/// <summary>Dispatch this event to the given listener.</summary>
		/// <remarks>Dispatch this event to the given listener.</remarks>
		/// <param name="listener">listener that wants this event.</param>
		public abstract void Dispatch(T listener);

		void RepositoryEvent.Dispatch(RepositoryListener listener)
		{
			this.Dispatch((T) listener);
		}
		
		public override string ToString()
		{
			string type = GetType().Name;
			if (repository == null)
			{
				return type;
			}
			return type + "[" + repository + "]";
		}
	}
	
	public interface RepositoryEvent
	{
	    // Methods
	    void Dispatch(RepositoryListener listener);
	    Type GetListenerType();
	    void SetRepository(Repository r);
	}
}
