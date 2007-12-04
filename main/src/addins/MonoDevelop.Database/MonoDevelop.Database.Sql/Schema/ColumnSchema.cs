//
// Authors:
//	Christian Hergert  <chris@mosaix.net>
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
// Copyright (c) 2007 Ben Motmans
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
	public class ColumnSchema : AbstractSchema
	{
		protected ISchema parent;
		protected string dataType = String.Empty;
		protected bool hasDefaultValue;
		protected string defaultValue;
		protected bool nullable;
		protected int position = 0;
		protected ConstraintSchemaCollection constraints;
		
		public ColumnSchema (ISchemaProvider schemaProvider, ISchema parent)
			: base (schemaProvider)
		{
			this.parent = parent;
		}
		
		public ColumnSchema (ISchemaProvider schemaProvider, ISchema parent, string name)
			: base (schemaProvider)
		{
			this.constraints = new ConstraintSchemaCollection ();
			this.parent = parent;
			this.name = name;
		}
		
		public ColumnSchema (ColumnSchema column)
			: base (column)
		{
			parent = column.parent; //do not clone, this would create an infinite loop
			dataType = column.dataType;
			hasDefaultValue = column.hasDefaultValue;
			defaultValue = column.defaultValue;
			nullable = column.nullable;
			position = column.position;
			constraints = new ConstraintSchemaCollection (column.constraints);
		}
		
		public ISchema Parent {
			get { return parent; }
			set {
				if (parent != value) {
					parent = value;
					OnChanged ();
				}
			}
		}
		
		public DataTypeSchema DataType {
			get { return SchemaProvider.GetDataType (dataType); }
		}
		
		public string DataTypeName {
			get { return dataType; }
			set {
				if (dataType != value) {
					dataType = value;
					OnChanged ();
				}
			}
		}
		
		public virtual string DefaultValue {
			get { return defaultValue; }
			set {
				if (defaultValue != value) {
					defaultValue = value;
					OnChanged();
				}
			}
		}
		
		public virtual bool HasDefaultValue {
			get { return hasDefaultValue; }
			set {
				if (hasDefaultValue != value) {
					hasDefaultValue = value;
					OnChanged();
				}
			}
		}
		
		public virtual bool IsNullable {
			get { return nullable; }
			set {
				if (nullable != value) {
					nullable = value;
					OnChanged();
				}
			}
		}
		
		public virtual int Position {
			get { return position; }
			set {
				if (position != value) {
					position = value;
					OnChanged();
				}
			}
		}
		
		public ConstraintSchemaCollection Constraints {
			get {
				if (constraints == null)
					constraints = provider.GetColumnConstraints (parent as TableSchema, this);
				return constraints;
			}
		}
		
		public override void Refresh()
		{
			constraints = null;
		}
		
		public override object Clone ()
		{
			return new ColumnSchema (this);
		}
	}
}
