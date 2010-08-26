using System.Text;

namespace GitSharp.Core.Util.JavaHelper
{
    public static class Charset
    {
        public static Encoding forName(string encodingAlias)
        {
            Encoding encoder;

            if (encodingAlias == "euc_JP")
            {
                encodingAlias = "EUC-JP";   // Hacked as euc_JP is not valid from the IANA perspective (http://www.iana.org/assignments/character-sets)
                // See also http://tagunov.tripod.com/i18n/jdk11enc.html for further historical information
            }

            switch (encodingAlias.ToUpperInvariant())
            {
                case "UTF-8":
                    encoder = new UTF8Encoding(false, true);
                    break;

                default:
                    encoder = Encoding.GetEncoding(encodingAlias, new EncoderExceptionFallback(), new DecoderExceptionFallback());
                    break;
            }

            return encoder;
        }
    }
}