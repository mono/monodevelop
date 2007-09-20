//
// Authors:
//	Ben Motmans  <ben.motmans@gmail.com>
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
using System.Data;
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public class SelectStatement : IStatement
	{
		protected List<IdentifierExpression> columns;
		protected FromClause from;
		protected WhereClause where;
		protected OrderByClause orderBy;
		protected GroupByClause groupBy;
		protected HavingClause having;
		protected UnionClause union;
		protected JoinClause join;

		public SelectStatement (FromClause from)
		{
			if (from == null)
				throw new ArgumentNullException ("from");
			From = from;
			this.columns = new List<IdentifierExpression> ();
			this.columns.Add (new IdentifierExpression ());
		}

		public SelectStatement (FromClause from, IEnumerable<IdentifierExpression> columns)
		{
			if (from == null)
				throw new ArgumentNullException ("from");
			if (columns == null)
				throw new ArgumentNullException ("columns");
			
			From = from;
			this.columns = new List<IdentifierExpression> ();
			this.columns.AddRange (columns);
		}
		
		public List<IdentifierExpression> Columns {
			get { return columns; }
		}

		public FromClause From {
			get { return from; }
			set {
				if (value == null)
					throw new ArgumentNullException ("from");
				this.from = value;
			}
		}

		public WhereClause Where {
			get { return where; }
			set { where = value; }
		}

		public OrderByClause OrderBy {
			get { return orderBy; }
			set { orderBy = value; }
		}

		public GroupByClause GroupBy {
			get { return groupBy; }
			set { groupBy = value; }
		}

		public HavingClause Having {
			get { return having; }
			set { having = value; }
		}

		public UnionClause Union {
			get { return union; }
			set { union = value; }
		}
		
		public JoinClause Join {
			get { return join; }
			set { join = value; }
		}
	}
}