//
// Authors:
//   Christian Hergert	<chris@mosaix.net>
//   Ben Motmans  <ben.motmans@gmail.com>
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
using MonoDevelop.Database.Sql;

namespace MonoDevelop.Database.ConnectionManager
{
	public abstract class BaseNode
	{
		public event EventHandler RefreshEvent;
		protected DatabaseConnectionContext context;
		
		public BaseNode (DatabaseConnectionContext context)
		{
			if (context == null)
				throw new ArgumentNullException ("context");
			
			this.context = context;
		}
		
		public DatabaseConnectionContext ConnectionContext {
			get { return context; }
		}
		
		public void Refresh ()
		{
			if (RefreshEvent != null)
				RefreshEvent (this, EventArgs.Empty);
		}
	}
	
	public class TableNode : BaseNode
	{
		protected TableSchema table;
		
		public TableNode (DatabaseConnectionContext context, TableSchema table)
			: base (context)
		{
			if (table == null)
				throw new ArgumentNullException ("table");
			
			this.table = table;
		}
		
		public TableSchema Table {
			get { return table; }
		}
	}
	
	public class TablesNode : BaseNode
	{
		public TablesNode (DatabaseConnectionContext context)
			: base (context)
		{
		}
	}
	
	public class ViewNode : BaseNode
	{
		protected ViewSchema view;
		
		public ViewNode (DatabaseConnectionContext context, ViewSchema view)
			: base (context)
		{
			if (view == null)
				throw new ArgumentNullException ("view");
			
			this.view = view;
		}
		
		public ViewSchema View {
			get { return view; }
		}
	}
	
	public class ViewsNode : BaseNode
	{
		public ViewsNode (DatabaseConnectionContext context)
			: base (context)
		{
		}
	}
	
	public class ProcedureNode : BaseNode
	{
		protected ProcedureSchema procedure;
		
		public ProcedureNode (DatabaseConnectionContext context, ProcedureSchema procedure)
			: base (context)
		{
			if (procedure == null)
				throw new ArgumentNullException ("procedure");
			
			this.procedure = procedure;
		}
		
		public ProcedureSchema Procedure {
			get { return procedure; }
		}
	}
	
	public class ProceduresNode : BaseNode
	{
		public ProceduresNode (DatabaseConnectionContext context)
			: base (context)
		{
		}
	}
	
	public class AggregatesNode : BaseNode
	{
		public AggregatesNode (DatabaseConnectionContext context)
			: base (context)
		{
		}
	}
	
	public class GroupsNode : BaseNode
	{
		public GroupsNode (DatabaseConnectionContext context)
			: base (context)
		{
		}
	}
	
	public class LanguagesNode : BaseNode
	{
		public LanguagesNode (DatabaseConnectionContext context)
			: base (context)
		{
		}
	}
	
	public class OperatorsNode : BaseNode
	{
		public OperatorsNode (DatabaseConnectionContext context)
			: base (context)
		{
		}
	}
	
	public class RulesNode : BaseNode
	{
		public RulesNode (DatabaseConnectionContext context)
			: base (context)
		{
		}
	}

	public class RolesNode : BaseNode
	{
		public RolesNode (DatabaseConnectionContext context)
			: base (context)
		{
		}
	}
	
	public class SequencesNode : BaseNode
	{
		public SequencesNode (DatabaseConnectionContext context)
			: base (context)
		{
		}
	}
	
	public class UserNode : BaseNode
	{
		protected UserSchema user;
		
		public UserNode (DatabaseConnectionContext context, UserSchema user)
			: base (context)
		{
			if (user == null)
				throw new ArgumentNullException ("user");
			
			this.user = user;
		}
		
		public UserSchema User {
			get { return user; }
		}
	}
	
	public class UsersNode : BaseNode
	{
		public UsersNode (DatabaseConnectionContext context)
			: base (context)
		{
		}
	}

	public class TypesNode : BaseNode
	{
		public TypesNode (DatabaseConnectionContext context)
			: base (context)
		{
		}
	}
	
	public class ColumnNode : BaseNode
	{
		protected ColumnSchema column;
		
		public ColumnNode (DatabaseConnectionContext context, ColumnSchema column)
			: base (context)
		{
			if (column == null)
				throw new ArgumentNullException ("column");
			
			this.column = column;
		}
		
		public ColumnSchema Column {
			get { return column; }
		}
	}

	public class ColumnsNode : BaseNode
	{
		protected ISchema schema;
		
		public ColumnsNode (DatabaseConnectionContext context, ISchema schema)
			: base (context)
		{
			if (schema == null)
				throw new ArgumentNullException ("schema");
			
			this.schema = schema;
		}
		
		public ISchema Schema {
			get { return schema; }
		}
	}
	
	public class ConstraintsNode : BaseNode
	{
		protected ISchema schema;
		
		public ConstraintsNode (DatabaseConnectionContext context, ISchema schema)
			: base (context)
		{
			if (schema == null)
				throw new ArgumentNullException ("schema");
			
			this.schema = schema;
		}
		
		public ISchema Schema {
			get { return schema; }
		}
	}
	
	public class TriggersNode : BaseNode
	{
		public TriggersNode (DatabaseConnectionContext context)
			: base (context)
		{
		}
	}
	
	public class ParametersNode : BaseNode
	{
		private ProcedureSchema procedure;
		
		public ParametersNode (DatabaseConnectionContext context, ProcedureSchema procedure)
			: base (context)
		{
			if (procedure == null)
				throw new ArgumentNullException ("procedure");
			
			this.procedure = procedure;
		}
		
		public ProcedureSchema Procedure {
			get { return procedure; }
		}
	}
}