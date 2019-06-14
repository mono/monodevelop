// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace UnitTests
{
	public static class ObjectReference
	{
		// We want to ensure this isn't inlined, because we need to ensure that any temporaries
		// on the stack in this method or targetFactory get cleaned up. Otherwise, they might still
		// be alive when we want to make later assertions.
		[MethodImpl (MethodImplOptions.NoInlining)]
		public static ObjectReference<T> CreateFromFactory<T> (Func<T> targetFactory) where T : class
		{
			return new ObjectReference<T> (targetFactory ());
		}

		public static ObjectReference<T> Create<T> (T target) where T : class
		{
			return new ObjectReference<T> (target);
		}
	}

	/// <summary>
	/// A wrapper to hold onto an object that you wish to make assertions about the lifetime of. This type has specific protections
	/// to ensure the best possible patterns to avoid "gotchas" with these sorts of tests.
	/// </summary>
	public sealed class ObjectReference<T> where T : class
	{
		private T _strongReference;

		/// <summary>
		/// Tracks if <see cref="GetReference"/> was called, which means it's no longer safe to do lifetime assertions.
		/// </summary>
		private bool _strongReferenceRetrievedOutsideScopedCall;

		private readonly WeakReference _weakReference;

		public ObjectReference (T target)
		{
			if (target == null) {
				throw new ArgumentNullException (nameof (target));
			}

			_strongReference = target;
			_weakReference = new WeakReference (target);
		}

		public void Release ()
		{
			if (_strongReferenceRetrievedOutsideScopedCall) {
				throw new InvalidOperationException ($"The strong reference being held by the {nameof (ObjectReference<T>)} was retrieved via a call to {nameof (GetReference)}. Since the CLR might have cached a temporary somewhere in your stack, assertions can no longer be made about the correctness of lifetime.");
			}
			_strongReference = null;
		}

		public void AssertAlive ()
		{
			Assert.True (_weakReference.IsAlive, "Reference should still be held.");
		}

		public void AssertDead ()
		{
			Assert.False (_weakReference.IsAlive, "Reference should have been released but was not.");
		}

		/// <summary>
		/// Provides the underlying strong refernce to the given action. This method is marked not be inlined, to ensure that no temporaries are left
		/// on the stack that might still root the strong reference. The caller must not "leak" the object out of the given action for any lifetime
		/// assertions to be safe.
		/// </summary>
		[MethodImpl (MethodImplOptions.NoInlining)]
		public void UseReference (Action<T> action)
		{
			action (GetReferenceWithChecks ());
		}

		/// <summary>
		/// Provides the underlying strong refernce to the given function. This method is marked not be inlined, to ensure that no temporaries are left
		/// on the stack that might still root the strong reference. The caller must not "leak" the object out of the given action for any lifetime
		/// assertions to be safe.
		/// </summary>
		[MethodImpl (MethodImplOptions.NoInlining)]
		public U UseReference<U> (Func<T, U> function)
		{
			return function (GetReferenceWithChecks ());
		}

		/// <summary>
		/// Fetches the object strongly being held from this. Because the value returned might be cached in a local temporary from
		/// the caller of this function, no further calls to <see cref="AssertAlive"/> or <see cref="AssertDead"/> may be called
		/// on this object as the test is not valid either way. If you need to operate with the object without invalidating
		/// the ability to reference the object, see <see cref="UseReference"/>.
		/// </summary>
		public T GetReference ()
		{
			_strongReferenceRetrievedOutsideScopedCall = true;
			return GetReferenceWithChecks ();
		}

		private T GetReferenceWithChecks ()
		{
			if (_strongReference == null) {
				throw new InvalidOperationException ($"The type has already been released due to a call to {nameof (Release)}.");
			}

			return _strongReference;
		}
	}
}
