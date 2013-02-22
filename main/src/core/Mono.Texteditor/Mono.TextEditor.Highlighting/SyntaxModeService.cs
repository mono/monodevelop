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
		static Dictionary<string, IStreamProvider> syntaxModeLookup = new Dictionary<string, IStreamProvider> ();
		static Dictionary<string, IStreamProvider> styleLookup      = new Dictionary<string, IStreamProvider> ();
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
		
		public static ColorScheme GetColorStyle (string name)
		{
			if (styles.ContainsKey (name))
				return styles [name];
			if (styleLookup.ContainsKey (name)) {
				LoadStyle (name);
				return GetColorStyle (name);
			}
			return GetColorStyle ("Default");
		}
		
		public static IStreamProvider GetProvider (SyntaxMode mode)
		{
			foreach (string mimeType in mode.MimeType.Split (';')) {
				if (syntaxModeLookup.ContainsKey (mimeType)) 
					return syntaxModeLookup[mimeType];
			}
			return null;
		}
		
		public static IStreamProvider GetProvider (ColorScheme style)
		{
			if (styleLookup.ContainsKey (style.Name)) 
				return styleLookup[style.Name];
			return null;
		}
		
		static void LoadStyle (string name)
		{
			if (!styleLookup.ContainsKey (name))
				throw new System.ArgumentException ("Style " + name + " not found", "name");
			var provider = styleLookup [name];
			var stream = provider.Open ();
			try {
				if (provider is UrlStreamProvider) {
					var usp = provider as UrlStreamProvider;
					if (usp.Url.EndsWith (".vssettings", StringComparison.Ordinal)) {
						styles [name] = ColorScheme.Import (usp.Url, stream);
					} else {
						styles [name] = ColorScheme.LoadFrom (stream);
					}
				} else {
					styles [name] = ColorScheme.LoadFrom (stream);
				}
			} catch (Exception e) {
				throw new IOException ("Error while loading style :" + name, e);
			} finally {
				stream.Close ();
			}
		}
		
		static void LoadSyntaxMode (string mimeType)
		{
			if (!syntaxModeLookup.ContainsKey (mimeType))
				throw new System.ArgumentException ("Syntax mode for mime:" + mimeType + " not found", "mimeType");
			var reader = syntaxModeLookup [mimeType].Open ();
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
				try {
					LoadSyntaxMode (mimeType);
					result = GetSyntaxMode (doc, mimeType);
				} catch (Exception e) {
					Console.WriteLine (e);
				}
				syntaxModeLookup.Remove (mimeType);
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
				GetColorStyle (style);
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
					line != null && doc.GetTextAt (line).Trim ().EndsWith (span.Continuation, StringComparison.Ordinal);
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
		
		static string Scan (Stream stream, string attribute)
		{
			var reader = XmlReader.Create (stream);
			while (reader.Read () && !reader.IsStartElement ()) 
				;
			return reader.GetAttribute (attribute);
		}

		public static List<ValidationEventArgs> ValidateStyleFile (string fileName)
		{
			List<ValidationEventArgs> result = new List<ValidationEventArgs> ();
			return result;
		}

		public static bool IsValidSyntaxMode (string fileName)
		{
			if (!fileName.EndsWith ("SyntaxMode.xml", StringComparison.Ordinal))
				return false;
			try {
				using (var stream = File.OpenRead (fileName)) {
					string mimeTypes = Scan (stream, SyntaxMode.MimeTypesAttribute);
					return !String.IsNullOrEmpty (mimeTypes);
				}
			} catch (Exception) {
				return false;
			}
		}
		
		public static void LoadStylesAndModes (string path)
		{
			foreach (string file in Directory.GetFiles (path)) {
				if (file.EndsWith ("SyntaxMode.xml", StringComparison.Ordinal)) {
					using (var stream = File.OpenRead (file)) {
						string mimeTypes = Scan (stream, SyntaxMode.MimeTypesAttribute);
						foreach (string mimeType in mimeTypes.Split (';')) {
							syntaxModeLookup [mimeType] = new UrlStreamProvider (file);
						}
					}
				} else if (file.EndsWith ("Style.json", StringComparison.Ordinal)) {
					using (var stream = File.OpenRead (file)) {
						string styleName = ScanStyle (stream);
						styleLookup [styleName] = new UrlStreamProvider (file);
						isLoadedFromFile [styleName] = file;
					}
				} else if (file.EndsWith (".vssettings", StringComparison.Ordinal)) {
					using (var stream = File.OpenRead (file)) {
						string styleName = Path.GetFileNameWithoutExtension (file);
						styleLookup [styleName] = new UrlStreamProvider (file);
						isLoadedFromFile [styleName] = file;
					}
				}
			}
		}

		public static void LoadStylesAndModes (Assembly assembly)
		{
			foreach (string resource in assembly.GetManifestResourceNames ()) {
				if (resource.EndsWith ("SyntaxMode.xml", StringComparison.Ordinal)) {
					using (Stream stream = assembly.GetManifestResourceStream (resource)) {
						string mimeTypes = Scan (stream, SyntaxMode.MimeTypesAttribute);
						ResourceStreamProvider provider = new ResourceStreamProvider (assembly, resource);
						foreach (string mimeType in mimeTypes.Split (';')) {
							syntaxModeLookup [mimeType] = provider;
						}
					}
				} else if (resource.EndsWith ("Style.json", StringComparison.Ordinal)) {
					using (Stream stream = assembly.GetManifestResourceStream (resource)) {
						string styleName = ScanStyle (stream);
						styleLookup [styleName] = new ResourceStreamProvider (assembly, resource);
					}
				}
			}
		}
		static System.Text.RegularExpressions.Regex nameRegex = new System.Text.RegularExpressions.Regex ("\\s*\"name\"\\s*:\\s*\"(.*)\"\\s*,");

		static string ScanStyle (Stream stream)
		{
			var file = new StreamReader (stream);
			file.ReadLine ();
			var nameLine = file.ReadLine ();
			var match = nameRegex.Match (nameLine);
			if (!match.Success)
				return null;
			return match.Groups[1].Value;
		}

		public static void AddSyntaxMode (IStreamProvider provider)
		{
			using (var reader = provider.Open ()) {
				string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
				foreach (string mimeType in mimeTypes.Split (';')) {
					syntaxModeLookup [mimeType] = provider;
				}
			}
		}
		
		public static void RemoveSyntaxMode (IStreamProvider provider)
		{
			using (var reader = provider.Open ()) {
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

		public static void AddStyle (IStreamProvider provider)
		{
			using (var stream = provider.Open ()) {
				string styleName = ScanStyle (stream);
				styleLookup [styleName] = provider;
			}
		}

		public static void RemoveStyle (IStreamProvider provider)
		{
			using (var stream = provider.Open ()) {
				string styleName = ScanStyle (stream);
				styleLookup.Remove (styleName);
			}
		}
		
		static List<Tuple<string, string, SemanticRule>> semanticRules = new List<Tuple<string, string, SemanticRule>> ();
		
		public static void AddSemanticRule (string mime, string ruleName, SemanticRule rule)
		{
			semanticRules.Add (Tuple.Create (mime, ruleName, rule));
		}

		public static ColorScheme DefaultColorStyle {
			get {
				return GetColorStyle ("Default");
			}
		}
		
		static SyntaxModeService ()
		{
			StartUpdateThread ();
			LoadStylesAndModes (typeof(SyntaxModeService).Assembly);
			SyntaxModeService.AddSemanticRule ("text/x-csharp", "Comment", new HighlightUrlSemanticRule ("Comment(Line)"));
			SyntaxModeService.AddSemanticRule ("text/x-csharp", "XmlDocumentation", new HighlightUrlSemanticRule ("Comment(Doc)"));
			SyntaxModeService.AddSemanticRule ("text/x-csharp", "String", new HighlightUrlSemanticRule ("String"));
			
			InstallSyntaxMode ("text/x-jay", new SyntaxModeProvider (doc => new JaySyntaxMode (doc)));
		}
	}
}
