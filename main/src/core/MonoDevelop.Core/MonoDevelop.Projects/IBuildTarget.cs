// IBuildTarget.cs
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

using System.Collections.Generic;
using MonoDevelop.Core;
using System;

namespace MonoDevelop.Projects
{
	public interface IBuildTarget: IWorkspaceObject
	{
		BuildResult RunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration);
		bool SupportsTarget (string target);

		[Obsolete ("This method will be removed in future releases")]
		bool NeedsBuilding (ConfigurationSelector configuration);

		[Obsolete ("This method will be removed in future releases")]
		void SetNeedsBuilding (bool needsBuilding, ConfigurationSelector configuration);

		//TODO: move these to IExecutableWorkspaceObject when we break API
		void Execute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration);
		bool CanExecute (ExecutionContext context, ConfigurationSelector configuration);
	}

	public interface IExecutableWorkspaceObject : IBuildTarget
	{
		/// <summary>
		/// Gets the build targets that should be built before the project is executed.
		/// If the project itself is not included, it will not be built.
		/// </summary>
		IEnumerable<IBuildTarget> GetExecutionDependencies ();
	}
}
