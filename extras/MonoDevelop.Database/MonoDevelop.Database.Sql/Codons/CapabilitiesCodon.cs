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
	[ExtensionNode (Description="The list of supported flags for each database object.")]
	public class CapabilitiesCodon : ExtensionNode
	{
		[NodeAttribute("category", true, "The database object category, eg: Table.")]
		string category = null;
		
		[NodeAttribute("action", true, "The database operation for which the flags are valid, eg: Create.")]
		string action = null;

		[NodeAttribute("flags", true, "Comma seperated list of SchemaAction flags.")]
		string flags = null;

		private SchemaActions actions;
		private int parsedFlags;

		public string Category {
			get { return category; }
		}

		public SchemaActions Actions {
			get { return actions; }	
		}
		
		public int ParsedFlags {
			get { return parsedFlags; }	
		}

		protected override void Read (NodeElement elem)
		{
			base.Read (elem);
			
			actions = (SchemaActions)Enum.Parse (typeof (SchemaActions), action);
			parsedFlags = CapabilitiesUtility.Parse (category, flags);
		}
	}
}
