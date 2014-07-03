//
// JurassicUtility.cs
//
// Author:
//       Harsimran Bath <harsimranbath@gmail.com>
//
// Copyright (c) 2014 Harsimran
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
using System.Text;
using System.Collections.Generic;

namespace Jurassic
{
	public static class JurassicUtility
	{
		public static string BuildFunctionSignature (string functionName, IList<string> argumentNames)
		{
			if (string.IsNullOrWhiteSpace (functionName))
				functionName = "function"; // for inline functions, callbacks, delegates, etc.

			var builder = new StringBuilder (functionName);

			builder.Append (" (");
			if (argumentNames.Count > 0) {
				for (int i = 0; i < argumentNames.Count; i++) {
					var currentArgument = argumentNames [i];
					if (i + 1 != argumentNames.Count) {
						builder.Append (string.Concat (currentArgument, ", "));
					} else {
						builder.Append (string.Concat (currentArgument));
					}
				}
			}
			builder.Append (")");

			return builder.ToString ();
		}
	}
}

