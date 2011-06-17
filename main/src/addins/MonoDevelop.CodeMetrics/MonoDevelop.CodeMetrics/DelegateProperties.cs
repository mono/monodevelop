// 
// DelegateProperties.cs
//  
// Author:
//       Nikhil Sarda <diff.operator@gmail.com>
// 
// Copyright (c) 2010 Nikhil Sarda
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
using System.Collections;
using System.Collections.Generic;
using System.Text;


using Gtk;

using MonoDevelop.Core;
 
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;


namespace MonoDevelop.CodeMetrics
{
	public class DelegateProperties : IProperties
	{
		ITypeDefinition dlgte;
		
		public ITypeDefinition Delegate {
			get {
				return dlgte;
			}
		}
		
		public string FullName {
			get; internal set;
		}
		
		public int CyclometricComplexity {
			get {
				return 0;
			}
		}
		
		public int ClassCoupling {
			get; internal set;
		}
		
		public int StartLine {
			get; private set;
		}
		
		public int EndLine {
			get; private set;
		}
		
		public ulong LOCReal {
			get; internal set;
		}
		
		public ulong LOCComments {
			get;internal set;
		}
		
		public string FilePath {
			get; set;
		}
		
		public DelegateProperties (ITypeDefinition i)
		{
			dlgte = i;
			FullName = Delegate.FullName;
			StartLine = Delegate.BodyRegion.BeginLine;
			EndLine = Delegate.BodyRegion.EndLine;
			FilePath="";
		}
	}
}

