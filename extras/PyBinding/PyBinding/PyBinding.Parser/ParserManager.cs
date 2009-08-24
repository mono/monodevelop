// 
// ParserManager.cs
//  
// Author:
//       Christian Hergert <chris@dronelabs.com>
// 
// Copyright (c) 2009 Christian Hergert
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
using System.Collections.Generic;
using System.Threading;

using PyBinding.Runtime;

namespace PyBinding.Parser
{
	/// <summary>
	/// Manage the parser subprocesses and access to them.
	/// </summary>
	internal static class ParserManager
	{
		static Dictionary<Type,PythonParserInternal> s_parsers = new Dictionary<Type, PythonParserInternal> ();
		static object s_syncRoot = new object ();
		
		internal static PythonParserInternal GetParser (IPythonRuntime runtime)
		{
			if (runtime == null)
				throw new ArgumentNullException ("runtime");
			
			lock (s_syncRoot) {
				if (!s_parsers.ContainsKey (runtime.GetType ())) {
					s_parsers [runtime.GetType ()] = new PythonParserInternal (runtime);
				}
			}
			
			return s_parsers [runtime.GetType ()];
		}
	}
}
