// 
// SqlMetalServices.cs
//  
// Author:
//       Luciano N. Callero <lnc19@hotmail.com>
// 
// Copyright (c) 2010 Lucian0
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
using System;
using System.Text;
using System.IO;
using System.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Database.Sql
{
	public enum FileOutputType {
		DBML,
		Code,
		Both
	}
	
	public static class SqlMetalServices
	{
		static string metal;
		
		static SqlMetalServices ()
		{
			metal = "sqlmetal";
		}
		
		public static bool Generate (string provider, DatabaseConnectionSettings connection, string outputType, 
		                             string outputFile, string language, string outputStyle, string defaultNamespace, 
		                             string entityBase, string entityAttr, string membersAttr, string generateTypes, 
		                             string culture, bool generateSchema, bool generateTimestamp, 
		                             bool overrideEqualAndHash, bool extractProcedures, bool pluralize)
		{
			
			StringBuilder parameters = new StringBuilder ();
			
			if (provider.Equals ("sqlite", StringComparison.InvariantCultureIgnoreCase)) {
				parameters.AppendFormat ("/provider:{0} ", provider);
				parameters.AppendFormat ("/conn:Uri=file:{0} ", connection.Database);
			} else {
				parameters.AppendFormat ("/server:{0} ", connection.Server);
				parameters.AppendFormat ("/provider:{0} ", provider);
				parameters.AppendFormat ("/user:{0} ", connection.Username);
				parameters.AppendFormat ("/password:{0} ", connection.Password);
				parameters.AppendFormat ("/database:{0} ", connection.Database);
			}
			
			if (outputType.Equals (AddinCatalog.GetString ("code"), StringComparison.InvariantCultureIgnoreCase)
			    || outputType.Equals (AddinCatalog.GetString ("Code & DBML"), 
			                          StringComparison.InvariantCultureIgnoreCase)) {
				parameters.AppendFormat ("/code:{0} ", outputFile);
				parameters.AppendFormat ("/case:{0} ", outputStyle);
				parameters.AppendFormat ("/language:{0} ", language.Replace ("#", @"\#"));
				
			}
			
			if (outputType.Equals (AddinCatalog.GetString ("DBML"), StringComparison.InvariantCultureIgnoreCase)
			    || outputType.Equals (AddinCatalog.GetString ("Code & DBML"), 
			                          StringComparison.InvariantCultureIgnoreCase)) {
				string dbmlFile = string.Concat (Path.GetFileNameWithoutExtension (outputFile), ".dbml");
				parameters.AppendFormat ("/dbml:{0} ", dbmlFile);
			}
			
			if (!string.IsNullOrEmpty (defaultNamespace))
				parameters.AppendFormat ("/namespace:{0} ", defaultNamespace);

			if (!string.IsNullOrEmpty (entityBase))
				parameters.AppendFormat ("/entityBase:{0} ", entityBase);

			if (!string.IsNullOrEmpty (entityAttr))
				parameters.AppendFormat ("/entityAttributes:{0} ", entityAttr);

			if (!string.IsNullOrEmpty (membersAttr))
				parameters.AppendFormat ("/memberAttributes:{0} ", membersAttr);

			if (!string.IsNullOrEmpty (generateTypes))
				parameters.AppendFormat ("/generate-type:{0} ", generateTimestamp);

			if (!string.IsNullOrEmpty (culture))
				parameters.AppendFormat ("/culture:{0} ", culture);

			if (!generateSchema)
				parameters.Append ("/schema:false");

			if (!generateTimestamp)
				parameters.Append ("/generate-timestamps:false");
			
			if (overrideEqualAndHash)
				parameters.Append ("/generateEqualsAndHash");
			
			if (extractProcedures)
				parameters.Append ("/sprocs");
			
			if (pluralize)
				parameters.Append ("/pluralize");
		
			Console.WriteLine (parameters);
			ProcessStartInfo info = new ProcessStartInfo (metal, parameters.ToString ());
			info.UseShellExecute = false;
			info.RedirectStandardError = true;
			info.RedirectStandardOutput = true;
			StringWriter outWriter = new StringWriter ();
			StringWriter errorWriter = new StringWriter ();
			MonoDevelop.Core.Execution.ProcessWrapper p = Runtime.ProcessService.StartProcess (info, outWriter, errorWriter, null);
			p.WaitForExit ();
			if (errorWriter.ToString () != "")  {
				QueryService.RaiseException ("Cannot create Linq Class", 
				                             new SqlMetalExecException (errorWriter.ToString ()));
				return false;
			}
			
			// Process.Start (metal, parameters.ToString ());
			return true;
		}
		
		
		public static bool Generate (string provider, DatabaseConnectionSettings connection, string outputType, 
		                             string outputFile, string defaultNamespace, string entityBase, string entityAttr, 
		                             string membersAttr, string generateTypes, string culture, bool generateSchema, 
		                             bool generateTimestamp, bool overrideEqualAndHash, bool extractProcedures, 
		                             bool pluralize)
		{
			return Generate (provider, connection, outputType, outputFile, "", "", defaultNamespace, entityBase, entityAttr,
			          membersAttr, generateTypes, culture, generateSchema, generateTimestamp, overrideEqualAndHash, 
			          extractProcedures, pluralize);
		}

	}
}

