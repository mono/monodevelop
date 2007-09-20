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
	public delegate void ExecuteCallback<T> (IPooledDbConnection connection, T result, object state);
	
	public interface IPooledDbConnection : IDisposable
	{
		IConnectionPool ConnectionPool { get; }
		
		IDbConnection DbConnection { get; }
		
		bool IsOpen { get; }
		
		void Release ();

		void Destroy ();
		
		IDbCommand CreateCommand (IStatement statement);
		IDbCommand CreateCommand (string sql);
		
		IDbCommand CreateStoredProcedure (string sql);
		
		int ExecuteNonQuery (string sql);
		object ExecuteScalar (string sql);
		IDataReader ExecuteReader (string sql);
		DataSet ExecuteSet (string sql);
		DataTable ExecuteTable (string sql);
		
		int ExecuteNonQuery (IDbCommand command);
		object ExecuteScalar (IDbCommand command);
		IDataReader ExecuteReader (IDbCommand command);
		DataSet ExecuteSet (IDbCommand command);
		DataTable ExecuteTable (IDbCommand command);
		
		void ExecuteNonQueryAsync (IDbCommand command, ExecuteCallback<int> callback, object state);
		void ExecuteScalarAsync (IDbCommand command, ExecuteCallback<object> callback, object state);
		void ExecuteReaderAsync (IDbCommand command, ExecuteCallback<IDataReader> callback, object state);
		void ExecuteSetAsync (IDbCommand command, ExecuteCallback<DataSet> callback, object state);
		void ExecuteTableAsync (IDbCommand command, ExecuteCallback<DataTable> callback, object state);
		
		DataTable GetSchema (string collectionName, params string[] restrictionValues);
	}
}
