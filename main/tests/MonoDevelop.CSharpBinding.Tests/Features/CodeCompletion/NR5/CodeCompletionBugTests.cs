//
// CodeCompletionBugTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory6.CSharp.Completion;
using NUnit.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using System.Collections.Immutable;
using MonoDevelop.Ide.CodeCompletion;

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion
{
	[TestFixture]
	class CodeCompletionBugTests : TestBase
	{
		internal static CompletionResult CreateProvider (string text, SourceCodeKind? sourceCodeKind = null)
		{
			return CreateProvider (text, false, null, null, sourceCodeKind);
		}
		
		internal static CompletionResult CreateCtrlSpaceProvider (string text, SourceCodeKind? sourceCodeKind = null)
		{
			return CreateProvider (text, true, null, null, sourceCodeKind);
		}
		
		internal static void CombinedProviderTest (string text, Action<CompletionResult> act)
		{
			var provider = CreateProvider (text);
			Assert.IsNotNull (provider, "provider == null");
			act (provider);
			
			provider = CreateCtrlSpaceProvider (text);
			Assert.IsNotNull (provider, "provider == null");
			act (provider);
		}

		public class TestFactory : ICompletionDataFactory
		{
			public class MyCompletionData : CompletionData
			{
				public MyCompletionData (string text) : base (text)
				{
				}
			}

			public class OverrideCompletionData : MyCompletionData
			{
				public int DeclarationBegin {
					get;
					set;
				}

				public OverrideCompletionData (string text, int declarationBegin) : base (text)
				{
					this.DeclarationBegin = declarationBegin;
				}

				public override bool IsOverload (CompletionData other)
				{
					return false;
				}
			}

			CompletionData ICompletionDataFactory.CreateFormatItemCompletionData (ICompletionDataKeyHandler keyHandler, string format, string description, object example)
			{
				return new CompletionData (format);
			}

			CompletionData ICompletionDataFactory.CreateKeywordCompletion (ICompletionDataKeyHandler keyHandler, string data)
			{
				return new CompletionData (data);
			}

			CompletionData ICompletionDataFactory.CreateXmlDocCompletionData(ICompletionDataKeyHandler keyHandler, string tag, string description, string tagInsertionText)
			{
				return new CompletionData (tag);
			}

			CompletionData ICompletionDataFactory.CreateGenericData(ICompletionDataKeyHandler keyHandler, string data, GenericDataType genericDataType)
			{
					return new CompletionData(data);
			}
			
			ISymbolCompletionData ICompletionDataFactory.CreateEnumMemberCompletionData (ICompletionDataKeyHandler keyHandler, ISymbol typeAlias, IFieldSymbol field)
			{
				return new SymbolCompletionData(field, field.ContainingType.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat) + "." + field.Name);
			}

			public CompletionData CreateNewOverrideCompletionData (ICompletionDataKeyHandler keyHandler, int declarationBegin, ITypeSymbol currentType, ISymbol m, bool afterKeyword)
			{
				return new OverrideCompletionData(afterKeyword ? m.Name : "override " + m.Name, declarationBegin);
			}

			public CompletionData CreatePartialCompletionData (ICompletionDataKeyHandler keyHandler, int declarationBegin, ITypeSymbol currentType, IMethodSymbol method, bool afterKeyword)
			{
				return new OverrideCompletionData(afterKeyword ? method.Name : "partial " + method.Name, declarationBegin);
			}

			public CompletionData CreateObjectCreation (ICompletionDataKeyHandler keyHandler, ITypeSymbol type, ISymbol symbol, int declarationBegin, bool afterKeyword)
			{
				return new CompletionData(afterKeyword ? symbol.ToDisplayString () : "new " + symbol.ToDisplayString ());
			}

			class SymbolCompletionData : ISymbolCompletionData
			{
				public SymbolCompletionData(ISymbol symbol) : base (symbol.Name)
				{
					this.Symbol = symbol;
				}

				public SymbolCompletionData(ISymbol symbol, string text) : base (text)
				{
					this.Symbol = symbol;
				}
			}

			ISymbolCompletionData ICompletionDataFactory.CreateSymbolCompletionData(ICompletionDataKeyHandler keyHandler, ISymbol symbol)
			{
				return new SymbolCompletionData(symbol);
			}

			ISymbolCompletionData ICompletionDataFactory.CreateSymbolCompletionData(ICompletionDataKeyHandler keyHandler, ISymbol symbol, string text)
			{
				return new SymbolCompletionData(symbol, text);
			}

			CompletionData ICompletionDataFactory.CreateCastCompletionData (ICompletionDataKeyHandler keyHandler, ISymbol member, SyntaxNode nodeToCast, ITypeSymbol targetType)
			{
				return new SymbolCompletionData(member);
			}

			CompletionData ICompletionDataFactory.CreateNewMethodDelegate(ICompletionDataKeyHandler keyHandler, ITypeSymbol delegateType, string varName, INamedTypeSymbol curType)
			{
				return new CompletionData (varName);
			}

			ISymbolCompletionData ICompletionDataFactory.CreateExistingMethodDelegate (ICompletionDataKeyHandler keyHandler, IMethodSymbol method)
			{
				return new SymbolCompletionData(method);
			}

			CompletionData ICompletionDataFactory.CreateAnonymousMethod(ICompletionDataKeyHandler keyHandler, string displayText, string description, string textBeforeCaret, string textAfterCaret)
			{
				return new CompletionData (displayText);
			}

			class TestCategory : CompletionCategory, IComparable, IComparable<CompletionCategory>
			{
				string text;

				public TestCategory (string text)
				{
					this.text = text;
				}

				public override int CompareTo (CompletionCategory other)
				{
					return text.CompareTo (other.DisplayText);
				}

				public override string ToString ()
				{
					return string.Format ("[TestCategory: text={0}]", text);
				}

				int IComparable.CompareTo (object obj)
				{
					return CompareTo ((CompletionCategory)obj);
                }
			}

			CompletionCategory ICompletionDataFactory.CreateCompletionDataCategory (ISymbol forSymbol)
			{
				return new TestCategory(forSymbol.ToDisplayString ());
			}


		}
//
//		public static void CreateCompilation (string parsedText, out IProjectContent pctx, out SyntaxTree syntaxTree, out CSharpUnresolvedFile unresolvedFile, bool expectErrors, params IUnresolvedAssembly[] references)
//		{
//			pctx = new CSharpProjectContent();
//			var refs = new List<IUnresolvedAssembly> { mscorlib.Value, systemCore.Value, systemAssembly.Value, systemXmlLinq.Value };
//			if (references != null)
//				refs.AddRange (references);
//			
//			pctx = pctx.AddAssemblyReferences(refs);
//			
//			syntaxTree = new CSharpParser().Parse(parsedText, "program.cs");
//			syntaxTree.Freeze();
//			if (!expectErrors && syntaxTree.Errors.Count > 0) {
//				Console.WriteLine ("----");
//				Console.WriteLine (parsedText);
//				Console.WriteLine ("----");
//				foreach (var error in syntaxTree.Errors)
//					Console.WriteLine (error.Message);
//				Assert.Fail ("Parse error.");
//			}
//
//			unresolvedFile = syntaxTree.ToTypeSystem();
//			pctx = pctx.AddOrUpdateFiles(unresolvedFile);
//		}
//
		internal static CompletionEngine CreateEngine(string text, out int cursorPosition, out SemanticModel semanticModel, out Document document, MetadataReference[] references, SourceCodeKind? sourceCodeKind = null)
		{
			string editorText;
			var selectionStart = text.IndexOf('$');
			cursorPosition = selectionStart;
			int endPos = text.IndexOf('$', cursorPosition + 1);
			if (endPos == -1) {
				editorText = cursorPosition < 0 ? text : text.Substring(0, cursorPosition) + text.Substring(cursorPosition + 1);
			} else {
				editorText = text.Substring(0, cursorPosition) + text.Substring(cursorPosition + 1, endPos - cursorPosition - 1) + text.Substring(endPos + 1);
				cursorPosition = endPos - 1; 
			}
//			var doc = new ReadOnlyDocument(editorText);
//
//			IProjectContent pctx;
//			SyntaxTree syntaxTree;
//			CSharpUnresolvedFile unresolvedFile;
//			CreateCompilation (parsedText, out pctx, out syntaxTree, out unresolvedFile, true, references);
//			var cmp = pctx.CreateCompilation();
//
//			var loc = cursorPosition > 0 ? doc.GetLocation(selectionStart) : new TextLocation (1, 1);
//
//			var rctx = new CSharpTypeResolveContext(cmp.MainAssembly);
//			rctx = rctx.WithUsingScope(unresolvedFile.GetUsingScope(loc).Resolve(cmp));
//
//			var curDef = unresolvedFile.GetInnermostTypeDefinition(loc);
//			if (curDef != null) {
//				var resolvedDef = curDef.Resolve(rctx).GetDefinition();
//				rctx = rctx.WithCurrentTypeDefinition(resolvedDef);
//				var curMember = resolvedDef.Members.FirstOrDefault(m => m.Region.Begin <= loc && loc < m.BodyRegion.End);
//				if (curMember != null) {
//					rctx = rctx.WithCurrentMember(curMember);
//				}
//			}
//			var mb = new DefaultCompletionContextProvider(doc, unresolvedFile);
//			mb.AddSymbol ("TEST");
//			foreach (var sym in syntaxTree.ConditionalSymbols) {
//				mb.AddSymbol(sym);
//			}

			var workspace = new InspectionActionTestBase.TestWorkspace ();

			var projectId  = ProjectId.CreateNewId();
			var documentId = DocumentId.CreateNewId(projectId);

			workspace.Open(ProjectInfo.Create(
						projectId,
						VersionStamp.Create(),
						"TestProject",
						"TestProject",
						LanguageNames.CSharp,
						null,
						null,
						new CSharpCompilationOptions (
							OutputKind.DynamicallyLinkedLibrary,
							false,
							"TestProject.dll",
							"",
							"Script",
							null,
							OptimizationLevel.Debug,
							false,
							false
						),
						new CSharpParseOptions (
							LanguageVersion.CSharp6,
							DocumentationMode.Parse,
							sourceCodeKind.HasValue ? sourceCodeKind.Value : SourceCodeKind.Regular,
							ImmutableArray.Create("DEBUG", "TEST")
						),
						new [] {
							DocumentInfo.Create(
								documentId,
								"a.cs",
								null,
								SourceCodeKind.Regular,
								TextLoader.From(TextAndVersion.Create(SourceText.From(editorText), VersionStamp.Create())) 
							)
						},
						null,
						InspectionActionTestBase.DefaultMetadataReferences
					)
			);

			var engine = new CompletionEngine(workspace, new TestFactory ());
			var project = workspace.CurrentSolution.GetProject(projectId);
			Compilation compilation;
			try {
				compilation = project.GetCompilationAsync().Result;
				var provider = workspace.Services.GetLanguageServices(LanguageNames.CSharp);
				var factory = new CSharpCodeGenerationServiceFactory ();
				var languageService = factory.CreateLanguageService (provider);
				var service = languageService as ICodeGenerationService;

				var ts = compilation.GetTypeSymbol("System", "Object", 0);
				foreach (var member in ts.GetMembers ()) {
					var method = member as IMethodSymbol;
					if (method == null)
						continue;
					service.CreateMethodDeclaration(method, CodeGenerationDestination.Unspecified, new CodeGenerationOptions());
				}


			} catch (AggregateException e) {
				Console.WriteLine(e.InnerException);
				foreach (var inner in e.InnerExceptions)
					Console.WriteLine("----" + inner);
				Assert.Fail("Error while creating compilation. See output for details."); 
			}
//			if (!workspace.TryApplyChanges(workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(editorText)))) {
//				Assert.Fail();
//			}
			document = workspace.CurrentSolution.GetDocument(documentId);
			semanticModel = document.GetSemanticModelAsync().Result;


//			engine.AutomaticallyAddImports = true;
//			engine.EolMarker = Environment.NewLine;
//			engine.FormattingPolicy = FormattingOptionsFactory.CreateMono();
			return engine;
		}
		
		internal static CompletionResult CreateProvider(string text, bool isCtrlSpace, Action<CompletionEngine> engineCallback, MetadataReference[] references, SourceCodeKind? sourceCodeKind = null)
		{
			int cursorPosition;
			SemanticModel semanticModel;
			Document document;
			var idx = text.IndexOf("$$");
			if (idx >= 0) {
				text = text.Substring(0, idx) + text.Substring(idx + 1);
			}
			var engine = CreateEngine(text, out cursorPosition, out semanticModel, out document, references, sourceCodeKind);
			if (engineCallback != null)
				engineCallback(engine);
			char triggerChar = cursorPosition > 0 ? document.GetTextAsync().Result [cursorPosition - 1] : '\0';

			try {
				var task = engine.GetCompletionDataAsync (new CompletionContext (document, cursorPosition, semanticModel),  new CompletionTriggerInfo (isCtrlSpace ? CompletionTriggerReason.CompletionCommand : CompletionTriggerReason.CharTyped, triggerChar));
				task.Wait ();
				return task.Result;
			} catch (Exception e) {
				Assert.Fail (e.ToString ());
			}
			return CompletionResult.Empty;
		}

		internal static CompletionResult CreateProvider(string text, bool isCtrlSpace, params MetadataReference[] references)
		{
			return CreateProvider(text, isCtrlSpace, null, references);
		}

//		static Tuple<ReadOnlyDocument, CSharpCompletionEngine> GetContent(string text, SyntaxTree syntaxTree)
//		{
//			var doc = new ReadOnlyDocument(text);
//			IProjectContent pctx = new CSharpProjectContent();
//			pctx = pctx.AddAssemblyReferences(new [] { mscorlib.Value, systemAssembly.Value, systemCore.Value, systemXmlLinq.Value });
//			var unresolvedFile = syntaxTree.ToTypeSystem();
//			
//			pctx = pctx.AddOrUpdateFiles(unresolvedFile);
//			var cmp = pctx.CreateCompilation();
//			
//			var mb = new DefaultCompletionContextProvider(doc, unresolvedFile);
//			var engine = new CSharpCompletionEngine (doc, mb, new TestFactory (new CSharpResolver (new CSharpTypeResolveContext (cmp.MainAssembly))), pctx, new CSharpTypeResolveContext (cmp.MainAssembly));
//			engine.EolMarker = Environment.NewLine;
//			engine.FormattingPolicy = FormattingOptionsFactory.CreateMono ();
//			return Tuple.Create (doc, engine);
//		}
//		
//		static CompletionResult CreateProvider (CSharpCompletionEngine engine, IDocument doc, TextLocation loc)
//		{
//			var cursorPosition = doc.GetOffset (loc);
//			
//			var data = engine.GetCompletionData (cursorPosition, true);
//			
//			return new CompletionResult {
//				Data = data,
//				AutoCompleteEmptyMatch = engine.AutoCompleteEmptyMatch,
//				AutoSelect = engine.AutoSelect,
//				DefaultCompletionString = engine.DefaultCompletionString
//			};
//		}
//		
		internal static void CheckObjectMembers (CompletionResult provider)
		{
			Assert.IsNotNull (provider.Find ("Equals"), "Method 'System.Object.Equals' not found.");
			Assert.IsNotNull (provider.Find ("GetHashCode"), "Method 'System.Object.GetHashCode' not found.");
			Assert.IsNotNull (provider.Find ("GetType"), "Method 'System.Object.GetType' not found.");
			Assert.IsNotNull (provider.Find ("ToString"), "Method 'System.Object.ToString' not found.");
		}
		
		internal static void CheckProtectedObjectMembers (CompletionResult provider)
		{
			CheckObjectMembers (provider);
			Assert.IsNotNull (provider.Find ("MemberwiseClone"), "Method 'System.Object.MemberwiseClone' not found.");
		}
		
		internal static void CheckStaticObjectMembers (CompletionResult provider)
		{
			Assert.IsNotNull (provider.Find ("Equals"), "Method 'System.Object.Equals' not found.");
			Assert.IsNotNull (provider.Find ("ReferenceEquals"), "Method 'System.Object.ReferenceEquals' not found.");
		}
		
//		class TestLocVisitor : DepthFirstAstVisitor
//		{
//			public List<Tuple<TextLocation, string>> Output = new List<Tuple<TextLocation, string>> ();
//			
//			public override void VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression)
//			{
//				Output.Add (Tuple.Create (memberReferenceExpression.MemberNameToken.StartLocation, memberReferenceExpression.MemberName));
//			}
//			
//			public override void VisitIdentifierExpression (IdentifierExpression identifierExpression)
//			{
//				Output.Add (Tuple.Create (identifierExpression.StartLocation, identifierExpression.Identifier));
//			}
//		}
		
//		[Ignore("TODO")]
//		[Test]
//		public void TestLoadAllTests ()
//		{
//			int found = 0;
//			int missing = 0;
//			int exceptions = 0;
//			int i = 0;
//			foreach (var file in Directory.EnumerateFiles ("/Users/mike/work/mono/mcs/tests", "*.cs")) {
//				if (i++ > 2)
//					break;
//				if (i <= 2)
//					continue;
//				var text = File.ReadAllText (file, Encoding.Default);
//				try {
//					var unit = new CSharpParser ().Parse (text, file);
//					
//					var cnt = GetContent (text, unit);
//					
//					var visitor = new TestLocVisitor ();
//					unit.AcceptVisitor (visitor);
//					foreach (var loc in visitor.Output) {
//						var provider = CreateProvider (cnt.Item2, cnt.Item1, loc.Item1);
//						if (provider.Find (loc.Item2) != null) {
//							found++;
//						} else {
//							missing++;
//						}
//					}
//				} catch (Exception e) {
//					Console.WriteLine ("Exception in:" + file  + "/" + e);
//					exceptions++;
//				}
//			}
//			Console.WriteLine ("Found:" + found);
//			Console.WriteLine ("Missing:" + missing);
//			Console.WriteLine ("Exceptions:" + exceptions);
//			if (missing > 0)
//				Assert.Fail ();
//		}

		[Test]
		public void TestSimpleCodeCompletion ()
		{
			CompletionResult provider = CreateProvider (
@"class Test { public void TM1 () {} public void TM2 () {} public int TF1; }
class CCTest {
void TestMethod ()
{
	Test t;
	$t.$
}
}
");
			Assert.IsNotNull (provider);
			Assert.AreEqual (7, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("TM1"));
			Assert.IsNotNull (provider.Find ("TM2"));
			Assert.IsNotNull (provider.Find ("TF1"));
		}

		[Test]
		public void TestSimpleInterfaceCodeCompletion ()
		{
			CompletionResult provider = CreateProvider (
@"interface ITest { void TM1 (); void TM2 (); int TF1 { get; } }
class CCTest {
void TestMethod ()
{
	ITest t;
	$t.$
}
}
");
			Assert.IsNotNull (provider);
			Assert.AreEqual (7, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("TM1"));
			Assert.IsNotNull (provider.Find ("TM2"));
			Assert.IsNotNull (provider.Find ("TF1"));
		}

		/// <summary>
		/// Bug 399695 - Code completion not working with an enum in a different namespace
		/// </summary>
		[Test]
		public void TestBug399695 ()
		{
			CompletionResult provider = CreateProvider (
@"namespace Other { enum TheEnum { One, Two } }
namespace ThisOne { 
        public class Test {
                public Other.TheEnum TheEnum {
                        set { }
                }

                public void TestMethod () {
                        $TheEnum = $
                }
        }
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("Other.TheEnum"), "Other.TheEnum not found.");
		}
		
		[Test]
		public void TestInnerEnum ()
		{
			var provider = CreateProvider (
@"class Other { 
	public enum TheEnum { One, Two }
	public Other (TheEnum e) { }
}

public class Test {
	public void TestMethod () {
		$new Other (O$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("Other.TheEnum"), "'Other.TheEnum' not found.");
		}

		
		
		/// <summary>
		/// Bug 318834 - autocompletion kicks in when inputting decimals
		/// </summary>
		[Test]
		public void TestBug318834 ()
		{
			CompletionResult provider = CreateProvider (
@"class T
{
        static void Main ()
        {
                $decimal foo = 0.$
        }
}

");
			Assert.IsFalse(provider.AutoSelect);
		}

		[Ignore("FixMe")]
		[Test]
		public void TestBug318834CaseB ()
		{
			CompletionResult provider = CreateProvider (
@"class T
{
        static void Main ()
        {
                $decimal foo = 0.0.$
        }
}

");
			Assert.IsNotNull (provider);
			Assert.IsTrue(provider.AutoSelect);
		}

		/// <summary>
		/// Bug 321306 - Code completion doesn't recognize child namespaces
		/// </summary>
		[Test]
		public void TestBug321306 ()
		{
			CompletionResult provider = CreateProvider (
@"namespace a
{
	namespace b
	{
		public class c
		{
			public static int hi;
		}
	}
	
	public class d
	{
		public d ()
		{
			$b.$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("c"), "class 'c' not found.");
		}

		/// <summary>
		/// Bug 322089 - Code completion for indexer
		/// </summary>
		[Test]
		public void TestBug322089 ()
		{
			CompletionResult provider = CreateProvider (
@"class AClass
{
	public int AField;
	public int BField;
}

class Test
{
	public void TestMethod ()
	{
		AClass[] list = new AClass[0];
		$list[0].$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			for (int i = 0; i < provider.Count; i++) {
				var varname = provider [i];
				Console.WriteLine (varname.DisplayText);
			}
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("AField"), "field 'AField' not found.");
			Assert.IsNotNull (provider.Find ("BField"), "field 'BField' not found.");
		}
		
		/// <summary>
		/// Bug 323283 - Code completion for indexers offered by generic types (generics)
		/// </summary>
		[Test]
		public void TestBug323283 ()
		{
			CompletionResult provider = CreateProvider (
@"class AClass
{
	public int AField;
	public int BField;
}

class MyClass<T>
{
	public T this[int i] {
		get {
			return default (T);
		}
	}
}

class Test
{
	public void TestMethod ()
	{
		MyClass<AClass> list = new MyClass<AClass> ();
		$list[0].$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("AField"), "field 'AField' not found.");
			Assert.IsNotNull (provider.Find ("BField"), "field 'BField' not found.");
		}

		/// <summary>
		/// Bug 323317 - Code completion not working just after a constructor
		/// </summary>
		[Test]
		public void TestBug323317 ()
		{
			CompletionResult provider = CreateProvider (
@"class AClass
{
	public int AField;
	public int BField;
}

class Test
{
	public void TestMethod ()
	{
		$new AClass().$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("AField"), "field 'AField' not found.");
			Assert.IsNotNull (provider.Find ("BField"), "field 'BField' not found.");
		}
		
		/// <summary>
		/// Bug 325509 - Inaccessible methods displayed in autocomplete
		/// </summary>
		[Test]
		public void TestBug325509 ()
		{
			CompletionResult provider = CreateProvider (
@"class AClass
{
	public int A;
	public int B;
	
	protected int C;
	int D;
}

class Test
{
	public void TestMethod ()
	{
		AClass a;
		$a.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("A"), "field 'A' not found.");
			Assert.IsNotNull (provider.Find ("B"), "field 'B' not found.");
			Assert.IsNull (provider.Find ("C"), "field 'C' found, but shouldn't.");
			Assert.IsNull (provider.Find ("D"), "field 'D' found, but shouldn't.");
		}

		/// <summary>
		/// Bug 338392 - MD tries to use types when declaring namespace
		/// </summary>
		[Test]
		public void TestBug338392 ()
		{
			CompletionResult provider = CreateProvider (
@"namespace A
{
        class C
        {
        }
}

$namespace A.$
");
			if (provider != null)
				Assert.AreEqual (0, provider.Count);
		}

		/// <summary>
		/// Bug 427284 - Code Completion: class list shows the full name of classes
		/// </summary>
		[Test]
		public void TestBug427284 ()
		{
			CompletionResult provider = CreateProvider (
@"namespace TestNamespace
{
        class Test
        {
        }
}
class TestClass
{
	void Method ()
	{
		$TestNamespace.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found.");
		}

		/// <summary>
		/// Bug 427294 - Code Completion: completion on values returned by methods doesn't work
		/// </summary>
		[Test]
		public void TestBug427294 ()
		{
			CompletionResult provider = CreateProvider (
@"class TestClass
{
	public TestClass GetTestClass ()
	{
	}
}

class Test
{
	public void TestMethod ()
	{
		TestClass a;
		$a.GetTestClass ().$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (5, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("GetTestClass"), "method 'GetTestClass' not found.");
		}
		
		/// <summary>
		/// Bug 405000 - Namespace alias qualifier operator (::) does not trigger code completion
		/// </summary>
		[Test]
		public void TestBug405000 ()
		{
			CompletionResult provider = CreateProvider (
@"namespace A {
	class Test
	{
	}
}

namespace B {
	using foo = A;
	class C
	{
		public static void Main ()
		{
			$foo::$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found.");
		}
		
		/// <summary>
		/// Bug 427649 - Code Completion: protected methods shown in code completion
		/// </summary>
		[Test]
		public void TestBug427649 ()
		{
			CompletionResult provider = CreateProvider (
@"class BaseClass
{
	protected void ProtecedMember ()
	{
	}
}

class C : BaseClass
{
	public static void Main ()
	{
		BaseClass bc;
		$bc.$
	}
}
");
			// protected members should not be displayed in this case.
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (4, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
		}
		
		/// <summary>
		/// Bug 427734 - Code Completion issues with enums
		/// </summary>
		[Test]
		public void TestBug427734A ()
		{
			CompletionResult provider = CreateProvider (
@"public class Test
{
	public enum SomeEnum { a,b }
	
	public void Run ()
	{
		$Test.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (3, provider.Count);
			CodeCompletionBugTests.CheckStaticObjectMembers (provider); // 2 from System.Object
			Assert.IsNotNull (provider.Find ("SomeEnum"), "enum 'SomeEnum' not found.");
		}
		
		/// <summary>
		/// Bug 427734 - Code Completion issues with enums
		/// </summary>
		[Test]
		public void TestBug427734B ()
		{
			CompletionResult provider = CreateProvider (
@"public class Test
{
	public enum SomeEnum { a,b }
	
	public void Run ()
	{
		$SomeEnum.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("a"), "enum member 'a' not found.");
			Assert.IsNotNull (provider.Find ("b"), "enum member 'b' not found.");
		}
		
		/// <summary>
		/// Bug 431764 - Completion doesn't work in properties
		/// </summary>
		[Test]
		public void TestBug431764 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"public class Test
{
	int number;
	public int Number {
		set { $this.number = $ }
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsTrue (provider.Count > 0, "provider should not be empty.");
			Assert.IsNotNull (provider.Find ("value"), "Should contain 'value'");
		}
		
		/// <summary>
		/// Bug 431797 - Code completion showing invalid options
		/// </summary>
		[Test]
		public void TestBug431797A ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
				@"public class Test
{
	private List<string> strings;
	$public $
}");

			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("strings"), "should not contain 'strings'");
					}
		
		/// <summary>
		/// Bug 431797 - Code completion showing invalid options
		/// </summary>
		[Test]
		public void TestBug431797B ()
		{
			CompletionResult provider = CreateProvider (
@"public class Test
{
	public delegate string [] AutoCompleteHandler (string text, int pos);
	public void Method ()
	{
		Test t = new Test ();
		$t.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("AutoCompleteHandler"), "should not contain 'AutoCompleteHandler' delegate");
		}
		
		/// <summary>
		/// Bug 432681 - Incorrect completion in nested classes
		/// </summary>
		[Test]
		public void TestBug432681 ()
		{
			CompletionResult provider = CreateProvider (
@"

class C {
        public class D
        {
        }

        public void Method ()
        {
                $C.D c = new $
        }
}");
			Assert.IsNotNull (provider, "provider not found.");
			// the correct string is handled at display level (D in that case)
			Assert.AreEqual ("C.D", provider.DefaultCompletionString, "Completion string is incorrect");
		}
		
		[Test]
		public void TestGenericObjectCreation ()
		{
			CompletionResult provider = CreateProvider (
@"
class List<T>
{
}
class Test{
	public void Method ()
	{
		$List<int> i = new $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsTrue (provider.Find ("List<int>") != null, "List<int> not found");
		}
		
		/// <summary>
		/// Bug 431803 - Autocomplete not giving any options
		/// </summary>
		[Test]
		public void TestBug431803 ()
		{
			CompletionResult provider = CreateProvider (
@"public class Test
{
	public string[] GetStrings ()
	{
		$return new $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("string"), "type string not found.");
		}

		/// <summary>
		/// Bug 434770 - No autocomplete on array types
		/// </summary>
		[Test]
		public void TestBug434770 ()
		{
			CompletionResult provider = CreateProvider (
@"
public class Test
{
	public void AMethod ()
	{
		byte[] buffer = new byte[1024];
		$buffer.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Length"), "property 'Length' not found.");
		}
		
		/// <summary>
		/// Bug 439601 - Intellisense Broken For Partial Classes
		/// </summary>
		[Test]
		public void TestBug439601 ()
		{
			CompletionResult provider = CreateProvider (
@"
namespace MyNamespace
{
	partial class FormMain
	{
		private void Foo()
		{
			Bar();
		}
		
		private void Blah()
		{
			Foo();
		}
	}
}

namespace MyNamespace
{
	public partial class FormMain
	{
		public FormMain()
		{
		}
		
		private void Bar()
		{
			$this.$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
			Assert.IsNotNull (provider.Find ("Blah"), "method 'Blah' not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 1932 - [new resolver] fields don't show up unless prefixed with 'this.'
		/// </summary>
		[Test]
		public void TestBug1932 ()
		{
			CombinedProviderTest (
@"
namespace MyNamespace
{
	partial class FormMain
	{
		int field1;
		string field2;
	}
}

namespace MyNamespace
{
	public partial class FormMain
	{
		private void Bar()
		{
			$f$
		}
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("field1"), "field 'field1' not found.");
				Assert.IsNotNull (provider.Find ("field2"), "field 'field2' not found.");
			});
		}
		
		/// <summary>
		/// Bug 1967 - [new resolver] Intellisense doesn't work
		/// </summary>
		[Test]
		public void TestBug1967 ()
		{
			CombinedProviderTest (
@"
namespace MyNamespace
{
	partial class FormMain
	{
		FormMain field1;
		string field2;
	}
}

namespace MyNamespace
{
	public partial class FormMain
	{
		private void Bar()
		{
			$field1.$
		}
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("field1"), "field 'field1' not found.");
				Assert.IsNotNull (provider.Find ("field2"), "field 'field2' not found.");
			});
		}
		
		
		/// <summary>
		/// Bug 432434 - Code completion doesn't work with subclasses
		/// </summary>
		[Test]
		public void TestBug432434 ()
		{
			CompletionResult provider = CreateProvider (

@"public class Test
{
	public class Inner
	{
		public void Inner1 ()
		{
		}
		
		public void Inner2 ()
		{
		}
	}
	
	public void Run ()
	{
		Inner inner = new Inner ();
		$inner.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Inner1"), "Method inner1 not found.");
			Assert.IsNotNull (provider.Find ("Inner2"), "Method inner2 not found.");
		}

		/// <summary>
		/// Bug 432434A - Code completion doesn't work with subclasses
		/// </summary>
		[Test]
		public void TestBug432434A ()
		{
			CompletionResult provider = CreateProvider (

@"    public class E
        {
                public class Inner
                {
                        public void Method ()
                        {
                                Inner inner = new Inner();
                                $inner.$
                        }
                }
        }
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Method"), "Method 'Method' not found.");
		}
		
		/// <summary>
		/// Bug 432434B - Code completion doesn't work with subclasses
		/// </summary>
		[Ignore("FixMe")]
		[Test]
		public void TestBug432434B ()
		{
			CompletionResult provider = CreateProvider (

@"  public class E
        {
                public class Inner
                {
                        public class ReallyInner $: $
                        {

                        }
                }
        }
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("E"), "Class 'E' not found.");
			Assert.IsNotNull (provider.Find ("Inner"), "Class 'Inner' not found.");
			Assert.IsNull (provider.Find ("ReallyInner"), "Class 'ReallyInner' found, but shouldn't.");
		}
		

		/// <summary>
		/// Bug 436705 - code completion for constructors does not handle class name collisions properly
		/// </summary>
		[Test]
		public void TestBug436705 ()
		{
			CompletionResult provider = CreateProvider (
@"namespace System.Drawing {
	public class Point
	{
	}
}

public class Point
{
}

class C {

        public void Method ()
        {
                $System.Drawing.Point p = new $
        }
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual ("System.Drawing.Point", provider.DefaultCompletionString, "Completion string is incorrect");
		}
		
		/// <summary>
		/// Bug 439963 - Lacking members in code completion
		/// </summary>
		[Test]
		public void TestBug439963 ()
		{
			CompletionResult provider = CreateProvider (
@"public class StaticTest
{
	public void Test1()
	{}
	public void Test2()
	{}
	
	public static StaticTest GetObject ()
	{
	}
}

public class Test
{
	public void TestMethod ()
	{
		$StaticTest.GetObject ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Test1"), "Method 'Test1' not found.");
			Assert.IsNotNull (provider.Find ("Test2"), "Method 'Test2' not found.");
			Assert.IsNull (provider.Find ("GetObject"), "Method 'GetObject' found, but shouldn't.");
		}

		/// <summary>
		/// Bug 441671 - Finalisers show up in code completion
		/// </summary>
		[Test]
		public void TestBug441671 ()
		{
			CompletionResult provider = CreateProvider (
@"class TestClass
{
	public TestClass (int i)
	{
	}
	public void TestMethod ()
	{
	}
	public ~TestClass ()
	{
	}
}

class AClass
{
	void AMethod ()
	{
		TestClass c;
		$c.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			//Assert.AreEqual (5, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNull (provider.Find (".dtor"), "destructor found - but shouldn't.");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found.");
		}
		
		/// <summary>
		/// Bug 444110 - Code completion doesn't activate
		/// </summary>
		[Test]
		public void TestBug444110 ()
		{
			CompletionResult provider = CreateProvider (
@"using System;
using System.Collections.Generic;

namespace System.Collections.Generic {
	
	public class TemplateClass<T>
	{
		public T TestField;
	}
}

namespace CCTests
{
	
	public class Test
	{
		public TemplateClass<int> TemplateClass { get; set; }
	}
	
	class MainClass
	{
		public static void Main(string[] args)
		{
			Test t = new Test();
			$t.TemplateClass.$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (5, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("TestField"), "field 'TestField' not found.");
		}
		
		/// <summary>
		/// Bug 460234 - Invalid options shown when typing 'override'
		/// </summary>
		[Test]
		public void TestBug460234 ()
		{
			CompletionResult provider = CreateProvider (
@"
public class TestMe : System.Object
{
	$override $

	public override string ToString ()
	{
		return null; 
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			//Assert.AreEqual (2, provider.Count);
			Assert.IsNull (provider.Find ("Finalize"), "method 'Finalize' found, but shouldn't.");
			Assert.IsNotNull (provider.Find ("GetHashCode"), "method 'GetHashCode' not found.");
			Assert.IsNotNull (provider.Find ("Equals"), "method 'Equals' not found.");
		}
		
		/// <summary>
		/// Bug 457003 - code completion shows variables out of scope
		/// </summary>
		[Test]
		public void TestBug457003 ()
		{
			CompletionResult provider = CreateProvider (
@"
class A
{
	public void Test ()
	{
		if (true) {
			A st = null;
		}
		
		if (true) {
			int i = 0;
			$st.$
		}
	}
}
");
			if (provider != null)
				Assert.IsTrue (provider.Count == 0, "variable 'st' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 457237 - code completion doesn't show static methods when setting global variable
		/// </summary>
		[Test]
		public void TestBug457237 ()
		{
			CompletionResult provider = CreateProvider (
@"
class Test
{
	public static double Val = 0.5;
}

class Test2
{
	$double dd = Test.$
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Val"), "field 'Val' not found.");
		}

		/// <summary>
		/// Bug 459682 - Static methods/properties don't show up in subclasses
		/// </summary>
		[Test]
		public void TestBug459682 ()
		{
			CompletionResult provider = CreateProvider (
@"public class BaseC
{
	public static int TESTER;
}
public class Child : BaseC
{
	public Child()
	{
		$Child.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TESTER"), "field 'TESTER' not found.");
		}
		/// <summary>
		/// Bug 466692 - Missing completion for return/break keywords after yield
		/// </summary>
		[Test]
		public void TestBug466692 ()
		{
			CompletionResult provider = CreateProvider (
@"using System.Collections.Generic;
public class TestMe 
{
	public IEnumerable<int> Test ()
	{
		$yield r$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (2, provider.Count);
			Assert.IsNotNull (provider.Find ("break"), "keyword 'break' not found");
			Assert.IsNotNull (provider.Find ("return"), "keyword 'return' not found");
		}
		
		/// <summary>
		/// Bug 467507 - No completion of base members inside explicit events
		/// </summary>
		[Test]
		public void TestBug467507 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"
using System;

class Test
{
	public void TestMe ()
	{
	}
	
	public event EventHandler TestEvent {
		add {
			$
		}
		remove {
			
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("TestMe"), "method 'TestMe' not found");
			Assert.IsNotNull (provider.Find ("value"), "keyword 'value' not found");
		}
		
		/// <summary>
		/// Bug 444643 - Extension methods don't show up on array types
		/// </summary>
		[Test]
		public void TestBug444643 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"
using System;
using System.Collections.Generic;

	static class ExtensionTest
	{
		public static bool TestExt<T> (this IList<T> list, T val)
		{
			return true;
		}
	}
	
	class MainClass
	{
		public static void Main(string[] args)
		{
			$args.$
		}
	}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TestExt"), "method 'TestExt' not found");
		}
		
		/// <summary>
		/// Bug 471935 - Code completion window not showing in MD1CustomDataItem.cs
		/// </summary>
		[Test]
		public void TestBug471935 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"
public class AClass
{
	public AClass Test ()
	{
		if (true) {
			AClass data;
			$data.$
			return data;
		} else if (false) {
			AClass data;
			return data;
		}
		return null;
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found");
		}
		
		/// <summary>
		/// Bug 471937 - Code completion of 'new' showing invorrect entries 
		/// </summary>
		[Test]
		public void TestBug471937()
		{
			CompletionResult provider = CreateCtrlSpaceProvider(
@"
class B
{
}

class A
{
	public void Test()
	{
		int i = 5;
		i += 5;
		$A a = new $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("A"), "class 'A' not found.");
			Assert.AreEqual ("A", provider.DefaultCompletionString);
//			Assert.IsNull (provider.Find ("B"), "class 'B' found, but shouldn'tj.");
		}
		
		/// <summary>
		/// Bug 2268 - Potential omission in code completion
		/// </summary>
		[Test]
		public void TestBug2268 ()
		{
			CombinedProviderTest (
@"
public class Outer
{
    static int value = 5;

    class Inner
    {
        void Method ()
        {
            $v$
        }
    }
}
", provider => Assert.IsNotNull(provider.Find("value"), "field 'value' not found."));
		}
		
		
		/// <summary>
		/// Bug 2295 - [New Resolver] 'new' completion doesn't select the correct class name 
		/// </summary>
		[Test]
		public void TestBug2295 ()
		{
			CombinedProviderTest (
@"
class A
{
	public void Test()
	{
		A a;
		$a = new $
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("A"), "class 'A' not found.");
				Assert.AreEqual ("A", provider.DefaultCompletionString);
			});
		}
		
		
	
		
		
		/// <summary>
		/// Bug 2061 - Typing 'new' in a method all does not offer valid completion
		/// </summary>
		[Test]
		public void TestBug2061 ()
		{
			CombinedProviderTest (
@"
class A
{
	void CallTest(A a)
	{
	}
	public void Test()
	{
		$CallTest(new $
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("A"), "class 'A' not found.");
				Assert.AreEqual ("A", provider.DefaultCompletionString);
			});
		}

		[Test]
		public void TestBug2061Case2 ()
		{
			CombinedProviderTest (
@"
class A
{
	void CallTest(int i, string s, A a)
	{
	}

	public void Test()
	{
		$CallTest(5, """", new $
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("A"), "class 'A' not found.");
				Assert.AreEqual ("A", provider.DefaultCompletionString);
			});
		}
		
		/// <summary>
		/// Bug 2788 - Locals do not show up inside the 'for' statement context
		/// </summary>
		[Test]
		public void TestBug2788 ()
		{
			CombinedProviderTest (
@"
class A
{
	public void Test()
	{
		
		var foo = new byte[100];
		$for (int i = 0; i < f$
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("foo"), "'foo' not found.");
				Assert.IsNotNull (provider.Find ("i"), "'i' not found.");
			});
		}
		
		/// <summary>
		/// Bug 2800 - Finalize is offered as a valid completion target
		/// </summary>
		[Test]
		public void TestBug2800 ()
		{
			CombinedProviderTest (
@"
class A
{
	public void Test()
	{
		$this.$
	}
}
", provider => Assert.IsNull(provider.Find("Finalize"), "'Finalize' found."));
		}
		
		[Test]
		public void TestBug2800B ()
		{
			CombinedProviderTest (
@"
class A
{
	$public override $
}
", provider => {
				foreach (var data in provider) {
					Console.WriteLine (data);
				}
				
				Assert.IsNotNull (provider.Find ("ToString"), "'ToString' not found.");
				Assert.IsNull (provider.Find ("Finalize"), "'Finalize' found.");
			});
		}
		[Test]
		public void TestOverrideCompletion ()
		{
			CombinedProviderTest (
@"using System;

class Base
{

	public virtual int Property { get;}
	public virtual int Method () { }
	public virtual event EventHandler Event;
	public virtual int this[int i] { get { } }
}


class A : Base
{
	$public override $
}
", provider => {
				Assert.IsNotNull (provider.Find ("Property"), "'Property' not found.");
				Assert.IsNotNull (provider.Find ("Method"), "'Method' not found.");
				Assert.IsNotNull (provider.Find ("Event"), "'Event' not found.");
				Assert.IsNotNull (provider.Find ("ToString"), "'Event' not found.");
				Assert.IsNotNull (provider.Find ("GetHashCode"), "'GetHashCode' not found.");
				Assert.IsNotNull (provider.Find ("Equals"), "'Equals' not found.");
				//Assert.AreEqual (7, provider.Count);
			});
		}
		
		
		/// <summary>
		/// Bug 3370 -MD ignores member hiding
		/// </summary>
		[Test]
		public void TestBug3370 ()
		{
			CombinedProviderTest (
@"
class A
{
	$public override $
}
", provider => {
				Assert.IsNotNull (provider.Find ("ToString"), "'ToString' not found.");
				Assert.IsNull (provider.Find ("Finalize"), "'Finalize' found.");
			});
		}
		
		/// <summary>
		/// Bug 2793 - op_Equality should not be offered in the completion list
		/// </summary>
		[Test]
		public void Test2793 ()
		{
			CombinedProviderTest (
@"
using System;

public class MyClass
{
    public class A
    {
        public event EventHandler MouseClick;
    }

    public class B : A
    {
        public new event EventHandler MouseClick;
    }

    public class C : B
    {
        public new void MouseClick ()
        {
        }
    }

    static public void Main ()
    {
        C myclass = new C ();
        $myclass.$
    }
}", provider => Assert.AreEqual(1, provider.Data.Count(c => c.DisplayText == "MouseClick")));
		}
		
		/// <summary>
		/// Bug 2798 - Unnecessary namespace qualification being prepended
		/// </summary>
		[Test]
		public void Test2798 ()
		{
			CombinedProviderTest (
@"
using System;

namespace Foobar
{
    class MainClass
    {
        public enum Foo
        {
            Value1,
            Value2
        }

        public class Test
        {
            Foo Foo {
                get; set;
            }

            public static void Method (Foo foo)
            {
                $if (foo == F$
            }
        }
    }
}
", 
				provider => {
					Assert.IsNull (provider.Find ("MainClass.Foo"), "'MainClass.Foo' found.");
					Assert.IsNotNull (provider.Find ("Foo"), "'Foo' not found.");
					Assert.IsNotNull (provider.Find ("MainClass.Foo.Value1"), "'Foo.Value1' not found.");
					Assert.IsNotNull (provider.Find ("MainClass.Foo.Value2"), "'Foo.Value2' not found.");
				}
			);
		}
		
		
		/// <summary>
		/// Bug 2799 - No completion offered when declaring fields in a class
		/// </summary>
		[Test]
		public void TestBug2799 ()
		{
			CombinedProviderTest (
@"namespace Foobar
{
    class MainClass
    {
        public enum Foo
        {
            Value1,
            Value2
        }
    }


    public class Second
    {
        $MainClass.$
    }
}

", provider => Assert.IsNotNull(provider.Find("Foo"), "'Foo' not found."));
		}
		
		/// <summary>
		/// Bug 3371 - MD intellisense ignores namespace aliases
		/// </summary>
		[Test]
		public void TestBug3371 ()
		{
			CombinedProviderTest (
@"namespace A
{
    using Base = B.Color;

    class Color
    {
        protected Base Base
        {
            get { return Base.Blue; }
        }

        protected Base NewBase {
            get {
                $return Base.$
            }
        }

        public static void Main ()
        {
        }
    }
}

namespace B
{
    public struct Color
    {
        public static Color Blue = new Color ();

        public static Color From (int i)
        {
            return new Color ();
        }
    }
}
", provider => {
				Assert.IsNotNull (provider.Find ("Blue"), "'Blue' not found.");
				Assert.IsNotNull (provider.Find ("From"), "'From' not found.");
			});
		}
		
		[Test]
		public void TestNewInConstructor ()
		{
			CombinedProviderTest (
@"
class CallTest
{
	public CallTest(int i, string s, A a)
	{

	}
}

class A
{


	public void Test()
	{
		$new CallTest(5, """", new $
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("A"), "class 'A' not found.");
				Assert.AreEqual ("A", provider.DefaultCompletionString);
			});
		}		
		
		/// <summary>
		/// Bug 473686 - Constants are not included in code completion
		/// </summary>
		[Test]
		public void TestBug473686 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"
class ATest
{
	const int TESTCONST = 0;

	static void Test()
	{
		$$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("TESTCONST"), "constant 'TESTCONST' not found.");
		}
		
		/// <summary>
		/// Bug 473849 - Classes with no visible constructor shouldn't appear in "new" completion
		/// </summary>
		[Test]
		public void TestBug473849 ()
		{
			CompletionResult provider = CreateProvider (
@"
class TestB
{
	protected TestB()
	{
	}
}

class TestC : TestB
{
	internal TestC ()
	{
	}
}

class TestD : TestB
{
	public TestD ()
	{
	}
}

class TestE : TestD
{
	protected TestE ()
	{
	}
}

class Test : TestB
{
	void TestMethod ()
	{
		$TestB test = new $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
		//	Assert.IsNull (provider.Find ("TestE"), "class 'TestE' found, but shouldn't.");
			Assert.IsNotNull (provider.Find ("TestD"), "class 'TestD' not found");
			Assert.IsNotNull (provider.Find ("TestC"), "class 'TestC' not found");
			Assert.IsNotNull (provider.Find ("TestB"), "class 'TestB' not found");
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found");
		}
		
		/// <summary>
		/// Bug 474199 - Code completion not working for a nested class
		/// </summary>
		[Test]
		public void TestBug474199A ()
		{
			CompletionResult provider = CreateProvider (
@"
public class InnerTest
{
	public class Inner
	{
		public void Test()
		{
		}
	}
}

public class ExtInner : InnerTest
{
}

class Test
{
	public void TestMethod ()
	{
		var inner = new ExtInner.Inner ();
		$inner.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found");
		}
		
		/// <summary>
		/// Bug 3438 - [New Resolver] Local var missing in code completion
		/// </summary>
		[Test]
		public void Test3438 ()
		{
			CombinedProviderTest (
@"
using System;
using System.Text;

class C
{
	void GetElementXml (int indent)
	{
		StringBuilder sb = new StringBuilder ();
		if (indent == 0)
			sb.Append ("" xmlns:android=\""http://schemas.android.com/apk/res/android\"""");
		
		if (indent != 0) {
			string data;
			$d$
		}
	}	
}", provider => Assert.IsNotNull(provider.Find("data"), "'data' not found."));
		}
		
		/// <summary>
		/// Bug 3436 - [New Resolver] Type missing in return type completion
		/// </summary>
		[Test]
		public void Test3436 ()
		{
			CombinedProviderTest (
@"
namespace A 
{
	public class SomeClass {}
}

namespace Foo 
{
	public partial class Bar {}
}

namespace Foo 
{
	using A;
	public partial class Bar {
		$S$
	}
}
", provider => Assert.IsNotNull(provider.Find("SomeClass"), "'SomeClass' not found."));
		}
		
		

		
		/// <summary>
		/// Bug 350862 - Autocomplete bug with enums
		/// </summary>
		[Test]
		public void TestBug350862 ()
		{
			CompletionResult provider = CreateProvider (
@"
public enum MyEnum {
	A,
	B,
	C
}

public class Test
{
	MyEnum item;
	public void Method (MyEnum val)
	{
		$item = $
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("val"), "parameter 'val' not found");
		}
		
		/// <summary>
		/// Bug 470954 - using System.Windows.Forms is not honored
		/// </summary>
		[Test]
		public void TestBug470954 ()
		{
			CompletionResult provider = CreateProvider (
@"
public class Control
{
	public MouseButtons MouseButtons { get; set; }
}

public enum MouseButtons {
	Left, Right
}

public class SomeControl : Control
{
	public void Run ()
	{
		$MouseButtons m = MouseButtons.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Left"), "enum 'Left' not found");
			Assert.IsNotNull (provider.Find ("Right"), "enum 'Right' not found");
		}
		
		/// <summary>
		/// Bug 470954 - using System.Windows.Forms is not honored
		/// </summary>
		[Test]
		public void TestBug470954_Bis ()
		{
			CompletionResult provider = CreateProvider (
@"
public class Control
{
	public string MouseButtons { get; set; }
}

public enum MouseButtons {
	Left, Right
}

public class SomeControl : Control
{
	public void Run ()
	{
		$int m = MouseButtons.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNull (provider.Find ("Left"), "enum 'Left' found");
			Assert.IsNull (provider.Find ("Right"), "enum 'Right' found");
		}
		
		
		
		/// <summary>
		/// Bug 487228 - No intellisense for implicit arrays
		/// </summary>
		[Test]
		public void TestBug487228 ()
		{
			CompletionResult provider = CreateProvider (
@"
public class Test
{
	public void Method ()
	{
		var v = new [] { new Test () };
		$v[0].$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Method"), "method 'Method' not found");
		}
		
		/// <summary>
		/// Bug 487218 - var does not work with arrays
		/// </summary>
		[Test]
		public void TestBug487218 ()
		{
			CompletionResult provider = CreateProvider (
@"
public class Test
{
	public void Method ()
	{
		var v = new Test[] { new Test () };
		$v[0].$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Method"), "method 'Method' not found");
		}
		
		/// <summary>
		/// Bug 487206 - Intellisense not working
		/// </summary>
		[Test]
		public void TestBug487206 ()
		{
			CompletionResult provider = CreateProvider (
@"
class CastByExample
{
	static T Cast<T> (object obj, T type)
	{
		return (T) obj;
	}
	
	static void Main ()
	{
		var typed = Cast (o, new { Foo = 5 });
		$typed.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Foo"), "property 'Foo' not found");
		}

		/// <summary>
		/// Bug 487203 - Extension methods not working
		/// </summary>
		[Test]
		public void TestBug487203 ()
		{
			CompletionResult provider = CreateProvider (
@"
using System;
using System.Linq;
using System.Collections.Generic;


class Program 
{
	public void Foo ()
	{
		Program[] prgs;
		foreach (var prg in (from Program p in prgs select p)) {
			$prg.$
		}
	}
}		
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found");
		}
		
		/// <summary>
		/// Bug 491020 - Wrong typeof intellisense
		/// </summary>
		[Test]
		public void TestBug491020 ()
		{
			CompletionResult provider = CreateProvider (
@"
public class EventClass<T>
{
	public class Inner {}
	public delegate void HookDelegate (T del);
	public void Method ()
	{}
}

public class Test
{
	public static void Main ()
	{
		$EventClass<int>.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Inner"), "class 'Inner' not found.");
			Assert.IsNotNull (provider.Find ("HookDelegate"), "delegate 'HookDelegate' not found.");
			Assert.IsNull (provider.Find ("Method"), "method 'Method' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 491020 - Wrong typeof intellisense
		/// It's a different case when the class is inside a namespace.
		/// </summary>
		[Test]
		public void TestBug491020B ()
		{
			CompletionResult provider = CreateProvider (
@"

namespace A {
	public class EventClass<T>
	{
		public class Inner {}
		public delegate void HookDelegate (T del);
		public void Method ()
		{}
	}
}

public class Test
{
	public static void Main ()
	{
		$A.EventClass<int>.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Inner"), "class 'Inner' not found.");
			Assert.IsNotNull (provider.Find ("HookDelegate"), "delegate 'HookDelegate' not found.");
			Assert.IsNull (provider.Find ("Method"), "method 'Method' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 491019 - No intellisense for recursive generics
		/// </summary>
		[Test]
		public void TestBug491019 ()
		{
			CompletionResult provider = CreateProvider (
@"
public abstract class NonGenericBase
{
	public abstract int this[int i] { get; }
}

public abstract class GenericBase<T> : NonGenericBase where T : GenericBase<T>
{
	T Instance { get { return default (T); } }

	public void Foo ()
	{
		$Instance.Instance.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Instance"), "property 'Instance' not found.");
			Assert.IsNull (provider.Find ("this"), "'this' found, but shouldn't.");
		}
		
		
				
		/// <summary>
		/// Bug 429034 - Class alias completion not working properly
		/// </summary>
		[Test]
		public void TestBug429034 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"
using Path = System.IO.Path;

class Test
{
	void Test ()
	{
		$$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Path"), "class 'Path' not found.");
		}	
		
		/// <summary>
		/// Bug 429034 - Class alias completion not working properly
		/// </summary>
		[Test]
		public void TestBug429034B ()
		{
			CompletionResult provider = CreateProvider (
@"
using Path = System.IO.Path;

class Test
{
	void Test ()
	{
		$Path.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("DirectorySeparatorChar"), "method 'PathTest' not found.");
		}
		
		[Test]
		public void TestInvalidCompletion ()
		{
			CompletionResult provider = CreateProvider (
@"
class TestClass
{
	public void TestMethod ()
	{
	}
}

class Test
{
	public void Foo ()
	{
		TestClass tc;
		$tc.garbage.$
	}
}
");
			if (provider != null)
				Assert.IsNull (provider.Find ("TestMethod"), "method 'TestMethod' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 510919 - Code completion does not show interface method when not using a local var 
		/// </summary>
		[Test]
		public void TestBug510919 ()
		{
			CompletionResult provider = CreateProvider (
@"
public class Foo : IFoo 
{
	public void Bar () { }
}

public interface IFoo 
{
	void Bar ();
}

public class Program
{
	static IFoo GiveMeFoo () 
	{
		return new Foo ();
	}

	static void Main ()
	{
		$GiveMeFoo ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
			
		
			
		/// <summary>
		/// Bug 538208 - Go to declaration not working over a generic method...
		/// </summary>
		[Test]
		public void TestBug538208 ()
		{
			// We've to test 2 expressions for this bug. Since there are 2 ways of accessing
			// members.
			// First: the identifier expression
			CompletionResult provider = CreateCtrlSpaceProvider (
@"
class MyClass
{
	public string Test { get; set; }
	
	T foo<T>(T arg)
	{
		return arg;
	}

	public void Main(string[] args)
	{
		var myObject = foo<MyClass>(new MyClass());
		$myObject.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Test"), "property 'Test' not found.");
			
			// now the member reference expression 
			provider = CreateCtrlSpaceProvider (
@"
class MyClass2
{
	public string Test { get; set; }
	
	T foo<T>(T arg)
	{
		return arg;
	}

	public void Main(string[] args)
	{
		var myObject = this.foo<MyClass2>(new MyClass2());
		$myObject.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Test"), "property 'Test' not found.");
		}
		
		/// <summary>
		/// Bug 542976 resolution problem
		/// </summary>
		[Test]
		public void TestBug542976 ()
		{
			CompletionResult provider = CreateProvider (
@"
class KeyValuePair<S, T>
{
	public S Key { get; set;}
	public T Value { get; set;}
}

class TestMe<T> : System.Collections.Generic.IEnumerable<T>
{
	public System.Collections.Generic.IEnumerator<T> GetEnumerator ()
	{
		throw new System.NotImplementedException();
	}

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
	{
		throw new System.NotImplementedException();
	}
}

namespace TestMe 
{
	class Bar
	{
		public int Field;
	}
	
	class Test
	{
		void Foo (TestMe<KeyValuePair<Bar, int>> things)
		{
			foreach (var thing in things) {
				$thing.Key.$
			}
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Field"), "field 'Field' not found.");
		}
		
		
		/// <summary>
		/// Bug 545189 - C# resolver bug
		/// </summary>
		[Test]
		public void TestBug545189A ()
		{
			CompletionResult provider = CreateProvider (
@"
public class A<T>
{
	public class B
	{
		public T field;
	}
}

public class Foo
{
	public void Bar ()
	{
		A<Foo>.B baz = new A<Foo>.B ();
		$baz.field.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 549864 - Intellisense does not work properly with expressions
		/// </summary>
		[Test]
		public void TestBug549864 ()
		{
			CompletionResult provider = CreateProvider (
@"
delegate T MyFunc<S, T> (S t);

class TestClass
{
	public string Value {
		get;
		set;
	}
	
	public static object GetProperty<TType> (MyFunc<TType, object> expression)
	{
		return null;
	}
	private static object ValueProperty = TestClass.GetProperty<TestClass> ($x => x.$);
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Value"), "property 'Value' not found.");
		}
		
		
		/// <summary>
		/// Bug 550185 - Intellisence for extension methods
		/// </summary>
		[Test]
		public void TestBug550185 ()
		{
			CompletionResult provider = CreateProvider (
@"
public interface IMyinterface<T> {
	T Foo ();
}

public static class ExtMethods {
	public static int MyCountMethod(this IMyinterface<string> i)
	{
		return 0;
	}
}

class TestClass
{
	void Test ()
	{
		IMyinterface<int> test;
		$test.$
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("MyCountMet2hod"), "method 'MyCountMethod' found, but shouldn't.");
		}
		
			
		/// <summary>
		/// Bug 553101 – Enum completion does not use type aliases
		/// </summary>
		[Test]
		public void TestBug553101 ()
		{
			CompletionResult provider = CreateProvider (
@"
namespace Some.Type 
{
	public enum Name { Foo, Bar }
}

namespace Test
{
	using STN = Some.Type.Name;
	
	public class Main
	{
		public void TestMe ()
		{
			$STN foo = $
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
		}
			
		/// <summary>
		/// Bug 555523 - C# code completion gets confused by extension methods with same names as properties
		/// </summary>
		[Test]
		public void TestBug555523A ()
		{
			CompletionResult provider = CreateProvider (
@"
class A
{
	public int AA { get; set; }
}

class B
{
	public int BB { get; set; }
}

static class ExtMethod
{
	public static A Extension (this MyClass myClass)
	{
		return null;
	}
}

class MyClass
{
	public B Extension {
		get;
		set;
	}
}

class MainClass
{
	public static void Main (string[] args)
	{
		MyClass myClass;
		$myClass.Extension ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("AA"), "property 'AA' not found.");
		}
		
		/// <summary>
		/// Bug 555523 - C# code completion gets confused by extension methods with same names as properties
		/// </summary>
		[Test]
		public void TestBug555523B ()
		{
			CompletionResult provider = CreateProvider (
@"
class A
{
	public int AA { get; set; }
}

class B
{
	public int BB { get; set; }
}

static class ExtMethod
{
	public static A Extension (this MyClass myClass)
	{
		return null;
	}
}

class MyClass
{
	public B Extension {
		get;
		set;
	}
}

class MainClass
{
	public static void Main (string[] args)
	{
		MyClass myClass;
		$myClass.Extension.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("BB"), "property 'BB' not found.");
		}
		
		
		/// <summary>
		/// Bug 561964 - Wrong type in tooltip when there are two properties with the same name
		/// </summary>
		[Test]
		public void TestBug561964 ()
		{
			CompletionResult provider = CreateProvider (
@"
interface A1 {
	int A { get; }
}
interface A2 {
	int B { get; }
}

interface IFoo {
	A1 Bar { get; }
}

class Foo : IFoo
{
	A1 IFoo.Bar { get { return null; } }
	public A2 Bar { get { return null; } }

	public static int Main (string[] args)
	{
		$new Foo().Bar.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("B"), "property 'B' not found.");
		}
		
		
		/// <summary>
		/// Bug 568204 - Inconsistency in resolution
		/// </summary>
		[Test]
		public void TestBug568204 ()
		{
			CompletionResult provider = CreateProvider (
@"
public class Style 
{
	public static Style TestMe ()
	{
		return new Style ();
	}
	
	public void Print ()
	{
		System.Console.WriteLine (""Hello World!"");
	}
}

public class Foo
{
	public Style Style { get; set;} 
	
	public void Bar ()
	{
		$Style.TestMe ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Print"), "method 'Print' not found.");
		}
		
		/// <summary>
		/// Bug 577225 - Inconsistent autocomplete on returned value of generic method.
		/// </summary>
		[Test]
		public void TestBug577225 ()
		{
			CompletionResult provider = CreateProvider (
@"
using Foo;
	
namespace Foo 
{
	public class FooBar
	{
		public void Bar ()
		{
		}
	}
}

namespace Other 
{
	public class MainClass
	{
		public static T Test<T> ()
		{
			return default (T);
		}
			
		public static void Main (string[] args)
		{
			$Test<FooBar> ().$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		
		
		/// <summary>
		/// Bug 582017 - C# Generic Type Constraints
		/// </summary>
		[Test]
		public void TestBug582017 ()
		{
			CompletionResult provider = CreateProvider (
@"
class Bar
{
	public void MyMethod ()
	{
	}
}

class Foo
{
	public static void Test<T> (T theObject) where T : Bar
	{
		$theObject.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("MyMethod"), "method 'MyMethod' not found.");
		}
		
		/// <summary>
		/// Bug 586304 - Intellisense does not show several linq extenion methods when using nested generic type
		/// </summary>
		[Test]
		public void TestBug586304 ()
		{
			CompletionResult provider = CreateProvider (
@"
using System;
using System.Collections.Generic;

public static class ExtMethods
{
	public static bool IsEmpty<T> (this IEnumerable<T> v)
	{
		return !v.Any ();
	}
}

public class Lazy<T> {}

public class IntelliSenseProblems
{
    public IEnumerable<Lazy<T>> GetLazies<T>()
    {
        return Enumerable.Empty<Lazy<T>>();
    }
}

public class Test
{ 
   void test ()
   {
		var values = new IntelliSenseProblems ();
		$var x = values.GetLazies<string> ().$
   }
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("IsEmpty"), "method 'IsEmpty' not found.");
		}
		
		/// <summary>
		/// Bug 586304 - Intellisense does not show several linq extenion methods when using nested generic type
		/// </summary>
		[Test]
		public void TestBug586304B ()
		{
			CompletionResult provider = CreateProvider (
@"
public delegate S Func<T, S> (T t);

public class Lazy<T> {
	public virtual bool IsLazy ()
	{
		return true;
	}
}

static class ExtMethods
{
	public static T Where<T>(this Lazy<T> t, Func<T, bool> pred)
	{
		return default (T);
	}
}

class MyClass
{
	public void Test()
	{
		Lazy<Lazy<MyClass>> c; 
		$c.Where (x => x.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("Test"), "method 'Test' found, but shouldn't.");
			Assert.IsNotNull (provider.Find ("IsLazy"), "method 'IsLazy' not found.");
		}
		
		
		/// <summary>
		/// Bug 587543 - Intellisense ignores interface constraints
		/// </summary>
		[Test]
		public void TestBug587543 ()
		{
			CompletionResult provider = CreateProvider (
@"
interface ITest
{
	void Foo ();
}

class C
{
	void Test<T> (T t) where T : ITest
	{
		$t.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
		}

		
		/// <summary>
		/// Bug 587549 - Intellisense does not work with override constraints
		/// </summary>
		[Test]
		public void TestBug587549 ()
		{
			CompletionResult provider = CreateProvider (
@"
public interface ITest
{
	void Bar();
}

public class BaseClass
{
	public void Foo ()
	{}
}

public abstract class Printer
{
	public abstract void Print<T, U> (object x) where T : BaseClass, U where U : ITest;
}

public class PrinterImpl : Printer
{
	public override void Print<A, B> (object x)
	{
		A a;
		$a.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 588223 - Intellisense does not recognize nested generics correctly.
		/// </summary>
		[Test]
		public void TestBug588223 ()
		{
			CompletionResult provider = CreateProvider (
@"
class Lazy<T> { public void Foo () {} }
class Lazy<T, S> { public void Bar () {} }

class Test
{
	public object Get ()
	{
		return null;
	}
	
	public Lazy<T> Get<T> ()
	{
		return null;
	}

	public Lazy<T, TMetaDataView> Get<T, TMetaDataView> ()
	{
		return null;
	}
	
	public Test ()
	{
		Test t = new Test ();
		var bug = t.Get<string, string> ();
		$bug.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 592120 - Type resolver bug with this.Property[]
		/// </summary>
		[Test]
		public void TestBug592120 ()
		{
			CompletionResult provider = CreateProvider (
@"

interface IBar
{
	void Test ();
}

class Foo
{
	public IBar[] X { get; set; }

	public void Bar ()
	{
		var y = this.X;
		$y.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("Test"), "method 'Test' found, but shouldn't.");
		}
		
		
		/// <summary>
		/// Bug 576354 - Type inference failure
		/// </summary>
		[Test]
		public void TestBug576354 ()
		{
			CompletionResult provider = CreateProvider (
@"
delegate T Func<S, T> (S s);

class Foo
{
	string str;
	
	public Foo (string str)
	{
		this.str = str;
	}
	
	public void Bar () 
	{
		System.Console.WriteLine (str);
	}
}

class MyTest
{
	static T Test<T> (Func<string, T> myFunc)
	{
		return myFunc (""Hello World"");
	}
	
	public static void Main (string[] args)
	{
		var result = Test (str => new Foo (str));
		$result.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 534680 - LINQ keywords missing from Intellisense
		/// </summary>
		[Test]
		public void TestBug534680 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"
class Foo
{
	public static void Main (string[] args)
	{
		$from str in args $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("select"), "keyword 'select' not found.");
		}
		
		/// <summary>
		/// Bug 610006 - Intellisense gives members of return type of functions even when that function isn't invoked
		/// </summary>
		[Ignore("Roslyn bug")]
		[Test]
		public void TestBug610006 ()
		{
			CompletionResult provider = CreateProvider (
@"
class MainClass
{
	public MainClass FooBar ()
	{
	}
	
	public void Test ()
	{
		$FooBar.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("FooBar"), "method 'FooBar' found, but shouldn't.");
		}
		
		
		/// <summary>
		/// Bug 614045 - Types hidden by members are not formatted properly by ambience
		/// </summary>
		[Test]
		public void TestBug614045 ()
		{
			CompletionResult provider = CreateProvider (
@"
namespace A
{
	enum Foo
	{
		One,
		Two,
		Three
	}
}

namespace B
{
	using A;
	
	public class Baz
	{
		public string Foo;
		
		void Test (Foo a)
		{
			$switch (a) {
			case $
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			//Assert.IsNull (provider.Find ("Foo"), "enum 'Foo' found, but shouldn't.");
			Assert.IsNotNull (provider.Find ("A.Foo"), "enum 'A.Foo' not found.");
		}

		[Test]
		public void TestBug614045_IndexerCase ()
		{
			CompletionResult provider = CreateProvider (
				@"
namespace A
{
	enum Foo
	{
		One,
		Two,
		Three
	}
}

namespace B
{
	using A;
	
	public class Baz
	{
		public string Foo;
		
		int this[Foo b] {
			get {}
			set {
				$switch (b) {
				case $
			}
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			// Assert.IsNull (provider.Find ("Foo"), "enum 'Foo' found, but shouldn't.");
			Assert.IsNotNull (provider.Find ("A.Foo"), "enum 'A.Foo' not found.");
		}
		/// <summary>
		/// Bug 615992 - Intellisense broken when calling generic method.
		/// </summary>
		[Test]
		public void TestBug615992 ()
		{
			CompletionResult provider = CreateProvider (
@"public delegate void Act<T> (T t);

public class Foo
{
	public void Bar ()
	{
	}
}

class TestBase
{
	protected void Method<T> (Act<T> action)
	{
	}
}

class Test : TestBase
{
	public Test ()
	{
		$Method<Foo> (f => f.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 625064 - Internal classes aren't suggested for completion
		/// </summary>
		[Test]
		public void TestBug625064 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"class Foo 
{
	class Bar { }
	$List<$
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "class 'Bar' not found.");
		}
		
		
		/// <summary>
		/// Bug 631875 - No Intellisense for arrays
		/// </summary>
		[Test]
		public void TestBug631875 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"class C
{
	static void Main ()
	{
		var objects = new[] { new { X = (object)null }};
		$objects[0].$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("X"), "property 'X' not found.");
		}
		
		/// <summary>
		/// Bug 632228 - Wrong var inference
		/// </summary>
		[Test]
		public void TestBug632228 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"
class C {
	public void FooBar () {}
	public static void Main ()
	{
		var thingToTest = new[] { new C (), 22, new object(), string.Empty, null };
		$thingToTest[0].$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("FooBar"), "method 'FooBar' found, but shouldn't.");
		}

		/// <summary>
		/// Bug 632696 - No intellisense for constraints
		/// </summary>
		[Test]
		public void TestBug632696 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"
class Program
{
	void Foo ()
	{
	}

	static void Foo<T> () where T : Program
	{
		var s = new[] { default(T) };
		$s[0].$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
		}
		
		[Test]
		public void TestCommentsWithWindowsEol ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider ("class TestClass\r\n{\r\npublic static void Main (string[] args) {\r\n// TestComment\r\n$args.$\r\n}\r\n}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("ToString"), "method 'ToString' not found.");
		}
		
		[Test]
		public void TestGhostEntryBug ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"
using System.IO;

class TestClass
{
	public Path Path {
		get;
		set;
	}
	
	void Test ()
	{
		$$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("System.IO.Path"), "'System.IO.Path' found but shouldn't.");
			Assert.IsNotNull (provider.Find ("Path"), "property 'Path' not found.");
		}
		
		
		/// <summary>
		/// Bug 648562 – Abstract members are allowed by base call
		/// </summary>
		[Ignore("Roslyn bug")]
		[Test]
		public void TestBug648562 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"using System;

abstract class A
{
    public abstract void Foo<T> (T type);
}

class B : A
{
    public override void Foo<U> (U type)
    {
        $base.$
    }
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("Foo"), "method 'Foo' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 633767 - Wrong intellisense for simple lambda
		/// </summary>
		[Test]
		public void TestBug633767 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"using System;

public class E
{
	public int Foo { get; set; }
}

public class C
{
	delegate void D<T> (T t);
	
	static T M<T> (T t, D<T> a)
	{
		return t;
	}

	static void MethodArg (object o)
	{
	}

	public static int Main ()
	{
		D<object> action = l => Console.WriteLine (l);
		var b = M (new E (), action);
		$b.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("Foo"), "property 'Foo' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 616208 - Renaming a struct/class is renaming too much
		/// </summary>
		[Test]
		public void TestBug616208 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"using System;

namespace System 
{
	public class Foo { public int Bar; };
}

namespace test.Util
{
	public class Foo { public string x; }
}

namespace Test
{
	public class A
	{
		public Foo X;
		
		public A ()
		{
			$X.$
		}
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "property 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 668135 - Problems with "new" completion
		/// </summary>
		[Test]
		public void TestBug668135a ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"public class A
{
	public A ()
	{
		string test;
		$Console.WriteLine (test = new $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("string"), "class 'string' not found.");
		}
		
		/// <summary>
		/// Bug 668453 - var completion infers var type too eagerly
		/// </summary>
		[Test]
		public void TestBug668453 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"public class Test
{
	private void FooBar ()
	{
		$var str = new $
		FooBar ();
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("FooBar"), "method 'FooBar' found.");
		}
		
		/// <summary>
		/// Bug 669285 - Extension method on T[] shows up on T
		/// </summary>
		[Test]
		public void TestBug669285 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"static class Ext
{
	public static void Foo<T> (this T[] t)
	{
	}
}

public class Test<T>
{
	public void Foo ()
	{
		T t;
		$t.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("Foo"), "method 'Foo' found.");
		}

		[Test]
		public void TestBug669285B ()
		{
			var provider = CreateCtrlSpaceProvider (
@"static class Ext
{
	public static void Foo<T> (this T[] t)
	{
	}
}

public class Test<T>
{
	public void Foo ()
	{
		T[] t;
		$t.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
		}
		
		
		/// <summary>
		/// Bug 669818 - Autocomplete missing for new nested class
		/// </summary>
		[Test]
		public void TestBug669818 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"using System;
public class Foo
{
    public class Bar
    {
    }
	public static void FooBar () {}
}
class TestNested
{
    public static void Main (string[] args)
    {
        $new Foo.$
    }
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "class 'Bar' not found.");
			Assert.IsNull (provider.Find ("FooBar"), "method 'FooBar' found.");
		}
		
		/// <summary>
		/// Bug 674514 - foreach value should not be in the completion list
		/// </summary>
		[Test]
		public void TestBug674514 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"using System;
using System.Linq;
using System.Collections.Generic;

class Foo
{
	public static void Main (string[] args)
	{
		$foreach (var arg in $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("args"), "parameter 'args' not found.");
			Assert.IsNull (provider.Find ("arg"), "variable 'arg' found.");
		}
		
		[Test]
		public void TestBug674514B ()
		{
			var provider = CreateCtrlSpaceProvider (
@"using System;
using System.Linq;
using System.Collections.Generic;

class Foo
{
	public static void Main (string[] args)
	{
		$foreach (var arg in args) 
			Console.WriteLine ($
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("args"), "parameter 'args' not found.");
			Assert.IsNotNull (provider.Find ("arg"), "variable 'arg' not found.");
		}
		
		/// <summary>
		/// Bug 675436 - Completion is trying to complete symbol names in declarations
		/// </summary>
		[Ignore("test is valid there.")]
		[Test]
		public void TestBug675436_LocalVar ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"class Test
{
    public static void Main (string[] args)
    {
        $int test = $
    }
}
");
			Assert.IsNull (provider.Find ("test"), "name 'test' found.");
		}
		
		/// <summary>
		/// Bug 675956 - Completion in for loops is broken
		/// </summary>
		[Test]
		public void TestBug675956 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"class Test
{
    public static void Main (string[] args)
    {
        $for (int i = 0; $
    }
}
");
			Assert.IsNotNull (provider.Find ("i"), "variable 'i' not found.");
		}
		
		/// <summary>
		/// Bug 675956 - Completion in for loops is broken
		/// </summary>
		[Test]
		public void TestBug675956Case2 ()
		{
			CompletionResult provider = CreateProvider (
@"class Test
{
    public static void Main (string[] args)
    {
        $for (int i = 0; i$
    }
}
");
			Assert.IsNotNull (provider.Find ("i"), "variable 'i' not found.");
		}
		
		/// <summary>
		/// Bug 676311 - auto completion too few proposals in fluent API (Moq)
		/// </summary>
		[Test]
		public void TestBug676311 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"using System;
using System.Linq;
using System.Linq.Expressions;

namespace Test
{
	public interface IFoo<T>
	{
		void Foo1 ();
	}

	public interface IFoo<T, S>
	{
		void Foo2 ();
	}
	
	public class Test<T>
	{
		public IFoo<T> TestMe (Expression<Action<T>> act)
		{
			return null;
		}
		
		public IFoo<T, S> TestMe<S> (Expression<Func<S, T>> func)
		{
			return null;
		}
		
		public string TestMethod (string str)
		{
			return str;
		}
	}
	
	class MainClass
	{
		public static void Main (string[] args)
		{
			var t = new Test<string> ();
			var s = t.TestMe (x => t.TestMethod (x));
			$s.$
		}
	}
}");
			Assert.IsNotNull (provider.Find ("Foo1"), "method 'Foo1' not found.");
		}
		/// <summary>
		/// Bug 676311 - auto completion too few proposals in fluent API (Moq)
		/// </summary>
		[Test]
		public void TestBug676311B ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"using System;
using System.Linq;
using System.Linq.Expressions;

namespace Test
{
	public interface IFoo<T>
	{
		void Foo1 ();
	}

	public interface IFoo<T, S>
	{
		void Foo2 ();
	}
	
	public class Test<T>
	{
		public IFoo<T> TestMe (Expression<Action<T>> act)
		{
			return null;
		}
		
		public IFoo<T, S> TestMe<S> (Expression<Func<S, T>> func)
		{
			return null;
		}
		
		public string TestMethod (string str)
		{
			return str;
		}
	}
	
	class MainClass
	{
		public static void Main (string[] args)
		{
			var t = new Test<string> ();
			var s = t.TestMe<string> (x => t.TestMethod (x));
			$s.$
		}
	}
}");
			Assert.IsNotNull (provider.Find ("Foo2"), "method 'Foo2' not found.");
		}
		
		/// <summary>
		/// Bug 676311 - auto completion too few proposals in fluent API (Moq)
		/// </summary>
		[Test]
		public void TestBug676311_Case2 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"using System;
using System.Linq.Expressions;

namespace Test
{
	public interface IFoo<T>
	{
		void Foo1 ();
	}

	public interface IFoo<T, S>
	{
		void Foo2 ();
	}
	
	public class Test<T>
	{
		public IFoo<T> TestMe (Expression<Action<T>> act)
		{
			return null;
		}
		
		public IFoo<T, S> TestMe<S> (Expression<Func<S, T>> func)
		{
			return null;
		}
		
		public void TestMethod (string str)
		{
		}
	}
	
	class MainClass
	{
		public static void Main (string[] args)
		{
			var t = new Test<string> ();
			var s = t.TestMe (x => t.TestMethod (x));
			$s.$
		}
	}
}");
			Assert.IsNotNull (provider.Find ("Foo1"), "method 'Foo2' not found.");
		}
		
		/// <summary>
		/// Bug 678340 - Cannot infer types from Dictionary&lt;K,V&gt;.Values;
		/// </summary>
		[Test]
		public void TestBug678340 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"using System;
using System.Collections.Generic;

public class Test
{
	public void SomeMethod ()
	{
		var foo = new Dictionary<string,Test> ();
		foreach (var bar in foo.Values) {
			$bar.$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("SomeMethod"), "method 'SomeMethod' not found.");
		}
		/// <summary>
		/// Bug 678340 - Cannot infer types from Dictionary&lt;K,V&gt;.Values
		/// </summary>
		[Test]
		public void TestBug678340_Case2 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"public class Foo<T>
{
	public class TestFoo
	{
		public T Return ()
		{
			
		}
	}
	
	public TestFoo Bar = new TestFoo ();
}

public class Test
{
	public void SomeMethod ()
	{
		Foo<Test> foo = new Foo<Test> ();
		var f = foo.Bar;
		$f.Return ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("SomeMethod"), "method 'SomeMethod' not found.");
		}
		
		/// <summary>
		/// Bug 679792 - MonoDevelop becomes unresponsive and leaks memory
		/// </summary>
		[Test]
		public void TestBug679792 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"using System.Collections.Generic;

class TestClass
{
	public static void Main (string[] args)
	{
		Dictionary<string, Dictionary<string, TestClass>> cache;
		$cache[""Hello""] [""World""] = new $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TestClass"), "class 'TestClass' not found.");
		}
		
		/// <summary>
		/// Bug 679995 - Variable missing from completiom
		/// </summary>
		/// 
		[Test]
		public void TestBug679995 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"class TestClass
{
	public void Foo ()
	{
		using (var testMe = new TestClass ()) {
			$$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("testMe"), "variable 'testMe' not found.");
		}
		
		/// <summary>
		/// Bug 680264 - Lamba completion inference issues
		/// </summary>
		/// 
		[Test]
		public void TestBug680264 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"
public delegate S Func<T, S> (T t);

public static class Linq
{
	public static bool Any<T> (this T[] t, Func<T, bool> func)
	{
		return true;
	}
}

class TestClass
{
	public void Foo ()
	{
		TestClass[] test;
		$test.Any (t => t.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
		}
		
		/// <summary>
		/// Bug 683037 - Missing autocompletion when 'using' directive references namespace by relative names
		/// </summary>
		/// 
		[Test]
		public void TestBug683037 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"namespace N1.N2
{
	public class C1
	{
		public void Foo () {
			System.Console.WriteLine (1);
		}
	}
}

namespace N1
{
	using N2;

	public class C2
	{
		public static void Main (string[] args)
		{
			C1 x = new C1 ();

			$x.$
		}
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
		}
		
		/// <summary>
		/// Bug 690606 - Incomplete subclasses listing in code completion
		/// </summary>
		[Test]
		public void TestBug690606 ()
		{
			CompletionResult provider = CreateCtrlSpaceProvider (
@"
public abstract class Base {}
public abstract class MyBase<T> : Base {}
public class A : MyBase<string> {}
public class B : MyBase<int> {}
public class C : MyBase<bool> {}

public class Test
{
	public static void Main (string[] args)
	{
		$Base x = new $
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("A"), "class 'A' not found.");
			Assert.IsNotNull (provider.Find ("B"), "class 'B' not found.");
			Assert.IsNotNull (provider.Find ("C"), "class 'C' not found.");
		}
		
		/// <summary>
		/// Bug 1744 - [New Resolver] Issues while typing a property
		/// </summary>
		[Test]
		public void Test1744 ()
		{
			var provider = CreateProvider (
@"
public class Test
{
	$public p$
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("void"), "class 'void' not found.");
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found.");
			Assert.IsNotNull (provider.Find ("System"), "namespace 'System' not found.");
		}
		
		/// <summary>
		/// Bug 1747 - [New Resolver] Code completion issues when declaring a generic dictionary
		/// </summary>
		[Test]
		public void Test1747()
		{
			var provider = CreateProvider(
@"using System.Collections.Generic;
public class Test
{
	$Dictionary<int,string> field = new $
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("System.Collections.Generic.Dictionary<int, string>"), "type 'Dictionary<int, string>' not found.");
			Assert.AreEqual ("System.Collections.Generic.Dictionary<int, string>", provider.DefaultCompletionString);
		}

		[Ignore]
		[Test]
		public void Test1747Case2 ()
		{
			var provider = CreateProvider (
@"public class Test
{
	$Dictionary<int, string> d$
}
");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider not empty.");
			
			provider = CreateCtrlSpaceProvider (
@"public class Test
{
	$Dictionary<int, string> $
}
");
			Assert.IsFalse (provider == null || provider.Count == 0, "provider not found.");
			
		}
		
		[Test]
		public void TestCompletionInTryCatch ()
		{
			CompletionResult provider = CreateProvider (
@"class Test { public void TM1 () {} public void TM2 () {} public int TF1; }
class CCTest {
void TestMethod ()
{
	Test t;
	try {
		$t.$
}
}
");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("TM1"));
			Assert.IsNotNull (provider.Find ("TM2"));
			Assert.IsNotNull (provider.Find ("TF1"));
		}
		
		[Test]
		public void TestPartialCompletionData ()
		{
			var provider = CreateProvider (
@"
public partial class TestMe
{
	partial void MyMethod ();
	partial void Implemented ();
}

public partial class TestMe
{
	$partial $

	partial void Implemented () { }
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("MyMethod"), "method 'MyMethod' not found.");
			Assert.IsNull (provider.Find ("Implemented"), "method 'Implemented'  found.");
		}
		
		/// <summary>
		/// Bug 224 - Code completion cannot handle lambdas properly. 
		/// </summary>
		[Test]
		public void TestBug224 ()
		{
			CombinedProviderTest (
@"
using System;

public sealed class CrashEventArgs : EventArgs
{
	public int ArgsNum { get; set; }

	public CrashEventArgs ()
	{
		
	}
}

interface ICrashMonitor
{
	event EventHandler<CrashEventArgs> CrashDetected;

	void StartMonitoring ();

	void StopMonitoring ();
}

namespace ConsoleProject
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			ICrashMonitor mon;
			$mon.CrashDetected += (sender, e) => e.$
		}
	}
}
", provider => Assert.IsNotNull(provider.Find("ArgsNum"), "property 'ArgsNum' not found."));
		}

		[Test]
		public void TestParameterContext ()
		{
			var provider = CreateProvider (
@"
public class TestMe
{
	$void TestMe (TestClassParameter t$
}");
			if (provider != null && provider.Count > 0) {
				foreach (var p in provider)
					Console.WriteLine(p.DisplayText);
			}
			Assert.IsTrue (provider == null || provider.Count == 0, "provider was not empty.");
		}
		
		/// <summary>
		/// Bug 2123 - Completion kicks in after an array type is used in method parameters
		/// </summary>
		[Test]
		public void TestParameterContextCase2FromBug2123 ()
		{
			CompletionResult provider = CreateProvider (
@"class Program
{
	public Program ($string[] a$)
	{
	}
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test]
		public void TestParameterContextNameProposal ()
		{
			var provider = CreateCtrlSpaceProvider (
@"
public class TestMe
{
	$void TestMe (TestClassParameter $
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("testClassParameter"), "'testClassParameter' not found.");
			Assert.IsNotNull (provider.Find ("classParameter"), "'classParameter' not found.");
			Assert.IsNotNull (provider.Find ("parameter"), "'parameter' not found.");
		}
		
		[Test]
		public void TestParameterTypeNameContext ()
		{
			CombinedProviderTest (
@"class Program
{
	public Program ($System.$)
	{
	}
}", provider => Assert.IsNotNull(provider.Find("Object"), "'Object' not found."));
		}
		
		[Test]
		public void TestMethodNameContext ()
		{
			CompletionResult provider = CreateProvider (
@"using System;
namespace Test 
{
	class Program
	{
		void SomeMethod ()
		{
			
		}
		
		$public void T$
	}
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test]
		public void TestNamedParameters ()
		{
			CombinedProviderTest (
@"class MyClass {
    string Bar { get; set; }

    void MethodOne(string foo="""", string bar="""")
	{
    }

    void MethodTwo() {
        $MethodOne(b$
    }
}", provider => {
				Assert.IsNotNull (provider.Find ("bar:"), "'bar:' not found.");
				Assert.IsNotNull (provider.Find ("foo:"), "'foo:' not found.");
			});
		}

		[Test]
		public void TestNamedParameters2 ()
		{
			var provider = CreateCtrlSpaceProvider (
@"class MyClass {
    string Bar { get; set; }

    void MethodOne(string foo="""", string bar="""")
	{
    }

    void MethodTwo() {
        MethodOne($$);
    }
}");
			Assert.IsNotNull (provider.Find ("bar:"), "'bar:' not found.");
			Assert.IsNotNull (provider.Find ("foo:"), "'foo:' not found.");
		}

		[Test]
		public void TestNamedParametersConstructorCase ()
		{
			CombinedProviderTest (
@"class MyClass {
    MyClass(string foo="""", string bar="""")
	{
    }

    void MethodTwo() {
        $new MyClass(b$
    }
}", provider => {
				Assert.IsNotNull (provider.Find ("bar:"), "'bar:' not found.");
				Assert.IsNotNull (provider.Find ("foo:"), "'foo:' not found.");
			});
		}
		
		[Test]
		public void TestConstructorThisBase ()
		{
			CombinedProviderTest (
@"class Program
{
	public Program () : $t$
	{
	}
}", provider => {
				Assert.IsNotNull (provider.Find ("this"), "'this' not found.");
				Assert.IsNotNull (provider.Find ("base"), "'base' not found.");
			});
		}
		
		[Test]
		public void TestAnonymousArguments ()
		{
			CombinedProviderTest (
@"
using System;
class Program
{
	public static void Main ()
	{
		EventHandler f = delegate (object sender, EventArgs args) {
			$Console.WriteLine(s$
		};
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("sender"), "'sender' not found.");
				Assert.IsNotNull (provider.Find ("args"), "'args' not found.");
			});
		}

		[Ignore("FixMe")]
		[Test]
		public void TestCodeCompletionCategorySorting ()
		{
			CompletionResult provider = CreateProvider (
@"class CClass : BClass
{
	public int C;
}

class BClass : AClass
{
	public int B;
}

class AClass
{
	public int A;
}

class Test
{
	public void TestMethod ()
	{
		CClass a;
		$a.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			
			var list = new List<CompletionCategory> ();
			
			for (int i = 0; i < provider.Count; i++) {
				if (list.Contains (provider [i].CompletionCategory))
					continue;
				list.Add (provider [i].CompletionCategory);
			}	
			Assert.AreEqual (4, list.Count);
			
			list.Sort ();
			Assert.AreEqual ("AClass", list [0].DisplayText);
			Assert.AreEqual ("BClass", list [1].DisplayText);
			Assert.AreEqual ("CClass", list [2].DisplayText);
			Assert.AreEqual ("object", list [3].DisplayText);
		}
		
		[Test]
		public void TestAsExpressionContext ()
		{
			var provider = CreateProvider (
@"class CClass : BClass
{
	public int C;
}

class BClass : AClass
{
	public int B;
}

class AClass
{
	public int A;
}

class Test
{
	public void TestMethod ()
	{
		AClass a;
		$a as A$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("AClass"), "'AClass' not found.");
			Assert.IsNotNull (provider.Find ("BClass"), "'BClass' not found.");
			Assert.IsNotNull (provider.Find ("CClass"), "'CClass' not found.");
			Assert.IsNotNull (provider.Find ("Test"), "'Test' not found.");
//			Assert.IsNull (provider.Find ("TestMethod"), "'TestMethod' found.");
			
		}
		
		/// <summary>
		/// Bug 2109 - [Regression] Incorrect autocompletion when declaring an enum 
		/// </summary>
		[Test]
		public void TestBug2109B ()
		{
			CompletionResult provider = CreateProvider (
@"namespace Foobar
{
    class MainClass
    {
        public enum Foo
        {
            Value1,
            Value2
        }

        public class Test
        {
            Foo Foo {
                get; set;
            }

            public static void Method (Foo foo)
            {
                $Foo.$
            }
        }
    }
}
");
			Assert.IsNotNull (provider.Find ("Value1"), "field 'Value1' not found.");
			Assert.IsNotNull (provider.Find ("Value2"), "field 'Value2' not found.");
		}
		
		/// <summary>
		/// Bug 3581 - [New Resolver] No code completion on Attributes
		/// </summary>
		[Test]
		public void TestBug3581 ()
		{
			CompletionResult provider = CreateProvider (
@"using System;

namespace Foobar
{
	class Intent 
	{
		public static int Foo = 0;
		public static int Bar = 1;
	}
	
	class MyAttribute : Attribute
	{
		public int[] Categories;
	}
	
	[MyAttribute(Categories = new [] { $I$ })]
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine (ddsd);
		}
	}
}

");
			Assert.IsNotNull (provider.Find ("Intent"), "'Intent' not found.");
		}
		
		[Test]
		public void TestForConditionContext ()
		{
			CompletionResult provider = CreateProvider (
@"using System;

class MainClass
{
	public static void Main (string[] args)
	{
		$for (int i = 0; i < System.$
	}
}
");
			Assert.IsNotNull (provider.Find ("Math"), "'Math' not found.");
		}
		
		[Test]
		public void TestConditionalExpression ()
		{
			CompletionResult provider = CreateProvider (
				@"using System;

class MainClass
{
	public static void Main (string[] args)
	{
		int a;
		$a = true ? System.$
	}
}
");
			Assert.IsNotNull (provider.Find ("Math"), "'Math' not found.");
		}
		
		/// <summary>
		/// Bug 3655 - Autocompletion does not work for the assembly attribute [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyExternalAssembly")] 
		/// </summary>
		[Test]
		public void Test3655 ()
		{
			CombinedProviderTest (@"$[a$", provider => {
				Assert.IsNotNull (provider.Find ("assembly"), "'assembly' not found.");
				Assert.IsNotNull (provider.Find ("System"), "'System' not found.");
			});
		}
		
		[Test]
		public void Test3655Case2 ()
		{
			CombinedProviderTest (@"$[assembly:System.R$", provider => Assert.IsNotNull(provider.Find("Runtime"), "'Runtime' not found."));
		}
		
		[Test]
		public void Test3655Case2Part2 ()
		{
			CombinedProviderTest (@"$[assembly:System.$", provider => Assert.IsNotNull(provider.Find("Runtime"), "'Runtime' not found."));
		}
		
		[Test]
		public void Test3655Case3 ()
		{
			CombinedProviderTest (@"$[assembly:System.Runtime.C$", provider => Assert.IsNotNull(provider.Find("CompilerServices"), "'CompilerServices' not found."));
		}
		
		[Test]
		public void Test3655Case3Part2 ()
		{
			CombinedProviderTest (@"$[assembly:System.Runtime.$", provider => Assert.IsNotNull(provider.Find("CompilerServices"), "'CompilerServices' not found."));
		}
		
		[Test]
		public void Test3655Case4 ()
		{
			CombinedProviderTest (@"$[assembly:System.Runtime.CompilerServices.I$", provider => Assert.IsNotNull(provider.Find("InternalsVisibleTo"), "'InternalsVisibleTo' not found."));
		}
		
		[Test]
		public void Test3655Case4Part2 ()
		{
			CombinedProviderTest (@"$[assembly:System.Runtime.CompilerServices.$", provider => Assert.IsNotNull(provider.Find("InternalsVisibleTo"), "'InternalsVisibleTo' not found."));
		}
		
		[Test]
		public void TestUsingContext ()
		{
			CombinedProviderTest (@"$using System.$", provider => {
				Assert.IsNotNull (provider.Find ("IO"), "'IO' not found.");
				Assert.IsNull (provider.Find ("Console"), "'Console' found.");
			});
		}
		
		[Test]
		public void TestUsingContextCase2 ()
		{
			CombinedProviderTest (@"$using System.U$", provider => {
				Assert.IsNotNull (provider.Find ("IO"), "'IO' not found.");
				Assert.IsNull (provider.Find ("Console"), "'Console' found.");
			});
		}

		[Ignore("FixMe")]
		[Test]
		public void TestInterfaceReturnType()
		{
			var provider = CreateProvider(
@"using System;
using System.Collections.Generic;

class MainClass
{
	public IEnumerable<string> Test ()
	{
		$return new a$
	}
}
");
			Assert.IsNotNull(provider.Find("string[]"), "'string[]' not found.");
			Assert.IsNotNull(provider.Find("List<string>"), "'List<string>' not found.");
			Assert.IsNull(provider.Find("IEnumerable"), "'IEnumerable' found.");
			Assert.IsNull(provider.Find("IEnumerable<string>"), "'IEnumerable<string>' found.");
		}

		[Ignore("FixMe")]
		[Test]
		public void TestInterfaceReturnTypeCase2 ()
		{
			var provider = CreateProvider (
@"using System;
using System.Collections.Generic;

class MainClass
{
	public IEnumerable<string> Test ()
	{
		$return new System.Collections.Generic.a$
	}
}
");
			Assert.IsNotNull (provider.Find ("List"), "'List' not found.");
			Assert.IsNull (provider.Find ("IEnumerable"), "'IEnumerable' found.");
		}

		[Ignore("FixMe")]
		[Test]
		public void TestInterfaceReturnTypeCase3 ()
		{
			var provider = CreateProvider (
@"using System;
using System.Collections.Generic;

class MainClass
{
	public IEnumerable<string> Test ()
	{
		$return new System.Collections.Generic.$
	}
}
");
			Assert.IsNotNull (provider.Find ("List"), "'List' not found.");
			Assert.IsNull (provider.Find ("IEnumerable"), "'IEnumerable' found.");
		}


		/// <summary>
		/// Bug 3957 - [New Resolver]Override completion doesn't work well for overloaded methods
		/// </summary>
		[Test]
		public void TestBug3957 ()
		{
			var provider = CreateProvider (
@"class A
{
    public virtual void Method()
    {}
    public virtual void Method(int i)
    {}
}

class B : A
{
	$override $
}

");
			Assert.AreEqual(2, provider.Data.Count(d => d.DisplayText == "Method"));
		}

		/// <summary>
		/// Bug 3973 - code completion forgets context if text is deleted 
		/// </summary>
		[Test]
		public void TestBug3973 ()
		{
			var provider = CreateProvider (
@"
using System;

class A
{
	public static void Main (string[] args)
	{
		Console.$W$
	}
}

");
			Assert.IsNotNull (provider.Find ("WriteLine"), "'WriteLine' not found.");
		}

		/// <summary>
		/// Bug 4017 - code completion in foreach does not work for local variables declared in the same block
		/// </summary>
		[Test]
		public void TestBug4017()
		{
			var provider = CreateProvider (
@"
class TestClass
{
    void Foo()
    {
        string[] args = null;
        $foreach(string arg in a$
    }
}
");
			Assert.IsNotNull (provider.Find ("args"), "'args' not found.");
		}

		/// <summary>
		/// Bug 4020 - code completion handles explicit interface implementations improperly
		/// </summary>
		[Test]
		public void TestBug4020 ()
		{
			// todo: maybe a better solution would be 
			//       having an item to insert the proper cast on 'Dispose' ?
			var provider = CreateProvider (
@"
using System;
namespace Test
{
    class TestClass : IDisposable
    {
        void IDisposable.Dispose ()
        {
        }
        public void Foo()
        {
            $D$
        }
    }
}
");
			Assert.IsNull (provider.Find ("Dispose"), "'Dispose' found.");
		}


		/// <summary>
		/// Bug 4085 - code completion problem with generic dictionary
		/// </summary>
		[Test]
		public void TestBug4085()
		{
			// Name proposal feature breaks here
			var provider = CreateCtrlSpaceProvider(
@"using System.Collections.Generic;
namespace Test
{
	class TestClass
	{
		static void Main()
		{
			$IDictionary<string, TestClass> foo = new Dictionary<string, $
		}
	}
}

");
			Assert.IsNotNull(provider.Find("TestClass"), "'TestClass' not found.");
		}

		/// <summary>
		/// Bug 4283 - Newresolver: completing constructor parameters
		/// </summary>
		[Test]
		public void TestBug4283()
		{
			var provider = CreateCtrlSpaceProvider(
@"class Program
{
	public Program (int test) : base($)
	{
	}
}");
			Assert.IsNotNull(provider.Find("test"), "'test' not found.");
		}

		[Test]
		public void TestBug4283ThisCase()
		{
			var provider = CreateCtrlSpaceProvider(
@"class Program
{
	public Program (int test) : this($)
	{
	}
}");
			Assert.IsNotNull(provider.Find("test"), "'test' not found.");
		}



		/// <summary>
		/// Bug 4174 - Intellisense popup after #region (same line) 
		/// </summary>
		[Test]
		public void TestBug4174()
		{
			var provider = CreateProvider(
@"
namespace Test
{
	class TestClass  
    {
$#region S$
    }
}");
			Assert.IsTrue(provider == null || provider.Count == 0);
		}


		[Test]
		public void TestParameterAttributeContext()
		{
			CombinedProviderTest(
@"using System;
using System.Runtime.InteropServices;

public class Test
{
	$static extern IntPtr somefunction([MarshalAs(UnmanagedType.LPTStr)] string fileName, [MarshalAs(UnmanagedType.$
}
", provider => Assert.IsNotNull(provider.Find("LPStr"), "'LPStr' not found."));
		}


		/// <summary>
		/// Bug 1051 - Code completion can't handle interface return types properly
		/// </summary>
		[Ignore("Fix me")]
		[Test]
		public void TestBug1051()
		{
			CombinedProviderTest(
@"using System;
using System.Collections.Generic;

public class Test
{
	IEnumerable<string> TestFoo()
	{
		$return new $
	}
}
", provider => {
				Assert.IsNull(provider.Find("IEnumerable<string>"), "'IEnumerable<string>' found.");
				Assert.IsNotNull(provider.Find("List<string>"), "'List<string>' not found.");
				Assert.IsNotNull(provider.Find("string[]"), "'string[]' not found.");
			});
		}

		/// <summary>
		/// Bug 2668 - No completion offered for enum keys of Dictionaries 
		/// </summary>
		[Test]
		public void TestBug2668()
		{
			CombinedProviderTest(
@"using System;
using System.Collections.Generic;

public enum SomeEnum { One, Two }

public class Test
{
	void TestFoo()
	{
		Dictionary<SomeEnum,int> dict = new Dictionary<SomeEnum,int>();
		$dict[O$

	}
}
", provider => {
				Assert.IsNotNull(provider.Find("SomeEnum"), "'SomeEnum' not found.");
				Assert.IsNotNull(provider.Find("SomeEnum.One"), "'SomeEnum.One' not found.");
			});
		}

		/// <summary>
		/// Bug 4487 - Filtering possible types for new expressions a bit too aggressively
		/// </summary>
		[Test]
		public void TestBug4487()
		{
			// note 'string bar = new Test ().ToString ()' would be valid.
			CombinedProviderTest(
@"public class Test
{
	void TestFoo()
	{
		$string bar = new T$
	}
}
", provider => Assert.IsNotNull(provider.Find("Test"), "'Test' not found."));
		}

		/// <summary>
		/// Bug 4525 - Unexpected code completion exception
		/// </summary>
		[Test]
		public void TestBug4525()
		{
			CombinedProviderTest(
@"public class Test
{
	$public new s$
}
", provider => Assert.IsNotNull(provider.Find("static"), "'static' not found."));
		}
		/// <summary>
		/// Bug 4604 - [Resolver] Attribute Properties are not offered valid autocomplete choices
		/// </summary>
		[Test]
		public void TestBug4604()
		{
			CombinedProviderTest(
@"
		public sealed class MyAttribute : System.Attribute
		{
			public bool SomeBool {
				get;
				set;
			}
		}
$[MyAttribute(SomeBool=t$
public class Test
{
}
", provider => {
				Assert.IsNotNull(provider.Find("true"), "'true' not found.");
				Assert.IsNotNull(provider.Find("false"), "'false' not found.");
			});
		}


		/// <summary>
		/// Bug 4624 - [AutoComplete] Attribute autocomplete inserts entire attribute class name. 
		/// </summary>
		[Test]
		public void TestBug4624()
		{
			CombinedProviderTest(
@"using System;

enum TestEnum
{
   $[E$
   EnumMember
}

", provider => Assert.IsNotNull(provider.Find("Obsolete"), "'Obsolete' not found."));
		}

		[Test]
		public void TestCatchContext()
		{
			CombinedProviderTest(
@"using System;

class Foo
{
	void Test ()
	{
		$try { } catch (S$
	}
}


", provider => {
				Assert.IsNotNull(provider.Find("Exception"), "'Exception' not found.");
				Assert.IsNull(provider.Find("String"), "'String' found.");
			});
		}

		[Test]
		public void TestCatchContextFollowUp()
		{
			CombinedProviderTest(
@"using System;

class Foo
{
	void Test ()
	{
		$try { } catch (System.$
	}
}


", provider => {
				Assert.IsNotNull(provider.Find("Exception"), "'Exception' not found.");
				Assert.IsNull(provider.Find("String"), "'String' found.");
			});
		}

		/// <summary>
		/// Bug 4688 - No code completion in nested using statements
		/// </summary>
		[Test]
		public void TestBug4688()
		{
			CombinedProviderTest(
@"using System;

public class TestFoo
{
	void Bar ()
	{
		// Read the file from
		$using (S$
	}
}

", provider => Assert.IsNotNull(provider.Find("String"), "'String'not found."));
		}

		/// <summary>
		/// Bug 4808 - Enums have an unknown 'split_char' member included in them.
		/// </summary>
		[Test]
		public void TestBug4808()
		{
			var provider = CreateProvider(
@"using System;

enum Foo { A, B }
public class TestFoo
{
	void Bar ()
	{
		$Foo.$
	}
}
"
			);
			Assert.IsNotNull(provider.Find("A"));
			Assert.IsNotNull(provider.Find("B"));
			Assert.IsNull(provider.Find("split_char"), "'split_char' found.");
		}


		/// <summary>
		/// Bug 4961 - Code completion for enumerations in static classes doesn't work.
		/// </summary>
		[Test]
		public void TestBug4961()
		{
			CombinedProviderTest(
				@"using System;
using System.Collections.Generic;

namespace EnumerationProblem
{
	public enum Options
	{
		GiveCompletion,
		IwouldLoveIt,
	}
	
	static class Converter
	{
		private static Dictionary<Options, string> options = new Dictionary<Options, string> () 
		{
			${ Options.$
		};
	}
}

", provider => {
				Assert.IsNotNull(provider.Find("GiveCompletion"));
				Assert.IsNotNull(provider.Find("IwouldLoveIt"));
			});
		}

		/// <summary>
		/// Bug 5191 - Creating extension method problem when typing "this" 
		/// </summary>
		[Test]
		public void TestBug5191()
		{
			CombinedProviderTest(
@"using System;

static class Ext
{
	$public static void Foo(t$
}
", provider => Assert.IsNotNull(provider.Find("this"), "'this' not found."));

			CombinedProviderTest(
@"using System;

static class Ext
{
	$public static void Foo(int foo, t$
}
", provider => Assert.IsNull(provider.Find("this"), "'this' found."));
		}
		
		/// <summary>
		/// Bug 5404 - Completion and highlighting for pointers 
		/// </summary>
		[Test]
		public void TestBug5404()
		{
			CombinedProviderTest(
				@"using System;

namespace TestConsole
{
unsafe class MainClass
{
public int i = 5, j =19;

public static void Main (string[] args)
{
MainClass*  mc;
$mc->$
}
}
}
", provider => Assert.IsNotNull(provider.Find("i"), "'i' not found."));
		}
		
		/// <summary>
		/// Bug 6146 - No intellisense on value keyword in property set method
		/// </summary>
		[Test]
		public void TestBug6146()
		{
			CombinedProviderTest(
				@"using System;
public class FooBar
{
	public FooBar Foo {
		set {
			$value.$
		}
	}
}

", provider => Assert.IsNotNull(provider.Find("Foo")));
		}


		[Test]
		public void TestBug6146Case2()
		{
			CombinedProviderTest(
				@"using System;
public class FooBar
{
	public FooBar Foo {
		set {
			$value.Foo.F$
		}
	}
}

", provider => Assert.IsNotNull(provider.Find("Foo")));
		}

		[Test]
		public void TestCompletionInPreprocessorIf()
		{
			CombinedProviderTest(
				@"using System;
public class FooBar
{
	public static void Main (string[] args)
	{
		#if TEST
		$Console.$
		#endif
	}
}

", provider => Assert.IsNotNull(provider.Find("WriteLine")));
		}

		[Test]
		public void TestCompletionInUndefinedPreprocessorIf()
		{
			CombinedProviderTest(
				@"using System;
public class FooBar
{
	public static void Main (string[] args)
	{
		#if UNDEFINED
		$Console.$
		#endif
	}
}

", provider => Assert.IsNull(provider.Find("WriteLine")));
		}

		/// <summary>
		/// Bug 7041 - No completion inside new[]
		/// </summary>
		[Test]
		public void TestBug7041()
		{
			CombinedProviderTest(
				@"using System;

		namespace ConsoleApplication2
		{
			class Test
			{
				public string[] Foo { get; set; }
			}

			class Program
			{
				static void Main(string[] args)
				{
					var a = new Test ()
					{
						$Foo = new [] { S$
					}

				}
			}
		}

", provider => Assert.IsNotNull(provider.Find("System")));
		}

		[Test]
		public void TestGlobalPrimitiveTypes()
		{
			CombinedProviderTest(
				@"$u$", provider => {
				Assert.IsNotNull(provider.Find("using"));
				Assert.IsNull(provider.Find("ushort"));
			});
		}

		[Test]
		public void TestGlobalPrimitiveTypesCase2()
		{
			CombinedProviderTest(
				@"$delegate u$", provider => {
				Assert.IsNotNull(provider.Find("ushort"));
				Assert.IsNotNull(provider.Find("System"));
				Assert.IsNull(provider.Find("using"));
			});
		}

		/// <summary>
		/// Bug 7207 - Missing inherited enum in completion
		/// </summary>
		[Test]
		public void TestBug7207()
		{
			CombinedProviderTest(
				@"using System;

class A
{
    protected enum MyEnum
    {
        A
    }
	
	class Hidden {}

}

class C : A
{
	class NotHidden {}
    public static void Main ()
    {
       $var a2 = M$
    }
}

", provider => {
				Assert.IsNotNull(provider.Find("MyEnum"));
				Assert.IsNotNull(provider.Find("NotHidden"));
				Assert.IsNull(provider.Find("Hidden"));
			});
		}


		/// <summary>
		/// Bug 7191 - code completion problem with generic interface using nested type
		/// </summary>
		[Ignore("FixMe")]
		[Test]
		public void TestBug7191()
		{
			CombinedProviderTest(
				@"using System.Collections.Generic;
namespace bug
{
    public class Outer
    {
        public class Nested
        {
        }
    }
    public class TestClass
    {
        void Bar()
        {
            $IList<Outer.Nested> foo = new $
        }
    }
}

", provider => AssertExists(provider, "List<Outer.Nested>"));
		}


		/// <summary>
		/// Bug 6849 - Regression: Inaccesible types in completion
		/// </summary>
		[Test]
		public void TestBug6849()
		{
			CombinedProviderTest(
				@"
namespace bug
{
   public class TestClass
    {
        void Bar()
        {
            $new System.Collections.Generic.$
        }
    }
}

", provider => {
				// it's likely to be mono specific.
				Assert.IsNull(provider.Find("RBTree"));
				Assert.IsNull(provider.Find("GenericComparer"));
				Assert.IsNull(provider.Find("InternalStringComparer"));
			});
		}


		[Test]
		public void TestBug6849Case2()
		{

			CombinedProviderTest(
				@"
namespace bug
{
   public class TestClass
    {
        void Bar()
        {
            $System.Collections.Generic.$
        }
    }
}

", provider => {
				// it's likely to be mono specific.
				Assert.IsNull(provider.Find("RBTree"));
				Assert.IsNull(provider.Find("GenericComparer"));
				Assert.IsNull(provider.Find("InternalStringComparer"));
			});
		}

		/// <summary>
		/// Bug 6237 - Code completion includes private code 
		/// </summary>
		[Ignore("FixMe")]
		[Test]
		public void TestBug6237 ()
		{
			CombinedProviderTest(
				@"
namespace bug
{
   public class TestClass
    {
        void Bar()
        {
            $System.Xml.Linq.XElement.$
        }
    }
}

", provider => {
				Assert.IsTrue (provider.Count > 0);
				// it's likely to be mono specific.
				foreach (var data in provider) {
					Assert.IsFalse(data.DisplayText.StartsWith("<", StringComparison.Ordinal), "Data was:" + data.DisplayText);
				}
			});
		}


		/// <summary>
		/// Bug 7795 - Completion cannot handle nested types 
		/// </summary>
		[Test]
		public void TestBug7795 ()
		{

			CombinedProviderTest(
				@"
using System;
using System.Linq;
using System.Collections;

class Foo
{
    public enum Selector
    {
        VV
    }
}

public class Bugged
{
    static void Test (Foo.Selector selector)
    {

    }

    void Selector ()
    {

    }

    public static void Main ()
    {
        Test ($S$);
    }
}
", provider => Assert.NotNull(provider.Find ("Foo.Selector")));
		}



		/// <summary>
		/// Bug 8618 - Intellisense broken within compiler directives
		/// </summary>
		[Test]
		public void TestBug8618 ()
		{
			
			CombinedProviderTest(
				@"
public class TestClass
{
void Bar(object argument)
{
object local;
#if FOOBAR
$a$
#endif
}
}

", provider => {
				Assert.IsNull(provider.Find("argument"));
				Assert.IsNull(provider.Find("local"));
			});
		}

		[Test]
		public void TestBug8618Case2 ()
		{
			
			CombinedProviderTest(
				@"#define FOOBAR

public class TestClass
{
void Bar(object argument)
{
object local;
#if FOOBAR
$a$
#endif
}
}

", provider => {
				Assert.IsNotNull(provider.Find("argument"));
				Assert.IsNotNull(provider.Find("local"));
			});
		}

		/// <summary>
		/// Bug 8655 - Completion for attribute properties not working
		/// </summary>
		[Test]
		public void TestBug8655 ()
		{
			
			CombinedProviderTest(
				@"using System;

namespace TestConsole
{
	[AttributeUsage (AttributeTargets.Assembly, Inherited = true, AllowMultiple = true)]
	public sealed class MyAttribute : Attribute
	{
		public int NamedInt { get; set; }
		public int[] Categories { get; set; }

		public MyAttribute (string[] str) { }
	}


	$[MyAttribute(new[] {""Foo"", ""Bar""}, Categories = new[] {1,2,3}, n$
	class MainClass
	{
	}
}


", provider => {
				Assert.IsNotNull(provider.Find("NamedInt"));
				// Assert.IsNull(provider.Find("delegate"));
			});
		}

		/// <summary>
		/// Bug 9026 - Completion shows inaccesible members 
		/// </summary>
		[Test]
		public void TestBug9026 ()
		{
			
			CombinedProviderTest(
				@"using System;
class Test { class Foo {} }

class MainClass
{
	public static void Main (string[] args)
	{
		$new Test.$
	}
}


", provider => Assert.IsNull(provider.Find("Foo")));
		}

		/// <summary>
		/// Bug 9115 - Code completion fumbles on named lambda parameters.
		/// </summary>
		[Test]
		public void TestBug9115 ()
		{
			
			CombinedProviderTest(
				@"using System;

class MainClass
{

	static void Run(Action<int> act) { }
	public static void Main (string[] args)
	{
		$Run(act: i$
	}
}


", provider => Assert.IsFalse(provider.AutoSelect));
		}

		/// <summary>
		/// Bug 9896 - Wrong dot completion
		/// </summary>
		[Test]
		public void TestBug9896 ()
		{
			
			CombinedProviderTest(
				@"using System; 

public class Testing 
{ 
    public static void DoNothing() {} 

    public static void Main() 
    { 
        $DoNothing ().$
    } 
}

", AssertEmpty);
		}

		/// <summary>
		///Bug 9905 - Cannot type new() constraint 
		/// </summary>
		[Test]
		public void TestBug9905 ()
		{
			
			CombinedProviderTest(
				@"using System; 

public class Testing 
{ 
    public static void DoNothing<T>() where T : class$, n$
	{
	} 
}

", provider => Assert.IsNotNull(provider.Find("new")));
		}

		/// <summary>
		/// Bug 10361 - No completion for optional attribute arguments
		/// </summary>
		[Test]
		public void TestBug10361 ()
		{
			CombinedProviderTest(
				@"using System;
		
		namespace test {
			class RequestAttribute : Attribute {
				public int RequestId { get; set; }
				public bool RequireLogin { get; set; }
				
				public RequestAttribute (int requestId, bool requireLogin = false) {
					RequestId = requestId;
					RequireLogin = requireLogin;
				}
			}
			
			class MainClass {
				[RequestAttribute(5$, r$)]
				public static void Main (string[] args) {
					Console.WriteLine(""Hello World!"");
				}
			}
		}
", provider => Assert.IsNotNull(provider.Find("requireLogin:")));
		}

		/// <summary>
		/// NullReferenceException when inserting space after 'in' modifier
		/// </summary>
		[Test]
		public void TestCrashContravariantTypeParameter ()
		{
			CompletionResult provider = CreateProvider (
				@"public delegate void ModelCollectionChangedEventHandler<in$ $T>();
");
			Assert.AreEqual(0, provider.Count);
		}

		[Test]
		public void TestSwitchCase ()
		{

			CombinedProviderTest(
				@"using System;
class Test
{
	public void Test (ConsoleColor color)
	{
		$switch (c$
	}
}
", provider => Assert.IsNotNull(provider.Find("color")));
		}

		[Test]
		public void TestSwitchCaseCase ()
		{

			CombinedProviderTest(
				@"using System;
class Test
{
	public void Test (ConsoleColor color)
	{
		switch (color) {
			$case C$
		}
	}
}
", provider => Assert.IsNotNull(provider.Find("ConsoleColor")));
		}

		/// <summary>
		/// Bug 11906 - Intellisense choice injects full name on edit of existing name.
		/// </summary>
		[Test]
		public void TestBug11906()
		{
			// The bug was caused by completion popping up in the middle of a word.
			var provider = CreateProvider(@"using System;
using System.Threading.Tasks;

enum Test_Struct {
	Some_Value1,
	Some_Value2,
	Some_Value3
}

public class Test
{
	public static void Main (string[] args)
	{
		Test_Struct v1 = Test_Struct.Some_$V$Value2;
	}
}");
			Assert.IsTrue(provider == null || provider.Count == 0);
		}

		[Test]
		public void TestBugWithLambdaParameter()
		{
			CombinedProviderTest(@"using System.Collections.Generic;

		class C
		{
			public static void Main (string[] args)
			{
				List<string> list;
				$list.Find(l => l.Name == l.Name ? l$
			}
		}", provider => Assert.IsNotNull(provider.Find("l")));
		}

		[Test]
		public void TestLexerBug ()
		{
			CompletionResult provider = CreateProvider (
				@"
public class TestMe : System.Object
{
/*

	//*/
	$override $
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Equals"), "method 'Equals' not found.");
		}

		/// <summary>
		/// Bug 13366 - Task result cannot be resolved in incomplete task continution
		/// </summary>
		[Test]
		public void TestBug13366 ()
		{
			var provider = CreateProvider (
				@"using System;
using System.Threading.Tasks;

public class TestMe
{
	
	void Test ()
	{
		$Task.Factory.StartNew (() => 5).ContinueWith (t => t.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Result"), "property 'Result' not found.");
		}

		[Ignore("Fixme")]
		[Test]
		public void TestBug13366Case2 ()
		{
			var provider = CreateProvider (
				@"using System;

class A { public void AMethod () {} }
class B { public void BMethod () {} }

public class TestMe
{
	void Foo(Action<A> act) {}
	void Foo(Action<B> act) {}
	
	void Test ()
	{
		$Foo(a => a.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("AMethod"), "method 'AMethod' not found.");
			Assert.IsNotNull (provider.Find ("BMethod"), "method 'BMethod' not found.");
		}

		/// <summary>
		/// Bug 13746 - Not useful completion for async delegates 
		/// </summary>
		[Test]
		public void TestBug13746 ()
		{
			var provider = CreateProvider (
				@"using System;
using System.Threading.Tasks;

class Test
{
    public static void Main()
    {
        var c = new HttpClient ();
        $Task.Run (a$
        return;
    }
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual(1, provider.Data.Count(cd => cd.DisplayText == "async delegate"));
			Assert.AreEqual(1, provider.Data.Count(cd => cd.DisplayText == "() =>"));
			Assert.AreEqual(1, provider.Data.Count(cd => cd.DisplayText == "async () =>"));
		}

		[Ignore]
		[Test]
		public void TestBasicIntersectionProblem ()
		{
			CombinedProviderTest(@"using System;

class A { public int AInt { get { return 1; } } }
class B { public int BInt { get { return 0; } } }

class Testm
{
	public void Foo (Action<A> a) {}
	public void Foo (Action<B> b) {}

	public void Bar ()
	{
		$Foo(x => x.$
	}
}", provider => {
				Assert.IsNotNull (provider.Find ("AInt"), "property 'AInt' not found.");
				Assert.IsNotNull (provider.Find ("BInt"), "property 'BInt' not found.");
			});
		}

		[Test]
		public void TestComplexIntersectionTypeProblem ()
		{
			CombinedProviderTest(@"using System.Threading.Tasks;
using System.Linq;

class Foo
{
	public void Bar ()
	{
		$Task.Factory.ContinueWhenAll (new[] { Task.Factory.StartNew (() => 5) }, t => t.Select (r => r.$
	}
}", provider => Assert.IsNotNull(provider.Find("Result"), "property 'Result' not found."));
		}

		/// <summary>
		/// Bug 8795 - Completion shows namespace entry which in not usable
		/// </summary>
		[Test]
		public void TestBug8795 ()
		{
			CombinedProviderTest(@"namespace A.B
{
    public class Foo
    {
    }
}
namespace Foo
{
    using A.B;

    class MainClass
    {
        public static void Main ()
        {
            $F$
        }
    }
}
", provider => provider.Data.Single(d => d.DisplayText == "Foo"));
		}
	
		/// <summary>
		/// Bug 10228 - [AST] Incomplete linq statements missing 
		/// </summary>
		[Test]
		public void TestBug10228 ()
		{
			CombinedProviderTest(@"using System;
using System.Linq;
using System.Collections.Generic;

class Program
{
	public void Hello()
	{
		var somelist = new List<object>();
		$var query = from item in somelist group i$
	}
}

", provider => Assert.IsNotNull(provider.Find("item"), "'item' not found."));
		}


		/// <summary>
		/// Bug 15183 - New completion in params suggests array type 
		/// </summary>
		[Test]
		public void TestBug15183 ()
		{
			CombinedProviderTest(@"class Foo
{
	static void Bar (params Foo[] args)
	{
		$Bar (new $
	}
}
", provider => Assert.IsNotNull(provider.Find("Foo"), "'Foo' not found."));
		}

		/// <summary>
		/// Bug 15387 - Broken completion for class inheritance at namespace level 
		/// </summary>
		[Test]
		public void TestBug15387 ()
		{
			CombinedProviderTest(@"using System;
$class Foo : I$
", provider => Assert.IsNotNull(provider.Find("IDisposable"), "'IDisposable' not found."));
		}

		/// <summary>
		/// Bug 15550 - Inheritance completion 
		/// </summary>
		[Test]
		public void TestBug15550 ()
		{
			CombinedProviderTest(@"using System;
$class Foo : C$
", provider => Assert.IsNull(provider.Find("Console"), "'Console' found (static class)."));
		}

		[Test]
		public void TestBug15550Case2 ()
		{
			CombinedProviderTest(@"using System;
$class Foo : IDisposable, F$
", provider => Assert.IsNull(provider.Find("Activator"), "'Activator' found (sealed class)."));
		}


		[Test]
		public void TestGotoCompletion ()
		{
			var provider = CreateCtrlSpaceProvider(@"using System;

class Program
{
	public void Hello()
	{
		$goto i$
	}
}

");
			Assert.IsTrue(provider == null || provider.Count == 0); 
		}

		/// <summary>
		/// Bug 17653 - Wrong completion entry in tuple factory method
		/// </summary>
		[Test]
		public void TestBug17653 ()
		{
			CombinedProviderTest(@"using System;
class Foo
{
	public static void Main (string[] args)
	{
		$Tuple.Create(new $
	}
}
", provider => Assert.IsNull(provider.Find("T1"), "'T1' found (type parameter)."));
		}

		[Test]
		public void TestBug17653_ValidTypeParameterCreation ()
		{
			CombinedProviderTest(@"using System;
class Foo<T1> where T1 : new()
{
	public static void Main (string[] args)
	{
		$T1 t = new $
	}
}
", provider => { 
				Assert.IsNotNull(provider.Find("T1"), "'T1' found (type parameter).");
				Assert.AreEqual("T1", provider.DefaultCompletionString);
			});
		}

		[Test]
		public void TestDoubleWhitespace ()
		{
			var provider = CreateProvider(@"using System;

class Program
{
	public void Hello()
	{
	$	 $
	}
}

");
			AssertEmpty (provider);
		}

		[Test]
		public void TestSpaceAfterSemicolon ()
		{
			var provider = CreateProvider(@"using System;

class Program
{
	public void Hello()
	{
		Hello();$ $
	}
}

");
			AssertEmpty (provider);
		}

		[Test]
		public void TestSpaceAfterParens ()
		{
			var provider = CreateProvider(@"using System;

class Program
{
	public void Hello()
	{
		Hello($ $);
	}
}

");
			AssertEmpty (provider);
		}
			}
}
