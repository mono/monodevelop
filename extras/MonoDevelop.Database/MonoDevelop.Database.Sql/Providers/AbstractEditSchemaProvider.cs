//
// Authors:
//	Christian Hergert  <chris@mosaix.net>
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
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
using System.Text;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public abstract class AbstractEditSchemaProvider : AbstractSchemaProvider, IEditSchemaProvider
	{
		protected AbstractEditSchemaProvider (IConnectionPool connectionPool)
			: base (connectionPool)
		{
		}		
		
		public virtual void CreateDatabase (DatabaseSchema database)
		{
			throw new NotImplementedException ();
		}

		public virtual void CreateTable (TableSchema table)
		{
			string sql = GetTableCreateStatement (table);
			ExecuteNonQuery (sql);
		}

		public virtual void CreateView (ViewSchema view)
		{
			throw new NotImplementedException ();
		}

		public virtual void CreateProcedure (ProcedureSchema procedure)
		{
			throw new NotImplementedException ();
		}

		public virtual void CreateIndex (IndexSchema index)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void CreateTrigger (TriggerSchema trigger)
		{
			throw new NotImplementedException ();
		}

		public virtual void CreateUser (UserSchema user)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void AlterDatabase (DatabaseAlterSchema database)
		{
			throw new NotImplementedException ();
		}

		public virtual void AlterTable (TableAlterSchema table)
		{
			throw new NotImplementedException ();
		}

		public virtual void AlterView (ViewAlterSchema view)
		{
			throw new NotImplementedException ();
		}

		public virtual void AlterProcedure (ProcedureAlterSchema procedure)
		{
			throw new NotImplementedException ();
		}

		public virtual void AlterIndex (IndexAlterSchema index)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void AlterTrigger (TriggerAlterSchema trigger)
		{
			throw new NotImplementedException ();
		}

		public virtual void AlterUser (UserAlterSchema user)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void DropDatabase (DatabaseSchema database)
		{
			throw new NotImplementedException ();
		}

		public virtual void DropTable (TableSchema table)
		{
			throw new NotImplementedException ();
		}

		public virtual void DropView (ViewSchema view)
		{
			throw new NotImplementedException ();
		}

		public virtual void DropProcedure (ProcedureSchema procedure)
		{
			throw new NotImplementedException ();
		}

		public virtual void DropIndex (IndexSchema index)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void DropTrigger (TriggerSchema trigger)
		{
			throw new NotImplementedException ();
		}

		public virtual void DropUser (UserSchema user)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void RenameDatabase (DatabaseSchema database, string name)
		{
			throw new NotImplementedException ();
		}

		public virtual void RenameTable (TableSchema table, string name)
		{
			throw new NotImplementedException ();
		}

		public virtual void RenameView (ViewSchema view, string name)
		{
			throw new NotImplementedException ();
		}

		public virtual void RenameProcedure (ProcedureSchema procedure, string name)
		{
			throw new NotImplementedException ();
		}

		public virtual void RenameIndex (IndexSchema index, string name)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void RenameTrigger (TriggerSchema trigger, string name)
		{
			throw new NotImplementedException ();
		}

		public virtual void RenameUser (UserSchema user, string name)
		{
			throw new NotImplementedException ();
		}
		
		public virtual string GetTableAlterStatement (TableAlterSchema table)
		{
			throw new NotImplementedException ();
		}
		
		public virtual string GetViewAlterStatement (ViewSchema view)
		{
			throw new NotImplementedException ();
		}
		
		public virtual string GetProcedureAlterStatement (ProcedureSchema procedure)
		{
			throw new NotImplementedException ();
		}
	}
}
