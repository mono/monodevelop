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

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.AspNet
{
	public class AspNetAppProjectConfiguration : DotNetProjectConfiguration 
	{
		[ItemProperty ("AspNet/DisableCodeBehindGeneration", DefaultValue = false)]
		bool disableCodeBehindGeneration = false;
		
		[ItemProperty ("AspNet/nonStandardOutputDirectory", DefaultValue = false)]
		bool nonStandardOutputDirectory = false;
		
		public bool DisableCodeBehindGeneration {
			get { return disableCodeBehindGeneration; }
			set { disableCodeBehindGeneration = value; }
		}
		
		#region //override behaviour of base class to make sure things compile to the right places
		
		string GetStandardOutputDirectory ()
		{
			if (ParentItem != null && ParentItem.BaseDirectory != null) {
				return System.IO.Path.Combine (ParentItem.BaseDirectory, "bin");
			} else {
				return "bin";
			}
		}
		
		public override string OutputDirectory {
			get {
				if (nonStandardOutputDirectory)
					return base.OutputDirectory;
				else
					return GetStandardOutputDirectory ();
			}
			set {
				base.OutputDirectory = value;
			}
		}
		
		#endregion
		
	}
}
