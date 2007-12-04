//
// DbProviderBase.cs
//
// Authors:
// Christian Hergert	<chris@mosaix.net>
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
using System.Collections;
using System.Data;
using System.Threading;

namespace Mono.Data.Sql
{
	public delegate void DbProviderChangedEventHandler (object sender, DbProviderChangedArgs args);
	public delegate void SQLCallback (object sender, object Results);
	
	public class DbProviderChangedArgs : EventArgs
	{
		public DbProviderBase Provider = null;

		public DbProviderChangedArgs (DbProviderBase provider)
		{
			Provider = provider;
		}
	}
	
	[Serializable]
	public abstract class DbProviderBase
	{
		public event DbProviderChangedEventHandler NameChanged;
		public event DbProviderChangedEventHandler StateChanged;
		public event DbProviderChangedEventHandler Refreshed;
		
		protected string name = String.Empty;

		#region // Threading objects
		Object ThreadSync = new Object ();
		SQLCallback ThreadedSQLCallback;
		TableSchema ThreadedTableSchema;
		ViewSchema ThreadedViewSchema;
		ProcedureSchema ThreadedProcedureSchema;
		String ThreadedSQLText = String.Empty;
		#endregion // End of Threading objects
		
		public DbProviderBase ()
		{
		}
		
		public virtual string ProviderName {
			get {
				return "Unknown Provider";
			}
		}
		
		public virtual string Name {
			get {
				return name;
			}
			set {
				name = value;
				if (NameChanged != null)
					NameChanged (this, new DbProviderChangedArgs (this));
			}
		}
		
		public virtual IDbConnection Connection {
			get {
				return (IDbConnection) null;
			}
		}
		
		public virtual string ConnectionString {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}
		
		public virtual bool IsConnectionStringWrong {
			get {
				return true;
			}
		}
		
		public virtual bool IsOpen {
			get {
				return false;
			}
		}
		
		public virtual bool CanExplain {
			get {
				return true;
			}
		}
		
		public virtual bool Open ()
		{
			return IsOpen;
		}
		
		public virtual void Close ()
		{
			throw new NotImplementedException ();
		}
		
		public virtual void Refresh ()
		{
			if (Refreshed != null)
				Refreshed (this, new DbProviderChangedArgs (this));
		}
		
		public virtual bool SupportsSchemaType (Type type)
		{
			return false;
		}
		
		public virtual void ExecuteSQL (string SQLText, SQLCallback Callback)
		{
			lock (ThreadSync) {
				ThreadedSQLCallback = Callback;
				ThreadedSQLText = SQLText;
				Thread eThread = new Thread (new ThreadStart (ExecuteSQLThreadStart));
				eThread.Start ();
			}
		}

		public virtual DataTable ExecuteSQL (string SQLText)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void ExplainSQL (string SQLText, SQLCallback Callback)
		{
			if (CanExplain != true)
				return;
			
			lock (ThreadSync) {
				ThreadedSQLCallback = Callback;
				ThreadedSQLText = SQLText;
				Thread eThread = new Thread (new ThreadStart (ExplainSQLThreadStart));
				eThread.Start ();
			}
		}
		
		public virtual DataTable ExplainSQL (string SQLText)
		{
			if (CanExplain == false)
				return null;
			
			return ExecuteSQL (String.Format ("EXPLAIN {0}", SQLText));
		}
		
		public virtual void GetTables (SQLCallback Callback)
		{
			Thread eThread = null;
			lock (ThreadSync) {
				ThreadedSQLCallback = Callback;
				eThread = new Thread (new ThreadStart (GetTablesThreadStart));
				eThread.Start ();
			}
		}
		
		public virtual TableSchema[] GetTables ()
		{
			throw new NotImplementedException ();
		}
		
		public virtual void GetTableColumns (SQLCallback Callback, TableSchema schema)
		{
			Thread eThread = null;
			lock (ThreadSync) {
				ThreadedSQLCallback = Callback;
				ThreadedTableSchema = schema;
				eThread = new Thread (new ThreadStart (GetTableColumnsThreadStart));
				eThread.Start ();
			}
		}

		public virtual ColumnSchema[] GetTableColumns (TableSchema schema)
		{
			throw new NotImplementedException ();
		}

		public virtual void GetTableConstraints (SQLCallback Callback, TableSchema schema)
		{
			Thread eThread = null;
			lock (ThreadSync) {
				ThreadedSQLCallback = Callback;
				ThreadedTableSchema = schema;
				eThread = new Thread (new ThreadStart (GetTableConstraintsThreadStart));
				eThread.Start ();
			}
		}
		
		public virtual ConstraintSchema[] GetTableConstraints (TableSchema schema)
		{
			throw new NotImplementedException ();
		}

		public virtual void GetViews (SQLCallback Callback)
		{
			Thread eThread = null;
			lock (ThreadSync) {
				ThreadedSQLCallback = Callback;
				eThread = new Thread (new ThreadStart (GetViewsThreadStart));
				eThread.Start ();
			}
		}
		
		public virtual ViewSchema[] GetViews ()
		{
			throw new NotImplementedException ();
		}

		public virtual void GetViewColumns (SQLCallback Callback, ViewSchema schema)
		{
			Thread eThread = null;
			lock (ThreadSync) {
				ThreadedSQLCallback = Callback;
				ThreadedViewSchema = schema;
				eThread = new Thread (new ThreadStart (GetViewColumnsThreadStart));
				eThread.Start ();
			}
		}

		public virtual ColumnSchema[] GetViewColumns (ViewSchema schema)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void GetProcedures (SQLCallback Callback)
		{
			Thread eThread = null;
			lock (ThreadSync) {
				ThreadedSQLCallback = Callback;
				eThread = new Thread (new ThreadStart (GetProceduresThreadStart));
				eThread.Start ();
			}
		}
		
		public virtual ProcedureSchema[] GetProcedures ()
		{
			throw new NotImplementedException ();
		}
		
		public virtual void GetProcedureColumns (SQLCallback Callback, ProcedureSchema schema)
		{
			Thread eThread = null;
			lock (ThreadSync) {
				ThreadedSQLCallback = Callback;
				ThreadedProcedureSchema = schema;
				eThread = new Thread (new ThreadStart (
					GetProcedureColumnsThreadStart));
				eThread.Start ();
			}
		}
		
		public virtual ColumnSchema[] GetProcedureColumns (ProcedureSchema schema)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void GetUsers (SQLCallback Callback)
		{
			Thread eThread = null;
			lock (ThreadSync) {
				ThreadedSQLCallback = Callback;
				eThread = new Thread (new ThreadStart (GetUsersThreadStart));
				eThread.Start ();
			}
		}
		
		public virtual UserSchema[] GetUsers ()
		{
			throw new NotImplementedException ();
		}
		
		protected virtual void OnOpen ()
		{
			if (StateChanged != null)
				StateChanged (this, new DbProviderChangedArgs (this));
		}
		
		protected virtual void OnClose ()
		{
			if (StateChanged != null)
				StateChanged (this, new DbProviderChangedArgs (this));
		}
		
		protected virtual void ExecuteSQLThreadStart ()
		{
			string SQLText = ThreadedSQLText;
			SQLCallback Callback = ThreadedSQLCallback;
			Callback (this, ExecuteSQL (SQLText));
		}
		
		protected virtual void ExplainSQLThreadStart ()
		{
			string SQLText = ThreadedSQLText;
			SQLCallback Callback = ThreadedSQLCallback;
			Callback (this, ExplainSQL (SQLText));
		}
		
		protected virtual void GetTablesThreadStart ()
		{
			SQLCallback Callback = ThreadedSQLCallback;
			Callback (this, GetTables ());
		}

		protected virtual void GetTableColumnsThreadStart ()
		{
			SQLCallback Callback = ThreadedSQLCallback;
			TableSchema Table = ThreadedTableSchema;
			Callback (this, GetTableColumns (Table));
		}

		protected virtual void GetTableConstraintsThreadStart ()
		{
			SQLCallback Callback = ThreadedSQLCallback;
			TableSchema Table = ThreadedTableSchema;
			Callback (this, GetTableConstraints (Table));
		}

		protected virtual void GetViewsThreadStart ()
		{
			SQLCallback Callback = ThreadedSQLCallback;
			Callback (this, GetViews ());
		}

		protected virtual void GetViewColumnsThreadStart ()
		{
			SQLCallback Callback = ThreadedSQLCallback;
			ViewSchema view = ThreadedViewSchema;
			Callback (this, GetViewColumns (view));
		}

		protected virtual void GetProceduresThreadStart ()
		{
			SQLCallback Callback = ThreadedSQLCallback;
			Callback (this, GetProcedures ());
		}
		
		protected virtual void GetProcedureColumnsThreadStart ()
		{
			SQLCallback Callback = ThreadedSQLCallback;
			ProcedureSchema schema = ThreadedProcedureSchema;
			Callback (this, GetProcedureColumns (schema));
		}

		protected virtual void GetUsersThreadStart ()
		{
			SQLCallback Callback = ThreadedSQLCallback;
			Callback (this, GetUsers ());
		}
	}
}