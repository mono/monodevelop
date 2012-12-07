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
using System.Collections.Generic;
using System.Linq;
using System;

namespace MonoDevelop.RazorGenerator
{
	class PreprocessedTemplateCodeTransformers
	{
		public static void MakePartialAndRemoveCtor (RazorHost host, CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
		{
			generatedClass.IsPartial = true;
			// The generated class has a constructor in there by default.
			generatedClass.Members.Remove (generatedClass.Members.OfType<CodeConstructor> ().Single ());
		}

		public static void AddGeneratedTemplateClassAttribute (RazorHost host, CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
		{
			string tool = "RazorTemplatePreprocessor";
			Version version = typeof (PreprocessedTemplateCodeTransformers).Assembly.GetName().Version;
			generatedClass.CustomAttributes.Add(
				new CodeAttributeDeclaration(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).FullName,
					new CodeAttributeArgument(new CodePrimitiveExpression(tool)),
					new CodeAttributeArgument(new CodePrimitiveExpression(version.ToString()))
			));
		}

		static void AddComments (CodeTypeMember member, bool docComment, params string[] comments)
		{
			foreach (var c in comments) {
				member.Comments.Add (new CodeCommentStatement (c, docComment));
			}
		}

		public static void InjectBaseClass (RazorHost host, CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
		{
			bool generateBaseClass = generatedClass.BaseTypes.Count == 0;
			bool integrateHelpers = !generateBaseClass && generatedClass.BaseTypes [0].BaseType == "object";
			if (!generateBaseClass && !integrateHelpers)
				return;

			CodeTypeDeclaration helperClass = generatedClass;

			if (generateBaseClass) {
				var baseName = generatedClass.Name + "Base";
				generatedClass.BaseTypes.Add (new CodeTypeReference (baseName));

				helperClass = new CodeTypeDeclaration (baseName) {
					TypeAttributes = generatedClass.TypeAttributes | System.Reflection.TypeAttributes.Abstract,
				};
				AddComments (helperClass, false,
					"NOTE: this is the default generated helper class. You may choose to extract it to a separate file ",
					"in order to customize it or share it between multiple templates, and specify the template's base ",
					"class via the @inherits directive."
				);
				generatedNamespace.Types.Add (helperClass);

				var baseExecute = new CodeMemberMethod () {
					Name = executeMethod.Name,
					ReturnType = executeMethod.ReturnType,
					Attributes = (executeMethod.Attributes & ~MemberAttributes.Override) | MemberAttributes.Abstract,
				};
				AddComments (baseExecute, false,
					" This method is REQUIRED. The generated Razor subclass will override it with the generated code.",
					""
				);
				AddComments (baseExecute, true,
					"<summary>Executes the template, writing output to the Write and WriteLiteral methods.<summary>.",
					"<remarks>Not intended to be called directly. Call the Generate method instead.</remarks>"
				);

				helperClass.Members.Add (baseExecute);
			} else {
				generatedClass.BaseTypes [0].BaseType = "System.Object";
				executeMethod.Attributes = (executeMethod.Attributes & (~MemberAttributes.AccessMask | ~MemberAttributes.Override))
					| MemberAttributes.Private | MemberAttributes.Final;
			}

			helperClass.Members.Add (new CodeSnippetTypeMember (@"
        // This field is OPTIONAL, but used by the default implementation of Generate, Write and WriteLiteral
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
            this.__razor_writer = writer;
            Execute ();
            this.__razor_writer = null;
        }

        // This method is REQUIRED, but you may choose to implement it differently
        //
		///<summary>Writes literal values to the template output without HTML escaping them.</summary>
        protected void WriteLiteral (string value)
        {
            __razor_writer.Write (value);
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

        // This method is REQUIRED if the template has any Razor helpers, but you may choose to implement it differently
        //
        ///<remarks>Used by Razor helpers to HTML escape values.</remarks>
        protected static void WriteTo (System.IO.TextWriter writer, object value)
        {
            if (value != null) {
                writer.Write (System.Web.HttpUtility.HtmlEncode (value.ToString ()));
            }
        }
        "));
		}

		/* rewrite:
		public System.Web.WebPages.HelperResult foo (int i)
		{
			return new System.Web.WebPages.HelperResult(__razor_helper_writer => {
				WriteLiteralTo(__razor_helper_writer, "<p>");
				WriteTo(__razor_helper_writer, i);
				WriteLiteralTo(__razor_helper_writer, "</p>\n");
			});
		}
		to:
		public static Action<TextWriter> foo (int i)
		{
			return __razor_helper_writer => {
				__razor_helper_writer.Write("<p>");
				WriteTo(__razor_helper_writer, i);
				__razor_helper_writer.Write("</p>\n");
			};
		}
		*/
		static string[,] replacements = new string [,] {
			{ "public System.Web.WebPages.HelperResult " , "public static Action<System.IO.TextWriter> " },
			{ "return new System.Web.WebPages.HelperResult(__razor_helper_writer" , "return __razor_helper_writer" },
			{ "WriteLiteralTo(__razor_helper_writer," , "__razor_helper_writer.Write(" },
		};

		public static void SimplifyHelpers (RazorHost host, CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
		{
			foreach (var method in generatedClass.Members.OfType<CodeSnippetTypeMember> ()) {
				using (var writer = new System.IO.StringWriter (new System.Text.StringBuilder (method.Text.Length))) {
					bool foundStart = false;
					using (var reader = new System.IO.StringReader (method.Text)) {
						bool lineHidden = false;
						string line;
						while ((line = reader.ReadLine ()) != null) {
							if (!foundStart) {
								if (line.StartsWith ("public System.Web.WebPages.HelperResult")) {
									foundStart = true;
								} else if (!string.IsNullOrWhiteSpace (line) && !line.StartsWith ("#line")) {
									break;
								}
							}
							if (line.StartsWith ("#line")) {
								lineHidden = line == "#line hidden";
							}
							if (lineHidden && line == "});") {
								writer.WriteLine ("};");
								continue;
							}
							var len = replacements.GetLength (0);
							for (int i = 0; i < len; i++) {
								var bad = replacements[i,0];
								if (line.StartsWith (bad)) {
									line = replacements[i,1] + line.Substring (bad.Length);
								}
							}
							writer.WriteLine (line);
						}
					}
					if (foundStart) {
						method.Text = writer.ToString ();
					}
				}
			}
		}
	}
}