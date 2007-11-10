/*
Copyright (C) 2006  Matthias Braun <matze@braunis.de>
					Scott Ellington <scott.ellington@gmail.com>
 
This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the
Free Software Foundation, Inc., 59 Temple Place - Suite 330,
Boston, MA 02111-1307, USA.
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.Autotools
{
	
	/**
	 * This class allows for instantiation of template texts that contain
	 * %%VARNAME%% sequences. The class contains a hashtable of replacements
	 * that are used to replace the variables
	 */
	public class TemplateEngine
	{
		public Dictionary<string, string> Variables = new Dictionary<string, string> ();
		
		public TemplateEngine()
		{
		}
		
		public string Process ( TextReader reader )
		{
			StringWriter sw = new StringWriter ();
			Process ( reader, sw );
			return sw.ToString ();
		}
		
		public void Process (TextReader reader, TextWriter writer)
		{
			// we do the template instantiation in memory as this should
			// improve performance and we don't expect too big templates
			
			String input = reader.ReadToEnd();
			StringBuilder result = new StringBuilder();
			
			for(int i = 0; i < input.Length-1; ++i) {
				char c = input[i];
				
				if(c == '%' && input[i+1] == '%') {
					i += 2;
					StringBuilder varname = new StringBuilder();
					for( ; i < input.Length-1; ++i) {
						if(input[i] == '%' && input[i+1] == '%') {
							i += 1;
							break;
						}
						varname.Append(input[i]);
					}
					
					string val;
					if (Variables.TryGetValue (varname.ToString (), out val))
						result.Append (val);
					else
						LoggingService.LogWarning ("No replacement for variable %%" +
						                  varname + "%% defined");
					continue;
				}
				
				result.Append(c);
			}
			
			writer.Write(result.ToString());
		}
	}
	
}
