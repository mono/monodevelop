//
// IsoCodes.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 1999-2006 Vaclav Slavik (Code and design inspiration - poedit.org)
// Copyright (C) 2007 David Makovský
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Collections.Generic;

namespace MonoDevelop.Gettext
{
	static class IsoCodes
	{
		internal class IsoCode
		{
			string iso;
			string name;
			
			public string Iso {
				get { return iso; }
			}
			
			public string Name {
				get { return name; }
			}
			
			public IsoCode (string iso, string name)
			{
				this.iso  = iso;
				this.name = name;
			}
		}
		
		static IDictionary<string, IsoCode> isoLanguagesByCode;
		static IDictionary<string, IsoCode> isoLanguagesByLang;
		
		static IDictionary<string, IsoCode> isoCountriesByCode;
		static IDictionary<string, IsoCode> isoCountriesByCountry;
		
		static IsoCodes ()
		{
			// TODO: localize???		
			IsoCode[] isoLanguagesTable = new IsoCode[]
			{
				new IsoCode ("aa", "Afar"),
				new IsoCode ("ab", "Abkhazian"),
				new IsoCode ("ae", "Avestan"),
				new IsoCode ("af", "Afrikaans"),
				new IsoCode ("am", "Amharic"),
				new IsoCode ("ar", "Arabic"),
				new IsoCode ("as", "Assamese"),
				new IsoCode ("ay", "Aymara"),
				new IsoCode ("az", "Azerbaijani"),

				new IsoCode ("ba", "Bashkir"),
				new IsoCode ("be", "Belarusian"),
				new IsoCode ("bg", "Bulgarian"),
				new IsoCode ("bh", "Bihari"),
				new IsoCode ("bi", "Bislama"),
				new IsoCode ("bn", "Bengali"),
				new IsoCode ("bo", "Tibetan"),
				new IsoCode ("br", "Breton"),
				new IsoCode ("bs", "Bosnian"),

				new IsoCode ("ca", "Catalan"),
				new IsoCode ("ce", "Chechen"),
				new IsoCode ("ch", "Chamorro"),
				new IsoCode ("co", "Corsican"),
				new IsoCode ("cs", "Czech"),
				new IsoCode ("cu", "Church Slavic"),
				new IsoCode ("cv", "Chuvash"),
				new IsoCode ("cy", "Welsh"),

				new IsoCode ("da", "Danish"),
				new IsoCode ("de", "German"),
				new IsoCode ("dz", "Dzongkha"),

				new IsoCode ("el", "Greek"),
				new IsoCode ("en", "English"),
				new IsoCode ("eo", "Esperanto"),
				new IsoCode ("es", "Spanish"),
				new IsoCode ("et", "Estonian"),
				new IsoCode ("eu", "Basque"),

				new IsoCode ("fa", "Persian"),
				new IsoCode ("fi", "Finnish"),
				new IsoCode ("fj", "Fijian"),
				new IsoCode ("fo", "Faroese"),
				new IsoCode ("fr", "French"),
				new IsoCode ("fur", "Friulian"),
				new IsoCode ("fy", "Frisian"),

				new IsoCode ("ga", "Irish"),
				new IsoCode ("gd", "Gaelic"),
				new IsoCode ("gl", "Galician"),
				new IsoCode ("gn", "Guarani"),
				new IsoCode ("gu", "Gujarati"),

				new IsoCode ("ha", "Hausa"),
				new IsoCode ("he", "Hebrew"),
				new IsoCode ("hi", "Hindi"),
				new IsoCode ("ho", "Hiri Motu"),
				new IsoCode ("hr", "Croatian"),
				new IsoCode ("hu", "Hungarian"),
				new IsoCode ("hy", "Armenian"),
				new IsoCode ("hz", "Herero"),

				new IsoCode ("ia", "Interlingua"),
				new IsoCode ("id", "Indonesian"),
				new IsoCode ("ie", "Interlingue"),
				new IsoCode ("ik", "Inupiaq"),
				new IsoCode ("is", "Icelandic"),
				new IsoCode ("it", "Italian"),
				new IsoCode ("iu", "Inuktitut"),

				new IsoCode ("ja", "Japanese"),
				new IsoCode ("jw", "Javanese"),

				new IsoCode ("ka", "Georgian"),
				new IsoCode ("ki", "Kikuyu"),
				new IsoCode ("kj", "Kuanyama"),
				new IsoCode ("kk", "Kazakh"),
				new IsoCode ("kl", "Kalaallisut"),
				new IsoCode ("km", "Khmer"),
				new IsoCode ("kn", "Kannada"),
				new IsoCode ("ko", "Korean"),
				new IsoCode ("ks", "Kashmiri"),
				new IsoCode ("ku", "Kurdish"),
				new IsoCode ("kv", "Komi"),
				new IsoCode ("kw", "Cornish"),
				new IsoCode ("ky", "Kyrgyz"),

				new IsoCode ("la", "Latin"),
				new IsoCode ("lb", "Letzeburgesch"),
				new IsoCode ("ln", "Lingala"),
				new IsoCode ("lo", "Lao"),
				new IsoCode ("lt", "Lithuanian"),
				new IsoCode ("lv", "Latvian"),

				new IsoCode ("mg", "Malagasy"),
				new IsoCode ("mh", "Marshall"),
				new IsoCode ("mi", "Maori"),
				new IsoCode ("mk", "Macedonian"),
				new IsoCode ("ml", "Malayalam"),
				new IsoCode ("mn", "Mongolian"),
				new IsoCode ("mo", "Moldavian"),
				new IsoCode ("mr", "Marathi"),
				new IsoCode ("ms", "Malay"),
				new IsoCode ("mt", "Maltese"),
				new IsoCode ("my", "Burmese"),

				new IsoCode ("na", "Nauru"),
				new IsoCode ("ne", "Nepali"),
				new IsoCode ("ng", "Ndonga"),
				new IsoCode ("nl", "Dutch"),
				new IsoCode ("nn", "Norwegian Nynorsk"),
				new IsoCode ("nb", "Norwegian Bokmal"),
				new IsoCode ("nr", "Ndebele, South"),
				new IsoCode ("nv", "Navajo"),
				new IsoCode ("ny", "Chichewa; Nyanja"),

				new IsoCode ("oc", "Occitan"),
				new IsoCode ("om", "(Afan) Oromo"),
				new IsoCode ("or", "Oriya"),
				new IsoCode ("os", "Ossetian; Ossetic"),

				new IsoCode ("pa", "Panjabi"),
				new IsoCode ("pi", "Pali"),
				new IsoCode ("pl", "Polish"),
				new IsoCode ("ps", "Pashto, Pushto"),
				new IsoCode ("pt", "Portuguese"),

				new IsoCode ("qu", "Quechua"),

				new IsoCode ("rm", "Rhaeto-Romance"),
				new IsoCode ("rn", "Rundi"),
				new IsoCode ("ro", "Romanian"),
				new IsoCode ("ru", "Russian"),
				new IsoCode ("rw", "Kinyarwanda"),

				new IsoCode ("sa", "Sanskrit"),
				new IsoCode ("sc", "Sardinian"),
				new IsoCode ("sd", "Sindhi"),
				new IsoCode ("se", "Northern Sami"),
				new IsoCode ("sg", "Sangro"),
				new IsoCode ("sh", "Serbo-Croatian"),
				new IsoCode ("si", "Sinhalese"),
				new IsoCode ("sk", "Slovak"),
				new IsoCode ("sl", "Slovenian"),
				new IsoCode ("sm", "Samoan"),
				new IsoCode ("sn", "Shona"),
				new IsoCode ("so", "Somali"),
				new IsoCode ("sq", "Albanian"),
				new IsoCode ("sr", "Serbian"),
				new IsoCode ("ss", "Siswati"),
				new IsoCode ("st", "Sesotho"),
				new IsoCode ("su", "Sundanese"),
				new IsoCode ("sv", "Swedish"),
				new IsoCode ("sw", "Swahili"),

				new IsoCode ("ta", "Tamil"),
				new IsoCode ("te", "Telugu"),
				new IsoCode ("tg", "Tajik"),
				new IsoCode ("th", "Thai"),
				new IsoCode ("ti", "Tigrinya"),
				new IsoCode ("tk", "Turkmen"),
				new IsoCode ("tl", "Tagalog"),
				new IsoCode ("tn", "Setswana"),
				new IsoCode ("to", "Tonga"),
				new IsoCode ("tr", "Turkish"),
				new IsoCode ("ts", "Tsonga"),
				new IsoCode ("tt", "Tatar"),
				new IsoCode ("tw", "Twi"),
				new IsoCode ("ty", "Tahitian"),

				new IsoCode ("ug", "Uighur"),
				new IsoCode ("uk", "Ukrainian"),
				new IsoCode ("ur", "Urdu"),
				new IsoCode ("uz", "Uzbek"),

				new IsoCode ("vi", "Vietnamese"),
				new IsoCode ("vo", "Volapuk"),

				new IsoCode ("wa", "Walloon"),
				new IsoCode ("wo", "Wolof"),

				new IsoCode ("xh", "Xhosa"),

				new IsoCode ("yi", "Yiddish"),
				new IsoCode ("yo", "Yoruba"),

				new IsoCode ("za", "Zhuang"),
				new IsoCode ("zh", "Chinese"),
				new IsoCode ("zu", "Zulu")
			};
			
			isoLanguagesByCode = new Dictionary<string, IsoCode> ();
			isoLanguagesByLang = new Dictionary<string, IsoCode> ();
			
			foreach (IsoCode lang in isoLanguagesTable)
			{
				isoLanguagesByCode.Add (lang.Iso, lang);
				isoLanguagesByLang.Add (lang.Name, lang);
			}
			
			IsoCode[] isoCountriesTable = new IsoCode[]
			{			
				new IsoCode ("AF", "AFGHANISTAN"),
				new IsoCode ("AL", "ALBANIA"),
				new IsoCode ("DZ", "ALGERIA"),
				new IsoCode ("AS", "AMERICAN SAMOA"),
				new IsoCode ("AD", "ANDORRA"),
				new IsoCode ("AO", "ANGOLA"),
				new IsoCode ("AI", "ANGUILLA"),
				new IsoCode ("AQ", "ANTARCTICA"),
				new IsoCode ("AG", "ANTIGUA AND BARBUDA"),
				new IsoCode ("AR", "ARGENTINA"),
				new IsoCode ("AM", "ARMENIA"),
				new IsoCode ("AW", "ARUBA"),
				new IsoCode ("AU", "AUSTRALIA"),
				new IsoCode ("AT", "AUSTRIA"),
				new IsoCode ("AZ", "AZERBAIJAN"),
				new IsoCode ("BS", "BAHAMAS"),
				new IsoCode ("BH", "BAHRAIN"),
				new IsoCode ("BD", "BANGLADESH"),
				new IsoCode ("BB", "BARBADOS"),
				new IsoCode ("BY", "BELARUS"),
				new IsoCode ("BE", "BELGIUM"),
				new IsoCode ("BZ", "BELIZE"),
				new IsoCode ("BJ", "BENIN"),
				new IsoCode ("BM", "BERMUDA"),
				new IsoCode ("BT", "BHUTAN"),
				new IsoCode ("BO", "BOLIVIA"),
				new IsoCode ("BA", "BOSNIA AND HERZEGOVINA"),
				new IsoCode ("BW", "BOTSWANA"),
				new IsoCode ("BV", "BOUVET ISLAND"),
				new IsoCode ("BR", "BRAZIL"),
				new IsoCode ("IO", "BRITISH INDIAN OCEAN TERRITORY"),
				new IsoCode ("BN", "BRUNEI DARUSSALAM"),
				new IsoCode ("BG", "BULGARIA"),
				new IsoCode ("BF", "BURKINA FASO"),
				new IsoCode ("BI", "BURUNDI"),
				new IsoCode ("KH", "CAMBODIA"),
				new IsoCode ("CM", "CAMEROON"),
				new IsoCode ("CA", "CANADA"),
				new IsoCode ("CV", "CAPE VERDE"),
				new IsoCode ("KY", "CAYMAN ISLANDS"),
				new IsoCode ("CF", "CENTRAL AFRICAN REPUBLIC"),
				new IsoCode ("TD", "CHAD"),
				new IsoCode ("CL", "CHILE"),
				new IsoCode ("CN", "CHINA"),
				new IsoCode ("CX", "CHRISTMAS ISLAND"),
				new IsoCode ("CC", "COCOS (KEELING) ISLANDS"),
				new IsoCode ("CO", "COLOMBIA"),
				new IsoCode ("KM", "COMOROS"),
				new IsoCode ("CG", "CONGO"),
				new IsoCode ("CD", "CONGO, THE DEMOCRATIC REPUBLIC OF THE"),
				new IsoCode ("CK", "COOK ISLANDS"),
				new IsoCode ("CR", "COSTA RICA"),
				new IsoCode ("CI", "COTE D'IVOIRE"),
				new IsoCode ("HR", "CROATIA"),
				new IsoCode ("CU", "CUBA"),
				new IsoCode ("CY", "CYPRUS"),
				new IsoCode ("CZ", "CZECH REPUBLIC"),
				new IsoCode ("DK", "DENMARK"),
				new IsoCode ("DJ", "DJIBOUTI"),
				new IsoCode ("DM", "DOMINICA"),
				new IsoCode ("DO", "DOMINICAN REPUBLIC"),
				new IsoCode ("EC", "ECUADOR"),
				new IsoCode ("EG", "EGYPT"),
				new IsoCode ("SV", "EL SALVADOR"),
				new IsoCode ("GQ", "EQUATORIAL GUINEA"),
				new IsoCode ("ER", "ERITREA"),
				new IsoCode ("EE", "ESTONIA"),
				new IsoCode ("ET", "ETHIOPIA"),
				new IsoCode ("FK", "FALKLAND ISLANDS (MALVINAS)"),
				new IsoCode ("FO", "FAROE ISLANDS"),
				new IsoCode ("FJ", "FIJI"),
				new IsoCode ("FI", "FINLAND"),
				new IsoCode ("FR", "FRANCE"),
				new IsoCode ("GF", "FRENCH GUIANA"),
				new IsoCode ("PF", "FRENCH POLYNESIA"),
				new IsoCode ("TF", "FRENCH SOUTHERN TERRITORIES"),
				new IsoCode ("GA", "GABON"),
				new IsoCode ("GM", "GAMBIA"),
				new IsoCode ("GE", "GEORGIA"),
				new IsoCode ("DE", "GERMANY"),
				new IsoCode ("GH", "GHANA"),
				new IsoCode ("GI", "GIBRALTAR"),
				new IsoCode ("GR", "GREECE"),
				new IsoCode ("GL", "GREENLAND"),
				new IsoCode ("GD", "GRENADA"),
				new IsoCode ("GP", "GUADELOUPE"),
				new IsoCode ("GU", "GUAM"),
				new IsoCode ("GT", "GUATEMALA"),
				new IsoCode ("GN", "GUINEA"),
				new IsoCode ("GW", "GUINEA-BISSAU"),
				new IsoCode ("GY", "GUYANA"),
				new IsoCode ("HT", "HAITI"),
				new IsoCode ("HM", "HEARD ISLAND AND MCDONALD ISLANDS"),
				new IsoCode ("VA", "HOLY SEE (VATICAN CITY STATE)"),
				new IsoCode ("HN", "HONDURAS"),
				new IsoCode ("HK", "HONG KONG"),
				new IsoCode ("HU", "HUNGARY"),
				new IsoCode ("IS", "ICELAND"),
				new IsoCode ("IN", "INDIA"),
				new IsoCode ("ID", "INDONESIA"),
				new IsoCode ("IR", "IRAN, ISLAMIC REPUBLIC OF"),
				new IsoCode ("IQ", "IRAQ"),
				new IsoCode ("IE", "IRELAND"),
				new IsoCode ("IL", "ISRAEL"),
				new IsoCode ("IT", "ITALY"),
				new IsoCode ("JM", "JAMAICA"),
				new IsoCode ("JP", "JAPAN"),
				new IsoCode ("JO", "JORDAN"),
				new IsoCode ("KZ", "KAZAKHSTAN"),
				new IsoCode ("KE", "KENYA"),
				new IsoCode ("KI", "KIRIBATI"),
				new IsoCode ("KP", "KOREA, DEMOCRATIC PEOPLE'S REPUBLIC OF"),
				new IsoCode ("KR", "KOREA, REPUBLIC OF"),
				new IsoCode ("KW", "KUWAIT"),
				new IsoCode ("KG", "KYRGYZSTAN"),
				new IsoCode ("LA", "LAO PEOPLE'S DEMOCRATIC REPUBLIC"),
				new IsoCode ("LV", "LATVIA"),
				new IsoCode ("LB", "LEBANON"),
				new IsoCode ("LS", "LESOTHO"),
				new IsoCode ("LR", "LIBERIA"),
				new IsoCode ("LY", "LIBYAN ARAB JAMAHIRIYA"),
				new IsoCode ("LI", "LIECHTENSTEIN"),
				new IsoCode ("LT", "LITHUANIA"),
				new IsoCode ("LU", "LUXEMBOURG"),
				new IsoCode ("MO", "MACAO"),
				new IsoCode ("MK", "MACEDONIA, THE FORMER YUGOSLAV REPUBLIC OF"),
				new IsoCode ("MG", "MADAGASCAR"),
				new IsoCode ("MW", "MALAWI"),
				new IsoCode ("MY", "MALAYSIA"),
				new IsoCode ("MV", "MALDIVES"),
				new IsoCode ("ML", "MALI"),
				new IsoCode ("MT", "MALTA"),
				new IsoCode ("MH", "MARSHALL ISLANDS"),
				new IsoCode ("MQ", "MARTINIQUE"),
				new IsoCode ("MR", "MAURITANIA"),
				new IsoCode ("MU", "MAURITIUS"),
				new IsoCode ("YT", "MAYOTTE"),
				new IsoCode ("MX", "MEXICO"),
				new IsoCode ("FM", "MICRONESIA, FEDERATED STATES OF"),
				new IsoCode ("MD", "MOLDOVA, REPUBLIC OF"),
				new IsoCode ("MC", "MONACO"),
				new IsoCode ("MN", "MONGOLIA"),
				new IsoCode ("MS", "MONTSERRAT"),
				new IsoCode ("MA", "MOROCCO"),
				new IsoCode ("MZ", "MOZAMBIQUE"),
				new IsoCode ("MM", "MYANMAR"),
				new IsoCode ("NA", "NAMIBIA"),
				new IsoCode ("NR", "NAURU"),
				new IsoCode ("NP", "NEPAL"),
				new IsoCode ("NL", "NETHERLANDS"),
				new IsoCode ("AN", "NETHERLANDS ANTILLES"),
				new IsoCode ("NC", "NEW CALEDONIA"),
				new IsoCode ("NZ", "NEW ZEALAND"),
				new IsoCode ("NI", "NICARAGUA"),
				new IsoCode ("NE", "NIGER"),
				new IsoCode ("NG", "NIGERIA"),
				new IsoCode ("NU", "NIUE"),
				new IsoCode ("NF", "NORFOLK ISLAND"),
				new IsoCode ("MP", "NORTHERN MARIANA ISLANDS"),
				new IsoCode ("NO", "NORWAY"),
				new IsoCode ("OM", "OMAN"),
				new IsoCode ("PK", "PAKISTAN"),
				new IsoCode ("PW", "PALAU"),
				new IsoCode ("PS", "PALESTINIAN TERRITORY, OCCUPIED"),
				new IsoCode ("PA", "PANAMA"),
				new IsoCode ("PG", "PAPUA NEW GUINEA"),
				new IsoCode ("PY", "PARAGUAY"),
				new IsoCode ("PE", "PERU"),
				new IsoCode ("PH", "PHILIPPINES"),
				new IsoCode ("PN", "PITCAIRN"),
				new IsoCode ("PL", "POLAND"),
				new IsoCode ("PT", "PORTUGAL"),
				new IsoCode ("PR", "PUERTO RICO"),
				new IsoCode ("QA", "QATAR"),
				new IsoCode ("RE", "REUNION"),
				new IsoCode ("RO", "ROMANIA"),
				new IsoCode ("RU", "RUSSIAN FEDERATION"),
				new IsoCode ("RW", "RWANDA"),
				new IsoCode ("SH", "SAINT HELENA"),
				new IsoCode ("KN", "SAINT KITTS AND NEVIS"),
				new IsoCode ("LC", "SAINT LUCIA"),
				new IsoCode ("PM", "SAINT PIERRE AND MIQUELON"),
				new IsoCode ("VC", "SAINT VINCENT AND THE GRENADINES"),
				new IsoCode ("WS", "SAMOA"),
				new IsoCode ("SM", "SAN MARINO"),
				new IsoCode ("ST", "SAO TOME AND PRINCIPE"),
				new IsoCode ("SA", "SAUDI ARABIA"),
				new IsoCode ("SN", "SENEGAL"),
				new IsoCode ("SC", "SEYCHELLES"),
				new IsoCode ("SL", "SIERRA LEONE"),
				new IsoCode ("SG", "SINGAPORE"),
				new IsoCode ("SK", "SLOVAKIA"),
				new IsoCode ("SI", "SLOVENIA"),
				new IsoCode ("SB", "SOLOMON ISLANDS"),
				new IsoCode ("SO", "SOMALIA"),
				new IsoCode ("ZA", "SOUTH AFRICA"),
				new IsoCode ("GS", "SOUTH GEORGIA AND THE SOUTH SANDWICH ISLANDS"),
				new IsoCode ("ES", "SPAIN"),
				new IsoCode ("LK", "SRI LANKA"),
				new IsoCode ("SD", "SUDAN"),
				new IsoCode ("SR", "SURINAME"),
				new IsoCode ("SJ", "SVALBARD AND JAN MAYEN"),
				new IsoCode ("SZ", "SWAZILAND"),
				new IsoCode ("SE", "SWEDEN"),
				new IsoCode ("CH", "SWITZERLAND"),
				new IsoCode ("SY", "SYRIAN ARAB REPUBLIC"),
				new IsoCode ("TW", "TAIWAN"),
				new IsoCode ("TJ", "TAJIKISTAN"),
				new IsoCode ("TZ", "TANZANIA, UNITED REPUBLIC OF"),
				new IsoCode ("TH", "THAILAND"),
				new IsoCode ("TL", "TIMOR-LESTE"),
				new IsoCode ("TG", "TOGO"),
				new IsoCode ("TK", "TOKELAU"),
				new IsoCode ("TO", "TONGA"),
				new IsoCode ("TT", "TRINIDAD AND TOBAGO"),
				new IsoCode ("TN", "TUNISIA"),
				new IsoCode ("TR", "TURKEY"),
				new IsoCode ("TM", "TURKMENISTAN"),
				new IsoCode ("TC", "TURKS AND CAICOS ISLANDS"),
				new IsoCode ("TV", "TUVALU"),
				new IsoCode ("UG", "UGANDA"),
				new IsoCode ("UA", "UKRAINE"),
				new IsoCode ("AE", "UNITED ARAB EMIRATES"),
				new IsoCode ("GB", "UNITED KINGDOM"),
				new IsoCode ("US", "UNITED STATES"),
				new IsoCode ("UM", "UNITED STATES MINOR OUTLYING ISLANDS"),
				new IsoCode ("UY", "URUGUAY"),
				new IsoCode ("UZ", "UZBEKISTAN"),
				new IsoCode ("VU", "VANUATU"),
				new IsoCode ("VE", "VENEZUELA"),
				new IsoCode ("VN", "VIET NAM"),
				new IsoCode ("VG", "VIRGIN ISLANDS, BRITISH"),
				new IsoCode ("VI", "VIRGIN ISLANDS, U.S."),
				new IsoCode ("WF", "WALLIS AND FUTUNA"),
				new IsoCode ("EH", "WESTERN SAHARA"),
				new IsoCode ("YE", "YEMEN"),
				new IsoCode ("YU", "YUGOSLAVIA"),
				new IsoCode ("ZM", "ZAMBIA"),
				new IsoCode ("ZW", "ZIMBABWE")
			};
			
			isoCountriesByCode = new Dictionary<string, IsoCode> ();
			isoCountriesByCountry = new Dictionary<string, IsoCode> ();
			
			foreach (IsoCode country in isoCountriesTable) {
				isoCountriesByCode.Add (country.Iso, country);
				isoCountriesByCountry.Add (country.Name, country);
			}
		 
		}
		
		public static IsoCode LookupLanguageCode (string code)
		{
			if (isoLanguagesByCode.ContainsKey (code))
				return isoLanguagesByCode [code];
			return null;
		}
		
		public static IsoCode LookupCountryCode (string code)
		{
			if (isoCountriesByCode.ContainsKey (code))
				return isoCountriesByCode [code];
			return null;
		}

		public static bool IsKnownLanguageCode (string code)
		{
			return isoLanguagesByCode.ContainsKey (code);
		}
		
		public static bool IsKnownCountryCode (string code)
		{
			return isoCountriesByCode.ContainsKey (code);
		}
		
		public static IEnumerable<IsoCode> KnownLanguages
		{
			get { return isoLanguagesByLang.Values; }
		}
		
		public static IEnumerable<IsoCode> KnownCountries
		{
			get { return isoCountriesByCountry.Values; }
		}
	}
}

