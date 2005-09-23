//
// FakeNodes.cs
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

using Mono.Data.Sql;

namespace MonoQuery
{
	public class TablesNode
	{
		public event EventHandler RefreshEvent;
		
		public DbProviderBase Provider = null;
		
		public TablesNode (DbProviderBase provider)
		{
			Provider = provider;
		}
		
		public void Refresh ()
		{
			if (RefreshEvent != null)
				RefreshEvent (this, null);
		}
	}
	
	public class ViewsNode
	{
		public DbProviderBase Provider = null;
		
		public ViewsNode (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
	
	public class ProceduresNode
	{
		public DbProviderBase Provider = null;
		
		public ProceduresNode (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
	
	public class AggregatesNode
	{
		public DbProviderBase Provider = null;
		
		public AggregatesNode (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
	
	public class GroupsNode
	{
		public DbProviderBase Provider = null;
		
		public GroupsNode (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
	
	public class LanguagesNode
	{
		public DbProviderBase Provider = null;
		
		public LanguagesNode (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
	
	public class OperatorsNode
	{
		public DbProviderBase Provider = null;
		
		public OperatorsNode (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
	
	public class RulesNode
	{
		public DbProviderBase Provider = null;
		
		public RulesNode (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
	
	public class RolesNode
	{
		public DbProviderBase Provider = null;
		
		public RolesNode (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
	
	public class SequencesNode
	{
		public DbProviderBase Provider = null;
		
		public SequencesNode (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
	
	public class UsersNode
	{
		public DbProviderBase Provider = null;
		
		public UsersNode (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
	
	public class TypesNode
	{
		public DbProviderBase Provider = null;
		
		public TypesNode (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
	
	public class ColumnsNode
	{
		public DbProviderBase Provider = null;
		public ISchema Parent = null;
		
		public ColumnsNode (DbProviderBase provider, ISchema parent)
		{
			Provider = provider;
			Parent = parent;
		}
	}
	
	public class ConstraintsNode
	{
		public DbProviderBase Provider = null;
		public TableSchema Table = null;
		
		public ConstraintsNode (DbProviderBase provider, TableSchema table)
		{
			Provider = provider;
			Table = table;
		}
	}
	
	public class TriggersNode
	{
		public DbProviderBase Provider = null;
		
		public TriggersNode (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
	
	public class ParametersNode
	{
		public DbProviderBase Provider = null;
		
		public ParametersNode (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
}