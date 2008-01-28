//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
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

using Gtk;
using System;
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Database.Sql;
using MonoDevelop.Core;

namespace MonoDevelop.Database.Components
{
	public static class ComponentHelper
	{
		public static string GetClassName (string dbName)
		{
			return GetDotNetName (dbName, "Class", true);
		}
		
		public static string GetFieldName (string dbName)
		{
			return GetDotNetName (dbName, "field", false);
		}
		
		public static string GetPropertyName (string fieldName)
		{
			return GetDotNetName (fieldName, fieldName, true);
		}
		
		internal static string GetDotNetName (string dbName, string alternative, bool startWithLetter)
		{
			StringBuilder sb = new StringBuilder ();
			
			bool validStart = false;
			bool capitalizeNext = true;
			foreach (char c in dbName) {
				if (char.IsLetter (c)) {
					validStart = true;
					if (capitalizeNext)
						sb.Append (char.ToUpper (c));
					else
						sb.Append (c);
					capitalizeNext = false;
				} else if (char.IsDigit (c)) {
					if (validStart) {
						sb.Append (c);
						capitalizeNext = false;
					}
				} else if (c == '_') {
					if (!validStart && !startWithLetter) {
						sb.Append (c);
						validStart = true;
					}
					capitalizeNext = true;
				}
			}
			
			if (sb.Length == 0)
				return alternative;
			
			return sb.ToString ();
		}
	}
}
