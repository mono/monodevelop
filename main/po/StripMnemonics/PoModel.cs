using System;
using System.Collections.Generic;

namespace StripMnemonics
{
	[Serializable]
	public class POBlock : IEquatable<POBlock>
	{
		string id = string.Empty;
		public string Id
		{
			get { return id; }
			set { id = value.Replace("\r\n", "\n").Replace("\n", "\r\n"); }
		}

		string idPlural = null;
		public string IdPlural
		{
			get { return idPlural; }
			set { idPlural = value.Replace("\r\n", "\n").Replace("\n", "\r\n"); }
		}

		string translatedString = string.Empty;
		public string TranslatedString
		{
			get { return translatedString; }
			set { translatedString = value.Replace("\r\n", "\n").Replace("\n", "\r\n"); }
		}

		readonly string[] translatedPlural = new string[3];
		public string GetTranslatedPlural(int index)
		{
			return translatedPlural[index];
		}

		public void SetTranslatedPlural(int index, string value)
		{
			translatedPlural[index] = value.Replace("\r\n", "\n").Replace("\n", "\r\n");
		}

		public List<string> Metadata { get; } = new List<string>();

		static bool IsPluralEqual(string a, string b)
		{
			return (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) || a == b;
		}

		public bool Equals(POBlock other)
		{
			return id == other.id &&
					idPlural == other.idPlural &&
					translatedString == other.translatedString &&
					IsPluralEqual(translatedPlural[0], other.translatedPlural[0]) &&
					IsPluralEqual(translatedPlural[1], other.translatedPlural[1]) &&
					IsPluralEqual(translatedPlural[2], other.translatedPlural[2]);
		}
	}

	[Serializable]
	public class POFile : IEquatable<POFile>
	{
		public string FileName;
		public List<string> CopyrightHeader { get; } = new List<string> ();
		public int NPlurals;
		public bool NPluralsSet;
		public POBlock Header { get; set; }
		public List<POBlock> Messages { get; } = new List<POBlock> ();

		public bool Equals(POFile other)
		{
			bool basic = NPlurals == other.NPlurals &&
						 Header.Equals(other.Header);

			bool collections = true;
			for (int i = 0; i < CopyrightHeader.Count; ++i)
				collections &= CopyrightHeader[i] == other.CopyrightHeader[i];

			bool messages = true;
			for (int i = 0; i < Messages.Count; ++i)
			{
				var msg = Messages[i];
				var otherMsg = other.Messages[i];
				bool equals = msg.Equals(otherMsg);
				messages &= equals;
			}

			return basic && collections && messages;
		}
	}
}

