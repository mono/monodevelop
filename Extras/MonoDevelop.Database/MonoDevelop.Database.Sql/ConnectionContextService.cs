//
// Authors:
//   Christian Hergert <chris@mosaix.net>
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2005 Christian Hergert
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
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Database.Sql
{
	public static class ConnectionContextService
	{
		public static event DatabaseConnectionContextEventHandler ConnectionContextAdded;
		public static event DatabaseConnectionContextEventHandler ConnectionContextRemoved;
		public static event DatabaseConnectionContextEventHandler ConnectionContextEdited;
		public static event DatabaseConnectionContextEventHandler ConnectionContextRefreshed;

		private static DatabaseConnectionContextCollection contexts;
		
		private static string configFile = null;

		static ConnectionContextService ()
		{
			configFile = Path.Combine (PropertyService.ConfigPath, "MonoDevelop.Database.ConnectionManager.xml");
			Initialize (configFile);
		}
		
		public static DatabaseConnectionContextCollection DatabaseConnections {
			get { return contexts; }
		}
		
		public static DatabaseConnectionContext AddDatabaseConnectionContext (DatabaseConnectionSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException ("settings");
			
			DatabaseConnectionContext context = new DatabaseConnectionContext (settings);
			AddDatabaseConnectionContext (context);
			return context;
		}
		
		public static void AddDatabaseConnectionContext (DatabaseConnectionContext context)
		{
			if (context == null)
				throw new ArgumentNullException ("context");
			
			if (!contexts.Contains (context)) {
				contexts.Add (context);
				Save ();
				if (ConnectionContextAdded != null)
					ConnectionContextAdded (null, new DatabaseConnectionContextEventArgs (context));
			}
		}
		
		public static void RemoveDatabaseConnectionContext (DatabaseConnectionContext context)
		{
			if (context == null)
				throw new ArgumentNullException ("context");
			
			contexts.Remove (context);
			Save ();
			if (ConnectionContextRemoved != null)
				ConnectionContextRemoved (null, new DatabaseConnectionContextEventArgs (context));
		}
		
		public static void EditDatabaseConnectionContext (DatabaseConnectionContext context)
		{
			if (context == null)
				throw new ArgumentNullException ("context");
			
			Save ();
			if (ConnectionContextEdited != null)
				ConnectionContextEdited (null, new DatabaseConnectionContextEventArgs (context));
		}
		
		internal static void Initialize (string configFile)
		{
			DatabaseConnectionSettingsCollection connections = null;
			if (File.Exists (configFile)) {
				try {
					using (FileStream fs = File.OpenRead (configFile)) {
						XmlSerializer serializer = new XmlSerializer (typeof (DatabaseConnectionSettingsCollection));
						connections = (DatabaseConnectionSettingsCollection) serializer.Deserialize (fs);
					}
				} catch {
					Runtime.LoggingService.Error (GettextCatalog.GetString ("Unable to load stored SQL connection information."));
					File.Delete (configFile);
				}
			}
			
			contexts = new DatabaseConnectionContextCollection ();
			if (connections != null) {
				StringBuilder sb = new StringBuilder ();
				foreach (DatabaseConnectionSettings settings in connections) {
					IDbFactory fac = DbFactoryService.GetDbFactory (settings);
					if (fac == null) {
						sb.Append ("Error: unable to load database provider '");
						sb.Append (settings.ProviderIdentifier);
						sb.Append ("' for connection '");
						sb.Append (settings.Name);
						sb.Append ("'");
						sb.Append (Environment.NewLine);
						continue;
					}
					
					contexts.Add (new DatabaseConnectionContext (settings));
				}
				
				if (sb.Length > 0) {
					Exception ex = new Exception (sb.ToString ());
					QueryService.RaiseException (ex);
				}
			}
		}

		public static void Save ()
		{
			//temporarily empty all passwords that don't need to be saved
			Dictionary<DatabaseConnectionSettings, string> tmp = new Dictionary<DatabaseConnectionSettings,string> ();
			DatabaseConnectionSettingsCollection collection = new DatabaseConnectionSettingsCollection ();
			foreach (DatabaseConnectionContext context in contexts) {
				if (!context.ConnectionSettings.SavePassword) {
					tmp.Add (context.ConnectionSettings, context.ConnectionSettings.Password);
					context.ConnectionSettings.Password = null;
				}
				collection.Add (context.ConnectionSettings);
			}

			using (FileStream fs = new FileStream (configFile, FileMode.Create)) {
				XmlSerializer serializer = new XmlSerializer (typeof (DatabaseConnectionSettingsCollection));
				serializer.Serialize (fs, collection);
			}
			
			foreach (KeyValuePair<DatabaseConnectionSettings, string> pair in tmp)
				pair.Key.Password = pair.Value;
		}
	}
}
