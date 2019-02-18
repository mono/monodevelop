//
// ServiceProvider.cs
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

using System.Threading;
using System.Threading.Tasks;
using System;

namespace MonoDevelop.Core
{
	/// <summary>
	/// Defines a mechanism for retrieving a service object
	/// </summary>
	public abstract class ServiceProvider
	{
		/// <summary>
		/// Returns the service of the provided type, creating and initializing it if necessary
		/// </summary>
		/// <returns>The service.</returns>
		/// <typeparam name="T">The type of the service being requested</typeparam>
		public abstract Task<T> GetService<T> () where T:class;

		/// <summary>
		/// Returns the service of the provided type if it has been initialized, null otherwise
		/// </summary>
		/// <returns>The service.</returns>
		/// <typeparam name="T">The type of the service being requested</typeparam>
		public abstract T PeekService<T> () where T : class;

		/// <summary>
		/// Executes an action when a service is initialized
		/// </summary>
		/// <param name="action">Action to run</param>
		/// <typeparam name="T">Service type</typeparam>
		/// <returns>A registration object that can be disposed to unregister the callback</returns>
		/// <remarks>This method does not cause the initialization of the service.</remarks>
		public abstract IDisposable WhenServiceInitialized<T> (Action<T> action) where T : class;
	}
}
