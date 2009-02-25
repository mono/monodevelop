// 
// ProjectParameters.cs
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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;

namespace CSharpBinding
{
	public class CSharpProjectParameters: ProjectParameters
	{
		[ItemProperty ("StartupObject", DefaultValue = "")]
		string mainclass = string.Empty;
		
		[ProjectPathItemProperty ("ApplicationIcon", DefaultValue = "")]
		string win32Icon = String.Empty;

		[ProjectPathItemProperty ("Win32Resource", DefaultValue = "")]
		string win32Resource = String.Empty;
	
		[ItemProperty ("CodePage", DefaultValue = 0)]
		int codePage;
		
		public string MainClass {
			get {
				return mainclass;
			}
			set {
				mainclass = value ?? string.Empty;
			}
		}
		
		public int CodePage {
			get {
				return codePage;
			}
			set {
				codePage = value;
			}
		}
		
		public string Win32Icon {
			get {
				return win32Icon;
			}
			set {
				win32Icon = value ?? string.Empty;
			}
		}
		
		public string Win32Resource {
			get {
				return win32Resource;
			}
			set {
				win32Resource = value ?? string.Empty;
			}
		}
		
	}
}
