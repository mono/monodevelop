using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.JavaScript.Formatting
{
	[PolicyType ("JavaScript formatting")]
	public class JavaScriptFormattingPolicy : IEquatable<JavaScriptFormattingPolicy>
	{
		List<JavaScriptFormattingSettings> formats = new List<JavaScriptFormattingSettings> ();
		JavaScriptFormattingSettings defaultFormat = new JavaScriptFormattingSettings ();

		public JavaScriptFormattingPolicy ()
		{
		}

		[ItemProperty]
		public List<JavaScriptFormattingSettings> Formats {
			get { return formats; }
		}

		[ItemProperty]
		public JavaScriptFormattingSettings DefaultFormat {
			get { return defaultFormat; }
		}

		public bool Equals (JavaScriptFormattingPolicy other)
		{
			if (!defaultFormat.Equals (other.defaultFormat))
				return false;

			if (formats.Count != other.formats.Count)
				return false;

			List<JavaScriptFormattingSettings> list = new List<JavaScriptFormattingSettings> (other.formats);
			foreach (JavaScriptFormattingSettings fs in formats) {
				bool found = false;
				for (int n = 0; n < list.Count; n++) {
					if (fs.Equals (list [n])) {
						list.RemoveAt (n);
						found = true;
						break;
					}
				}
				if (!found)
					return false;
			}
			return true;
		}

		public JavaScriptFormattingPolicy Clone ()
		{
			JavaScriptFormattingPolicy clone = new JavaScriptFormattingPolicy ();
			clone.defaultFormat = defaultFormat.Clone ();
			foreach (var f in formats)
				clone.formats.Add (f.Clone ());
			return clone;
		}
	}
}
