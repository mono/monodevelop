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
	public class MySqlCharacterSetSchema : AbstractSchema
	{
		protected string defaultCollactionName;
		protected int maxLength;
		
		public MySqlCharacterSetSchema (ISchemaProvider schemaProvider)
			: base (schemaProvider)
		{
		}
		
		public MySqlCharacterSetSchema (MySqlCharacterSetSchema schema)
			: base (schema)
		{
			defaultCollactionName = schema.defaultCollactionName;
			defaultCollactionName = schema.defaultCollactionName;
		}
		
		public int MaxLength {
			get { return maxLength; }
			set {
				if (maxLength != value) {
					maxLength = value;
					OnChanged ();
				}
			}
		}
		
		public string DefaultCollactionName {
			get { return defaultCollactionName; }
			set {
				if (defaultCollactionName != value) {
					defaultCollactionName = value;
					OnChanged ();
				}
			}
		}
		
		public override object Clone ()
		{
			return new MySqlCharacterSetSchema (this);
		}
	}
}
