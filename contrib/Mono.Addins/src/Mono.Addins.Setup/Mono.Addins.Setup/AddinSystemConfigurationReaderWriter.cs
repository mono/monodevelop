//
// AddinSystemConfigurationReader.cs
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
	internal class AddinSystemConfigurationReader : XmlSerializationReader
	{
		public object ReadRoot_AddinSystemConfiguration ()
		{
			Reader.MoveToContent();
			if (Reader.LocalName != "AddinSystemConfiguration" || Reader.NamespaceURI != "")
				throw CreateUnknownNodeException();
			return ReadObject_AddinSystemConfiguration (true, true);
		}

		public Mono.Addins.Setup.AddinSystemConfiguration ReadObject_AddinSystemConfiguration (bool isNullable, bool checkType)
		{
			Mono.Addins.Setup.AddinSystemConfiguration ob = null;
			if (isNullable && ReadNull()) return null;

			if (checkType) 
			{
				System.Xml.XmlQualifiedName t = GetXsiType();
				if (t == null)
				{ }
				else if (t.Name != "AddinSystemConfiguration" || t.Namespace != "")
					throw CreateUnknownTypeException(t);
			}

			ob = new Mono.Addins.Setup.AddinSystemConfiguration ();

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

			while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
			{
				if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (Reader.LocalName == "AddinPaths" && Reader.NamespaceURI == "" && !b3) {
						if (((object)ob.@AddinPaths) == null)
							throw CreateReadOnlyCollectionException ("System.Collections.Specialized.StringCollection");
						if (Reader.IsEmptyElement) {
							Reader.Skip();
						} else {
							int n4 = 0;
							Reader.ReadStartElement();
							Reader.MoveToContent();

							while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
							{
								if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
								{
									if (Reader.LocalName == "Addin" && Reader.NamespaceURI == "") {
										if (((object)ob.@AddinPaths) == null)
											throw CreateReadOnlyCollectionException ("System.Collections.Specialized.StringCollection");
										ob.@AddinPaths.Add (Reader.ReadElementString ());
										n4++;
									}
									else UnknownNode (null);
								}
								else UnknownNode (null);

								Reader.MoveToContent();
							}
							ReadEndElement();
						}
						b3 = true;
					}
					else if (Reader.LocalName == "RepositoryIdCount" && Reader.NamespaceURI == "" && !b1) {
						b1 = true;
						ob.@RepositoryIdCount = Int32.Parse (Reader.ReadElementString (), CultureInfo.InvariantCulture);
					}
					else if (Reader.LocalName == "DisabledAddins" && Reader.NamespaceURI == "" && !b2) {
						if (((object)ob.@DisabledAddins) == null)
							throw CreateReadOnlyCollectionException ("System.Collections.Specialized.StringCollection");
						if (Reader.IsEmptyElement) {
							Reader.Skip();
						} else {
							int n5 = 0;
							Reader.ReadStartElement();
							Reader.MoveToContent();

							while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
							{
								if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
								{
									if (Reader.LocalName == "Addin" && Reader.NamespaceURI == "") {
										if (((object)ob.@DisabledAddins) == null)
											throw CreateReadOnlyCollectionException ("System.Collections.Specialized.StringCollection");
										ob.@DisabledAddins.Add (Reader.ReadElementString ());
										n5++;
									}
									else UnknownNode (null);
								}
								else UnknownNode (null);

								Reader.MoveToContent();
							}
							ReadEndElement();
						}
						b2 = true;
					}
					else if (Reader.LocalName == "Repositories" && Reader.NamespaceURI == "" && !b0) {
						if (((object)ob.@Repositories) == null)
							throw CreateReadOnlyCollectionException ("System.Collections.ArrayList");
						if (Reader.IsEmptyElement) {
							Reader.Skip();
						} else {
							int n6 = 0;
							Reader.ReadStartElement();
							Reader.MoveToContent();

							while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
							{
								if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
								{
									if (Reader.LocalName == "Repository" && Reader.NamespaceURI == "") {
										if (((object)ob.@Repositories) == null)
											throw CreateReadOnlyCollectionException ("System.Collections.ArrayList");
										ob.@Repositories.Add (ReadObject_RepositoryRecord (false, true));
										n6++;
									}
									else UnknownNode (null);
								}
								else UnknownNode (null);

								Reader.MoveToContent();
							}
							ReadEndElement();
						}
						b0 = true;
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

		public Mono.Addins.Setup.RepositoryRecord ReadObject_RepositoryRecord (bool isNullable, bool checkType)
		{
			Mono.Addins.Setup.RepositoryRecord ob = null;
			if (isNullable && ReadNull()) return null;

			if (checkType) 
			{
				System.Xml.XmlQualifiedName t = GetXsiType();
				if (t == null)
				{ }
				else if (t.Name != "RepositoryRecord" || t.Namespace != "")
					throw CreateUnknownTypeException(t);
			}

			ob = new Mono.Addins.Setup.RepositoryRecord ();

			Reader.MoveToElement();

			while (Reader.MoveToNextAttribute())
			{
				if (Reader.LocalName == "id" && Reader.NamespaceURI == "") {
					ob.@Id = Reader.Value;
				}
				else if (IsXmlnsAttribute (Reader.Name)) {
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

			bool b7=false, b8=false, b9=false, b10=false, b11=false;

			while (Reader.NodeType != System.Xml.XmlNodeType.EndElement) 
			{
				if (Reader.NodeType == System.Xml.XmlNodeType.Element) 
				{
					if (Reader.LocalName == "File" && Reader.NamespaceURI == "" && !b8) {
						b8 = true;
						ob.@File = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "IsReference" && Reader.NamespaceURI == "" && !b7) {
						b7 = true;
						ob.@IsReference = XmlConvert.ToBoolean (Reader.ReadElementString ());
					}
					else if (Reader.LocalName == "Name" && Reader.NamespaceURI == "" && !b10) {
						b10 = true;
						ob.@Name = Reader.ReadElementString ();
					}
					else if (Reader.LocalName == "Url" && Reader.NamespaceURI == "" && !b9) {
						b9 = true;
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

		protected override void InitCallbacks ()
		{
		}

		protected override void InitIDs ()
		{
		}

	}

	internal class AddinSystemConfigurationWriter : XmlSerializationWriter
	{
		const string xmlNamespace = "http://www.w3.org/2000/xmlns/";
		public void WriteRoot_AddinSystemConfiguration (object o)
		{
			WriteStartDocument ();
			Mono.Addins.Setup.AddinSystemConfiguration ob = (Mono.Addins.Setup.AddinSystemConfiguration) o;
			TopLevelElement ();
			WriteObject_AddinSystemConfiguration (ob, "AddinSystemConfiguration", "", true, false, true);
		}

		void WriteObject_AddinSystemConfiguration (Mono.Addins.Setup.AddinSystemConfiguration ob, string element, string namesp, bool isNullable, bool needType, bool writeWrappingElem)
		{
			if (((object)ob) == null)
			{
				if (isNullable)
					WriteNullTagLiteral(element, namesp);
				return;
			}

			System.Type type = ob.GetType ();
			if (type == typeof(Mono.Addins.Setup.AddinSystemConfiguration))
			{ }
			else {
				throw CreateUnknownTypeException (ob);
			}

			if (writeWrappingElem) {
				WriteStartElement (element, namesp, ob);
			}

			if (needType) WriteXsiType("AddinSystemConfiguration", "");

			if (ob.@Repositories != null) {
				WriteStartElement ("Repositories", "", ob.@Repositories);
				for (int n12 = 0; n12 < ob.@Repositories.Count; n12++) {
					WriteObject_RepositoryRecord (((Mono.Addins.Setup.RepositoryRecord) ob.@Repositories[n12]), "Repository", "", false, false, true);
				}
				WriteEndElement (ob.@Repositories);
			}
			WriteElementString ("RepositoryIdCount", "", ob.@RepositoryIdCount.ToString(CultureInfo.InvariantCulture));
			if (ob.@DisabledAddins != null) {
				WriteStartElement ("DisabledAddins", "", ob.@DisabledAddins);
				for (int n13 = 0; n13 < ob.@DisabledAddins.Count; n13++) {
					WriteElementString ("Addin", "", ob.@DisabledAddins[n13]);
				}
				WriteEndElement (ob.@DisabledAddins);
			}
			if (ob.@AddinPaths != null) {
				WriteStartElement ("AddinPaths", "", ob.@AddinPaths);
				for (int n14 = 0; n14 < ob.@AddinPaths.Count; n14++) {
					WriteElementString ("Addin", "", ob.@AddinPaths[n14]);
				}
				WriteEndElement (ob.@AddinPaths);
			}
			if (writeWrappingElem) WriteEndElement (ob);
		}

		void WriteObject_RepositoryRecord (Mono.Addins.Setup.RepositoryRecord ob, string element, string namesp, bool isNullable, bool needType, bool writeWrappingElem)
		{
			if (((object)ob) == null)
			{
				if (isNullable)
					WriteNullTagLiteral(element, namesp);
				return;
			}

			System.Type type = ob.GetType ();
			if (type == typeof(Mono.Addins.Setup.RepositoryRecord))
			{ }
			else {
				throw CreateUnknownTypeException (ob);
			}

			if (writeWrappingElem) {
				WriteStartElement (element, namesp, ob);
			}

			if (needType) WriteXsiType("RepositoryRecord", "");

			WriteAttribute ("id", "", ob.@Id);

			WriteElementString ("IsReference", "", (ob.@IsReference?"true":"false"));
			WriteElementString ("File", "", ob.@File);
			WriteElementString ("Url", "", ob.@Url);
			WriteElementString ("Name", "", ob.@Name);
			WriteElementString ("LastModified", "", ob.@LastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture));
			if (writeWrappingElem) WriteEndElement (ob);
		}

		protected override void InitCallbacks ()
		{
		}

	}

}

