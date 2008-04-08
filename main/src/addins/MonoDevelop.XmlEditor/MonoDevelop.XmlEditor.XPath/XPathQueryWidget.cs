//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
//

using Glade;
using Gtk;
using GtkSourceView;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Gui.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Xml;
using System.Xml.XPath;

namespace MonoDevelop.XmlEditor
{/*
	/// <summary>
	/// The XPathQueryWidget uses this interface to
	/// access the currently active XmlEditorViewContent and
	/// jump to a particular file.
	/// </summary>
	public interface IXmlEditorViewContentProvider
	{
		XmlEditorViewContent View {get;}
		
		void JumpTo(string fileName, int line, int column);
	}
	
	public class XPathQueryWidget : GladeWidgetExtract
	{		
		/// <summary>
		/// The filename that the last query was executed on.
		/// </summary>
		string fileName = String.Empty;
		
		/// <summary>
		/// The total number of xpath queries to remember.
		/// </summary>
		const int xpathQueryHistoryLimit = 20;
		int xpathHistoryListCount;
		
		/// <summary>
		/// The number of namespaces we have added to the
		/// grid.
		/// </summary>
		int namespaceCount;

		enum MoveCaret {
			ByJumping = 1,
			ByScrolling = 2
		}
	
		[Widget] TreeView resultsTreeView;
		[Widget] TreeView namespacesTreeView;
		[Widget] Button queryButton;
		[Widget] ComboBoxEntry xpathComboBoxEntry;
		[Widget] Notebook notebook;
		
		// Results List: xpath result, line number, XPathNodeMatch or Exception.
		const int xpathMatchColumnNumber = 0;
		const int xpathMatchLineColumnNumber = 1;
		const int xpathNodeMatchColumnNumber = 2;
		ListStore resultsList = new ListStore(typeof(string), typeof(string), typeof(object));
		
		// Namespace list: prefix, namespace.
		const int prefixColumnNumber = 0;
		const int namespaceColumnNumber = 1;
		ListStore namespacesList = new ListStore(typeof(string), typeof(string));
		
		// XPath query history: xpath query
		ListStore xpathHistoryList = new ListStore(typeof(string));
		
		IXmlEditorViewContentProvider viewProvider;
		
		public XPathQueryWidget(IXmlEditorViewContentProvider viewProvider)
			: base ("XmlEditor.glade", "XPathQueryPad")
		{
			this.viewProvider = viewProvider;
			InitResultsList();
			InitNamespaceList();
					
			queryButton.Clicked += QueryButtonClicked;

			EntryCompletion completion = new EntryCompletion();
			completion.Model = xpathHistoryList;
			completion.TextColumn = 0;
			
			xpathComboBoxEntry.Model = xpathHistoryList;
			xpathComboBoxEntry.TextColumn = 0;
			xpathComboBoxEntry.Entry.Completion = completion;
			xpathComboBoxEntry.Entry.Changed += XPathComboBoxEntryChanged;
			xpathComboBoxEntry.Entry.Activated += XPathComboBoxEntryActivated;
		}
				
		/// <summary>
		/// Adds a namespace to the namespace list.
		/// </summary>
		public void AddNamespace(string prefix, string uri)
		{
			TreeIter iter = namespacesList.Insert(namespaceCount);
			namespacesList.SetValue(iter, prefixColumnNumber, prefix);
			namespacesList.SetValue(iter, namespaceColumnNumber, uri);
			namespaceCount++;
		}
		
		/// <summary>
		/// Gets the list of namespaces in the namespace list.
		/// </summary>
		public ReadOnlyCollection<XmlNamespace> GetNamespaces()
		{
			List<XmlNamespace> namespaces = new List<XmlNamespace>();		
			TreeIter iter;
			bool addNamespaces = namespacesList.GetIterFirst(out iter);
			while (addNamespaces) {
				string prefix = GetNamespacePrefix(iter);
				string uri = GetNamespaceUri(iter);
				if (prefix.Length == 0 && uri.Length == 0) {
					// Ignore.
				} else {
					namespaces.Add(new XmlNamespace(prefix, uri));
				}
				addNamespaces = namespacesList.IterNext(ref iter);
			}
			return new ReadOnlyCollection<XmlNamespace>(namespaces);
		}
			
		/// <summary>
		/// Gets the previously used XPath queries from the combo box drop down list.
		/// </summary>
		public string [] GetXPathHistory()
		{
			List<string> xpaths = new List<string>();
			TreeIter iter;
			bool add = xpathHistoryList.GetIterFirst(out iter);
			while (add) {
				string xpath = (string)xpathHistoryList.GetValue(iter, 0);
				xpaths.Add(xpath);
				add = xpathHistoryList.IterNext(ref iter);
			}
			return xpaths.ToArray();
		}
		
		/// <summary>
		/// Adds the xpath into the combo box drop down list.
		/// </summary>
		public void AddXPath(string xpath)
		{
			xpathHistoryList.AppendValues(xpath);
		}
		
		/// <summary>
		/// Gets or sets the active xpath query.
		/// </summary>
		public string Query {
			get {
				return xpathComboBoxEntry.Entry.Text;
			}
			set {
				xpathComboBoxEntry.Entry.Text = value;
			}
		}
		
		public void UpdateQueryButtonState()
		{
			queryButton.Sensitive = IsXPathQueryEntered && IsXmlEditorViewContentActive;
		}
		
		void InitResultsList()
		{
			resultsTreeView.Model = resultsList;
			resultsTreeView.Selection.Mode = SelectionMode.Single;
			resultsTreeView.RowActivated += ResultsTreeViewRowActivated;
			resultsTreeView.Selection.Changed += ResultsTreeViewSelectionChanged;
				
			// Match column.
			CellRendererText renderer = new CellRendererText();
			resultsTreeView.AppendColumn("Match", renderer, "text", xpathMatchColumnNumber);

			// Line number column.
			renderer = new CellRendererText();
			resultsTreeView.AppendColumn("Line", renderer, "text", xpathMatchLineColumnNumber);
		}
		
		void InitNamespaceList()
		{
			namespacesTreeView.Model = namespacesList;
			namespacesTreeView.Selection.Mode = SelectionMode.Single;
			
			// Prefix column.
			CellRendererText renderer = new CellRendererText();
			renderer.Edited += PrefixEdited;
			renderer.Editable = true;
			namespacesTreeView.AppendColumn("Prefix", renderer, "text", prefixColumnNumber);

			// Namespace column.
			renderer = new CellRendererText();
			renderer.Edited += NamespaceEdited;
			renderer.Editable = true;
			namespacesTreeView.AppendColumn("Namespace", renderer, "text", namespaceColumnNumber);
		
			// Add a few blank rows.
			for (int i = 0; i < 20; ++i) {
				namespacesList.Append();
			}
		}
		
		void PrefixEdited(object source, EditedArgs e)
		{
			ListItemEdited(e, namespacesList, prefixColumnNumber);
		}

		void NamespaceEdited(object source, EditedArgs e)
		{	
			ListItemEdited(e, namespacesList, namespaceColumnNumber);
		}
		
		/// <summary>
		/// Updates the list store item's text that has been 
		/// edited by the user.
		/// </summary>
		void ListItemEdited(EditedArgs e, ListStore list, int column)
		{
			TreePath path = new TreePath(e.Path);
			TreeIter iter;
			if (list.GetIter(out iter, path)) {
				list.SetValue(iter, column, e.NewText);
			}
		}
		
		void XPathComboBoxEntryChanged(object sender, EventArgs e)
		{
			UpdateQueryButtonState();
		}
				
		bool IsXPathQueryEntered {
			get {
				return xpathComboBoxEntry.Entry.Text.Length > 0;
			}
		}
		
		bool IsXmlEditorViewContentActive {
			get {
				return viewProvider.View != null;
			}
		}
		
		void QueryButtonClicked(object sender, EventArgs e)
		{
			RunXPathQuery();
		}
		
		void RunXPathQuery()
		{
			XmlEditorViewContent view = viewProvider.View;
			if (view == null) {
				return;
			}
			
			try {
				fileName = view.FileName;		
				// Clear previous XPath results.
				ClearResults();
				view.Select(0, 0);
				XmlEditorView xmlEditor = view.XmlEditorView;
				xmlEditor.RemoveXPathMarkers();

				// Run XPath query.
				XPathNodeMatch[] nodes = xmlEditor.SelectNodes(Query, GetNamespaces());
				if (nodes.Length > 0) {
					AddXPathResults(nodes);
					xmlEditor.AddXPathMarkers(nodes);
				} else {
					AddNoXPathResult();
				}
				AddXPathToHistory();
			} catch (XPathException xpathEx) {
				AddErrorResult(xpathEx);
			} catch (XmlException xmlEx) {
				AddErrorResult(xmlEx);
			} finally {
				notebook.CurrentPage = 0;
			}
		}
	
		void ClearResults()
		{
			resultsList.Clear();
		}
		
		void AddXPathResults(XPathNodeMatch[] nodes)
		{
			foreach (XPathNodeMatch node in nodes) {
				string line = String.Empty;
				if (node.HasLineInfo()) {
					int lineNumber = node.LineNumber + 1;
					line = lineNumber.ToString();
				}
				resultsList.AppendValues(node.DisplayValue, line, node);
			}
		}
		
		void AddNoXPathResult()
		{
			resultsList.AppendValues("XPath query found 0 items.");
		}
		
		void AddErrorResult(XmlException ex)
		{
			resultsList.AppendValues(ex.Message, ex.LineNumber.ToString(), ex);
		}
		
		void AddErrorResult(XPathException ex)
		{
			resultsList.AppendValues(String.Concat("XPath: ", ex.Message), String.Empty, ex);
		}
		
		void ResultsTreeViewRowActivated(object sender, EventArgs e)
		{
			JumpToResultLocation();
		}		
		/// <summary>
		/// Switches focus to the location of the XPath query result.
		/// </summary>
		void JumpToResultLocation()
		{
			MoveCaretToResultLocation(MoveCaret.ByJumping);
		}
		
		/// <summary>
		/// Scrolls the text editor so the location of the XPath query results is visible.
		/// </summary>
		void ScrollToResultLocation()
		{
			MoveCaretToResultLocation(MoveCaret.ByScrolling);
		}
		
		void MoveCaretToResultLocation(MoveCaret moveCaret)
		{
			TreeIter iter;
			if (resultsTreeView.Selection.GetSelected(out iter)) {
				object tag = resultsList.GetValue(iter, xpathNodeMatchColumnNumber);
				XPathNodeMatch xpathNodeMatch = tag as XPathNodeMatch;
				XPathException xpathException = tag as XPathException;
				XmlException xmlException = tag as XmlException;
				if (xpathNodeMatch != null) {
					MoveCaretToXPathNodeMatch(moveCaret, xpathNodeMatch);
				} else if (xmlException != null) {
					MoveCaretToXmlException(moveCaret, xmlException);
				} else if (xpathException != null && moveCaret == MoveCaret.ByJumping) {					xpathComboBoxEntry.Entry.HasFocus = true;
				}
			}
		}
		
		void MoveCaretToXPathNodeMatch(MoveCaret moveCaret, XPathNodeMatch node)
		{
			if (moveCaret == MoveCaret.ByJumping) {
				viewProvider.JumpTo(fileName, node.LineNumber, node.LinePosition);
			} else {
				ScrollTo(fileName, node.LineNumber, node.LinePosition, node.Value.Length);
			}
		}
		
		void MoveCaretToXmlException(MoveCaret moveCaret, XmlException ex)
		{
			int line =  ex.LineNumber - 1;
			int column = ex.LinePosition - 1;
			if (moveCaret == MoveCaret.ByJumping) {
				viewProvider.JumpTo(fileName, line, column);
			} else {
				ScrollTo(fileName, line, column);
			}
		}
		
		void ScrollTo(string fileName, int line, int column, int length)
		{
			XmlEditorViewContent view = viewProvider.View;
			if (view != null && IsFileNameMatch(view)) {
				view.SetCaretTo(line, column);
				SourceBuffer buffer = (SourceBuffer)view.XmlEditorView.Buffer;
				if (length > 0 && line < buffer.LineCount) {
					TextIter lineIter = buffer.GetIterAtLine(line);
					int startOffset = lineIter.Offset + column;
					int endOffset = startOffset + length;
					view.Select(startOffset, endOffset);
				}
			}
		}
		
		/// <summary>
		/// Tests whether the specified view matches the filename the XPath		/// results were found in.
		/// </summary>
		bool IsFileNameMatch(XmlEditorViewContent view)
		{
			return fileName == view.FileName;
		}
		
		/// <summary>
		/// Adds the text in the combo box to the combo box drop down list.
		/// </summary>
		void AddXPathToHistory()
		{
			string newXPath = Query;
			if (!XPathHistoryListContains(newXPath)) {
				TreeIter iter = xpathHistoryList.Prepend();
				xpathHistoryList.SetValue(iter, 0, newXPath);
				xpathHistoryListCount++;
				if (xpathHistoryListCount > xpathQueryHistoryLimit) {	
					iter = GetLastIter(xpathHistoryList);
					xpathHistoryList.Remove(ref iter);
				}
			}
		}
		
		/// <summary>
		/// Brute force approach to get last list item.
		/// </summary>
		TreeIter GetLastIter(ListStore list)
		{
			TreeIter iter = TreeIter.Zero;
			TreeIter lastIter = iter;
			bool success = list.GetIterFirst(out iter);
			while (success) {
				lastIter = iter;
				success = list.IterNext(ref iter);
			}
			return lastIter;
		}
		
		/// <summary>
		/// Returns true if the xpath already exists.
		/// </summary>
		bool XPathHistoryListContains(string xpath)
		{
			TreeIter iter;
			bool success = xpathHistoryList.GetIterFirst(out iter);
			while (success) {
				string existingXPath = (string)xpathHistoryList.GetValue(iter, 0);
				if (xpath.Equals(existingXPath, StringComparison.InvariantCultureIgnoreCase)) { 
					return true;
				}
				success = xpathHistoryList.IterNext(ref iter);
			}
			return false;
		}
		void ResultsTreeViewSelectionChanged(object sender, EventArgs e)
		{
			ScrollToResultLocation();
		}
		
		/// <summary>
		/// Gets the namespace prefix from the specified
		/// list store row.
		/// </summary>
		string GetNamespacePrefix(TreeIter iter)
		{
			string prefix = (string)namespacesList.GetValue(iter, prefixColumnNumber);
			if (prefix != null) {
				return prefix;
			}
			return String.Empty;
		}
		
		/// <summary>
		/// Gets the namespace uri from the specified
		/// list store row.
		/// </summary>
		string GetNamespaceUri(TreeIter iter)
		{
			string uri = (string)namespacesList.GetValue(iter, namespaceColumnNumber);
			if (uri != null) {
				return uri;
			}
			return String.Empty;
		}
 		
		void ScrollTo(string fileName, int line, int column)
		{
			ScrollTo(fileName, line, column, 0);
		}	
		
		void XPathComboBoxEntryActivated(object source, EventArgs e)
		{
			RunXPathQuery();
		}
	}*/
}
