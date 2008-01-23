/*
 * Copyright (C) 2004 Jorn Baayen <jorn@nl.linux.org>
 * 
 * Modified by Todd Berman <tberman@sevenl.net> to fit with MonoDevelop.
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 *
 * !!! Note that this class has to have the same API as the one
 *     from GNU.Gettext.dll, because otherwise the strings won't
 *     be picked up by update-po.
 */

using System;
using System.IO;
using System.Runtime.InteropServices;

using MonoDevelop.Core;

using Mono.Unix;

namespace MonoDevelop.Core
{

	public class GettextCatalog
	{
		static GettextCatalog ()
		{
			//variable can be used to override where Gettext looks for the catalogues
			string catalog = System.Environment.GetEnvironmentVariable ("MONODEVELOP_LOCALE_PATH");
			
			if (string.IsNullOrEmpty (catalog)) {
				string location = System.Reflection.Assembly.GetExecutingAssembly ().Location;
				location = Path.GetDirectoryName (location);
				//adding "../.." should give us $prefix
				string prefix = Path.Combine (Path.Combine (location, ".."), "..");
				//normalise it
				prefix = Path.GetFullPath (prefix);
				//catalogue is installed to "$prefix/share/locale" by default
				catalog = Path.Combine (Path.Combine (prefix, "share"), "locale");
			}
			Catalog.Init ("monodevelop", catalog);
		}
	
		private GettextCatalog ()
		{
		}

		public static string GetString (string str)
		{
			return str != null ? Catalog.GetString (str) : null;
		}
	
		public static string GetString (string str, params object[] arguments)
		{
			return string.Format (GetString (str), arguments);
		}
	
		public static string GetPluralString (string singular,
			    	 	              string plural,
					              int n)
		{
			return Catalog.GetPluralString (singular, plural, n);
		}

		public static string GetPluralString (string singular,
			    	 	              string plural,
					              int n,
						      params object[] arguments)
		{
			return string.Format(GetPluralString (singular, plural, n), arguments);
		}

	}
}
