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
using System.Data;
using Npgsql;
using System.Collections.Generic;
namespace MonoDevelop.Database.Sql
{
	public class NpgsqlConnectionProvider : AbstractConnectionProvider
	{
		public override IPooledDbConnection CreateConnection (IConnectionPool pool, DatabaseConnectionSettings settings, out string error)
		{
			string connStr = null;
			try {	
				if (settings.UseConnectionString) {
					connStr = settings.ConnectionString;
				} else {
					//User ID=root;Password=myPassword;Host=localhost;Port=5432;Database=myDataBase;Pooling=true;Min Pool Size=0;Max Pool Size=100;Connection Lifetime=0;
					connStr = String.Format ("User ID={0};Password={1};Host={2};Port={3};Database={4};",
						settings.Username, settings.Password, settings.Server, settings.Port, settings.Database);
				}
				connStr = SetConnectionStringParameter (connStr, String.Empty, "Pooling", "false");
				NpgsqlConnection connection = new NpgsqlConnection (connStr);
				connection.Open ();
				
				error = null;
				return new NpgsqlPooledDbConnection (pool, connection);
			} catch (Exception e) {
				error = e.Message;
				return null;
			}
		}
	}
}
