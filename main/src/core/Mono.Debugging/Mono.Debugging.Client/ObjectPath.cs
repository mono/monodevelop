// ObjectPath.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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

namespace Mono.Debugging.Client
{
	[Serializable]
	public struct ObjectPath
	{
		string[] path;
		
		public ObjectPath (params string[] path)
		{
			this.path = path;
		}
		
		public string this [int n] {
			get {
				if (path == null)
					throw new IndexOutOfRangeException ();
				return path [n]; 
			}
		}
		
		public int Length {
			get { return path != null ? path.Length : 0; }
		}
		
		public IEnumerable GetEnumerator ()
		{
			if (path != null)
				return path;
			else
				return new string [0];
		}
		
		public ObjectPath GetSubpath (int start)
		{
			if (start == 0)
				return this;
			else {
				string[] newPath = new string [path.Length - start];
				Array.Copy (path, start, newPath, 0, newPath.Length);
				return new ObjectPath (newPath);
			}
		}
		
		public ObjectPath Append (string name)
		{
			string[] newPath = new string [path.Length + 1];
			Array.Copy (path, newPath, path.Length);
			newPath [path.Length] = name;
			return new ObjectPath (newPath);
		}
		
		public string LastName {
			get {
				if (Length == 0)
					return "";
				else
					return path [path.Length - 1];
			}
		}
	}
}
