//
// RepositoryReader.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Text;
using System.Collections;
using System.Globalization;

namespace Mono.Addins.Setup
{
	internal class RepositoryReader : XmlSerializationReader
	{
		public object ReadRoot_Repository ()
		{
			Reader.MoveToContent();
			if (Reader.LocalName != "Repository" || Reader.NamespaceURI != "")
				throw CreateUnknownNodeException();
			return ReadObject_Repository (true, true);
		}

		public Mono.Addins.Setup.Repository ReadObject_Repository (bool isNullable, bool checkType)
		{
			Mono.Addins.Setup.Repository ob = null;
			if (isNullable && ReadNull()) return null;

			if (checkType) 
			{
				System.Xml.XmlQualifiedName t = GetXsiType();
				if (t == null)
				{ }
				else if (t.Name != "Repository" || t.Namespace != "")
					throw CreateUnknownTypeException(t);
			}

			ob = new Mono.Addins.Setup.Repository ();

			Reader.MoveToElement();

			while (Reader.MoveToNextAttribute())
			{
				if (IsXmlnsAttribute (Reader.Name)) {
				}
				else {
					UnknownNode (ob);
				}
			}

			Reader.MoveToElement();
			if (Reader.IsEmptyElement) {
				Reader.Skip ();
				return ob;
			}

			Reader.ReadStartElement();
			Reader.MoveToContent();

			bool b0=false, b1=false, b2=false, b3=false;

			Mono.Addins.Setup.RepositoryEntryCollection o5;
			o5 = ob.@Repositories;
			Mono.Addins.Setup.RepositoryEntryCollection o7;
			o7 = ob.@Addins;
			int n4=0, n6=0;

			while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
			{
				if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (Reader.LocalName == "Addin" && Reader.NamespaceURI == "" && !b3) {
						if (((object)o7) == null)
							throw CreateReadOnlyCollectionException ("Mono.Addins.Setup.RepositoryEntryCollection");
						o7.Add (ReadObject_AddinRepositoryEntry (false, true));
						n6++;
					}
					else if (Reader.LocalName == "Repository" && Reader.NamespaceURI == "" && !b2) {
						if (((object)o5) == null)
							throw CreateReadOnlyCollectionException ("Mono.Addins.Setup.RepositoryEntryCollection");
						o5.Add (ReadObject_ReferenceRepositoryEntry (false, true));
						n4++;
					}
					else if (Reader.LocalName == "Name" && Reader.NamespaceURI == "" && !b0) {
						b0 = true;
						ob.@Name = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "Url" && Reader.NamespaceURI == "" && !b1) {
						b1 = true;
						ob.@Url = Reader.ReadElementString ();
					}
					else {
						UnknownNode (ob);
					}
				}
				else
					UnknownNode(ob);

				Reader.MoveToContent();
			}


			ReadEndElement();

			return ob;
		}

		public Mono.Addins.Setup.PackageRepositoryEntry ReadObject_AddinRepositoryEntry (bool isNullable, bool checkType)
		{
			Mono.Addins.Setup.PackageRepositoryEntry ob = null;
			if (isNullable && ReadNull()) return null;

			if (checkType) 
			{
				System.Xml.XmlQualifiedName t = GetXsiType();
				if (t == null)
				{ }
				else if (t.Name != "PackageRepositoryEntry" || t.Namespace != "")
					throw CreateUnknownTypeException(t);
			}

			ob = new Mono.Addins.Setup.PackageRepositoryEntry ();

			Reader.MoveToElement();

			while (Reader.MoveToNextAttribute())
			{
				if (IsXmlnsAttribute (Reader.Name)) {
				}
				else {
					UnknownNode (ob);
				}
			}

			Reader.MoveToElement();
			if (Reader.IsEmptyElement) {
				Reader.Skip ();
				return ob;
			}

			Reader.ReadStartElement();
			Reader.MoveToContent();

			bool b8=false, b9=false;

			while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
			{
				if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (Reader.LocalName == "Addin" && Reader.NamespaceURI == "" && !b9) {
						b9 = true;
						ob.@Addin = ReadObject_AddinInfo (false, true);
					}
					else if (Reader.LocalName == "Url" && Reader.NamespaceURI == "" && !b8) {
						b8 = true;
						ob.@Url = Reader.ReadElementString ();
					}
					else {
						UnknownNode (ob);
					}
				}
				else
					UnknownNode(ob);

				Reader.MoveToContent();
			}

			ReadEndElement();

			return ob;
		}

		public Mono.Addins.Setup.ReferenceRepositoryEntry ReadObject_ReferenceRepositoryEntry (bool isNullable, bool checkType)
		{
			Mono.Addins.Setup.ReferenceRepositoryEntry ob = null;
			if (isNullable && ReadNull()) return null;

			if (checkType) 
			{
				System.Xml.XmlQualifiedName t = GetXsiType();
				if (t == null)
				{ }
				else if (t.Name != "ReferenceRepositoryEntry" || t.Namespace != "")
					throw CreateUnknownTypeException(t);
			}

			ob = new Mono.Addins.Setup.ReferenceRepositoryEntry ();

			Reader.MoveToElement();

			while (Reader.MoveToNextAttribute())
			{
				if (IsXmlnsAttribute (Reader.Name)) {
				}
				else {
					UnknownNode (ob);
				}
			}

			Reader.MoveToElement();
			if (Reader.IsEmptyElement) {
				Reader.Skip ();
				return ob;
			}

			Reader.ReadStartElement();
			Reader.MoveToContent();

			bool b10=false, b11=false;

			while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
			{
				if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (Reader.LocalName == "Url" && Reader.NamespaceURI == "" && !b10) {
						b10 = true;
						ob.@Url = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "LastModified" && Reader.NamespaceURI == "" && !b11) {
						b11 = true;
						ob.@LastModified = XmlConvert.ToDateTime (Reader.ReadElementString ());
					}
					else {
						UnknownNode (ob);
					}
				}
				else
					UnknownNode(ob);

				Reader.MoveToContent();
			}

			ReadEndElement();

			return ob;
		}

		public Mono.Addins.Setup.AddinInfo ReadObject_AddinInfo (bool isNullable, bool checkType)
		{
			Mono.Addins.Setup.AddinInfo ob = null;
			if (isNullable && ReadNull()) return null;

			if (checkType) 
			{
				System.Xml.XmlQualifiedName t = GetXsiType();
				if (t == null)
				{ }
				else if (t.Name != "AddinInfo" || t.Namespace != "")
					throw CreateUnknownTypeException(t);
			}

			ob = new Mono.Addins.Setup.AddinInfo ();

			Reader.MoveToElement();

			while (Reader.MoveToNextAttribute())
			{
				if (IsXmlnsAttribute (Reader.Name)) {
				}
				else {
					UnknownNode (ob);
				}
			}

			Reader.MoveToElement();
			if (Reader.IsEmptyElement) {
				Reader.Skip ();
				return ob;
			}

			Reader.ReadStartElement();
			Reader.MoveToContent();

			bool b12=false, b13=false, b14=false, b15=false, b16=false, b17=false, b18=false, b19=false, b20=false, b21=false, b22=false, b23=false;

			while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
			{
				if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (Reader.LocalName == "Version" && Reader.NamespaceURI == "" && !b15) {
						b15 = true;
						ob.@Version = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "Dependencies" && Reader.NamespaceURI == "" && !b22) {
						if (((object)ob.@Dependencies) == null)
							throw CreateReadOnlyCollectionException ("Mono.Addins.Setup.DependencyCollection");
						if (Reader.IsEmptyElement) {
							Reader.Skip();
						} else {
							int n24 = 0;
							Reader.ReadStartElement();
							Reader.MoveToContent();

							while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
							{
								if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
								{
									if (Reader.LocalName == "AssemblyDependency" && Reader.NamespaceURI == "") {
										if (((object)ob.@Dependencies) == null)
											throw CreateReadOnlyCollectionException ("Mono.Addins.Setup.DependencyCollection");
										ob.@Dependencies.Add (ReadObject_AssemblyDependency (false, true));
										n24++;
									}
									else if (Reader.LocalName == "NativeDependency" && Reader.NamespaceURI == "") {
										if (((object)ob.@Dependencies) == null)
											throw CreateReadOnlyCollectionException ("Mono.Addins.Setup.DependencyCollection");
										ob.@Dependencies.Add (ReadObject_NativeReference (false, true));
										n24++;
									}
									else if (Reader.LocalName == "AddinDependency" && Reader.NamespaceURI == "") {
										if (((object)ob.@Dependencies) == null)
											throw CreateReadOnlyCollectionException ("Mono.Addins.Setup.DependencyCollection");
										ob.@Dependencies.Add (ReadObject_AddinReference (false, true));
										n24++;
									}
									else UnknownNode (null);
								}
								else UnknownNode (null);

								Reader.MoveToContent();
							}
							ReadEndElement();
						}
						b22 = true;
					}
					else if (Reader.LocalName == "Name" && Reader.NamespaceURI == "" && !b14) {
						b14 = true;
						ob.@Name = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "BaseVersion" && Reader.NamespaceURI == "" && !b16) {
						b16 = true;
						ob.@BaseVersion = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "Id" && Reader.NamespaceURI == "" && !b12) {
						b12 = true;
						ob.@LocalId = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "Url" && Reader.NamespaceURI == "" && !b19) {
						b19 = true;
						ob.@Url = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "Copyright" && Reader.NamespaceURI == "" && !b18) {
						b18 = true;
						ob.@Copyright = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "Description" && Reader.NamespaceURI == "" && !b20) {
						b20 = true;
						ob.@Description = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "Author" && Reader.NamespaceURI == "" && !b17) {
						b17 = true;
						ob.@Author = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "OptionalDependencies" && Reader.NamespaceURI == "" && !b23) {
						if (((object)ob.@OptionalDependencies) == null)
							throw CreateReadOnlyCollectionException ("Mono.Addins.Setup.DependencyCollection");
						if (Reader.IsEmptyElement) {
							Reader.Skip();
						} else {
							int n25 = 0;
							Reader.ReadStartElement();
							Reader.MoveToContent();

							while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
							{
								if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
								{
									if (Reader.LocalName == "AssemblyDependency" && Reader.NamespaceURI == "") {
										if (((object)ob.@OptionalDependencies) == null)
											throw CreateReadOnlyCollectionException ("Mono.Addins.Setup.DependencyCollection");
										ob.@OptionalDependencies.Add (ReadObject_AssemblyDependency (false, true));
										n25++;
									}
									else if (Reader.LocalName == "NativeDependency" && Reader.NamespaceURI == "") {
										if (((object)ob.@OptionalDependencies) == null)
											throw CreateReadOnlyCollectionException ("Mono.Addins.Setup.DependencyCollection");
										ob.@OptionalDependencies.Add (ReadObject_NativeReference (false, true));
										n25++;
									}
									else if (Reader.LocalName == "AddinDependency" && Reader.NamespaceURI == "") {
										if (((object)ob.@OptionalDependencies) == null)
											throw CreateReadOnlyCollectionException ("Mono.Addins.Setup.DependencyCollection");
										ob.@OptionalDependencies.Add (ReadObject_AddinReference (false, true));
										n25++;
									}
									else UnknownNode (null);
								}
								else UnknownNode (null);

								Reader.MoveToContent();
							}
							ReadEndElement();
						}
						b23 = true;
					}
					else if (Reader.LocalName == "Namespace" && Reader.NamespaceURI == "" && !b13) {
						b13 = true;
						ob.@Namespace = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "Category" && Reader.NamespaceURI == "" && !b21) {
						b21 = true;
						ob.@Category = Reader.ReadElementString ();
					}
					else {
						UnknownNode (ob);
					}
				}
				else
					UnknownNode(ob);

				Reader.MoveToContent();
			}

			ReadEndElement();

			return ob;
		}

		public Mono.Addins.Description.AssemblyDependency ReadObject_AssemblyDependency (bool isNullable, bool checkType)
		{
			Mono.Addins.Description.AssemblyDependency ob = null;
			if (isNullable && ReadNull()) return null;

			if (checkType) 
			{
				System.Xml.XmlQualifiedName t = GetXsiType();
				if (t == null)
				{ }
				else if (t.Name != "AssemblyDependency" || t.Namespace != "")
					throw CreateUnknownTypeException(t);
			}

			ob = new Mono.Addins.Description.AssemblyDependency ();

			Reader.MoveToElement();

			while (Reader.MoveToNextAttribute())
			{
				if (IsXmlnsAttribute (Reader.Name)) {
				}
				else {
					UnknownNode (ob);
				}
			}

			Reader.MoveToElement();
			if (Reader.IsEmptyElement) {
				Reader.Skip ();
				return ob;
			}

			Reader.ReadStartElement();
			Reader.MoveToContent();

			bool b26=false, b27=false;

			while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
			{
				if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (Reader.LocalName == "Package" && Reader.NamespaceURI == "" && !b27) {
						b27 = true;
						ob.@Package = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "FullName" && Reader.NamespaceURI == "" && !b26) {
						b26 = true;
						ob.@FullName = Reader.ReadElementString ();
					}
					else {
						UnknownNode (ob);
					}
				}
				else
					UnknownNode(ob);

				Reader.MoveToContent();
			}

			ReadEndElement();

			return ob;
		}

		public Mono.Addins.Description.NativeDependency ReadObject_NativeReference (bool isNullable, bool checkType)
		{
			Mono.Addins.Description.NativeDependency ob = null;
			if (isNullable && ReadNull()) return null;

			if (checkType) 
			{
				System.Xml.XmlQualifiedName t = GetXsiType();
				if (t == null)
				{ }
				else if (t.Name != "NativeReference" || t.Namespace != "")
					throw CreateUnknownTypeException(t);
			}

			ob = new Mono.Addins.Description.NativeDependency ();

			Reader.MoveToElement();

			while (Reader.MoveToNextAttribute())
			{
				if (IsXmlnsAttribute (Reader.Name)) {
				}
				else {
					UnknownNode (ob);
				}
			}

			Reader.MoveToElement();
			if (Reader.IsEmptyElement) {
				Reader.Skip ();
				return ob;
			}

			Reader.ReadStartElement();
			Reader.MoveToContent();

			while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
			{
				if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
				{
					UnknownNode (ob);
				}
				else
					UnknownNode(ob);

				Reader.MoveToContent();
			}

			ReadEndElement();

			return ob;
		}

		public Mono.Addins.Description.AddinDependency ReadObject_AddinReference (bool isNullable, bool checkType)
		{
			Mono.Addins.Description.AddinDependency ob = null;
			if (isNullable && ReadNull()) return null;

			if (checkType) 
			{
				System.Xml.XmlQualifiedName t = GetXsiType();
				if (t == null)
				{ }
				else if (t.Name != "AddinReference" || t.Namespace != "")
					throw CreateUnknownTypeException(t);
			}

			ob = new Mono.Addins.Description.AddinDependency ();

			Reader.MoveToElement();

			while (Reader.MoveToNextAttribute())
			{
				if (IsXmlnsAttribute (Reader.Name)) {
				}
				else {
					UnknownNode (ob);
				}
			}

			Reader.MoveToElement();
			if (Reader.IsEmptyElement) {
				Reader.Skip ();
				return ob;
			}

			Reader.ReadStartElement();
			Reader.MoveToContent();

			bool b28=false, b29=false;

			while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
			{
				if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (Reader.LocalName == "Version" && Reader.NamespaceURI == "" && !b29) {
						b29 = true;
						ob.@Version = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "AddinId" && Reader.NamespaceURI == "" && !b28) {
						b28 = true;
						ob.@AddinId = Reader.ReadElementString ();
					}
					else {
						UnknownNode (ob);
					}
				}
				else
					UnknownNode(ob);

				Reader.MoveToContent();
			}

			ReadEndElement();

			return ob;
		}

		protected override void InitCallbacks ()
		{
		}

		protected override void InitIDs ()
		{
		}

	}

	internal class RepositoryWriter : XmlSerializationWriter
	{
		const string xmlNamespace = "http://www.w3.org/2000/xmlns/";
		public void WriteRoot_Repository (object o)
		{
			WriteStartDocument ();
			Mono.Addins.Setup.Repository ob = (Mono.Addins.Setup.Repository) o;
			TopLevelElement ();
			WriteObject_Repository (ob, "Repository", "", true, false, true);
		}

		void WriteObject_Repository (Mono.Addins.Setup.Repository ob, string element, string namesp, bool isNullable, bool needType, bool writeWrappingElem)
		{
			if (((object)ob) == null)
			{
				if (isNullable)
					WriteNullTagLiteral(element, namesp);
				return;
			}

			System.Type type = ob.GetType ();
			if (type == typeof(Mono.Addins.Setup.Repository))
			{ }
			else {
				throw CreateUnknownTypeException (ob);
			}

			if (writeWrappingElem) {
				WriteStartElement (element, namesp, ob);
			}

			if (needType) WriteXsiType("Repository", "");

			WriteElementString ("Name", "", ob.@Name);
			WriteElementString ("Url", "", ob.@Url);
			if (ob.@Repositories != null) {
				for (int n30 = 0; n30 < ob.@Repositories.Count; n30++) {
					WriteObject_ReferenceRepositoryEntry (((Mono.Addins.Setup.ReferenceRepositoryEntry) ob.@Repositories[n30]), "Repository", "", false, false, true);
				}
			}
			if (ob.@Addins != null) {
				for (int n31 = 0; n31 < ob.@Addins.Count; n31++) {
					WriteObject_AddinRepositoryEntry (((Mono.Addins.Setup.PackageRepositoryEntry) ob.@Addins[n31]), "Addin", "", false, false, true);
				}
			}
			if (writeWrappingElem) WriteEndElement (ob);
		}

		void WriteObject_ReferenceRepositoryEntry (Mono.Addins.Setup.ReferenceRepositoryEntry ob, string element, string namesp, bool isNullable, bool needType, bool writeWrappingElem)
		{
			if (((object)ob) == null)
			{
				if (isNullable)
					WriteNullTagLiteral(element, namesp);
				return;
			}

			System.Type type = ob.GetType ();
			if (type == typeof(Mono.Addins.Setup.ReferenceRepositoryEntry))
			{ }
			else {
				throw CreateUnknownTypeException (ob);
			}

			if (writeWrappingElem) {
				WriteStartElement (element, namesp, ob);
			}

			if (needType) WriteXsiType("ReferenceRepositoryEntry", "");

			WriteElementString ("Url", "", ob.@Url);
			WriteElementString ("LastModified", "", ob.@LastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture));
			if (writeWrappingElem) WriteEndElement (ob);
		}

		void WriteObject_AddinRepositoryEntry (Mono.Addins.Setup.PackageRepositoryEntry ob, string element, string namesp, bool isNullable, bool needType, bool writeWrappingElem)
		{
			if (((object)ob) == null)
			{
				if (isNullable)
					WriteNullTagLiteral(element, namesp);
				return;
			}

			System.Type type = ob.GetType ();
			if (type == typeof(Mono.Addins.Setup.PackageRepositoryEntry))
			{ }
			else {
				throw CreateUnknownTypeException (ob);
			}

			if (writeWrappingElem) {
				WriteStartElement (element, namesp, ob);
			}

			if (needType) WriteXsiType("PackageRepositoryEntry", "");

			WriteElementString ("Url", "", ob.@Url);
			WriteObject_AddinInfo (ob.@Addin, "Addin", "", false, false, true);
			if (writeWrappingElem) WriteEndElement (ob);
		}

		void WriteObject_AddinInfo (Mono.Addins.Setup.AddinInfo ob, string element, string namesp, bool isNullable, bool needType, bool writeWrappingElem)
		{
			if (((object)ob) == null)
			{
				if (isNullable)
					WriteNullTagLiteral(element, namesp);
				return;
			}

			System.Type type = ob.GetType ();
			if (type == typeof(Mono.Addins.Setup.AddinInfo))
			{ }
			else {
				throw CreateUnknownTypeException (ob);
			}

			if (writeWrappingElem) {
				WriteStartElement (element, namesp, ob);
			}

			if (needType) WriteXsiType("AddinInfo", "");

			WriteElementString ("Id", "", ob.@LocalId);
			WriteElementString ("Namespace", "", ob.@Namespace);
			WriteElementString ("Name", "", ob.@Name);
			WriteElementString ("Version", "", ob.@Version);
			WriteElementString ("BaseVersion", "", ob.@BaseVersion);
			WriteElementString ("Author", "", ob.@Author);
			WriteElementString ("Copyright", "", ob.@Copyright);
			WriteElementString ("Url", "", ob.@Url);
			WriteElementString ("Description", "", ob.@Description);
			WriteElementString ("Category", "", ob.@Category);
			if (ob.@Dependencies != null) {
				WriteStartElement ("Dependencies", "", ob.@Dependencies);
				for (int n32 = 0; n32 < ob.@Dependencies.Count; n32++) {
					if (((object)ob.@Dependencies[n32]) == null) { }
					else if (ob.@Dependencies[n32].GetType() == typeof(Mono.Addins.Description.AssemblyDependency)) {
						WriteObject_AssemblyDependency (((Mono.Addins.Description.AssemblyDependency) ob.@Dependencies[n32]), "AssemblyDependency", "", false, false, true);
					}
					else if (ob.@Dependencies[n32].GetType() == typeof(Mono.Addins.Description.NativeDependency)) {
						WriteObject_NativeReference (((Mono.Addins.Description.NativeDependency) ob.@Dependencies[n32]), "NativeDependency", "", false, false, true);
					}
					else if (ob.@Dependencies[n32].GetType() == typeof(Mono.Addins.Description.AddinDependency)) {
						WriteObject_AddinReference (((Mono.Addins.Description.AddinDependency) ob.@Dependencies[n32]), "AddinDependency", "", false, false, true);
					}
					else throw CreateUnknownTypeException (ob.@Dependencies[n32]);
				}
				WriteEndElement (ob.@Dependencies);
			}
			if (ob.@OptionalDependencies != null) {
				WriteStartElement ("OptionalDependencies", "", ob.@OptionalDependencies);
				for (int n33 = 0; n33 < ob.@OptionalDependencies.Count; n33++) {
					if (((object)ob.@OptionalDependencies[n33]) == null) { }
					else if (ob.@OptionalDependencies[n33].GetType() == typeof(Mono.Addins.Description.AssemblyDependency)) {
						WriteObject_AssemblyDependency (((Mono.Addins.Description.AssemblyDependency) ob.@OptionalDependencies[n33]), "AssemblyDependency", "", false, false, true);
					}
					else if (ob.@OptionalDependencies[n33].GetType() == typeof(Mono.Addins.Description.NativeDependency)) {
						WriteObject_NativeReference (((Mono.Addins.Description.NativeDependency) ob.@OptionalDependencies[n33]), "NativeDependency", "", false, false, true);
					}
					else if (ob.@OptionalDependencies[n33].GetType() == typeof(Mono.Addins.Description.AddinDependency)) {
						WriteObject_AddinReference (((Mono.Addins.Description.AddinDependency) ob.@OptionalDependencies[n33]), "AddinDependency", "", false, false, true);
					}
					else throw CreateUnknownTypeException (ob.@OptionalDependencies[n33]);
				}
				WriteEndElement (ob.@OptionalDependencies);
			}
			if (writeWrappingElem) WriteEndElement (ob);
		}

		void WriteObject_AssemblyDependency (Mono.Addins.Description.AssemblyDependency ob, string element, string namesp, bool isNullable, bool needType, bool writeWrappingElem)
		{
			if (((object)ob) == null)
			{
				if (isNullable)
					WriteNullTagLiteral(element, namesp);
				return;
			}

			System.Type type = ob.GetType ();
			if (type == typeof(Mono.Addins.Description.AssemblyDependency))
			{ }
			else {
				throw CreateUnknownTypeException (ob);
			}

			if (writeWrappingElem) {
				WriteStartElement (element, namesp, ob);
			}

			if (needType) WriteXsiType("AssemblyDependency", "");

			WriteElementString ("FullName", "", ob.@FullName);
			WriteElementString ("Package", "", ob.@Package);
			if (writeWrappingElem) WriteEndElement (ob);
		}

		void WriteObject_NativeReference (Mono.Addins.Description.NativeDependency ob, string element, string namesp, bool isNullable, bool needType, bool writeWrappingElem)
		{
			if (((object)ob) == null)
			{
				if (isNullable)
					WriteNullTagLiteral(element, namesp);
				return;
			}

			System.Type type = ob.GetType ();
			if (type == typeof(Mono.Addins.Description.NativeDependency))
			{ }
			else {
				throw CreateUnknownTypeException (ob);
			}

			if (writeWrappingElem) {
				WriteStartElement (element, namesp, ob);
			}

			if (needType) WriteXsiType("NativeReference", "");

			if (writeWrappingElem) WriteEndElement (ob);
		}

		void WriteObject_AddinReference (Mono.Addins.Description.AddinDependency ob, string element, string namesp, bool isNullable, bool needType, bool writeWrappingElem)
		{
			if (((object)ob) == null)
			{
				if (isNullable)
					WriteNullTagLiteral(element, namesp);
				return;
			}

			System.Type type = ob.GetType ();
			if (type == typeof(Mono.Addins.Description.AddinDependency))
			{ }
			else {
				throw CreateUnknownTypeException (ob);
			}

			if (writeWrappingElem) {
				WriteStartElement (element, namesp, ob);
			}

			if (needType) WriteXsiType("AddinReference", "");

			WriteElementString ("AddinId", "", ob.@AddinId);
			WriteElementString ("Version", "", ob.@Version);
			if (writeWrappingElem) WriteEndElement (ob);
		}

		protected override void InitCallbacks ()
		{
		}

	}

}

