//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using System;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// A namespace Uri and a prefix.
	/// </summary>
	public class XmlNamespace
	{
		string prefix = String.Empty;
		string uri = String.Empty;
		
		const string prefixToStringStart = "Prefix [";
		const string uriToStringMiddle = "] Uri [";
		
		public XmlNamespace(string prefix, string uri)
		{
			this.prefix = prefix;
			this.uri = uri;
		}
		
		public string Prefix {
			get {
				return prefix;
			}
		}
		
		public string Uri {
			get {
				return uri;
			}
		}
		
		public override string ToString()
		{
			return String.Concat(prefixToStringStart, prefix, uriToStringMiddle, uri, "]");
		}
		
		/// <summary>
		/// Creates an XmlNamespace instance from the given string that is in the
		/// format returned by ToString.
		/// </summary>
		public static XmlNamespace FromString(string s)
		{
			int prefixIndex = s.IndexOf(prefixToStringStart);
			if (prefixIndex >= 0) {
				prefixIndex += prefixToStringStart.Length;
				int uriIndex = s.IndexOf(uriToStringMiddle, prefixIndex);
				if (uriIndex >= 0) {
					string prefix = s.Substring(prefixIndex, uriIndex - prefixIndex);
					uriIndex += uriToStringMiddle.Length;
					string uri = s.Substring(uriIndex, s.Length - (uriIndex + 1));
					return new XmlNamespace(prefix, uri);
				}
			}
			return new XmlNamespace(String.Empty, String.Empty);
		}
	}
}
