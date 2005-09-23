//
// This test aims to test as much of the NpgsqlDbProvider as possbile.
// It requires a local setup of postgres with user and db chris and
// the connection should be trusted via 127.0.0.1. A script may be
// provided at some point to make these tests possible by others.
//

using System;

using NUnit.Framework;
using Mono.Data.Sql;

namespace Mono.Data.Sql.Tests
{
	[TestFixture]
	public class CreateProviderTests
	{
		public CreateProviderTests ()
		{
		}
		
		[TestFixtureSetUp]
		public void Initialize()
		{
		}
		
		[TestFixtureTearDown]
		public void Dispose()
		{
		}
		
		[Test]
		public void CreateNpgsqlProvider ()
		{
			NpgsqlDbProvider provider = new NpgsqlDbProvider ();
			Assert.IsNotNull (provider);
		}
		
		[Test]
		public void CreateMySqlProvider ()
		{
			MySqlDbProvider provider = new MySqlDbProvider ();
			Assert.IsNotNull (provider);
		}
		
		[Test]
		public void CreateSqliteProvider ()
		{
			SqliteDbProvider provider = new SqliteDbProvider ();
			Assert.IsNotNull (provider);
		}
	}
}
