using System.Text;
using System.Collections.Generic;

namespace Sharpen
{
	using System;

	internal class MessageFormat
	{
		public static string Format (string message, params object[] args)
		{
			StringBuilder sb = new StringBuilder ();
			bool inQuote = false;
			bool inPlaceholder = false;
			int argStartPos = 0;
			List<string> placeholderArgs = new List<string> (3);
			
			for (int n=0; n<message.Length; n++) {
				char c = message[n];
				if (c == '\'') {
					if (!inQuote)
						inQuote = true;
					else if (n > 0 && message [n-1] == '\'') {
						inQuote = false;
					}
					else {
						inQuote = false;
						continue;
					}
				}
				else if (c == '{' && !inQuote) {
					inPlaceholder = true;
					argStartPos = n + 1;
					continue;
				}
				else if (c == '}' && !inQuote && inPlaceholder) {
					inPlaceholder = false;
					placeholderArgs.Add (message.Substring (argStartPos, n - argStartPos));
					AddFormatted (sb, placeholderArgs, args);
					placeholderArgs.Clear ();
					continue;
				}
				else if (c == ',' && inPlaceholder) {
					placeholderArgs.Add (message.Substring (argStartPos, n - argStartPos));
					argStartPos = n + 1;
					continue;
				}
				else if (inPlaceholder)
					continue;
				
				sb.Append (c);
			}
			return sb.ToString ();
		}

		static void AddFormatted (StringBuilder sb, List<string> placeholderArgs, object[] args)
		{
			if (placeholderArgs.Count > 3)
				throw new ArgumentException ("Invalid format pattern: {" + string.Join (",", placeholderArgs.ToArray()) + "}");
				
			int narg;
			if (!int.TryParse (placeholderArgs[0], out narg))
				throw new ArgumentException ("Invalid argument index: " + placeholderArgs[0]);
			if (narg < 0 || narg >= args.Length)
				throw new ArgumentException ("Invalid argument index: " + narg);
			
			object arg = args [narg];
			sb.Append (arg);
			
			// TODO: handle format types and styles
		}
	}
}
