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
using System.Collections.Generic;using MonoDevelop.Database.Sql;

namespace MonoDevelop.Database.Components
{
	public interface ISchemaContainer
	{
		ISchema Schema { get; }
		
		ColumnSchemaCollection Columns { get; }
		
		ParameterSchemaCollection Parameters { get; }
		
		SchemaContainerType SchemaContainerType { get; }
	}
	
	public enum SchemaContainerType
	{
		Table,
		View,
		Procedure,
		Query
	}
	
	public class TableSchemaContainer : ISchemaContainer
	{
		protected TableSchema schema;
		
		public TableSchemaContainer (TableSchema schema)
		{
			if (schema == null)
				throw new ArgumentNullException ("schema");
			
			this.schema = schema;
		}
		
		public ISchema Schema {
			get { return schema; }
		}
		
		public ColumnSchemaCollection Columns {
			get { return schema.Columns; }
		}
		
		public ParameterSchemaCollection Parameters {
			get { return null; }
		}
		
		public SchemaContainerType SchemaContainerType {
			get { return SchemaContainerType.Table; }
		}
	}
	
	public class ViewSchemaContainer : ISchemaContainer
	{
		protected ViewSchema schema;
		
		public ViewSchemaContainer (ViewSchema schema)
		{
			if (schema == null)
				throw new ArgumentNullException ("schema");
			
			this.schema = schema;
		}
		
		public ISchema Schema {
			get { return schema; }
		}
		
		public ColumnSchemaCollection Columns {
			get { return schema.Columns; }
		}
		
		public ParameterSchemaCollection Parameters {
			get { return null; }
		}
		
		public SchemaContainerType SchemaContainerType {
			get { return SchemaContainerType.View; }
		}
	}
	
	public class ProcedureSchemaContainer : ISchemaContainer
	{
		protected ProcedureSchema schema;
		
		public ProcedureSchemaContainer (ProcedureSchema schema)
		{
			if (schema == null)
				throw new ArgumentNullException ("schema");
			
			this.schema = schema;
		}
		
		public ISchema Schema {
			get { return schema; }
		}
		
		public ParameterSchemaCollection Parameters {
			get { return schema.Parameters; }
		}
		
		public SchemaContainerType SchemaContainerType {
			get { return SchemaContainerType.Procedure; }
		}
		
		public ColumnSchemaCollection Columns {
			get { throw new NotImplementedException (); }
		}
	}
}