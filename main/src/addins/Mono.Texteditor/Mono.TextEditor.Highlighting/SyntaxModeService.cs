// SyntaxModeService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Xml;

namespace Mono.TextEditor.Highlighting
{
	public static class SyntaxModeService
	{
		static Dictionary<string, SyntaxMode> syntaxModes = new Dictionary<string, SyntaxMode> ();
		static Dictionary<string, Style>      styles      = new Dictionary<string, Style> ();
		static Dictionary<string, string> syntaxModeLookup = new Dictionary<string, string> ();
		static Dictionary<string, string> styleLookup      = new Dictionary<string, string> ();
		
		public static string[] Styles {
			get {
				List<string> result = new List<string> ();
				foreach (string style in styles.Keys) {
					if (!result.Contains (style))
						result.Add (style);
				}
				foreach (string style in styleLookup.Keys) {
					if (!result.Contains (style))
						result.Add (style);
				}
				return result.ToArray ();
			}
		}
		
		public static Style GetColorStyle (Gtk.Widget widget, string name) 
		{
			if (styles.ContainsKey (name))
				return styles [name];
			if (styleLookup.ContainsKey (name)) {
				LoadStyle (name);
				return GetColorStyle (widget, name);		
			}
			return new DefaultStyle (widget);
		}
		
		static void LoadStyle (string name)
		{
			XmlTextReader reader = new XmlTextReader (typeof (SyntaxModeService).Assembly.GetManifestResourceStream (styleLookup [name]));
			styles [name] = Style.Read (reader);
			reader.Close ();
		}
		
		static void LoadSyntaxMode (string mimeType)
		{
			XmlTextReader reader = new XmlTextReader (typeof (SyntaxModeService).Assembly.GetManifestResourceStream (syntaxModeLookup [mimeType]));
			SyntaxMode mode = SyntaxMode.Read (reader);
			foreach (string mime in mode.MimeType.Split (';')) {
				syntaxModes [mime] = mode;
			}
			reader.Close ();
		}
					
		public static SyntaxMode GetSyntaxMode (string mimeType)
		{
			if (syntaxModes.ContainsKey (mimeType))
				return syntaxModes [mimeType];
			if (syntaxModeLookup.ContainsKey (mimeType)) {
				LoadSyntaxMode (mimeType);
				syntaxModeLookup.Remove (mimeType);
				return GetSyntaxMode (mimeType);
			}
			return null;
		}
		
		public static void ScanSpans (Document doc, Rule rule, Stack<Span> spanStack, int start, int end)
		{
			Dictionary<char, List<Span>> spanTree = rule.spanStarts;
			int endOffset = 0;
			end = System.Math.Min (end, doc.Length);
			Span curSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
			Rule curRule = rule;
			for (int offset = start; offset < end; offset++) {
				char ch = doc.GetCharAt (offset);
				if (curSpan != null && !String.IsNullOrEmpty (curSpan.End)) {
					if (curSpan.Escape == ch && offset + 1 < end && endOffset == 0 && doc.GetCharAt (offset + 1) == curSpan.End[0]) {
						offset++;
						continue;
					} else	if (curSpan.End[endOffset] == ch) {
						endOffset++;
						if (endOffset >= curSpan.End.Length) {
							spanStack.Pop ();
							curSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
							curRule = curSpan != null ? doc.SyntaxMode.GetRule (curSpan.Rule) : rule;
							spanTree = curRule != null ? curRule.spanStarts : null;
							endOffset = 0;
							continue;
						}
					} else if (endOffset != 0) {
						endOffset = 0;
						if (curSpan.End[endOffset] == ch) {
							offset--;
							continue;
						}
					}
					if (String.IsNullOrEmpty (curSpan.Rule))
						continue;
				}
				
				if (spanTree != null && spanTree.ContainsKey (ch)) {
					bool found = false;
					foreach (Span span in spanTree[ch]) {
						bool mismatch = false;
						for (int j = 1; j < span.Begin.Length; j++) {
							if (offset + j >= doc.Length || span.Begin [j] != doc.GetCharAt (offset + j)) {
								mismatch = true;
								break;
							}
						}
						
						if (!mismatch) {
							spanStack.Push (span);
							curSpan = span;
							curRule = doc.SyntaxMode.GetRule (curSpan.Rule);
							spanTree = curRule != null ? curRule.spanStarts : null;
							found = true; 
							offset += span.Begin.Length - 1;
							break;
						}
					}
					if (found) 
						continue;
				} else {
					spanTree = curRule != null ? curRule.spanStarts : null;
				}
			 skip:
					;
			}
		}
		static bool IsEqual (Span[] spans1, Span[] spans2)
		{
			if (spans1 == null || spans1.Length == 0) 
				return spans2 == null || spans2.Length == 0;
			if (spans2 == null || spans1.Length != spans2.Length)
				return false;
			for (int i = 0; i < spans1.Length; i++) {
				if (spans1[i] != spans2[i]) {
					return false;
				}
			}
			return true;
		}
		
		class UpdateWorkerThread : WorkerThread
		{
			Document doc;
			SyntaxMode mode;
			int startOffset;
			int endOffset;
			
			public UpdateWorkerThread (Document doc, SyntaxMode mode, int startOffset, int endOffset)
			{
				this.doc         = doc;
				this.mode        = mode;
				this.startOffset = startOffset;
				this.endOffset   = endOffset;
			}
			
			protected void ScanSpansThreaded (Document doc, Rule rule, Stack<Span> spanStack, int start, int end)
			{
				Dictionary<char, List<Span>> spanTree = rule.spanStarts;
				int endOffset = 0;
				end = System.Math.Min (end, doc.Length);
				Span curSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
				Rule curRule = rule;
				for (int offset = start; offset < end; offset++) {
					if (IsStopping)
						return;
					char ch = doc.GetCharAt (offset);
					if (curSpan != null && !String.IsNullOrEmpty (curSpan.End)) {
						if (curSpan.Escape == ch && offset + 1 < end && endOffset == 0 && doc.GetCharAt (offset + 1) == curSpan.End[0]) {
							offset++;
							continue;
						} else	if (curSpan.End[endOffset] == ch) {
							endOffset++;
							if (endOffset >= curSpan.End.Length) {
								spanStack.Pop ();
								curSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
								curRule = curSpan != null ? doc.SyntaxMode.GetRule (curSpan.Rule) : rule;
								spanTree = curRule != null ? curRule.spanStarts : null;
								endOffset = 0;
								continue;
							}
						} else if (endOffset != 0) {
							endOffset = 0;
							if (curSpan.End[endOffset] == ch) {
								offset--;
								continue;
							}
						}
						if (String.IsNullOrEmpty (curSpan.Rule))
							continue;
						
					}
					if (spanTree != null && spanTree.ContainsKey (ch)) {
						bool found = false;
						foreach (Span span in spanTree[ch]) {
							bool mismatch = false;
							for (int j = 1; j < span.Begin.Length; j++) {
								if (offset + j >= doc.Length || span.Begin [j] != doc.GetCharAt (offset + j)) {
									mismatch = true;
									break;
								}
							}
							
							if (!mismatch) {
								spanStack.Push (span);
								curSpan = span;
								curRule = doc.SyntaxMode.GetRule (curSpan.Rule);
								spanTree = curRule != null ? curRule.spanStarts : null;
								offset += span.Begin.Length - 1;

								found = true; 
								break;
							}
						}
						if (found) 
							continue;
					} else {
						spanTree = curRule != null ? curRule.spanStarts : null;
					}
				 skip:
						;
				}
			}
			
			protected override void InnerRun ()
			{
				bool doUpdate = false;
				RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = doc.GetLineByOffset (startOffset).Iter;
				Stack<Span> spanStack = iter.Current.StartSpan != null ? new Stack<Span> (iter.Current.StartSpan) : new Stack<Span> ();
				
				do {
					LineSegment line = iter.Current;
					if (line == null || line.Offset < 0)
						break;
					
					Span[] newSpans = spanStack.ToArray ();
					if (line.Offset > endOffset) {
						bool equal = IsEqual (line.StartSpan, newSpans);
						doUpdate |= !equal;
						if (equal) 
							break;
					}
					line.StartSpan = newSpans.Length > 0 ? newSpans : null;
					Rule rule = mode;
					if (spanStack.Count > 0 && !String.IsNullOrEmpty (spanStack.Peek ().Rule))
						rule = mode.GetRule (spanStack.Peek ().Rule) ?? mode;
					
					ScanSpansThreaded (doc, rule, spanStack, line.Offset, line.EndOffset);
					while (spanStack.Count > 0 && spanStack.Peek ().StopAtEol)
						spanStack.Pop ();
				} while (!IsStopping && iter.MoveNext ());
				if (doUpdate) {
					GLib.Timeout.Add (0, delegate {
						doc.RequestUpdate (new UpdateAll ());
						doc.CommitDocumentUpdate ();
						return false;
					});
				}
				base.Stop ();
			}
		}
		
//		static bool updateIsRunning = false;
//		static void Update (object o)
//		{
//			updateIsRunning = false;
//		}
		
		static readonly object syncObject = new object();
		static UpdateWorkerThread updateThread = null;
		
		public static void WaitForUpdate ()
		{
			lock (syncObject) {
				if (updateThread != null)
					updateThread.WaitForFinish ();
			}
		}
		
		public static void StartUpdate (Document doc, SyntaxMode mode, int startOffset, int endOffset)
		{
			lock (syncObject) {
				if (updateThread != null) 
					updateThread.Stop ();
				
				updateThread = new UpdateWorkerThread (doc, mode, startOffset, endOffset);
				updateThread.Start ();
			}
		}
		
		static string Scan (XmlTextReader reader, string attribute)
		{
			while (reader.Read () && !reader.IsStartElement ()) 
				;
			return reader.GetAttribute (attribute);
		}
		
		static SyntaxModeService ()
		{
			Assembly thisAssembly = typeof (SyntaxModeService).Assembly;
			foreach (string resource in thisAssembly.GetManifestResourceNames ()) {
				if (!resource.EndsWith (".xml")) 
					continue;
				XmlTextReader reader =  new XmlTextReader (thisAssembly.GetManifestResourceStream (resource));
				if (resource.EndsWith ("SyntaxMode.xml")) {
					string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
					foreach (string mimeType in mimeTypes.Split (';')) {
						syntaxModeLookup [mimeType] = resource;
					}
				} else if (resource.EndsWith ("Style.xml")) {
					string styleName = Scan (reader, Style.NameAttribute);
					styleLookup [styleName] = resource;
				}
				reader.Close ();
			}
			SyntaxModeService.GetSyntaxMode ("text/x-csharp").AddSemanticRule ("Comment", new HighlightUrlSemanticRule ("comment"));
			SyntaxModeService.GetSyntaxMode ("text/x-csharp").AddSemanticRule ("XmlDocumentation", new HighlightUrlSemanticRule ("comment"));
			SyntaxModeService.GetSyntaxMode ("text/x-csharp").AddSemanticRule ("String", new HighlightUrlSemanticRule ("literal"));
		}
	}
}