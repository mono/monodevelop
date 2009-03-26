//
// Error.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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

namespace MonoDevelop.Projects.Dom
{
	
	public enum ErrorType {
		Error,
		Warning
	}
		
	public class Error
	{
		public DomRegion Region { get; private set; }

		public string Message { get; private set; }

		public ErrorType ErrorType { get; set; }
		
		public Error (int line, int column, string message) : this (ErrorType.Error, line, column, message)
		{
		}
		
		public Error (ErrorType errorType, int line, int column, string message)
			: this (errorType, new DomLocation (line, column), message)
		{
		}
		
		public Error (ErrorType errorType, DomLocation location , string message)
			: this (ErrorType.Error, new DomRegion (location, DomLocation.Empty), message)
		{
		}
		
		public Error (ErrorType errorType, DomRegion region, string message)
		{
			this.ErrorType = errorType;
			this.Region = region;
			this.Message = message;
		}
		
		public override string ToString ()
		{
			return string.Format("[Error: Region={0}, Message={1}, ErrorType={2}]", Region, Message, ErrorType);
		}
	}
}
