//
// UserException.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Core
{
	// An exception that can be used to report errors to the user

	public class UserException: ApplicationException
	{
		string details;
		
		public UserException (string message): base (message)
		{
		}
		
		public UserException (string message, string details): base (message)
		{
			this.details = details;
		}
		
		public string Details {
			get { return details; }
		}

		/// <summary>
		/// If true, it means that the error has already been reported to the user (for example, by showing a dialog), so it doesn't have to be reported again.
		/// </summary>
		/// <value><c>true</c> if already reported to user; otherwise, <c>false</c>.</value>
		public bool AlreadyReportedToUser { get; set; }
	}
}
