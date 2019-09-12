using MonoDevelop.Core;

namespace MonoDevelop.Ide
{
	sealed class XwtTranslationCatalog : Xwt.ITranslationCatalog
	{
		public string GetString (string str)
		{
			return GettextCatalog.GetString (str);
		}

		public string GetPluralString (string singular, string plural, int number)
		{
			return GettextCatalog.GetPluralString (singular, plural, number);
		}
	}
}
