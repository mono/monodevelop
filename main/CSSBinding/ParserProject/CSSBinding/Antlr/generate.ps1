java -jar .\antlr-3.4-complete.jar ExCSS.g

copy-item -path ExCSSLexer.cs -Destination ..\ExCss\ExCSSLexer.cs -force
copy-item -path ExCSSParser.cs -Destination ..\ExCss\ExCSSParser.cs -force