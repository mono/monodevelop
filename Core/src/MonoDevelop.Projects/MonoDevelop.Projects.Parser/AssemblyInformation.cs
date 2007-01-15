// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Threading;
using System.Xml;
using MonoDevelop.Projects.Parser;
using Mono.Cecil;

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
		
/*		byte[] GetBytes (string fileName)
		{
			Runtime.LoggingService.Info (fileName);
			FileStream fs = System.IO.File.OpenRead(fileName);
			long size = fs.Length;
			byte[] outArray = new byte[size];
			fs.Read(outArray, 0, (int)size);
			fs.Close();
			return outArray;
		}
*/

		public void Load(string fileName, bool nonLocking)
		{
//			AssemblyReader assembly = new AssemblyReader();
//			assembly.Load(fileName);
//			
//			TypeDef[] typeDefTable = (TypeDef[])assembly.MetadataTable.Tables[TypeDef.TABLE_ID];
//			
//			for (int i = 0; i < typeDefTable.Length; ++i) {
//				Runtime.LoggingService.Info("ADD " + i);
//				classes.Add(new SharpAssemblyClass(assembly, typeDefTable, i));
//			}
			
			// read xml documentation for the assembly
			/*XmlDocument doc        = null;
			Hashtable   docuNodes  = new Hashtable();
			string      xmlDocFile = System.IO.Path.ChangeExtension(fileName, ".xml");
			
			string   localizedXmlDocFile = System.IO.Path.GetDirectoryName(fileName) + System.IO.Path.DirectorySeparatorChar +
			                               Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName + System.IO.Path.DirectorySeparatorChar +
								           System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(fileName), ".xml");
			if (System.IO.File.Exists(localizedXmlDocFile)) {
				xmlDocFile = localizedXmlDocFile;
			}
			
			if (System.IO.File.Exists(xmlDocFile)) {
				doc = new XmlDocument();
				doc.Load(xmlDocFile);
				
				// convert the XmlDocument into a hash table
				if (doc.DocumentElement != null && doc.DocumentElement["members"] != null) {
					foreach (XmlNode node in doc.DocumentElement["members"].ChildNodes) {
						if (node != null && node.Attributes != null && node.Attributes["name"] != null) {
							docuNodes[node.Attributes["name"].InnerText] = node;
						}
					}
				}
				}*/

			//FIXME: Re-enable this code when the mono bug goes away, 0.32
			//hopefully
			//System.Reflection.Assembly asm = nonLocking ? Assembly.Load(GetBytes(fileName)) : Assembly.LoadFrom(fileName);
			
			this.fileName = fileName;
			
			AssemblyDefinition asm = AssemblyFactory.GetAssembly (fileName);
			
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
