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
using System.Threading;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Database.Sql
{
	public abstract class AbstractPooledDbConnection : IPooledDbConnection
	{
		protected IConnectionPool connectionPool;
		protected IDbConnection connection;
		
		protected bool disposed = false;
		
		protected AbstractPooledDbConnection (IConnectionPool connectionPool, IDbConnection connection)
		{
			this.connectionPool = connectionPool;
			this.connection = connection;
		}
		
		public virtual IConnectionPool ConnectionPool {
			get { return connectionPool; }
		}
		
		public virtual IDbConnection DbConnection {
			get { return connection; }
		}
		
		public virtual bool IsOpen {
			get { return connection.State == ConnectionState.Open; }
		}
		
		public virtual void Release ()
		{
			connectionPool.Release (this);
		}

		public virtual void Destroy ()
		{
			connection.Close ();
			connection.Dispose ();
			connection = null;
		}
		
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		
		protected virtual void Dispose (bool disposing)
		{
			if (!this.disposed) {
				if (disposing)
					Destroy ();
				disposed = true;
			}
		}
		
		public virtual IDbCommand CreateCommand (IStatement statement)
		{
			if (statement == null)
				throw new ArgumentNullException ("statement");
			
			string sql = connectionPool.DbFactory.Dialect.GetSql (statement);
			Runtime.LoggingService.DebugFormat ("Statement = {0}", sql);
			
			IDbCommand command = connection.CreateCommand ();
			command.CommandType = CommandType.Text;
			command.CommandText = sql;
			return command;
		}

		public virtual IDbCommand CreateCommand (string sql)
		{
			if (sql == null)
				throw new ArgumentNullException ("sql");
			
			IDbCommand command = connection.CreateCommand ();
			command.CommandType = CommandType.Text;
			command.CommandText = sql;
			return command;
		}
		
		public virtual IDbCommand CreateStoredProcedure (string procedure)
		{
			if (procedure == null)
				throw new ArgumentNullException ("procedure");
			
			IDbCommand command = connection.CreateCommand ();
			command.CommandType = CommandType.StoredProcedure;
			command.CommandText = procedure;
			return command;
		}
		
		public virtual int ExecuteNonQuery (string sql)
		{
			if (String.IsNullOrEmpty (sql))
				throw new ArgumentNullException ("sql");
			
			int result = -1;
			using (IDbCommand command = CreateCommand (sql))
				result = ExecuteNonQuery (command);
			return result;
		}

		public virtual object ExecuteScalar (string sql)
		{
			if (String.IsNullOrEmpty (sql))
				throw new ArgumentNullException ("sql");
			
			object result = null;
			using (IDbCommand command = CreateCommand (sql))
				result = ExecuteScalar (command);
			return result;
		}

		public virtual IDataReader ExecuteReader (string sql)
		{
			if (String.IsNullOrEmpty (sql))
				throw new ArgumentNullException ("sql");
			
			IDataReader result = null;
			using (IDbCommand command = CreateCommand (sql))
				result = ExecuteReader (command);
			return result;
		}
		
		public virtual DataSet ExecuteSet (string sql)
		{
			if (String.IsNullOrEmpty (sql))
				throw new ArgumentNullException ("sql");
			
			DataSet result = null;
			using (IDbCommand command = CreateCommand (sql))
				result = ExecuteSet (command);
			return result;
		}

		public virtual DataTable ExecuteTable (string sql)
		{
			if (String.IsNullOrEmpty (sql))
				throw new ArgumentNullException ("sql");
			
			DataTable result = null;
			using (IDbCommand command = CreateCommand (sql))
				result = ExecuteTable (command);
			return result;
		}
		
		public virtual int ExecuteNonQuery (IDbCommand command)
		{
			if (command == null)
				throw new ArgumentNullException ("command");

			try {
				return command.ExecuteNonQuery ();
			} catch (Exception e) {
				QueryService.RaiseException (e);
				return -1;
			}
		}

		public virtual object ExecuteScalar (IDbCommand command)
		{
			if (command == null)
				throw new ArgumentNullException ("command");

			try {
				return command.ExecuteScalar ();
			} catch (Exception e) {
				QueryService.RaiseException (e);
				return null;
			}
		}

		public virtual IDataReader ExecuteReader (IDbCommand command)
		{
			if (command == null)
				throw new ArgumentNullException ("command");

			try {
				return command.ExecuteReader ();
			} catch (Exception e) {
				QueryService.RaiseException (e);
				return null;
			}
		}

		public abstract DataSet ExecuteSet (IDbCommand command);
		public abstract DataTable ExecuteTable (IDbCommand command);
		
		public virtual void ExecuteNonQueryAsync (IDbCommand command, ExecuteCallback<int> callback, object state)
		{
			if (command == null)
				throw new ArgumentNullException ("command");
			if (callback == null)
				throw new ArgumentNullException ("callback");
			
			AsyncExecuteState<int> internalState = new AsyncExecuteState<int> (command, callback, state);
			ThreadPool.QueueUserWorkItem (new WaitCallback (ExecuteNonQueryThreaded), internalState);
		}

		public virtual void ExecuteScalarAsync (IDbCommand command, ExecuteCallback<object> callback, object state)
		{
			if (command == null)
				throw new ArgumentNullException ("command");
			if (callback == null)
				throw new ArgumentNullException ("callback");
			
			AsyncExecuteState<object> internalState = new AsyncExecuteState<object> (command, callback, state);
			ThreadPool.QueueUserWorkItem (new WaitCallback (ExecuteScalarThreaded), internalState);
		}

		public virtual void ExecuteReaderAsync (IDbCommand command, ExecuteCallback<IDataReader> callback, object state)
		{
			if (command == null)
				throw new ArgumentNullException ("command");
			if (callback == null)
				throw new ArgumentNullException ("callback");
			
			AsyncExecuteState<IDataReader> internalState = new AsyncExecuteState<IDataReader> (command, callback, state);
			ThreadPool.QueueUserWorkItem (new WaitCallback (ExecuteReaderThreaded), internalState);
		}

		public virtual void ExecuteSetAsync (IDbCommand command, ExecuteCallback<DataSet> callback, object state)
		{
			if (command == null)
				throw new ArgumentNullException ("command");
			if (callback == null)
				throw new ArgumentNullException ("callback");
			
			AsyncExecuteState<DataSet> internalState = new AsyncExecuteState<DataSet> (command, callback, state);
			ThreadPool.QueueUserWorkItem (new WaitCallback (ExecuteSetThreaded), internalState);
		}

		public virtual void ExecuteTableAsync (IDbCommand command, ExecuteCallback<DataTable> callback, object state)
		{
			if (command == null)
				throw new ArgumentNullException ("command");
			if (callback == null)
				throw new ArgumentNullException ("callback");
			
			AsyncExecuteState<DataTable> internalState = new AsyncExecuteState<DataTable> (command, callback, state);
			ThreadPool.QueueUserWorkItem (new WaitCallback (ExecuteTableThreaded), internalState);
		}
		
		public abstract DataTable GetSchema (string collectionName, params string[] restrictionValues);
		
		private void ExecuteNonQueryThreaded (object state)
		{
			AsyncExecuteState<int> internalState = state as AsyncExecuteState<int>;
			
			int result = ExecuteNonQuery (internalState.Command);
			internalState.Callback (this, result, internalState.State);
		}
		
		private void ExecuteScalarThreaded (object state)
		{
			AsyncExecuteState<object> internalState = state as AsyncExecuteState<object>;
			
			object result = ExecuteScalar (internalState.Command);
			internalState.Callback (this, result, internalState.State);
		}
		
		private void ExecuteReaderThreaded (object state)
		{
			AsyncExecuteState<IDataReader> internalState = state as AsyncExecuteState<IDataReader>;
			
			IDataReader result = ExecuteReader (internalState.Command);
			internalState.Callback (this, result, internalState.State);
		}
		
		private void ExecuteSetThreaded (object state)
		{
			AsyncExecuteState<DataSet> internalState = state as AsyncExecuteState<DataSet>;
			
			DataSet result = ExecuteSet (internalState.Command);
			internalState.Callback (this, result, internalState.State);
		}
		
		private void ExecuteTableThreaded (object state)
		{
			AsyncExecuteState<DataTable> internalState = state as AsyncExecuteState<DataTable>;
			
			DataTable result = ExecuteTable (internalState.Command);
			internalState.Callback (this, result, internalState.State);
		}
	}
			
	internal class AsyncExecuteState<T>
	{
		public AsyncExecuteState (IDbCommand command, ExecuteCallback<T> callback, object state)
		{
			Command = command;
			Callback = callback;
			State = state;
		}
		
		public IDbCommand Command;
		public ExecuteCallback<T> Callback;
		public object State;
	}
}
