//  AssemblyInformation.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Threading;
using System.Xml;
using MonoDevelop.Projects.Parser;
using Mono.Cecil;
using Mono.Cecil.Mdb;
using Mono.Unix;

//using ICSharpCode.SharpAssembly.Metadata.Rows;
//using ICSharpCode.SharpAssembly.Metadata;
//using ICSharpCode.SharpAssembly.PE;
//using ICSharpCode.SharpAssembly;
using MonoDevelop.Core;


namespace MonoDevelop.Projects.Parser {
	
	/// <summary>
	/// This class loads an assembly and converts all types from this assembly
	/// to a parser layer Class Collection.
	/// </summary>
	[Serializable]
	internal class AssemblyInformation : MarshalByRefObject
	{
		ClassCollection classes = new ClassCollection();
		string fileName;
		
		/// <value>
		/// A <code>ClassColection</code> that contains all loaded classes.
		/// </value>
		public ClassCollection Classes {
			get {
				return classes;
			}
		}
		
		public string FileName
		{
			get { return fileName; }
		}
		
		public AssemblyInformation()
		{
		}
		
		public void Load(string fileName, bool nonLocking)
		{
			// Jump links
			fileName = Mono.Unix.UnixPath.GetRealPath (fileName);
			
			this.fileName = fileName;
			
			AssemblyDefinition asm = AssemblyFactory.GetAssembly (fileName);
			
			if (File.Exists (fileName + ".mdb")) {
				MdbFactory f = new MdbFactory ();
				try {
					asm.MainModule.LoadSymbols (f.CreateReader (asm.MainModule, fileName));
				} catch (Exception ex) {
					// Ignore symbol loading errors
					LoggingService.LogError (ex.ToString ());
				}
			}
			
			foreach (TypeDefinition type in asm.MainModule.Types) {
				TypeAttributes vis = type.Attributes & TypeAttributes.VisibilityMask;
				
				// Don't include: 
				// * private types
				// * the <Module> type
				// * special types defined by the runtime
				// * nested types (the declaring type will already include them)
				if (!type.FullName.StartsWith("<") && 
					!type.IsSpecialName && 
					(vis == TypeAttributes.Public || vis == TypeAttributes.NestedPublic) &&
					type.DeclaringType == null)
				{
					classes.Add(new ReflectionClass(type));
				}
			}
		}
	}
}
