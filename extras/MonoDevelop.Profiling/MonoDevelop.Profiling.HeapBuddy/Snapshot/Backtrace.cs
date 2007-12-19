//
// Backtrace.cs
//
// Copyright (C) 2005 Novell, Inc.
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

namespace MonoDevelop.Profiling.HeapBuddy
{
	public class Backtrace
	{

		public Type Type;
		
		public int LastGeneration;

		public ObjectStats LastObjectStats;

		public Frame [] frames;

		uint code;
		OutfileReader reader;

		public Backtrace (uint code, OutfileReader reader)
		{
			this.code = code;
			this.reader = reader;
		}

		public uint Code {
			get { return code; }
			set { code = value; }
		}

		public Frame [] Frames {
			
			get {
				if (frames == null)
					frames = reader.GetFrames (code);
				return frames;
			}

			set {
				frames = value;
			}
		}

		public bool MatchesType (string pattern)
		{
			return Type.Matches (pattern);
		}

		public bool MatchesMethod (string pattern)
		{
			int n = Frames.Length;
			for (int i = 0; i < n; ++i)
				if (Util.ContainsNoCase (frames [i].MethodName, pattern))
					return true;
			return false;
		}

		public bool Matches (string pattern)
		{
			return MatchesType (pattern) || MatchesMethod (pattern);
		}
	}

}