//
// From Razor Generator (http://razorgenerator.codeplex.com/)
// Licensed under the Microsoft Public License (MS-PL)
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace RazorGenerator.Core
{
	internal static class DirectivesParser
	{
		public static Dictionary<string, string> ParseDirectives(string fullPath)
		{
			var directives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			ParseFileDirectives(directives, fullPath);
			return directives;
		}

		private static void ParseFileDirectives(Dictionary<string, string> directives, string filePath)
		{
			var inputFileContent = File.ReadAllText(filePath);
			int index = inputFileContent.IndexOf("*@", StringComparison.OrdinalIgnoreCase);
			if (inputFileContent.TrimStart().StartsWith("@*") && index != -1)
			{
				string directivesLine = inputFileContent.Substring(0, index).TrimStart('*', '@');
				ParseKeyValueDirectives(directives, directivesLine);
			}
		}

		private static void ParseKeyValueDirectives(Dictionary<string, string> directives, string directivesLine)
		{
			// TODO: Make this better.
			var regex = new Regex(@"\b(?<Key>\w+)\s*:\s*(?<Value>[~\\\/\w\.]+)\b", RegexOptions.ExplicitCapture);
			foreach (Match item in regex.Matches(directivesLine))
			{
				var key = item.Groups["Key"].Value;
				var value = item.Groups["Value"].Value;

				directives.Add(key, value);
			}
		}
	}
}