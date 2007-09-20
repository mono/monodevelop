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
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public sealed class DatabaseConnectionContext
	{
		public event EventHandler RefreshEvent;
		
		private bool temp;
		private DatabaseConnectionSettings connectionSettings;
		
		private IConnectionPool connectionPool;
		private ISchemaProvider schemaProvider;
		
		public DatabaseConnectionContext (DatabaseConnectionSettings connectionSettings)
			: this (connectionSettings, false)
		{
		}
		
		public DatabaseConnectionContext (DatabaseConnectionSettings connectionSettings, bool temp)
		{
			if (connectionSettings == null)
				throw new ArgumentNullException ("connectionSettings");
			
			this.connectionSettings = connectionSettings;
			this.temp = temp;
		}
		
		//temp connection contexts are not shown in the database pad
		public bool IsTemporary {
			get { return temp; }
			set { temp = false; }
		}
		
		public IDbFactory DbFactory {
			get { return DbFactoryService.GetDbFactory (connectionSettings.ProviderIdentifier); }
		}
		
		public DatabaseConnectionSettings ConnectionSettings {
			get { return connectionSettings; }
		}
		
		public IConnectionPool ConnectionPool {
			get {
				if (connectionPool == null)
					connectionPool = DbFactoryService.CreateConnectionPool (this);
				return connectionPool;
			}
		}
		
		public bool HasConnectionPool {
			get { return connectionPool != null; }
		}

		public ISchemaProvider SchemaProvider {
			get {
				if (ConnectionPool != null) {
					if (schemaProvider == null)
						schemaProvider = DbFactoryService.CreateSchemaProvider (this, connectionPool);
					return schemaProvider;
				}
				return null;
			}
		}
		
		public bool HasSchemaProvider {
			get { return schemaProvider != null; }
		}
		
		public void Refresh ()
		{
			if (RefreshEvent != null)
				RefreshEvent (this, EventArgs.Empty);
		}
	}
}