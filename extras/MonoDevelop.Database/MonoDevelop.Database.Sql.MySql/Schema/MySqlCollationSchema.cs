//
// Authors:
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2008 Ben Motmans
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
using System.Collections;

namespace MonoDevelop.Database.Sql.MySql
{
	public class MySqlCollationSchema : AbstractSchema
	{
		protected int id;
		protected bool isDefaultCollation;
		protected bool isCompiled;
		protected int sortLength;
		
		protected string characterSetName;
		
		public MySqlCollationSchema (ISchemaProvider schemaProvider)
			: base (schemaProvider)
		{
		}
		
		public MySqlCollationSchema (MySqlCollationSchema schema)
			: base (schema)
		{
			id = schema.id;
			isDefaultCollation = schema.isDefaultCollation;
			isCompiled = schema.isCompiled;
			sortLength = schema.sortLength;
		}
		
		public int Id {
			get { return id; }
			set {
				if (id != value) {
					id = value;
					OnChanged ();
				}
			}
		}
		
		public bool IsDefaultCollation {
			get { return isDefaultCollation; }
			set {
				if (isDefaultCollation != value) {
					isDefaultCollation = value;
					OnChanged ();
				}
			}
		}
		
		public bool IsCompiled {
			get { return isCompiled; }
			set {
				if (isCompiled != value) {
					isCompiled = value;
					OnChanged ();
				}
			}
		}
		
		public int SortLength {
			get { return sortLength; }
			set {
				if (sortLength != value) {
					sortLength = value;
					OnChanged ();
				}
			}
		}
		
		public string CharacterSetName {
			get { return characterSetName; }
			set {
				if (characterSetName != value) {
					characterSetName = value;
					OnChanged ();
				}
			}
		}
		
		public override object Clone ()
		{
			return new MySqlCollationSchema (this);
		}
	}
}
