// 
// ProjectDomStats.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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

using System.Collections.Generic;
using System.IO;

namespace MonoDevelop.Projects.Dom.Parser
{
	class ProjectDomStats
	{
		public int ClassEntries;
		public int LoadedClasses;
		public int ReturnTypes;
		public int ReturnTypeParts;
		public int ClassesWithUnsharedReturnTypes;
		public int UnsharedReturnTypes;
		public List<string> UnsharedReturnTypesDetail;
		public int Parameters;
		public int Methods;
		public int Properties;
		public int Fields;
		public int Attributes;
		public int Events;
		public int InstantiatedTypes;
		
		public void Add (ProjectDomStats s)
		{
			ClassEntries += s.ClassEntries;
			LoadedClasses += s.LoadedClasses;
			ReturnTypes += s.ReturnTypes;
			ReturnTypeParts += s.ReturnTypeParts;
			ClassesWithUnsharedReturnTypes += s.ClassesWithUnsharedReturnTypes;
			UnsharedReturnTypes += s.UnsharedReturnTypes;
			Parameters += s.Parameters;
			Methods += s.Methods;
			Properties += s.Properties;
			Fields += s.Fields;
			Attributes += s.Attributes;
			Events += s.Events;
			InstantiatedTypes += s.InstantiatedTypes;
		}
		
		public void Dump (TextWriter tw, string indent)
		{
			tw.WriteLine (indent + "Class entries: " + ClassEntries);
			tw.WriteLine (indent + "Loaded classes: " + LoadedClasses);
			tw.WriteLine (indent + "Instantiated types: " + InstantiatedTypes);
			tw.WriteLine (indent + "Return types: " + ReturnTypes);
			tw.WriteLine (indent + "Return type parts: " + ReturnTypeParts);
			tw.WriteLine (indent + "Classes with unshared RTs: " + ClassesWithUnsharedReturnTypes);
			tw.WriteLine (indent + "Unshared RTs: " + UnsharedReturnTypes);
			tw.WriteLine (indent + "Methods: " + Methods);
			tw.WriteLine (indent + "Parameters: " + Parameters);
			tw.WriteLine (indent + "Properties: " + Properties);
			tw.WriteLine (indent + "Fields: " + Fields);
			tw.WriteLine (indent + "Attributes: " + Attributes);
			tw.WriteLine (indent + "Events: " + Events);
			
			if (UnsharedReturnTypesDetail != null && UnsharedReturnTypesDetail.Count > 0) {
				tw.WriteLine (indent + "Unshared RTs (first 10):");
				foreach (var s in UnsharedReturnTypesDetail)
					tw.WriteLine (indent + "  " + s);
			}
		}
	}
}
