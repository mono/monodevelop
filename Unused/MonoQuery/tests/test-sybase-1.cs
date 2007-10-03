// test-sybase-1.cs
//
// $ mcs test-sybase-1.cs /r:System.Data /r:Mono.Data.SybaseClient /r:Mono.Data.Sql
//

using System;
using System.Data;
using Mono.Data.SybaseClient;
using System.Text;
using Mono.Data.Sql;

namespace Mono.Data.Sql.Tests
{
	public class CreateProviderTest
	{
		static SybaseDbProvider provider = null;

		static TableSchema[] tables = null;
		static ViewSchema[] views = null;
		
		public static void Main(string[] args) 
		{	
			Console.WriteLine("Test Sybase Meta Data Provider...");

			provider = new SybaseDbProvider ();
			provider.ConnectionString = "Server=DANPC,5000;Database=testdb;User ID=sa;Password=;";

			Console.WriteLine("Opening database...");
			provider.Open ();
			
			Console.WriteLine("Do tests...");			

			ListTables ();
			ListViews ();

			tables = null;
			views = null;

			Console.WriteLine("Close provider...");			

			provider.Close();

			Console.WriteLine("Test Done.");
		}
		
		public static void ListTables ()
		{
			Console.WriteLine("List Tables...");
			tables = provider.GetTables ();
			Console.WriteLine("Tables Found: {0}", tables.Length);
			
			for (int i = 0; i < tables.Length; i++) {
				TableSchema table = tables[i];
				Console.WriteLine("  Table{0} Owner: {1} Name: {2} ", 
					i, table.OwnerName, table.Name);
				if (i == 0)
					ListTableColumns(table);
			}
		}

		public static void ListTableColumns (TableSchema table)
		{
			ColumnSchema[] columns = table.Columns;

			for (int c = 0; c < columns.Length; c++) {
				ColumnSchema column = columns[c];
				Console.WriteLine("Column{0}: ", c);
				Console.WriteLine("  Name: {0}", column.Name);
				Console.WriteLine("  DataTypeName: {0}", column.DataTypeName);
				Console.WriteLine("  Length: {0}", column.Length);
				//Console.WriteLine("  Precision: {0}", column.Precision);
				//Console.WriteLine("  Scale: {0}", column.Scale);
				//Console.WriteLine("  NotNull: {0}", column.NotNull);
				Console.WriteLine("");
			}
		}

		public static void ListViews () 
		{
			Console.WriteLine("List Views...");

			views = provider.GetViews ();
			for (int v = 0; v < views.Length; v++) {
				ViewSchema view = views[v];
				Console.WriteLine("View{0}: " ,v);
				Console.WriteLine("  Name: {0}", view.Name);
				Console.WriteLine("  Owner: {0}", view.OwnerName);
				if (v == 0)
					ListView (view);
			}
		}

		public static void ListView (ViewSchema view) 
		{
			Console.WriteLine("View Definition:\n" + view.Definition);
		}
	}
}

