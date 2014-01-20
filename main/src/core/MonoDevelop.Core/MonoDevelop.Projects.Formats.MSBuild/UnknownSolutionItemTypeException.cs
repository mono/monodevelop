//
// UnknownSolutionItemTypeException.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects.Formats.MD1;
using MonoDevelop.Projects.Extensions;
using Mono.Addins;
using System.Linq;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.ProgressMonitoring;
using System.Threading.Tasks;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	
	class UnknownSolutionItemTypeException : InvalidOperationException
	{
		public UnknownSolutionItemTypeException ()
			: base ("Unknown solution item type")
		{
		}
		
		public UnknownSolutionItemTypeException (string name)
			: base ("Unknown solution item type: " + name)
		{
			this.TypeName = name;
		}
		
		public string TypeName { get; private set; }
	}
	
}
