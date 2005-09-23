//
// Providers/DbProviderCollection.cs
//
// Authors:
//   Christian Hergert <chris@mosaix.net>
//
// Copyright (c) 2005 Christian Hergert
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
using System.Xml.Serialization;
using System.Collections;

using Mono.Data.Sql;

namespace MonoQuery
{
	[Serializable]
	[XmlInclude (typeof (MySqlDbProvider))]
	[XmlInclude (typeof (NpgsqlDbProvider))]
	[XmlInclude (typeof (SqliteDbProvider))]
	[XmlInclude (typeof (OracleDbProvider))]
	[XmlInclude (typeof (SqlDbProvider))]
	[XmlInclude (typeof (FirebirdDbProvider))]
	[XmlInclude (typeof (SybaseDbProvider))]
	[XmlInclude (typeof (OdbcDbProvider))]
	public class DbProviderCollection : CollectionBase
	{
		public DbProviderCollection () : base ()
		{
		}

		public event EventHandler Changed;

		public DbProviderBase this[int index] {
			get {
				return ((DbProviderBase) List[index]);
			}
			set {
				List[index] = value;
			}
		}

		public int Add (DbProviderBase item)
		{
			int retval = List.Add (item);

			if( Changed != null )
				Changed (this, new EventArgs ());

			return( retval );
		}

		public int IndexOf (DbProviderBase item)
		{
			return (List.IndexOf (item));
		}

		public void Insert (int index, DbProviderBase item)
		{
			List.Insert (index, item);

			if (Changed != null)
				Changed (this, new EventArgs ());
		}

		public void Remove (DbProviderBase item)
		{
			List.Remove (item);

			if( Changed != null )
				Changed (this, new EventArgs ());
		}

		public bool Contains(DbProviderBase item)
		{
			return (List.Contains (item));
		}
	}
}
