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
using System.Xml.Schema;
using System.Linq;

namespace Mono.TextEditor.Highlighting
{
	public static class SyntaxModeService
	{
		static Dictionary<string, ISyntaxModeProvider> syntaxModes = new Dictionary<string, ISyntaxModeProvider> ();
		static Dictionary<string, ColorScheme> styles      = new Dictionary<string, ColorScheme> ();
		static Dictionary<string, IXmlProvider> syntaxModeLookup = new Dictionary<string, IXmlProvider> ();
		static Dictionary<string, IXmlProvider> styleLookup      = new Dictionary<string, IXmlProvider> ();
		static Dictionary<string, string> isLoadedFromFile = new Dictionary<string, string> ();
		
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
		
		public static string GetFileNameForStyle (ColorScheme style)
		{
			string result;
			if (!isLoadedFromFile.TryGetValue (style.Name, out result))
				return null;
			return result;
		}
		
		public static void InstallSyntaxMode (string mimeType, ISyntaxModeProvider modeProvider)
		{
			if (syntaxModeLookup.ContainsKey (mimeType))
				syntaxModeLookup.Remove (mimeType);
			syntaxModes[mimeType] = modeProvider;
		}
		
		public static ColorScheme GetColorStyle (Gtk.Style widgetStyle, string name)
		{
			if (styles.ContainsKey (name))
				return styles [name];
			if (styleLookup.ContainsKey (name)) {
				LoadStyle (name);
				return GetColorStyle (widgetStyle, name);
			}
			return GetColorStyle (widgetStyle, "Default");
		}
		
		public static IXmlProvider GetProvider (SyntaxMode mode)
		{
			foreach (string mimeType in mode.MimeType.Split (';')) {
				if (syntaxModeLookup.ContainsKey (mimeType)) 
					return syntaxModeLookup[mimeType];
			}
			return null;
		}
		
		public static IXmlProvider GetProvider (ColorScheme style)
		{
			if (styleLookup.ContainsKey (style.Name)) 
				return styleLookup[style.Name];
			return null;
		}
		
		static void LoadStyle (string name)
		{
			if (!styleLookup.ContainsKey (name))
				throw new System.ArgumentException ("Style " + name + " not found", "name");
			XmlReader reader = styleLookup [name].Open ();
			try {
				styles [name] = ColorScheme.LoadFrom (reader);
			} catch (Exception e) {
				throw new IOException ("Error while loading style :" + name, e);
			} finally {
				reader.Close ();
			}
		}
		
		static void LoadSyntaxMode (string mimeType)
		{
			if (!syntaxModeLookup.ContainsKey (mimeType))
				throw new System.ArgumentException ("Syntax mode for mime:" + mimeType + " not found", "mimeType");
			XmlReader reader = syntaxModeLookup [mimeType].Open ();
			try {
				var mode = SyntaxMode.Read (reader);
				foreach (string mime in mode.MimeType.Split (';')) {
					syntaxModes [mime] = new ProtoTypeSyntaxModeProvider (mode);
				}
			} catch (Exception e) {
				throw new IOException ("Error while syntax mode for mime:" + mimeType, e);
			} finally {
				reader.Close ();
			}
		}
		
		public static SyntaxMode GetSyntaxMode (TextDocument doc)
		{
			return GetSyntaxMode (doc, doc.MimeType);
		}
		
		public static SyntaxMode GetSyntaxMode (TextDocument doc, string mimeType)
		{
			if (string.IsNullOrEmpty (mimeType))
				return null;
			SyntaxMode result = null;
			if (syntaxModes.ContainsKey (mimeType)) {
				result = syntaxModes [mimeType].Create (doc);
			} else if (syntaxModeLookup.ContainsKey (mimeType)) {
				LoadSyntaxMode (mimeType);
				syntaxModeLookup.Remove (mimeType);
				result = GetSyntaxMode (doc, mimeType);
			}
			if (result != null) {
				foreach (var rule in semanticRules.Where (r => r.Item1 == mimeType)) {
					result.AddSemanticRule (rule.Item2, rule.Item3);
				}
			}
			return result;
		}
		
		public static bool ValidateAllSyntaxModes ()
		{
			var doc = new TextDocument ();
			foreach (string mime in new List<string> (syntaxModeLookup.Keys)) {
				GetSyntaxMode (doc, mime);
			}
			syntaxModeLookup.Clear ();
			foreach (string style in new List<string> (styleLookup.Keys)) {
				GetColorStyle (null, style);
			}
			styleLookup.Clear ();
			bool result = true;
			foreach (KeyValuePair<string, ColorScheme> style in styles) {
				var checkedModes = new HashSet<ISyntaxModeProvider> ();
				foreach (var mode in syntaxModes) {
					if (checkedModes.Contains (mode.Value))
						continue;
					if (!mode.Value.Create (doc).Validate (style.Value)) {
						System.Console.WriteLine (mode.Key + " failed to validate against:" + style.Key);
						result = false;
					}
					checkedModes.Add (mode.Value);
				}
			}
			return result;
		}
		
		public static void Remove (ColorScheme style)
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
		
		public static void ScanSpans (TextDocument doc, SyntaxMode mode, Rule rule, CloneableStack<Span> spanStack, int start, int end)
		{
			if (mode == null)
				return;
			SyntaxMode.SpanParser parser = mode.CreateSpanParser (null, spanStack);
			parser.ParseSpans (start, end - start);
		}
		
		static Queue<UpdateWorker> updateQueue = new Queue<UpdateWorker> ();
		
		class UpdateWorker
		{
			TextDocument doc;
			SyntaxMode mode;
			int startOffset;
			int endOffset;
			
			public ManualResetEvent ManualResetEvent {
				get;
				private set;
			}
			
			public TextDocument Doc {
				get { return this.doc; }
			}
			
			public bool IsFinished {
				get;
				set;
			}
			public UpdateWorker (TextDocument doc, SyntaxMode mode, int startOffset, int endOffset)
			{
				this.doc = doc;
				this.mode = mode;
				this.startOffset = startOffset;
				this.endOffset = endOffset;
				IsFinished = false;
				ManualResetEvent = new ManualResetEvent (false);
			}
			
			
			bool EndsWithContinuation (Span span, DocumentLine line)
			{
				return !span.StopAtEol || span.StopAtEol && !string.IsNullOrEmpty (span.Continuation) &&
					line != null && doc.GetTextAt (line).Trim ().EndsWith (span.Continuation);
			}
			
			public void InnerRun ()
			{
				bool doUpdate = false;
				int startLine = doc.OffsetToLineNumber (startOffset);
				if (startLine < 0)
					return;
				try {
					var lineSegment = doc.GetLine (startLine);
					if (lineSegment == null)
						return;
					var span = lineSegment.StartSpan;
					if (span == null)
						return;
					var spanStack = span.Clone ();
					SyntaxMode.SpanParser parser = mode.CreateSpanParser(null, spanStack);
					foreach (var line in doc.GetLinesStartingAt (startLine)) {
						if (line == null)
							return;
						if (line.Offset > endOffset) {
							span = line.StartSpan;
							if (span == null)
								return;
							bool equal = span.Equals(spanStack);
							doUpdate |= !equal;
							if (equal)
								break;
						}
						line.StartSpan = spanStack.Clone();
						parser.ParseSpans(line.Offset, line.LengthIncludingDelimiter);
						while (spanStack.Count > 0 && !EndsWithContinuation(spanStack.Peek(), line))
							parser.PopSpan();
					}
				} catch (Exception e) {
					Console.WriteLine ("Syntax highlighting exception:" + e);
				}
				if (doUpdate) {
					Gtk.Application.Invoke (delegate {
						doc.RequestUpdate (new UpdateAll ());
						doc.CommitDocumentUpdate ();
					});
				}
				IsFinished = true;
				ManualResetEvent.Set ();
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
			updateThread.Name = "Syntax highlighting";
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
		
		public static void StartUpdate (TextDocument doc, SyntaxMode mode, int startOffset, int endOffset)
		{
			lock (updateQueue) {
				updateQueue.Enqueue (new UpdateWorker (doc, mode, startOffset, endOffset));
			}
			queueSignal.Set ();
		}
		
		public static void WaitUpdate (TextDocument doc)
		{
			UpdateWorker[] arr;
			lock (updateQueue) {
				arr = updateQueue.ToArray ();
			}
			foreach (UpdateWorker worker in arr) {
				try {
					if (worker != null && worker.Doc == doc)
						worker.ManualResetEvent.WaitOne ();
				} catch (Exception e) {
					Console.WriteLine (e);
				}
			}
		}
		
		static string Scan (XmlReader reader, string attribute)
		{
			while (reader.Read () && !reader.IsStartElement ()) 
				;
			return reader.GetAttribute (attribute);
		}
		/*
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
		}*/
		
		public static List<ValidationEventArgs> ValidateStyleFile (string fileName)
		{
			List<ValidationEventArgs> result = new List<ValidationEventArgs> ();
			XmlReaderSettings settings = new XmlReaderSettings ();
			settings.ValidationType = ValidationType.Schema;
			settings.ValidationEventHandler += delegate(object sender, ValidationEventArgs e) {
				result.Add (e);
			};
			settings.Schemas.Add (null, new XmlTextReader (typeof(SyntaxModeService).Assembly.GetManifestResourceStream ("Styles.xsd")));
			
			using (XmlReader reader = XmlReader.Create (fileName, settings)) {
				while (reader.Read ())
					;
			}
			return result;
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
					using (XmlTextReader reader =  new XmlTextReader (file)) {
						string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
						foreach (string mimeType in mimeTypes.Split (';')) {
							syntaxModeLookup [mimeType] = new UrlXmlProvider (file);
						}
					}
				} else if (file.EndsWith ("Style.xml")) {
					using (XmlTextReader reader =  new XmlTextReader (file)) {
						string styleName = Scan (reader, ColorScheme.NameAttribute);
						styleLookup [styleName] = new UrlXmlProvider (file);
						isLoadedFromFile [styleName] = file;
					}
				}
			}
		}
		public static void LoadStylesAndModes (Assembly assembly)
		{
			foreach (string resource in assembly.GetManifestResourceNames ()) {
				if (!resource.EndsWith (".xml")) 
					continue;
				if (resource.EndsWith ("SyntaxMode.xml")) {
					using (Stream stream = assembly.GetManifestResourceStream (resource)) 
					using (XmlTextReader reader =  new XmlTextReader (stream)) {
						string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
						ResourceXmlProvider provider = new ResourceXmlProvider (assembly, resource);
						foreach (string mimeType in mimeTypes.Split (';')) {
							syntaxModeLookup [mimeType] = provider;
						}
					}
				} else if (resource.EndsWith ("Style.xml")) {
					using (Stream stream = assembly.GetManifestResourceStream (resource)) 
					using (XmlTextReader reader = new XmlTextReader (stream)) {
						string styleName = Scan (reader, ColorScheme.NameAttribute);
						styleLookup [styleName] = new ResourceXmlProvider (assembly, resource);
					}
				}
			}
		}

		public static void AddSyntaxMode (IXmlProvider provider)
		{
			using (XmlReader reader = provider.Open ()) {
				string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
				foreach (string mimeType in mimeTypes.Split (';')) {
					syntaxModeLookup [mimeType] = provider;
				}
			}
		}
		
		public static void RemoveSyntaxMode (IXmlProvider provider)
		{
			using (XmlReader reader = provider.Open ()) {
				string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
				foreach (string mimeType in mimeTypes.Split (';')) {
					syntaxModeLookup.Remove (mimeType);
				}
			}
		}
		
		public static void AddStyle (string fileName, ColorScheme style)
		{
			isLoadedFromFile [style.Name] = fileName;
			styles [style.Name] = style;
		}
		public static void AddStyle (IXmlProvider provider)
		{
			using (XmlReader reader = provider.Open ()) {
				string styleName = Scan (reader, ColorScheme.NameAttribute);
				styleLookup [styleName] = provider;
			}
		}
		public static void RemoveStyle (IXmlProvider provider)
		{
			using (XmlReader reader = provider.Open ()) {
				string styleName = Scan (reader, ColorScheme.NameAttribute);
				styleLookup.Remove (styleName);
			}
		}
		
		static List<Tuple<string, string, SemanticRule>> semanticRules = new List<Tuple<string, string, SemanticRule>> ();
		
		public static void AddSemanticRule (string mime, string ruleName, SemanticRule rule)
		{
			semanticRules.Add (Tuple.Create (mime, ruleName, rule));
		}
		
		static SyntaxModeService ()
		{
			StartUpdateThread ();
			LoadStylesAndModes (typeof(SyntaxModeService).Assembly);
			SyntaxModeService.AddSemanticRule ("text/x-csharp", "Comment", new HighlightUrlSemanticRule ("comment"));
			SyntaxModeService.AddSemanticRule ("text/x-csharp", "XmlDocumentation", new HighlightUrlSemanticRule ("comment"));
			SyntaxModeService.AddSemanticRule ("text/x-csharp", "String", new HighlightUrlSemanticRule ("string"));
			
			InstallSyntaxMode ("text/x-jay", new SyntaxModeProvider (doc => new JaySyntaxMode (doc)));
		}
	}
}
