//
// Copyright (c) Microsoft Corp
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor
{
	class XmlExpandSelectionHandler
	{
		public static bool CanExpandSelection (TextEditor editor)
		{
			if (!editor.IsSomethingSelected) {
				return true;
			}
			if (editor.Selections.Count () == 1) {
				return editor.SelectionRange.Offset > 0 || editor.SelectionRange.Length != editor.Length;
			}
			return false;
		}

		internal static void ExpandSelection (TextEditor editor, Func<XmlParser> getTreeParser)
		{
			var selectionAnnotation = GetAnnotation (editor, getTreeParser);
			if (selectionAnnotation.NodePath.Count == 0)
				return;

			var newRegion = selectionAnnotation.Grow ();
			if (newRegion.HasValue) {
				editor.SetSelection (newRegion.Value.Begin, newRegion.Value.End);
			}
		}

		public static bool CanShrinkSelection (TextEditor editor)
		{
			return editor.IsSomethingSelected && editor.Selections.Count () == 1;
		}

		internal static void ShrinkSelection (TextEditor editor, Func<XmlParser> getTreeParser)
		{
			var selectionAnnotation = GetAnnotation (editor, getTreeParser);
			if (selectionAnnotation.NodePath.Count == 0)
				return;

			var newRegion = selectionAnnotation.Shrink ();
			if (newRegion.HasValue) {
				editor.SetSelection (newRegion.Value.Begin, newRegion.Value.End);
			} else {
				editor.ClearSelection ();
			}
		}

		static XmlExpandSelectionAnnotation GetAnnotation (TextEditor editor, Func<XmlParser> getTreeParser)
		{
			var result = editor.Annotation<XmlExpandSelectionAnnotation> ();
			if (result == null) {
				result = new XmlExpandSelectionAnnotation (editor, getTreeParser ());
				editor.AddAnnotation (result);
			}
			return result;
		}

		enum SelectionLevel
		{
			Self,
			Name,
			Content,
			WithClosing,
			Document,
			Attributes
		}

		class XmlExpandSelectionAnnotation
		{
			ImmutableStack<(int, SelectionLevel)> expansions = ImmutableStack<(int, SelectionLevel)>.Empty;

			readonly IReadonlyTextDocument document;
			readonly TextEditor editor;
			readonly XmlParser parser;
			public List<XObject> NodePath { get; }
			public int Index { get; set; } = -1;
			public SelectionLevel Level { get; set; }

			public XmlExpandSelectionAnnotation (TextEditor editor, XmlParser parser)
			{
				this.parser = parser;
				this.editor = editor;
				document = editor.CreateDocumentSnapshot ();
				editor.CaretPositionChanged += Editor_CaretPositionChanged;
				NodePath = GetNodePath (parser, document);
			}

			void Editor_CaretPositionChanged (object sender, EventArgs e)
			{
				editor.CaretPositionChanged -= Editor_CaretPositionChanged;
				editor.RemoveAnnotations<XmlExpandSelectionAnnotation> ();
			}

			DocumentRegion? GetCurrent ()
			{
				if (Index < 0) {
					return null;
				}
				var current = NodePath [Index];
				switch (Level) {
				case SelectionLevel.Self:
					return current.Region;
				case SelectionLevel.WithClosing:
					var element = (XElement)current;
					return new DocumentRegion (element.Region.Begin, element.ClosingTag.Region.End);
				case SelectionLevel.Name:
					return current.TryGetNameRegion ().Value;
				case SelectionLevel.Content:
					if (current is XElement el) {
						return new DocumentRegion (el.Region.End, el.ClosingTag.Region.Begin);
					}
					return ((XAttribute)current).GetAttributeValueRegion (document);
				case SelectionLevel.Document:
					return new DocumentRegion (new DocumentLocation (1, 1), document.OffsetToLocation (document.Length));
				case SelectionLevel.Attributes:
					return ((XElement)current).GetAttributesRegion ();
				}
				throw new InvalidOperationException ();
			}

			public DocumentRegion? Grow ()
			{
				var old = (Index, Level);
				if (GrowStateInternal ()) {
					expansions = expansions.Push (old);
					return GetCurrent ();
				}
				return null;
			}

			bool GrowStateInternal ()
			{
				if (Index + 1 == NodePath.Count) {
					return false;
				}

				//if an index is selected, we may need to transition level rather than transitioning index
				if (Index >= 0) {
					var current = NodePath [Index];
					if (current is XElement element) {
						switch (Level) {
						case SelectionLevel.Self:
							if (!element.IsSelfClosing) {
								Level = SelectionLevel.WithClosing;
								return true;
							}
							break;
						case SelectionLevel.Content:
							Level = SelectionLevel.WithClosing;
							return true;
						case SelectionLevel.Name:
							Level = SelectionLevel.Self;
							return true;
						case SelectionLevel.Attributes:
							Level = SelectionLevel.Self;
							return true;
						}
					} else if (current is XAttribute att) {
						switch (Level) {
						case SelectionLevel.Name:
						case SelectionLevel.Content:
							Level = SelectionLevel.Self;
							return true;
						}
					} else if (Level == SelectionLevel.Name) {
						Level = SelectionLevel.Self;
						return true;
					} else if (Level == SelectionLevel.Document) {
						return false;
					}
				}

				//advance up the node path
				Index++;
				var newNode = NodePath [Index];

				//determine the starting selection level for the new node
				if (newNode is XDocument) {
					Level = SelectionLevel.Document;
					return true;
				}

				AdvanceUntilClosed (newNode, parser, document);

				if (newNode.Region.ContainsOuter (editor.CaretLocation)) {
					var nr = newNode.TryGetNameRegion ();
					if (nr != null && nr.Value.ContainsOuter (editor.CaretLocation)) {
						Level = SelectionLevel.Name;
						return true;
					}
					if (newNode is XAttribute attribute) {
						var valRegion = attribute.GetAttributeValueRegion (document);
						if (valRegion.ContainsOuter (editor.CaretLocation)) {
							Level = SelectionLevel.Content;
							return true;
						}
					}
					if (newNode is XElement xElement && xElement.Attributes.Count > 1) {
						var attsRegion = xElement.GetAttributesRegion ();
						if (attsRegion.ContainsOuter (editor.CaretLocation)) {
							Level = SelectionLevel.Attributes;
							return true;
						}
					}
					Level = SelectionLevel.Self;
					return true;
				}

				if (newNode is XElement el) {
					if (el.IsSelfClosing) {
						Level = SelectionLevel.Self;
						return true;
					}
					if (el.ClosingTag.Region.ContainsOuter (editor.CaretLocation)) {
						Level = SelectionLevel.WithClosing;
						return true;
					}
					Level = SelectionLevel.Content;
					return true;
				}

				Level = SelectionLevel.Self;
				return true;
			}

			public DocumentRegion? Shrink ()
			{
				// if we have expansion state, pop it
				if (!expansions.IsEmpty) {
					expansions = expansions.Pop (out var last);
					Index = last.Item1;
					Level = last.Item2;
					return GetCurrent ();
				}

				return null;
			}

			//advance the parser in chunks until the given node is complete
			static void AdvanceUntilClosed (XObject ob, XmlParser parser, IReadonlyTextDocument document)
			{
				const int chunk = 200;
				var el = ob as XElement;
				while (parser.Position < document.Length) {
					parser.Parse (document.CreateReader (parser.Position, Math.Min (document.Length - parser.Position, chunk)));
					if (el?.IsClosed ?? ob.IsEnded || !parser.Nodes.Contains (ob.Parent)) {
						break;
					}
				}
			}

			static List<XObject> GetNodePath (XmlParser parser, IReadonlyTextDocument document)
			{
				int offset = parser.Position;
				var length = document.Length;
				int i = offset;

				var nodePath = parser.Nodes.ToList ();

				//if inside body of unclosed element, capture whole body
				if (parser.CurrentState is XmlRootState && parser.Nodes.Peek () is XElement unclosedEl) {
					while (i < length && InRootOrClosingTagState () && !unclosedEl.IsClosed) {
						parser.Push (document.GetCharAt (i++));
					}
				}

				//if in potential start of a state, capture it
				else if (parser.CurrentState is XmlRootState && GetStateTag() > 0) {
					//eat until we figure out whether it's a state transition 
					while (i < length && GetStateTag () > 0) {
						parser.Push (document.GetCharAt (i++));
					}
					//if it transitioned to another state, eat until we get a new node on the stack
					if (NotInRootState ()) {
						var newState = parser.CurrentState;
						while (i < length && NotInRootState() && parser.Nodes.Count <= nodePath.Count) {
							parser.Push (document.GetCharAt (i++));
						}
						if (parser.Nodes.Count > nodePath.Count) {
							nodePath.Insert (0, parser.Nodes.Peek ());
						}
					}
				}

				//ensure any unfinished names are captured
				while (i < length && InNameOrAttributeState ()) {
					parser.Push (document.GetCharAt (i++));
				}

				//if nodes are incomplete, they won't get connected
				//HACK: the only way to reconnect them is reflection
				if (nodePath.Count > 1) {
					for (int idx = 0; idx < nodePath.Count - 1; idx++) {
						var node = nodePath [idx];
						if (node.Parent == null) {
							var parent = nodePath [idx + 1];
							node.Parent = parent;
						}
					}
				}

				return nodePath;

				bool InNameOrAttributeState () =>
					parser.CurrentState is XmlNameState
						|| parser.CurrentState is XmlAttributeState
						  || parser.CurrentState is XmlAttributeValueState;

				bool InRootOrClosingTagState () =>
					parser.CurrentState is XmlRootState
					  || parser.CurrentState is XmlNameState
					  || parser.CurrentState is XmlClosingTagState;

				int GetStateTag () => ((IXmlParserContext)parser).StateTag;

				bool NotInRootState () => !(parser.CurrentState is XmlRootState);
			}
		}
	}

	internal static class XmlExtensions
	{
		public static DocumentRegion? TryGetNameRegion (this XObject xobject)
		{
			if (xobject is XElement element) {
				var r = element.Region;
				return new DocumentRegion (r.BeginLine, r.BeginColumn + 1, r.BeginLine, r.BeginColumn + 1 + element.Name.FullName.Length);
			}
			if (xobject is XClosingTag closingTag) {
				var r = closingTag.Region;
				return new DocumentRegion (r.BeginLine, r.BeginColumn + 2, r.BeginLine, r.BeginColumn + 2 + closingTag.Name.FullName.Length);
			}
			if (xobject is XAttribute attribute) {
				var r = attribute.Region;
				return new DocumentRegion (r.BeginLine, r.BeginColumn, r.BeginLine, r.BeginColumn + attribute.Name.FullName.Length);
			}
			return null;
		}

		public static DocumentRegion GetAttributeValueRegion (this XAttribute att, IReadonlyTextDocument doc)
		{
			int endOffset = doc.LocationToOffset (att.Region.End) - 1;
			int startOffset = endOffset - att.Value.Length;
			return new DocumentRegion (doc.OffsetToLocation (startOffset), doc.OffsetToLocation (endOffset));
		}

		public static DocumentRegion GetAttributesRegion (this XElement element)
		{
			return new DocumentRegion (element.Attributes.First.Region.Begin, element.Attributes.Last.Region.End);
		}

		public static bool ContainsOuter (this DocumentRegion region, DocumentLocation location)
		{
			return region.Begin <= location && location <= region.End;
		}
	}
}
