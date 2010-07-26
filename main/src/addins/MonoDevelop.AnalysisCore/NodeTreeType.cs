// 
// NodeTreeType.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
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
using System.Diagnostics;

namespace MonoDevelop.AnalysisCore
{
	// Type of the analysis tree. Basically a key for the analysis tree cache.
	public class NodeTreeType
	{
		string input, fileExtension;
		
		public string Input { get { return input; } }
		public string FileExtension { get { return fileExtension; } }
		
		public NodeTreeType (string input, string fileExtension)
		{
			Debug.Assert (!string.IsNullOrEmpty (input));
			Debug.Assert (!string.IsNullOrEmpty (fileExtension));
			
			this.input = input;
			this.fileExtension = fileExtension;
		}
		
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			var other = obj as NodeTreeType;
			return other != null && input == other.input && fileExtension == other.fileExtension;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (input != null ? input.GetHashCode () : 0)
					^ (fileExtension != null ? fileExtension.GetHashCode () : 0);
			}
		}
	}
}

