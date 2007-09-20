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
	public sealed class PooledCommand : IDbCommand
	{
		private IPooledDbConnection connection;
		private IDbCommand command;
		
		public PooledCommand (IPooledDbConnection connection, IDbCommand command)
		{
			if (connection == null)
				throw new ArgumentNullException ("connection");
			if (command == null)
				throw new ArgumentNullException ("command");
			
			this.connection = connection;
			this.command = command;
		}
		
		public string CommandText {
			get { return command.CommandText; }
			set { command.CommandText = value; }
		}
		
		public int CommandTimeout {
			get { return command.CommandTimeout; }
			set { command.CommandTimeout = value; }
		}
		
		public CommandType CommandType {
			get { return command.CommandType; }
			set { command.CommandType = value; }
		}
		
		public IDbConnection Connection {
			get { return command.Connection; }
			set { command.Connection = value; }
		}
		
		public IDataParameterCollection Parameters {
			get { return command.Parameters; }
		}
		
		public IDbTransaction Transaction {
			get { return command.Transaction; }
			set { command.Transaction = value; }
		}
		
		public UpdateRowSource UpdatedRowSource {
			get { return command.UpdatedRowSource; }
			set { command.UpdatedRowSource = value; }
		}
		
		public void Dispose ()
		{
			command.Dispose ();
			connection.Release ();
		}
		
		public void Cancel ()
		{
			command.Cancel ();
		}
		
		public IDbDataParameter CreateParameter ()
		{
			return command.CreateParameter ();
		}
		
		public int ExecuteNonQuery ()
		{
			return command.ExecuteNonQuery ();
		}
		
		public IDataReader ExecuteReader ()
		{
			return command.ExecuteReader ();
		}
		
		public IDataReader ExecuteReader (CommandBehavior behavior)
		{
			return command.ExecuteReader (behavior);
		}
		
		public object ExecuteScalar ()
		{
			return command.ExecuteScalar ();
		}
		
		public void Prepare ()
		{
			command.Prepare ();
		}
	}
}