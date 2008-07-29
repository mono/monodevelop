// 
// MonoDevelopDatabaseConnection.cs
// 
// Author:
//   Aaron Bockover <abockover@novell.com>
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

namespace MonoDevelop.Projects.Dom.Database
{
	
	public class MonoDevelopDatabaseConnection : Hyena.Data.Sqlite.HyenaSqliteConnection
	{
		public MonoDevelopDatabaseConnection (string fileName) : base (fileName)
		{
			// Each cache page is about 1.5K, so 32768 pages = 49152K = 48M
			int cache_size = 32768;
			Execute ("PRAGMA cache_size = ?", cache_size);
			Execute ("PRAGMA synchronous = OFF");
			Execute ("PRAGMA temp_store = MEMORY");
			Execute ("PRAGMA count_changes = OFF");

			// do we want this or not?
			//Execute ("PRAGMA case_sensitive_like=ON");
			
			//for debugging
			//Hyena.Data.Sqlite.HyenaSqliteCommand.LogAll = true;
		}
	}
}
