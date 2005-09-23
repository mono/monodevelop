// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Roman Taranchenko" email="rnt@smtp.ru"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Text;

namespace MonoDevelop.Core.Properties {
	
	/// <summary>
	/// Contains supported character encodings.
	/// </summary>
	public class CharacterEncodings	{

		static int[] _wellKnownCodePages = {
			    37,    //  IBM EBCDIC (US-Canada)
			   437,    //  OEM United States
			   500,    //  IBM EBCDIC (International)
			   708,    //  Arabic (ASMO 708)
			   850,    //  Western European (DOS)
			   852,    //  Central European (DOS)
			   855,    //  Cyrillic (DOS)
			   857,    //  Turkish (DOS)
			   858,    //  Western European (DOS with Euro)
			   860,    //  Portuguese (DOS)
			   861,    //  Icelandic (DOS)
			   862,    //  Hebrew (DOS)
			   863,    //  French Canadian (DOS)
			   864,    //  Arabic (DOS)
			   865,    //  Nordic (DOS)
			   866,    //  Russian (DOS)
			   869,    //  Greek (DOS)
			   870,    //  IBM EBCDIC (Latin 2)
			   874,    //  Thai (Windows)
			   875,    //  IBM EBCDIC (Greek)
			   932,    //  Japanese (Shift-JIS)
			   936,    //  Chinese Simplified (GB2312)
			   949,    //  Korean
			   950,    //  Chinese Traditional (Big5)
			  1026,    //  IBM EBCDIC (Turkish)
			  1047,    //  IBM EBCDIC (Open Systems Latin 1)
			  1140,    //  IBM EBCDIC (US-Canada with Euro)
			  1141,    //  IBM EBCDIC (Germany with Euro)
			  1142,    //  IBM EBCDIC (Denmark/Norway with Euro)
			  1143,    //  IBM EBCDIC (Finland/Sweden with Euro)
			  1144,    //  IBM EBCDIC (Italy with Euro)
			  1145,    //  IBM EBCDIC (Latin America/Spain with Euro)
			  1146,    //  IBM EBCDIC (United Kingdom with Euro)
			  1147,    //  IBM EBCDIC (France with Euro)
			  1148,    //  IBM EBCDIC (International with Euro)
			  1149,    //  IBM EBCDIC (Icelandic with Euro)
			  1200,    //  Unicode
			  1201,    //  Unicode (Big-Endian)
			  1250,    //  Central European (Windows)
			  1251,    //  Cyrillic (Windows)
			  1252,    //  Western European (Windows)
			  1253,    //  Greek (Windows)
			  1254,    //  Turkish (Windows)
			  1255,    //  Hebrew (Windows)
			  1256,    //  Arabic (Windows)
			  1257,    //  Baltic (Windows)
			  1258,    //  Vietnamese (Windows)
			 10000,    //  Western European (Mac)
			 10007,    //  Cyrillic (Mac)
			 10017,    //  Ukrainian (Mac)
			 10079,    //  Icelandic (Mac)
			 20127,    //  US-ASCII
			 20261,    //  T.61
			 20273,    //  IBM EBCDIC (Germany)
			 20277,    //  IBM EBCDIC (Denmark/Norway)
			 20278,    //  IBM EBCDIC (Finland/Sweden)
			 20280,    //  IBM EBCDIC (Italy)
			 20284,    //  IBM EBCDIC (Latin America/Spain)
			 20285,    //  IBM EBCDIC (United Kingdom)
			 20290,    //  IBM EBCDIC (Japanese Katakana Extended)
			 20297,    //  IBM EBCDIC (France)
			 20420,    //  IBM EBCDIC (Arabic)
			 20424,    //  IBM EBCDIC (Hebrew)
			 20866,    //  Cyrillic (KOI8-R)
			 20871,    //  IBM EBCDIC (Icelandic)
			 21025,    //  IBM EBCDIC (Cyrillic - Serbian, Bulgarian)
			 21866,    //  Ukrainian (KOI8-U)
			 28591,    //  Western European (ISO)
			 28592,    //  Central European (ISO)
			 28593,    //  Latin 3 (ISO)
			 28594,    //  Baltic (ISO)
			 28595,    //  Cyrillic (ISO)
			 28596,    //  Arabic (ISO)
			 28597,    //  Greek (ISO)
			 28598,    //  Hebrew (ISO)
			 28599,    //  Latin 5 (ISO)
			 28605,    //  Latin 9 (ISO)
			 38598,    //  Hebrew (ISO Alternative)
			 50220,    //  Japanese (JIS)
			 50221,    //  Japanese (JIS-Allow 1 byte Kana)
			 50222,    //  Japanese (JIS-Allow 1 byte Kana - SO/SI)
			 50225,    //  Korean (ISO)
			 50227,    //  Chinese Simplified (ISO-2022)
			 51932,    //  Japanese (EUC)
			 51936,    //  Chinese Simplified (EUC)
			 52936,    //  Chinese Simplified (HZ)
			 54936,    //  Chinese Simplified (GB18030)
			 57002,    //  ISCII Devanagari
			 57003,    //  ISCII Bengali
			 57004,    //  ISCII Tamil
			 57005,    //  ISCII Telugu
			 57006,    //  ISCII Assamese
			 57007,    //  ISCII Oriya
			 57008,    //  ISCII Kannada
			 57009,    //  ISCII Malayalam
			 57010,    //  ISCII Gujarati
			 57011,    //  ISCII Punjabi
			 65000,    //  Unicode (UTF-7)
			 65001     //  Unicode (UTF-8)
		};

		static ArrayList _encodings;
		static ArrayList _names;
		static Hashtable _cp2index;
	    
		class EncodingWrapper: IComparable {
         
			private Encoding _encoding;
			private int _cp;
        
			public EncodingWrapper(int cp) {
				_encoding = Encoding.GetEncoding(cp);
				_cp = cp;
			}

			public int CodePage {
				get {
					return _cp;
				}
			}

			public Encoding Encoding {
				get {
					return _encoding;
				}
			}

			public String Name {
				get {
					if (_cp == 0) {
						return "System Default  [ "+Encoding.EncodingName+" ]";
					}
					return Encoding.EncodingName;
				}
			}
        
			public override string ToString() {
				return _cp.ToString();
			}
        
			public override bool Equals(object o) {
				if (o == null) {
					return false;
				}
				if (o == this) {
					return true;
				}
				if (o is EncodingWrapper) {
					return _cp == ((EncodingWrapper)o)._cp;
				}
				return false;
			}
            
			public override int GetHashCode() {
				return _cp;
			}

			int IComparable.CompareTo(object o) {
				// we need to sort encodings by display name
				return Name.CompareTo(((EncodingWrapper)o).Name);
			}
		}
        
        static IList GetSupportedEncodings() {
			ArrayList list = new ArrayList();
			foreach (int cp in _wellKnownCodePages) {
				try {
					list.Add(new EncodingWrapper(cp));
				}
				catch {
					// ignore possible 
					// System.ArgumentException or
					// System.NotSupportedException because
					// .NET fx, mono, rotor & Portable.NET
					// support different sets of encodings
				}
			}
			list.Sort();
        	return list;
        }
        
		static CharacterEncodings() {
			_encodings = new ArrayList();
//			try {
//				// create system default encoding
//				_encodings.Add(new EncodingWrapper(0));
//			}
//			catch {}
			_encodings.AddRange(GetSupportedEncodings());
			
			_names = new ArrayList();
			_cp2index = new Hashtable();
			int i = 0;
			foreach (EncodingWrapper ew in _encodings) {
				_names.Add(ew.Name);
				_cp2index[ew.CodePage] = i;
				++i;		
			}
		}

		public static IList Names {
			get {
				return _names;
			}
		}
        
		public static Encoding GetEncodingByIndex(int i) {
			if (i < 0 || i >= _encodings.Count) {
				return null;
			}
			return ((EncodingWrapper)_encodings[i]).Encoding;
		}
		
		public static Encoding GetEncodingByCodePage(int cp) {
			return GetEncodingByIndex(GetEncodingIndex(cp));
		}
		
		public static int GetEncodingIndex(int cp) {
			try {
				return (Int32)_cp2index[cp];
			}
			catch {
				return -1;
			}
		}

		public static int GetCodePageByIndex(int i) {
			Encoding e = GetEncodingByIndex(i);
			if (e != null) {
				return e.CodePage;
			}
			return -1;
		}
		
		public static bool IsUnicode(Encoding encoding) {
			return IsUnicode(encoding.CodePage);			
		}
		
		public static bool IsUnicode(int codePage) {
			return (codePage == 1200 || codePage == 1201 || codePage == 65000 || codePage == 65001);
		}
	}
}
