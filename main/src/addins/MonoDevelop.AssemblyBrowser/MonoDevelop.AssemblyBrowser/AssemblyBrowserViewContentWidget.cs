//
// AssemblyBrowserViewContentWidget.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.ComponentModel;
using System.Text;
using System.Xml;
using Gtk;

using Mono.Cecil;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using System.Linq;
using Mono.TextEditor;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.Documentation;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using Mono.TextEditor.Theatrics;
using MonoDevelop.SourceEditor;
using XmlDocIdLib;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.AssemblyBrowser
{
	public class AssemblyBrowserViewContentWidget : Gtk.VBox
	{
		public bool PublicApiOnly {
			get {
				return true;
			}
		}

		readonly Ambience ambience = AmbienceService.GetAmbience ("text/x-csharp");
		public Ambience Ambience {
			get { return ambience; }
		}

		readonly TextEditor inspectEditor;

		public class AssemblyBrowserTreeView : ExtensibleTreeView
		{
			bool publicApiOnly = true;

			public bool PublicApiOnly {
				get {
					return publicApiOnly;
				}
				set {
					if (publicApiOnly == value)
						return;
					publicApiOnly = value;
					var root = GetRootNode ();
					if (root != null)
						RefreshNode (root);
				}
			}

			public AssemblyBrowserTreeView (NodeBuilder[] builders, TreePadOption[] options) : base (builders, options)
			{
			}
		}

		public AssemblyBrowserViewContentWidget ()
		{
			var options = new MonoDevelop.Ide.Gui.CommonTextEditorOptions () {
				ShowFoldMargin = false,
				ShowIconMargin = false,
				ShowLineNumberMargin = false,
				HighlightCaretLine = true,
			};
			inspectEditor = new TextEditor (new TextDocument (), options);
			inspectEditor.ButtonPressEvent += HandleInspectEditorButtonPressEvent;

			this.inspectEditor.Document.ReadOnly = true;
//			this.inspectEditor.Document.SyntaxMode = new Mono.TextEditor.Highlighting.MarkupSyntaxMode ();
			this.inspectEditor.TextViewMargin.GetLink = delegate(Mono.TextEditor.MarginMouseEventArgs arg) {
				var loc = inspectEditor.PointToLocation (arg.X, arg.Y);
				int offset = inspectEditor.LocationToOffset (loc);
				var referencedSegment = ReferencedSegments != null ? ReferencedSegments.FirstOrDefault (seg => seg.Segment.Contains (offset)) : null;
				if (referencedSegment == null)
					return null;
				if (referencedSegment.Reference is TypeDefinition)
					return new XmlDocIdGenerator ().GetXmlDocPath ((TypeDefinition)referencedSegment.Reference);

				if (referencedSegment.Reference is MethodDefinition)
					return new XmlDocIdGenerator ().GetXmlDocPath ((MethodDefinition)referencedSegment.Reference);

				if (referencedSegment.Reference is PropertyDefinition)
					return new XmlDocIdGenerator ().GetXmlDocPath ((PropertyDefinition)referencedSegment.Reference);

				if (referencedSegment.Reference is FieldDefinition)
					return new XmlDocIdGenerator ().GetXmlDocPath ((FieldDefinition)referencedSegment.Reference);

				if (referencedSegment.Reference is EventDefinition)
					return new XmlDocIdGenerator ().GetXmlDocPath ((EventDefinition)referencedSegment.Reference);

				if (referencedSegment.Reference is FieldDefinition)
					return new XmlDocIdGenerator ().GetXmlDocPath ((FieldDefinition)referencedSegment.Reference);

				if (referencedSegment.Reference is TypeReference) {
					return new XmlDocIdGenerator ().GetXmlDocPath ((TypeReference)referencedSegment.Reference);
				}
				return referencedSegment.Reference.ToString ();
			};
			this.inspectEditor.LinkRequest += InspectEditorhandleLinkRequest;

			var documentationScrolledWindow = new CompactScrolledWindow ();
			documentationScrolledWindow.Child = inspectEditor;
			this.PackStart (documentationScrolledWindow, true, true, 0);

			this.ShowAll ();
		}

		[CommandHandler (EditCommands.Copy)]
		protected void OnCopyCommand ()
		{
			inspectEditor.RunAction (Mono.TextEditor.ClipboardActions.Copy);
		}

		[CommandHandler (EditCommands.SelectAll)]
		protected void OnSelectAllCommand ()
		{
			inspectEditor.RunAction (Mono.TextEditor.SelectionActions.SelectAll);
		}

		void HandleInspectEditorButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button != 3)
				return;
			var menuSet = new CommandEntrySet ();
			menuSet.AddItem (EditCommands.SelectAll);
			menuSet.AddItem (EditCommands.Copy);
			IdeApp.CommandService.ShowContextMenu (menuSet, this);
		}

		void InspectEditorhandleLinkRequest (object sender, Mono.TextEditor.LinkEventArgs args)
		{
			/*
			var loader = (AssemblyLoader)this.TreeView.GetSelectedNode ().GetParentDataItem (typeof(AssemblyLoader), true);

			if (args.Button == 2 || (args.Button == 1 && (args.ModifierState & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask)) {
				AssemblyBrowserViewContent assemblyBrowserView = new AssemblyBrowserViewContent ();
				foreach (var cu in definitions) {
					assemblyBrowserView.Load (cu.UnresolvedAssembly.AssemblyName);
				}
				IdeApp.Workbench.OpenDocument (assemblyBrowserView, true);
				((AssemblyBrowserWidget)assemblyBrowserView.Control).Open (args.Link);
			} else {
				this.Open (args.Link, loader);
			}*/
		}

		List<ReferenceSegment> ReferencedSegments = new List<ReferenceSegment>();
		List<UnderlineMarker> underlineMarkers = new List<UnderlineMarker> ();

		public void ClearReferenceSegment ()
		{
			ReferencedSegments = null;
			underlineMarkers.ForEach (m => inspectEditor.Document.RemoveMarker (m));
			underlineMarkers.Clear ();
		}

		internal void SetReferencedSegments (List<ReferenceSegment> refs)
		{
			ReferencedSegments = refs;
			if (ReferencedSegments == null)
				return;
			foreach (var seg in refs) {
				DocumentLine line = inspectEditor.GetLineByOffset (seg.Offset);
				if (line == null)
					continue;
				// FIXME: ILSpy sometimes gives reference segments for punctuation. See http://bugzilla.xamarin.com/show_bug.cgi?id=2918
				string text = inspectEditor.GetTextAt (seg);
				if (text != null && text.Length == 1 && !(char.IsLetter (text [0]) || text [0] == '…'))
					continue;
				var marker = new UnderlineMarker (new Cairo.Color (0, 0, 1.0), 1 + seg.Offset - line.Offset, 1 + seg.EndOffset - line.Offset);
				marker.Wave = false;
				underlineMarkers.Add (marker);
				inspectEditor.Document.AddMarker (line, marker);
			}
		}

		public void Show (ITreeNavigator nav)
		{
			if (nav == null)
				return;
			IAssemblyBrowserNodeBuilder builder = nav.TypeNodeBuilder as IAssemblyBrowserNodeBuilder;
			if (builder == null) {
				this.inspectEditor.Document.Text = "";
				return;
			}

			ClearReferenceSegment ();
			inspectEditor.Document.ClearFoldSegments ();
			/*}			case 1:
				inspectEditor.Options.ShowFoldMargin = true;
				this.inspectEditor.Document.MimeType = "text/x-ilasm";
				SetReferencedSegments (builder.Disassemble (inspectEditor.GetTextEditorData (), nav));
				break;*/

			inspectEditor.Options.ShowFoldMargin = true;
			this.inspectEditor.Document.MimeType = "text/x-csharp";
			SetReferencedSegments (builder.Decompile (inspectEditor.GetTextEditorData (), nav, PublicApiOnly));
			this.inspectEditor.QueueDraw ();
		}
	}
}

