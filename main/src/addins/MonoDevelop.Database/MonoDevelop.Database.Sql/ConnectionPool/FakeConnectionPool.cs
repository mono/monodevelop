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
	public class FakeConnectionPool : IConnectionPool
	{
		protected IDbFactory factory;
		protected DatabaseConnectionContext context;
		protected IConnectionProvider connectionProvider;
		
		protected IPooledDbConnection connection;
		protected bool inUse = false;
		
		protected bool hasErrors;
		protected string error;
		
		protected bool isInitialized;
		
		public FakeConnectionPool (IDbFactory factory, IConnectionProvider connectionProvider, DatabaseConnectionContext context)
		{
			if (factory == null)
				throw new ArgumentNullException ("factory");
			if (connectionProvider == null)
				throw new ArgumentNullException ("connectionProvider");
			if (context == null)
				throw new ArgumentNullException ("context");
			
			this.factory = factory;
			this.connectionProvider = connectionProvider;
			this.context = context;
		}
		
		public virtual IDbFactory DbFactory {
			get { return factory; }
		}
		
		public virtual DatabaseConnectionContext ConnectionContext {
			get { return context; }
		}
		
		public virtual IConnectionProvider ConnectionProvider {
			get { return connectionProvider; }
		}
		
		public virtual bool IsInitialized {
			get { return isInitialized; }
		}
		
		public virtual bool HasErrors {
			get { return hasErrors; }
		}
		
		public virtual string Error {
			get { return error; }
		}
		
		public virtual int MinSize {
			get { return 1; }
			set { throw new NotImplementedException (); }
		}
		
		public virtual int MaxSize {
			get { return 1; }
			set { throw new NotImplementedException (); }
		}

		public virtual int GrowSize {
			get { throw new NotImplementedException (); }
			set { throw new NotImplementedException (); }
		}

		public virtual int ShrinkSize {
			get { throw new NotImplementedException (); }
			set { throw new NotImplementedException (); }
		}
		
		public virtual int ConnectionCount {
			get { return 1; }
		}

		public virtual int FreeConnectionCount {
			get { return (connection == null || (connection.IsOpen && !inUse)) ? 1 : 0; }
		}

		public virtual bool HasFreeConnection {
			get { return FreeConnectionCount == 1; }
		}
		
		public virtual IPooledDbConnection Request ()
		{
			Initialize (); //this does nothing when already initialized, so it's safe to call every time

			if (HasFreeConnection) {
				inUse = true;
				return connection;
			} else {
				throw new InvalidOperationException ("No connection available.");
			}
		}

		public virtual void Release (IPooledDbConnection connection)
		{
			if (this.connection == connection && connection != null)
				inUse = false;
		}
		
		public virtual bool Initialize ()
		{
			if (isInitialized)
				return true;
			
			connection = connectionProvider.CreateConnection (this, context.ConnectionSettings, out error);
			if (connection == null) {
				hasErrors = true;
				return false;
			}
			
			hasErrors = false;
			isInitialized = true;
			return true;
		}
		
		public virtual void Close ()
		{
			if (connection != null)
				connection.Dispose ();
			
			isInitialized = false;
		}
	}
}