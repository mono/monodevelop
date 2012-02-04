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
using NGit.Events;
using Sharpen;

namespace NGit.Events
{
	/// <summary>
	/// Manages a thread-safe list of
	/// <see cref="RepositoryListener">RepositoryListener</see>
	/// s.
	/// </summary>
	public class ListenerList
	{
		private readonly ConcurrentMap<Type, CopyOnWriteArrayList<ListenerHandle>> lists = 
			new ConcurrentHashMap<Type, CopyOnWriteArrayList<ListenerHandle>>();

		/// <summary>Register an IndexChangedListener.</summary>
		/// <remarks>Register an IndexChangedListener.</remarks>
		/// <param name="listener">the listener implementation.</param>
		/// <returns>handle to later remove the listener.</returns>
		public virtual ListenerHandle AddIndexChangedListener(IndexChangedListener listener
			)
		{
			return AddListener<IndexChangedListener>(listener);
		}

		/// <summary>Register a RefsChangedListener.</summary>
		/// <remarks>Register a RefsChangedListener.</remarks>
		/// <param name="listener">the listener implementation.</param>
		/// <returns>handle to later remove the listener.</returns>
		public virtual ListenerHandle AddRefsChangedListener(RefsChangedListener listener
			)
		{
			return AddListener<RefsChangedListener>(listener);
		}

		/// <summary>Register a ConfigChangedListener.</summary>
		/// <remarks>Register a ConfigChangedListener.</remarks>
		/// <param name="listener">the listener implementation.</param>
		/// <returns>handle to later remove the listener.</returns>
		public virtual ListenerHandle AddConfigChangedListener(ConfigChangedListener listener
			)
		{
			return AddListener<ConfigChangedListener>(listener);
		}

		/// <summary>Add a listener to the list.</summary>
		/// <remarks>Add a listener to the list.</remarks>
		/// <?></?>
		/// <param name="type">type of listener being registered.</param>
		/// <param name="listener">the listener instance.</param>
		/// <returns>a handle to later remove the registration, if desired.</returns>
		public virtual ListenerHandle AddListener<T>(T listener) where T:RepositoryListener
		{
			System.Type type = typeof(T);
			ListenerHandle handle = new ListenerHandle(this, type, listener);
			Add(handle);
			return handle;
		}

		/// <summary>Dispatch an event to all interested listeners.</summary>
		/// <remarks>
		/// Dispatch an event to all interested listeners.
		/// <p>
		/// Listeners are selected by the type of listener the event delivers to.
		/// </remarks>
		/// <param name="event">the event to deliver.</param>
		public virtual void Dispatch(RepositoryEvent @event)
		{
			IList<ListenerHandle> list = lists.Get(@event.GetListenerType());
			if (list != null)
			{
				foreach (ListenerHandle handle in list)
				{
					@event.Dispatch(handle.listener);
				}
			}
		}

		private void Add(ListenerHandle handle)
		{
			IList<ListenerHandle> list = lists.Get(handle.type);
			if (list == null)
			{
				CopyOnWriteArrayList<ListenerHandle> newList;
				newList = new CopyOnWriteArrayList<ListenerHandle>();
				list = lists.PutIfAbsent(handle.type, newList);
				if (list == null)
				{
					list = newList;
				}
			}
			list.AddItem(handle);
		}

		internal virtual void Remove(ListenerHandle handle)
		{
			IList<ListenerHandle> list = lists.Get(handle.type);
			if (list != null)
			{
				list.Remove(handle);
			}
		}
	}
}
