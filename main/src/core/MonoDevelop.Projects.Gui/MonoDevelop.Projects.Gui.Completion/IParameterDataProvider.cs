// IParameterDataProvider.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//


using System;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Projects.Gui.Completion
{
	public interface IParameterDataProvider
	{
		// Returns the number of methods
		int OverloadCount { get; }
		
		// Returns the index of the parameter where the cursor is currently positioned.
		// -1 means the cursor is outside the method parameter list
		// 0 means no parameter entered
		// > 0 is the index of the parameter (1-based)
		int GetCurrentParameterIndex (CodeCompletionContext ctx);
		
		// Returns the markup to use to represent the specified method overload
		// in the parameter information window.
		string GetMethodMarkup (int overload, string[] parameterMarkup, int currentParameter);
		
		// Returns the text to use to represent the specified parameter
		string GetParameterMarkup (int overload, int paramIndex);
		
		// Returns the number of parameters of the specified method
		int GetParameterCount (int overload);
	}
}
