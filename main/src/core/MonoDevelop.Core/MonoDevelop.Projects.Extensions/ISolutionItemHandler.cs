// ISolutionItemHandler.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Extensions
{
	/// <summary>
	/// An abstraction of some solution item operations that may be specific to the underlying file format.
	/// </summary>
	public interface ISolutionItemHandler: IDisposable
	{
		/// <summary>
		/// Executes a build target
		/// </summary>
		/// <returns>
		/// The result of the operation
		/// </returns>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		/// <param name='target'>
		/// Name of the target to execute
		/// </param>
		/// <param name='configuration'>
		/// Selector to be used to get the target configuration
		/// </param>
		BuildResult RunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration);
		
		/// <summary>
		/// Saves the solution item
		/// </summary>
		/// <param name='monitor'>
		/// A progress monitor
		/// </param>
		void Save (IProgressMonitor monitor);
		
		/// <summary>
		/// Gets a value indicating whether the name of the solution item should be the same as the name of the file
		/// </summary>
		/// <value>
		/// <c>true</c> if the file name must be in sync with the solution item name; otherwise, <c>false</c>.
		/// </value>
		bool SyncFileName { get; }
		
		/// <summary>
		/// Unique and immutable identifier of the solution item inside the solution
		/// </summary>
		string ItemId { get; }

		/// <summary>
		/// Notifies that this solution item has been modified
		/// </summary>
		/// <param name='hint'>
		/// Hint about which part of the solution item has been modified. This will typically be the property name.
		/// </param>
		void OnModified (string hint);
	}
}
