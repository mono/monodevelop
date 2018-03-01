//
// ErrorHelper.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System.Diagnostics.Contracts;
using System.Text;

namespace MonoDevelop.Core
{
	public static class ErrorHelper
	{
		[Pure]
		public static string GetErrorMessage (string message, Exception ex)
		{
			var exMsg = ex != null ? GetErrorMessage (ex) : "";
			if (string.IsNullOrEmpty (message)) {
				if (!string.IsNullOrEmpty (exMsg))
					return exMsg;
				if (ex != null)
					return ex.Message;
			}
			return message;
		}

		[Pure]
		public static string GetErrorMessage (Exception ex)
		{
			if (ex is AggregateException) {
				var ae = (AggregateException)ex;
				if (ae.InnerExceptions.Count == 1)
					return GetErrorMessage (ae.InnerException);
				StringBuilder sb = StringBuilderCache.Allocate ();
				foreach (var e in ae.InnerExceptions) {
					if (sb.Length > 0 && sb [sb.Length - 1] != '.')
						sb.Append (". ");
					sb.Append (GetErrorMessage (ex).Trim ());
				}
				return StringBuilderCache.ReturnAndFree (sb);
			} else if (ex is UserException) {
				var ue = (UserException)ex;
				if (!string.IsNullOrEmpty (ue.Details)) {
					var msg = ex.Message.TrimEnd ();
					if (!msg.EndsWith ("."))
						msg += ". ";
					else
						msg += " ";
					return msg + ue.Details;
				}
			} else if (ex is NullReferenceException || ex is InvalidCastException)
				return string.Empty;
			return ex.Message;
		}	}
}

