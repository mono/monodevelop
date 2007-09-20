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
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Database.Sql
{
	public static class DbFactoryService
	{
		private static Dictionary<string, IDbFactory> factories;
		
		static DbFactoryService ()
		{
			factories = new Dictionary<string, IDbFactory> ();
			foreach (DbFactoryCodon codon in AddinManager.GetExtensionNodes ("/MonoDevelop/Database/Sql")) {
				IDbFactory fac = codon.DbFactory;
				if (fac != null) {
					factories.Add (fac.Identifier, fac);
					Runtime.LoggingService.Debug ("DB FACTORY: " + fac.Identifier);
				}
			}
		}

		public static IEnumerable<IDbFactory> DbFactories {
			get { return factories.Values; }
		}
		
		public static int DbFactoryCount {
			get { return factories.Count; }
		}
		
		public static IDbFactory GetDbFactory (string id)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			IDbFactory fac = null;
			if (factories.TryGetValue (id, out fac))
				return fac;
			return null;
		}
		
		public static IDbFactory GetDbFactory (DatabaseConnectionSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException ("settings");
			
			return GetDbFactory (settings.ProviderIdentifier);
		}
		
		public static IConnectionPool CreateConnectionPool (DatabaseConnectionContext context)
		{
			if (context == null)
				throw new ArgumentNullException ("context");
			
			IDbFactory fac = GetDbFactory (context.ConnectionSettings);
			if (fac != null)
				return fac.CreateConnectionPool (context);
			return null;
		}
		
		public static ISchemaProvider CreateSchemaProvider (DatabaseConnectionContext context, IConnectionPool pool)
		{
			if (context == null)
				throw new ArgumentNullException ("context");
			if (pool == null)
				throw new ArgumentNullException ("pool");
			
			IDbFactory fac = GetDbFactory (context.ConnectionSettings);
			if (fac != null)
				return fac.CreateSchemaProvider (pool);
			return null;
		}
	}
}
