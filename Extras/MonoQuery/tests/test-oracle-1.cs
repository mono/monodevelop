// test-oracle-1.cs
//
// $ mcs test-oracle-1.cs /r:System.Data /r:System.Data.OracleClient /r:Mono.Data.Sql
//

using System;
using System.Data;
using System.Data.OracleClient;
using System.Text;
using Mono.Data.Sql;

namespace Mono.Data.Sql.Tests
{
	public class CreateProviderTest
	{
		static OracleDbProvider provider = null;

		static TableSchema[] tables = null;
		static ViewSchema[] views = null;
		
		public static void Main(string[] args) 
		{	
			Console.WriteLine("Test Oracle Meta Data Provider...");

			provider = new OracleDbProvider ();
			provider.ConnectionString = "Data Source=palis;user id=scott;password=tiger";
			provider.Open ();
			
			ListTables ();
			ListViews ();

			tables = null;
			views = null;

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
				Console.WriteLine("  Table{0} Owner: {1} Name: {2} TableSpace: {3}", 
					i, table.OwnerName, table.Name, table.TableSpaceName);
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
				Console.WriteLine("  Precision: {0}", column.Precision);
				Console.WriteLine("  Scale: {0}", column.Scale);
				Console.WriteLine("  NotNull: {0}", column.NotNull);
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

