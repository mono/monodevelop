//
// Authors:
//	Christian Hergert  <chris@mosaix.net>
//	Daniel Morgan <danielmorgan@verizon.net>
//	Sureshkumar T <tsureshkumar@novell.com>
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
using MySql.Data.MySqlClient;
using System.Collections.Generic;
namespace MonoDevelop.Database.Sql
{
	public class MySqlConnectionProvider : AbstractConnectionProvider
	{
		public override IPooledDbConnection CreateConnection (IConnectionPool pool, DatabaseConnectionSettings settings, out string error)
		{
			string connStr = null;
			try {	
				if (settings.UseConnectionString) {
					connStr = settings.ConnectionString;
				} else {
					//"Server=Server;Port=1234;Database=Test;Uid=UserName;Pwd=asdasd;"
					//Default port is 3306. Enter value -1 to use a named pipe connection. 
					connStr = String.Format ("Server={0};Port={1};Database={2};Uid={3};Pwd={4};",
						settings.Server, settings.Port, settings.Database, settings.Username, settings.Password);
				}
				connStr = SetConnectionStringParameter (connStr, String.Empty, "Pooling", "false");
				MySqlConnection connection = new MySqlConnection (connStr);
				connection.Open ();
				
				error = null;
				return new MySqlPooledDbConnection (pool, connection);
			} catch (Exception e) {
				error = e.Message;
				return null;
			}
		}
	}
}
