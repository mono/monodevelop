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
		static List<SyntaxMode> syntaxModes = new List<SyntaxMode> ();
		static List<Style>      styles      = new List<Style> ();
		
		public static ReadOnlyCollection<SyntaxMode> SyntaxModes {
			get {
				return syntaxModes.AsReadOnly ();
			}
		}
		public static ReadOnlyCollection<Style> Styles {
			get {
				return styles.AsReadOnly ();
			}
		}
		
		public static SyntaxMode GetSyntaxMode (string mimeType)
		{
			foreach (SyntaxMode mode in syntaxModes) {
				if (mode.MimeType == mimeType)
					return mode;
			}
			return null;
		}
		
		public static void ScanSpans (Document doc, Rule rule, Stack<Span> spanStack, int start, int end)
		{
			Dictionary<char, Rule.Pair<Span, object>> tree = rule.spanTree;
			Rule.Pair<Span, object> pair = null;
			int endOffset = 0;
			end = System.Math.Min (end, doc.Buffer.Length);
			for (int offset = start; offset < end; offset++) {
				char ch = doc.Buffer.GetCharAt (offset);
				Span curSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
				if (curSpan != null && !String.IsNullOrEmpty (curSpan.End)) {
					if (curSpan.End[endOffset] == ch) {
						endOffset++;
						if (endOffset >= curSpan.End.Length) {
							spanStack.Pop ();
						}
						continue;
					}
				}
				endOffset = 0;
				if (!tree.ContainsKey (ch)) {
					tree = rule.spanTree;
					if (pair != null && pair.o1 != null) 
						spanStack.Push (pair.o1);
					pair      = null;
					endOffset = 0;
					continue;
				}
				pair = tree[ch];
				tree = (Dictionary<char, Rule.Pair<Span, object>>)pair.o2;
			}
			if (pair != null && pair.o1 != null)
				spanStack.Push (pair.o1);			
		}
		
		static void Update (object o)
		{
			object[] data   = (object[])o;
			Document doc    = (Document)data[0];
			SyntaxMode mode = (SyntaxMode)data[1];
			int startOffset = (int)data[2];
			int endOffset   = (int)data[3];
			bool foundEndLine = false;
			RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = doc.Splitter.GetByOffset (startOffset).Iter;
			LineSegment endLine = doc.Splitter.GetByOffset (endOffset);
			Stack<Span> spanStack = iter.Current.StartSpan != null ? new Stack<Span> (iter.Current.StartSpan) : new Stack<Span> ();
			do {
				LineSegment line = iter.Current;
				int         lineOffset = line.Offset;
				if (lineOffset < 0)
					break;
				Span[] newSpans = spanStack.ToArray ();
				if (foundEndLine) {
					if (line.StartSpan == null || newSpans.Length == line.StartSpan.Length) {
						bool equal = false;
						if (line.StartSpan != null) {
							equal = true;
							for (int i = 0; i < newSpans.Length; i++) {
								if (line.StartSpan[i] != newSpans[i]) {
									equal = false;
									break;
								}
							}
						}
						if (equal)
							break;
					}
				}
				line.StartSpan = newSpans.Length > 0 ? newSpans : null;
				Rule rule = mode;
				if (spanStack.Count > 0 && !String.IsNullOrEmpty (spanStack.Peek ().Rule)) {
					rule = mode.GetRule (spanStack.Peek ().Rule) ?? mode;
				}
				ScanSpans (doc, rule, spanStack, lineOffset, lineOffset + line.EditableLength);
				while (spanStack.Count > 0 && spanStack.Peek ().StopAtEol)
					spanStack.Pop ();
				if (line == endLine) {
					foundEndLine = true;
				}
			} while (iter.MoveNext ());
		}
		static Thread updateThread = null;
		public static void StartUpdate (Document doc, SyntaxMode mode, int startOffset, int endOffset)
		{
			if (updateThread != null && updateThread.IsAlive)
				updateThread.Abort ();
			updateThread = new Thread (new ParameterizedThreadStart (Update));
			updateThread.Priority = ThreadPriority.Lowest;
			updateThread.IsBackground = true;
			updateThread.Start (new object[] {doc, mode, startOffset, endOffset});
		}
		
		static SyntaxModeService ()
		{
			Assembly thisAssembly = typeof (SyntaxModeService).Assembly;
			foreach (string resource in thisAssembly.GetManifestResourceNames ()) {
				if (resource.EndsWith ("SyntaxMode.xml"))
					syntaxModes.Add (SyntaxMode.Read (new XmlTextReader(thisAssembly.GetManifestResourceStream (resource))));
				else if (resource.EndsWith ("Style.xml")) 
					styles.Add (Style.Read (new XmlTextReader(thisAssembly.GetManifestResourceStream (resource))) );
			}
		}
	}
}
