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
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public abstract class AbstractConnectionProvider : IConnectionProvider
	{
		public abstract IPooledDbConnection CreateConnection (IConnectionPool pool, DatabaseConnectionSettings settings, out string error);

		public virtual bool CheckConnection (IPooledDbConnection connection, DatabaseConnectionSettings settings)
		{
			if (connection.IsOpen) {
				IDbConnection conn = connection.DbConnection;
				if (conn.Database == settings.Database) {
					return true;
				} else {
					try {
						conn.ChangeDatabase (settings.Database);
						return true;
					} catch {
						return false;
					}
				}
			}
			return false;
		}
		
		protected virtual string SetConnectionStringParameter (string connectionString, string quoteChar, string parameter, string value)
		{
			Regex regex = new Regex (parameter + "[ \t]*=[ \t]*" + quoteChar + "([a-zA-Z0-9_.]+)" + quoteChar, RegexOptions.IgnoreCase);
			Match match = regex.Match (connectionString);
			if (match.Success) {
				return connectionString.Substring (0, match.Index) + value + connectionString.Substring (match.Index + match.Length);
			} else {
				connectionString.TrimEnd ();
				return String.Concat (connectionString,
					connectionString.EndsWith (";") ? "" : ";",
					parameter, "=", quoteChar, value, quoteChar, ";");
			}
		}
	}
}
