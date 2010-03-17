//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
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

using MonoDevelop.Core;
using MonoDevelop.Database.Components;
using MonoDevelop.Database.Sql;
using System;
using System.Collections.Generic;
using System.Data;
namespace MonoDevelop.Database.Sql.Npgsql
{
	public class NpgsqlDbFactory : IDbFactory, IDbLinq
	{
		private ISqlDialect dialect;
		private IConnectionProvider connectionProvider;
		private IGuiProvider guiProvider;
		
		public string Identifier {
			get { return "Npgsql"; }
		}
		
		public string Name {
			get { return AddinCatalog.GetString ("PostgreSQL database"); }
		}
		
		public ISqlDialect Dialect {
			get {
				if (dialect == null)
					dialect = new NpgsqlDialect ();
				return dialect;
			}
		}
		
		public IConnectionProvider ConnectionProvider {
			get {
				if (connectionProvider == null)
					connectionProvider = new NpgsqlConnectionProvider ();
				return connectionProvider;
			}
		}
		
		public IGuiProvider GuiProvider {
			get {
				if (guiProvider == null)
					guiProvider = new NpgsqlGuiProvider ();
				return guiProvider;
			}
		}
		
		public IConnectionPool CreateConnectionPool (DatabaseConnectionContext context)
		{
			return new DefaultConnectionPool (this, ConnectionProvider, context);
		}
		
		public ISchemaProvider CreateSchemaProvider (IConnectionPool connectionPool)
		{
			return new NpgsqlSchemaProvider (connectionPool);
		}
		
		public DatabaseConnectionSettings GetDefaultConnectionSettings ()
		{
			DatabaseConnectionSettings settings = new DatabaseConnectionSettings ();
			settings.ProviderIdentifier = Identifier;
			settings.Server = "localhost";
			settings.Port = 5432;
			settings.Username = "postgres";
			settings.MaxPoolSize = 5;
			
			return settings;
		}
		
		#region IDbLinq implementation
		public bool Generate (DatabaseConnectionSettings connection, string outputType, string outputFile, string language,
		                      string style, string defaultNamespace, string entityBase, string entityAttr, 
		                      string membersAttr, string generateTypes, string culture, bool generateSchema, 
		                      bool generateTimestamp, bool overrideEqualAndHash, bool extractProcedures, bool pluralize)
		{
			return SqlMetalServices.Generate (Provider, connection, outputType, outputFile, language, style, defaultNamespace,
			                           entityBase, entityAttr, membersAttr, generateTypes, culture, generateSchema,
			                           generateTimestamp, overrideEqualAndHash, extractProcedures, pluralize);
		}
		
		
		public bool Generate (DatabaseConnectionSettings connection, string outputType, string outputFile, string defaultNamespace, 
		                      string entityBase, string entityAttr, string membersAttr, string generateTypes, 
		                      string culture, bool generateSchema, bool generateTimestamp, bool overrideEqualAndHash, 
		                      bool extractProcedures, bool pluralize)
		{
			return SqlMetalServices.Generate (Provider, connection, outputType, outputFile, defaultNamespace,
			                           entityBase, entityAttr, membersAttr, generateTypes, culture, generateSchema,
			                           generateTimestamp, overrideEqualAndHash, extractProcedures, pluralize);		}
		
		public string Provider {
			get {
				 return "PostgreSql";
			}
		}
		
		
		public bool HasProcedures {
			get {
				return true;
			}
		}
		
		#endregion
		
	}
}