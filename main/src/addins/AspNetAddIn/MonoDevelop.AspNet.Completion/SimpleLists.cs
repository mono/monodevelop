// 
// SimpleLists.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Linq;

using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.AspNet.Completion
{
	
	
	static class SimpleList
	{
		
		public static ICompletionDataList CreateBoolean (bool defaultValue)
		{
			CompletionDataList provider = new CompletionDataList ();
			provider.Add ("true", "md-literal");
			provider.Add ("false", "md-literal");
			provider.DefaultCompletionString = defaultValue? "true" : "false";
			return provider;
		}
		
		public static ICompletionDataList CreateEnum<T> (T defaultValue)
		{
			CompletionDataList provider = new CompletionDataList ();
			foreach (string name in Enum.GetNames (typeof (T))) {
				provider.Add (name, "md-literal");
			}
			provider.DefaultCompletionString = defaultValue.ToString ();
			return provider;
		}
		
		public static ICompletionDataList Create (string defaultValue, IEnumerable<string> vals)
		{
			CompletionDataList provider = new CompletionDataList ();
			foreach (string v in vals) {
				provider.Add (v, "md-literal");
			}
			provider.DefaultCompletionString = defaultValue;
			return provider;
		}
		
		public static ICompletionDataList Create (int defaultValue, IEnumerable<int> vals)
		{
			return Create (defaultValue.ToString (), from s in vals select s.ToString ());
		}
	}
}
