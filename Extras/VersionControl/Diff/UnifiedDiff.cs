/*
 * A utility class for writing unified diffs.
 */

using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Algorithm.Diff {
	
	public class UnifiedDiff {
		private UnifiedDiff() {
		}
		
		public static void WriteUnifiedDiff(string[] leftLines, string leftName, string[] rightLines, string rightName, System.IO.TextWriter writer, int context, bool caseSensitive, bool compareWhitespace) {
			Diff diff = new Diff(leftLines, rightLines, caseSensitive, compareWhitespace);
			WriteUnifiedDiff(diff, writer, leftName, rightName, context);
		}

		public static void WriteUnifiedDiff(string leftFile, string rightFile, System.IO.TextWriter writer, int context, bool caseSensitive, bool compareWhitespace) {
			WriteUnifiedDiff(LoadFileLines(leftFile), leftFile, LoadFileLines(rightFile), rightFile, writer, context, caseSensitive, compareWhitespace);
		}
		
		internal static string[] LoadFileLines(string file) {
			ArrayList lines = new ArrayList();
			using (System.IO.StreamReader reader = new System.IO.StreamReader(file)) {
				string s;
				while ((s = reader.ReadLine()) != null)
					lines.Add(s);
			}
			return (string[])lines.ToArray(typeof(string));
		}

		public static void WriteUnifiedDiff(Diff diff, TextWriter writer) {
			WriteUnifiedDiff(diff, writer, "Left", "Right", 2);
		}
		
		public static void WriteUnifiedDiff(Diff diff, TextWriter writer, string fromfile, string tofile, int context) {
			writer.Write("--- ");
			writer.WriteLine(fromfile);
			writer.Write("+++ ");
			writer.WriteLine(tofile);
			
			ArrayList hunkset = new ArrayList();
			
			foreach (Diff.Hunk hunk in diff) {
				Diff.Hunk lasthunk = null;
				if (hunkset.Count > 0) lasthunk = (Diff.Hunk)hunkset[hunkset.Count-1];
				
				if (hunk.Same) {
					// At the start of a hunk set, keep only context lines of context.
					if (lasthunk == null) {
						if (hunk.Left.Count > context)
							hunkset.Add( hunk.Crop(hunk.Left.Count-context, 0) );
						else
							hunkset.Add( hunk );
					// Can't have two same hunks in a row, so the last one was a difference.
					} else {
						// Small enough context that this unified diff range will not stop.
						if (hunk.Left.Count <= context*2) {
							hunkset.Add( hunk );
							
						// Too much of the same.  Keep context lines and end this section.
						// And then keep the last context lines as context for the next section.
						} else {
							hunkset.Add( hunk.Crop(0, hunk.Left.Count-context) );
							WriteUnifiedDiffSection(writer, hunkset);
							hunkset.Clear();
							
							if (hunk.Left.Count > context)
								hunkset.Add( hunk.Crop(hunk.Left.Count-context, 0) );
							else
								hunkset.Add( hunk );
						}
					}
				
				} else {
					hunkset.Add(hunk);
				}
			}
			
			if (hunkset.Count > 0 && !(hunkset.Count == 1 && ((Diff.Hunk)hunkset[0]).Same))
				WriteUnifiedDiffSection(writer, hunkset);
		}
			
		private static void WriteUnifiedDiffSection(TextWriter writer, ArrayList hunks) {
			Diff.Hunk first = (Diff.Hunk)hunks[0];
			Diff.Hunk last = (Diff.Hunk)hunks[hunks.Count-1];
			
			writer.Write("@@ -");
			writer.Write(first.Left.Start+1);
			writer.Write(",");
			writer.Write(last.Left.End-first.Left.Start+1);
			writer.Write(" +");
			writer.Write(first.Right.Start+1);
			writer.Write(",");
			writer.Write(last.Right.End-first.Right.Start+1);
			writer.WriteLine(" @@");
			
			foreach (Diff.Hunk hunk in hunks) {
				if (hunk.Same) {
					WriteBlock(writer, ' ', hunk.Left);
					continue;
				}
				
				WriteBlock(writer, '-', hunk.Left);
				WriteBlock(writer, '+', hunk.Right);
			}
		}
		
		private static void WriteBlock(TextWriter writer, char prefix, Range items) {
			if (items.Count > 0 && items[0] is char)
				WriteCharBlock(writer, prefix, items);
			else
				WriteStringBlock(writer, prefix, items);
		}
		
		private static void WriteStringBlock(TextWriter writer, char prefix, Range items) {
			foreach (object item in items) {
				writer.Write(prefix);
				writer.WriteLine(item.ToString());
			}
		}
		
		private static void WriteCharBlock(TextWriter writer, char prefix, Range items) {
			bool newline = true;
			int counter = 0;
			foreach (char c in items) {
				if (c == '\n' && !newline) {
					writer.WriteLine();
					newline = true;
				}
				
				if (newline) {
					writer.Write(prefix);
					newline = false;
					counter = 0;
				}
				
				if (c == '\n') {
					writer.WriteLine("[newline]");
					newline = true;
				} else {
					writer.Write(c);
					counter++;
					if (counter == 60) {
						writer.WriteLine();
						newline = true;
					}
				}
			}
			if (!newline) writer.WriteLine();
		}
	}
}

