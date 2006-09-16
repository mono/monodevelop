//
// AspNetAppProjectConfiguration.cs: ASP.NET-specific configuration options 
//     and behaviours
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

namespace AspNetAddIn
{
	public class AspNetAppProjectConfiguration : DotNetProjectConfiguration 
	{
		[ItemProperty ("AspNet/AutoGenerateCodeBehindMembers")]
		bool autoGenerateCodeBehindMembers = false;
		
		public bool AutoGenerateCodeBehindMembers {
			get { return autoGenerateCodeBehindMembers; }
			set { autoGenerateCodeBehindMembers = value; }
		}
		
		#region //override behaviour of base class to make sure things compile to the right places
		
		public override CompileTarget CompileTarget {
			get { return CompileTarget.Library; }
			set {
				if (value != CompileTarget.Library)
					throw new InvalidOperationException ("ASP.NET applications must compile to a library.");
			}
		}
		
		public override string OutputDirectory {
			get {
				if (SourceDirectory != null)
					return System.IO.Path.Combine (SourceDirectory, "bin");
				else
					return "bin";
			}
			set { }
		}
		
		#endregion
		
	}
}
