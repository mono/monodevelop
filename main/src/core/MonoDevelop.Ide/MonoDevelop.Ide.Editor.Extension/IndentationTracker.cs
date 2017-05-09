// 
// IndentationTracker.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Editor.Extension
{
	[Flags]
	public enum IndentationTrackerFeatures {
		None = 0,
		SmartBackspace = 1,
        CustomIndentationEngine = 2,
		All = SmartBackspace
	}

	/// <summary>
	/// The indentation tracker is for giving the editor information about virtual line indentations.
	/// </summary>
	public abstract class IndentationTracker
	{
		public virtual IndentationTrackerFeatures SupportedFeatures {
			get {
				return IndentationTrackerFeatures.None;
			}
		}

		/// <summary>
		/// Gets the indentation string for a given line.
		/// </summary>
		public abstract string GetIndentationString (int lineNumber);
	}
}