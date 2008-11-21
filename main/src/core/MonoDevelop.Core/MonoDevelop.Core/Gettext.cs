// 
// Gettext.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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
using System.IO;

using Mono.Unix;

namespace MonoDevelop.Core
{
	
	
	public static class GettextCatalog
	{
		
		static GettextCatalog ()
		{
			//variable can be used to override where Gettext looks for the catalogues
			string catalog = System.Environment.GetEnvironmentVariable ("MONODEVELOP_LOCALE_PATH");
			
			if (string.IsNullOrEmpty (catalog)) {
				string location = System.Reflection.Assembly.GetExecutingAssembly ().Location;
				location = Path.GetDirectoryName (location);
				// MD is located at $prefix/lib/monodevelop/bin
				// adding "../../.." should give us $prefix
				string prefix = Path.Combine (Path.Combine (Path.Combine (location, ".."), ".."), "..");
				//normalise it
				prefix = Path.GetFullPath (prefix);
				//catalogue is installed to "$prefix/share/locale" by default
				catalog = Path.Combine (Path.Combine (prefix, "share"), "locale");
			}
			Catalog.Init ("monodevelop", catalog);
		}
		
		#region GetString
		
		public static string GetString (string phrase)
		{
			return Catalog.GetString (phrase);
		}
		
		public static string GetString (string phrase, object arg0)
		{
			return string.Format (Catalog.GetString (phrase), arg0);
		}
		
		public static string GetString (string phrase, object arg0, object arg1)
		{
			return string.Format (Catalog.GetString (phrase), arg0, arg1);
		}
		
		public static string GetString (string phrase, object arg0, object arg1, object arg2)
		{
			return string.Format (Catalog.GetString (phrase), arg0, arg1, arg2);
		}
		
		public static string GetString (string phrase, params object[] args)
		{
			return string.Format (Catalog.GetString (phrase), args);
		}
		
		#endregion
		
		#region GetPluralString
		
		public static string GetPluralString (string singular, string plural, int number)
		{
			return Catalog.GetPluralString (singular, plural, number);
		}
		
		public static string GetPluralString (string singular, string plural, int number, object arg0)
		{
			return string.Format (Catalog.GetPluralString (singular, plural, number), arg0);
		}
		
		public static string GetPluralString (string singular, string plural, int number, object arg0, object arg1)
		{
			return string.Format (Catalog.GetPluralString (singular, plural, number), arg0, arg1);
		}
		
		public static string GetPluralString (string singular, string plural, int number, 
		                                      object arg0, object arg1, object arg2)
		{
			return string.Format (Catalog.GetPluralString (singular, plural, number), arg0, arg1, arg2);
		}
		
		public static string GetPluralString (string singular, string plural, int number, params object[] args)
		{
			return string.Format (Catalog.GetPluralString (singular, plural, number), args);
		}
		#endregion
	}
}
