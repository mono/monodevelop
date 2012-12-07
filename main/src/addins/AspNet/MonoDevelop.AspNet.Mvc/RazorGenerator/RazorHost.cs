using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Razor;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.WebPages;

namespace MonoDevelop.RazorGenerator
{
	delegate void RazorCodeTransformer (
		RazorHost host, CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace,
		CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod);

	class RazorHost : RazorEngineHost
	{
		private readonly RazorCodeTransformer[] _transformers;
		private readonly string _fullPath;
		private readonly CodeDomProvider _codeDomProvider;
		private readonly CodeGeneratorOptions _codeGeneratorOptions;
		private string _defaultClassName;

		public RazorHost(string fullPath, CodeDomProvider codeDomProvider = null,
		                 RazorCodeTransformer[] transformers = null, CodeGeneratorOptions codeGeneratorOptions = null)
			: base(RazorCodeLanguage.GetLanguageByExtension(".cshtml"))
		{
			if (fullPath == null)
			{
				throw new ArgumentNullException("fullPath");
			}
			_transformers = transformers;
			_fullPath = fullPath;
			_codeDomProvider = codeDomProvider ?? new Microsoft.CSharp.CSharpCodeProvider ();
			base.DefaultNamespace = "ASP";
			EnableLinePragmas = true;

			base.GeneratedClassContext = new GeneratedClassContext(
				executeMethodName: GeneratedClassContext.DefaultExecuteMethodName,
				writeMethodName: GeneratedClassContext.DefaultWriteMethodName,
				writeLiteralMethodName: GeneratedClassContext.DefaultWriteLiteralMethodName,
				writeToMethodName: "WriteTo",
				writeLiteralToMethodName: "WriteLiteralTo",
				templateTypeName: typeof(HelperResult).FullName,
				defineSectionMethodName: "DefineSection",
				beginContextMethodName: "BeginContext",
				endContextMethodName: "EndContext"
				)
			{
				ResolveUrlMethodName = "Href"
			};

			_codeGeneratorOptions = codeGeneratorOptions ?? new CodeGeneratorOptions () {
				// HACK: we use true, even though razor uses false, to work around a mono bug where it omits the 
				// line ending after "#line hidden", resulting in the unparseable "#line hiddenpublic"
				BlankLinesBetweenMembers = true,
				BracingStyle = "C",
				// matches Razor built-in settings
				IndentString = String.Empty,
			};
		}

		public CodeDomProvider CodeDomProvider {
			get { return _codeDomProvider; }
		}

		public CodeGeneratorOptions CodeGeneratorOptions {
			get { return _codeGeneratorOptions; }
		}

		public string FullPath
		{
			get { return _fullPath; }
		}

		public override string DefaultClassName
		{
			get
			{
				return _defaultClassName ?? GetClassName();
			}
			set
			{
				if (!String.Equals(value, "__CompiledTemplate", StringComparison.OrdinalIgnoreCase))
				{
					//  By default RazorEngineHost assigns the name __CompiledTemplate. We'll ignore this assignment
					_defaultClassName = value;
				}
			}
		}

		public Func<RazorHost,ParserBase> ParserFactory { get; set; }

		public RazorCodeGenerator CodeGenerator { get; set; }

		public bool EnableLinePragmas { get; set; }

		public string GenerateCode (out CompilerErrorCollection errors)
		{
			errors = new CompilerErrorCollection ();

			// Create the engine
			RazorTemplateEngine engine = new RazorTemplateEngine(this);

			// Generate code
			GeneratorResults results = null;
			try
			{
				Stream stream = File.OpenRead(_fullPath);
				using (var reader = new StreamReader(stream, Encoding.Default, detectEncodingFromByteOrderMarks: true))
				{
					results = engine.GenerateCode(reader, className: DefaultClassName, rootNamespace: DefaultNamespace, sourceFileName: _fullPath);
				}
			} catch (Exception e) {
				errors.Add (new CompilerError (FullPath, 1, 1, null, e.ToString ()));
				//Returning null signifies that generation has failed
				return null;
			}

			// Output errors
			foreach (RazorError error in results.ParserErrors) {
				errors.Add (new CompilerError (FullPath, error.Location.LineIndex + 1, error.Location.CharacterIndex + 1, null, error.Message));
			}

			try
			{
				using (StringWriter writer = new StringWriter()) {
					//Generate the code
					writer.WriteLine("#pragma warning disable 1591");
					_codeDomProvider.GenerateCodeFromCompileUnit(results.GeneratedCode, writer, _codeGeneratorOptions);
					writer.WriteLine("#pragma warning restore 1591");
					return writer.ToString();
				}
			} catch (Exception e) {
				errors.Add (new CompilerError (FullPath, 1, 1, null, e.ToString ()));
				//Returning null signifies that generation has failed
				return null;
			}
		}

		public override void PostProcessGeneratedCode(CodeGeneratorContext context)
		{
			if (_transformers == null) {
				return;
			}
			foreach (var t in _transformers) {
				t (this, context.CompileUnit, context.Namespace, context.GeneratedClass, context.TargetMethod);
			}
		}

		public override RazorCodeGenerator DecorateCodeGenerator(RazorCodeGenerator incomingCodeGenerator)
		{
			var codeGenerator = CodeGenerator ?? base.DecorateCodeGenerator(incomingCodeGenerator);
			codeGenerator.GenerateLinePragmas = EnableLinePragmas;
			return codeGenerator;
		}

		public override ParserBase DecorateCodeParser(ParserBase incomingCodeParser)
		{
			return ParserFactory != null? ParserFactory (this) : base.DecorateCodeParser(incomingCodeParser);
		}

		protected virtual string GetClassName()
		{
			string filename = Path.GetFileNameWithoutExtension(_fullPath);
			return ParserHelpers.SanitizeClassName(filename);
		}
	}
}