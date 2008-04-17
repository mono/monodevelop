// created by jay 0.7 (c) 1998 Axel.Schreiner@informatik.uni-osnabrueck.de

#line 2 "cs-parser.jay"
//
// cs-parser.jay: The Parser for the C# compiler
//
// Authors: Miguel de Icaza (miguel@gnu.org)
//          Ravi Pratap     (ravi@ximian.com)
//          Marek Safar		(marek.safar@gmail.com)
//
// Licensed under the terms of the GNU GPL
//
// (C) 2001 Ximian, Inc (http://www.ximian.com)
// (C) 2004 Novell, Inc
//
// TODO:
//   (1) Figure out why error productions dont work.  `type-declaration' is a
//       great spot to put an `error' because you can reproduce it with this input:
//	 "public X { }"
//
// Possible optimization:
//   Run memory profiler with parsing only, and consider dropping 
//   arraylists where not needed.   Some pieces can use linked lists.
//
using System.Text;
using System.IO;
using System;

namespace Mono.CSharp
{
	using System.Collections;

	/// <summary>
	///    The C# Parser
	/// </summary>
	public class CSharpParser {
		NamespaceEntry  current_namespace;
		TypeContainer   current_container;
		DeclSpace	current_class;
	
		IAnonymousHost anonymous_host;

		/// <summary>
		///   Current block is used to add statements as we find
		///   them.  
		/// </summary>
		Block      current_block;

		Delegate   current_delegate;
		
		GenericMethod current_generic_method;
		AnonymousMethodExpression current_anonymous_method;

		/// <summary>
		///   This is used by the unary_expression code to resolve
		///   a name against a parameter.  
		/// </summary>
		Parameters current_local_parameters;

		/// <summary>
		///   Using during property parsing to describe the implicit
		///   value parameter that is passed to the "set" and "get"accesor
		///   methods (properties and indexers).
		/// </summary>
		Expression implicit_value_parameter_type;
		Parameters indexer_parameters;

		/// <summary>
		///   Hack to help create non-typed array initializer
		/// </summary>
		public static Expression current_array_type;
		Expression pushed_current_array_type;

		/// <summary>
		///   Used to determine if we are parsing the get/set pair
		///   of an indexer or a property
		/// </summmary>
		bool parsing_indexer;

		bool parsing_anonymous_method;

		///
		/// An out-of-band stack.
		///
		static Stack oob_stack;

		///
		/// Switch stack.
		///
		Stack switch_stack;

		static public int yacc_verbose_flag;

		///
		/// The current file.
		///
		SourceFile file;

		///
		/// Temporary Xml documentation cache.
		/// For enum types, we need one more temporary store.
		///
		string tmpComment;
		string enumTypeComment;
	       		
		/// Current attribute target
		string current_attr_target;
		
		/// assembly and module attribute definitions are enabled
		bool global_attrs_enabled = true;
		bool has_get, has_set;
		bool parameter_modifiers_not_allowed;
		bool params_modifiers_not_allowed;
		bool arglist_allowed;

		readonly CompilationUnit cu;
#line default

  /** error output stream.
      It should be changeable.
    */
  public System.IO.TextWriter ErrorOutput = System.Console.Out;

  /** simplified error message.
      @see <a href="#yyerror(java.lang.String, java.lang.String[])">yyerror</a>
    */
  public void yyerror (string message) {
    yyerror(message, null);
  }

  /** (syntax) error message.
      Can be overwritten to control message format.
      @param message text to be displayed.
      @param expected vector of acceptable tokens, if available.
    */
  public void yyerror (string message, string[] expected) {
    if ((yacc_verbose_flag > 0) && (expected != null) && (expected.Length  > 0)) {
      ErrorOutput.Write (message+", expecting");
      for (int n = 0; n < expected.Length; ++ n)
        ErrorOutput.Write (" "+expected[n]);
        ErrorOutput.WriteLine ();
    } else
      ErrorOutput.WriteLine (message);
  }

  /** debugging support, requires the package jay.yydebug.
      Set to null to suppress debugging messages.
    */
  internal yydebug.yyDebug debug;

  protected static  int yyFinal = 5;
 // Put this array into a separate class so it is only initialized if debugging is actually used
 // Use MarshalByRefObject to disable inlining
 class YYRules : MarshalByRefObject {
  public static  string [] yyRule = {
    "$accept : compilation_unit",
    "compilation_unit : outer_declarations opt_EOF",
    "compilation_unit : outer_declarations global_attributes opt_EOF",
    "compilation_unit : global_attributes opt_EOF",
    "compilation_unit : opt_EOF",
    "opt_EOF :",
    "opt_EOF : EOF",
    "outer_declarations : outer_declaration",
    "outer_declarations : outer_declarations outer_declaration",
    "outer_declaration : extern_alias_directive",
    "outer_declaration : using_directive",
    "outer_declaration : namespace_member_declaration",
    "extern_alias_directives : extern_alias_directive",
    "extern_alias_directives : extern_alias_directives extern_alias_directive",
    "extern_alias_directive : EXTERN IDENTIFIER IDENTIFIER SEMICOLON",
    "using_directives : using_directive",
    "using_directives : using_directives using_directive",
    "using_directive : using_alias_directive",
    "using_directive : using_namespace_directive",
    "using_alias_directive : USING IDENTIFIER ASSIGN namespace_or_type_name SEMICOLON",
    "using_alias_directive : USING error",
    "using_namespace_directive : USING namespace_name SEMICOLON",
    "$$1 :",
    "namespace_declaration : opt_attributes NAMESPACE namespace_or_type_name $$1 namespace_body opt_semicolon",
    "opt_semicolon :",
    "opt_semicolon : SEMICOLON",
    "opt_comma :",
    "opt_comma : COMMA",
    "namespace_name : namespace_or_type_name",
    "$$2 :",
    "namespace_body : OPEN_BRACE $$2 namespace_body_body",
    "namespace_body_body : opt_extern_alias_directives opt_using_directives opt_namespace_member_declarations CLOSE_BRACE",
    "$$3 :",
    "namespace_body_body : error $$3 CLOSE_BRACE",
    "namespace_body_body : opt_extern_alias_directives opt_using_directives opt_namespace_member_declarations EOF",
    "opt_using_directives :",
    "opt_using_directives : using_directives",
    "opt_extern_alias_directives :",
    "opt_extern_alias_directives : extern_alias_directives",
    "opt_namespace_member_declarations :",
    "opt_namespace_member_declarations : namespace_member_declarations",
    "namespace_member_declarations : namespace_member_declaration",
    "namespace_member_declarations : namespace_member_declarations namespace_member_declaration",
    "namespace_member_declaration : type_declaration",
    "namespace_member_declaration : namespace_declaration",
    "namespace_member_declaration : field_declaration",
    "namespace_member_declaration : method_declaration",
    "type_declaration : class_declaration",
    "type_declaration : struct_declaration",
    "type_declaration : interface_declaration",
    "type_declaration : enum_declaration",
    "type_declaration : delegate_declaration",
    "global_attributes : attribute_sections",
    "opt_attributes :",
    "opt_attributes : attribute_sections",
    "attribute_sections : attribute_section",
    "attribute_sections : attribute_sections attribute_section",
    "attribute_section : OPEN_BRACKET attribute_target_specifier attribute_list opt_comma CLOSE_BRACKET",
    "attribute_section : OPEN_BRACKET attribute_list opt_comma CLOSE_BRACKET",
    "attribute_target_specifier : attribute_target COLON",
    "attribute_target : IDENTIFIER",
    "attribute_target : EVENT",
    "attribute_target : RETURN",
    "attribute_target : error",
    "attribute_list : attribute",
    "attribute_list : attribute_list COMMA attribute",
    "attribute : attribute_name opt_attribute_arguments",
    "attribute_name : namespace_or_type_name",
    "opt_attribute_arguments :",
    "opt_attribute_arguments : OPEN_PARENS attribute_arguments CLOSE_PARENS",
    "attribute_arguments : opt_positional_argument_list",
    "attribute_arguments : positional_argument_list COMMA named_argument_list",
    "attribute_arguments : named_argument_list",
    "opt_positional_argument_list :",
    "opt_positional_argument_list : positional_argument_list",
    "positional_argument_list : expression",
    "positional_argument_list : positional_argument_list COMMA expression",
    "named_argument_list : named_argument",
    "named_argument_list : named_argument_list COMMA named_argument",
    "named_argument_list : named_argument_list COMMA expression",
    "named_argument : IDENTIFIER ASSIGN expression",
    "class_body : OPEN_BRACE opt_class_member_declarations CLOSE_BRACE",
    "opt_class_member_declarations :",
    "opt_class_member_declarations : class_member_declarations",
    "class_member_declarations : class_member_declaration",
    "class_member_declarations : class_member_declarations class_member_declaration",
    "class_member_declaration : constant_declaration",
    "class_member_declaration : field_declaration",
    "class_member_declaration : method_declaration",
    "class_member_declaration : property_declaration",
    "class_member_declaration : event_declaration",
    "class_member_declaration : indexer_declaration",
    "class_member_declaration : operator_declaration",
    "class_member_declaration : constructor_declaration",
    "class_member_declaration : destructor_declaration",
    "class_member_declaration : type_declaration",
    "$$4 :",
    "$$5 :",
    "$$6 :",
    "$$7 :",
    "struct_declaration : opt_attributes opt_modifiers opt_partial STRUCT $$4 type_name $$5 opt_class_base opt_type_parameter_constraints_clauses $$6 struct_body $$7 opt_semicolon",
    "struct_declaration : opt_attributes opt_modifiers opt_partial STRUCT error",
    "$$8 :",
    "struct_body : OPEN_BRACE $$8 opt_struct_member_declarations CLOSE_BRACE",
    "opt_struct_member_declarations :",
    "opt_struct_member_declarations : struct_member_declarations",
    "struct_member_declarations : struct_member_declaration",
    "struct_member_declarations : struct_member_declarations struct_member_declaration",
    "struct_member_declaration : constant_declaration",
    "struct_member_declaration : field_declaration",
    "struct_member_declaration : method_declaration",
    "struct_member_declaration : property_declaration",
    "struct_member_declaration : event_declaration",
    "struct_member_declaration : indexer_declaration",
    "struct_member_declaration : operator_declaration",
    "struct_member_declaration : constructor_declaration",
    "struct_member_declaration : type_declaration",
    "struct_member_declaration : destructor_declaration",
    "constant_declaration : opt_attributes opt_modifiers CONST type constant_declarators SEMICOLON",
    "constant_declarators : constant_declarator",
    "constant_declarators : constant_declarators COMMA constant_declarator",
    "constant_declarator : IDENTIFIER ASSIGN constant_expression",
    "constant_declarator : IDENTIFIER",
    "field_declaration : opt_attributes opt_modifiers type variable_declarators SEMICOLON",
    "field_declaration : opt_attributes opt_modifiers FIXED type fixed_variable_declarators SEMICOLON",
    "field_declaration : opt_attributes opt_modifiers FIXED type error",
    "field_declaration : opt_attributes opt_modifiers VOID variable_declarators SEMICOLON",
    "fixed_variable_declarators : fixed_variable_declarator",
    "fixed_variable_declarators : fixed_variable_declarators COMMA fixed_variable_declarator",
    "fixed_variable_declarator : IDENTIFIER OPEN_BRACKET expression CLOSE_BRACKET",
    "fixed_variable_declarator : IDENTIFIER OPEN_BRACKET CLOSE_BRACKET",
    "variable_declarators : variable_declarator",
    "variable_declarators : variable_declarators COMMA variable_declarator",
    "variable_declarator : IDENTIFIER ASSIGN variable_initializer",
    "variable_declarator : IDENTIFIER",
    "variable_declarator : IDENTIFIER OPEN_BRACKET opt_expression CLOSE_BRACKET",
    "variable_initializer : expression",
    "variable_initializer : array_initializer",
    "variable_initializer : STACKALLOC type OPEN_BRACKET expression CLOSE_BRACKET",
    "variable_initializer : ARGLIST",
    "variable_initializer : STACKALLOC type",
    "$$9 :",
    "method_declaration : method_header $$9 method_body",
    "opt_error_modifier :",
    "opt_error_modifier : modifiers",
    "open_parens : OPEN_PARENS",
    "open_parens : OPEN_PARENS_LAMBDA",
    "$$10 :",
    "$$11 :",
    "method_header : opt_attributes opt_modifiers type member_name open_parens $$10 opt_formal_parameter_list CLOSE_PARENS $$11 opt_type_parameter_constraints_clauses",
    "$$12 :",
    "$$13 :",
    "method_header : opt_attributes opt_modifiers VOID member_name open_parens $$12 opt_formal_parameter_list CLOSE_PARENS $$13 opt_type_parameter_constraints_clauses",
    "$$14 :",
    "method_header : opt_attributes opt_modifiers PARTIAL VOID member_name open_parens opt_formal_parameter_list CLOSE_PARENS $$14 opt_type_parameter_constraints_clauses",
    "method_header : opt_attributes opt_modifiers type modifiers member_name open_parens opt_formal_parameter_list CLOSE_PARENS",
    "method_body : block",
    "method_body : SEMICOLON",
    "opt_formal_parameter_list :",
    "opt_formal_parameter_list : formal_parameter_list",
    "opt_parameter_list_no_mod :",
    "$$15 :",
    "opt_parameter_list_no_mod : $$15 formal_parameter_list",
    "formal_parameter_list : fixed_parameters",
    "formal_parameter_list : fixed_parameters COMMA parameter_array",
    "formal_parameter_list : fixed_parameters COMMA arglist_modifier",
    "formal_parameter_list : parameter_array COMMA error",
    "formal_parameter_list : fixed_parameters COMMA parameter_array COMMA error",
    "formal_parameter_list : arglist_modifier COMMA error",
    "formal_parameter_list : fixed_parameters COMMA ARGLIST COMMA error",
    "formal_parameter_list : parameter_array",
    "formal_parameter_list : arglist_modifier",
    "fixed_parameters : fixed_parameter",
    "fixed_parameters : fixed_parameters COMMA fixed_parameter",
    "fixed_parameter : opt_attributes opt_parameter_modifier type IDENTIFIER",
    "fixed_parameter : opt_attributes opt_parameter_modifier type IDENTIFIER OPEN_BRACKET CLOSE_BRACKET",
    "fixed_parameter : opt_attributes opt_parameter_modifier type",
    "fixed_parameter : opt_attributes opt_parameter_modifier type error",
    "fixed_parameter : opt_attributes opt_parameter_modifier type IDENTIFIER ASSIGN constant_expression",
    "opt_parameter_modifier :",
    "opt_parameter_modifier : parameter_modifiers",
    "parameter_modifiers : parameter_modifier",
    "parameter_modifiers : parameter_modifiers parameter_modifier",
    "parameter_modifier : REF",
    "parameter_modifier : OUT",
    "parameter_modifier : THIS",
    "parameter_array : opt_attributes params_modifier type IDENTIFIER",
    "parameter_array : opt_attributes params_modifier type error",
    "params_modifier : PARAMS",
    "params_modifier : PARAMS parameter_modifier",
    "params_modifier : PARAMS params_modifier",
    "arglist_modifier : ARGLIST",
    "$$16 :",
    "$$17 :",
    "$$18 :",
    "property_declaration : opt_attributes opt_modifiers type namespace_or_type_name $$16 OPEN_BRACE $$17 accessor_declarations $$18 CLOSE_BRACE",
    "accessor_declarations : get_accessor_declaration",
    "accessor_declarations : get_accessor_declaration accessor_declarations",
    "accessor_declarations : set_accessor_declaration",
    "accessor_declarations : set_accessor_declaration accessor_declarations",
    "accessor_declarations : error",
    "$$19 :",
    "get_accessor_declaration : opt_attributes opt_modifiers GET $$19 accessor_body",
    "$$20 :",
    "set_accessor_declaration : opt_attributes opt_modifiers SET $$20 accessor_body",
    "accessor_body : block",
    "accessor_body : SEMICOLON",
    "$$21 :",
    "$$22 :",
    "$$23 :",
    "$$24 :",
    "interface_declaration : opt_attributes opt_modifiers opt_partial INTERFACE $$21 type_name $$22 opt_class_base opt_type_parameter_constraints_clauses $$23 interface_body $$24 opt_semicolon",
    "interface_declaration : opt_attributes opt_modifiers opt_partial INTERFACE error",
    "interface_body : OPEN_BRACE opt_interface_member_declarations CLOSE_BRACE",
    "opt_interface_member_declarations :",
    "opt_interface_member_declarations : interface_member_declarations",
    "interface_member_declarations : interface_member_declaration",
    "interface_member_declarations : interface_member_declarations interface_member_declaration",
    "interface_member_declaration : interface_method_declaration",
    "interface_member_declaration : interface_property_declaration",
    "interface_member_declaration : interface_event_declaration",
    "interface_member_declaration : interface_indexer_declaration",
    "interface_member_declaration : delegate_declaration",
    "interface_member_declaration : class_declaration",
    "interface_member_declaration : struct_declaration",
    "interface_member_declaration : enum_declaration",
    "interface_member_declaration : interface_declaration",
    "interface_member_declaration : constant_declaration",
    "opt_new : opt_modifiers",
    "$$25 :",
    "interface_method_declaration_body : OPEN_BRACE $$25 opt_statement_list CLOSE_BRACE",
    "interface_method_declaration_body : SEMICOLON",
    "$$26 :",
    "$$27 :",
    "interface_method_declaration : opt_attributes opt_new type namespace_or_type_name open_parens opt_formal_parameter_list CLOSE_PARENS $$26 opt_type_parameter_constraints_clauses $$27 interface_method_declaration_body",
    "$$28 :",
    "$$29 :",
    "interface_method_declaration : opt_attributes opt_new VOID namespace_or_type_name open_parens opt_formal_parameter_list CLOSE_PARENS $$28 opt_type_parameter_constraints_clauses $$29 interface_method_declaration_body",
    "$$30 :",
    "$$31 :",
    "interface_property_declaration : opt_attributes opt_new type IDENTIFIER OPEN_BRACE $$30 accessor_declarations $$31 CLOSE_BRACE",
    "interface_property_declaration : opt_attributes opt_new type error",
    "interface_event_declaration : opt_attributes opt_new EVENT type IDENTIFIER SEMICOLON",
    "interface_event_declaration : opt_attributes opt_new EVENT type error",
    "interface_event_declaration : opt_attributes opt_new EVENT type IDENTIFIER ASSIGN",
    "$$32 :",
    "$$33 :",
    "interface_event_declaration : opt_attributes opt_new EVENT type IDENTIFIER OPEN_BRACE $$32 event_accessor_declarations $$33 CLOSE_BRACE",
    "$$34 :",
    "$$35 :",
    "interface_indexer_declaration : opt_attributes opt_new type THIS OPEN_BRACKET opt_parameter_list_no_mod CLOSE_BRACKET OPEN_BRACE $$34 accessor_declarations $$35 CLOSE_BRACE",
    "$$36 :",
    "operator_declaration : opt_attributes opt_modifiers operator_declarator $$36 operator_body",
    "operator_body : block",
    "operator_body : SEMICOLON",
    "$$37 :",
    "operator_declarator : type OPERATOR overloadable_operator open_parens $$37 opt_parameter_list_no_mod CLOSE_PARENS",
    "operator_declarator : conversion_operator_declarator",
    "overloadable_operator : BANG",
    "overloadable_operator : TILDE",
    "overloadable_operator : OP_INC",
    "overloadable_operator : OP_DEC",
    "overloadable_operator : TRUE",
    "overloadable_operator : FALSE",
    "overloadable_operator : PLUS",
    "overloadable_operator : MINUS",
    "overloadable_operator : STAR",
    "overloadable_operator : DIV",
    "overloadable_operator : PERCENT",
    "overloadable_operator : BITWISE_AND",
    "overloadable_operator : BITWISE_OR",
    "overloadable_operator : CARRET",
    "overloadable_operator : OP_SHIFT_LEFT",
    "overloadable_operator : OP_SHIFT_RIGHT",
    "overloadable_operator : OP_EQ",
    "overloadable_operator : OP_NE",
    "overloadable_operator : OP_GT",
    "overloadable_operator : OP_LT",
    "overloadable_operator : OP_GE",
    "overloadable_operator : OP_LE",
    "$$38 :",
    "conversion_operator_declarator : IMPLICIT OPERATOR type open_parens $$38 opt_parameter_list_no_mod CLOSE_PARENS",
    "$$39 :",
    "conversion_operator_declarator : EXPLICIT OPERATOR type open_parens $$39 opt_parameter_list_no_mod CLOSE_PARENS",
    "conversion_operator_declarator : IMPLICIT error",
    "conversion_operator_declarator : EXPLICIT error",
    "constructor_declaration : opt_attributes opt_modifiers constructor_declarator constructor_body",
    "constructor_declarator : constructor_header",
    "constructor_declarator : constructor_header constructor_initializer",
    "$$40 :",
    "constructor_header : IDENTIFIER $$40 open_parens opt_formal_parameter_list CLOSE_PARENS",
    "constructor_body : block_prepared",
    "constructor_body : SEMICOLON",
    "constructor_initializer : COLON BASE open_parens opt_argument_list CLOSE_PARENS",
    "constructor_initializer : COLON THIS open_parens opt_argument_list CLOSE_PARENS",
    "constructor_initializer : COLON error",
    "opt_finalizer :",
    "opt_finalizer : UNSAFE",
    "opt_finalizer : EXTERN",
    "$$41 :",
    "destructor_declaration : opt_attributes opt_finalizer TILDE $$41 IDENTIFIER OPEN_PARENS CLOSE_PARENS block",
    "event_declaration : opt_attributes opt_modifiers EVENT type variable_declarators SEMICOLON",
    "$$42 :",
    "$$43 :",
    "event_declaration : opt_attributes opt_modifiers EVENT type namespace_or_type_name OPEN_BRACE $$42 event_accessor_declarations $$43 CLOSE_BRACE",
    "event_declaration : opt_attributes opt_modifiers EVENT type namespace_or_type_name error",
    "event_accessor_declarations : add_accessor_declaration remove_accessor_declaration",
    "event_accessor_declarations : remove_accessor_declaration add_accessor_declaration",
    "event_accessor_declarations : add_accessor_declaration",
    "event_accessor_declarations : remove_accessor_declaration",
    "event_accessor_declarations : error",
    "event_accessor_declarations :",
    "$$44 :",
    "add_accessor_declaration : opt_attributes ADD $$44 block",
    "add_accessor_declaration : opt_attributes ADD error",
    "add_accessor_declaration : opt_attributes modifiers ADD",
    "$$45 :",
    "remove_accessor_declaration : opt_attributes REMOVE $$45 block",
    "remove_accessor_declaration : opt_attributes REMOVE error",
    "remove_accessor_declaration : opt_attributes modifiers REMOVE",
    "$$46 :",
    "$$47 :",
    "indexer_declaration : opt_attributes opt_modifiers indexer_declarator OPEN_BRACE $$46 accessor_declarations $$47 CLOSE_BRACE",
    "indexer_declarator : type THIS OPEN_BRACKET opt_parameter_list_no_mod CLOSE_BRACKET",
    "indexer_declarator : type namespace_or_type_name DOT THIS OPEN_BRACKET opt_formal_parameter_list CLOSE_BRACKET",
    "$$48 :",
    "enum_declaration : opt_attributes opt_modifiers ENUM IDENTIFIER opt_enum_base $$48 enum_body opt_semicolon",
    "opt_enum_base :",
    "opt_enum_base : COLON type",
    "$$49 :",
    "$$50 :",
    "enum_body : OPEN_BRACE $$49 opt_enum_member_declarations $$50 CLOSE_BRACE",
    "opt_enum_member_declarations :",
    "opt_enum_member_declarations : enum_member_declarations opt_comma",
    "enum_member_declarations : enum_member_declaration",
    "enum_member_declarations : enum_member_declarations COMMA enum_member_declaration",
    "enum_member_declaration : opt_attributes IDENTIFIER",
    "$$51 :",
    "enum_member_declaration : opt_attributes IDENTIFIER $$51 ASSIGN expression",
    "$$52 :",
    "$$53 :",
    "delegate_declaration : opt_attributes opt_modifiers DELEGATE type type_name open_parens opt_formal_parameter_list CLOSE_PARENS $$52 opt_type_parameter_constraints_clauses $$53 SEMICOLON",
    "opt_nullable :",
    "opt_nullable : INTERR",
    "namespace_or_type_name : IDENTIFIER opt_type_argument_list",
    "namespace_or_type_name : IDENTIFIER DOUBLE_COLON IDENTIFIER opt_type_argument_list",
    "namespace_or_type_name : namespace_or_type_name DOT IDENTIFIER opt_type_argument_list",
    "member_name : IDENTIFIER opt_type_parameter_list",
    "member_name : namespace_or_type_name DOT IDENTIFIER opt_type_parameter_list",
    "type_name : IDENTIFIER opt_type_parameter_list",
    "opt_type_argument_list :",
    "opt_type_argument_list : OP_GENERICS_LT type_arguments OP_GENERICS_GT",
    "opt_type_parameter_list :",
    "opt_type_parameter_list : OP_GENERICS_LT type_arguments OP_GENERICS_GT",
    "type_arguments : type_argument",
    "type_arguments : type_arguments COMMA type_argument",
    "type_argument : type",
    "type_argument : attribute_sections type",
    "type : namespace_or_type_name opt_nullable",
    "type : builtin_types opt_nullable",
    "type : array_type",
    "type : pointer_type",
    "pointer_type : type STAR",
    "pointer_type : VOID STAR",
    "non_expression_type : builtin_types opt_nullable",
    "non_expression_type : non_expression_type rank_specifier",
    "non_expression_type : non_expression_type STAR",
    "non_expression_type : expression rank_specifiers",
    "non_expression_type : expression STAR",
    "non_expression_type : multiplicative_expression STAR",
    "type_list : base_type_name",
    "type_list : type_list COMMA base_type_name",
    "base_type_name : type",
    "builtin_types : OBJECT",
    "builtin_types : STRING",
    "builtin_types : BOOL",
    "builtin_types : DECIMAL",
    "builtin_types : FLOAT",
    "builtin_types : DOUBLE",
    "builtin_types : integral_type",
    "integral_type : SBYTE",
    "integral_type : BYTE",
    "integral_type : SHORT",
    "integral_type : USHORT",
    "integral_type : INT",
    "integral_type : UINT",
    "integral_type : LONG",
    "integral_type : ULONG",
    "integral_type : CHAR",
    "integral_type : VOID",
    "array_type : type rank_specifiers opt_nullable",
    "primary_expression : literal",
    "primary_expression : type_name",
    "primary_expression : IDENTIFIER DOUBLE_COLON IDENTIFIER opt_type_argument_list",
    "primary_expression : parenthesized_expression",
    "primary_expression : default_value_expression",
    "primary_expression : member_access",
    "primary_expression : invocation_expression",
    "primary_expression : element_access",
    "primary_expression : this_access",
    "primary_expression : base_access",
    "primary_expression : post_increment_expression",
    "primary_expression : post_decrement_expression",
    "primary_expression : new_expression",
    "primary_expression : typeof_expression",
    "primary_expression : sizeof_expression",
    "primary_expression : checked_expression",
    "primary_expression : unchecked_expression",
    "primary_expression : pointer_member_access",
    "primary_expression : anonymous_method_expression",
    "literal : boolean_literal",
    "literal : integer_literal",
    "literal : real_literal",
    "literal : LITERAL_CHARACTER",
    "literal : LITERAL_STRING",
    "literal : NULL",
    "real_literal : LITERAL_FLOAT",
    "real_literal : LITERAL_DOUBLE",
    "real_literal : LITERAL_DECIMAL",
    "integer_literal : LITERAL_INTEGER",
    "boolean_literal : TRUE",
    "boolean_literal : FALSE",
    "parenthesized_expression_0 : OPEN_PARENS expression CLOSE_PARENS",
    "parenthesized_expression_0 : OPEN_PARENS expression error",
    "parenthesized_expression : parenthesized_expression_0 CLOSE_PARENS_NO_CAST",
    "parenthesized_expression : parenthesized_expression_0 CLOSE_PARENS",
    "parenthesized_expression : parenthesized_expression_0 CLOSE_PARENS_MINUS",
    "member_access : primary_expression DOT IDENTIFIER opt_type_argument_list",
    "member_access : predefined_type DOT IDENTIFIER opt_type_argument_list",
    "predefined_type : builtin_types",
    "invocation_expression : primary_expression OPEN_PARENS opt_argument_list CLOSE_PARENS",
    "invocation_expression : parenthesized_expression_0 CLOSE_PARENS_OPEN_PARENS OPEN_PARENS CLOSE_PARENS",
    "invocation_expression : parenthesized_expression_0 CLOSE_PARENS_OPEN_PARENS primary_expression",
    "invocation_expression : parenthesized_expression_0 CLOSE_PARENS_OPEN_PARENS OPEN_PARENS non_simple_argument CLOSE_PARENS",
    "invocation_expression : parenthesized_expression_0 CLOSE_PARENS_OPEN_PARENS OPEN_PARENS argument_list COMMA argument CLOSE_PARENS",
    "opt_object_or_collection_initializer :",
    "opt_object_or_collection_initializer : object_or_collection_initializer",
    "object_or_collection_initializer : OPEN_BRACE opt_member_initializer_list CLOSE_BRACE",
    "object_or_collection_initializer : OPEN_BRACE member_initializer_list COMMA CLOSE_BRACE",
    "opt_member_initializer_list :",
    "opt_member_initializer_list : member_initializer_list",
    "member_initializer_list : member_initializer",
    "member_initializer_list : member_initializer_list COMMA member_initializer",
    "member_initializer : IDENTIFIER ASSIGN initializer_value",
    "member_initializer : non_assignment_expression",
    "member_initializer : OPEN_BRACE expression_list CLOSE_BRACE",
    "member_initializer : OPEN_BRACE CLOSE_BRACE",
    "initializer_value : expression",
    "initializer_value : object_or_collection_initializer",
    "opt_argument_list :",
    "opt_argument_list : argument_list",
    "argument_list : argument",
    "argument_list : argument_list COMMA argument",
    "argument_list : argument_list error",
    "argument : expression",
    "argument : non_simple_argument",
    "non_simple_argument : REF variable_reference",
    "non_simple_argument : OUT variable_reference",
    "non_simple_argument : ARGLIST OPEN_PARENS argument_list CLOSE_PARENS",
    "non_simple_argument : ARGLIST OPEN_PARENS CLOSE_PARENS",
    "non_simple_argument : ARGLIST",
    "variable_reference : expression",
    "element_access : primary_expression OPEN_BRACKET expression_list CLOSE_BRACKET",
    "element_access : primary_expression rank_specifiers",
    "expression_list : expression",
    "expression_list : expression_list COMMA expression",
    "this_access : THIS",
    "base_access : BASE DOT IDENTIFIER opt_type_argument_list",
    "base_access : BASE OPEN_BRACKET expression_list CLOSE_BRACKET",
    "base_access : BASE error",
    "post_increment_expression : primary_expression OP_INC",
    "post_decrement_expression : primary_expression OP_DEC",
    "new_expression : object_or_delegate_creation_expression",
    "new_expression : array_creation_expression",
    "new_expression : anonymous_type_expression",
    "object_or_delegate_creation_expression : NEW type OPEN_PARENS opt_argument_list CLOSE_PARENS opt_object_or_collection_initializer",
    "object_or_delegate_creation_expression : NEW type object_or_collection_initializer",
    "array_creation_expression : NEW type OPEN_BRACKET expression_list CLOSE_BRACKET opt_rank_specifier opt_array_initializer",
    "array_creation_expression : NEW type rank_specifiers array_initializer",
    "array_creation_expression : NEW rank_specifiers array_initializer",
    "array_creation_expression : NEW error",
    "array_creation_expression : NEW type error",
    "anonymous_type_expression : NEW OPEN_BRACE anonymous_type_parameters CLOSE_BRACE",
    "anonymous_type_parameters :",
    "anonymous_type_parameters : anonymous_type_parameter",
    "anonymous_type_parameters : anonymous_type_parameters COMMA anonymous_type_parameter",
    "anonymous_type_parameter : IDENTIFIER ASSIGN variable_initializer",
    "anonymous_type_parameter : IDENTIFIER",
    "anonymous_type_parameter : member_access",
    "anonymous_type_parameter : error",
    "opt_rank_specifier :",
    "opt_rank_specifier : rank_specifiers",
    "opt_rank_specifier_or_nullable :",
    "opt_rank_specifier_or_nullable : INTERR",
    "opt_rank_specifier_or_nullable : opt_nullable rank_specifiers",
    "opt_rank_specifier_or_nullable : opt_nullable rank_specifiers INTERR",
    "rank_specifiers : rank_specifier opt_rank_specifier",
    "rank_specifier : OPEN_BRACKET opt_dim_separators CLOSE_BRACKET",
    "opt_dim_separators :",
    "opt_dim_separators : dim_separators",
    "dim_separators : COMMA",
    "dim_separators : dim_separators COMMA",
    "opt_array_initializer :",
    "opt_array_initializer : array_initializer",
    "array_initializer : OPEN_BRACE CLOSE_BRACE",
    "array_initializer : OPEN_BRACE variable_initializer_list opt_comma CLOSE_BRACE",
    "variable_initializer_list : variable_initializer",
    "variable_initializer_list : variable_initializer_list COMMA variable_initializer",
    "$$54 :",
    "typeof_expression : TYPEOF $$54 OPEN_PARENS typeof_type_expression CLOSE_PARENS",
    "typeof_type_expression : type",
    "typeof_type_expression : unbound_type_name",
    "unbound_type_name : IDENTIFIER GENERIC_DIMENSION",
    "unbound_type_name : IDENTIFIER DOUBLE_COLON IDENTIFIER GENERIC_DIMENSION",
    "unbound_type_name : unbound_type_name DOT IDENTIFIER GENERIC_DIMENSION",
    "unbound_type_name : namespace_or_type_name DOT IDENTIFIER GENERIC_DIMENSION",
    "sizeof_expression : SIZEOF OPEN_PARENS type CLOSE_PARENS",
    "checked_expression : CHECKED OPEN_PARENS expression CLOSE_PARENS",
    "unchecked_expression : UNCHECKED OPEN_PARENS expression CLOSE_PARENS",
    "pointer_member_access : primary_expression OP_PTR IDENTIFIER",
    "$$55 :",
    "anonymous_method_expression : DELEGATE opt_anonymous_method_signature $$55 block",
    "opt_anonymous_method_signature :",
    "opt_anonymous_method_signature : anonymous_method_signature",
    "$$56 :",
    "anonymous_method_signature : open_parens $$56 opt_formal_parameter_list CLOSE_PARENS",
    "default_value_expression : DEFAULT_OPEN_PARENS type CLOSE_PARENS",
    "unary_expression : primary_expression",
    "unary_expression : BANG prefixed_unary_expression",
    "unary_expression : TILDE prefixed_unary_expression",
    "unary_expression : cast_expression",
    "cast_list : parenthesized_expression_0 CLOSE_PARENS_CAST unary_expression",
    "cast_list : parenthesized_expression_0 CLOSE_PARENS_NO_CAST default_value_expression",
    "cast_list : parenthesized_expression_0 CLOSE_PARENS_OPEN_PARENS cast_expression",
    "cast_expression : cast_list",
    "cast_expression : OPEN_PARENS non_expression_type CLOSE_PARENS prefixed_unary_expression",
    "prefixed_unary_expression : unary_expression",
    "prefixed_unary_expression : PLUS prefixed_unary_expression",
    "prefixed_unary_expression : MINUS prefixed_unary_expression",
    "prefixed_unary_expression : OP_INC prefixed_unary_expression",
    "prefixed_unary_expression : OP_DEC prefixed_unary_expression",
    "prefixed_unary_expression : STAR prefixed_unary_expression",
    "prefixed_unary_expression : BITWISE_AND prefixed_unary_expression",
    "multiplicative_expression : prefixed_unary_expression",
    "multiplicative_expression : multiplicative_expression STAR prefixed_unary_expression",
    "multiplicative_expression : multiplicative_expression DIV prefixed_unary_expression",
    "multiplicative_expression : multiplicative_expression PERCENT prefixed_unary_expression",
    "additive_expression : multiplicative_expression",
    "additive_expression : additive_expression PLUS multiplicative_expression",
    "additive_expression : additive_expression MINUS multiplicative_expression",
    "shift_expression : additive_expression",
    "shift_expression : shift_expression OP_SHIFT_LEFT additive_expression",
    "shift_expression : shift_expression OP_SHIFT_RIGHT additive_expression",
    "opt_error :",
    "opt_error : error",
    "nullable_type_or_conditional : type opt_error",
    "relational_expression : shift_expression",
    "relational_expression : relational_expression OP_LT shift_expression",
    "relational_expression : relational_expression OP_GT shift_expression",
    "relational_expression : relational_expression OP_LE shift_expression",
    "relational_expression : relational_expression OP_GE shift_expression",
    "$$57 :",
    "relational_expression : relational_expression IS $$57 nullable_type_or_conditional",
    "$$58 :",
    "relational_expression : relational_expression AS $$58 nullable_type_or_conditional",
    "equality_expression : relational_expression",
    "equality_expression : equality_expression OP_EQ relational_expression",
    "equality_expression : equality_expression OP_NE relational_expression",
    "and_expression : equality_expression",
    "and_expression : and_expression BITWISE_AND equality_expression",
    "exclusive_or_expression : and_expression",
    "exclusive_or_expression : exclusive_or_expression CARRET and_expression",
    "inclusive_or_expression : exclusive_or_expression",
    "inclusive_or_expression : inclusive_or_expression BITWISE_OR exclusive_or_expression",
    "conditional_and_expression : inclusive_or_expression",
    "conditional_and_expression : conditional_and_expression OP_AND inclusive_or_expression",
    "conditional_or_expression : conditional_and_expression",
    "conditional_or_expression : conditional_or_expression OP_OR conditional_and_expression",
    "conditional_expression : conditional_or_expression",
    "conditional_expression : conditional_or_expression INTERR expression COLON expression",
    "conditional_expression : conditional_or_expression OP_COALESCING expression",
    "conditional_expression : conditional_or_expression INTERR CLOSE_PARENS",
    "assignment_expression : prefixed_unary_expression ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_MULT_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_DIV_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_MOD_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_ADD_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_SUB_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_SHIFT_LEFT_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_SHIFT_RIGHT_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_AND_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_OR_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_XOR_ASSIGN expression",
    "lambda_parameter_list : lambda_parameter",
    "lambda_parameter_list : lambda_parameter_list COMMA lambda_parameter",
    "lambda_parameter : parameter_modifier type IDENTIFIER",
    "lambda_parameter : type IDENTIFIER",
    "lambda_parameter : IDENTIFIER",
    "opt_lambda_parameter_list :",
    "opt_lambda_parameter_list : lambda_parameter_list",
    "$$59 :",
    "lambda_expression_body : $$59 expression",
    "lambda_expression_body : block",
    "$$60 :",
    "lambda_expression : IDENTIFIER ARROW $$60 lambda_expression_body",
    "$$61 :",
    "lambda_expression : OPEN_PARENS_LAMBDA opt_lambda_parameter_list CLOSE_PARENS ARROW $$61 lambda_expression_body",
    "expression : assignment_expression",
    "expression : non_assignment_expression",
    "non_assignment_expression : conditional_expression",
    "non_assignment_expression : lambda_expression",
    "non_assignment_expression : query_expression",
    "constant_expression : expression",
    "boolean_expression : expression",
    "$$62 :",
    "$$63 :",
    "$$64 :",
    "$$65 :",
    "class_declaration : opt_attributes opt_modifiers opt_partial CLASS $$62 type_name $$63 opt_class_base opt_type_parameter_constraints_clauses $$64 class_body $$65 opt_semicolon",
    "opt_partial :",
    "opt_partial : PARTIAL",
    "opt_modifiers :",
    "opt_modifiers : modifiers",
    "modifiers : modifier",
    "modifiers : modifiers modifier",
    "modifier : NEW",
    "modifier : PUBLIC",
    "modifier : PROTECTED",
    "modifier : INTERNAL",
    "modifier : PRIVATE",
    "modifier : ABSTRACT",
    "modifier : SEALED",
    "modifier : STATIC",
    "modifier : READONLY",
    "modifier : VIRTUAL",
    "modifier : OVERRIDE",
    "modifier : EXTERN",
    "modifier : VOLATILE",
    "modifier : UNSAFE",
    "opt_class_base :",
    "opt_class_base : class_base",
    "class_base : COLON type_list",
    "opt_type_parameter_constraints_clauses :",
    "opt_type_parameter_constraints_clauses : type_parameter_constraints_clauses",
    "type_parameter_constraints_clauses : type_parameter_constraints_clause",
    "type_parameter_constraints_clauses : type_parameter_constraints_clauses type_parameter_constraints_clause",
    "type_parameter_constraints_clause : WHERE IDENTIFIER COLON type_parameter_constraints",
    "type_parameter_constraints : type_parameter_constraint",
    "type_parameter_constraints : type_parameter_constraints COMMA type_parameter_constraint",
    "type_parameter_constraint : type",
    "type_parameter_constraint : NEW OPEN_PARENS CLOSE_PARENS",
    "type_parameter_constraint : CLASS",
    "type_parameter_constraint : STRUCT",
    "$$66 :",
    "block : OPEN_BRACE $$66 opt_statement_list CLOSE_BRACE",
    "$$67 :",
    "block_prepared : OPEN_BRACE $$67 opt_statement_list CLOSE_BRACE",
    "opt_statement_list :",
    "opt_statement_list : statement_list",
    "statement_list : statement",
    "statement_list : statement_list statement",
    "statement : declaration_statement",
    "statement : valid_declaration_statement",
    "statement : labeled_statement",
    "valid_declaration_statement : block",
    "valid_declaration_statement : empty_statement",
    "valid_declaration_statement : expression_statement",
    "valid_declaration_statement : selection_statement",
    "valid_declaration_statement : iteration_statement",
    "valid_declaration_statement : jump_statement",
    "valid_declaration_statement : try_statement",
    "valid_declaration_statement : checked_statement",
    "valid_declaration_statement : unchecked_statement",
    "valid_declaration_statement : lock_statement",
    "valid_declaration_statement : using_statement",
    "valid_declaration_statement : unsafe_statement",
    "valid_declaration_statement : fixed_statement",
    "embedded_statement : valid_declaration_statement",
    "embedded_statement : declaration_statement",
    "embedded_statement : labeled_statement",
    "empty_statement : SEMICOLON",
    "$$68 :",
    "labeled_statement : IDENTIFIER COLON $$68 statement",
    "declaration_statement : local_variable_declaration SEMICOLON",
    "declaration_statement : local_constant_declaration SEMICOLON",
    "local_variable_type : primary_expression opt_rank_specifier_or_nullable",
    "local_variable_type : builtin_types opt_rank_specifier_or_nullable",
    "local_variable_pointer_type : primary_expression STAR",
    "local_variable_pointer_type : builtin_types STAR",
    "local_variable_pointer_type : VOID STAR",
    "local_variable_pointer_type : local_variable_pointer_type STAR",
    "local_variable_declaration : local_variable_type variable_declarators",
    "local_variable_declaration : local_variable_pointer_type opt_rank_specifier_or_nullable variable_declarators",
    "local_constant_declaration : CONST local_variable_type constant_declarators",
    "expression_statement : statement_expression SEMICOLON",
    "statement_expression : expression",
    "statement_expression : error",
    "selection_statement : if_statement",
    "selection_statement : switch_statement",
    "if_statement : IF OPEN_PARENS boolean_expression CLOSE_PARENS embedded_statement",
    "if_statement : IF OPEN_PARENS boolean_expression CLOSE_PARENS embedded_statement ELSE embedded_statement",
    "$$69 :",
    "switch_statement : SWITCH OPEN_PARENS $$69 expression CLOSE_PARENS switch_block",
    "switch_block : OPEN_BRACE opt_switch_sections CLOSE_BRACE",
    "opt_switch_sections :",
    "opt_switch_sections : switch_sections",
    "switch_sections : switch_section",
    "switch_sections : switch_sections switch_section",
    "$$70 :",
    "switch_section : switch_labels $$70 statement_list",
    "switch_labels : switch_label",
    "switch_labels : switch_labels switch_label",
    "switch_label : CASE constant_expression COLON",
    "switch_label : DEFAULT_COLON",
    "switch_label : error",
    "iteration_statement : while_statement",
    "iteration_statement : do_statement",
    "iteration_statement : for_statement",
    "iteration_statement : foreach_statement",
    "while_statement : WHILE OPEN_PARENS boolean_expression CLOSE_PARENS embedded_statement",
    "do_statement : DO embedded_statement WHILE OPEN_PARENS boolean_expression CLOSE_PARENS SEMICOLON",
    "$$71 :",
    "for_statement : FOR open_parens opt_for_initializer SEMICOLON $$71 opt_for_condition SEMICOLON opt_for_iterator CLOSE_PARENS embedded_statement",
    "opt_for_initializer :",
    "opt_for_initializer : for_initializer",
    "for_initializer : local_variable_declaration",
    "for_initializer : statement_expression_list",
    "opt_for_condition :",
    "opt_for_condition : boolean_expression",
    "opt_for_iterator :",
    "opt_for_iterator : for_iterator",
    "for_iterator : statement_expression_list",
    "statement_expression_list : statement_expression",
    "statement_expression_list : statement_expression_list COMMA statement_expression",
    "foreach_statement : FOREACH open_parens type IN expression CLOSE_PARENS",
    "$$72 :",
    "foreach_statement : FOREACH open_parens type IDENTIFIER IN expression CLOSE_PARENS $$72 embedded_statement",
    "jump_statement : break_statement",
    "jump_statement : continue_statement",
    "jump_statement : goto_statement",
    "jump_statement : return_statement",
    "jump_statement : throw_statement",
    "jump_statement : yield_statement",
    "break_statement : BREAK SEMICOLON",
    "continue_statement : CONTINUE SEMICOLON",
    "goto_statement : GOTO IDENTIFIER SEMICOLON",
    "goto_statement : GOTO CASE constant_expression SEMICOLON",
    "goto_statement : GOTO DEFAULT SEMICOLON",
    "return_statement : RETURN opt_expression SEMICOLON",
    "throw_statement : THROW opt_expression SEMICOLON",
    "yield_statement : IDENTIFIER RETURN expression SEMICOLON",
    "yield_statement : IDENTIFIER RETURN SEMICOLON",
    "yield_statement : IDENTIFIER BREAK SEMICOLON",
    "opt_expression :",
    "opt_expression : expression",
    "try_statement : TRY block catch_clauses",
    "try_statement : TRY block FINALLY block",
    "try_statement : TRY block catch_clauses FINALLY block",
    "try_statement : TRY block error",
    "catch_clauses : catch_clause",
    "catch_clauses : catch_clauses catch_clause",
    "opt_identifier :",
    "opt_identifier : IDENTIFIER",
    "$$73 :",
    "catch_clause : CATCH opt_catch_args $$73 block",
    "opt_catch_args :",
    "opt_catch_args : catch_args",
    "catch_args : open_parens type opt_identifier CLOSE_PARENS",
    "checked_statement : CHECKED block",
    "unchecked_statement : UNCHECKED block",
    "$$74 :",
    "unsafe_statement : UNSAFE $$74 block",
    "$$75 :",
    "fixed_statement : FIXED open_parens type fixed_pointer_declarators CLOSE_PARENS $$75 embedded_statement",
    "fixed_pointer_declarators : fixed_pointer_declarator",
    "fixed_pointer_declarators : fixed_pointer_declarators COMMA fixed_pointer_declarator",
    "fixed_pointer_declarator : IDENTIFIER ASSIGN expression",
    "fixed_pointer_declarator : IDENTIFIER",
    "$$76 :",
    "lock_statement : LOCK OPEN_PARENS expression CLOSE_PARENS $$76 embedded_statement",
    "$$77 :",
    "using_statement : USING open_parens local_variable_declaration CLOSE_PARENS $$77 embedded_statement",
    "$$78 :",
    "using_statement : USING open_parens expression CLOSE_PARENS $$78 embedded_statement",
    "$$79 :",
    "query_expression : first_from_clause $$79 query_body",
    "first_from_clause : FROM IDENTIFIER IN expression",
    "first_from_clause : FROM type IDENTIFIER IN expression",
    "from_clause : FROM IDENTIFIER IN expression",
    "from_clause : FROM type IDENTIFIER IN expression",
    "query_body : opt_query_body_clauses select_or_group_clause opt_query_continuation",
    "select_or_group_clause : SELECT expression",
    "select_or_group_clause : GROUP expression BY expression",
    "opt_query_body_clauses :",
    "opt_query_body_clauses : query_body_clauses",
    "query_body_clauses : query_body_clause",
    "query_body_clauses : query_body_clauses query_body_clause",
    "query_body_clause : from_clause",
    "query_body_clause : let_clause",
    "query_body_clause : where_clause",
    "query_body_clause : join_clause",
    "query_body_clause : orderby_clause",
    "let_clause : LET IDENTIFIER ASSIGN expression",
    "where_clause : WHERE boolean_expression",
    "join_clause : JOIN IDENTIFIER IN expression ON expression EQUALS expression opt_join_into",
    "join_clause : JOIN type IDENTIFIER IN expression ON expression EQUALS expression opt_join_into",
    "opt_join_into :",
    "opt_join_into : INTO IDENTIFIER",
    "orderby_clause : ORDERBY orderings",
    "orderings : order_by",
    "orderings : order_by COMMA orderings_then_by",
    "orderings_then_by : then_by",
    "orderings_then_by : orderings_then_by COMMA then_by",
    "order_by : expression",
    "order_by : expression ASCENDING",
    "order_by : expression DESCENDING",
    "then_by : expression",
    "then_by : expression ASCENDING",
    "then_by : expression DESCENDING",
    "opt_query_continuation :",
    "$$80 :",
    "opt_query_continuation : INTO IDENTIFIER $$80 query_body",
  };
 public static string getRule (int index) {
    return yyRule [index];
 }
}
  protected static  string [] yyNames = {    
    "end-of-file",null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,"'!'",null,null,null,"'%'","'&'",
    null,"'('","')'","'*'","'+'","','","'-'","'.'","'/'",null,null,null,
    null,null,null,null,null,null,null,"':'","';'","'<'","'='","'>'",
    "'?'",null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,"'['",null,"']'","'^'",null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,"'{'","'|'","'}'","'~'",null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,"EOF","NONE","ERROR","FIRST_KEYWORD","ABSTRACT","AS","ADD",
    "ASSEMBLY","BASE","BOOL","BREAK","BYTE","CASE","CATCH","CHAR",
    "CHECKED","CLASS","CONST","CONTINUE","DECIMAL","DEFAULT","DELEGATE",
    "DO","DOUBLE","ELSE","ENUM","EVENT","EXPLICIT","EXTERN","FALSE",
    "FINALLY","FIXED","FLOAT","FOR","FOREACH","GOTO","IF","IMPLICIT","IN",
    "INT","INTERFACE","INTERNAL","IS","LOCK","LONG","NAMESPACE","NEW",
    "NULL","OBJECT","OPERATOR","OUT","OVERRIDE","PARAMS","PRIVATE",
    "PROTECTED","PUBLIC","READONLY","REF","RETURN","REMOVE","SBYTE",
    "SEALED","SHORT","SIZEOF","STACKALLOC","STATIC","STRING","STRUCT",
    "SWITCH","THIS","THROW","TRUE","TRY","TYPEOF","UINT","ULONG",
    "UNCHECKED","UNSAFE","USHORT","USING","VIRTUAL","VOID","VOLATILE",
    "WHERE","WHILE","ARGLIST","PARTIAL","ARROW","QUERY_FIRST_TOKEN",
    "FROM","JOIN","ON","EQUALS","SELECT","GROUP","BY","LET","ORDERBY",
    "ASCENDING","DESCENDING","INTO","QUERY_LAST_TOKEN","GET","\"get\"",
    "SET","\"set\"","LAST_KEYWORD","OPEN_BRACE","CLOSE_BRACE",
    "OPEN_BRACKET","CLOSE_BRACKET","OPEN_PARENS","CLOSE_PARENS","DOT",
    "COMMA","COLON","SEMICOLON","TILDE","PLUS","MINUS","BANG","ASSIGN",
    "OP_LT","OP_GENERICS_LT","OP_GT","OP_GENERICS_GT","BITWISE_AND",
    "BITWISE_OR","STAR","PERCENT","DIV","CARRET","INTERR","DOUBLE_COLON",
    "\"::\"","OP_INC","\"++\"","OP_DEC","\"--\"","OP_SHIFT_LEFT","\"<<\"",
    "OP_SHIFT_RIGHT","\">>\"","OP_LE","\"<=\"","OP_GE","\">=\"","OP_EQ",
    "\"==\"","OP_NE","\"!=\"","OP_AND","\"&&\"","OP_OR","\"||\"",
    "OP_MULT_ASSIGN","\"*=\"","OP_DIV_ASSIGN","\"/=\"","OP_MOD_ASSIGN",
    "\"%=\"","OP_ADD_ASSIGN","\"+=\"","OP_SUB_ASSIGN","\"-=\"",
    "OP_SHIFT_LEFT_ASSIGN","\"<<=\"","OP_SHIFT_RIGHT_ASSIGN","\">>=\"",
    "OP_AND_ASSIGN","\"&=\"","OP_XOR_ASSIGN","\"^=\"","OP_OR_ASSIGN",
    "\"|=\"","OP_PTR","\"->\"","OP_COALESCING","\"??\"","LITERAL_INTEGER",
    "\"int literal\"","LITERAL_FLOAT","\"float literal\"",
    "LITERAL_DOUBLE","\"double literal\"","LITERAL_DECIMAL",
    "\"decimal literal\"","LITERAL_CHARACTER","\"character literal\"",
    "LITERAL_STRING","\"string literal\"","IDENTIFIER",
    "OPEN_PARENS_LAMBDA","CLOSE_PARENS_CAST","CLOSE_PARENS_NO_CAST",
    "CLOSE_PARENS_OPEN_PARENS","CLOSE_PARENS_MINUS","DEFAULT_OPEN_PARENS",
    "GENERIC_DIMENSION","DEFAULT_COLON","LOWPREC","UMINUS","HIGHPREC",
  };

  /** index-checked interface to yyNames[].
      @param token single character or %token value.
      @return token name or [illegal] or [unknown].
    */
  public static string yyname (int token) {
    if ((token < 0) || (token > yyNames.Length)) return "[illegal]";
    string name;
    if ((name = yyNames[token]) != null) return name;
    return "[unknown]";
  }

  /** computes list of expected tokens on error by tracing the tables.
      @param state for which to compute the list.
      @return list of token names.
    */
  protected string[] yyExpecting (int state) {
    int token, n, len = 0;
    bool[] ok = new bool[yyNames.Length];

    if ((n = yySindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }
    if ((n = yyRindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }

    string [] result = new string[len];
    for (n = token = 0; n < len;  ++ token)
      if (ok[token]) result[n++] = yyNames[token];
    return result;
  }

  /** the generated parser, with debugging messages.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @param yydebug debug message writer implementing yyDebug, or null.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex, Object yyd)
				 {
    this.debug = (yydebug.yyDebug)yyd;
    return yyparse(yyLex);
  }

  /** initial size and increment of the state/value stack [default 256].
      This is not final so that it can be overwritten outside of invocations
      of yyparse().
    */
  protected int yyMax;

  /** executed at the beginning of a reduce action.
      Used as $$ = yyDefault($1), prior to the user-specified action, if any.
      Can be overwritten to provide deep copy, etc.
      @param first value for $1, or null.
      @return first.
    */
  protected Object yyDefault (Object first) {
    return first;
  }

  /** the generated parser.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex)
  {
    if (yyMax <= 0) yyMax = 256;			// initial size
    int yyState = 0;                                   // state stack ptr
    int [] yyStates = new int[yyMax];	                // state stack 
    Object yyVal = null;                               // value stack ptr
    Object [] yyVals = new Object[yyMax];	        // value stack
    int yyToken = -1;					// current input
    int yyErrorFlag = 0;				// #tks to shift

    /*yyLoop:*/ for (int yyTop = 0;; ++ yyTop) {
      if (yyTop >= yyStates.Length) {			// dynamically increase
        int[] i = new int[yyStates.Length+yyMax];
        yyStates.CopyTo (i, 0);
        yyStates = i;
        Object[] o = new Object[yyVals.Length+yyMax];
        yyVals.CopyTo (o, 0);
        yyVals = o;
      }
      yyStates[yyTop] = yyState;
      yyVals[yyTop] = yyVal;
      if (debug != null) debug.push(yyState, yyVal);

      /*yyDiscarded:*/ for (;;) {	// discarding a token does not change stack
        int yyN;
        if ((yyN = yyDefRed[yyState]) == 0) {	// else [default] reduce (yyN)
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
            if (debug != null)
              debug.lex(yyState, yyToken, yyname(yyToken), yyLex.value());
          }
          if ((yyN = yySindex[yyState]) != 0 && ((yyN += yyToken) >= 0)
              && (yyN < yyTable.Length) && (yyCheck[yyN] == yyToken)) {
            if (debug != null)
              debug.shift(yyState, yyTable[yyN], yyErrorFlag-1);
            yyState = yyTable[yyN];		// shift to yyN
            yyVal = yyLex.value();
            yyToken = -1;
            if (yyErrorFlag > 0) -- yyErrorFlag;
            goto continue_yyLoop;
          }
          if ((yyN = yyRindex[yyState]) != 0 && (yyN += yyToken) >= 0
              && yyN < yyTable.Length && yyCheck[yyN] == yyToken)
            yyN = yyTable[yyN];			// reduce (yyN)
          else
            switch (yyErrorFlag) {
  
            case 0:
              // yyerror(String.Format ("syntax error, got token `{0}'", yyname (yyToken)), yyExpecting(yyState));
              if (debug != null) debug.error("syntax error");
              goto case 1;
            case 1: case 2:
              yyErrorFlag = 3;
              do {
                if ((yyN = yySindex[yyStates[yyTop]]) != 0
                    && (yyN += Token.yyErrorCode) >= 0 && yyN < yyTable.Length
                    && yyCheck[yyN] == Token.yyErrorCode) {
                  if (debug != null)
                    debug.shift(yyStates[yyTop], yyTable[yyN], 3);
                  yyState = yyTable[yyN];
                  yyVal = yyLex.value();
                  goto continue_yyLoop;
                }
                if (debug != null) debug.pop(yyStates[yyTop]);
              } while (-- yyTop >= 0);
              if (debug != null) debug.reject();
              throw new yyParser.yyException("irrecoverable syntax error");
  
            case 3:
              if (yyToken == 0) {
                if (debug != null) debug.reject();
                throw new yyParser.yyException("irrecoverable syntax error at end-of-file");
              }
              if (debug != null)
                debug.discard(yyState, yyToken, yyname(yyToken),
  							yyLex.value());
              yyToken = -1;
              goto continue_yyDiscarded;		// leave stack alone
            }
        }
        int yyV = yyTop + 1-yyLen[yyN];
        if (debug != null)
          debug.reduce(yyState, yyStates[yyV-1], yyN, YYRules.getRule (yyN), yyLen[yyN]);
        yyVal = yyDefault(yyV > yyTop ? null : yyVals[yyV]);
        switch (yyN) {
case 5:
#line 334 "cs-parser.jay"
  {
		Lexer.check_incorrect_doc_comment ();
	  }
  break;
case 6:
#line 338 "cs-parser.jay"
  {
		Lexer.check_incorrect_doc_comment ();
	  }
  break;
case 14:
#line 360 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		string s = lt.Value;
		if (s != "alias"){
			Report.Error (1003, lt.Location, "'alias' expected");
		} else if (RootContext.Version == LanguageVersion.ISO_1) {
			Report.FeatureIsNotAvailable (lt.Location, "external alias");
		} else {
			lt = (LocatedToken) yyVals[-1+yyTop]; 
			current_namespace.AddUsingExternalAlias (lt.Value, lt.Location);
		}
	  }
  break;
case 17:
#line 381 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 18:
#line 386 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 19:
#line 395 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-3+yyTop];
		current_namespace.AddUsingAlias (lt.Value, (MemberName) yyVals[-1+yyTop], (Location) yyVals[-4+yyTop]);
	  }
  break;
case 20:
#line 399 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
	  }
  break;
case 21:
#line 406 "cs-parser.jay"
  {
		current_namespace.AddUsing ((MemberName) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 22:
#line 418 "cs-parser.jay"
  {
		MemberName name = (MemberName) yyVals[0+yyTop];

		if (yyVals[-2+yyTop] != null) {
			Report.Error(1671, name.Location, "A namespace declaration cannot have modifiers or attributes");
		}

		if (name.TypeArguments != null)
			syntax_error (lexer.Location, "namespace name expected");

		current_namespace = new NamespaceEntry (current_namespace, file, name.GetName ());
		cu.AddNamespace (current_namespace);		
		
		current_class = current_namespace.SlaveDeclSpace;
		current_container = current_class.PartialContainer;
	  }
  break;
case 23:
#line 435 "cs-parser.jay"
  { 
		current_namespace = current_namespace.Parent;
		current_class = current_namespace.SlaveDeclSpace;
		current_container = current_class.PartialContainer;
	  }
  break;
case 28:
#line 453 "cs-parser.jay"
  {
		MemberName name = (MemberName) yyVals[0+yyTop];

		if (name.TypeArguments != null)
			syntax_error (lexer.Location, "namespace name expected");

		yyVal = name;
	  }
  break;
case 29:
#line 465 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 32:
#line 478 "cs-parser.jay"
  {
		Report.Error (1518, lexer.Location, "Expected `class', `delegate', `enum', `interface', or `struct'");
	  }
  break;
case 34:
#line 486 "cs-parser.jay"
  {
		Report.Error (1513, lexer.Location, "} expected");
	  }
  break;
case 43:
#line 513 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			DeclSpace ds = (DeclSpace)yyVals[0+yyTop];

			if ((ds.ModFlags & (Modifiers.PRIVATE|Modifiers.PROTECTED)) != 0){
				Report.Error (1527, ds.Location, 
				"Namespace elements cannot be explicitly declared as private, protected or protected internal");
			}
		}
		current_namespace.DeclarationFound = true;
	  }
  break;
case 44:
#line 524 "cs-parser.jay"
  {
		current_namespace.DeclarationFound = true;
	  }
  break;
case 45:
#line 528 "cs-parser.jay"
  {
		Report.Error (116, ((MemberCore) yyVals[0+yyTop]).Location, "A namespace can only contain types and namespace declarations");
	  }
  break;
case 46:
#line 531 "cs-parser.jay"
  {
		Report.Error (116, ((MemberCore) yyVals[0+yyTop]).Location, "A namespace can only contain types and namespace declarations");
	  }
  break;
case 52:
#line 557 "cs-parser.jay"
  {
	if (yyVals[0+yyTop] != null) {
		Attributes attrs = (Attributes)yyVals[0+yyTop];
		if (global_attrs_enabled) {
			CodeGen.Assembly.AddAttributes (attrs.Attrs);
		} else {
			foreach (Attribute a in attrs.Attrs) {
				Report.Error (1730, a.Location, "Assembly and module attributes must precede all other elements except using clauses and extern alias declarations");
			}
		}
	}

	yyVal = yyVals[0+yyTop];
}
  break;
case 53:
#line 574 "cs-parser.jay"
  {
		global_attrs_enabled = false;
		yyVal = null;
      }
  break;
case 54:
#line 579 "cs-parser.jay"
  { 
		global_attrs_enabled = false;
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 55:
#line 588 "cs-parser.jay"
  {
		if (current_attr_target != String.Empty) {
			ArrayList sect = (ArrayList) yyVals[0+yyTop];

			if (global_attrs_enabled) {
				if (current_attr_target == "module") {
					CodeGen.Module.AddAttributes (sect);
					yyVal = null;
				} else if (current_attr_target != null && current_attr_target.Length > 0) {
					CodeGen.Assembly.AddAttributes (sect);
					yyVal = null;
				} else {
					yyVal = new Attributes (sect);
				}
				if (yyVal == null) {
					if (RootContext.Documentation != null) {
						Lexer.check_incorrect_doc_comment ();
						Lexer.doc_state =
							XmlCommentState.Allowed;
					}
				}
			} else {
				yyVal = new Attributes (sect);
			}		
		}
		else
			yyVal = null;
		current_attr_target = null;
	  }
  break;
case 56:
#line 618 "cs-parser.jay"
  {
		if (current_attr_target != String.Empty) {
			Attributes attrs = yyVals[-1+yyTop] as Attributes;
			ArrayList sect = (ArrayList) yyVals[0+yyTop];

			if (global_attrs_enabled) {
				if (current_attr_target == "module") {
					CodeGen.Module.AddAttributes (sect);
					yyVal = null;
				} else if (current_attr_target == "assembly") {
					CodeGen.Assembly.AddAttributes (sect);
					yyVal = null;
				} else {
					if (attrs == null)
						attrs = new Attributes (sect);
					else
						attrs.AddAttributes (sect);			
				}
			} else {
				if (attrs == null)
					attrs = new Attributes (sect);
				else
					attrs.AddAttributes (sect);
			}		
			yyVal = attrs;
		}
		else
			yyVal = null;
		current_attr_target = null;
	  }
  break;
case 57:
#line 652 "cs-parser.jay"
  {
		yyVal = yyVals[-2+yyTop];
 	  }
  break;
case 58:
#line 656 "cs-parser.jay"
  {
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 59:
#line 663 "cs-parser.jay"
  {
		current_attr_target = (string)yyVals[-1+yyTop];
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 60:
#line 671 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		yyVal = CheckAttributeTarget (lt.Value, lt.Location);
	  }
  break;
case 61:
#line 675 "cs-parser.jay"
  { yyVal = "event"; }
  break;
case 62:
#line 676 "cs-parser.jay"
  { yyVal = "return"; }
  break;
case 63:
#line 678 "cs-parser.jay"
  {
  		string name = yyNames [yyToken].ToLower ();
		yyVal = CheckAttributeTarget (name, GetLocation (yyVals[0+yyTop]));
	  }
  break;
case 64:
#line 686 "cs-parser.jay"
  {
		ArrayList attrs = new ArrayList (4);
		attrs.Add (yyVals[0+yyTop]);

		yyVal = attrs;
	       
	  }
  break;
case 65:
#line 694 "cs-parser.jay"
  {
		ArrayList attrs = (ArrayList) yyVals[-2+yyTop];
		attrs.Add (yyVals[0+yyTop]);

		yyVal = attrs;
	  }
  break;
case 66:
#line 704 "cs-parser.jay"
  {
		MemberName mname = (MemberName) yyVals[-1+yyTop];
		if (mname.IsGeneric) {
			Report.Error (404, lexer.Location,
				      "'<' unexpected: attributes cannot be generic");
		}

		object [] arguments = (object []) yyVals[0+yyTop];
		MemberName left = mname.Left;
		string identifier = mname.Name;

		Expression left_expr = left == null ? null : left.GetTypeExpression ();

		if (current_attr_target == String.Empty)
			yyVal = null;
		else if (global_attrs_enabled && (current_attr_target == "assembly" || current_attr_target == "module"))
			/* FIXME: supply "nameEscaped" parameter here.*/
			yyVal = new GlobalAttribute (current_namespace, current_attr_target,
						  left_expr, identifier, arguments, mname.Location, lexer.IsEscapedIdentifier (mname.Location));
		else
			yyVal = new Attribute (current_attr_target, left_expr, identifier, arguments, mname.Location, lexer.IsEscapedIdentifier (mname.Location));
	  }
  break;
case 67:
#line 729 "cs-parser.jay"
  { /* reserved attribute name or identifier: 17.4 */ }
  break;
case 68:
#line 733 "cs-parser.jay"
  { yyVal = null; }
  break;
case 69:
#line 735 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 70:
#line 743 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] == null)
			yyVal = null;
		else {
			yyVal = new object [] { yyVals[0+yyTop], null };
		}
	  }
  break;
case 71:
#line 751 "cs-parser.jay"
  {
		yyVal = new object[] { yyVals[-2+yyTop], yyVals[0+yyTop] };
	  }
  break;
case 72:
#line 755 "cs-parser.jay"
  {
		yyVal = new object [] { null, yyVals[0+yyTop] };
	  }
  break;
case 73:
#line 762 "cs-parser.jay"
  { yyVal = null; }
  break;
case 75:
#line 768 "cs-parser.jay"
  {
		ArrayList args = new ArrayList (4);
		args.Add (new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Expression));

		yyVal = args;
	  }
  break;
case 76:
#line 775 "cs-parser.jay"
  {
		ArrayList args = (ArrayList) yyVals[-2+yyTop];
		args.Add (new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Expression));

		yyVal = args;
	 }
  break;
case 77:
#line 785 "cs-parser.jay"
  {
		ArrayList args = new ArrayList (4);
		args.Add (yyVals[0+yyTop]);

		yyVal = args;
	  }
  break;
case 78:
#line 792 "cs-parser.jay"
  {	  
		ArrayList args = (ArrayList) yyVals[-2+yyTop];
		args.Add (yyVals[0+yyTop]);

		yyVal = args;
	  }
  break;
case 79:
#line 799 "cs-parser.jay"
  {
		  Report.Error (1016, ((Expression) yyVals[0+yyTop]).Location, "Named attribute argument expected");
		  yyVal = null;
		}
  break;
case 80:
#line 807 "cs-parser.jay"
  {
		/* FIXME: keep location*/
		yyVal = new DictionaryEntry (
			((LocatedToken) yyVals[-2+yyTop]).Value, 
			new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Expression));
	  }
  break;
case 96:
#line 849 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 97:
#line 853 "cs-parser.jay"
  { 
		MemberName name = MakeName ((MemberName) yyVals[0+yyTop]);
		push_current_class (new Struct (current_namespace, current_class, name, (int) yyVals[-4+yyTop], (Attributes) yyVals[-5+yyTop]), yyVals[-3+yyTop]);
	  }
  break;
case 98:
#line 859 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		current_class.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);

		if (RootContext.Documentation != null)
			current_container.DocComment = Lexer.consume_doc_comment ();
	  }
  break;
case 99:
#line 868 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 100:
#line 873 "cs-parser.jay"
  {
		yyVal = pop_current_class ();
	  }
  break;
case 101:
#line 876 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
	  }
  break;
case 102:
#line 883 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 118:
#line 925 "cs-parser.jay"
  {
		int modflags = (int) yyVals[-4+yyTop];
		foreach (VariableDeclaration constant in (ArrayList) yyVals[-1+yyTop]){
			Location l = constant.Location;
			if ((modflags & Modifiers.STATIC) != 0) {
				Report.Error (504, l, "The constant `{0}' cannot be marked static", current_container.GetSignatureForError () + '.' + (string) constant.identifier);
				continue;
			}

			Const c = new Const (
				current_class, (Expression) yyVals[-2+yyTop], (string) constant.identifier, 
				(Expression) constant.expression_or_array_initializer, modflags, 
				(Attributes) yyVals[-5+yyTop], l);

			if (RootContext.Documentation != null) {
				c.DocComment = Lexer.consume_doc_comment ();
				Lexer.doc_state = XmlCommentState.Allowed;
			}
			current_container.AddConstant (c);
		}
	  }
  break;
case 119:
#line 950 "cs-parser.jay"
  {
		ArrayList constants = new ArrayList (4);
		if (yyVals[0+yyTop] != null)
			constants.Add (yyVals[0+yyTop]);
		yyVal = constants;
	  }
  break;
case 120:
#line 957 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			ArrayList constants = (ArrayList) yyVals[-2+yyTop];
			constants.Add (yyVals[0+yyTop]);
		}
	  }
  break;
case 121:
#line 967 "cs-parser.jay"
  {
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[-2+yyTop], yyVals[0+yyTop]);
	  }
  break;
case 122:
#line 971 "cs-parser.jay"
  {
		/* A const field requires a value to be provided*/
		Report.Error (145, ((LocatedToken) yyVals[0+yyTop]).Location, "A const field requires a value to be provided");
		yyVal = null;
	  }
  break;
case 123:
#line 984 "cs-parser.jay"
  { 
		Expression type = (Expression) yyVals[-2+yyTop];
		int mod = (int) yyVals[-3+yyTop];

		current_array_type = null;

		foreach (VariableDeclaration var in (ArrayList) yyVals[-1+yyTop]){
			Field field = new Field (current_class, type, mod, var.identifier, 
						 (Attributes) yyVals[-4+yyTop], var.Location);

			field.Initializer = var.expression_or_array_initializer;

			if (RootContext.Documentation != null) {
				field.DocComment = Lexer.consume_doc_comment ();
				Lexer.doc_state = XmlCommentState.Allowed;
			}
			current_container.AddField (field);
			yyVal = field; /* FIXME: might be better if it points to the top item*/
		}
	  }
  break;
case 124:
#line 1010 "cs-parser.jay"
  { 
			Expression type = (Expression) yyVals[-2+yyTop];
			int mod = (int) yyVals[-4+yyTop];

			current_array_type = null;

			foreach (VariableDeclaration var in (ArrayList) yyVals[-1+yyTop]) {
				FixedField field = new FixedField (current_class, type, mod, var.identifier,
					(Expression)var.expression_or_array_initializer, (Attributes) yyVals[-5+yyTop], var.Location);

				if (RootContext.Documentation != null) {
					field.DocComment = Lexer.consume_doc_comment ();
					Lexer.doc_state = XmlCommentState.Allowed;
				}
				current_container.AddField (field);
				yyVal = field; /* FIXME: might be better if it points to the top item*/
			}
	  }
  break;
case 125:
#line 1033 "cs-parser.jay"
  {
		Report.Error (1641, GetLocation (yyVals[-1+yyTop]), "A fixed size buffer field must have the array size specifier after the field name");
	  }
  break;
case 126:
#line 1040 "cs-parser.jay"
  {
		current_array_type = null;
		Report.Error (670, (Location) yyVals[-2+yyTop], "Fields cannot have void type");
	  }
  break;
case 127:
#line 1048 "cs-parser.jay"
  {
		ArrayList decl = new ArrayList (2);
		decl.Add (yyVals[0+yyTop]);
		yyVal = decl;
  	  }
  break;
case 128:
#line 1054 "cs-parser.jay"
  {
		ArrayList decls = (ArrayList) yyVals[-2+yyTop];
		decls.Add (yyVals[0+yyTop]);
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 129:
#line 1063 "cs-parser.jay"
  {
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[-3+yyTop], yyVals[-1+yyTop]);
	  }
  break;
case 130:
#line 1067 "cs-parser.jay"
  {
		Report.Error (443, lexer.Location, "Value or constant expected");
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[-2+yyTop], null);
	  }
  break;
case 131:
#line 1075 "cs-parser.jay"
  {
		ArrayList decl = new ArrayList (4);
		if (yyVals[0+yyTop] != null)
			decl.Add (yyVals[0+yyTop]);
		yyVal = decl;
	  }
  break;
case 132:
#line 1082 "cs-parser.jay"
  {
		ArrayList decls = (ArrayList) yyVals[-2+yyTop];
		decls.Add (yyVals[0+yyTop]);
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 133:
#line 1091 "cs-parser.jay"
  {
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[-2+yyTop], yyVals[0+yyTop]);
	  }
  break;
case 134:
#line 1095 "cs-parser.jay"
  {
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[0+yyTop], null);
	  }
  break;
case 135:
#line 1099 "cs-parser.jay"
  {
		Report.Error (650, ((LocatedToken) yyVals[-3+yyTop]).Location, "Syntax error, bad array declarator. To declare a managed array the rank specifier precedes the variable's identifier. " +
			"To declare a fixed size buffer field, use the fixed keyword before the field type");
		yyVal = null;
	  }
  break;
case 136:
#line 1108 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 137:
#line 1112 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 138:
#line 1116 "cs-parser.jay"
  {
		yyVal = new StackAlloc ((Expression) yyVals[-3+yyTop], (Expression) yyVals[-1+yyTop], (Location) yyVals[-4+yyTop]);
	  }
  break;
case 139:
#line 1120 "cs-parser.jay"
  {
		yyVal = new ArglistAccess ((Location) yyVals[0+yyTop]);
	  }
  break;
case 140:
#line 1124 "cs-parser.jay"
  {
		Report.Error (1575, (Location) yyVals[-1+yyTop], "A stackalloc expression requires [] after type");
                yyVal = null;
	  }
  break;
case 141:
#line 1131 "cs-parser.jay"
  {
		anonymous_host = (IAnonymousHost) yyVals[0+yyTop];
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.NotAllowed;
	  }
  break;
case 142:
#line 1137 "cs-parser.jay"
  {
		Method method = (Method) yyVals[-2+yyTop];
		method.Block = (ToplevelBlock) yyVals[0+yyTop];
		current_container.AddMethod (method);

		anonymous_host = null;
		current_generic_method = null;
		current_local_parameters = null;

		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 144:
#line 1154 "cs-parser.jay"
  {
		int m = (int) yyVals[0+yyTop];
		int i = 1;

		while (m != 0){
			if ((i & m) != 0){
				Report.Error (1585, lexer.Location,
					"Member modifier `{0}' must precede the member type and name",
					Modifiers.Name (i));
			}
			m &= ~i;
			i = i << 1;
		}
	  }
  break;
case 147:
#line 1186 "cs-parser.jay"
  {
		arglist_allowed = true;
	  }
  break;
case 148:
#line 1190 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 149:
#line 1194 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;
		arglist_allowed = false;
		MemberName name = (MemberName) yyVals[-6+yyTop];
		current_local_parameters = (Parameters) yyVals[-3+yyTop];

		if (yyVals[0+yyTop] != null && name.TypeArguments == null)
			Report.Error (80, lexer.Location,
				      "Constraints are not allowed on non-generic declarations");

		Method method;

		GenericMethod generic = null;
		if (name.TypeArguments != null) {
			generic = new GenericMethod (current_namespace, current_class, name,
						     (Expression) yyVals[-7+yyTop], current_local_parameters);

			generic.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);
		}

		method = new Method (current_class, generic, (Expression) yyVals[-7+yyTop], (int) yyVals[-8+yyTop], false,
				     name, current_local_parameters, (Attributes) yyVals[-9+yyTop]);

		anonymous_host = method;
		current_generic_method = generic;

		if (RootContext.Documentation != null)
			method.DocComment = Lexer.consume_doc_comment ();

		yyVal = method;
	  }
  break;
case 150:
#line 1229 "cs-parser.jay"
  {
		arglist_allowed = true;
	  }
  break;
case 151:
#line 1233 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 152:
#line 1237 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;
		arglist_allowed = false;

		MemberName name = (MemberName) yyVals[-6+yyTop];
		current_local_parameters = (Parameters) yyVals[-3+yyTop];

		if (yyVals[0+yyTop] != null && name.TypeArguments == null)
			Report.Error (80, lexer.Location,
				      "Constraints are not allowed on non-generic declarations");

		Method method;
		GenericMethod generic = null;
		if (name.TypeArguments != null) {
			generic = new GenericMethod (current_namespace, current_class, name,
						     TypeManager.system_void_expr, current_local_parameters);

			generic.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);
		}

		method = new Method (current_class, generic, TypeManager.system_void_expr,
				     (int) yyVals[-8+yyTop], false, name, current_local_parameters, (Attributes) yyVals[-9+yyTop]);

		anonymous_host = method;
		current_generic_method = generic;

		if (RootContext.Documentation != null)
			method.DocComment = Lexer.consume_doc_comment ();

		yyVal = method;
	}
  break;
case 153:
#line 1273 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 154:
#line 1277 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		MemberName name = (MemberName) yyVals[-5+yyTop];
		current_local_parameters = (Parameters) yyVals[-3+yyTop];

		if (yyVals[-1+yyTop] != null && name.TypeArguments == null)
			Report.Error (80, lexer.Location,
				      "Constraints are not allowed on non-generic declarations");

		Method method;
		GenericMethod generic = null;
		if (name.TypeArguments != null) {
			generic = new GenericMethod (current_namespace, current_class, name,
						     TypeManager.system_void_expr, current_local_parameters);

			generic.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);
		}

		int modifiers = (int) yyVals[-8+yyTop];


		const int invalid_partial_mod = Modifiers.Accessibility | Modifiers.ABSTRACT | Modifiers.EXTERN |
			Modifiers.NEW | Modifiers.OVERRIDE | Modifiers.SEALED | Modifiers.VIRTUAL;

		if ((modifiers & invalid_partial_mod) != 0) {
			Report.Error (750, name.Location, "A partial method cannot define access modifier or " +
       			"any of abstract, extern, new, override, sealed, or virtual modifiers");
			modifiers &= ~invalid_partial_mod;
		}

		if ((current_class.ModFlags & Modifiers.PARTIAL) == 0) {
			Report.Error (751, name.Location, "A partial method must be declared within a " +
       			"partial class or partial struct");
		}

		modifiers |= Modifiers.PARTIAL | Modifiers.PRIVATE;
		
		method = new Method (current_class, generic, TypeManager.system_void_expr,
				     modifiers, false, name, current_local_parameters, (Attributes) yyVals[-9+yyTop]);

		anonymous_host = method;
		current_generic_method = generic;

		if (RootContext.Documentation != null)
			method.DocComment = Lexer.consume_doc_comment ();

		yyVal = method;
	  }
  break;
case 155:
#line 1330 "cs-parser.jay"
  {
		MemberName name = (MemberName) yyVals[-3+yyTop];
		Report.Error (1585, name.Location, 
			"Member modifier `{0}' must precede the member type and name", Modifiers.Name ((int) yyVals[-4+yyTop]));

		Method method = new Method (current_class, null, TypeManager.system_void_expr,
					    0, false, name, (Parameters) yyVals[-1+yyTop], (Attributes) yyVals[-7+yyTop]);

		current_local_parameters = (Parameters) yyVals[-1+yyTop];

		if (RootContext.Documentation != null)
			method.DocComment = Lexer.consume_doc_comment ();

		yyVal = null;
	  }
  break;
case 157:
#line 1349 "cs-parser.jay"
  { yyVal = null; }
  break;
case 158:
#line 1353 "cs-parser.jay"
  { yyVal = Parameters.EmptyReadOnlyParameters; }
  break;
case 160:
#line 1358 "cs-parser.jay"
  { yyVal = Parameters.EmptyReadOnlyParameters; }
  break;
case 161:
#line 1360 "cs-parser.jay"
  {
		parameter_modifiers_not_allowed = true;
	  }
  break;
case 162:
#line 1364 "cs-parser.jay"
  {
		parameter_modifiers_not_allowed = false;
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 163:
#line 1372 "cs-parser.jay"
  { 
		ArrayList pars_list = (ArrayList) yyVals[0+yyTop];

		Parameter [] pars = new Parameter [pars_list.Count];
		pars_list.CopyTo (pars);

	  	yyVal = new Parameters (pars); 
	  }
  break;
case 164:
#line 1381 "cs-parser.jay"
  {
		ArrayList pars_list = (ArrayList) yyVals[-2+yyTop];
		pars_list.Add (yyVals[0+yyTop]);

		Parameter [] pars = new Parameter [pars_list.Count];
		pars_list.CopyTo (pars);

		yyVal = new Parameters (pars); 
	  }
  break;
case 165:
#line 1391 "cs-parser.jay"
  {
		ArrayList pars_list = (ArrayList) yyVals[-2+yyTop];
		/*pars_list.Add (new ArglistParameter (GetLocation ($3)));*/

		Parameter [] pars = new Parameter [pars_list.Count];
		pars_list.CopyTo (pars);

		yyVal = new Parameters (pars, true);
	  }
  break;
case 166:
#line 1401 "cs-parser.jay"
  {
		if (yyVals[-2+yyTop] != null)
			Report.Error (231, ((Parameter) yyVals[-2+yyTop]).Location, "A params parameter must be the last parameter in a formal parameter list");
		yyVal = null;
	  }
  break;
case 167:
#line 1407 "cs-parser.jay"
  {
		if (yyVals[-2+yyTop] != null)
			Report.Error (231, ((Parameter) yyVals[-2+yyTop]).Location, "A params parameter must be the last parameter in a formal parameter list");
		yyVal = null;
	  }
  break;
case 168:
#line 1413 "cs-parser.jay"
  {
		Report.Error (257, (Location) yyVals[-2+yyTop], "An __arglist parameter must be the last parameter in a formal parameter list");
		yyVal = null;
	  }
  break;
case 169:
#line 1418 "cs-parser.jay"
  {
		Report.Error (257, (Location) yyVals[-2+yyTop], "An __arglist parameter must be the last parameter in a formal parameter list");
		yyVal = null;
	  }
  break;
case 170:
#line 1423 "cs-parser.jay"
  {
		yyVal = new Parameters (new Parameter[] { (Parameter) yyVals[0+yyTop] } );
	  }
  break;
case 171:
#line 1427 "cs-parser.jay"
  {
		yyVal = new Parameters (new Parameter[0], true);
	  }
  break;
case 172:
#line 1434 "cs-parser.jay"
  {
		ArrayList pars = new ArrayList (4);

		pars.Add (yyVals[0+yyTop]);
		yyVal = pars;
	  }
  break;
case 173:
#line 1441 "cs-parser.jay"
  {
		ArrayList pars = (ArrayList) yyVals[-2+yyTop];
		Parameter p = (Parameter)yyVals[0+yyTop];
		if (p != null) {
			if ((p.modFlags & Parameter.Modifier.This) != 0)
				Report.Error (1100, p.Location, "The parameter modifier `this' can only be used on the first parameter");
			pars.Add (p);
		}
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 174:
#line 1458 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		yyVal = new Parameter ((Expression) yyVals[-1+yyTop], lt.Value, (Parameter.Modifier) yyVals[-2+yyTop], (Attributes) yyVals[-3+yyTop], lt.Location);
	  }
  break;
case 175:
#line 1466 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		Report.Error (1552, lt.Location, "Array type specifier, [], must appear before parameter name");
		yyVal = null;
	  }
  break;
case 176:
#line 1474 "cs-parser.jay"
  {
		Report.Error (1001, GetLocation (yyVals[0+yyTop]), "Identifier expected");
		yyVal = null;
	  }
  break;
case 177:
#line 1481 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 178:
#line 1491 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		Report.Error (241, lt.Location, "Default parameter specifiers are not permitted");
		 yyVal = null;
	   }
  break;
case 179:
#line 1499 "cs-parser.jay"
  { yyVal = Parameter.Modifier.NONE; }
  break;
case 181:
#line 1505 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 182:
#line 1509 "cs-parser.jay"
  {
		Parameter.Modifier p2 = (Parameter.Modifier)yyVals[0+yyTop];
  		Parameter.Modifier mod = (Parameter.Modifier)yyVals[-1+yyTop] | p2;
  		if (((Parameter.Modifier)yyVals[-1+yyTop] & p2) == p2) {
  			Error_DuplicateParameterModifier (lexer.Location, p2);
  		} else {
	  		switch (mod & ~Parameter.Modifier.This) {
  				case Parameter.Modifier.REF:
					Report.Error (1101, lexer.Location, "The parameter modifiers `this' and `ref' cannot be used altogether");
  					break;
   				case Parameter.Modifier.OUT:
					Report.Error (1102, lexer.Location, "The parameter modifiers `this' and `out' cannot be used altogether");
  					break;
  				default:
 					Report.Error (1108, lexer.Location, "A parameter cannot have specified more than one modifier");
 					break;
 			}
  		}
  		yyVal = mod;
	  }
  break;
case 183:
#line 1533 "cs-parser.jay"
  {
	  	if (parameter_modifiers_not_allowed)
	  		Error_ParameterModifierNotValid ("ref", (Location)yyVals[0+yyTop]);
	  		
	  	yyVal = Parameter.Modifier.REF;
	  }
  break;
case 184:
#line 1540 "cs-parser.jay"
  {
	  	if (parameter_modifiers_not_allowed)
	  		Error_ParameterModifierNotValid ("out", (Location)yyVals[0+yyTop]);
	  
	  	yyVal = Parameter.Modifier.OUT;
	  }
  break;
case 185:
#line 1547 "cs-parser.jay"
  {
		if (parameter_modifiers_not_allowed)
	  		Error_ParameterModifierNotValid ("this", (Location)yyVals[0+yyTop]);

	  	if (RootContext.Version <= LanguageVersion.ISO_2)
	  		Report.FeatureIsNotAvailable (GetLocation (yyVals[0+yyTop]), "extension methods");
	  			
		yyVal = Parameter.Modifier.This;
	  }
  break;
case 186:
#line 1560 "cs-parser.jay"
  { 
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		yyVal = new ParamsParameter ((Expression) yyVals[-1+yyTop], lt.Value, (Attributes) yyVals[-3+yyTop], lt.Location);
	  }
  break;
case 187:
#line 1564 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 188:
#line 1572 "cs-parser.jay"
  {
		if (params_modifiers_not_allowed)
			Report.Error (1670, ((Location) yyVals[0+yyTop]), "The `params' modifier is not allowed in current context");
	  }
  break;
case 189:
#line 1577 "cs-parser.jay"
  {
		Parameter.Modifier mod = (Parameter.Modifier)yyVals[0+yyTop];
		if ((mod & Parameter.Modifier.This) != 0) {
			Report.Error (1104, (Location)yyVals[-1+yyTop], "The parameter modifiers `this' and `params' cannot be used altogether");
		} else {
			Report.Error (1611, (Location)yyVals[-1+yyTop], "The params parameter cannot be declared as ref or out");
		}	  
	  }
  break;
case 190:
#line 1586 "cs-parser.jay"
  {
		Error_DuplicateParameterModifier ((Location)yyVals[-1+yyTop], Parameter.Modifier.PARAMS);
	  }
  break;
case 191:
#line 1593 "cs-parser.jay"
  {
	  	if (!arglist_allowed)
	  		Report.Error (1669, (Location) yyVals[0+yyTop], "__arglist is not valid in this context");
	  }
  break;
case 192:
#line 1604 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			tmpComment = Lexer.consume_doc_comment ();
	  }
  break;
case 193:
#line 1609 "cs-parser.jay"
  {
		implicit_value_parameter_type = (Expression) yyVals[-3+yyTop];

		lexer.PropertyParsing = true;
	  }
  break;
case 194:
#line 1615 "cs-parser.jay"
  {
		lexer.PropertyParsing = false;
		has_get = has_set = false;
	  }
  break;
case 195:
#line 1620 "cs-parser.jay"
  { 
		if (yyVals[-2+yyTop] == null)
			break;

		Property prop;
		Accessors accessors = (Accessors) yyVals[-2+yyTop];
		Accessor get_block = accessors.get_or_add;
		Accessor set_block = accessors.set_or_remove;

		MemberName name = (MemberName) yyVals[-6+yyTop];

		if (name.TypeArguments != null)
			syntax_error (lexer.Location, "a property can't have type arguments");

		prop = new Property (current_class, (Expression) yyVals[-7+yyTop], (int) yyVals[-8+yyTop], false,
				     name, (Attributes) yyVals[-9+yyTop], get_block, set_block, accessors.declared_in_reverse, current_block);

		current_container.AddProperty (prop);
		implicit_value_parameter_type = null;

		if (RootContext.Documentation != null)
			prop.DocComment = ConsumeStoredComment ();

	  }
  break;
case 196:
#line 1648 "cs-parser.jay"
  {
		yyVal = new Accessors ((Accessor) yyVals[0+yyTop], null);
	 }
  break;
case 197:
#line 1652 "cs-parser.jay"
  { 
		Accessors accessors = (Accessors) yyVals[0+yyTop];
		accessors.get_or_add = (Accessor) yyVals[-1+yyTop];
		yyVal = accessors;
	 }
  break;
case 198:
#line 1658 "cs-parser.jay"
  {
		yyVal = new Accessors (null, (Accessor) yyVals[0+yyTop]);
	 }
  break;
case 199:
#line 1662 "cs-parser.jay"
  { 
		Accessors accessors = (Accessors) yyVals[0+yyTop];
		accessors.set_or_remove = (Accessor) yyVals[-1+yyTop];
		accessors.declared_in_reverse = true;
		yyVal = accessors;
	 }
  break;
case 200:
#line 1669 "cs-parser.jay"
  {
		Report.Error (1014, GetLocation (yyVals[0+yyTop]), "A get or set accessor expected");
		yyVal = null;
	  }
  break;
case 201:
#line 1677 "cs-parser.jay"
  {
		/* If this is not the case, then current_local_parameters has already*/
		/* been set in indexer_declaration*/
		if (parsing_indexer == false)
			current_local_parameters = null;
		else 
			current_local_parameters = indexer_parameters;
		lexer.PropertyParsing = false;

		anonymous_host = SimpleAnonymousHost.GetSimple ();
	  }
  break;
case 202:
#line 1689 "cs-parser.jay"
  {
		if (has_get) {
			Report.Error (1007, (Location) yyVals[-2+yyTop], "Property accessor already defined");
			break;
		}
		Accessor accessor = new Accessor ((ToplevelBlock) yyVals[0+yyTop], (int) yyVals[-3+yyTop], (Attributes) yyVals[-4+yyTop], (Location) yyVals[-2+yyTop]);
		has_get = true;
		current_local_parameters = null;
		lexer.PropertyParsing = true;

		SimpleAnonymousHost.Simple.Propagate (accessor);
		anonymous_host = null;

		if (RootContext.Documentation != null)
			if (Lexer.doc_state == XmlCommentState.Error)
				Lexer.doc_state = XmlCommentState.NotAllowed;

		yyVal = accessor;
	  }
  break;
case 203:
#line 1712 "cs-parser.jay"
  {
		Parameter [] args;
		Parameter implicit_value_parameter = new Parameter (
			implicit_value_parameter_type, "value", 
			Parameter.Modifier.NONE, null, (Location) yyVals[0+yyTop]);

		if (parsing_indexer == false) {
			args  = new Parameter [1];
			args [0] = implicit_value_parameter;
			current_local_parameters = new Parameters (args);
		} else {
			Parameter [] fpars = indexer_parameters.FixedParameters;

			if (fpars != null){
				int count = fpars.Length;

				args = new Parameter [count + 1];
				fpars.CopyTo (args, 0);
				args [count] = implicit_value_parameter;
			} else 
				args = null;
			current_local_parameters = new Parameters (
				args);
		}
		
		lexer.PropertyParsing = false;

		anonymous_host = SimpleAnonymousHost.GetSimple ();
	  }
  break;
case 204:
#line 1742 "cs-parser.jay"
  {
		if (has_set) {
			Report.Error (1007, ((LocatedToken) yyVals[-2+yyTop]).Location, "Property accessor already defined");
			break;
		}
		Accessor accessor = new Accessor ((ToplevelBlock) yyVals[0+yyTop], (int) yyVals[-3+yyTop], (Attributes) yyVals[-4+yyTop], (Location) yyVals[-2+yyTop]);
		has_set = true;
		current_local_parameters = null;
		lexer.PropertyParsing = true;

		SimpleAnonymousHost.Simple.Propagate (accessor);
		anonymous_host = null;

		if (RootContext.Documentation != null
			&& Lexer.doc_state == XmlCommentState.Error)
			Lexer.doc_state = XmlCommentState.NotAllowed;

		yyVal = accessor;
	  }
  break;
case 206:
#line 1765 "cs-parser.jay"
  { yyVal = null; }
  break;
case 207:
#line 1773 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 208:
#line 1777 "cs-parser.jay"
  {
		MemberName name = MakeName ((MemberName) yyVals[0+yyTop]);
		push_current_class (new Interface (current_namespace, current_class, name, (int) yyVals[-4+yyTop], (Attributes) yyVals[-5+yyTop]), yyVals[-3+yyTop]);
	  }
  break;
case 209:
#line 1783 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		current_class.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);

		if (RootContext.Documentation != null) {
			current_container.DocComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 210:
#line 1794 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 211:
#line 1799 "cs-parser.jay"
  {
		yyVal = pop_current_class ();
	  }
  break;
case 212:
#line 1802 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
	  }
  break;
case 218:
#line 1825 "cs-parser.jay"
  { 
		if (yyVals[0+yyTop] == null)
			break;

		Method m = (Method) yyVals[0+yyTop];

		if (m.IsExplicitImpl)
		        Report.Error (541, m.Location, "`{0}': explicit interface declaration can only be declared in a class or struct",
				m.GetSignatureForError ());

		current_container.AddMethod (m);

		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 219:
#line 1841 "cs-parser.jay"
  { 
		if (yyVals[0+yyTop] == null)
			break;

		Property p = (Property) yyVals[0+yyTop];

		if (p.IsExplicitImpl)
		        Report.Error (541, p.Location, "`{0}': explicit interface declaration can only be declared in a class or struct",
				p.GetSignatureForError ());

		current_container.AddProperty (p);

		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 220:
#line 1857 "cs-parser.jay"
  { 
		if (yyVals[0+yyTop] != null){
			Event e = (Event) yyVals[0+yyTop];

			if (e.IsExplicitImpl)
		        Report.Error (541, e.Location, "`{0}': explicit interface declaration can only be declared in a class or struct",
				e.GetSignatureForError ());
			
			current_container.AddEvent (e);
		}

		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 221:
#line 1872 "cs-parser.jay"
  { 
		if (yyVals[0+yyTop] == null)
			break;

		Indexer i = (Indexer) yyVals[0+yyTop];

		if (i.IsExplicitImpl)
		        Report.Error (541, i.Location, "`{0}': explicit interface declaration can only be declared in a class or struct",
				i.GetSignatureForError ());

		current_container.AddIndexer (i);

		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 222:
#line 1888 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 223:
#line 1895 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 224:
#line 1902 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 225:
#line 1909 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 226:
#line 1916 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 227:
#line 1923 "cs-parser.jay"
  {
		Report.Error (525, GetLocation (yyVals[0+yyTop]), "Interfaces cannot contain fields or constants");
	  }
  break;
case 228:
#line 1930 "cs-parser.jay"
  {
		int val = (int) yyVals[0+yyTop];
		val = Modifiers.Check (Modifiers.NEW | Modifiers.UNSAFE, val, 0, GetLocation (yyVals[0+yyTop]));
		yyVal = val;
	  }
  break;
case 229:
#line 1939 "cs-parser.jay"
  {
		Report.Error (531, (Location)yyVals[0+yyTop],
			      "`{0}.{1}{2}': interface members cannot have a definition",
			      current_class.GetSignatureForError (),
			      ((MemberName) yyVals[-1+yyTop]).GetSignatureForError (),
			      ((Parameters)yyVals[-5+yyTop]).GetSignatureForError ());
	  
		lexer.ConstraintsParsing = false;
	  }
  break;
case 230:
#line 1949 "cs-parser.jay"
  {
		yyVal = null;
	  }
  break;
case 232:
#line 1958 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 233:
#line 1962 "cs-parser.jay"
  {
		/* Refer to the name as $-1 in interface_method_declaration_body	  */
		yyVal = yyVals[-5+yyTop];
	  }
  break;
case 234:
#line 1967 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		MemberName name = (MemberName) yyVals[-7+yyTop];

		if (yyVals[-2+yyTop] != null && name.TypeArguments == null)
			Report.Error (80, lexer.Location,
				      "Constraints are not allowed on non-generic declarations");

		GenericMethod generic = null;
		if (name.TypeArguments != null) {
			generic = new GenericMethod (current_namespace, current_class, name,
						     (Expression) yyVals[-8+yyTop], (Parameters) yyVals[-5+yyTop]);

			generic.SetParameterInfo ((ArrayList) yyVals[-2+yyTop]);
		}

		yyVal = new Method (current_class, generic, (Expression) yyVals[-8+yyTop], (int) yyVals[-9+yyTop], true, name,
				 (Parameters) yyVals[-5+yyTop], (Attributes) yyVals[-10+yyTop]);
		if (RootContext.Documentation != null)
			((Method) yyVal).DocComment = Lexer.consume_doc_comment ();
	  }
  break;
case 235:
#line 1991 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 236:
#line 1995 "cs-parser.jay"
  {
		yyVal = yyVals[-5+yyTop];
	  }
  break;
case 237:
#line 1999 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		MemberName name = (MemberName) yyVals[-7+yyTop];

		if (yyVals[-2+yyTop] != null && name.TypeArguments == null)
			Report.Error (80, lexer.Location,
				      "Constraints are not allowed on non-generic declarations");

		GenericMethod generic = null;
		if (name.TypeArguments != null) {
			generic = new GenericMethod (current_namespace, current_class, name,
						     TypeManager.system_void_expr, (Parameters) yyVals[-5+yyTop]);

			generic.SetParameterInfo ((ArrayList) yyVals[-2+yyTop]);
		}

		yyVal = new Method (current_class, generic, TypeManager.system_void_expr, (int) yyVals[-9+yyTop],
				 true, name, (Parameters) yyVals[-5+yyTop], (Attributes) yyVals[-10+yyTop]);
		if (RootContext.Documentation != null)
			((Method) yyVal).DocComment = Lexer.consume_doc_comment ();
	  }
  break;
case 238:
#line 2028 "cs-parser.jay"
  {
		lexer.PropertyParsing = true;
		implicit_value_parameter_type = (Expression)yyVals[-2+yyTop];
	  }
  break;
case 239:
#line 2033 "cs-parser.jay"
  {
		has_get = has_set = false; 
		lexer.PropertyParsing = false;
		implicit_value_parameter_type = null;
	  }
  break;
case 240:
#line 2039 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-5+yyTop];
		MemberName name = new MemberName (lt.Value, lt.Location);

		if (yyVals[-6+yyTop] == TypeManager.system_void_expr) {
			Report.Error (547, lt.Location, "`{0}': property or indexer cannot have void type", lt.Value);
			break;
		}

		Property p = null;
		if (yyVals[-2+yyTop] == null) {
			p = new Property (current_class, (Expression) yyVals[-6+yyTop], (int) yyVals[-7+yyTop], true,
				   name, (Attributes) yyVals[-8+yyTop],
				   null, null, false);

			Report.Error (548, p.Location, "`{0}': property or indexer must have at least one accessor", p.GetSignatureForError ());
			break;
		}

		Accessors accessor = (Accessors) yyVals[-2+yyTop];
		p = new Property (current_class, (Expression) yyVals[-6+yyTop], (int) yyVals[-7+yyTop], true,
				   name, (Attributes) yyVals[-8+yyTop],
				   accessor.get_or_add, accessor.set_or_remove, accessor.declared_in_reverse);

		if (accessor.get_or_add != null && accessor.get_or_add.Block != null) {
			Report.Error (531, p.Location, "`{0}.get': interface members cannot have a definition", p.GetSignatureForError ());
			yyVal = null;
			break;
		}

		if (accessor.set_or_remove != null && accessor.set_or_remove.Block != null) {
			Report.Error (531, p.Location, "`{0}.set': interface members cannot have a definition", p.GetSignatureForError ());
			yyVal = null;
			break;
		}

		if (RootContext.Documentation != null)
			p.DocComment = Lexer.consume_doc_comment ();

		yyVal = p;
	  }
  break;
case 241:
#line 2082 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 242:
#line 2091 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new EventField (current_class, (Expression) yyVals[-2+yyTop], (int) yyVals[-4+yyTop], true,
				     new MemberName (lt.Value, lt.Location),
				     (Attributes) yyVals[-5+yyTop]);
		if (RootContext.Documentation != null)
			((EventField) yyVal).DocComment = Lexer.consume_doc_comment ();
	  }
  break;
case 243:
#line 2099 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 244:
#line 2103 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		Report.Error (68, lt.Location, "`{0}.{1}': event in interface cannot have initializer", current_container.Name, lt.Value);
		yyVal = null;
	  }
  break;
case 245:
#line 2109 "cs-parser.jay"
  {
		implicit_value_parameter_type = (Expression) yyVals[-2+yyTop];
		lexer.EventParsing = true;
	  }
  break;
case 246:
#line 2114 "cs-parser.jay"
  {
		lexer.EventParsing = false;
		implicit_value_parameter_type = null;
	  }
  break;
case 247:
#line 2118 "cs-parser.jay"
  {
		Report.Error (69, (Location) yyVals[-7+yyTop], "Event in interface cannot have add or remove accessors");
 		yyVal = null;
 	  }
  break;
case 248:
#line 2128 "cs-parser.jay"
  {
		lexer.PropertyParsing = true;
		implicit_value_parameter_type = (Expression)yyVals[-5+yyTop];
	  }
  break;
case 249:
#line 2133 "cs-parser.jay"
  { 
		has_get = has_set = false;
 		lexer.PropertyParsing = false;
 		implicit_value_parameter_type = null;
	  }
  break;
case 250:
#line 2139 "cs-parser.jay"
  {
		Indexer i = null;
		if (yyVals[-2+yyTop] == null) {
			i = new Indexer (current_class, (Expression) yyVals[-9+yyTop],
				  new MemberName (TypeContainer.DefaultIndexerName, (Location) yyVals[-8+yyTop]),
				  (int) yyVals[-10+yyTop], true, (Parameters) yyVals[-6+yyTop], (Attributes) yyVals[-11+yyTop],
				  null, null, false);

			Report.Error (548, i.Location, "`{0}': property or indexer must have at least one accessor", i.GetSignatureForError ());
			break;
		}

		Accessors accessors = (Accessors) yyVals[-2+yyTop];
		i = new Indexer (current_class, (Expression) yyVals[-9+yyTop],
				  new MemberName (TypeContainer.DefaultIndexerName, (Location) yyVals[-8+yyTop]),
				  (int) yyVals[-10+yyTop], true, (Parameters) yyVals[-6+yyTop], (Attributes) yyVals[-11+yyTop],
				   accessors.get_or_add, accessors.set_or_remove, accessors.declared_in_reverse);

		if (accessors.get_or_add != null && accessors.get_or_add.Block != null) {
			Report.Error (531, i.Location, "`{0}.get': interface members cannot have a definition", i.GetSignatureForError ());
			yyVal = null;
			break;
		}

		if (accessors.set_or_remove != null && accessors.set_or_remove.Block != null) {
			Report.Error (531, i.Location, "`{0}.set': interface members cannot have a definition", i.GetSignatureForError ());
			yyVal = null;
			break;
		}

		if (RootContext.Documentation != null)
			i.DocComment = ConsumeStoredComment ();

		yyVal = i;
	  }
  break;
case 251:
#line 2178 "cs-parser.jay"
  {
		anonymous_host = SimpleAnonymousHost.GetSimple ();
	  }
  break;
case 252:
#line 2182 "cs-parser.jay"
  {
		if (yyVals[-2+yyTop] == null)
			break;

		OperatorDeclaration decl = (OperatorDeclaration) yyVals[-2+yyTop];
		Operator op = new Operator (
			current_class, decl.optype, decl.ret_type, (int) yyVals[-3+yyTop], 
			current_local_parameters,
			(ToplevelBlock) yyVals[0+yyTop], (Attributes) yyVals[-4+yyTop], decl.location);

		if (RootContext.Documentation != null) {
			op.DocComment = tmpComment;
			Lexer.doc_state = XmlCommentState.Allowed;
		}

		SimpleAnonymousHost.Simple.Propagate (op);
		anonymous_host = null;

		/* Note again, checking is done in semantic analysis*/
		current_container.AddOperator (op);

		current_local_parameters = null;
	  }
  break;
case 254:
#line 2209 "cs-parser.jay"
  { yyVal = null; }
  break;
case 255:
#line 2214 "cs-parser.jay"
  {
		params_modifiers_not_allowed = true;
	  }
  break;
case 256:
#line 2218 "cs-parser.jay"
  {
		params_modifiers_not_allowed = false;

		Location loc = (Location) yyVals[-5+yyTop];
		Operator.OpType op = (Operator.OpType) yyVals[-4+yyTop];
		current_local_parameters = (Parameters)yyVals[-1+yyTop];
		
		int p_count = current_local_parameters.Count;
		if (p_count == 1) {
			if (op == Operator.OpType.Addition)
				op = Operator.OpType.UnaryPlus;
			else if (op == Operator.OpType.Subtraction)
				op = Operator.OpType.UnaryNegation;
		}
		
		if (IsUnaryOperator (op)) {
			if (p_count == 2) {
				Report.Error (1020, loc, "Overloadable binary operator expected");
			} else if (p_count != 1) {
				Report.Error (1535, loc, "Overloaded unary operator `{0}' takes one parameter",
					Operator.GetName (op));
			}
		} else {
			if (p_count > 2) {
				Report.Error (1534, loc, "Overloaded binary operator `{0}' takes two parameters",
					Operator.GetName (op));
			} else if (p_count != 2) {
				Report.Error (1019, loc, "Overloadable unary operator expected");
			}
		}
		
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.NotAllowed;
		}

		yyVal = new OperatorDeclaration (op, (Expression) yyVals[-6+yyTop], loc);
	  }
  break;
case 258:
#line 2261 "cs-parser.jay"
  { yyVal = Operator.OpType.LogicalNot; }
  break;
case 259:
#line 2262 "cs-parser.jay"
  { yyVal = Operator.OpType.OnesComplement; }
  break;
case 260:
#line 2263 "cs-parser.jay"
  { yyVal = Operator.OpType.Increment; }
  break;
case 261:
#line 2264 "cs-parser.jay"
  { yyVal = Operator.OpType.Decrement; }
  break;
case 262:
#line 2265 "cs-parser.jay"
  { yyVal = Operator.OpType.True; }
  break;
case 263:
#line 2266 "cs-parser.jay"
  { yyVal = Operator.OpType.False; }
  break;
case 264:
#line 2268 "cs-parser.jay"
  { yyVal = Operator.OpType.Addition; }
  break;
case 265:
#line 2269 "cs-parser.jay"
  { yyVal = Operator.OpType.Subtraction; }
  break;
case 266:
#line 2271 "cs-parser.jay"
  { yyVal = Operator.OpType.Multiply; }
  break;
case 267:
#line 2272 "cs-parser.jay"
  {  yyVal = Operator.OpType.Division; }
  break;
case 268:
#line 2273 "cs-parser.jay"
  { yyVal = Operator.OpType.Modulus; }
  break;
case 269:
#line 2274 "cs-parser.jay"
  { yyVal = Operator.OpType.BitwiseAnd; }
  break;
case 270:
#line 2275 "cs-parser.jay"
  { yyVal = Operator.OpType.BitwiseOr; }
  break;
case 271:
#line 2276 "cs-parser.jay"
  { yyVal = Operator.OpType.ExclusiveOr; }
  break;
case 272:
#line 2277 "cs-parser.jay"
  { yyVal = Operator.OpType.LeftShift; }
  break;
case 273:
#line 2278 "cs-parser.jay"
  { yyVal = Operator.OpType.RightShift; }
  break;
case 274:
#line 2279 "cs-parser.jay"
  { yyVal = Operator.OpType.Equality; }
  break;
case 275:
#line 2280 "cs-parser.jay"
  { yyVal = Operator.OpType.Inequality; }
  break;
case 276:
#line 2281 "cs-parser.jay"
  { yyVal = Operator.OpType.GreaterThan; }
  break;
case 277:
#line 2282 "cs-parser.jay"
  { yyVal = Operator.OpType.LessThan; }
  break;
case 278:
#line 2283 "cs-parser.jay"
  { yyVal = Operator.OpType.GreaterThanOrEqual; }
  break;
case 279:
#line 2284 "cs-parser.jay"
  { yyVal = Operator.OpType.LessThanOrEqual; }
  break;
case 280:
#line 2289 "cs-parser.jay"
  {
		params_modifiers_not_allowed = true;
	  }
  break;
case 281:
#line 2293 "cs-parser.jay"
  {
		params_modifiers_not_allowed = false;

		Location loc = (Location) yyVals[-5+yyTop];
		current_local_parameters = (Parameters)yyVals[-1+yyTop];  
		  
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.NotAllowed;
		}

		yyVal = new OperatorDeclaration (Operator.OpType.Implicit, (Expression) yyVals[-4+yyTop], loc);
	  }
  break;
case 282:
#line 2307 "cs-parser.jay"
  {
		params_modifiers_not_allowed = true;
	  }
  break;
case 283:
#line 2311 "cs-parser.jay"
  {
		params_modifiers_not_allowed = false;
		
		Location loc = (Location) yyVals[-5+yyTop];
		current_local_parameters = (Parameters)yyVals[-1+yyTop];  
		  
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.NotAllowed;
		}

		yyVal = new OperatorDeclaration (Operator.OpType.Explicit, (Expression) yyVals[-4+yyTop], loc);
	  }
  break;
case 284:
#line 2325 "cs-parser.jay"
  {
		syntax_error ((Location) yyVals[-1+yyTop], "'operator' expected");
	  }
  break;
case 285:
#line 2329 "cs-parser.jay"
  {
		syntax_error ((Location) yyVals[-1+yyTop], "'operator' expected");
	  }
  break;
case 286:
#line 2339 "cs-parser.jay"
  { 
		Constructor c = (Constructor) yyVals[-1+yyTop];
		c.Block = (ToplevelBlock) yyVals[0+yyTop];
		c.OptAttributes = (Attributes) yyVals[-3+yyTop];
		int yield_method = c.ModFlags & Modifiers.METHOD_YIELDS;
		int mods = (int) yyVals[-2+yyTop];
		
		if (RootContext.Documentation != null)
			c.DocComment = ConsumeStoredComment ();

		if ((mods & Modifiers.STATIC) != 0 && c.Name == current_container.Basename) {
			if ((mods & Modifiers.Accessibility) != 0){
				Report.Error (515, c.Location,
					"`{0}': access modifiers are not allowed on static constructors",
					c.GetSignatureForError ());
			}
	
			if (c.Initializer != null){
				Report.Error (514, c.Location,
					"`{0}': static constructor cannot have an explicit `this' or `base' constructor call",
					c.GetSignatureForError ());
			}
		}

		c.ModFlags = Modifiers.Check (Constructor.AllowedModifiers, mods, Modifiers.PRIVATE, c.Location) | yield_method;
		current_container.AddConstructor (c);

		current_local_parameters = null;
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 287:
#line 2374 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 288:
#line 2378 "cs-parser.jay"
  {
		((Constructor)yyVals[-1+yyTop]).Initializer = (ConstructorInitializer) yyVals[0+yyTop];
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 289:
#line 2386 "cs-parser.jay"
  {
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 290:
#line 2393 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-4+yyTop];
		current_local_parameters = (Parameters) yyVals[-1+yyTop];
		current_block = new ToplevelBlock (null, current_local_parameters, null, lt.Location);

		yyVal = new Constructor (current_class, lt.Value, 0, current_local_parameters,
				      null, lt.Location);

		anonymous_host = (IAnonymousHost) yyVal;
	  }
  break;
case 292:
#line 2407 "cs-parser.jay"
  { current_block = null; yyVal = null; }
  break;
case 293:
#line 2412 "cs-parser.jay"
  {
		yyVal = new ConstructorBaseInitializer ((ArrayList) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 294:
#line 2416 "cs-parser.jay"
  {
		yyVal = new ConstructorThisInitializer ((ArrayList) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 295:
#line 2419 "cs-parser.jay"
  {
		Report.Error (1018, (Location) yyVals[-1+yyTop], "Keyword this or base expected");
		yyVal = null;
	  }
  break;
case 296:
#line 2426 "cs-parser.jay"
  { yyVal = 0; }
  break;
case 297:
#line 2427 "cs-parser.jay"
  { yyVal = Modifiers.UNSAFE; }
  break;
case 298:
#line 2428 "cs-parser.jay"
  { yyVal = Modifiers.EXTERN; }
  break;
case 299:
#line 2433 "cs-parser.jay"
  {
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.NotAllowed;
		}
	  }
  break;
case 300:
#line 2440 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-3+yyTop];
		if (lt.Value != current_container.MemberName.Name){
			Report.Error (574, lt.Location, "Name of destructor must match name of class");
		} else if (current_container.Kind != Kind.Class){
			Report.Error (575, lt.Location, "Only class types can contain destructor");
		} else {
			Location l = lt.Location;

			int m = (int) yyVals[-6+yyTop];
			if (!RootContext.StdLib && current_container.Name == "System.Object")
				m |= Modifiers.PROTECTED | Modifiers.VIRTUAL;
			else
				m |= Modifiers.PROTECTED | Modifiers.OVERRIDE;
                        
			Method d = new Destructor (
				current_class, TypeManager.system_void_expr, m, "Finalize", 
				Parameters.EmptyReadOnlyParameters, (Attributes) yyVals[-7+yyTop], l);
			if (RootContext.Documentation != null)
				d.DocComment = ConsumeStoredComment ();
		  
			d.Block = (ToplevelBlock) yyVals[0+yyTop];
			current_container.AddMethod (d);
		}
	  }
  break;
case 301:
#line 2471 "cs-parser.jay"
  {
		current_array_type = null;
		foreach (VariableDeclaration var in (ArrayList) yyVals[-1+yyTop]) {

			MemberName name = new MemberName (var.identifier,
				var.Location);

			EventField e = new EventField (
				current_class, (Expression) yyVals[-2+yyTop], (int) yyVals[-4+yyTop], false, name,
				(Attributes) yyVals[-5+yyTop]);

			e.Initializer = var.expression_or_array_initializer;

			current_container.AddEvent (e);

			if (RootContext.Documentation != null) {
				e.DocComment = Lexer.consume_doc_comment ();
				Lexer.doc_state = XmlCommentState.Allowed;
			}
		}
	  }
  break;
case 302:
#line 2496 "cs-parser.jay"
  {
		implicit_value_parameter_type = (Expression) yyVals[-2+yyTop];  
		lexer.EventParsing = true;
	  }
  break;
case 303:
#line 2501 "cs-parser.jay"
  {
		lexer.EventParsing = false;  
	  }
  break;
case 304:
#line 2505 "cs-parser.jay"
  {
		MemberName name = (MemberName) yyVals[-5+yyTop];

		if (yyVals[-2+yyTop] == null){
			Report.Error (65, (Location) yyVals[-7+yyTop], "`{0}.{1}': event property must have both add and remove accessors",
				current_container.Name, name.GetSignatureForError ());
			yyVal = null;
		} else {
			Accessors accessors = (Accessors) yyVals[-2+yyTop];
			
			if (name.TypeArguments != null)
				syntax_error (lexer.Location, "an event can't have type arguments");

			if (accessors.get_or_add == null || accessors.set_or_remove == null)
				/* CS0073 is already reported, so no CS0065 here.*/
				yyVal = null;
			else {
				Event e = new EventProperty (
					current_class, (Expression) yyVals[-6+yyTop], (int) yyVals[-8+yyTop], false, name,
					(Attributes) yyVals[-9+yyTop], accessors.get_or_add, accessors.set_or_remove);
				if (RootContext.Documentation != null) {
					e.DocComment = Lexer.consume_doc_comment ();
					Lexer.doc_state = XmlCommentState.Allowed;
				}

				current_container.AddEvent (e);
				implicit_value_parameter_type = null;
			}
		}
	  }
  break;
case 305:
#line 2535 "cs-parser.jay"
  {
		MemberName mn = (MemberName) yyVals[-1+yyTop];

		if (mn.Left != null)
			Report.Error (71, mn.Location, "An explicit interface implementation of an event must use property syntax");
		else 
			Report.Error (71, mn.Location, "Event declaration should use property syntax");

		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 306:
#line 2550 "cs-parser.jay"
  {
		yyVal = new Accessors ((Accessor) yyVals[-1+yyTop], (Accessor) yyVals[0+yyTop]);
	  }
  break;
case 307:
#line 2554 "cs-parser.jay"
  {
		Accessors accessors = new Accessors ((Accessor) yyVals[0+yyTop], (Accessor) yyVals[-1+yyTop]);
		accessors.declared_in_reverse = true;
		yyVal = accessors;
	  }
  break;
case 308:
#line 2559 "cs-parser.jay"
  { yyVal = null; }
  break;
case 309:
#line 2560 "cs-parser.jay"
  { yyVal = null; }
  break;
case 310:
#line 2562 "cs-parser.jay"
  { 
		Report.Error (1055, GetLocation (yyVals[0+yyTop]), "An add or remove accessor expected");
		yyVal = null;
	  }
  break;
case 311:
#line 2566 "cs-parser.jay"
  { yyVal = null; }
  break;
case 312:
#line 2571 "cs-parser.jay"
  {
		Parameter [] args = new Parameter [1];
		Parameter implicit_value_parameter = new Parameter (
			implicit_value_parameter_type, "value", 
			Parameter.Modifier.NONE, null, (Location) yyVals[0+yyTop]);

		args [0] = implicit_value_parameter;
		
		current_local_parameters = new Parameters (args);  
		lexer.EventParsing = false;
		
		anonymous_host = SimpleAnonymousHost.GetSimple ();
	  }
  break;
case 313:
#line 2585 "cs-parser.jay"
  {
		Accessor accessor = new Accessor ((ToplevelBlock) yyVals[0+yyTop], 0, (Attributes) yyVals[-3+yyTop], (Location) yyVals[-2+yyTop]);
		lexer.EventParsing = true;
		
		current_local_parameters = null;
		SimpleAnonymousHost.Simple.Propagate (accessor);
		anonymous_host = null;
		
		yyVal = accessor;
	  }
  break;
case 314:
#line 2595 "cs-parser.jay"
  {
		Report.Error (73, (Location) yyVals[-1+yyTop], "An add or remove accessor must have a body");
		yyVal = null;
	  }
  break;
case 315:
#line 2599 "cs-parser.jay"
  {
		Report.Error (1609, (Location) yyVals[0+yyTop], "Modifiers cannot be placed on event accessor declarations");
		yyVal = null;
	  }
  break;
case 316:
#line 2607 "cs-parser.jay"
  {
		Parameter [] args = new Parameter [1];
		Parameter implicit_value_parameter = new Parameter (
			implicit_value_parameter_type, "value", 
			Parameter.Modifier.NONE, null, (Location) yyVals[0+yyTop]);

		args [0] = implicit_value_parameter;
		
		current_local_parameters = new Parameters (args);  
		lexer.EventParsing = false;
	  }
  break;
case 317:
#line 2619 "cs-parser.jay"
  {
		yyVal = new Accessor ((ToplevelBlock) yyVals[0+yyTop], 0, (Attributes) yyVals[-3+yyTop], (Location) yyVals[-2+yyTop]);
		lexer.EventParsing = true;
	  }
  break;
case 318:
#line 2623 "cs-parser.jay"
  {
		Report.Error (73, (Location) yyVals[-1+yyTop], "An add or remove accessor must have a body");
		yyVal = null;
	  }
  break;
case 319:
#line 2627 "cs-parser.jay"
  {
		Report.Error (1609, (Location) yyVals[0+yyTop], "Modifiers cannot be placed on event accessor declarations");
		yyVal = null;
	  }
  break;
case 320:
#line 2636 "cs-parser.jay"
  {
		IndexerDeclaration decl = (IndexerDeclaration) yyVals[-1+yyTop];

		implicit_value_parameter_type = decl.type;
		
		lexer.PropertyParsing = true;
		parsing_indexer  = true;
		
		indexer_parameters = decl.param_list;
		anonymous_host = SimpleAnonymousHost.GetSimple ();
	  }
  break;
case 321:
#line 2648 "cs-parser.jay"
  {
		  lexer.PropertyParsing = false;
		  has_get = has_set = false;
		  parsing_indexer  = false;
	  }
  break;
case 322:
#line 2654 "cs-parser.jay"
  { 
		if (yyVals[-2+yyTop] == null)
			break;

		/* The signature is computed from the signature of the indexer.  Look*/
	 	/* at section 3.6 on the spec*/
		Indexer indexer;
		IndexerDeclaration decl = (IndexerDeclaration) yyVals[-5+yyTop];
		Location loc = decl.location;
		Accessors accessors = (Accessors) yyVals[-2+yyTop];
		Accessor get_block = accessors.get_or_add;
		Accessor set_block = accessors.set_or_remove;

		MemberName name;
		if (decl.interface_type != null)
			name = new MemberName (decl.interface_type, TypeContainer.DefaultIndexerName, loc);
		else
			name = new MemberName (TypeContainer.DefaultIndexerName, loc);

		indexer = new Indexer (current_class, decl.type, name,
				       (int) yyVals[-6+yyTop], false, decl.param_list, (Attributes) yyVals[-7+yyTop],
				       get_block, set_block, accessors.declared_in_reverse);

		if (RootContext.Documentation != null)
			indexer.DocComment = ConsumeStoredComment ();

		current_container.AddIndexer (indexer);
		
		current_local_parameters = null;
		implicit_value_parameter_type = null;
		indexer_parameters = null;
	  }
  break;
case 323:
#line 2690 "cs-parser.jay"
  {
		Parameters pars = (Parameters) yyVals[-1+yyTop];
		if (pars.Empty){
			Report.Error (1551, (Location) yyVals[-3+yyTop], "Indexers must have at least one parameter");
		}
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}

		yyVal = new IndexerDeclaration ((Expression) yyVals[-4+yyTop], null, pars, (Location) yyVals[-3+yyTop]);
	  }
  break;
case 324:
#line 2703 "cs-parser.jay"
  {
		Parameters pars = (Parameters) yyVals[-1+yyTop];
		if (pars.Empty){
			Report.Error (1551, (Location) yyVals[-3+yyTop], "Indexers must have at least one parameter");
		}

		MemberName name = (MemberName) yyVals[-5+yyTop];
		yyVal = new IndexerDeclaration ((Expression) yyVals[-6+yyTop], name, pars, (Location) yyVals[-3+yyTop]);

		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 325:
#line 2723 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			enumTypeComment = Lexer.consume_doc_comment ();
	  }
  break;
case 326:
#line 2729 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-4+yyTop];
		Location enum_location = lt.Location;

		MemberName name = MakeName (new MemberName (lt.Value, enum_location));
		Enum e = new Enum (current_namespace, current_class, (Expression) yyVals[-3+yyTop], (int) yyVals[-6+yyTop],
				   name, (Attributes) yyVals[-7+yyTop]);
		
		if (RootContext.Documentation != null)
			e.DocComment = enumTypeComment;


		EnumMember em = null;
		foreach (VariableDeclaration ev in (ArrayList) yyVals[-1+yyTop]) {
			em = new EnumMember (
				e, em, ev.identifier, (Expression) ev.expression_or_array_initializer,
				ev.OptAttributes, ev.Location);

/*			if (RootContext.Documentation != null)*/
				em.DocComment = ev.DocComment;

			e.AddEnumMember (em);
		}

		current_container.AddTypeContainer (e);
		current_namespace.AddType (e);
		yyVal = e;

	  }
  break;
case 327:
#line 2761 "cs-parser.jay"
  { yyVal = TypeManager.system_int32_expr; }
  break;
case 328:
#line 2762 "cs-parser.jay"
  { yyVal = yyVals[0+yyTop];   }
  break;
case 329:
#line 2767 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 330:
#line 2772 "cs-parser.jay"
  {
	  	/* here will be evaluated after CLOSE_BLACE is consumed.*/
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 331:
#line 2778 "cs-parser.jay"
  {
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 332:
#line 2784 "cs-parser.jay"
  { yyVal = new ArrayList (4); }
  break;
case 333:
#line 2785 "cs-parser.jay"
  { yyVal = yyVals[-1+yyTop]; }
  break;
case 334:
#line 2790 "cs-parser.jay"
  {
		ArrayList l = new ArrayList (4);

		l.Add (yyVals[0+yyTop]);
		yyVal = l;
	  }
  break;
case 335:
#line 2797 "cs-parser.jay"
  {
		ArrayList l = (ArrayList) yyVals[-2+yyTop];

		l.Add (yyVals[0+yyTop]);

		yyVal = l;
	  }
  break;
case 336:
#line 2808 "cs-parser.jay"
  {
		VariableDeclaration vd = new VariableDeclaration (
			(LocatedToken) yyVals[0+yyTop], null, (Attributes) yyVals[-1+yyTop]);

		if (RootContext.Documentation != null) {
			vd.DocComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}

		yyVal = vd;
	  }
  break;
case 337:
#line 2820 "cs-parser.jay"
  {
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.NotAllowed;
		}
	  }
  break;
case 338:
#line 2827 "cs-parser.jay"
  { 
		VariableDeclaration vd = new VariableDeclaration (
			(LocatedToken) yyVals[-3+yyTop], yyVals[0+yyTop], (Attributes) yyVals[-4+yyTop]);

		if (RootContext.Documentation != null)
			vd.DocComment = ConsumeStoredComment ();

		yyVal = vd;
	  }
  break;
case 339:
#line 2844 "cs-parser.jay"
  {
		MemberName name = MakeName ((MemberName) yyVals[-3+yyTop]);
		Parameters p = (Parameters) yyVals[-1+yyTop];

		Delegate del = new Delegate (current_namespace, current_class, (Expression) yyVals[-4+yyTop],
					     (int) yyVals[-6+yyTop], name, p, (Attributes) yyVals[-7+yyTop]);

		if (RootContext.Documentation != null) {
			del.DocComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}

		current_container.AddDelegate (del);
		current_namespace.AddDelegate (del);
		current_delegate = del;
		lexer.ConstraintsParsing = true;
	  }
  break;
case 340:
#line 2862 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;
	  }
  break;
case 341:
#line 2866 "cs-parser.jay"
  {
		current_delegate.SetParameterInfo ((ArrayList) yyVals[-2+yyTop]);
		yyVal = current_delegate;

		current_delegate = null;
	  }
  break;
case 342:
#line 2876 "cs-parser.jay"
  {
		lexer.CheckNullable (false);
		yyVal = false;
	  }
  break;
case 343:
#line 2881 "cs-parser.jay"
  {
	  	/* FIXME: A hack with parsing conditional operator as nullable type*/
		/*if (RootContext.Version < LanguageVersion.ISO_2)*/
		/*	Report.FeatureIsNotAvailable (lexer.Location, "nullable types");*/
	  		
		lexer.CheckNullable (true);
		yyVal = true;
	  }
  break;
case 344:
#line 2893 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName (lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 345:
#line 2897 "cs-parser.jay"
  {
		LocatedToken lt1 = (LocatedToken) yyVals[-3+yyTop];
		LocatedToken lt2 = (LocatedToken) yyVals[-1+yyTop];
		if (RootContext.Version == LanguageVersion.ISO_1)
			Report.FeatureIsNotAvailable (lt1.Location, "namespace alias qualifier");
		
		yyVal = new MemberName (lt1.Value, lt2.Value, (TypeArguments) yyVals[0+yyTop], lt1.Location);
	  }
  break;
case 346:
#line 2905 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName ((MemberName) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 347:
#line 2913 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName (lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 348:
#line 2918 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName ((MemberName) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 349:
#line 2926 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName (lt.Value, (TypeArguments)yyVals[0+yyTop], lt.Location);	  
	  }
  break;
case 350:
#line 2937 "cs-parser.jay"
  { yyVal = null; }
  break;
case 351:
#line 2939 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-2+yyTop]), "generics");	  
	  
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 352:
#line 2952 "cs-parser.jay"
  { yyVal = null; }
  break;
case 353:
#line 2954 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-2+yyTop]), "generics");
	  
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 354:
#line 2964 "cs-parser.jay"
  {
		TypeArguments type_args = new TypeArguments (lexer.Location);
		type_args.Add ((Expression) yyVals[0+yyTop]);
		yyVal = type_args;
	  }
  break;
case 355:
#line 2970 "cs-parser.jay"
  {
		TypeArguments type_args = (TypeArguments) yyVals[-2+yyTop];
		type_args.Add ((Expression) yyVals[0+yyTop]);
		yyVal = type_args;
	  }
  break;
case 356:
#line 2979 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
  	  }
  break;
case 357:
#line 2983 "cs-parser.jay"
  {
		SimpleName sn = yyVals[0+yyTop] as SimpleName;
		if (sn == null)
			Error_TypeExpected (GetLocation (yyVals[0+yyTop]));
		else
			yyVals[0+yyTop] = new TypeParameterName (sn.Name, (Attributes) yyVals[-1+yyTop], lexer.Location);
		yyVal = yyVals[0+yyTop];  	  
  	  }
  break;
case 358:
#line 3001 "cs-parser.jay"
  {
		MemberName name = (MemberName) yyVals[-1+yyTop];

		if ((bool) yyVals[0+yyTop]) {
			yyVal = new ComposedCast (name.GetTypeExpression (), "?", lexer.Location);
		} else {
			if (RootContext.Version > LanguageVersion.ISO_2 && name.Name == "var")
				yyVal = new VarExpr (name.Location);
			else
				yyVal = name.GetTypeExpression ();
		}
	  }
  break;
case 359:
#line 3014 "cs-parser.jay"
  {
		if ((bool) yyVals[0+yyTop])
			yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "?", lexer.Location);
	  }
  break;
case 362:
#line 3024 "cs-parser.jay"
  {
		/**/
		/* Note that here only unmanaged types are allowed but we*/
		/* can't perform checks during this phase - we do it during*/
		/* semantic analysis.*/
		/**/
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "*", Lexer.Location);
	  }
  break;
case 363:
#line 3033 "cs-parser.jay"
  {
		yyVal = new ComposedCast (TypeManager.system_void_expr, "*", (Location) yyVals[-1+yyTop]);
	  }
  break;
case 364:
#line 3040 "cs-parser.jay"
  {
		if ((bool) yyVals[0+yyTop])
			yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "?", lexer.Location);
	  }
  break;
case 365:
#line 3045 "cs-parser.jay"
  {
		Location loc = GetLocation (yyVals[-1+yyTop]);
		if (loc.IsNull)
			loc = lexer.Location;
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], (string) yyVals[0+yyTop], loc);
	  }
  break;
case 366:
#line 3052 "cs-parser.jay"
  {
		Location loc = GetLocation (yyVals[-1+yyTop]);
		if (loc.IsNull)
			loc = lexer.Location;
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "*", loc);
	  }
  break;
case 367:
#line 3059 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], (string) yyVals[0+yyTop]);
	  }
  break;
case 368:
#line 3063 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "*");
	  }
  break;
case 369:
#line 3072 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "*");
	  }
  break;
case 370:
#line 3079 "cs-parser.jay"
  {
		ArrayList types = new ArrayList (2);
		types.Add (yyVals[0+yyTop]);
		yyVal = types;
	  }
  break;
case 371:
#line 3085 "cs-parser.jay"
  {
		ArrayList types = (ArrayList) yyVals[-2+yyTop];
		types.Add (yyVals[0+yyTop]);
		yyVal = types;
	  }
  break;
case 372:
#line 3094 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] is ComposedCast)
			Report.Error (1521, GetLocation (yyVals[0+yyTop]), "Invalid base type `{0}'", ((ComposedCast)yyVals[0+yyTop]).GetSignatureForError ());
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 373:
#line 3106 "cs-parser.jay"
  { yyVal = TypeManager.system_object_expr; }
  break;
case 374:
#line 3107 "cs-parser.jay"
  { yyVal = TypeManager.system_string_expr; }
  break;
case 375:
#line 3108 "cs-parser.jay"
  { yyVal = TypeManager.system_boolean_expr; }
  break;
case 376:
#line 3109 "cs-parser.jay"
  { yyVal = TypeManager.system_decimal_expr; }
  break;
case 377:
#line 3110 "cs-parser.jay"
  { yyVal = TypeManager.system_single_expr; }
  break;
case 378:
#line 3111 "cs-parser.jay"
  { yyVal = TypeManager.system_double_expr; }
  break;
case 380:
#line 3116 "cs-parser.jay"
  { yyVal = TypeManager.system_sbyte_expr; }
  break;
case 381:
#line 3117 "cs-parser.jay"
  { yyVal = TypeManager.system_byte_expr; }
  break;
case 382:
#line 3118 "cs-parser.jay"
  { yyVal = TypeManager.system_int16_expr; }
  break;
case 383:
#line 3119 "cs-parser.jay"
  { yyVal = TypeManager.system_uint16_expr; }
  break;
case 384:
#line 3120 "cs-parser.jay"
  { yyVal = TypeManager.system_int32_expr; }
  break;
case 385:
#line 3121 "cs-parser.jay"
  { yyVal = TypeManager.system_uint32_expr; }
  break;
case 386:
#line 3122 "cs-parser.jay"
  { yyVal = TypeManager.system_int64_expr; }
  break;
case 387:
#line 3123 "cs-parser.jay"
  { yyVal = TypeManager.system_uint64_expr; }
  break;
case 388:
#line 3124 "cs-parser.jay"
  { yyVal = TypeManager.system_char_expr; }
  break;
case 389:
#line 3125 "cs-parser.jay"
  { yyVal = TypeManager.system_void_expr; }
  break;
case 390:
#line 3130 "cs-parser.jay"
  {
		string rank_specifiers = (string) yyVals[-1+yyTop];
		if ((bool) yyVals[0+yyTop])
			rank_specifiers += "?";

		yyVal = current_array_type = new ComposedCast ((Expression) yyVals[-2+yyTop], rank_specifiers);
	  }
  break;
case 391:
#line 3144 "cs-parser.jay"
  {
		/* 7.5.1: Literals*/
	  }
  break;
case 392:
#line 3148 "cs-parser.jay"
  {
		MemberName mn = (MemberName) yyVals[0+yyTop];
		yyVal = mn.GetTypeExpression ();
	  }
  break;
case 393:
#line 3153 "cs-parser.jay"
  {
		LocatedToken lt1 = (LocatedToken) yyVals[-3+yyTop];
		LocatedToken lt2 = (LocatedToken) yyVals[-1+yyTop];
		if (RootContext.Version == LanguageVersion.ISO_1)
			Report.FeatureIsNotAvailable (lt1.Location, "namespace alias qualifier");

		yyVal = new QualifiedAliasMember (lt1.Value, lt2.Value, (TypeArguments) yyVals[0+yyTop], lt1.Location);
	  }
  break;
case 413:
#line 3183 "cs-parser.jay"
  { yyVal = new CharLiteral ((char) lexer.Value, lexer.Location); }
  break;
case 414:
#line 3184 "cs-parser.jay"
  { yyVal = new StringLiteral ((string) lexer.Value, lexer.Location); }
  break;
case 415:
#line 3185 "cs-parser.jay"
  { yyVal = new NullLiteral (lexer.Location); }
  break;
case 416:
#line 3189 "cs-parser.jay"
  { yyVal = new FloatLiteral ((float) lexer.Value, lexer.Location); }
  break;
case 417:
#line 3190 "cs-parser.jay"
  { yyVal = new DoubleLiteral ((double) lexer.Value, lexer.Location); }
  break;
case 418:
#line 3191 "cs-parser.jay"
  { yyVal = new DecimalLiteral ((decimal) lexer.Value, lexer.Location); }
  break;
case 419:
#line 3195 "cs-parser.jay"
  { 
		object v = lexer.Value;

		if (v is int){
			yyVal = new IntLiteral ((int) v, lexer.Location);
		} else if (v is uint)
			yyVal = new UIntLiteral ((UInt32) v, lexer.Location);
		else if (v is long)
			yyVal = new LongLiteral ((Int64) v, lexer.Location);
		else if (v is ulong)
			yyVal = new ULongLiteral ((UInt64) v, lexer.Location);
		else
			Console.WriteLine ("OOPS.  Unexpected result from scanner");
	  }
  break;
case 420:
#line 3212 "cs-parser.jay"
  { yyVal = new BoolLiteral (true, lexer.Location); }
  break;
case 421:
#line 3213 "cs-parser.jay"
  { yyVal = new BoolLiteral (false, lexer.Location); }
  break;
case 422:
#line 3218 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
		lexer.Deambiguate_CloseParens (yyVal);
		/* After this, the next token returned is one of*/
		/* CLOSE_PARENS_CAST, CLOSE_PARENS_NO_CAST (CLOSE_PARENS), CLOSE_PARENS_OPEN_PARENS*/
		/* or CLOSE_PARENS_MINUS.*/
	  }
  break;
case 423:
#line 3225 "cs-parser.jay"
  { CheckToken (1026, yyToken, "Expecting ')'", lexer.Location); }
  break;
case 424:
#line 3230 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 425:
#line 3234 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 426:
#line 3238 "cs-parser.jay"
  {
		/* If a parenthesized expression is followed by a minus, we need to wrap*/
		/* the expression inside a ParenthesizedExpression for the CS0075 check*/
		/* in Binary.DoResolve().*/
		yyVal = new ParenthesizedExpression ((Expression) yyVals[-1+yyTop]);
	  }
  break;
case 427:
#line 3248 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberAccess ((Expression) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 428:
#line 3253 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		/* TODO: Location is wrong as some predefined types doesn't hold a location*/
		yyVal = new MemberAccess ((Expression) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 430:
#line 3266 "cs-parser.jay"
  {
		if (yyVals[-3+yyTop] == null)
			Report.Error (1, (Location) yyVals[-2+yyTop], "Parse error");
	        else
			yyVal = new Invocation ((Expression) yyVals[-3+yyTop], (ArrayList) yyVals[-1+yyTop]);
	  }
  break;
case 431:
#line 3273 "cs-parser.jay"
  {
		yyVal = new Invocation ((Expression) yyVals[-3+yyTop], new ArrayList ());
	  }
  break;
case 432:
#line 3277 "cs-parser.jay"
  {
		yyVal = new InvocationOrCast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 433:
#line 3281 "cs-parser.jay"
  {
		ArrayList args = new ArrayList (1);
		args.Add (yyVals[-1+yyTop]);
		yyVal = new Invocation ((Expression) yyVals[-4+yyTop], args);
	  }
  break;
case 434:
#line 3287 "cs-parser.jay"
  {
		ArrayList args = ((ArrayList) yyVals[-3+yyTop]);
		args.Add (yyVals[-1+yyTop]);
		yyVal = new Invocation ((Expression) yyVals[-6+yyTop], args);
	  }
  break;
case 435:
#line 3295 "cs-parser.jay"
  { yyVal = null; }
  break;
case 437:
#line 3301 "cs-parser.jay"
  {
	  	if (yyVals[-1+yyTop] == null)
	  	  yyVal = CollectionOrObjectInitializers.Empty;
	  	else
	  	  yyVal = new CollectionOrObjectInitializers ((ArrayList) yyVals[-1+yyTop], GetLocation (yyVals[-2+yyTop]));
	  }
  break;
case 438:
#line 3308 "cs-parser.jay"
  {
	  	yyVal = new CollectionOrObjectInitializers ((ArrayList) yyVals[-2+yyTop], GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 439:
#line 3314 "cs-parser.jay"
  { yyVal = null; }
  break;
case 440:
#line 3316 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	}
  break;
case 441:
#line 3323 "cs-parser.jay"
  {
	  	ArrayList a = new ArrayList ();
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 442:
#line 3329 "cs-parser.jay"
  {
	  	ArrayList a = (ArrayList)yyVals[-2+yyTop];
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 443:
#line 3338 "cs-parser.jay"
  {
	  	LocatedToken lt = yyVals[-2+yyTop] as LocatedToken;
	  	yyVal = new ElementInitializer (lt.Value, (Expression)yyVals[0+yyTop], lt.Location);
	  }
  break;
case 444:
#line 3343 "cs-parser.jay"
  {
		yyVal = new CollectionElementInitializer ((Expression)yyVals[0+yyTop]);
	  }
  break;
case 445:
#line 3347 "cs-parser.jay"
  {
	  	yyVal = new CollectionElementInitializer ((ArrayList)yyVals[-1+yyTop], GetLocation (yyVals[-2+yyTop]));
	  }
  break;
case 446:
#line 3351 "cs-parser.jay"
  {
	  	Report.Error (1920, GetLocation (yyVals[-1+yyTop]), "An element initializer cannot be empty");
	  }
  break;
case 449:
#line 3362 "cs-parser.jay"
  { yyVal = null; }
  break;
case 451:
#line 3368 "cs-parser.jay"
  { 
		ArrayList list = new ArrayList (4);
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 452:
#line 3374 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-2+yyTop];
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 453:
#line 3379 "cs-parser.jay"
  {
		CheckToken (1026, yyToken, "Expected `,' or `)'", GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 454:
#line 3387 "cs-parser.jay"
  {
		yyVal = new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Expression);
	  }
  break;
case 455:
#line 3391 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 456:
#line 3398 "cs-parser.jay"
  { 
		yyVal = new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Ref);
	  }
  break;
case 457:
#line 3402 "cs-parser.jay"
  { 
		yyVal = new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Out);
	  }
  break;
case 458:
#line 3406 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-1+yyTop];
		Argument[] args = new Argument [list.Count];
		list.CopyTo (args, 0);

		Expression expr = new Arglist (args, (Location) yyVals[-3+yyTop]);
		yyVal = new Argument (expr, Argument.AType.Expression);
	  }
  break;
case 459:
#line 3415 "cs-parser.jay"
  {
		yyVal = new Argument (new Arglist ((Location) yyVals[-2+yyTop]), Argument.AType.Expression);
	  }
  break;
case 460:
#line 3419 "cs-parser.jay"
  {
		yyVal = new Argument (new ArglistAccess ((Location) yyVals[0+yyTop]), Argument.AType.ArgList);
	  }
  break;
case 461:
#line 3425 "cs-parser.jay"
  { note ("section 5.4"); yyVal = yyVals[0+yyTop]; }
  break;
case 462:
#line 3430 "cs-parser.jay"
  {
		yyVal = new ElementAccess ((Expression) yyVals[-3+yyTop], (ArrayList) yyVals[-1+yyTop]);
	  }
  break;
case 463:
#line 3434 "cs-parser.jay"
  {
		/* So the super-trick is that primary_expression*/
		/* can only be either a SimpleName or a MemberAccess. */
		/* The MemberAccess case arises when you have a fully qualified type-name like :*/
		/* Foo.Bar.Blah i;*/
		/* SimpleName is when you have*/
		/* Blah i;*/
		  
		Expression expr = (Expression) yyVals[-1+yyTop];  
		if (expr is ComposedCast){
			yyVal = new ComposedCast (expr, (string) yyVals[0+yyTop]);
		} else if (expr is ATypeNameExpression){
			/**/
			/* So we extract the string corresponding to the SimpleName*/
			/* or MemberAccess*/
			/* */
			yyVal = new ComposedCast (expr, (string) yyVals[0+yyTop]);
		} else {
			Error_ExpectingTypeName (expr);
			yyVal = TypeManager.system_object_expr;
		}
		
		current_array_type = (Expression)yyVal;
	  }
  break;
case 464:
#line 3462 "cs-parser.jay"
  {
		ArrayList list = new ArrayList (4);
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 465:
#line 3468 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-2+yyTop];
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 466:
#line 3477 "cs-parser.jay"
  {
		yyVal = new This (current_block, (Location) yyVals[0+yyTop]);
	  }
  break;
case 467:
#line 3484 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new BaseAccess (lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 468:
#line 3489 "cs-parser.jay"
  {
		yyVal = new BaseIndexerAccess ((ArrayList) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 469:
#line 3492 "cs-parser.jay"
  {
		Report.Error (175, (Location) yyVals[-1+yyTop], "Use of keyword `base' is not valid in this context");
		yyVal = null;
	  }
  break;
case 470:
#line 3500 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PostIncrement,
				       (Expression) yyVals[-1+yyTop], (Location) yyVals[0+yyTop]);
	  }
  break;
case 471:
#line 3508 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PostDecrement,
				       (Expression) yyVals[-1+yyTop], (Location) yyVals[0+yyTop]);
	  }
  break;
case 475:
#line 3522 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			if (RootContext.Version <= LanguageVersion.ISO_2)
				Report.FeatureIsNotAvailable (GetLocation (yyVals[-5+yyTop]), "object initializers");
				
			yyVal = new NewInitialize ((Expression) yyVals[-4+yyTop], (ArrayList) yyVals[-2+yyTop], (CollectionOrObjectInitializers) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]);
		}
		else
			yyVal = new New ((Expression) yyVals[-4+yyTop], (ArrayList) yyVals[-2+yyTop], (Location) yyVals[-5+yyTop]);
	  }
  break;
case 476:
#line 3533 "cs-parser.jay"
  {
		if (RootContext.Version <= LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-2+yyTop]), "collection initializers");
	  
		yyVal = new NewInitialize ((Expression) yyVals[-1+yyTop], null, (CollectionOrObjectInitializers) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 477:
#line 3545 "cs-parser.jay"
  {
		yyVal = new ArrayCreation ((Expression) yyVals[-5+yyTop], (ArrayList) yyVals[-3+yyTop], (string) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-6+yyTop]);
	  }
  break;
case 478:
#line 3549 "cs-parser.jay"
  {
		yyVal = new ArrayCreation ((Expression) yyVals[-2+yyTop], (string) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 479:
#line 3553 "cs-parser.jay"
  {
		yyVal = new ImplicitlyTypedArrayCreation ((string) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 480:
#line 3557 "cs-parser.jay"
  {
		Report.Error (1031, (Location) yyVals[-1+yyTop], "Type expected");
                yyVal = null;
	  }
  break;
case 481:
#line 3562 "cs-parser.jay"
  {
		Report.Error (1526, (Location) yyVals[-2+yyTop], "A new expression requires () or [] after type");
		yyVal = null;
	  }
  break;
case 482:
#line 3570 "cs-parser.jay"
  {
	  	if (RootContext.Version <= LanguageVersion.ISO_2)
	  		Report.FeatureIsNotAvailable (GetLocation (yyVals[-3+yyTop]), "anonymous types");

		yyVal = new AnonymousTypeDeclaration ((ArrayList) yyVals[-1+yyTop], current_container, GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 483:
#line 3579 "cs-parser.jay"
  { yyVal = null; }
  break;
case 484:
#line 3581 "cs-parser.jay"
  {
	  	ArrayList a = new ArrayList (4);
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 485:
#line 3587 "cs-parser.jay"
  {
	  	ArrayList a = (ArrayList) yyVals[-2+yyTop];
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 486:
#line 3596 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken)yyVals[-2+yyTop];
	  	yyVal = new AnonymousTypeParameter ((Expression)yyVals[0+yyTop], lt.Value, lt.Location);
	  }
  break;
case 487:
#line 3601 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken)yyVals[0+yyTop];
	  	yyVal = new AnonymousTypeParameter (new SimpleName (lt.Value, lt.Location),
	  		lt.Value, lt.Location);
	  }
  break;
case 488:
#line 3607 "cs-parser.jay"
  {
	  	MemberAccess ma = (MemberAccess) yyVals[0+yyTop];
	  	yyVal = new AnonymousTypeParameter (ma, ma.Name, ma.Location);
	  }
  break;
case 489:
#line 3612 "cs-parser.jay"
  {
		Report.Error (746, lexer.Location, "Invalid anonymous type member declarator. " +
		"Anonymous type members must be a member assignment, simple name or member access expression");
	  }
  break;
case 490:
#line 3620 "cs-parser.jay"
  {
		  yyVal = "";
	  }
  break;
case 491:
#line 3624 "cs-parser.jay"
  {
			yyVal = yyVals[0+yyTop];
	  }
  break;
case 492:
#line 3631 "cs-parser.jay"
  {
		yyVal = "";
	  }
  break;
case 493:
#line 3635 "cs-parser.jay"
  {
		yyVal = "?";
	  }
  break;
case 494:
#line 3639 "cs-parser.jay"
  {
		if ((bool) yyVals[-1+yyTop])
			yyVal = "?" + yyVals[0+yyTop];
		else
			yyVal = yyVals[0+yyTop];
	  }
  break;
case 495:
#line 3646 "cs-parser.jay"
  {
		if ((bool) yyVals[-2+yyTop])
			yyVal = "?" + yyVals[-1+yyTop] + "?";
		else
			yyVal = yyVals[-1+yyTop] + "?";
	  }
  break;
case 496:
#line 3656 "cs-parser.jay"
  {
		  yyVal = (string) yyVals[0+yyTop] + (string) yyVals[-1+yyTop];
	  }
  break;
case 497:
#line 3663 "cs-parser.jay"
  {
		yyVal = "[" + (string) yyVals[-1+yyTop] + "]";
	  }
  break;
case 498:
#line 3670 "cs-parser.jay"
  {
		yyVal = "";
	  }
  break;
case 499:
#line 3674 "cs-parser.jay"
  {
		  yyVal = yyVals[0+yyTop];
	  }
  break;
case 500:
#line 3681 "cs-parser.jay"
  {
		yyVal = ",";
	  }
  break;
case 501:
#line 3685 "cs-parser.jay"
  {
		yyVal = (string) yyVals[-1+yyTop] + ",";
	  }
  break;
case 502:
#line 3692 "cs-parser.jay"
  {
		yyVal = null;
	  }
  break;
case 503:
#line 3696 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 504:
#line 3703 "cs-parser.jay"
  {
		ArrayList list = new ArrayList (4);
		yyVal = list;
	  }
  break;
case 505:
#line 3708 "cs-parser.jay"
  {
		yyVal = (ArrayList) yyVals[-2+yyTop];
	  }
  break;
case 506:
#line 3715 "cs-parser.jay"
  {
		ArrayList list = new ArrayList (4);
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 507:
#line 3721 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-2+yyTop];
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 508:
#line 3730 "cs-parser.jay"
  {
	  	pushed_current_array_type = current_array_type;
	  	lexer.TypeOfParsing = true;
	  }
  break;
case 509:
#line 3735 "cs-parser.jay"
  {
	  	lexer.TypeOfParsing = false;
		Expression type = (Expression)yyVals[-1+yyTop];
		if (type == TypeManager.system_void_expr)
			yyVal = new TypeOfVoid ((Location) yyVals[-4+yyTop]);
		else
			yyVal = new TypeOf (type, (Location) yyVals[-4+yyTop]);
		current_array_type = pushed_current_array_type;
	  }
  break;
case 510:
#line 3748 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 511:
#line 3752 "cs-parser.jay"
  {
		yyVal = new UnboundTypeExpression ((MemberName)yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 512:
#line 3759 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (lexer.Location, "generics");
	  
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);

		yyVal = new MemberName (lt.Value, ta, lt.Location);
	  }
  break;
case 513:
#line 3769 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-3+yyTop];
		MemberName left = new MemberName (lt.Value, lt.Location);
		lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);
		
		if (RootContext.Version == LanguageVersion.ISO_1)
			Report.FeatureIsNotAvailable (lt.Location, "namespace alias qualifier");
		
		yyVal = new MemberName (left, lt.Value, ta, lt.Location);
	  }
  break;
case 514:
#line 3781 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);
		
	  	yyVal = new MemberName ((MemberName)yyVals[-3+yyTop], lt.Value, ta, lt.Location);
	  }
  break;
case 515:
#line 3788 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);
		
	  	yyVal = new MemberName ((MemberName)yyVals[-3+yyTop], lt.Value, ta, lt.Location);
	  }
  break;
case 516:
#line 3798 "cs-parser.jay"
  { 
		yyVal = new SizeOf ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 517:
#line 3805 "cs-parser.jay"
  {
		yyVal = new CheckedExpr ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 518:
#line 3812 "cs-parser.jay"
  {
		yyVal = new UnCheckedExpr ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 519:
#line 3819 "cs-parser.jay"
  {
		Expression deref;
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];

		deref = new Indirection ((Expression) yyVals[-2+yyTop], lt.Location);
		yyVal = new MemberAccess (deref, lt.Value);
	  }
  break;
case 520:
#line 3830 "cs-parser.jay"
  {
		start_anonymous (false, (Parameters) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 521:
#line 3834 "cs-parser.jay"
  {
		yyVal = end_anonymous ((ToplevelBlock) yyVals[0+yyTop], (Location) yyVals[-3+yyTop]);
	}
  break;
case 522:
#line 3840 "cs-parser.jay"
  { yyVal = null; }
  break;
case 524:
#line 3846 "cs-parser.jay"
  {
	  	params_modifiers_not_allowed = true; 
	  }
  break;
case 525:
#line 3850 "cs-parser.jay"
  {
	  	params_modifiers_not_allowed = false;
	  	yyVal = yyVals[-1+yyTop];
	  }
  break;
case 526:
#line 3858 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (lexer.Location, "default value expression");

		yyVal = new DefaultValueExpression ((Expression) yyVals[-1+yyTop], lexer.Location);
	  }
  break;
case 528:
#line 3869 "cs-parser.jay"
  {
		yyVal = new Unary (Unary.Operator.LogicalNot, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 529:
#line 3873 "cs-parser.jay"
  {
		yyVal = new Unary (Unary.Operator.OnesComplement, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 531:
#line 3881 "cs-parser.jay"
  {
		yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 532:
#line 3885 "cs-parser.jay"
  {
	  	yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 533:
#line 3889 "cs-parser.jay"
  {
		yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 535:
#line 3897 "cs-parser.jay"
  {
		/* TODO: wrong location*/
		yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 537:
#line 3910 "cs-parser.jay"
  { 
	  	yyVal = new Unary (Unary.Operator.UnaryPlus, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 538:
#line 3914 "cs-parser.jay"
  { 
		yyVal = new Unary (Unary.Operator.UnaryNegation, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 539:
#line 3918 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PreIncrement,
				       (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 540:
#line 3923 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PreDecrement,
				       (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 541:
#line 3928 "cs-parser.jay"
  {
		yyVal = new Indirection ((Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 542:
#line 3932 "cs-parser.jay"
  {
		yyVal = new Unary (Unary.Operator.AddressOf, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 544:
#line 3940 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Multiply, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 545:
#line 3945 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Division, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 546:
#line 3950 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Modulus, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 548:
#line 3959 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Addition, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 549:
#line 3964 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Subtraction, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 551:
#line 3973 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LeftShift, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 552:
#line 3978 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.RightShift, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 553:
#line 3986 "cs-parser.jay"
  {
		yyVal = false;
	  }
  break;
case 554:
#line 3990 "cs-parser.jay"
  {
		lexer.PutbackNullable ();
		yyVal = true;
	  }
  break;
case 555:
#line 3998 "cs-parser.jay"
  {
		if (((bool) yyVals[0+yyTop]) && (yyVals[-1+yyTop] is ComposedCast))
			yyVal = ((ComposedCast) yyVals[-1+yyTop]).RemoveNullable ();
		else
			yyVal = yyVals[-1+yyTop];
	  }
  break;
case 557:
#line 4009 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LessThan, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 558:
#line 4014 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.GreaterThan, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 559:
#line 4019 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LessThanOrEqual, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 560:
#line 4024 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.GreaterThanOrEqual, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 561:
#line 4029 "cs-parser.jay"
  {
		yyErrorFlag = 3;
	  }
  break;
case 562:
#line 4032 "cs-parser.jay"
  {
		yyVal = new Is ((Expression) yyVals[-3+yyTop], (Expression) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 563:
#line 4036 "cs-parser.jay"
  {
		yyErrorFlag = 3;
	  }
  break;
case 564:
#line 4039 "cs-parser.jay"
  {
		yyVal = new As ((Expression) yyVals[-3+yyTop], (Expression) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 566:
#line 4047 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Equality, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 567:
#line 4052 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Inequality, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 569:
#line 4061 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.BitwiseAnd, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 571:
#line 4070 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.ExclusiveOr, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 573:
#line 4079 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.BitwiseOr, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 575:
#line 4088 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LogicalAnd, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 577:
#line 4097 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LogicalOr, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 579:
#line 4106 "cs-parser.jay"
  {
		yyVal = new Conditional ((Expression) yyVals[-4+yyTop], (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 580:
#line 4110 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-1+yyTop]), "null coalescing operator");
			
		yyVal = new Nullable.NullCoalescingOperator ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 581:
#line 4118 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((Expression) yyVals[-2+yyTop], "?", lexer.Location);
		lexer.PutbackCloseParens ();
	  }
  break;
case 582:
#line 4126 "cs-parser.jay"
  {
		yyVal = new Assign ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 583:
#line 4130 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Multiply, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 584:
#line 4135 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Division, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 585:
#line 4140 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Modulus, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 586:
#line 4145 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Addition, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 587:
#line 4150 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Subtraction, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 588:
#line 4155 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.LeftShift, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 589:
#line 4160 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.RightShift, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 590:
#line 4165 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.BitwiseAnd, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 591:
#line 4170 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.BitwiseOr, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 592:
#line 4175 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.ExclusiveOr, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 593:
#line 4183 "cs-parser.jay"
  {
		ArrayList pars = new ArrayList (4);
		pars.Add (yyVals[0+yyTop]);

		yyVal = pars;
	  }
  break;
case 594:
#line 4190 "cs-parser.jay"
  {
		ArrayList pars = (ArrayList) yyVals[-2+yyTop];
		Parameter p = (Parameter)yyVals[0+yyTop];
		if (pars[0].GetType () != p.GetType ()) {
			Report.Error (748, p.Location, "All lambda parameters must be typed either explicitly or implicitly");
		}
		
		pars.Add (p);
		yyVal = pars;
	  }
  break;
case 595:
#line 4204 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];

		yyVal = new Parameter ((Expression) yyVals[-1+yyTop], lt.Value, (Parameter.Modifier) yyVals[-2+yyTop], null, lt.Location);
	  }
  break;
case 596:
#line 4210 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];

		yyVal = new Parameter ((Expression) yyVals[-1+yyTop], lt.Value, Parameter.Modifier.NONE, null, lt.Location);
	  }
  break;
case 597:
#line 4216 "cs-parser.jay"
  {
	  	LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		yyVal = new ImplicitLambdaParameter (lt.Value, lt.Location);
	  }
  break;
case 598:
#line 4223 "cs-parser.jay"
  { yyVal = Parameters.EmptyReadOnlyParameters; }
  break;
case 599:
#line 4224 "cs-parser.jay"
  { 
		ArrayList pars_list = (ArrayList) yyVals[0+yyTop];
		yyVal = new Parameters ((Parameter[])pars_list.ToArray (typeof (Parameter)));
	  }
  break;
case 600:
#line 4231 "cs-parser.jay"
  {
		start_block (lexer.Location);
	  }
  break;
case 601:
#line 4235 "cs-parser.jay"
  {
		Block b = end_block (lexer.Location);
		b.AddStatement (new ContextualReturn ((Expression) yyVals[0+yyTop]));
		yyVal = b;
	  }
  break;
case 602:
#line 4240 "cs-parser.jay"
  { 
	  	yyVal = yyVals[0+yyTop]; 
	  }
  break;
case 603:
#line 4247 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		Parameter p = new ImplicitLambdaParameter (lt.Value, lt.Location);
		start_anonymous (true, new Parameters (p), (Location) yyVals[0+yyTop]);
	  }
  break;
case 604:
#line 4253 "cs-parser.jay"
  {
		yyVal = end_anonymous ((ToplevelBlock) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 605:
#line 4257 "cs-parser.jay"
  {
		start_anonymous (true, (Parameters) yyVals[-2+yyTop], (Location) yyVals[0+yyTop]);
	  }
  break;
case 606:
#line 4261 "cs-parser.jay"
  {
		yyVal = end_anonymous ((ToplevelBlock) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 614:
#line 4293 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 615:
#line 4297 "cs-parser.jay"
  {
		MemberName name = MakeName ((MemberName) yyVals[0+yyTop]);
		push_current_class (new Class (current_namespace, current_class, name, (int) yyVals[-4+yyTop], (Attributes) yyVals[-5+yyTop]), yyVals[-3+yyTop]);
	  }
  break;
case 616:
#line 4303 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		current_class.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);

		if (RootContext.Documentation != null) {
			current_container.DocComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 617:
#line 4314 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 618:
#line 4319 "cs-parser.jay"
  {
		yyVal = pop_current_class ();
	  }
  break;
case 619:
#line 4326 "cs-parser.jay"
  { yyVal = null; }
  break;
case 620:
#line 4328 "cs-parser.jay"
  { yyVal = yyVals[0+yyTop]; }
  break;
case 621:
#line 4332 "cs-parser.jay"
  { yyVal = (int) 0; }
  break;
case 624:
#line 4339 "cs-parser.jay"
  { 
		int m1 = (int) yyVals[-1+yyTop];
		int m2 = (int) yyVals[0+yyTop];

		if ((m1 & m2) != 0) {
			Location l = lexer.Location;
			Report.Error (1004, l, "Duplicate `{0}' modifier", Modifiers.Name (m2));
		}
		yyVal = (int) (m1 | m2);
	  }
  break;
case 625:
#line 4352 "cs-parser.jay"
  { yyVal = Modifiers.NEW; }
  break;
case 626:
#line 4353 "cs-parser.jay"
  { yyVal = Modifiers.PUBLIC; }
  break;
case 627:
#line 4354 "cs-parser.jay"
  { yyVal = Modifiers.PROTECTED; }
  break;
case 628:
#line 4355 "cs-parser.jay"
  { yyVal = Modifiers.INTERNAL; }
  break;
case 629:
#line 4356 "cs-parser.jay"
  { yyVal = Modifiers.PRIVATE; }
  break;
case 630:
#line 4357 "cs-parser.jay"
  { yyVal = Modifiers.ABSTRACT; }
  break;
case 631:
#line 4358 "cs-parser.jay"
  { yyVal = Modifiers.SEALED; }
  break;
case 632:
#line 4359 "cs-parser.jay"
  { yyVal = Modifiers.STATIC; }
  break;
case 633:
#line 4360 "cs-parser.jay"
  { yyVal = Modifiers.READONLY; }
  break;
case 634:
#line 4361 "cs-parser.jay"
  { yyVal = Modifiers.VIRTUAL; }
  break;
case 635:
#line 4362 "cs-parser.jay"
  { yyVal = Modifiers.OVERRIDE; }
  break;
case 636:
#line 4363 "cs-parser.jay"
  { yyVal = Modifiers.EXTERN; }
  break;
case 637:
#line 4364 "cs-parser.jay"
  { yyVal = Modifiers.VOLATILE; }
  break;
case 638:
#line 4365 "cs-parser.jay"
  { yyVal = Modifiers.UNSAFE; }
  break;
case 641:
#line 4374 "cs-parser.jay"
  { current_container.AddBasesForPart (current_class, (ArrayList) yyVals[0+yyTop]); }
  break;
case 642:
#line 4378 "cs-parser.jay"
  { yyVal = null; }
  break;
case 643:
#line 4380 "cs-parser.jay"
  { yyVal = yyVals[0+yyTop]; }
  break;
case 644:
#line 4384 "cs-parser.jay"
  {
		ArrayList constraints = new ArrayList (1);
		constraints.Add (yyVals[0+yyTop]);
		yyVal = constraints;
	  }
  break;
case 645:
#line 4389 "cs-parser.jay"
  {
		ArrayList constraints = (ArrayList) yyVals[-1+yyTop];
		Constraints new_constraint = (Constraints)yyVals[0+yyTop];

		foreach (Constraints c in constraints) {
			if (new_constraint.TypeParameter == c.TypeParameter) {
				Report.Error (409, new_constraint.Location, "A constraint clause has already been specified for type parameter `{0}'",
					new_constraint.TypeParameter);
			}
		}

		constraints.Add (new_constraint);
		yyVal = constraints;
	  }
  break;
case 646:
#line 4406 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		yyVal = new Constraints (lt.Value, (ArrayList) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 647:
#line 4413 "cs-parser.jay"
  {
		ArrayList constraints = new ArrayList (1);
		constraints.Add (yyVals[0+yyTop]);
		yyVal = constraints;
	  }
  break;
case 648:
#line 4418 "cs-parser.jay"
  {
		ArrayList constraints = (ArrayList) yyVals[-2+yyTop];

		constraints.Add (yyVals[0+yyTop]);
		yyVal = constraints;
	  }
  break;
case 650:
#line 4428 "cs-parser.jay"
  {
		yyVal = SpecialConstraint.Constructor;
	  }
  break;
case 651:
#line 4431 "cs-parser.jay"
  {
		yyVal = SpecialConstraint.ReferenceType;
	  }
  break;
case 652:
#line 4434 "cs-parser.jay"
  {
		yyVal = SpecialConstraint.ValueType;
	  }
  break;
case 653:
#line 4454 "cs-parser.jay"
  {
		++lexer.parsing_block;
		start_block ((Location) yyVals[0+yyTop]);
	  }
  break;
case 654:
#line 4459 "cs-parser.jay"
  {
	 	--lexer.parsing_block;
		yyVal = end_block ((Location) yyVals[0+yyTop]);
	  }
  break;
case 655:
#line 4467 "cs-parser.jay"
  {
		++lexer.parsing_block;
	  }
  break;
case 656:
#line 4471 "cs-parser.jay"
  {
		--lexer.parsing_block;
		yyVal = end_block ((Location) yyVals[0+yyTop]);
	  }
  break;
case 661:
#line 4489 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null && (Block) yyVals[0+yyTop] != current_block){
			current_block.AddStatement ((Statement) yyVals[0+yyTop]);
			current_block = (Block) yyVals[0+yyTop];
		}
	  }
  break;
case 662:
#line 4496 "cs-parser.jay"
  {
		current_block.AddStatement ((Statement) yyVals[0+yyTop]);
	  }
  break;
case 678:
#line 4521 "cs-parser.jay"
  {
		  Report.Error (1023, GetLocation (yyVals[0+yyTop]), "An embedded statement may not be a declaration or labeled statement");
		  yyVal = null;
	  }
  break;
case 679:
#line 4526 "cs-parser.jay"
  {
		  Report.Error (1023, GetLocation (yyVals[0+yyTop]), "An embedded statement may not be a declaration or labeled statement");
		  yyVal = null;
	  }
  break;
case 680:
#line 4534 "cs-parser.jay"
  {
		  yyVal = EmptyStatement.Value;
	  }
  break;
case 681:
#line 4541 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		LabeledStatement labeled = new LabeledStatement (lt.Value, lt.Location);

		if (current_block.AddLabel (labeled))
			current_block.AddStatement (labeled);
	  }
  break;
case 683:
#line 4553 "cs-parser.jay"
  {
		current_array_type = null;
		if (yyVals[-1+yyTop] != null){
			DictionaryEntry de = (DictionaryEntry) yyVals[-1+yyTop];
			Expression e = (Expression) de.Key;

			yyVal = declare_local_variables (e, (ArrayList) de.Value, e.Location);
		}
	  }
  break;
case 684:
#line 4564 "cs-parser.jay"
  {
		current_array_type = null;
		if (yyVals[-1+yyTop] != null){
			DictionaryEntry de = (DictionaryEntry) yyVals[-1+yyTop];

			yyVal = declare_local_constants ((Expression) de.Key, (ArrayList) de.Value);
		}
	  }
  break;
case 685:
#line 4582 "cs-parser.jay"
  { 
		/* FIXME: Do something smart here regarding the composition of the type.*/

		/* Ok, the above "primary_expression" is there to get rid of*/
		/* both reduce/reduce and shift/reduces in the grammar, it should*/
		/* really just be "type_name".  If you use type_name, a reduce/reduce*/
		/* creeps up.  If you use namespace_or_type_name (which is all we need*/
		/* really) two shift/reduces appear.*/
		/* */

		/* So the super-trick is that primary_expression*/
		/* can only be either a SimpleName or a MemberAccess. */
		/* The MemberAccess case arises when you have a fully qualified type-name like :*/
		/* Foo.Bar.Blah i;*/
		/* SimpleName is when you have*/
		/* Blah i;*/
		
		Expression expr = (Expression) yyVals[-1+yyTop];  
		if (expr is ComposedCast){
			yyVal = new ComposedCast (expr, (string) yyVals[0+yyTop]);
		} else if (expr is ATypeNameExpression){
			/**/
			/* So we extract the string corresponding to the SimpleName*/
			/* or MemberAccess*/
			/**/
			
			if ((string) yyVals[0+yyTop] == "") {
				SimpleName sn = expr as SimpleName;
				if (sn != null && RootContext.Version > LanguageVersion.ISO_2 && sn.Name == "var")
					yyVal = new VarExpr (sn.Location);
				else
					yyVal = yyVals[-1+yyTop];
			} else {
				yyVal = new ComposedCast (expr, (string) yyVals[0+yyTop]);
			}
		} else {
			Error_ExpectingTypeName (expr);
			yyVal = TypeManager.system_object_expr;
		}
	  }
  break;
case 686:
#line 4623 "cs-parser.jay"
  {
		if ((string) yyVals[0+yyTop] == "")
			yyVal = yyVals[-1+yyTop];
		else
			yyVal = current_array_type = new ComposedCast ((Expression) yyVals[-1+yyTop], (string) yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 687:
#line 4633 "cs-parser.jay"
  {
		Expression expr = (Expression) yyVals[-1+yyTop];  

		if (expr is ATypeNameExpression) {
			yyVal = new ComposedCast (expr, "*");
		} else {
			Error_ExpectingTypeName (expr);
			yyVal = expr;
		}
	  }
  break;
case 688:
#line 4644 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "*", lexer.Location);
	  }
  break;
case 689:
#line 4648 "cs-parser.jay"
  {
		yyVal = new ComposedCast (TypeManager.system_void_expr, "*", (Location) yyVals[-1+yyTop]);
	  }
  break;
case 690:
#line 4652 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "*");
	  }
  break;
case 691:
#line 4659 "cs-parser.jay"
  {
		if (yyVals[-1+yyTop] != null) {
			VarExpr ve = yyVals[-1+yyTop] as VarExpr;
			if (ve != null)
				ve.VariableInitializer = (ArrayList)yyVals[0+yyTop];
				
			yyVal = new DictionaryEntry (yyVals[-1+yyTop], yyVals[0+yyTop]);
		} else
			yyVal = null;
	  }
  break;
case 692:
#line 4670 "cs-parser.jay"
  {
		if (yyVals[-2+yyTop] != null){
			Expression t;

			if ((string) yyVals[-1+yyTop] == "")
				t = (Expression) yyVals[-2+yyTop];
			else
				t = new ComposedCast ((Expression) yyVals[-2+yyTop], (string) yyVals[-1+yyTop]);
			yyVal = new DictionaryEntry (t, yyVals[0+yyTop]);
		} else 
			yyVal = null;
	  }
  break;
case 693:
#line 4686 "cs-parser.jay"
  {
		if (yyVals[-1+yyTop] != null)
			yyVal = new DictionaryEntry (yyVals[-1+yyTop], yyVals[0+yyTop]);
		else
			yyVal = null;
	  }
  break;
case 694:
#line 4695 "cs-parser.jay"
  { yyVal = yyVals[-1+yyTop]; }
  break;
case 695:
#line 4704 "cs-parser.jay"
  {
		Expression expr = (Expression) yyVals[0+yyTop];
		ExpressionStatement s = expr as ExpressionStatement;
		if (s == null) {
			expr.Error_InvalidExpressionStatement ();
			yyVal = null;
		}
		yyVal = new StatementExpression (s);
	  }
  break;
case 696:
#line 4714 "cs-parser.jay"
  {
		Report.Error (1002, GetLocation (yyVals[0+yyTop]), "Expecting `;'");
		yyVal = null;
	  }
  break;
case 699:
#line 4728 "cs-parser.jay"
  { 
		Location l = (Location) yyVals[-4+yyTop];

		yyVal = new If ((Expression) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);

		/* FIXME: location for warning should be loc property of $5.*/
		if (yyVals[0+yyTop] == EmptyStatement.Value)
			Report.Warning (642, 3, l, "Possible mistaken empty statement");

	  }
  break;
case 700:
#line 4740 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-6+yyTop];

		yyVal = new If ((Expression) yyVals[-4+yyTop], (Statement) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);

		/* FIXME: location for warning should be loc property of $5 and $7.*/
		if (yyVals[-2+yyTop] == EmptyStatement.Value)
			Report.Warning (642, 3, l, "Possible mistaken empty statement");
		if (yyVals[0+yyTop] == EmptyStatement.Value)
			Report.Warning (642, 3, l, "Possible mistaken empty statement");
	  }
  break;
case 701:
#line 4755 "cs-parser.jay"
  { 
		if (switch_stack == null)
			switch_stack = new Stack (2);
		switch_stack.Push (current_block);
	  }
  break;
case 702:
#line 4762 "cs-parser.jay"
  {
		yyVal = new Switch ((Expression) yyVals[-2+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]);
		current_block = (Block) switch_stack.Pop ();
	  }
  break;
case 703:
#line 4772 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 704:
#line 4779 "cs-parser.jay"
  {
	  	Report.Warning (1522, 1, lexer.Location, "Empty switch block"); 
		yyVal = new ArrayList ();
	  }
  break;
case 706:
#line 4788 "cs-parser.jay"
  {
		ArrayList sections = new ArrayList (4);

		sections.Add (yyVals[0+yyTop]);
		yyVal = sections;
	  }
  break;
case 707:
#line 4795 "cs-parser.jay"
  {
		ArrayList sections = (ArrayList) yyVals[-1+yyTop];

		sections.Add (yyVals[0+yyTop]);
		yyVal = sections;
	  }
  break;
case 708:
#line 4805 "cs-parser.jay"
  {
		current_block = current_block.CreateSwitchBlock (lexer.Location);
	  }
  break;
case 709:
#line 4809 "cs-parser.jay"
  {
		yyVal = new SwitchSection ((ArrayList) yyVals[-2+yyTop], current_block.Explicit);
	  }
  break;
case 710:
#line 4816 "cs-parser.jay"
  {
		ArrayList labels = new ArrayList (4);

		labels.Add (yyVals[0+yyTop]);
		yyVal = labels;
	  }
  break;
case 711:
#line 4823 "cs-parser.jay"
  {
		ArrayList labels = (ArrayList) (yyVals[-1+yyTop]);
		labels.Add (yyVals[0+yyTop]);

		yyVal = labels;
	  }
  break;
case 712:
#line 4832 "cs-parser.jay"
  { yyVal = new SwitchLabel ((Expression) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]); }
  break;
case 713:
#line 4833 "cs-parser.jay"
  { yyVal = new SwitchLabel (null, (Location) yyVals[0+yyTop]); }
  break;
case 714:
#line 4834 "cs-parser.jay"
  {
		Report.Error (
			1523, GetLocation (yyVals[0+yyTop]), 
			"The keyword case or default must precede code in switch block");
	  }
  break;
case 719:
#line 4850 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-4+yyTop];
		yyVal = new While ((Expression) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);
	  }
  break;
case 720:
#line 4859 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-6+yyTop];

		yyVal = new Do ((Statement) yyVals[-5+yyTop], (Expression) yyVals[-2+yyTop], l);
	  }
  break;
case 721:
#line 4869 "cs-parser.jay"
  {
		Location l = lexer.Location;
		start_block (l);  
		Block assign_block = current_block;

		if (yyVals[-1+yyTop] is DictionaryEntry){
			DictionaryEntry de = (DictionaryEntry) yyVals[-1+yyTop];
			
			Expression type = (Expression) de.Key;
			ArrayList var_declarators = (ArrayList) de.Value;

			foreach (VariableDeclaration decl in var_declarators){

				LocalInfo vi;

				vi = current_block.AddVariable (type, decl.identifier, decl.Location);
				if (vi == null)
					continue;

				Expression expr = decl.expression_or_array_initializer;
					
				LocalVariableReference var;
				var = new LocalVariableReference (assign_block, decl.identifier, l);

				if (expr != null) {
					Assign a = new Assign (var, expr, decl.Location);
					
					assign_block.AddStatement (new StatementExpression (a));
				}
			}
			
			/* Note: the $$ below refers to the value of this code block, not of the LHS non-terminal.*/
			/* This can be referred to as $5 below.*/
			yyVal = null;
		} else {
			yyVal = yyVals[-1+yyTop];
		}
	  }
  break;
case 722:
#line 4910 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-9+yyTop];

		For f = new For ((Statement) yyVals[-5+yyTop], (Expression) yyVals[-4+yyTop], (Statement) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);

		current_block.AddStatement (f);

		yyVal = end_block (lexer.Location);
	  }
  break;
case 723:
#line 4922 "cs-parser.jay"
  { yyVal = EmptyStatement.Value; }
  break;
case 727:
#line 4932 "cs-parser.jay"
  { yyVal = null; }
  break;
case 729:
#line 4937 "cs-parser.jay"
  { yyVal = EmptyStatement.Value; }
  break;
case 732:
#line 4947 "cs-parser.jay"
  {
		/* CHANGE: was `null'*/
		Statement s = (Statement) yyVals[0+yyTop];
		Block b = new Block (current_block, s.loc, lexer.Location);   

		b.AddStatement (s);
		yyVal = b;
	  }
  break;
case 733:
#line 4956 "cs-parser.jay"
  {
		Block b = (Block) yyVals[-2+yyTop];

		b.AddStatement ((Statement) yyVals[0+yyTop]);
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 734:
#line 4966 "cs-parser.jay"
  {
		Report.Error (230, (Location) yyVals[-5+yyTop], "Type and identifier are both required in a foreach statement");
		yyVal = null;
	  }
  break;
case 735:
#line 4972 "cs-parser.jay"
  {
		start_block (lexer.Location);
		Block foreach_block = current_block;

		LocatedToken lt = (LocatedToken) yyVals[-3+yyTop];
		Location l = lt.Location;
		LocalInfo vi = foreach_block.AddVariable ((Expression) yyVals[-4+yyTop], lt.Value, l);
		if (vi != null) {
			vi.SetReadOnlyContext (LocalInfo.ReadOnlyContext.Foreach);

			/* Get a writable reference to this read-only variable.*/
			/**/
			/* Note that the $$ here refers to the value of _this_ code block,*/
			/* not the value of the LHS non-terminal.  This can be referred to as $8 below.*/
			yyVal = new LocalVariableReference (foreach_block, lt.Value, l, vi, false);
		} else {
			yyVal = null;
		}
	  }
  break;
case 736:
#line 4992 "cs-parser.jay"
  {
		LocalVariableReference v = (LocalVariableReference) yyVals[-1+yyTop];
		Location l = (Location) yyVals[-8+yyTop];

		if (v != null) {
			Foreach f = new Foreach ((Expression) yyVals[-6+yyTop], v, (Expression) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], l);
			current_block.AddStatement (f);
		}

		yyVal = end_block (lexer.Location);
	  }
  break;
case 743:
#line 5016 "cs-parser.jay"
  {
		yyVal = new Break ((Location) yyVals[-1+yyTop]);
	  }
  break;
case 744:
#line 5023 "cs-parser.jay"
  {
		yyVal = new Continue ((Location) yyVals[-1+yyTop]);
	  }
  break;
case 745:
#line 5030 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new Goto (lt.Value, lt.Location);
	  }
  break;
case 746:
#line 5035 "cs-parser.jay"
  {
		yyVal = new GotoCase ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 747:
#line 5039 "cs-parser.jay"
  {
		yyVal = new GotoDefault ((Location) yyVals[-2+yyTop]);
	  }
  break;
case 748:
#line 5046 "cs-parser.jay"
  {
		yyVal = new Return ((Expression) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 749:
#line 5053 "cs-parser.jay"
  {
		yyVal = new Throw ((Expression) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 750:
#line 5060 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-3+yyTop];
		string s = lt.Value;
		if (s != "yield"){
			Report.Error (1003, lt.Location, "; expected");
			yyVal = null;
		}
		if (RootContext.Version == LanguageVersion.ISO_1){
			Report.FeatureIsNotAvailable (lt.Location, "yield statement");
			yyVal = null;
		}
		if (anonymous_host == null){
			Report.Error (204, lt.Location, "yield statement can only be used within a method, operator or property");
			yyVal = null;
		} else {
			anonymous_host.SetYields ();
			yyVal = new Yield ((Expression) yyVals[-1+yyTop], lt.Location); 
		}
	  }
  break;
case 751:
#line 5080 "cs-parser.jay"
  {
		Report.Error (1627, (Location) yyVals[-1+yyTop], "Expression expected after yield return");
		yyVal = null;
	  }
  break;
case 752:
#line 5085 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		string s = lt.Value;
		if (s != "yield"){
			Report.Error (1003, lt.Location, "; expected");
			yyVal = null;
		}
		if (RootContext.Version == LanguageVersion.ISO_1){
			Report.FeatureIsNotAvailable (lt.Location, "yield statement");
			yyVal = null;
		}
		if (anonymous_host == null){
			Report.Error (204, lt.Location, "yield statement can only be used within a method, operator or property");
			yyVal = null;
		} else {
			anonymous_host.SetYields ();
			yyVal = new YieldBreak (lt.Location);
		}
	  }
  break;
case 755:
#line 5113 "cs-parser.jay"
  {
		yyVal = new TryCatch ((Block) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-2+yyTop], false);
	  }
  break;
case 756:
#line 5117 "cs-parser.jay"
  {
		yyVal = new TryFinally ((Statement) yyVals[-2+yyTop], (Block) yyVals[0+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 757:
#line 5121 "cs-parser.jay"
  {
		yyVal = new TryFinally (new TryCatch ((Block) yyVals[-3+yyTop], (ArrayList) yyVals[-2+yyTop], (Location) yyVals[-4+yyTop], true), (Block) yyVals[0+yyTop], (Location) yyVals[-4+yyTop]);
	  }
  break;
case 758:
#line 5125 "cs-parser.jay"
  {
		Report.Error (1524, (Location) yyVals[-2+yyTop], "Expected catch or finally");
		yyVal = null;
	  }
  break;
case 759:
#line 5133 "cs-parser.jay"
  {
		ArrayList l = new ArrayList (4);

		l.Add (yyVals[0+yyTop]);
		yyVal = l;
	  }
  break;
case 760:
#line 5140 "cs-parser.jay"
  {
		ArrayList l = (ArrayList) yyVals[-1+yyTop];

		l.Add (yyVals[0+yyTop]);
		yyVal = l;
	  }
  break;
case 761:
#line 5149 "cs-parser.jay"
  { yyVal = null; }
  break;
case 763:
#line 5155 "cs-parser.jay"
  {
		Expression type = null;
		
		if (yyVals[0+yyTop] != null) {
			DictionaryEntry cc = (DictionaryEntry) yyVals[0+yyTop];
			type = (Expression) cc.Key;
			LocatedToken lt = (LocatedToken) cc.Value;

			if (lt != null){
				ArrayList one = new ArrayList (4);

				one.Add (new VariableDeclaration (lt, null));

				start_block (lexer.Location);
				current_block = declare_local_variables (type, one, lt.Location);
			}
		}
	  }
  break;
case 764:
#line 5172 "cs-parser.jay"
  {
		Expression type = null;
		string id = null;
		Block var_block = null;

		if (yyVals[-2+yyTop] != null){
			DictionaryEntry cc = (DictionaryEntry) yyVals[-2+yyTop];
			type = (Expression) cc.Key;
			LocatedToken lt = (LocatedToken) cc.Value;

			if (lt != null){
				id = lt.Value;
				var_block = end_block (lexer.Location);
			}
		}

		yyVal = new Catch (type, id, (Block) yyVals[0+yyTop], var_block, ((Block) yyVals[0+yyTop]).loc);
	  }
  break;
case 765:
#line 5193 "cs-parser.jay"
  { yyVal = null; }
  break;
case 767:
#line 5199 "cs-parser.jay"
  {
		yyVal = new DictionaryEntry (yyVals[-2+yyTop], yyVals[-1+yyTop]);
	  }
  break;
case 768:
#line 5207 "cs-parser.jay"
  {
		yyVal = new Checked ((Block) yyVals[0+yyTop]);
	  }
  break;
case 769:
#line 5214 "cs-parser.jay"
  {
		yyVal = new Unchecked ((Block) yyVals[0+yyTop]);
	  }
  break;
case 770:
#line 5221 "cs-parser.jay"
  {
		RootContext.CheckUnsafeOption ((Location) yyVals[0+yyTop]);
	  }
  break;
case 771:
#line 5223 "cs-parser.jay"
  {
		yyVal = new Unsafe ((Block) yyVals[0+yyTop]);
	  }
  break;
case 772:
#line 5232 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-1+yyTop];
		Expression type = (Expression) yyVals[-2+yyTop];
		Location l = (Location) yyVals[-4+yyTop];
		int top = list.Count;

		start_block (lexer.Location);

		for (int i = 0; i < top; i++){
			Pair p = (Pair) list [i];
			LocalInfo v;

			v = current_block.AddVariable (type, (string) p.First, l);
			if (v == null)
				continue;

			v.SetReadOnlyContext (LocalInfo.ReadOnlyContext.Fixed);
			v.Pinned = true;
			p.First = v;
			list [i] = p;
		}
	  }
  break;
case 773:
#line 5255 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-6+yyTop];

		Fixed f = new Fixed ((Expression) yyVals[-4+yyTop], (ArrayList) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], l);

		current_block.AddStatement (f);

		yyVal = end_block (lexer.Location);
	  }
  break;
case 774:
#line 5267 "cs-parser.jay"
  { 
	   	ArrayList declarators = new ArrayList (4);
	   	if (yyVals[0+yyTop] != null)
			declarators.Add (yyVals[0+yyTop]);
		yyVal = declarators;
	  }
  break;
case 775:
#line 5274 "cs-parser.jay"
  {
		ArrayList declarators = (ArrayList) yyVals[-2+yyTop];
		if (yyVals[0+yyTop] != null)
			declarators.Add (yyVals[0+yyTop]);
		yyVal = declarators;
	  }
  break;
case 776:
#line 5284 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		/* FIXME: keep location*/
		yyVal = new Pair (lt.Value, yyVals[0+yyTop]);
	  }
  break;
case 777:
#line 5290 "cs-parser.jay"
  {
		Report.Error (210, ((LocatedToken) yyVals[0+yyTop]).Location, "You must provide an initializer in a fixed or using statement declaration");
		yyVal = null;
	  }
  break;
case 778:
#line 5298 "cs-parser.jay"
  {
		/**/
 	  }
  break;
case 779:
#line 5302 "cs-parser.jay"
  {
		yyVal = new Lock ((Expression) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]);
	  }
  break;
case 780:
#line 5309 "cs-parser.jay"
  {
		start_block (lexer.Location);
		Block assign_block = current_block;

		DictionaryEntry de = (DictionaryEntry) yyVals[-1+yyTop];
		Location l = (Location) yyVals[-3+yyTop];

		Expression type = (Expression) de.Key;
		ArrayList var_declarators = (ArrayList) de.Value;

		Stack vars = new Stack ();

		foreach (VariableDeclaration decl in var_declarators) {
			LocalInfo vi = current_block.AddVariable (type, decl.identifier, decl.Location);
			if (vi == null)
				continue;
			vi.SetReadOnlyContext (LocalInfo.ReadOnlyContext.Using);

			Expression expr = decl.expression_or_array_initializer;
			if (expr == null) {
				Report.Error (210, l, "You must provide an initializer in a fixed or using statement declaration");
				continue;
			}
			LocalVariableReference var;

			/* Get a writable reference to this read-only variable.*/
			var = new LocalVariableReference (assign_block, decl.identifier, l, vi, false);

			/* This is so that it is not a warning on using variables*/
			vi.Used = true;

			vars.Push (new DictionaryEntry (var, expr));

			/* Assign a = new Assign (var, expr, decl.Location);*/
			/* assign_block.AddStatement (new StatementExpression (a));*/
		}

		/* Note: the $$ here refers to the value of this code block and not of the LHS non-terminal.*/
		/* It can be referred to as $5 below.*/
		yyVal = vars;
	  }
  break;
case 781:
#line 5351 "cs-parser.jay"
  {
		Statement stmt = (Statement) yyVals[0+yyTop];
		Stack vars = (Stack) yyVals[-1+yyTop];
		Location l = (Location) yyVals[-5+yyTop];

		while (vars.Count > 0) {
			  DictionaryEntry de = (DictionaryEntry) vars.Pop ();
			  stmt = new Using ((Expression) de.Key, (Expression) de.Value, stmt, l);
		}
		current_block.AddStatement (stmt);
		yyVal = end_block (lexer.Location);
	  }
  break;
case 782:
#line 5364 "cs-parser.jay"
  {
		start_block (lexer.Location);
	  }
  break;
case 783:
#line 5368 "cs-parser.jay"
  {
		current_block.AddStatement (new UsingTemporary ((Expression) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]));
		yyVal = end_block (lexer.Location);
	  }
  break;
case 784:
#line 5379 "cs-parser.jay"
  {
		++lexer.query_parsing;
	  }
  break;
case 785:
#line 5383 "cs-parser.jay"
  {
		if (--lexer.query_parsing == 1)
			lexer.query_parsing = 0;
			
		Linq.AQueryClause from = yyVals[-2+yyTop] as Linq.AQueryClause;
			
		from.Tail.Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = from;
		
		current_block.SetEndLocation (lexer.Location);
		current_block = current_block.Parent;
	  }
  break;
case 786:
#line 5399 "cs-parser.jay"
  {
		current_block = new Linq.QueryBlock (current_block, GetLocation (yyVals[-3+yyTop]));
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance, lt.Value, lt.Location);
		yyVal = new Linq.QueryExpression (lt, new Linq.QueryStartClause ((Expression)yyVals[0+yyTop]));
	  }
  break;
case 787:
#line 5407 "cs-parser.jay"
  {
		current_block = new Linq.QueryBlock (current_block, GetLocation (yyVals[-4+yyTop]));
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];

		Expression type = (Expression)yyVals[-3+yyTop];
		current_block.AddVariable (type, lt.Value, lt.Location);
		yyVal = new Linq.QueryExpression (lt, new Linq.Cast (type, (Expression)yyVals[0+yyTop]));
	  }
  break;
case 788:
#line 5419 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance,
			lt.Value, lt.Location);
			
		yyVal = new Linq.SelectMany (lt, (Expression)yyVals[0+yyTop]);			
	  }
  break;
case 789:
#line 5428 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];

		Expression type = (Expression)yyVals[-3+yyTop];
		current_block.AddVariable (type, lt.Value, lt.Location);
		yyVal = new Linq.SelectMany (lt, new Linq.Cast (type, (Expression)yyVals[0+yyTop]));
	  }
  break;
case 790:
#line 5439 "cs-parser.jay"
  {
	  	Linq.AQueryClause head = (Linq.AQueryClause)yyVals[-1+yyTop];
		
		if (yyVals[0+yyTop] != null)
			head.Next = (Linq.AQueryClause)yyVals[0+yyTop];
				
		if (yyVals[-2+yyTop] != null) {
			Linq.AQueryClause clause = (Linq.AQueryClause)yyVals[-2+yyTop];
			clause.Tail.Next = head;
			head = clause;
		}
		
		yyVal = head;
	  }
  break;
case 791:
#line 5457 "cs-parser.jay"
  {
		yyVal = new Linq.Select ((Expression)yyVals[0+yyTop], GetLocation (yyVals[-1+yyTop]));
	  }
  break;
case 792:
#line 5461 "cs-parser.jay"
  {
	    yyVal = new Linq.GroupBy ((Expression)yyVals[-2+yyTop], (Expression)yyVals[0+yyTop], GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 796:
#line 5474 "cs-parser.jay"
  {
		((Linq.AQueryClause)yyVals[-1+yyTop]).Tail.Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 802:
#line 5490 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance,
			lt.Value, lt.Location);	  
	  	yyVal = new Linq.Let (lt, (Expression)yyVals[0+yyTop], GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 803:
#line 5500 "cs-parser.jay"
  {
		yyVal = new Linq.Where ((Expression)yyVals[0+yyTop], GetLocation (yyVals[-1+yyTop]));
	  }
  break;
case 804:
#line 5507 "cs-parser.jay"
  {
		Location loc = GetLocation (yyVals[-8+yyTop]);
		LocatedToken lt = (LocatedToken) yyVals[-7+yyTop];
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance,
			lt.Value, lt.Location);
		
		if (yyVals[0+yyTop] == null) {
	  		yyVal = new Linq.Join (lt, (Expression)yyVals[-5+yyTop], (Expression)yyVals[-3+yyTop],
				(Expression)yyVals[-1+yyTop], loc);
		} else {
			LocatedToken lt_into = (LocatedToken) yyVals[0+yyTop];
			yyVal = new Linq.GroupJoin (lt, (Expression)yyVals[-5+yyTop], (Expression)yyVals[-3+yyTop],
				(Expression)yyVals[-1+yyTop], lt_into, loc);
		}
	  }
  break;
case 805:
#line 5523 "cs-parser.jay"
  {
		Location loc = GetLocation (yyVals[-9+yyTop]);
		LocatedToken lt = (LocatedToken) yyVals[-7+yyTop];
		current_block.AddVariable ((Expression)yyVals[-8+yyTop], lt.Value, lt.Location);
		
		Linq.Cast cast = new Linq.Cast ((Expression)yyVals[-8+yyTop], (Expression)yyVals[-5+yyTop]);
		if (yyVals[0+yyTop] == null) {
		  	yyVal = new Linq.Join (lt, cast, (Expression)yyVals[-3+yyTop],
				(Expression)yyVals[-1+yyTop], loc);
		} else {
			LocatedToken lt_into = (LocatedToken) yyVals[0+yyTop];
			yyVal = new Linq.GroupJoin (lt, cast, (Expression)yyVals[-3+yyTop],
				(Expression)yyVals[-1+yyTop], lt_into, loc);
		}
	  }
  break;
case 807:
#line 5543 "cs-parser.jay"
  {
	  	yyVal = yyVals[0+yyTop];
	  }
  break;
case 808:
#line 5550 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 810:
#line 5558 "cs-parser.jay"
  {
		((Linq.AQueryClause)yyVals[-2+yyTop]).Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 811:
#line 5566 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 812:
#line 5570 "cs-parser.jay"
  {
		((Linq.AQueryClause)yyVals[-2+yyTop]).Tail.Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 813:
#line 5578 "cs-parser.jay"
  {
		yyVal = new Linq.OrderByAscending ((Expression)yyVals[0+yyTop]);	
	  }
  break;
case 814:
#line 5582 "cs-parser.jay"
  {
		yyVal = new Linq.OrderByAscending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 815:
#line 5586 "cs-parser.jay"
  {
		yyVal = new Linq.OrderByDescending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 816:
#line 5593 "cs-parser.jay"
  {
		yyVal = new Linq.ThenByAscending ((Expression)yyVals[0+yyTop]);	
	  }
  break;
case 817:
#line 5597 "cs-parser.jay"
  {
		yyVal = new Linq.ThenByAscending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 818:
#line 5601 "cs-parser.jay"
  {
		yyVal = new Linq.ThenByDescending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 820:
#line 5610 "cs-parser.jay"
  {
		/* query continuation block is not linked with query block but with block*/
		/* before. This means each query can use same range variable names for*/
		/* different identifiers.*/

		current_block.SetEndLocation (GetLocation (yyVals[-1+yyTop]));
		current_block = current_block.Parent;
		current_block = new Linq.QueryBlock (current_block, GetLocation (yyVals[-1+yyTop]));
		
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance,
			lt.Value, lt.Location);
	  }
  break;
case 821:
#line 5624 "cs-parser.jay"
  {
  		yyVal = new Linq.QueryExpression ((LocatedToken) yyVals[-2+yyTop],
  			(Linq.AQueryClause)yyVals[0+yyTop]);
	  }
  break;
#line default
        }
        yyTop -= yyLen[yyN];
        yyState = yyStates[yyTop];
        int yyM = yyLhs[yyN];
        if (yyState == 0 && yyM == 0) {
          if (debug != null) debug.shift(0, yyFinal);
          yyState = yyFinal;
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
            if (debug != null)
               debug.lex(yyState, yyToken,yyname(yyToken), yyLex.value());
          }
          if (yyToken == 0) {
            if (debug != null) debug.accept(yyVal);
            return yyVal;
          }
          goto continue_yyLoop;
        }
        if (((yyN = yyGindex[yyM]) != 0) && ((yyN += yyState) >= 0)
            && (yyN < yyTable.Length) && (yyCheck[yyN] == yyState))
          yyState = yyTable[yyN];
        else
          yyState = yyDgoto[yyM];
        if (debug != null) debug.shift(yyStates[yyTop], yyState);
	 goto continue_yyLoop;
      continue_yyDiscarded: continue;	// implements the named-loop continue: 'continue yyDiscarded'
      }
    continue_yyLoop: continue;		// implements the named-loop continue: 'continue yyLoop'
    }
  }

   static  short [] yyLhs  = {              -1,
    0,    0,    0,    0,    2,    2,    1,    1,    4,    4,
    4,    8,    8,    5,    9,    9,    6,    6,   10,   10,
   11,   17,   14,   18,   18,   19,   19,   13,   21,   16,
   20,   25,   20,   20,   23,   23,   22,   22,   24,   24,
   26,   26,    7,    7,    7,    7,   27,   27,   27,   27,
   27,    3,   15,   15,   35,   35,   36,   36,   37,   39,
   39,   39,   39,   38,   38,   40,   41,   42,   42,   43,
   43,   43,   44,   44,   45,   45,   46,   46,   46,   48,
   49,   50,   50,   51,   51,   52,   52,   52,   52,   52,
   52,   52,   52,   52,   52,   63,   65,   68,   69,   31,
   31,   71,   67,   70,   70,   72,   72,   73,   73,   73,
   73,   73,   73,   73,   73,   73,   73,   53,   75,   75,
   76,   76,   28,   28,   28,   28,   79,   79,   80,   80,
   78,   78,   81,   81,   81,   82,   82,   82,   82,   82,
   87,   29,   88,   88,   90,   90,   93,   94,   85,   95,
   96,   85,   97,   85,   85,   86,   86,   92,   92,  100,
  101,  100,   99,   99,   99,   99,   99,   99,   99,   99,
   99,  102,  102,  105,  105,  105,  105,  105,  106,  106,
  107,  107,  108,  108,  108,  103,  103,  109,  109,  109,
  104,  110,  112,  113,   54,  111,  111,  111,  111,  111,
  117,  114,  118,  115,  116,  116,  119,  120,  122,  123,
   32,   32,  121,  124,  124,  125,  125,  126,  126,  126,
  126,  126,  126,  126,  126,  126,  126,  131,  134,  132,
  132,  135,  136,  127,  137,  138,  127,  139,  140,  128,
  128,  129,  129,  129,  142,  143,  129,  144,  145,  130,
  148,   57,  147,  147,  150,  146,  146,  149,  149,  149,
  149,  149,  149,  149,  149,  149,  149,  149,  149,  149,
  149,  149,  149,  149,  149,  149,  149,  149,  149,  152,
  151,  153,  151,  151,  151,   58,  154,  154,  158,  156,
  155,  155,  157,  157,  157,  161,  161,  161,  162,   59,
   55,  163,  164,   55,   55,  141,  141,  141,  141,  141,
  141,  167,  165,  165,  165,  168,  166,  166,  166,  170,
  171,   56,  169,  169,  174,   33,  172,  172,  176,  177,
  173,  175,  175,  178,  178,  179,  180,  179,  181,  182,
   34,  183,  183,   12,   12,   12,   91,   91,   62,  184,
  184,  185,  185,  186,  186,  187,  187,   74,   74,   74,
   74,  190,  190,  191,  191,  191,  191,  191,  191,  195,
  195,  196,  188,  188,  188,  188,  188,  188,  188,  197,
  197,  197,  197,  197,  197,  197,  197,  197,  197,  189,
  198,  198,  198,  198,  198,  198,  198,  198,  198,  198,
  198,  198,  198,  198,  198,  198,  198,  198,  198,  199,
  199,  199,  199,  199,  199,  218,  218,  218,  217,  216,
  216,  219,  219,  200,  200,  200,  202,  202,  220,  203,
  203,  203,  203,  203,  224,  224,  225,  225,  226,  226,
  227,  227,  228,  228,  228,  228,  229,  229,  160,  160,
  222,  222,  222,  223,  223,  221,  221,  221,  221,  221,
  232,  204,  204,  231,  231,  205,  206,  206,  206,  207,
  208,  209,  209,  209,  233,  233,  234,  234,  234,  234,
  234,  235,  238,  238,  238,  239,  239,  239,  239,  236,
  236,  240,  240,  240,  240,  193,  192,  241,  241,  242,
  242,  237,  237,   84,   84,  243,  243,  244,  210,  245,
  245,  246,  246,  246,  246,  211,  212,  213,  214,  248,
  215,  247,  247,  250,  249,  201,  251,  251,  251,  251,
  254,  254,  254,  253,  253,  252,  252,  252,  252,  252,
  252,  252,  194,  194,  194,  194,  255,  255,  255,  256,
  256,  256,  257,  257,  258,  259,  259,  259,  259,  259,
  260,  259,  261,  259,  262,  262,  262,  263,  263,  264,
  264,  265,  265,  266,  266,  267,  267,  268,  268,  268,
  268,  269,  269,  269,  269,  269,  269,  269,  269,  269,
  269,  269,  270,  270,  271,  271,  271,  272,  272,  274,
  273,  273,  276,  275,  277,  275,   47,   47,  230,  230,
  230,   77,  279,  280,  281,  282,  283,   30,   61,   61,
   60,   60,   89,   89,  284,  284,  284,  284,  284,  284,
  284,  284,  284,  284,  284,  284,  284,  284,   64,   64,
  285,   66,   66,  286,  286,  287,  288,  288,  289,  289,
  289,  289,  290,   98,  291,  159,  133,  133,  292,  292,
  293,  293,  293,  295,  295,  295,  295,  295,  295,  295,
  295,  295,  295,  295,  295,  295,  309,  309,  309,  297,
  310,  296,  294,  294,  313,  313,  314,  314,  314,  314,
  311,  311,  312,  298,  315,  315,  299,  299,  316,  316,
  318,  317,  319,  320,  320,  321,  321,  324,  322,  323,
  323,  325,  325,  325,  300,  300,  300,  300,  326,  327,
  332,  328,  330,  330,  334,  334,  331,  331,  333,  333,
  336,  335,  335,  329,  337,  329,  301,  301,  301,  301,
  301,  301,  338,  339,  340,  340,  340,  341,  342,  343,
  343,  343,   83,   83,  302,  302,  302,  302,  344,  344,
  346,  346,  348,  345,  347,  347,  349,  303,  304,  350,
  307,  352,  308,  351,  351,  353,  353,  354,  305,  355,
  306,  356,  306,  359,  278,  357,  357,  360,  360,  358,
  362,  362,  361,  361,  364,  364,  365,  365,  365,  365,
  365,  366,  367,  368,  368,  370,  370,  369,  371,  371,
  373,  373,  372,  372,  372,  374,  374,  374,  363,  375,
  363,
  };
   static  short [] yyLen = {           2,
    2,    3,    2,    1,    0,    1,    1,    2,    1,    1,
    1,    1,    2,    4,    1,    2,    1,    1,    5,    2,
    3,    0,    6,    0,    1,    0,    1,    1,    0,    3,
    4,    0,    3,    4,    0,    1,    0,    1,    0,    1,
    1,    2,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    0,    1,    1,    2,    5,    4,    2,    1,
    1,    1,    1,    1,    3,    2,    1,    0,    3,    1,
    3,    1,    0,    1,    1,    3,    1,    3,    3,    3,
    3,    0,    1,    1,    2,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    0,    0,    0,    0,   13,
    5,    0,    4,    0,    1,    1,    2,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    6,    1,    3,
    3,    1,    5,    6,    5,    5,    1,    3,    4,    3,
    1,    3,    3,    1,    4,    1,    1,    5,    1,    2,
    0,    3,    0,    1,    1,    1,    0,    0,   10,    0,
    0,   10,    0,   10,    8,    1,    1,    0,    1,    0,
    0,    2,    1,    3,    3,    3,    5,    3,    5,    1,
    1,    1,    3,    4,    6,    3,    4,    6,    0,    1,
    1,    2,    1,    1,    1,    4,    4,    1,    2,    2,
    1,    0,    0,    0,   10,    1,    2,    1,    2,    1,
    0,    5,    0,    5,    1,    1,    0,    0,    0,    0,
   13,    5,    3,    0,    1,    1,    2,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    0,    4,
    1,    0,    0,   11,    0,    0,   11,    0,    0,    9,
    4,    6,    5,    6,    0,    0,   10,    0,    0,   12,
    0,    5,    1,    1,    0,    7,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    0,
    7,    0,    7,    2,    2,    4,    1,    2,    0,    5,
    1,    1,    5,    5,    2,    0,    1,    1,    0,    8,
    6,    0,    0,   10,    6,    2,    2,    1,    1,    1,
    0,    0,    4,    3,    3,    0,    4,    3,    3,    0,
    0,    8,    5,    7,    0,    8,    0,    2,    0,    0,
    5,    0,    2,    1,    3,    2,    0,    5,    0,    0,
   12,    0,    1,    2,    4,    4,    2,    4,    2,    0,
    3,    0,    3,    1,    3,    1,    2,    2,    2,    1,
    1,    2,    2,    2,    2,    2,    2,    2,    2,    1,
    3,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    3,
    1,    1,    4,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    3,    3,    2,    2,    2,    4,    4,    1,    4,
    4,    3,    5,    7,    0,    1,    3,    4,    0,    1,
    1,    3,    3,    1,    3,    2,    1,    1,    0,    1,
    1,    3,    2,    1,    1,    2,    2,    4,    3,    1,
    1,    4,    2,    1,    3,    1,    4,    4,    2,    2,
    2,    1,    1,    1,    6,    3,    7,    4,    3,    2,
    3,    4,    0,    1,    3,    3,    1,    1,    1,    0,
    1,    0,    1,    2,    3,    2,    3,    0,    1,    1,
    2,    0,    1,    2,    4,    1,    3,    0,    5,    1,
    1,    2,    4,    4,    4,    4,    4,    4,    3,    0,
    4,    0,    1,    0,    4,    3,    1,    2,    2,    1,
    3,    3,    3,    1,    4,    1,    2,    2,    2,    2,
    2,    2,    1,    3,    3,    3,    1,    3,    3,    1,
    3,    3,    0,    1,    2,    1,    3,    3,    3,    3,
    0,    4,    0,    4,    1,    3,    3,    1,    3,    1,
    3,    1,    3,    1,    3,    1,    3,    1,    5,    3,
    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,
    3,    3,    1,    3,    3,    2,    1,    0,    1,    0,
    2,    1,    0,    4,    0,    6,    1,    1,    1,    1,
    1,    1,    1,    0,    0,    0,    0,   13,    0,    1,
    0,    1,    1,    2,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    0,    1,
    2,    0,    1,    1,    2,    4,    1,    3,    1,    3,
    1,    1,    0,    4,    0,    4,    0,    1,    1,    2,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    0,    4,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    3,    3,    2,    1,    1,    1,    1,    5,    7,
    0,    6,    3,    0,    1,    1,    2,    0,    3,    1,
    2,    3,    1,    1,    1,    1,    1,    1,    5,    7,
    0,   10,    0,    1,    1,    1,    0,    1,    0,    1,
    1,    1,    3,    6,    0,    9,    1,    1,    1,    1,
    1,    1,    2,    2,    3,    4,    3,    3,    3,    4,
    3,    3,    0,    1,    3,    4,    5,    3,    1,    2,
    0,    1,    0,    4,    0,    1,    4,    2,    2,    0,
    3,    0,    7,    1,    3,    3,    1,    0,    6,    0,
    6,    0,    6,    0,    3,    4,    5,    4,    5,    3,
    2,    4,    0,    1,    1,    2,    1,    1,    1,    1,
    1,    4,    2,    9,   10,    0,    2,    2,    1,    3,
    1,    3,    1,    2,    2,    1,    2,    2,    0,    0,
    4,
  };
   static  short [] yyDefRed = {            0,
    6,    0,    0,    0,    0,    0,    4,    0,    7,    9,
   10,   11,   17,   18,   44,    0,   43,   45,   46,   47,
   48,   49,   50,   51,    0,   55,  141,    0,   20,    0,
    0,    0,   63,   61,   62,    0,    0,    0,    0,    0,
   64,    0,    1,    0,    8,    3,  630,  636,  628,    0,
  625,  635,  629,  627,  626,  633,  631,  632,  638,  634,
  637,    0,    0,  623,   56,    0,    0,    0,    0,    0,
  344,    0,   21,    0,    0,    0,    0,   59,    0,   66,
    2,    0,  375,  381,  388,  376,    0,  378,    0,    0,
  377,  384,  386,  373,  380,  382,  374,  385,  387,  383,
    0,    0,    0,    0,    0,    0,  360,  361,  379,  624,
  653,  157,  142,  156,   14,    0,    0,    0,    0,    0,
  354,    0,    0,    0,   65,   58,    0,    0,    0,  421,
    0,  415,    0,  466,  420,  508,    0,  389,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  419,  416,
  417,  418,  413,  414,    0,    0,    0,    0,   70,    0,
    0,   75,   77,  392,  429,    0,    0,  391,  394,  395,
  396,  397,  398,  399,  400,  401,  402,  403,  404,  405,
  406,  407,  408,  409,  410,  411,  412,    0,    0,  608,
  472,  473,  474,  536,    0,  530,  534,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  609,  607,  610,  611,
  784,    0,    0,    0,    0,  363,    0,    0,    0,  131,
    0,    0,  343,  358,  614,    0,    0,    0,  362,    0,
    0,    0,    0,    0,  359,    0,   19,    0,    0,  351,
  345,  346,   57,  469,    0,    0,    0,  145,  146,  524,
  520,  523,  480,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  529,  537,  538,
  528,  542,  541,  539,  540,  603,    0,    0,    0,  349,
  184,  183,  185,    0,    0,    0,    0,  593,    0,    0,
   69,    0,    0,    0,    0,    0,    0,    0,    0,  470,
  471,    0,  463,  425,    0,    0,    0,  426,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  563,  561,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   29,    0,    0,    0,    0,  325,  125,    0,    0,  127,
    0,    0,    0,  347,    0,    0,  126,  150,    0,    0,
    0,  212,    0,  101,    0,  500,    0,    0,  123,    0,
  147,  491,  496,  390,  696,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  770,    0,    0,    0,  680,    0,  695,  664,    0,
    0,    0,    0,  659,  661,  662,  663,  665,  666,  667,
  668,  669,  670,  671,  672,  673,  674,  675,  676,    0,
    0,    0,    0,    0,  697,  698,  715,  716,  717,  718,
  737,  738,  739,  740,  741,  742,  355,  464,    0,    0,
    0,    0,    0,  489,    0,    0,    0,    0,    0,    0,
  484,  481,    0,    0,    0,    0,  476,    0,  479,    0,
    0,    0,    0,    0,  423,  422,  368,  367,  364,    0,
  366,  365,    0,    0,   80,    0,    0,  596,    0,    0,
    0,  526,    0,   76,   79,   78,  544,  546,  545,    0,
    0,    0,    0,  454,    0,  455,    0,  451,    0,  519,
  531,  532,    0,    0,  533,    0,  582,  583,  584,  585,
  586,  587,  588,  589,  590,  592,  591,    0,  543,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  581,    0,    0,  580,    0,    0,
    0,    0,    0,  785,  797,    0,    0,  795,  798,  799,
  800,  801,    0,   25,   23,    0,    0,    0,    0,    0,
  124,  754,    0,    0,  139,  136,  133,  137,    0,    0,
    0,  132,    0,    0,  615,  208,   97,  497,  501,    0,
    0,  743,  768,    0,    0,    0,  744,  678,  677,  679,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  701,    0,    0,  769,    0,    0,  689,    0,    0,    0,
  681,  654,  688,    0,    0,  686,  687,  685,  660,  683,
  684,    0,  690,    0,  694,  468,    0,  467,  517,  191,
    0,    0,    0,  159,    0,    0,    0,  172,  521,    0,
    0,  424,    0,  482,    0,    0,    0,    0,    0,  441,
  444,    0,    0,  478,  504,  506,    0,  516,    0,    0,
    0,    0,    0,  518,  786,    0,  535,  602,  604,    0,
  353,  393,  595,  594,  605,  462,  461,  457,  456,    0,
  430,  453,    0,  427,  431,    0,    0,    0,  428,    0,
  564,  562,    0,  613,  803,    0,    0,    0,    0,    0,
    0,  808,    0,    0,    0,    0,  796,   32,   12,    0,
   30,    0,    0,  329,    0,  130,    0,  128,  135,    0,
    0,  348,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  119,    0,    0,  725,  732,    0,  724,    0,    0,
  612,    0,  747,  745,    0,    0,  748,    0,  749,  758,
    0,    0,    0,  759,  771,    0,    0,    0,  752,  751,
    0,    0,    0,    0,  465,    0,    0,    0,  181,    0,
  525,    0,    0,    0,  486,    0,  485,  446,    0,    0,
  437,    0,    0,    0,    0,    0,    0,  512,    0,  509,
    0,  787,  601,    0,  459,    0,  452,  433,    0,  554,
  555,  579,    0,    0,    0,    0,    0,  814,  815,    0,
  791,    0,    0,  790,    0,   13,   15,    0,    0,  339,
    0,  326,  129,    0,  151,  153,    0,    0,  640,    0,
    0,  155,  148,    0,    0,    0,    0,    0,  774,  721,
    0,    0,    0,  746,    0,  778,    0,    0,  763,  766,
  756,    0,  760,  782,  780,    0,  750,  682,  495,  189,
  190,    0,  182,    0,    0,    0,  165,  173,  166,  168,
    0,  445,  447,  448,  443,  438,  442,    0,  475,  436,
  507,  505,    0,    0,    0,  606,  458,    0,  788,    0,
    0,    0,  802,    0,    0,  811,    0,  820,   33,   16,
   41,    0,    0,    0,    0,  330,    0,  334,    0,    0,
    0,    0,    0,  370,    0,  616,    0,  644,  209,   98,
    0,  121,  120,    0,    0,  772,    0,    0,  733,    0,
    0,    0,    0,    0,    0,    0,  757,    0,    0,  719,
  177,    0,  187,  186,    0,    0,  503,  477,  513,  515,
  514,  434,  789,    0,    0,  817,  818,    0,  792,    0,
   34,   31,   42,  340,    0,    0,    0,  333,  138,  152,
  154,    0,    0,    0,  645,    0,    0,  149,    0,  776,
    0,  775,  728,    0,  734,    0,    0,  779,    0,  702,
  762,    0,  764,  783,  781,    0,    0,  169,  167,    0,
    0,  812,  821,    0,    0,  331,  335,  371,    0,    0,
  617,    0,  210,  102,   99,  720,  773,    0,  735,  700,
  714,    0,  713,    0,    0,  706,    0,  710,  767,  175,
  178,    0,    0,  341,    0,  651,    0,  652,    0,    0,
  647,    0,   95,   87,   88,    0,    0,   84,   86,   89,
   90,   91,   92,   93,   94,    0,    0,  223,  224,  226,
  225,  222,  227,    0,    0,  216,  218,  219,  220,  221,
    0,    0,    0,    0,    0,  730,    0,    0,  703,  707,
    0,  711,    0,    0,  338,    0,    0,    0,    0,    0,
    0,   81,   85,  618,    0,    0,  213,  217,  211,  116,
  109,  110,  108,  111,  112,  113,  114,  115,  117,    0,
    0,  106,  100,    0,  736,  712,    0,    0,  804,    0,
  650,  648,    0,    0,    0,    0,    0,    0,  251,  257,
    0,    0,    0,  299,  620,    0,    0,    0,  103,  107,
  722,  807,  805,    0,    0,  285,    0,  284,    0,    0,
    0,    0,    0,    0,  655,  292,  286,  291,    0,  288,
  320,    0,    0,    0,  241,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  263,  262,  259,  264,  265,
  258,  277,  276,  269,  270,  266,  268,  267,  271,  260,
  261,  272,  273,  279,  278,  274,  275,    0,    0,    0,
    0,  254,  253,  252,    0,  295,    0,    0,    0,    0,
  243,    0,    0,    0,  238,    0,  118,  305,  302,  301,
  282,  280,    0,  255,    0,    0,    0,  193,    0,    0,
    0,  200,    0,  321,    0,    0,    0,  245,  242,  244,
    0,    0,    0,    0,    0,    0,    0,  290,    0,  323,
  162,    0,    0,  656,    0,    0,    0,    0,  197,  199,
    0,    0,  235,    0,  239,  232,  310,    0,  303,    0,
    0,    0,    0,    0,    0,  194,  293,  294,  201,  203,
  322,  300,  246,    0,  248,    0,    0,    0,    0,    0,
    0,    0,  306,    0,  307,  283,  281,  256,  324,    0,
    0,    0,    0,  236,    0,  240,  233,  314,    0,  318,
    0,  315,  319,  304,    0,    0,  195,  206,  205,  202,
  204,  247,    0,  249,    0,  313,  317,  229,  231,  237,
    0,  234,    0,  250,    0,  230,
  };
  protected static  short [] yyDgoto  = {             5,
    6,    7,    8,    9,   10,   11,   12,  710,  818,   13,
   14,  103,   32,   15,  631,  342,  212,  555,   77,  711,
  553,  712,  819,  902,  815,  903,   17,   18,   19,   20,
   21,   22,   23,   24,  632,   26,   38,   39,   40,   41,
   42,   80,  158,  159,  160,  161,  398,  163, 1011, 1046,
 1047, 1048, 1049, 1050, 1051, 1052, 1053, 1054, 1055,   62,
  104,  164,  365,  828,  727,  916, 1015,  977, 1073, 1110,
 1072, 1111, 1112,  119,  731,  732,  742,  230,  349,  350,
  220,  567,  563,  568,   27,  113,   66,    0,   63,  250,
  232,  633,  581,  921,  573,  910,  911,  399,  634, 1225,
 1226,  635,  636,  637,  638,  767,  768,  286,  770, 1201,
 1234, 1253, 1300, 1235, 1236, 1320, 1301, 1302,  363,  726,
 1013,  976, 1071, 1064, 1065, 1066, 1067, 1068, 1069, 1070,
 1096, 1330,  400, 1333, 1287, 1325, 1284, 1323, 1243, 1286,
 1269, 1262, 1303, 1305, 1331, 1129, 1204, 1154, 1198, 1249,
 1130, 1247, 1246, 1131, 1157, 1132, 1160, 1150, 1158,  495,
 1091, 1162, 1245, 1291, 1270, 1271, 1309, 1311, 1133, 1209,
 1258,  346,  715,  558,  906,  821,  966,  907,  908, 1005,
  904, 1004,  615,   71,  280,  120,  121,  165,  107,  108,
  265,  233,  234,  166,  913,  914,  109,  167,  168,  169,
  170,  171,  172,  173,  174,  175,  176,  177,  178,  179,
  180,  181,  182,  183,  184,  185,  186,  187,  188,  189,
  496,  497,  498,  879,  457,  648,  649,  650,  875,  190,
  439,  678,  191,  192,  193,  373,  948,  450,  451,  616,
  367,  368,  657,  258,  662,  663,  251,  443,  252,  442,
  194,  195,  196,  197,  198,  199,  801,  691,  200,  524,
  523,  201,  202,  203,  204,  205,  206,  207,  208,  287,
  288,  289,  669,  670,  209,  474,  794,  210,  695,  361,
  725,  974, 1056,   64,  829,  917,  918, 1040, 1041,  236,
 1205,  403,  404,  588,  589,  590,  408,  409,  410,  411,
  412,  413,  414,  415,  416,  417,  418,  419,  591,  762,
  420,  421,  422,  423,  424,  425,  426,  748,  990, 1024,
 1025, 1026, 1027, 1081, 1028,  427,  428,  429,  430,  737,
  984,  928, 1074,  738,  739, 1076, 1077,  431,  432,  433,
  434,  435,  436,  753,  754,  992,  849,  936,  850,  605,
  838,  981,  839,  933,  939,  938,  211,  544,  340,  545,
  546,  706,  814,  547,  548,  549,  550,  551,  552, 1119,
  702,  703,  895,  896,  960,
  };
  protected static  short [] yySindex = {           83,
    0, -347, -206, -190,    0,   83,    0,   21,    0,    0,
    0,    0,    0,    0,    0,  981,    0,    0,    0,    0,
    0,    0,    0,    0,  -29,    0,    0, -317,    0,  536,
  -66,  -51,    0,    0,    0, -256,  -66,  -26,   -4,   32,
    0,   88,    0,   21,    0,    0,    0,    0,    0,  -26,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0, 8875,11603,    0,    0,  191,  120,  -26, 8907,   94,
    0,  139,    0, -256,   -4,  -26,  249,    0, 6301,    0,
    0,  -66,    0,    0,    0,    0, 5330,    0,  231, 5330,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 -281,  326,  216,  302,  911,  295,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  463,  401, 8907,  267, -243,
    0,  421,  421,  454,    0,    0, -116,  521,  -87,    0,
  739,    0,  530,    0,    0,    0,  556,    0, 5979, 6384,
 6789, 6789, 6789, 6789, 6789, 6789, 6789, 6789,    0,    0,
    0,    0,    0,    0,  271, 4520, 5330,  472,    0,  566,
  589,    0,    0,    0,    0,  674,  476,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  116,  540,    0,
    0,    0,    0,    0,  753,    0,    0,  362,  234,  -70,
  569,  598,  563,  608,  592,  221,    0,    0,    0,    0,
    0,  659, -277,  697, -192,    0, -245,  695,  605,    0,
  -87,  603,    0,    0,    0,  816,  819,  712,    0,  679,
  620,  -87,  720,  295,    0, 1909,    0,  267, 8907,    0,
    0,    0,    0,    0, 6384,  652, 6384,    0,    0,    0,
    0,    0,    0, 2236,  -84,  729, 5330,  736, 6384,  -33,
  -21,  229,  -61,  295,  335,  693,  110,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 6384, 8907,  669,    0,
    0,    0,    0, -256,  111, 5330,  763,    0,  761,  357,
    0, 6301, 6301, 6789, 6789, 6789, 5408, 5003,  702,    0,
    0,  703,    0,    0, 7002,  724, 7077,    0,  740, 6384,
 6384, 6384, 6384, 6384, 6384, 6384, 6384, 6384, 6384, 6384,
 6789, 6789, 6789, 6789,    0,    0, 6789, 6789, 6789, 6789,
 6789, 6789, 6789, 6789, 6789, 6789, 5491, 6789, 6384,  915,
    0,  821,  804,  -87, 5330,    0,    0,  823,  711,    0,
 6384, 5086, 8907,    0,  752,  755,    0,    0,  460,  -87,
  764,    0,  764,    0,  764,    0,  853,  836,    0,  -87,
    0,    0,    0,    0,    0,  852,  158, 7207,  861, 1909,
  -87,  -87,  -87, -209,  858,  867, 6384,  870, 6384,  887,
  430,    0,  -87,  872,  891,    0,   12,    0,    0,  895,
  171,  577, 1909,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  892,
  894,  755,  486,  897,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  523,  421,
  905,   48,  887,    0, 6384,  539,  476,    0, -293,  448,
    0,    0, 5652, 5408, 5003,  -39,    0, 4598,    0,  442,
 8951,  906, 6384,  983,    0,    0,    0,    0,    0, 6789,
    0,    0, 6872,  887,    0,  433,  421,    0,  122, 4520,
  932,    0,  589,    0,    0,    0,    0,    0,    0,  632,
 6384, 6384,  912,    0,  913,    0, -183,    0,  421,    0,
    0,    0, 4681,  476,    0,  421,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  674,    0,  674,
  362,  362, 5330, 5330,  234,  234,  234,  234,  -70,  -70,
  569,  598,  563,  608,    0,  909,  592,    0, 6384, 8975,
 9019,  837, 6384,    0,    0,  681,  915,    0,    0,    0,
    0,    0, -130,    0,    0,   48,  267,  922, 5735,  842,
    0,    0,  920, 5330,    0,    0,    0,    0,  455,  917,
  123,    0,   48,   48,    0,    0,    0,    0,    0,   48,
   48,    0,    0,  916,  597,  847,    0,    0,    0,    0,
  957, 5330, 1992, 5330, 6384,  927,  928, 6384, 6384,  929,
    0,  931,  418,    0,  887, 6545,    0, 6384,  934, 5896,
    0,    0,    0,    0,  720,    0,    0,    0,    0,    0,
    0,  937,    0,  755,    0,    0, 6384,    0,    0,    0,
  473,  -29,  944,    0,  943,  945,  946,    0,    0, -136,
 5086,    0, 7278,    0, 2236, 6057,  319,  954,  950,    0,
    0,  651,  953,    0,    0,    0,  952,    0,  172,  330,
  267,  955,  956,    0,    0, 6384,    0,    0,    0, 6384,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 4842,
    0,    0, 5003,    0,    0,  -61,  958, -165,    0, -182,
    0,    0, 6384,    0,    0,    8,  164,   43,  192,  947,
  687,    0,  959, 6384, 6384,  971,    0,    0,    0, 1044,
    0,  995,  963,    0,  821,    0,  966,    0,    0,  410,
    0,    0,  965,  967,  969,  969,  969,  973,  984,  978,
  964,    0,  989,  206,    0,    0,  985,    0,  990, -244,
    0,  987,    0,    0,  993,  996,    0, 6384,    0,    0,
  -87,  887,  424,    0,    0,  999, 1000, 1001,    0,    0,
  991, 1909,  982,  937,    0,  473, 5330,  452,    0, 5330,
    0,  340, 1117, 1119,    0, 4681,    0,    0,  590, 6140,
    0, 5247,  720, 1012, 5086, 1013,  933,    0,  935,    0,
  939,    0,    0,  887,    0,  -81,    0,    0, 5003,    0,
    0,    0, 6384, 1090, 6384, 1097, 6384,    0,    0, 6384,
    0, 1041,  948,    0, 1032,    0,    0,  995,  -29,    0,
  -29,    0,    0, 5408,    0,    0, 5330, 1058,    0, 1058,
 1058,    0,    0, 6384,  847, 6384, 1021,  749,    0,    0,
 2153, 6384, 1111,    0, 1909,    0, 1036, 5330,    0,    0,
    0,  887,    0,    0,    0, 1909,    0,    0,    0,    0,
    0, -184,    0, -157, 1030, 1039,    0,    0,    0,    0,
 -136,    0,    0,    0,    0,    0,    0,  729,    0,    0,
    0,    0, -302,  -59,  960,    0,    0, 1043,    0, 6384,
 1065, 6384,    0,  691, 1049,    0, 6384,    0,    0,    0,
    0, -105,  -29, 1058,  974,    0, 1050,    0, 1059, 1058,
 1058,  267, 1056,    0,  986,    0, 1058,    0,    0,    0,
 1058,    0,    0, 1062, 6384,    0,  988, 6384,    0, 1063,
 6384, 1158, 1909, 1082,  208,  887,    0, 1909, 1909,    0,
    0,  403,    0,    0, 1191, 1192,    0,    0,    0,    0,
    0,    0,    0, 6384, 1103,    0,    0, 6384,    0,  915,
    0,    0,    0,    0,    0, 1087,  -29,    0,    0,    0,
    0, 5330, 1081, 1091,    0, 1104, 1109,    0, 1101,    0,
 1909,    0,    0, 1105,    0, 1085, 1909,    0, -207,    0,
    0, 1115,    0,    0,    0, 1118, 6384,    0,    0, 1138,
 6384,    0,    0, 1120, 1112,    0,    0,    0, 4925,  -29,
    0,  -29,    0,    0,    0,    0,    0, 2153,    0,    0,
    0, 6384,    0, 1123, -207,    0, -207,    0,    0,    0,
    0, 6384, 1142,    0, 6384,    0, 1124,    0,  267, 1125,
    0,11633,    0,    0,    0, 1129,  -29,    0,    0,    0,
    0,    0,    0,    0,    0,  821,11603,    0,    0,    0,
    0,    0,    0, 1132,  -29,    0,    0,    0,    0,    0,
  821,  -29,  821, 1130,  990,    0, 1909, 1126,    0,    0,
 1909,    0, 1143, 6384,    0, 1133, 4925,    0,    0, 8488,
 1127,    0,    0,    0,  307,  534,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 1139,
  -29,    0,    0, 1909,    0,    0, 1909, 1055,    0, 1143,
    0,    0, 5330, 5330,   33,  288, -256,  359,    0,    0,
  404, 1134, 1141,    0,    0, 5330,   -7, -223,    0,    0,
    0,    0,    0,  210,  218,    0, 5330,    0, 5330,  -87,
 2004, 1144, 1137,  490,    0,    0,    0,    0,  282,    0,
    0, 1060, -151, -276,    0, 1145,  344, -276,  750,  383,
   -5,  797,  -54,  -54,   48,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  -87,    0, -238,
 1148,    0,    0,    0, 1909,    0,  -87,  -87, -154, 1146,
    0,  470,   48,    0,    0,   48,    0,    0,    0,    0,
    0,    0, 1140,    0, 1149,   48, 1147,    0, 1150, 5003,
 5003,    0,11603,    0, -154, -154, 1151,    0,    0,    0,
 1153, 1152, -154, 1154, -129,    0,    0,    0,    0,    0,
    0,   48, -154,    0, 1155, 1157,  815, 1162,    0,    0,
  887, -129,    0, 1164,    0,    0,    0,11423,    0,  -29,
  -29, 1160, 1165, 1167, 1166,    0,    0,    0,    0,    0,
    0,    0,    0, 1058,    0, 1173, 1058, 1261, 1262,11453,
 1174,11483,    0,11513,    0,    0,    0,    0,    0, 1176,
  503,  503, 1178,    0, -154,    0,    0,    0,  887,    0,
  887,    0,    0,    0,11543,11573,    0,    0,    0,    0,
    0,    0,  529,    0,  529,    0,    0,    0,    0,    0,
 1180,    0, 1909,    0, 1182,    0,
  };
  protected static  short [] yyRindex = {         1435,
    0,    0,    0,    0,    0, 1435,    0, 1531,    0,    0,
    0,    0,    0,    0,    0, 8641,    0,    0,    0,    0,
    0,    0,    0,    0, 1355,    0,    0,    0,    0,  580,
 1177,    0,    0,    0,    0,  626,  568,    0, 1188,    0,
    0,  653,    0, 1531,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  349, 8388,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 2658, 1188, 1189,    0,    0, 1190,    0,
    0, 1194,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
11263,  356, 2792,    0,    0, 2792,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 2906,    0,  517,    0,
    0, 2524, 2524,    0,    0,    0,    0,    0, 1199,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,10976, 1195,    0,    0,    0, 1201,
 1202,    0,    0,    0,    0, 9353, 9180,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 9282,    0,    0, 9628, 9846,10211,
10424,10542,10660,10778, 9136, 1110,    0,    0,    0,    0,
    0,    0,    0, 1204,    0,    0,   66,    0,    0,    0,
    0,    0,    0,    0,    0, 1131, 1156, 1206,    0,    0,
    0,    0, 2349, 2792,    0, 1213,    0,  525,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  593,    0,    0,    0,    0,    0,  150,
    0, 3020,    0,  492,    0,10933, 3020,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  142,    0,    0, 1211,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1206, 1220,    0,    0,
    0,    0,    0,    0,    0, 3138,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  746,
    0, 1264,   53,    0,    0,    0,    0,    0,    0,    0,
 1223,    0,    0,    0,    0,    0,    0,    0,   70,    0,
    0,    0,    0,    0,    0,    0,    0, 1224,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1219,    0, 1219,    0,
    0,    0,    0,   17,    0,    0, 8155,    0,    0,    0,
    3, 8221, 1228,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, -210,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 3256,
    0, 8801,    0,    0,    0,  321,    0,  380,    0,    0,
    0,    0, 1229, 1206, 1220,  -32,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  516, 6628,    0,    0, 3256,    0,    0,    0,
    0,    0, 1236,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  -36,    0,    0,    0, 1237,    0, 3256,    0,
    0,    0,    0, 3714,    0, 3256,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 9455,    0, 9526,
 9699, 9775,    0,    0, 9922, 9993,10069,10140,10282,10353,
10471,10589,10707,10825,    0,    0,10896,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  759,    0,    0,    0,
    0,    0, 3888,    0,    0, 8801, 1245,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,   80,
  601,    0, 8801, 8801,    0,    0,    0,    0,    0, 8801,
 8801,    0,    0,    3, 1163,    0,    0,    0,    0,    0,
    0,    0, 1240,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,   25,    0,    0,    0,    0,    0,    0,
    0,  664,    0,    0,    0,    0,    0,    0,    0,    0,
 9051, 7467,    0,    0,  813,  826,  839,    0,    0,    0,
    0,    0,    0,    0,    0,    0,11133,    0, 1249,    0,
    0,    0,    0,    0,    0,    0, 1250,    0,  386,  518,
 1248,    0, 1251,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0, 1247, -141,    0,    0,11072,
    0,    0,    0,    0,    0,  150,    0,  150,    0,    0,
  478,    0,  782,    0,    0, 2863,    0,    0,    0, 3979,
    0, 4080,    0,    0, 1106,    0,    0,    0,    0,  575,
  108,    0,    0,    0,  377,  377,  377,    0,    0,  840,
 1246,    0,    0,    0,    0,    0,    0,    0, 1252,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 1258,    0, 1521,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 1179,  665,    0, 9095,    0, 9139,    0,    0,
    0, 1805,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 3374, 3478, 1259,    0,    0,    0,    0,    0,
    0,    0,    0, 6628,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 4171, 4272,    0,
 -279,    0,    0, 1206,    0,    0,    0, 1268,    0, 1268,
 1268,    0,    0,    0,    0,    0,  846,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  637,    0,    0,  849,  863,    0,    0,    0,    0,
 1247,    0,    0,    0,    0,    0,    0, 3596,    0,    0,
    0,    0,  386,  386,    0,    0,    0, -126,    0,    0,
    0,    0,    0,  748,  802,    0,    0,    0,    0,    0,
    0,    0, 4356, 1263,    0,    0, 1250,    0,    0,  542,
  542,  372,  393,    0,    0,    0,  547,    0,    0,    0,
  542,    0,    0,    0,    0,    0,    0, 1265,    0,    0,
    0, 1715,    0,    0, 1270,    0,    0,    0,    0,    0,
    0,  645,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  746,
    0,    0,    0,    0,  438,    0, -265,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 1269,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 7551,
    0, 8071,    0,    0,    0,    0,    0, 1272,    0,    0,
    0,    0,    0,    0, 1277,    0, 4437,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  240, -227,
    0, 8456,    0,    0,    0,    0, 7646,    0,    0,    0,
    0,    0,    0,    0,    0, 1106, 8567,    0,    0,    0,
    0,    0,    0,    0, 8150,    0,    0,    0,    0,    0,
 1106, 7730, 1106,    0, 1276,    0,    0,    0,    0,    0,
    0,    0,  851,    0,    0,    0,    0, 7909, 7992,  349,
    0,    0,    0,    0,  186,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 7825,    0,    0,    0,    0,    0, -221,    0,    0,  851,
    0,    0,    0,    0,    0,    0, 7359,    0,    0,    0,
    0,  561,    0,    0,    0,    0, -172,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 1282,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  129,    0,    0,  -10,
    0,    0,    0,    0, 8801,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 8681,    0,
    0,    0,    0,    0, 1213,    0,    0,    0,11393,    0,
    0,    0, 8801, 8681,    0, 8801,    0,    0,    0,    0,
    0,    0,    0,    0,    0, 1805,    0,    0,    0, 1220,
 1220,    0,  882,    0, 3019,11298,    0,    0,    0,    0,
    0,    0,11393,    0, 7397, 8756, 8756,    0, 8756,    0,
    0, 1611,11393,    0,    0,    0,    0,    0,    0,    0,
    0, 7397,    0,    0,    0,    0,    0,    0,    0,11333,
11363,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  542,    0,    0,  542, 1283, 1284,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,11393,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 1213,    0,    0,    0,
  };
  protected static  short [] yyGindex = {            0,
    0,  639, 1643, 1644, -497, -596, -738,    0,    0,    0,
    0,    9,    0,    0,    1,    0,    0, -674,  -69,    0,
    0,    0,    0,    0,    0,    0, -640, -605, -586, -929,
 -914, -895, -749, -699,   63,   -9,    0, 1616,    0, 1579,
    0,    0,    0,    0,    0, 1367,  -45, 1368,    0,    0,
    0,  615, -717, -961, -726, -637, -627, -475, -470, -990,
    0, -106,    0,  387,    0, -792,    0,    0,    0,    0,
    0,    0,  553,  461,  526,  834, -781,  -96,    0, 1116,
 1315, -433,  869, -235,    0,    0,    0,    0, -102,  -83,
    5, -538,    0,    0,    0,    0,    0,  -62,  449, -519,
    0,    0,  908,  910,  919,    0,    0, -561,  918,    0,
 -629,    0,    0,    0,    0,  379,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  618,    0,    0,    0,    0,
    0,  360,-1148,    0,    0,    0,    0,    0,    0,    0,
  426,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, -447,
    0,    0,    0,    0,  428,  425,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  730,    0,
    0,    0,  -77,  -91, -193,   18, 1461,  -60,    0,    0,
    0, 1437,  -44, -123,    0,  732,    0, -217,    0,    0,
 1399, -234,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, -214,    0,
 -459, -457, -608,    0,  261,    0,    0,  925,    0, -431,
 -274, 1217,    0,    0,    0,  936,    0,    0, 1069, -341,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 1405, 1016, 1409,    0,  803,  811,    0, 1196,  812,    0,
    0, 1385, 1387, 1390, 1386, 1388,    0,    0,    0,    0,
 1254,    0,  941,    0,    0,    0,    0,    0, -553,    0,
    0,    0,    0,  -63,    0,    0,  810,    0,  641,    0,
    0,  648, -388, -226, -225, -222,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, -791,    0,
 -138,    0, 1352,    0, -565,    0,    0,    0,    0,    0,
    0,  714,    0,    0,  715,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  723,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  997,    0,    0,    0,    0,    0,
    0,    0,  817,    0,    0,    0,    0,  789,    0,    0,
    0,    0,    0,    0, 1208,    0,    0,    0,    0,  631,
    0,    0,    0,  798,    0,
  };
  protected static  short [] yyTable = {           110,
   16,  106,  231,  114,  219,  124,   16,  653,  106,  405,
  406,   31,   37,  407,  619,   65,  266,  713,  402,  448,
  459,  651,  490,  354,  656,  224,  106,  736,  235,  106,
  241,  242, 1165,  162,  723,  724,  447,  919,  920,  449,
  822,  728,  729,  687,  745,  688,   37,  709, 1021,   29,
  842, 1090,  922,  932,  758,  709, 1229,  106,   82,  595,
  618, 1022,   25,  347,  940,   33, 1095,  596,   25,  769,
  106,  941,  682,  800,  797,  304,  116,   69,  106,  264,
  901,  624, 1058,  389,   37,  332,  256, 1227,  228,  504,
  682,  248,   34,   72,  263,  106,  106, 1059,  943,   27,
   28, 1232, 1166,  216, 1211,  221,  344,  229,   65,  218,
 1104,  964,  646,  218,  455,  817, 1060,  970,  971,  465,
  351,  228,  303,   69,   35,  708, 1267,  239,  978,  452,
   67,  118,  352,   70,  353, 1058,  646,  358,  240,  244,
  229,  988,  228,  709,   70,  646,  994,  995,  371, 1104,
 1059,  961,  949,  389,    2,  342,  374,  642,  643,  308,
  585,  229,  402,  449,  963,  354,  217,  110,   53, 1060,
  343,  452,  249,  228,  682,  401,  405,  406,  106,  652,
  407,  228,   53,  228, 1335,  402,  469,  683,  372, 1017,
  888,  325,  229,  389,  465, 1020,  106,  518,  520,  438,
  229,  441,  229,  843,  860,  799,  863,  775,  228,  570,
  456,    4,  816,  462,  228, 1031,  389,  106,  468,  460,
  654,  900,  796,  342, 1167,  106,  360,  229,  326,  455,
  218,  475,  466,  229,  709,  370,    4,  492,  597,  218,
 1078,   30, 1257,  618,  452,  350,  484,  485, 1023,  245,
 1218,  438,  494,  246,  575,  348,  576,   36,  577,  962,
  556,  463, 1061,  942,  507,  508,  509,  510,  511,  512,
  513,  514,  515,  516,  517,  929,  574,    1,  609,  453,
  248,  454,  924,  455,  106, 1115,  580,  887, 1146,  683,
  944,  536,  106,  538, 1063,  476, 1212,  592,  593,  594,
  229,  118,  803,   72,  228,  562,  566,  466,  327,  606,
  328,  228, 1062,  248,  583, 1061,  687,  584,  688,  401,
   69,   73, 1141,  467,  458,  622,  610,  603,  604,  329,
  229,  330,  460,  342,  460,  342,    4,  805, 1147,    1,
  118,  562,  401,  562,  228, 1105,   69, 1063,  628,  223,
  651,  881,  342,  350, 1103,  276,   70,  303, 1219,  350,
  134,  249,  134,  229,   72, 1062,   76,    2,  342, 1043,
  569,  779,  429,  858,  983,  402,  722,  216,  374,  266,
  639, 1094,  389,  611, 1105,  672,  389,   69,  402,  630,
  343,  278,  352, 1103,  249,  950, 1099,   70, 1113,  640,
  106,  279,  303,   78, 1044,  389, 1043,  684,  438,  494,
  448,  668,  566,    4,  689,  118,  352,  665,    3,  106,
  352,   74,   69, 1045,  352,  504,  464,  447,  449,  350,
  449, 1100,   70,  352, 1106,  350,  134,  352,  134,  350,
   74, 1044,  264,  350, 1107,  677,  677,  352,    4,  350,
  492,  228,  736,  228,  735,   79,  228,  686,  619,  303,
 1045,  228,  106,  106,  389,  228, 1101,  757,  228,  660,
 1100,  351,  493, 1106,  228,  353,  228,  351,  242,  106,
  106,  228,  619, 1107,  304, 1102,  228,  228,  351,  278,
  228, 1304,  115,  694, 1307,  229,  350,  701,  350,  279,
  352,  352,  228,  106,  228, 1101,  229,  350,  228,  619,
  597,  350,  597,  717,  352,  350,  228,  228,  352,  350,
  228,  111,  105,  228, 1102,  247,  350,  764,  352,  228,
  350,  106,  401,  106,  350,  405,  406, 1206,  350,  407,
  303,  122,  755, 1148,  402,  401, 1207,  213,  229,  741,
  215,   69,  694,  746,  111,  613,  353,  228,  478,  614,
  756,  787,  694,  112,  761,  305,  306,  307,  308,  673,
  763,  228,  276,  228,  225,  228,  229,  350,  238,  649,
 1123,  765,  224,  228,   87,   72,  123,  786,   89,  350,
  229,  255,  229, 1149,  229,  566, 1108,  350,  226,  261,
  438, 1109,  229,  649,  223, 1259, 1260, 1208,  278,  337,
  649,  804,  649, 1265,  276,  126,  285,  290,  279,   47,
  792,  619,   65, 1276,  793,  227,  788,  402,  620,  323,
  338,  324,  228,  228,  494, 1108, 1223,  494,  402,  806,
 1109,  468,  947,   48,   43,  619,   46,  802,  277, 1135,
  278,  229,  620,  837,  339,  991,   49,  730,  811,  812,
  279,   51,  276,  222, 1151, 1170,   52,  848,   53,   54,
   55,   56,  619,  750, 1241, 1324,   57, 1244,  214,  620,
   58,  865,   81,  223, 1152,  487,  352,  751,  352,  851,
  352,  487,   59,  751, 1242,   60,  780,   61,  278,  789,
  228,  401,  847,  470,  752,    4,  106, 1215,  279,  106,
  852,  372,  352, 1275,  352,  402,  639,  460,  223,  471,
  402,  402,  228,   69,  228,  482, 1272, 1273,  619, 1274,
  871,  668,  641,   70,  873,  372,  321,  322,  372,  566,
  639,  229,  372,  229,  488,  396,  479,  396,  351,  396,
  488,  350,  352,  494,  350,  350,  641,  889,  281,  891,
  352,  893,   69,  402,  894,  282,  106, 1155,  996,  402,
  350,  396,   70,  396,  350,  824, 1156,  283,  909,  281,
  997,  766, 1255, 1256,  401,  216,  282,  106,  741,  937,
  694,  241,  242,  111,  229,  401,  930,  259,  283,   83,
   69,   84,  336,  239,   85,  557,  217,  228,  336,   86,
  658,  396,  644,   88,  671,  337, 1136,  813,  645,   16,
  243,  905,   91,  813,  813,  239,  229,  813,  813,   92,
  813,  813,   72, 1238,   93,  237,  721,  968,   94,  353,
  291,  297, 1239,  298,  953,  299,  955, 1240,  813,   70,
   95,  959,   96,  111,  405,  406,   97,  342,  407,  402,
  342,  429, 1202,  402,   98,   99,  111,  300,  100,  301,
  623, 1137,  401,  993,  614, 1318,  342,  401,  401,  980,
   47,  369,  694,  342,  369,  986,  342,  356,  247,  626,
  405,  406, 1328,  627,  407,  357,  402,  257,  356,  402,
  369, 1329,  342,   16,   48,  642,  357,  302, 1000,  309,
  643,  106,  894,   68,  642,   69,  641,   49,  278,  643,
  401,  661,   51,  259,  287,   70,  401,   52,  279,   53,
   54,   55,   56,  287,   67,   67,  292,   57,   67,  140,
  285,   58,  297,  140,  298,  140,  299,  140,  106,  350,
  334,  741,  350,   59,  872, 1033,   60,  483,   61,  293,
  627,  617,  297,  483,  298,  614,  299,  905,  300,  134,
  301,  134,  331,  134,  332,  356,  741,  357,  405,  406,
  333,   74,  407,  690,  690,  614, 1083,  402,  300, 1085,
  301,  335,  350,  350,  253,  350,  350,   60,  676,  336,
  697,  699,  627,  176,   83,  176,   84,  176,  302,   85,
 1042,  174, 1057,  174,   86,  174,  401,  783,   88,   68,
  401,  627,  341,   68,  720,  231,  106,   91,  302,  106,
  704,  705,  691,  692,   92,  106,  691,  692, 1120,   93,
  874,  808,  809,   94,  880,  956,  957, 1042, 1172,  356,
  359,  369,  734,  401,  740,   95,  401,   96,  294,  295,
  296,   97,  106,  106,  355, 1057, 1175,  359,  345,   98,
   99,  362, 1042,  100,  364,  106,  117,  473,  295,  296,
 1213,  560,  366,  561, 1216,  228,  106,  816,  106, 1221,
 1222, 1203,  458,  816,  816,  793,  793,  816,  816,  440,
  816,  816,  254,  461,  228,   24,  405,  406,  794,  794,
  407, 1042,  830,  831, 1224,  402,  477,  926,  816,  927,
  835,  809, 1217, 1230, 1231,  521,  522,  809,  809,  481,
  310,  809,  809,  480,  809,  809, 1153,  525,  526,  527,
  528,  810,  529,  530,  401, 1164, 1168,  810,  810,  499,
  500,  810,  810, 1171,  810,  810,  268,  269,  270,  271,
  272,  273,  274,  275,  311, 1290,  312,  356,  313, 1220,
  314,   47,  315, 1279,  316, 1280,  317,  157,  318,  163,
  319,  163,  320,  278,  494,  494,   74,  506,  559, 1315,
  806, 1316,  170,  554,  170,   48,  806,  806, 1282,  570,
  806,  806,  571,  806,  806,  171,  579,  171,   49, 1233,
  122,  343,  122,   51,  777,  191,  777,  191,   52,  578,
   53,   54,   55,   56,  582,  598,  110,  862,   57,  164,
  864,  164,   58,  587,  599, 1233, 1233,  601, 1319, 1319,
  621,   47,  621, 1233,   59, 1268, 1326,   60, 1327,   61,
  111,  110,  110, 1233,  539,  600,  607,  602,  608,  612,
  540,  541, 1268,   24,  620,   48,  621,  542,  543,  625,
 1292, 1294,  401,  629,  664,  675,  228,  666,   49,  680,
  693,  681,   50,   51,  700,  714,  719,  912,   52,  348,
   53,   54,   55,   56,  730,  229,  353,  733,   57,  743,
  744,  747,   58,  749,  614, 1233,  759,  356,  935,  487,
  488,  489,  771,  772,   59,  773,  774,   60,  781,   61,
  782,  784,  785,  790,  807,  791,  798,  813,    2,  810,
    3,  820,  823,  825,  835,  826,  519,  519,  519,  519,
  827,  832,  519,  519,  519,  519,  519,  519,  519,  519,
  519,  519,  833,  519,   52,  834,  836,  840,  217,  844,
  841,  845,   24,  857,  846,  578,   24,  854,  855,  856,
  859,   24,  869,   24,  870,  453,   24,  882,   24,   24,
  883,   24,  884,   24,  890,   24,  885,   24,   24,   24,
   24,  892,  897,   24,   24,  898,  899,  915,  925,   24,
  945,   24,   24,   24,  934,  931,   24,   24,   24,  946,
   24,  952,  954,   24,  951,   24,   24,   24,   24,  958,
  967,  965,   24,   24,   24,  969,  972,   24,   24,   24,
  979,  985,  912,  973,    5,  837,   24,   24,  987,   24,
   24,   24,   24,   24,   24,  989,  998,  999,   24,  578,
 1001, 1006, 1009, 1019, 1010,  578,  578,  578,  578,  578,
  578,  578,  578,  578,  578,  578,  578, 1012,  519, 1039,
   24,   24, 1014, 1016,  578,  578,  578, 1018,  578,   24,
  578,  578,  578, 1029, 1030,  667, 1032, 1079,  487, 1035,
 1084, 1086, 1034, 1092,  578, 1087, 1097, 1116, 1114, 1118,
 1134, 1121, 1142, 1139, 1161, 1159, 1200, 1210, 1248, 1199,
 1214, 1228, 1252, 1237, 1254, 1250, 1308, 1310, 1264, 1261,
   24, 1263, 1266, 1277,   24, 1278, 1281, 1285, 1296,   24,
    5,   24, 1299, 1297,   24, 1298,   24, 1306, 1314,   24,
 1317,   24, 1322,   24, 1334,   24, 1336, 1039,   24,   28,
 1128,   24,   24,   24,   26,   27, 1138,   22,   73,   24,
   24,   24,  522,  598,   24,   24,   24,  327,   24,   74,
   72,   24,  498,   24,   24,   24,   24,  657,  207,  599,
   24,   24,   24, 1144, 1145,   24,   24,   24,  449,  753,
  499,  753,  658,  439,   24,   24, 1163,   24,   24,   24,
   24,   24,   24,   96,   71,  450,   24, 1173,  328, 1174,
  492,   52,  723,  440,   26,   54,  510,  454,  693,  511,
   54,  765,   54,   27,  726,   54,  494,   54,   24,   24,
   54,  642,   54,  704,   54,  642,   54,  727,  761,   54,
  729,  705,   54,   54,  731,  192,  312,  316,   44,   45,
   54,   54,   54,   75,  125,   54,   54,   54,  483,   54,
  486, 1093,   54, 1140,   54,   54,   54,   54,  923, 1169,
  572,   54,   54,   54, 1251,  718,   54,   54,   54,  866,
 1321,  867, 1098,  861, 1332,   54,   54, 1283,   54,   54,
  868,   54,   54,   54, 1293,   53, 1007,   54, 1295,  437,
   53,  472,   53, 1008,  502,   53,  877,   53,  679,  501,
   53,   24,   53,  777,   53,  505,   53,  531,  878,  692,
  532,  534,   53,   53,  533,  537,  975, 1122, 1117,  586,
   53,   53,   53,  674,  886,   53,   53,   53, 1080,   53,
 1075, 1082,   53,  982,   53,   53,   53,   53, 1003,  853,
 1143,   53,   53,   53,  707, 1002,   53,   53,   53,    0,
    0,    0,    0,    0,    0,   53,   53,    0,   53,   53,
    0,   53,   53,   53,    0,    0,  755,   53,    0,    0,
    0,    0,    0,    0,    0,  755,  755,  755,  755,  755,
    0,  755,  755,    0,  755,  755,  755,  519,  755,  755,
  755,  755,   54,    0,    0,    0,  755,    0,  755,  755,
  755,  755,  755,  755,    0,    0,  755,    0,    0,    0,
  755,  755,    0,  755,  755,  755,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  755,    0,  755,    0,  755,
  755,    0,    0,  755,    0,  755,  755,  755,  755,  755,
  755,  755,  755,  755,  755,  755,  755,    0,  755,    0,
    0,  755,    0,    0,    0,    0,  755,    0,    0,    0,
    0,    0,    0,    0,    0,    0,   53,    0,   53,    0,
    0,   53,   53,    0,  755,  755,   53,    0,  755,    0,
   53,    0,    0,  755,  755,  755,  755,  755,    0,   53,
    0,    0,    0,  755,    0,  755,   53,    0,    0,    0,
    0,   53,  755,    0,  755,   53,    0,   53,    0,   53,
    0,    0,    0,    0,   53,    0,    0,   53,    0,   53,
    0,    0,    0,   53,    0,    0,   53,    0,    0,    0,
    0,   53,   53,    0,    0,   53,    0,    0,   53,    0,
    0,    0,    0,    0,    0,    0,  755,    0,  755,    0,
  755,    0,  755,    0,  755,    0,  755,    0,  755,  755,
  699,    0,    0,    0,  755,    0,  755,  158,    0,  699,
  699,  699,  699,  699,    0,  699,  699,    0,  699,  699,
  699,    0,  699,  699,  699,    0,    0,    0,    0,    0,
  699,    0,  699,  699,  699,  699,  699,  699,    0,    0,
  699,    0,    0,    0,  699,  699,    0,  699,  699,  699,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  699,
    0,  699,    0,  699,  699,    0,    0,  699,    0,  699,
  699,  699,  699,  699,  699,  699,  699,  699,  699,  699,
  699,    0,  699,    0,    0,  699,    0,    0,   53,    0,
  699,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   53,    0,   53,    0,    0,   53,    0,    0,  699,  699,
   53,    0,  699,    0,   53,    0,    0,  699,  699,  699,
  699,  699,    0,   53,    0,    0,    0,  699,    0,  699,
   53,    0,    0,    0,    0,   53,  699,    0,  699,   53,
    0,   53,    0,   53,    0,    0,    0,    0,   53,    0,
    0,   53,    0,   53,    0,    0,    0,   53,    0,    0,
   53,    0,    0,    0,    0,   53,   53,    0,    0,   53,
    0,    0,   53,    0,    0,    0,    0,    0,    0,    0,
  699,    0,  699,    0,  699,    0,  699,    0,  699,    0,
  699,    0,  699,  699,  375,    0,    0,    0,  699,    0,
  699,    0,    0,  127,   83,  376,   84,    0,    0,   85,
  377,    0,  378,  379,   86,    0,  129,  380,   88,    0,
    0,    0,    0,    0,  130,    0,  381,   91,  382,  383,
  384,  385,    0,    0,   92,    0,    0,    0,  386,   93,
    0,  131,  132,   94,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  387,    0,   95,    0,   96,  133,    0,
    0,   97,    0,  388,  134,  389,  135,  390,  136,   98,
   99,  391,  392,  100,  393,    0,  394,  375,    0,  395,
    0,    0,   53,    0,  139,    0,  127,   83,    0,   84,
    0,    0,   85,  128,    0,    0,    0,   86,    0,  129,
    0,   88,  111,    0,    0,    0,  140,  130,    0,    0,
   91,  396,  141,  142,  143,  144,    0,   92,    0, 1176,
    0,  145,   93,  146,  131,  132,   94,    0,    0,    0,
  147,    0,  148,    0,    0,    0,    0,    0,   95,    0,
   96,  133,    0,    0,   97,    0,    0,  134,    0,  135,
    0,  136,   98,   99,  137,    0,  100,    0,    0,  394,
    0, 1177,    0,    0,    0,    0,    0,  139,    0,    0,
    0,    0,    0,    0,  149,    0,  150,    0,  151,    0,
  152,    0,  153,    0,  154,    0,  397,  156,    0,  140,
    0,    0,  157,    0,    0,  141,  142,  143,  144,    0,
    0,    0,    0,    0,  145,    0,  146, 1178, 1179, 1180,
 1181,    0, 1182,  147, 1183,  148, 1184, 1185, 1186, 1187,
 1188, 1189,    0,    0,    0, 1190,    0, 1191,    0, 1192,
    0, 1193,    0, 1194,    0, 1195,    0, 1196,  375, 1197,
    0,    0,    0,    0,    0,    0,    0,  127,   83,    0,
   84,    0,    0,   85,  128,    0,    0,  149,   86,  150,
  129,  151,   88,  152,    0,  153,    0,  154,  130,  262,
  156,   91,    0,    0,    0,  157,    0,    0,   92,    0,
    0,    0,    0,   93,    0,  131,  132,   94,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,   95,
    0,   96,  133,    0,    0,   97,    0,    0,  134,    0,
  135,    0,  136,   98,   99,  137,    0,  100,    0,    0,
  138,  444,    0,    0,    0,    0,    0,    0,  139,    0,
  127,   83,    0,   84,    0,    0,   85,  128,    0,    0,
    0,   86,    0,  129,    0,   88,    0,    0,    0,    0,
  140,  130,    0,    0,   91,    0,  141,  142,  143,  144,
    0,   92,    0,    0,    0,  145,   93,  146,  131,  132,
   94,    0,    0,    0,  147,    0,  148,    0,    0,    0,
    0,    0,   95,    0,   96,  133,    0,    0,   97,    0,
    0,  134,    0,  135,    0,  136,   98,   99,  137,    0,
  100,    0,    0,  138,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  149,    0,
  150,    0,  151,    0,  152,    0,  153,    0,  154,    0,
  262,  156,    0,  445,  490,    0,  157,    0,    0,  490,
  490,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  490,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  490,    0,    0,  490,  490,    0,    0,
    0,  490,    0,    0,  490,    0,  490,    0,  490,  490,
  490,  490,    0,    0,    0,    0,  490,    0,    0,    0,
  490,  149,    0,  150,  490,  151,    0,  152,    0,  153,
    0,  154,  490,  446,    0,  490,    0,  490,  490,  157,
    0,    0,    0,    0,  490,  490,  490,  490,  490,  490,
  490,  490,  490,  490,  490,  490,    0,    0,    0,    0,
    0,    0,  490,  490,    0,  490,  490,  490,  490,  490,
  490,  490,    0,  490,  490,    0,  490,  490,    0,  490,
  490,  490,  490,  490,  490,  490,  490,  490,    0,    0,
  490,    0,  490,    0,  490,    0,  490,    0,  490,    0,
  490,    0,  490,    0,  490,    0,  490,    0,  490,    0,
  490,    0,  490,    0,  490,    0,  490,    0,  490,    0,
  490,    0,  490,    0,  490,    0,  490,    0,  490,  350,
  490,    0,  490,    0,  350,  350,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  490,  490,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  350,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  350,    0,
    0,  350,  350,    0,    0,    0,  350,    0,    0,  350,
    0,  350,    0,  350,  350,  350,  350,    0,    0,    0,
    0,  350,    0,    0,    0,  350,    0,    0,    0,  350,
    0,    0,    0,    0,    0,    0,    0,  350,    0,    0,
  350,    0,  350,  350,    0,    0,    0,    0,    0,  350,
  350,  350,  350,  350,  350,  350,  350,  350,  350,  350,
  350,    0,    0,    0,    0,    0,    0,  350,  350,  350,
  350,  350,  350,  350,  350,  350,  350,    0,    0,    0,
    0,    0,  350,    0,  350,  350,  350,  350,  350,    0,
    0,  350,  350,  350,    0,    0,    0,    0,  350,  350,
    0,    0,    0,  350,    0,  350,    0,  350,    0,  350,
    0,  350,    0,  350,    0,    0,    0,    0,    0,    0,
    0,    0,  350,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  350,    0,    0,  350,  350,  350,    0,    0,
  350,    0,    0,    0,    0,  350,    0,  350,  350,  350,
  350,  350,  350,    0,    0,  350,    0,    0,    0,  350,
    0,    0,    0,  350,    0,    0,    0,    0,    0,    0,
    0,  350,    0,    0,  350,    0,  350,  350,    0,    0,
    0,    0,    0,  350,  350,  350,  350,  350,  350,  350,
  350,  350,  350,  350,  350,    0,    0,    0,    0,    0,
    0,  350,  350,  350,  350,  350,  350,  350,  350,  350,
  350,    0,    0,    0,    0,    0,  350,    0,  350,  350,
  350,  350,  350,    0,    0,  350,  350,  342,    0,    0,
    0,    0,  342,  342,    0,    0,    0,  350,    0,  350,
    0,  350,    0,  350,    0,  350,    0,  350,    0,    0,
    0,    0,    0,    0,    0,    0,  342,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  342,    0,    0,  342,
  342,  350,    0,    0,  342,    0,    0,  342,    0,  342,
    0,  342,  342,  342,  342,  350,  350,    0,    0,  342,
    0,    0,    0,  342,    0,    0,    0,  342,  819,    0,
    0,    0,    0,    0,    0,  342,    0,    0,  342,    0,
  342,  342,    0,    0,    0,    0,    0,  342,  342,  342,
  342,  342,  342,  342,  342,  342,  342,  342,  342,    0,
    0,    0,    0,    0,    0,  342,  342,  342,  342,  342,
  342,  389,  342,  342,  342,    0,    0,  389,    0,    0,
  342,    0,  342,  342,  342,  342,  342,    0,    0,  342,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  342,    0,  342,    0,  342,    0,  342,    0,  342,
  389,  342,  819,    0,  389,    0,    0,    0,  819,  819,
  819,  819,  819,  819,  819,  819,  819,  819,  819,    0,
    0,    0,    0,    0,    0,  342,    0,  819,  819,  819,
    0,  819,    0,  819,  819,  819,    0,    0,    0,  342,
  342,    0,    0,    0,    0,  389,    0,  819,    0,    0,
    0,  389,  389,  389,  389,  389,  389,  389,  389,  389,
  389,  389,  389,    0,    0,    0,    0,    0,    0,  389,
  389,  389,  389,  389,  389,  352,  389,  389,  389,   53,
    0,  352,    0,    0,  389,    0,  389,  389,  389,  389,
    0,    0,    0,  389,  389,    0,    0,    0,    0,    0,
    0,    0,    0,   53,    0,  389,    0,  389,    0,  389,
    0,  389,    0,  389,    0,  389,   53,    0,  352,    0,
    0,   53,    0,    0,    0,    0,   53,    0,   53,   53,
   53,   53,    0,    0,    0,    0,   53,    0,    0,  389,
   53,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   53,  389,  389,   53,    0,   53,    0,  352,
    0,    0,    0,    0,    0,  352,  352,  352,  352,  352,
  352,  352,  352,  352,  352,  352,  352,   53,    0,   53,
    0,    0,    0,  196,  352,  352,  352,  352,  352,  352,
  352,  352,  352,  424,  352,  352,    0,  352,  352,  424,
  352,    0,  352,  352,  352,  352,  352,  352,  352,    0,
    0,  352,    0,  352,    0,  352,    0,  352,    0,  352,
    0,  352,    0,  352,    0,  352,    0,  352,    0,  352,
    0,  352,    0,  352,    0,  352,  424,  352,    0,  352,
    0,  352,    0,  352,    0,  352,    0,  352,    0,  352,
    0,  352,    0,  352,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  352,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  424,    0,    0,
    0,    0,    0,  424,  424,  424,  424,  424,  424,  424,
  424,  424,  424,  424,  424,    0,    0,    0,    0,    0,
    0,    0,  424,  424,  424,  424,  424,  424,  424,  424,
  424,  350,  424,  424,    0,  424,  424,  350,  424,    0,
  424,  424,  424,  424,  424,  424,  424,    0,    0,  424,
    0,  424,    0,  424,    0,  424,    0,  424,    0,  424,
    0,  424,    0,  424,    0,  424,    0,  424,    0,  424,
    0,  424,    0,  424,  350,  424,    0,  424,    0,  424,
    0,  424,    0,  424,    0,  424,    0,  424,    0,  424,
    0,  424,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  424,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  350,    0,    0,    0,    0,
    0,  350,  350,  350,  350,  350,  350,  350,  350,  350,
  350,  350,  350,    0,    0,    0,    0,    0,    0,    0,
  350,  350,  350,  350,  350,  350,  350,  350,  350,  490,
  350,  350,    0,  350,  350,  490,  350,    0,  350,  350,
  350,  350,  350,  350,  350,    0,    0,  350,    0,  350,
    0,  350,    0,  350,    0,  350,    0,  350,    0,  350,
    0,  350,    0,  350,    0,  350,    0,  350,    0,  350,
    0,  350,  490,  350,    0,  350,    0,  350,    0,  350,
    0,  350,    0,  350,    0,  350,    0,  350,    0,  350,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  350,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  490,    0,    0,    0,    0,    0,  490,
  490,  490,  490,  490,  490,  490,  490,  490,  490,  490,
  490,    0,    0,  435,    0,    0,    0,  490,  490,  435,
  490,  490,  490,  490,  490,  490,  490,    0,  490,  490,
    0,  490,  490,    0,  490,    0,  490,  490,  490,  490,
  490,  490,  490,    0,    0,  490,    0,  490,    0,  490,
    0,  490,    0,  490,    0,  490,  435,  490,    0,  490,
    0,  490,    0,  490,    0,  490,    0,  490,    0,  490,
    0,  490,    0,  490,    0,  490,    0,  490,    0,  490,
    0,  490,    0,  490,    0,  490,    0,  490,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  435,    0,    0,
    0,  490,    0,  435,  435,  435,  435,  435,  435,  435,
  435,  435,  435,  435,  435,    0,    0,    0,    0,    0,
    0,    0,  435,  435,  435,  435,  435,  435,  435,  435,
  435,  502,  435,  435,    0,  435,  435,  502,  435,    0,
  435,  435,  435,  435,  435,  435,  435,    0,    0,  435,
    0,  435,    0,  435,    0,  435,    0,  435,    0,  435,
    0,  435,    0,  435,    0,  435,    0,  435,    0,  435,
    0,  435,    0,  435,  502,  435,    0,  435,    0,  435,
    0,  435,    0,  435,    0,  435,    0,  435,    0,  435,
    0,  435,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  435,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  502,    0,    0,    0,    0,
    0,  502,  502,  502,  502,  502,  502,  502,  502,  502,
  502,  502,  502,    0,    0,    0,    0,    0,    0,    0,
  502,  502,  502,  502,  502,  502,  502,  502,  502,  432,
  502,  502,    0,  502,  502,  432,  502,    0,  502,  502,
  502,  502,  502,  502,  502,    0,    0,  502,    0,  502,
    0,  502,    0,  502,    0,  502,    0,  502,    0,  502,
    0,  502,    0,  502,    0,  502,    0,  502,    0,  502,
    0,  502,  432,  502,    0,  502,    0,  502,    0,  502,
    0,  502,    0,  502,    0,  502,    0,  502,    0,  502,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  502,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  432,    0,    0,    0,    0,    0,  432,
  432,  432,  432,  432,  432,  432,  432,  432,  432,  432,
  432,    0,    0,    0,    0,    0,    0,    0,  432,    0,
  432,    0,  432,    0,  432,  432,  432,    0,  432,  432,
    0,  432,  432,    0,  432,    0,  432,  432,  432,  432,
  432,  432,  432,    0,    0,    0,    0,    0,    0,  432,
    0,  432,    0,  432,    0,  432,    0,  432,    0,  432,
    0,  432,    0,  432,    0,  432,    0,  432,    0,  432,
    0,  432,    0,  432,    0,  432,    0,  432,    0,  432,
    0,  432,    0,  432,   37,    0,    0,  432,   37,    0,
    0,    0,    0,   37,    0,   37,    0,    0,   37,    0,
   37,  432,    0,   37,    0,   37,    0,   37,    0,   37,
    0,    0,    0,    0,    0,   37,   37,    0,    0,    0,
    0,    0,    0,   37,   37,   37,    0,    0,   37,   37,
   37,    0,   37,    0,    0,   37,    0,   37,   37,   37,
   37,    0,    0,    0,   37,   37,   37,    0,    0,   37,
   37,   37,    0,    0,    0,    0,    0,    0,   37,   37,
    0,   37,   37,   37,   37,   37,   37,    0,    0,    0,
   37,    0,    0,    0,    0,   38,    0,    0,    0,   38,
    0,    0,    0,    0,   38,    0,   38,    0,    0,   38,
    0,   38,   37,   37,   38,    0,   38,    0,   38,    0,
   38,    0,    0,    0,    0,    0,   38,   38,    0,    0,
    0,    0,    0,    0,   38,   38,   38,    0,    0,   38,
   38,   38,    0,   38,    0,    0,   38,    0,   38,   38,
   38,   38,    0,    0,    0,   38,   38,   38,    0,    0,
   38,   38,   38,    0,    0,    0,    0,    0,    0,   38,
   38,    0,   38,   38,   38,   38,   38,   38,    0,    0,
    0,   38,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   37,   35,    0,    0,    0,
   35,    0,    0,   38,   38,   35,    0,   35,    0,    0,
   35,    0,   35,    0,    0,   35,    0,   35,    0,   35,
    0,   35,    0,    0,   35,    0,    0,   35,   35,    0,
    0,    0,    0,    0,    0,   35,   35,   35,    0,    0,
   35,   35,   35,    0,   35,    0,    0,   35,    0,   35,
   35,   35,   35,    0,    0,    0,   35,   35,   35,    0,
    0,   35,   35,   35,    0,    0,    0,    0,    0,    0,
   35,   35,    0,   35,   35,    0,   35,   35,   35,    0,
    0,    0,   35,    0,    0,    0,   38,   36,    0,    0,
    0,   36,    0,    0,    0,    0,   36,    0,   36,    0,
    0,   36,    0,   36,   35,   35,   36,    0,   36,    0,
   36,    0,   36,    0,    0,   36,    0,    0,   36,   36,
    0,    0,    0,    0,    0,    0,   36,   36,   36,    0,
    0,   36,   36,   36,    0,   36,    0,    0,   36,    0,
   36,   36,   36,   36,    0,    0,    0,   36,   36,   36,
    0,    0,   36,   36,   36,    0,    0,    0,    0,    0,
    0,   36,   36,    0,   36,   36,    0,   36,   36,   36,
    0,    0,    0,   36,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,   35,   39,    0,
    0,    0,   53,    0,    0,   36,   36,   53,    0,   53,
    0,    0,   53,    0,   53,    0,    0,   53,    0,   53,
    0,   53,    0,   53,    0,    0,   53,    0,    0,   53,
   53,    0,    0,    0,    0,    0,    0,   53,   53,   53,
    0,    0,   53,   53,   53,    0,   53,    0,    0,   53,
    0,   53,   53,   53,   53,    0,    0,    0,   53,   53,
   53,    0,    0,   53,   53,   53,    0,    0,    0,    0,
    0,    0,   53,   53,    0,   53,   53,    0,   53,   53,
   53,    0,   40,    0,   53,    0,   53,    0,   36,    0,
    0,   53,    0,   53,    0,    0,   53,    0,   53,    0,
    0,   53,    0,   53,    0,   53,   39,   53,    0,    0,
   53,    0,    0,   53,   53,    0,    0,    0,    0,    0,
    0,   53,   53,   53,    0,    0,   53,   53,   53,    0,
   53,    0,    0,   53,    0,   53,   53,   53,   53,    0,
    0,    0,   53,   53,   53,    0,    0,   53,   53,   53,
    0,    0,    0,    0,    0,    0,   53,   53,    0,   53,
   53,    0,   53,   53,   53,    0,    0,    0,   53,    0,
    0,  708,  708,  708,  708,    0,    0,  708,  708,    0,
  708,  708,  708,    0,  708,  708,  708,    0,    0,   53,
   40,    0,  708,    0,  708,  708,  708,  708,  708,  708,
    0,    0,  708,    0,    0,    0,  708,  708,    0,  708,
  708,  708,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  708,    0,  708,    0,  708,  708,    0,    0,  708,
    0,  708,  708,  708,  708,  708,  708,  708,  708,  708,
  708,  708,  708,    0,  708,    0,    0,  708,    0,    0,
    0,    0,  708,    0,    0,   83,    0,   84,    0,    0,
   85,    0,    0,    0,    0,   86,    0,    0,    0,   88,
  708,    0,    0,   53,  708,    0,    0,    0,   91,  708,
  708,  708,  708,  708,    0,   92,    0,    0,    0,  708,
   93,  708,    0,    0,   94,    0,  281,    0,  708,    0,
  708,    0,    0,  282,    0,    0,   95,    0,   96,    0,
    0,    0,   97,    0,    0,  283,    0,    0,    0,    0,
   98,   99,    0,    0,  100,    0,    0,  117,    0,    0,
    0,    0,  127,   83,    0,   84,    0,    0,   85,  128,
    0,    0,  708,   86,  708,  129,  708,   88,  708,    0,
  708,    0,  708,  130,  708,  708,   91,    0,    0,    0,
  708,    0,    0,   92,    0,    0,    0,    0,   93,    0,
  131,  132,   94,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   95,    0,   96,  133,  564,    0,
   97,    0,    0,  134,    0,  135,    0,  136,   98,   99,
  137,    0,  100,    0,    0,  138,    0,    0,    0,  565,
    0,    0,    0,  139,    0,  127,   83,    0,   84,    0,
    0,   85,  128,    0,    0,    0,   86,    0,  129,    0,
   88,  458,  655,    0,    0,  140,  130,  284,    0,   91,
    0,  141,  142,  143,  144,    0,   92,    0,    0,    0,
  145,   93,  146,  131,  132,   94,    0,  491,    0,  147,
    0,  148,    0,    0,  492,    0,    0,   95,    0,   96,
  133,    0,    0,   97,    0,    0,  134,    0,  135,    0,
  136,   98,   99,  137,    0,  100,    0,    0,  138,    0,
    0,    0,  493,    0,    0,    0,  139,    0,    0,    0,
    0,    0,    0,  149,    0,  150,    0,  151,    0,  152,
    0,  153,    0,  154,    0,  262,  156,    0,  140,  685,
    0,  157,    0,    0,  141,  142,  143,  144,    0,    0,
    0,    0,    0,  145,    0,  146,    0,    0,    0,    0,
    0,    0,  147,    0,  148,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  127,   83,    0,   84,
    0,    0,   85,  128,    0,    0,  149,   86,  150,  129,
  151,   88,  152,    0,  153,    0,  154,  130,  262,  156,
   91,    0,    0,    0,  157,    0,    0,   92,    0,    0,
    0,    0,   93,    0,  131,  132,   94,    0,  491,    0,
    0,    0,    0,    0,    0,  492,    0,    0,   95,    0,
   96,  133,    0,    0,   97,    0,    0,  134,    0,  135,
    0,  136,   98,   99,  137,    0,  100,    0,    0,  138,
    0,    0,    0,  493,    0,    0,    0,  139,    0,    0,
   83,    0,   84,    0,    0,   85,    0, 1036,    0,    0,
   86,    0,    0,    0,   88,    0,    0,    0,    0,  140,
  795,    0,    0,   91,    0,  141,  142,  143,  144,    0,
   92,    0,    0,    0,  145,   93,  146, 1037,    0,   94,
    0,    0,    0,  147,    0,  148,    0,    0,    0,    0,
    0,   95,    0,   96,    0,    0,    0,   97, 1038,    0,
    0,    0,    0,    0,    0,   98,   99,    0,    0,  100,
    0,    0,  117,    0,    0,    0,    0,  127,   83,    0,
   84,    0,    0,   85,  128,    0,    0,  149,   86,  150,
  129,  151,   88,  152,    0,  153,    0,  154,  130,  262,
  156,   91,    0,    0,    0,  157,    0,    0,   92,    0,
    0,    0,    0,   93,    0,  131,  132,   94,    0,  491,
    0,    0,    0,    0,    0,    0,  492,    0,    0,   95,
    0,   96,  133,    0,    0,   97,    0,    0,  134,    0,
  135,    0,  136,   98,   99,  137,    0,  100,    0,    0,
  138,    0,    0,    0,  493,    0,    0,    0,  139,    0,
  127,   83,    0,   84,    0,    0,   85,  128,    0,    0,
    0,   86,    0,  129,    0,   88,    0,    0,    0,    0,
  140,  130,   74,    0,   91,    0,  141,  142,  143,  144,
    0,   92,    0,    0,    0,  145,   93,  146,  131,  132,
   94,    0,    0,    0,  147,    0,  148,    0,    0,    0,
    0,    0,   95,    0,   96,  133,  564,    0,   97,    0,
    0,  134,    0,  135,    0,  136,   98,   99,  137,    0,
  100,    0,    0,  138,    0,    0,    0,  565,    0,    0,
    0,  139,    0,    0,    0,    0,    0,    0,  149,    0,
  150,    0,  151,    0,  152,    0,  153,    0,  154,  458,
  262,  156,    0,  140,    0,    0,  157,    0,    0,  141,
  142,  143,  144,    0,    0,    0,    0,    0,  145,    0,
  146,    0,    0,    0,    0,    0,    0,  147,    0,  148,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  127,   83,    0,   84,    0,    0,   85,  128,    0,
    0,  149,   86,  150,  129,  151,   88,  152,    0,  153,
    0,  154,  130,  262,  156,   91,    0,    0,    0,  157,
    0,    0,   92,    0,    0,    0,    0,   93,    0,  131,
  132,   94,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,   95,    0,   96,  133,    0,    0,   97,
    0,    0,  134,    0,  135,    0,  136,   98,   99,  137,
    0,  100,    0,    0,  138,    0,    0,    0,    0,    0,
    0,    0,  139,    0,    0,   83,    0,   84,    0,    0,
   85,    0,    0,    0,    0,   86,    0,    0,    0,   88,
  646,  876,    0,    0,  140,    0,    0,    0,   91,    0,
  141,  142,  143,  144,    0,   92,    0,    0,    0,  145,
   93,  146,    0,    0,   94,    0,    0,    0,  147,    0,
  148,    0,    0,    0,    0,    0,   95,    0,   96,    0,
    0,    0,   97,    0,    0,    0,    0,    0,    0,    0,
   98,   99,    0,    0,  100,    0,    0,  117,    0,    0,
    0,    0,  127,   83,    0,   84,    0,    0,   85,  128,
    0,    0,  149,   86,  150,  129,  151,   88,  152,    0,
  153,    0,  154,  130,  647,  156,   91,    0,    0,    0,
  157,    0,    0,   92,    0,    0,    0,    0,   93,    0,
  131,  132,   94,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   95,    0,   96,  133,    0,    0,
   97,    0,    0,  134,    0,  135,    0,  136,   98,   99,
  137,    0,  100,    0,    0,  138,    0,    0,    0,    0,
    0,    0,    0,  139,    0,  127,   83,    0,   84,    0,
    0,   85,  128,    0,    0,    0,   86,    0,  129,    0,
   88,    0,    0,    0,    0,  140,  130,   74,  366,   91,
    0,  141,  142,  143,  144,    0,   92,    0,    0,    0,
  145,   93,  146,  131,  132,   94,    0,    0,    0,  147,
    0,  148,    0,    0,    0,    0,    0,   95,    0,   96,
  133,    0,    0,   97,    0,    0,  134,    0,  135,    0,
  136,   98,   99,  137,    0,  100,    0,    0,  138,    0,
    0,    0,    0,    0,    0,    0,  139,    0,    0,    0,
    0,    0,    0,  149,    0,  150,    0,  151,    0,  152,
    0,  153,    0,  154,    0,  262,  156,    0,  140,  535,
    0,  157,    0,    0,  141,  142,  143,  144,    0,    0,
    0,    0,    0,  145,    0,  146,    0,    0,    0,    0,
    0,    0,  147,    0,  148,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  127,   83,    0,   84,
    0,    0,   85,  128,    0,    0,  149,   86,  150,  129,
  151,   88,  152,    0,  153,    0,  154,  130,  262,  156,
   91,    0,    0,    0,  157,    0,    0,   92,    0,    0,
    0,    0,   93,    0,  131,  132,   94,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,   95,    0,
   96,  133,    0,    0,   97,    0,    0,  134,    0,  135,
    0,  136,   98,   99,  137,    0,  100,    0,    0,  138,
    0,    0,    0,    0,    0,    0,    0,  139,    0,  127,
   83,    0,   84,    0,    0,   85,  128,    0,    0,    0,
   86,    0,  129,    0,   88,  646,    0,    0,    0,  140,
  130,    0,    0,   91,    0,  141,  142,  143,  144,    0,
   92,    0,    0,    0,  145,   93,  146,  131,  132,   94,
    0,    0,    0,  147,    0,  148,    0,    0,    0,    0,
    0,   95,    0,   96,  133,    0,    0,   97,    0,    0,
  134,    0,  135,    0,  136,   98,   99,  137,    0,  100,
    0,    0,  138,    0,    0,    0,    0,    0,    0,    0,
  139,    0,    0,    0,    0,    0,    0,  149,    0,  150,
    0,  151,    0,  152,    0,  153,    0,  154,    0,  647,
  156,  716,  140,    0,    0,  157,    0,    0,  141,  142,
  143,  144,    0,    0,    0,    0,    0,  145,    0,  146,
    0,    0,    0,    0,    0,    0,  147,    0,  148,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  127,   83,    0,   84,    0,    0,   85,  128,    0,    0,
  149,   86,  150,  129,  151,   88,  152,    0,  153,    0,
  154,  130,  262,  156,   91,    0,    0,    0,  157,    0,
    0,   92,    0,    0,    0,    0,   93,    0,  131,  132,
   94,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   95,    0,   96,  133,    0,    0,   97,    0,
    0,  134,    0,  135,    0,  136,   98,   99,  137,    0,
  100,    0,    0,  138,    0,    0,    0,    0,    0,    0,
    0,  139,    0,    0,   83,    0,   84,    0,    0,   85,
    0,    0,    0,    0,   86,    0,    0,    0,   88,    0,
    0,    0,    0,  140,    0,    0,    0,   91,  760,  141,
  142,  143,  144,    0,   92,    0,    0,    0,  145,   93,
  146,    0,    0,   94,    0,    0,    0,  147,    0,  148,
    0,    0,    0,    0,    0,   95,    0,   96,    0,    0,
    0,   97,    0,    0,    0,    0,    0,    0,    0,   98,
   99,    0,    0,  100,    0,    0,  117,    0,    0,    0,
    0,  127,   83,    0,   84,    0,    0,   85,  128,    0,
    0,  149,   86,  150,  129,  151,   88,  152,    0,  153,
    0,  154,  130,  262,  156,   91,    0,    0,    0,  157,
    0,    0,   92,    0,    0,    0,    0,   93,    0,  131,
  132,   94,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,   95,    0,   96,  133,    0,    0,   97,
    0,    0,  134,    0,  135,    0,  136,   98,   99,  137,
    0,  100,    0,    0,  138,    0,    0,    0,    0,    0,
    0,    0,  139,    0,  127,   83,    0,   84,    0,    0,
   85,  128,    0,    0,    0,   86,    0,  129,    0,   88,
    0,  778,    0,    0,  140,  130,  260,    0,   91,    0,
  141,  142,  143,  144,    0,   92,    0,    0,    0,  145,
   93,  146,  131,  132,   94,    0,    0,    0,  147,    0,
  148,    0,    0,    0,    0,    0,   95,    0,   96,  133,
    0,    0,   97,    0,    0,  134,    0,  135,    0,  136,
   98,   99,  137,    0,  100,    0,    0,  138,    0,    0,
    0,    0,    0,    0,    0,  139,    0,    0,    0,    0,
    0,    0,  149,    0,  150,    0,  151,    0,  152,    0,
  153,    0,  154,  453,  262,  156,    0,  140,    0,    0,
  157,    0,    0,  141,  142,  143,  144,    0,    0,    0,
    0,    0,  145,    0,  146,    0,    0,    0,    0,    0,
    0,  147,    0,  148,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  127,   83,    0,   84,    0,
    0,   85,  128,    0,    0,  149,   86,  150,  129,  151,
   88,  152,    0,  153,    0,  154,  130,  262,  156,   91,
    0,    0,    0,  157,    0,    0,   92,    0,    0,    0,
    0,   93,    0,  131,  132,   94,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,   95,    0,   96,
  133,    0,    0,   97,    0,    0,  134,    0,  135,    0,
  136,   98,   99,  137,    0,  100,    0,    0,  138,    0,
    0,    0,    0,    0,    0,    0,  139,    0,  127,   83,
    0,   84,    0,    0,   85,  128,    0,    0,    0,   86,
    0,  129,    0,   88,    0,    0,    0,    0,  140,  130,
    0,    0,   91,    0,  141,  142,  143,  144,    0,   92,
    0,    0,    0,  145,   93,  146,  131,  132,   94,    0,
    0,    0,  147,    0,  148,    0,    0,    0,    0,    0,
   95,    0,   96,  133,    0,    0,   97,    0,    0,  134,
    0,  135,    0,  136,   98,   99,  137,    0,  100,    0,
    0,  138,    0,    0,    0,    0,    0,    0,    0,  139,
    0,    0,    0,    0,    0,    0,  149,    0,  150,    0,
  151,    0,  152,    0,  153,    0,  154,    0,  155,  156,
    0,  140,    0,    0,  157,    0,    0,  141,  142,  143,
  144,    0,    0,    0,    0,    0,  145,    0,  146,    0,
    0,    0,    0,    0,    0,  147,    0,  148,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  127,
   83,    0,   84,    0,    0,   85,  128,    0,    0,  149,
   86,  150,  129,  151,   88,  152,    0,  153,    0,  154,
  130,  262,  156,   91,    0,    0,    0,  157,    0,    0,
   92,    0,    0,    0,    0,   93,    0,  131,  132,   94,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   95,    0,   96,  133,    0,    0,   97,    0,    0,
  134,    0,  135,    0,  136,   98,   99,  137,    0,  100,
    0,    0,  394,    0,    0,    0,    0,    0,    0,    0,
  139,    0,  600,  600,    0,  600,    0,    0,  600,  600,
    0,    0,    0,  600,    0,  600,    0,  600,    0,    0,
    0,    0,  140,  600,    0,    0,  600,    0,  141,  142,
  143,  144,    0,  600,    0,    0,    0,  145,  600,  146,
  600,  600,  600,    0,    0,    0,  147,    0,  148,    0,
    0,    0,    0,    0,  600,    0,  600,  600,    0,    0,
  600,    0,    0,  600,    0,  600,    0,  600,  600,  600,
  600,    0,  600,    0,    0,  600,    0,    0,    0,    0,
    0,    0,    0,  600,    0,    0,    0,    0,    0,    0,
  149,    0,  150,    0,  151,    0,  152,    0,  153,    0,
  154,    0,  262,  156,    0,  600,    0,    0,  157,    0,
    0,  600,  600,  600,  600,    0,    0,    0,    0,    0,
  600,    0,  600,    0,    0,    0,    0,    0,    0,  600,
    0,  600,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  127,   83,    0,   84,    0,    0,   85,
  128,    0,    0,  600,   86,  600,  129,  600,   88,  600,
    0,  600,    0,  600,  130,  600,  600,   91,    0,    0,
    0,  600,    0,    0,   92,    0,    0,    0,    0,   93,
    0,  131,  132,   94,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   95,    0,   96,  133,    0,
    0,   97,    0,    0,  134,    0,  135,    0,  136,   98,
   99,  137,    0,  100,    0,    0,  138,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  127,   83,    0,   84,
    0,    0,   85,  128,    0,    0,    0,   86,    0,  129,
    0,   88,    0,    0,    0,    0,  140,  130,    0,    0,
   91,    0,  141,  142,  143,  144,    0,   92,    0,    0,
    0,  145,   93,  146,  131,  132,   94,    0,    0,    0,
  147,    0,  148,    0,    0,    0,    0,    0,   95,    0,
   96,  133,    0,    0,   97,    0,    0,  134,    0,  135,
    0,  136,   98,   99,  137,    0,  100,    0,    0,  138,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  149,    0,  150,    0,  151,    0,
  152,    0,  153,    0,  154,    0,  267,    0,    0,  140,
    0,    0,  157,    0,    0,  141,  142,  143,  144,    0,
    0,    0,    0,    0,  145,    0,    0,    0,    0,    0,
    0,    0,    0,  147,    0,  148,  127,   83,    0,   84,
    0,    0,   85,  128,    0,    0,    0,   86,    0,  129,
    0,   88,    0,    0,    0,    0,    0,  130,    0,    0,
   91,    0,    0,    0,    0,    0,    0,   92,    0,    0,
    0,    0,   93,    0,  131,  132,   94,  149,    0,  150,
    0,  151,    0,  152,    0,  153,    0,  154,   95,  267,
   96,  133,    0,    0,   97,  157,    0,  134,    0,  135,
    0,  136,   98,   99,  137,    0,  100,    0,    0,  138,
    0,  127,   83,    0,   84,    0,    0,   85,  128,    0,
    0,    0,   86,    0,  129,    0,   88,    0,    0,    0,
    0,    0,  130,    0,    0,   91,    0,    0,    0,  140,
    0,    0,   92,    0,    0,  141,    0,   93,  144,  131,
  132,   94,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,   95,    0,   96,  133,    0,    0,   97,
    0,    0,  134,    0,  135,    0,  136,   98,   99,  137,
    0,  100,    0,    0,  138,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  149,    0,  150,
    0,  151,    0,  152,  503,  153,    0,  154,    0,  267,
    0,    0,    0,    0,    0,  157,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  127,   83,    0,   84,    0,    0,   85,  128,    0,
    0,    0,   86,    0,  129,    0,   88,    0,    0,    0,
    0,    0,  130,    0,    0,   91,    0,    0,    0,    0,
    0,    0,   92,    0,    0,    0,    0,   93,    0,  131,
  132,   94,  149,    0,  150,    0,  151,    0,  152,    0,
  153,    0,  154,   95,  267,   96,  133,    0,    0,   97,
  157,    0,  134,    0,  135,    0,  136,   98,   99,  137,
    0,  100,  127,   83,  138,   84,    0,    0,   85,  128,
    0,    0,    0,   86,    0,  129,    0,   88,    0,    0,
    0,    0,    0,  130,    0,    0,   91,    0,    0,    0,
    0,    0,    0,   92,  445,    0,    0,    0,   93,    0,
  131,  132,   94,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   95,    0,   96,  133,    0,    0,
   97,    0,    0,  134,    0,  135,    0,  136,   98,   99,
  137,    0,  100,    0,    0,  138,    0,    0,    0,  350,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  149,  350,  150,  776,  151,    0,  152,    0,
  153,    0,  154,    0,  267,    0,  350,   53,    0,   53,
  157,  350,    0,    0,  350,    0,  350,    0,  350,  350,
  350,  350,    0,    0,    0,    0,  350,    0,    0,    0,
  350,   53,    0,    0,  350,    0,    0,    0,    0,    0,
    0,    0,  350,    0,   53,  350,    0,  350,    0,   53,
    0,    0,    0,    0,   53,    0,   53,   53,   53,   53,
    0,    0,   53,  149,   53,  150,    0,  151,   53,  152,
    0,  153,    0,  154,  350,  267,  289,   54,  350,   54,
   53,  157,   54,   53,   54,   53,    0,   54,    0,   54,
   54,    0,   54,  350,   54,    0,   54,  350,   54,   54,
   54,   54,    0,    0,   54,   54,    0,    0,    0,    0,
   54,  311,   54,   54,   54,    0,    0,   54,   54,   54,
    0,   54,    0,   54,   54,   54,   54,   54,   54,   54,
   54,    0,   54,   54,   54,   54,    0,    0,   54,   54,
   54,    0,   54,    0,    0,    0,    0,   54,   54,    0,
   54,   54,    0,   54,   54,   54,  350,  289,    0,   54,
    0,   53,    0,    0,    0,    0,   53,    0,   53,    0,
    0,   53,    0,   53,   53,   54,   53,   54,   53,    0,
   53,    0,   53,   53,   53,   53,    0,    0,   53,   53,
   54,    0,    0,    0,   53,    0,   53,   53,   53,    0,
    0,   53,    0,   53,    0,   53,    0,    0,   53,    0,
   53,   53,   53,   53,    0,    0,    0,   53,   53,   53,
    0,    0,   53,   53,   53,    0,    0,    0,    0,    0,
    0,   53,   53,    0,   53,   53,    0,   53,   53,   53,
    0,    0,    0,   53,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,   53,    0,    0,    0,
    0,   53,    0,   53,   54,   82,   53,    0,   53,   53,
    0,   53,    0,   53,   53,   53,    0,   53,   53,   53,
   53,    0,    0,   53,   53,    0,    0,    0,    0,   53,
    0,   53,   53,   53,    0,    0,   53,    0,   53,    0,
   53,    0,    0,   53,    0,   53,   53,   53,   53,    0,
    0,    0,   53,   53,   53,    0,    0,   53,   53,   53,
    0,    0,    0,    0,    0,    0,   53,   53,    0,   53,
   53,    0,   53,   53,   53,    0,    0,    0,   53,    0,
   53,    0,    0,    0,    0,   53,    0,   53,   53,    0,
   53,    0,   53,   53,    0,   53,    0,   53,    0,   53,
   83,   53,   53,   53,   53,    0,    0,   53,   53,   53,
    0,    0,    0,   53,    0,   53,   53,   53,    0,    0,
   53,    0,   53,    0,   53,    0,    0,   53,    0,   53,
   53,   53,   53,    0,    0,    0,   53,   53,   53,    0,
    0,   53,   53,   53,    0,    0,    0,    0,    0,    0,
   53,   53,    0,   53,   53,    0,   53,   53,   53,    0,
    0,    0,   53,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   53,    0,    0,    0,    0,
   53,    0,   53,   53,  104,   53,    0,   53,   53,    0,
   53,    0,   53,   53,   53,    0,   53,   53,   53,   53,
    0,    0,   53,   53,    0,    0,    0,    0,   53,    0,
   53,   53,   53,    0,    0,   53,    0,   53,    0,   53,
    0,    0,   53,    0,   53,   53,   53,   53,    0,    0,
    0,   53,   53,   53,    0,    0,   53,   53,   53,    0,
    0,    0,    0,    0,    0,   53,   53,    0,   53,   53,
    0,   53,   53,   53,    0,    0,    0,   53,    0,  636,
    0,    0,    0,    0,  636,    0,  636,   53,    0,  636,
    0,  636,  636,    0,  636,    0,  636,    0,  636,  105,
  636,  636,  636,  636,    0,    0,  636,  636,   53,    0,
    0,    0,  636,    0,  636,  636,  636,    0,    0,  636,
    0,  636,    0,  636,    0,    0,  636,    0,  636,  636,
  636,  636,    0,    0,    0,  636,  636,  636,    0,    0,
  636,  636,  636,    0,    0,    0,    0,    0,    0,  636,
  636,    0,  636,  636,    0,  636,  636,  636,    0,    0,
    0,  636,  638,    0,    0,    0,    0,  638,    0,  638,
    0,    0,  638,    0,  638,  638,    0,  638,    0,  638,
    0,  638,   53,  638,  638,  638,  638,    0,    0,  638,
  638,    0,  298,    0,    0,  638,    0,  638,  638,  638,
    0,    0,  638,    0,  638,    0,  638,    0,    0,  638,
    0,  638,  638,  638,  638,    0,    0,    0,  638,  638,
  638,    0,    0,  638,  638,  638,    0,    0,    0,    0,
    0,    0,  638,  638,    0,  638,  638,    0,  638,  638,
  638,   53,    0,    0,  638,    0,   53,    0,   53,    0,
    0,   53,    0,   53,   53,    0,   53,    0,   53,    0,
   53,    0,   53,   53,    0,   53,  636,    0,    0,   53,
    0,    0,    0,    0,    0,  297,   53,   53,   53,    0,
    0,   53,    0,   53,    0,   53,    0,    0,   53,    0,
   53,   53,   53,   53,    0,    0,    0,   53,   53,   53,
    0,    0,   53,   53,   53,    0,    0,    0,    0,    0,
    0,   53,   53,    0,   53,   53,    0,   53,   53,   53,
   53,    0,    0,   53,    0,   53,  352,   53,    0,    0,
   53,    0,   53,   53,    0,   53,    0,   53,    0,   53,
    0,   53,   53,    0,   53,  214,    0,    0,   53,  638,
    0,    0,    0,    0,    0,   53,   53,   53,    0,    0,
   53,    0,   53,  352,   53,    0,    0,   53,    0,   53,
   53,   53,   53,    0,    0,    0,   53,   53,   53,    0,
    0,   53,   53,   53,    0,    0,    0,    0,    0,    0,
   53,   53,  527,   53,   53,    0,   53,   53,   53,    0,
    0,    0,   53,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  215,    0,    0,    0,   53,  527,
  352,    0,  352,    0,  352,    0,    0,  352,    0,  352,
  352,    0,  352,  352,    0,  352,    0,  352,  352,  352,
  352,  352,  352,  352,    0,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,  527,
    0,  527,    0,  527,    0,  527,  527,   53,  527,  527,
    0,  527,  352,  527,  527,    0,  527,  527,  527,    0,
    0,    0,    0,    0,    0,    0,  527,    0,  527,    0,
  527,    0,  527,    0,  527,    0,  527,    0,  527,    0,
  527,    0,  527,    0,  527,    0,  527,    0,  527,    0,
  527,    0,  527,    0,  527,    0,  527,    0,  527,    0,
  527,    0,    0,  622,  527,  622,    0,    0,  622,    0,
  622,  622,    0,  622,    0,  622,    0,  622,  492,  622,
  622,  622,    0,    0,    0,  622,  622,    0,    0,    0,
    0,  622,    0,  622,  622,    0,    0,    0,  622,    0,
    0,    0,  622,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  622,    0,  622,    0,    0,    0,
  622,  622,    0,    0,    0,    0,    0,    0,  622,  622,
    0,  621,  622,  621,    0,  622,  621,    0,  621,  621,
  622,  621,    0,  621,    0,  621,    0,  621,  621,  621,
    0,    0,    0,  621,  621,    0,  622,    0,  622,  621,
    0,  621,  621,   83,    0,   84,  621,    0,   85,    0,
  621, 1123,    0,   86,    0,   87,    0,   88,    0,   89,
 1124, 1125,  621,    0,  621,   90,   91,    0,  621,  621,
    0, 1126,    0,   92,    0,    0,  621,  621,   93,    0,
  621,    0,   94,  621,    0,    0,    0,    0,  621,    0,
    0,    0,    0,    0,   95,    0,   96,    0,    0,    0,
   97,    0,    0,    0,    0,    0,    0,    0,   98,   99,
    0,    0,  100,    0,    0,  101,    0,    0,    0,  296,
  102,    0,  621,    0,  621,  622,    0,  621,    0,  621,
  621,    0,  621,    0,  621,    0,  621,    0,  621,  621,
    0,    0,    0,    0,    0,  621,    0,    0,    0,    0,
    0,    0,  621,  621,    0,    0,    0,  621,    0,    0,
    0,  621,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  621,    0,  621,    0,    0,    0,  621,
  621,    0,    0,    0,    0,    0,    0,  621,  621,    0,
    0,  621,    0,  621,  621,    0,  621,    0,  621,  621,
    0,  621,    0,  621,    0,    0,  621,    0,  621,    0,
  621,    0,  621,    0,    0,    0,    0,    0,  621,  621,
    0,    0,    0,    0,    0, 1127,  621,  621,    0,    0,
    0,  621,    0,    0,    0,  621,  161,    0,  161,    0,
    0,  161,    0,    0,    0,    0,  161,  621,    0,  621,
  161,    0,    0,  621,  621,    0,    0,    0,    0,  161,
    0,  621,  621,    0,    0,  621,  161,    0,  621,    0,
    0,  161,    0,  621,    0,  161,    0,  161,    0,  161,
    0,    0,    0,    0,  161,    0,    0,  161,    0,  161,
    0,    0,    0,  161,    0,    0,  161,    0,    0,    0,
    0,  161,  161,    0,  621,  161,    0,    0,  161,    0,
    0,  161,  161,  161,    0,    0,  161,    0,    0,    0,
    0,  161,    0,    0,    0,  161,    0,    0,    0,    0,
    0,    0,    0,    0,  161,    0,  161,  160,    0,    0,
    0,  161,    0,    0,    0,    0,  161,    0,    0,    0,
  161,    0,  161,    0,  161,    0,   53,    0,   53,  161,
    0,   53,  161,    0,  161,    0,   53,    0,  161,    0,
   53,  161,    0,    0,    0,    0,  161,  161,  621,   53,
  161,    0,    0,  161,    0,    0,   53,  161,    0,    0,
    0,   53,    0,    0,    0,   53,    0,   53,    0,   53,
    0,    0,    0,    0,   53,    0,    0,   53,    0,   53,
    0,  161,    0,   53,  160,    0,   53,    0,  161,    0,
    0,   53,   53,    0,    0,   53,    0,    0,   53,    0,
   83,    0,   84,    0,    0,   85,    0,    0,    0,    0,
   86,    0,   87,    0,   88,    0,   89,    0,    0,    0,
    0,    0,   90,   91,    0,    0,    0,    0,    0,  158,
   92,    0,   83,    0,   84,   93,    0,   85,    0,   94,
    0,    0,   86,    0,    0,    0,   88,    0,    0,    0,
    0,   95,    0,   96,    0,   91,    0,   97,    0,    0,
    0,    0,   92,  161,    0,   98,   99,   93,    0,  100,
    0,   94,  101,    0,    0,    0,   83,  102,   84,    0,
    0,   85,    0,   95,    0,   96,   86,    0,    0,   97,
   88,    0,    0,    0,    0,    0,    0,   98,   99,   91,
   83,  100,   84,    0,  117,   85,   92,    0,   53,    0,
   86,   93,    0,    0,   88,   94,    0,    0,    0,    0,
    0,    0,    0,   91,    0,    0,    0,   95,    0,   96,
   92,    0,    4,   97,    0,   93,    0,    0,    0,   94,
    0,   98,   99,    0,   83,  100,   84,    0,  117,   85,
    0,   95,    0,   96,   86,    0,    0,   97,   88,    0,
    0,    0,    0,    0,    0,   98,   99,   91,    0,  100,
    0,    0,  117,    0,   92,    0,  179,    0,  179,   93,
    0,  179,   74,   94,    0,    0,  179,    0,    0,    0,
  179,    0,    0,    0,    0,   95,    0,   96,    0,  179,
    0,   97,    0,    0,    0,    0,  179,    0,    0,   98,
   99,  179,    0,  100,   74,  179,  117,    0,    0,    0,
  188,    0,  188,    0,    0,  188,    0,  179,    0,  179,
  188,    0,    0,  179,  188,    0,    0,    0,    0,    0,
    0,  179,  179,  188,    0,  179,    0,    0,  179,    0,
  188,  576,    0,    0,    0,  188,    0,    0,  659,  188,
    0,    0,    0,    0,  180,    0,  180,    0,    0,  180,
    0,  188,    0,  188,  180,    0,    0,  188,  180,    0,
    0,    0,  696,    0,    0,  188,  188,  180,    0,  188,
    0,    0,  188,    0,  180,  527,    0,    0,    0,  180,
    0,  527,    0,  180,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  180,    0,  180,    0,    0,
    0,  180,    0,    0,    0,    0,  698,    0,    0,  180,
  180,    0,    0,  180,    0,  576,  180,    0,  527,    0,
    0,  576,  576,  576,  576,  576,  576,  576,  576,  576,
  576,  576,  576,    0,    0,    0,    0,    0,  179,    0,
  576,  576,  576,    0,  576,    0,  576,  576,  576,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  527,
  576,    0,    0,    0,  576,  527,  527,  527,  527,  527,
  527,  527,  527,  527,  527,  527,  527,  543,    0,    0,
    0,    0,  188,  543,  527,  576,  527,    0,  527,    0,
  527,  527,  527,    0,  527,  527,    0,  527,  527,    0,
  527,    0,  527,  527,  527,  527,  527,  527,  527,  576,
    0,    0,    0,    0,    0,  527,    0,  527,    0,  527,
  543,  527,    0,  527,    0,  527,  180,  527,    0,  527,
    0,  527,    0,  527,    0,  527,    0,  527,    0,  527,
    0,  527,    0,  527,    0,  527,    0,  527,  547,  527,
    0,    0,    0,  527,  547,    0,    0,    0,    0,    0,
    0,  543,    0,    0,    0,    0,    0,  543,  543,  543,
  543,  543,  543,  543,  543,  543,  543,  543,  543,    0,
    0,    0,    0,    0,    0,    0,  543,  543,  543,    0,
  543,  547,  543,  543,  543,    0,  543,  543,    0,    0,
  543,    0,  543,    0,  543,  543,  543,  543,  543,  543,
  543,    0,    0,    0,    0,    0,    0,  543,    0,  543,
    0,  543,    0,  543,    0,  543,    0,  543,    0,  543,
    0,  543,  547,    0,    0,    0,    0,    0,  547,  547,
  547,  547,  547,  547,  547,  547,  547,  547,  547,  547,
  548,    0,    0,    0,    0,  543,  548,  547,  547,  547,
    0,  547,    0,  547,  547,  547,    0,  547,  547,    0,
    0,  547,    0,  547,    0,  547,  547,    0,    0,    0,
  547,  547,    0,    0,    0,    0,    0,    0,  547,    0,
  547,    0,  547,  548,  547,    0,  547,    0,  547,    0,
  547,    0,  547,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  549,    0,    0,    0,    0,  547,  549,    0,    0,
    0,    0,    0,    0,  548,    0,    0,    0,    0,    0,
  548,  548,  548,  548,  548,  548,  548,  548,  548,  548,
  548,  548,    0,    0,    0,    0,    0,    0,    0,  548,
  548,  548,    0,  548,  549,  548,  548,  548,    0,  548,
  548,    0,    0,  548,    0,  548,    0,  548,  548,    0,
    0,    0,  548,  548,    0,    0,    0,    0,    0,    0,
  548,    0,  548,    0,  548,    0,  548,    0,  548,    0,
  548,    0,  548,    0,  548,  549,    0,    0,    0,    0,
    0,  549,  549,  549,  549,  549,  549,  549,  549,  549,
  549,  549,  549,  550,    0,    0,    0,    0,  548,  550,
  549,  549,  549,    0,  549,    0,  549,  549,  549,    0,
  549,  549,    0,    0,  549,    0,  549,    0,  549,  549,
    0,    0,    0,  549,  549,    0,    0,    0,    0,    0,
    0,  549,    0,  549,    0,  549,  550,  549,    0,  549,
    0,  549,    0,  549,    0,  549,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  551,    0,    0,    0,    0,  549,
  551,    0,    0,    0,    0,    0,    0,  550,    0,    0,
    0,    0,    0,  550,  550,  550,  550,  550,  550,  550,
  550,  550,  550,  550,  550,    0,    0,    0,    0,    0,
    0,    0,  550,  550,  550,    0,  550,  551,  550,  550,
  550,    0,    0,    0,    0,    0,  550,    0,  550,    0,
  550,  550,  550,    0,    0,  550,  550,    0,    0,    0,
    0,    0,    0,  550,    0,  550,    0,  550,    0,  550,
  552,  550,    0,  550,    0,  550,  552,  550,  551,    0,
    0,    0,    0,    0,  551,  551,  551,  551,  551,  551,
  551,  551,  551,  551,  551,  551,    0,    0,    0,    0,
    0,  550,    0,  551,  551,  551,    0,  551,    0,  551,
  551,  551,    0,  552,    0,    0,    0,  551,    0,  551,
    0,  551,  551,  551,    0,    0,  551,  551,    0,    0,
    0,    0,    0,    0,  551,    0,  551,    0,  551,    0,
  551,  556,  551,    0,  551,    0,  551,  556,  551,    0,
    0,    0,    0,    0,  552,    0,    0,    0,    0,    0,
  552,  552,  552,  552,  552,  552,  552,  552,  552,  552,
  552,  552,  551,    0,    0,    0,    0,    0,    0,  552,
  552,  552,    0,  552,  556,  552,  552,  552,    0,    0,
    0,    0,    0,  552,    0,  552,    0,  552,  552,  552,
    0,    0,  552,  552,    0,    0,    0,    0,    0,    0,
  552,    0,  552,    0,  552,    0,  552,  557,  552,    0,
  552,    0,  552,  557,  552,  556,    0,    0,    0,    0,
    0,  556,  556,  556,  556,  556,  556,  556,  556,  556,
  556,  556,  556,    0,    0,    0,    0,    0,  552,    0,
  556,  556,  556,    0,  556,    0,  556,  556,  556,    0,
  557,    0,    0,    0,  556,    0,  556,    0,  556,  556,
  556,    0,    0,  556,  556,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  556,    0,  556,  558,  556,
    0,  556,    0,  556,  558,  556,    0,    0,    0,    0,
    0,  557,    0,    0,    0,    0,    0,  557,  557,  557,
  557,  557,  557,  557,  557,  557,  557,  557,  557,  556,
    0,    0,    0,    0,    0,    0,  557,  557,  557,    0,
  557,  558,  557,  557,  557,    0,    0,    0,    0,    0,
  557,    0,  557,    0,  557,  557,  557,    0,    0,  557,
  557,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  557,    0,  557,  559,  557,    0,  557,    0,  557,
  559,  557,  558,    0,    0,    0,    0,    0,  558,  558,
  558,  558,  558,  558,  558,  558,  558,  558,  558,  558,
    0,    0,    0,    0,    0,  557,    0,  558,  558,  558,
    0,  558,    0,  558,  558,  558,    0,  559,    0,    0,
    0,  558,    0,  558,    0,  558,  558,  558,    0,    0,
  558,  558,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  558,    0,  558,  560,  558,    0,  558,    0,
  558,  560,  558,    0,    0,    0,    0,    0,  559,    0,
    0,    0,    0,    0,  559,  559,  559,  559,  559,  559,
  559,  559,  559,  559,  559,  559,  558,    0,    0,    0,
    0,    0,    0,  559,  559,  559,    0,  559,  560,  559,
  559,  559,    0,    0,    0,    0,    0,  559,    0,  559,
    0,  559,  559,  559,    0,    0,  559,  559,    0,    0,
    0,    0,    0,    0,    0,    0,  565,    0,  559,    0,
  559,    0,  559,    0,  559,    0,  559,    0,  559,  560,
    0,    0,    0,    0,    0,  560,  560,  560,  560,  560,
  560,  560,  560,  560,  560,  560,  560,    0,    0,    0,
    0,    0,  559,    0,  560,  560,  560,    0,  560,    0,
  560,  560,  560,    0,    0,    0,    0,    0,  560,    0,
  560,    0,  560,  560,  560,    0,    0,  560,  560,    0,
    0,    0,    0,    0,    0,    0,    0,  566,    0,  560,
    0,  560,    0,  560,    0,  560,    0,  560,    0,  560,
  565,    0,    0,    0,    0,    0,  565,  565,  565,  565,
  565,  565,  565,  565,  565,  565,  565,  565,    0,    0,
    0,    0,    0,  560,    0,  565,  565,  565,    0,  565,
    0,  565,  565,  565,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  565,  565,  565,    0,    0,  565,  565,
    0,    0,    0,    0,    0,    0,    0,    0,  567,    0,
    0,    0,    0,    0,  565,    0,  565,    0,  565,    0,
  565,  566,    0,    0,    0,    0,    0,  566,  566,  566,
  566,  566,  566,  566,  566,  566,  566,  566,  566,    0,
    0,    0,    0,    0,  565,    0,  566,  566,  566,    0,
  566,    0,  566,  566,  566,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  566,  566,  566,    0,    0,  566,
  566,    0,    0,    0,    0,    0,    0,    0,    0,  568,
    0,    0,    0,    0,    0,  566,    0,  566,    0,  566,
    0,  566,  567,    0,    0,    0,    0,    0,  567,  567,
  567,  567,  567,  567,  567,  567,  567,  567,  567,  567,
    0,    0,    0,    0,    0,  566,    0,  567,  567,  567,
    0,  567,    0,  567,  567,  567,  569,    0,    0,    0,
    0,    0,    0,    0,    0,  567,  567,  567,    0,    0,
  567,  567,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  567,    0,  567,    0,
  567,    0,  567,  568,    0,    0,    0,    0,    0,  568,
  568,  568,  568,  568,  568,  568,  568,  568,  568,  568,
  568,    0,    0,    0,    0,    0,  567,    0,  568,  568,
  568,    0,  568,    0,  568,  568,  568,  570,    0,    0,
    0,    0,    0,    0,    0,    0,  568,  568,  568,    0,
  569,  568,  568,    0,    0,    0,  569,  569,  569,  569,
  569,  569,  569,  569,  569,  569,  569,  569,    0,    0,
    0,  568,    0,  568,    0,  569,  569,  569,    0,  569,
    0,  569,  569,  569,  571,    0,    0,    0,    0,    0,
    0,    0,    0,  569,  569,  569,    0,  568,  569,  569,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  569,    0,
  569,  570,    0,    0,    0,    0,    0,  570,  570,  570,
  570,  570,  570,  570,  570,  570,  570,  570,  570,    0,
    0,    0,    0,    0,  569,    0,  570,  570,  570,    0,
  570,    0,  570,  570,  570,  572,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  570,  570,    0,  571,  570,
  570,    0,    0,    0,  571,  571,  571,  571,  571,  571,
  571,  571,  571,  571,  571,  571,    0,    0,    0,  570,
    0,  570,    0,  571,  571,  571,    0,  571,    0,  571,
  571,  571,  573,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  571,  571,    0,  570,  571,  571,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  571,    0,  571,  572,
    0,    0,    0,    0,    0,  572,  572,  572,  572,  572,
  572,  572,  572,  572,  572,  572,  572,    0,    0,    0,
    0,    0,  571,    0,  572,  572,  572,    0,  572,    0,
  572,  572,  572,  574,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  572,  572,    0,  573,    0,  572,    0,
    0,    0,  573,  573,  573,  573,  573,  573,  573,  573,
  573,  573,  573,  573,    0,    0,    0,  572,    0,  572,
    0,  573,  573,  573,    0,  573,    0,  573,  573,  573,
  575,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  573,  573,    0,  572,    0,  573,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  573,    0,  573,  574,    0,    0,
    0,    0,    0,  574,  574,  574,  574,  574,  574,  574,
  574,  574,  574,  574,  574,    0,    0,    0,    0,    0,
  573,    0,  574,  574,  574,    0,  574,    0,  574,  574,
  574,  577,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  574,    0,  575,    0,  574,    0,    0,    0,
  575,  575,  575,  575,  575,  575,  575,  575,  575,  575,
  575,  575,    0,    0,    0,  574,    0,  574,  547,  575,
  575,  575,    0,  575,  547,  575,  575,  575,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  575,
    0,  574,    0,  575,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  547,  575,    0,  575,  577,    0,  352,    0,    0,
    0,  577,  577,  577,  577,  577,  577,  577,  577,  577,
  577,  577,  577,    0,    0,    0,    0,    0,  575,    0,
  577,  577,  577,    0,  577,    0,  577,  577,  577,    0,
    0,    0,    0,    0,  352,    0,    0,    0,    0,    0,
  577,    0,    0,    0,  577,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  547,    0,
    0,  547,    0,  547,    0,  577,    0,  547,  547,    0,
    0,  547,    0,  547,    0,  547,  547,    0,    0,    0,
  547,  547,    0,    0,    0,    0,    0,    0,  547,  577,
  547,    0,  547,  553,  547,    0,  547,    0,  547,    0,
  547,  352,  547,  352,  352,  352,  352,    0,    0,    0,
  352,  352,    0,    0,  352,    0,  352,    0,  352,  352,
  352,  352,  352,  352,  352,    0,  547,  352,    0,  352,
  553,  352,    0,  352,    0,  352,    0,  352,    0,  352,
    0,  352,    0,  352,    0,  352,    0,  352,    0,  352,
    0,  352,    0,  352,  352,  352,    0,  352,    0,  352,
    0,  352,    0,  352,    0,  352,    0,  352,    0,  352,
    0,  553,    0,    0,    0,    0,    0,  553,  553,  553,
  553,  553,  553,  553,  553,  553,  553,  553,  553,    0,
    0,  352,    0,    0,    0,    0,  553,    0,  553,    0,
  553,    0,  553,  553,  553,    0,    0,    0,    0,    0,
  553,    0,  553,    0,  553,  553,    0,    0,    0,  553,
  553,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  553,    0,  553,    0,  553,    0,  553,    0,  553,
    0,  553,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  352,  352,    0,
  352,    0,  352,  352,    0,  553,    0,  352,  352,    0,
    0,  352,    0,  352,    0,  352,  352,  352,  352,  352,
  352,  352,    0,  389,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,    0,    0,    0,  389,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,   53,    0,
  389,    0,    0,    0,  352,  389,  352,    0,  389,    0,
  389,    0,  389,  389,  389,  389,    0,    0,    0,    0,
  389,    0,   53,    0,  389,    0,    0,    0,  389,    0,
    0,    0,    0,   53,    0,   53,  389,    0,    0,  389,
   53,  389,    0,    0,    0,   53,    0,   53,   53,   53,
   53,    0,    0,    0,    0,   53,    0,   53,    0,   53,
    0,    0,    0,   53,    0,   53,    0,    0,  389,    0,
   53,   53,    0,    0,   53,   53,   53,    0,    0,    0,
   53,    0,   53,   53,   53,   53,    0,   53,   53,    0,
   53,  389,    0,   53,   53,    0,   53,    0,   53,    0,
   53,    0,  198,    0,    0,   53,   53,    0,    0,   53,
   53,   53,   53,   53,   53,   53,    0,   53,    0,    0,
   53,    0,    0,   47,   53, 1288,    0,    0,    0,    0,
   53,    0,    0,    0,    0,   53,   53,  308,    0,   53,
   53,   53,   53,   53,   53,   53,    0,   48,    0,    0,
   53,    0,    0,   47,   53, 1312,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   53,  309,    0,   53,
   52,   53,   53,   54,   55,   56,    0,   48, 1289,    0,
   57,    0,    0,   47,   58,    0,    0,    0,    0,    0,
   49,   53,    0,   53,    0,   51,   59,    0,    0,   60,
   52,   61,   53,   54,   55,   56,    0,   48, 1313,    0,
   57,    0,    0,   47,   58, 1288,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   59,    0,    0,   60,
   52,   61,   53,   54,   55,   56,    0,   48, 1289,    0,
   57,    0,    0,   47,   58,    0,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   59,    0,    0,   60,
   52,   61,   53,   54,   55,   56,    0,   48,    0,    0,
   57,    0,    0,   47,   58, 1312,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   59,    0,    0,   60,
   52,   61,   53,   54,   55,   56,    0,   48, 1313,    0,
   57,    0,    0,   47,   58,    0,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   59,    0,    0,   60,
   52,   61,   53,   54,   55,   56,    0,   48,    0,    0,
   57,    0,    0,   47,   58,    0,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   59,    0,    0,   60,
   52,   61,   53,   54,   55,   56,    0, 1088,    0,    0,
   57,    0,    0,    0,   58,    0,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   59,    0,    0,   60,
   52,   61,   53,   54,   55,   56,    0,    0,    0,    0,
   57,    0,    0,    0,   58,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1089,    0,    0,   60,
    0,   61,
  };
  protected static  short [] yyCheck = {            63,
    0,   62,  105,   66,  101,   75,    6,  455,   69,  236,
  236,    3,    4,  236,  403,   25,  140,  556,  236,  254,
  256,  453,  297,  217,  458,  103,   87,  593,  106,   90,
  122,  123,  256,   79,  573,  574,  254,  830,  831,  254,
  715,  580,  581,  503,  598,  503,   38,  269,  256,  256,
  295, 1042,  834,  845,  608,  553, 1205,  118,   50,  269,
  402,  269,    0,  256,  856,  256, 1057,  277,    6,  631,
  131,  256,  256,  256,  683,  369,   68,  380,  139,  140,
  819,  423, 1012,  256,   76,  365,  131,  326,  366,  307,
  256,  368,  283,  370,  140,  156,  157, 1012,  256,  365,
  448,  256,  326,  385,  256,  101,  213,  385,  118,  101,
 1072,  904,  340,  105,  256,  712, 1012,  910,  911,  256,
  366,  366,  167,  380,  315,  256,  256,  371,  921,  256,
  448,   69,  378,  390,  380, 1065,  364,  221,  382,  256,
  385,  933,  366,  365,  390,  373,  938,  939,  232, 1111,
 1065,  257,  455,  326,  285,  366,  234,  451,  452,  453,
  378,  385,  380,  378,  903,  359,  448,  231,  448, 1065,
  448,  256,  449,  366,  256,  236,  403,  403,  239,  454,
  403,  366,  448,  366, 1333,  403,  264,  371,  233,  981,
  799,  262,  385,  366,  256,  987,  257,  321,  322,  245,
  385,  247,  385,  448,  766,  371,  768,  641,  366,  448,
  255,  366,  710,  259,  366,  997,  389,  278,  263,  256,
  456,  818,  680,  256,  448,  286,  222,  385,  299,  371,
  222,  277,  369,  385,  456,  231,  366,  448,  448,  231,
 1022,  448, 1233,  585,  371,  256,  292,  293,  456,  366,
  256,  297,  298,  370,  361,  448,  363,  448,  365,  365,
  344,  295, 1012,  448,  310,  311,  312,  313,  314,  315,
  316,  317,  318,  319,  320,  841,  360,  257,  267,  364,
  368,  366,  836,  368,  345, 1077,  370,  369,  256,  371,
  448,  337,  353,  339, 1012,  278,  448,  381,  382,  383,
  385,  239,  295,  370,  366,  351,  352,  369,  379,  393,
  381,  366, 1012,  368,  377, 1065,  776,  378,  776,  380,
  380,  373, 1114,  385,  364,  422,  315,  390,  391,  400,
  385,  402,  369,  366,  371,  368,  366,  295,  306,  257,
  278,  387,  403,  389,  366, 1072,  380, 1065,  440,  389,
  782,  785,  385,  364, 1072,  344,  390,  402,  364,  370,
  371,  449,  373,  385,  370, 1065,  371,  285,  366, 1010,
  353,  646,  370,  762,  928,  593,  570,  385,  456,  503,
  443, 1056,  366,  372, 1111,  477,  370,  380,  606,  342,
  366,  380,  340, 1111,  449,  455, 1071,  390, 1073,  445,
  461,  390,  447,  372, 1010,  389, 1047,  499,  454,  455,
  645,  474,  458,  366,  506,  353,  364,  463,  336,  480,
  368,  448,  380, 1010,  372,  643,  448,  645,  643,  364,
  645, 1072,  390,  368, 1072,  370,  371,  368,  373,  370,
  448, 1047,  503,  364, 1072,  491,  492,  368,  366,  370,
  448,  266, 1018,  268,  593,  368,  271,  503,  273,  504,
 1047,  276,  523,  524,  448,  280, 1072,  606,  283,  461,
 1111,  364,  448, 1111,  289,  368,  366,  370,  570,  540,
  541,  296,  297, 1111,  369, 1072,  301,  366,  366,  380,
  305, 1284,  373,  539, 1287,  385,  368,  543,  370,  390,
  378,  449,  317,  564,  319, 1111,  385,  366,  323,  324,
  369,  370,  371,  559,  449,  366,  331,  332,  449,  370,
  335,  364,   62,  338, 1111,  368,  385,  624,  449,  366,
  389,  592,  593,  594,  385,  762,  762,  256,  389,  762,
  585,  448,  605,  256,  762,  606,  265,   87,  385,  595,
   90,  380,  598,  599,  364,  385,  449,  366,  448,  389,
  606,  390,  608,  373,  610,  450,  451,  452,  453,  448,
  615,  366,  344,  366,  273,  366,  385,  449,  118,  340,
  274,  627,  660,  366,  278,  370,  448,  657,  282,  448,
  385,  131,  385,  306,  385,  641, 1072,  448,  297,  139,
  646, 1072,  385,  364,  389, 1235, 1236,  326,  380,  389,
  371,  448,  373, 1243,  344,  367,  156,  157,  390,  261,
  666,  273,  632, 1253,  670,  324,  455,  845,  273,  396,
  410,  398,  366,  448,  680, 1111, 1175,  683,  856,  448,
 1111,  686,  878,  285,    6,  297,    8,  693,  378,  343,
  380,  385,  297,  448,  434,  448,  298,  448,  704,  705,
  390,  303,  344,  338,  306,  448,  308,  751,  310,  311,
  312,  313,  324,  256, 1213, 1305,  318, 1216,  448,  324,
  322,  342,   44,  389,  326,  365,  366,  270,  368,  752,
  370,  371,  334,  270, 1214,  337,  378,  339,  380,  370,
  366,  762,  748,  369,  287,  366,  767,  364,  390,  770,
  287,  340,  392, 1252,  394,  933,  340,  257,  389,  385,
  938,  939,  366,  380,  366,  369, 1246, 1247, 1117, 1249,
  776,  794,  340,  390,  780,  364,  375,  376,  783,  785,
  364,  385,  371,  385,  365,  366,  286,  368,  366,  370,
  371,  366,  432,  799,  369,  370,  364,  803,  307,  805,
  378,  807,  380,  981,  810,  314,  827,  364,  366,  987,
  385,  392,  390,  394,  389,  366,  373,  326,  824,  307,
  378,  309, 1230, 1231,  845,  385,  314,  848,  834,  852,
  836,  883,  884,  364,  385,  856,  842,  368,  326,  266,
  380,  268,  365,  371,  271,  345,  448,  366,  371,  276,
  369,  432,  365,  280,  382,  378,  283,  340,  371,  819,
  367,  821,  289,  346,  347,  371,  385,  350,  351,  296,
  353,  354,  370,  364,  301,  373,  382,  907,  305,  380,
  369,  366,  373,  368,  890,  370,  892,  378,  371,  390,
  317,  897,  319,  364, 1081, 1081,  323,  366, 1081, 1077,
  369,  370,  373, 1081,  331,  332,  364,  392,  335,  394,
  385,  338,  933,  936,  389,  373,  385,  938,  939,  925,
  261,  366,  928,  366,  369,  931,  369,  371,  368,  367,
 1117, 1117,  364,  371, 1117,  371, 1114,  368,  382, 1117,
  385,  373,  385,  903,  285,  364,  382,  432,  954,  370,
  364,  972,  958,  378,  373,  380,  378,  298,  380,  373,
  981,  461,  303,  368,  364,  390,  987,  308,  390,  310,
  311,  312,  313,  373,  367,  368,  371,  318,  371,  365,
  480,  322,  366,  369,  368,  371,  370,  373, 1009,  370,
  388,  997,  373,  334,  365, 1001,  337,  365,  339,  371,
  371,  385,  366,  371,  368,  389,  370,  967,  392,  369,
  394,  371,  404,  373,  406,  371, 1022,  373, 1205, 1205,
  383,  448, 1205,  523,  524,  389, 1032, 1205,  392, 1035,
  394,  384,  367,  368,  256,  370,  371,  372,  367,  408,
  540,  541,  371,  367,  266,  369,  268,  371,  432,  271,
 1010,  367, 1012,  369,  276,  371, 1077,  367,  280,  367,
 1081,  371,  364,  371,  564, 1128, 1087,  289,  432, 1090,
  350,  351,  369,  369,  296, 1096,  373,  373, 1084,  301,
  780,  355,  356,  305,  784,  355,  356, 1047, 1145,  371,
  448,  373,  592, 1114,  594,  317, 1117,  319,  385,  386,
  387,  323, 1123, 1124,  370, 1065, 1150,  448,  372,  331,
  332,  256, 1072,  335,  256, 1136,  338,  385,  386,  387,
 1164,  371,  371,  373, 1168,  366, 1147,  340, 1149, 1173,
 1174, 1154,  364,  346,  347,  350,  351,  350,  351,  448,
  353,  354,  364,  368,  366,    0, 1333, 1333,  350,  351,
 1333, 1111,  726,  727, 1198, 1333,  448,  369,  371,  371,
  371,  340,  373, 1207, 1208,  323,  324,  346,  347,  369,
  378,  350,  351,  371,  353,  354, 1128,  327,  328,  329,
  330,  340,  331,  332, 1205, 1137, 1138,  346,  347,  448,
  448,  350,  351, 1145,  353,  354,  141,  142,  143,  144,
  145,  146,  147,  148,  412, 1268,  414,  371,  416,  373,
  418,  261,  420,  359,  422,  361,  424,  454,  426,  367,
  428,  369,  430,  380, 1230, 1231,  448,  448,  366, 1292,
  340, 1294,  367,  373,  369,  285,  346,  347, 1261,  448,
  350,  351,  448,  353,  354,  367,  371,  369,  298, 1209,
  371,  448,  373,  303,  369,  367,  371,  369,  308,  367,
  310,  311,  312,  313,  373,  368, 1290,  767,  318,  367,
  770,  369,  322,  373,  368, 1235, 1236,  368, 1301, 1302,
  359,  261,  361, 1243,  334, 1245, 1309,  337, 1311,  339,
  364, 1315, 1316, 1253,  340,  387,  385,  389,  368,  365,
  346,  347, 1262,    0,  373,  285,  373,  353,  354,  373,
 1270, 1271, 1333,  369,  369,  344,  366,  295,  298,  368,
  372,  369,  302,  303,  448,  364,  367,  827,  308,  448,
  310,  311,  312,  313,  448,  385,  380,  341,  318,  373,
  373,  373,  322,  373,  389, 1305,  373,  371,  848,  294,
  295,  296,  369,  371,  334,  371,  371,  337,  365,  339,
  371,  369,  371,  369,  378,  370,  369,  357,  285,  371,
  336,  369,  367,  369,  371,  369,  321,  322,  323,  324,
  372,  369,  327,  328,  329,  330,  331,  332,  333,  334,
  335,  336,  369,  338,    0,  378,  368,  373,  448,  373,
  371,  369,  257,  373,  369,  256,  261,  369,  369,  369,
  389,  266,  256,  268,  256,  364,  271,  365,  273,  274,
  448,  276,  448,  278,  295,  280,  448,  282,  283,  284,
  285,  295,  352,  288,  289,  448,  365,  340,  378,  294,
  371,  296,  297,  298,  369,  295,  301,  302,  303,  371,
  305,  369,  348,  308,  455,  310,  311,  312,  313,  371,
  371,  448,  317,  318,  319,  367,  371,  322,  323,  324,
  369,  369,  972,  448,    0,  448,  331,  332,  281,  334,
  335,  336,  337,  338,  339,  364,  256,  256,  343,  340,
  348,  365,  372,  369,  364,  346,  347,  348,  349,  350,
  351,  352,  353,  354,  355,  356,  357,  364,  453, 1009,
  365,  366,  364,  373,  365,  366,  367,  373,  369,  374,
  371,  372,  373,  369,  367,  470,  349,  365,  473,  378,
  349,  368,  373,  365,  385,  371,  365,  372,  369,  357,
  374,  369,  448,  365,  364,  372,  370,  448,  369,  366,
  366,  364,  366,  368,  365,  367,  256,  256,  367,  369,
  257,  369,  369,  369,  261,  369,  365,  364,  369,  266,
    0,  268,  367,  369,  271,  369,  273,  365,  365,  276,
  365,  278,  365,  280,  365,  282,  365, 1087,  285,  373,
 1090,  288,  289,  448,  367,  367, 1096,  364,  369,  296,
  297,  298,  364,  369,  301,  302,  303,  364,  305,  369,
  369,  308,  367,  310,  311,  312,  313,  365,  448,  369,
  317,  318,  319, 1123, 1124,  322,  323,  324,  369,  367,
  367,  373,  365,  365,  331,  332, 1136,  334,  335,  336,
  337,  338,  339,  448,  369,  369,  343, 1147,  364, 1149,
  448,  257,  373,  365,  365,  261,  369,  371,  373,  369,
  266,  364,  268,  365,  373,  271,  448,  273,  365,  366,
  276,  364,  278,  365,  280,  373,  282,  373,  369,  285,
  369,  365,  288,  289,  369,  364,  364,  364,    6,    6,
  296,  297,  298,   38,   76,  301,  302,  303,  292,  305,
  293, 1047,  308, 1111,  310,  311,  312,  313,  835, 1144,
  356,  317,  318,  319, 1226,  560,  322,  323,  324,  772,
 1302,  772, 1065,  766, 1325,  331,  332, 1262,  334,  335,
  772,  337,  338,  339, 1270,  261,  967,  343, 1271,  239,
  266,  265,  268,  972,  306,  271,  782,  273,  492,  305,
  276,  448,  278,  645,  280,  307,  282,  333,  783,  524,
  334,  336,  288,  289,  335,  338,  917, 1087, 1081,  378,
  296,  297,  298,  480,  794,  301,  302,  303, 1025,  305,
 1018, 1027,  308,  927,  310,  311,  312,  313,  960,  753,
 1120,  317,  318,  319,  547,  958,  322,  323,  324,   -1,
   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,  334,  335,
   -1,  337,  338,  339,   -1,   -1,  256,  343,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  265,  266,  267,  268,  269,
   -1,  271,  272,   -1,  274,  275,  276,  782,  278,  279,
  280,  281,  448,   -1,   -1,   -1,  286,   -1,  288,  289,
  290,  291,  292,  293,   -1,   -1,  296,   -1,   -1,   -1,
  300,  301,   -1,  303,  304,  305,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  315,   -1,  317,   -1,  319,
  320,   -1,   -1,  323,   -1,  325,  326,  327,  328,  329,
  330,  331,  332,  333,  334,  335,  336,   -1,  338,   -1,
   -1,  341,   -1,   -1,   -1,   -1,  346,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  266,   -1,  268,   -1,
   -1,  271,  448,   -1,  364,  365,  276,   -1,  368,   -1,
  280,   -1,   -1,  373,  374,  375,  376,  377,   -1,  289,
   -1,   -1,   -1,  383,   -1,  385,  296,   -1,   -1,   -1,
   -1,  301,  392,   -1,  394,  305,   -1,  307,   -1,  309,
   -1,   -1,   -1,   -1,  314,   -1,   -1,  317,   -1,  319,
   -1,   -1,   -1,  323,   -1,   -1,  326,   -1,   -1,   -1,
   -1,  331,  332,   -1,   -1,  335,   -1,   -1,  338,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,
  440,   -1,  442,   -1,  444,   -1,  446,   -1,  448,  449,
  256,   -1,   -1,   -1,  454,   -1,  456,  367,   -1,  265,
  266,  267,  268,  269,   -1,  271,  272,   -1,  274,  275,
  276,   -1,  278,  279,  280,   -1,   -1,   -1,   -1,   -1,
  286,   -1,  288,  289,  290,  291,  292,  293,   -1,   -1,
  296,   -1,   -1,   -1,  300,  301,   -1,  303,  304,  305,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  315,
   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,  325,
  326,  327,  328,  329,  330,  331,  332,  333,  334,  335,
  336,   -1,  338,   -1,   -1,  341,   -1,   -1,  448,   -1,
  346,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  266,   -1,  268,   -1,   -1,  271,   -1,   -1,  364,  365,
  276,   -1,  368,   -1,  280,   -1,   -1,  373,  374,  375,
  376,  377,   -1,  289,   -1,   -1,   -1,  383,   -1,  385,
  296,   -1,   -1,   -1,   -1,  301,  392,   -1,  394,  305,
   -1,  307,   -1,  309,   -1,   -1,   -1,   -1,  314,   -1,
   -1,  317,   -1,  319,   -1,   -1,   -1,  323,   -1,   -1,
  326,   -1,   -1,   -1,   -1,  331,  332,   -1,   -1,  335,
   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  436,   -1,  438,   -1,  440,   -1,  442,   -1,  444,   -1,
  446,   -1,  448,  449,  256,   -1,   -1,   -1,  454,   -1,
  456,   -1,   -1,  265,  266,  267,  268,   -1,   -1,  271,
  272,   -1,  274,  275,  276,   -1,  278,  279,  280,   -1,
   -1,   -1,   -1,   -1,  286,   -1,  288,  289,  290,  291,
  292,  293,   -1,   -1,  296,   -1,   -1,   -1,  300,  301,
   -1,  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  315,   -1,  317,   -1,  319,  320,   -1,
   -1,  323,   -1,  325,  326,  327,  328,  329,  330,  331,
  332,  333,  334,  335,  336,   -1,  338,  256,   -1,  341,
   -1,   -1,  448,   -1,  346,   -1,  265,  266,   -1,  268,
   -1,   -1,  271,  272,   -1,   -1,   -1,  276,   -1,  278,
   -1,  280,  364,   -1,   -1,   -1,  368,  286,   -1,   -1,
  289,  373,  374,  375,  376,  377,   -1,  296,   -1,  286,
   -1,  383,  301,  385,  303,  304,  305,   -1,   -1,   -1,
  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,
  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,
   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,
   -1,  328,   -1,   -1,   -1,   -1,   -1,  346,   -1,   -1,
   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,
  442,   -1,  444,   -1,  446,   -1,  448,  449,   -1,  368,
   -1,   -1,  454,   -1,   -1,  374,  375,  376,  377,   -1,
   -1,   -1,   -1,   -1,  383,   -1,  385,  374,  375,  376,
  377,   -1,  379,  392,  381,  394,  383,  384,  385,  386,
  387,  388,   -1,   -1,   -1,  392,   -1,  394,   -1,  396,
   -1,  398,   -1,  400,   -1,  402,   -1,  404,  256,  406,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  265,  266,   -1,
  268,   -1,   -1,  271,  272,   -1,   -1,  436,  276,  438,
  278,  440,  280,  442,   -1,  444,   -1,  446,  286,  448,
  449,  289,   -1,   -1,   -1,  454,   -1,   -1,  296,   -1,
   -1,   -1,   -1,  301,   -1,  303,  304,  305,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  317,
   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,
  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,
  338,  256,   -1,   -1,   -1,   -1,   -1,   -1,  346,   -1,
  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,
   -1,  276,   -1,  278,   -1,  280,   -1,   -1,   -1,   -1,
  368,  286,   -1,   -1,  289,   -1,  374,  375,  376,  377,
   -1,  296,   -1,   -1,   -1,  383,  301,  385,  303,  304,
  305,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,
   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,
   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,
  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,
  438,   -1,  440,   -1,  442,   -1,  444,   -1,  446,   -1,
  448,  449,   -1,  368,  256,   -1,  454,   -1,   -1,  261,
  262,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  285,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  295,   -1,   -1,  298,  299,   -1,   -1,
   -1,  303,   -1,   -1,  306,   -1,  308,   -1,  310,  311,
  312,  313,   -1,   -1,   -1,   -1,  318,   -1,   -1,   -1,
  322,  436,   -1,  438,  326,  440,   -1,  442,   -1,  444,
   -1,  446,  334,  448,   -1,  337,   -1,  339,  340,  454,
   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,
   -1,   -1,  364,  365,   -1,  367,  368,  369,  370,  371,
  372,  373,   -1,  375,  376,   -1,  378,  379,   -1,  381,
  382,  383,  384,  385,  386,  387,  388,  389,   -1,   -1,
  392,   -1,  394,   -1,  396,   -1,  398,   -1,  400,   -1,
  402,   -1,  404,   -1,  406,   -1,  408,   -1,  410,   -1,
  412,   -1,  414,   -1,  416,   -1,  418,   -1,  420,   -1,
  422,   -1,  424,   -1,  426,   -1,  428,   -1,  430,  256,
  432,   -1,  434,   -1,  261,  262,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  448,  449,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  285,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  295,   -1,
   -1,  298,  299,   -1,   -1,   -1,  303,   -1,   -1,  306,
   -1,  308,   -1,  310,  311,  312,  313,   -1,   -1,   -1,
   -1,  318,   -1,   -1,   -1,  322,   -1,   -1,   -1,  326,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  334,   -1,   -1,
  337,   -1,  339,  340,   -1,   -1,   -1,   -1,   -1,  346,
  347,  348,  349,  350,  351,  352,  353,  354,  355,  356,
  357,   -1,   -1,   -1,   -1,   -1,   -1,  364,  365,  366,
  367,  368,  369,  370,  371,  372,  373,   -1,   -1,   -1,
   -1,   -1,  379,   -1,  381,  382,  383,  384,  385,   -1,
   -1,  388,  389,  256,   -1,   -1,   -1,   -1,  261,  262,
   -1,   -1,   -1,  400,   -1,  402,   -1,  404,   -1,  406,
   -1,  408,   -1,  410,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  285,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  295,   -1,   -1,  298,  299,  434,   -1,   -1,
  303,   -1,   -1,   -1,   -1,  308,   -1,  310,  311,  312,
  313,  448,  449,   -1,   -1,  318,   -1,   -1,   -1,  322,
   -1,   -1,   -1,  326,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  334,   -1,   -1,  337,   -1,  339,  340,   -1,   -1,
   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,
   -1,  364,  365,  366,  367,  368,  369,  370,  371,  372,
  373,   -1,   -1,   -1,   -1,   -1,  379,   -1,  381,  382,
  383,  384,  385,   -1,   -1,  388,  389,  256,   -1,   -1,
   -1,   -1,  261,  262,   -1,   -1,   -1,  400,   -1,  402,
   -1,  404,   -1,  406,   -1,  408,   -1,  410,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  285,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  295,   -1,   -1,  298,
  299,  434,   -1,   -1,  303,   -1,   -1,  306,   -1,  308,
   -1,  310,  311,  312,  313,  448,  449,   -1,   -1,  318,
   -1,   -1,   -1,  322,   -1,   -1,   -1,  326,  256,   -1,
   -1,   -1,   -1,   -1,   -1,  334,   -1,   -1,  337,   -1,
  339,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,   -1,
   -1,   -1,   -1,   -1,   -1,  364,  365,  366,  367,  368,
  369,  256,  371,  372,  373,   -1,   -1,  262,   -1,   -1,
  379,   -1,  381,  382,  383,  384,  385,   -1,   -1,  388,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  400,   -1,  402,   -1,  404,   -1,  406,   -1,  408,
  295,  410,  340,   -1,  299,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,   -1,
   -1,   -1,   -1,   -1,   -1,  434,   -1,  365,  366,  367,
   -1,  369,   -1,  371,  372,  373,   -1,   -1,   -1,  448,
  449,   -1,   -1,   -1,   -1,  340,   -1,  385,   -1,   -1,
   -1,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,  364,
  365,  366,  367,  368,  369,  256,  371,  372,  373,  261,
   -1,  262,   -1,   -1,  379,   -1,  381,  382,  383,  384,
   -1,   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  285,   -1,  400,   -1,  402,   -1,  404,
   -1,  406,   -1,  408,   -1,  410,  298,   -1,  299,   -1,
   -1,  303,   -1,   -1,   -1,   -1,  308,   -1,  310,  311,
  312,  313,   -1,   -1,   -1,   -1,  318,   -1,   -1,  434,
  322,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  334,  448,  449,  337,   -1,  339,   -1,  340,
   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,
  351,  352,  353,  354,  355,  356,  357,  359,   -1,  361,
   -1,   -1,   -1,  365,  365,  366,  367,  368,  369,  370,
  371,  372,  373,  256,  375,  376,   -1,  378,  379,  262,
  381,   -1,  383,  384,  385,  386,  387,  388,  389,   -1,
   -1,  392,   -1,  394,   -1,  396,   -1,  398,   -1,  400,
   -1,  402,   -1,  404,   -1,  406,   -1,  408,   -1,  410,
   -1,  412,   -1,  414,   -1,  416,  299,  418,   -1,  420,
   -1,  422,   -1,  424,   -1,  426,   -1,  428,   -1,  430,
   -1,  432,   -1,  434,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  448,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  340,   -1,   -1,
   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  365,  366,  367,  368,  369,  370,  371,  372,
  373,  256,  375,  376,   -1,  378,  379,  262,  381,   -1,
  383,  384,  385,  386,  387,  388,  389,   -1,   -1,  392,
   -1,  394,   -1,  396,   -1,  398,   -1,  400,   -1,  402,
   -1,  404,   -1,  406,   -1,  408,   -1,  410,   -1,  412,
   -1,  414,   -1,  416,  299,  418,   -1,  420,   -1,  422,
   -1,  424,   -1,  426,   -1,  428,   -1,  430,   -1,  432,
   -1,  434,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  448,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,   -1,
   -1,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  365,  366,  367,  368,  369,  370,  371,  372,  373,  256,
  375,  376,   -1,  378,  379,  262,  381,   -1,  383,  384,
  385,  386,  387,  388,  389,   -1,   -1,  392,   -1,  394,
   -1,  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,
   -1,  406,   -1,  408,   -1,  410,   -1,  412,   -1,  414,
   -1,  416,  299,  418,   -1,  420,   -1,  422,   -1,  424,
   -1,  426,   -1,  428,   -1,  430,   -1,  432,   -1,  434,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  448,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,
  347,  348,  349,  350,  351,  352,  353,  354,  355,  356,
  357,   -1,   -1,  256,   -1,   -1,   -1,  364,  365,  262,
  367,  368,  369,  370,  371,  372,  373,   -1,  375,  376,
   -1,  378,  379,   -1,  381,   -1,  383,  384,  385,  386,
  387,  388,  389,   -1,   -1,  392,   -1,  394,   -1,  396,
   -1,  398,   -1,  400,   -1,  402,  299,  404,   -1,  406,
   -1,  408,   -1,  410,   -1,  412,   -1,  414,   -1,  416,
   -1,  418,   -1,  420,   -1,  422,   -1,  424,   -1,  426,
   -1,  428,   -1,  430,   -1,  432,   -1,  434,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  340,   -1,   -1,
   -1,  448,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  365,  366,  367,  368,  369,  370,  371,  372,
  373,  256,  375,  376,   -1,  378,  379,  262,  381,   -1,
  383,  384,  385,  386,  387,  388,  389,   -1,   -1,  392,
   -1,  394,   -1,  396,   -1,  398,   -1,  400,   -1,  402,
   -1,  404,   -1,  406,   -1,  408,   -1,  410,   -1,  412,
   -1,  414,   -1,  416,  299,  418,   -1,  420,   -1,  422,
   -1,  424,   -1,  426,   -1,  428,   -1,  430,   -1,  432,
   -1,  434,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  448,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,   -1,
   -1,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  365,  366,  367,  368,  369,  370,  371,  372,  373,  256,
  375,  376,   -1,  378,  379,  262,  381,   -1,  383,  384,
  385,  386,  387,  388,  389,   -1,   -1,  392,   -1,  394,
   -1,  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,
   -1,  406,   -1,  408,   -1,  410,   -1,  412,   -1,  414,
   -1,  416,  299,  418,   -1,  420,   -1,  422,   -1,  424,
   -1,  426,   -1,  428,   -1,  430,   -1,  432,   -1,  434,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  448,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,
  347,  348,  349,  350,  351,  352,  353,  354,  355,  356,
  357,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  365,   -1,
  367,   -1,  369,   -1,  371,  372,  373,   -1,  375,  376,
   -1,  378,  379,   -1,  381,   -1,  383,  384,  385,  386,
  387,  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,  396,
   -1,  398,   -1,  400,   -1,  402,   -1,  404,   -1,  406,
   -1,  408,   -1,  410,   -1,  412,   -1,  414,   -1,  416,
   -1,  418,   -1,  420,   -1,  422,   -1,  424,   -1,  426,
   -1,  428,   -1,  430,  257,   -1,   -1,  434,  261,   -1,
   -1,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,   -1,
  273,  448,   -1,  276,   -1,  278,   -1,  280,   -1,  282,
   -1,   -1,   -1,   -1,   -1,  288,  289,   -1,   -1,   -1,
   -1,   -1,   -1,  296,  297,  298,   -1,   -1,  301,  302,
  303,   -1,  305,   -1,   -1,  308,   -1,  310,  311,  312,
  313,   -1,   -1,   -1,  317,  318,  319,   -1,   -1,  322,
  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,
   -1,  334,  335,  336,  337,  338,  339,   -1,   -1,   -1,
  343,   -1,   -1,   -1,   -1,  257,   -1,   -1,   -1,  261,
   -1,   -1,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,
   -1,  273,  365,  366,  276,   -1,  278,   -1,  280,   -1,
  282,   -1,   -1,   -1,   -1,   -1,  288,  289,   -1,   -1,
   -1,   -1,   -1,   -1,  296,  297,  298,   -1,   -1,  301,
  302,  303,   -1,  305,   -1,   -1,  308,   -1,  310,  311,
  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,   -1,
  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,
  332,   -1,  334,  335,  336,  337,  338,  339,   -1,   -1,
   -1,  343,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  448,  257,   -1,   -1,   -1,
  261,   -1,   -1,  365,  366,  266,   -1,  268,   -1,   -1,
  271,   -1,  273,   -1,   -1,  276,   -1,  278,   -1,  280,
   -1,  282,   -1,   -1,  285,   -1,   -1,  288,  289,   -1,
   -1,   -1,   -1,   -1,   -1,  296,  297,  298,   -1,   -1,
  301,  302,  303,   -1,  305,   -1,   -1,  308,   -1,  310,
  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,
   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,
  331,  332,   -1,  334,  335,   -1,  337,  338,  339,   -1,
   -1,   -1,  343,   -1,   -1,   -1,  448,  257,   -1,   -1,
   -1,  261,   -1,   -1,   -1,   -1,  266,   -1,  268,   -1,
   -1,  271,   -1,  273,  365,  366,  276,   -1,  278,   -1,
  280,   -1,  282,   -1,   -1,  285,   -1,   -1,  288,  289,
   -1,   -1,   -1,   -1,   -1,   -1,  296,  297,  298,   -1,
   -1,  301,  302,  303,   -1,  305,   -1,   -1,  308,   -1,
  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,
   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,
   -1,  331,  332,   -1,  334,  335,   -1,  337,  338,  339,
   -1,   -1,   -1,  343,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  448,  257,   -1,
   -1,   -1,  261,   -1,   -1,  365,  366,  266,   -1,  268,
   -1,   -1,  271,   -1,  273,   -1,   -1,  276,   -1,  278,
   -1,  280,   -1,  282,   -1,   -1,  285,   -1,   -1,  288,
  289,   -1,   -1,   -1,   -1,   -1,   -1,  296,  297,  298,
   -1,   -1,  301,  302,  303,   -1,  305,   -1,   -1,  308,
   -1,  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,
  319,   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,
   -1,   -1,  331,  332,   -1,  334,  335,   -1,  337,  338,
  339,   -1,  257,   -1,  343,   -1,  261,   -1,  448,   -1,
   -1,  266,   -1,  268,   -1,   -1,  271,   -1,  273,   -1,
   -1,  276,   -1,  278,   -1,  280,  365,  282,   -1,   -1,
  285,   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,   -1,
   -1,  296,  297,  298,   -1,   -1,  301,  302,  303,   -1,
  305,   -1,   -1,  308,   -1,  310,  311,  312,  313,   -1,
   -1,   -1,  317,  318,  319,   -1,   -1,  322,  323,  324,
   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,  334,
  335,   -1,  337,  338,  339,   -1,   -1,   -1,  343,   -1,
   -1,  265,  266,  267,  268,   -1,   -1,  271,  272,   -1,
  274,  275,  276,   -1,  278,  279,  280,   -1,   -1,  448,
  365,   -1,  286,   -1,  288,  289,  290,  291,  292,  293,
   -1,   -1,  296,   -1,   -1,   -1,  300,  301,   -1,  303,
  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  315,   -1,  317,   -1,  319,  320,   -1,   -1,  323,
   -1,  325,  326,  327,  328,  329,  330,  331,  332,  333,
  334,  335,  336,   -1,  338,   -1,   -1,  341,   -1,   -1,
   -1,   -1,  346,   -1,   -1,  266,   -1,  268,   -1,   -1,
  271,   -1,   -1,   -1,   -1,  276,   -1,   -1,   -1,  280,
  364,   -1,   -1,  448,  368,   -1,   -1,   -1,  289,  373,
  374,  375,  376,  377,   -1,  296,   -1,   -1,   -1,  383,
  301,  385,   -1,   -1,  305,   -1,  307,   -1,  392,   -1,
  394,   -1,   -1,  314,   -1,   -1,  317,   -1,  319,   -1,
   -1,   -1,  323,   -1,   -1,  326,   -1,   -1,   -1,   -1,
  331,  332,   -1,   -1,  335,   -1,   -1,  338,   -1,   -1,
   -1,   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,
   -1,   -1,  436,  276,  438,  278,  440,  280,  442,   -1,
  444,   -1,  446,  286,  448,  449,  289,   -1,   -1,   -1,
  454,   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,
  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  317,   -1,  319,  320,  321,   -1,
  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,
  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,  342,
   -1,   -1,   -1,  346,   -1,  265,  266,   -1,  268,   -1,
   -1,  271,  272,   -1,   -1,   -1,  276,   -1,  278,   -1,
  280,  364,  365,   -1,   -1,  368,  286,  448,   -1,  289,
   -1,  374,  375,  376,  377,   -1,  296,   -1,   -1,   -1,
  383,  301,  385,  303,  304,  305,   -1,  307,   -1,  392,
   -1,  394,   -1,   -1,  314,   -1,   -1,  317,   -1,  319,
  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,
  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,
   -1,   -1,  342,   -1,   -1,   -1,  346,   -1,   -1,   -1,
   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,  442,
   -1,  444,   -1,  446,   -1,  448,  449,   -1,  368,  369,
   -1,  454,   -1,   -1,  374,  375,  376,  377,   -1,   -1,
   -1,   -1,   -1,  383,   -1,  385,   -1,   -1,   -1,   -1,
   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  265,  266,   -1,  268,
   -1,   -1,  271,  272,   -1,   -1,  436,  276,  438,  278,
  440,  280,  442,   -1,  444,   -1,  446,  286,  448,  449,
  289,   -1,   -1,   -1,  454,   -1,   -1,  296,   -1,   -1,
   -1,   -1,  301,   -1,  303,  304,  305,   -1,  307,   -1,
   -1,   -1,   -1,   -1,   -1,  314,   -1,   -1,  317,   -1,
  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,
   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,
   -1,   -1,   -1,  342,   -1,   -1,   -1,  346,   -1,   -1,
  266,   -1,  268,   -1,   -1,  271,   -1,  273,   -1,   -1,
  276,   -1,   -1,   -1,  280,   -1,   -1,   -1,   -1,  368,
  369,   -1,   -1,  289,   -1,  374,  375,  376,  377,   -1,
  296,   -1,   -1,   -1,  383,  301,  385,  303,   -1,  305,
   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,
   -1,  317,   -1,  319,   -1,   -1,   -1,  323,  324,   -1,
   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,   -1,  335,
   -1,   -1,  338,   -1,   -1,   -1,   -1,  265,  266,   -1,
  268,   -1,   -1,  271,  272,   -1,   -1,  436,  276,  438,
  278,  440,  280,  442,   -1,  444,   -1,  446,  286,  448,
  449,  289,   -1,   -1,   -1,  454,   -1,   -1,  296,   -1,
   -1,   -1,   -1,  301,   -1,  303,  304,  305,   -1,  307,
   -1,   -1,   -1,   -1,   -1,   -1,  314,   -1,   -1,  317,
   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,
  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,
  338,   -1,   -1,   -1,  342,   -1,   -1,   -1,  346,   -1,
  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,
   -1,  276,   -1,  278,   -1,  280,   -1,   -1,   -1,   -1,
  368,  286,  448,   -1,  289,   -1,  374,  375,  376,  377,
   -1,  296,   -1,   -1,   -1,  383,  301,  385,  303,  304,
  305,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,
   -1,   -1,  317,   -1,  319,  320,  321,   -1,  323,   -1,
   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,
  335,   -1,   -1,  338,   -1,   -1,   -1,  342,   -1,   -1,
   -1,  346,   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,
  438,   -1,  440,   -1,  442,   -1,  444,   -1,  446,  364,
  448,  449,   -1,  368,   -1,   -1,  454,   -1,   -1,  374,
  375,  376,  377,   -1,   -1,   -1,   -1,   -1,  383,   -1,
  385,   -1,   -1,   -1,   -1,   -1,   -1,  392,   -1,  394,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,
   -1,  436,  276,  438,  278,  440,  280,  442,   -1,  444,
   -1,  446,  286,  448,  449,  289,   -1,   -1,   -1,  454,
   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,  303,
  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,
   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,
   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  346,   -1,   -1,  266,   -1,  268,   -1,   -1,
  271,   -1,   -1,   -1,   -1,  276,   -1,   -1,   -1,  280,
  364,  365,   -1,   -1,  368,   -1,   -1,   -1,  289,   -1,
  374,  375,  376,  377,   -1,  296,   -1,   -1,   -1,  383,
  301,  385,   -1,   -1,  305,   -1,   -1,   -1,  392,   -1,
  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,   -1,
   -1,   -1,  323,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  331,  332,   -1,   -1,  335,   -1,   -1,  338,   -1,   -1,
   -1,   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,
   -1,   -1,  436,  276,  438,  278,  440,  280,  442,   -1,
  444,   -1,  446,  286,  448,  449,  289,   -1,   -1,   -1,
  454,   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,
  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,
  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,
  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  346,   -1,  265,  266,   -1,  268,   -1,
   -1,  271,  272,   -1,   -1,   -1,  276,   -1,  278,   -1,
  280,   -1,   -1,   -1,   -1,  368,  286,  448,  371,  289,
   -1,  374,  375,  376,  377,   -1,  296,   -1,   -1,   -1,
  383,  301,  385,  303,  304,  305,   -1,   -1,   -1,  392,
   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,
  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,
  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  346,   -1,   -1,   -1,
   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,  442,
   -1,  444,   -1,  446,   -1,  448,  449,   -1,  368,  369,
   -1,  454,   -1,   -1,  374,  375,  376,  377,   -1,   -1,
   -1,   -1,   -1,  383,   -1,  385,   -1,   -1,   -1,   -1,
   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  265,  266,   -1,  268,
   -1,   -1,  271,  272,   -1,   -1,  436,  276,  438,  278,
  440,  280,  442,   -1,  444,   -1,  446,  286,  448,  449,
  289,   -1,   -1,   -1,  454,   -1,   -1,  296,   -1,   -1,
   -1,   -1,  301,   -1,  303,  304,  305,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  317,   -1,
  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,
   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  346,   -1,  265,
  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,   -1,
  276,   -1,  278,   -1,  280,  364,   -1,   -1,   -1,  368,
  286,   -1,   -1,  289,   -1,  374,  375,  376,  377,   -1,
  296,   -1,   -1,   -1,  383,  301,  385,  303,  304,  305,
   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,
   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,
  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,
   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  346,   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,
   -1,  440,   -1,  442,   -1,  444,   -1,  446,   -1,  448,
  449,  367,  368,   -1,   -1,  454,   -1,   -1,  374,  375,
  376,  377,   -1,   -1,   -1,   -1,   -1,  383,   -1,  385,
   -1,   -1,   -1,   -1,   -1,   -1,  392,   -1,  394,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,
  436,  276,  438,  278,  440,  280,  442,   -1,  444,   -1,
  446,  286,  448,  449,  289,   -1,   -1,   -1,  454,   -1,
   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,  303,  304,
  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,
   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,
  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  346,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,
   -1,   -1,   -1,   -1,  276,   -1,   -1,   -1,  280,   -1,
   -1,   -1,   -1,  368,   -1,   -1,   -1,  289,  373,  374,
  375,  376,  377,   -1,  296,   -1,   -1,   -1,  383,  301,
  385,   -1,   -1,  305,   -1,   -1,   -1,  392,   -1,  394,
   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,   -1,   -1,
   -1,  323,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  331,
  332,   -1,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,
   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,
   -1,  436,  276,  438,  278,  440,  280,  442,   -1,  444,
   -1,  446,  286,  448,  449,  289,   -1,   -1,   -1,  454,
   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,  303,
  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,
   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,
   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  346,   -1,  265,  266,   -1,  268,   -1,   -1,
  271,  272,   -1,   -1,   -1,  276,   -1,  278,   -1,  280,
   -1,  365,   -1,   -1,  368,  286,  448,   -1,  289,   -1,
  374,  375,  376,  377,   -1,  296,   -1,   -1,   -1,  383,
  301,  385,  303,  304,  305,   -1,   -1,   -1,  392,   -1,
  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,  320,
   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,
  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  346,   -1,   -1,   -1,   -1,
   -1,   -1,  436,   -1,  438,   -1,  440,   -1,  442,   -1,
  444,   -1,  446,  364,  448,  449,   -1,  368,   -1,   -1,
  454,   -1,   -1,  374,  375,  376,  377,   -1,   -1,   -1,
   -1,   -1,  383,   -1,  385,   -1,   -1,   -1,   -1,   -1,
   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  265,  266,   -1,  268,   -1,
   -1,  271,  272,   -1,   -1,  436,  276,  438,  278,  440,
  280,  442,   -1,  444,   -1,  446,  286,  448,  449,  289,
   -1,   -1,   -1,  454,   -1,   -1,  296,   -1,   -1,   -1,
   -1,  301,   -1,  303,  304,  305,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,
  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,
  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  346,   -1,  265,  266,
   -1,  268,   -1,   -1,  271,  272,   -1,   -1,   -1,  276,
   -1,  278,   -1,  280,   -1,   -1,   -1,   -1,  368,  286,
   -1,   -1,  289,   -1,  374,  375,  376,  377,   -1,  296,
   -1,   -1,   -1,  383,  301,  385,  303,  304,  305,   -1,
   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,
  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,
   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,
   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  346,
   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,
  440,   -1,  442,   -1,  444,   -1,  446,   -1,  448,  449,
   -1,  368,   -1,   -1,  454,   -1,   -1,  374,  375,  376,
  377,   -1,   -1,   -1,   -1,   -1,  383,   -1,  385,   -1,
   -1,   -1,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  265,
  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,  436,
  276,  438,  278,  440,  280,  442,   -1,  444,   -1,  446,
  286,  448,  449,  289,   -1,   -1,   -1,  454,   -1,   -1,
  296,   -1,   -1,   -1,   -1,  301,   -1,  303,  304,  305,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,
  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,
   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  346,   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,
   -1,   -1,   -1,  276,   -1,  278,   -1,  280,   -1,   -1,
   -1,   -1,  368,  286,   -1,   -1,  289,   -1,  374,  375,
  376,  377,   -1,  296,   -1,   -1,   -1,  383,  301,  385,
  303,  304,  305,   -1,   -1,   -1,  392,   -1,  394,   -1,
   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,
  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,
  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  346,   -1,   -1,   -1,   -1,   -1,   -1,
  436,   -1,  438,   -1,  440,   -1,  442,   -1,  444,   -1,
  446,   -1,  448,  449,   -1,  368,   -1,   -1,  454,   -1,
   -1,  374,  375,  376,  377,   -1,   -1,   -1,   -1,   -1,
  383,   -1,  385,   -1,   -1,   -1,   -1,   -1,   -1,  392,
   -1,  394,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  265,  266,   -1,  268,   -1,   -1,  271,
  272,   -1,   -1,  436,  276,  438,  278,  440,  280,  442,
   -1,  444,   -1,  446,  286,  448,  449,  289,   -1,   -1,
   -1,  454,   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,
   -1,  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,
   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,
  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  265,  266,   -1,  268,
   -1,   -1,  271,  272,   -1,   -1,   -1,  276,   -1,  278,
   -1,  280,   -1,   -1,   -1,   -1,  368,  286,   -1,   -1,
  289,   -1,  374,  375,  376,  377,   -1,  296,   -1,   -1,
   -1,  383,  301,  385,  303,  304,  305,   -1,   -1,   -1,
  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,
  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,
   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,
  442,   -1,  444,   -1,  446,   -1,  448,   -1,   -1,  368,
   -1,   -1,  454,   -1,   -1,  374,  375,  376,  377,   -1,
   -1,   -1,   -1,   -1,  383,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  392,   -1,  394,  265,  266,   -1,  268,
   -1,   -1,  271,  272,   -1,   -1,   -1,  276,   -1,  278,
   -1,  280,   -1,   -1,   -1,   -1,   -1,  286,   -1,   -1,
  289,   -1,   -1,   -1,   -1,   -1,   -1,  296,   -1,   -1,
   -1,   -1,  301,   -1,  303,  304,  305,  436,   -1,  438,
   -1,  440,   -1,  442,   -1,  444,   -1,  446,  317,  448,
  319,  320,   -1,   -1,  323,  454,   -1,  326,   -1,  328,
   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,
   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,
   -1,   -1,  276,   -1,  278,   -1,  280,   -1,   -1,   -1,
   -1,   -1,  286,   -1,   -1,  289,   -1,   -1,   -1,  368,
   -1,   -1,  296,   -1,   -1,  374,   -1,  301,  377,  303,
  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,
   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,
   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,
   -1,  440,   -1,  442,  368,  444,   -1,  446,   -1,  448,
   -1,   -1,   -1,   -1,   -1,  454,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,
   -1,   -1,  276,   -1,  278,   -1,  280,   -1,   -1,   -1,
   -1,   -1,  286,   -1,   -1,  289,   -1,   -1,   -1,   -1,
   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,  303,
  304,  305,  436,   -1,  438,   -1,  440,   -1,  442,   -1,
  444,   -1,  446,  317,  448,  319,  320,   -1,   -1,  323,
  454,   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,
   -1,  335,  265,  266,  338,  268,   -1,   -1,  271,  272,
   -1,   -1,   -1,  276,   -1,  278,   -1,  280,   -1,   -1,
   -1,   -1,   -1,  286,   -1,   -1,  289,   -1,   -1,   -1,
   -1,   -1,   -1,  296,  368,   -1,   -1,   -1,  301,   -1,
  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,
  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,
  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,  261,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  436,  285,  438,  368,  440,   -1,  442,   -1,
  444,   -1,  446,   -1,  448,   -1,  298,  261,   -1,  263,
  454,  303,   -1,   -1,  306,   -1,  308,   -1,  310,  311,
  312,  313,   -1,   -1,   -1,   -1,  318,   -1,   -1,   -1,
  322,  285,   -1,   -1,  326,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  334,   -1,  298,  337,   -1,  339,   -1,  303,
   -1,   -1,   -1,   -1,  308,   -1,  310,  311,  312,  313,
   -1,   -1,  316,  436,  318,  438,   -1,  440,  322,  442,
   -1,  444,   -1,  446,  366,  448,  368,  261,  370,  263,
  334,  454,  266,  337,  268,  339,   -1,  271,   -1,  273,
  274,   -1,  276,  385,  278,   -1,  280,  389,  282,  283,
  284,  285,   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,
  294,  365,  296,  297,  298,   -1,   -1,  301,  302,  303,
   -1,  305,   -1,  307,  308,  309,  310,  311,  312,  313,
  314,   -1,  316,  317,  318,  319,   -1,   -1,  322,  323,
  324,   -1,  326,   -1,   -1,   -1,   -1,  331,  332,   -1,
  334,  335,   -1,  337,  338,  339,  448,  449,   -1,  343,
   -1,  261,   -1,   -1,   -1,   -1,  266,   -1,  268,   -1,
   -1,  271,   -1,  273,  274,  359,  276,  361,  278,   -1,
  280,   -1,  282,  283,  284,  285,   -1,   -1,  288,  289,
  374,   -1,   -1,   -1,  294,   -1,  296,  297,  298,   -1,
   -1,  301,   -1,  303,   -1,  305,   -1,   -1,  308,   -1,
  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,
   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,
   -1,  331,  332,   -1,  334,  335,   -1,  337,  338,  339,
   -1,   -1,   -1,  343,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  261,   -1,   -1,   -1,
   -1,  266,   -1,  268,  448,  365,  271,   -1,  273,  274,
   -1,  276,   -1,  278,  374,  280,   -1,  282,  283,  284,
  285,   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,  294,
   -1,  296,  297,  298,   -1,   -1,  301,   -1,  303,   -1,
  305,   -1,   -1,  308,   -1,  310,  311,  312,  313,   -1,
   -1,   -1,  317,  318,  319,   -1,   -1,  322,  323,  324,
   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,  334,
  335,   -1,  337,  338,  339,   -1,   -1,   -1,  343,   -1,
  261,   -1,   -1,   -1,   -1,  266,   -1,  268,  448,   -1,
  271,   -1,  273,  274,   -1,  276,   -1,  278,   -1,  280,
  365,  282,  283,  284,  285,   -1,   -1,  288,  289,  374,
   -1,   -1,   -1,  294,   -1,  296,  297,  298,   -1,   -1,
  301,   -1,  303,   -1,  305,   -1,   -1,  308,   -1,  310,
  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,
   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,
  331,  332,   -1,  334,  335,   -1,  337,  338,  339,   -1,
   -1,   -1,  343,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  261,   -1,   -1,   -1,   -1,
  266,   -1,  268,  448,  365,  271,   -1,  273,  274,   -1,
  276,   -1,  278,  374,  280,   -1,  282,  283,  284,  285,
   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,  294,   -1,
  296,  297,  298,   -1,   -1,  301,   -1,  303,   -1,  305,
   -1,   -1,  308,   -1,  310,  311,  312,  313,   -1,   -1,
   -1,  317,  318,  319,   -1,   -1,  322,  323,  324,   -1,
   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,  334,  335,
   -1,  337,  338,  339,   -1,   -1,   -1,  343,   -1,  261,
   -1,   -1,   -1,   -1,  266,   -1,  268,  448,   -1,  271,
   -1,  273,  274,   -1,  276,   -1,  278,   -1,  280,  365,
  282,  283,  284,  285,   -1,   -1,  288,  289,  374,   -1,
   -1,   -1,  294,   -1,  296,  297,  298,   -1,   -1,  301,
   -1,  303,   -1,  305,   -1,   -1,  308,   -1,  310,  311,
  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,   -1,
  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,
  332,   -1,  334,  335,   -1,  337,  338,  339,   -1,   -1,
   -1,  343,  261,   -1,   -1,   -1,   -1,  266,   -1,  268,
   -1,   -1,  271,   -1,  273,  274,   -1,  276,   -1,  278,
   -1,  280,  448,  282,  283,  284,  285,   -1,   -1,  288,
  289,   -1,  374,   -1,   -1,  294,   -1,  296,  297,  298,
   -1,   -1,  301,   -1,  303,   -1,  305,   -1,   -1,  308,
   -1,  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,
  319,   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,
   -1,   -1,  331,  332,   -1,  334,  335,   -1,  337,  338,
  339,  261,   -1,   -1,  343,   -1,  266,   -1,  268,   -1,
   -1,  271,   -1,  273,  274,   -1,  276,   -1,  278,   -1,
  280,   -1,  282,  283,   -1,  285,  448,   -1,   -1,  289,
   -1,   -1,   -1,   -1,   -1,  374,  296,  297,  298,   -1,
   -1,  301,   -1,  303,   -1,  305,   -1,   -1,  308,   -1,
  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,
   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,
   -1,  331,  332,   -1,  334,  335,   -1,  337,  338,  339,
  261,   -1,   -1,  343,   -1,  266,  262,  268,   -1,   -1,
  271,   -1,  273,  274,   -1,  276,   -1,  278,   -1,  280,
   -1,  282,  283,   -1,  285,  365,   -1,   -1,  289,  448,
   -1,   -1,   -1,   -1,   -1,  296,  297,  298,   -1,   -1,
  301,   -1,  303,  299,  305,   -1,   -1,  308,   -1,  310,
  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,
   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,
  331,  332,  262,  334,  335,   -1,  337,  338,  339,   -1,
   -1,   -1,  343,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  365,   -1,   -1,   -1,  448,  299,
  366,   -1,  368,   -1,  370,   -1,   -1,  373,   -1,  375,
  376,   -1,  378,  379,   -1,  381,   -1,  383,  384,  385,
  386,  387,  388,  389,   -1,   -1,  392,   -1,  394,   -1,
  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,   -1,
  406,   -1,  408,   -1,  410,   -1,  412,   -1,  414,   -1,
  416,   -1,  418,   -1,  420,   -1,  422,   -1,  424,   -1,
  426,   -1,  428,   -1,  430,   -1,  432,   -1,  434,  369,
   -1,  371,   -1,  373,   -1,  375,  376,  448,  378,  379,
   -1,  381,  448,  383,  384,   -1,  386,  387,  388,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  396,   -1,  398,   -1,
  400,   -1,  402,   -1,  404,   -1,  406,   -1,  408,   -1,
  410,   -1,  412,   -1,  414,   -1,  416,   -1,  418,   -1,
  420,   -1,  422,   -1,  424,   -1,  426,   -1,  428,   -1,
  430,   -1,   -1,  266,  434,  268,   -1,   -1,  271,   -1,
  273,  274,   -1,  276,   -1,  278,   -1,  280,  448,  282,
  283,  284,   -1,   -1,   -1,  288,  289,   -1,   -1,   -1,
   -1,  294,   -1,  296,  297,   -1,   -1,   -1,  301,   -1,
   -1,   -1,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,
  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,
   -1,  266,  335,  268,   -1,  338,  271,   -1,  273,  274,
  343,  276,   -1,  278,   -1,  280,   -1,  282,  283,  284,
   -1,   -1,   -1,  288,  289,   -1,  359,   -1,  361,  294,
   -1,  296,  297,  266,   -1,  268,  301,   -1,  271,   -1,
  305,  274,   -1,  276,   -1,  278,   -1,  280,   -1,  282,
  283,  284,  317,   -1,  319,  288,  289,   -1,  323,  324,
   -1,  294,   -1,  296,   -1,   -1,  331,  332,  301,   -1,
  335,   -1,  305,  338,   -1,   -1,   -1,   -1,  343,   -1,
   -1,   -1,   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,
  323,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,
   -1,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,  374,
  343,   -1,  266,   -1,  268,  448,   -1,  271,   -1,  273,
  274,   -1,  276,   -1,  278,   -1,  280,   -1,  282,  283,
   -1,   -1,   -1,   -1,   -1,  289,   -1,   -1,   -1,   -1,
   -1,   -1,  296,  297,   -1,   -1,   -1,  301,   -1,   -1,
   -1,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,  323,
  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,
   -1,  335,   -1,  448,  338,   -1,  266,   -1,  268,  343,
   -1,  271,   -1,  273,   -1,   -1,  276,   -1,  278,   -1,
  280,   -1,  282,   -1,   -1,   -1,   -1,   -1,  288,  289,
   -1,   -1,   -1,   -1,   -1,  448,  296,  297,   -1,   -1,
   -1,  301,   -1,   -1,   -1,  305,  266,   -1,  268,   -1,
   -1,  271,   -1,   -1,   -1,   -1,  276,  317,   -1,  319,
  280,   -1,   -1,  323,  324,   -1,   -1,   -1,   -1,  289,
   -1,  331,  332,   -1,   -1,  335,  296,   -1,  338,   -1,
   -1,  301,   -1,  343,   -1,  305,   -1,  307,   -1,  309,
   -1,   -1,   -1,   -1,  314,   -1,   -1,  317,   -1,  319,
   -1,   -1,   -1,  323,   -1,   -1,  326,   -1,   -1,   -1,
   -1,  331,  332,   -1,  448,  335,   -1,   -1,  338,   -1,
   -1,  266,  342,  268,   -1,   -1,  271,   -1,   -1,   -1,
   -1,  276,   -1,   -1,   -1,  280,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  289,   -1,  366,  367,   -1,   -1,
   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,   -1,   -1,
  305,   -1,  307,   -1,  309,   -1,  266,   -1,  268,  314,
   -1,  271,  317,   -1,  319,   -1,  276,   -1,  323,   -1,
  280,  326,   -1,   -1,   -1,   -1,  331,  332,  448,  289,
  335,   -1,   -1,  338,   -1,   -1,  296,  342,   -1,   -1,
   -1,  301,   -1,   -1,   -1,  305,   -1,  307,   -1,  309,
   -1,   -1,   -1,   -1,  314,   -1,   -1,  317,   -1,  319,
   -1,  366,   -1,  323,  369,   -1,  326,   -1,  448,   -1,
   -1,  331,  332,   -1,   -1,  335,   -1,   -1,  338,   -1,
  266,   -1,  268,   -1,   -1,  271,   -1,   -1,   -1,   -1,
  276,   -1,  278,   -1,  280,   -1,  282,   -1,   -1,   -1,
   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,   -1,  369,
  296,   -1,  266,   -1,  268,  301,   -1,  271,   -1,  305,
   -1,   -1,  276,   -1,   -1,   -1,  280,   -1,   -1,   -1,
   -1,  317,   -1,  319,   -1,  289,   -1,  323,   -1,   -1,
   -1,   -1,  296,  448,   -1,  331,  332,  301,   -1,  335,
   -1,  305,  338,   -1,   -1,   -1,  266,  343,  268,   -1,
   -1,  271,   -1,  317,   -1,  319,  276,   -1,   -1,  323,
  280,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,  289,
  266,  335,  268,   -1,  338,  271,  296,   -1,  448,   -1,
  276,  301,   -1,   -1,  280,  305,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  289,   -1,   -1,   -1,  317,   -1,  319,
  296,   -1,  366,  323,   -1,  301,   -1,   -1,   -1,  305,
   -1,  331,  332,   -1,  266,  335,  268,   -1,  338,  271,
   -1,  317,   -1,  319,  276,   -1,   -1,  323,  280,   -1,
   -1,   -1,   -1,   -1,   -1,  331,  332,  289,   -1,  335,
   -1,   -1,  338,   -1,  296,   -1,  266,   -1,  268,  301,
   -1,  271,  448,  305,   -1,   -1,  276,   -1,   -1,   -1,
  280,   -1,   -1,   -1,   -1,  317,   -1,  319,   -1,  289,
   -1,  323,   -1,   -1,   -1,   -1,  296,   -1,   -1,  331,
  332,  301,   -1,  335,  448,  305,  338,   -1,   -1,   -1,
  266,   -1,  268,   -1,   -1,  271,   -1,  317,   -1,  319,
  276,   -1,   -1,  323,  280,   -1,   -1,   -1,   -1,   -1,
   -1,  331,  332,  289,   -1,  335,   -1,   -1,  338,   -1,
  296,  256,   -1,   -1,   -1,  301,   -1,   -1,  448,  305,
   -1,   -1,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,
   -1,  317,   -1,  319,  276,   -1,   -1,  323,  280,   -1,
   -1,   -1,  448,   -1,   -1,  331,  332,  289,   -1,  335,
   -1,   -1,  338,   -1,  296,  256,   -1,   -1,   -1,  301,
   -1,  262,   -1,  305,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,   -1,   -1,
   -1,  323,   -1,   -1,   -1,   -1,  448,   -1,   -1,  331,
  332,   -1,   -1,  335,   -1,  340,  338,   -1,  299,   -1,
   -1,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,   -1,   -1,   -1,   -1,   -1,  448,   -1,
  365,  366,  367,   -1,  369,   -1,  371,  372,  373,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  340,
  385,   -1,   -1,   -1,  389,  346,  347,  348,  349,  350,
  351,  352,  353,  354,  355,  356,  357,  256,   -1,   -1,
   -1,   -1,  448,  262,  365,  410,  367,   -1,  369,   -1,
  371,  372,  373,   -1,  375,  376,   -1,  378,  379,   -1,
  381,   -1,  383,  384,  385,  386,  387,  388,  389,  434,
   -1,   -1,   -1,   -1,   -1,  396,   -1,  398,   -1,  400,
  299,  402,   -1,  404,   -1,  406,  448,  408,   -1,  410,
   -1,  412,   -1,  414,   -1,  416,   -1,  418,   -1,  420,
   -1,  422,   -1,  424,   -1,  426,   -1,  428,  256,  430,
   -1,   -1,   -1,  434,  262,   -1,   -1,   -1,   -1,   -1,
   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  365,  366,  367,   -1,
  369,  299,  371,  372,  373,   -1,  375,  376,   -1,   -1,
  379,   -1,  381,   -1,  383,  384,  385,  386,  387,  388,
  389,   -1,   -1,   -1,   -1,   -1,   -1,  396,   -1,  398,
   -1,  400,   -1,  402,   -1,  404,   -1,  406,   -1,  408,
   -1,  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
  256,   -1,   -1,   -1,   -1,  434,  262,  365,  366,  367,
   -1,  369,   -1,  371,  372,  373,   -1,  375,  376,   -1,
   -1,  379,   -1,  381,   -1,  383,  384,   -1,   -1,   -1,
  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,  396,   -1,
  398,   -1,  400,  299,  402,   -1,  404,   -1,  406,   -1,
  408,   -1,  410,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  256,   -1,   -1,   -1,   -1,  434,  262,   -1,   -1,
   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  365,
  366,  367,   -1,  369,  299,  371,  372,  373,   -1,  375,
  376,   -1,   -1,  379,   -1,  381,   -1,  383,  384,   -1,
   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,
  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,   -1,
  406,   -1,  408,   -1,  410,  340,   -1,   -1,   -1,   -1,
   -1,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,  256,   -1,   -1,   -1,   -1,  434,  262,
  365,  366,  367,   -1,  369,   -1,  371,  372,  373,   -1,
  375,  376,   -1,   -1,  379,   -1,  381,   -1,  383,  384,
   -1,   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,
   -1,  396,   -1,  398,   -1,  400,  299,  402,   -1,  404,
   -1,  406,   -1,  408,   -1,  410,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  256,   -1,   -1,   -1,   -1,  434,
  262,   -1,   -1,   -1,   -1,   -1,   -1,  340,   -1,   -1,
   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  365,  366,  367,   -1,  369,  299,  371,  372,
  373,   -1,   -1,   -1,   -1,   -1,  379,   -1,  381,   -1,
  383,  384,  385,   -1,   -1,  388,  389,   -1,   -1,   -1,
   -1,   -1,   -1,  396,   -1,  398,   -1,  400,   -1,  402,
  256,  404,   -1,  406,   -1,  408,  262,  410,  340,   -1,
   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,
   -1,  434,   -1,  365,  366,  367,   -1,  369,   -1,  371,
  372,  373,   -1,  299,   -1,   -1,   -1,  379,   -1,  381,
   -1,  383,  384,  385,   -1,   -1,  388,  389,   -1,   -1,
   -1,   -1,   -1,   -1,  396,   -1,  398,   -1,  400,   -1,
  402,  256,  404,   -1,  406,   -1,  408,  262,  410,   -1,
   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,  434,   -1,   -1,   -1,   -1,   -1,   -1,  365,
  366,  367,   -1,  369,  299,  371,  372,  373,   -1,   -1,
   -1,   -1,   -1,  379,   -1,  381,   -1,  383,  384,  385,
   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,
  396,   -1,  398,   -1,  400,   -1,  402,  256,  404,   -1,
  406,   -1,  408,  262,  410,  340,   -1,   -1,   -1,   -1,
   -1,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,   -1,   -1,   -1,   -1,   -1,  434,   -1,
  365,  366,  367,   -1,  369,   -1,  371,  372,  373,   -1,
  299,   -1,   -1,   -1,  379,   -1,  381,   -1,  383,  384,
  385,   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  400,   -1,  402,  256,  404,
   -1,  406,   -1,  408,  262,  410,   -1,   -1,   -1,   -1,
   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,  434,
   -1,   -1,   -1,   -1,   -1,   -1,  365,  366,  367,   -1,
  369,  299,  371,  372,  373,   -1,   -1,   -1,   -1,   -1,
  379,   -1,  381,   -1,  383,  384,  385,   -1,   -1,  388,
  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  400,   -1,  402,  256,  404,   -1,  406,   -1,  408,
  262,  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
   -1,   -1,   -1,   -1,   -1,  434,   -1,  365,  366,  367,
   -1,  369,   -1,  371,  372,  373,   -1,  299,   -1,   -1,
   -1,  379,   -1,  381,   -1,  383,  384,  385,   -1,   -1,
  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  400,   -1,  402,  256,  404,   -1,  406,   -1,
  408,  262,  410,   -1,   -1,   -1,   -1,   -1,  340,   -1,
   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,  434,   -1,   -1,   -1,
   -1,   -1,   -1,  365,  366,  367,   -1,  369,  299,  371,
  372,  373,   -1,   -1,   -1,   -1,   -1,  379,   -1,  381,
   -1,  383,  384,  385,   -1,   -1,  388,  389,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  256,   -1,  400,   -1,
  402,   -1,  404,   -1,  406,   -1,  408,   -1,  410,  340,
   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,
  351,  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,
   -1,   -1,  434,   -1,  365,  366,  367,   -1,  369,   -1,
  371,  372,  373,   -1,   -1,   -1,   -1,   -1,  379,   -1,
  381,   -1,  383,  384,  385,   -1,   -1,  388,  389,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  256,   -1,  400,
   -1,  402,   -1,  404,   -1,  406,   -1,  408,   -1,  410,
  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,   -1,   -1,
   -1,   -1,   -1,  434,   -1,  365,  366,  367,   -1,  369,
   -1,  371,  372,  373,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  383,  384,  385,   -1,   -1,  388,  389,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  256,   -1,
   -1,   -1,   -1,   -1,  404,   -1,  406,   -1,  408,   -1,
  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,   -1,
   -1,   -1,   -1,   -1,  434,   -1,  365,  366,  367,   -1,
  369,   -1,  371,  372,  373,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  383,  384,  385,   -1,   -1,  388,
  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  256,
   -1,   -1,   -1,   -1,   -1,  404,   -1,  406,   -1,  408,
   -1,  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
   -1,   -1,   -1,   -1,   -1,  434,   -1,  365,  366,  367,
   -1,  369,   -1,  371,  372,  373,  256,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  383,  384,  385,   -1,   -1,
  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  404,   -1,  406,   -1,
  408,   -1,  410,  340,   -1,   -1,   -1,   -1,   -1,  346,
  347,  348,  349,  350,  351,  352,  353,  354,  355,  356,
  357,   -1,   -1,   -1,   -1,   -1,  434,   -1,  365,  366,
  367,   -1,  369,   -1,  371,  372,  373,  256,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  383,  384,  385,   -1,
  340,  388,  389,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,   -1,   -1,
   -1,  408,   -1,  410,   -1,  365,  366,  367,   -1,  369,
   -1,  371,  372,  373,  256,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  383,  384,  385,   -1,  434,  388,  389,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  408,   -1,
  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,   -1,
   -1,   -1,   -1,   -1,  434,   -1,  365,  366,  367,   -1,
  369,   -1,  371,  372,  373,  256,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  384,  385,   -1,  340,  388,
  389,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,  408,
   -1,  410,   -1,  365,  366,  367,   -1,  369,   -1,  371,
  372,  373,  256,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  384,  385,   -1,  434,  388,  389,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  408,   -1,  410,  340,
   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,
  351,  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,
   -1,   -1,  434,   -1,  365,  366,  367,   -1,  369,   -1,
  371,  372,  373,  256,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  384,  385,   -1,  340,   -1,  389,   -1,
   -1,   -1,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,   -1,   -1,   -1,  408,   -1,  410,
   -1,  365,  366,  367,   -1,  369,   -1,  371,  372,  373,
  256,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  384,  385,   -1,  434,   -1,  389,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  408,   -1,  410,  340,   -1,   -1,
   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,
  434,   -1,  365,  366,  367,   -1,  369,   -1,  371,  372,
  373,  256,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  385,   -1,  340,   -1,  389,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,   -1,   -1,   -1,  408,   -1,  410,  256,  365,
  366,  367,   -1,  369,  262,  371,  372,  373,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  385,
   -1,  434,   -1,  389,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  299,  408,   -1,  410,  340,   -1,  262,   -1,   -1,
   -1,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,   -1,   -1,   -1,   -1,   -1,  434,   -1,
  365,  366,  367,   -1,  369,   -1,  371,  372,  373,   -1,
   -1,   -1,   -1,   -1,  299,   -1,   -1,   -1,   -1,   -1,
  385,   -1,   -1,   -1,  389,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  366,   -1,
   -1,  369,   -1,  371,   -1,  410,   -1,  375,  376,   -1,
   -1,  379,   -1,  381,   -1,  383,  384,   -1,   -1,   -1,
  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,  396,  434,
  398,   -1,  400,  262,  402,   -1,  404,   -1,  406,   -1,
  408,  366,  410,  368,  369,  370,  371,   -1,   -1,   -1,
  375,  376,   -1,   -1,  379,   -1,  381,   -1,  383,  384,
  385,  386,  387,  388,  389,   -1,  434,  392,   -1,  394,
  299,  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,
   -1,  406,   -1,  408,   -1,  410,   -1,  412,   -1,  414,
   -1,  416,   -1,  418,  262,  420,   -1,  422,   -1,  424,
   -1,  426,   -1,  428,   -1,  430,   -1,  432,   -1,  434,
   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,   -1,
   -1,  299,   -1,   -1,   -1,   -1,  365,   -1,  367,   -1,
  369,   -1,  371,  372,  373,   -1,   -1,   -1,   -1,   -1,
  379,   -1,  381,   -1,  383,  384,   -1,   -1,   -1,  388,
  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  400,   -1,  402,   -1,  404,   -1,  406,   -1,  408,
   -1,  410,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  365,  366,   -1,
  368,   -1,  370,  371,   -1,  434,   -1,  375,  376,   -1,
   -1,  379,   -1,  381,   -1,  383,  384,  385,  386,  387,
  388,  389,   -1,  261,  392,   -1,  394,   -1,  396,   -1,
  398,   -1,  400,   -1,  402,   -1,  404,   -1,  406,   -1,
  408,   -1,  410,   -1,   -1,   -1,   -1,  285,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  261,   -1,
  298,   -1,   -1,   -1,  432,  303,  434,   -1,  306,   -1,
  308,   -1,  310,  311,  312,  313,   -1,   -1,   -1,   -1,
  318,   -1,  285,   -1,  322,   -1,   -1,   -1,  326,   -1,
   -1,   -1,   -1,  261,   -1,  298,  334,   -1,   -1,  337,
  303,  339,   -1,   -1,   -1,  308,   -1,  310,  311,  312,
  313,   -1,   -1,   -1,   -1,  318,   -1,  285,   -1,  322,
   -1,   -1,   -1,  261,   -1,  263,   -1,   -1,  366,   -1,
  298,  334,   -1,   -1,  337,  303,  339,   -1,   -1,   -1,
  308,   -1,  310,  311,  312,  313,   -1,  285,  316,   -1,
  318,  389,   -1,  261,  322,   -1,  359,   -1,  361,   -1,
  298,   -1,  365,   -1,   -1,  303,  334,   -1,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,   -1,   -1,
  318,   -1,   -1,  261,  322,  263,   -1,   -1,   -1,   -1,
  298,   -1,   -1,   -1,   -1,  303,  334,  365,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,   -1,   -1,
  318,   -1,   -1,  261,  322,  263,   -1,   -1,   -1,   -1,
  298,   -1,   -1,   -1,   -1,  303,  334,  365,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,  316,   -1,
  318,   -1,   -1,  261,  322,   -1,   -1,   -1,   -1,   -1,
  298,  359,   -1,  361,   -1,  303,  334,   -1,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,  316,   -1,
  318,   -1,   -1,  261,  322,  263,   -1,   -1,   -1,   -1,
  298,   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,  316,   -1,
  318,   -1,   -1,  261,  322,   -1,   -1,   -1,   -1,   -1,
  298,   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,   -1,   -1,
  318,   -1,   -1,  261,  322,  263,   -1,   -1,   -1,   -1,
  298,   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,  316,   -1,
  318,   -1,   -1,  261,  322,   -1,   -1,   -1,   -1,   -1,
  298,   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,   -1,   -1,
  318,   -1,   -1,  261,  322,   -1,   -1,   -1,   -1,   -1,
  298,   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,   -1,   -1,
  318,   -1,   -1,   -1,  322,   -1,   -1,   -1,   -1,   -1,
  298,   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,   -1,   -1,   -1,
  318,   -1,   -1,   -1,  322,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  334,   -1,   -1,  337,
   -1,  339,
  };

#line 5631 "cs-parser.jay"

// <summary>
//   A class used to pass around variable declarations and constants
// </summary>
public class VariableDeclaration {
	public string identifier;
	public Expression expression_or_array_initializer;
	public Location Location;
	public Attributes OptAttributes;
	public string DocComment;

	public VariableDeclaration (LocatedToken lt, object eoai, Attributes opt_attrs)
	{
		this.identifier = lt.Value;
		if (eoai is ArrayList) {
			this.expression_or_array_initializer = new ArrayCreation (CSharpParser.current_array_type, "", (ArrayList)eoai, lt.Location);
		} else {
			this.expression_or_array_initializer = (Expression)eoai;
		}
		this.Location = lt.Location;
		this.OptAttributes = opt_attrs;
	}

	public VariableDeclaration (LocatedToken lt, object eoai) : this (lt, eoai, null)
	{
	}
}

// <summary>
//   A class used to hold info about an indexer declarator
// </summary>
public class IndexerDeclaration {
	public Expression type;
	public MemberName interface_type;
	public Parameters param_list;
	public Location location;

	public IndexerDeclaration (Expression type, MemberName interface_type,
				   Parameters param_list, Location loc)
	{
		this.type = type;
		this.interface_type = interface_type;
		this.param_list = param_list;
		this.location = loc;
	}
}

//
// We use this when we do not have an object in advance that is an IAnonymousHost
//
public class SimpleAnonymousHost : IAnonymousHost {
	public static readonly SimpleAnonymousHost Simple = new SimpleAnonymousHost ();

	bool yields;
	ArrayList anonymous_methods;

	public static SimpleAnonymousHost GetSimple () {
		Simple.yields = false;
		Simple.anonymous_methods = null;
		return Simple;
	}

	public void SetYields ()
	{
		yields = true;
	}

	public void AddAnonymousMethod (AnonymousMethodExpression anonymous)
	{
		if (anonymous_methods == null)
			anonymous_methods = new ArrayList ();
		anonymous_methods.Add (anonymous);
	}

	public void Propagate (IAnonymousHost real_host)
	{
		if (yields)
			real_host.SetYields ();
		if (anonymous_methods != null) {
			foreach (AnonymousMethodExpression ame in anonymous_methods)
				real_host.AddAnonymousMethod (ame);
		}
	}
}

// <summary>
//  A class used to hold info about an operator declarator
// </summary>
struct OperatorDeclaration {
	public readonly Operator.OpType optype;
	public readonly Expression ret_type;
	public readonly Location location;

	public OperatorDeclaration (Operator.OpType op, Expression ret_type, Location location)
	{
		optype = op;
		this.ret_type = ret_type;
		this.location = location;
	}
}

void Error_ExpectingTypeName (Expression expr)
{
	if (expr is Invocation){
		Report.Error (1002, expr.Location, "Expecting `;'");
	} else {
		expr.Error_InvalidExpressionStatement ();
	}
}

static void Error_ParameterModifierNotValid (string modifier, Location loc)
{
	Report.Error (631, loc, "The parameter modifier `{0}' is not valid in this context",
			              modifier);
}

static void Error_DuplicateParameterModifier (Location loc, Parameter.Modifier mod)
{
	Report.Error (1107, loc, "Duplicate parameter modifier `{0}'",
  		Parameter.GetModifierSignature (mod));
}

static void Error_TypeExpected (Location loc)
{
	Report.Error (1031, loc, "Type expected");
}

void push_current_class (TypeContainer tc, object partial_token)
{
	if (partial_token != null)
		current_container = current_container.AddPartial (tc);
	else
		current_container = current_container.AddTypeContainer (tc);
		
	current_namespace.AddType (tc);
	current_class = tc;
}

DeclSpace pop_current_class ()
{
	DeclSpace retval = current_class;

	current_class = current_class.Parent;
	current_container = current_class.PartialContainer;

	return retval;
}

// <summary>
//   Given the @class_name name, it creates a fully qualified name
//   based on the containing declaration space
// </summary>
MemberName
MakeName (MemberName class_name)
{
	Namespace ns = current_namespace.NS;

	if (current_container.Name.Length == 0){
		if (ns.Name.Length != 0)
			return new MemberName (ns.MemberName, class_name);
		else
			return class_name;
	} else {
		return new MemberName (current_container.MemberName, class_name);
	}
}

Block declare_local_variables (Expression type, ArrayList variable_declarators, Location loc)
{
	Block implicit_block;
	ArrayList inits = null;

	//
	// We use the `Used' property to check whether statements
	// have been added to the current block.  If so, we need
	// to create another block to contain the new declaration
	// otherwise, as an optimization, we use the same block to
	// add the declaration.
	//
	// FIXME: A further optimization is to check if the statements
	// that were added were added as part of the initialization
	// below.  In which case, no other statements have been executed
	// and we might be able to reduce the number of blocks for
	// situations like this:
	//
	// int j = 1;  int k = j + 1;
	//
	if (current_block.Used)
		implicit_block = new Block (current_block, loc, Location.Null);
	else
		implicit_block = current_block;

	foreach (VariableDeclaration decl in variable_declarators){

		if (implicit_block.AddVariable (type, decl.identifier, decl.Location) != null) {
			if (decl.expression_or_array_initializer != null){
				if (inits == null)
					inits = new ArrayList (4);
				inits.Add (decl);
			}
		}
	}

	if (inits == null)
		return implicit_block;

	foreach (VariableDeclaration decl in inits){
		Assign assign;
		Expression expr = decl.expression_or_array_initializer;
		
		LocalVariableReference var;
		var = new LocalVariableReference (implicit_block, decl.identifier, loc);

		assign = new Assign (var, expr, decl.Location);

		implicit_block.AddStatement (new StatementExpression (assign));
	}
	
	return implicit_block;
}

Block declare_local_constants (Expression type, ArrayList declarators)
{
	Block implicit_block;

	if (current_block.Used)
		implicit_block = new Block (current_block);
	else
		implicit_block = current_block;

	foreach (VariableDeclaration decl in declarators){
		implicit_block.AddConstant (type, decl.identifier, (Expression) decl.expression_or_array_initializer, decl.Location);
	}
	
	return implicit_block;
}

string CheckAttributeTarget (string a, Location l)
{
	switch (a) {
	case "assembly" : case "module" : case "field" : case "method" : case "param" : case "property" : case "type" :
			return a;
	}

	Report.Warning (658, 1, l,
		 "`{0}' is invalid attribute target. All attributes in this attribute section will be ignored", a);
	return string.Empty;
}

static bool IsUnaryOperator (Operator.OpType op)
{
	switch (op) {
		
	case Operator.OpType.LogicalNot: 
	case Operator.OpType.OnesComplement: 
	case Operator.OpType.Increment:
	case Operator.OpType.Decrement:
	case Operator.OpType.True: 
	case Operator.OpType.False: 
	case Operator.OpType.UnaryPlus: 
	case Operator.OpType.UnaryNegation:
		return true;
	}
	return false;
}

void syntax_error (Location l, string msg)
{
	Report.Error (1003, l, "Syntax error, " + msg);
}

void note (string s)
{
	// Used to put annotations
}

Tokenizer lexer;

public Tokenizer Lexer {
	get {
		return lexer;
	}
}		   

static CSharpParser ()
{
	oob_stack = new Stack ();
}

public CSharpParser (SeekableStreamReader reader, SourceFile file, ArrayList defines, CompilationUnit cu)
{
	this.file = file;
	this.cu = cu;
	
	current_namespace = cu.DefaultNamespace;
	current_class = current_namespace.SlaveDeclSpace;
	current_container = current_class.PartialContainer; // == RootContest.ToplevelTypes
	oob_stack.Clear ();
	lexer = new Tokenizer (reader, file, defines);
}

public void parse ()
{
	int errors = Report.Errors;
	try {
		if (yacc_verbose_flag > 1)
			yyparse (lexer, new yydebug.yyDebugSimple ());
		else
			yyparse (lexer);
		Tokenizer tokenizer = lexer as Tokenizer;
		tokenizer.cleanup ();
	} catch (Exception e){
		//
		// Removed for production use, use parser verbose to get the output.
		//
		// Console.WriteLine (e);
		if (Report.Errors == errors)
			Report.Error (-25, lexer.Location, "Parsing error");
		if (yacc_verbose_flag > 0)
			Console.WriteLine (e);
	}

	if (RootContext.ToplevelTypes.NamespaceEntry != null)
		throw new InternalErrorException ("who set it?");
}

static void CheckToken (int error, int yyToken, string msg, Location loc)
{
	if (yyToken >= Token.FIRST_KEYWORD && yyToken <= Token.LAST_KEYWORD)
		Report.Error (error, loc, "{0}: `{1}' is a keyword", msg, yyNames [yyToken].ToLower ());
	else
		Report.Error (error, loc, msg);
}

void CheckIdentifierToken (int yyToken, Location loc)
{
	CheckToken (1041, yyToken, "Identifier expected", loc);
}

string ConsumeStoredComment ()
{
	string s = tmpComment;
	tmpComment = null;
	Lexer.doc_state = XmlCommentState.Allowed;
	return s;
}

Location GetLocation (object obj)
{
	if (obj is MemberCore)
		return ((MemberCore) obj).Location;
	if (obj is MemberName)
		return ((MemberName) obj).Location;
	if (obj is LocatedToken)
		return ((LocatedToken) obj).Location;
	if (obj is Location)
		return (Location) obj;
	return lexer.Location;
}

void start_block (Location loc)
{
	if (current_block == null || parsing_anonymous_method) {
		current_block = new ToplevelBlock (current_block, current_local_parameters, current_generic_method, loc);
		parsing_anonymous_method = false;
	} else {
		current_block = new ExplicitBlock (current_block, loc, Location.Null);
	}
}

Block
end_block (Location loc)
{
	Block retval = current_block.Explicit;
	retval.SetEndLocation (loc);
	current_block = retval.Parent;
	return retval;
}

void
start_anonymous (bool lambda, Parameters parameters, Location loc)
{
	oob_stack.Push (current_anonymous_method);
	oob_stack.Push (current_local_parameters);

	current_local_parameters = parameters;

	ToplevelBlock top_current_block = current_block == null ? null : current_block.Toplevel;

	current_anonymous_method = lambda 
		? new LambdaExpression (
			current_anonymous_method, current_generic_method, current_container,
			parameters, top_current_block, loc) 
		: new AnonymousMethodExpression (
			current_anonymous_method, current_generic_method, current_container,
			parameters, top_current_block, loc);

	// Force the next block to be created as a ToplevelBlock
	parsing_anonymous_method = true;
}

/*
 * Completes the anonymous method processing, if lambda_expr is null, this
 * means that we have a Statement instead of an Expression embedded 
 */
AnonymousMethodExpression 
end_anonymous (ToplevelBlock anon_block, Location loc)
{
	AnonymousMethodExpression retval;

	if (RootContext.Version == LanguageVersion.ISO_1){
		Report.FeatureIsNotAvailable (loc, "anonymous methods");
		retval = null;
	} else  {
		current_anonymous_method.Block = anon_block;
		if ((anonymous_host != null) && (current_anonymous_method.Parent == null))
			anonymous_host.AddAnonymousMethod (current_anonymous_method);

		retval = current_anonymous_method;
	}

	current_local_parameters = (Parameters) oob_stack.Pop ();
	current_anonymous_method = (AnonymousMethodExpression) oob_stack.Pop ();

	return retval;
}

/* end end end */
}
#line default
namespace yydebug {
        using System;
	 internal interface yyDebug {
		 void push (int state, Object value);
		 void lex (int state, int token, string name, Object value);
		 void shift (int from, int to, int errorFlag);
		 void pop (int state);
		 void discard (int state, int token, string name, Object value);
		 void reduce (int from, int to, int rule, string text, int len);
		 void shift (int from, int to);
		 void accept (Object value);
		 void error (string message);
		 void reject ();
	 }
	 
	 class yyDebugSimple : yyDebug {
		 void println (string s){
			 Console.Error.WriteLine (s);
		 }
		 
		 public void push (int state, Object value) {
			 println ("push\tstate "+state+"\tvalue "+value);
		 }
		 
		 public void lex (int state, int token, string name, Object value) {
			 println("lex\tstate "+state+"\treading "+name+"\tvalue "+value);
		 }
		 
		 public void shift (int from, int to, int errorFlag) {
			 switch (errorFlag) {
			 default:				// normally
				 println("shift\tfrom state "+from+" to "+to);
				 break;
			 case 0: case 1: case 2:		// in error recovery
				 println("shift\tfrom state "+from+" to "+to
					     +"\t"+errorFlag+" left to recover");
				 break;
			 case 3:				// normally
				 println("shift\tfrom state "+from+" to "+to+"\ton error");
				 break;
			 }
		 }
		 
		 public void pop (int state) {
			 println("pop\tstate "+state+"\ton error");
		 }
		 
		 public void discard (int state, int token, string name, Object value) {
			 println("discard\tstate "+state+"\ttoken "+name+"\tvalue "+value);
		 }
		 
		 public void reduce (int from, int to, int rule, string text, int len) {
			 println("reduce\tstate "+from+"\tuncover "+to
				     +"\trule ("+rule+") "+text);
		 }
		 
		 public void shift (int from, int to) {
			 println("goto\tfrom state "+from+" to "+to);
		 }
		 
		 public void accept (Object value) {
			 println("accept\tvalue "+value);
		 }
		 
		 public void error (string message) {
			 println("error\t"+message);
		 }
		 
		 public void reject () {
			 println("reject");
		 }
		 
	 }
}
// %token constants
 class Token {
  public const int EOF = 257;
  public const int NONE = 258;
  public const int ERROR = 259;
  public const int FIRST_KEYWORD = 260;
  public const int ABSTRACT = 261;
  public const int AS = 262;
  public const int ADD = 263;
  public const int ASSEMBLY = 264;
  public const int BASE = 265;
  public const int BOOL = 266;
  public const int BREAK = 267;
  public const int BYTE = 268;
  public const int CASE = 269;
  public const int CATCH = 270;
  public const int CHAR = 271;
  public const int CHECKED = 272;
  public const int CLASS = 273;
  public const int CONST = 274;
  public const int CONTINUE = 275;
  public const int DECIMAL = 276;
  public const int DEFAULT = 277;
  public const int DELEGATE = 278;
  public const int DO = 279;
  public const int DOUBLE = 280;
  public const int ELSE = 281;
  public const int ENUM = 282;
  public const int EVENT = 283;
  public const int EXPLICIT = 284;
  public const int EXTERN = 285;
  public const int FALSE = 286;
  public const int FINALLY = 287;
  public const int FIXED = 288;
  public const int FLOAT = 289;
  public const int FOR = 290;
  public const int FOREACH = 291;
  public const int GOTO = 292;
  public const int IF = 293;
  public const int IMPLICIT = 294;
  public const int IN = 295;
  public const int INT = 296;
  public const int INTERFACE = 297;
  public const int INTERNAL = 298;
  public const int IS = 299;
  public const int LOCK = 300;
  public const int LONG = 301;
  public const int NAMESPACE = 302;
  public const int NEW = 303;
  public const int NULL = 304;
  public const int OBJECT = 305;
  public const int OPERATOR = 306;
  public const int OUT = 307;
  public const int OVERRIDE = 308;
  public const int PARAMS = 309;
  public const int PRIVATE = 310;
  public const int PROTECTED = 311;
  public const int PUBLIC = 312;
  public const int READONLY = 313;
  public const int REF = 314;
  public const int RETURN = 315;
  public const int REMOVE = 316;
  public const int SBYTE = 317;
  public const int SEALED = 318;
  public const int SHORT = 319;
  public const int SIZEOF = 320;
  public const int STACKALLOC = 321;
  public const int STATIC = 322;
  public const int STRING = 323;
  public const int STRUCT = 324;
  public const int SWITCH = 325;
  public const int THIS = 326;
  public const int THROW = 327;
  public const int TRUE = 328;
  public const int TRY = 329;
  public const int TYPEOF = 330;
  public const int UINT = 331;
  public const int ULONG = 332;
  public const int UNCHECKED = 333;
  public const int UNSAFE = 334;
  public const int USHORT = 335;
  public const int USING = 336;
  public const int VIRTUAL = 337;
  public const int VOID = 338;
  public const int VOLATILE = 339;
  public const int WHERE = 340;
  public const int WHILE = 341;
  public const int ARGLIST = 342;
  public const int PARTIAL = 343;
  public const int ARROW = 344;
  public const int QUERY_FIRST_TOKEN = 345;
  public const int FROM = 346;
  public const int JOIN = 347;
  public const int ON = 348;
  public const int EQUALS = 349;
  public const int SELECT = 350;
  public const int GROUP = 351;
  public const int BY = 352;
  public const int LET = 353;
  public const int ORDERBY = 354;
  public const int ASCENDING = 355;
  public const int DESCENDING = 356;
  public const int INTO = 357;
  public const int QUERY_LAST_TOKEN = 358;
  public const int GET = 359;
  public const int get = 360;
  public const int SET = 361;
  public const int set = 362;
  public const int LAST_KEYWORD = 363;
  public const int OPEN_BRACE = 364;
  public const int CLOSE_BRACE = 365;
  public const int OPEN_BRACKET = 366;
  public const int CLOSE_BRACKET = 367;
  public const int OPEN_PARENS = 368;
  public const int CLOSE_PARENS = 369;
  public const int DOT = 370;
  public const int COMMA = 371;
  public const int COLON = 372;
  public const int SEMICOLON = 373;
  public const int TILDE = 374;
  public const int PLUS = 375;
  public const int MINUS = 376;
  public const int BANG = 377;
  public const int ASSIGN = 378;
  public const int OP_LT = 379;
  public const int OP_GENERICS_LT = 380;
  public const int OP_GT = 381;
  public const int OP_GENERICS_GT = 382;
  public const int BITWISE_AND = 383;
  public const int BITWISE_OR = 384;
  public const int STAR = 385;
  public const int PERCENT = 386;
  public const int DIV = 387;
  public const int CARRET = 388;
  public const int INTERR = 389;
  public const int DOUBLE_COLON = 390;
  public const int OP_INC = 392;
  public const int OP_DEC = 394;
  public const int OP_SHIFT_LEFT = 396;
  public const int OP_SHIFT_RIGHT = 398;
  public const int OP_LE = 400;
  public const int OP_GE = 402;
  public const int OP_EQ = 404;
  public const int OP_NE = 406;
  public const int OP_AND = 408;
  public const int OP_OR = 410;
  public const int OP_MULT_ASSIGN = 412;
  public const int OP_DIV_ASSIGN = 414;
  public const int OP_MOD_ASSIGN = 416;
  public const int OP_ADD_ASSIGN = 418;
  public const int OP_SUB_ASSIGN = 420;
  public const int OP_SHIFT_LEFT_ASSIGN = 422;
  public const int OP_SHIFT_RIGHT_ASSIGN = 424;
  public const int OP_AND_ASSIGN = 426;
  public const int OP_XOR_ASSIGN = 428;
  public const int OP_OR_ASSIGN = 430;
  public const int OP_PTR = 432;
  public const int OP_COALESCING = 434;
  public const int LITERAL_INTEGER = 436;
  public const int LITERAL_FLOAT = 438;
  public const int LITERAL_DOUBLE = 440;
  public const int LITERAL_DECIMAL = 442;
  public const int LITERAL_CHARACTER = 444;
  public const int LITERAL_STRING = 446;
  public const int IDENTIFIER = 448;
  public const int OPEN_PARENS_LAMBDA = 449;
  public const int CLOSE_PARENS_CAST = 450;
  public const int CLOSE_PARENS_NO_CAST = 451;
  public const int CLOSE_PARENS_OPEN_PARENS = 452;
  public const int CLOSE_PARENS_MINUS = 453;
  public const int DEFAULT_OPEN_PARENS = 454;
  public const int GENERIC_DIMENSION = 455;
  public const int DEFAULT_COLON = 456;
  public const int LOWPREC = 457;
  public const int UMINUS = 458;
  public const int HIGHPREC = 459;
  public const int yyErrorCode = 256;
 }
 namespace yyParser {
  using System;
  /** thrown for irrecoverable syntax errors and stack overflow.
    */
  internal class yyException : System.Exception {
    public yyException (string message) : base (message) {
    }
  }

  /** must be implemented by a scanner object to supply input to the parser.
    */
  internal interface yyInput {
    /** move on to next token.
        @return false if positioned beyond tokens.
        @throws IOException on input error.
      */
    bool advance (); // throws java.io.IOException;
    /** classifies current token.
        Should not be called if advance() returned false.
        @return current %token or single character.
      */
    int token ();
    /** associated with current token.
        Should not be called if advance() returned false.
        @return value for token().
      */
    Object value ();
  }
 }
} // close outermost namespace, that MUST HAVE BEEN opened in the prolog
