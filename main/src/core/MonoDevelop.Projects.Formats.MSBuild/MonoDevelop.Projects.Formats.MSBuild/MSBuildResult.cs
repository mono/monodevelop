// 
// MSBuildResult.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Projects.Formats.MSBuild
{
	[Serializable]
	public class MSBuildResult
	{
		public MSBuildResult (bool isWarning, string file, int line, int column, string code, string message)
		{
			IsWarning = isWarning;
			File = file;
			Line = line;
			Column = column;
			Code = code;
			Message = message;
		}
		
		public bool IsWarning { 
			get{ return isWarning; }
			set{ isWarning = value; }
		}
		bool isWarning;
		
		public string File {
			get{ return file; }
			set{ file = value; }
		}
		string file;
		
		public int Line {
			get{ return line; }
			set{ line = value; }
		}
		int line;
		
		public int Column {
			get{ return column; }
			set{ column = value; }
		}
		int column;
		
		public string Code {
			get{ return code; }
			set{ code = value; }
		}
		string code;
		
		public string Message {
			get{ return message; }
			set{ message = value; }
		}
		string message;
	}
}
