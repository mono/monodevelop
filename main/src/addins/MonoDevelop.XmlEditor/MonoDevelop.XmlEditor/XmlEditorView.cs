//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
// Copyright (C) 2004-2007 MonoDevelop Team
//

using Gdk;
using Gtk;
using GtkSourceView;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.Gui.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;

namespace MonoDevelop.XmlEditor
{
	public class XmlEditorView : SourceView, ICompletionWidget, IClipboardHandler
	{	
		XmlSchemaCompletionDataCollection schemaCompletionDataItems = new XmlSchemaCompletionDataCollection();
		XmlSchemaCompletionData defaultSchemaCompletionData = null;
		string defaultNamespacePrefix = String.Empty;
		bool autoCompleteElements = true;
		bool showSchemaAnnotation = true;
		SourceBuffer buffer;
		Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
		XPathNodeTextMarker xpathNodeTextMarker;
		event EventHandler completionContextChanged;
		ICodeCompletionContext completionContext;
		XmlEditorViewContent viewContent;
		
		public XmlEditorView (XmlEditorViewContent viewContent)
		{
			this.viewContent = viewContent;
			InitSyntaxHighlighting();
			xpathNodeTextMarker = new XPathNodeTextMarker(buffer);
			Buffer.Changed += BufferChanged;
		}

		/// <summary>
		/// Gets the schemas that the xml editor will use.
		/// </summary>
		/// <remarks>
		/// Probably should NOT have a 'set' property, but allowing one
		/// allows us to share the completion data amongst multiple
		/// xml editor controls.
		/// </remarks>
		public XmlSchemaCompletionDataCollection SchemaCompletionDataItems {
			get {
				return schemaCompletionDataItems;
			}
			set {
				schemaCompletionDataItems = value;
			}
		}
		
#region XPath stuff

		/// <summary>
		/// Finds the xml nodes that match the specified xpath.
		/// </summary>
		/// <returns>An array of XPathNodeMatch items. These include line number 
		/// and line position information aswell as the node found.</returns>
		public static XPathNodeMatch[] SelectNodes(string xml, string xpath, ReadOnlyCollection<XmlNamespace> namespaces)
		{
			XmlTextReader xmlReader = new XmlTextReader(new StringReader(xml));
			xmlReader.XmlResolver = null;
			XPathDocument doc = new XPathDocument(xmlReader);
			XPathNavigator navigator = doc.CreateNavigator();
			
			// Add namespaces.
			XmlNamespaceManager namespaceManager = new XmlNamespaceManager(navigator.NameTable);
			foreach (XmlNamespace xmlNamespace in namespaces) {
				namespaceManager.AddNamespace(xmlNamespace.Prefix, xmlNamespace.Uri);
			}
	
			// Run the xpath query.                                                        
			XPathNodeIterator iterator = navigator.Select(xpath, namespaceManager);
			
			List<XPathNodeMatch> nodes = new List<XPathNodeMatch>();
			while (iterator.MoveNext()) {
				nodes.Add(new XPathNodeMatch(iterator.Current));
			}			
			return nodes.ToArray();
		}
		
		/// <summary>
		/// Finds the xml nodes that match the specified xpath.
		/// </summary>
		/// <returns>An array of XPathNodeMatch items. These include line number 
		/// and line position information aswell as the node found.</returns>
		public static XPathNodeMatch[] SelectNodes(string xml, string xpath)
		{
			List<XmlNamespace> list = new List<XmlNamespace>();
			return SelectNodes(xml, xpath, new ReadOnlyCollection<XmlNamespace>(list));
		}
		
		/// <summary>
		/// Finds the xml nodes in the current document that match the specified xpath.
		/// </summary>
		/// <returns>An array of XPathNodeMatch items. These include line number 
		/// and line position information aswell as the node found.</returns>
		public XPathNodeMatch[] SelectNodes(string xpath, ReadOnlyCollection<XmlNamespace> namespaces)
		{
			return SelectNodes(Buffer.Text, xpath, namespaces);
		}
		
		/// <summary>
		/// Highlights the xpath matches in the xml.
		/// </summary>
		public void AddXPathMarkers(XPathNodeMatch[] nodes)
		{
			xpathNodeTextMarker.AddMarkers(nodes);
		}
		
		/// <summary>
		/// Removes the xpath match highlighting.
		/// </summary>
		public void RemoveXPathMarkers()
		{
			xpathNodeTextMarker.RemoveMarkers();
		}
#endregion


	}
}
