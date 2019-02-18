//
// DocumentControllerFactory.cs
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
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// Controller factories are in charge of creating controllers
	/// </summary>
	public abstract class DocumentControllerFactory
	{
		internal protected ServiceProvider ServiceProvider { get; set; }

		/// <summary>
		/// Unique identifier of the controller factory.
		/// </summary>
		public virtual string Id => GetType ().FullName;

		/// <summary>
		/// Checks if this factory can create a controller for the provided file, and returns the kind of
		/// controller it can create.
		/// </summary>
		protected virtual IEnumerable<DocumentControllerDescription> GetSupportedControllers (ModelDescriptor modelDescriptor)
		{
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Checks if this factory can create a controller for the provided file, and returns the kind of
		/// controller it can create.
		/// </summary>
		public virtual Task<IEnumerable<DocumentControllerDescription>> GetSupportedControllersAsync (ModelDescriptor modelDescriptor)
		{
			return Task.FromResult (GetSupportedControllers (modelDescriptor));
		}

		/// <summary>
		/// Creates a controller for editing the provided file
		/// </summary>
		public abstract Task<DocumentController> CreateController (ModelDescriptor modelDescriptor, DocumentControllerDescription controllerDescription);
	}
}
