//
// This test should try to load tables and check their contents for npgsql.
//

using System;

using Mono.Data.Sql;
using NUnit.Framework;

namespace Mono.Data.Sql.Tests
{
	[TestFixture]
	public class NpgsqlTablesTest
	{
		NpgsqlDbProvider provider = null;
		
		[TestFixtureSetUp]
		public void SetUp ()
		{
			provider = new NpgsqlDbProvider ();
			provider.ConnectionString =
				"Server=localhost;User ID=chris;Database=chris;";
			provider.Open ();
		}
		
		[Test]
		public void GetTablesTest ()
		{
			TableSchema[] tables = provider.GetTables ();
			Assert.IsTrue (tables.Length > 0);
		}
	}
}