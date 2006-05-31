
using System;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.ComponentModel;

namespace MonoDevelop.Core.AddIns
{
	public class DumpAddinInfoApp: IApplication
	{
		ArrayList addins = new ArrayList ();
		bool indent = true;
		
		public int Run (string[] args)
		{
			Runtime.Initialize ();
			string outFile = "addin-tree.xml";
			IProgressMonitor monitor = new MonoDevelop.Core.ProgressMonitoring.ConsoleProgressMonitor ();
			foreach (string arg in args) {
				if (arg == "-noindent")
					indent = false;
				else if (arg.StartsWith ("-out:"))
					outFile = arg.Substring (5);
				else {
					Runtime.AddInService.PreloadAddin (monitor, arg);
				}
			}
			
			XmlDocument doc = new XmlDocument ();
			doc.AppendChild (doc.CreateElement ("ExtensionTree"));
			XmlElement tree = doc.CreateElement ("Tree");
			XmlElement codons = doc.CreateElement ("Codons");
			XmlElement extensions = doc.CreateElement ("ExtensionPoints");
			XmlElement addinsElem = doc.CreateElement ("AddIns");
			doc.DocumentElement.AppendChild (tree);
			doc.DocumentElement.AppendChild (extensions);
			doc.DocumentElement.AppendChild (codons);
			doc.DocumentElement.AppendChild (addinsElem);
			
			DumpNode (extensions, tree, AddInTreeSingleton.AddInTree.GetTreeNode (null));
			
			foreach (CodonBuilder cbuilder in AddInTreeSingleton.AddInTree.CodonFactory.GetAllBuilders ()) {
				XmlElement codon = doc.CreateElement ("Codon");
				codons.AppendChild (codon);
				codon.SetAttribute ("id", cbuilder.CodonName);
				
				Type type = cbuilder.CodonType;
				FillProperties (codon, type);
			}
			
			foreach (string aname in addins) {
				XmlElement addin = doc.CreateElement ("AddIn");
				addin.SetAttribute ("name", aname);
				addinsElem.AppendChild (addin);
			}
			
			if (!indent) {
				XmlTextWriter tw = new XmlTextWriter (new System.IO.StreamWriter (outFile));
				doc.WriteTo (tw);
				tw.Close ();
			} else
				doc.Save (outFile);

			Console.WriteLine ("Saved file '" + outFile + "'.");
			return 0;
		}
		
		bool DumpNode (XmlElement extensions, XmlElement xmlNode, IAddInTreeNode treeNode)
		{
			bool hasExtensionPoint = false;
			
			if (treeNode.AllowedChildNodes != null && treeNode.AllowedChildNodes.Length > 0) {
				string path = xmlNode.GetAttribute ("name");
				XmlElement pnode = xmlNode.ParentNode as XmlElement;
				while (pnode != null && pnode.Name == "Node") {
					path = pnode.GetAttribute ("name") + "/" + path;
					pnode = pnode.ParentNode as XmlElement;
				}
				
				xmlNode.SetAttribute ("path", path);
				foreach (string codon in treeNode.AllowedChildNodes) {
					XmlElement xcodon = xmlNode.OwnerDocument.CreateElement ("ChildNode");
					xcodon.SetAttribute ("id", codon);
					xmlNode.AppendChild (xcodon);
				}
				
				if (treeNode.Description != null && treeNode.Description.Length > 0) {
					string desc = "";
					string title = treeNode.Description;
					int i = title.IndexOf ("|");
					if (i != -1) {
						desc = title.Substring (i+1);
						title = title.Substring (0, i);
					}
					title = title.Trim ();
					if (title.EndsWith ("."))
						title = title.Substring (0, title.Length - 1);
					xmlNode.SetAttribute ("title", title);
					xmlNode.SetAttribute ("remarks", desc);
					hasExtensionPoint = true;
				}
				
				if (treeNode.OwnerAddIn != null) {
					xmlNode.SetAttribute ("add-in", treeNode.OwnerAddIn.Id);
					if (!addins.Contains (treeNode.OwnerAddIn.Id))
						addins.Add (treeNode.OwnerAddIn.Id);
				}
			}
			
			foreach (DictionaryEntry entry in treeNode.ChildNodes) {
				string name = (string)entry.Key;
				IAddInTreeNode node = (IAddInTreeNode) entry.Value;
				XmlElement child = xmlNode.OwnerDocument.CreateElement ("Node");
				child.SetAttribute ("name", name);
				
				if (node.Codon != null) {
					Type codonType = node.Codon.GetType ();
					string[] cnodes = CodonBuilder.GetAllowedChildNodes (codonType);
					if (cnodes == null || cnodes.Length == 0)
						continue;
				}
				
				xmlNode.AppendChild (child);
				if (DumpNode (extensions, child, node))
					hasExtensionPoint = true;
				else
					xmlNode.RemoveChild (child);
			}
			return hasExtensionPoint;
		}
		
		void FillProperties (XmlElement codon, Type currentType)
		{
			object[] ats = currentType.GetCustomAttributes (typeof(DescriptionAttribute), false);
			if (ats.Length > 0)
				codon.SetAttribute ("description", ((DescriptionAttribute)ats[0]).Description);
				
			string[] childNodes = CodonBuilder.GetAllowedChildNodes (currentType);
			if (childNodes != null && childNodes.Length > 0) {
				foreach (string codonId in childNodes) {
					XmlElement xcodon = codon.OwnerDocument.CreateElement ("ChildNode");
					xcodon.SetAttribute ("id", codonId);
					codon.AppendChild (xcodon);
				}
			}
		
			FieldInfo[] fieldInfoArray = currentType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
			
			foreach (FieldInfo fieldInfo in fieldInfoArray) {
				// process XmlMemberAttributeAttribute attributes
				
				XmlElement prop = null;
				
				if (fieldInfo.IsDefined (typeof(XmlMemberAttributeAttribute), true)) {
					XmlMemberAttributeAttribute codonAttribute = (XmlMemberAttributeAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(XmlMemberAttributeAttribute));
				
					if (codonAttribute.Name == "insertafter" || codonAttribute.Name == "insertbefore")
						continue;

					if (codonAttribute.Name == "class" && !(typeof(ClassCodon).IsAssignableFrom (currentType)))
						continue;
						
					prop = codon.OwnerDocument.CreateElement ("Property");
					prop.SetAttribute ("name", codonAttribute.Name);
					prop.SetAttribute ("type", fieldInfo.FieldType.FullName);
					
					if (codonAttribute.IsRequired)
						prop.SetAttribute ("is-required", "true");
				}
				
				// process XmlMemberAttributeAttribute attributes
				
				if (fieldInfo.IsDefined (typeof(XmlMemberArrayAttribute), true)) {
					XmlMemberArrayAttribute codonArrayAttribute = (XmlMemberArrayAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(XmlMemberArrayAttribute));
				
					if (codonArrayAttribute.Name == "insertafter" || codonArrayAttribute.Name == "insertbefore")
						continue;

					prop = codon.OwnerDocument.CreateElement ("Property");
					prop.SetAttribute ("name", codonArrayAttribute.Name);
					prop.SetAttribute ("type", fieldInfo.FieldType.FullName);
					prop.SetAttribute ("array-separator", new string (codonArrayAttribute.Separator));
					
					if (codonArrayAttribute.IsRequired)
						prop.SetAttribute ("is-required", "true");
				}
				
				if (prop != null) {
					object[] dats = fieldInfo.GetCustomAttributes (typeof(DescriptionAttribute), false);
					if (dats.Length > 0)
						prop.SetAttribute ("description", ((DescriptionAttribute)dats[0]).Description);
					codon.AppendChild (prop);
				}
			}		
		}
	}
}
