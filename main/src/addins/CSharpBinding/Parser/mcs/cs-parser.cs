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
		FullNamedExpression implicit_value_parameter_type;
		Parameters indexer_parameters;

		/// <summary>
		///   Hack to help create non-typed array initializer
		/// </summary>
		public static FullNamedExpression current_array_type;
		FullNamedExpression pushed_current_array_type;

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
				current_class, (FullNamedExpression) yyVals[-2+yyTop], (string) constant.identifier, 
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
		FullNamedExpression type = (FullNamedExpression) yyVals[-2+yyTop];
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
			FullNamedExpression type = (FullNamedExpression) yyVals[-2+yyTop];
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
						     (FullNamedExpression) yyVals[-7+yyTop], current_local_parameters);

			generic.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);
		}

		method = new Method (current_class, generic, (FullNamedExpression) yyVals[-7+yyTop], (int) yyVals[-8+yyTop], false,
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
			if (p.HasExtensionMethodModifier)
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
		yyVal = new Parameter ((FullNamedExpression) yyVals[-1+yyTop], lt.Value, (Parameter.Modifier) yyVals[-2+yyTop], (Attributes) yyVals[-3+yyTop], lt.Location);
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
		yyVal = new ParamsParameter ((FullNamedExpression) yyVals[-1+yyTop], lt.Value, (Attributes) yyVals[-3+yyTop], lt.Location);
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
		implicit_value_parameter_type = (FullNamedExpression) yyVals[-3+yyTop];

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

		prop = new Property (current_class, (FullNamedExpression) yyVals[-7+yyTop], (int) yyVals[-8+yyTop], false,
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
						     (FullNamedExpression) yyVals[-8+yyTop], (Parameters) yyVals[-5+yyTop]);

			generic.SetParameterInfo ((ArrayList) yyVals[-2+yyTop]);
		}

		yyVal = new Method (current_class, generic, (FullNamedExpression) yyVals[-8+yyTop], (int) yyVals[-9+yyTop], true, name,
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
		implicit_value_parameter_type = (FullNamedExpression)yyVals[-2+yyTop];
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
			p = new Property (current_class, (FullNamedExpression) yyVals[-6+yyTop], (int) yyVals[-7+yyTop], true,
				   name, (Attributes) yyVals[-8+yyTop],
				   null, null, false);

			Report.Error (548, p.Location, "`{0}': property or indexer must have at least one accessor", p.GetSignatureForError ());
			break;
		}

		Accessors accessor = (Accessors) yyVals[-2+yyTop];
		p = new Property (current_class, (FullNamedExpression) yyVals[-6+yyTop], (int) yyVals[-7+yyTop], true,
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
		yyVal = new EventField (current_class, (FullNamedExpression) yyVals[-2+yyTop], (int) yyVals[-4+yyTop], true,
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
		implicit_value_parameter_type = (FullNamedExpression) yyVals[-2+yyTop];
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
		implicit_value_parameter_type = (FullNamedExpression)yyVals[-5+yyTop];
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
			i = new Indexer (current_class, (FullNamedExpression) yyVals[-9+yyTop],
				  new MemberName (TypeContainer.DefaultIndexerName, (Location) yyVals[-8+yyTop]),
				  (int) yyVals[-10+yyTop], true, (Parameters) yyVals[-6+yyTop], (Attributes) yyVals[-11+yyTop],
				  null, null, false);

			Report.Error (548, i.Location, "`{0}': property or indexer must have at least one accessor", i.GetSignatureForError ());
			break;
		}

		Accessors accessors = (Accessors) yyVals[-2+yyTop];
		i = new Indexer (current_class, (FullNamedExpression) yyVals[-9+yyTop],
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

		yyVal = new OperatorDeclaration (op, (FullNamedExpression) yyVals[-6+yyTop], loc);
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

		yyVal = new OperatorDeclaration (Operator.OpType.Implicit, (FullNamedExpression) yyVals[-4+yyTop], loc);
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

		yyVal = new OperatorDeclaration (Operator.OpType.Explicit, (FullNamedExpression) yyVals[-4+yyTop], loc);
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
				current_class, (FullNamedExpression) yyVals[-2+yyTop], (int) yyVals[-4+yyTop], false, name,
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
		implicit_value_parameter_type = (FullNamedExpression) yyVals[-2+yyTop];  
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
					current_class, (FullNamedExpression) yyVals[-6+yyTop], (int) yyVals[-8+yyTop], false, name,
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

		yyVal = new IndexerDeclaration ((FullNamedExpression) yyVals[-4+yyTop], null, pars, (Location) yyVals[-3+yyTop]);
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
		yyVal = new IndexerDeclaration ((FullNamedExpression) yyVals[-6+yyTop], name, pars, (Location) yyVals[-3+yyTop]);

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
		Enum e = new Enum (current_namespace, current_class, (FullNamedExpression) yyVals[-3+yyTop], (int) yyVals[-6+yyTop],
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

		Delegate del = new Delegate (current_namespace, current_class, (FullNamedExpression) yyVals[-4+yyTop],
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
			yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], "?", lexer.Location);
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
		yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], "*", Lexer.Location);
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
			yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], "?", lexer.Location);
	  }
  break;
case 365:
#line 3045 "cs-parser.jay"
  {
		Location loc = GetLocation (yyVals[-1+yyTop]);
		if (loc.IsNull)
			loc = lexer.Location;
		yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], (string) yyVals[0+yyTop], loc);
	  }
  break;
case 366:
#line 3052 "cs-parser.jay"
  {
		Location loc = GetLocation (yyVals[-1+yyTop]);
		if (loc.IsNull)
			loc = lexer.Location;
		yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], "*", loc);
	  }
  break;
case 367:
#line 3064 "cs-parser.jay"
  {
		FullNamedExpression e = yyVals[-1+yyTop] as FullNamedExpression;
		if (e != null)
			yyVal = new ComposedCast (e, "*");
		else
			Error_TypeExpected (GetLocation (yyVals[-1+yyTop]));
	  }
  break;
case 368:
#line 3075 "cs-parser.jay"
  {
		ArrayList types = new ArrayList (2);
		types.Add (yyVals[0+yyTop]);
		yyVal = types;
	  }
  break;
case 369:
#line 3081 "cs-parser.jay"
  {
		ArrayList types = (ArrayList) yyVals[-2+yyTop];
		types.Add (yyVals[0+yyTop]);
		yyVal = types;
	  }
  break;
case 370:
#line 3090 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] is ComposedCast)
			Report.Error (1521, GetLocation (yyVals[0+yyTop]), "Invalid base type `{0}'", ((ComposedCast)yyVals[0+yyTop]).GetSignatureForError ());
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 371:
#line 3102 "cs-parser.jay"
  { yyVal = TypeManager.system_object_expr; }
  break;
case 372:
#line 3103 "cs-parser.jay"
  { yyVal = TypeManager.system_string_expr; }
  break;
case 373:
#line 3104 "cs-parser.jay"
  { yyVal = TypeManager.system_boolean_expr; }
  break;
case 374:
#line 3105 "cs-parser.jay"
  { yyVal = TypeManager.system_decimal_expr; }
  break;
case 375:
#line 3106 "cs-parser.jay"
  { yyVal = TypeManager.system_single_expr; }
  break;
case 376:
#line 3107 "cs-parser.jay"
  { yyVal = TypeManager.system_double_expr; }
  break;
case 378:
#line 3112 "cs-parser.jay"
  { yyVal = TypeManager.system_sbyte_expr; }
  break;
case 379:
#line 3113 "cs-parser.jay"
  { yyVal = TypeManager.system_byte_expr; }
  break;
case 380:
#line 3114 "cs-parser.jay"
  { yyVal = TypeManager.system_int16_expr; }
  break;
case 381:
#line 3115 "cs-parser.jay"
  { yyVal = TypeManager.system_uint16_expr; }
  break;
case 382:
#line 3116 "cs-parser.jay"
  { yyVal = TypeManager.system_int32_expr; }
  break;
case 383:
#line 3117 "cs-parser.jay"
  { yyVal = TypeManager.system_uint32_expr; }
  break;
case 384:
#line 3118 "cs-parser.jay"
  { yyVal = TypeManager.system_int64_expr; }
  break;
case 385:
#line 3119 "cs-parser.jay"
  { yyVal = TypeManager.system_uint64_expr; }
  break;
case 386:
#line 3120 "cs-parser.jay"
  { yyVal = TypeManager.system_char_expr; }
  break;
case 387:
#line 3121 "cs-parser.jay"
  { yyVal = TypeManager.system_void_expr; }
  break;
case 388:
#line 3126 "cs-parser.jay"
  {
		string rank_specifiers = (string) yyVals[-1+yyTop];
		if ((bool) yyVals[0+yyTop])
			rank_specifiers += "?";

		yyVal = current_array_type = new ComposedCast ((FullNamedExpression) yyVals[-2+yyTop], rank_specifiers);
	  }
  break;
case 389:
#line 3140 "cs-parser.jay"
  {
		/* 7.5.1: Literals*/
	  }
  break;
case 390:
#line 3144 "cs-parser.jay"
  {
		MemberName mn = (MemberName) yyVals[0+yyTop];
		yyVal = mn.GetTypeExpression ();
	  }
  break;
case 391:
#line 3149 "cs-parser.jay"
  {
		LocatedToken lt1 = (LocatedToken) yyVals[-3+yyTop];
		LocatedToken lt2 = (LocatedToken) yyVals[-1+yyTop];
		if (RootContext.Version == LanguageVersion.ISO_1)
			Report.FeatureIsNotAvailable (lt1.Location, "namespace alias qualifier");

		yyVal = new QualifiedAliasMember (lt1.Value, lt2.Value, (TypeArguments) yyVals[0+yyTop], lt1.Location);
	  }
  break;
case 411:
#line 3179 "cs-parser.jay"
  { yyVal = new CharLiteral ((char) lexer.Value, lexer.Location); }
  break;
case 412:
#line 3180 "cs-parser.jay"
  { yyVal = new StringLiteral ((string) lexer.Value, lexer.Location); }
  break;
case 413:
#line 3181 "cs-parser.jay"
  { yyVal = new NullLiteral (lexer.Location); }
  break;
case 414:
#line 3185 "cs-parser.jay"
  { yyVal = new FloatLiteral ((float) lexer.Value, lexer.Location); }
  break;
case 415:
#line 3186 "cs-parser.jay"
  { yyVal = new DoubleLiteral ((double) lexer.Value, lexer.Location); }
  break;
case 416:
#line 3187 "cs-parser.jay"
  { yyVal = new DecimalLiteral ((decimal) lexer.Value, lexer.Location); }
  break;
case 417:
#line 3191 "cs-parser.jay"
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
case 418:
#line 3208 "cs-parser.jay"
  { yyVal = new BoolLiteral (true, lexer.Location); }
  break;
case 419:
#line 3209 "cs-parser.jay"
  { yyVal = new BoolLiteral (false, lexer.Location); }
  break;
case 420:
#line 3214 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
		lexer.Deambiguate_CloseParens (yyVal);
		/* After this, the next token returned is one of*/
		/* CLOSE_PARENS_CAST, CLOSE_PARENS_NO_CAST (CLOSE_PARENS), CLOSE_PARENS_OPEN_PARENS*/
		/* or CLOSE_PARENS_MINUS.*/
	  }
  break;
case 421:
#line 3221 "cs-parser.jay"
  { CheckToken (1026, yyToken, "Expecting ')'", lexer.Location); }
  break;
case 422:
#line 3226 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 423:
#line 3230 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 424:
#line 3234 "cs-parser.jay"
  {
		/* If a parenthesized expression is followed by a minus, we need to wrap*/
		/* the expression inside a ParenthesizedExpression for the CS0075 check*/
		/* in Binary.DoResolve().*/
		yyVal = new ParenthesizedExpression ((Expression) yyVals[-1+yyTop]);
	  }
  break;
case 425:
#line 3244 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberAccess ((Expression) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 426:
#line 3249 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		/* TODO: Location is wrong as some predefined types doesn't hold a location*/
		yyVal = new MemberAccess ((Expression) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 428:
#line 3262 "cs-parser.jay"
  {
		if (yyVals[-3+yyTop] == null)
			Report.Error (1, (Location) yyVals[-2+yyTop], "Parse error");
	        else
			yyVal = new Invocation ((Expression) yyVals[-3+yyTop], (ArrayList) yyVals[-1+yyTop]);
	  }
  break;
case 429:
#line 3269 "cs-parser.jay"
  {
		yyVal = new Invocation ((Expression) yyVals[-3+yyTop], new ArrayList ());
	  }
  break;
case 430:
#line 3273 "cs-parser.jay"
  {
		yyVal = new InvocationOrCast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 431:
#line 3277 "cs-parser.jay"
  {
		ArrayList args = new ArrayList (1);
		args.Add (yyVals[-1+yyTop]);
		yyVal = new Invocation ((Expression) yyVals[-4+yyTop], args);
	  }
  break;
case 432:
#line 3283 "cs-parser.jay"
  {
		ArrayList args = ((ArrayList) yyVals[-3+yyTop]);
		args.Add (yyVals[-1+yyTop]);
		yyVal = new Invocation ((Expression) yyVals[-6+yyTop], args);
	  }
  break;
case 433:
#line 3291 "cs-parser.jay"
  { yyVal = null; }
  break;
case 435:
#line 3297 "cs-parser.jay"
  {
	  	if (yyVals[-1+yyTop] == null)
	  	  yyVal = CollectionOrObjectInitializers.Empty;
	  	else
	  	  yyVal = new CollectionOrObjectInitializers ((ArrayList) yyVals[-1+yyTop], GetLocation (yyVals[-2+yyTop]));
	  }
  break;
case 436:
#line 3304 "cs-parser.jay"
  {
	  	yyVal = new CollectionOrObjectInitializers ((ArrayList) yyVals[-2+yyTop], GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 437:
#line 3310 "cs-parser.jay"
  { yyVal = null; }
  break;
case 438:
#line 3312 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	}
  break;
case 439:
#line 3319 "cs-parser.jay"
  {
	  	ArrayList a = new ArrayList ();
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 440:
#line 3325 "cs-parser.jay"
  {
	  	ArrayList a = (ArrayList)yyVals[-2+yyTop];
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 441:
#line 3334 "cs-parser.jay"
  {
	  	LocatedToken lt = yyVals[-2+yyTop] as LocatedToken;
	  	yyVal = new ElementInitializer (lt.Value, (Expression)yyVals[0+yyTop], lt.Location);
	  }
  break;
case 442:
#line 3339 "cs-parser.jay"
  {
		yyVal = new CollectionElementInitializer ((Expression)yyVals[0+yyTop]);
	  }
  break;
case 443:
#line 3343 "cs-parser.jay"
  {
	  	yyVal = new CollectionElementInitializer ((ArrayList)yyVals[-1+yyTop], GetLocation (yyVals[-2+yyTop]));
	  }
  break;
case 444:
#line 3347 "cs-parser.jay"
  {
	  	Report.Error (1920, GetLocation (yyVals[-1+yyTop]), "An element initializer cannot be empty");
	  }
  break;
case 447:
#line 3358 "cs-parser.jay"
  { yyVal = null; }
  break;
case 449:
#line 3364 "cs-parser.jay"
  { 
		ArrayList list = new ArrayList (4);
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 450:
#line 3370 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-2+yyTop];
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 451:
#line 3375 "cs-parser.jay"
  {
		CheckToken (1026, yyToken, "Expected `,' or `)'", GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 452:
#line 3383 "cs-parser.jay"
  {
		yyVal = new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Expression);
	  }
  break;
case 453:
#line 3387 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 454:
#line 3394 "cs-parser.jay"
  { 
		yyVal = new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Ref);
	  }
  break;
case 455:
#line 3398 "cs-parser.jay"
  { 
		yyVal = new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Out);
	  }
  break;
case 456:
#line 3402 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-1+yyTop];
		Argument[] args = new Argument [list.Count];
		list.CopyTo (args, 0);

		Expression expr = new Arglist (args, (Location) yyVals[-3+yyTop]);
		yyVal = new Argument (expr, Argument.AType.Expression);
	  }
  break;
case 457:
#line 3411 "cs-parser.jay"
  {
		yyVal = new Argument (new Arglist ((Location) yyVals[-2+yyTop]), Argument.AType.Expression);
	  }
  break;
case 458:
#line 3415 "cs-parser.jay"
  {
		yyVal = new Argument (new ArglistAccess ((Location) yyVals[0+yyTop]), Argument.AType.ArgList);
	  }
  break;
case 459:
#line 3421 "cs-parser.jay"
  { note ("section 5.4"); yyVal = yyVals[0+yyTop]; }
  break;
case 460:
#line 3426 "cs-parser.jay"
  {
		yyVal = new ElementAccess ((Expression) yyVals[-3+yyTop], (ArrayList) yyVals[-1+yyTop]);
	  }
  break;
case 461:
#line 3430 "cs-parser.jay"
  {
		/* So the super-trick is that primary_expression*/
		/* can only be either a SimpleName or a MemberAccess. */
		/* The MemberAccess case arises when you have a fully qualified type-name like :*/
		/* Foo.Bar.Blah i;*/
		/* SimpleName is when you have*/
		/* Blah i;*/
		  
		Expression expr = (Expression) yyVals[-1+yyTop];  
		if (expr is ComposedCast){
			yyVal = new ComposedCast ((ComposedCast)expr, (string) yyVals[0+yyTop]);
		} else if (expr is ATypeNameExpression){
			/**/
			/* So we extract the string corresponding to the SimpleName*/
			/* or MemberAccess*/
			/* */
			yyVal = new ComposedCast ((ATypeNameExpression)expr, (string) yyVals[0+yyTop]);
		} else {
			Error_ExpectingTypeName (expr);
			yyVal = TypeManager.system_object_expr;
		}
		
		current_array_type = (FullNamedExpression)yyVal;
	  }
  break;
case 462:
#line 3458 "cs-parser.jay"
  {
		ArrayList list = new ArrayList (4);
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 463:
#line 3464 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-2+yyTop];
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 464:
#line 3473 "cs-parser.jay"
  {
		yyVal = new This (current_block, (Location) yyVals[0+yyTop]);
	  }
  break;
case 465:
#line 3480 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new BaseAccess (lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 466:
#line 3485 "cs-parser.jay"
  {
		yyVal = new BaseIndexerAccess ((ArrayList) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 467:
#line 3488 "cs-parser.jay"
  {
		Report.Error (175, (Location) yyVals[-1+yyTop], "Use of keyword `base' is not valid in this context");
		yyVal = null;
	  }
  break;
case 468:
#line 3496 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PostIncrement,
				       (Expression) yyVals[-1+yyTop], (Location) yyVals[0+yyTop]);
	  }
  break;
case 469:
#line 3504 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PostDecrement,
				       (Expression) yyVals[-1+yyTop], (Location) yyVals[0+yyTop]);
	  }
  break;
case 473:
#line 3518 "cs-parser.jay"
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
case 474:
#line 3529 "cs-parser.jay"
  {
		if (RootContext.Version <= LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-2+yyTop]), "collection initializers");
	  
		yyVal = new NewInitialize ((Expression) yyVals[-1+yyTop], null, (CollectionOrObjectInitializers) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 475:
#line 3541 "cs-parser.jay"
  {
		yyVal = new ArrayCreation ((FullNamedExpression) yyVals[-5+yyTop], (ArrayList) yyVals[-3+yyTop], (string) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-6+yyTop]);
	  }
  break;
case 476:
#line 3545 "cs-parser.jay"
  {
		yyVal = new ArrayCreation ((FullNamedExpression) yyVals[-2+yyTop], (string) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 477:
#line 3549 "cs-parser.jay"
  {
		yyVal = new ImplicitlyTypedArrayCreation ((string) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 478:
#line 3553 "cs-parser.jay"
  {
		Report.Error (1031, (Location) yyVals[-1+yyTop], "Type expected");
                yyVal = null;
	  }
  break;
case 479:
#line 3558 "cs-parser.jay"
  {
		Report.Error (1526, (Location) yyVals[-2+yyTop], "A new expression requires () or [] after type");
		yyVal = null;
	  }
  break;
case 480:
#line 3566 "cs-parser.jay"
  {
	  	if (RootContext.Version <= LanguageVersion.ISO_2)
	  		Report.FeatureIsNotAvailable (GetLocation (yyVals[-3+yyTop]), "anonymous types");

		yyVal = new AnonymousTypeDeclaration ((ArrayList) yyVals[-1+yyTop], current_container, GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 481:
#line 3575 "cs-parser.jay"
  { yyVal = null; }
  break;
case 482:
#line 3577 "cs-parser.jay"
  {
	  	ArrayList a = new ArrayList (4);
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 483:
#line 3583 "cs-parser.jay"
  {
	  	ArrayList a = (ArrayList) yyVals[-2+yyTop];
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 484:
#line 3592 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken)yyVals[-2+yyTop];
	  	yyVal = new AnonymousTypeParameter ((Expression)yyVals[0+yyTop], lt.Value, lt.Location);
	  }
  break;
case 485:
#line 3597 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken)yyVals[0+yyTop];
	  	yyVal = new AnonymousTypeParameter (new SimpleName (lt.Value, lt.Location),
	  		lt.Value, lt.Location);
	  }
  break;
case 486:
#line 3603 "cs-parser.jay"
  {
	  	MemberAccess ma = (MemberAccess) yyVals[0+yyTop];
	  	yyVal = new AnonymousTypeParameter (ma, ma.Name, ma.Location);
	  }
  break;
case 487:
#line 3608 "cs-parser.jay"
  {
		Report.Error (746, lexer.Location, "Invalid anonymous type member declarator. " +
		"Anonymous type members must be a member assignment, simple name or member access expression");
	  }
  break;
case 488:
#line 3616 "cs-parser.jay"
  {
		  yyVal = "";
	  }
  break;
case 489:
#line 3620 "cs-parser.jay"
  {
			yyVal = yyVals[0+yyTop];
	  }
  break;
case 490:
#line 3627 "cs-parser.jay"
  {
		yyVal = "";
	  }
  break;
case 491:
#line 3631 "cs-parser.jay"
  {
		yyVal = "?";
	  }
  break;
case 492:
#line 3635 "cs-parser.jay"
  {
		if ((bool) yyVals[-1+yyTop])
			yyVal = "?" + yyVals[0+yyTop];
		else
			yyVal = yyVals[0+yyTop];
	  }
  break;
case 493:
#line 3642 "cs-parser.jay"
  {
		if ((bool) yyVals[-2+yyTop])
			yyVal = "?" + yyVals[-1+yyTop] + "?";
		else
			yyVal = yyVals[-1+yyTop] + "?";
	  }
  break;
case 494:
#line 3652 "cs-parser.jay"
  {
		  yyVal = (string) yyVals[0+yyTop] + (string) yyVals[-1+yyTop];
	  }
  break;
case 495:
#line 3659 "cs-parser.jay"
  {
		yyVal = "[" + (string) yyVals[-1+yyTop] + "]";
	  }
  break;
case 496:
#line 3666 "cs-parser.jay"
  {
		yyVal = "";
	  }
  break;
case 497:
#line 3670 "cs-parser.jay"
  {
		  yyVal = yyVals[0+yyTop];
	  }
  break;
case 498:
#line 3677 "cs-parser.jay"
  {
		yyVal = ",";
	  }
  break;
case 499:
#line 3681 "cs-parser.jay"
  {
		yyVal = (string) yyVals[-1+yyTop] + ",";
	  }
  break;
case 500:
#line 3688 "cs-parser.jay"
  {
		yyVal = null;
	  }
  break;
case 501:
#line 3692 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 502:
#line 3699 "cs-parser.jay"
  {
		ArrayList list = new ArrayList (4);
		yyVal = list;
	  }
  break;
case 503:
#line 3704 "cs-parser.jay"
  {
		yyVal = (ArrayList) yyVals[-2+yyTop];
	  }
  break;
case 504:
#line 3711 "cs-parser.jay"
  {
		ArrayList list = new ArrayList (4);
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 505:
#line 3717 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-2+yyTop];
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 506:
#line 3726 "cs-parser.jay"
  {
	  	pushed_current_array_type = current_array_type;
	  	lexer.TypeOfParsing = true;
	  }
  break;
case 507:
#line 3731 "cs-parser.jay"
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
case 508:
#line 3744 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 509:
#line 3748 "cs-parser.jay"
  {
		yyVal = new UnboundTypeExpression ((MemberName)yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 510:
#line 3755 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (lexer.Location, "generics");
	  
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);

		yyVal = new MemberName (lt.Value, ta, lt.Location);
	  }
  break;
case 511:
#line 3765 "cs-parser.jay"
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
case 512:
#line 3777 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);
		
	  	yyVal = new MemberName ((MemberName)yyVals[-3+yyTop], lt.Value, ta, lt.Location);
	  }
  break;
case 513:
#line 3784 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);
		
	  	yyVal = new MemberName ((MemberName)yyVals[-3+yyTop], lt.Value, ta, lt.Location);
	  }
  break;
case 514:
#line 3794 "cs-parser.jay"
  { 
		yyVal = new SizeOf ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 515:
#line 3801 "cs-parser.jay"
  {
		yyVal = new CheckedExpr ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 516:
#line 3808 "cs-parser.jay"
  {
		yyVal = new UnCheckedExpr ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 517:
#line 3815 "cs-parser.jay"
  {
		Expression deref;
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];

		deref = new Indirection ((Expression) yyVals[-2+yyTop], lt.Location);
		yyVal = new MemberAccess (deref, lt.Value);
	  }
  break;
case 518:
#line 3826 "cs-parser.jay"
  {
		start_anonymous (false, (Parameters) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 519:
#line 3830 "cs-parser.jay"
  {
		yyVal = end_anonymous ((ToplevelBlock) yyVals[0+yyTop], (Location) yyVals[-3+yyTop]);
	}
  break;
case 520:
#line 3836 "cs-parser.jay"
  { yyVal = null; }
  break;
case 522:
#line 3842 "cs-parser.jay"
  {
	  	params_modifiers_not_allowed = true; 
	  }
  break;
case 523:
#line 3846 "cs-parser.jay"
  {
	  	params_modifiers_not_allowed = false;
	  	yyVal = yyVals[-1+yyTop];
	  }
  break;
case 524:
#line 3854 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (lexer.Location, "default value expression");

		yyVal = new DefaultValueExpression ((Expression) yyVals[-1+yyTop], lexer.Location);
	  }
  break;
case 526:
#line 3865 "cs-parser.jay"
  {
		yyVal = new Unary (Unary.Operator.LogicalNot, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 527:
#line 3869 "cs-parser.jay"
  {
		yyVal = new Unary (Unary.Operator.OnesComplement, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 529:
#line 3877 "cs-parser.jay"
  {
		yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 530:
#line 3881 "cs-parser.jay"
  {
	  	yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 531:
#line 3885 "cs-parser.jay"
  {
		yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 533:
#line 3893 "cs-parser.jay"
  {
		/* TODO: wrong location*/
		yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 535:
#line 3906 "cs-parser.jay"
  { 
	  	yyVal = new Unary (Unary.Operator.UnaryPlus, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 536:
#line 3910 "cs-parser.jay"
  { 
		yyVal = new Unary (Unary.Operator.UnaryNegation, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 537:
#line 3914 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PreIncrement,
				       (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 538:
#line 3919 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PreDecrement,
				       (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 539:
#line 3924 "cs-parser.jay"
  {
		yyVal = new Indirection ((Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 540:
#line 3928 "cs-parser.jay"
  {
		yyVal = new Unary (Unary.Operator.AddressOf, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 542:
#line 3936 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Multiply, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 543:
#line 3941 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Division, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 544:
#line 3946 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Modulus, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 546:
#line 3955 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Addition, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 547:
#line 3960 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Subtraction, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 549:
#line 3969 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LeftShift, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 550:
#line 3974 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.RightShift, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 551:
#line 3982 "cs-parser.jay"
  {
		yyVal = false;
	  }
  break;
case 552:
#line 3986 "cs-parser.jay"
  {
		lexer.PutbackNullable ();
		yyVal = true;
	  }
  break;
case 553:
#line 3994 "cs-parser.jay"
  {
		if (((bool) yyVals[0+yyTop]) && (yyVals[-1+yyTop] is ComposedCast))
			yyVal = ((ComposedCast) yyVals[-1+yyTop]).RemoveNullable ();
		else
			yyVal = yyVals[-1+yyTop];
	  }
  break;
case 555:
#line 4005 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LessThan, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 556:
#line 4010 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.GreaterThan, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 557:
#line 4015 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LessThanOrEqual, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 558:
#line 4020 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.GreaterThanOrEqual, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 559:
#line 4025 "cs-parser.jay"
  {
		yyErrorFlag = 3;
	  }
  break;
case 560:
#line 4028 "cs-parser.jay"
  {
		yyVal = new Is ((Expression) yyVals[-3+yyTop], (Expression) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 561:
#line 4032 "cs-parser.jay"
  {
		yyErrorFlag = 3;
	  }
  break;
case 562:
#line 4035 "cs-parser.jay"
  {
		yyVal = new As ((Expression) yyVals[-3+yyTop], (Expression) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 564:
#line 4043 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Equality, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 565:
#line 4048 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Inequality, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 567:
#line 4057 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.BitwiseAnd, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 569:
#line 4066 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.ExclusiveOr, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 571:
#line 4075 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.BitwiseOr, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 573:
#line 4084 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LogicalAnd, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 575:
#line 4093 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LogicalOr, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 577:
#line 4102 "cs-parser.jay"
  {
		yyVal = new Conditional ((Expression) yyVals[-4+yyTop], (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 578:
#line 4106 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-1+yyTop]), "null coalescing operator");
			
		yyVal = new Nullable.NullCoalescingOperator ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 579:
#line 4114 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((FullNamedExpression) yyVals[-2+yyTop], "?", lexer.Location);
		lexer.PutbackCloseParens ();
	  }
  break;
case 580:
#line 4122 "cs-parser.jay"
  {
		yyVal = new Assign ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 581:
#line 4126 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Multiply, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 582:
#line 4131 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Division, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 583:
#line 4136 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Modulus, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 584:
#line 4141 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Addition, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 585:
#line 4146 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Subtraction, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 586:
#line 4151 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.LeftShift, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 587:
#line 4156 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.RightShift, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 588:
#line 4161 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.BitwiseAnd, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 589:
#line 4166 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.BitwiseOr, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 590:
#line 4171 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.ExclusiveOr, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 591:
#line 4179 "cs-parser.jay"
  {
		ArrayList pars = new ArrayList (4);
		pars.Add (yyVals[0+yyTop]);

		yyVal = pars;
	  }
  break;
case 592:
#line 4186 "cs-parser.jay"
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
case 593:
#line 4200 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];

		yyVal = new Parameter ((FullNamedExpression) yyVals[-1+yyTop], lt.Value, (Parameter.Modifier) yyVals[-2+yyTop], null, lt.Location);
	  }
  break;
case 594:
#line 4206 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];

		yyVal = new Parameter ((FullNamedExpression) yyVals[-1+yyTop], lt.Value, Parameter.Modifier.NONE, null, lt.Location);
	  }
  break;
case 595:
#line 4212 "cs-parser.jay"
  {
	  	LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		yyVal = new ImplicitLambdaParameter (lt.Value, lt.Location);
	  }
  break;
case 596:
#line 4219 "cs-parser.jay"
  { yyVal = Parameters.EmptyReadOnlyParameters; }
  break;
case 597:
#line 4220 "cs-parser.jay"
  { 
		ArrayList pars_list = (ArrayList) yyVals[0+yyTop];
		yyVal = new Parameters ((Parameter[])pars_list.ToArray (typeof (Parameter)));
	  }
  break;
case 598:
#line 4227 "cs-parser.jay"
  {
		start_block (lexer.Location);
	  }
  break;
case 599:
#line 4231 "cs-parser.jay"
  {
		Block b = end_block (lexer.Location);
		b.AddStatement (new ContextualReturn ((Expression) yyVals[0+yyTop]));
		yyVal = b;
	  }
  break;
case 600:
#line 4236 "cs-parser.jay"
  { 
	  	yyVal = yyVals[0+yyTop]; 
	  }
  break;
case 601:
#line 4243 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		Parameter p = new ImplicitLambdaParameter (lt.Value, lt.Location);
		start_anonymous (true, new Parameters (p), (Location) yyVals[0+yyTop]);
	  }
  break;
case 602:
#line 4249 "cs-parser.jay"
  {
		yyVal = end_anonymous ((ToplevelBlock) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 603:
#line 4253 "cs-parser.jay"
  {
		start_anonymous (true, (Parameters) yyVals[-2+yyTop], (Location) yyVals[0+yyTop]);
	  }
  break;
case 604:
#line 4257 "cs-parser.jay"
  {
		yyVal = end_anonymous ((ToplevelBlock) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 612:
#line 4289 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 613:
#line 4293 "cs-parser.jay"
  {
		MemberName name = MakeName ((MemberName) yyVals[0+yyTop]);
		push_current_class (new Class (current_namespace, current_class, name, (int) yyVals[-4+yyTop], (Attributes) yyVals[-5+yyTop]), yyVals[-3+yyTop]);
	  }
  break;
case 614:
#line 4299 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		current_class.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);

		if (RootContext.Documentation != null) {
			current_container.DocComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 615:
#line 4310 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 616:
#line 4315 "cs-parser.jay"
  {
		yyVal = pop_current_class ();
	  }
  break;
case 617:
#line 4322 "cs-parser.jay"
  { yyVal = null; }
  break;
case 618:
#line 4324 "cs-parser.jay"
  { yyVal = yyVals[0+yyTop]; }
  break;
case 619:
#line 4328 "cs-parser.jay"
  { yyVal = (int) 0; }
  break;
case 622:
#line 4335 "cs-parser.jay"
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
case 623:
#line 4348 "cs-parser.jay"
  { yyVal = Modifiers.NEW; }
  break;
case 624:
#line 4349 "cs-parser.jay"
  { yyVal = Modifiers.PUBLIC; }
  break;
case 625:
#line 4350 "cs-parser.jay"
  { yyVal = Modifiers.PROTECTED; }
  break;
case 626:
#line 4351 "cs-parser.jay"
  { yyVal = Modifiers.INTERNAL; }
  break;
case 627:
#line 4352 "cs-parser.jay"
  { yyVal = Modifiers.PRIVATE; }
  break;
case 628:
#line 4353 "cs-parser.jay"
  { yyVal = Modifiers.ABSTRACT; }
  break;
case 629:
#line 4354 "cs-parser.jay"
  { yyVal = Modifiers.SEALED; }
  break;
case 630:
#line 4355 "cs-parser.jay"
  { yyVal = Modifiers.STATIC; }
  break;
case 631:
#line 4356 "cs-parser.jay"
  { yyVal = Modifiers.READONLY; }
  break;
case 632:
#line 4357 "cs-parser.jay"
  { yyVal = Modifiers.VIRTUAL; }
  break;
case 633:
#line 4358 "cs-parser.jay"
  { yyVal = Modifiers.OVERRIDE; }
  break;
case 634:
#line 4359 "cs-parser.jay"
  { yyVal = Modifiers.EXTERN; }
  break;
case 635:
#line 4360 "cs-parser.jay"
  { yyVal = Modifiers.VOLATILE; }
  break;
case 636:
#line 4361 "cs-parser.jay"
  { yyVal = Modifiers.UNSAFE; }
  break;
case 639:
#line 4370 "cs-parser.jay"
  { current_container.AddBasesForPart (current_class, (ArrayList) yyVals[0+yyTop]); }
  break;
case 640:
#line 4374 "cs-parser.jay"
  { yyVal = null; }
  break;
case 641:
#line 4376 "cs-parser.jay"
  { yyVal = yyVals[0+yyTop]; }
  break;
case 642:
#line 4380 "cs-parser.jay"
  {
		ArrayList constraints = new ArrayList (1);
		constraints.Add (yyVals[0+yyTop]);
		yyVal = constraints;
	  }
  break;
case 643:
#line 4385 "cs-parser.jay"
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
case 644:
#line 4402 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		yyVal = new Constraints (lt.Value, (ArrayList) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 645:
#line 4409 "cs-parser.jay"
  {
		ArrayList constraints = new ArrayList (1);
		constraints.Add (yyVals[0+yyTop]);
		yyVal = constraints;
	  }
  break;
case 646:
#line 4414 "cs-parser.jay"
  {
		ArrayList constraints = (ArrayList) yyVals[-2+yyTop];

		constraints.Add (yyVals[0+yyTop]);
		yyVal = constraints;
	  }
  break;
case 648:
#line 4424 "cs-parser.jay"
  {
		yyVal = SpecialConstraint.Constructor;
	  }
  break;
case 649:
#line 4427 "cs-parser.jay"
  {
		yyVal = SpecialConstraint.ReferenceType;
	  }
  break;
case 650:
#line 4430 "cs-parser.jay"
  {
		yyVal = SpecialConstraint.ValueType;
	  }
  break;
case 651:
#line 4450 "cs-parser.jay"
  {
		++lexer.parsing_block;
		start_block ((Location) yyVals[0+yyTop]);
	  }
  break;
case 652:
#line 4455 "cs-parser.jay"
  {
	 	--lexer.parsing_block;
		yyVal = end_block ((Location) yyVals[0+yyTop]);
	  }
  break;
case 653:
#line 4463 "cs-parser.jay"
  {
		++lexer.parsing_block;
	  }
  break;
case 654:
#line 4467 "cs-parser.jay"
  {
		--lexer.parsing_block;
		yyVal = end_block ((Location) yyVals[0+yyTop]);
	  }
  break;
case 659:
#line 4485 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null && (Block) yyVals[0+yyTop] != current_block){
			current_block.AddStatement ((Statement) yyVals[0+yyTop]);
			current_block = (Block) yyVals[0+yyTop];
		}
	  }
  break;
case 660:
#line 4492 "cs-parser.jay"
  {
		current_block.AddStatement ((Statement) yyVals[0+yyTop]);
	  }
  break;
case 676:
#line 4517 "cs-parser.jay"
  {
		  Report.Error (1023, GetLocation (yyVals[0+yyTop]), "An embedded statement may not be a declaration or labeled statement");
		  yyVal = null;
	  }
  break;
case 677:
#line 4522 "cs-parser.jay"
  {
		  Report.Error (1023, GetLocation (yyVals[0+yyTop]), "An embedded statement may not be a declaration or labeled statement");
		  yyVal = null;
	  }
  break;
case 678:
#line 4530 "cs-parser.jay"
  {
		  yyVal = EmptyStatement.Value;
	  }
  break;
case 679:
#line 4537 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		LabeledStatement labeled = new LabeledStatement (lt.Value, lt.Location);

		if (current_block.AddLabel (labeled))
			current_block.AddStatement (labeled);
	  }
  break;
case 681:
#line 4549 "cs-parser.jay"
  {
		current_array_type = null;
		if (yyVals[-1+yyTop] != null){
			DictionaryEntry de = (DictionaryEntry) yyVals[-1+yyTop];
			Expression e = (Expression) de.Key;

			yyVal = declare_local_variables (e, (ArrayList) de.Value, e.Location);
		}
	  }
  break;
case 682:
#line 4560 "cs-parser.jay"
  {
		current_array_type = null;
		if (yyVals[-1+yyTop] != null){
			DictionaryEntry de = (DictionaryEntry) yyVals[-1+yyTop];

			yyVal = declare_local_constants ((Expression) de.Key, (ArrayList) de.Value);
		}
	  }
  break;
case 683:
#line 4578 "cs-parser.jay"
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
			yyVal = new ComposedCast ((ComposedCast)expr, (string) yyVals[0+yyTop]);
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
				yyVal = new ComposedCast ((ATypeNameExpression)expr, (string) yyVals[0+yyTop]);
			}
		} else {
			Error_ExpectingTypeName (expr);
			yyVal = TypeManager.system_object_expr;
		}
	  }
  break;
case 684:
#line 4619 "cs-parser.jay"
  {
		if ((string) yyVals[0+yyTop] == "")
			yyVal = yyVals[-1+yyTop];
		else
			yyVal = current_array_type = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], (string) yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 685:
#line 4629 "cs-parser.jay"
  {
		ATypeNameExpression expr = yyVals[-1+yyTop] as ATypeNameExpression;

		if (expr != null) {
			yyVal = new ComposedCast (expr, "*");
		} else {
			Error_ExpectingTypeName ((Expression)yyVals[-1+yyTop]);
			yyVal = expr;
		}
	  }
  break;
case 686:
#line 4640 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], "*", lexer.Location);
	  }
  break;
case 687:
#line 4644 "cs-parser.jay"
  {
		yyVal = new ComposedCast (TypeManager.system_void_expr, "*", (Location) yyVals[-1+yyTop]);
	  }
  break;
case 688:
#line 4648 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], "*");
	  }
  break;
case 689:
#line 4655 "cs-parser.jay"
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
case 690:
#line 4666 "cs-parser.jay"
  {
		if (yyVals[-2+yyTop] != null){
			Expression t;

			if ((string) yyVals[-1+yyTop] == "")
				t = (Expression) yyVals[-2+yyTop];
			else
				t = new ComposedCast ((FullNamedExpression) yyVals[-2+yyTop], (string) yyVals[-1+yyTop]);
			yyVal = new DictionaryEntry (t, yyVals[0+yyTop]);
		} else 
			yyVal = null;
	  }
  break;
case 691:
#line 4682 "cs-parser.jay"
  {
		if (yyVals[-1+yyTop] != null)
			yyVal = new DictionaryEntry (yyVals[-1+yyTop], yyVals[0+yyTop]);
		else
			yyVal = null;
	  }
  break;
case 692:
#line 4691 "cs-parser.jay"
  { yyVal = yyVals[-1+yyTop]; }
  break;
case 693:
#line 4700 "cs-parser.jay"
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
case 694:
#line 4710 "cs-parser.jay"
  {
		Report.Error (1002, GetLocation (yyVals[0+yyTop]), "Expecting `;'");
		yyVal = null;
	  }
  break;
case 697:
#line 4724 "cs-parser.jay"
  { 
		Location l = (Location) yyVals[-4+yyTop];

		yyVal = new If ((Expression) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);

		/* FIXME: location for warning should be loc property of $5.*/
		if (yyVals[0+yyTop] == EmptyStatement.Value)
			Report.Warning (642, 3, l, "Possible mistaken empty statement");

	  }
  break;
case 698:
#line 4736 "cs-parser.jay"
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
case 699:
#line 4751 "cs-parser.jay"
  { 
		if (switch_stack == null)
			switch_stack = new Stack (2);
		switch_stack.Push (current_block);
	  }
  break;
case 700:
#line 4758 "cs-parser.jay"
  {
		yyVal = new Switch ((Expression) yyVals[-2+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]);
		current_block = (Block) switch_stack.Pop ();
	  }
  break;
case 701:
#line 4768 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 702:
#line 4775 "cs-parser.jay"
  {
	  	Report.Warning (1522, 1, lexer.Location, "Empty switch block"); 
		yyVal = new ArrayList ();
	  }
  break;
case 704:
#line 4784 "cs-parser.jay"
  {
		ArrayList sections = new ArrayList (4);

		sections.Add (yyVals[0+yyTop]);
		yyVal = sections;
	  }
  break;
case 705:
#line 4791 "cs-parser.jay"
  {
		ArrayList sections = (ArrayList) yyVals[-1+yyTop];

		sections.Add (yyVals[0+yyTop]);
		yyVal = sections;
	  }
  break;
case 706:
#line 4801 "cs-parser.jay"
  {
		current_block = current_block.CreateSwitchBlock (lexer.Location);
	  }
  break;
case 707:
#line 4805 "cs-parser.jay"
  {
		yyVal = new SwitchSection ((ArrayList) yyVals[-2+yyTop], current_block.Explicit);
	  }
  break;
case 708:
#line 4812 "cs-parser.jay"
  {
		ArrayList labels = new ArrayList (4);

		labels.Add (yyVals[0+yyTop]);
		yyVal = labels;
	  }
  break;
case 709:
#line 4819 "cs-parser.jay"
  {
		ArrayList labels = (ArrayList) (yyVals[-1+yyTop]);
		labels.Add (yyVals[0+yyTop]);

		yyVal = labels;
	  }
  break;
case 710:
#line 4828 "cs-parser.jay"
  { yyVal = new SwitchLabel ((Expression) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]); }
  break;
case 711:
#line 4829 "cs-parser.jay"
  { yyVal = new SwitchLabel (null, (Location) yyVals[0+yyTop]); }
  break;
case 712:
#line 4830 "cs-parser.jay"
  {
		Report.Error (
			1523, GetLocation (yyVals[0+yyTop]), 
			"The keyword case or default must precede code in switch block");
	  }
  break;
case 717:
#line 4846 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-4+yyTop];
		yyVal = new While ((Expression) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);
	  }
  break;
case 718:
#line 4855 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-6+yyTop];

		yyVal = new Do ((Statement) yyVals[-5+yyTop], (Expression) yyVals[-2+yyTop], l);
	  }
  break;
case 719:
#line 4865 "cs-parser.jay"
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
case 720:
#line 4906 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-9+yyTop];

		For f = new For ((Statement) yyVals[-5+yyTop], (Expression) yyVals[-4+yyTop], (Statement) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);

		current_block.AddStatement (f);

		yyVal = end_block (lexer.Location);
	  }
  break;
case 721:
#line 4918 "cs-parser.jay"
  { yyVal = EmptyStatement.Value; }
  break;
case 725:
#line 4928 "cs-parser.jay"
  { yyVal = null; }
  break;
case 727:
#line 4933 "cs-parser.jay"
  { yyVal = EmptyStatement.Value; }
  break;
case 730:
#line 4943 "cs-parser.jay"
  {
		/* CHANGE: was `null'*/
		Statement s = (Statement) yyVals[0+yyTop];
		Block b = new Block (current_block, s.loc, lexer.Location);   

		b.AddStatement (s);
		yyVal = b;
	  }
  break;
case 731:
#line 4952 "cs-parser.jay"
  {
		Block b = (Block) yyVals[-2+yyTop];

		b.AddStatement ((Statement) yyVals[0+yyTop]);
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 732:
#line 4962 "cs-parser.jay"
  {
		Report.Error (230, (Location) yyVals[-5+yyTop], "Type and identifier are both required in a foreach statement");
		yyVal = null;
	  }
  break;
case 733:
#line 4968 "cs-parser.jay"
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
case 734:
#line 4988 "cs-parser.jay"
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
case 741:
#line 5012 "cs-parser.jay"
  {
		yyVal = new Break ((Location) yyVals[-1+yyTop]);
	  }
  break;
case 742:
#line 5019 "cs-parser.jay"
  {
		yyVal = new Continue ((Location) yyVals[-1+yyTop]);
	  }
  break;
case 743:
#line 5026 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new Goto (lt.Value, lt.Location);
	  }
  break;
case 744:
#line 5031 "cs-parser.jay"
  {
		yyVal = new GotoCase ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 745:
#line 5035 "cs-parser.jay"
  {
		yyVal = new GotoDefault ((Location) yyVals[-2+yyTop]);
	  }
  break;
case 746:
#line 5042 "cs-parser.jay"
  {
		yyVal = new Return ((Expression) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 747:
#line 5049 "cs-parser.jay"
  {
		yyVal = new Throw ((Expression) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 748:
#line 5056 "cs-parser.jay"
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
case 749:
#line 5076 "cs-parser.jay"
  {
		Report.Error (1627, (Location) yyVals[-1+yyTop], "Expression expected after yield return");
		yyVal = null;
	  }
  break;
case 750:
#line 5081 "cs-parser.jay"
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
case 753:
#line 5109 "cs-parser.jay"
  {
		yyVal = new TryCatch ((Block) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-2+yyTop], false);
	  }
  break;
case 754:
#line 5113 "cs-parser.jay"
  {
		yyVal = new TryFinally ((Statement) yyVals[-2+yyTop], (Block) yyVals[0+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 755:
#line 5117 "cs-parser.jay"
  {
		yyVal = new TryFinally (new TryCatch ((Block) yyVals[-3+yyTop], (ArrayList) yyVals[-2+yyTop], (Location) yyVals[-4+yyTop], true), (Block) yyVals[0+yyTop], (Location) yyVals[-4+yyTop]);
	  }
  break;
case 756:
#line 5121 "cs-parser.jay"
  {
		Report.Error (1524, (Location) yyVals[-2+yyTop], "Expected catch or finally");
		yyVal = null;
	  }
  break;
case 757:
#line 5129 "cs-parser.jay"
  {
		ArrayList l = new ArrayList (4);

		l.Add (yyVals[0+yyTop]);
		yyVal = l;
	  }
  break;
case 758:
#line 5136 "cs-parser.jay"
  {
		ArrayList l = (ArrayList) yyVals[-1+yyTop];

		l.Add (yyVals[0+yyTop]);
		yyVal = l;
	  }
  break;
case 759:
#line 5145 "cs-parser.jay"
  { yyVal = null; }
  break;
case 761:
#line 5151 "cs-parser.jay"
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
case 762:
#line 5168 "cs-parser.jay"
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
case 763:
#line 5189 "cs-parser.jay"
  { yyVal = null; }
  break;
case 765:
#line 5195 "cs-parser.jay"
  {
		yyVal = new DictionaryEntry (yyVals[-2+yyTop], yyVals[-1+yyTop]);
	  }
  break;
case 766:
#line 5203 "cs-parser.jay"
  {
		yyVal = new Checked ((Block) yyVals[0+yyTop]);
	  }
  break;
case 767:
#line 5210 "cs-parser.jay"
  {
		yyVal = new Unchecked ((Block) yyVals[0+yyTop]);
	  }
  break;
case 768:
#line 5217 "cs-parser.jay"
  {
		RootContext.CheckUnsafeOption ((Location) yyVals[0+yyTop]);
	  }
  break;
case 769:
#line 5219 "cs-parser.jay"
  {
		yyVal = new Unsafe ((Block) yyVals[0+yyTop]);
	  }
  break;
case 770:
#line 5228 "cs-parser.jay"
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
case 771:
#line 5251 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-6+yyTop];

		Fixed f = new Fixed ((Expression) yyVals[-4+yyTop], (ArrayList) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], l);

		current_block.AddStatement (f);

		yyVal = end_block (lexer.Location);
	  }
  break;
case 772:
#line 5263 "cs-parser.jay"
  { 
	   	ArrayList declarators = new ArrayList (4);
	   	if (yyVals[0+yyTop] != null)
			declarators.Add (yyVals[0+yyTop]);
		yyVal = declarators;
	  }
  break;
case 773:
#line 5270 "cs-parser.jay"
  {
		ArrayList declarators = (ArrayList) yyVals[-2+yyTop];
		if (yyVals[0+yyTop] != null)
			declarators.Add (yyVals[0+yyTop]);
		yyVal = declarators;
	  }
  break;
case 774:
#line 5280 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		/* FIXME: keep location*/
		yyVal = new Pair (lt.Value, yyVals[0+yyTop]);
	  }
  break;
case 775:
#line 5286 "cs-parser.jay"
  {
		Report.Error (210, ((LocatedToken) yyVals[0+yyTop]).Location, "You must provide an initializer in a fixed or using statement declaration");
		yyVal = null;
	  }
  break;
case 776:
#line 5294 "cs-parser.jay"
  {
		/**/
 	  }
  break;
case 777:
#line 5298 "cs-parser.jay"
  {
		yyVal = new Lock ((Expression) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]);
	  }
  break;
case 778:
#line 5305 "cs-parser.jay"
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
case 779:
#line 5347 "cs-parser.jay"
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
case 780:
#line 5360 "cs-parser.jay"
  {
		start_block (lexer.Location);
	  }
  break;
case 781:
#line 5364 "cs-parser.jay"
  {
		current_block.AddStatement (new UsingTemporary ((Expression) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]));
		yyVal = end_block (lexer.Location);
	  }
  break;
case 782:
#line 5375 "cs-parser.jay"
  {
		++lexer.query_parsing;
	  }
  break;
case 783:
#line 5379 "cs-parser.jay"
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
case 784:
#line 5395 "cs-parser.jay"
  {
		current_block = new Linq.QueryBlock (current_block, GetLocation (yyVals[-3+yyTop]));
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance, lt.Value, lt.Location);
		yyVal = new Linq.QueryExpression (lt, new Linq.QueryStartClause ((Expression)yyVals[0+yyTop]));
	  }
  break;
case 785:
#line 5403 "cs-parser.jay"
  {
		current_block = new Linq.QueryBlock (current_block, GetLocation (yyVals[-4+yyTop]));
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];

		Expression type = (Expression)yyVals[-3+yyTop];
		current_block.AddVariable (type, lt.Value, lt.Location);
		yyVal = new Linq.QueryExpression (lt, new Linq.Cast (type, (Expression)yyVals[0+yyTop]));
	  }
  break;
case 786:
#line 5415 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance,
			lt.Value, lt.Location);
			
		yyVal = new Linq.SelectMany (lt, (Expression)yyVals[0+yyTop]);			
	  }
  break;
case 787:
#line 5424 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];

		Expression type = (Expression)yyVals[-3+yyTop];
		current_block.AddVariable (type, lt.Value, lt.Location);
		yyVal = new Linq.SelectMany (lt, new Linq.Cast (type, (Expression)yyVals[0+yyTop]));
	  }
  break;
case 788:
#line 5435 "cs-parser.jay"
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
case 789:
#line 5453 "cs-parser.jay"
  {
		yyVal = new Linq.Select ((Expression)yyVals[0+yyTop], GetLocation (yyVals[-1+yyTop]));
	  }
  break;
case 790:
#line 5457 "cs-parser.jay"
  {
	    yyVal = new Linq.GroupBy ((Expression)yyVals[-2+yyTop], (Expression)yyVals[0+yyTop], GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 794:
#line 5470 "cs-parser.jay"
  {
		((Linq.AQueryClause)yyVals[-1+yyTop]).Tail.Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 800:
#line 5486 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance,
			lt.Value, lt.Location);	  
	  	yyVal = new Linq.Let (lt, (Expression)yyVals[0+yyTop], GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 801:
#line 5496 "cs-parser.jay"
  {
		yyVal = new Linq.Where ((Expression)yyVals[0+yyTop], GetLocation (yyVals[-1+yyTop]));
	  }
  break;
case 802:
#line 5503 "cs-parser.jay"
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
case 803:
#line 5519 "cs-parser.jay"
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
case 805:
#line 5539 "cs-parser.jay"
  {
	  	yyVal = yyVals[0+yyTop];
	  }
  break;
case 806:
#line 5546 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 808:
#line 5554 "cs-parser.jay"
  {
		((Linq.AQueryClause)yyVals[-2+yyTop]).Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 809:
#line 5562 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 810:
#line 5566 "cs-parser.jay"
  {
		((Linq.AQueryClause)yyVals[-2+yyTop]).Tail.Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 811:
#line 5574 "cs-parser.jay"
  {
		yyVal = new Linq.OrderByAscending ((Expression)yyVals[0+yyTop]);	
	  }
  break;
case 812:
#line 5578 "cs-parser.jay"
  {
		yyVal = new Linq.OrderByAscending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 813:
#line 5582 "cs-parser.jay"
  {
		yyVal = new Linq.OrderByDescending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 814:
#line 5589 "cs-parser.jay"
  {
		yyVal = new Linq.ThenByAscending ((Expression)yyVals[0+yyTop]);	
	  }
  break;
case 815:
#line 5593 "cs-parser.jay"
  {
		yyVal = new Linq.ThenByAscending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 816:
#line 5597 "cs-parser.jay"
  {
		yyVal = new Linq.ThenByDescending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 818:
#line 5606 "cs-parser.jay"
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
case 819:
#line 5620 "cs-parser.jay"
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
   74,  190,  190,  191,  191,  191,  191,  194,  194,  195,
  188,  188,  188,  188,  188,  188,  188,  196,  196,  196,
  196,  196,  196,  196,  196,  196,  196,  189,  198,  198,
  198,  198,  198,  198,  198,  198,  198,  198,  198,  198,
  198,  198,  198,  198,  198,  198,  198,  199,  199,  199,
  199,  199,  199,  218,  218,  218,  217,  216,  216,  219,
  219,  200,  200,  200,  202,  202,  220,  203,  203,  203,
  203,  203,  224,  224,  225,  225,  226,  226,  227,  227,
  228,  228,  228,  228,  229,  229,  160,  160,  222,  222,
  222,  223,  223,  221,  221,  221,  221,  221,  232,  204,
  204,  231,  231,  205,  206,  206,  206,  207,  208,  209,
  209,  209,  233,  233,  234,  234,  234,  234,  234,  235,
  238,  238,  238,  239,  239,  239,  239,  236,  236,  240,
  240,  240,  240,  197,  192,  241,  241,  242,  242,  237,
  237,   84,   84,  243,  243,  244,  210,  245,  245,  246,
  246,  246,  246,  211,  212,  213,  214,  248,  215,  247,
  247,  250,  249,  201,  251,  251,  251,  251,  254,  254,
  254,  253,  253,  252,  252,  252,  252,  252,  252,  252,
  193,  193,  193,  193,  255,  255,  255,  256,  256,  256,
  257,  257,  258,  259,  259,  259,  259,  259,  260,  259,
  261,  259,  262,  262,  262,  263,  263,  264,  264,  265,
  265,  266,  266,  267,  267,  268,  268,  268,  268,  269,
  269,  269,  269,  269,  269,  269,  269,  269,  269,  269,
  270,  270,  271,  271,  271,  272,  272,  274,  273,  273,
  276,  275,  277,  275,   47,   47,  230,  230,  230,   77,
  279,  280,  281,  282,  283,   30,   61,   61,   60,   60,
   89,   89,  284,  284,  284,  284,  284,  284,  284,  284,
  284,  284,  284,  284,  284,  284,   64,   64,  285,   66,
   66,  286,  286,  287,  288,  288,  289,  289,  289,  289,
  290,   98,  291,  159,  133,  133,  292,  292,  293,  293,
  293,  295,  295,  295,  295,  295,  295,  295,  295,  295,
  295,  295,  295,  295,  309,  309,  309,  297,  310,  296,
  294,  294,  313,  313,  314,  314,  314,  314,  311,  311,
  312,  298,  315,  315,  299,  299,  316,  316,  318,  317,
  319,  320,  320,  321,  321,  324,  322,  323,  323,  325,
  325,  325,  300,  300,  300,  300,  326,  327,  332,  328,
  330,  330,  334,  334,  331,  331,  333,  333,  336,  335,
  335,  329,  337,  329,  301,  301,  301,  301,  301,  301,
  338,  339,  340,  340,  340,  341,  342,  343,  343,  343,
   83,   83,  302,  302,  302,  302,  344,  344,  346,  346,
  348,  345,  347,  347,  349,  303,  304,  350,  307,  352,
  308,  351,  351,  353,  353,  354,  305,  355,  306,  356,
  306,  359,  278,  357,  357,  360,  360,  358,  362,  362,
  361,  361,  364,  364,  365,  365,  365,  365,  365,  366,
  367,  368,  368,  370,  370,  369,  371,  371,  373,  373,
  372,  372,  372,  374,  374,  374,  363,  375,  363,
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
    1,    2,    2,    2,    2,    2,    2,    1,    3,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    3,    1,    1,
    4,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    3,
    3,    2,    2,    2,    4,    4,    1,    4,    4,    3,
    5,    7,    0,    1,    3,    4,    0,    1,    1,    3,
    3,    1,    3,    2,    1,    1,    0,    1,    1,    3,
    2,    1,    1,    2,    2,    4,    3,    1,    1,    4,
    2,    1,    3,    1,    4,    4,    2,    2,    2,    1,
    1,    1,    6,    3,    7,    4,    3,    2,    3,    4,
    0,    1,    3,    3,    1,    1,    1,    0,    1,    0,
    1,    2,    3,    2,    3,    0,    1,    1,    2,    0,
    1,    2,    4,    1,    3,    0,    5,    1,    1,    2,
    4,    4,    4,    4,    4,    4,    3,    0,    4,    0,
    1,    0,    4,    3,    1,    2,    2,    1,    3,    3,
    3,    1,    4,    1,    2,    2,    2,    2,    2,    2,
    1,    3,    3,    3,    1,    3,    3,    1,    3,    3,
    0,    1,    2,    1,    3,    3,    3,    3,    0,    4,
    0,    4,    1,    3,    3,    1,    3,    1,    3,    1,
    3,    1,    3,    1,    3,    1,    5,    3,    3,    3,
    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,
    1,    3,    3,    2,    1,    0,    1,    0,    2,    1,
    0,    4,    0,    6,    1,    1,    1,    1,    1,    1,
    1,    0,    0,    0,    0,   13,    0,    1,    0,    1,
    1,    2,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    0,    1,    2,    0,
    1,    1,    2,    4,    1,    3,    1,    3,    1,    1,
    0,    4,    0,    4,    0,    1,    1,    2,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    0,    4,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    3,
    3,    2,    1,    1,    1,    1,    5,    7,    0,    6,
    3,    0,    1,    1,    2,    0,    3,    1,    2,    3,
    1,    1,    1,    1,    1,    1,    5,    7,    0,   10,
    0,    1,    1,    1,    0,    1,    0,    1,    1,    1,
    3,    6,    0,    9,    1,    1,    1,    1,    1,    1,
    2,    2,    3,    4,    3,    3,    3,    4,    3,    3,
    0,    1,    3,    4,    5,    3,    1,    2,    0,    1,
    0,    4,    0,    1,    4,    2,    2,    0,    3,    0,
    7,    1,    3,    3,    1,    0,    6,    0,    6,    0,
    6,    0,    3,    4,    5,    4,    5,    3,    2,    4,
    0,    1,    1,    2,    1,    1,    1,    1,    1,    4,
    2,    9,   10,    0,    2,    2,    1,    3,    1,    3,
    1,    2,    2,    1,    2,    2,    0,    0,    4,
  };
   static  short [] yyDefRed = {            0,
    6,    0,    0,    0,    0,    0,    4,    0,    7,    9,
   10,   11,   17,   18,   44,    0,   43,   45,   46,   47,
   48,   49,   50,   51,    0,   55,  141,    0,   20,    0,
    0,    0,   63,   61,   62,    0,    0,    0,    0,    0,
   64,    0,    1,    0,    8,    3,  628,  634,  626,    0,
  623,  633,  627,  625,  624,  631,  629,  630,  636,  632,
  635,    0,    0,  621,   56,    0,    0,    0,    0,    0,
  344,    0,   21,    0,    0,    0,    0,   59,    0,   66,
    2,    0,  373,  379,  386,  374,    0,  376,    0,    0,
  375,  382,  384,  371,  378,  380,  372,  383,  385,  381,
    0,    0,    0,    0,    0,    0,  360,  361,  377,  622,
  651,  157,  142,  156,   14,    0,    0,    0,    0,    0,
  354,    0,    0,    0,   65,   58,    0,    0,    0,  419,
    0,  413,    0,  464,  418,  506,    0,  387,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  417,  414,
  415,  416,  411,  412,    0,    0,    0,    0,   70,    0,
    0,   75,   77,  390,  427,    0,    0,  389,  392,  393,
  394,  395,  396,  397,  398,  399,  400,  401,  402,  403,
  404,  405,  406,  407,  408,  409,  410,    0,    0,  606,
  470,  471,  472,  534,    0,  528,  532,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  607,  605,  608,  609,
  782,    0,    0,    0,    0,  363,    0,    0,    0,  131,
    0,    0,  343,  358,  612,    0,    0,    0,  362,    0,
    0,    0,    0,    0,  359,    0,   19,    0,    0,  351,
  345,  346,   57,  467,    0,    0,    0,  145,  146,  522,
  518,  521,  478,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  527,  535,  536,
  526,  540,  539,  537,  538,  601,    0,    0,    0,  349,
  184,  183,  185,    0,    0,    0,    0,  591,    0,    0,
   69,    0,    0,    0,    0,    0,    0,    0,    0,  468,
  469,    0,  461,  423,    0,    0,    0,  424,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  561,  559,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   29,    0,    0,    0,    0,  325,  125,    0,    0,  127,
    0,    0,    0,  347,    0,    0,  126,  150,    0,    0,
    0,  212,    0,  101,    0,  498,    0,    0,  123,    0,
  147,  489,  494,  388,  694,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  768,    0,    0,    0,  678,    0,  693,  662,    0,
    0,    0,    0,  657,  659,  660,  661,  663,  664,  665,
  666,  667,  668,  669,  670,  671,  672,  673,  674,    0,
    0,    0,    0,    0,  695,  696,  713,  714,  715,  716,
  735,  736,  737,  738,  739,  740,  355,  462,    0,    0,
    0,    0,    0,  487,    0,    0,    0,    0,    0,    0,
  482,  479,    0,    0,    0,    0,  474,    0,  477,    0,
    0,    0,    0,    0,  421,  420,  364,    0,  366,  365,
    0,    0,   80,    0,    0,  594,    0,    0,    0,  524,
    0,   76,   79,   78,  542,  544,  543,    0,    0,    0,
    0,  452,    0,  453,    0,  449,    0,  517,  529,  530,
    0,    0,  531,    0,  580,  581,  582,  583,  584,  585,
  586,  587,  588,  590,  589,    0,  541,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  579,    0,    0,  578,    0,    0,    0,    0,
    0,  783,  795,    0,    0,  793,  796,  797,  798,  799,
    0,   25,   23,    0,    0,    0,    0,    0,  124,  752,
    0,    0,  139,  136,  133,  137,    0,    0,    0,  132,
    0,    0,  613,  208,   97,  495,  499,    0,    0,  741,
  766,    0,    0,    0,  742,  676,  675,  677,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  699,    0,
    0,  767,    0,    0,  687,    0,    0,    0,  679,  652,
  686,    0,    0,  684,  685,  683,  658,  681,  682,    0,
  688,    0,  692,  466,    0,  465,  515,  191,    0,    0,
    0,  159,    0,    0,    0,  172,  519,    0,  422,    0,
  480,    0,    0,    0,    0,    0,  439,  442,    0,    0,
  476,  502,  504,    0,  514,    0,    0,    0,    0,    0,
  516,  784,    0,  533,  600,  602,    0,  353,  391,  593,
  592,  603,  460,  459,  455,  454,    0,  428,  451,    0,
  425,  429,    0,    0,    0,  426,    0,  562,  560,    0,
  611,  801,    0,    0,    0,    0,    0,    0,  806,    0,
    0,    0,    0,  794,   32,   12,    0,   30,    0,    0,
  329,    0,  130,    0,  128,  135,    0,    0,  348,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  119,    0,
    0,  723,  730,    0,  722,    0,    0,  610,    0,  745,
  743,    0,    0,  746,    0,  747,  756,    0,    0,    0,
  757,  769,    0,    0,    0,  750,  749,    0,    0,    0,
    0,  463,    0,    0,    0,  181,    0,  523,    0,    0,
    0,  484,    0,  483,  444,    0,    0,  435,    0,    0,
    0,    0,    0,    0,  510,    0,  507,    0,  785,  599,
    0,  457,    0,  450,  431,    0,  552,  553,  577,    0,
    0,    0,    0,    0,  812,  813,    0,  789,    0,    0,
  788,    0,   13,   15,    0,    0,  339,    0,  326,  129,
    0,  151,  153,    0,    0,  638,    0,    0,  155,  148,
    0,    0,    0,    0,    0,  772,  719,    0,    0,    0,
  744,    0,  776,    0,    0,  761,  764,  754,    0,  758,
  780,  778,    0,  748,  680,  493,  189,  190,    0,  182,
    0,    0,    0,  165,  173,  166,  168,  443,  445,  446,
  441,  436,  440,    0,  473,  434,  505,  503,    0,    0,
    0,  604,  456,    0,  786,    0,    0,    0,  800,    0,
    0,  809,    0,  818,   33,   16,   41,    0,    0,    0,
    0,  330,    0,  334,    0,    0,    0,    0,    0,  368,
    0,  614,    0,  642,  209,   98,    0,  121,  120,    0,
    0,  770,    0,    0,  731,    0,    0,    0,    0,    0,
    0,    0,  755,    0,    0,  717,  177,    0,  187,  186,
    0,    0,  501,  475,  511,  513,  512,  432,  787,    0,
    0,  815,  816,    0,  790,    0,   34,   31,   42,  340,
    0,    0,    0,  333,  138,  152,  154,    0,    0,    0,
  643,    0,    0,  149,    0,  774,    0,  773,  726,    0,
  732,    0,    0,  777,    0,  700,  760,    0,  762,  781,
  779,    0,    0,  169,  167,    0,    0,  810,  819,    0,
    0,  331,  335,  369,    0,    0,  615,    0,  210,  102,
   99,  718,  771,    0,  733,  698,  712,    0,  711,    0,
    0,  704,    0,  708,  765,  175,  178,    0,    0,  341,
    0,  649,    0,  650,    0,    0,  645,    0,   95,   87,
   88,    0,    0,   84,   86,   89,   90,   91,   92,   93,
   94,    0,    0,  223,  224,  226,  225,  222,  227,    0,
    0,  216,  218,  219,  220,  221,    0,    0,    0,    0,
    0,  728,    0,    0,  701,  705,    0,  709,    0,    0,
  338,    0,    0,    0,    0,    0,    0,   81,   85,  616,
    0,    0,  213,  217,  211,  116,  109,  110,  108,  111,
  112,  113,  114,  115,  117,    0,    0,  106,  100,    0,
  734,  710,    0,    0,  802,    0,  648,  646,    0,    0,
    0,    0,    0,    0,  251,  257,    0,    0,    0,  299,
  618,    0,    0,    0,  103,  107,  720,  805,  803,    0,
    0,  285,    0,  284,    0,    0,    0,    0,    0,    0,
  653,  292,  286,  291,    0,  288,  320,    0,    0,    0,
  241,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  263,  262,  259,  264,  265,  258,  277,  276,  269,
  270,  266,  268,  267,  271,  260,  261,  272,  273,  279,
  278,  274,  275,    0,    0,    0,    0,  254,  253,  252,
    0,  295,    0,    0,    0,    0,  243,    0,    0,    0,
  238,    0,  118,  305,  302,  301,  282,  280,    0,  255,
    0,    0,    0,  193,    0,    0,    0,  200,    0,  321,
    0,    0,    0,  245,  242,  244,    0,    0,    0,    0,
    0,    0,    0,  290,    0,  323,  162,    0,    0,  654,
    0,    0,    0,    0,  197,  199,    0,    0,  235,    0,
  239,  232,  310,    0,  303,    0,    0,    0,    0,    0,
    0,  194,  293,  294,  201,  203,  322,  300,  246,    0,
  248,    0,    0,    0,    0,    0,    0,    0,  306,    0,
  307,  283,  281,  256,  324,    0,    0,    0,    0,  236,
    0,  240,  233,  314,    0,  318,    0,  315,  319,  304,
    0,    0,  195,  206,  205,  202,  204,  247,    0,  249,
    0,  313,  317,  229,  231,  237,    0,  234,    0,  250,
    0,  230,
  };
  protected static  short [] yyDgoto  = {             5,
    6,    7,    8,    9,   10,   11,   12,  707,  815,   13,
   14,  103,   32,   15,  629,  342,  212,  553,   77,  708,
  551,  709,  816,  898,  812,  899,   17,   18,   19,   20,
   21,   22,   23,   24,  630,   26,   38,   39,   40,   41,
   42,   80,  158,  159,  160,  161,  398,  163, 1007, 1042,
 1043, 1044, 1045, 1046, 1047, 1048, 1049, 1050, 1051,   62,
  104,  164,  365,  825,  724,  912, 1011,  973, 1069, 1106,
 1068, 1107, 1108,  119,  728,  729,  739,  230,  349,  350,
  220,  565,  561,  566,   27,  113,   66,    0,   63,  250,
  232,  631,  579,  917,  571,  906,  907,  399,  632, 1221,
 1222,  633,  634,  635,  636,  764,  765,  286,  767, 1197,
 1230, 1249, 1296, 1231, 1232, 1316, 1297, 1298,  363,  723,
 1009,  972, 1067, 1060, 1061, 1062, 1063, 1064, 1065, 1066,
 1092, 1326,  400, 1329, 1283, 1321, 1280, 1319, 1239, 1282,
 1265, 1258, 1299, 1301, 1327, 1125, 1200, 1150, 1194, 1245,
 1126, 1243, 1242, 1127, 1153, 1128, 1156, 1146, 1154,  493,
 1087, 1158, 1241, 1287, 1266, 1267, 1305, 1307, 1129, 1205,
 1254,  346,  712,  556,  902,  818,  962,  903,  904, 1001,
  900, 1000,  613,   71,  280,  120,  121,  165,  107,  108,
  265,  233,  166,  909,  910,  109,  234,  167,  168,  169,
  170,  171,  172,  173,  174,  175,  176,  177,  178,  179,
  180,  181,  182,  183,  184,  185,  186,  187,  188,  189,
  494,  495,  496,  875,  457,  645,  646,  647,  871,  190,
  439,  675,  191,  192,  193,  373,  944,  450,  451,  614,
  367,  368,  654,  258,  659,  660,  251,  443,  252,  442,
  194,  195,  196,  197,  198,  199,  798,  688,  200,  522,
  521,  201,  202,  203,  204,  205,  206,  207,  208,  287,
  288,  289,  666,  667,  209,  472,  791,  210,  692,  361,
  722,  970, 1052,   64,  826,  913,  914, 1036, 1037,  236,
 1201,  403,  404,  586,  587,  588,  408,  409,  410,  411,
  412,  413,  414,  415,  416,  417,  418,  419,  589,  759,
  420,  421,  422,  423,  424,  425,  426,  745,  986, 1020,
 1021, 1022, 1023, 1077, 1024,  427,  428,  429,  430,  734,
  980,  924, 1070,  735,  736, 1072, 1073,  431,  432,  433,
  434,  435,  436,  750,  751,  988,  846,  932,  847,  603,
  835,  977,  836,  929,  935,  934,  211,  542,  340,  543,
  544,  703,  811,  545,  546,  547,  548,  549,  550, 1115,
  699,  700,  891,  892,  956,
  };
  protected static  short [] yySindex = {           62,
    0, -348, -206, -210,    0,   62,    0, -137,    0,    0,
    0,    0,    0,    0,    0, 8394,    0,    0,    0,    0,
    0,    0,    0,    0, -236,    0,    0, -306,    0,  434,
 -174,  -56,    0,    0,    0,   52, -174, -288,  -23,   80,
    0,   69,    0, -137,    0,    0,    0,    0,    0, -288,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0, 8885,11492,    0,    0,  231,   92, -288, 8975,   46,
    0,   81,    0,   52,  -23, -288,  282,    0, 6502,    0,
    0, -174,    0,    0,    0,    0, 6180,    0,  127, 6180,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 -282,  253,   -9,   29,  973,  321,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  685,  335, 8975,  263,  225,
    0,  355,  355,  373,    0,    0, -106,  398,  -27,    0,
  280,    0,  406,    0,    0,    0,  418,    0, 9015, 6585,
 6990, 6990, 6990, 6990, 6990, 6990, 6990, 6990,    0,    0,
    0,    0,    0,    0,  261, 1805, 6180,  435,    0,  410,
  440,    0,    0,    0,    0,  704,  558,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  -46,  502,    0,
    0,    0,    0,    0,  590,    0,    0,  167,  487, -125,
  498,  452,  522,  478,  482, -278,    0,    0,    0,    0,
    0,  554,  -55,  559, -205,    0,  365,  566,  648,    0,
  -27,  493,    0,    0,    0,  700,  728,  618,    0,  721,
  515,  -27,  631,  321,    0, 2110,    0,  263, 8975,    0,
    0,    0,    0,    0, 6585,  557, 6585,    0,    0,    0,
    0,    0,    0, 2437,  -22,  666, 6180,  672, 6585,  -26,
   74, -249, -149,  321,  347,  725,  274,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 6585, 8975,  658,    0,
    0,    0,    0,   52,  124, 6180,  677,    0,  687,  384,
    0, 6502, 6502, 6990, 6990, 6990, 5609, 5204,  668,    0,
    0,  716,    0,    0, 7203,  619, 7278,    0,  717, 6585,
 6585, 6585, 6585, 6585, 6585, 6585, 6585, 6585, 6585, 6585,
 6990, 6990, 6990, 6990,    0,    0, 6990, 6990, 6990, 6990,
 6990, 6990, 6990, 6990, 6990, 6990, 5692, 6990, 6585,  620,
    0,  775,  799,  -27, 6180,    0,    0,  824,  742,    0,
 6585, 5287, 8975,    0,  753,  754,    0,    0,  290,  -27,
  757,    0,  757,    0,  757,    0,  832,  829,    0,  -27,
    0,    0,    0,    0,    0,  837,  104, 7408,  838, 2110,
  -27,  -27,  -27, -207,  844,  847, 6585,  848, 6585,  855,
  585,    0,  -27,  840,  859,    0,  -68,    0,    0,  864,
  609,  407, 2110,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  858,
  868,  754,  614,  871,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  644,  355,
  870,  266,  855,    0, 6585,  470,  558,    0,    3,  568,
    0,    0, 5853, 5609, 5204,  109,    0, 4799,    0,  512,
 9055,  877, 6585,  952,    0,    0,    0, 6990,    0,    0,
 7073,  855,    0,  412,  355,    0,  130, 1805,  907,    0,
  440,    0,    0,    0,    0,    0,    0,  657, 6585, 6585,
  885,    0,  886,    0, -141,    0,  355,    0,    0,    0,
 4882,  558,    0,  355,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  704,    0,  704,  167,  167,
 6180, 6180,  487,  487,  487,  487, -125, -125,  498,  452,
  522,  478,    0,  884,  482,    0, 6585, 9093, 9133,  809,
 6585,    0,    0,  334,  620,    0,    0,    0,    0,    0,
 -123,    0,    0,  266,  263,  896, 5936,  815,    0,    0,
  897, 6180,    0,    0,    0,    0,  413,  892, -250,    0,
  266,  266,    0,    0,    0,    0,    0,  266,  266,    0,
    0,  888,  378,  818,    0,    0,    0,    0,  932, 6180,
 2193, 6180, 6585,  902,  905, 6585, 6585,  906,    0,  914,
  314,    0,  855, 6746,    0, 6585,  915, 6097,    0,    0,
    0,    0,  631,    0,    0,    0,    0,    0,    0,  909,
    0,  754,    0,    0, 6585,    0,    0,    0,  445, -236,
  920,    0,  919,  921,  922,    0,    0, 5287,    0, 7479,
    0, 2437, 6258,  299,  929,  927,    0,    0,  671,  930,
    0,    0,    0,  933,    0, -286,  379,  263,  931,  935,
    0,    0, 6585,    0,    0,    0, 6585,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 5043,    0,    0, 5204,
    0,    0, -149,  934, -110,    0, -178,    0,    0, 6585,
    0,    0,   37,  140,   41,  169,  923,  731,    0,  942,
 6585, 6585,  949,    0,    0,    0, 1029,    0,  985,  957,
    0,  775,    0,  960,    0,    0,  395,    0,    0,  961,
  964,  962,  962,  962,  966,  967,  959,  970,    0,  972,
  208,    0,    0,  965,    0,  971, -227,    0,  974,    0,
    0,  976,  977,    0, 6585,    0,    0,  -27,  855,  247,
    0,    0,  980,  981,  982,    0,    0,  983, 2110,  954,
  909,    0,  445, 6180,   26,    0, 6180,    0,  284, 1097,
 1101,    0, 4882,    0,    0,  586, 6341,    0, 5448,  631,
  995, 5287,  997,  912,    0,  917,    0,  936,    0,    0,
  855,    0, -163,    0,    0, 5204,    0,    0,    0, 6585,
 1068, 6585, 1072, 6585,    0,    0, 6585,    0, 1016,  937,
    0, 1004,    0,    0,  985, -236,    0, -236,    0,    0,
 5609,    0,    0, 6180, 1030,    0, 1030, 1030,    0,    0,
 6585,  818, 6585,  993,  759,    0,    0, 2354, 6585, 1092,
    0, 2110,    0, 1021, 6180,    0,    0,    0,  855,    0,
    0,    0, 2110,    0,    0,    0,    0,    0, -196,    0,
 -182, 1023, 1025,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  666,    0,    0,    0,    0, -311,  -31,
  947,    0,    0, 1036,    0, 6585, 1044, 6585,    0,  741,
 1035,    0, 6585,    0,    0,    0,    0, -132, -236, 1030,
  968,    0, 1037,    0, 1040, 1030, 1030,  263, 1038,    0,
  975,    0, 1030,    0,    0,    0, 1030,    0,    0, 1041,
 6585,    0,  979, 6585,    0, 1048, 6585, 1137, 2110, 1056,
  217,  855,    0, 2110, 2110,    0,    0, -244,    0,    0,
 1163, 1166,    0,    0,    0,    0,    0,    0,    0, 6585,
 1077,    0,    0, 6585,    0,  620,    0,    0,    0,    0,
    0, 1063, -236,    0,    0,    0,    0, 6180, 1057, 1069,
    0, 1071, 1073,    0, 1067,    0, 2110,    0,    0, 1074,
    0, 1079, 2110,    0, -208,    0,    0, 1080,    0,    0,
    0, 1075, 6585,    0,    0, 1103, 6585,    0,    0, 1081,
 1078,    0,    0,    0, 5126, -236,    0, -236,    0,    0,
    0,    0,    0, 2354,    0,    0,    0, 6585,    0, 1076,
 -208,    0, -208,    0,    0,    0,    0, 6585, 1109,    0,
 6585,    0, 1094,    0,  263, 1089,    0,11522,    0,    0,
    0, 1099, -236,    0,    0,    0,    0,    0,    0,    0,
    0,  775,11492,    0,    0,    0,    0,    0,    0, 1102,
 -236,    0,    0,    0,    0,    0,  775, -236,  775, 1100,
  971,    0, 2110, 1096,    0,    0, 2110,    0, 1114, 6585,
    0, 1104, 5126,    0,    0, 8622, 1098,    0,    0,    0,
 -176, 5531,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0, 1110, -236,    0,    0, 2110,
    0,    0, 2110,  998,    0, 1114,    0,    0, 6180, 6180,
  207,  251,   52,  603,    0,    0,  350, 1105, 1112,    0,
    0, 6180, -277, -203,    0,    0,    0,    0,    0,  224,
  234,    0, 6180,    0, 6180,  -27, 2205, 1115, 1108,  465,
    0,    0,    0,    0,  195,    0,    0, 1018, -181,   96,
    0, 1116,  273,   96,  758,  454,  -78,  761, -276, -276,
  266,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  -27,    0, -243, 1121,    0,    0,    0,
 2110,    0,  -27,  -27, -168, 1118,    0,  333,  266,    0,
    0,  266,    0,    0,    0,    0,    0,    0, 1111,    0,
 1124,  266, 1126,    0, 1129, 5204, 5204,    0,11492,    0,
 -168, -168, 1130,    0,    0,    0, 1132, 1136, -168, 1143,
 -139,    0,    0,    0,    0,    0,    0,  266, -168,    0,
 1144, 1145,  776, 1150,    0,    0,  855, -139,    0, 1153,
    0,    0,    0,11342,    0, -236, -236, 1152, 1157, 1159,
 1155,    0,    0,    0,    0,    0,    0,    0,    0, 1030,
    0, 1164, 1030, 1275, 1280,11372, 1172,10989,    0,11402,
    0,    0,    0,    0,    0, 1173,  532,  532, 1177,    0,
 -168,    0,    0,    0,  855,    0,  855,    0,    0,    0,
11432,11462,    0,    0,    0,    0,    0,    0,  534,    0,
  534,    0,    0,    0,    0,    0, 1178,    0, 2110,    0,
 1182,    0,
  };
  protected static  short [] yyRindex = {         1630,
    0,    0,    0,    0,    0, 1630,    0, 1548,    0,    0,
    0,    0,    0,    0,    0, 8701,    0,    0,    0,    0,
    0,    0,    0,    0, 1551,    0,    0,    0,    0,  695,
 1176,    0,    0,    0,    0,  615,  625,    0, 1183,    0,
    0,  676,    0, 1548,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  196, 8522,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 2859, 1183, 1188,    0,    0, 1193,    0,
    0, 1199,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
11247,  289, 2993,    0,    0, 2993,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 3107,    0,  459,    0,
    0, 2725, 2725,    0,    0,    0,    0,    0, 1200,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,10966, 1197,    0,    0,    0, 1198,
 1201,    0,    0,    0,    0, 9433, 9258,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 9362,    0,    0, 9708, 9926,  820,
10433,10551,10669,10787,10862, 1026,    0,    0,    0,    0,
    0,    0,    0, 1204,    0,    0,   65,    0,    0,    0,
    0,    0,    0,    0,    0, 1123, 1125, 1202,    0,    0,
    0,    0, 2550, 2993,    0, 1207,    0,  474,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  600,    0,    0,    0,    0,    0,  -82,
    0, 3221,    0,  436,    0, 1055, 3221,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  134,    0,    0, 1205,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1202, 1209,    0,    0,
    0,    0,    0,    0,    0, 3339,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  492,
    0, 1386, -213,    0,    0,    0,    0,    0,    0,    0,
 1212,    0,    0,    0,    0,    0,    0,    0,  179,    0,
    0,    0,    0,    0,    0,    0,    0, 1214,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1203,    0, 1203,    0,
    0,    0,    0,   64,    0,    0, 8351,    0,    0,    0,
  -57, 7453, 1217,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  -98,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 3457,
    0, 8861,    0,    0,    0,  330,    0,  371,    0,    0,
    0,    0, 1218, 1202, 1209, -111,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  553, 6829,    0,    0, 3457,    0,    0,    0,    0,    0,
 1216,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 -120,    0,    0,    0, 1221,    0, 3457,    0,    0,    0,
    0, 3915,    0, 3457,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0, 9535,    0, 9606, 9779, 9855,
    0,    0,10002,10073,10149,10220,10291,10362,10480,10598,
10716,10834,    0,    0,10906,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  751,    0,    0,    0,    0,    0,
 4089,    0,    0, 8861, 1227,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  -81,  387,    0,
 8861, 8861,    0,    0,    0,    0,    0, 8861, 8861,    0,
    0,  -57, 1138,    0,    0,    0,    0,    0,    0,    0,
 1219,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  -86,    0,    0,    0,    0,    0,    0,    0,  688,
    0,    0,    0,    0,    0,    0,    0,    0, 9173, 7663,
    0,    0,  769,  778,  782,    0,    0,    0,    0,    0,
    0,    0,    0,11117,    0, 1228,    0,    0,    0,    0,
    0,    0,    0, 1229,    0,  510,  561, 1226,    0, 1231,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 1230, -109,    0,    0,11057,    0,    0,    0,
    0,    0,  -82,    0,  -82,    0,    0,  724,    0,  608,
    0,    0, 2469,    0,    0,    0, 4180,    0, 4281,    0,
    0, 1222,    0,    0,    0,    0,  607,   75,    0,    0,
    0,  302,  302,  302,    0,    0,  781, 1224,    0,    0,
    0,    0,    0,    0,    0, 1225,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 1238,    0, 1722,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 1156,
  694,    0, 9211,    0, 9251,    0,    0,    0, 8935,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 3575,
 3679, 1240,    0,    0,    0,    0,    0,    0,    0,    0,
 6829,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 4372, 4473,    0,  -59,    0,    0,
 1202,    0,    0,    0, 1242,    0, 1242, 1242,    0,    0,
    0,    0,    0,  784,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  660,    0,
    0,  794,  811,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 3797,    0,    0,    0,    0,  510,  510,
    0,    0,    0, -108,    0,    0,    0,    0,    0,  867,
  969,    0,    0,    0,    0,    0,    0,    0, 4557, 1235,
    0,    0, 1229,    0,    0,  539,  539,  362,  341,    0,
    0,    0,  570,    0,    0,    0,  539,    0,    0,    0,
    0,    0,    0, 1237,    0,    0,    0, 1916,    0,    0,
 1244,    0,    0,    0,    0,    0,    0,  670,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  492,    0,    0,    0,    0,
  444,    0,  -49,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 1250,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0, 7747,    0, 8267,    0,    0,
    0,    0,    0, 1247,    0,    0,    0,    0,    0,    0,
 1253,    0, 4638,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  354,  305,    0, 8590,    0,    0,
    0,    0, 7842,    0,    0,    0,    0,    0,    0,    0,
    0, 1222, 1467,    0,    0,    0,    0,    0,    0,    0,
 8346,    0,    0,    0,    0,    0, 1222, 7926, 1222,    0,
 1251,    0,    0,    0,    0,    0,    0,    0,  978,    0,
    0,    0,    0, 8105, 8188,  196,    0,    0,    0,    0,
 4721,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 8021,    0,    0,    0,
    0,    0, -212,    0,    0,  978,    0,    0,    0,    0,
    0,    0,  349,    0,    0,    0,    0,  571,    0,    0,
    0,    0, -116,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 1255,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  182,    0,    0,  -92,    0,    0,    0,    0,
 8861,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 8741,    0,    0,    0,    0,    0,
 1207,    0,    0,    0, 3106,    0,    0,    0, 8861, 8741,
    0, 8861,    0,    0,    0,    0,    0,    0,    0,    0,
    0, 8935,    0,    0,    0, 1209, 1209,    0,  822,    0,
 3220,11282,    0,    0,    0,    0,    0,    0, 3106,    0,
  255, 8786, 8786,    0, 8786,    0,    0, 2006, 3106,    0,
    0,    0,    0,    0,    0,    0,    0,  255,    0,    0,
    0,    0,    0,    0,    0,  555, 1314,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  539,
    0,    0,  539, 1257, 1259,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 3106,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 1207,    0,
    0,    0,
  };
  protected static  short [] yyGindex = {            0,
    0,  630, 1623, 1625, -475, -620, -479,    0,    0,    0,
    0,    9,    0,    0,    1,    0,    0, -681,  -67,    0,
    0,    0,    0,    0,    0,    0, -698, -692, -649, -779,
 -564, -551, -500, -497,   32,   -5,    0, 1595,    0, 1558,
    0,    0,    0,    0,    0, 1343,  807, 1344,    0,    0,
    0,  596, -684, -969, -942, -734, -710, -569, -509, -971,
    0, -126,    0,  416,    0, -788,    0,    0,    0,    0,
    0,    0,  535,  269,  501,  812, -765,  -95,    0, 1087,
 1293, -413,  795, -241,    0,    0,    0,    0, -102, -169,
  -29, -536,    0,    0,    0,    0,    0,  -62,  428, -552,
    0,    0,  887,  889,  891,    0,    0, -573,  898,    0,
 -609,    0,    0,    0,    0,  357,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  602,    0,    0,    0,    0,
    0,  344,-1110,    0,    0,    0,    0,    0,    0,    0,
  409,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, -438,
    0,    0,    0,    0,  402,  411,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  709,    0,
    0,    0,  -77, -101, -173,   97, 1434,  -60,    0,    0,
    0, 1411,  -99,    0,  710,    0, -112, -226,    0,    0,
 1374, -230,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, -229,    0,
 -452, -468, -596,    0,  268,    0,    0,  911,    0, -419,
 -260, 1191,    0,    0,    0,  926,    0,    0, 1043, -337,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 1381,  153, 1385,    0,  833,  752,    0, 1179,  866,    0,
    0, 1369, 1373, 1376, 1377, 1378,    0,    0,    0,    0,
 1234,    0,  924,    0,    0,    0,    0,    0, -542,    0,
    0,    0,    0,  -63,    0,    0,  801,    0,  636,    0,
    0,  649, -392, -231, -222, -220,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, -718,    0,
 -266,    0, 1349,    0, -568,    0,    0,    0,    0,    0,
    0,  711,    0,    0,  707,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  722,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  987,    0,    0,    0,    0,    0,
    0,    0,  816,    0,    0,    0,    0,  786,    0,    0,
    0,    0,    0,    0, 1208,    0,    0,    0,    0,  628,
    0,    0,    0,  792,    0,
  };
  protected static  short [] yyTable = {           110,
   16,  106,  231,  114,  405,  219,   16,  124,  106,  402,
  617,   31,   37,  406,  459,  407,  650,  710,  256,   65,
  241,  242,  733,  448,  449,  224,  106,  447,  235,  106,
  819,   25,  685,  648,  720,  721,  488,   25,  915,  916,
  266,  725,  726,  354,  653,   33,   37, 1017,  684,   29,
  347,  358, 1161,  742,  303,  766,  707,  106,   82,  937,
 1018,  593,  371,  755,  616,  918, 1086,  839,   69,  594,
  106,  221,   34,  939, 1207,  706,  116,  797,  106,  264,
  502, 1091, 1223,  794,   37,  622,  344, 1228,  814,  228,
 1225,  248,  679,   69,  276,  106,  106, 1119, 1100,   28,
  118,   87,  216,  784,   35,   89,  465,  216,  229,  218,
  337,  960,   65,  218,  679,  351, 1263,  966,  967,    1,
  372,  992, 1162,  928,  957, 1101,  352,  352,  974,    4,
  278,  338,  705,  993,  936,  458,  325, 1100,  228,  387,
  279,   67,  456,  945,  342,  679,  453,  450,  449,  244,
  352,  583,  707,  402,  352,  339,  374,  229,  352,   74,
  228,    2,  228,  350, 1101,  217, 1131,  110,  785,  228,
   74,  405,  249,  326,  554,  401,  402, 1214,  106,  229,
  406,  229,  407,  228,  228,  354,  467,  228,  229,  857,
  572,  860,  360,  649,  896,   72,  106,    4,  607,  884,
  578,  370,  229,  229,  568,  883,  229,  680,  793,  387,
  984,  590,  591,  592,  651,  990,  991,  106, 1331,  466,
  840,  516,  518,  604,  772,  106,    4, 1027, 1054,  680,
  218,  813,  958,  452,  573,  352,  574,   36,  575,  218,
  595,   30,  348,  707, 1163,  616,  608, 1019,  458,  387,
  458,  938, 1074,  327,  342,  328,  342, 1253, 1013,  245,
  796,  453,  450,  246, 1016,  940, 1208,  342,  463,  925,
  118,  350,  387,  342,  329,  276,  330,  350,  134,  343,
  134, 1054,  350,  350,  106, 1215,  352,  350,  350,  303,
  920,   72,  106,  268,  269,  270,  271,  272,  273,  274,
  275,  225,  350,  609,  685,  332,  350, 1039,  342,  118,
  228,  278,  427, 1040,  581,   27,   73,  582,    1,  401,
  684,  279,  304, 1059,  732,  226,  620,  601,  602,  229,
  105,  800,  281, 1102,  303,  802,  897,  754,  626,  282,
  248,  453,  401,  454, 1039,  455,    2,   76,   69,  490,
 1040,  283,  227,   69, 1111,  213, 1041, 1103,  215,  648,
   72,  491,  229,   70,  402,  350,  855,  352,  877, 1096,
 1090,  304, 1102,  669,  474, 1097, 1059,  402,  374,  223,
  637,  979,  776, 1099,  118, 1095,  238, 1109,   53,  303,
  490, 1137,  343, 1041,  719,  681, 1103,    3,   53,  255,
  106,  266,  686,  305,  306,  307,  308,  261, 1096,  665,
  449,  448,  449,  502, 1097,  447,   69,  106, 1098,  959,
   69,  249, 1099,  946,  285,  290,   70,    4,  350,  387,
   70,   69,  352,  387,  350,  134,   79,  134,  351,  228,
  264,   70,  353, 1055,  351,  733,  485,  486,  487,  567,
 1202,   78,  387,  639,  640,  308, 1056, 1098,  229, 1203,
  106,  106, 1142,  248,  115,   72,  242,  111,  617,  657,
  303,  247,  458,  517,  517,  517,  517,  106,  106,  517,
  517,  517,  517,  517,  517,  517,  517,  517,  517,  228,
  517, 1300,  617,  122, 1303,  228, 1055,  223, 1104,  350,
  760,  106,  595,  350,  595,  228, 1144, 1057,  229, 1056,
 1058,  387, 1143,  352,  229,   53,  748,   53,  350,  617,
 1204,  464,  350,  353,  229,  460,  761,  405,  123,  106,
  401,  106,  402,  849,  228,  253,  406, 1104,  407,   53,
  752,  321,  322,  401,  249,   83,  352,   84,  350,  350,
   85,  350,   53,  229,  477,   86, 1145,   53, 1105,   88,
 1057,  618,   53, 1058,   53,   53,   53,   53,   91,  747,
   53,  476,   53,  228,  214,   92,   53,  670,  845,  224,
   93,  350,  228,  748,   94,  618,  783,  801,   53,  228,
  222,   53,  229,   53,  111,  239,   95, 1105,   96,  228,
  749,  229,   97,  112,  276,  517,  240,  628,  229,  350,
   98,   99,  618,  555,  100,  402,  803,  117,  229,  311,
  664, 1255, 1256,  485,   65,  862,  402,  352,  228, 1261,
  350,    4,  943,  350, 1219,   43, 1211,   46,  277, 1272,
  278,  637,  276,  254,  644,  228,  350,  229,  126,    4,
  279,  350,   69,  278,  350,  834,  350, 1238,  350,  350,
  350,  350,   70,  279,  987,  637,  350,  372,  644,  353,
  350,  727, 1237,   81,  350, 1240,  777,  644,  278,   70,
  639, 1166,  350,  701,  702,  350,  848,  350,  279, 1268,
 1269, 1320, 1270,  647,  485,  352, 1234,  352,  401,  352,
  485,  370,  402,  106,  639, 1235,  106,  402,  402,  223,
 1236, 1271,  228, 1151,  350,  468,  289,  647,  350,  216,
  617,  352, 1152,  352,  647,  370,  647,   74,  665,  658,
  351,  469,  370,  350,   69,  486,  394,  350,  394,  243,
  394,  486,  352,  297,  353,  298,  285,  299,  786,  228,
  402,  281,  480,  763,   70,  134,  402,  134,  282,  134,
  821,  352,  394,  106,  394,  247,  612,  223,  229,  300,
  283,  301,  297,  257,  298,   47,  299,  241,  242,  229,
  292,  401,  239,  239,  106,  259,  933, 1251, 1252,  687,
  687,  615,  401,  668,  718,  612,  350,  289,  300,   48,
  301,  342,  394,  291,  342,  427,  694,  696,  336,  302,
  293,   68,   49,   69,  336,   53,   16,   51,  901,  351,
  342,  337,   52,   70,   53,   54,   55,   56,  111,  356,
  717,  352,   57,   69,  333,  964,   58, 1198,  302,   53,
  356,  791,  791,   70,  357,  405,  402,  638,   59,  278,
  402,   60,   53,   61,  406,  357,  407,   53,  731,  279,
  737,  335,   53,   47,   53,   53,   53,   53,  401,  989,
   53,  309,   53,  401,  401,  350,   53,  228,  350,  350,
  655,  405,  323,  402,  324,  162,  402,   48,   53,  336,
  406,   53,  407,   53,  350,  111,  229, 1324,  350,   16,
   49,  331,  640,  332, 1314,   51, 1325,  106, 1147,  334,
   52,  640,   53,   54,   55,   56,  401,  341,  367,  308,
   57,  367,  401,  297,   58,  298,  342,  299, 1148,  342,
  345,  517,  641,  641,  287,  355,   59,  367,  642,   60,
  359,   61,  641,  287,  106,  342,  263,  807,  111,  300,
  868,  301,  259,  807,  807,  362,  625,  807,  807,  537,
  807,  807,  359,  901,  481,  538,  539,  310,  228,  405,
  481,  140,  540,  541,  402,  140, 1171,  140,  406,  140,
  407,  350,  350,  364,  350,  350,   60,  229,  366,  302,
 1209,   67,   67,  611, 1212,   67,  228,  612,  621, 1217,
 1218,  311,  612,  312,  440,  313, 1038,  314, 1053,  315,
  624,  316,  401,  317,  625,  318,  401,  319,  356,  320,
  357,  231,  106,  673, 1220,  106,  176,  625,  176,  458,
  176,  106,  859, 1226, 1227,  861,  174,  780,  174,  461,
  174,  625,   68, 1038,  870, 1168,   68,  478,  876,  401,
  217,  438,  401,  441,   72,  479,  689,  237,  106,  106,
  689, 1053,  690,  811,  350,  462,  690,  350, 1038,  811,
  811,  106,  157,  811,  811,  563,  811,  811,  523,  524,
  525,  526,  106,  473,  106,  805,  806, 1199,  294,  295,
  296,  356,  908,  369,  811,  952,  953,  405,  482,  483,
  792,  792,  402,  438,  492,  475,  406, 1038,  407,  471,
  295,  296,  558,  931,  559,  497,  505,  506,  507,  508,
  509,  510,  511,  512,  513,  514,  515,  922,  832,  923,
 1213,  356, 1149, 1216, 1275,  163, 1276,  163,  827,  828,
  401, 1160, 1164,  534,  170,  536,  170,  552,  171, 1167,
  171,  122,  775,  122,  775,  519,  520,  560,  564,  563,
  191, 1286,  191,  498,  504,  563,  563,  563,  563,  563,
  563,  563,  563,  563,  563,  563,  563,  164,  278,  164,
  619,  598,  619,  600,  563, 1311,  563, 1312,  563,  557,
  563,  563,  563,  560, 1278,  560,  527,  528,  576,  577,
  568,  569,  563,  563,  343, 1229,  814,  563,  563,  580,
  585,  596,  814,  814,  597,  599,  814,  814,  111,  814,
  814,   24,  110,  563,  605,  563,  606,  563,  610,  563,
  618, 1229, 1229,   47, 1315, 1315,  908,  814,  627, 1229,
  619, 1264, 1322,  623, 1323,  661,  663,  110,  110, 1229,
  672,  263,  677,  563,  678,  690,  697,   48, 1264,  711,
  438,  492,  348,  716,  564,  727, 1288, 1290,  401,  662,
   49,  353,  730, 1035,  740,   51,  612,  741,  744,  356,
   52,  576,   53,   54,   55,   56,  746,  756,  768,  769,
   57,  770,  771,  778,   58,  674,  674,  779,  781,  787,
  804, 1229,  795,  782,  788,  810,   59,  683,  808,   60,
  545,   61,  807,    2,  808,  808,  545,  804,  808,  808,
    3,  808,  808,  804,  804,  817,  820,  804,  804,  822,
  804,  804,  823,  824,  829,  830,  831,  837,  228,  833,
  832,  838,  856,  691,  842,  843,  841,  698,  851,  852,
  853, 1035,  866,  545, 1124,  854,  867,  229,  453,  879,
 1134,  878,  886,  714,  880,  576,  888,  893,  895,  911,
  921,  576,  576,  576,  576,  576,  576,  576,  576,  576,
  576,  576,  576,  881,  894,   24,  927, 1140, 1141,  930,
  576,  950,  576,  941,  576,  942,  576,  576,  576,  738,
 1159,  947,  691,  743,  948,  954,  965,  963,  968,  975,
  753, 1169,  691, 1170,  758,  961,  981,  983,  994,  985,
  217,  995,  969,  545,  997,  545,  834, 1002, 1005,  545,
  545,  762, 1006,  545, 1008,  545, 1010,  545,  545, 1012,
 1075, 1026,  545,  545,  564, 1138, 1014, 1015, 1025,  438,
  545, 1028,  545, 1030,  545, 1031,  545, 1080,  545, 1083,
  545, 1082,  545, 1088,  545, 1206, 1093, 1112, 1110,  789,
 1114, 1130, 1117,  790, 1135, 1157, 1155, 1196,   24, 1244,
 1195, 1210,   24,  492, 1224, 1233,  492,   24,  545,   24,
 1246, 1248,   24, 1250,   24,   24,  799,   24, 1257,   24,
 1259,   24, 1260,   24,   24,   24,   24,  808,  809,   24,
   24, 1262, 1273, 1274, 1277,   24, 1281,   24,   24,   24,
 1292, 1295,   24,   24,   24, 1293,   24, 1294, 1302,   24,
 1304,   24,   24,   24,   24, 1306, 1310, 1313,   24,   24,
   24, 1318, 1330,   24,   24,   24, 1332,    5,   28,   26,
   52,  844,   24,   24,   27,   24,   24,   24,   24,   24,
   24,   73,   22,  520,   24,  596,   74,  327,  496,   72,
  207,  655,   96,  597,   53,  751,   53,  447,  751,  683,
  497,  656,  437,  869,   71,  490,   24,   24,  564,  448,
  328,  721,  438,   26,  508,   24,  691,  724,   53,  509,
  452,  763,  492,  492,   27,  640,  885,  640,  887,  725,
  889,   53,  759,  890,  702,  727,   53,  703,  192,  729,
  312,   53,  316,   53,   53,   53,   53,  905,   44,    5,
   45,   53,   75,  125,  481,   53,  484,  738, 1089,  691,
 1165, 1136,   24,  919,  715,  926,   24,   53,  570, 1247,
   53,   24,   53,   24, 1317,  863,   24,  864,   24,  865,
  858,   24, 1094,   24, 1328,   24, 1279,   24, 1291,   24,
   24, 1003,  437,   24,   24,  470, 1289, 1004,  309,  500,
  676,   24,   24,   24,  774,  499,   24,   24,   24,  873,
   24,  503,  949,   24,  951,   24,   24,   24,   24,  955,
  689,  529,   24,   24,   24,  874,  530,   24,   24,   24,
  531,  671,  532,  971,  882,  535,   24,   24, 1118,   24,
   24,   24,   24,   24,   24, 1113,  584,  976,   24, 1078,
  691, 1076,  619,  982,  619, 1071,  850,  619,  978,  619,
  619,  999,  619, 1139,  619,  998,  619,    0,  619,  619,
   24,   24,  704,    0,    0,  619,  996,    0,    0,    0,
  890,    0,  619,  619,    0,    0,    0,  619,    0,    0,
    0,  619,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  619,    0,  619,    0,    0,    0,  619,
  619,    0,    0,    0,    0,    0,    0,  619,  619,  738,
    0,  619,    0, 1029,  619,    0,    0,   52,    0,  619,
    0,   54,    0,    0,    0,    0,   54,    0,   54,    0,
    0,   54,    0,   54,  738,    0,   54,    0,   54,    0,
   54,    0,   54,   24, 1079,   54,    0, 1081,   54,   54,
    0,    0,    0,    0,    0,    0,   54,   54,   54,    0,
    0,   54,   54,   54,    0,   54,    0,    0,   54,    0,
   54,   54,   54,   54,    0,    0,    0,   54,   54,   54,
    0,    0,   54,   54,   54,    0,    0,    0,    0,    0,
    0,   54,   54,    0,   54,   54, 1116,   54,   54,   54,
   53,    0,    0,   54,    0,   53,    0,   53,    0,    0,
   53,    0,   53,    0,    0,   53,    0,   53,    0,   53,
    0,   53,    0,    0,  619,    0,    0,   53,   53,    0,
    0,    0,    0,    0,    0,   53,   53,   53,    0,    0,
   53,   53,   53,    0,   53,    0,    0,   53,    0,   53,
   53,   53,   53,    0,    0,    0,   53,   53,   53,    0,
    0,   53,   53,   53,    0,    0,    0,    0,    0,    0,
   53,   53,    0,   53,   53,    0,   53,   53,   53,    0,
    0,    0,   53,    0,    0,    0,    0,  753,    0,    0,
    0,    0,    0,    0,    0,    0,  753,  753,  753,  753,
  753,    0,  753,  753,    0,  753,  753,  753,   54,  753,
  753,  753,  753,    0,    0,    0,    0,  753,    0,  753,
  753,  753,  753,  753,  753,    0,    0,  753,    0,    0,
    0,  753,  753,    0,  753,  753,  753,    0,    0,    0,
    0,    0,  492,  492,    0,    0,  753,    0,  753,    0,
  753,  753,    0,    0,  753,    0,  753,  753,  753,  753,
  753,  753,  753,  753,  753,  753,  753,  753,    0,  753,
    0,    0,  753,    0,    0,    0,    0,  753,    0,    0,
   83,    0,   84,    0,    0,   85,    0,   53,    0,    0,
   86,    0,    0,    0,   88,  753,  753,    0,    0,  753,
    0,    0,    0,   91,  753,  753,  753,  753,  753,    0,
   92,    0,    0,    0,  753,   93,  753,    0,    0,   94,
    0,  281,    0,  753,    0,  753,    0,    0,  282,    0,
    0,   95,    0,   96,    0,    0,    0,   97,    0,    0,
  283,    0,    0,    0,    0,   98,   99,    0,    0,  100,
    0,    0,  117,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  753,    0,  753,
    0,  753,    0,  753,    0,  753,    0,  753,    0,  753,
  753,  697,    0,    0,    0,  753,    0,  753,    0,    0,
  697,  697,  697,  697,  697,    0,  697,  697,    0,  697,
  697,  697,    0,  697,  697,  697,    0,    0,    0,    0,
    0,  697,    0,  697,  697,  697,  697,  697,  697,    0,
    0,  697,    0,    0,    0,  697,  697,    0,  697,  697,
  697,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  697,    0,  697,    0,  697,  697,    0,    0,  697,    0,
  697,  697,  697,  697,  697,  697,  697,  697,  697,  697,
  697,  697,  284,  697,    0,    0,  697,    0,    0,    0,
    0,  697,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   53,    0,   53,    0,    0,   53,    0,    0,  697,
  697,   53,    0,  697,    0,   53,    0,    0,  697,  697,
  697,  697,  697,    0,   53,    0,    0,    0,  697,    0,
  697,   53,    0,    0,    0,    0,   53,  697,    0,  697,
   53,    0,   53,    0,   53,    0,    0,    0,    0,   53,
    0,    0,   53,    0,   53,    0,    0,    0,   53,    0,
    0,   53,    0,    0,    0,    0,   53,   53,    0,    0,
   53,    0,    0,   53,    0,    0,    0,    0,    0,    0,
    0,  697,    0,  697,    0,  697,    0,  697,    0,  697,
    0,  697,    0,  697,  697,  375,    0,    0,    0,  697,
    0,  697,  158,    0,  127,   83,  376,   84,    0,    0,
   85,  377,    0,  378,  379,   86,    0,  129,  380,   88,
    0,    0,    0,    0,    0,  130,    0,  381,   91,  382,
  383,  384,  385,    0,    0,   92,    0,    0,    0,  386,
   93,    0,  131,  132,   94,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  387,    0,   95,    0,   96,  133,
    0,    0,   97,    0,  388,  134,  389,  135,  390,  136,
   98,   99,  391,  392,  100,  393,    0,  394,  375,    0,
  395,    0,    0,   53,    0,  139,    0,  127,   83,    0,
   84,    0,    0,   85,  128,    0,    0,    0,   86,    0,
  129,    0,   88,  111,    0,    0,    0,  140,  130,    0,
    0,   91,  396,  141,  142,  143,  144,    0,   92,    0,
 1172,    0,  145,   93,  146,  131,  132,   94,    0,    0,
    0,  147,    0,  148,    0,    0,    0,    0,    0,   95,
    0,   96,  133,    0,    0,   97,    0,    0,  134,    0,
  135,    0,  136,   98,   99,  137,    0,  100,    0,    0,
  394,    0, 1173,    0,    0,    0,    0,    0,  139,    0,
    0,    0,    0,    0,    0,  149,    0,  150,    0,  151,
    0,  152,    0,  153,    0,  154,    0,  397,  156,    0,
  140,    0,    0,  157,    0,    0,  141,  142,  143,  144,
    0,    0,    0,    0,    0,  145,    0,  146, 1174, 1175,
 1176, 1177,    0, 1178,  147, 1179,  148, 1180, 1181, 1182,
 1183, 1184, 1185,    0,    0,    0, 1186,    0, 1187,    0,
 1188,    0, 1189,    0, 1190,    0, 1191,    0, 1192,  375,
 1193,    0,    0,    0,    0,    0,    0,    0,  127,   83,
    0,   84,    0,    0,   85,  128,    0,    0,  149,   86,
  150,  129,  151,   88,  152,    0,  153,    0,  154,  130,
  262,  156,   91,    0,    0,    0,  157,    0,    0,   92,
    0,    0,    0,    0,   93,    0,  131,  132,   94,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   95,    0,   96,  133,    0,    0,   97,    0,    0,  134,
    0,  135,    0,  136,   98,   99,  137,    0,  100,    0,
    0,  138,  444,    0,    0,    0,    0,    0,    0,  139,
    0,  127,   83,    0,   84,    0,    0,   85,  128,    0,
    0,    0,   86,    0,  129,    0,   88,    0,    0,    0,
    0,  140,  130,    0,  817,   91,    0,  141,  142,  143,
  144,    0,   92,    0,    0,    0,  145,   93,  146,  131,
  132,   94,    0,    0,    0,  147,    0,  148,    0,    0,
    0,    0,    0,   95,    0,   96,  133,    0,    0,   97,
    0,    0,  134,    0,  135,    0,  136,   98,   99,  137,
    0,  100,    0,    0,  138,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  149,
    0,  150,    0,  151,    0,  152,    0,  153,    0,  154,
    0,  262,  156,    0,  445,  488,    0,  157,  817,    0,
  488,  488,    0,    0,  817,  817,  817,  817,  817,  817,
  817,  817,  817,  817,  817,    0,    0,    0,    0,    0,
    0,    0,    0,  817,  488,  817,    0,  817,    0,  817,
  817,  817,    0,    0,  488,    0,    0,  488,  488,    0,
    0,    0,  488,    0,    0,  488,    0,  488,    0,  488,
  488,  488,  488,    0,    0,    0,    0,  488,    0,    0,
    0,  488,  149,    0,  150,  488,  151,    0,  152,    0,
  153,    0,  154,  488,  446,    0,  488,    0,  488,  488,
  157,    0,    0,    0,    0,  488,  488,  488,  488,  488,
  488,  488,  488,  488,  488,  488,  488,    0,    0,    0,
    0,    0,    0,  488,  488,    0,  488,  488,  488,  488,
  488,  488,  488,    0,  488,  488,    0,  488,  488,    0,
  488,  488,  488,  488,  488,  488,  488,  488,  488,    0,
    0,  488,    0,  488,    0,  488,    0,  488,    0,  488,
    0,  488,    0,  488,    0,  488,    0,  488,    0,  488,
    0,  488,    0,  488,    0,  488,    0,  488,    0,  488,
    0,  488,    0,  488,    0,  488,    0,  488,    0,  488,
  350,  488,    0,  488,    0,  350,  350,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  488,  488,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  350,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  350,
    0,    0,  350,  350,    0,    0,    0,  350,    0,    0,
  350,    0,  350,    0,  350,  350,  350,  350,    0,    0,
    0,    0,  350,    0,    0,    0,  350,    0,    0,    0,
  350,    0,    0,    0,    0,    0,    0,    0,  350,    0,
    0,  350,    0,  350,  350,    0,    0,    0,    0,    0,
  350,  350,  350,  350,  350,  350,  350,  350,  350,  350,
  350,  350,    0,    0,    0,    0,    0,    0,  350,  350,
  350,  350,  350,  350,  350,  350,  350,  350,    0,    0,
    0,    0,    0,  350,    0,  350,  350,  350,  350,  350,
    0,    0,  350,  350,  350,    0,    0,    0,    0,  350,
  350,    0,    0,    0,  350,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,    0,    0,    0,    0,
    0,    0,    0,  350,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  350,    0,    0,  350,  350,  350,    0,
    0,  350,    0,    0,    0,    0,  350,    0,  350,  350,
  350,  350,  350,  350,    0,    0,  350,    0,    0,    0,
  350,    0,    0,    0,  350,    0,    0,    0,    0,    0,
    0,    0,  350,    0,    0,  350,    0,  350,  350,    0,
    0,    0,    0,    0,  350,  350,  350,  350,  350,  350,
  350,  350,  350,  350,  350,  350,    0,    0,    0,    0,
    0,    0,  350,  350,  350,  350,  350,  350,  350,  350,
  350,  350,    0,    0,    0,    0,    0,  350,    0,  350,
  350,  350,  350,  350,    0,    0,  350,  350,  342,    0,
    0,    0,    0,  342,  342,    0,    0,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
    0,    0,    0,    0,    0,    0,    0,  342,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  342,    0,    0,
  342,  342,  350,    0,    0,  342,    0,    0,  342,    0,
  342,    0,  342,  342,  342,  342,  350,  350,    0,    0,
  342,    0,    0,    0,  342,    0,    0,    0,  342,    0,
    0,    0,    0,    0,    0,    0,  342,    0,    0,  342,
    0,  342,  342,    0,    0,    0,    0,    0,  342,  342,
  342,  342,  342,  342,  342,  342,  342,  342,  342,  342,
    0,    0,    0,    0,    0,    0,  342,  342,  342,  342,
  342,  342,  387,  342,  342,  342,   53,    0,  387,    0,
    0,  342,    0,  342,  342,  342,  342,  342,    0,    0,
  342,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   53,    0,  342,    0,  342,    0,  342,    0,  342,    0,
  342,  387,  342,   53,    0,  387,    0,    0,   53,    0,
    0,    0,    0,   53,    0,   53,   53,   53,   53,    0,
    0,    0,    0,   53,    0,    0,  342,   53,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,   53,
  342,  342,   53,    0,   53,    0,  387,    0,    0,    0,
    0,    0,  387,  387,  387,  387,  387,  387,  387,  387,
  387,  387,  387,  387,   53,    0,   53,    0,    0,    0,
  387,  387,  387,  387,  387,  387,  352,  387,  387,  387,
   53,    0,  352,    0,    0,  387,    0,  387,  387,  387,
  387,    0,    0,    0,  387,  387,    0,    0,    0,    0,
    0,    0,    0,    0,   53,    0,  387,    0,  387,    0,
  387,    0,  387,    0,  387,    0,  387,   53,    0,  352,
    0,    0,   53,    0,    0,    0,    0,   53,    0,   53,
   53,   53,   53,    0,    0,    0,    0,   53,    0,    0,
  387,   53,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,   53,  387,  387,   53,    0,   53,    0,
  352,    0,    0,    0,    0,    0,  352,  352,  352,  352,
  352,  352,  352,  352,  352,  352,  352,  352,   53,    0,
   53,    0,    0,    0,  196,  352,  352,  352,  352,  352,
  352,  352,  352,  352,  422,  352,  352,    0,  352,  352,
  422,  352,    0,  352,  352,  352,  352,  352,  352,  352,
    0,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,  422,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  352,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  422,    0,
    0,    0,    0,    0,  422,  422,  422,  422,  422,  422,
  422,  422,  422,  422,  422,  422,    0,    0,    0,    0,
    0,    0,    0,  422,  422,  422,  422,  422,  422,  422,
  422,  422,  350,  422,  422,    0,  422,  422,  350,  422,
    0,  422,  422,  422,  422,  422,  422,  422,    0,    0,
  422,    0,  422,    0,  422,    0,  422,    0,  422,    0,
  422,    0,  422,    0,  422,    0,  422,    0,  422,    0,
  422,    0,  422,    0,  422,  350,  422,    0,  422,    0,
  422,    0,  422,    0,  422,    0,  422,    0,  422,    0,
  422,    0,  422,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  422,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  350,    0,    0,    0,
    0,    0,  350,  350,  350,  350,  350,  350,  350,  350,
  350,  350,  350,  350,    0,    0,    0,    0,    0,    0,
    0,  350,  350,  350,  350,  350,  350,  350,  350,  350,
  488,  350,  350,    0,  350,  350,  488,  350,    0,  350,
  350,  350,  350,  350,  350,  350,    0,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,  350,  488,  350,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  350,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  488,    0,    0,    0,    0,    0,
  488,  488,  488,  488,  488,  488,  488,  488,  488,  488,
  488,  488,    0,    0,  433,    0,    0,    0,  488,  488,
  433,  488,  488,  488,  488,  488,  488,  488,    0,  488,
  488,    0,  488,  488,    0,  488,    0,  488,  488,  488,
  488,  488,  488,  488,    0,    0,  488,    0,  488,    0,
  488,    0,  488,    0,  488,    0,  488,  433,  488,    0,
  488,    0,  488,    0,  488,    0,  488,    0,  488,    0,
  488,    0,  488,    0,  488,    0,  488,    0,  488,    0,
  488,    0,  488,    0,  488,    0,  488,    0,  488,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  433,    0,
    0,    0,  488,    0,  433,  433,  433,  433,  433,  433,
  433,  433,  433,  433,  433,  433,    0,    0,    0,    0,
    0,    0,    0,  433,  433,  433,  433,  433,  433,  433,
  433,  433,  500,  433,  433,    0,  433,  433,  500,  433,
    0,  433,  433,  433,  433,  433,  433,  433,    0,    0,
  433,    0,  433,    0,  433,    0,  433,    0,  433,    0,
  433,    0,  433,    0,  433,    0,  433,    0,  433,    0,
  433,    0,  433,    0,  433,  500,  433,    0,  433,    0,
  433,    0,  433,    0,  433,    0,  433,    0,  433,    0,
  433,    0,  433,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  433,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  500,    0,    0,    0,
    0,    0,  500,  500,  500,  500,  500,  500,  500,  500,
  500,  500,  500,  500,    0,    0,    0,    0,    0,    0,
    0,  500,  500,  500,  500,  500,  500,  500,  500,  500,
  430,  500,  500,    0,  500,  500,  430,  500,    0,  500,
  500,  500,  500,  500,  500,  500,    0,    0,  500,    0,
  500,    0,  500,    0,  500,    0,  500,    0,  500,    0,
  500,    0,  500,    0,  500,    0,  500,    0,  500,    0,
  500,    0,  500,  430,  500,    0,  500,    0,  500,    0,
  500,    0,  500,    0,  500,    0,  500,    0,  500,    0,
  500,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  500,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  430,    0,    0,    0,    0,    0,
  430,  430,  430,  430,  430,  430,  430,  430,  430,  430,
  430,  430,    0,    0,    0,    0,    0,    0,    0,  430,
    0,  430,    0,  430,    0,  430,  430,  430,    0,  430,
  430,    0,  430,  430,    0,  430,    0,  430,  430,  430,
  430,  430,  430,  430,    0,    0,    0,    0,    0,    0,
  430,    0,  430,    0,  430,    0,  430,    0,  430,    0,
  430,    0,  430,    0,  430,    0,  430,    0,  430,    0,
  430,    0,  430,    0,  430,    0,  430,    0,  430,    0,
  430,    0,  430,    0,  430,   37,    0,    0,  430,   37,
    0,    0,    0,    0,   37,    0,   37,    0,    0,   37,
    0,   37,  430,    0,   37,    0,   37,    0,   37,    0,
   37,    0,    0,    0,    0,    0,   37,   37,    0,    0,
    0,    0,    0,    0,   37,   37,   37,    0,    0,   37,
   37,   37,    0,   37,    0,    0,   37,    0,   37,   37,
   37,   37,    0,    0,    0,   37,   37,   37,    0,    0,
   37,   37,   37,    0,    0,    0,    0,    0,    0,   37,
   37,    0,   37,   37,   37,   37,   37,   37,    0,    0,
    0,   37,    0,    0,    0,    0,   38,    0,    0,    0,
   38,    0,    0,    0,    0,   38,    0,   38,    0,    0,
   38,    0,   38,   37,   37,   38,    0,   38,    0,   38,
    0,   38,    0,    0,    0,    0,    0,   38,   38,    0,
    0,    0,    0,    0,    0,   38,   38,   38,    0,    0,
   38,   38,   38,    0,   38,    0,    0,   38,    0,   38,
   38,   38,   38,    0,    0,    0,   38,   38,   38,    0,
    0,   38,   38,   38,    0,    0,    0,    0,    0,    0,
   38,   38,    0,   38,   38,   38,   38,   38,   38,    0,
    0,    0,   38,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,   37,   35,    0,    0,
    0,   35,    0,    0,   38,   38,   35,    0,   35,    0,
    0,   35,    0,   35,    0,    0,   35,    0,   35,    0,
   35,    0,   35,    0,    0,   35,    0,    0,   35,   35,
    0,    0,    0,    0,    0,    0,   35,   35,   35,    0,
    0,   35,   35,   35,    0,   35,    0,    0,   35,    0,
   35,   35,   35,   35,    0,    0,    0,   35,   35,   35,
    0,    0,   35,   35,   35,    0,    0,    0,    0,    0,
    0,   35,   35,    0,   35,   35,    0,   35,   35,   35,
    0,    0,    0,   35,    0,    0,    0,   38,   36,    0,
    0,    0,   36,    0,    0,    0,    0,   36,    0,   36,
    0,    0,   36,    0,   36,   35,   35,   36,    0,   36,
    0,   36,    0,   36,    0,    0,   36,    0,    0,   36,
   36,    0,    0,    0,    0,    0,    0,   36,   36,   36,
    0,    0,   36,   36,   36,    0,   36,    0,    0,   36,
    0,   36,   36,   36,   36,    0,    0,    0,   36,   36,
   36,    0,    0,   36,   36,   36,    0,    0,    0,    0,
    0,    0,   36,   36,    0,   36,   36,    0,   36,   36,
   36,    0,    0,    0,   36,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,   35,   39,
    0,    0,    0,   53,    0,    0,   36,   36,   53,    0,
   53,    0,    0,   53,    0,   53,    0,    0,   53,    0,
   53,    0,   53,    0,   53,    0,    0,   53,    0,    0,
   53,   53,    0,    0,    0,    0,    0,    0,   53,   53,
   53,    0,    0,   53,   53,   53,    0,   53,    0,    0,
   53,    0,   53,   53,   53,   53,    0,    0,    0,   53,
   53,   53,    0,    0,   53,   53,   53,    0,    0,    0,
    0,    0,    0,   53,   53,    0,   53,   53,    0,   53,
   53,   53,    0,   40,    0,   53,    0,   53,    0,   36,
    0,    0,   53,    0,   53,    0,    0,   53,    0,   53,
    0,    0,   53,    0,   53,    0,   53,   39,   53,    0,
    0,   53,    0,    0,   53,   53,    0,    0,    0,    0,
    0,    0,   53,   53,   53,    0,    0,   53,   53,   53,
    0,   53,    0,    0,   53,    0,   53,   53,   53,   53,
    0,    0,    0,   53,   53,   53,    0,    0,   53,   53,
   53,    0,    0,    0,    0,    0,    0,   53,   53,    0,
   53,   53,    0,   53,   53,   53,    0,    0,    0,   53,
    0,    0,  706,  706,  706,  706,    0,    0,  706,  706,
    0,  706,  706,  706,    0,  706,  706,  706,    0,    0,
   53,   40,    0,  706,    0,  706,  706,  706,  706,  706,
  706,    0,    0,  706,    0,    0,    0,  706,  706,    0,
  706,  706,  706,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  706,    0,  706,    0,  706,  706,    0,    0,
  706,    0,  706,  706,  706,  706,  706,  706,  706,  706,
  706,  706,  706,  706,    0,  706,    0,    0,  706,    0,
    0,    0,    0,  706,    0,    0,  228,    0,  228,    0,
    0,  228,    0,  617,    0,    0,  228,    0,    0,    0,
  228,  706,    0,  228,   53,  706,    0,    0,    0,  228,
  706,  706,  706,  706,  706,    0,  228,  617,    0,    0,
  706,  228,  706,    0,    0,  228,    0,    0,    0,  706,
    0,  706,    0,    0,    0,    0,    0,  228,    0,  228,
    0,    0,    0,  228,  617,    0,    0,    0,    0,    0,
    0,  228,  228,    0,    0,  228,    0,    0,  228,    0,
    0,    0,    0,  127,   83,    0,   84,    0,    0,   85,
  128,    0,    0,  706,   86,  706,  129,  706,   88,  706,
    0,  706,    0,  706,  130,  706,  706,   91,    0,    0,
    0,  706,    0,    0,   92,    0,    0,    0,    0,   93,
    0,  131,  132,   94,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   95,    0,   96,  133,  562,
    0,   97,    0,    0,  134,    0,  135,    0,  136,   98,
   99,  137,    0,  100,    0,    0,  138,    0,    0,    0,
  563,    0,    0,    0,  139,    0,  127,   83,    0,   84,
    0,    0,   85,  128,    0,    0,    0,   86,    0,  129,
    0,   88,  458,  652,    0,    0,  140,  130,  228,    0,
   91,    0,  141,  142,  143,  144,    0,   92,    0,    0,
    0,  145,   93,  146,  131,  132,   94,    0,  489,    0,
  147,    0,  148,    0,    0,  490,    0,    0,   95,    0,
   96,  133,    0,    0,   97,    0,    0,  134,    0,  135,
    0,  136,   98,   99,  137,    0,  100,    0,    0,  138,
    0,    0,    0,  491,    0,    0,    0,  139,    0,    0,
    0,    0,    0,    0,  149,    0,  150,    0,  151,    0,
  152,    0,  153,    0,  154,    0,  262,  156,    0,  140,
  682,    0,  157,    0,    0,  141,  142,  143,  144,    0,
    0,    0,    0,    0,  145,    0,  146,    0,    0,    0,
    0,    0,    0,  147,    0,  148,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  127,   83,    0,
   84,    0,    0,   85,  128,    0,    0,  149,   86,  150,
  129,  151,   88,  152,    0,  153,    0,  154,  130,  262,
  156,   91,    0,    0,    0,  157,    0,    0,   92,    0,
    0,    0,    0,   93,    0,  131,  132,   94,    0,  489,
    0,    0,    0,    0,    0,    0,  490,    0,    0,   95,
    0,   96,  133,    0,    0,   97,    0,    0,  134,    0,
  135,    0,  136,   98,   99,  137,    0,  100,    0,    0,
  138,    0,    0,    0,  491,    0,    0,    0,  139,    0,
    0,   83,    0,   84,    0,    0,   85,    0, 1032,    0,
    0,   86,    0,    0,    0,   88,    0,    0,    0,    0,
  140,  792,    0,    0,   91,    0,  141,  142,  143,  144,
    0,   92,    0,    0,    0,  145,   93,  146, 1033,    0,
   94,    0,    0,    0,  147,    0,  148,    0,    0,    0,
    0,    0,   95,    0,   96,    0,    0,    0,   97, 1034,
    0,    0,    0,    0,    0,    0,   98,   99,    0,    0,
  100,    0,    0,  117,    0,    0,    0,    0,  127,   83,
    0,   84,    0,    0,   85,  128,    0,    0,  149,   86,
  150,  129,  151,   88,  152,    0,  153,    0,  154,  130,
  262,  156,   91,    0,    0,    0,  157,    0,    0,   92,
    0,    0,    0,    0,   93,    0,  131,  132,   94,    0,
  489,    0,    0,    0,    0,    0,    0,  490,    0,    0,
   95,    0,   96,  133,    0,    0,   97,    0,    0,  134,
    0,  135,    0,  136,   98,   99,  137,    0,  100,    0,
    0,  138,    0,    0,    0,  491,    0,    0,    0,  139,
    0,  127,   83,    0,   84,    0,    0,   85,  128,    0,
    0,    0,   86,    0,  129,    0,   88,    0,    0,    0,
    0,  140,  130,   74,    0,   91,    0,  141,  142,  143,
  144,    0,   92,    0,    0,    0,  145,   93,  146,  131,
  132,   94,    0,    0,    0,  147,    0,  148,    0,    0,
    0,    0,    0,   95,    0,   96,  133,  562,    0,   97,
    0,    0,  134,    0,  135,    0,  136,   98,   99,  137,
    0,  100,    0,    0,  138,    0,    0,    0,  563,    0,
    0,    0,  139,    0,    0,    0,    0,    0,    0,  149,
    0,  150,    0,  151,    0,  152,    0,  153,    0,  154,
  458,  262,  156,    0,  140,    0,    0,  157,    0,    0,
  141,  142,  143,  144,    0,    0,    0,    0,    0,  145,
    0,  146,    0,    0,    0,    0,    0,    0,  147,    0,
  148,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  127,   83,    0,   84,    0,    0,   85,  128,
    0,    0,  149,   86,  150,  129,  151,   88,  152,    0,
  153,    0,  154,  130,  262,  156,   91,    0,    0,    0,
  157,    0,    0,   92,    0,    0,    0,    0,   93,    0,
  131,  132,   94,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   95,    0,   96,  133,    0,    0,
   97,    0,    0,  134,    0,  135,    0,  136,   98,   99,
  137,    0,  100,    0,    0,  138,    0,    0,    0,    0,
    0,    0,    0,  139,    0,    0,   83,    0,   84,    0,
    0,   85,    0,    0,    0,    0,   86,    0,    0,    0,
   88,  643,  872, 1132,    0,  140,    0,    0,    0,   91,
    0,  141,  142,  143,  144,    0,   92,    0,    0,    0,
  145,   93,  146,    0,    0,   94,    0,    0,    0,  147,
    0,  148,    0,    0,    0,    0,    0,   95,    0,   96,
    0,    0,    0,   97,    0,    0,    0,    0,    0,    0,
    0,   98,   99,    0,    0,  100,    0,    0, 1133,    0,
    0,    0,    0,  127,   83,    0,   84,    0,    0,   85,
  128,    0,    0,  149,   86,  150,  129,  151,   88,  152,
    0,  153,    0,  154,  130,  644,  156,   91,    0,    0,
    0,  157,    0,    0,   92,    0,    0,    0,    0,   93,
    0,  131,  132,   94,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   95,    0,   96,  133,    0,
    0,   97,    0,    0,  134,    0,  135,    0,  136,   98,
   99,  137,    0,  100,    0,    0,  138,    0,    0,    0,
    0,    0,    0,    0,  139,    0,  127,   83,    0,   84,
    0,    0,   85,  128,    0,    0,    0,   86,    0,  129,
    0,   88,    0,    0,    0,    0,  140,  130,   74,  366,
   91,    0,  141,  142,  143,  144,    0,   92,    0,    0,
    0,  145,   93,  146,  131,  132,   94,    0,    0,    0,
  147,    0,  148,    0,    0,    0,    0,    0,   95,    0,
   96,  133,    0,    0,   97,    0,    0,  134,    0,  135,
    0,  136,   98,   99,  137,    0,  100,    0,    0,  138,
    0,    0,    0,    0,    0,    0,    0,  139,    0,    0,
    0,    0,    0,    0,  149,    0,  150,    0,  151,    0,
  152,    0,  153,    0,  154,    0,  262,  156,    0,  140,
  533,    0,  157,    0,    0,  141,  142,  143,  144,    0,
    0,    0,    0,    0,  145,    0,  146,    0,    0,    0,
    0,    0,    0,  147,    0,  148,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  127,   83,    0,
   84,    0,    0,   85,  128,    0,    0,  149,   86,  150,
  129,  151,   88,  152,    0,  153,    0,  154,  130,  262,
  156,   91,    0,    0,    0,  157,    0,    0,   92,    0,
    0,    0,    0,   93,    0,  131,  132,   94,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,   95,
    0,   96,  133,    0,    0,   97,    0,    0,  134,    0,
  135,    0,  136,   98,   99,  137,    0,  100,    0,    0,
  138,    0,    0,    0,    0,    0,    0,    0,  139,    0,
  127,   83,    0,   84,    0,    0,   85,  128,    0,    0,
    0,   86,    0,  129,    0,   88,  643,    0,    0,    0,
  140,  130,    0,    0,   91,    0,  141,  142,  143,  144,
    0,   92,    0,    0,    0,  145,   93,  146,  131,  132,
   94,    0,    0,    0,  147,    0,  148,    0,    0,    0,
    0,    0,   95,    0,   96,  133,    0,    0,   97,    0,
    0,  134,    0,  135,    0,  136,   98,   99,  137,    0,
  100,    0,    0,  138,    0,    0,    0,    0,    0,    0,
    0,  139,    0,    0,    0,    0,    0,    0,  149,    0,
  150,    0,  151,    0,  152,    0,  153,    0,  154,    0,
  644,  156,  713,  140,    0,    0,  157,    0,    0,  141,
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
    0,    0,    0,    0,  140,    0,    0,    0,   91,  757,
  141,  142,  143,  144,    0,   92,    0,    0,    0,  145,
   93,  146,    0,    0,   94,    0,    0,    0,  147,    0,
  148,    0,    0,    0,    0,    0,   95,    0,   96,    0,
    0,    0,   97,    0,    0,    0,    0,    0,    0,    0,
   98,   99,    0,    0,  100,    0,    0,  117,    0,    0,
    0,    0,  127,   83,    0,   84,    0,    0,   85,  128,
    0,    0,  149,   86,  150,  129,  151,   88,  152,    0,
  153,    0,  154,  130,  262,  156,   91,    0,    0,    0,
  157,    0,    0,   92,    0,    0,    0,    0,   93,    0,
  131,  132,   94,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   95,    0,   96,  133,    0,    0,
   97,    0,    0,  134,    0,  135,    0,  136,   98,   99,
  137,    0,  100,    0,    0,  138,    0,    0,    0,    0,
    0,    0,    0,  139,    0,  127,   83,    0,   84,    0,
    0,   85,  128,    0,    0,    0,   86,    0,  129,    0,
   88,    0,  775,    0,    0,  140,  130,   74,    0,   91,
    0,  141,  142,  143,  144,    0,   92,    0,    0,    0,
  145,   93,  146,  131,  132,   94,    0,    0,    0,  147,
    0,  148,    0,    0,    0,    0,    0,   95,    0,   96,
  133,    0,    0,   97,    0,    0,  134,    0,  135,    0,
  136,   98,   99,  137,    0,  100,    0,    0,  138,    0,
    0,    0,    0,    0,    0,    0,  139,    0,    0,    0,
    0,    0,    0,  149,    0,  150,    0,  151,    0,  152,
    0,  153,    0,  154,  453,  262,  156,    0,  140,    0,
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
   86,    0,  129,    0,   88,    0,    0,    0,    0,  140,
  130,    0,    0,   91,    0,  141,  142,  143,  144,    0,
   92,    0,    0,    0,  145,   93,  146,  131,  132,   94,
    0,    0,    0,  147,    0,  148,    0,    0,    0,    0,
    0,   95,    0,   96,  133,    0,    0,   97,    0,    0,
  134,    0,  135,    0,  136,   98,   99,  137,    0,  100,
    0,    0,  138,    0,    0,    0,    0,    0,    0,    0,
  139,    0,    0,    0,    0,    0,    0,  149,    0,  150,
    0,  151,    0,  152,    0,  153,    0,  154,    0,  155,
  156,    0,  140,    0,    0,  157,    0,    0,  141,  142,
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
  100,    0,    0,  394,    0,    0,    0,    0,    0,    0,
    0,  139,    0,  598,  598,    0,  598,    0,    0,  598,
  598,    0,    0,    0,  598,    0,  598,    0,  598,    0,
    0,    0,    0,  140,  598,    0,    0,  598,    0,  141,
  142,  143,  144,    0,  598,    0,    0,    0,  145,  598,
  146,  598,  598,  598,    0,    0,    0,  147,    0,  148,
    0,    0,    0,    0,    0,  598,    0,  598,  598,    0,
    0,  598,    0,    0,  598,    0,  598,    0,  598,  598,
  598,  598,    0,  598,    0,    0,  598,    0,    0,    0,
    0,    0,    0,    0,  598,    0,    0,    0,    0,    0,
    0,  149,    0,  150,    0,  151,    0,  152,    0,  153,
    0,  154,    0,  262,  156,    0,  598,    0,    0,  157,
    0,    0,  598,  598,  598,  598,    0,    0,    0,    0,
    0,  598,    0,  598,    0,    0,    0,    0,    0,    0,
  598,    0,  598,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  127,   83,    0,   84,    0,    0,
   85,  128,    0,    0,  598,   86,  598,  129,  598,   88,
  598,    0,  598,    0,  598,  130,  598,  598,   91,    0,
    0,    0,  598,    0,    0,   92,    0,    0,    0,    0,
   93,    0,  131,  132,   94,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,   95,    0,   96,  133,
    0,    0,   97,    0,    0,  134,    0,  135,    0,  136,
   98,   99,  137,    0,  100,    0,    0,  138,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  127,   83,    0,
   84,    0,    0,   85,  128,    0,    0,    0,   86,    0,
  129,    0,   88,    0,    0,    0,    0,  140,  130,    0,
    0,   91,    0,  141,  142,  143,  144,    0,   92,    0,
    0,    0,  145,   93,  146,  131,  132,   94,    0,    0,
    0,  147,    0,  148,    0,    0,    0,    0,    0,   95,
    0,   96,  133,    0,    0,   97,    0,    0,  134,    0,
  135,    0,  136,   98,   99,  137,    0,  100,    0,    0,
  138,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  149,    0,  150,    0,  151,
    0,  152,    0,  153,    0,  154,    0,  267,    0,    0,
  140,    0,    0,  157,    0,    0,  141,  142,  143,  144,
    0,    0,    0,    0,    0,  145,    0,    0,    0,    0,
    0,    0,    0,    0,  147,    0,  148,  127,   83,    0,
   84,    0,    0,   85,  128,    0,    0,    0,   86,    0,
  129,    0,   88,    0,    0,    0,    0,    0,  130,    0,
    0,   91,    0,    0,    0,    0,    0,    0,   92,    0,
    0,    0,    0,   93,    0,  131,  132,   94,  149,    0,
  150,    0,  151,    0,  152,    0,  153,    0,  154,   95,
  267,   96,  133,    0,    0,   97,  157,    0,  134,    0,
  135,    0,  136,   98,   99,  137,    0,  100,    0,    0,
  138,    0,  127,   83,    0,   84,    0,    0,   85,  128,
    0,    0,    0,   86,    0,  129,    0,   88,    0,    0,
    0,    0,    0,  130,    0,    0,   91,    0,    0,    0,
  140,    0,    0,   92,    0,    0,  141,    0,   93,  144,
  131,  132,   94,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   95,    0,   96,  133,    0,    0,
   97,    0,    0,  134,    0,  135,    0,  136,   98,   99,
  137,    0,  100,    0,    0,  138,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  149,    0,
  150,    0,  151,    0,  152,  501,  153,    0,  154,    0,
  267,    0,    0,    0,    0,    0,  157,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  127,   83,    0,   84,    0,    0,   85,  128,
    0,    0,    0,   86,    0,  129,    0,   88,    0,    0,
    0,    0,    0,  130,    0,    0,   91,    0,    0,    0,
    0,    0,    0,   92,    0,    0,    0,    0,   93,    0,
  131,  132,   94,  149,  525,  150,    0,  151,    0,  152,
    0,  153,    0,  154,   95,  267,   96,  133,    0,    0,
   97,  157,    0,  134,    0,  135,    0,  136,   98,   99,
  137,    0,  100,  127,   83,  138,   84,    0,    0,   85,
  128,  525,    0,    0,   86,    0,  129,    0,   88,    0,
    0,    0,    0,    0,  130,    0,    0,   91,    0,    0,
    0,    0,    0,    0,   92,  445,    0,    0,    0,   93,
    0,  131,  132,   94,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   95,    0,   96,  133,    0,
    0,   97,    0,    0,  134,    0,  135,    0,  136,   98,
   99,  137,    0,  100,    0,    0,  138,    0,    0,    0,
    0,  525,    0,  525,    0,  525,    0,  525,  525,    0,
  525,  525,    0,  525,    0,  525,  525,    0,  525,  525,
  525,    0,    0,  149,    0,  150,  773,  151,  525,  152,
  525,  153,  525,  154,  525,  267,  525,    0,  525,    0,
  525,  157,  525,    0,  525,    0,  525,    0,  525,    0,
  525,    0,  525,    0,  525,    0,  525,    0,  525,    0,
  525,    0,  525,    0,    0,    0,  525,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  490,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  149,    0,  150,    0,  151,    0,
  152,    0,  153,   54,  154,   54,  267,    0,   54,    0,
   54,    0,  157,   54,    0,   54,   54,    0,   54,    0,
   54,    0,   54,    0,   54,   54,   54,   54,    0,    0,
   54,   54,    0,    0,    0,    0,   54,    0,   54,   54,
   54,    0,    0,   54,   54,   54,    0,   54,    0,   54,
   54,   54,   54,   54,   54,   54,   54,    0,   54,   54,
   54,   54,    0,    0,   54,   54,   54,    0,   54,    0,
    0,    0,    0,   54,   54,    0,   54,   54,    0,   54,
   54,   54,    0,    0,    0,   54,    0,   53,    0,    0,
    0,    0,   53,    0,   53,    0,    0,   53,    0,   53,
   53,   54,   53,   54,   53,    0,   53,    0,   53,   53,
   53,   53,    0,    0,   53,   53,   54,    0,    0,    0,
   53,    0,   53,   53,   53,    0,    0,   53,    0,   53,
    0,   53,    0,    0,   53,    0,   53,   53,   53,   53,
    0,    0,    0,   53,   53,   53,    0,    0,   53,   53,
   53,    0,    0,    0,    0,    0,    0,   53,   53,    0,
   53,   53,    0,   53,   53,   53,    0,    0,    0,   53,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   53,    0,    0,    0,    0,   53,    0,   53,
   54,   82,   53,    0,   53,   53,    0,   53,    0,   53,
   53,   53,    0,   53,   53,   53,   53,    0,    0,   53,
   53,    0,    0,    0,    0,   53,    0,   53,   53,   53,
    0,    0,   53,    0,   53,    0,   53,    0,    0,   53,
    0,   53,   53,   53,   53,    0,    0,    0,   53,   53,
   53,    0,    0,   53,   53,   53,    0,    0,    0,    0,
    0,    0,   53,   53,    0,   53,   53,    0,   53,   53,
   53,    0,    0,    0,   53,    0,   53,    0,    0,    0,
    0,   53,    0,   53,   53,    0,   53,    0,   53,   53,
    0,   53,    0,   53,    0,   53,   83,   53,   53,   53,
   53,    0,    0,   53,   53,   53,    0,    0,    0,   53,
    0,   53,   53,   53,    0,    0,   53,    0,   53,    0,
   53,    0,    0,   53,    0,   53,   53,   53,   53,    0,
    0,    0,   53,   53,   53,    0,    0,   53,   53,   53,
    0,    0,    0,    0,    0,    0,   53,   53,    0,   53,
   53,    0,   53,   53,   53,    0,    0,    0,   53,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   53,    0,    0,    0,    0,   53,    0,   53,   53,
  104,   53,    0,   53,   53,    0,   53,    0,   53,   53,
   53,    0,   53,   53,   53,   53,    0,    0,   53,   53,
    0,    0,    0,    0,   53,    0,   53,   53,   53,    0,
    0,   53,    0,   53,    0,   53,    0,    0,   53,    0,
   53,   53,   53,   53,    0,    0,    0,   53,   53,   53,
    0,    0,   53,   53,   53,    0,    0,    0,    0,    0,
    0,   53,   53,    0,   53,   53,    0,   53,   53,   53,
    0,    0,    0,   53,    0,  634,    0,    0,    0,    0,
  634,    0,  634,   53,    0,  634,    0,  634,  634,    0,
  634,    0,  634,    0,  634,  105,  634,  634,  634,  634,
    0,    0,  634,  634,   53,    0,    0,    0,  634,    0,
  634,  634,  634,    0,    0,  634,    0,  634,    0,  634,
    0,    0,  634,    0,  634,  634,  634,  634,    0,    0,
    0,  634,  634,  634,    0,    0,  634,  634,  634,    0,
    0,    0,    0,    0,    0,  634,  634,    0,  634,  634,
    0,  634,  634,  634,    0,    0,    0,  634,  636,    0,
    0,    0,    0,  636,    0,  636,    0,    0,  636,    0,
  636,  636,    0,  636,    0,  636,    0,  636,   53,  636,
  636,  636,  636,    0,    0,  636,  636,    0,  298,    0,
    0,  636,    0,  636,  636,  636,    0,    0,  636,    0,
  636,    0,  636,    0,    0,  636,    0,  636,  636,  636,
  636,    0,    0,    0,  636,  636,  636,    0,    0,  636,
  636,  636,    0,    0,    0,    0,    0,    0,  636,  636,
    0,  636,  636,    0,  636,  636,  636,   53,    0,    0,
  636,    0,   53,    0,   53,    0,    0,   53,    0,   53,
   53,    0,   53,    0,   53,    0,   53,    0,   53,   53,
    0,   53,  634,    0,    0,   53,    0,    0,    0,    0,
    0,  297,   53,   53,   53,    0,    0,   53,    0,   53,
    0,   53,    0,    0,   53,    0,   53,   53,   53,   53,
    0,    0,    0,   53,   53,   53,    0,    0,   53,   53,
   53,    0,    0,    0,    0,    0,    0,   53,   53,    0,
   53,   53,    0,   53,   53,   53,   53,    0,    0,   53,
    0,   53,  352,   53,    0,    0,   53,    0,   53,   53,
    0,   53,    0,   53,    0,   53,    0,   53,   53,    0,
   53,  214,    0,    0,   53,  636,    0,    0,    0,    0,
    0,   53,   53,   53,    0,    0,   53,    0,   53,  352,
   53,    0,    0,   53,   47,   53,   53,   53,   53,    0,
    0,    0,   53,   53,   53,    0,    0,   53,   53,   53,
    0,    0,    0,    0,    0,    0,   53,   53,   48,   53,
   53,    0,   53,   53,   53,    0,    0,    0,   53,    0,
    0,   49,    0,    0,    0,   50,   51,    0,    0,    0,
    0,   52,    0,   53,   54,   55,   56,    0,    0,    0,
  215,   57,    0,    0,   53,   58,  352,    0,  352,    0,
  352,    0,    0,  352,    0,  352,  352,   59,  352,  352,
   60,  352,   61,  352,  352,  352,  352,  352,  352,  352,
    0,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,    0,  620,    0,  620,
    0,    0,  620,   53,  620,  620,    0,  620,  352,  620,
    0,  620,    0,  620,  620,  620,    0,    0,    0,  620,
  620,    0,    0,    0,    0,  620,    0,  620,  620,    0,
    0,    0,  620,    0,    0,    0,  620,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  620,    0,
  620,    0,    0,    0,  620,  620,    0,    0,    0,    0,
    0,    0,  620,  620,    0,  619,  620,  619,    0,  620,
  619,    0,  619,  619,  620,  619,    0,  619,    0,  619,
    0,  619,  619,  619,    0,    0,    0,  619,  619,    0,
  620,    0,  620,  619,    0,  619,  619,   83,    0,   84,
  619,    0,   85,    0,  619, 1119,    0,   86,    0,   87,
    0,   88,    0,   89, 1120, 1121,  619,    0,  619,   90,
   91,    0,  619,  619,    0, 1122,    0,   92,    0,    0,
  619,  619,   93,    0,  619,    0,   94,  619,    0,    0,
    0,    0,  619,    0,    0,    0,    0,    0,   95,    0,
   96,    0,    0,    0,   97,    0,    0,    0,    0,    0,
    0,    0,   98,   99,    0,    0,  100,    0,    0,  101,
    0,    0,    0,  296,  102,    0,  619,    0,  619,  620,
    0,  619,    0,  619,    0,    0,  619,    0,  619,    0,
  619,    0,  619,    0,    0,    0,    0,    0,  619,  619,
    0,    0,    0,    0,    0,    0,  619,  619,    0,    0,
    0,  619,    0,    0,    0,  619,  161,    0,  161,    0,
    0,  161,    0,    0,    0,    0,  161,  619,    0,  619,
  161,    0,    0,  619,  619,    0,    0,    0,    0,  161,
    0,  619,  619,    0,    0,  619,  161,  619,  619,    0,
    0,  161,    0,  619,    0,  161,    0,  161,    0,  161,
    0,  161,    0,  161,  161,    0,  161,  161,    0,  161,
    0,  161,    0,  161,    0,  161,  161,    0,    0, 1123,
    0,  161,  161,    0,  161,  161,    0,    0,  161,    0,
    0,  161,  161,    0,    0,    0,  161,    0,    0,    0,
  161,    0,  161,    0,  161,    0,    0,    0,    0,  161,
    0,    0,  161,    0,  161,    0,  161,  160,  161,    0,
    0,  161,    0,    0,    0,    0,  161,  161,    0,    0,
  161,    0,    0,  161,    0,    0,   53,  161,   53,    0,
    0,   53,    0,    0,    0,    0,   53,    0,    0,    0,
   53,    0,    0,    0,    0,    0,    0,    0,  619,   53,
   83,  161,   84,    0,  160,   85,   53,    0,    0,    0,
   86,   53,   87,    0,   88,   53,   89,   53,    0,   53,
    0,    0,   90,   91,   53,    0,    0,   53,    0,   53,
   92,    0,    0,   53,    0,   93,   53,    0,  161,   94,
    0,   53,   53,    0,    0,   53,    0,    0,   53,    0,
   53,   95,   53,   96,    0,   53,    0,   97,    0,    0,
   53,    0,    0,    0,   53,   98,   99,    0,    0,  100,
    0,    0,  101,   53,    0,    0,    0,  102,    0,  158,
   53,    0,    0,  161,    0,   53,    0,    0,    0,   53,
   83,   53,   84,   53,    0,   85,    0,    0,   53,    0,
   86,   53,    0,   53,   88,    0,    0,   53,    0,    0,
   53,    0,    0,   91,    0,   53,   53,    0,    0,   53,
   92,    0,   53,    0,    0,   93,    0,    0,    0,   94,
   83,    0,   84,    0,    0,   85,    0,    0,    0,    0,
   86,   95,    0,   96,   88,    0,    0,   97,    0,    0,
    0,    0,    0,   91,    0,   98,   99,    0,   53,  100,
   92,    0,  117,    0,    0,   93,    0,    0,    0,   94,
   83,    0,   84,    0,    0,   85,    0,    0,    0,    0,
   86,   95,   74,   96,   88,    0,    0,   97,    0,    0,
    4,    0,    0,   91,    0,   98,   99,    0,    0,  100,
   92,    0,  117,    0,    0,   93,    0,    0,   83,   94,
   84,    0,    0,   85,    0,    0,    0,    0,   86,    0,
    0,   95,   88,   96,    0,    0,    0,   97,    0,    0,
    0,   91,   53,    0,    0,   98,   99,    0,   92,  100,
    0,    0,  117,   93,    0,    0,    0,   94,   83,    0,
   84,    0,    0,   85,    0,    0,    0,    0,   86,   95,
    0,   96,   88,    0,    0,   97,    0,    0,    0,    0,
    0,   91,   74,   98,   99,    0,    0,  100,   92,    0,
  117,    0,    0,   93,    0,    0,    0,   94,  179,    0,
  179,    0,    0,  179,    0,    0,    0,    0,  179,   95,
    0,   96,  179,    0,    0,   97,    0,    0,    0,    0,
    0,  179,  260,   98,   99,    0,    0,  100,  179,    0,
  117,    0,    0,  179,    0,    0,  188,  179,  188,    0,
    0,  188,    0,    0,    0,    0,  188,    0,    0,  179,
  188,  179,    0,    0,    0,  179,    0,    0,    0,  188,
    0,    0,  656,  179,  179,    0,  188,  179,    0,    0,
  179,  188,    0,  525,    0,  188,  180,    0,  180,  525,
    0,  180,    0,    0,    0,    0,  180,  188,    0,  188,
  180,    0,    0,  188,    0,    0,    0,    0,    0,  180,
  693,  188,  188,    0,    0,  188,  180,    0,  188,    0,
    0,  180,    0,    0,    0,  180,  525,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  180,    0,  180,
    0,    0,    0,  180,    0,    0,    0,    0,    0,    0,
  695,  180,  180,    0,    0,  180,    0,    0,  180,    0,
    0,    0,    0,    0,    0,    0,    0,  525,    0,    0,
    0,    0,    0,  525,  525,  525,  525,  525,  525,  525,
  525,  525,  525,  525,  525,    0,    0,  541,    0,    0,
  179,    0,  525,  541,  525,    0,  525,    0,  525,  525,
  525,    0,  525,  525,    0,  525,  525,    0,  525,    0,
  525,  525,  525,  525,  525,  525,  525,    0,    0,    0,
    0,    0,    0,  525,    0,  525,    0,  525,  188,  525,
  541,  525,    0,  525,    0,  525,    0,  525,    0,  525,
    0,  525,    0,  525,    0,  525,    0,  525,    0,  525,
    0,  525,    0,  525,    0,  525,    0,  525,  545,    0,
    0,  525,    0,    0,  545,    0,    0,    0,  180,    0,
    0,  541,    0,    0,    0,    0,    0,  541,  541,  541,
  541,  541,  541,  541,  541,  541,  541,  541,  541,    0,
    0,    0,    0,    0,    0,    0,  541,    0,  541,    0,
  541,  545,  541,  541,  541,    0,  541,  541,    0,    0,
  541,    0,  541,    0,  541,  541,  541,  541,  541,  541,
  541,    0,    0,    0,    0,    0,    0,  541,    0,  541,
    0,  541,    0,  541,    0,  541,    0,  541,    0,  541,
    0,  541,  545,    0,    0,    0,    0,    0,  545,  545,
  545,  545,  545,  545,  545,  545,  545,  545,  545,  545,
  546,    0,    0,    0,    0,  541,  546,  545,    0,  545,
    0,  545,    0,  545,  545,  545,    0,  545,  545,    0,
    0,  545,    0,  545,    0,  545,  545,    0,    0,    0,
  545,  545,    0,    0,    0,    0,    0,    0,  545,    0,
  545,    0,  545,  546,  545,    0,  545,    0,  545,    0,
  545,    0,  545,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  547,    0,    0,    0,    0,  545,  547,    0,    0,
    0,    0,    0,    0,  546,    0,    0,    0,    0,    0,
  546,  546,  546,  546,  546,  546,  546,  546,  546,  546,
  546,  546,    0,    0,    0,    0,    0,    0,    0,  546,
    0,  546,    0,  546,  547,  546,  546,  546,    0,  546,
  546,    0,    0,  546,    0,  546,    0,  546,  546,    0,
    0,    0,  546,  546,    0,    0,    0,    0,    0,    0,
  546,    0,  546,    0,  546,    0,  546,    0,  546,    0,
  546,    0,  546,    0,  546,  547,    0,    0,    0,    0,
    0,  547,  547,  547,  547,  547,  547,  547,  547,  547,
  547,  547,  547,  548,    0,    0,    0,    0,  546,  548,
  547,    0,  547,    0,  547,    0,  547,  547,  547,    0,
  547,  547,    0,    0,  547,    0,  547,    0,  547,  547,
    0,    0,    0,  547,  547,    0,    0,    0,    0,    0,
    0,  547,    0,  547,    0,  547,  548,  547,    0,  547,
    0,  547,    0,  547,    0,  547,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  549,    0,    0,    0,    0,  547,
  549,    0,    0,    0,    0,    0,    0,  548,    0,    0,
    0,    0,    0,  548,  548,  548,  548,  548,  548,  548,
  548,  548,  548,  548,  548,    0,    0,    0,    0,    0,
    0,    0,  548,    0,  548,    0,  548,  549,  548,  548,
  548,    0,    0,    0,    0,    0,  548,    0,  548,    0,
  548,  548,    0,    0,    0,  548,  548,    0,    0,    0,
    0,    0,    0,  548,    0,  548,    0,  548,    0,  548,
  550,  548,    0,  548,    0,  548,  550,  548,  549,    0,
    0,    0,    0,    0,  549,  549,  549,  549,  549,  549,
  549,  549,  549,  549,  549,  549,    0,    0,    0,    0,
    0,  548,    0,  549,    0,  549,    0,  549,    0,  549,
  549,  549,    0,  550,    0,    0,    0,  549,    0,  549,
    0,  549,  549,    0,    0,    0,  549,  549,    0,    0,
    0,    0,    0,    0,  549,    0,  549,    0,  549,    0,
  549,  554,  549,    0,  549,    0,  549,  554,  549,    0,
    0,    0,    0,    0,  550,    0,    0,    0,    0,    0,
  550,  550,  550,  550,  550,  550,  550,  550,  550,  550,
  550,  550,  549,    0,    0,    0,    0,    0,    0,  550,
    0,  550,    0,  550,  554,  550,  550,  550,    0,    0,
    0,    0,    0,  550,    0,  550,    0,  550,  550,    0,
    0,    0,  550,  550,    0,    0,    0,    0,    0,    0,
  550,    0,  550,    0,  550,    0,  550,  555,  550,    0,
  550,    0,  550,  555,  550,  554,    0,    0,    0,    0,
    0,  554,  554,  554,  554,  554,  554,  554,  554,  554,
  554,  554,  554,    0,    0,    0,    0,    0,  550,    0,
  554,    0,  554,    0,  554,    0,  554,  554,  554,    0,
  555,    0,    0,    0,  554,    0,  554,    0,  554,  554,
    0,    0,    0,  554,  554,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  554,    0,  554,  556,  554,
    0,  554,    0,  554,  556,  554,    0,    0,    0,    0,
    0,  555,    0,    0,    0,    0,    0,  555,  555,  555,
  555,  555,  555,  555,  555,  555,  555,  555,  555,  554,
    0,    0,    0,    0,    0,    0,  555,    0,  555,    0,
  555,  556,  555,  555,  555,    0,    0,    0,    0,    0,
  555,    0,  555,    0,  555,  555,    0,    0,    0,  555,
  555,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  555,    0,  555,  557,  555,    0,  555,    0,  555,
  557,  555,  556,    0,    0,    0,    0,    0,  556,  556,
  556,  556,  556,  556,  556,  556,  556,  556,  556,  556,
    0,    0,    0,    0,    0,  555,    0,  556,    0,  556,
    0,  556,    0,  556,  556,  556,    0,  557,    0,    0,
    0,  556,    0,  556,    0,  556,  556,    0,    0,    0,
  556,  556,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  556,    0,  556,  558,  556,    0,  556,    0,
  556,  558,  556,    0,    0,    0,    0,    0,  557,    0,
    0,    0,    0,    0,  557,  557,  557,  557,  557,  557,
  557,  557,  557,  557,  557,  557,  556,    0,    0,    0,
    0,    0,    0,  557,    0,  557,    0,  557,  558,  557,
  557,  557,    0,    0,    0,    0,    0,  557,    0,  557,
    0,  557,  557,    0,    0,    0,  557,  557,    0,    0,
    0,    0,    0,    0,    0,    0,  564,    0,  557,    0,
  557,    0,  557,    0,  557,    0,  557,    0,  557,  558,
    0,    0,    0,    0,    0,  558,  558,  558,  558,  558,
  558,  558,  558,  558,  558,  558,  558,    0,    0,    0,
    0,    0,  557,    0,  558,    0,  558,    0,  558,    0,
  558,  558,  558,    0,    0,    0,    0,    0,  558,    0,
  558,    0,  558,  558,    0,    0,    0,  558,  558,    0,
    0,    0,    0,    0,    0,    0,    0,  565,    0,  558,
    0,  558,    0,  558,    0,  558,    0,  558,    0,  558,
  564,    0,    0,    0,    0,    0,  564,  564,  564,  564,
  564,  564,  564,  564,  564,  564,  564,  564,    0,    0,
    0,    0,    0,  558,    0,  564,    0,  564,    0,  564,
    0,  564,  564,  564,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  564,  564,    0,    0,    0,  564,  564,
    0,    0,    0,    0,    0,    0,    0,    0,  566,    0,
    0,    0,    0,    0,  564,    0,  564,    0,  564,    0,
  564,  565,    0,    0,    0,    0,    0,  565,  565,  565,
  565,  565,  565,  565,  565,  565,  565,  565,  565,    0,
    0,    0,    0,    0,  564,    0,  565,    0,  565,    0,
  565,    0,  565,  565,  565,  567,    0,    0,    0,    0,
    0,    0,    0,    0,  565,  565,    0,    0,    0,  565,
  565,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  565,    0,  565,    0,  565,
    0,  565,  566,    0,    0,    0,    0,    0,  566,  566,
  566,  566,  566,  566,  566,  566,  566,  566,  566,  566,
    0,    0,    0,    0,    0,  565,    0,  566,    0,  566,
    0,  566,    0,  566,  566,  566,  568,    0,    0,    0,
    0,    0,    0,    0,    0,  566,  566,    0,    0,  567,
  566,  566,    0,    0,    0,  567,  567,  567,  567,  567,
  567,  567,  567,  567,  567,  567,  567,    0,    0,    0,
  566,    0,  566,    0,  567,    0,  567,    0,  567,    0,
  567,  567,  567,  569,    0,    0,    0,    0,    0,    0,
    0,    0,  567,  567,    0,    0,  566,  567,  567,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  567,    0,  567,
  568,    0,    0,    0,    0,    0,  568,  568,  568,  568,
  568,  568,  568,  568,  568,  568,  568,  568,    0,    0,
    0,    0,    0,  567,    0,  568,    0,  568,    0,  568,
    0,  568,  568,  568,  570,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  568,    0,    0,  569,  568,  568,
    0,    0,    0,  569,  569,  569,  569,  569,  569,  569,
  569,  569,  569,  569,  569,    0,    0,    0,  568,    0,
  568,    0,  569,    0,  569,    0,  569,    0,  569,  569,
  569,  571,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  569,    0,    0,  568,  569,  569,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  569,    0,  569,  570,    0,
    0,    0,    0,    0,  570,  570,  570,  570,  570,  570,
  570,  570,  570,  570,  570,  570,    0,    0,    0,    0,
    0,  569,    0,  570,    0,  570,    0,  570,    0,  570,
  570,  570,  572,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  570,    0,    0,  571,    0,  570,    0,    0,
    0,  571,  571,  571,  571,  571,  571,  571,  571,  571,
  571,  571,  571,    0,    0,    0,  570,    0,  570,    0,
  571,    0,  571,    0,  571,    0,  571,  571,  571,  573,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  571,
    0,    0,  570,    0,  571,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  574,    0,    0,
    0,    0,    0,  571,    0,  571,  572,    0,    0,    0,
    0,    0,  572,  572,  572,  572,  572,  572,  572,  572,
  572,  572,  572,  572,    0,    0,    0,    0,    0,  571,
    0,  572,    0,  572,    0,  572,    0,  572,  572,  572,
    0,  575,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  573,    0,  572,    0,    0,    0,  573,
  573,  573,  573,  573,  573,  573,  573,  573,  573,  573,
  573,    0,    0,    0,  572,    0,  572,    0,  573,    0,
  573,  574,  573,    0,  573,  573,  573,  574,  574,  574,
  574,  574,  574,  574,  574,  574,  574,  574,  574,    0,
  572,    0,  573,    0,    0,    0,  574,  352,  574,    0,
  574,    0,  574,  574,  574,    0,    0,    0,    0,    0,
    0,  573,    0,  573,    0,  575,    0,    0,    0,   47,
  574,  575,  575,  575,  575,  575,  575,  575,  575,  575,
  575,  575,  575,    0,  352,    0,    0,  573,    0,    0,
  575,  574,  575,   48,  575,    0,  575,  575,  575,    0,
    0,    0,    0,    0,    0,    0,   49,    0,    0,    0,
    0,   51,    0,    0,  575,  574,   52,    0,   53,   54,
   55,   56,    0,    0, 1285,    0,   57,    0,    0,    0,
   58,    0,    0,    0,    0,  575,    0,    0,  551,    0,
    0,    0,   59,    0,    0,   60,    0,   61,    0,    0,
    0,  352,    0,  352,  352,  352,  352,    0,    0,  575,
  352,  352,    0,    0,  352,    0,  352,    0,  352,  352,
  352,  352,  352,  352,  352,  551,    0,  352,    0,  352,
    0,  352,    0,  352,    0,  352,    0,  352,    0,  352,
    0,  352,    0,  352,    0,  352,    0,  352,  352,  352,
    0,  352,    0,  352,    0,  352,    0,  352,    0,  352,
    0,  352,    0,  352,    0,  352,  551,  352,    0,  352,
    0,    0,  551,  551,  551,  551,  551,  551,  551,  551,
  551,  551,  551,  551,    0,  352,    0,    0,    0,    0,
    0,  551,    0,  551,    0,  551,    0,  551,  551,  551,
    0,    0,    0,    0,    0,  551,    0,  551,    0,  551,
  551,    0,    0,    0,  551,  551,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  551,    0,  551,    0,
  551,    0,  551,    0,  551,    0,  551,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  352,  352,    0,  352,    0,  352,  352,    0,    0,
  551,  352,  352,    0,    0,  352,    0,  352,    0,  352,
  352,  352,  352,  352,  352,  352,    0,  387,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,    0,    0,
    0,  387,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   53,    0,  387,    0,    0,    0,  352,  387,
  352,    0,  387,    0,  387,    0,  387,  387,  387,  387,
    0,    0,    0,    0,  387,    0,   53,    0,  387,    0,
    0,    0,  387,    0,    0,    0,    0,    0,    0,   53,
  387,    0,    0,  387,   53,  387,    0,    0,    0,   53,
    0,   53,   53,   53,   53,    0,    0,    0,    0,   53,
    0,    0,   47,   53, 1284,    0,    0,    0,    0,    0,
    0,    0,  387,    0,    0,   53,    0,    0,   53,    0,
   53,    0,    0,    0,    0,    0,   48,    0,    0,    0,
    0,    0,   47,    0, 1308,  387,    0,    0,    0,   49,
   53,    0,   53,    0,   51,    0,  198,    0,    0,   52,
    0,   53,   54,   55,   56,    0,   48, 1285,    0,   57,
    0,    0,   47,   58, 1284,    0,    0,    0,    0,   49,
    0,    0,    0,    0,   51,   59,    0,    0,   60,   52,
   61,   53,   54,   55,   56,    0,   48, 1309,    0,   57,
    0,    0,   47,   58,    0,    0,    0,    0,    0,   49,
    0,    0,    0,    0,   51,   59,    0,    0,   60,   52,
   61,   53,   54,   55,   56,    0,   48,    0,    0,   57,
    0,    0,   47,   58, 1308,    0,    0,    0,    0,   49,
    0,    0,    0,    0,   51,   59,    0,    0,   60,   52,
   61,   53,   54,   55,   56,    0,   48, 1309,    0,   57,
    0,    0,   47,   58,    0,    0,    0,    0,    0,   49,
    0,    0,    0,    0,   51,   59,    0,    0,   60,   52,
   61,   53,   54,   55,   56,    0,   48,    0,    0,   57,
    0,    0,   47,   58,    0,    0,    0,    0,    0,   49,
    0,    0,    0,    0,   51,   59,    0,    0,   60,   52,
   61,   53,   54,   55,   56,    0, 1084,    0,    0,   57,
    0,    0,    0,   58,    0,    0,    0,    0,    0,   49,
    0,    0,    0,    0,   51,   59,    0,    0,   60,   52,
   61,   53,   54,   55,   56,    0,    0,    0,    0,   57,
    0,    0,    0,   58,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0, 1085,    0,    0,   60,    0,
   61,
  };
  protected static  short [] yyCheck = {            63,
    0,   62,  105,   66,  236,  101,    6,   75,   69,  236,
  403,    3,    4,  236,  256,  236,  455,  554,  131,   25,
  122,  123,  591,  254,  254,  103,   87,  254,  106,   90,
  712,    0,  501,  453,  571,  572,  297,    6,  827,  828,
  140,  578,  579,  217,  458,  256,   38,  256,  501,  256,
  256,  221,  256,  596,  167,  629,  269,  118,   50,  256,
  269,  269,  232,  606,  402,  831, 1038,  295,  380,  277,
  131,  101,  283,  256,  256,  551,   68,  256,  139,  140,
  307, 1053,  326,  680,   76,  423,  213,  256,  709,  366,
 1201,  368,  256,  380,  344,  156,  157,  274, 1068,  448,
   69,  278,  385,  390,  315,  282,  256,  385,  385,  101,
  389,  900,  118,  105,  256,  366,  256,  906,  907,  257,
  233,  366,  326,  842,  257, 1068,  340,  378,  917,  366,
  380,  410,  256,  378,  853,  256,  262, 1107,  366,  256,
  390,  448,  255,  455,  256,  256,  256,  256,  378,  256,
  364,  378,  365,  380,  368,  434,  234,  385,  372,  448,
  366,  285,  366,  256, 1107,  448,  343,  231,  455,  366,
  448,  403,  449,  299,  344,  236,  403,  256,  239,  385,
  403,  385,  403,  366,  366,  359,  264,  366,  385,  763,
  360,  765,  222,  454,  815,  370,  257,  366,  267,  796,
  370,  231,  385,  385,  448,  369,  385,  371,  677,  326,
  929,  381,  382,  383,  456,  934,  935,  278, 1329,  369,
  448,  321,  322,  393,  638,  286,  366,  993, 1008,  371,
  222,  707,  365,  256,  361,  449,  363,  448,  365,  231,
  448,  448,  448,  456,  448,  583,  315,  456,  369,  366,
  371,  448, 1018,  379,  366,  381,  368, 1229,  977,  366,
  371,  371,  371,  370,  983,  448,  448,  366,  295,  838,
  239,  364,  389,  385,  400,  344,  402,  370,  371,  366,
  373, 1061,  364,  366,  345,  364,  368,  370,  370,  402,
  833,  370,  353,  141,  142,  143,  144,  145,  146,  147,
  148,  273,  385,  372,  773,  365,  389, 1006,  366,  278,
  366,  380,  370, 1006,  377,  365,  373,  378,  257,  380,
  773,  390,  369, 1008,  591,  297,  422,  390,  391,  385,
   62,  295,  307, 1068,  447,  295,  816,  604,  440,  314,
  368,  364,  403,  366, 1043,  368,  285,  371,  380,  448,
 1043,  326,  324,  380, 1073,   87, 1006, 1068,   90,  779,
  370,  448,  385,  390,  591,  448,  759,  449,  782, 1068,
 1052,  369, 1107,  475,  278, 1068, 1061,  604,  456,  389,
  443,  924,  643, 1068,  353, 1067,  118, 1069,  448,  502,
  448, 1110,  448, 1043,  568,  497, 1107,  336,  448,  131,
  461,  501,  504,  450,  451,  452,  453,  139, 1107,  472,
  640,  642,  642,  640, 1107,  642,  380,  478, 1068,  899,
  380,  449, 1107,  455,  156,  157,  390,  366,  364,  366,
  390,  380,  368,  370,  370,  371,  368,  373,  364,  366,
  501,  390,  368, 1008,  370, 1014,  294,  295,  296,  353,
  256,  372,  389,  451,  452,  453, 1008, 1107,  385,  265,
  521,  522,  256,  368,  373,  370,  568,  364,  273,  461,
  583,  368,  364,  321,  322,  323,  324,  538,  539,  327,
  328,  329,  330,  331,  332,  333,  334,  335,  336,  366,
  338, 1280,  297,  448, 1283,  366, 1061,  389, 1068,  366,
  613,  562,  369,  370,  371,  366,  256, 1008,  385, 1061,
 1008,  448,  306,  449,  385,  261,  270,  263,  385,  324,
  326,  448,  389,  449,  385,  257,  622,  759,  448,  590,
  591,  592,  759,  287,  366,  256,  759, 1107,  759,  285,
  603,  375,  376,  604,  449,  266,  368,  268,  370,  368,
  271,  370,  298,  385,  286,  276,  306,  303, 1068,  280,
 1061,  273,  308, 1061,  310,  311,  312,  313,  289,  256,
  316,  448,  318,  366,  448,  296,  322,  448,  748,  657,
  301,  448,  366,  270,  305,  297,  654,  448,  334,  366,
  338,  337,  385,  339,  364,  371,  317, 1107,  319,  366,
  287,  385,  323,  373,  344,  453,  382,  342,  385,  261,
  331,  332,  324,  345,  335,  842,  448,  338,  385,  365,
  468, 1231, 1232,  471,  630,  342,  853,  449,  366, 1239,
  449,  366,  874,  285, 1171,    6,  364,    8,  378, 1249,
  380,  340,  344,  364,  340,  366,  298,  385,  367,  366,
  390,  303,  380,  380,  306,  448,  308, 1210,  310,  311,
  312,  313,  390,  390,  448,  364,  318,  780,  364,  380,
  322,  448, 1209,   44,  326, 1212,  378,  373,  380,  390,
  340,  448,  334,  350,  351,  337,  749,  339,  390, 1242,
 1243, 1301, 1245,  340,  365,  366,  364,  368,  759,  370,
  371,  340,  929,  764,  364,  373,  767,  934,  935,  389,
  378, 1248,  366,  364,  366,  369,  368,  364,  370,  385,
 1113,  392,  373,  394,  371,  364,  373,  448,  791,  461,
  366,  385,  371,  385,  380,  365,  366,  389,  368,  367,
  370,  371,  378,  366,  380,  368,  478,  370,  370,  366,
  977,  307,  369,  309,  390,  369,  983,  371,  314,  373,
  366,  432,  392,  824,  394,  368,  389,  389,  385,  392,
  326,  394,  366,  368,  368,  261,  370,  879,  880,  385,
  371,  842,  371,  371,  845,  368,  849, 1226, 1227,  521,
  522,  385,  853,  382,  382,  389,  448,  449,  392,  285,
  394,  366,  432,  369,  369,  370,  538,  539,  365,  432,
  371,  378,  298,  380,  371,  261,  816,  303,  818,  366,
  385,  378,  308,  390,  310,  311,  312,  313,  364,  371,
  562,  378,  318,  380,  383,  903,  322,  373,  432,  285,
  382,  350,  351,  390,  371, 1077, 1073,  378,  334,  380,
 1077,  337,  298,  339, 1077,  382, 1077,  303,  590,  390,
  592,  384,  308,  261,  310,  311,  312,  313,  929,  932,
  316,  370,  318,  934,  935,  366,  322,  366,  369,  370,
  369, 1113,  396, 1110,  398,   79, 1113,  285,  334,  408,
 1113,  337, 1113,  339,  385,  364,  385,  364,  389,  899,
  298,  404,  364,  406,  373,  303,  373,  968,  306,  388,
  308,  373,  310,  311,  312,  313,  977,  364,  366,  365,
  318,  369,  983,  366,  322,  368,  366,  370,  326,  369,
  372,  779,  365,  364,  364,  370,  334,  385,  371,  337,
  448,  339,  373,  373, 1005,  385,  140,  340,  364,  392,
  365,  394,  368,  346,  347,  256,  371,  350,  351,  340,
  353,  354,  448,  963,  365,  346,  347,  378,  366, 1201,
  371,  365,  353,  354, 1201,  369, 1146,  371, 1201,  373,
 1201,  367,  368,  256,  370,  371,  372,  385,  371,  432,
 1160,  367,  368,  385, 1164,  371,  366,  389,  385, 1169,
 1170,  412,  389,  414,  448,  416, 1006,  418, 1008,  420,
  367,  422, 1073,  424,  371,  426, 1077,  428,  371,  430,
  373, 1124, 1083,  367, 1194, 1086,  367,  371,  369,  364,
  371, 1092,  764, 1203, 1204,  767,  367,  367,  369,  368,
  371,  371,  367, 1043,  777, 1141,  371,  371,  781, 1110,
  448,  245, 1113,  247,  370,  369,  369,  373, 1119, 1120,
  373, 1061,  369,  340,  370,  259,  373,  373, 1068,  346,
  347, 1132,  454,  350,  351,  256,  353,  354,  327,  328,
  329,  330, 1143,  277, 1145,  355,  356, 1150,  385,  386,
  387,  371,  824,  373,  371,  355,  356, 1329,  292,  293,
  350,  351, 1329,  297,  298,  448, 1329, 1107, 1329,  385,
  386,  387,  371,  845,  373,  448,  310,  311,  312,  313,
  314,  315,  316,  317,  318,  319,  320,  369,  371,  371,
  373,  371, 1124,  373,  359,  367,  361,  369,  723,  724,
 1201, 1133, 1134,  337,  367,  339,  369,  373,  367, 1141,
  369,  371,  369,  373,  371,  323,  324,  351,  352,  340,
  367, 1264,  369,  448,  448,  346,  347,  348,  349,  350,
  351,  352,  353,  354,  355,  356,  357,  367,  380,  369,
  359,  387,  361,  389,  365, 1288,  367, 1290,  369,  366,
  371,  372,  373,  387, 1257,  389,  331,  332,  367,  371,
  448,  448,  383,  384,  448, 1205,  340,  388,  389,  373,
  373,  368,  346,  347,  368,  368,  350,  351,  364,  353,
  354,    0, 1286,  404,  385,  406,  368,  408,  365,  410,
  373, 1231, 1232,  261, 1297, 1298,  968,  371,  369, 1239,
  373, 1241, 1305,  373, 1307,  369,  295, 1311, 1312, 1249,
  344,  445,  368,  434,  369,  372,  448,  285, 1258,  364,
  454,  455,  448,  367,  458,  448, 1266, 1267, 1329,  463,
  298,  380,  341, 1005,  373,  303,  389,  373,  373,  371,
  308,  256,  310,  311,  312,  313,  373,  373,  369,  371,
  318,  371,  371,  365,  322,  489,  490,  371,  369,  369,
  378, 1301,  369,  371,  370,  357,  334,  501,  340,  337,
  256,  339,  371,  285,  346,  347,  262,  340,  350,  351,
  336,  353,  354,  346,  347,  369,  367,  350,  351,  369,
  353,  354,  369,  372,  369,  369,  378,  373,  366,  368,
  371,  371,  389,  537,  369,  369,  373,  541,  369,  369,
  369, 1083,  256,  299, 1086,  373,  256,  385,  364,  448,
 1092,  365,  295,  557,  448,  340,  295,  352,  365,  340,
  378,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,  448,  448,    0,  295, 1119, 1120,  369,
  365,  348,  367,  371,  369,  371,  371,  372,  373,  593,
 1132,  455,  596,  597,  369,  371,  367,  371,  371,  369,
  604, 1143,  606, 1145,  608,  448,  369,  281,  256,  364,
  448,  256,  448,  369,  348,  371,  448,  365,  372,  375,
  376,  625,  364,  379,  364,  381,  364,  383,  384,  373,
  365,  367,  388,  389,  638,  448,  373,  369,  369,  643,
  396,  349,  398,  373,  400,  378,  402,  349,  404,  371,
  406,  368,  408,  365,  410,  448,  365,  372,  369,  663,
  357,  374,  369,  667,  365,  364,  372,  370,  257,  369,
  366,  366,  261,  677,  364,  368,  680,  266,  434,  268,
  367,  366,  271,  365,  273,  274,  690,  276,  369,  278,
  369,  280,  367,  282,  283,  284,  285,  701,  702,  288,
  289,  369,  369,  369,  365,  294,  364,  296,  297,  298,
  369,  367,  301,  302,  303,  369,  305,  369,  365,  308,
  256,  310,  311,  312,  313,  256,  365,  365,  317,  318,
  319,  365,  365,  322,  323,  324,  365,    0,  373,  367,
    0,  745,  331,  332,  367,  334,  335,  336,  337,  338,
  339,  369,  364,  364,  343,  369,  369,  364,  367,  369,
  448,  365,  448,  369,  261,  373,  263,  369,  367,  773,
  367,  365,  365,  777,  369,  448,  365,  366,  782,  369,
  364,  373,  365,  365,  369,  374,  373,  373,  285,  369,
  371,  364,  796,  448,  365,  364,  800,  373,  802,  373,
  804,  298,  369,  807,  365,  369,  303,  365,  364,  369,
  364,  308,  364,  310,  311,  312,  313,  821,    6,    0,
    6,  318,   38,   76,  292,  322,  293,  831, 1043,  833,
 1140, 1107,  257,  832,  558,  839,  261,  334,  356, 1222,
  337,  266,  339,  268, 1298,  769,  271,  769,  273,  769,
  763,  276, 1061,  278, 1321,  280, 1258,  282, 1267,  448,
  285,  963,  239,  288,  289,  265, 1266,  968,  365,  306,
  490,  296,  297,  298,  642,  305,  301,  302,  303,  779,
  305,  307,  886,  308,  888,  310,  311,  312,  313,  893,
  522,  333,  317,  318,  319,  780,  334,  322,  323,  324,
  335,  478,  336,  913,  791,  338,  331,  332, 1083,  334,
  335,  336,  337,  338,  339, 1077,  378,  921,  343, 1023,
  924, 1021,  266,  927,  268, 1014,  750,  271,  923,  273,
  274,  956,  276, 1116,  278,  954,  280,   -1,  282,  283,
  365,  366,  545,   -1,   -1,  289,  950,   -1,   -1,   -1,
  954,   -1,  296,  297,   -1,   -1,   -1,  301,   -1,   -1,
   -1,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,  323,
  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,  993,
   -1,  335,   -1,  997,  338,   -1,   -1,  257,   -1,  343,
   -1,  261,   -1,   -1,   -1,   -1,  266,   -1,  268,   -1,
   -1,  271,   -1,  273, 1018,   -1,  276,   -1,  278,   -1,
  280,   -1,  282,  448, 1028,  285,   -1, 1031,  288,  289,
   -1,   -1,   -1,   -1,   -1,   -1,  296,  297,  298,   -1,
   -1,  301,  302,  303,   -1,  305,   -1,   -1,  308,   -1,
  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,
   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,
   -1,  331,  332,   -1,  334,  335, 1080,  337,  338,  339,
  261,   -1,   -1,  343,   -1,  266,   -1,  268,   -1,   -1,
  271,   -1,  273,   -1,   -1,  276,   -1,  278,   -1,  280,
   -1,  282,   -1,   -1,  448,   -1,   -1,  288,  289,   -1,
   -1,   -1,   -1,   -1,   -1,  296,  297,  298,   -1,   -1,
  301,  302,  303,   -1,  305,   -1,   -1,  308,   -1,  310,
  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,
   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,
  331,  332,   -1,  334,  335,   -1,  337,  338,  339,   -1,
   -1,   -1,  343,   -1,   -1,   -1,   -1,  256,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  265,  266,  267,  268,
  269,   -1,  271,  272,   -1,  274,  275,  276,  448,  278,
  279,  280,  281,   -1,   -1,   -1,   -1,  286,   -1,  288,
  289,  290,  291,  292,  293,   -1,   -1,  296,   -1,   -1,
   -1,  300,  301,   -1,  303,  304,  305,   -1,   -1,   -1,
   -1,   -1, 1226, 1227,   -1,   -1,  315,   -1,  317,   -1,
  319,  320,   -1,   -1,  323,   -1,  325,  326,  327,  328,
  329,  330,  331,  332,  333,  334,  335,  336,   -1,  338,
   -1,   -1,  341,   -1,   -1,   -1,   -1,  346,   -1,   -1,
  266,   -1,  268,   -1,   -1,  271,   -1,  448,   -1,   -1,
  276,   -1,   -1,   -1,  280,  364,  365,   -1,   -1,  368,
   -1,   -1,   -1,  289,  373,  374,  375,  376,  377,   -1,
  296,   -1,   -1,   -1,  383,  301,  385,   -1,   -1,  305,
   -1,  307,   -1,  392,   -1,  394,   -1,   -1,  314,   -1,
   -1,  317,   -1,  319,   -1,   -1,   -1,  323,   -1,   -1,
  326,   -1,   -1,   -1,   -1,  331,  332,   -1,   -1,  335,
   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,
   -1,  440,   -1,  442,   -1,  444,   -1,  446,   -1,  448,
  449,  256,   -1,   -1,   -1,  454,   -1,  456,   -1,   -1,
  265,  266,  267,  268,  269,   -1,  271,  272,   -1,  274,
  275,  276,   -1,  278,  279,  280,   -1,   -1,   -1,   -1,
   -1,  286,   -1,  288,  289,  290,  291,  292,  293,   -1,
   -1,  296,   -1,   -1,   -1,  300,  301,   -1,  303,  304,
  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  315,   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,
  325,  326,  327,  328,  329,  330,  331,  332,  333,  334,
  335,  336,  448,  338,   -1,   -1,  341,   -1,   -1,   -1,
   -1,  346,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  266,   -1,  268,   -1,   -1,  271,   -1,   -1,  364,
  365,  276,   -1,  368,   -1,  280,   -1,   -1,  373,  374,
  375,  376,  377,   -1,  289,   -1,   -1,   -1,  383,   -1,
  385,  296,   -1,   -1,   -1,   -1,  301,  392,   -1,  394,
  305,   -1,  307,   -1,  309,   -1,   -1,   -1,   -1,  314,
   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,  323,   -1,
   -1,  326,   -1,   -1,   -1,   -1,  331,  332,   -1,   -1,
  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  436,   -1,  438,   -1,  440,   -1,  442,   -1,  444,
   -1,  446,   -1,  448,  449,  256,   -1,   -1,   -1,  454,
   -1,  456,  367,   -1,  265,  266,  267,  268,   -1,   -1,
  271,  272,   -1,  274,  275,  276,   -1,  278,  279,  280,
   -1,   -1,   -1,   -1,   -1,  286,   -1,  288,  289,  290,
  291,  292,  293,   -1,   -1,  296,   -1,   -1,   -1,  300,
  301,   -1,  303,  304,  305,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  315,   -1,  317,   -1,  319,  320,
   -1,   -1,  323,   -1,  325,  326,  327,  328,  329,  330,
  331,  332,  333,  334,  335,  336,   -1,  338,  256,   -1,
  341,   -1,   -1,  448,   -1,  346,   -1,  265,  266,   -1,
  268,   -1,   -1,  271,  272,   -1,   -1,   -1,  276,   -1,
  278,   -1,  280,  364,   -1,   -1,   -1,  368,  286,   -1,
   -1,  289,  373,  374,  375,  376,  377,   -1,  296,   -1,
  286,   -1,  383,  301,  385,  303,  304,  305,   -1,   -1,
   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,
   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,
  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,
  338,   -1,  328,   -1,   -1,   -1,   -1,   -1,  346,   -1,
   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,  440,
   -1,  442,   -1,  444,   -1,  446,   -1,  448,  449,   -1,
  368,   -1,   -1,  454,   -1,   -1,  374,  375,  376,  377,
   -1,   -1,   -1,   -1,   -1,  383,   -1,  385,  374,  375,
  376,  377,   -1,  379,  392,  381,  394,  383,  384,  385,
  386,  387,  388,   -1,   -1,   -1,  392,   -1,  394,   -1,
  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,  256,
  406,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  265,  266,
   -1,  268,   -1,   -1,  271,  272,   -1,   -1,  436,  276,
  438,  278,  440,  280,  442,   -1,  444,   -1,  446,  286,
  448,  449,  289,   -1,   -1,   -1,  454,   -1,   -1,  296,
   -1,   -1,   -1,   -1,  301,   -1,  303,  304,  305,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,
   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,
   -1,  338,  256,   -1,   -1,   -1,   -1,   -1,   -1,  346,
   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,
   -1,   -1,  276,   -1,  278,   -1,  280,   -1,   -1,   -1,
   -1,  368,  286,   -1,  256,  289,   -1,  374,  375,  376,
  377,   -1,  296,   -1,   -1,   -1,  383,  301,  385,  303,
  304,  305,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,
   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,
   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  436,
   -1,  438,   -1,  440,   -1,  442,   -1,  444,   -1,  446,
   -1,  448,  449,   -1,  368,  256,   -1,  454,  340,   -1,
  261,  262,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  365,  285,  367,   -1,  369,   -1,  371,
  372,  373,   -1,   -1,  295,   -1,   -1,  298,  299,   -1,
   -1,   -1,  303,   -1,   -1,  306,   -1,  308,   -1,  310,
  311,  312,  313,   -1,   -1,   -1,   -1,  318,   -1,   -1,
   -1,  322,  436,   -1,  438,  326,  440,   -1,  442,   -1,
  444,   -1,  446,  334,  448,   -1,  337,   -1,  339,  340,
  454,   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,
  351,  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,
   -1,   -1,   -1,  364,  365,   -1,  367,  368,  369,  370,
  371,  372,  373,   -1,  375,  376,   -1,  378,  379,   -1,
  381,  382,  383,  384,  385,  386,  387,  388,  389,   -1,
   -1,  392,   -1,  394,   -1,  396,   -1,  398,   -1,  400,
   -1,  402,   -1,  404,   -1,  406,   -1,  408,   -1,  410,
   -1,  412,   -1,  414,   -1,  416,   -1,  418,   -1,  420,
   -1,  422,   -1,  424,   -1,  426,   -1,  428,   -1,  430,
  256,  432,   -1,  434,   -1,  261,  262,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  448,  449,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  285,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  295,
   -1,   -1,  298,  299,   -1,   -1,   -1,  303,   -1,   -1,
  306,   -1,  308,   -1,  310,  311,  312,  313,   -1,   -1,
   -1,   -1,  318,   -1,   -1,   -1,  322,   -1,   -1,   -1,
  326,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  334,   -1,
   -1,  337,   -1,  339,  340,   -1,   -1,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,  364,  365,
  366,  367,  368,  369,  370,  371,  372,  373,   -1,   -1,
   -1,   -1,   -1,  379,   -1,  381,  382,  383,  384,  385,
   -1,   -1,  388,  389,  256,   -1,   -1,   -1,   -1,  261,
  262,   -1,   -1,   -1,  400,   -1,  402,   -1,  404,   -1,
  406,   -1,  408,   -1,  410,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  285,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  295,   -1,   -1,  298,  299,  434,   -1,
   -1,  303,   -1,   -1,   -1,   -1,  308,   -1,  310,  311,
  312,  313,  448,  449,   -1,   -1,  318,   -1,   -1,   -1,
  322,   -1,   -1,   -1,  326,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  334,   -1,   -1,  337,   -1,  339,  340,   -1,
   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,
   -1,   -1,  364,  365,  366,  367,  368,  369,  370,  371,
  372,  373,   -1,   -1,   -1,   -1,   -1,  379,   -1,  381,
  382,  383,  384,  385,   -1,   -1,  388,  389,  256,   -1,
   -1,   -1,   -1,  261,  262,   -1,   -1,   -1,  400,   -1,
  402,   -1,  404,   -1,  406,   -1,  408,   -1,  410,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  285,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  295,   -1,   -1,
  298,  299,  434,   -1,   -1,  303,   -1,   -1,  306,   -1,
  308,   -1,  310,  311,  312,  313,  448,  449,   -1,   -1,
  318,   -1,   -1,   -1,  322,   -1,   -1,   -1,  326,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  334,   -1,   -1,  337,
   -1,  339,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
   -1,   -1,   -1,   -1,   -1,   -1,  364,  365,  366,  367,
  368,  369,  256,  371,  372,  373,  261,   -1,  262,   -1,
   -1,  379,   -1,  381,  382,  383,  384,  385,   -1,   -1,
  388,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  285,   -1,  400,   -1,  402,   -1,  404,   -1,  406,   -1,
  408,  295,  410,  298,   -1,  299,   -1,   -1,  303,   -1,
   -1,   -1,   -1,  308,   -1,  310,  311,  312,  313,   -1,
   -1,   -1,   -1,  318,   -1,   -1,  434,  322,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  334,
  448,  449,  337,   -1,  339,   -1,  340,   -1,   -1,   -1,
   -1,   -1,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,  359,   -1,  361,   -1,   -1,   -1,
  364,  365,  366,  367,  368,  369,  256,  371,  372,  373,
  261,   -1,  262,   -1,   -1,  379,   -1,  381,  382,  383,
  384,   -1,   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  285,   -1,  400,   -1,  402,   -1,
  404,   -1,  406,   -1,  408,   -1,  410,  298,   -1,  299,
   -1,   -1,  303,   -1,   -1,   -1,   -1,  308,   -1,  310,
  311,  312,  313,   -1,   -1,   -1,   -1,  318,   -1,   -1,
  434,  322,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  334,  448,  449,  337,   -1,  339,   -1,
  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,  359,   -1,
  361,   -1,   -1,   -1,  365,  365,  366,  367,  368,  369,
  370,  371,  372,  373,  256,  375,  376,   -1,  378,  379,
  262,  381,   -1,  383,  384,  385,  386,  387,  388,  389,
   -1,   -1,  392,   -1,  394,   -1,  396,   -1,  398,   -1,
  400,   -1,  402,   -1,  404,   -1,  406,   -1,  408,   -1,
  410,   -1,  412,   -1,  414,   -1,  416,  299,  418,   -1,
  420,   -1,  422,   -1,  424,   -1,  426,   -1,  428,   -1,
  430,   -1,  432,   -1,  434,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  448,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  340,   -1,
   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  365,  366,  367,  368,  369,  370,  371,
  372,  373,  256,  375,  376,   -1,  378,  379,  262,  381,
   -1,  383,  384,  385,  386,  387,  388,  389,   -1,   -1,
  392,   -1,  394,   -1,  396,   -1,  398,   -1,  400,   -1,
  402,   -1,  404,   -1,  406,   -1,  408,   -1,  410,   -1,
  412,   -1,  414,   -1,  416,  299,  418,   -1,  420,   -1,
  422,   -1,  424,   -1,  426,   -1,  428,   -1,  430,   -1,
  432,   -1,  434,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  448,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,
   -1,   -1,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  365,  366,  367,  368,  369,  370,  371,  372,  373,
  256,  375,  376,   -1,  378,  379,  262,  381,   -1,  383,
  384,  385,  386,  387,  388,  389,   -1,   -1,  392,   -1,
  394,   -1,  396,   -1,  398,   -1,  400,   -1,  402,   -1,
  404,   -1,  406,   -1,  408,   -1,  410,   -1,  412,   -1,
  414,   -1,  416,  299,  418,   -1,  420,   -1,  422,   -1,
  424,   -1,  426,   -1,  428,   -1,  430,   -1,  432,   -1,
  434,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  448,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,   -1,   -1,  256,   -1,   -1,   -1,  364,  365,
  262,  367,  368,  369,  370,  371,  372,  373,   -1,  375,
  376,   -1,  378,  379,   -1,  381,   -1,  383,  384,  385,
  386,  387,  388,  389,   -1,   -1,  392,   -1,  394,   -1,
  396,   -1,  398,   -1,  400,   -1,  402,  299,  404,   -1,
  406,   -1,  408,   -1,  410,   -1,  412,   -1,  414,   -1,
  416,   -1,  418,   -1,  420,   -1,  422,   -1,  424,   -1,
  426,   -1,  428,   -1,  430,   -1,  432,   -1,  434,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  340,   -1,
   -1,   -1,  448,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  365,  366,  367,  368,  369,  370,  371,
  372,  373,  256,  375,  376,   -1,  378,  379,  262,  381,
   -1,  383,  384,  385,  386,  387,  388,  389,   -1,   -1,
  392,   -1,  394,   -1,  396,   -1,  398,   -1,  400,   -1,
  402,   -1,  404,   -1,  406,   -1,  408,   -1,  410,   -1,
  412,   -1,  414,   -1,  416,  299,  418,   -1,  420,   -1,
  422,   -1,  424,   -1,  426,   -1,  428,   -1,  430,   -1,
  432,   -1,  434,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  448,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,
   -1,   -1,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  365,  366,  367,  368,  369,  370,  371,  372,  373,
  256,  375,  376,   -1,  378,  379,  262,  381,   -1,  383,
  384,  385,  386,  387,  388,  389,   -1,   -1,  392,   -1,
  394,   -1,  396,   -1,  398,   -1,  400,   -1,  402,   -1,
  404,   -1,  406,   -1,  408,   -1,  410,   -1,  412,   -1,
  414,   -1,  416,  299,  418,   -1,  420,   -1,  422,   -1,
  424,   -1,  426,   -1,  428,   -1,  430,   -1,  432,   -1,
  434,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  448,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  365,
   -1,  367,   -1,  369,   -1,  371,  372,  373,   -1,  375,
  376,   -1,  378,  379,   -1,  381,   -1,  383,  384,  385,
  386,  387,  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,
  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,   -1,
  406,   -1,  408,   -1,  410,   -1,  412,   -1,  414,   -1,
  416,   -1,  418,   -1,  420,   -1,  422,   -1,  424,   -1,
  426,   -1,  428,   -1,  430,  257,   -1,   -1,  434,  261,
   -1,   -1,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,
   -1,  273,  448,   -1,  276,   -1,  278,   -1,  280,   -1,
  282,   -1,   -1,   -1,   -1,   -1,  288,  289,   -1,   -1,
   -1,   -1,   -1,   -1,  296,  297,  298,   -1,   -1,  301,
  302,  303,   -1,  305,   -1,   -1,  308,   -1,  310,  311,
  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,   -1,
  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,
  332,   -1,  334,  335,  336,  337,  338,  339,   -1,   -1,
   -1,  343,   -1,   -1,   -1,   -1,  257,   -1,   -1,   -1,
  261,   -1,   -1,   -1,   -1,  266,   -1,  268,   -1,   -1,
  271,   -1,  273,  365,  366,  276,   -1,  278,   -1,  280,
   -1,  282,   -1,   -1,   -1,   -1,   -1,  288,  289,   -1,
   -1,   -1,   -1,   -1,   -1,  296,  297,  298,   -1,   -1,
  301,  302,  303,   -1,  305,   -1,   -1,  308,   -1,  310,
  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,
   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,
  331,  332,   -1,  334,  335,  336,  337,  338,  339,   -1,
   -1,   -1,  343,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  448,  257,   -1,   -1,
   -1,  261,   -1,   -1,  365,  366,  266,   -1,  268,   -1,
   -1,  271,   -1,  273,   -1,   -1,  276,   -1,  278,   -1,
  280,   -1,  282,   -1,   -1,  285,   -1,   -1,  288,  289,
   -1,   -1,   -1,   -1,   -1,   -1,  296,  297,  298,   -1,
   -1,  301,  302,  303,   -1,  305,   -1,   -1,  308,   -1,
  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,
   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,
   -1,  331,  332,   -1,  334,  335,   -1,  337,  338,  339,
   -1,   -1,   -1,  343,   -1,   -1,   -1,  448,  257,   -1,
   -1,   -1,  261,   -1,   -1,   -1,   -1,  266,   -1,  268,
   -1,   -1,  271,   -1,  273,  365,  366,  276,   -1,  278,
   -1,  280,   -1,  282,   -1,   -1,  285,   -1,   -1,  288,
  289,   -1,   -1,   -1,   -1,   -1,   -1,  296,  297,  298,
   -1,   -1,  301,  302,  303,   -1,  305,   -1,   -1,  308,
   -1,  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,
  319,   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,
   -1,   -1,  331,  332,   -1,  334,  335,   -1,  337,  338,
  339,   -1,   -1,   -1,  343,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  448,  257,
   -1,   -1,   -1,  261,   -1,   -1,  365,  366,  266,   -1,
  268,   -1,   -1,  271,   -1,  273,   -1,   -1,  276,   -1,
  278,   -1,  280,   -1,  282,   -1,   -1,  285,   -1,   -1,
  288,  289,   -1,   -1,   -1,   -1,   -1,   -1,  296,  297,
  298,   -1,   -1,  301,  302,  303,   -1,  305,   -1,   -1,
  308,   -1,  310,  311,  312,  313,   -1,   -1,   -1,  317,
  318,  319,   -1,   -1,  322,  323,  324,   -1,   -1,   -1,
   -1,   -1,   -1,  331,  332,   -1,  334,  335,   -1,  337,
  338,  339,   -1,  257,   -1,  343,   -1,  261,   -1,  448,
   -1,   -1,  266,   -1,  268,   -1,   -1,  271,   -1,  273,
   -1,   -1,  276,   -1,  278,   -1,  280,  365,  282,   -1,
   -1,  285,   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,
   -1,   -1,  296,  297,  298,   -1,   -1,  301,  302,  303,
   -1,  305,   -1,   -1,  308,   -1,  310,  311,  312,  313,
   -1,   -1,   -1,  317,  318,  319,   -1,   -1,  322,  323,
  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,
  334,  335,   -1,  337,  338,  339,   -1,   -1,   -1,  343,
   -1,   -1,  265,  266,  267,  268,   -1,   -1,  271,  272,
   -1,  274,  275,  276,   -1,  278,  279,  280,   -1,   -1,
  448,  365,   -1,  286,   -1,  288,  289,  290,  291,  292,
  293,   -1,   -1,  296,   -1,   -1,   -1,  300,  301,   -1,
  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  315,   -1,  317,   -1,  319,  320,   -1,   -1,
  323,   -1,  325,  326,  327,  328,  329,  330,  331,  332,
  333,  334,  335,  336,   -1,  338,   -1,   -1,  341,   -1,
   -1,   -1,   -1,  346,   -1,   -1,  266,   -1,  268,   -1,
   -1,  271,   -1,  273,   -1,   -1,  276,   -1,   -1,   -1,
  280,  364,   -1,  283,  448,  368,   -1,   -1,   -1,  289,
  373,  374,  375,  376,  377,   -1,  296,  297,   -1,   -1,
  383,  301,  385,   -1,   -1,  305,   -1,   -1,   -1,  392,
   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,
   -1,   -1,   -1,  323,  324,   -1,   -1,   -1,   -1,   -1,
   -1,  331,  332,   -1,   -1,  335,   -1,   -1,  338,   -1,
   -1,   -1,   -1,  265,  266,   -1,  268,   -1,   -1,  271,
  272,   -1,   -1,  436,  276,  438,  278,  440,  280,  442,
   -1,  444,   -1,  446,  286,  448,  449,  289,   -1,   -1,
   -1,  454,   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,
   -1,  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,  320,  321,
   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,
  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,
  342,   -1,   -1,   -1,  346,   -1,  265,  266,   -1,  268,
   -1,   -1,  271,  272,   -1,   -1,   -1,  276,   -1,  278,
   -1,  280,  364,  365,   -1,   -1,  368,  286,  448,   -1,
  289,   -1,  374,  375,  376,  377,   -1,  296,   -1,   -1,
   -1,  383,  301,  385,  303,  304,  305,   -1,  307,   -1,
  392,   -1,  394,   -1,   -1,  314,   -1,   -1,  317,   -1,
  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,
   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,
   -1,   -1,   -1,  342,   -1,   -1,   -1,  346,   -1,   -1,
   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,
  442,   -1,  444,   -1,  446,   -1,  448,  449,   -1,  368,
  369,   -1,  454,   -1,   -1,  374,  375,  376,  377,   -1,
   -1,   -1,   -1,   -1,  383,   -1,  385,   -1,   -1,   -1,
   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  265,  266,   -1,
  268,   -1,   -1,  271,  272,   -1,   -1,  436,  276,  438,
  278,  440,  280,  442,   -1,  444,   -1,  446,  286,  448,
  449,  289,   -1,   -1,   -1,  454,   -1,   -1,  296,   -1,
   -1,   -1,   -1,  301,   -1,  303,  304,  305,   -1,  307,
   -1,   -1,   -1,   -1,   -1,   -1,  314,   -1,   -1,  317,
   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,
  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,
  338,   -1,   -1,   -1,  342,   -1,   -1,   -1,  346,   -1,
   -1,  266,   -1,  268,   -1,   -1,  271,   -1,  273,   -1,
   -1,  276,   -1,   -1,   -1,  280,   -1,   -1,   -1,   -1,
  368,  369,   -1,   -1,  289,   -1,  374,  375,  376,  377,
   -1,  296,   -1,   -1,   -1,  383,  301,  385,  303,   -1,
  305,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,
   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,  323,  324,
   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,   -1,
  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,  265,  266,
   -1,  268,   -1,   -1,  271,  272,   -1,   -1,  436,  276,
  438,  278,  440,  280,  442,   -1,  444,   -1,  446,  286,
  448,  449,  289,   -1,   -1,   -1,  454,   -1,   -1,  296,
   -1,   -1,   -1,   -1,  301,   -1,  303,  304,  305,   -1,
  307,   -1,   -1,   -1,   -1,   -1,   -1,  314,   -1,   -1,
  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,
   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,
   -1,  338,   -1,   -1,   -1,  342,   -1,   -1,   -1,  346,
   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,
   -1,   -1,  276,   -1,  278,   -1,  280,   -1,   -1,   -1,
   -1,  368,  286,  448,   -1,  289,   -1,  374,  375,  376,
  377,   -1,  296,   -1,   -1,   -1,  383,  301,  385,  303,
  304,  305,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,  320,  321,   -1,  323,
   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,
   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,  342,   -1,
   -1,   -1,  346,   -1,   -1,   -1,   -1,   -1,   -1,  436,
   -1,  438,   -1,  440,   -1,  442,   -1,  444,   -1,  446,
  364,  448,  449,   -1,  368,   -1,   -1,  454,   -1,   -1,
  374,  375,  376,  377,   -1,   -1,   -1,   -1,   -1,  383,
   -1,  385,   -1,   -1,   -1,   -1,   -1,   -1,  392,   -1,
  394,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,
   -1,   -1,  436,  276,  438,  278,  440,  280,  442,   -1,
  444,   -1,  446,  286,  448,  449,  289,   -1,   -1,   -1,
  454,   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,
  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,
  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,
  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  346,   -1,   -1,  266,   -1,  268,   -1,
   -1,  271,   -1,   -1,   -1,   -1,  276,   -1,   -1,   -1,
  280,  364,  365,  283,   -1,  368,   -1,   -1,   -1,  289,
   -1,  374,  375,  376,  377,   -1,  296,   -1,   -1,   -1,
  383,  301,  385,   -1,   -1,  305,   -1,   -1,   -1,  392,
   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,
   -1,   -1,   -1,  323,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  331,  332,   -1,   -1,  335,   -1,   -1,  338,   -1,
   -1,   -1,   -1,  265,  266,   -1,  268,   -1,   -1,  271,
  272,   -1,   -1,  436,  276,  438,  278,  440,  280,  442,
   -1,  444,   -1,  446,  286,  448,  449,  289,   -1,   -1,
   -1,  454,   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,
   -1,  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,
   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,
  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  346,   -1,  265,  266,   -1,  268,
   -1,   -1,  271,  272,   -1,   -1,   -1,  276,   -1,  278,
   -1,  280,   -1,   -1,   -1,   -1,  368,  286,  448,  371,
  289,   -1,  374,  375,  376,  377,   -1,  296,   -1,   -1,
   -1,  383,  301,  385,  303,  304,  305,   -1,   -1,   -1,
  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,
  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,
   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  346,   -1,   -1,
   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,
  442,   -1,  444,   -1,  446,   -1,  448,  449,   -1,  368,
  369,   -1,  454,   -1,   -1,  374,  375,  376,  377,   -1,
   -1,   -1,   -1,   -1,  383,   -1,  385,   -1,   -1,   -1,
   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  265,  266,   -1,
  268,   -1,   -1,  271,  272,   -1,   -1,  436,  276,  438,
  278,  440,  280,  442,   -1,  444,   -1,  446,  286,  448,
  449,  289,   -1,   -1,   -1,  454,   -1,   -1,  296,   -1,
   -1,   -1,   -1,  301,   -1,  303,  304,  305,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  317,
   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,
  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,
  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  346,   -1,
  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,
   -1,  276,   -1,  278,   -1,  280,  364,   -1,   -1,   -1,
  368,  286,   -1,   -1,  289,   -1,  374,  375,  376,  377,
   -1,  296,   -1,   -1,   -1,  383,  301,  385,  303,  304,
  305,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,
   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,
   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,
  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  346,   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,
  438,   -1,  440,   -1,  442,   -1,  444,   -1,  446,   -1,
  448,  449,  367,  368,   -1,   -1,  454,   -1,   -1,  374,
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
   -1,   -1,   -1,   -1,  368,   -1,   -1,   -1,  289,  373,
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
  280,   -1,  365,   -1,   -1,  368,  286,  448,   -1,  289,
   -1,  374,  375,  376,  377,   -1,  296,   -1,   -1,   -1,
  383,  301,  385,  303,  304,  305,   -1,   -1,   -1,  392,
   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,
  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,
  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  346,   -1,   -1,   -1,
   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,  442,
   -1,  444,   -1,  446,  364,  448,  449,   -1,  368,   -1,
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
  276,   -1,  278,   -1,  280,   -1,   -1,   -1,   -1,  368,
  286,   -1,   -1,  289,   -1,  374,  375,  376,  377,   -1,
  296,   -1,   -1,   -1,  383,  301,  385,  303,  304,  305,
   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,
   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,
  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,
   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  346,   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,
   -1,  440,   -1,  442,   -1,  444,   -1,  446,   -1,  448,
  449,   -1,  368,   -1,   -1,  454,   -1,   -1,  374,  375,
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
   -1,  346,   -1,  265,  266,   -1,  268,   -1,   -1,  271,
  272,   -1,   -1,   -1,  276,   -1,  278,   -1,  280,   -1,
   -1,   -1,   -1,  368,  286,   -1,   -1,  289,   -1,  374,
  375,  376,  377,   -1,  296,   -1,   -1,   -1,  383,  301,
  385,  303,  304,  305,   -1,   -1,   -1,  392,   -1,  394,
   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,
   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,
  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  346,   -1,   -1,   -1,   -1,   -1,
   -1,  436,   -1,  438,   -1,  440,   -1,  442,   -1,  444,
   -1,  446,   -1,  448,  449,   -1,  368,   -1,   -1,  454,
   -1,   -1,  374,  375,  376,  377,   -1,   -1,   -1,   -1,
   -1,  383,   -1,  385,   -1,   -1,   -1,   -1,   -1,   -1,
  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  265,  266,   -1,  268,   -1,   -1,
  271,  272,   -1,   -1,  436,  276,  438,  278,  440,  280,
  442,   -1,  444,   -1,  446,  286,  448,  449,  289,   -1,
   -1,   -1,  454,   -1,   -1,  296,   -1,   -1,   -1,   -1,
  301,   -1,  303,  304,  305,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,  320,
   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,
  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  265,  266,   -1,
  268,   -1,   -1,  271,  272,   -1,   -1,   -1,  276,   -1,
  278,   -1,  280,   -1,   -1,   -1,   -1,  368,  286,   -1,
   -1,  289,   -1,  374,  375,  376,  377,   -1,  296,   -1,
   -1,   -1,  383,  301,  385,  303,  304,  305,   -1,   -1,
   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,
   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,
  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,
  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,  440,
   -1,  442,   -1,  444,   -1,  446,   -1,  448,   -1,   -1,
  368,   -1,   -1,  454,   -1,   -1,  374,  375,  376,  377,
   -1,   -1,   -1,   -1,   -1,  383,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  392,   -1,  394,  265,  266,   -1,
  268,   -1,   -1,  271,  272,   -1,   -1,   -1,  276,   -1,
  278,   -1,  280,   -1,   -1,   -1,   -1,   -1,  286,   -1,
   -1,  289,   -1,   -1,   -1,   -1,   -1,   -1,  296,   -1,
   -1,   -1,   -1,  301,   -1,  303,  304,  305,  436,   -1,
  438,   -1,  440,   -1,  442,   -1,  444,   -1,  446,  317,
  448,  319,  320,   -1,   -1,  323,  454,   -1,  326,   -1,
  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,
  338,   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,
   -1,   -1,   -1,  276,   -1,  278,   -1,  280,   -1,   -1,
   -1,   -1,   -1,  286,   -1,   -1,  289,   -1,   -1,   -1,
  368,   -1,   -1,  296,   -1,   -1,  374,   -1,  301,  377,
  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,
  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,
  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,
  438,   -1,  440,   -1,  442,  368,  444,   -1,  446,   -1,
  448,   -1,   -1,   -1,   -1,   -1,  454,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,
   -1,   -1,   -1,  276,   -1,  278,   -1,  280,   -1,   -1,
   -1,   -1,   -1,  286,   -1,   -1,  289,   -1,   -1,   -1,
   -1,   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,
  303,  304,  305,  436,  262,  438,   -1,  440,   -1,  442,
   -1,  444,   -1,  446,  317,  448,  319,  320,   -1,   -1,
  323,  454,   -1,  326,   -1,  328,   -1,  330,  331,  332,
  333,   -1,  335,  265,  266,  338,  268,   -1,   -1,  271,
  272,  299,   -1,   -1,  276,   -1,  278,   -1,  280,   -1,
   -1,   -1,   -1,   -1,  286,   -1,   -1,  289,   -1,   -1,
   -1,   -1,   -1,   -1,  296,  368,   -1,   -1,   -1,  301,
   -1,  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,
   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,
  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,
   -1,  369,   -1,  371,   -1,  373,   -1,  375,  376,   -1,
  378,  379,   -1,  381,   -1,  383,  384,   -1,  386,  387,
  388,   -1,   -1,  436,   -1,  438,  368,  440,  396,  442,
  398,  444,  400,  446,  402,  448,  404,   -1,  406,   -1,
  408,  454,  410,   -1,  412,   -1,  414,   -1,  416,   -1,
  418,   -1,  420,   -1,  422,   -1,  424,   -1,  426,   -1,
  428,   -1,  430,   -1,   -1,   -1,  434,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  448,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,
  442,   -1,  444,  261,  446,  263,  448,   -1,  266,   -1,
  268,   -1,  454,  271,   -1,  273,  274,   -1,  276,   -1,
  278,   -1,  280,   -1,  282,  283,  284,  285,   -1,   -1,
  288,  289,   -1,   -1,   -1,   -1,  294,   -1,  296,  297,
  298,   -1,   -1,  301,  302,  303,   -1,  305,   -1,  307,
  308,  309,  310,  311,  312,  313,  314,   -1,  316,  317,
  318,  319,   -1,   -1,  322,  323,  324,   -1,  326,   -1,
   -1,   -1,   -1,  331,  332,   -1,  334,  335,   -1,  337,
  338,  339,   -1,   -1,   -1,  343,   -1,  261,   -1,   -1,
   -1,   -1,  266,   -1,  268,   -1,   -1,  271,   -1,  273,
  274,  359,  276,  361,  278,   -1,  280,   -1,  282,  283,
  284,  285,   -1,   -1,  288,  289,  374,   -1,   -1,   -1,
  294,   -1,  296,  297,  298,   -1,   -1,  301,   -1,  303,
   -1,  305,   -1,   -1,  308,   -1,  310,  311,  312,  313,
   -1,   -1,   -1,  317,  318,  319,   -1,   -1,  322,  323,
  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,
  334,  335,   -1,  337,  338,  339,   -1,   -1,   -1,  343,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  261,   -1,   -1,   -1,   -1,  266,   -1,  268,
  448,  365,  271,   -1,  273,  274,   -1,  276,   -1,  278,
  374,  280,   -1,  282,  283,  284,  285,   -1,   -1,  288,
  289,   -1,   -1,   -1,   -1,  294,   -1,  296,  297,  298,
   -1,   -1,  301,   -1,  303,   -1,  305,   -1,   -1,  308,
   -1,  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,
  319,   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,
   -1,   -1,  331,  332,   -1,  334,  335,   -1,  337,  338,
  339,   -1,   -1,   -1,  343,   -1,  261,   -1,   -1,   -1,
   -1,  266,   -1,  268,  448,   -1,  271,   -1,  273,  274,
   -1,  276,   -1,  278,   -1,  280,  365,  282,  283,  284,
  285,   -1,   -1,  288,  289,  374,   -1,   -1,   -1,  294,
   -1,  296,  297,  298,   -1,   -1,  301,   -1,  303,   -1,
  305,   -1,   -1,  308,   -1,  310,  311,  312,  313,   -1,
   -1,   -1,  317,  318,  319,   -1,   -1,  322,  323,  324,
   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,  334,
  335,   -1,  337,  338,  339,   -1,   -1,   -1,  343,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  261,   -1,   -1,   -1,   -1,  266,   -1,  268,  448,
  365,  271,   -1,  273,  274,   -1,  276,   -1,  278,  374,
  280,   -1,  282,  283,  284,  285,   -1,   -1,  288,  289,
   -1,   -1,   -1,   -1,  294,   -1,  296,  297,  298,   -1,
   -1,  301,   -1,  303,   -1,  305,   -1,   -1,  308,   -1,
  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,
   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,
   -1,  331,  332,   -1,  334,  335,   -1,  337,  338,  339,
   -1,   -1,   -1,  343,   -1,  261,   -1,   -1,   -1,   -1,
  266,   -1,  268,  448,   -1,  271,   -1,  273,  274,   -1,
  276,   -1,  278,   -1,  280,  365,  282,  283,  284,  285,
   -1,   -1,  288,  289,  374,   -1,   -1,   -1,  294,   -1,
  296,  297,  298,   -1,   -1,  301,   -1,  303,   -1,  305,
   -1,   -1,  308,   -1,  310,  311,  312,  313,   -1,   -1,
   -1,  317,  318,  319,   -1,   -1,  322,  323,  324,   -1,
   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,  334,  335,
   -1,  337,  338,  339,   -1,   -1,   -1,  343,  261,   -1,
   -1,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,   -1,
  273,  274,   -1,  276,   -1,  278,   -1,  280,  448,  282,
  283,  284,  285,   -1,   -1,  288,  289,   -1,  374,   -1,
   -1,  294,   -1,  296,  297,  298,   -1,   -1,  301,   -1,
  303,   -1,  305,   -1,   -1,  308,   -1,  310,  311,  312,
  313,   -1,   -1,   -1,  317,  318,  319,   -1,   -1,  322,
  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,
   -1,  334,  335,   -1,  337,  338,  339,  261,   -1,   -1,
  343,   -1,  266,   -1,  268,   -1,   -1,  271,   -1,  273,
  274,   -1,  276,   -1,  278,   -1,  280,   -1,  282,  283,
   -1,  285,  448,   -1,   -1,  289,   -1,   -1,   -1,   -1,
   -1,  374,  296,  297,  298,   -1,   -1,  301,   -1,  303,
   -1,  305,   -1,   -1,  308,   -1,  310,  311,  312,  313,
   -1,   -1,   -1,  317,  318,  319,   -1,   -1,  322,  323,
  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,
  334,  335,   -1,  337,  338,  339,  261,   -1,   -1,  343,
   -1,  266,  262,  268,   -1,   -1,  271,   -1,  273,  274,
   -1,  276,   -1,  278,   -1,  280,   -1,  282,  283,   -1,
  285,  365,   -1,   -1,  289,  448,   -1,   -1,   -1,   -1,
   -1,  296,  297,  298,   -1,   -1,  301,   -1,  303,  299,
  305,   -1,   -1,  308,  261,  310,  311,  312,  313,   -1,
   -1,   -1,  317,  318,  319,   -1,   -1,  322,  323,  324,
   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,  285,  334,
  335,   -1,  337,  338,  339,   -1,   -1,   -1,  343,   -1,
   -1,  298,   -1,   -1,   -1,  302,  303,   -1,   -1,   -1,
   -1,  308,   -1,  310,  311,  312,  313,   -1,   -1,   -1,
  365,  318,   -1,   -1,  448,  322,  366,   -1,  368,   -1,
  370,   -1,   -1,  373,   -1,  375,  376,  334,  378,  379,
  337,  381,  339,  383,  384,  385,  386,  387,  388,  389,
   -1,   -1,  392,   -1,  394,   -1,  396,   -1,  398,   -1,
  400,   -1,  402,   -1,  404,   -1,  406,   -1,  408,   -1,
  410,   -1,  412,   -1,  414,   -1,  416,   -1,  418,   -1,
  420,   -1,  422,   -1,  424,   -1,  426,   -1,  428,   -1,
  430,   -1,  432,   -1,  434,   -1,   -1,  266,   -1,  268,
   -1,   -1,  271,  448,  273,  274,   -1,  276,  448,  278,
   -1,  280,   -1,  282,  283,  284,   -1,   -1,   -1,  288,
  289,   -1,   -1,   -1,   -1,  294,   -1,  296,  297,   -1,
   -1,   -1,  301,   -1,   -1,   -1,  305,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  317,   -1,
  319,   -1,   -1,   -1,  323,  324,   -1,   -1,   -1,   -1,
   -1,   -1,  331,  332,   -1,  266,  335,  268,   -1,  338,
  271,   -1,  273,  274,  343,  276,   -1,  278,   -1,  280,
   -1,  282,  283,  284,   -1,   -1,   -1,  288,  289,   -1,
  359,   -1,  361,  294,   -1,  296,  297,  266,   -1,  268,
  301,   -1,  271,   -1,  305,  274,   -1,  276,   -1,  278,
   -1,  280,   -1,  282,  283,  284,  317,   -1,  319,  288,
  289,   -1,  323,  324,   -1,  294,   -1,  296,   -1,   -1,
  331,  332,  301,   -1,  335,   -1,  305,  338,   -1,   -1,
   -1,   -1,  343,   -1,   -1,   -1,   -1,   -1,  317,   -1,
  319,   -1,   -1,   -1,  323,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  331,  332,   -1,   -1,  335,   -1,   -1,  338,
   -1,   -1,   -1,  374,  343,   -1,  266,   -1,  268,  448,
   -1,  271,   -1,  273,   -1,   -1,  276,   -1,  278,   -1,
  280,   -1,  282,   -1,   -1,   -1,   -1,   -1,  288,  289,
   -1,   -1,   -1,   -1,   -1,   -1,  296,  297,   -1,   -1,
   -1,  301,   -1,   -1,   -1,  305,  266,   -1,  268,   -1,
   -1,  271,   -1,   -1,   -1,   -1,  276,  317,   -1,  319,
  280,   -1,   -1,  323,  324,   -1,   -1,   -1,   -1,  289,
   -1,  331,  332,   -1,   -1,  335,  296,  448,  338,   -1,
   -1,  301,   -1,  343,   -1,  305,   -1,  307,   -1,  309,
   -1,  266,   -1,  268,  314,   -1,  271,  317,   -1,  319,
   -1,  276,   -1,  323,   -1,  280,  326,   -1,   -1,  448,
   -1,  331,  332,   -1,  289,  335,   -1,   -1,  338,   -1,
   -1,  296,  342,   -1,   -1,   -1,  301,   -1,   -1,   -1,
  305,   -1,  307,   -1,  309,   -1,   -1,   -1,   -1,  314,
   -1,   -1,  317,   -1,  319,   -1,  366,  367,  323,   -1,
   -1,  326,   -1,   -1,   -1,   -1,  331,  332,   -1,   -1,
  335,   -1,   -1,  338,   -1,   -1,  266,  342,  268,   -1,
   -1,  271,   -1,   -1,   -1,   -1,  276,   -1,   -1,   -1,
  280,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  448,  289,
  266,  366,  268,   -1,  369,  271,  296,   -1,   -1,   -1,
  276,  301,  278,   -1,  280,  305,  282,  307,   -1,  309,
   -1,   -1,  288,  289,  314,   -1,   -1,  317,   -1,  319,
  296,   -1,   -1,  323,   -1,  301,  326,   -1,  448,  305,
   -1,  331,  332,   -1,   -1,  335,   -1,   -1,  338,   -1,
  266,  317,  268,  319,   -1,  271,   -1,  323,   -1,   -1,
  276,   -1,   -1,   -1,  280,  331,  332,   -1,   -1,  335,
   -1,   -1,  338,  289,   -1,   -1,   -1,  343,   -1,  369,
  296,   -1,   -1,  448,   -1,  301,   -1,   -1,   -1,  305,
  266,  307,  268,  309,   -1,  271,   -1,   -1,  314,   -1,
  276,  317,   -1,  319,  280,   -1,   -1,  323,   -1,   -1,
  326,   -1,   -1,  289,   -1,  331,  332,   -1,   -1,  335,
  296,   -1,  338,   -1,   -1,  301,   -1,   -1,   -1,  305,
  266,   -1,  268,   -1,   -1,  271,   -1,   -1,   -1,   -1,
  276,  317,   -1,  319,  280,   -1,   -1,  323,   -1,   -1,
   -1,   -1,   -1,  289,   -1,  331,  332,   -1,  448,  335,
  296,   -1,  338,   -1,   -1,  301,   -1,   -1,   -1,  305,
  266,   -1,  268,   -1,   -1,  271,   -1,   -1,   -1,   -1,
  276,  317,  448,  319,  280,   -1,   -1,  323,   -1,   -1,
  366,   -1,   -1,  289,   -1,  331,  332,   -1,   -1,  335,
  296,   -1,  338,   -1,   -1,  301,   -1,   -1,  266,  305,
  268,   -1,   -1,  271,   -1,   -1,   -1,   -1,  276,   -1,
   -1,  317,  280,  319,   -1,   -1,   -1,  323,   -1,   -1,
   -1,  289,  448,   -1,   -1,  331,  332,   -1,  296,  335,
   -1,   -1,  338,  301,   -1,   -1,   -1,  305,  266,   -1,
  268,   -1,   -1,  271,   -1,   -1,   -1,   -1,  276,  317,
   -1,  319,  280,   -1,   -1,  323,   -1,   -1,   -1,   -1,
   -1,  289,  448,  331,  332,   -1,   -1,  335,  296,   -1,
  338,   -1,   -1,  301,   -1,   -1,   -1,  305,  266,   -1,
  268,   -1,   -1,  271,   -1,   -1,   -1,   -1,  276,  317,
   -1,  319,  280,   -1,   -1,  323,   -1,   -1,   -1,   -1,
   -1,  289,  448,  331,  332,   -1,   -1,  335,  296,   -1,
  338,   -1,   -1,  301,   -1,   -1,  266,  305,  268,   -1,
   -1,  271,   -1,   -1,   -1,   -1,  276,   -1,   -1,  317,
  280,  319,   -1,   -1,   -1,  323,   -1,   -1,   -1,  289,
   -1,   -1,  448,  331,  332,   -1,  296,  335,   -1,   -1,
  338,  301,   -1,  256,   -1,  305,  266,   -1,  268,  262,
   -1,  271,   -1,   -1,   -1,   -1,  276,  317,   -1,  319,
  280,   -1,   -1,  323,   -1,   -1,   -1,   -1,   -1,  289,
  448,  331,  332,   -1,   -1,  335,  296,   -1,  338,   -1,
   -1,  301,   -1,   -1,   -1,  305,  299,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,
   -1,   -1,   -1,  323,   -1,   -1,   -1,   -1,   -1,   -1,
  448,  331,  332,   -1,   -1,  335,   -1,   -1,  338,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  340,   -1,   -1,
   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,   -1,   -1,  256,   -1,   -1,
  448,   -1,  365,  262,  367,   -1,  369,   -1,  371,  372,
  373,   -1,  375,  376,   -1,  378,  379,   -1,  381,   -1,
  383,  384,  385,  386,  387,  388,  389,   -1,   -1,   -1,
   -1,   -1,   -1,  396,   -1,  398,   -1,  400,  448,  402,
  299,  404,   -1,  406,   -1,  408,   -1,  410,   -1,  412,
   -1,  414,   -1,  416,   -1,  418,   -1,  420,   -1,  422,
   -1,  424,   -1,  426,   -1,  428,   -1,  430,  256,   -1,
   -1,  434,   -1,   -1,  262,   -1,   -1,   -1,  448,   -1,
   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  365,   -1,  367,   -1,
  369,  299,  371,  372,  373,   -1,  375,  376,   -1,   -1,
  379,   -1,  381,   -1,  383,  384,  385,  386,  387,  388,
  389,   -1,   -1,   -1,   -1,   -1,   -1,  396,   -1,  398,
   -1,  400,   -1,  402,   -1,  404,   -1,  406,   -1,  408,
   -1,  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
  256,   -1,   -1,   -1,   -1,  434,  262,  365,   -1,  367,
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
   -1,  367,   -1,  369,  299,  371,  372,  373,   -1,  375,
  376,   -1,   -1,  379,   -1,  381,   -1,  383,  384,   -1,
   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,
  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,   -1,
  406,   -1,  408,   -1,  410,  340,   -1,   -1,   -1,   -1,
   -1,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,  256,   -1,   -1,   -1,   -1,  434,  262,
  365,   -1,  367,   -1,  369,   -1,  371,  372,  373,   -1,
  375,  376,   -1,   -1,  379,   -1,  381,   -1,  383,  384,
   -1,   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,
   -1,  396,   -1,  398,   -1,  400,  299,  402,   -1,  404,
   -1,  406,   -1,  408,   -1,  410,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  256,   -1,   -1,   -1,   -1,  434,
  262,   -1,   -1,   -1,   -1,   -1,   -1,  340,   -1,   -1,
   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  365,   -1,  367,   -1,  369,  299,  371,  372,
  373,   -1,   -1,   -1,   -1,   -1,  379,   -1,  381,   -1,
  383,  384,   -1,   -1,   -1,  388,  389,   -1,   -1,   -1,
   -1,   -1,   -1,  396,   -1,  398,   -1,  400,   -1,  402,
  256,  404,   -1,  406,   -1,  408,  262,  410,  340,   -1,
   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,
   -1,  434,   -1,  365,   -1,  367,   -1,  369,   -1,  371,
  372,  373,   -1,  299,   -1,   -1,   -1,  379,   -1,  381,
   -1,  383,  384,   -1,   -1,   -1,  388,  389,   -1,   -1,
   -1,   -1,   -1,   -1,  396,   -1,  398,   -1,  400,   -1,
  402,  256,  404,   -1,  406,   -1,  408,  262,  410,   -1,
   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,  434,   -1,   -1,   -1,   -1,   -1,   -1,  365,
   -1,  367,   -1,  369,  299,  371,  372,  373,   -1,   -1,
   -1,   -1,   -1,  379,   -1,  381,   -1,  383,  384,   -1,
   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,
  396,   -1,  398,   -1,  400,   -1,  402,  256,  404,   -1,
  406,   -1,  408,  262,  410,  340,   -1,   -1,   -1,   -1,
   -1,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,   -1,   -1,   -1,   -1,   -1,  434,   -1,
  365,   -1,  367,   -1,  369,   -1,  371,  372,  373,   -1,
  299,   -1,   -1,   -1,  379,   -1,  381,   -1,  383,  384,
   -1,   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  400,   -1,  402,  256,  404,
   -1,  406,   -1,  408,  262,  410,   -1,   -1,   -1,   -1,
   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,  434,
   -1,   -1,   -1,   -1,   -1,   -1,  365,   -1,  367,   -1,
  369,  299,  371,  372,  373,   -1,   -1,   -1,   -1,   -1,
  379,   -1,  381,   -1,  383,  384,   -1,   -1,   -1,  388,
  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  400,   -1,  402,  256,  404,   -1,  406,   -1,  408,
  262,  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
   -1,   -1,   -1,   -1,   -1,  434,   -1,  365,   -1,  367,
   -1,  369,   -1,  371,  372,  373,   -1,  299,   -1,   -1,
   -1,  379,   -1,  381,   -1,  383,  384,   -1,   -1,   -1,
  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  400,   -1,  402,  256,  404,   -1,  406,   -1,
  408,  262,  410,   -1,   -1,   -1,   -1,   -1,  340,   -1,
   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,  434,   -1,   -1,   -1,
   -1,   -1,   -1,  365,   -1,  367,   -1,  369,  299,  371,
  372,  373,   -1,   -1,   -1,   -1,   -1,  379,   -1,  381,
   -1,  383,  384,   -1,   -1,   -1,  388,  389,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  256,   -1,  400,   -1,
  402,   -1,  404,   -1,  406,   -1,  408,   -1,  410,  340,
   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,
  351,  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,
   -1,   -1,  434,   -1,  365,   -1,  367,   -1,  369,   -1,
  371,  372,  373,   -1,   -1,   -1,   -1,   -1,  379,   -1,
  381,   -1,  383,  384,   -1,   -1,   -1,  388,  389,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  256,   -1,  400,
   -1,  402,   -1,  404,   -1,  406,   -1,  408,   -1,  410,
  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,   -1,   -1,
   -1,   -1,   -1,  434,   -1,  365,   -1,  367,   -1,  369,
   -1,  371,  372,  373,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  383,  384,   -1,   -1,   -1,  388,  389,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  256,   -1,
   -1,   -1,   -1,   -1,  404,   -1,  406,   -1,  408,   -1,
  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,   -1,
   -1,   -1,   -1,   -1,  434,   -1,  365,   -1,  367,   -1,
  369,   -1,  371,  372,  373,  256,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  383,  384,   -1,   -1,   -1,  388,
  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  404,   -1,  406,   -1,  408,
   -1,  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
   -1,   -1,   -1,   -1,   -1,  434,   -1,  365,   -1,  367,
   -1,  369,   -1,  371,  372,  373,  256,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  383,  384,   -1,   -1,  340,
  388,  389,   -1,   -1,   -1,  346,  347,  348,  349,  350,
  351,  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,
  408,   -1,  410,   -1,  365,   -1,  367,   -1,  369,   -1,
  371,  372,  373,  256,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  383,  384,   -1,   -1,  434,  388,  389,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  408,   -1,  410,
  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,   -1,   -1,
   -1,   -1,   -1,  434,   -1,  365,   -1,  367,   -1,  369,
   -1,  371,  372,  373,  256,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  384,   -1,   -1,  340,  388,  389,
   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,   -1,   -1,   -1,  408,   -1,
  410,   -1,  365,   -1,  367,   -1,  369,   -1,  371,  372,
  373,  256,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  384,   -1,   -1,  434,  388,  389,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  408,   -1,  410,  340,   -1,
   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,
   -1,  434,   -1,  365,   -1,  367,   -1,  369,   -1,  371,
  372,  373,  256,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  384,   -1,   -1,  340,   -1,  389,   -1,   -1,
   -1,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,   -1,   -1,   -1,  408,   -1,  410,   -1,
  365,   -1,  367,   -1,  369,   -1,  371,  372,  373,  256,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  384,
   -1,   -1,  434,   -1,  389,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  256,   -1,   -1,
   -1,   -1,   -1,  408,   -1,  410,  340,   -1,   -1,   -1,
   -1,   -1,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,  434,
   -1,  365,   -1,  367,   -1,  369,   -1,  371,  372,  373,
   -1,  256,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  340,   -1,  389,   -1,   -1,   -1,  346,
  347,  348,  349,  350,  351,  352,  353,  354,  355,  356,
  357,   -1,   -1,   -1,  408,   -1,  410,   -1,  365,   -1,
  367,  340,  369,   -1,  371,  372,  373,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,   -1,
  434,   -1,  389,   -1,   -1,   -1,  365,  262,  367,   -1,
  369,   -1,  371,  372,  373,   -1,   -1,   -1,   -1,   -1,
   -1,  408,   -1,  410,   -1,  340,   -1,   -1,   -1,  261,
  389,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,   -1,  299,   -1,   -1,  434,   -1,   -1,
  365,  410,  367,  285,  369,   -1,  371,  372,  373,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  298,   -1,   -1,   -1,
   -1,  303,   -1,   -1,  389,  434,  308,   -1,  310,  311,
  312,  313,   -1,   -1,  316,   -1,  318,   -1,   -1,   -1,
  322,   -1,   -1,   -1,   -1,  410,   -1,   -1,  262,   -1,
   -1,   -1,  334,   -1,   -1,  337,   -1,  339,   -1,   -1,
   -1,  366,   -1,  368,  369,  370,  371,   -1,   -1,  434,
  375,  376,   -1,   -1,  379,   -1,  381,   -1,  383,  384,
  385,  386,  387,  388,  389,  299,   -1,  392,   -1,  394,
   -1,  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,
   -1,  406,   -1,  408,   -1,  410,   -1,  412,  262,  414,
   -1,  416,   -1,  418,   -1,  420,   -1,  422,   -1,  424,
   -1,  426,   -1,  428,   -1,  430,  340,  432,   -1,  434,
   -1,   -1,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,   -1,  299,   -1,   -1,   -1,   -1,
   -1,  365,   -1,  367,   -1,  369,   -1,  371,  372,  373,
   -1,   -1,   -1,   -1,   -1,  379,   -1,  381,   -1,  383,
  384,   -1,   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  400,   -1,  402,   -1,
  404,   -1,  406,   -1,  408,   -1,  410,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  365,  366,   -1,  368,   -1,  370,  371,   -1,   -1,
  434,  375,  376,   -1,   -1,  379,   -1,  381,   -1,  383,
  384,  385,  386,  387,  388,  389,   -1,  261,  392,   -1,
  394,   -1,  396,   -1,  398,   -1,  400,   -1,  402,   -1,
  404,   -1,  406,   -1,  408,   -1,  410,   -1,   -1,   -1,
   -1,  285,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  261,   -1,  298,   -1,   -1,   -1,  432,  303,
  434,   -1,  306,   -1,  308,   -1,  310,  311,  312,  313,
   -1,   -1,   -1,   -1,  318,   -1,  285,   -1,  322,   -1,
   -1,   -1,  326,   -1,   -1,   -1,   -1,   -1,   -1,  298,
  334,   -1,   -1,  337,  303,  339,   -1,   -1,   -1,  308,
   -1,  310,  311,  312,  313,   -1,   -1,   -1,   -1,  318,
   -1,   -1,  261,  322,  263,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  366,   -1,   -1,  334,   -1,   -1,  337,   -1,
  339,   -1,   -1,   -1,   -1,   -1,  285,   -1,   -1,   -1,
   -1,   -1,  261,   -1,  263,  389,   -1,   -1,   -1,  298,
  359,   -1,  361,   -1,  303,   -1,  365,   -1,   -1,  308,
   -1,  310,  311,  312,  313,   -1,  285,  316,   -1,  318,
   -1,   -1,  261,  322,  263,   -1,   -1,   -1,   -1,  298,
   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,  308,
  339,  310,  311,  312,  313,   -1,  285,  316,   -1,  318,
   -1,   -1,  261,  322,   -1,   -1,   -1,   -1,   -1,  298,
   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,  308,
  339,  310,  311,  312,  313,   -1,  285,   -1,   -1,  318,
   -1,   -1,  261,  322,  263,   -1,   -1,   -1,   -1,  298,
   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,  308,
  339,  310,  311,  312,  313,   -1,  285,  316,   -1,  318,
   -1,   -1,  261,  322,   -1,   -1,   -1,   -1,   -1,  298,
   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,  308,
  339,  310,  311,  312,  313,   -1,  285,   -1,   -1,  318,
   -1,   -1,  261,  322,   -1,   -1,   -1,   -1,   -1,  298,
   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,  308,
  339,  310,  311,  312,  313,   -1,  285,   -1,   -1,  318,
   -1,   -1,   -1,  322,   -1,   -1,   -1,   -1,   -1,  298,
   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,  308,
  339,  310,  311,  312,  313,   -1,   -1,   -1,   -1,  318,
   -1,   -1,   -1,  322,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  334,   -1,   -1,  337,   -1,
  339,
  };

#line 5627 "cs-parser.jay"

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
	public FullNamedExpression type;
	public MemberName interface_type;
	public Parameters param_list;
	public Location location;

	public IndexerDeclaration (FullNamedExpression type, MemberName interface_type,
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
	public readonly FullNamedExpression ret_type;
	public readonly Location location;

	public OperatorDeclaration (Operator.OpType op, FullNamedExpression ret_type, Location location)
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
