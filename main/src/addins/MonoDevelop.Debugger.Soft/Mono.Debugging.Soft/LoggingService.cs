// 
// LoggingService.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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

namespace Mono.Debugging.Soft
{
	/// <summary>
	/// This is a simple abstraction so that MD can plug in its own logging service to handle the Mono.Debugging.Soft
	/// error logging, without Mono.Debugging.Soft depending on MonoDevelop.Core.
	/// In the absence of a custom logger, it writes to Console.
	/// </summary>
	public static class LoggingService
	{
		public static ICustomLogger CustomLogger { get; set; }
		
		internal static void LogError (string message, Exception ex)
		{
			if (CustomLogger != null)
				CustomLogger.LogError (message, ex);
			else
				Console.WriteLine (message + (ex != null? System.Environment.NewLine + ex.ToString () : string.Empty));
		}

		//this is meant to show a GUI if possible
		internal static void LogAndShowException (string message, Exception ex)
		{
			if (CustomLogger != null)
				CustomLogger.LogAndShowException (message, ex);
			else
				LogError (message, ex);
		}
	}
	
	public interface ICustomLogger
	{
		void LogError (string message, System.Exception ex);
		void LogAndShowException (string message, System.Exception ex);
	}
}