// IPythonRuntime.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
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

using PyBinding.Compiler;

namespace PyBinding.Runtime
{
	public interface IPythonRuntime : ICloneable
	{
		/// <value>
		/// The unique name of the python runtime (ex: Python25)
		/// </value>
		string Name {
			get;
		}
		
		/// <value>
		/// The path to the python runtime (ex: /usr/bin/python)
		/// </value>
		string Path {
			get;
			set;
		}
		
		/// <value>
		/// The compiler (if available) for the python runtime. This can
		/// be used to pre-compile all the source before runtime.
		/// </value>
		IPythonCompiler Compiler {
			get;
		}
		
		/// <summary>
		/// Builds a list of arguments to pass to the runtime for running
		/// a project with the passed configuration.
		/// </summary>
		/// <param name="configuration">
		/// A <see cref="PythonConfiguration"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		string[] GetArguments (PythonConfiguration configuration);
	}
}
