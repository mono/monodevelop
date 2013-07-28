This project is implimented as a Google summer of code 2013 project.

This CSS binding uses ANTLR parser generator to generate the parser. If you want to change/Upgrade the CSS grammer given is the procedure

What you have to do:
- Parser stand alone project can be found in "ParserProject" folder.
- In this project grammer is used in CSS.g3 file.
- load it to the project and you can change the grammer rules accordingly.
- Again uload the project and 
	add the snippet shown below 

	<PropertyGroup>   <!-- Folder containing AntlrBuildTask.dll -->   <AntlrBuildTaskPath>$(ProjectDir)..\Reference\Antlr</AntlrBuildTaskPath>   <!-- Path to the ANTLR Tool itself. -->   <AntlrToolPath>$(ProjectDir)..\Reference\Antlr\Antlr3.exe</AntlrToolPath> </PropertyGroup> <Import Project="$(ProjectDir)..\Reference\Antlr\Antlr3.targets" />

	in the "CssParser.csproj" file after the line 

	<Import Project="$(MSBuildBinPath)\Microsoft.CSharp.targets" /> 

- reload the project and build it.
- you can find the generated parser and lexer class files in the obj/Resease folder.

- You have to re- add them and again exclude the css.g3 file from the project and unload the project and again change comment out the added snippet added earlier.

- Now you have the working project after the grammer change.

You can directly copy and pase the parser and lexer code to the CSSBinding project's parser and lexer classes to use the new parser in the project.