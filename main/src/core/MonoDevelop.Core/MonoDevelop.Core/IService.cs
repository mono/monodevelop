//
// Service.cs
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
using System.Threading.Tasks;

namespace MonoDevelop.Core
{
	/// <summary>
	/// A service
	/// </summary>
	public interface IService
	{
		/// <summary>
		/// Initializes the service
		/// </summary>
		/// <returns>The initialize.</returns>
		/// <param name="serviceProvider">Service provider that can be used to initialize other services</param>
		Task Initialize (ServiceProvider serviceProvider);

		/// <summary>
		/// Releases all resource used by the <see cref="T:MonoDevelop.Core.IService"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:MonoDevelop.Core.IService"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="T:MonoDevelop.Core.IService"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the <see cref="T:MonoDevelop.Core.IService"/> so
		/// the garbage collector can reclaim the memory that the <see cref="T:MonoDevelop.Core.IService"/> was occupying.</remarks>
		Task Dispose ();
	}
}
