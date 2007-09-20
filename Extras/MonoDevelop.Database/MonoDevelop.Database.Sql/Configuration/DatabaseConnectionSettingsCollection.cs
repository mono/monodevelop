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

using System;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	[Serializable]
	public class DatabaseConnectionSettingsCollection : CollectionBase, IEnumerable<DatabaseConnectionSettings>
	{
		public event EventHandler Changed;
		
		public DatabaseConnectionSettingsCollection ()
			: base ()
		{
		}

		public DatabaseConnectionSettings this[int index] {
			get { return List[index] as DatabaseConnectionSettings; }
			set { List[index] = value; }
		}

		public int Add (DatabaseConnectionSettings item)
		{
			int retval = List.Add (item);
			OnChanged (EventArgs.Empty);
			return retval;
		}

		public int IndexOf (DatabaseConnectionSettings item)
		{
			return List.IndexOf (item);
		}

		public void Insert (int index, DatabaseConnectionSettings item)
		{
			List.Insert (index, item);
			OnChanged (EventArgs.Empty);
		}

		public void Remove (DatabaseConnectionSettings item)
		{
			List.Remove (item);
			OnChanged (EventArgs.Empty);
		}

		public bool Contains (DatabaseConnectionSettings item)
		{
			return List.Contains (item);
		}
		
		IEnumerator<DatabaseConnectionSettings> IEnumerable<DatabaseConnectionSettings>.GetEnumerator ()
		{
			foreach (DatabaseConnectionSettings cs in this)
				yield return cs;
		}
		
		protected virtual void OnChanged (EventArgs e)
		{
			if (Changed != null )
				Changed (this, e);
		}
	}
}