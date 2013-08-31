using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;


namespace CSSParserAntlr
{
    class Program
    {
        public static void Main(string[] args)
        {
            Stream inputStream = Console.OpenStandardInput();

            //ANTLRStringStream input = new ANTLRStringStream(content.ReadToEnd());
            string s = @"H1 {color: white; background: teal; FONT-FAMILY: arial, helvetica, lucida-sans, sans-serif; FONT-SIZE: 18pt; FONT-STYLE: normal; FONT-VARIANT: normal;";//inputStream.ToString();
            AntlrInputStream input = new AntlrInputStream(s);
            CSSLexer lexer = new CSSLexer(input);

            CommonTokenStream tokens = new CommonTokenStream(lexer);

            //tokens.hi
            CSSParser parser = new CSSParser(tokens);
            Class1 y = new Class1();
            //Class2 z = new Class2();

            //lexer.AddErrorListener(z);
            parser.AddErrorListener(y);
            var x = parser.styleSheet();

            var bodySetContext = x.bodylist().bodyset();
            foreach (var item in bodySetContext)
            {
                Console.WriteLine("Start Line" + item.Start.Column);
                Console.WriteLine("Stop Line" + item.Stop.Line);
                Console.WriteLine(item.GetText().Split(new char[]{'{'}).GetValue(0).ToString() +"{...");
            }

            Console.Read();
        }
    }
}
