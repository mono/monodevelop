//
// Based on TemplateCodeTransformer.cs from Razor Generator (http://razorgenerator.codeplex.com/)
//     Licensed under the Microsoft Public License (MS-PL)
//
// Changes:
//     Author: Michael Hutchinson <mhutch@xamarin.com>
//     Copyright (c) 2012 Xamarin Inc (http://xamarin.com)
//     Licensed under the Microsoft Public License (MS-PL)
//

using System.CodeDom;
using System.Linq;
using System;

namespace MonoDevelop.RazorGenerator
{
	static class PreprocessedTemplateCodeTransformers
	{
		public static void MakePartialAndRemoveCtor (CodeTypeDeclaration generatedClass)
		{
			generatedClass.IsPartial = true;
			// The generated class has a constructor in there by default.
			generatedClass.Members.Remove (generatedClass.Members.OfType<CodeConstructor> ().Single ());
		}

		public static void AddGeneratedTemplateClassAttribute (CodeTypeDeclaration generatedClass)
		{
			string tool = "RazorTemplatePreprocessor";
			Version version = typeof(PreprocessedTemplateCodeTransformers).Assembly.GetName ().Version;
			generatedClass.CustomAttributes.Add (
				new CodeAttributeDeclaration (typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).FullName,
					new CodeAttributeArgument (new CodePrimitiveExpression (tool)),
					new CodeAttributeArgument (new CodePrimitiveExpression (version.ToString ()))
				));
		}

		static void AddComments (CodeTypeMember member, bool docComment, params string[] comments)
		{
			foreach (var c in comments) {
				member.Comments.Add (new CodeCommentStatement (c, docComment));
			}
		}

		public static void InjectBaseClass (CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
		{
			bool generateBaseClass = generatedClass.BaseTypes.Count == 0;
			bool integrateHelpers = !generateBaseClass && generatedClass.BaseTypes [0].BaseType == "object";
			if (!generateBaseClass && !integrateHelpers)
				return;

			if (generateBaseClass) {
				var baseName = generatedClass.Name + "Base";
				generatedClass.BaseTypes.Add (new CodeTypeReference (baseName));

				var baseClass = new CodeTypeDeclaration (baseName) {
					TypeAttributes = generatedClass.TypeAttributes | System.Reflection.TypeAttributes.Abstract,
				};
				AddComments (baseClass, false,
					"NOTE: this is the default generated helper class. You may choose to extract it to a separate file ",
					"in order to customize it or share it between multiple templates, and specify the template's base ",
					"class via the @inherits directive."
				);
				generatedNamespace.Types.Add (baseClass);

				baseClass.Members.Add (new CodeSnippetTypeMember (baseMembersString));
				baseClass.Members.Add (new CodeSnippetTypeMember (baseExecuteMethodString));
			} else {
				generatedClass.BaseTypes [0].BaseType = "System.Object";
				executeMethod.Attributes = (executeMethod.Attributes & (~MemberAttributes.AccessMask | ~MemberAttributes.Override))
				| MemberAttributes.Private | MemberAttributes.Final;
				generatedClass.Members.Add (new CodeSnippetTypeMember (baseMembersString));
			}
		}

		const string baseExecuteMethodString =
@"		// This method is REQUIRED. The generated Razor subclass will override it with the generated code.
		//
		///<summary>Executes the template, writing output to the Write and WriteLiteral methods.</summary>.
		///<remarks>Not intended to be called directly. Call the Generate method instead.</remarks>
		public abstract void Execute ();
";
		const string baseMembersString =
@"		// This field is OPTIONAL, but used by the default implementation of Generate, Write and WriteLiteral
		//
		System.IO.TextWriter __razor_writer;

		// This method is OPTIONAL
		//
		///<summary>Executes the template and returns the output as a string.</summary>
		public string GenerateString ()
		{
			using (var sw = new System.IO.StringWriter ()) {
				Generate (sw);
				return sw.ToString();
			}
		}

		// This method is OPTIONAL, you may choose to implement Write and WriteLiteral without use of __razor_writer
		// and provide another means of invoking Execute.
		//
		///<summary>Executes the template, writing to the provided text writer.</summary>
		public void Generate (System.IO.TextWriter writer)
		{
			__razor_writer = writer;
			Execute ();
			__razor_writer = null;
		}

		// This method is REQUIRED, but you may choose to implement it differently
		//
		///<summary>Writes literal values to the template output without HTML escaping them.</summary>
		protected void WriteLiteral (string value)
		{
			__razor_writer.Write (value);
		}

		// This method is REQUIRED if the template uses any Razor helpers, but you may choose to implement it differently
		//
		///<summary>Writes literal values to the TextWriter without HTML escaping them.</summary>
		protected static void WriteLiteralTo (System.IO.TextWriter writer, string value)
		{
			writer.Write (value);
		}

		// This method is REQUIRED, but you may choose to implement it differently
		//
		///<summary>Writes values to the template output, HTML escaping them if necessary.</summary>
		protected void Write (object value)
		{
			WriteTo (__razor_writer, value);
		}

		// This method is REQUIRED if the template uses any Razor helpers, but you may choose to implement it differently
		//
		///<summary>Invokes the action to write directly to the template output.</summary>
		///<remarks>This is used for Razor helpers, which already perform any necessary HTML escaping.</remarks>
		protected void Write (Action<System.IO.TextWriter> write)
		{
			write (__razor_writer);
		}

		// This method is REQUIRED, but you may choose to implement it differently
		//
		///<summary>Writes an object value to the TextWriter, HTML escaping it if necessary.</summary>
		///<remarks>Used by Razor helpers to HTML escape values.</remarks>
		protected static void WriteTo (System.IO.TextWriter writer, object value)
		{
			if (value == null)
				return;
			//NOTE: a more sophisticated implementation would write safe and pre-escaped values directly to the
			//instead of double-escaping. See System.Web.IHtmlString in ASP.NET 4.0 for an example of this.
			System.Net.WebUtility.HtmlEncode (value.ToString (), writer);
		}

		protected static void WriteAttributeTo (System.IO.TextWriter writer, string name, string prefix, string suffix, params Tuple<object, bool>[] values)
		{
			throw new NotImplementedException ();
		}

		protected static void WriteAttribute (string name, string prefix, string suffix, params Tuple<object, bool>[] values)
		{
			WriteAttributeTo (__razor_writer, name, prefix, suffix, values);
		}
";
	}
}