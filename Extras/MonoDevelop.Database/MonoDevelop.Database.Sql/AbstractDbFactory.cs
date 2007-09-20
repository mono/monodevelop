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

namespace MonoDevelop.Database.Sql
{
	public abstract class AbstractDbFactory : IDbFactory
	{
		protected Dictionary<string, SchemaActions> supportedActions;
		protected Dictionary<string, Dictionary<SchemaActions, int>> capabilities;
		
		protected AbstractDbFactory ()
		{
			supportedActions = new Dictionary<string,SchemaActions> ();
			capabilities = new Dictionary<string,Dictionary<SchemaActions,int>> ();
		}
		
		public abstract string Identifier { get; }

		public abstract string Name { get; }

		public abstract ISqlDialect Dialect { get; }

		public abstract IConnectionProvider ConnectionProvider { get; }

		public abstract IGuiProvider GuiProvider { get; }
		
		public abstract DatabaseConnectionSettings GetDefaultConnectionSettings ();

		public abstract IConnectionPool CreateConnectionPool (MonoDevelop.Database.Sql.DatabaseConnectionContext context);

		public abstract ISchemaProvider CreateSchemaProvider (MonoDevelop.Database.Sql.IConnectionPool connectionPool);

		public SchemaActions GetSupportedActions (string category)
		{
			if (category == null)
				throw new ArgumentNullException ("category");
			
			SchemaActions actions;
			if (supportedActions.TryGetValue (category, out actions))
				return actions;
			return SchemaActions.None;
		}

		public void SetSupportedActions (string category, SchemaActions actions)
		{
			if (category == null)
				throw new ArgumentNullException ("category");
			
			if (supportedActions.ContainsKey (category)) {
				SchemaActions tmp = supportedActions[category];
				tmp |= actions;
				supportedActions[category] = tmp;
			} else {
				supportedActions.Add (category, actions);
			}
		}
		
		public bool IsActionSupported (string category, SchemaActions action)
		{
			SchemaActions actions = GetSupportedActions (category);
			return (actions & action) == action;
		}

		public int GetCapabilities (string category, SchemaActions action)
		{
			if (category == null)
				throw new ArgumentNullException ("category");
			
			Dictionary<SchemaActions, int> dict = null;
			if (capabilities.TryGetValue (category, out dict)) {
				int val;
				if (dict.TryGetValue (action, out val))
					return val;
			}
			return 0;
		}

		public void SetCapabilities (string category, SchemaActions action, int flags)
		{
			if (category == null)
				throw new ArgumentNullException ("category");
			if (flags < 0)
				throw new ArgumentException ("flags must be >= 0");
			
			foreach (int val in Enum.GetValues (typeof (SchemaActions))) {
				if (val == 0) //skip SchemaActions.None
					continue;
				
				if ((val & (int)action) == val) {
					if (!capabilities.ContainsKey (category))
						capabilities.Add (category, new Dictionary<SchemaActions, int> ());
					
					Dictionary<SchemaActions, int> dict = capabilities[category];
					if (dict.ContainsKey ((SchemaActions)val)) {
						int tmp = dict[(SchemaActions)val];
						tmp |= flags;
						dict[(SchemaActions)val] = tmp;
					} else {
						dict.Add ((SchemaActions)val, flags);
					}
				}
			}
		}
		
		public bool IsCapabilitySupported (string category, SchemaActions action, Enum capability)
		{
			if (category == null)
				throw new ArgumentNullException ("category");
			
			int lookup = GetCapabilities (category, action);
			int value = (int)capability;
			
			return (lookup & value) == value;
		}
	}
}
