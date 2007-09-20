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
	public static class CapabilitiesUtility
	{
		private static Dictionary<string, Type> types;

		static CapabilitiesUtility ()
		{
			types = new Dictionary<string, Type> ();

			foreach (CapabilityFlagsCodon codon in AddinManager.GetExtensionNodes ("/MonoDevelop/Database/Capabilities"))
				Register (codon.Category, codon.Type);
		}

		public static void Register (string category, Type type)
		{
			if (category == null)
				throw new ArgumentNullException ("category");
			if (type == null)
				throw new ArgumentNullException ("type");

			if (types.ContainsKey (category)) {
				types[category] = type;
				Runtime.LoggingService.WarnFormat ("Duplicate CapabilityFlags for category {0}.", category);
			} else {
				types.Add (category, type);
			}
		}

		public static int Parse (string category, string flags)
		{
			if (category == null)
				throw new ArgumentNullException ("category");
			
			Type type = null;
			if (!string.IsNullOrEmpty (flags) && types.TryGetValue (category, out type)) {
				object obj = Enum.Parse (type, flags, true);
				return (int)obj;
			}
			return 0;
		}
		
		public static Type GetType (string category)
		{
			if (category == null)
				throw new ArgumentNullException ("category");
			
			Type type = null;
			if (types.TryGetValue (category, out type))
				return type;
			return null;
		}
	}
}
