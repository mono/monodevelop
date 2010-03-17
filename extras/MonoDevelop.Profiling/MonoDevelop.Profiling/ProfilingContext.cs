//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Execution;
 
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.Profiling
{
	public sealed class ProfilingContext
	{
		private string filename;
		private IProcessAsyncOperation asyncOperation;
		
		public ProfilingContext (IProcessAsyncOperation asyncOperation, string filename)
		{
			if (asyncOperation == null)
				throw new ArgumentNullException ("asyncOperation");
			if (filename == null)
				throw new ArgumentNullException ("filename");
			
			this.asyncOperation = asyncOperation;
			this.filename = filename;
		}
		
		public IProcessAsyncOperation AsyncOperation {
			get { return asyncOperation; }
		}
		
		public string FileName {
			get { return filename; }
		}
	}
}