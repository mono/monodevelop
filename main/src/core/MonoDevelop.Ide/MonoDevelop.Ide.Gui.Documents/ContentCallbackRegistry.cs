//
// ContentCallbackRegistry.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Documents
{
	class ContentCallbackRegistry
	{
		List<ContentCallback> contentCallbacks;
		Func<Type,object> contentGetter;

		class ContentCallback : IDisposable
		{
			public bool IsAddCallback;
			public ContentCallbackRegistry Owner;
			public Type ContentType;
			public Delegate Callback;
			public object Content;

			public void Dispose ()
			{
				Owner.contentCallbacks.Remove (this);
			}
		}

		class ContentAddedOrRemovedCallback : IDisposable
		{
			public IDisposable ContentAddedRegistration;
			public IDisposable ContentRemovedRegistration;

			public void Dispose ()
			{
				ContentAddedRegistration.Dispose ();
				ContentRemovedRegistration.Dispose ();
			}
		}

		public ContentCallbackRegistry (Func<Type,object> contentGetter)
		{
			this.contentGetter = contentGetter;
		}

		public IDisposable RunWhenContentAdded<T> (Action<T> contentCallback)
		{
			if (contentCallbacks == null)
				contentCallbacks = new List<ContentCallback> ();
			var registration = new ContentCallback {
				IsAddCallback = true,
				Owner = this,
				ContentType = typeof (T),
				Callback = contentCallback
			};
			contentCallbacks.Add (registration);
			var content = contentGetter (typeof(T));
			if (content != null)
				InvokeCallback (content, registration);
			return registration;
		}

		public IDisposable RunWhenContentRemoved<T> (Action<T> contentCallback)
		{
			if (contentCallbacks == null)
				contentCallbacks = new List<ContentCallback> ();
			var registration = new ContentCallback {
				IsAddCallback = false,
				Owner = this,
				ContentType = typeof (T),
				Content = contentGetter (typeof (T)),
				Callback = contentCallback
			};
			contentCallbacks.Add (registration);
			return registration;
		}

		public IDisposable RunWhenContentAddedOrRemoved<T> (Action<T> addedCallback, Action<T> removedCallback)
		{
			return new ContentAddedOrRemovedCallback {
				ContentAddedRegistration = RunWhenContentAdded (addedCallback),
				ContentRemovedRegistration = RunWhenContentRemoved (removedCallback)
			};
		}

		public void InvokeContentChangeCallbacks ()
		{
			if (contentCallbacks == null)
				return;

			// Get the list of callbacks, grouped by content type

			foreach (var callbackSet in contentCallbacks.GroupBy (c => c.ContentType).ToList ()) {
				var content = contentGetter (callbackSet.Key);
				foreach (var callback in callbackSet)
					InvokeCallback (content, callback);
			}
		}

		void InvokeCallback (object content, ContentCallback callback)
		{
			bool invoke = false;
			object contentArg = content;
			if (callback.IsAddCallback) {
				if (content != null && content != callback.Content)
					invoke = true;
				callback.Content = content;
			} else {
				if (content == null) {
					if (callback.Content != null) {
						invoke = true;
						contentArg = callback.Content;
						callback.Content = null;
					}
				} else {
					// Content is available
					if (callback.Content == null)
						// Store the content, it will be used as argument when invoking this callback if the content is removed
						callback.Content = content;
					else if (callback.Content != content) {
						// Content for this callback was stored, but the instance has changed. The callback has to be invoked for the old content
						invoke = true;
						contentArg = callback.Content;
						callback.Content = content;
					}
				}
			}
			if (invoke) {
				try {
					callback.Callback.DynamicInvoke (contentArg);
				} catch (Exception ex) {
					LoggingService.LogInternalError ("Content callback invocation failed", ex);
				}
			}
		}
	}
}
