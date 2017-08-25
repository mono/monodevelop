using System;
using System.IO;
using System.Text.RegularExpressions;

namespace StripMnemonics
{
	class MainClass
	{
		static string[] langs = {
			"ja",
			"ko",
			"zh_CN",
			"zh_TW",
		};

		static Regex reg = new Regex(@"\(_\w\)$", RegexOptions.Compiled);

		static string StripMnemonics(string text)
		{
			if (reg.IsMatch(text)) {
				return text.Substring(0, text.Length - 4);
			}
			return text;
		}

		static void PostProcess(string file)
		{
			var poFile = POProcessor.Read(file);

			foreach (var block in poFile.Messages) {
				if (block.IdPlural == null) {
					block.TranslatedString = StripMnemonics(block.TranslatedString);
				} else {
					for (int i = 0; i < poFile.NPlurals; ++i)
						block.SetTranslatedPlural(i, StripMnemonics(block.GetTranslatedPlural(i)));
				}
			}

			POProcessor.Write(poFile, file);
		}

		public static void Main(string[] args)
		{
			if (args.Length != 1) {
				Console.WriteLine("Usage: StripMnemonics.exe <po_dir>");
				return;
			}

			foreach (var lang in langs)
				PostProcess(Path.Combine (args[0], lang + ".po"));
		}
	}
}
