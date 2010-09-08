// 
// Adaptors.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//		 Nikhil Sarda <diff.operator@gmail.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.Linq;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using MonoDevelop.AnalysisCore.Fixes;

namespace MonoDevelop.AnalysisCore.Rules
{
	public static class Adapters
	{
		public static ICompilationUnit GetCompilationUnit (ParsedDocument input)
		{
			return input.CompilationUnit;
		}
		
		public static IEnumerable<IType> GetTypes (ICompilationUnit input)
		{
			foreach (var type in input.Types) {
				foreach (var innerType in GetInnerTypes(type)) {
					yield return innerType;
				}
				yield return type;
			}
		}
		
		static IEnumerable<IType> GetInnerTypes (IType input)
		{
			foreach (var type in input.InnerTypes) {
				foreach (var innerType in GetInnerTypes(type)) {
					yield return innerType;
				}
				yield return type;
			}
		}
		
		public static IEnumerable<IMethod> GetMethods (IType input)
		{
			foreach (var meth in input.Methods) {
				yield return meth;
			}
		}
		
		public static IEnumerable<IProperty> GetProperties (IType input)
		{
			foreach (var prop in input.Properties) {
				yield return prop;
			}	
		}
		
		public static IEnumerable<IField> GetFields (IType input)
		{
			foreach (var field in input.Fields) {
				yield return field;
			}
		}	
	}
}