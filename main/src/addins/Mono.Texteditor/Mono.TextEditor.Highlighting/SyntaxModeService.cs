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
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;

namespace Mono.TextEditor.Highlighting
{
	public static class SyntaxModeService
	{
		static Dictionary<string, SyntaxMode> syntaxModes = new Dictionary<string, SyntaxMode> ();
		static Dictionary<string, Style>      styles      = new Dictionary<string, Style> ();
		static Dictionary<string, IXmlProvider> syntaxModeLookup = new Dictionary<string, IXmlProvider> ();
		static Dictionary<string, IXmlProvider> styleLookup      = new Dictionary<string, IXmlProvider> ();
		
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
		
		public static IXmlProvider GetProvider (SyntaxMode mode)
		{
			foreach (string mimeType in mode.MimeType.Split (';')) {
				if (syntaxModeLookup.ContainsKey (mimeType)) 
					return syntaxModeLookup[mimeType];
			}
			return null;
		}
		
		public static IXmlProvider GetProvider (Style style)
		{
			if (styleLookup.ContainsKey (style.Name)) 
				return styleLookup[style.Name];
			return null;
		}
		
		static void LoadStyle (string name)
		{
			if (!styleLookup.ContainsKey (name))
				throw new System.ArgumentException ("Style " + name + " not found", "name");
			XmlTextReader reader = styleLookup [name].Open ();
			try {
				styles [name] = Style.LoadFrom (reader);
			} finally {
				reader.Close ();
			}
		}
		
		static void LoadSyntaxMode (string mimeType)
		{
			if (!syntaxModeLookup.ContainsKey (mimeType))
				throw new System.ArgumentException ("Syntax mode for mime:" + mimeType + " not found", "mimeType");
			XmlTextReader reader = syntaxModeLookup [mimeType].Open ();
			try {
				SyntaxMode mode = SyntaxMode.Read (reader);
				foreach (string mime in mode.MimeType.Split (';')) {
					syntaxModes [mime] = mode;
				}
			} finally {
				reader.Close ();
			}
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
		
		public static bool ValidateAllSyntaxModes ()
		{
			foreach (string mime in new List<string> (syntaxModeLookup.Keys)) {
				GetSyntaxMode (mime);
			}
			syntaxModeLookup.Clear ();
			foreach (string style in new List<string> (styleLookup.Keys)) {
				GetColorStyle (null, style);
			}
			styleLookup.Clear ();
			bool result = true;
			foreach (KeyValuePair<string, Style> style in styles) {
				HashSet<SyntaxMode> checkedModes = new HashSet<SyntaxMode> ();
				foreach (KeyValuePair<string, SyntaxMode> mode in syntaxModes) {
					if (checkedModes.Contains (mode.Value))
						continue;
					if (!mode.Value.Validate (style.Value)) {
						System.Console.WriteLine(mode.Key + " failed to validate against:" + style.Key);
						result = false;
					}
					checkedModes.Add (mode.Value);
				}
			}
			return result;
		}
		
		public static void Remove (Style style)
		{
			if (styles.ContainsKey (style.Name))
				styles.Remove (style.Name);
			if (styleLookup.ContainsKey (style.Name))
				styleLookup.Remove (style.Name);
		}
		public static void Remove (SyntaxMode mode)
		{
			foreach (string mimeType in mode.MimeType.Split (';')) {
				if (syntaxModes.ContainsKey (mimeType)) 
					syntaxModes.Remove (mimeType);
				if (syntaxModeLookup.ContainsKey (mimeType)) 
					syntaxModeLookup.Remove (mimeType);
			}
		}
		
		public static void ScanSpans (Document doc, Rule rule, Stack<Span> spanStack, int start, int end)
		{
			Dictionary<char, Span[]> spanTree = rule != null ? rule.spanStarts : null;
			int endOffset = 0;
			end = System.Math.Min (end, doc.Length);
			Span curSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
			Rule curRule = rule;
			for (int offset = start; offset < end; offset++) {
				char ch = doc.GetCharAt (offset);
				if (curSpan != null && !String.IsNullOrEmpty (curSpan.End)) {
					if (!String.IsNullOrEmpty (curSpan.Escape) && offset + 1 < end && endOffset == 0 && doc.GetCharAt (offset + 1) == curSpan.End[0]) {
						bool match = true;
						for (int j = 0; j < curSpan.Escape.Length && offset + j < doc.Length; j++) {
							if (doc.GetCharAt (offset + j) != curSpan.Escape[j]) {
								match = false;
								break;
							}
						}
						if (match) {
							offset += curSpan.Escape.Length - 1;
							continue;
						}
							
						
					} 
					if (curSpan.End[endOffset] == ch) {
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
						if (!mismatch && span.BeginFlags.Contains ("firstNonWs")) {
							LineSegment line = doc.GetLineByOffset (offset);
							for (int k = line.Offset; k < offset; k++) {
								if (!Char.IsWhiteSpace (doc.GetCharAt (k))) {
									mismatch = true;
									break;
								}
							}
						}
						
						if (!mismatch) {
							spanStack.Push (span);
							curSpan = span;
							curRule = doc.SyntaxMode.GetRule (curSpan.Rule);
							spanTree = curRule != null ? curRule.spanStarts : null;
							found = true; 
							offset += span.Begin.Length - 1;
							endOffset = 0;
							break;
						}
					}
					if (found) 
						continue;
				} else {
					spanTree = curRule != null ? curRule.spanStarts : null;
				}
//			 skip:
//					;
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
		
		static Queue<UpdateWorker> updateQueue = new Queue<UpdateWorker> ();
		
		class UpdateWorker
		{
			Document doc;
			SyntaxMode mode;
			int startOffset;
			int endOffset;
			
			public UpdateWorker (Document doc, SyntaxMode mode, int startOffset, int endOffset)
			{
				this.doc         = doc;
				this.mode        = mode;
				this.startOffset = startOffset;
				this.endOffset   = endOffset;
			}
			
			protected void ScanSpansThreaded (Document doc, Rule rule, Stack<Span> spanStack, int start, int end)
			{
				Dictionary<char, Span[]> spanTree = rule != null ? rule.spanStarts : null;
				int endOffset = 0;
				end = System.Math.Min (end, doc.Length);
				Span curSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
				Rule curRule = rule;
				for (int offset = start; offset < end; offset++) {
					char ch;
					try {
						// document may have been changed and the thread is still running
						// (however a new highlighting thread will be created after each document change)
						ch = doc.GetCharAt (offset);
					} catch (Exception) {
						return;
					}
					if (curSpan != null && !String.IsNullOrEmpty (curSpan.End)) {
						if (!String.IsNullOrEmpty (curSpan.Escape) && offset + 1 < end && endOffset == 0 && doc.GetCharAt (offset + 1) == curSpan.End[0]) {
							bool match = true;
							for (int j = 0; j < curSpan.Escape.Length && offset + j < doc.Length; j++) {
								if (doc.GetCharAt (offset + j) != curSpan.Escape[j]) {
									match = false;
									break;
								}
							}
							
							if (match) {
								offset += curSpan.Escape.Length - 1;
								continue;
							}
							
							
						}
						if (curSpan.End[endOffset] == ch) {
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
							if (!mismatch && span.BeginFlags.Contains ("firstNonWs")) {
								LineSegment line = doc.GetLineByOffset (offset);
								for (int k = line.Offset; k < offset; k++) {
									if (!Char.IsWhiteSpace (doc.GetCharAt (k))) {
										mismatch = true;
										break;
									}
								}
							}
							
							if (!mismatch) {
								spanStack.Push (span);
								curSpan = span;
								curRule = doc.SyntaxMode.GetRule (curSpan.Rule);
								spanTree = curRule != null ? curRule.spanStarts : null;
								offset += span.Begin.Length - 1;
								endOffset = 0;
								
								found = true; 
								break;
							}
						}
						if (found) 
							continue;
					} else {
						spanTree = curRule != null ? curRule.spanStarts : null;
					}
//				 skip:
//						;
				}
			}
			
			public void InnerRun ()
			{
				bool doUpdate = false;
				RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = doc.GetLineByOffset (startOffset).Iter;
				Stack<Span> spanStack = iter.Current.StartSpan != null ? new Stack<Span> (iter.Current.StartSpan) : new Stack<Span> ();
				
				do {
					LineSegment line = iter.Current;
					if (line == null || line.Offset < 0)
						break;
					
					List<Span> spanList = new List<Span> (spanStack.ToArray ());
					spanList.Reverse ();
					for (int i = 0; i < spanList.Count; i++) {
						if (spanList[i].StopAtEol) {
							spanList.RemoveAt (i);
							i--;
						}
					}
					Span[] newSpans = spanList.ToArray ();
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
				} while (iter.MoveNext ());
				if (doUpdate) {
					GLib.Timeout.Add (0, delegate {
						doc.RequestUpdate (new UpdateAll ());
						doc.CommitDocumentUpdate ();
						return false;
					});
				}
			}
		}
		
//		static bool updateIsRunning = false;
//		static void Update (object o)
//		{
//			updateIsRunning = false;
//		}
		
	//	static readonly object syncObject  = new object();
		static Thread         updateThread = null;
		static AutoResetEvent queueSignal  = new AutoResetEvent (false);
		
		static void StartUpdateThread ()
		{
			updateThread = new Thread (ProcessQueue);
			updateThread.IsBackground = true;
			updateThread.Start();
		}
		
		static void ProcessQueue ()
		{
			while (true) {
				while (updateQueue.Count > 0) {
					UpdateWorker worker = null;
					lock (updateQueue) {
						worker = updateQueue.Dequeue ();
					}
					worker.InnerRun ();
				}
				queueSignal.WaitOne ();
			}
		}
		
		public static void StartUpdate (Document doc, SyntaxMode mode, int startOffset, int endOffset)
		{
			lock (updateQueue) {
				updateQueue.Enqueue (new UpdateWorker (doc, mode, startOffset, endOffset));
			}
			queueSignal.Set ();
		}
		
		static string Scan (XmlTextReader reader, string attribute)
		{
			while (reader.Read () && !reader.IsStartElement ()) 
				;
			return reader.GetAttribute (attribute);
		}
		
		public static bool IsValidStyle (string fileName)
		{
			if (!fileName.EndsWith ("Style.xml"))
				return false;
			try {
				using (XmlTextReader reader =  new XmlTextReader (fileName)) {
					string styleName = Scan (reader, Style.NameAttribute);
					return !String.IsNullOrEmpty (styleName);
				}
			} catch (Exception) {
				return false;
			}
		}
		
		public static bool IsValidSyntaxMode (string fileName)
		{
			if (!fileName.EndsWith ("SyntaxMode.xml"))
				return false;
			try {
				using (XmlTextReader reader =  new XmlTextReader (fileName)) {
					string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
					return !String.IsNullOrEmpty (mimeTypes);
				}
			} catch (Exception) {
				return false;
			}
		}
		
		public static void LoadStylesAndModes (string path)
		{
			foreach (string file in Directory.GetFiles (path)) {
				if (!file.EndsWith (".xml")) 
					continue;
				if (file.EndsWith ("SyntaxMode.xml")) {
					XmlTextReader reader =  new XmlTextReader (file);
					string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
					foreach (string mimeType in mimeTypes.Split (';')) {
						syntaxModeLookup [mimeType] = new UrlXmlProvider (file);
					}
					reader.Close ();
				} else if (file.EndsWith ("Style.xml")) {
					XmlTextReader reader =  new XmlTextReader (file);
					string styleName = Scan (reader, Style.NameAttribute);
					styleLookup [styleName] = new UrlXmlProvider (file);
					reader.Close ();
				}
			}
		}
		public static void LoadStylesAndModes (Assembly assembly)
		{
			foreach (string resource in assembly.GetManifestResourceNames ()) {
				if (!resource.EndsWith (".xml")) 
					continue;
				if (resource.EndsWith ("SyntaxMode.xml")) {
					XmlTextReader reader =  new XmlTextReader (assembly.GetManifestResourceStream (resource));
					string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
					ResourceXmlProvider provider = new ResourceXmlProvider (assembly, resource);
					foreach (string mimeType in mimeTypes.Split (';')) {
						syntaxModeLookup [mimeType] = provider;
					}
					reader.Close ();
				} else if (resource.EndsWith ("Style.xml")) {
					XmlTextReader reader = new XmlTextReader (assembly.GetManifestResourceStream (resource));
					string styleName = Scan (reader, Style.NameAttribute);
					styleLookup [styleName] = new ResourceXmlProvider (assembly, resource);
					reader.Close ();
				}
			}
		}

		public static void AddSyntaxMode (IXmlProvider provider)
		{
			using (XmlTextReader reader = provider.Open ()) {
				string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
				foreach (string mimeType in mimeTypes.Split (';')) {
					syntaxModeLookup [mimeType] = provider;
				}
			}
		}
		
		public static void RemoveSyntaxMode (IXmlProvider provider)
		{
			using (XmlTextReader reader = provider.Open ()) {
				string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
				foreach (string mimeType in mimeTypes.Split (';')) {
					syntaxModeLookup.Remove (mimeType);
				}
			}
		}
		
		public static void AddStyle (IXmlProvider provider)
		{
			using (XmlTextReader reader = provider.Open ()) {
				string styleName = Scan (reader, Style.NameAttribute);
				styleLookup [styleName] = provider;
			}
		}
		public static void RemoveStyle (IXmlProvider provider)
		{
			using (XmlTextReader reader = provider.Open ()) {
				string styleName = Scan (reader, Style.NameAttribute);
				styleLookup.Remove (styleName);
			}
		}
		
		static SyntaxModeService ()
		{
			StartUpdateThread ();
			LoadStylesAndModes (typeof (SyntaxModeService).Assembly);
			SyntaxModeService.GetSyntaxMode ("text/x-csharp").AddSemanticRule ("Comment", new HighlightUrlSemanticRule ("comment"));
			SyntaxModeService.GetSyntaxMode ("text/x-csharp").AddSemanticRule ("XmlDocumentation", new HighlightUrlSemanticRule ("comment"));
			SyntaxModeService.GetSyntaxMode ("text/x-csharp").AddSemanticRule ("String", new HighlightUrlSemanticRule ("string"));
		}

	}
}