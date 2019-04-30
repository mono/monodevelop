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
using System;
using System.Threading.Tasks;

namespace MonoDevelop.Core
{
	/// <summary>
	/// Abstract implementation of a service
	/// </summary>
	public abstract class Service: IService
	{
		ServiceProvider serviceProvider;

		/// <summary>
		/// Gets the service provider used to initialize this service
		/// </summary>
		/// <value>The service provider.</value>
		public ServiceProvider ServiceProvider => serviceProvider;

		/// <summary>
		/// Initializes the service
		/// </summary>
		/// <returns>The initialize.</returns>
		/// <param name="serviceProvider">Service provider that can be used to initialize other services</param>
		protected virtual Task OnInitialize (ServiceProvider serviceProvider)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Disposes the service
		/// </summary>
		protected virtual Task OnDispose ()
		{
			return Task.CompletedTask;
		}

		Task IService.Initialize (ServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider;
			return OnInitialize (serviceProvider);
		}

		Task IService.Dispose ()
		{
			return OnDispose ();
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Interface)]
	public class DefaultServiceImplementationAttribute : Attribute
	{
		public DefaultServiceImplementationAttribute ()
		{
		}

		public DefaultServiceImplementationAttribute (Type implementationType)
		{
			ImplementationType = implementationType;
		}

		public Type ImplementationType { get; set; }
	}
}
