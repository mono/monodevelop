using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StripMnemonics
{
	public static class POProcessor
	{
		public static POFile Read (string poFile)
		{
			var po = new POFile {
				FileName = poFile
			};

			using (var stream = File.OpenRead (poFile))
			using (var reader = new StreamReader (stream)) {
				string line = reader.ReadLine ();

				// Read PO Copyright header.
				while (line.StartsWith ("# ", StringComparison.Ordinal)) {
					po.CopyrightHeader.Add (line);
					line = reader.ReadLine ();
				}

				po.Header = ReadBlock (ref line, reader, po);
				if (!po.NPluralsSet)
				{
					po.NPluralsSet = true;
					po.NPlurals = 2;
				}

				while (!reader.EndOfStream) {
					var block = ReadBlock (ref line, reader, po);
					if (block != null)
						po.Messages.Add (block);
				}
			}
			return po;
		}

		static POBlock ReadBlock (ref string line, StreamReader reader, POFile po)
		{
			var map = new HashSet<string> {
				{ "# " },
				{ "#. " },
				{ "#: " },
				{ "#, " },
				{ "#| msgid " },
			};

			var block = new POBlock ();
			bool init = false;

			do {
				foreach (var mapping in map) {
					if (line.StartsWith (mapping, StringComparison.Ordinal)) {
						block.Metadata.Add(line);
						goto read_next;
					}
				}

				string actual;
				bool readNext;
				if ((actual = ReadString(ref line, reader, "msgid ", out readNext, po)) != null)
				{
					block.Id = actual;
					init = true;
					if (!readNext)
						continue;
				}

				if ((actual = ReadString(ref line, reader, "msgid_plural ", out readNext, po)) != null)
				{
					block.IdPlural = actual;
					if (!readNext)
						continue;
				}

				if ((actual = ReadString(ref line, reader, "msgstr ", out readNext, po)) != null)
				{
					block.TranslatedString = actual;
					if (!readNext)
						continue;
				}

				if ((actual = ReadString(ref line, reader, "msgstr[0] ", out readNext, po)) != null)
				{
					block.SetTranslatedPlural(0, actual);
					if (!readNext)
						continue;
				}

				if ((actual = ReadString(ref line, reader, "msgstr[1] ", out readNext, po)) != null)
				{
					block.SetTranslatedPlural(1, actual);
					if (!readNext)
						continue;
				}

				if ((actual = ReadString(ref line, reader, "msgstr[2] ", out readNext, po)) != null)
				{
					block.SetTranslatedPlural(2, actual);
					if (!readNext)
						continue;
				}

			read_next:
				line = reader.ReadLine ();
			} while (!string.IsNullOrEmpty (line));

			if (init)
				return block;
			return null;
		}

		static string ReadString(ref string line, StreamReader reader, string header, out bool readNext, POFile po)
		{
			if (!line.StartsWith(header, StringComparison.Ordinal))
			{
				readNext = false;
				return null;
			}

			var actual = line.Substring(header.Length);
			actual = actual.Substring(Math.Min (1, actual.Length), Math.Max (actual.Length - 2, 0));
			bool isMultiline = string.IsNullOrEmpty(actual);
			if (!isMultiline)
			{
				readNext = true;
				return actual;
			}

			var multiLineString = new List<string>();

			line = reader.ReadLine();
			while (!string.IsNullOrEmpty(line) && line.StartsWith("\"", StringComparison.Ordinal))
			{
				var leadingStripped = line.Remove(0, 1);
				var stripped = leadingStripped.Remove(leadingStripped.Length - 1, 1);
				multiLineString.Add(stripped);

				// Parse number of plural forms to write for each string.
				if (line.StartsWith("\"Plural-Forms:", StringComparison.Ordinal))
				{
					var rest = line.Substring("\"Plural-Forms: ".Length).TrimEnd('\"').Split (';');
					int plurals;
					if (int.TryParse(rest[0].Trim().Substring("nplurals=".Length), out plurals))
					    po.NPlurals = plurals;
					po.NPluralsSet = true;
				}
				line = reader.ReadLine();
			}
			readNext = false;
			return string.Concat (multiLineString);
		}


		public static void Write(POFile po, string outPath)
		{
			using (var stream = File.Open(outPath, FileMode.Create))
			using (var writer = new StreamWriter(stream))
			{
				foreach (var line in po.CopyrightHeader)
					writer.WriteLine(line);

				writer.WriteLine("msgid \"\"");
				writer.WriteLine("msgstr \"\"");
				foreach (var line in po.Header.TranslatedString.Split(new string[] { "\\n" }, StringSplitOptions.RemoveEmptyEntries))
					writer.WriteLine($"\"{line}\\n\"");

				writer.WriteLine();
				bool isMessages = po.FileName.EndsWith("messages.po", StringComparison.OrdinalIgnoreCase);
				foreach (var block in po.Messages)
				{
					WriteBlock(block, writer, po, isMessages);
					writer.WriteLine();
				}
			}
		}

		static IEnumerable<string> LineWrap(string text, string kind)
		{
			// Gettext wraps at 80 chars, including quotes.
			// Also breaks on \n.

			// Take into account kind, quotes and space.
			if (text.Length + kind.Length + 3 > 80 || text.Contains("\\n"))
				yield return kind + " \"\"";
			else
			{
				yield return kind + $" \"{text}\"";
				yield break;
			}

			var words = text.Split(new char[] { ' ' });
			var sb = new StringBuilder();

			for (int i = 0; i < words.Length; ++i)
			{
				// Add the space into account.
				if (sb.Length + words[i].Length + 3 > 79)
				{
					yield return $"\"{sb.ToString()}\"";
					sb.Clear();
				}

				if (words[i].Contains("\\n"))
				{
					var split = words[i].Split(new string[] { "\\n" }, StringSplitOptions.None);
					for (int splitIndex = 0; splitIndex < split.Length - 1; ++splitIndex)
					{
						sb.Append(split[splitIndex]);
						sb.Append("\\n");
						yield return $"\"{sb.ToString()}\"";
						sb.Clear();
					}
					sb.Append(split[split.Length - 1]);
				}
				else
				{
					sb.Append(words[i]);
				}

				if (i != words.Length - 1)
					sb.Append(' ');
			}
			if (sb.Length > 0)
				yield return $"\"{sb.ToString()}\"";
			yield break;
		}

		static void WriteBlock(POBlock block, StreamWriter writer, POFile po, bool isMessages)
		{
			foreach (var item in block.Metadata)
				writer.WriteLine(item);

			foreach (var line in LineWrap(block.Id.Replace("\r\n", "\n"), "msgid"))
				writer.WriteLine(line);

			if (block.IdPlural != null)
			{
				foreach (var line in LineWrap(block.IdPlural.Replace("\r\n", "\n"), "msgid_plural"))
					writer.WriteLine(line);

				for (int i = 0; i < po.NPlurals; ++i)
				{
					var translatedPlural = block.GetTranslatedPlural(i);
					if (translatedPlural == null)
						continue;

					string value = isMessages ? "" : translatedPlural.Replace("\r\n", "\n");
					foreach (var line in LineWrap(value, $"msgstr[{i}]"))
						writer.WriteLine(line);
				}
			}
			else
			{
				string value = isMessages ? "" : block.TranslatedString.Replace("\r\n", "\n");
				foreach (var line in LineWrap(value, "msgstr"))
					writer.WriteLine(line);
			}
		}
	}
}

