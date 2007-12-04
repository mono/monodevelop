//
// Schema/IndexConstraintSchema.cs
//
// Authors:
//   Christian Hergert	<chris@mosaix.net>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
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

namespace MonoDevelop.Database.Sql
{
	public class IndexSchema : AbstractSchema
	{
		protected string tableName;
		protected IndexType type;
		protected ColumnSchemaCollection columns; //TODO: create col subclass, to include sort order and length

		public IndexSchema (ISchemaProvider schemaProvider)
			: base (schemaProvider)
		{
			columns = new ColumnSchemaCollection ();
		}
		
		public IndexSchema (IndexSchema index)
			: base (index)
		{
			this.tableName = index.tableName;
			this.type = index.type;
			this.columns = index.columns;
		}
		
		public string TableName {
			get { return tableName; }
			set {
				if (tableName != value) {
					tableName = value;
					OnChanged ();
				}
			}
		}
		
		public IndexType IndexType {
			get { return type; }
			set {
				if (type != value) {
					type = value;
					OnChanged ();
				}
			}
		}
		
		public ColumnSchemaCollection Columns {
			get { return columns; }
		}
		
		public override object Clone ()
		{
			return new IndexSchema (this);
		}
	}
}
