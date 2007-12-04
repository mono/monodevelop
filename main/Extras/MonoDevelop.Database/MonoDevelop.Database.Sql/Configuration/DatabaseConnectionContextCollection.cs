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
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public class DatabaseConnectionContextCollection : IEnumerable<DatabaseConnectionContext>
	{
		public event EventHandler Changed;
		private List<DatabaseConnectionContext> contexts;
		
		public DatabaseConnectionContextCollection ()
		{
			contexts = new List<DatabaseConnectionContext> ();
		}

		public DatabaseConnectionContext this[int index] {
			get { return contexts[index]; }
		}

		internal void Add (DatabaseConnectionContext item)
		{
			contexts.Add (item);
			OnChanged (EventArgs.Empty);
		}
		
		public int Count {
			get { return contexts.Count; }
		}

		public int IndexOf (DatabaseConnectionContext item)
		{
			return contexts.IndexOf (item);
		}

		internal void Insert (int index, DatabaseConnectionContext item)
		{
			contexts.Insert (index, item);
			OnChanged (EventArgs.Empty);
		}

		internal void Remove (DatabaseConnectionContext item)
		{
			contexts.Remove (item);
			OnChanged (EventArgs.Empty);
		}

		public bool Contains (DatabaseConnectionContext item)
		{
			return contexts.Contains (item);
		}
		
		public bool Contains (string name)
		{
			foreach (DatabaseConnectionContext context in this) {
				if (context.ConnectionSettings.Name == name)
					return true;
			}
			return false;
		}
		
		public IEnumerator<DatabaseConnectionContext> GetEnumerator ()
		{
			return contexts.GetEnumerator ();
		}
		
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return (contexts as IEnumerable).GetEnumerator ();
		}
		
		protected virtual void OnChanged (EventArgs e)
		{
			if (Changed != null )
				Changed (this, e);
		}
	}
}