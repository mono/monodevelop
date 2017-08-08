//
// AbstractCodeLensExtension.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using MonoDevelop.Ide.Gui.Content;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide.FindInFiles;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Diagnostics.CodeAnalysis;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using System.Collections.Immutable;
using Mono.Addins;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.TypeSystem;
using Cairo;

namespace MonoDevelop.Ide.Editor.Extension
{
	public sealed class GtkCodeLansDrawingParameters : CodeLansDrawingParameters
	{
		public Pango.Layout Layout { get; private set; }
		public Context Context { get; private set; }

		public GtkCodeLansDrawingParameters (TextEditor editor, int lineNr, Xwt.Rectangle lineArea, double x, double y, Pango.Layout layout, Context context) : base (editor, lineNr, lineArea, x, y)
		{
			Layout = layout;
			Context = context;
		}
	}

	public abstract class CodeLansDrawingParameters
	{
		public TextEditor Editor { get; private set; }
		public int LineNr { get; private set; }
		public Xwt.Rectangle LineArea { get; private set; }

		public double X { get; private set; }
		public double Y { get; private set; }

		public CodeLansDrawingParameters (TextEditor editor, int lineNr, Xwt.Rectangle lineArea, double x, double y)
		{
			Editor = editor;
			LineNr = lineNr;
			LineArea = lineArea;
			X = x;
			Y = y;
		}
	}

	public abstract class CodeLens
	{
		public abstract TextSegment CodeLensSpan { get; }

		public abstract void Draw (CodeLansDrawingParameters drawingParameters);

		public abstract Xwt.Size Size { get; }
	}

	public abstract class CodeLensProvider
	{
		internal string MimeType { get; set; }

		public abstract Task<IEnumerable<CodeLens>> GetLenses (TextEditor editor, DocumentContext ctx, CancellationToken token);
	}

	class CodeLensTextEditorExtension : TextEditorExtension
	{
		CancellationTokenSource src = new CancellationTokenSource ();
		bool isDisposed;
		static List<CodeLensProvider> codeLensProviders = new List<CodeLensProvider> ();

		static CodeLensTextEditorExtension ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/CodeLensProvider", delegate (object sender, ExtensionNodeEventArgs args) {
				var node = (MimeTypeExtensionNode)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					var matcher = (CodeLensProvider)node.CreateInstance ();
					matcher.MimeType = node.MimeType;
					codeLensProviders.Add (matcher);
					break;
				case ExtensionChange.Remove:
					var toRemove = codeLensProviders.FirstOrDefault (m => m.MimeType == node.MimeType);
					if (toRemove != null)
						codeLensProviders.Remove (toRemove);
					break;
				}
			});
		}

		protected override void Initialize ()
		{
			DocumentContext.DocumentParsed += DocumentContext_DocumentParsed;
		}

		public override void Dispose ()
		{
			if (isDisposed)
				return;
			isDisposed = true;
			CancelDocumentParsedUpdate ();
			DocumentContext.DocumentParsed -= DocumentContext_DocumentParsed;
			base.Dispose ();
		}

		void CancelDocumentParsedUpdate ()
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
		}

		void DocumentContext_DocumentParsed (object sender, EventArgs e)
		{
			CancelDocumentParsedUpdate ();
			var token = src.Token;
			var parsedDocument = DocumentContext.ParsedDocument;
			if (!isDisposed)
				UpdateLenses (token);
		}

		List<ICodeLensMarker> codeLensMarkers = new List<ICodeLensMarker> ();
		struct CodeLensCacheItem
		{
			public ICodeLensMarker Marker { get; set; }
			public CodeLens Lens { get; set; }

			public CodeLensCacheItem (ICodeLensMarker marker, CodeLens lens)
			{
				Marker = marker;
				Lens = lens;
			}
		}

		Dictionary<CodeLensProvider, List<CodeLensCacheItem>> lensCache = new Dictionary<CodeLensProvider, List<CodeLensCacheItem>> ();

		async void UpdateLenses (CancellationToken token = default (CancellationToken))
		{
			foreach (var provider in codeLensProviders) {
				var lenses = await provider.GetLenses (Editor, DocumentContext, token).ConfigureAwait (false);
				await Runtime.RunInMainThread (delegate {
					if (lensCache.TryGetValue (provider, out List<CodeLensCacheItem> cache)) {
						lensCache.Remove (provider);
						foreach (var item in cache)
							item.Marker.RemoveLens (item.Lens);
					}

					var newCache = new List<CodeLensCacheItem> ();
					foreach (var lens in lenses) {
						var line = Editor.GetLineByOffset (lens.CodeLensSpan.Offset);
						var codeLensMarker = Editor.GetLineMarkers (line).OfType<ICodeLensMarker> ().FirstOrDefault ();
						if (codeLensMarker == null) {
							codeLensMarker = Editor.TextMarkerFactory.CreateCodeLensMarker (Editor);
							Editor.AddMarker (line, codeLensMarker);
							codeLensMarkers.Add (codeLensMarker);
						}
						codeLensMarker.AddLens (lens);
						newCache.Add (new CodeLensCacheItem (codeLensMarker, lens));
					}
					lensCache.Add (provider, newCache);
				});
			}

			await Runtime.RunInMainThread (delegate {
				RemoveUnusedCodeLensMarkers ();
			});
		}

		void RemoveUnusedCodeLensMarkers ()
		{
			foreach (var marker in codeLensMarkers.ToArray ()) {
				if (marker.CodeLensCount == 0) {
					Editor.RemoveMarker (marker);
					codeLensMarkers.Remove (marker);
				}
			}
		}
	}
}