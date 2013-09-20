[CSSBinding]
	This is a summerization of CSSBinding Project. This project will cover CSS support for MonoDevelop.
	
	[Implimented Features & Status]
	
	1. CSS Parsing
	2. Syntax Coloring
	3. Css Folding
	4. CSS Binding
	5. Error Desplaying[In Progress]
	6. Auto Code Completion [Not Done]

	This CSS binding uses ANTLR parser generator to generate the parser. You can change/Upgrade the CSS grammer given is the procedure
	There are 2 projects.
	- Parser stand alone project can be found in /ParserProject

		- Antlr 3 version parser generator project[/ParserProject/CSSBinding]
		- Antlr 4 version parser generator project[/ParserProject/CSSParserAntlr]

	What you have to edit grammar and recreate parser
	- in Versin 4 Project(This is the currently used in the project):
		- In this project grammar is changed from version 3 to 4 [CSS.g4]. This where the lexer rules and parser rules are written.
		- Uload the project before you build the project 
			add the snippet shown below 
				<PropertyGroup>
					<!-- Folder containing Antlr4BuildTasks.dll -->
					<Antlr4BuildTaskPath>$(ProjectDir)..\Reference\Antlr4</Antlr4BuildTaskPath>
					<!-- Path to the ANTLR Tool itself. -->
					<Antlr4ToolPath>$(ProjectDir)..\Reference\Antlr4\antlr4-csharp-4.0.1-SNAPSHOT-complete.jar</Antlr4ToolPath>
				</PropertyGroup>
				<Import Project="$(ProjectDir)..\Reference\Antlr4\Antlr4.targets" />
			  
			  in the "CSSParserAntlr.csproj" file after the line 
				<Import Project="$(MSBuildBinPath)\Microsoft.CSharp.targets" />
			
		- reload the project and build it.
		- you can find the generated parser and lexer class files in the obj/Resease folder. There are 6 generated classes.
			1. CSSBaseListener.cs
			2. CSSBaseVisitor.cs
			3. CSSLexer.cs
			4. CSSListener.cs
			5. CSSParser.cs
			6. CSSVisitor.cs
			
		- You can use them to the project class.
			NOTE: 
			- It is best to keep the same name space to the parser classes in the CSSBinding Project and the seperate parser project to avoid contentions.
			- If the IReadOnlyList keyword is not identified in Xamarin project, then replace it with IList keyword.
		- Now you have the working project after the grammer change.
		-
	- in Versin 3 Project:
	
		- In this project grammer is used in CSS.g3 file.
		- Uload the project before you build the project 
			add the snippet shown below 

			<PropertyGroup>   
				<!-- Folder containing AntlrBuildTask.dll -->   
				<AntlrBuildTaskPath>$(ProjectDir)..\Reference\Antlr</AntlrBuildTaskPath>   
				<!-- Path to the ANTLR Tool itself. -->   
				<AntlrToolPath>$(ProjectDir)..\Reference\Antlr\Antlr3.exe</AntlrToolPath>
			</PropertyGroup> 
			<Import Project="$(ProjectDir)..\Reference\Antlr\Antlr3.targets" />

			in the "CssParser.csproj" file after the line 
				<Import Project="$(MSBuildBinPath)\Microsoft.CSharp.targets" /> 

		- reload the project and build it.
		- you can find the generated parser and lexer class files in the obj/Resease folder.
		- You can use them as the parser classes in the project.
		- Now you have the working project after the grammer change.

		You can directly copy and pase the parser and lexer code to the CSSBinding project's parser and lexer classes to use the new parser in the project.
		
	