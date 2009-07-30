// 
// RemoteLoggerController.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using Microsoft.Build.Framework;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class RemoteLoggerController: MarshalByRefObject
	{
		BuildResult result = new BuildResult ();
		FilePath basePath;
		
		public RemoteLoggerController (FilePath basePath)
		{
			this.basePath = basePath;
		}
		
		public BuildResult BuildResult {
			get { return result; }
		}
		
		public void WarningRaised (string file, int line, int column, string code, string message)
		{
			if (file != null)
				file = basePath.Combine (file).FullPath;
			result.AddWarning (file, line, column, code, message);
		}

		public void ErrorRaised (string file, int line, int column, string code, string message)
		{
			if (file != null)
				file = basePath.Combine (file).FullPath;
			result.AddError (file, line, column, code, message);
		}
	}
}
