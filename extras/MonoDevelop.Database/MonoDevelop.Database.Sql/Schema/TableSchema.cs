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
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	/// <summary>
	/// SQL schema object representing a table
	/// </summary>
	public class TableSchema : AbstractSchema
	{
		//TODO: support for inherited table schemas
		
		protected bool isSystemTable = false;
		protected string tableSpaceName;
		
		protected ColumnSchemaCollection columns;
		protected ConstraintSchemaCollection constraints;
		protected TriggerSchemaCollection triggers;
		
		public TableSchema (ISchemaProvider schemaProvider)
			: base (schemaProvider)
		{
		}
		
		public TableSchema (ISchemaProvider schemaProvider, string name)
			: base (schemaProvider)
		{
			Name = name;
			
			columns = new ColumnSchemaCollection ();
			constraints = new ConstraintSchemaCollection ();
			triggers = new TriggerSchemaCollection ();
		}
		
		public TableSchema (TableSchema table)
			: base (table)
		{
			isSystemTable = table.isSystemTable;
			tableSpaceName = table.tableSpaceName;
			columns = new ColumnSchemaCollection (table.columns);
			constraints = new ConstraintSchemaCollection (table.constraints);
			triggers = new TriggerSchemaCollection (table.triggers);
		}
		
		/// <summary>
		/// Is this table part of the database core
		/// </summary>
		public bool IsSystemTable {
			get {
				return isSystemTable;
			}
			set {
				isSystemTable = value;
				OnChanged ();
			}
		}
		
		public string TableSpaceName {
			get {
				return tableSpaceName;
			}
			set {
				tableSpaceName = value;
				OnChanged ();
			}
		}
		
		/// <summary>
		/// Override ToString() to show the tables FullName
		/// <remarks>
		/// Returns 'schema.tablename'
		/// </remarks>
		/// </summary>
		public override string ToString ()
		{
			return FullName;
		}
		
		/// <summary>
		/// Collection of columns associated with this table.
		/// </summary>
		public ColumnSchemaCollection Columns {
			get {
				if (columns == null)
					columns = provider.GetTableColumns(this);
				return columns;
			}
		}
		
		/// <summary>
		/// Collection of constraints associated with this table.
		/// </summary>
		public ConstraintSchemaCollection Constraints {
			get {
				if (constraints == null)
					constraints = provider.GetTableConstraints(this);
				return constraints;
			}
		}
		
		public TriggerSchemaCollection Triggers {
			get {
				if (triggers == null)
					triggers = provider.GetTableTriggers(this);
				return triggers;
			}
		}
		
		/// <summary>
		/// Refresh the information associated with this table.
		/// </summary>
		public override void Refresh()
		{
			// TODO: Update Name, etc.
			columns = null;
			constraints = null;
			triggers = null;
			definition = null;
		}
		
		public override object Clone ()
		{
			TableSchema clone = new TableSchema (this);
			if (clone.columns != null) {
				foreach (ColumnSchema column in clone.columns)
					column.Parent = clone;
			}
			return clone;
		}
	}
}
