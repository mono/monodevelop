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
    "struct_body : OPEN_BRACE opt_struct_member_declarations CLOSE_BRACE",
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
    "$$8 :",
    "method_declaration : method_header $$8 method_body",
    "opt_error_modifier :",
    "opt_error_modifier : modifiers",
    "open_parens : OPEN_PARENS",
    "open_parens : OPEN_PARENS_LAMBDA",
    "$$9 :",
    "$$10 :",
    "method_header : opt_attributes opt_modifiers type member_name open_parens $$9 opt_formal_parameter_list CLOSE_PARENS $$10 opt_type_parameter_constraints_clauses",
    "$$11 :",
    "$$12 :",
    "method_header : opt_attributes opt_modifiers VOID member_name open_parens $$11 opt_formal_parameter_list CLOSE_PARENS $$12 opt_type_parameter_constraints_clauses",
    "$$13 :",
    "method_header : opt_attributes opt_modifiers PARTIAL VOID member_name open_parens opt_formal_parameter_list CLOSE_PARENS $$13 opt_type_parameter_constraints_clauses",
    "method_header : opt_attributes opt_modifiers type modifiers member_name open_parens opt_formal_parameter_list CLOSE_PARENS",
    "method_body : block",
    "method_body : SEMICOLON",
    "opt_formal_parameter_list :",
    "opt_formal_parameter_list : formal_parameter_list",
    "opt_parameter_list_no_mod :",
    "$$14 :",
    "opt_parameter_list_no_mod : $$14 formal_parameter_list",
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
    "$$15 :",
    "$$16 :",
    "$$17 :",
    "property_declaration : opt_attributes opt_modifiers type namespace_or_type_name $$15 OPEN_BRACE $$16 accessor_declarations $$17 CLOSE_BRACE",
    "accessor_declarations : get_accessor_declaration",
    "accessor_declarations : get_accessor_declaration accessor_declarations",
    "accessor_declarations : set_accessor_declaration",
    "accessor_declarations : set_accessor_declaration accessor_declarations",
    "accessor_declarations : error",
    "$$18 :",
    "get_accessor_declaration : opt_attributes opt_modifiers GET $$18 accessor_body",
    "$$19 :",
    "set_accessor_declaration : opt_attributes opt_modifiers SET $$19 accessor_body",
    "accessor_body : block",
    "accessor_body : SEMICOLON",
    "$$20 :",
    "$$21 :",
    "$$22 :",
    "$$23 :",
    "interface_declaration : opt_attributes opt_modifiers opt_partial INTERFACE $$20 type_name $$21 opt_class_base opt_type_parameter_constraints_clauses $$22 interface_body $$23 opt_semicolon",
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
    "$$24 :",
    "interface_method_declaration_body : OPEN_BRACE $$24 opt_statement_list CLOSE_BRACE",
    "interface_method_declaration_body : SEMICOLON",
    "$$25 :",
    "$$26 :",
    "interface_method_declaration : opt_attributes opt_new type namespace_or_type_name open_parens opt_formal_parameter_list CLOSE_PARENS $$25 opt_type_parameter_constraints_clauses $$26 interface_method_declaration_body",
    "$$27 :",
    "$$28 :",
    "interface_method_declaration : opt_attributes opt_new VOID namespace_or_type_name open_parens opt_formal_parameter_list CLOSE_PARENS $$27 opt_type_parameter_constraints_clauses $$28 interface_method_declaration_body",
    "$$29 :",
    "$$30 :",
    "interface_property_declaration : opt_attributes opt_new type IDENTIFIER OPEN_BRACE $$29 accessor_declarations $$30 CLOSE_BRACE",
    "interface_property_declaration : opt_attributes opt_new type error",
    "interface_event_declaration : opt_attributes opt_new EVENT type IDENTIFIER SEMICOLON",
    "interface_event_declaration : opt_attributes opt_new EVENT type error",
    "interface_event_declaration : opt_attributes opt_new EVENT type IDENTIFIER ASSIGN",
    "$$31 :",
    "$$32 :",
    "interface_event_declaration : opt_attributes opt_new EVENT type IDENTIFIER OPEN_BRACE $$31 event_accessor_declarations $$32 CLOSE_BRACE",
    "$$33 :",
    "$$34 :",
    "interface_indexer_declaration : opt_attributes opt_new type THIS OPEN_BRACKET opt_parameter_list_no_mod CLOSE_BRACKET OPEN_BRACE $$33 accessor_declarations $$34 CLOSE_BRACE",
    "$$35 :",
    "operator_declaration : opt_attributes opt_modifiers operator_declarator $$35 operator_body",
    "operator_body : block",
    "operator_body : SEMICOLON",
    "$$36 :",
    "operator_declarator : type OPERATOR overloadable_operator open_parens $$36 opt_parameter_list_no_mod CLOSE_PARENS",
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
    "$$37 :",
    "conversion_operator_declarator : IMPLICIT OPERATOR type open_parens $$37 opt_parameter_list_no_mod CLOSE_PARENS",
    "$$38 :",
    "conversion_operator_declarator : EXPLICIT OPERATOR type open_parens $$38 opt_parameter_list_no_mod CLOSE_PARENS",
    "conversion_operator_declarator : IMPLICIT error",
    "conversion_operator_declarator : EXPLICIT error",
    "constructor_declaration : opt_attributes opt_modifiers constructor_declarator constructor_body",
    "constructor_declarator : constructor_header",
    "constructor_declarator : constructor_header constructor_initializer",
    "$$39 :",
    "constructor_header : IDENTIFIER $$39 open_parens opt_formal_parameter_list CLOSE_PARENS",
    "constructor_body : block_prepared",
    "constructor_body : SEMICOLON",
    "constructor_initializer : COLON BASE open_parens opt_argument_list CLOSE_PARENS",
    "constructor_initializer : COLON THIS open_parens opt_argument_list CLOSE_PARENS",
    "constructor_initializer : COLON error",
    "opt_finalizer :",
    "opt_finalizer : UNSAFE",
    "opt_finalizer : EXTERN",
    "$$40 :",
    "destructor_declaration : opt_attributes opt_finalizer TILDE $$40 IDENTIFIER OPEN_PARENS CLOSE_PARENS block",
    "event_declaration : opt_attributes opt_modifiers EVENT type variable_declarators SEMICOLON",
    "$$41 :",
    "$$42 :",
    "event_declaration : opt_attributes opt_modifiers EVENT type namespace_or_type_name OPEN_BRACE $$41 event_accessor_declarations $$42 CLOSE_BRACE",
    "event_declaration : opt_attributes opt_modifiers EVENT type namespace_or_type_name error",
    "event_accessor_declarations : add_accessor_declaration remove_accessor_declaration",
    "event_accessor_declarations : remove_accessor_declaration add_accessor_declaration",
    "event_accessor_declarations : add_accessor_declaration",
    "event_accessor_declarations : remove_accessor_declaration",
    "event_accessor_declarations : error",
    "event_accessor_declarations :",
    "$$43 :",
    "add_accessor_declaration : opt_attributes ADD $$43 block",
    "add_accessor_declaration : opt_attributes ADD error",
    "add_accessor_declaration : opt_attributes modifiers ADD",
    "$$44 :",
    "remove_accessor_declaration : opt_attributes REMOVE $$44 block",
    "remove_accessor_declaration : opt_attributes REMOVE error",
    "remove_accessor_declaration : opt_attributes modifiers REMOVE",
    "$$45 :",
    "$$46 :",
    "indexer_declaration : opt_attributes opt_modifiers indexer_declarator OPEN_BRACE $$45 accessor_declarations $$46 CLOSE_BRACE",
    "indexer_declarator : type THIS OPEN_BRACKET opt_parameter_list_no_mod CLOSE_BRACKET",
    "indexer_declarator : type namespace_or_type_name DOT THIS OPEN_BRACKET opt_formal_parameter_list CLOSE_BRACKET",
    "$$47 :",
    "$$48 :",
    "enum_declaration : opt_attributes opt_modifiers ENUM IDENTIFIER opt_enum_base $$47 enum_body $$48 opt_semicolon",
    "opt_enum_base :",
    "opt_enum_base : COLON type",
    "enum_body : OPEN_BRACE opt_enum_member_declarations CLOSE_BRACE",
    "opt_enum_member_declarations :",
    "opt_enum_member_declarations : enum_member_declarations opt_comma",
    "enum_member_declarations : enum_member_declaration",
    "enum_member_declarations : enum_member_declarations COMMA enum_member_declaration",
    "enum_member_declaration : opt_attributes IDENTIFIER",
    "$$49 :",
    "enum_member_declaration : opt_attributes IDENTIFIER $$49 ASSIGN expression",
    "$$50 :",
    "$$51 :",
    "delegate_declaration : opt_attributes opt_modifiers DELEGATE type type_name open_parens opt_formal_parameter_list CLOSE_PARENS $$50 opt_type_parameter_constraints_clauses $$51 SEMICOLON",
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
    "$$52 :",
    "typeof_expression : TYPEOF $$52 OPEN_PARENS typeof_type_expression CLOSE_PARENS",
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
    "$$53 :",
    "anonymous_method_expression : DELEGATE opt_anonymous_method_signature $$53 block",
    "opt_anonymous_method_signature :",
    "opt_anonymous_method_signature : anonymous_method_signature",
    "$$54 :",
    "anonymous_method_signature : open_parens $$54 opt_formal_parameter_list CLOSE_PARENS",
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
    "$$55 :",
    "relational_expression : relational_expression IS $$55 nullable_type_or_conditional",
    "$$56 :",
    "relational_expression : relational_expression AS $$56 nullable_type_or_conditional",
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
    "$$57 :",
    "lambda_expression_body : $$57 expression",
    "lambda_expression_body : block",
    "$$58 :",
    "lambda_expression : IDENTIFIER ARROW $$58 lambda_expression_body",
    "$$59 :",
    "lambda_expression : OPEN_PARENS_LAMBDA opt_lambda_parameter_list CLOSE_PARENS ARROW $$59 lambda_expression_body",
    "expression : assignment_expression",
    "expression : non_assignment_expression",
    "non_assignment_expression : conditional_expression",
    "non_assignment_expression : lambda_expression",
    "non_assignment_expression : query_expression",
    "constant_expression : expression",
    "boolean_expression : expression",
    "$$60 :",
    "$$61 :",
    "$$62 :",
    "$$63 :",
    "class_declaration : opt_attributes opt_modifiers opt_partial CLASS $$60 type_name $$61 opt_class_base opt_type_parameter_constraints_clauses $$62 class_body $$63 opt_semicolon",
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
    "$$64 :",
    "block : OPEN_BRACE $$64 opt_statement_list CLOSE_BRACE",
    "$$65 :",
    "block_prepared : OPEN_BRACE $$65 opt_statement_list CLOSE_BRACE",
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
    "$$66 :",
    "labeled_statement : IDENTIFIER COLON $$66 statement",
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
    "$$67 :",
    "switch_statement : SWITCH OPEN_PARENS $$67 expression CLOSE_PARENS switch_block",
    "switch_block : OPEN_BRACE opt_switch_sections CLOSE_BRACE",
    "opt_switch_sections :",
    "opt_switch_sections : switch_sections",
    "switch_sections : switch_section",
    "switch_sections : switch_sections switch_section",
    "$$68 :",
    "switch_section : switch_labels $$68 statement_list",
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
    "$$69 :",
    "for_statement : FOR open_parens opt_for_initializer SEMICOLON $$69 opt_for_condition SEMICOLON opt_for_iterator CLOSE_PARENS embedded_statement",
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
    "$$70 :",
    "foreach_statement : FOREACH open_parens type IDENTIFIER IN expression CLOSE_PARENS $$70 embedded_statement",
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
    "$$71 :",
    "catch_clause : CATCH opt_catch_args $$71 block",
    "opt_catch_args :",
    "opt_catch_args : catch_args",
    "catch_args : open_parens type opt_identifier CLOSE_PARENS",
    "checked_statement : CHECKED block",
    "unchecked_statement : UNCHECKED block",
    "$$72 :",
    "unsafe_statement : UNSAFE $$72 block",
    "$$73 :",
    "fixed_statement : FIXED open_parens type fixed_pointer_declarators CLOSE_PARENS $$73 embedded_statement",
    "fixed_pointer_declarators : fixed_pointer_declarator",
    "fixed_pointer_declarators : fixed_pointer_declarators COMMA fixed_pointer_declarator",
    "fixed_pointer_declarator : IDENTIFIER ASSIGN expression",
    "fixed_pointer_declarator : IDENTIFIER",
    "$$74 :",
    "lock_statement : LOCK OPEN_PARENS expression CLOSE_PARENS $$74 embedded_statement",
    "$$75 :",
    "using_statement : USING open_parens local_variable_declaration CLOSE_PARENS $$75 embedded_statement",
    "$$76 :",
    "using_statement : USING open_parens expression CLOSE_PARENS $$76 embedded_statement",
    "$$77 :",
    "query_expression : first_from_clause $$77 query_body",
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
    "$$78 :",
    "opt_query_continuation : INTO IDENTIFIER $$78 query_body",
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
case 81:
#line 818 "cs-parser.jay"
  {
		((TypeContainer)current_class).MembersBlock = new Dom.LocationBlock (GetLocation (yyVals[-2+yyTop]), GetLocation (yyVals[0+yyTop]));
	  }
  break;
case 96:
#line 852 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 97:
#line 856 "cs-parser.jay"
  { 
		MemberName name = MakeName ((MemberName) yyVals[0+yyTop]);
		push_current_class (new Struct (current_namespace, current_class, name, (int) yyVals[-4+yyTop], (Attributes) yyVals[-5+yyTop]), yyVals[-3+yyTop]);
	  }
  break;
case 98:
#line 862 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		current_class.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);

		if (RootContext.Documentation != null) {
			current_container.DocComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 99:
#line 873 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 100:
#line 878 "cs-parser.jay"
  {
		yyVal = pop_current_class ();
	  }
  break;
case 101:
#line 881 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
	  }
  break;
case 102:
#line 888 "cs-parser.jay"
  {
		((TypeContainer)current_class).MembersBlock = new Dom.LocationBlock (GetLocation (yyVals[-2+yyTop]), GetLocation (yyVals[0+yyTop]));
	  }
  break;
case 117:
#line 928 "cs-parser.jay"
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
case 118:
#line 953 "cs-parser.jay"
  {
		ArrayList constants = new ArrayList (4);
		if (yyVals[0+yyTop] != null)
			constants.Add (yyVals[0+yyTop]);
		yyVal = constants;
	  }
  break;
case 119:
#line 960 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			ArrayList constants = (ArrayList) yyVals[-2+yyTop];
			constants.Add (yyVals[0+yyTop]);
		}
	  }
  break;
case 120:
#line 970 "cs-parser.jay"
  {
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[-2+yyTop], yyVals[0+yyTop]);
	  }
  break;
case 121:
#line 974 "cs-parser.jay"
  {
		/* A const field requires a value to be provided*/
		Report.Error (145, ((LocatedToken) yyVals[0+yyTop]).Location, "A const field requires a value to be provided");
		yyVal = null;
	  }
  break;
case 122:
#line 987 "cs-parser.jay"
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
case 123:
#line 1013 "cs-parser.jay"
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
case 124:
#line 1036 "cs-parser.jay"
  {
		Report.Error (1641, GetLocation (yyVals[-1+yyTop]), "A fixed size buffer field must have the array size specifier after the field name");
	  }
  break;
case 125:
#line 1043 "cs-parser.jay"
  {
		current_array_type = null;
		Report.Error (670, (Location) yyVals[-2+yyTop], "Fields cannot have void type");
	  }
  break;
case 126:
#line 1051 "cs-parser.jay"
  {
		ArrayList decl = new ArrayList (2);
		decl.Add (yyVals[0+yyTop]);
		yyVal = decl;
  	  }
  break;
case 127:
#line 1057 "cs-parser.jay"
  {
		ArrayList decls = (ArrayList) yyVals[-2+yyTop];
		decls.Add (yyVals[0+yyTop]);
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 128:
#line 1066 "cs-parser.jay"
  {
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[-3+yyTop], yyVals[-1+yyTop]);
	  }
  break;
case 129:
#line 1070 "cs-parser.jay"
  {
		Report.Error (443, lexer.Location, "Value or constant expected");
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[-2+yyTop], null);
	  }
  break;
case 130:
#line 1078 "cs-parser.jay"
  {
		ArrayList decl = new ArrayList (4);
		if (yyVals[0+yyTop] != null)
			decl.Add (yyVals[0+yyTop]);
		yyVal = decl;
	  }
  break;
case 131:
#line 1085 "cs-parser.jay"
  {
		ArrayList decls = (ArrayList) yyVals[-2+yyTop];
		decls.Add (yyVals[0+yyTop]);
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 132:
#line 1094 "cs-parser.jay"
  {
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[-2+yyTop], yyVals[0+yyTop]);
	  }
  break;
case 133:
#line 1098 "cs-parser.jay"
  {
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[0+yyTop], null);
	  }
  break;
case 134:
#line 1102 "cs-parser.jay"
  {
		Report.Error (650, ((LocatedToken) yyVals[-3+yyTop]).Location, "Syntax error, bad array declarator. To declare a managed array the rank specifier precedes the variable's identifier. " +
			"To declare a fixed size buffer field, use the fixed keyword before the field type");
		yyVal = null;
	  }
  break;
case 135:
#line 1111 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 136:
#line 1115 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 137:
#line 1119 "cs-parser.jay"
  {
		yyVal = new StackAlloc ((Expression) yyVals[-3+yyTop], (Expression) yyVals[-1+yyTop], (Location) yyVals[-4+yyTop]);
	  }
  break;
case 138:
#line 1123 "cs-parser.jay"
  {
		yyVal = new ArglistAccess ((Location) yyVals[0+yyTop]);
	  }
  break;
case 139:
#line 1127 "cs-parser.jay"
  {
		Report.Error (1575, (Location) yyVals[-1+yyTop], "A stackalloc expression requires [] after type");
                yyVal = null;
	  }
  break;
case 140:
#line 1134 "cs-parser.jay"
  {
		anonymous_host = (IAnonymousHost) yyVals[0+yyTop];
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.NotAllowed;
	  }
  break;
case 141:
#line 1140 "cs-parser.jay"
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
case 143:
#line 1157 "cs-parser.jay"
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
case 146:
#line 1189 "cs-parser.jay"
  {
		arglist_allowed = true;
	  }
  break;
case 147:
#line 1193 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 148:
#line 1197 "cs-parser.jay"
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
case 149:
#line 1232 "cs-parser.jay"
  {
		arglist_allowed = true;
	  }
  break;
case 150:
#line 1236 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 151:
#line 1240 "cs-parser.jay"
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
case 152:
#line 1276 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 153:
#line 1280 "cs-parser.jay"
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
case 154:
#line 1333 "cs-parser.jay"
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
case 156:
#line 1352 "cs-parser.jay"
  { yyVal = null; }
  break;
case 157:
#line 1356 "cs-parser.jay"
  { yyVal = Parameters.EmptyReadOnlyParameters; }
  break;
case 159:
#line 1361 "cs-parser.jay"
  { yyVal = Parameters.EmptyReadOnlyParameters; }
  break;
case 160:
#line 1363 "cs-parser.jay"
  {
		parameter_modifiers_not_allowed = true;
	  }
  break;
case 161:
#line 1367 "cs-parser.jay"
  {
		parameter_modifiers_not_allowed = false;
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 162:
#line 1375 "cs-parser.jay"
  { 
		ArrayList pars_list = (ArrayList) yyVals[0+yyTop];

		Parameter [] pars = new Parameter [pars_list.Count];
		pars_list.CopyTo (pars);

	  	yyVal = new Parameters (pars); 
	  }
  break;
case 163:
#line 1384 "cs-parser.jay"
  {
		ArrayList pars_list = (ArrayList) yyVals[-2+yyTop];
		pars_list.Add (yyVals[0+yyTop]);

		Parameter [] pars = new Parameter [pars_list.Count];
		pars_list.CopyTo (pars);

		yyVal = new Parameters (pars); 
	  }
  break;
case 164:
#line 1394 "cs-parser.jay"
  {
		ArrayList pars_list = (ArrayList) yyVals[-2+yyTop];
		/*pars_list.Add (new ArglistParameter (GetLocation ($3)));*/

		Parameter [] pars = new Parameter [pars_list.Count];
		pars_list.CopyTo (pars);

		yyVal = new Parameters (pars, true);
	  }
  break;
case 165:
#line 1404 "cs-parser.jay"
  {
		if (yyVals[-2+yyTop] != null)
			Report.Error (231, ((Parameter) yyVals[-2+yyTop]).Location, "A params parameter must be the last parameter in a formal parameter list");
		yyVal = null;
	  }
  break;
case 166:
#line 1410 "cs-parser.jay"
  {
		if (yyVals[-2+yyTop] != null)
			Report.Error (231, ((Parameter) yyVals[-2+yyTop]).Location, "A params parameter must be the last parameter in a formal parameter list");
		yyVal = null;
	  }
  break;
case 167:
#line 1416 "cs-parser.jay"
  {
		Report.Error (257, (Location) yyVals[-2+yyTop], "An __arglist parameter must be the last parameter in a formal parameter list");
		yyVal = null;
	  }
  break;
case 168:
#line 1421 "cs-parser.jay"
  {
		Report.Error (257, (Location) yyVals[-2+yyTop], "An __arglist parameter must be the last parameter in a formal parameter list");
		yyVal = null;
	  }
  break;
case 169:
#line 1426 "cs-parser.jay"
  {
		yyVal = new Parameters (new Parameter[] { (Parameter) yyVals[0+yyTop] } );
	  }
  break;
case 170:
#line 1430 "cs-parser.jay"
  {
		yyVal = new Parameters (new Parameter[0], true);
	  }
  break;
case 171:
#line 1437 "cs-parser.jay"
  {
		ArrayList pars = new ArrayList (4);

		pars.Add (yyVals[0+yyTop]);
		yyVal = pars;
	  }
  break;
case 172:
#line 1444 "cs-parser.jay"
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
case 173:
#line 1461 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		yyVal = new Parameter ((FullNamedExpression) yyVals[-1+yyTop], lt.Value, (Parameter.Modifier) yyVals[-2+yyTop], (Attributes) yyVals[-3+yyTop], lt.Location);
	  }
  break;
case 174:
#line 1469 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		Report.Error (1552, lt.Location, "Array type specifier, [], must appear before parameter name");
		yyVal = null;
	  }
  break;
case 175:
#line 1477 "cs-parser.jay"
  {
		Report.Error (1001, GetLocation (yyVals[0+yyTop]), "Identifier expected");
		yyVal = null;
	  }
  break;
case 176:
#line 1484 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 177:
#line 1494 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		Report.Error (241, lt.Location, "Default parameter specifiers are not permitted");
		 yyVal = null;
	   }
  break;
case 178:
#line 1502 "cs-parser.jay"
  { yyVal = Parameter.Modifier.NONE; }
  break;
case 180:
#line 1508 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 181:
#line 1512 "cs-parser.jay"
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
case 182:
#line 1536 "cs-parser.jay"
  {
	  	if (parameter_modifiers_not_allowed)
	  		Error_ParameterModifierNotValid ("ref", (Location)yyVals[0+yyTop]);
	  		
	  	yyVal = Parameter.Modifier.REF;
	  }
  break;
case 183:
#line 1543 "cs-parser.jay"
  {
	  	if (parameter_modifiers_not_allowed)
	  		Error_ParameterModifierNotValid ("out", (Location)yyVals[0+yyTop]);
	  
	  	yyVal = Parameter.Modifier.OUT;
	  }
  break;
case 184:
#line 1550 "cs-parser.jay"
  {
		if (parameter_modifiers_not_allowed)
	  		Error_ParameterModifierNotValid ("this", (Location)yyVals[0+yyTop]);

	  	if (RootContext.Version <= LanguageVersion.ISO_2)
	  		Report.FeatureIsNotAvailable (GetLocation (yyVals[0+yyTop]), "extension methods");
	  			
		yyVal = Parameter.Modifier.This;
	  }
  break;
case 185:
#line 1563 "cs-parser.jay"
  { 
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		yyVal = new ParamsParameter ((FullNamedExpression) yyVals[-1+yyTop], lt.Value, (Attributes) yyVals[-3+yyTop], lt.Location);
	  }
  break;
case 186:
#line 1567 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 187:
#line 1575 "cs-parser.jay"
  {
		if (params_modifiers_not_allowed)
			Report.Error (1670, ((Location) yyVals[0+yyTop]), "The `params' modifier is not allowed in current context");
	  }
  break;
case 188:
#line 1580 "cs-parser.jay"
  {
		Parameter.Modifier mod = (Parameter.Modifier)yyVals[0+yyTop];
		if ((mod & Parameter.Modifier.This) != 0) {
			Report.Error (1104, (Location)yyVals[-1+yyTop], "The parameter modifiers `this' and `params' cannot be used altogether");
		} else {
			Report.Error (1611, (Location)yyVals[-1+yyTop], "The params parameter cannot be declared as ref or out");
		}	  
	  }
  break;
case 189:
#line 1589 "cs-parser.jay"
  {
		Error_DuplicateParameterModifier ((Location)yyVals[-1+yyTop], Parameter.Modifier.PARAMS);
	  }
  break;
case 190:
#line 1596 "cs-parser.jay"
  {
	  	if (!arglist_allowed)
	  		Report.Error (1669, (Location) yyVals[0+yyTop], "__arglist is not valid in this context");
	  }
  break;
case 191:
#line 1607 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			tmpComment = Lexer.consume_doc_comment ();
	  }
  break;
case 192:
#line 1612 "cs-parser.jay"
  {
		implicit_value_parameter_type = (FullNamedExpression) yyVals[-3+yyTop];

		lexer.PropertyParsing = true;
	  }
  break;
case 193:
#line 1618 "cs-parser.jay"
  {
		lexer.PropertyParsing = false;
		has_get = has_set = false;
	  }
  break;
case 194:
#line 1623 "cs-parser.jay"
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
case 195:
#line 1651 "cs-parser.jay"
  {
		yyVal = new Accessors ((Accessor) yyVals[0+yyTop], null);
	 }
  break;
case 196:
#line 1655 "cs-parser.jay"
  { 
		Accessors accessors = (Accessors) yyVals[0+yyTop];
		accessors.get_or_add = (Accessor) yyVals[-1+yyTop];
		yyVal = accessors;
	 }
  break;
case 197:
#line 1661 "cs-parser.jay"
  {
		yyVal = new Accessors (null, (Accessor) yyVals[0+yyTop]);
	 }
  break;
case 198:
#line 1665 "cs-parser.jay"
  { 
		Accessors accessors = (Accessors) yyVals[0+yyTop];
		accessors.set_or_remove = (Accessor) yyVals[-1+yyTop];
		accessors.declared_in_reverse = true;
		yyVal = accessors;
	 }
  break;
case 199:
#line 1672 "cs-parser.jay"
  {
		Report.Error (1014, GetLocation (yyVals[0+yyTop]), "A get or set accessor expected");
		yyVal = null;
	  }
  break;
case 200:
#line 1680 "cs-parser.jay"
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
case 201:
#line 1692 "cs-parser.jay"
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
case 202:
#line 1715 "cs-parser.jay"
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
case 203:
#line 1745 "cs-parser.jay"
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
case 205:
#line 1768 "cs-parser.jay"
  { yyVal = null; }
  break;
case 206:
#line 1776 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 207:
#line 1780 "cs-parser.jay"
  {
		MemberName name = MakeName ((MemberName) yyVals[0+yyTop]);
		push_current_class (new Interface (current_namespace, current_class, name, (int) yyVals[-4+yyTop], (Attributes) yyVals[-5+yyTop]), yyVals[-3+yyTop]);
	  }
  break;
case 208:
#line 1786 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		current_class.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);

		if (RootContext.Documentation != null) {
			current_container.DocComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 209:
#line 1797 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 210:
#line 1802 "cs-parser.jay"
  {
		yyVal = pop_current_class ();
	  }
  break;
case 211:
#line 1805 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
	  }
  break;
case 212:
#line 1812 "cs-parser.jay"
  {
		((TypeContainer)current_class).MembersBlock = new Dom.LocationBlock (GetLocation (yyVals[-2+yyTop]), GetLocation (yyVals[0+yyTop]));
	  }
  break;
case 217:
#line 1829 "cs-parser.jay"
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
case 218:
#line 1845 "cs-parser.jay"
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
case 219:
#line 1861 "cs-parser.jay"
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
case 220:
#line 1876 "cs-parser.jay"
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
case 221:
#line 1892 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 222:
#line 1899 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 223:
#line 1906 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 224:
#line 1913 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 225:
#line 1920 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 226:
#line 1927 "cs-parser.jay"
  {
		Report.Error (525, GetLocation (yyVals[0+yyTop]), "Interfaces cannot contain fields or constants");
	  }
  break;
case 227:
#line 1934 "cs-parser.jay"
  {
		int val = (int) yyVals[0+yyTop];
		val = Modifiers.Check (Modifiers.NEW | Modifiers.UNSAFE, val, 0, GetLocation (yyVals[0+yyTop]));
		yyVal = val;
	  }
  break;
case 228:
#line 1943 "cs-parser.jay"
  {
		Report.Error (531, (Location)yyVals[0+yyTop],
			      "`{0}.{1}{2}': interface members cannot have a definition",
			      current_class.GetSignatureForError (),
			      ((MemberName) yyVals[-1+yyTop]).GetSignatureForError (),
			      ((Parameters)yyVals[-5+yyTop]).GetSignatureForError ());
	  
		lexer.ConstraintsParsing = false;
	  }
  break;
case 229:
#line 1953 "cs-parser.jay"
  {
		yyVal = null;
	  }
  break;
case 231:
#line 1962 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 232:
#line 1966 "cs-parser.jay"
  {
		/* Refer to the name as $-1 in interface_method_declaration_body	  */
		yyVal = yyVals[-5+yyTop];
	  }
  break;
case 233:
#line 1971 "cs-parser.jay"
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
case 234:
#line 1995 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 235:
#line 1999 "cs-parser.jay"
  {
		yyVal = yyVals[-5+yyTop];
	  }
  break;
case 236:
#line 2003 "cs-parser.jay"
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
case 237:
#line 2032 "cs-parser.jay"
  {
		lexer.PropertyParsing = true;
		implicit_value_parameter_type = (FullNamedExpression)yyVals[-2+yyTop];
	  }
  break;
case 238:
#line 2037 "cs-parser.jay"
  {
		has_get = has_set = false; 
		lexer.PropertyParsing = false;
		implicit_value_parameter_type = null;
	  }
  break;
case 239:
#line 2043 "cs-parser.jay"
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
case 240:
#line 2086 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 241:
#line 2095 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new EventField (current_class, (FullNamedExpression) yyVals[-2+yyTop], (int) yyVals[-4+yyTop], true,
				     new MemberName (lt.Value, lt.Location),
				     (Attributes) yyVals[-5+yyTop]);
		if (RootContext.Documentation != null)
			((EventField) yyVal).DocComment = Lexer.consume_doc_comment ();
	  }
  break;
case 242:
#line 2103 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 243:
#line 2107 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		Report.Error (68, lt.Location, "`{0}.{1}': event in interface cannot have initializer", current_container.Name, lt.Value);
		yyVal = null;
	  }
  break;
case 244:
#line 2113 "cs-parser.jay"
  {
		implicit_value_parameter_type = (FullNamedExpression) yyVals[-2+yyTop];
		lexer.EventParsing = true;
	  }
  break;
case 245:
#line 2118 "cs-parser.jay"
  {
		lexer.EventParsing = false;
		implicit_value_parameter_type = null;
	  }
  break;
case 246:
#line 2122 "cs-parser.jay"
  {
		Report.Error (69, (Location) yyVals[-7+yyTop], "Event in interface cannot have add or remove accessors");
 		yyVal = null;
 	  }
  break;
case 247:
#line 2132 "cs-parser.jay"
  {
		lexer.PropertyParsing = true;
		implicit_value_parameter_type = (FullNamedExpression)yyVals[-5+yyTop];
	  }
  break;
case 248:
#line 2137 "cs-parser.jay"
  { 
		has_get = has_set = false;
 		lexer.PropertyParsing = false;
 		implicit_value_parameter_type = null;
	  }
  break;
case 249:
#line 2143 "cs-parser.jay"
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
case 250:
#line 2182 "cs-parser.jay"
  {
		anonymous_host = SimpleAnonymousHost.GetSimple ();
	  }
  break;
case 251:
#line 2186 "cs-parser.jay"
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
case 253:
#line 2213 "cs-parser.jay"
  { yyVal = null; }
  break;
case 254:
#line 2218 "cs-parser.jay"
  {
		params_modifiers_not_allowed = true;
	  }
  break;
case 255:
#line 2222 "cs-parser.jay"
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
case 257:
#line 2265 "cs-parser.jay"
  { yyVal = Operator.OpType.LogicalNot; }
  break;
case 258:
#line 2266 "cs-parser.jay"
  { yyVal = Operator.OpType.OnesComplement; }
  break;
case 259:
#line 2267 "cs-parser.jay"
  { yyVal = Operator.OpType.Increment; }
  break;
case 260:
#line 2268 "cs-parser.jay"
  { yyVal = Operator.OpType.Decrement; }
  break;
case 261:
#line 2269 "cs-parser.jay"
  { yyVal = Operator.OpType.True; }
  break;
case 262:
#line 2270 "cs-parser.jay"
  { yyVal = Operator.OpType.False; }
  break;
case 263:
#line 2272 "cs-parser.jay"
  { yyVal = Operator.OpType.Addition; }
  break;
case 264:
#line 2273 "cs-parser.jay"
  { yyVal = Operator.OpType.Subtraction; }
  break;
case 265:
#line 2275 "cs-parser.jay"
  { yyVal = Operator.OpType.Multiply; }
  break;
case 266:
#line 2276 "cs-parser.jay"
  {  yyVal = Operator.OpType.Division; }
  break;
case 267:
#line 2277 "cs-parser.jay"
  { yyVal = Operator.OpType.Modulus; }
  break;
case 268:
#line 2278 "cs-parser.jay"
  { yyVal = Operator.OpType.BitwiseAnd; }
  break;
case 269:
#line 2279 "cs-parser.jay"
  { yyVal = Operator.OpType.BitwiseOr; }
  break;
case 270:
#line 2280 "cs-parser.jay"
  { yyVal = Operator.OpType.ExclusiveOr; }
  break;
case 271:
#line 2281 "cs-parser.jay"
  { yyVal = Operator.OpType.LeftShift; }
  break;
case 272:
#line 2282 "cs-parser.jay"
  { yyVal = Operator.OpType.RightShift; }
  break;
case 273:
#line 2283 "cs-parser.jay"
  { yyVal = Operator.OpType.Equality; }
  break;
case 274:
#line 2284 "cs-parser.jay"
  { yyVal = Operator.OpType.Inequality; }
  break;
case 275:
#line 2285 "cs-parser.jay"
  { yyVal = Operator.OpType.GreaterThan; }
  break;
case 276:
#line 2286 "cs-parser.jay"
  { yyVal = Operator.OpType.LessThan; }
  break;
case 277:
#line 2287 "cs-parser.jay"
  { yyVal = Operator.OpType.GreaterThanOrEqual; }
  break;
case 278:
#line 2288 "cs-parser.jay"
  { yyVal = Operator.OpType.LessThanOrEqual; }
  break;
case 279:
#line 2293 "cs-parser.jay"
  {
		params_modifiers_not_allowed = true;
	  }
  break;
case 280:
#line 2297 "cs-parser.jay"
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
case 281:
#line 2311 "cs-parser.jay"
  {
		params_modifiers_not_allowed = true;
	  }
  break;
case 282:
#line 2315 "cs-parser.jay"
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
case 283:
#line 2329 "cs-parser.jay"
  {
		syntax_error ((Location) yyVals[-1+yyTop], "'operator' expected");
	  }
  break;
case 284:
#line 2333 "cs-parser.jay"
  {
		syntax_error ((Location) yyVals[-1+yyTop], "'operator' expected");
	  }
  break;
case 285:
#line 2343 "cs-parser.jay"
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
case 286:
#line 2378 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 287:
#line 2382 "cs-parser.jay"
  {
		((Constructor)yyVals[-1+yyTop]).Initializer = (ConstructorInitializer) yyVals[0+yyTop];
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 288:
#line 2390 "cs-parser.jay"
  {
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 289:
#line 2397 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-4+yyTop];
		current_local_parameters = (Parameters) yyVals[-1+yyTop];
		current_block = new ToplevelBlock (null, current_local_parameters, null, lt.Location);

		yyVal = new Constructor (current_class, lt.Value, 0, current_local_parameters,
				      null, lt.Location);

		anonymous_host = (IAnonymousHost) yyVal;
	  }
  break;
case 291:
#line 2411 "cs-parser.jay"
  { current_block = null; yyVal = null; }
  break;
case 292:
#line 2416 "cs-parser.jay"
  {
		yyVal = new ConstructorBaseInitializer ((ArrayList) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 293:
#line 2420 "cs-parser.jay"
  {
		yyVal = new ConstructorThisInitializer ((ArrayList) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 294:
#line 2423 "cs-parser.jay"
  {
		Report.Error (1018, (Location) yyVals[-1+yyTop], "Keyword this or base expected");
		yyVal = null;
	  }
  break;
case 295:
#line 2430 "cs-parser.jay"
  { yyVal = 0; }
  break;
case 296:
#line 2431 "cs-parser.jay"
  { yyVal = Modifiers.UNSAFE; }
  break;
case 297:
#line 2432 "cs-parser.jay"
  { yyVal = Modifiers.EXTERN; }
  break;
case 298:
#line 2437 "cs-parser.jay"
  {
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.NotAllowed;
		}
	  }
  break;
case 299:
#line 2444 "cs-parser.jay"
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
case 300:
#line 2475 "cs-parser.jay"
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
case 301:
#line 2500 "cs-parser.jay"
  {
		implicit_value_parameter_type = (FullNamedExpression) yyVals[-2+yyTop];  
		lexer.EventParsing = true;
	  }
  break;
case 302:
#line 2505 "cs-parser.jay"
  {
		lexer.EventParsing = false;  
	  }
  break;
case 303:
#line 2509 "cs-parser.jay"
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
case 304:
#line 2539 "cs-parser.jay"
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
case 305:
#line 2554 "cs-parser.jay"
  {
		yyVal = new Accessors ((Accessor) yyVals[-1+yyTop], (Accessor) yyVals[0+yyTop]);
	  }
  break;
case 306:
#line 2558 "cs-parser.jay"
  {
		Accessors accessors = new Accessors ((Accessor) yyVals[0+yyTop], (Accessor) yyVals[-1+yyTop]);
		accessors.declared_in_reverse = true;
		yyVal = accessors;
	  }
  break;
case 307:
#line 2563 "cs-parser.jay"
  { yyVal = null; }
  break;
case 308:
#line 2564 "cs-parser.jay"
  { yyVal = null; }
  break;
case 309:
#line 2566 "cs-parser.jay"
  { 
		Report.Error (1055, GetLocation (yyVals[0+yyTop]), "An add or remove accessor expected");
		yyVal = null;
	  }
  break;
case 310:
#line 2570 "cs-parser.jay"
  { yyVal = null; }
  break;
case 311:
#line 2575 "cs-parser.jay"
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
case 312:
#line 2589 "cs-parser.jay"
  {
		Accessor accessor = new Accessor ((ToplevelBlock) yyVals[0+yyTop], 0, (Attributes) yyVals[-3+yyTop], (Location) yyVals[-2+yyTop]);
		lexer.EventParsing = true;
		
		current_local_parameters = null;
		SimpleAnonymousHost.Simple.Propagate (accessor);
		anonymous_host = null;
		
		yyVal = accessor;
	  }
  break;
case 313:
#line 2599 "cs-parser.jay"
  {
		Report.Error (73, (Location) yyVals[-1+yyTop], "An add or remove accessor must have a body");
		yyVal = null;
	  }
  break;
case 314:
#line 2603 "cs-parser.jay"
  {
		Report.Error (1609, (Location) yyVals[0+yyTop], "Modifiers cannot be placed on event accessor declarations");
		yyVal = null;
	  }
  break;
case 315:
#line 2611 "cs-parser.jay"
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
case 316:
#line 2623 "cs-parser.jay"
  {
		yyVal = new Accessor ((ToplevelBlock) yyVals[0+yyTop], 0, (Attributes) yyVals[-3+yyTop], (Location) yyVals[-2+yyTop]);
		lexer.EventParsing = true;
	  }
  break;
case 317:
#line 2627 "cs-parser.jay"
  {
		Report.Error (73, (Location) yyVals[-1+yyTop], "An add or remove accessor must have a body");
		yyVal = null;
	  }
  break;
case 318:
#line 2631 "cs-parser.jay"
  {
		Report.Error (1609, (Location) yyVals[0+yyTop], "Modifiers cannot be placed on event accessor declarations");
		yyVal = null;
	  }
  break;
case 319:
#line 2640 "cs-parser.jay"
  {
		IndexerDeclaration decl = (IndexerDeclaration) yyVals[-1+yyTop];

		implicit_value_parameter_type = decl.type;
		
		lexer.PropertyParsing = true;
		parsing_indexer  = true;
		
		indexer_parameters = decl.param_list;
		anonymous_host = SimpleAnonymousHost.GetSimple ();
	  }
  break;
case 320:
#line 2652 "cs-parser.jay"
  {
		  lexer.PropertyParsing = false;
		  has_get = has_set = false;
		  parsing_indexer  = false;
	  }
  break;
case 321:
#line 2658 "cs-parser.jay"
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
case 322:
#line 2694 "cs-parser.jay"
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
case 323:
#line 2707 "cs-parser.jay"
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
case 324:
#line 2727 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		Location enum_location = lt.Location;

		MemberName name = MakeName (new MemberName (lt.Value, enum_location));
		push_current_class (new Enum (current_namespace, current_class, (FullNamedExpression) yyVals[0+yyTop], (int) yyVals[-3+yyTop], name, (Attributes) yyVals[-4+yyTop]), null);

		if (RootContext.Documentation != null) {
			enumTypeComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 325:
#line 2740 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 326:
#line 2745 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			current_class.DocComment = enumTypeComment;

		/* TODO: use curent_class inside enum_body and delete redundant arraylist allocation*/
		Enum e = (Enum)current_class;
		EnumMember em = null;
		foreach (VariableDeclaration ev in (ArrayList) yyVals[-2+yyTop]) {
			em = new EnumMember (
				e, em, ev.identifier, (Expression) ev.expression_or_array_initializer,
				ev.OptAttributes, ev.Location);

/*			if (RootContext.Documentation != null)*/
				em.DocComment = ev.DocComment;

			e.AddEnumMember (em);
		}

		yyVal = pop_current_class ();
	  }
  break;
case 327:
#line 2768 "cs-parser.jay"
  { yyVal = TypeManager.system_int32_expr; }
  break;
case 328:
#line 2769 "cs-parser.jay"
  { yyVal = yyVals[0+yyTop];   }
  break;
case 329:
#line 2774 "cs-parser.jay"
  {
		((TypeContainer)current_class).MembersBlock = new Dom.LocationBlock (GetLocation (yyVals[-2+yyTop]), GetLocation (yyVals[0+yyTop]));
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 330:
#line 2781 "cs-parser.jay"
  { yyVal = new ArrayList (4); }
  break;
case 331:
#line 2782 "cs-parser.jay"
  { yyVal = yyVals[-1+yyTop]; }
  break;
case 332:
#line 2787 "cs-parser.jay"
  {
		ArrayList l = new ArrayList (4);

		l.Add (yyVals[0+yyTop]);
		yyVal = l;
	  }
  break;
case 333:
#line 2794 "cs-parser.jay"
  {
		ArrayList l = (ArrayList) yyVals[-2+yyTop];

		l.Add (yyVals[0+yyTop]);

		yyVal = l;
	  }
  break;
case 334:
#line 2805 "cs-parser.jay"
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
case 335:
#line 2817 "cs-parser.jay"
  {
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.NotAllowed;
		}
	  }
  break;
case 336:
#line 2824 "cs-parser.jay"
  { 
		VariableDeclaration vd = new VariableDeclaration (
			(LocatedToken) yyVals[-3+yyTop], yyVals[0+yyTop], (Attributes) yyVals[-4+yyTop]);

		if (RootContext.Documentation != null)
			vd.DocComment = ConsumeStoredComment ();

		yyVal = vd;
	  }
  break;
case 337:
#line 2841 "cs-parser.jay"
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
case 338:
#line 2859 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;
	  }
  break;
case 339:
#line 2863 "cs-parser.jay"
  {
		current_delegate.SetParameterInfo ((ArrayList) yyVals[-2+yyTop]);
		yyVal = current_delegate;

		current_delegate = null;
	  }
  break;
case 340:
#line 2873 "cs-parser.jay"
  {
		lexer.CheckNullable (false);
		yyVal = false;
	  }
  break;
case 341:
#line 2878 "cs-parser.jay"
  {
	  	/* FIXME: A hack with parsing conditional operator as nullable type*/
		/*if (RootContext.Version < LanguageVersion.ISO_2)*/
		/*	Report.FeatureIsNotAvailable (lexer.Location, "nullable types");*/
	  		
		lexer.CheckNullable (true);
		yyVal = true;
	  }
  break;
case 342:
#line 2890 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName (lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 343:
#line 2894 "cs-parser.jay"
  {
		LocatedToken lt1 = (LocatedToken) yyVals[-3+yyTop];
		LocatedToken lt2 = (LocatedToken) yyVals[-1+yyTop];
		if (RootContext.Version == LanguageVersion.ISO_1)
			Report.FeatureIsNotAvailable (lt1.Location, "namespace alias qualifier");
		
		yyVal = new MemberName (lt1.Value, lt2.Value, (TypeArguments) yyVals[0+yyTop], lt1.Location);
	  }
  break;
case 344:
#line 2902 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName ((MemberName) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 345:
#line 2910 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName (lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 346:
#line 2915 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName ((MemberName) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 347:
#line 2923 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName (lt.Value, (TypeArguments)yyVals[0+yyTop], lt.Location);	  
	  }
  break;
case 348:
#line 2934 "cs-parser.jay"
  { yyVal = null; }
  break;
case 349:
#line 2936 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-2+yyTop]), "generics");	  
	  
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 350:
#line 2949 "cs-parser.jay"
  { yyVal = null; }
  break;
case 351:
#line 2951 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-2+yyTop]), "generics");
	  
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 352:
#line 2961 "cs-parser.jay"
  {
		TypeArguments type_args = new TypeArguments (lexer.Location);
		type_args.Add ((Expression) yyVals[0+yyTop]);
		yyVal = type_args;
	  }
  break;
case 353:
#line 2967 "cs-parser.jay"
  {
		TypeArguments type_args = (TypeArguments) yyVals[-2+yyTop];
		type_args.Add ((Expression) yyVals[0+yyTop]);
		yyVal = type_args;
	  }
  break;
case 354:
#line 2976 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
  	  }
  break;
case 355:
#line 2980 "cs-parser.jay"
  {
		SimpleName sn = yyVals[0+yyTop] as SimpleName;
		if (sn == null)
			Error_TypeExpected (GetLocation (yyVals[0+yyTop]));
		else
			yyVals[0+yyTop] = new TypeParameterName (sn.Name, (Attributes) yyVals[-1+yyTop], lexer.Location);
		yyVal = yyVals[0+yyTop];  	  
  	  }
  break;
case 356:
#line 2998 "cs-parser.jay"
  {
		MemberName name = (MemberName) yyVals[-1+yyTop];

		if ((bool) yyVals[0+yyTop]) {
			yyVal = new ComposedCast (name.GetTypeExpression (), "?", lexer.Location);
		} else {
			if (RootContext.Version > LanguageVersion.ISO_2 && name.Left == null && name.Name == "var")
				yyVal = new VarExpr (name.Location);
			else
				yyVal = name.GetTypeExpression ();
		}
	  }
  break;
case 357:
#line 3011 "cs-parser.jay"
  {
		if ((bool) yyVals[0+yyTop])
			yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], "?", lexer.Location);
	  }
  break;
case 360:
#line 3021 "cs-parser.jay"
  {
		/**/
		/* Note that here only unmanaged types are allowed but we*/
		/* can't perform checks during this phase - we do it during*/
		/* semantic analysis.*/
		/**/
		yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], "*", Lexer.Location);
	  }
  break;
case 361:
#line 3030 "cs-parser.jay"
  {
		yyVal = new ComposedCast (TypeManager.system_void_expr, "*", (Location) yyVals[-1+yyTop]);
	  }
  break;
case 362:
#line 3037 "cs-parser.jay"
  {
		if ((bool) yyVals[0+yyTop])
			yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], "?", lexer.Location);
	  }
  break;
case 363:
#line 3042 "cs-parser.jay"
  {
		Location loc = GetLocation (yyVals[-1+yyTop]);
		if (loc.IsNull)
			loc = lexer.Location;
		yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], (string) yyVals[0+yyTop], loc);
	  }
  break;
case 364:
#line 3049 "cs-parser.jay"
  {
		Location loc = GetLocation (yyVals[-1+yyTop]);
		if (loc.IsNull)
			loc = lexer.Location;
		yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], "*", loc);
	  }
  break;
case 365:
#line 3061 "cs-parser.jay"
  {
		FullNamedExpression e = yyVals[-1+yyTop] as FullNamedExpression;
		if (e != null)
			yyVal = new ComposedCast (e, "*");
		else
			Error_TypeExpected (GetLocation (yyVals[-1+yyTop]));
	  }
  break;
case 366:
#line 3072 "cs-parser.jay"
  {
		ArrayList types = new ArrayList (2);
		types.Add (yyVals[0+yyTop]);
		yyVal = types;
	  }
  break;
case 367:
#line 3078 "cs-parser.jay"
  {
		ArrayList types = (ArrayList) yyVals[-2+yyTop];
		types.Add (yyVals[0+yyTop]);
		yyVal = types;
	  }
  break;
case 368:
#line 3087 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] is ComposedCast)
			Report.Error (1521, GetLocation (yyVals[0+yyTop]), "Invalid base type `{0}'", ((ComposedCast)yyVals[0+yyTop]).GetSignatureForError ());
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 369:
#line 3099 "cs-parser.jay"
  { yyVal = TypeManager.system_object_expr; }
  break;
case 370:
#line 3100 "cs-parser.jay"
  { yyVal = TypeManager.system_string_expr; }
  break;
case 371:
#line 3101 "cs-parser.jay"
  { yyVal = TypeManager.system_boolean_expr; }
  break;
case 372:
#line 3102 "cs-parser.jay"
  { yyVal = TypeManager.system_decimal_expr; }
  break;
case 373:
#line 3103 "cs-parser.jay"
  { yyVal = TypeManager.system_single_expr; }
  break;
case 374:
#line 3104 "cs-parser.jay"
  { yyVal = TypeManager.system_double_expr; }
  break;
case 376:
#line 3109 "cs-parser.jay"
  { yyVal = TypeManager.system_sbyte_expr; }
  break;
case 377:
#line 3110 "cs-parser.jay"
  { yyVal = TypeManager.system_byte_expr; }
  break;
case 378:
#line 3111 "cs-parser.jay"
  { yyVal = TypeManager.system_int16_expr; }
  break;
case 379:
#line 3112 "cs-parser.jay"
  { yyVal = TypeManager.system_uint16_expr; }
  break;
case 380:
#line 3113 "cs-parser.jay"
  { yyVal = TypeManager.system_int32_expr; }
  break;
case 381:
#line 3114 "cs-parser.jay"
  { yyVal = TypeManager.system_uint32_expr; }
  break;
case 382:
#line 3115 "cs-parser.jay"
  { yyVal = TypeManager.system_int64_expr; }
  break;
case 383:
#line 3116 "cs-parser.jay"
  { yyVal = TypeManager.system_uint64_expr; }
  break;
case 384:
#line 3117 "cs-parser.jay"
  { yyVal = TypeManager.system_char_expr; }
  break;
case 385:
#line 3118 "cs-parser.jay"
  { yyVal = TypeManager.system_void_expr; }
  break;
case 386:
#line 3123 "cs-parser.jay"
  {
		string rank_specifiers = (string) yyVals[-1+yyTop];
		if ((bool) yyVals[0+yyTop])
			rank_specifiers += "?";

		yyVal = current_array_type = new ComposedCast ((FullNamedExpression) yyVals[-2+yyTop], rank_specifiers);
	  }
  break;
case 387:
#line 3137 "cs-parser.jay"
  {
		/* 7.5.1: Literals*/
	  }
  break;
case 388:
#line 3141 "cs-parser.jay"
  {
		MemberName mn = (MemberName) yyVals[0+yyTop];
		yyVal = mn.GetTypeExpression ();
	  }
  break;
case 389:
#line 3146 "cs-parser.jay"
  {
		LocatedToken lt1 = (LocatedToken) yyVals[-3+yyTop];
		LocatedToken lt2 = (LocatedToken) yyVals[-1+yyTop];
		if (RootContext.Version == LanguageVersion.ISO_1)
			Report.FeatureIsNotAvailable (lt1.Location, "namespace alias qualifier");

		yyVal = new QualifiedAliasMember (lt1.Value, lt2.Value, (TypeArguments) yyVals[0+yyTop], lt1.Location);
	  }
  break;
case 409:
#line 3176 "cs-parser.jay"
  { yyVal = new CharLiteral ((char) lexer.Value, lexer.Location); }
  break;
case 410:
#line 3177 "cs-parser.jay"
  { yyVal = new StringLiteral ((string) lexer.Value, lexer.Location); }
  break;
case 411:
#line 3178 "cs-parser.jay"
  { yyVal = new NullLiteral (lexer.Location); }
  break;
case 412:
#line 3182 "cs-parser.jay"
  { yyVal = new FloatLiteral ((float) lexer.Value, lexer.Location); }
  break;
case 413:
#line 3183 "cs-parser.jay"
  { yyVal = new DoubleLiteral ((double) lexer.Value, lexer.Location); }
  break;
case 414:
#line 3184 "cs-parser.jay"
  { yyVal = new DecimalLiteral ((decimal) lexer.Value, lexer.Location); }
  break;
case 415:
#line 3188 "cs-parser.jay"
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
case 416:
#line 3205 "cs-parser.jay"
  { yyVal = new BoolLiteral (true, lexer.Location); }
  break;
case 417:
#line 3206 "cs-parser.jay"
  { yyVal = new BoolLiteral (false, lexer.Location); }
  break;
case 418:
#line 3211 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
		lexer.Deambiguate_CloseParens (yyVal);
		/* After this, the next token returned is one of*/
		/* CLOSE_PARENS_CAST, CLOSE_PARENS_NO_CAST (CLOSE_PARENS), CLOSE_PARENS_OPEN_PARENS*/
		/* or CLOSE_PARENS_MINUS.*/
	  }
  break;
case 419:
#line 3218 "cs-parser.jay"
  { CheckToken (1026, yyToken, "Expecting ')'", lexer.Location); }
  break;
case 420:
#line 3223 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 421:
#line 3227 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 422:
#line 3231 "cs-parser.jay"
  {
		/* If a parenthesized expression is followed by a minus, we need to wrap*/
		/* the expression inside a ParenthesizedExpression for the CS0075 check*/
		/* in Binary.DoResolve().*/
		yyVal = new ParenthesizedExpression ((Expression) yyVals[-1+yyTop]);
	  }
  break;
case 423:
#line 3241 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberAccess ((Expression) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 424:
#line 3246 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		/* TODO: Location is wrong as some predefined types doesn't hold a location*/
		yyVal = new MemberAccess ((Expression) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 426:
#line 3259 "cs-parser.jay"
  {
		if (yyVals[-3+yyTop] == null)
			Report.Error (1, (Location) yyVals[-2+yyTop], "Parse error");
	        else
			yyVal = new Invocation ((Expression) yyVals[-3+yyTop], (ArrayList) yyVals[-1+yyTop]);
	  }
  break;
case 427:
#line 3266 "cs-parser.jay"
  {
		yyVal = new Invocation ((Expression) yyVals[-3+yyTop], new ArrayList ());
	  }
  break;
case 428:
#line 3270 "cs-parser.jay"
  {
		yyVal = new InvocationOrCast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 429:
#line 3274 "cs-parser.jay"
  {
		ArrayList args = new ArrayList (1);
		args.Add (yyVals[-1+yyTop]);
		yyVal = new Invocation ((Expression) yyVals[-4+yyTop], args);
	  }
  break;
case 430:
#line 3280 "cs-parser.jay"
  {
		ArrayList args = ((ArrayList) yyVals[-3+yyTop]);
		args.Add (yyVals[-1+yyTop]);
		yyVal = new Invocation ((Expression) yyVals[-6+yyTop], args);
	  }
  break;
case 431:
#line 3288 "cs-parser.jay"
  { yyVal = null; }
  break;
case 433:
#line 3294 "cs-parser.jay"
  {
	  	if (yyVals[-1+yyTop] == null)
	  	  yyVal = CollectionOrObjectInitializers.Empty;
	  	else
	  	  yyVal = new CollectionOrObjectInitializers ((ArrayList) yyVals[-1+yyTop], GetLocation (yyVals[-2+yyTop]));
	  }
  break;
case 434:
#line 3301 "cs-parser.jay"
  {
	  	yyVal = new CollectionOrObjectInitializers ((ArrayList) yyVals[-2+yyTop], GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 435:
#line 3307 "cs-parser.jay"
  { yyVal = null; }
  break;
case 436:
#line 3309 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	}
  break;
case 437:
#line 3316 "cs-parser.jay"
  {
	  	ArrayList a = new ArrayList ();
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 438:
#line 3322 "cs-parser.jay"
  {
	  	ArrayList a = (ArrayList)yyVals[-2+yyTop];
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 439:
#line 3331 "cs-parser.jay"
  {
	  	LocatedToken lt = yyVals[-2+yyTop] as LocatedToken;
	  	yyVal = new ElementInitializer (lt.Value, (Expression)yyVals[0+yyTop], lt.Location);
	  }
  break;
case 440:
#line 3336 "cs-parser.jay"
  {
		yyVal = new CollectionElementInitializer ((Expression)yyVals[0+yyTop]);
	  }
  break;
case 441:
#line 3340 "cs-parser.jay"
  {
	  	yyVal = new CollectionElementInitializer ((ArrayList)yyVals[-1+yyTop], GetLocation (yyVals[-2+yyTop]));
	  }
  break;
case 442:
#line 3344 "cs-parser.jay"
  {
	  	Report.Error (1920, GetLocation (yyVals[-1+yyTop]), "An element initializer cannot be empty");
	  }
  break;
case 445:
#line 3355 "cs-parser.jay"
  { yyVal = null; }
  break;
case 447:
#line 3361 "cs-parser.jay"
  { 
		ArrayList list = new ArrayList (4);
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 448:
#line 3367 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-2+yyTop];
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 449:
#line 3372 "cs-parser.jay"
  {
		CheckToken (1026, yyToken, "Expected `,' or `)'", GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 450:
#line 3380 "cs-parser.jay"
  {
		yyVal = new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Expression);
	  }
  break;
case 451:
#line 3384 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 452:
#line 3391 "cs-parser.jay"
  { 
		yyVal = new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Ref);
	  }
  break;
case 453:
#line 3395 "cs-parser.jay"
  { 
		yyVal = new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Out);
	  }
  break;
case 454:
#line 3399 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-1+yyTop];
		Argument[] args = new Argument [list.Count];
		list.CopyTo (args, 0);

		Expression expr = new Arglist (args, (Location) yyVals[-3+yyTop]);
		yyVal = new Argument (expr, Argument.AType.Expression);
	  }
  break;
case 455:
#line 3408 "cs-parser.jay"
  {
		yyVal = new Argument (new Arglist ((Location) yyVals[-2+yyTop]), Argument.AType.Expression);
	  }
  break;
case 456:
#line 3412 "cs-parser.jay"
  {
		yyVal = new Argument (new ArglistAccess ((Location) yyVals[0+yyTop]), Argument.AType.ArgList);
	  }
  break;
case 457:
#line 3418 "cs-parser.jay"
  { note ("section 5.4"); yyVal = yyVals[0+yyTop]; }
  break;
case 458:
#line 3423 "cs-parser.jay"
  {
		yyVal = new ElementAccess ((Expression) yyVals[-3+yyTop], (ArrayList) yyVals[-1+yyTop]);
	  }
  break;
case 459:
#line 3427 "cs-parser.jay"
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
case 460:
#line 3455 "cs-parser.jay"
  {
		ArrayList list = new ArrayList (4);
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 461:
#line 3461 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-2+yyTop];
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 462:
#line 3470 "cs-parser.jay"
  {
		yyVal = new This (current_block, (Location) yyVals[0+yyTop]);
	  }
  break;
case 463:
#line 3477 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new BaseAccess (lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 464:
#line 3482 "cs-parser.jay"
  {
		yyVal = new BaseIndexerAccess ((ArrayList) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 465:
#line 3485 "cs-parser.jay"
  {
		Report.Error (175, (Location) yyVals[-1+yyTop], "Use of keyword `base' is not valid in this context");
		yyVal = null;
	  }
  break;
case 466:
#line 3493 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PostIncrement,
				       (Expression) yyVals[-1+yyTop], (Location) yyVals[0+yyTop]);
	  }
  break;
case 467:
#line 3501 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PostDecrement,
				       (Expression) yyVals[-1+yyTop], (Location) yyVals[0+yyTop]);
	  }
  break;
case 471:
#line 3515 "cs-parser.jay"
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
case 472:
#line 3526 "cs-parser.jay"
  {
		if (RootContext.Version <= LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-2+yyTop]), "collection initializers");
	  
		yyVal = new NewInitialize ((Expression) yyVals[-1+yyTop], null, (CollectionOrObjectInitializers) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 473:
#line 3538 "cs-parser.jay"
  {
		yyVal = new ArrayCreation ((FullNamedExpression) yyVals[-5+yyTop], (ArrayList) yyVals[-3+yyTop], (string) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-6+yyTop]);
	  }
  break;
case 474:
#line 3542 "cs-parser.jay"
  {
		yyVal = new ArrayCreation ((FullNamedExpression) yyVals[-2+yyTop], (string) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 475:
#line 3546 "cs-parser.jay"
  {
		yyVal = new ImplicitlyTypedArrayCreation ((string) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 476:
#line 3550 "cs-parser.jay"
  {
		Report.Error (1031, (Location) yyVals[-1+yyTop], "Type expected");
                yyVal = null;
	  }
  break;
case 477:
#line 3555 "cs-parser.jay"
  {
		Report.Error (1526, (Location) yyVals[-2+yyTop], "A new expression requires () or [] after type");
		yyVal = null;
	  }
  break;
case 478:
#line 3563 "cs-parser.jay"
  {
	  	if (RootContext.Version <= LanguageVersion.ISO_2)
	  		Report.FeatureIsNotAvailable (GetLocation (yyVals[-3+yyTop]), "anonymous types");

		yyVal = new AnonymousTypeDeclaration ((ArrayList) yyVals[-1+yyTop], current_container, GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 479:
#line 3572 "cs-parser.jay"
  { yyVal = null; }
  break;
case 480:
#line 3574 "cs-parser.jay"
  {
	  	ArrayList a = new ArrayList (4);
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 481:
#line 3580 "cs-parser.jay"
  {
	  	ArrayList a = (ArrayList) yyVals[-2+yyTop];
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 482:
#line 3589 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken)yyVals[-2+yyTop];
	  	yyVal = new AnonymousTypeParameter ((Expression)yyVals[0+yyTop], lt.Value, lt.Location);
	  }
  break;
case 483:
#line 3594 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken)yyVals[0+yyTop];
	  	yyVal = new AnonymousTypeParameter (new SimpleName (lt.Value, lt.Location),
	  		lt.Value, lt.Location);
	  }
  break;
case 484:
#line 3600 "cs-parser.jay"
  {
	  	MemberAccess ma = (MemberAccess) yyVals[0+yyTop];
	  	yyVal = new AnonymousTypeParameter (ma, ma.Name, ma.Location);
	  }
  break;
case 485:
#line 3605 "cs-parser.jay"
  {
		Report.Error (746, lexer.Location, "Invalid anonymous type member declarator. " +
		"Anonymous type members must be a member assignment, simple name or member access expression");
	  }
  break;
case 486:
#line 3613 "cs-parser.jay"
  {
		  yyVal = "";
	  }
  break;
case 487:
#line 3617 "cs-parser.jay"
  {
			yyVal = yyVals[0+yyTop];
	  }
  break;
case 488:
#line 3624 "cs-parser.jay"
  {
		yyVal = "";
	  }
  break;
case 489:
#line 3628 "cs-parser.jay"
  {
		yyVal = "?";
	  }
  break;
case 490:
#line 3632 "cs-parser.jay"
  {
		if ((bool) yyVals[-1+yyTop])
			yyVal = "?" + yyVals[0+yyTop];
		else
			yyVal = yyVals[0+yyTop];
	  }
  break;
case 491:
#line 3639 "cs-parser.jay"
  {
		if ((bool) yyVals[-2+yyTop])
			yyVal = "?" + yyVals[-1+yyTop] + "?";
		else
			yyVal = yyVals[-1+yyTop] + "?";
	  }
  break;
case 492:
#line 3649 "cs-parser.jay"
  {
		  yyVal = (string) yyVals[0+yyTop] + (string) yyVals[-1+yyTop];
	  }
  break;
case 493:
#line 3656 "cs-parser.jay"
  {
		yyVal = "[" + (string) yyVals[-1+yyTop] + "]";
	  }
  break;
case 494:
#line 3663 "cs-parser.jay"
  {
		yyVal = "";
	  }
  break;
case 495:
#line 3667 "cs-parser.jay"
  {
		  yyVal = yyVals[0+yyTop];
	  }
  break;
case 496:
#line 3674 "cs-parser.jay"
  {
		yyVal = ",";
	  }
  break;
case 497:
#line 3678 "cs-parser.jay"
  {
		yyVal = (string) yyVals[-1+yyTop] + ",";
	  }
  break;
case 498:
#line 3685 "cs-parser.jay"
  {
		yyVal = null;
	  }
  break;
case 499:
#line 3689 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 500:
#line 3696 "cs-parser.jay"
  {
		ArrayList list = new ArrayList (4);
		yyVal = list;
	  }
  break;
case 501:
#line 3701 "cs-parser.jay"
  {
		yyVal = (ArrayList) yyVals[-2+yyTop];
	  }
  break;
case 502:
#line 3708 "cs-parser.jay"
  {
		ArrayList list = new ArrayList (4);
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 503:
#line 3714 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-2+yyTop];
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 504:
#line 3723 "cs-parser.jay"
  {
	  	pushed_current_array_type = current_array_type;
	  	lexer.TypeOfParsing = true;
	  }
  break;
case 505:
#line 3728 "cs-parser.jay"
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
case 506:
#line 3741 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 507:
#line 3745 "cs-parser.jay"
  {
		yyVal = new UnboundTypeExpression ((MemberName)yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 508:
#line 3752 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (lexer.Location, "generics");
	  
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);

		yyVal = new MemberName (lt.Value, ta, lt.Location);
	  }
  break;
case 509:
#line 3762 "cs-parser.jay"
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
case 510:
#line 3774 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);
		
	  	yyVal = new MemberName ((MemberName)yyVals[-3+yyTop], lt.Value, ta, lt.Location);
	  }
  break;
case 511:
#line 3781 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);
		
	  	yyVal = new MemberName ((MemberName)yyVals[-3+yyTop], lt.Value, ta, lt.Location);
	  }
  break;
case 512:
#line 3791 "cs-parser.jay"
  { 
		yyVal = new SizeOf ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 513:
#line 3798 "cs-parser.jay"
  {
		yyVal = new CheckedExpr ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 514:
#line 3805 "cs-parser.jay"
  {
		yyVal = new UnCheckedExpr ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 515:
#line 3812 "cs-parser.jay"
  {
		Expression deref;
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];

		deref = new Indirection ((Expression) yyVals[-2+yyTop], lt.Location);
		yyVal = new MemberAccess (deref, lt.Value);
	  }
  break;
case 516:
#line 3823 "cs-parser.jay"
  {
		start_anonymous (false, (Parameters) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 517:
#line 3827 "cs-parser.jay"
  {
		yyVal = end_anonymous ((ToplevelBlock) yyVals[0+yyTop], (Location) yyVals[-3+yyTop]);
	}
  break;
case 518:
#line 3833 "cs-parser.jay"
  { yyVal = null; }
  break;
case 520:
#line 3839 "cs-parser.jay"
  {
	  	params_modifiers_not_allowed = true; 
	  }
  break;
case 521:
#line 3843 "cs-parser.jay"
  {
	  	params_modifiers_not_allowed = false;
	  	yyVal = yyVals[-1+yyTop];
	  }
  break;
case 522:
#line 3851 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (lexer.Location, "default value expression");

		yyVal = new DefaultValueExpression ((Expression) yyVals[-1+yyTop], lexer.Location);
	  }
  break;
case 524:
#line 3862 "cs-parser.jay"
  {
		yyVal = new Unary (Unary.Operator.LogicalNot, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 525:
#line 3866 "cs-parser.jay"
  {
		yyVal = new Unary (Unary.Operator.OnesComplement, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 527:
#line 3874 "cs-parser.jay"
  {
		yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 528:
#line 3878 "cs-parser.jay"
  {
	  	yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 529:
#line 3882 "cs-parser.jay"
  {
		yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 531:
#line 3890 "cs-parser.jay"
  {
		/* TODO: wrong location*/
		yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 533:
#line 3903 "cs-parser.jay"
  { 
	  	yyVal = new Unary (Unary.Operator.UnaryPlus, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 534:
#line 3907 "cs-parser.jay"
  { 
		yyVal = new Unary (Unary.Operator.UnaryNegation, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 535:
#line 3911 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PreIncrement,
				       (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 536:
#line 3916 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PreDecrement,
				       (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 537:
#line 3921 "cs-parser.jay"
  {
		yyVal = new Indirection ((Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 538:
#line 3925 "cs-parser.jay"
  {
		yyVal = new Unary (Unary.Operator.AddressOf, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 540:
#line 3933 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Multiply, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 541:
#line 3938 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Division, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 542:
#line 3943 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Modulus, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 544:
#line 3952 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Addition, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 545:
#line 3957 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Subtraction, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 547:
#line 3966 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LeftShift, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 548:
#line 3971 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.RightShift, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 549:
#line 3979 "cs-parser.jay"
  {
		yyVal = false;
	  }
  break;
case 550:
#line 3983 "cs-parser.jay"
  {
		lexer.PutbackNullable ();
		yyVal = true;
	  }
  break;
case 551:
#line 3991 "cs-parser.jay"
  {
		if (((bool) yyVals[0+yyTop]) && (yyVals[-1+yyTop] is ComposedCast))
			yyVal = ((ComposedCast) yyVals[-1+yyTop]).RemoveNullable ();
		else
			yyVal = yyVals[-1+yyTop];
	  }
  break;
case 553:
#line 4002 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LessThan, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 554:
#line 4007 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.GreaterThan, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 555:
#line 4012 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LessThanOrEqual, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 556:
#line 4017 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.GreaterThanOrEqual, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 557:
#line 4022 "cs-parser.jay"
  {
		yyErrorFlag = 3;
	  }
  break;
case 558:
#line 4025 "cs-parser.jay"
  {
		yyVal = new Is ((Expression) yyVals[-3+yyTop], (Expression) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 559:
#line 4029 "cs-parser.jay"
  {
		yyErrorFlag = 3;
	  }
  break;
case 560:
#line 4032 "cs-parser.jay"
  {
		yyVal = new As ((Expression) yyVals[-3+yyTop], (Expression) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 562:
#line 4040 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Equality, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 563:
#line 4045 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Inequality, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 565:
#line 4054 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.BitwiseAnd, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 567:
#line 4063 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.ExclusiveOr, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 569:
#line 4072 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.BitwiseOr, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 571:
#line 4081 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LogicalAnd, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 573:
#line 4090 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LogicalOr, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 575:
#line 4099 "cs-parser.jay"
  {
		yyVal = new Conditional ((Expression) yyVals[-4+yyTop], (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 576:
#line 4103 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-1+yyTop]), "null coalescing operator");
			
		yyVal = new Nullable.NullCoalescingOperator ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 577:
#line 4111 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((FullNamedExpression) yyVals[-2+yyTop], "?", lexer.Location);
		lexer.PutbackCloseParens ();
	  }
  break;
case 578:
#line 4119 "cs-parser.jay"
  {
		yyVal = new SimpleAssign ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 579:
#line 4123 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Multiply, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 580:
#line 4128 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Division, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 581:
#line 4133 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Modulus, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 582:
#line 4138 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Addition, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 583:
#line 4143 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Subtraction, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 584:
#line 4148 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.LeftShift, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 585:
#line 4153 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.RightShift, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 586:
#line 4158 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.BitwiseAnd, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 587:
#line 4163 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.BitwiseOr, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 588:
#line 4168 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.ExclusiveOr, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 589:
#line 4176 "cs-parser.jay"
  {
		ArrayList pars = new ArrayList (4);
		pars.Add (yyVals[0+yyTop]);

		yyVal = pars;
	  }
  break;
case 590:
#line 4183 "cs-parser.jay"
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
case 591:
#line 4197 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];

		yyVal = new Parameter ((FullNamedExpression) yyVals[-1+yyTop], lt.Value, (Parameter.Modifier) yyVals[-2+yyTop], null, lt.Location);
	  }
  break;
case 592:
#line 4203 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];

		yyVal = new Parameter ((FullNamedExpression) yyVals[-1+yyTop], lt.Value, Parameter.Modifier.NONE, null, lt.Location);
	  }
  break;
case 593:
#line 4209 "cs-parser.jay"
  {
	  	LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		yyVal = new ImplicitLambdaParameter (lt.Value, lt.Location);
	  }
  break;
case 594:
#line 4216 "cs-parser.jay"
  { yyVal = Parameters.EmptyReadOnlyParameters; }
  break;
case 595:
#line 4217 "cs-parser.jay"
  { 
		ArrayList pars_list = (ArrayList) yyVals[0+yyTop];
		yyVal = new Parameters ((Parameter[])pars_list.ToArray (typeof (Parameter)));
	  }
  break;
case 596:
#line 4224 "cs-parser.jay"
  {
		start_block (lexer.Location);
	  }
  break;
case 597:
#line 4228 "cs-parser.jay"
  {
		Block b = end_block (lexer.Location);
		b.AddStatement (new ContextualReturn ((Expression) yyVals[0+yyTop]));
		yyVal = b;
	  }
  break;
case 598:
#line 4233 "cs-parser.jay"
  { 
	  	yyVal = yyVals[0+yyTop]; 
	  }
  break;
case 599:
#line 4240 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		Parameter p = new ImplicitLambdaParameter (lt.Value, lt.Location);
		start_anonymous (true, new Parameters (p), (Location) yyVals[0+yyTop]);
	  }
  break;
case 600:
#line 4246 "cs-parser.jay"
  {
		yyVal = end_anonymous ((ToplevelBlock) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 601:
#line 4250 "cs-parser.jay"
  {
		start_anonymous (true, (Parameters) yyVals[-2+yyTop], (Location) yyVals[0+yyTop]);
	  }
  break;
case 602:
#line 4254 "cs-parser.jay"
  {
		yyVal = end_anonymous ((ToplevelBlock) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 610:
#line 4286 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 611:
#line 4290 "cs-parser.jay"
  {
		MemberName name = MakeName ((MemberName) yyVals[0+yyTop]);
		push_current_class (new Class (current_namespace, current_class, name, (int) yyVals[-4+yyTop], (Attributes) yyVals[-5+yyTop]), yyVals[-3+yyTop]);
	  }
  break;
case 612:
#line 4296 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		current_class.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);

		if (RootContext.Documentation != null) {
			current_container.DocComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 613:
#line 4307 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 614:
#line 4312 "cs-parser.jay"
  {
		yyVal = pop_current_class ();
	  }
  break;
case 615:
#line 4319 "cs-parser.jay"
  { yyVal = null; }
  break;
case 616:
#line 4321 "cs-parser.jay"
  { yyVal = yyVals[0+yyTop]; }
  break;
case 617:
#line 4325 "cs-parser.jay"
  { yyVal = (int) 0; }
  break;
case 620:
#line 4332 "cs-parser.jay"
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
case 621:
#line 4345 "cs-parser.jay"
  { yyVal = Modifiers.NEW; }
  break;
case 622:
#line 4346 "cs-parser.jay"
  { yyVal = Modifiers.PUBLIC; }
  break;
case 623:
#line 4347 "cs-parser.jay"
  { yyVal = Modifiers.PROTECTED; }
  break;
case 624:
#line 4348 "cs-parser.jay"
  { yyVal = Modifiers.INTERNAL; }
  break;
case 625:
#line 4349 "cs-parser.jay"
  { yyVal = Modifiers.PRIVATE; }
  break;
case 626:
#line 4350 "cs-parser.jay"
  { yyVal = Modifiers.ABSTRACT; }
  break;
case 627:
#line 4351 "cs-parser.jay"
  { yyVal = Modifiers.SEALED; }
  break;
case 628:
#line 4352 "cs-parser.jay"
  { yyVal = Modifiers.STATIC; }
  break;
case 629:
#line 4353 "cs-parser.jay"
  { yyVal = Modifiers.READONLY; }
  break;
case 630:
#line 4354 "cs-parser.jay"
  { yyVal = Modifiers.VIRTUAL; }
  break;
case 631:
#line 4355 "cs-parser.jay"
  { yyVal = Modifiers.OVERRIDE; }
  break;
case 632:
#line 4356 "cs-parser.jay"
  { yyVal = Modifiers.EXTERN; }
  break;
case 633:
#line 4357 "cs-parser.jay"
  { yyVal = Modifiers.VOLATILE; }
  break;
case 634:
#line 4358 "cs-parser.jay"
  { yyVal = Modifiers.UNSAFE; }
  break;
case 637:
#line 4367 "cs-parser.jay"
  { current_container.AddBasesForPart (current_class, (ArrayList) yyVals[0+yyTop]); }
  break;
case 638:
#line 4371 "cs-parser.jay"
  { yyVal = null; }
  break;
case 639:
#line 4373 "cs-parser.jay"
  { yyVal = yyVals[0+yyTop]; }
  break;
case 640:
#line 4377 "cs-parser.jay"
  {
		ArrayList constraints = new ArrayList (1);
		constraints.Add (yyVals[0+yyTop]);
		yyVal = constraints;
	  }
  break;
case 641:
#line 4382 "cs-parser.jay"
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
case 642:
#line 4399 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		yyVal = new Constraints (lt.Value, (ArrayList) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 643:
#line 4406 "cs-parser.jay"
  {
		ArrayList constraints = new ArrayList (1);
		constraints.Add (yyVals[0+yyTop]);
		yyVal = constraints;
	  }
  break;
case 644:
#line 4411 "cs-parser.jay"
  {
		ArrayList constraints = (ArrayList) yyVals[-2+yyTop];

		constraints.Add (yyVals[0+yyTop]);
		yyVal = constraints;
	  }
  break;
case 646:
#line 4421 "cs-parser.jay"
  {
		yyVal = SpecialConstraint.Constructor;
	  }
  break;
case 647:
#line 4424 "cs-parser.jay"
  {
		yyVal = SpecialConstraint.ReferenceType;
	  }
  break;
case 648:
#line 4427 "cs-parser.jay"
  {
		yyVal = SpecialConstraint.ValueType;
	  }
  break;
case 649:
#line 4447 "cs-parser.jay"
  {
		++lexer.parsing_block;
		start_block ((Location) yyVals[0+yyTop]);
	  }
  break;
case 650:
#line 4452 "cs-parser.jay"
  {
	 	--lexer.parsing_block;
		yyVal = end_block ((Location) yyVals[0+yyTop]);
	  }
  break;
case 651:
#line 4460 "cs-parser.jay"
  {
		++lexer.parsing_block;
	  }
  break;
case 652:
#line 4464 "cs-parser.jay"
  {
		--lexer.parsing_block;
		yyVal = end_block ((Location) yyVals[0+yyTop]);
	  }
  break;
case 657:
#line 4482 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null && (Block) yyVals[0+yyTop] != current_block){
			current_block.AddStatement ((Statement) yyVals[0+yyTop]);
			current_block = (Block) yyVals[0+yyTop];
		}
	  }
  break;
case 658:
#line 4489 "cs-parser.jay"
  {
		current_block.AddStatement ((Statement) yyVals[0+yyTop]);
	  }
  break;
case 674:
#line 4514 "cs-parser.jay"
  {
		  Report.Error (1023, GetLocation (yyVals[0+yyTop]), "An embedded statement may not be a declaration or labeled statement");
		  yyVal = null;
	  }
  break;
case 675:
#line 4519 "cs-parser.jay"
  {
		  Report.Error (1023, GetLocation (yyVals[0+yyTop]), "An embedded statement may not be a declaration or labeled statement");
		  yyVal = null;
	  }
  break;
case 676:
#line 4527 "cs-parser.jay"
  {
		  yyVal = EmptyStatement.Value;
	  }
  break;
case 677:
#line 4534 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		LabeledStatement labeled = new LabeledStatement (lt.Value, lt.Location);

		if (current_block.AddLabel (labeled))
			current_block.AddStatement (labeled);
	  }
  break;
case 679:
#line 4546 "cs-parser.jay"
  {
		current_array_type = null;
		if (yyVals[-1+yyTop] != null){
			DictionaryEntry de = (DictionaryEntry) yyVals[-1+yyTop];
			Expression e = (Expression) de.Key;

			yyVal = declare_local_variables (e, (ArrayList) de.Value, e.Location);
		}
	  }
  break;
case 680:
#line 4557 "cs-parser.jay"
  {
		current_array_type = null;
		if (yyVals[-1+yyTop] != null){
			DictionaryEntry de = (DictionaryEntry) yyVals[-1+yyTop];

			yyVal = declare_local_constants ((Expression) de.Key, (ArrayList) de.Value);
		}
	  }
  break;
case 681:
#line 4575 "cs-parser.jay"
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
case 682:
#line 4616 "cs-parser.jay"
  {
		if ((string) yyVals[0+yyTop] == "")
			yyVal = yyVals[-1+yyTop];
		else
			yyVal = current_array_type = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], (string) yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 683:
#line 4626 "cs-parser.jay"
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
case 684:
#line 4637 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], "*", lexer.Location);
	  }
  break;
case 685:
#line 4641 "cs-parser.jay"
  {
		yyVal = new ComposedCast (TypeManager.system_void_expr, "*", (Location) yyVals[-1+yyTop]);
	  }
  break;
case 686:
#line 4645 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((FullNamedExpression) yyVals[-1+yyTop], "*");
	  }
  break;
case 687:
#line 4652 "cs-parser.jay"
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
case 688:
#line 4663 "cs-parser.jay"
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
case 689:
#line 4679 "cs-parser.jay"
  {
		if (yyVals[-1+yyTop] != null)
			yyVal = new DictionaryEntry (yyVals[-1+yyTop], yyVals[0+yyTop]);
		else
			yyVal = null;
	  }
  break;
case 690:
#line 4688 "cs-parser.jay"
  { yyVal = yyVals[-1+yyTop]; }
  break;
case 691:
#line 4697 "cs-parser.jay"
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
case 692:
#line 4707 "cs-parser.jay"
  {
		Report.Error (1002, GetLocation (yyVals[0+yyTop]), "Expecting `;'");
		yyVal = null;
	  }
  break;
case 695:
#line 4721 "cs-parser.jay"
  { 
		Location l = (Location) yyVals[-4+yyTop];

		yyVal = new If ((Expression) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);

		/* FIXME: location for warning should be loc property of $5.*/
		if (yyVals[0+yyTop] == EmptyStatement.Value)
			Report.Warning (642, 3, l, "Possible mistaken empty statement");

	  }
  break;
case 696:
#line 4733 "cs-parser.jay"
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
case 697:
#line 4748 "cs-parser.jay"
  { 
		if (switch_stack == null)
			switch_stack = new Stack (2);
		switch_stack.Push (current_block);
	  }
  break;
case 698:
#line 4755 "cs-parser.jay"
  {
		yyVal = new Switch ((Expression) yyVals[-2+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]);
		current_block = (Block) switch_stack.Pop ();
	  }
  break;
case 699:
#line 4765 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 700:
#line 4772 "cs-parser.jay"
  {
	  	Report.Warning (1522, 1, lexer.Location, "Empty switch block"); 
		yyVal = new ArrayList ();
	  }
  break;
case 702:
#line 4781 "cs-parser.jay"
  {
		ArrayList sections = new ArrayList (4);

		sections.Add (yyVals[0+yyTop]);
		yyVal = sections;
	  }
  break;
case 703:
#line 4788 "cs-parser.jay"
  {
		ArrayList sections = (ArrayList) yyVals[-1+yyTop];

		sections.Add (yyVals[0+yyTop]);
		yyVal = sections;
	  }
  break;
case 704:
#line 4798 "cs-parser.jay"
  {
		current_block = current_block.CreateSwitchBlock (lexer.Location);
	  }
  break;
case 705:
#line 4802 "cs-parser.jay"
  {
		yyVal = new SwitchSection ((ArrayList) yyVals[-2+yyTop], current_block.Explicit);
	  }
  break;
case 706:
#line 4809 "cs-parser.jay"
  {
		ArrayList labels = new ArrayList (4);

		labels.Add (yyVals[0+yyTop]);
		yyVal = labels;
	  }
  break;
case 707:
#line 4816 "cs-parser.jay"
  {
		ArrayList labels = (ArrayList) (yyVals[-1+yyTop]);
		labels.Add (yyVals[0+yyTop]);

		yyVal = labels;
	  }
  break;
case 708:
#line 4825 "cs-parser.jay"
  { yyVal = new SwitchLabel ((Expression) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]); }
  break;
case 709:
#line 4826 "cs-parser.jay"
  { yyVal = new SwitchLabel (null, (Location) yyVals[0+yyTop]); }
  break;
case 710:
#line 4827 "cs-parser.jay"
  {
		Report.Error (
			1523, GetLocation (yyVals[0+yyTop]), 
			"The keyword case or default must precede code in switch block");
	  }
  break;
case 715:
#line 4843 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-4+yyTop];
		yyVal = new While ((Expression) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);
	  }
  break;
case 716:
#line 4852 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-6+yyTop];

		yyVal = new Do ((Statement) yyVals[-5+yyTop], (Expression) yyVals[-2+yyTop], l);
	  }
  break;
case 717:
#line 4862 "cs-parser.jay"
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
					Assign a = new SimpleAssign (var, expr, decl.Location);
					
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
case 718:
#line 4903 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-9+yyTop];

		For f = new For ((Statement) yyVals[-5+yyTop], (Expression) yyVals[-4+yyTop], (Statement) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);

		current_block.AddStatement (f);

		yyVal = end_block (lexer.Location);
	  }
  break;
case 719:
#line 4915 "cs-parser.jay"
  { yyVal = EmptyStatement.Value; }
  break;
case 723:
#line 4925 "cs-parser.jay"
  { yyVal = null; }
  break;
case 725:
#line 4930 "cs-parser.jay"
  { yyVal = EmptyStatement.Value; }
  break;
case 728:
#line 4940 "cs-parser.jay"
  {
		/* CHANGE: was `null'*/
		Statement s = (Statement) yyVals[0+yyTop];
		Block b = new Block (current_block, s.loc, lexer.Location);   

		b.AddStatement (s);
		yyVal = b;
	  }
  break;
case 729:
#line 4949 "cs-parser.jay"
  {
		Block b = (Block) yyVals[-2+yyTop];

		b.AddStatement ((Statement) yyVals[0+yyTop]);
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 730:
#line 4959 "cs-parser.jay"
  {
		Report.Error (230, (Location) yyVals[-5+yyTop], "Type and identifier are both required in a foreach statement");
		yyVal = null;
	  }
  break;
case 731:
#line 4965 "cs-parser.jay"
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
case 732:
#line 4985 "cs-parser.jay"
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
case 739:
#line 5009 "cs-parser.jay"
  {
		yyVal = new Break ((Location) yyVals[-1+yyTop]);
	  }
  break;
case 740:
#line 5016 "cs-parser.jay"
  {
		yyVal = new Continue ((Location) yyVals[-1+yyTop]);
	  }
  break;
case 741:
#line 5023 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new Goto (lt.Value, lt.Location);
	  }
  break;
case 742:
#line 5028 "cs-parser.jay"
  {
		yyVal = new GotoCase ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 743:
#line 5032 "cs-parser.jay"
  {
		yyVal = new GotoDefault ((Location) yyVals[-2+yyTop]);
	  }
  break;
case 744:
#line 5039 "cs-parser.jay"
  {
		yyVal = new Return ((Expression) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 745:
#line 5046 "cs-parser.jay"
  {
		yyVal = new Throw ((Expression) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 746:
#line 5053 "cs-parser.jay"
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
case 747:
#line 5073 "cs-parser.jay"
  {
		Report.Error (1627, (Location) yyVals[-1+yyTop], "Expression expected after yield return");
		yyVal = null;
	  }
  break;
case 748:
#line 5078 "cs-parser.jay"
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
case 751:
#line 5106 "cs-parser.jay"
  {
		yyVal = new TryCatch ((Block) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-2+yyTop], false);
	  }
  break;
case 752:
#line 5110 "cs-parser.jay"
  {
		yyVal = new TryFinally ((Statement) yyVals[-2+yyTop], (Block) yyVals[0+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 753:
#line 5114 "cs-parser.jay"
  {
		yyVal = new TryFinally (new TryCatch ((Block) yyVals[-3+yyTop], (ArrayList) yyVals[-2+yyTop], (Location) yyVals[-4+yyTop], true), (Block) yyVals[0+yyTop], (Location) yyVals[-4+yyTop]);
	  }
  break;
case 754:
#line 5118 "cs-parser.jay"
  {
		Report.Error (1524, (Location) yyVals[-2+yyTop], "Expected catch or finally");
		yyVal = null;
	  }
  break;
case 755:
#line 5126 "cs-parser.jay"
  {
		ArrayList l = new ArrayList (4);

		l.Add (yyVals[0+yyTop]);
		yyVal = l;
	  }
  break;
case 756:
#line 5133 "cs-parser.jay"
  {
		ArrayList l = (ArrayList) yyVals[-1+yyTop];

		l.Add (yyVals[0+yyTop]);
		yyVal = l;
	  }
  break;
case 757:
#line 5142 "cs-parser.jay"
  { yyVal = null; }
  break;
case 759:
#line 5148 "cs-parser.jay"
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
case 760:
#line 5165 "cs-parser.jay"
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
case 761:
#line 5186 "cs-parser.jay"
  { yyVal = null; }
  break;
case 763:
#line 5192 "cs-parser.jay"
  {
		yyVal = new DictionaryEntry (yyVals[-2+yyTop], yyVals[-1+yyTop]);
	  }
  break;
case 764:
#line 5200 "cs-parser.jay"
  {
		yyVal = new Checked ((Block) yyVals[0+yyTop]);
	  }
  break;
case 765:
#line 5207 "cs-parser.jay"
  {
		yyVal = new Unchecked ((Block) yyVals[0+yyTop]);
	  }
  break;
case 766:
#line 5214 "cs-parser.jay"
  {
		RootContext.CheckUnsafeOption ((Location) yyVals[0+yyTop]);
	  }
  break;
case 767:
#line 5216 "cs-parser.jay"
  {
		yyVal = new Unsafe ((Block) yyVals[0+yyTop]);
	  }
  break;
case 768:
#line 5225 "cs-parser.jay"
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
case 769:
#line 5248 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-6+yyTop];

		Fixed f = new Fixed ((Expression) yyVals[-4+yyTop], (ArrayList) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], l);

		current_block.AddStatement (f);

		yyVal = end_block (lexer.Location);
	  }
  break;
case 770:
#line 5260 "cs-parser.jay"
  { 
	   	ArrayList declarators = new ArrayList (4);
	   	if (yyVals[0+yyTop] != null)
			declarators.Add (yyVals[0+yyTop]);
		yyVal = declarators;
	  }
  break;
case 771:
#line 5267 "cs-parser.jay"
  {
		ArrayList declarators = (ArrayList) yyVals[-2+yyTop];
		if (yyVals[0+yyTop] != null)
			declarators.Add (yyVals[0+yyTop]);
		yyVal = declarators;
	  }
  break;
case 772:
#line 5277 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		/* FIXME: keep location*/
		yyVal = new Pair (lt.Value, yyVals[0+yyTop]);
	  }
  break;
case 773:
#line 5283 "cs-parser.jay"
  {
		Report.Error (210, ((LocatedToken) yyVals[0+yyTop]).Location, "You must provide an initializer in a fixed or using statement declaration");
		yyVal = null;
	  }
  break;
case 774:
#line 5291 "cs-parser.jay"
  {
		/**/
 	  }
  break;
case 775:
#line 5295 "cs-parser.jay"
  {
		yyVal = new Lock ((Expression) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]);
	  }
  break;
case 776:
#line 5302 "cs-parser.jay"
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

			/* Assign a = new SimpleAssign (var, expr, decl.Location);*/
			/* assign_block.AddStatement (new StatementExpression (a));*/
		}

		/* Note: the $$ here refers to the value of this code block and not of the LHS non-terminal.*/
		/* It can be referred to as $5 below.*/
		yyVal = vars;
	  }
  break;
case 777:
#line 5344 "cs-parser.jay"
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
case 778:
#line 5357 "cs-parser.jay"
  {
		start_block (lexer.Location);
	  }
  break;
case 779:
#line 5361 "cs-parser.jay"
  {
		current_block.AddStatement (new UsingTemporary ((Expression) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]));
		yyVal = end_block (lexer.Location);
	  }
  break;
case 780:
#line 5372 "cs-parser.jay"
  {
		++lexer.query_parsing;
	  }
  break;
case 781:
#line 5376 "cs-parser.jay"
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
case 782:
#line 5392 "cs-parser.jay"
  {
		current_block = new Linq.QueryBlock (current_block, GetLocation (yyVals[-3+yyTop]));
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance, lt.Value, lt.Location);
		yyVal = new Linq.QueryExpression (lt, new Linq.QueryStartClause ((Expression)yyVals[0+yyTop]));
	  }
  break;
case 783:
#line 5400 "cs-parser.jay"
  {
		current_block = new Linq.QueryBlock (current_block, GetLocation (yyVals[-4+yyTop]));
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];

		Expression type = (Expression)yyVals[-3+yyTop];
		current_block.AddVariable (type, lt.Value, lt.Location);
		yyVal = new Linq.QueryExpression (lt, new Linq.Cast (type, (Expression)yyVals[0+yyTop]));
	  }
  break;
case 784:
#line 5412 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance,
			lt.Value, lt.Location);
			
		yyVal = new Linq.SelectMany (lt, (Expression)yyVals[0+yyTop]);			
	  }
  break;
case 785:
#line 5421 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];

		Expression type = (Expression)yyVals[-3+yyTop];
		current_block.AddVariable (type, lt.Value, lt.Location);
		yyVal = new Linq.SelectMany (lt, new Linq.Cast (type, (Expression)yyVals[0+yyTop]));
	  }
  break;
case 786:
#line 5432 "cs-parser.jay"
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
case 787:
#line 5450 "cs-parser.jay"
  {
		yyVal = new Linq.Select ((Expression)yyVals[0+yyTop], GetLocation (yyVals[-1+yyTop]));
	  }
  break;
case 788:
#line 5454 "cs-parser.jay"
  {
	    yyVal = new Linq.GroupBy ((Expression)yyVals[-2+yyTop], (Expression)yyVals[0+yyTop], GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 792:
#line 5467 "cs-parser.jay"
  {
		((Linq.AQueryClause)yyVals[-1+yyTop]).Tail.Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 798:
#line 5483 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance,
			lt.Value, lt.Location);	  
	  	yyVal = new Linq.Let (lt, (Expression)yyVals[0+yyTop], GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 799:
#line 5493 "cs-parser.jay"
  {
		yyVal = new Linq.Where ((Expression)yyVals[0+yyTop], GetLocation (yyVals[-1+yyTop]));
	  }
  break;
case 800:
#line 5500 "cs-parser.jay"
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
case 801:
#line 5516 "cs-parser.jay"
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
case 803:
#line 5536 "cs-parser.jay"
  {
	  	yyVal = yyVals[0+yyTop];
	  }
  break;
case 804:
#line 5543 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 806:
#line 5551 "cs-parser.jay"
  {
		((Linq.AQueryClause)yyVals[-2+yyTop]).Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 807:
#line 5559 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 808:
#line 5563 "cs-parser.jay"
  {
		((Linq.AQueryClause)yyVals[-2+yyTop]).Tail.Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 809:
#line 5571 "cs-parser.jay"
  {
		yyVal = new Linq.OrderByAscending ((Expression)yyVals[0+yyTop]);	
	  }
  break;
case 810:
#line 5575 "cs-parser.jay"
  {
		yyVal = new Linq.OrderByAscending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 811:
#line 5579 "cs-parser.jay"
  {
		yyVal = new Linq.OrderByDescending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 812:
#line 5586 "cs-parser.jay"
  {
		yyVal = new Linq.ThenByAscending ((Expression)yyVals[0+yyTop]);	
	  }
  break;
case 813:
#line 5590 "cs-parser.jay"
  {
		yyVal = new Linq.ThenByAscending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 814:
#line 5594 "cs-parser.jay"
  {
		yyVal = new Linq.ThenByDescending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 816:
#line 5603 "cs-parser.jay"
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
case 817:
#line 5617 "cs-parser.jay"
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
   31,   67,   70,   70,   71,   71,   72,   72,   72,   72,
   72,   72,   72,   72,   72,   72,   53,   74,   74,   75,
   75,   28,   28,   28,   28,   78,   78,   79,   79,   77,
   77,   80,   80,   80,   81,   81,   81,   81,   81,   86,
   29,   87,   87,   89,   89,   92,   93,   84,   94,   95,
   84,   96,   84,   84,   85,   85,   91,   91,   99,  100,
   99,   98,   98,   98,   98,   98,   98,   98,   98,   98,
  101,  101,  104,  104,  104,  104,  104,  105,  105,  106,
  106,  107,  107,  107,  102,  102,  108,  108,  108,  103,
  109,  111,  112,   54,  110,  110,  110,  110,  110,  116,
  113,  117,  114,  115,  115,  118,  119,  121,  122,   32,
   32,  120,  123,  123,  124,  124,  125,  125,  125,  125,
  125,  125,  125,  125,  125,  125,  130,  133,  131,  131,
  134,  135,  126,  136,  137,  126,  138,  139,  127,  127,
  128,  128,  128,  141,  142,  128,  143,  144,  129,  147,
   57,  146,  146,  149,  145,  145,  148,  148,  148,  148,
  148,  148,  148,  148,  148,  148,  148,  148,  148,  148,
  148,  148,  148,  148,  148,  148,  148,  148,  151,  150,
  152,  150,  150,  150,   58,  153,  153,  157,  155,  154,
  154,  156,  156,  156,  160,  160,  160,  161,   59,   55,
  162,  163,   55,   55,  140,  140,  140,  140,  140,  140,
  166,  164,  164,  164,  167,  165,  165,  165,  169,  170,
   56,  168,  168,  173,  174,   33,  171,  171,  172,  175,
  175,  176,  176,  177,  178,  177,  179,  180,   34,  181,
  181,   12,   12,   12,   90,   90,   62,  182,  182,  183,
  183,  184,  184,  185,  185,   73,   73,   73,   73,  188,
  188,  189,  189,  189,  189,  192,  192,  193,  186,  186,
  186,  186,  186,  186,  186,  194,  194,  194,  194,  194,
  194,  194,  194,  194,  194,  187,  196,  196,  196,  196,
  196,  196,  196,  196,  196,  196,  196,  196,  196,  196,
  196,  196,  196,  196,  196,  197,  197,  197,  197,  197,
  197,  216,  216,  216,  215,  214,  214,  217,  217,  198,
  198,  198,  200,  200,  218,  201,  201,  201,  201,  201,
  222,  222,  223,  223,  224,  224,  225,  225,  226,  226,
  226,  226,  227,  227,  159,  159,  220,  220,  220,  221,
  221,  219,  219,  219,  219,  219,  230,  202,  202,  229,
  229,  203,  204,  204,  204,  205,  206,  207,  207,  207,
  231,  231,  232,  232,  232,  232,  232,  233,  236,  236,
  236,  237,  237,  237,  237,  234,  234,  238,  238,  238,
  238,  195,  190,  239,  239,  240,  240,  235,  235,   83,
   83,  241,  241,  242,  208,  243,  243,  244,  244,  244,
  244,  209,  210,  211,  212,  246,  213,  245,  245,  248,
  247,  199,  249,  249,  249,  249,  252,  252,  252,  251,
  251,  250,  250,  250,  250,  250,  250,  250,  191,  191,
  191,  191,  253,  253,  253,  254,  254,  254,  255,  255,
  256,  257,  257,  257,  257,  257,  258,  257,  259,  257,
  260,  260,  260,  261,  261,  262,  262,  263,  263,  264,
  264,  265,  265,  266,  266,  266,  266,  267,  267,  267,
  267,  267,  267,  267,  267,  267,  267,  267,  268,  268,
  269,  269,  269,  270,  270,  272,  271,  271,  274,  273,
  275,  273,   47,   47,  228,  228,  228,   76,  277,  278,
  279,  280,  281,   30,   61,   61,   60,   60,   88,   88,
  282,  282,  282,  282,  282,  282,  282,  282,  282,  282,
  282,  282,  282,  282,   64,   64,  283,   66,   66,  284,
  284,  285,  286,  286,  287,  287,  287,  287,  288,   97,
  289,  158,  132,  132,  290,  290,  291,  291,  291,  293,
  293,  293,  293,  293,  293,  293,  293,  293,  293,  293,
  293,  293,  307,  307,  307,  295,  308,  294,  292,  292,
  311,  311,  312,  312,  312,  312,  309,  309,  310,  296,
  313,  313,  297,  297,  314,  314,  316,  315,  317,  318,
  318,  319,  319,  322,  320,  321,  321,  323,  323,  323,
  298,  298,  298,  298,  324,  325,  330,  326,  328,  328,
  332,  332,  329,  329,  331,  331,  334,  333,  333,  327,
  335,  327,  299,  299,  299,  299,  299,  299,  336,  337,
  338,  338,  338,  339,  340,  341,  341,  341,   82,   82,
  300,  300,  300,  300,  342,  342,  344,  344,  346,  343,
  345,  345,  347,  301,  302,  348,  305,  350,  306,  349,
  349,  351,  351,  352,  303,  353,  304,  354,  304,  357,
  276,  355,  355,  358,  358,  356,  360,  360,  359,  359,
  362,  362,  363,  363,  363,  363,  363,  364,  365,  366,
  366,  368,  368,  367,  369,  369,  371,  371,  370,  370,
  370,  372,  372,  372,  361,  373,  361,
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
    5,    3,    0,    1,    1,    2,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    6,    1,    3,    3,
    1,    5,    6,    5,    5,    1,    3,    4,    3,    1,
    3,    3,    1,    4,    1,    1,    5,    1,    2,    0,
    3,    0,    1,    1,    1,    0,    0,   10,    0,    0,
   10,    0,   10,    8,    1,    1,    0,    1,    0,    0,
    2,    1,    3,    3,    3,    5,    3,    5,    1,    1,
    1,    3,    4,    6,    3,    4,    6,    0,    1,    1,
    2,    1,    1,    1,    4,    4,    1,    2,    2,    1,
    0,    0,    0,   10,    1,    2,    1,    2,    1,    0,
    5,    0,    5,    1,    1,    0,    0,    0,    0,   13,
    5,    3,    0,    1,    1,    2,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    0,    4,    1,
    0,    0,   11,    0,    0,   11,    0,    0,    9,    4,
    6,    5,    6,    0,    0,   10,    0,    0,   12,    0,
    5,    1,    1,    0,    7,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    0,    7,
    0,    7,    2,    2,    4,    1,    2,    0,    5,    1,
    1,    5,    5,    2,    0,    1,    1,    0,    8,    6,
    0,    0,   10,    6,    2,    2,    1,    1,    1,    0,
    0,    4,    3,    3,    0,    4,    3,    3,    0,    0,
    8,    5,    7,    0,    0,    9,    0,    2,    3,    0,
    2,    1,    3,    2,    0,    5,    0,    0,   12,    0,
    1,    2,    4,    4,    2,    4,    2,    0,    3,    0,
    3,    1,    3,    1,    2,    2,    2,    1,    1,    2,
    2,    2,    2,    2,    2,    1,    3,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    3,    1,    1,    4,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    3,    3,    2,
    2,    2,    4,    4,    1,    4,    4,    3,    5,    7,
    0,    1,    3,    4,    0,    1,    1,    3,    3,    1,
    3,    2,    1,    1,    0,    1,    1,    3,    2,    1,
    1,    2,    2,    4,    3,    1,    1,    4,    2,    1,
    3,    1,    4,    4,    2,    2,    2,    1,    1,    1,
    6,    3,    7,    4,    3,    2,    3,    4,    0,    1,
    3,    3,    1,    1,    1,    0,    1,    0,    1,    2,
    3,    2,    3,    0,    1,    1,    2,    0,    1,    2,
    4,    1,    3,    0,    5,    1,    1,    2,    4,    4,
    4,    4,    4,    4,    3,    0,    4,    0,    1,    0,
    4,    3,    1,    2,    2,    1,    3,    3,    3,    1,
    4,    1,    2,    2,    2,    2,    2,    2,    1,    3,
    3,    3,    1,    3,    3,    1,    3,    3,    0,    1,
    2,    1,    3,    3,    3,    3,    0,    4,    0,    4,
    1,    3,    3,    1,    3,    1,    3,    1,    3,    1,
    3,    1,    3,    1,    5,    3,    3,    3,    3,    3,
    3,    3,    3,    3,    3,    3,    3,    3,    1,    3,
    3,    2,    1,    0,    1,    0,    2,    1,    0,    4,
    0,    6,    1,    1,    1,    1,    1,    1,    1,    0,
    0,    0,    0,   13,    0,    1,    0,    1,    1,    2,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    0,    1,    2,    0,    1,    1,
    2,    4,    1,    3,    1,    3,    1,    1,    0,    4,
    0,    4,    0,    1,    1,    2,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    0,    4,    2,    2,
    2,    2,    2,    2,    2,    2,    2,    3,    3,    2,
    1,    1,    1,    1,    5,    7,    0,    6,    3,    0,
    1,    1,    2,    0,    3,    1,    2,    3,    1,    1,
    1,    1,    1,    1,    5,    7,    0,   10,    0,    1,
    1,    1,    0,    1,    0,    1,    1,    1,    3,    6,
    0,    9,    1,    1,    1,    1,    1,    1,    2,    2,
    3,    4,    3,    3,    3,    4,    3,    3,    0,    1,
    3,    4,    5,    3,    1,    2,    0,    1,    0,    4,
    0,    1,    4,    2,    2,    0,    3,    0,    7,    1,
    3,    3,    1,    0,    6,    0,    6,    0,    6,    0,
    3,    4,    5,    4,    5,    3,    2,    4,    0,    1,
    1,    2,    1,    1,    1,    1,    1,    4,    2,    9,
   10,    0,    2,    2,    1,    3,    1,    3,    1,    2,
    2,    1,    2,    2,    0,    0,    4,
  };
   static  short [] yyDefRed = {            0,
    6,    0,    0,    0,    0,    0,    4,    0,    7,    9,
   10,   11,   17,   18,   44,    0,   43,   45,   46,   47,
   48,   49,   50,   51,    0,   55,  140,    0,   20,    0,
    0,    0,   63,   61,   62,    0,    0,    0,    0,    0,
   64,    0,    1,    0,    8,    3,  626,  632,  624,    0,
  621,  631,  625,  623,  622,  629,  627,  628,  634,  630,
  633,    0,    0,  619,   56,    0,    0,    0,    0,    0,
  342,    0,   21,    0,    0,    0,    0,   59,    0,   66,
    2,    0,  371,  377,  384,  372,    0,  374,    0,    0,
  373,  380,  382,  369,  376,  378,  370,  381,  383,  379,
    0,    0,    0,    0,    0,    0,  358,  359,  375,  620,
  649,  156,  141,  155,   14,    0,    0,    0,    0,    0,
  352,    0,    0,    0,   65,   58,    0,    0,    0,  417,
    0,  411,    0,  462,  416,  504,    0,  385,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  415,  412,
  413,  414,  409,  410,    0,    0,    0,    0,   70,    0,
    0,   75,   77,  388,  425,    0,    0,  387,  390,  391,
  392,  393,  394,  395,  396,  397,  398,  399,  400,  401,
  402,  403,  404,  405,  406,  407,  408,    0,    0,  604,
  468,  469,  470,  532,    0,  526,  530,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  605,  603,  606,  607,
  780,    0,    0,    0,    0,  361,    0,    0,    0,  130,
    0,    0,  341,  356,  610,    0,    0,    0,  360,    0,
    0,    0,    0,    0,  357,    0,   19,    0,    0,  349,
  343,  344,   57,  465,    0,    0,    0,  144,  145,  520,
  516,  519,  476,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  525,  533,  534,
  524,  538,  537,  535,  536,  599,    0,    0,    0,  347,
  183,  182,  184,    0,    0,    0,    0,  589,    0,    0,
   69,    0,    0,    0,    0,    0,    0,    0,    0,  466,
  467,    0,  459,  421,    0,    0,    0,  422,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  559,  557,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   29,    0,    0,    0,    0,  324,  124,    0,    0,  126,
    0,    0,    0,  345,    0,    0,  125,  149,    0,    0,
    0,  211,    0,  101,    0,  496,    0,    0,  122,    0,
  146,  487,  492,  386,  692,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  766,    0,    0,    0,  676,    0,  691,  660,    0,
    0,    0,    0,  655,  657,  658,  659,  661,  662,  663,
  664,  665,  666,  667,  668,  669,  670,  671,  672,    0,
    0,    0,    0,    0,  693,  694,  711,  712,  713,  714,
  733,  734,  735,  736,  737,  738,  353,  460,    0,    0,
    0,    0,    0,  485,    0,    0,    0,    0,    0,    0,
  480,  477,    0,    0,    0,    0,  472,    0,  475,    0,
    0,    0,    0,    0,  419,  418,  362,    0,  364,  363,
    0,    0,   80,    0,    0,  592,    0,    0,    0,  522,
    0,   76,   79,   78,  540,  542,  541,    0,    0,    0,
    0,  450,    0,  451,    0,  447,    0,  515,  527,  528,
    0,    0,  529,    0,  578,  579,  580,  581,  582,  583,
  584,  585,  586,  588,  587,    0,  539,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  577,    0,    0,  576,    0,    0,    0,    0,
    0,  781,  793,    0,    0,  791,  794,  795,  796,  797,
    0,   25,   23,    0,    0,    0,    0,    0,  123,  750,
    0,    0,  138,  135,  132,  136,    0,    0,    0,  131,
    0,    0,  611,  207,   97,  493,  497,    0,    0,  739,
  764,    0,    0,    0,  740,  674,  673,  675,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  697,    0,
    0,  765,    0,    0,  685,    0,    0,    0,  677,  650,
  684,    0,    0,  682,  683,  681,  656,  679,  680,    0,
  686,    0,  690,  464,    0,  463,  513,  190,    0,    0,
    0,  158,    0,    0,    0,  171,  517,    0,  420,    0,
  478,    0,    0,    0,    0,    0,  437,  440,    0,    0,
  474,  500,  502,    0,  512,    0,    0,    0,    0,    0,
  514,  782,    0,  531,  598,  600,    0,  351,  389,  591,
  590,  601,  458,  457,  453,  452,    0,  426,  449,    0,
  423,  427,    0,    0,    0,  424,    0,  560,  558,    0,
  609,  799,    0,    0,    0,    0,    0,    0,  804,    0,
    0,    0,    0,  792,   32,   12,    0,   30,    0,    0,
    0,  325,  129,    0,  127,  134,    0,    0,  346,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  118,    0,
    0,  721,  728,    0,  720,    0,    0,  608,    0,  743,
  741,    0,    0,  744,    0,  745,  754,    0,    0,    0,
  755,  767,    0,    0,    0,  748,  747,    0,    0,    0,
    0,  461,    0,    0,    0,  180,    0,  521,    0,    0,
    0,  482,    0,  481,  442,    0,    0,  433,    0,    0,
    0,    0,    0,    0,  508,    0,  505,    0,  783,  597,
    0,  455,    0,  448,  429,    0,  550,  551,  575,    0,
    0,    0,    0,    0,  810,  811,    0,  787,    0,    0,
  786,    0,   13,   15,    0,    0,  337,    0,    0,    0,
  332,    0,  128,    0,  150,  152,    0,    0,  636,    0,
    0,  154,  147,    0,    0,    0,    0,    0,  770,  717,
    0,    0,    0,  742,    0,  774,    0,    0,  759,  762,
  752,    0,  756,  778,  776,    0,  746,  678,  491,  188,
  189,    0,  181,    0,    0,    0,  164,  172,  165,  167,
  441,  443,  444,  439,  434,  438,    0,  471,  432,  503,
  501,    0,    0,    0,  602,  454,    0,  784,    0,    0,
    0,  798,    0,    0,  807,    0,  816,   33,   16,   41,
    0,    0,    0,    0,  329,    0,  331,  326,    0,    0,
    0,    0,    0,  366,    0,  612,    0,  640,  208,   98,
    0,  120,  119,    0,    0,  768,    0,    0,  729,    0,
    0,    0,    0,    0,    0,    0,  753,    0,    0,  715,
  176,    0,  186,  185,    0,    0,  499,  473,  509,  511,
  510,  430,  785,    0,    0,  813,  814,    0,  788,    0,
   34,   31,   42,  338,    0,  333,  137,  151,  153,    0,
    0,    0,  641,    0,    0,  148,    0,  772,    0,  771,
  724,    0,  730,    0,    0,  775,    0,  698,  758,    0,
  760,  779,  777,    0,    0,  168,  166,    0,    0,  808,
  817,    0,    0,  367,    0,    0,  613,    0,  209,    0,
   99,  716,  769,    0,  731,  696,  710,    0,  709,    0,
    0,  702,    0,  706,  763,  174,  177,    0,    0,  339,
  336,  647,    0,  648,    0,    0,  643,    0,   95,   87,
   88,    0,    0,   84,   86,   89,   90,   91,   92,   93,
   94,    0,    0,  222,  223,  225,  224,  221,  226,    0,
    0,  215,  217,  218,  219,  220,    0,  115,  108,  109,
  107,  110,  111,  112,  113,  114,  116,    0,    0,  105,
    0,    0,    0,  726,    0,    0,  699,  703,    0,  707,
    0,    0,    0,    0,    0,    0,    0,    0,   81,   85,
  614,    0,    0,  212,  216,  210,  102,  106,  100,    0,
  732,  708,    0,    0,  800,    0,  646,  644,    0,    0,
    0,    0,    0,    0,  250,  256,    0,    0,    0,  298,
  616,    0,    0,    0,  718,  803,  801,    0,    0,  284,
    0,  283,    0,    0,    0,    0,    0,    0,  651,  291,
  285,  290,    0,  287,  319,    0,    0,    0,  240,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  262,
  261,  258,  263,  264,  257,  276,  275,  268,  269,  265,
  267,  266,  270,  259,  260,  271,  272,  278,  277,  273,
  274,    0,    0,    0,    0,  253,  252,  251,    0,  294,
    0,    0,    0,    0,  242,    0,    0,    0,  237,    0,
  117,  304,  301,  300,  281,  279,    0,  254,    0,    0,
    0,  192,    0,    0,    0,  199,    0,  320,    0,    0,
    0,  244,  241,  243,    0,    0,    0,    0,    0,    0,
    0,  289,    0,  322,  161,    0,    0,  652,    0,    0,
    0,    0,  196,  198,    0,    0,  234,    0,  238,  231,
  309,    0,  302,    0,    0,    0,    0,    0,    0,  193,
  292,  293,  200,  202,  321,  299,  245,    0,  247,    0,
    0,    0,    0,    0,    0,    0,  305,    0,  306,  282,
  280,  255,  323,    0,    0,    0,    0,  235,    0,  239,
  232,  313,    0,  317,    0,  314,  318,  303,    0,    0,
  194,  205,  204,  201,  203,  246,    0,  248,    0,  312,
  316,  228,  230,  236,    0,  233,    0,  249,    0,  229,
  };
  protected static  short [] yyDgoto  = {             5,
    6,    7,    8,    9,   10,   11,   12,  707,  815,   13,
   14,  103,   32,   15,  629,  342,  212,  553,   77,  708,
  551,  709,  816,  901,  812,  902,   17,   18,   19,   20,
   21,   22,   23,   24,  630,   26,   38,   39,   40,   41,
   42,   80,  158,  159,  160,  161,  398,  163, 1007, 1042,
 1043, 1044, 1045, 1046, 1047, 1048, 1049, 1050, 1051,   62,
  104,  164,  365,  828,  724,  916, 1011,  975, 1081, 1078,
 1079, 1080,  119,  728,  729,  739,  230,  349,  350,  220,
  565,  561,  566,   27,  113,   66,    0,   63,  250,  232,
  631,  579,  921,  571,  910,  911,  399,  632, 1219, 1220,
  633,  634,  635,  636,  764,  765,  286,  767, 1195, 1228,
 1247, 1294, 1229, 1230, 1314, 1295, 1296,  363,  723, 1009,
  974, 1067, 1060, 1061, 1062, 1063, 1064, 1065, 1066, 1103,
 1324,  400, 1327, 1281, 1319, 1278, 1317, 1237, 1280, 1263,
 1256, 1297, 1299, 1325, 1125, 1198, 1148, 1192, 1243, 1126,
 1241, 1240, 1127, 1151, 1128, 1154, 1144, 1152,  493, 1098,
 1156, 1239, 1285, 1264, 1265, 1303, 1305, 1129, 1203, 1252,
  346,  712,  556,  822,  819,  820,  821,  965,  903, 1002,
  613,   71,  280,  120,  121,  165,  107,  108,  265,  233,
  166,  913,  914,  109,  234,  167,  168,  169,  170,  171,
  172,  173,  174,  175,  176,  177,  178,  179,  180,  181,
  182,  183,  184,  185,  186,  187,  188,  189,  494,  495,
  496,  878,  457,  645,  646,  647,  874,  190,  439,  675,
  191,  192,  193,  373,  948,  450,  451,  614,  367,  368,
  654,  258,  659,  660,  251,  443,  252,  442,  194,  195,
  196,  197,  198,  199,  798,  688,  200,  522,  521,  201,
  202,  203,  204,  205,  206,  207,  208,  287,  288,  289,
  666,  667,  209,  472,  791,  210,  692,  361,  722,  972,
 1052,   64,  829,  917,  918, 1036, 1037,  236, 1199,  403,
  404,  586,  587,  588,  408,  409,  410,  411,  412,  413,
  414,  415,  416,  417,  418,  419,  589,  759,  420,  421,
  422,  423,  424,  425,  426,  745,  988, 1020, 1021, 1022,
 1023, 1089, 1024,  427,  428,  429,  430,  734,  982,  928,
 1082,  735,  736, 1084, 1085,  431,  432,  433,  434,  435,
  436,  750,  751,  990,  849,  936,  850,  603,  838,  979,
  839,  933,  939,  938,  211,  542,  340,  543,  544,  703,
  811,  545,  546,  547,  548,  549,  550, 1115,  699,  700,
  894,  895,  960,
  };
  protected static  short [] yySindex = {           79,
    0, -182, -208, -202,    0,   79,    0,   65,    0,    0,
    0,    0,    0,    0,    0, 8190,    0,    0,    0,    0,
    0,    0,    0,    0,   -9,    0,    0, -145,    0, -249,
   18,   12,    0,    0,    0,  409,   18,  -25,  150,  243,
    0,  252,    0,   65,    0,    0,    0,    0,    0,  -25,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0, 8805,11503,    0,    0,  487,  299,  -25, 8837,  272,
    0,  285,    0,  409,  150,  -25,   86,    0, 6298,    0,
    0,   18,    0,    0,    0,    0, 5976,    0,  307, 5976,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 -281,  422,  225,   15,  546,  436,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   44,  470, 8837,  275,  241,
    0,  483,  483,  508,    0,    0,   28,  514, -267,    0,
  833,    0,  533,    0,    0,    0,  594,    0, 8881, 6381,
 6786, 6786, 6786, 6786, 6786, 6786, 6786, 6786,    0,    0,
    0,    0,    0,    0,  315,  184, 5976,  564,    0,  556,
  607,    0,    0,    0,    0,  631,  552,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,   74,  534,    0,
    0,    0,    0,    0,  537,    0,    0,  310, -263,  -68,
  714,  605,  612,  619,  625, -287,    0,    0,    0,    0,
    0,  644,    6,  675, -165,    0,  463,  688,  568,    0,
 -267,  628,    0,    0,    0,  826,  838,  708,    0,  778,
 1189, -267,  734,  436,    0, 1906,    0,  275, 8837,    0,
    0,    0,    0,    0, 6381,  649, 6381,    0,    0,    0,
    0,    0,    0, 2233,  -69,  739, 5976,  743, 6381,  -21,
   72,  344, -173,  436,  406,  653,  558,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 6381, 8837,  689,    0,
    0,    0,    0,  409,   81, 5976,  782,    0,  776,  467,
    0, 6298, 6298, 6786, 6786, 6786, 5405, 5000,  709,    0,
    0,  724,    0,    0, 6999,  738, 7074,    0,  727, 6381,
 6381, 6381, 6381, 6381, 6381, 6381, 6381, 6381, 6381, 6381,
 6786, 6786, 6786, 6786,    0,    0, 6786, 6786, 6786, 6786,
 6786, 6786, 6786, 6786, 6786, 6786, 5488, 6786, 6381,  731,
    0,  812,  815, -267, 5976,    0,    0,  836,  810,    0,
 6381, 5083, 8837,    0,  772,  784,    0,    0,  570, -267,
  788,    0,  788,    0,  788,    0,  857,  887,    0, -267,
    0,    0,    0,    0,    0,  866,  608, 7204,  891, 1906,
 -267, -267, -267, -177,  901,  904, 6381,  906, 6381,  916,
  664,    0, -267,  897,  917,    0,   52,    0,    0,  919,
  672,  445, 1906,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  915,
  918,  784,  685,  920,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  750,  483,
  923,  -33,  916,    0, 6381,  404,  552,    0,   60, -233,
    0,    0, 5649, 5405, 5000,   80,    0, 4595,    0,  476,
 8905,  926, 6381,  994,    0,    0,    0, 6786,    0,    0,
 6869,  916,    0,  356,  483,    0,   85,  184,  952,    0,
  607,    0,    0,    0,    0,    0,    0,  752, 6381, 6381,
  929,    0,  932,    0, -180,    0,  483,    0,    0,    0,
 4678,  552,    0,  483,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  631,    0,  631,  310,  310,
 5976, 5976, -263, -263, -263, -263,  -68,  -68,  714,  605,
  612,  619,    0,  933,  625,    0, 6381, 8949, 8981,  850,
 6381,    0,    0,  780,  731,    0,    0,    0,    0,    0,
 -135,    0,    0,  -33,  275,  942, 5732,  859,    0,    0,
  941, 5976,    0,    0,    0,    0,  380,  930,   71,    0,
  -33,  -33,    0,    0,    0,    0,    0,  -33,  -33,    0,
    0,  922,  532,  864,    0,    0,    0,    0,  972, 5976,
 1989, 5976, 6381,  944,  945, 6381, 6381,  947,    0,  949,
 -150,    0,  916, 6542,    0, 6381,  950, 5893,    0,    0,
    0,    0,  734,    0,    0,    0,    0,    0,    0,  943,
    0,  784,    0,    0, 6381,    0,    0,    0,  509,   -9,
  946,    0,  954,  956,  961,    0,    0, 5083,    0, 7275,
    0, 2233, 6054,  393,  968,  963,    0,    0,  761,  966,
    0,    0,    0,  967,    0,  -27,  346,  275,  970,  971,
    0,    0, 6381,    0,    0,    0, 6381,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 4839,    0,    0, 5000,
    0,    0, -173,  974, -101,    0, -104,    0,    0, 6381,
    0,    0,   46,  138,   50,  169,  959,  811,    0,  975,
 6381, 6381,  991,    0,    0,    0, 1064,    0, 1014,  984,
   -9,    0,    0,  987,    0,    0,  461,    0,    0,  989,
  990,  988,  988,  988,  992,  993,  978,  995,    0,  996,
  206,    0,    0,  997,    0,  998, -221,    0,  999,    0,
    0, 1002, 1004,    0, 6381,    0,    0, -267,  916,   19,
    0,    0, 1005, 1006, 1007,    0,    0, 1009, 1906,  976,
  943,    0,  509, 5976,  486,    0, 5976,    0,  129, 1107,
 1121,    0, 4678,    0,    0,  -48, 6137,    0, 5244,  734,
 1015, 5083, 1013,  940,    0,  948,    0,  951,    0,    0,
  916,    0, -197,    0,    0, 5000,    0,    0,    0, 6381,
 1091, 6381, 1092, 6381,    0,    0, 6381,    0, 1041,  953,
    0, 1029,    0,    0, 1014,   -9,    0,  957, 1045, 1040,
    0,  812,    0, 5405,    0,    0, 5976, 1074,    0, 1074,
 1074,    0,    0, 6381,  864, 6381, 1037,  825,    0,    0,
 2150, 6381, 1122,    0, 1906,    0, 1047, 5976,    0,    0,
    0,  916,    0,    0,    0, 1906,    0,    0,    0,    0,
    0, -157,    0, -147, 1048, 1052,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  739,    0,    0,    0,
    0, -285, -105,  969,    0,    0, 1059,    0, 6381, 1083,
 6381,    0,  858, 1062,    0, 6381,    0,    0,    0,    0,
 -159,   -9, 1074,    0,    0,   -9,    0,    0, 1071, 1074,
 1074,  275, 1068,    0, 1001,    0, 1074,    0,    0,    0,
 1074,    0,    0, 1075, 6381,    0, 1003, 6381,    0, 1076,
 6381, 1159, 1906, 1088,  228,  916,    0, 1906, 1906,    0,
    0,  337,    0,    0, 1197, 1198,    0,    0,    0,    0,
    0,    0,    0, 6381, 1109,    0,    0, 6381,    0,  731,
    0,    0,    0,    0, 1086,    0,    0,    0,    0, 5976,
 1093, 1104,    0, 1105, 1106,    0, 1098,    0, 1906,    0,
    0, 1099,    0, 1108, 1906,    0, -184,    0,    0, 1110,
    0,    0,    0, 1111, 6381,    0,    0, 1126, 6381,    0,
    0, 1100, 6381,    0, 4922,   -9,    0,   -9,    0,   -9,
    0,    0,    0, 2150,    0,    0,    0, 6381,    0, 1115,
 -184,    0, -184,    0,    0,    0,    0, 6381, 1127,    0,
    0,    0, 1113,    0,  275, 1112,    0,11533,    0,    0,
    0, 1117,   -9,    0,    0,    0,    0,    0,    0,    0,
    0,  812,11503,    0,    0,    0,    0,    0,    0, 1119,
   -9,    0,    0,    0,    0,    0,  812,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 1120,   -9,    0,
  812, 1125,  998,    0, 1906, 1114,    0,    0, 1906,    0,
 1131, 6381, 1134, 4922,    0,    0, 8418, 1130,    0,    0,
    0,  353, 5327,    0,    0,    0,    0,    0,    0, 1906,
    0,    0, 1906, 1043,    0, 1131,    0,    0, 5976, 5976,
   31,  202,  409, 1018,    0,    0,  559, 1123, 1129,    0,
    0, 5976,    8, -207,    0,    0,    0,  253,  263,    0,
 5976,    0, 5976, -267, 2001, 1139, 1136,  583,    0,    0,
    0,    0,  391,    0,    0, 1061, -108,   10,    0, 1144,
  378,   10,  827,  562,  -56,  830,  124,  124,  -33,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0, -267,    0, -271, 1132,    0,    0,    0, 1906,    0,
 -267, -267, -134, 1145,    0,  498,  -33,    0,    0,  -33,
    0,    0,    0,    0,    0,    0, 1149,    0, 1147,  -33,
 1154,    0, 1156, 5000, 5000,    0,11503,    0, -134, -134,
 1161,    0,    0,    0, 1163, 1158, -134, 1165,  -99,    0,
    0,    0,    0,    0,    0,  -33, -134,    0, 1166, 1168,
  846, 1173,    0,    0,  916,  -99,    0, 1177,    0,    0,
    0,11353,    0,   -9,   -9, 1174, 1175, 1176, 1179,    0,
    0,    0,    0,    0,    0,    0,    0, 1074,    0, 1185,
 1074, 1286, 1295,11383, 1190,11413,    0,  965,    0,    0,
    0,    0,    0, 1192,  610,  610, 1193,    0, -134,    0,
    0,    0,  916,    0,  916,    0,    0,    0,11443,11473,
    0,    0,    0,    0,    0,    0,  622,    0,  622,    0,
    0,    0,    0,    0, 1195,    0, 1906,    0, 1200,    0,
  };
  protected static  short [] yyRindex = {         1430,
    0,    0,    0,    0,    0, 1430,    0, 1566,    0,    0,
    0,    0,    0,    0,    0, 8571,    0,    0,    0,    0,
    0,    0,    0,    0, 1347,    0,    0,    0,    0,  449,
 1194,    0,    0,    0,    0,  684,  453,    0, 1204,    0,
    0,  773,    0, 1566,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  373, 8318,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 2655, 1204, 1209,    0,    0, 1208,    0,
    0, 1214,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
11168,  433, 2789,    0,    0, 2789,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 2903,    0,  510,    0,
    0, 2521, 2521,    0,    0,    0,    0,    0, 1215,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,10887, 1211,    0,    0,    0, 1212,
 1222,    0,    0,    0,    0, 9283, 9110,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 9212,    0,    0, 9558, 9776,10141,
10354,10472,10590,10708,10783,  209,    0,    0,    0,    0,
    0,    0,    0, 1220,    0,    0,  104,    0,    0,    0,
    0,    0,    0,    0,    0, 1148, 1150, 1225,    0,    0,
    0,    0, 2346, 2789,    0, 1228,    0,  535,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  286,    0,    0,    0,    0,    0, -277,
    0, 3017,    0, -242,    0,10826, 3017,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  313,    0,    0, 1226,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1225, 1230,    0,    0,
    0,    0,    0,    0,    0, 3135,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  828,
    0, 1251,  -10,    0,    0,    0,    0,    0,    0,    0,
 1233,    0,    0,    0,    0,    0,    0,    0,  175,    0,
    0,    0,    0,    0,    0,    0,    0, 1234,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1224,    0, 1224,    0,
    0,    0,    0, -255,    0,    0, 8147,    0,    0,    0,
   33, 7249, 1237,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  -39,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 3253,
    0, 8731,    0,    0,    0,  382,    0,  543,    0,    0,
    0,    0, 1238, 1225, 1230,  -61,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  481, 6625,    0,    0, 3253,    0,    0,    0,    0,    0,
 1236,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 -146,    0,    0,    0, 1240,    0, 3253,    0,    0,    0,
    0, 3711,    0, 3253,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0, 9385,    0, 9456, 9629, 9705,
    0,    0, 9852, 9923, 9999,10070,10212,10283,10401,10519,
10637,10755,    0,    0,10827,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  905,    0,    0,    0,    0,    0,
 3885,    0,    0, 8731, 1242,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,   57,  696,    0,
 8731, 8731,    0,    0,    0,    0,    0, 8731, 8731,    0,
    0,   33, 1162,    0,    0,    0,    0,    0,    0,    0,
 1239,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   36,    0,    0,    0,    0,    0,    0,    0,  733,
    0,    0,    0,    0,    0,    0,    0,    0, 9025, 7459,
    0,    0,  839,  842,  843,    0,    0,    0,    0,    0,
    0,    0,    0,11038,    0, 1246,    0,    0,    0,    0,
    0,    0,    0, 1249,    0,  600,  520, 1250,    0, 1252,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 1253,  -91,    0,    0,10978,    0,    0,    0,
    0,    0, -277,    0, -277,    0,    0,  455,    0,  673,
    0,    0, 2265,    0,    0,    0, 3976,    0, 4077,    0,
  -92,    0,    0,    0,    0,    0,  633,  118,    0,    0,
    0,  136,  136,  136,    0,    0,  844, 1255,    0,    0,
    0,    0,    0,    0,    0, 1257,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 1243,    0, 1518,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 1178,
  774,    0, 9069,    0, 9093,    0,    0,    0, 1802,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 3371,
 3475, 1266,    0,    0,    0,    0,    0,    0,    0,    0,
 6625,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 4168, 4269,    0,    0,    0, 1249,
    0, 1124,    0, 1225,    0,    0,    0, 1258,    0, 1258,
 1258,    0,    0,    0,    0,    0,  854,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  721,    0,    0,  860,  868,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 3593,    0,    0,    0,
    0,  600,  600,    0,    0,    0,    5,    0,    0,    0,
    0,    0,  695,  808,    0,    0,    0,    0,    0,    0,
    0, 4353, 1260,  565,    0,  -75,    0,    0,    0,  626,
  626,  368,  235,    0,    0,    0,  637,    0,    0,    0,
  626,    0,    0,    0,    0,    0,    0, 1261,    0,    0,
    0, 1712,    0,    0, 1269,    0,    0,    0,    0,    0,
    0,  745,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  828,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1274,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0, 7543,    0, 8063,    0, 7638,
    0,    0,    0, 1271,    0,    0,    0,    0,    0,    0,
 1276,    0, 4434,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, -210, -224,    0, 8386,    0,    0,
    0,    0, 7722,    0,    0,    0,    0,    0,    0,    0,
    0, 1124, 8497,    0,    0,    0,    0,    0,    0,    0,
 8142,    0,    0,    0,    0,    0, 1124,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 7817,    0,
 1124,    0, 1273,    0,    0,    0,    0,    0,    0,    0,
  823,    0,    0,    0, 7901, 7984,  373,    0,    0,    0,
    0, 4517,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, -209,    0,    0,  823,    0,    0,    0,    0,
    0,    0,  355,    0,    0,    0,    0,  641,    0,    0,
    0,    0,  -87,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1282,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  201,    0,    0,  -22,    0,    0,    0,    0, 8731,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 8611,    0,    0,    0,    0,    0, 1228,    0,
    0,    0, 2902,    0,    0,    0, 8731, 8611,    0, 8731,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 1802,
    0,    0,    0, 1230, 1230,    0,  883,    0, 3016,11203,
    0,    0,    0,    0,    0,    0, 2902,    0,11263, 8686,
 8686,    0, 8686,    0,    0, 1608, 2902,    0,    0,    0,
    0,    0,    0,    0,    0,11263,    0,    0,    0,    0,
    0,    0,    0,11293,11323,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  626,    0,    0,
  626, 1283, 1287,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 2902,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1228,    0,    0,    0,
  };
  protected static  short [] yyGindex = {            0,
    0,  735, 1647, 1648, -478, -547, -722,    0,    0,    0,
    0,    2,    0,    0,    1,    0,    0, -789,  -64,    0,
    0,    0,    0,    0,    0,    0, -492, -458, -388, -574,
 -514, -412, -371, -333,   82,   -4,    0, 1618,    0, 1585,
    0,    0,    0,    0,    0, 1370,  291, 1374,    0,    0,
    0,  620, -562, -684, -666, -663, -482, -440, -410, -963,
    0,  -67,    0,  536,    0, -785,    0,    0,    0,    0,
    0,  589,  248,  538,  837, -781,  -76,    0, 1116, 1317,
 -417,  865, -243,    0,    0,    0,    0, -102, -171,  -24,
 -536,    0,    0,    0,    0,    0,  -62,  457, -477,    0,
    0,  911,  914,  924,    0,    0, -573,  912,    0, -585,
    0,    0,    0,    0,  392,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  634,    0,    0,    0,    0,    0,
  370,-1142,    0,    0,    0,    0,    0,    0,    0,  431,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, -447,    0,
    0,    0,    0,  427,  430,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  791,    0,    0,    0,
  -59,  -85, -193,   47, 1461,  -60,    0,    0,    0, 1437,
 -118,    0,  737,    0, -116, -220,    0,    0, 1398, -231,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, -234,    0, -473, -469,
 -594,    0,  365,    0,    0,  934,    0, -427, -266, 1219,
    0,    0,    0,  925,    0,    0, 1069, -318,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 1409,  -79,
 1408,    0,  938,  861,    0, 1199,  939,    0,    0, 1383,
 1386, 1382, 1387, 1384,    0,    0,    0,    0, 1247,    0,
  955,    0,    0,    0,    0,    0, -567,    0,    0,    0,
    0,  -63,    0,    0,  807,    0,  635,    0,    0,  645,
 -391, -222, -219, -217,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, -703,    0,  -12,    0,
 1352,    0, -581,    0,    0,    0,    0,    0,    0,  715,
    0,    0,  716,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  723,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 1000,    0,    0,    0,    0,    0,    0,    0,
  817,    0,    0,    0,    0,  785,    0,    0,    0,    0,
    0,    0, 1206,    0,    0,    0,    0,  639,    0,    0,
    0,  798,    0,
  };
  protected static  short [] yyTable = {           110,
   16,  106,  231,  114,   31,   37,   16,  650,  106,  733,
  124,  617,  459,  405,  256,  402,  406,  710,  407,  449,
   65,  266,  448,  354,  219,  648,  106,  684,  742,  106,
  488,  685,  908,  447,  720,  721,  241,  242,  755,   37,
  653,  725,  726,  224,  919,  920,  235,   29, 1159,  358,
  303,   82,  922,   33, 1221,  766, 1223,  106,  679,  705,
  371,  268,  269,  270,  271,  272,  273,  274,  275,  116,
  106, 1017,  706,  842, 1097,  679,  221,   37,  106,  264,
   34,   25,  465,  616, 1018,  794,  502,   25,  348, 1102,
  347,  593,  348,  900,   69,  106,  106,  961,  941,  594,
  248,  337,  218,  216,  622,  747,  218,  348,  943,  456,
  385,  348,   35,   65,  385,  642,  372,  964, 1160,  748,
  705, 1226,  338,  340,  968,  969,  340,  425,   68,  645,
   69,  641,  323,  385,  324,  976,  749,  642,  456,  642,
   70,  932,  340,  449,  228,  344,  339, 1205,  642,    2,
  118,  797,  940,  645,  679,  705, 1261,  583,  228,  402,
  645,  814,  645,  229,  451,  354,  217,  110,  385,  949,
  348,  886,  554,  680,  374,  401,  568,  229,  106,  963,
  405,  249,  402,  406, 1329,  407,  452,  649,  572,  860,
  680,  863,  385,  325,  340,  466,  106,  360,  578, 1212,
  228,  887,  516,  518,  467,  962,  370,  793,  228,  590,
  591,  592,  651, 1027,  485,  486,  487,  106,  228,  229,
  772,  604,  456,  218,  456,  106,  843,  229,  813,  986,
  326,    4,  218,  348,  992,  993, 1086,  229,  385,   30,
 1161,  517,  517,  517,  517,   36,  705,  517,  517,  517,
  517,  517,  517,  517,  517,  517,  517,  228,  517,  929,
  448,  228, 1101, 1251,  616,   28,    4,  899,  924,  796,
  595, 1019,  330,  463,   69, 1013,  229, 1106,  385,  451,
  229, 1016,  348,  244,  106,  303, 1140,  225,  748,   27,
  942, 1109,  106,  573,  453,  574,  454,  575,  455,  684,
  944,  385,   67,  685,  340,  852,  340, 1213,  628,  105,
  327,  226,  328,   72,  581,  229,  871,  582,  607,  401,
  118,    1,  625,  340,  474, 1072,  340,  601,  602,  350,
  303,  329,    4,  330,  213,    1, 1141,  215,  227, 1206,
  800,  348,  401, 1073,  802,  620, 1074,  348,  133,  950,
  133,  648,   69,  350,  626,   53,    4,  350,   69,  118,
  981,  350,  784,    2,  880,  238,  608,  858,   70,  162,
  402,  228,   53,  517,  719,  448,  776,  248,  255,   72,
  637, 1111,  266,  402,   73,  303,  261,   72,  664,  669,
  229,  485,  216,  245, 1072,  276,  374,  246,  340,  567,
  106,  341,  425,  285,  290,  449, 1135,  449,  488,  665,
  448,  681, 1073,   72,    3, 1074,  237,  106,  686,  502,
  348,  447,   74,  609,  350,   69,  348,  785,  304,   69,
  263,  278,  733, 1054,  118,   70,  351,  228,  350,   70,
  264,  279,  304,  458,    4, 1059,  228, 1071,  352,   83,
  228,   84,  126,  343,   85,   74,  229, 1142,  249,   86,
  106,  106,  657,   88,  574,  229,  303,  348,  223,  229,
  865,  350,   91,  348,  133,  635,  133,  106,  106,   92,
  488,  349,  242,  489,   93,  351, 1054,  349,   94,  228,
  281,  248, 1298, 1055,    4, 1301,  760,  282, 1059,  635,
   95,  106,   96,  228,  460,  350,   97, 1143,  229,  283,
  639,  640,  308, 1039,   98,   99, 1071, 1068,  100,  464,
   76,  117,  229,  305,  306,  307,  308, 1075,  476,  106,
  401,  106,  670,  477,  228,  438,  405,  441,  402,  406,
  752,  407,  350,  401,  348,  761, 1055, 1040,  574,  462,
 1039, 1069,  350,  229,  574,  574,  574,  574,  574,  574,
  574,  574,  574,  574,  574,  574,  351,  473,  348, 1076,
  348,  228,  249,  574,  637,  574,  848,  574,  732,  574,
  574,  574,  482,  483, 1040,  801, 1068,  438,  492,  783,
  229,  754,  555,  228,   72, 1056, 1075,  224,  637, 1077,
  505,  506,  507,  508,  509,  510,  511,  512,  513,  514,
  515,  239,  229,  223,   78,  348,  803, 1041,  228,   79,
 1069, 1070,  240,  350,  402,   65, 1119,  534,  228,  536,
   87,  284, 1217,  947,   89,  402, 1057,  229, 1076,  348,
  228,  560,  564, 1253, 1254,  615, 1200,  229, 1056,  348,
  479, 1259,  348,  837, 1041, 1201,  479,  348,  276,  229,
  348, 1270,  348,  372,  348,  348,  348,  348, 1077,  615,
 1235,  115,  348, 1238, 1058,  989,  348,  560,  348,  560,
  348,  593,  348,  593,  321,  322,  851,  276,  348, 1057,
 1070,  348,  277,  348,  278, 1131,  615,  348,  401,  517,
  727,  348,  994,  106,  279,  616,  106,  368,  658, 1269,
 1164,  818,  402, 1318,  995,  786, 1202,  402,  402,  122,
  348,  617,  288,  278,  348,  285,  239, 1058,  665,  616,
 1236,  368,  123,  279,  223,  263,  276,  668,  368,  348,
   43, 1209,   46,  348,  438,  492,  483,  350,  564,  350,
  239,  350,  483,  662,  214,  907,  616,   69,  402,  222,
  348,  718, 1266, 1267,  402, 1268,  106,   70,  687,  687,
  777,  228,  278,  350,  468,  350, 1249, 1250,   81,  674,
  674,  638,  279,  278,  401,  694,  696,  106,   69,  937,
  469,  683,  281,  279,  809,  401,  241,  242,   70,  282,
  809,  809,  348,  288,  809,  809,   47,  809,  809,  717,
  297,  283,  298,  350,  299,  281,   16,  763,  348,   67,
   67,  348,  282,   67,  223,  809,  824,  691,  351,  615,
   48,  698,  228,  612,  283,  480,  300,  731,  301,  737,
  352,  228,  353,   49,  655,  229,  365,  714,   51,  365,
  111,  229,   70,   52,  216,   53,   54,   55,   56,  112,
  229, 1232,   69,   57,  402,  365,  405,   58,  402,  406,
 1233,  407,  401,  991,  243, 1234,  302,  401,  401,   59,
  354,  247,   60,  738,   61,  340,  691,  743,  340,  402,
  405,  354,  402,  406,  753,  407,  691,  297,  758,  298,
  257,  299,   16,  309,  340,  355,  818,  484,  392,  106,
  392,  228,  392,  484,  310,  762,  355,  297,  401,  298,
  612,  299, 1149,  300,  401,  301,  292,  351,  564,  334,
  229, 1150,  291,  438,  392,  334,  392,  278,  356,  352,
  357,   69,  335,  300,  106,  301,  111,  279,  311,  353,
  312,   70,  313,  789,  314, 1196,  315,  790,  316,   70,
  317,  259,  318,  302,  319,  348,  320,  492,  348,  348,
  492,  111, 1169,  111,  392,  247,  405,  293,  402,  406,
  799,  407, 1312,  302,  348, 1322, 1207,  333,  348,  638,
 1210,  808,  809,  217, 1323, 1215, 1216,  139,  638,  334,
  639,  139,  335,  139,  286,  139, 1038,  341, 1053,  639,
 1038,  862,  805,  286,  864,  294,  295,  296,  805,  805,
 1218,  231,  805,  805,  401,  805,  805,  111,  401, 1224,
 1225,  259,  336,  106,  812,  847,  106,  471,  295,  296,
  812,  812,  106, 1038,  812,  812,  345,  812,  812,  401,
  348,  348,  401,  348,  348,   60,  611,  355,  106,  106,
  612, 1053, 1166,  683,  133,  812,  133,  872,  133,  621,
  537,  106,  564,  612,  912,  359,  538,  539,  366, 1038,
  106,  362,  106,  540,  541, 1197,  492,  175,  253,  175,
  888,  175,  890,  364,  892,  935,  440,  893,   83,  228,
   84,  687,  458,   85,  405,  687,  402,  406,   86,  407,
  461,  173,   88,  173,  909,  173,  624,  331,  673,  332,
  625,   91,  625,   24,  738, 1147,  691,  780,   92,  701,
  702,  625,  930,   93, 1158, 1162,  475,   94,  401,   68,
 1165,  873,  688,   68,  479,  879,  688,  806,  356,   95,
  369,   96,  478,  806,  806,   97,  497,  806,  806, 1284,
  806,  806,  802,   98,   99,  805,  806,  100,  802,  802,
  117,  498,  802,  802,  504,  802,  802,  789,  789,  953,
  558,  955,  559, 1309,  552, 1310,  959,  523,  524,  525,
  526,  157, 1276,  926,  278,  927,  254,  835,  228, 1211,
  356,  557, 1214, 1227, 1273,  162, 1274,  162,  169,  170,
  169,  170,  956,  957,  121,  978,  121,  912,  691,  568,
  110,  984,  773,  576,  773,   47,  190, 1282,  190, 1227,
 1227,  569, 1313, 1313,  163,  343,  163, 1227,  580, 1262,
 1320,  617, 1321,  617,  998,  110,  110, 1227,  893,   48,
   24,  598, 1035,  600,  790,  790, 1262,  577,  830,  831,
  519,  520,   49,  585, 1286, 1288,  401,   51,  596,  527,
  528,  597,   52,  599,   53,   54,   55,   56,   47,  111,
   74,  605,   57,  610,  606,  738,   58,  618,  663, 1029,
  619,  627,  623, 1031,  661,  672,  677,  697,   59, 1227,
  678,   60,   48,   61,  690,  711,  348,  716,  738,  353,
  612,  727,  730,  356,  768,   49,  740,  741, 1091,  744,
   51,  746,  756, 1145,  769,   52,  770,   53,   54,   55,
   56,  771,  778,  779,  781,   57,  804,  782,  787,   58,
  788, 1035,  795, 1146, 1124,  807,   52,  810,    2,    3,
 1134,   59,  817,  823,   60,  834,   61,  825,  826,  827,
  832,  833,  869,  836,  859,  835, 1138, 1139,  841,  840,
  845,  844,  846,  854,  855,  856,  870,  881,  453, 1157,
   24,  857, 1116,  228,   24,  889,  891,  882, 1167,   24,
 1168,   24,  896,  898,   24,  883,   24,   24,  884,   24,
  897,   24,  229,   24,  904,   24,   24,   24,   24,  905,
  906,   24,   24,  915,  925,  934,  931,   24,  945,   24,
   24,   24,  946,  951,   24,   24,   24,  952,   24,    5,
  954,   24,  958,   24,   24,   24,   24,  967,  970,  985,
   24,   24,   24,  977,  983,   24,   24,   24,  971,   47,
  837,  987,  996,  997,   24,   24,  999,   24,   24,   24,
   24,   24,   24, 1003, 1005,  217,   24, 1006, 1008, 1010,
 1012, 1014, 1030,   48, 1028, 1092, 1015, 1026, 1025, 1087,
 1093, 1099, 1094, 1104, 1107, 1112,   49, 1114,   24,   24,
 1136,   51, 1155, 1110, 1153, 1222,   52,   24,   53,   54,
   55,   56, 1117, 1130, 1193, 1194,   57,   24, 1204, 1208,
   58,   24, 1231, 1244,  492,  492,   24, 1242,   24, 1246,
 1248,   24,   59,   24, 1258,   60,   24,   61,   24, 1255,
   24, 1257,   24, 1260, 1271,   24, 1272, 1275,   24,   24,
 1279, 1302, 1290, 1291, 1292, 1293,   24,   24,   24, 1300,
 1304,   24,   24,   24, 1308,   24, 1311, 1316,   24, 1328,
   24,   24,   24,   24, 1330,    5,   28,   24,   24,   24,
   26,   24,   24,   24,   24,   27,   73,   22,  518,  594,
   74,   24,   24,  327,   24,   24,   24,   24,   24,   24,
   72,  494,  653,   24,  595,  206,  749,   96,  445,  749,
  495,  654,  435,   52,   71,  328,  761,   54,  446,  488,
  436,  719,   54,   26,   54,   24,   24,   54,  506,   54,
  507,  638,   54,  450,   54,  490,   54,  689,   54,  722,
   27,   54,  638,  723,   54,   54,  359,  757,  700,  725,
  701,  727,   54,   54,   54,  191,  311,   54,   54,   54,
  315,   54,   44,   45,   54,   75,   54,   54,   54,   54,
  125,  481, 1100,   54,   54,   54,  484, 1108,   54,   54,
   54,  923,  570,  715,  861, 1163, 1245,   54,   54,  866,
   54,   54,  867,   54,   54,   54, 1277, 1315, 1326,   54,
   53, 1289,  868, 1287, 1105,   53,  966,   53,   24,  437,
   53,  470,   53,  500,  877,   53, 1004,   53,  676,   53,
  774,   53,  876,  499,  503,  529,  531,   53,   53,  530,
  689,  535,  532,  973,  671,   53,   53,   53, 1118,  584,
   53,   53,   53, 1113,   53, 1088, 1083,   53, 1090,   53,
   53,   53,   53,  980, 1001,  885,   53,   53,   53,  853,
  704,   53,   53,   53, 1137, 1000,    0,    0,    0,    0,
   53,   53,    0,   53,   53,    0,   53,   53,   53,    0,
    0,    0,   53,  751,    0,    0,    0,    0,    0,    0,
    0,    0,  751,  751,  751,  751,  751,    0,  751,  751,
    0,  751,  751,  751,   54,  751,  751,  751,  751,    0,
    0,    0,    0,  751,    0,  751,  751,  751,  751,  751,
  751,    0,    0,  751,    0,    0,    0,  751,  751,    0,
  751,  751,  751,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  751,    0,  751,    0,  751,  751,    0,    0,
  751,    0,  751,  751,  751,  751,  751,  751,  751,  751,
  751,  751,  751,  751,    0,  751,    0,    0,  751,    0,
    0,    0,    0,  751,    0,    0,    0,    0,    0,    0,
    0,    0,    0,   53,    0,   53,    0,   53,   53,    0,
    0,  751,  751,   53,    0,  751,    0,   53,    0,    0,
  751,  751,  751,  751,  751,    0,   53,    0,    0,    0,
  751,    0,  751,   53,    0,    0,    0,    0,   53,  751,
    0,  751,   53,    0,   53,    0,   53,    0,    0,    0,
    0,   53,    0,    0,   53,    0,   53,    0,    0,    0,
   53,    0,    0,   53,    0,    0,    0,    0,   53,   53,
    0,    0,   53,    0,    0,   53,    0,    0,    0,    0,
    0,    0,    0,  751,    0,  751,    0,  751,    0,  751,
    0,  751,    0,  751,    0,  751,  751,  695,    0,    0,
    0,  751,    0,  751,  157,    0,  695,  695,  695,  695,
  695,    0,  695,  695,    0,  695,  695,  695,    0,  695,
  695,  695,    0,    0,    0,    0,    0,  695,    0,  695,
  695,  695,  695,  695,  695,    0,    0,  695,    0,    0,
    0,  695,  695,    0,  695,  695,  695,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  695,    0,  695,    0,
  695,  695,    0,    0,  695,    0,  695,  695,  695,  695,
  695,  695,  695,  695,  695,  695,  695,  695,    0,  695,
    0,    0,  695,    0,    0,   53,    0,  695,    0,    0,
    0,    0,    0,    0,    0,    0,    0,   53,    0,   53,
    0,    0,   53,    0,    0,  695,  695,   53,    0,  695,
    0,   53,    0,    0,  695,  695,  695,  695,  695,    0,
   53,    0,    0,    0,  695,    0,  695,   53,    0,    0,
    0,    0,   53,  695,    0,  695,   53,    0,   53,    0,
   53,    0,    0,    0,    0,   53,    0,    0,   53,    0,
   53,    0,    0,    0,   53,    0,    0,   53,    0,    0,
    0,    0,   53,   53,    0,    0,   53,    0,    0,   53,
    0,    0,    0,    0,    0,    0,    0,  695,    0,  695,
    0,  695,    0,  695,    0,  695,    0,  695,    0,  695,
  695,  375,    0,    0,    0,  695,    0,  695,    0,    0,
  127,   83,  376,   84,    0,    0,   85,  377,    0,  378,
  379,   86,    0,  129,  380,   88,    0,    0,    0,    0,
    0,  130,    0,  381,   91,  382,  383,  384,  385,    0,
    0,   92,    0,    0,    0,  386,   93,    0,  131,  132,
   94,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  387,    0,   95,    0,   96,  133,    0,    0,   97,    0,
  388,  134,  389,  135,  390,  136,   98,   99,  391,  392,
  100,  393,    0,  394,  375,    0,  395,    0,    0,   53,
    0,  139,    0,  127,   83,    0,   84,    0,    0,   85,
  128,    0,    0,    0,   86,    0,  129,    0,   88,  111,
    0,    0,    0,  140,  130,    0,    0,   91,  396,  141,
  142,  143,  144,    0,   92,    0, 1170,    0,  145,   93,
  146,  131,  132,   94,    0,    0,    0,  147,    0,  148,
    0,    0,    0,    0,    0,   95,    0,   96,  133,    0,
    0,   97,    0,    0,  134,    0,  135,    0,  136,   98,
   99,  137,    0,  100,    0,    0,  394,    0, 1171,    0,
    0,    0,    0,    0,  139,    0,    0,    0,    0,    0,
    0,  149,    0,  150,    0,  151,    0,  152,    0,  153,
    0,  154,    0,  397,  156,    0,  140,    0,    0,  157,
    0,    0,  141,  142,  143,  144,    0,    0,    0,    0,
    0,  145,    0,  146, 1172, 1173, 1174, 1175,    0, 1176,
  147, 1177,  148, 1178, 1179, 1180, 1181, 1182, 1183,    0,
    0,    0, 1184,    0, 1185,    0, 1186,    0, 1187,    0,
 1188,    0, 1189,    0, 1190,  375, 1191,    0,    0,    0,
    0,    0,    0,    0,  127,   83,    0,   84,    0,    0,
   85,  128,    0,    0,  149,   86,  150,  129,  151,   88,
  152,    0,  153,    0,  154,  130,  262,  156,   91,    0,
    0,    0,  157,    0,    0,   92,    0,    0,    0,    0,
   93,    0,  131,  132,   94,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,   95,    0,   96,  133,
    0,    0,   97,    0,    0,  134,    0,  135,    0,  136,
   98,   99,  137,    0,  100,    0,    0,  138,  444,    0,
    0,    0,    0,    0,    0,  139,    0,  127,   83,    0,
   84,    0,    0,   85,  128,    0,    0,    0,   86,    0,
  129,    0,   88,    0,    0,    0,    0,  140,  130,    0,
  815,   91,    0,  141,  142,  143,  144,    0,   92,    0,
    0,    0,  145,   93,  146,  131,  132,   94,    0,    0,
    0,  147,    0,  148,    0,    0,    0,    0,    0,   95,
    0,   96,  133,    0,    0,   97,    0,    0,  134,    0,
  135,    0,  136,   98,   99,  137,    0,  100,    0,    0,
  138,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  149,    0,  150,    0,  151,
    0,  152,    0,  153,    0,  154,    0,  262,  156,    0,
  445,  486,    0,  157,  815,    0,  486,  486,    0,    0,
  815,  815,  815,  815,  815,  815,  815,  815,  815,  815,
  815,    0,    0,    0,    0,    0,    0,    0,    0,  815,
  486,  815,    0,  815,    0,  815,  815,  815,    0,    0,
  486,    0,    0,  486,  486,    0,    0,    0,  486,    0,
    0,  486,    0,  486,    0,  486,  486,  486,  486,    0,
    0,    0,    0,  486,    0,    0,    0,  486,  149,    0,
  150,  486,  151,    0,  152,    0,  153,    0,  154,  486,
  446,    0,  486,    0,  486,  486,  157,    0,    0,    0,
    0,  486,  486,  486,  486,  486,  486,  486,  486,  486,
  486,  486,  486,    0,    0,    0,    0,    0,    0,  486,
  486,    0,  486,  486,  486,  486,  486,  486,  486,    0,
  486,  486,    0,  486,  486,    0,  486,  486,  486,  486,
  486,  486,  486,  486,  486,    0,    0,  486,    0,  486,
    0,  486,    0,  486,    0,  486,    0,  486,    0,  486,
    0,  486,    0,  486,    0,  486,    0,  486,    0,  486,
    0,  486,    0,  486,    0,  486,    0,  486,    0,  486,
    0,  486,    0,  486,    0,  486,  348,  486,    0,  486,
    0,  348,  348,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  486,  486,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  348,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  348,    0,    0,  348,  348,
    0,    0,    0,  348,    0,    0,  348,    0,  348,    0,
  348,  348,  348,  348,    0,    0,    0,    0,  348,    0,
    0,    0,  348,    0,    0,    0,  348,    0,    0,    0,
    0,    0,    0,    0,  348,    0,    0,  348,    0,  348,
  348,    0,    0,    0,    0,    0,  348,  348,  348,  348,
  348,  348,  348,  348,  348,  348,  348,  348,    0,    0,
    0,    0,    0,    0,  348,  348,  348,  348,  348,  348,
  348,  348,  348,  348,    0,    0,    0,    0,    0,  348,
    0,  348,  348,  348,  348,  348,    0,    0,  348,  348,
  348,    0,    0,    0,    0,  348,  348,    0,    0,    0,
  348,    0,  348,    0,  348,    0,  348,    0,  348,    0,
  348,    0,    0,    0,    0,    0,    0,    0,    0,  348,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  348,
    0,    0,  348,  348,  348,    0,    0,  348,    0,    0,
    0,    0,  348,    0,  348,  348,  348,  348,  348,  348,
    0,    0,  348,    0,    0,    0,  348,    0,    0,    0,
  348,    0,    0,    0,    0,    0,    0,    0,  348,    0,
    0,  348,    0,  348,  348,    0,    0,    0,    0,    0,
  348,  348,  348,  348,  348,  348,  348,  348,  348,  348,
  348,  348,    0,    0,    0,    0,    0,    0,  348,  348,
  348,  348,  348,  348,  348,  348,  348,  348,    0,    0,
    0,    0,    0,  348,    0,  348,  348,  348,  348,  348,
    0,    0,  348,  348,  340,    0,    0,    0,    0,  340,
  340,    0,    0,    0,  348,    0,  348,    0,  348,    0,
  348,    0,  348,    0,  348,    0,    0,    0,    0,    0,
    0,    0,    0,  340,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  340,    0,    0,  340,  340,  348,    0,
    0,  340,    0,    0,  340,    0,  340,    0,  340,  340,
  340,  340,  348,  348,    0,    0,  340,    0,    0,    0,
  340,    0,    0,    0,  340,    0,    0,    0,    0,    0,
    0,    0,  340,    0,    0,  340,    0,  340,  340,    0,
    0,    0,    0,    0,  340,  340,  340,  340,  340,  340,
  340,  340,  340,  340,  340,  340,    0,    0,    0,    0,
    0,    0,  340,  340,  340,  340,  340,  340,  385,  340,
  340,  340,   53,    0,  385,    0,    0,  340,    0,  340,
  340,  340,  340,  340,    0,    0,  340,    0,    0,    0,
    0,    0,    0,    0,    0,    0,   53,    0,  340,    0,
  340,    0,  340,    0,  340,    0,  340,  385,  340,   53,
    0,  385,    0,    0,   53,    0,    0,    0,    0,   53,
    0,   53,   53,   53,   53,    0,    0,    0,    0,   53,
    0,    0,  340,   53,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   53,  340,  340,   53,    0,
   53,    0,  385,    0,    0,    0,    0,    0,  385,  385,
  385,  385,  385,  385,  385,  385,  385,  385,  385,  385,
   53,    0,   53,    0,    0,    0,  385,  385,  385,  385,
  385,  385,  350,  385,  385,  385,   53,    0,  350,    0,
    0,  385,    0,  385,  385,  385,  385,    0,    0,    0,
  385,  385,    0,    0,    0,    0,    0,    0,    0,    0,
   53,    0,  385,    0,  385,    0,  385,    0,  385,    0,
  385,    0,  385,   53,    0,  350,    0,    0,   53,    0,
    0,    0,    0,   53,    0,   53,   53,   53,   53,    0,
    0,    0,    0,   53,    0,    0,  385,   53,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,   53,
  385,  385,   53,    0,   53,    0,  350,    0,    0,    0,
    0,    0,  350,  350,  350,  350,  350,  350,  350,  350,
  350,  350,  350,  350,   53,    0,   53,    0,    0,    0,
  195,  350,  350,  350,  350,  350,  350,  350,  350,  350,
  420,  350,  350,    0,  350,  350,  420,  350,    0,  350,
  350,  350,  350,  350,  350,  350,    0,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,  350,  420,  350,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  350,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  420,    0,    0,    0,    0,    0,
  420,  420,  420,  420,  420,  420,  420,  420,  420,  420,
  420,  420,    0,    0,    0,    0,    0,    0,    0,  420,
  420,  420,  420,  420,  420,  420,  420,  420,  348,  420,
  420,    0,  420,  420,  348,  420,    0,  420,  420,  420,
  420,  420,  420,  420,    0,    0,  420,    0,  420,    0,
  420,    0,  420,    0,  420,    0,  420,    0,  420,    0,
  420,    0,  420,    0,  420,    0,  420,    0,  420,    0,
  420,  348,  420,    0,  420,    0,  420,    0,  420,    0,
  420,    0,  420,    0,  420,    0,  420,    0,  420,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  420,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  348,    0,    0,    0,    0,    0,  348,  348,
  348,  348,  348,  348,  348,  348,  348,  348,  348,  348,
    0,    0,    0,    0,    0,    0,    0,  348,  348,  348,
  348,  348,  348,  348,  348,  348,  486,  348,  348,    0,
  348,  348,  486,  348,    0,  348,  348,  348,  348,  348,
  348,  348,    0,    0,  348,    0,  348,    0,  348,    0,
  348,    0,  348,    0,  348,    0,  348,    0,  348,    0,
  348,    0,  348,    0,  348,    0,  348,    0,  348,  486,
  348,    0,  348,    0,  348,    0,  348,    0,  348,    0,
  348,    0,  348,    0,  348,    0,  348,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  348,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  486,    0,    0,    0,    0,    0,  486,  486,  486,  486,
  486,  486,  486,  486,  486,  486,  486,  486,    0,    0,
  431,    0,    0,    0,  486,  486,  431,  486,  486,  486,
  486,  486,  486,  486,    0,  486,  486,    0,  486,  486,
    0,  486,    0,  486,  486,  486,  486,  486,  486,  486,
    0,    0,  486,    0,  486,    0,  486,    0,  486,    0,
  486,    0,  486,  431,  486,    0,  486,    0,  486,    0,
  486,    0,  486,    0,  486,    0,  486,    0,  486,    0,
  486,    0,  486,    0,  486,    0,  486,    0,  486,    0,
  486,    0,  486,    0,  486,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  431,    0,    0,    0,  486,    0,
  431,  431,  431,  431,  431,  431,  431,  431,  431,  431,
  431,  431,    0,    0,    0,    0,    0,    0,    0,  431,
  431,  431,  431,  431,  431,  431,  431,  431,  498,  431,
  431,    0,  431,  431,  498,  431,    0,  431,  431,  431,
  431,  431,  431,  431,    0,    0,  431,    0,  431,    0,
  431,    0,  431,    0,  431,    0,  431,    0,  431,    0,
  431,    0,  431,    0,  431,    0,  431,    0,  431,    0,
  431,  498,  431,    0,  431,    0,  431,    0,  431,    0,
  431,    0,  431,    0,  431,    0,  431,    0,  431,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  431,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  498,    0,    0,    0,    0,    0,  498,  498,
  498,  498,  498,  498,  498,  498,  498,  498,  498,  498,
    0,    0,    0,    0,    0,    0,    0,  498,  498,  498,
  498,  498,  498,  498,  498,  498,  428,  498,  498,    0,
  498,  498,  428,  498,    0,  498,  498,  498,  498,  498,
  498,  498,    0,    0,  498,    0,  498,    0,  498,    0,
  498,    0,  498,    0,  498,    0,  498,    0,  498,    0,
  498,    0,  498,    0,  498,    0,  498,    0,  498,  428,
  498,    0,  498,    0,  498,    0,  498,    0,  498,    0,
  498,    0,  498,    0,  498,    0,  498,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  498,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  428,    0,    0,    0,    0,    0,  428,  428,  428,  428,
  428,  428,  428,  428,  428,  428,  428,  428,    0,    0,
    0,    0,    0,    0,    0,  428,    0,  428,    0,  428,
    0,  428,  428,  428,    0,  428,  428,    0,  428,  428,
    0,  428,    0,  428,  428,  428,  428,  428,  428,  428,
    0,    0,    0,    0,    0,    0,  428,    0,  428,    0,
  428,    0,  428,    0,  428,    0,  428,    0,  428,    0,
  428,    0,  428,    0,  428,    0,  428,    0,  428,    0,
  428,    0,  428,    0,  428,    0,  428,    0,  428,    0,
  428,   37,    0,    0,  428,   37,    0,    0,    0,    0,
   37,    0,   37,    0,    0,   37,    0,   37,  428,    0,
   37,    0,   37,    0,   37,    0,   37,    0,    0,    0,
    0,    0,   37,   37,    0,    0,    0,    0,    0,    0,
   37,   37,   37,    0,    0,   37,   37,   37,    0,   37,
    0,    0,   37,    0,   37,   37,   37,   37,    0,    0,
    0,   37,   37,   37,    0,    0,   37,   37,   37,    0,
    0,    0,    0,    0,    0,   37,   37,    0,   37,   37,
   37,   37,   37,   37,    0,    0,    0,   37,    0,    0,
    0,    0,   38,    0,    0,    0,   38,    0,    0,    0,
    0,   38,    0,   38,    0,    0,   38,    0,   38,   37,
   37,   38,    0,   38,    0,   38,    0,   38,    0,    0,
    0,    0,    0,   38,   38,    0,    0,    0,    0,    0,
    0,   38,   38,   38,    0,    0,   38,   38,   38,    0,
   38,    0,    0,   38,    0,   38,   38,   38,   38,    0,
    0,    0,   38,   38,   38,    0,    0,   38,   38,   38,
    0,    0,    0,    0,    0,    0,   38,   38,    0,   38,
   38,   38,   38,   38,   38,    0,    0,    0,   38,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   37,   35,    0,    0,    0,   35,    0,    0,
   38,   38,   35,    0,   35,    0,    0,   35,    0,   35,
    0,    0,   35,    0,   35,    0,   35,    0,   35,    0,
    0,   35,    0,    0,   35,   35,    0,    0,    0,    0,
    0,    0,   35,   35,   35,    0,    0,   35,   35,   35,
    0,   35,    0,    0,   35,    0,   35,   35,   35,   35,
    0,    0,    0,   35,   35,   35,    0,    0,   35,   35,
   35,    0,    0,    0,    0,    0,    0,   35,   35,    0,
   35,   35,    0,   35,   35,   35,    0,    0,    0,   35,
    0,    0,    0,   38,   36,    0,    0,    0,   36,    0,
    0,    0,    0,   36,    0,   36,    0,    0,   36,    0,
   36,   35,   35,   36,    0,   36,    0,   36,    0,   36,
    0,    0,   36,    0,    0,   36,   36,    0,    0,    0,
    0,    0,    0,   36,   36,   36,    0,    0,   36,   36,
   36,    0,   36,    0,    0,   36,    0,   36,   36,   36,
   36,    0,    0,    0,   36,   36,   36,    0,    0,   36,
   36,   36,    0,    0,    0,    0,    0,    0,   36,   36,
    0,   36,   36,    0,   36,   36,   36,    0,    0,    0,
   36,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   35,   39,    0,    0,    0,   53,
    0,    0,   36,   36,   53,    0,   53,    0,    0,   53,
    0,   53,    0,    0,   53,    0,   53,    0,   53,    0,
   53,    0,    0,   53,    0,    0,   53,   53,    0,    0,
    0,    0,    0,    0,   53,   53,   53,    0,    0,   53,
   53,   53,    0,   53,    0,    0,   53,    0,   53,   53,
   53,   53,    0,    0,    0,   53,   53,   53,    0,    0,
   53,   53,   53,    0,    0,    0,    0,    0,    0,   53,
   53,    0,   53,   53,    0,   53,   53,   53,    0,   40,
    0,   53,    0,   53,    0,   36,    0,    0,   53,    0,
   53,    0,    0,   53,    0,   53,    0,    0,   53,    0,
   53,    0,   53,   39,   53,    0,    0,   53,    0,    0,
   53,   53,    0,    0,    0,    0,    0,    0,   53,   53,
   53,    0,    0,   53,   53,   53,    0,   53,    0,    0,
   53,    0,   53,   53,   53,   53,    0,    0,    0,   53,
   53,   53,    0,    0,   53,   53,   53,    0,    0,    0,
    0,    0,    0,   53,   53,    0,   53,   53,    0,   53,
   53,   53,    0,    0,    0,   53,    0,    0,  704,  704,
  704,  704,    0,    0,  704,  704,    0,  704,  704,  704,
    0,  704,  704,  704,    0,    0,   53,   40,    0,  704,
    0,  704,  704,  704,  704,  704,  704,    0,    0,  704,
    0,    0,    0,  704,  704,    0,  704,  704,  704,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  704,    0,
  704,    0,  704,  704,    0,    0,  704,    0,  704,  704,
  704,  704,  704,  704,  704,  704,  704,  704,  704,  704,
    0,  704,    0,    0,  704,    0,    0,    0,    0,  704,
    0,    0,  227,    0,  227,    0,    0,  227,    0,  615,
    0,    0,  227,    0,    0,    0,  227,  704,    0,  227,
   53,  704,    0,    0,    0,  227,  704,  704,  704,  704,
  704,    0,  227,  615,    0,    0,  704,  227,  704,    0,
    0,  227,    0,    0,    0,  704,    0,  704,    0,    0,
    0,    0,    0,  227,    0,  227,    0,    0,    0,  227,
  615,    0,    0,    0,    0,    0,    0,  227,  227,    0,
    0,  227,    0,    0,  227,    0,    0,    0,    0,  127,
   83,    0,   84,    0,    0,   85,  128,    0,    0,  704,
   86,  704,  129,  704,   88,  704,    0,  704,    0,  704,
  130,  704,  704,   91,    0,    0,    0,  704,    0,    0,
   92,    0,    0,    0,    0,   93,    0,  131,  132,   94,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   95,    0,   96,  133,  562,    0,   97,    0,    0,
  134,    0,  135,    0,  136,   98,   99,  137,    0,  100,
    0,    0,  138,    0,    0,    0,  563,    0,    0,    0,
  139,    0,  127,   83,    0,   84,    0,    0,   85,  128,
    0,    0,    0,   86,    0,  129,    0,   88,  458,  652,
    0,    0,  140,  130,  227,    0,   91,    0,  141,  142,
  143,  144,    0,   92,    0,    0,    0,  145,   93,  146,
  131,  132,   94,    0,  489,    0,  147,    0,  148,    0,
    0,  490,    0,    0,   95,    0,   96,  133,    0,    0,
   97,    0,    0,  134,    0,  135,    0,  136,   98,   99,
  137,    0,  100,    0,    0,  138,    0,    0,    0,  491,
    0,    0,    0,  139,    0,    0,    0,    0,    0,    0,
  149,    0,  150,    0,  151,    0,  152,    0,  153,    0,
  154,    0,  262,  156,    0,  140,  682,    0,  157,    0,
    0,  141,  142,  143,  144,    0,    0,    0,    0,    0,
  145,    0,  146,    0,    0,    0,    0,    0,    0,  147,
    0,  148,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  127,   83,    0,   84,    0,    0,   85,
  128,    0,    0,  149,   86,  150,  129,  151,   88,  152,
    0,  153,    0,  154,  130,  262,  156,   91,    0,    0,
    0,  157,    0,    0,   92,    0,    0,    0,    0,   93,
    0,  131,  132,   94,    0,  489,    0,    0,    0,    0,
    0,    0,  490,    0,    0,   95,    0,   96,  133,    0,
    0,   97,    0,    0,  134,    0,  135,    0,  136,   98,
   99,  137,    0,  100,    0,    0,  138,    0,    0,    0,
  491,    0,    0,    0,  139,    0,    0,   83,    0,   84,
    0,    0,   85,    0, 1032,    0,    0,   86,    0,    0,
    0,   88,    0,    0,    0,    0,  140,  792,    0,    0,
   91,    0,  141,  142,  143,  144,    0,   92,    0,    0,
    0,  145,   93,  146, 1033,    0,   94,    0,    0,    0,
  147,    0,  148,    0,    0,    0,    0,    0,   95,    0,
   96,    0,    0,    0,   97, 1034,    0,    0,    0,    0,
    0,    0,   98,   99,    0,    0,  100,    0,    0,  117,
    0,    0,    0,    0,  127,   83,    0,   84,    0,    0,
   85,  128,    0,    0,  149,   86,  150,  129,  151,   88,
  152,    0,  153,    0,  154,  130,  262,  156,   91,    0,
    0,    0,  157,    0,    0,   92,    0,    0,    0,    0,
   93,    0,  131,  132,   94,    0,  489,    0,    0,    0,
    0,    0,    0,  490,    0,    0,   95,    0,   96,  133,
    0,    0,   97,    0,    0,  134,    0,  135,    0,  136,
   98,   99,  137,    0,  100,    0,    0,  138,    0,    0,
    0,  491,    0,    0,    0,  139,    0,  127,   83,    0,
   84,    0,    0,   85,  128,    0,    0,    0,   86,    0,
  129,    0,   88,    0,    0,    0,    0,  140,  130,   74,
    0,   91,    0,  141,  142,  143,  144,    0,   92,    0,
    0,    0,  145,   93,  146,  131,  132,   94,    0,    0,
    0,  147,    0,  148,    0,    0,    0,    0,    0,   95,
    0,   96,  133,  562,    0,   97,    0,    0,  134,    0,
  135,    0,  136,   98,   99,  137,    0,  100,    0,    0,
  138,    0,    0,    0,  563,    0,    0,    0,  139,    0,
    0,    0,    0,    0,    0,  149,    0,  150,    0,  151,
    0,  152,    0,  153,    0,  154,  458,  262,  156,    0,
  140,    0,    0,  157,    0,    0,  141,  142,  143,  144,
    0,    0,    0,    0,    0,  145,    0,  146,    0,    0,
    0,    0,    0,    0,  147,    0,  148,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  127,   83,
    0,   84,    0,    0,   85,  128,    0,    0,  149,   86,
  150,  129,  151,   88,  152,    0,  153,    0,  154,  130,
  262,  156,   91,    0,    0,    0,  157,    0,    0,   92,
    0,    0,    0,    0,   93,    0,  131,  132,   94,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   95,    0,   96,  133,    0,    0,   97,    0,    0,  134,
    0,  135,    0,  136,   98,   99,  137,    0,  100,    0,
    0,  138,    0,    0,    0,    0,    0,    0,    0,  139,
    0,    0,   83,    0,   84,    0,    0,   85,    0,    0,
    0,    0,   86,    0,    0,    0,   88,  643,  875, 1132,
    0,  140,    0,    0,    0,   91,    0,  141,  142,  143,
  144,    0,   92,    0,    0,    0,  145,   93,  146,    0,
    0,   94,    0,    0,    0,  147,    0,  148,    0,    0,
    0,    0,    0,   95,    0,   96,    0,    0,    0,   97,
    0,    0,    0,    0,    0,    0,    0,   98,   99,    0,
    0,  100,    0,    0, 1133,    0,    0,    0,    0,  127,
   83,    0,   84,    0,    0,   85,  128,    0,    0,  149,
   86,  150,  129,  151,   88,  152,    0,  153,    0,  154,
  130,  644,  156,   91,    0,    0,    0,  157,    0,    0,
   92,    0,    0,    0,    0,   93,    0,  131,  132,   94,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   95,    0,   96,  133,    0,    0,   97,    0,    0,
  134,    0,  135,    0,  136,   98,   99,  137,    0,  100,
    0,    0,  138,    0,    0,    0,    0,    0,    0,    0,
  139,    0,  127,   83,    0,   84,    0,    0,   85,  128,
    0,    0,    0,   86,    0,  129,    0,   88,    0,    0,
    0,    0,  140,  130,   74,  366,   91,    0,  141,  142,
  143,  144,    0,   92,    0,    0,    0,  145,   93,  146,
  131,  132,   94,    0,    0,    0,  147,    0,  148,    0,
    0,    0,    0,    0,   95,    0,   96,  133,    0,    0,
   97,    0,    0,  134,    0,  135,    0,  136,   98,   99,
  137,    0,  100,    0,    0,  138,    0,    0,    0,    0,
    0,    0,    0,  139,    0,    0,    0,    0,    0,    0,
  149,    0,  150,    0,  151,    0,  152,    0,  153,    0,
  154,    0,  262,  156,    0,  140,  533,    0,  157,    0,
    0,  141,  142,  143,  144,    0,    0,    0,    0,    0,
  145,    0,  146,    0,    0,    0,    0,    0,    0,  147,
    0,  148,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  127,   83,    0,   84,    0,    0,   85,
  128,    0,    0,  149,   86,  150,  129,  151,   88,  152,
    0,  153,    0,  154,  130,  262,  156,   91,    0,    0,
    0,  157,    0,    0,   92,    0,    0,    0,    0,   93,
    0,  131,  132,   94,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   95,    0,   96,  133,    0,
    0,   97,    0,    0,  134,    0,  135,    0,  136,   98,
   99,  137,    0,  100,    0,    0,  138,    0,    0,    0,
    0,    0,    0,    0,  139,    0,  127,   83,    0,   84,
    0,    0,   85,  128,    0,    0,    0,   86,    0,  129,
    0,   88,  643,    0,    0,    0,  140,  130,    0,    0,
   91,    0,  141,  142,  143,  144,    0,   92,    0,    0,
    0,  145,   93,  146,  131,  132,   94,    0,    0,    0,
  147,    0,  148,    0,    0,    0,    0,    0,   95,    0,
   96,  133,    0,    0,   97,    0,    0,  134,    0,  135,
    0,  136,   98,   99,  137,    0,  100,    0,    0,  138,
    0,    0,    0,    0,    0,    0,    0,  139,    0,    0,
    0,    0,    0,    0,  149,    0,  150,    0,  151,    0,
  152,    0,  153,    0,  154,    0,  644,  156,  713,  140,
    0,    0,  157,    0,    0,  141,  142,  143,  144,    0,
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
    0,   83,    0,   84,    0,    0,   85,    0,    0,    0,
    0,   86,    0,    0,    0,   88,    0,    0,    0,    0,
  140,    0,    0,    0,   91,  757,  141,  142,  143,  144,
    0,   92,    0,    0,    0,  145,   93,  146,    0,    0,
   94,    0,    0,    0,  147,    0,  148,    0,    0,    0,
    0,    0,   95,    0,   96,    0,    0,    0,   97,    0,
    0,    0,    0,    0,    0,    0,   98,   99,    0,    0,
  100,    0,    0,  117,    0,    0,    0,    0,  127,   83,
    0,   84,    0,    0,   85,  128,    0,    0,  149,   86,
  150,  129,  151,   88,  152,    0,  153,    0,  154,  130,
  262,  156,   91,    0,    0,    0,  157,    0,    0,   92,
    0,    0,    0,    0,   93,    0,  131,  132,   94,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   95,    0,   96,  133,    0,    0,   97,    0,    0,  134,
    0,  135,    0,  136,   98,   99,  137,    0,  100,    0,
    0,  138,    0,    0,    0,    0,    0,    0,    0,  139,
    0,  127,   83,    0,   84,    0,    0,   85,  128,    0,
    0,    0,   86,    0,  129,    0,   88,    0,  775,    0,
    0,  140,  130,   74,    0,   91,    0,  141,  142,  143,
  144,    0,   92,    0,    0,    0,  145,   93,  146,  131,
  132,   94,    0,    0,    0,  147,    0,  148,    0,    0,
    0,    0,    0,   95,    0,   96,  133,    0,    0,   97,
    0,    0,  134,    0,  135,    0,  136,   98,   99,  137,
    0,  100,    0,    0,  138,    0,    0,    0,    0,    0,
    0,    0,  139,    0,    0,    0,    0,    0,    0,  149,
    0,  150,    0,  151,    0,  152,    0,  153,    0,  154,
  453,  262,  156,    0,  140,    0,    0,  157,    0,    0,
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
    0,    0,    0,  139,    0,  127,   83,    0,   84,    0,
    0,   85,  128,    0,    0,    0,   86,    0,  129,    0,
   88,    0,    0,    0,    0,  140,  130,    0,    0,   91,
    0,  141,  142,  143,  144,    0,   92,    0,    0,    0,
  145,   93,  146,  131,  132,   94,    0,    0,    0,  147,
    0,  148,    0,    0,    0,    0,    0,   95,    0,   96,
  133,    0,    0,   97,    0,    0,  134,    0,  135,    0,
  136,   98,   99,  137,    0,  100,    0,    0,  138,    0,
    0,    0,    0,    0,    0,    0,  139,    0,    0,    0,
    0,    0,    0,  149,    0,  150,    0,  151,    0,  152,
    0,  153,    0,  154,    0,  155,  156,    0,  140,    0,
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
    0,  136,   98,   99,  137,    0,  100,    0,    0,  394,
    0,    0,    0,    0,    0,    0,    0,  139,    0,  596,
  596,    0,  596,    0,    0,  596,  596,    0,    0,    0,
  596,    0,  596,    0,  596,    0,    0,    0,    0,  140,
  596,    0,    0,  596,    0,  141,  142,  143,  144,    0,
  596,    0,    0,    0,  145,  596,  146,  596,  596,  596,
    0,    0,    0,  147,    0,  148,    0,    0,    0,    0,
    0,  596,    0,  596,  596,    0,    0,  596,    0,    0,
  596,    0,  596,    0,  596,  596,  596,  596,    0,  596,
    0,    0,  596,    0,    0,    0,    0,    0,    0,    0,
  596,    0,    0,    0,    0,    0,    0,  149,    0,  150,
    0,  151,    0,  152,    0,  153,    0,  154,    0,  262,
  156,    0,  596,    0,    0,  157,    0,    0,  596,  596,
  596,  596,    0,    0,    0,    0,    0,  596,    0,  596,
    0,    0,    0,    0,    0,    0,  596,    0,  596,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  127,   83,    0,   84,    0,    0,   85,  128,    0,    0,
  596,   86,  596,  129,  596,   88,  596,    0,  596,    0,
  596,  130,  596,  596,   91,    0,    0,    0,  596,    0,
    0,   92,    0,    0,    0,    0,   93,    0,  131,  132,
   94,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   95,    0,   96,  133,    0,    0,   97,    0,
    0,  134,    0,  135,    0,  136,   98,   99,  137,    0,
  100,    0,    0,  138,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  127,   83,    0,   84,    0,    0,   85,
  128,    0,    0,    0,   86,    0,  129,    0,   88,    0,
    0,    0,    0,  140,  130,    0,    0,   91,    0,  141,
  142,  143,  144,    0,   92,    0,    0,    0,  145,   93,
  146,  131,  132,   94,    0,    0,    0,  147,    0,  148,
    0,    0,    0,    0,    0,   95,    0,   96,  133,    0,
    0,   97,    0,    0,  134,    0,  135,    0,  136,   98,
   99,  137,    0,  100,    0,    0,  138,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  149,    0,  150,    0,  151,    0,  152,    0,  153,
    0,  154,    0,  267,    0,    0,  140,    0,    0,  157,
    0,    0,  141,  142,  143,  144,    0,    0,    0,    0,
    0,  145,    0,    0,    0,    0,    0,    0,    0,    0,
  147,    0,  148,  127,   83,    0,   84,    0,    0,   85,
  128,    0,    0,    0,   86,    0,  129,    0,   88,    0,
    0,    0,    0,    0,  130,    0,    0,   91,    0,    0,
    0,    0,    0,    0,   92,    0,    0,    0,    0,   93,
    0,  131,  132,   94,  149,    0,  150,    0,  151,    0,
  152,    0,  153,    0,  154,   95,  267,   96,  133,    0,
    0,   97,  157,    0,  134,    0,  135,    0,  136,   98,
   99,  137,    0,  100,    0,    0,  138,    0,  127,   83,
    0,   84,    0,    0,   85,  128,    0,    0,    0,   86,
    0,  129,    0,   88,    0,    0,    0,    0,    0,  130,
    0,    0,   91,    0,    0,    0,  140,    0,    0,   92,
    0,    0,  141,    0,   93,  144,  131,  132,   94,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   95,    0,   96,  133,    0,    0,   97,    0,    0,  134,
    0,  135,    0,  136,   98,   99,  137,    0,  100,    0,
    0,  138,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  149,    0,  150,    0,  151,    0,
  152,  501,  153,    0,  154,    0,  267,    0,    0,    0,
    0,    0,  157,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  127,   83,
    0,   84,    0,    0,   85,  128,    0,    0,    0,   86,
    0,  129,    0,   88,    0,    0,    0,    0,    0,  130,
    0,    0,   91,    0,    0,    0,    0,    0,    0,   92,
    0,    0,    0,    0,   93,    0,  131,  132,   94,  149,
  523,  150,    0,  151,    0,  152,    0,  153,    0,  154,
   95,  267,   96,  133,    0,    0,   97,  157,    0,  134,
    0,  135,    0,  136,   98,   99,  137,    0,  100,  127,
   83,  138,   84,    0,    0,   85,  128,  523,    0,    0,
   86,    0,  129,    0,   88,    0,    0,    0,    0,    0,
  130,    0,    0,   91,    0,    0,    0,    0,    0,    0,
   92,  445,    0,    0,    0,   93,    0,  131,  132,   94,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   95,    0,   96,  133,    0,    0,   97,    0,    0,
  134,    0,  135,    0,  136,   98,   99,  137,    0,  100,
    0,    0,  138,    0,    0,    0,    0,  523,    0,  523,
    0,  523,    0,  523,  523,    0,  523,  523,    0,  523,
    0,  523,  523,    0,  523,  523,  523,    0,    0,  149,
    0,  150,  773,  151,  523,  152,  523,  153,  523,  154,
  523,  267,  523,    0,  523,    0,  523,  157,  523,    0,
  523,    0,  523,    0,  523,    0,  523,    0,  523,    0,
  523,    0,  523,    0,  523,    0,  523,    0,  523,    0,
    0,    0,  523,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  488,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  149,    0,  150,    0,  151,    0,  152,    0,  153,   54,
  154,   54,  267,    0,   54,    0,   54,    0,  157,   54,
    0,   54,   54,    0,   54,    0,   54,    0,   54,    0,
   54,   54,   54,   54,    0,    0,   54,   54,    0,    0,
    0,    0,   54,    0,   54,   54,   54,    0,    0,   54,
   54,   54,    0,   54,    0,   54,   54,   54,   54,   54,
   54,   54,   54,    0,   54,   54,   54,   54,    0,    0,
   54,   54,   54,    0,   54,    0,    0,    0,    0,   54,
   54,    0,   54,   54,    0,   54,   54,   54,    0,    0,
    0,   54,    0,   53,    0,    0,    0,    0,   53,    0,
   53,    0,    0,   53,    0,   53,   53,   54,   53,   54,
   53,    0,   53,    0,   53,   53,   53,   53,    0,    0,
   53,   53,   54,    0,    0,    0,   53,    0,   53,   53,
   53,    0,    0,   53,    0,   53,    0,   53,    0,    0,
   53,    0,   53,   53,   53,   53,    0,    0,    0,   53,
   53,   53,    0,    0,   53,   53,   53,    0,    0,    0,
    0,    0,    0,   53,   53,    0,   53,   53,    0,   53,
   53,   53,    0,    0,    0,   53,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,   53,    0,
    0,    0,    0,   53,    0,   53,   54,   82,   53,    0,
   53,   53,    0,   53,    0,   53,   53,   53,    0,   53,
   53,   53,   53,    0,    0,   53,   53,    0,    0,    0,
    0,   53,    0,   53,   53,   53,    0,    0,   53,    0,
   53,    0,   53,    0,    0,   53,    0,   53,   53,   53,
   53,    0,    0,    0,   53,   53,   53,    0,    0,   53,
   53,   53,    0,    0,    0,    0,    0,    0,   53,   53,
    0,   53,   53,    0,   53,   53,   53,    0,    0,    0,
   53,    0,   53,    0,    0,    0,    0,   53,    0,   53,
   53,    0,   53,    0,   53,   53,    0,   53,    0,   53,
    0,   53,  103,   53,   53,   53,   53,    0,    0,   53,
   53,   53,    0,    0,    0,   53,    0,   53,   53,   53,
    0,    0,   53,    0,   53,    0,   53,    0,    0,   53,
    0,   53,   53,   53,   53,    0,    0,    0,   53,   53,
   53,    0,    0,   53,   53,   53,    0,    0,    0,    0,
    0,    0,   53,   53,    0,   53,   53,    0,   53,   53,
   53,    0,    0,    0,   53,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,   53,    0,    0,
    0,    0,   53,    0,   53,   53,   83,   53,    0,   53,
   53,    0,   53,    0,   53,   53,   53,    0,   53,   53,
   53,   53,    0,    0,   53,   53,    0,    0,    0,    0,
   53,    0,   53,   53,   53,    0,    0,   53,    0,   53,
    0,   53,    0,    0,   53,    0,   53,   53,   53,   53,
    0,    0,    0,   53,   53,   53,    0,    0,   53,   53,
   53,    0,    0,    0,    0,    0,    0,   53,   53,    0,
   53,   53,    0,   53,   53,   53,    0,    0,    0,   53,
    0,  632,    0,    0,    0,    0,  632,    0,  632,   53,
    0,  632,    0,  632,  632,    0,  632,    0,  632,    0,
  632,  104,  632,  632,  632,  632,    0,    0,  632,  632,
   53,    0,    0,    0,  632,    0,  632,  632,  632,    0,
    0,  632,    0,  632,    0,  632,    0,    0,  632,    0,
  632,  632,  632,  632,    0,    0,    0,  632,  632,  632,
    0,    0,  632,  632,  632,    0,    0,    0,    0,    0,
    0,  632,  632,    0,  632,  632,    0,  632,  632,  632,
    0,    0,    0,  632,  634,    0,    0,    0,    0,  634,
    0,  634,    0,    0,  634,    0,  634,  634,    0,  634,
    0,  634,    0,  634,   53,  634,  634,  634,  634,    0,
    0,  634,  634,    0,  297,    0,    0,  634,    0,  634,
  634,  634,    0,    0,  634,    0,  634,    0,  634,    0,
    0,  634,    0,  634,  634,  634,  634,    0,    0,    0,
  634,  634,  634,    0,    0,  634,  634,  634,    0,    0,
    0,    0,    0,    0,  634,  634,    0,  634,  634,    0,
  634,  634,  634,   53,    0,    0,  634,    0,   53,    0,
   53,    0,    0,   53,    0,   53,   53,    0,   53,    0,
   53,    0,   53,    0,   53,   53,    0,   53,  632,    0,
    0,   53,    0,    0,    0,    0,    0,  296,   53,   53,
   53,    0,    0,   53,    0,   53,    0,   53,    0,    0,
   53,    0,   53,   53,   53,   53,    0,    0,    0,   53,
   53,   53,    0,    0,   53,   53,   53,    0,    0,    0,
    0,    0,    0,   53,   53,    0,   53,   53,    0,   53,
   53,   53,   53,    0,    0,   53,    0,   53,  350,   53,
    0,    0,   53,    0,   53,   53,    0,   53,    0,   53,
    0,   53,    0,   53,   53,    0,   53,  213,    0,    0,
   53,  634,    0,    0,    0,    0,    0,   53,   53,   53,
    0,    0,   53,    0,   53,  350,   53,    0,    0,   53,
   47,   53,   53,   53,   53,    0,    0,    0,   53,   53,
   53,    0,    0,   53,   53,   53,    0,    0,    0,    0,
    0,    0,   53,   53,   48,   53,   53,    0,   53,   53,
   53,    0,    0,    0,   53,    0,    0,   49,    0,    0,
    0,   50,   51,    0,    0,    0,    0,   52,    0,   53,
   54,   55,   56,    0,    0,    0,  214,   57,    0,    0,
   53,   58,  350,    0,  350,    0,  350,    0,    0,  350,
    0,  350,  350,   59,  350,  350,   60,  350,   61,  350,
  350,  350,  350,  350,  350,  350,    0,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,    0,  618,    0,  618,    0,    0,  618,   53,
  618,  618,    0,  618,  350,  618,    0,  618,    0,  618,
  618,  618,    0,    0,    0,  618,  618,    0,    0,    0,
    0,  618,    0,  618,  618,    0,    0,    0,  618,    0,
    0,    0,  618,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  618,    0,  618,    0,    0,    0,
  618,  618,    0,    0,    0,    0,    0,    0,  618,  618,
    0,  617,  618,  617,    0,  618,  617,    0,  617,  617,
  618,  617,    0,  617,    0,  617,    0,  617,  617,  617,
    0,    0,    0,  617,  617,    0,  618,    0,  618,  617,
    0,  617,  617,   83,    0,   84,  617,    0,   85,    0,
  617, 1119,    0,   86,    0,   87,    0,   88,    0,   89,
 1120, 1121,  617,    0,  617,   90,   91,    0,  617,  617,
    0, 1122,    0,   92,    0,    0,  617,  617,   93,    0,
  617,    0,   94,  617,    0,    0,    0,    0,  617,    0,
    0,    0,    0,    0,   95,    0,   96,    0,    0,    0,
   97,    0,    0,    0,    0,    0,    0,    0,   98,   99,
    0,    0,  100,    0,    0,  101,    0,    0,    0,  295,
  102,    0,  617,    0,  617,  618,    0,  617,    0,  617,
  617,    0,  617,    0,  617,    0,  617,    0,  617,  617,
    0,    0,    0,    0,    0,  617,    0,    0,    0,    0,
    0,    0,  617,  617,    0,    0,    0,  617,    0,    0,
    0,  617,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  617,    0,  617,    0,    0,    0,  617,
  617,    0,    0,    0,    0,    0,    0,  617,  617,    0,
    0,  617,    0,  617,  617,    0,  617,    0,  617,  617,
    0,  617,    0,  617,    0,    0,  617,    0,  617,    0,
  617,    0,  617,    0,    0,    0,    0,    0,  617,  617,
    0,    0,    0,    0,    0, 1123,  617,  617,    0,    0,
    0,  617,    0,    0,    0,  617,  160,    0,  160,    0,
    0,  160,    0,    0,    0,    0,  160,  617,    0,  617,
  160,    0,    0,  617,  617,    0,    0,    0,    0,  160,
    0,  617,  617,    0,    0,  617,  160,    0,  617,    0,
    0,  160,    0,  617,    0,  160,    0,  160,    0,  160,
    0,    0,    0,    0,  160,    0,    0,  160,    0,  160,
    0,    0,    0,  160,    0,    0,  160,    0,    0,    0,
    0,  160,  160,    0,  617,  160,    0,    0,  160,    0,
    0,  160,  160,  160,    0,    0,  160,    0,    0,    0,
    0,  160,    0,    0,    0,  160,    0,    0,    0,    0,
    0,    0,    0,    0,  160,    0,  160,  159,    0,    0,
    0,  160,    0,    0,    0,    0,  160,    0,    0,    0,
  160,    0,  160,    0,  160,    0,   53,    0,   53,  160,
    0,   53,  160,    0,  160,    0,   53,    0,  160,    0,
   53,  160,    0,    0,    0,    0,  160,  160,  617,   53,
  160,    0,    0,  160,    0,    0,   53,  160,    0,    0,
    0,   53,    0,    0,    0,   53,    0,   53,    0,   53,
    0,    0,    0,    0,   53,    0,    0,   53,    0,   53,
    0,  160,    0,   53,  159,    0,   53,    0,  160,    0,
    0,   53,   53,    0,    0,   53,    0,    0,   53,    0,
   83,    0,   84,    0,    0,   85,    0,    0,    0,    0,
   86,    0,   87,    0,   88,    0,   89,    0,    0,    0,
    0,    0,   90,   91,    0,    0,    0,    0,    0,  157,
   92,    0,   83,    0,   84,   93,    0,   85,    0,   94,
    0,    0,   86,    0,    0,    0,   88,    0,    0,    0,
    0,   95,    0,   96,    0,   91,    0,   97,    0,    0,
    0,    0,   92,  160,    0,   98,   99,   93,    0,  100,
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
    0,    0,  117,    0,   92,    0,   83,    0,   84,   93,
    0,   85,   74,   94,    0,    0,   86,    0,    0,    0,
   88,    0,    0,    0,    0,   95,    0,   96,    0,   91,
    0,   97,    0,    0,    0,    0,   92,    0,    0,   98,
   99,   93,    0,  100,   74,   94,  117,    0,    0,    0,
  178,    0,  178,    0,    0,  178,    0,   95,    0,   96,
  178,    0,    0,   97,  178,    0,    0,    0,    0,    0,
    0,   98,   99,  178,    0,  100,    0,    0,  117,    0,
  178,    0,    0,    0,    0,  178,    0,    0,  260,  178,
    0,    0,    0,    0,  187,    0,  187,    0,    0,  187,
    0,  178,    0,  178,  187,    0,    0,  178,  187,    0,
    0,    0,  656,    0,    0,  178,  178,  187,  179,  178,
  179,    0,  178,  179,  187,  523,    0,    0,  179,  187,
    0,  523,  179,  187,    0,    0,    0,    0,    0,    0,
    0,  179,    0,    0,    0,  187,    0,  187,  179,    0,
    0,  187,    0,  179,    0,    0,  693,  179,    0,  187,
  187,    0,    0,  187,    0,    0,  187,    0,  523,  179,
    0,  179,    0,    0,    0,  179,    0,    0,    0,    0,
    0,    0,    0,  179,  179,    0,    0,  179,  695,    0,
  179,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  523,
    0,    0,    0,    0,    0,  523,  523,  523,  523,  523,
  523,  523,  523,  523,  523,  523,  523,  539,    0,    0,
    0,    0,  178,  539,  523,    0,  523,    0,  523,    0,
  523,  523,  523,    0,  523,  523,    0,  523,  523,    0,
  523,    0,  523,  523,  523,  523,  523,  523,  523,    0,
    0,    0,    0,    0,    0,  523,    0,  523,    0,  523,
  539,  523,    0,  523,    0,  523,  187,  523,    0,  523,
    0,  523,    0,  523,    0,  523,    0,  523,    0,  523,
    0,  523,    0,  523,    0,  523,    0,  523,  543,  523,
  179,    0,    0,  523,  543,    0,    0,    0,    0,    0,
    0,  539,    0,    0,    0,    0,    0,  539,  539,  539,
  539,  539,  539,  539,  539,  539,  539,  539,  539,    0,
    0,    0,    0,    0,    0,    0,  539,    0,  539,    0,
  539,  543,  539,  539,  539,    0,  539,  539,    0,    0,
  539,    0,  539,    0,  539,  539,  539,  539,  539,  539,
  539,    0,    0,    0,    0,    0,    0,  539,    0,  539,
    0,  539,    0,  539,    0,  539,    0,  539,    0,  539,
    0,  539,  543,    0,    0,    0,    0,    0,  543,  543,
  543,  543,  543,  543,  543,  543,  543,  543,  543,  543,
  544,    0,    0,    0,    0,  539,  544,  543,    0,  543,
    0,  543,    0,  543,  543,  543,    0,  543,  543,    0,
    0,  543,    0,  543,    0,  543,  543,    0,    0,    0,
  543,  543,    0,    0,    0,    0,    0,    0,  543,    0,
  543,    0,  543,  544,  543,    0,  543,    0,  543,    0,
  543,    0,  543,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  545,    0,    0,    0,    0,  543,  545,    0,    0,
    0,    0,    0,    0,  544,    0,    0,    0,    0,    0,
  544,  544,  544,  544,  544,  544,  544,  544,  544,  544,
  544,  544,    0,    0,    0,    0,    0,    0,    0,  544,
    0,  544,    0,  544,  545,  544,  544,  544,    0,  544,
  544,    0,    0,  544,    0,  544,    0,  544,  544,    0,
    0,    0,  544,  544,    0,    0,    0,    0,    0,    0,
  544,    0,  544,    0,  544,    0,  544,    0,  544,    0,
  544,    0,  544,    0,  544,  545,    0,    0,    0,    0,
    0,  545,  545,  545,  545,  545,  545,  545,  545,  545,
  545,  545,  545,  546,    0,    0,    0,    0,  544,  546,
  545,    0,  545,    0,  545,    0,  545,  545,  545,    0,
  545,  545,    0,    0,  545,    0,  545,    0,  545,  545,
    0,    0,    0,  545,  545,    0,    0,    0,    0,    0,
    0,  545,    0,  545,    0,  545,  546,  545,    0,  545,
    0,  545,    0,  545,    0,  545,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  547,    0,    0,    0,    0,  545,
  547,    0,    0,    0,    0,    0,    0,  546,    0,    0,
    0,    0,    0,  546,  546,  546,  546,  546,  546,  546,
  546,  546,  546,  546,  546,    0,    0,    0,    0,    0,
    0,    0,  546,    0,  546,    0,  546,  547,  546,  546,
  546,    0,    0,    0,    0,    0,  546,    0,  546,    0,
  546,  546,    0,    0,    0,  546,  546,    0,    0,    0,
    0,    0,    0,  546,    0,  546,    0,  546,    0,  546,
  548,  546,    0,  546,    0,  546,  548,  546,  547,    0,
    0,    0,    0,    0,  547,  547,  547,  547,  547,  547,
  547,  547,  547,  547,  547,  547,    0,    0,    0,    0,
    0,  546,    0,  547,    0,  547,    0,  547,    0,  547,
  547,  547,    0,  548,    0,    0,    0,  547,    0,  547,
    0,  547,  547,    0,    0,    0,  547,  547,    0,    0,
    0,    0,    0,    0,  547,    0,  547,    0,  547,    0,
  547,  552,  547,    0,  547,    0,  547,  552,  547,    0,
    0,    0,    0,    0,  548,    0,    0,    0,    0,    0,
  548,  548,  548,  548,  548,  548,  548,  548,  548,  548,
  548,  548,  547,    0,    0,    0,    0,    0,    0,  548,
    0,  548,    0,  548,  552,  548,  548,  548,    0,    0,
    0,    0,    0,  548,    0,  548,    0,  548,  548,    0,
    0,    0,  548,  548,    0,    0,    0,    0,    0,    0,
  548,    0,  548,    0,  548,    0,  548,  553,  548,    0,
  548,    0,  548,  553,  548,  552,    0,    0,    0,    0,
    0,  552,  552,  552,  552,  552,  552,  552,  552,  552,
  552,  552,  552,    0,    0,    0,    0,    0,  548,    0,
  552,    0,  552,    0,  552,    0,  552,  552,  552,    0,
  553,    0,    0,    0,  552,    0,  552,    0,  552,  552,
    0,    0,    0,  552,  552,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  552,    0,  552,  554,  552,
    0,  552,    0,  552,  554,  552,    0,    0,    0,    0,
    0,  553,    0,    0,    0,    0,    0,  553,  553,  553,
  553,  553,  553,  553,  553,  553,  553,  553,  553,  552,
    0,    0,    0,    0,    0,    0,  553,    0,  553,    0,
  553,  554,  553,  553,  553,    0,    0,    0,    0,    0,
  553,    0,  553,    0,  553,  553,    0,    0,    0,  553,
  553,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  553,    0,  553,  555,  553,    0,  553,    0,  553,
  555,  553,  554,    0,    0,    0,    0,    0,  554,  554,
  554,  554,  554,  554,  554,  554,  554,  554,  554,  554,
    0,    0,    0,    0,    0,  553,    0,  554,    0,  554,
    0,  554,    0,  554,  554,  554,    0,  555,    0,    0,
    0,  554,    0,  554,    0,  554,  554,    0,    0,    0,
  554,  554,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  554,    0,  554,  556,  554,    0,  554,    0,
  554,  556,  554,    0,    0,    0,    0,    0,  555,    0,
    0,    0,    0,    0,  555,  555,  555,  555,  555,  555,
  555,  555,  555,  555,  555,  555,  554,    0,    0,    0,
    0,    0,    0,  555,    0,  555,    0,  555,  556,  555,
  555,  555,    0,    0,    0,    0,    0,  555,    0,  555,
    0,  555,  555,    0,    0,    0,  555,  555,    0,    0,
    0,    0,    0,    0,    0,    0,  561,    0,  555,    0,
  555,    0,  555,    0,  555,    0,  555,    0,  555,  556,
    0,    0,    0,    0,    0,  556,  556,  556,  556,  556,
  556,  556,  556,  556,  556,  556,  556,    0,    0,    0,
    0,    0,  555,    0,  556,    0,  556,    0,  556,    0,
  556,  556,  556,    0,    0,    0,    0,    0,  556,    0,
  556,    0,  556,  556,    0,    0,    0,  556,  556,    0,
    0,    0,    0,    0,    0,    0,    0,  562,    0,  556,
    0,  556,    0,  556,    0,  556,    0,  556,    0,  556,
  561,    0,    0,    0,    0,    0,  561,  561,  561,  561,
  561,  561,  561,  561,  561,  561,  561,  561,    0,    0,
    0,    0,    0,  556,    0,  561,    0,  561,    0,  561,
    0,  561,  561,  561,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  561,  561,    0,    0,    0,  561,  561,
    0,    0,    0,    0,    0,    0,    0,    0,  563,    0,
    0,    0,    0,    0,  561,    0,  561,    0,  561,    0,
  561,  562,    0,    0,    0,    0,    0,  562,  562,  562,
  562,  562,  562,  562,  562,  562,  562,  562,  562,    0,
    0,    0,    0,    0,  561,    0,  562,    0,  562,    0,
  562,    0,  562,  562,  562,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  562,  562,    0,    0,    0,  562,
  562,    0,    0,    0,    0,    0,    0,    0,    0,  564,
    0,    0,    0,    0,    0,  562,    0,  562,    0,  562,
    0,  562,  563,    0,    0,    0,    0,    0,  563,  563,
  563,  563,  563,  563,  563,  563,  563,  563,  563,  563,
    0,    0,    0,    0,    0,  562,    0,  563,    0,  563,
    0,  563,    0,  563,  563,  563,  565,    0,    0,    0,
    0,    0,    0,    0,    0,  563,  563,    0,    0,    0,
  563,  563,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  563,    0,  563,    0,
  563,    0,  563,  564,    0,    0,    0,    0,    0,  564,
  564,  564,  564,  564,  564,  564,  564,  564,  564,  564,
  564,    0,    0,    0,    0,    0,  563,    0,  564,    0,
  564,    0,  564,    0,  564,  564,  564,  566,    0,    0,
    0,    0,    0,    0,    0,    0,  564,  564,    0,    0,
  565,  564,  564,    0,    0,    0,  565,  565,  565,  565,
  565,  565,  565,  565,  565,  565,  565,  565,    0,    0,
    0,  564,    0,  564,    0,  565,    0,  565,    0,  565,
    0,  565,  565,  565,  567,    0,    0,    0,    0,    0,
    0,    0,    0,  565,  565,    0,    0,  564,  565,  565,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  565,    0,
  565,  566,    0,    0,    0,    0,    0,  566,  566,  566,
  566,  566,  566,  566,  566,  566,  566,  566,  566,    0,
    0,    0,    0,    0,  565,    0,  566,    0,  566,    0,
  566,    0,  566,  566,  566,  568,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  566,    0,    0,  567,  566,
  566,    0,    0,    0,  567,  567,  567,  567,  567,  567,
  567,  567,  567,  567,  567,  567,    0,    0,    0,  566,
    0,  566,    0,  567,    0,  567,    0,  567,    0,  567,
  567,  567,  569,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  567,    0,    0,  566,  567,  567,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  567,    0,  567,  568,
    0,    0,    0,    0,    0,  568,  568,  568,  568,  568,
  568,  568,  568,  568,  568,  568,  568,    0,    0,    0,
    0,    0,  567,    0,  568,    0,  568,    0,  568,    0,
  568,  568,  568,  570,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  568,    0,    0,  569,    0,  568,    0,
    0,    0,  569,  569,  569,  569,  569,  569,  569,  569,
  569,  569,  569,  569,    0,    0,    0,  568,    0,  568,
    0,  569,    0,  569,    0,  569,    0,  569,  569,  569,
  571,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  569,    0,    0,  568,    0,  569,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  572,    0,
    0,    0,    0,    0,  569,    0,  569,  570,    0,    0,
    0,    0,    0,  570,  570,  570,  570,  570,  570,  570,
  570,  570,  570,  570,  570,    0,    0,    0,    0,    0,
  569,    0,  570,    0,  570,    0,  570,    0,  570,  570,
  570,  543,  573,    0,    0,    0,    0,  543,    0,    0,
    0,    0,    0,    0,  571,    0,  570,    0,    0,    0,
  571,  571,  571,  571,  571,  571,  571,  571,  571,  571,
  571,  571,    0,    0,    0,  570,    0,  570,    0,  571,
    0,  571,  572,  571,  543,  571,  571,  571,  572,  572,
  572,  572,  572,  572,  572,  572,  572,  572,  572,  572,
    0,  570,    0,  571,    0,    0,    0,  572,  350,  572,
    0,  572,    0,  572,  572,  572,    0,    0,    0,    0,
    0,    0,  571,    0,  571,    0,  573,    0,    0,    0,
    0,  572,  573,  573,  573,  573,  573,  573,  573,  573,
  573,  573,  573,  573,    0,  350,    0,    0,  571,    0,
    0,  573,  572,  573,  543,  573,  543,  573,  573,  573,
  543,  543,    0,    0,  543,    0,  543,    0,  543,  543,
    0,    0,    0,  543,  543,  573,  572,    0,    0,    0,
    0,  543,    0,  543,    0,  543,    0,  543,    0,  543,
    0,  543,    0,  543,    0,  543,  573,    0,    0,  549,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  350,    0,  350,  350,  350,  350,    0,  543,
  573,  350,  350,    0,    0,  350,    0,  350,    0,  350,
  350,  350,  350,  350,  350,  350,  549,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,  350,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,  549,  350,    0,
  350,    0,    0,  549,  549,  549,  549,  549,  549,  549,
  549,  549,  549,  549,  549,    0,  350,    0,    0,    0,
    0,    0,  549,    0,  549,    0,  549,    0,  549,  549,
  549,    0,    0,    0,    0,    0,  549,    0,  549,    0,
  549,  549,    0,    0,    0,  549,  549,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  549,    0,  549,
    0,  549,    0,  549,    0,  549,    0,  549,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  350,  350,    0,  350,    0,  350,  350,    0,
    0,  549,  350,  350,    0,    0,  350,    0,  350,    0,
  350,  350,  350,  350,  350,  350,  350,    0,  385,  350,
    0,  350,    0,  350,    0,  350,    0,  350,    0,  350,
    0,  350,    0,  350,    0,  350,    0,  350,    0,    0,
    0,    0,  385,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,   53,    0,  385,    0,    0,    0,  350,
  385,  350,    0,  385,    0,  385,    0,  385,  385,  385,
  385,    0,    0,    0,    0,  385,    0,   53,    0,  385,
    0,    0,    0,  385,    0,    0,    0,    0,    0,    0,
   53,  385,    0,    0,  385,   53,  385,    0,    0,    0,
   53,    0,   53,   53,   53,   53,    0,    0,    0,    0,
   53,    0,    0,   53,   53,   53,    0,    0,    0,    0,
    0,    0,    0,  385,    0,    0,   53,    0,    0,   53,
    0,   53,    0,    0,    0,    0,    0,   53,    0,    0,
    0,    0,    0,   53,    0,    0,  385,    0,    0,    0,
   53,   53,    0,   53,    0,   53,    0,  197,    0,    0,
   53,    0,   53,   53,   53,   53,    0,   53,   53,    0,
   53,    0,    0,   53,   53,   53,    0,    0,    0,    0,
   53,    0,    0,    0,    0,   53,   53,    0,    0,   53,
   53,   53,   53,   53,   53,   53,    0,   53,   53,    0,
   53,    0,    0,   47,   53, 1282,    0,    0,    0,    0,
   53,    0,    0,    0,    0,   53,   53,  310,    0,   53,
   53,   53,   53,   53,   53,   53,    0,   48,    0,    0,
   53,    0,    0,   47,   53, 1306,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   53,  307,    0,   53,
   52,   53,   53,   54,   55,   56,    0,   48, 1283,    0,
   57,    0,    0,   47,   58,    0,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   59,  308,    0,   60,
   52,   61,   53,   54,   55,   56,    0,   48, 1307,    0,
   57,    0,    0,   47,   58,    0,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   59,    0,    0,   60,
   52,   61,   53,   54,   55,   56,    0,   48, 1283,    0,
   57,    0,    0,   47,   58, 1306,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   59,    0,    0,   60,
   52,   61,   53,   54,   55,   56,    0,   48, 1307,    0,
   57,    0,    0,   47,   58,    0,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   59,    0,    0,   60,
   52,   61,   53,   54,   55,   56,    0,   48,    0,    0,
   57,    0,    0,   47,   58,    0,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   59,    0,    0,   60,
   52,   61,   53,   54,   55,   56,    0, 1095,    0,    0,
   57,    0,    0,    0,   58,    0,    0,    0,    0,    0,
   49,    0,    0,    0,    0,   51,   59,    0,    0,   60,
   52,   61,   53,   54,   55,   56,    0,    0,    0,    0,
   57,    0,    0,    0,   58,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1096,    0,    0,   60,
    0,   61,
  };
  protected static  short [] yyCheck = {            63,
    0,   62,  105,   66,    3,    4,    6,  455,   69,  591,
   75,  403,  256,  236,  131,  236,  236,  554,  236,  254,
   25,  140,  254,  217,  101,  453,   87,  501,  596,   90,
  297,  501,  822,  254,  571,  572,  122,  123,  606,   38,
  458,  578,  579,  103,  830,  831,  106,  256,  256,  221,
  167,   50,  834,  256,  326,  629, 1199,  118,  256,  269,
  232,  141,  142,  143,  144,  145,  146,  147,  148,   68,
  131,  256,  551,  295, 1038,  256,  101,   76,  139,  140,
  283,    0,  256,  402,  269,  680,  307,    6,  366, 1053,
  256,  269,  370,  816,  380,  156,  157,  257,  256,  277,
  368,  389,  101,  385,  423,  256,  105,  385,  256,  256,
  366,  389,  315,  118,  370,  340,  233,  903,  326,  270,
  256,  256,  410,  366,  910,  911,  369,  370,  378,  340,
  380,  365,  396,  389,  398,  921,  287,  371,  255,  364,
  390,  845,  385,  378,  366,  213,  434,  256,  373,  285,
   69,  256,  856,  364,  256,  365,  256,  378,  366,  380,
  371,  709,  373,  385,  256,  359,  448,  231,  256,  455,
  448,  369,  344,  371,  234,  236,  448,  385,  239,  902,
  403,  449,  403,  403, 1327,  403,  256,  454,  360,  763,
  371,  765,  448,  262,  256,  369,  257,  222,  370,  256,
  366,  796,  321,  322,  264,  365,  231,  677,  366,  381,
  382,  383,  456,  995,  294,  295,  296,  278,  366,  385,
  638,  393,  369,  222,  371,  286,  448,  385,  707,  933,
  299,  366,  231,  256,  938,  939, 1018,  385,  326,  448,
  448,  321,  322,  323,  324,  448,  456,  327,  328,  329,
  330,  331,  332,  333,  334,  335,  336,  366,  338,  841,
  256,  366, 1052, 1227,  583,  448,  366,  815,  836,  371,
  448,  456,  365,  295,  380,  979,  385, 1067,  366,  371,
  385,  985,  448,  256,  345,  402,  256,  273,  270,  365,
  448, 1081,  353,  361,  364,  363,  366,  365,  368,  773,
  448,  389,  448,  773,  366,  287,  368,  364,  342,   62,
  379,  297,  381,  370,  377,  385,  365,  378,  267,  380,
  239,  257,  371,  385,  278, 1010,  366,  390,  391,  340,
  447,  400,  366,  402,   87,  257,  306,   90,  324,  448,
  295,  364,  403, 1010,  295,  422, 1010,  370,  371,  455,
  373,  779,  380,  364,  440,  448,  366,  368,  380,  278,
  928,  372,  390,  285,  782,  118,  315,  759,  390,   79,
  591,  366,  448,  453,  568,  371,  643,  368,  131,  370,
  443, 1085,  501,  604,  373,  502,  139,  370,  468,  475,
  385,  471,  385,  366, 1079,  344,  456,  370,  366,  353,
  461,  366,  370,  156,  157,  640, 1110,  642,  448,  472,
  642,  497, 1079,  370,  336, 1079,  373,  478,  504,  640,
  364,  642,  448,  372,  368,  380,  370,  455,  369,  380,
  140,  380, 1014, 1008,  353,  390,  366,  366,  449,  390,
  501,  390,  369,  364,  366, 1008,  366, 1010,  378,  266,
  366,  268,  367,  448,  271,  448,  385,  256,  449,  276,
  521,  522,  461,  280,  256,  385,  583,  364,  389,  385,
  342,  368,  289,  370,  371,  340,  373,  538,  539,  296,
  448,  364,  568,  448,  301,  368, 1061,  370,  305,  366,
  307,  368, 1278, 1008,  366, 1281,  613,  314, 1061,  364,
  317,  562,  319,  366,  257,  449,  323,  306,  385,  326,
  451,  452,  453, 1006,  331,  332, 1079, 1010,  335,  448,
  371,  338,  385,  450,  451,  452,  453, 1010,  448,  590,
  591,  592,  448,  286,  366,  245,  759,  247,  759,  759,
  603,  759,  368,  604,  370,  622, 1061, 1006,  340,  259,
 1043, 1010,  449,  385,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,  449,  277,  368, 1010,
  370,  366,  449,  365,  340,  367,  748,  369,  591,  371,
  372,  373,  292,  293, 1043,  448, 1079,  297,  298,  654,
  385,  604,  345,  366,  370, 1008, 1079,  657,  364, 1010,
  310,  311,  312,  313,  314,  315,  316,  317,  318,  319,
  320,  371,  385,  389,  372,  261,  448, 1006,  366,  368,
 1079, 1010,  382,  449,  845,  630,  274,  337,  366,  339,
  278,  448, 1169,  877,  282,  856, 1008,  385, 1079,  285,
  366,  351,  352, 1229, 1230,  273,  256,  385, 1061,  449,
  365, 1237,  298,  448, 1043,  265,  371,  303,  344,  385,
  306, 1247,  308,  780,  310,  311,  312,  313, 1079,  297,
 1207,  373,  318, 1210, 1008,  448,  322,  387,  366,  389,
  326,  369,  370,  371,  375,  376,  749,  344,  334, 1061,
 1079,  337,  378,  339,  380,  343,  324,  385,  759,  779,
  448,  389,  366,  764,  390,  273,  767,  340,  461, 1246,
  448,  711,  933, 1299,  378,  370,  326,  938,  939,  448,
  366, 1113,  368,  380,  370,  478,  371, 1061,  791,  297,
 1208,  364,  448,  390,  389,  445,  344,  382,  371,  385,
    6,  364,    8,  389,  454,  455,  365,  366,  458,  368,
  371,  370,  371,  463,  448,  820,  324,  380,  979,  338,
  448,  382, 1240, 1241,  985, 1243,  827,  390,  521,  522,
  378,  366,  380,  392,  369,  394, 1224, 1225,   44,  489,
  490,  378,  390,  380,  845,  538,  539,  848,  380,  852,
  385,  501,  307,  390,  340,  856,  882,  883,  390,  314,
  346,  347,  448,  449,  350,  351,  261,  353,  354,  562,
  366,  326,  368,  432,  370,  307,  816,  309,  370,  367,
  368,  373,  314,  371,  389,  371,  366,  537,  366,  385,
  285,  541,  366,  389,  326,  369,  392,  590,  394,  592,
  378,  366,  380,  298,  369,  385,  366,  557,  303,  369,
  364,  385,  390,  308,  385,  310,  311,  312,  313,  373,
  385,  364,  380,  318, 1085,  385, 1089,  322, 1089, 1089,
  373, 1089,  933,  936,  367,  378,  432,  938,  939,  334,
  371,  368,  337,  593,  339,  366,  596,  597,  369, 1110,
 1113,  382, 1113, 1113,  604, 1113,  606,  366,  608,  368,
  368,  370,  902,  370,  385,  371,  906,  365,  366,  970,
  368,  366,  370,  371,  378,  625,  382,  366,  979,  368,
  389,  370,  364,  392,  985,  394,  371,  366,  638,  365,
  385,  373,  369,  643,  392,  371,  394,  380,  371,  378,
  373,  380,  378,  392, 1005,  394,  364,  390,  412,  380,
  414,  390,  416,  663,  418,  373,  420,  667,  422,  390,
  424,  368,  426,  432,  428,  366,  430,  677,  369,  370,
  680,  364, 1144,  364,  432,  368, 1199,  371, 1199, 1199,
  690, 1199,  373,  432,  385,  364, 1158,  383,  389,  364,
 1162,  701,  702,  448,  373, 1167, 1168,  365,  373,  388,
  364,  369,  384,  371,  364,  373, 1006,  364, 1008,  373,
 1010,  764,  340,  373,  767,  385,  386,  387,  346,  347,
 1192, 1124,  350,  351, 1085,  353,  354,  364, 1089, 1201,
 1202,  368,  408, 1094,  340,  745, 1097,  385,  386,  387,
  346,  347, 1103, 1043,  350,  351,  372,  353,  354, 1110,
  367,  368, 1113,  370,  371,  372,  385,  370, 1119, 1120,
  389, 1061, 1139,  773,  369,  371,  371,  777,  373,  385,
  340, 1132,  782,  389,  827,  448,  346,  347,  371, 1079,
 1141,  256, 1143,  353,  354, 1148,  796,  367,  256,  369,
  800,  371,  802,  256,  804,  848,  448,  807,  266,  366,
  268,  369,  364,  271, 1327,  373, 1327, 1327,  276, 1327,
  368,  367,  280,  369,  824,  371,  367,  404,  367,  406,
  371,  289,  371,    0,  834, 1124,  836,  367,  296,  350,
  351,  371,  842,  301, 1133, 1134,  448,  305, 1199,  367,
 1139,  777,  369,  371,  369,  781,  373,  340,  371,  317,
  373,  319,  371,  346,  347,  323,  448,  350,  351, 1262,
  353,  354,  340,  331,  332,  355,  356,  335,  346,  347,
  338,  448,  350,  351,  448,  353,  354,  350,  351,  889,
  371,  891,  373, 1286,  373, 1288,  896,  327,  328,  329,
  330,  454, 1255,  369,  380,  371,  364,  371,  366,  373,
  371,  366,  373, 1203,  359,  367,  361,  369,  367,  367,
  369,  369,  355,  356,  371,  925,  373,  970,  928,  448,
 1284,  931,  369,  367,  371,  261,  367,  263,  369, 1229,
 1230,  448, 1295, 1296,  367,  448,  369, 1237,  373, 1239,
 1303,  359, 1305,  361,  954, 1309, 1310, 1247,  958,  285,
    0,  387, 1005,  389,  350,  351, 1256,  371,  723,  724,
  323,  324,  298,  373, 1264, 1265, 1327,  303,  368,  331,
  332,  368,  308,  368,  310,  311,  312,  313,  261,  364,
  448,  385,  318,  365,  368,  995,  322,  373,  295,  999,
  373,  369,  373, 1003,  369,  344,  368,  448,  334, 1299,
  369,  337,  285,  339,  372,  364,  448,  367, 1018,  380,
  389,  448,  341,  371,  369,  298,  373,  373, 1028,  373,
  303,  373,  373,  306,  371,  308,  371,  310,  311,  312,
  313,  371,  365,  371,  369,  318,  378,  371,  369,  322,
  370, 1094,  369,  326, 1097,  371,    0,  357,  285,  336,
 1103,  334,  369,  367,  337,  378,  339,  369,  369,  372,
  369,  369,  256,  368,  389,  371, 1119, 1120,  371,  373,
  369,  373,  369,  369,  369,  369,  256,  365,  364, 1132,
  257,  373, 1092,  366,  261,  295,  295,  448, 1141,  266,
 1143,  268,  352,  365,  271,  448,  273,  274,  448,  276,
  448,  278,  385,  280,  448,  282,  283,  284,  285,  365,
  371,  288,  289,  340,  378,  369,  295,  294,  371,  296,
  297,  298,  371,  455,  301,  302,  303,  369,  305,    0,
  348,  308,  371,  310,  311,  312,  313,  367,  371,  281,
  317,  318,  319,  369,  369,  322,  323,  324,  448,  261,
  448,  364,  256,  256,  331,  332,  348,  334,  335,  336,
  337,  338,  339,  378,  372,  448,  343,  364,  364,  364,
  373,  373,  373,  285,  349,  349,  369,  367,  369,  365,
  368,  365,  371,  365,  365,  372,  298,  357,  365,  366,
  448,  303,  364,  369,  372,  364,  308,  374,  310,  311,
  312,  313,  369,  374,  366,  370,  318,  257,  448,  366,
  322,  261,  368,  367, 1224, 1225,  266,  369,  268,  366,
  365,  271,  334,  273,  367,  337,  276,  339,  278,  369,
  280,  369,  282,  369,  369,  285,  369,  365,  288,  289,
  364,  256,  369,  369,  369,  367,  296,  297,  298,  365,
  256,  301,  302,  303,  365,  305,  365,  365,  308,  365,
  310,  311,  312,  313,  365,    0,  373,  317,  318,  319,
  367,  448,  322,  323,  324,  367,  369,  364,  364,  369,
  369,  331,  332,  364,  334,  335,  336,  337,  338,  339,
  369,  367,  365,  343,  369,  448,  373,  448,  369,  367,
  367,  365,  365,  257,  369,  364,  364,  261,  369,  448,
  365,  373,  266,  365,  268,  365,  366,  271,  369,  273,
  369,  364,  276,  371,  278,  448,  280,  373,  282,  373,
  365,  285,  373,  373,  288,  289,  448,  369,  365,  369,
  365,  369,  296,  297,  298,  364,  364,  301,  302,  303,
  364,  305,    6,    6,  308,   38,  310,  311,  312,  313,
   76,  292, 1043,  317,  318,  319,  293, 1079,  322,  323,
  324,  835,  356,  558,  763, 1138, 1220,  331,  332,  769,
  334,  335,  769,  337,  338,  339, 1256, 1296, 1319,  343,
  261, 1265,  769, 1264, 1061,  266,  906,  268,  448,  239,
  271,  265,  273,  306,  780,  276,  970,  278,  490,  280,
  642,  282,  779,  305,  307,  333,  335,  288,  289,  334,
  522,  338,  336,  917,  478,  296,  297,  298, 1094,  378,
  301,  302,  303, 1089,  305, 1021, 1014,  308, 1023,  310,
  311,  312,  313,  927,  960,  791,  317,  318,  319,  750,
  545,  322,  323,  324, 1116,  958,   -1,   -1,   -1,   -1,
  331,  332,   -1,  334,  335,   -1,  337,  338,  339,   -1,
   -1,   -1,  343,  256,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  265,  266,  267,  268,  269,   -1,  271,  272,
   -1,  274,  275,  276,  448,  278,  279,  280,  281,   -1,
   -1,   -1,   -1,  286,   -1,  288,  289,  290,  291,  292,
  293,   -1,   -1,  296,   -1,   -1,   -1,  300,  301,   -1,
  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  315,   -1,  317,   -1,  319,  320,   -1,   -1,
  323,   -1,  325,  326,  327,  328,  329,  330,  331,  332,
  333,  334,  335,  336,   -1,  338,   -1,   -1,  341,   -1,
   -1,   -1,   -1,  346,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  266,   -1,  268,   -1,  448,  271,   -1,
   -1,  364,  365,  276,   -1,  368,   -1,  280,   -1,   -1,
  373,  374,  375,  376,  377,   -1,  289,   -1,   -1,   -1,
  383,   -1,  385,  296,   -1,   -1,   -1,   -1,  301,  392,
   -1,  394,  305,   -1,  307,   -1,  309,   -1,   -1,   -1,
   -1,  314,   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,
  323,   -1,   -1,  326,   -1,   -1,   -1,   -1,  331,  332,
   -1,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,  442,
   -1,  444,   -1,  446,   -1,  448,  449,  256,   -1,   -1,
   -1,  454,   -1,  456,  367,   -1,  265,  266,  267,  268,
  269,   -1,  271,  272,   -1,  274,  275,  276,   -1,  278,
  279,  280,   -1,   -1,   -1,   -1,   -1,  286,   -1,  288,
  289,  290,  291,  292,  293,   -1,   -1,  296,   -1,   -1,
   -1,  300,  301,   -1,  303,  304,  305,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  315,   -1,  317,   -1,
  319,  320,   -1,   -1,  323,   -1,  325,  326,  327,  328,
  329,  330,  331,  332,  333,  334,  335,  336,   -1,  338,
   -1,   -1,  341,   -1,   -1,  448,   -1,  346,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  266,   -1,  268,
   -1,   -1,  271,   -1,   -1,  364,  365,  276,   -1,  368,
   -1,  280,   -1,   -1,  373,  374,  375,  376,  377,   -1,
  289,   -1,   -1,   -1,  383,   -1,  385,  296,   -1,   -1,
   -1,   -1,  301,  392,   -1,  394,  305,   -1,  307,   -1,
  309,   -1,   -1,   -1,   -1,  314,   -1,   -1,  317,   -1,
  319,   -1,   -1,   -1,  323,   -1,   -1,  326,   -1,   -1,
   -1,   -1,  331,  332,   -1,   -1,  335,   -1,   -1,  338,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,
   -1,  440,   -1,  442,   -1,  444,   -1,  446,   -1,  448,
  449,  256,   -1,   -1,   -1,  454,   -1,  456,   -1,   -1,
  265,  266,  267,  268,   -1,   -1,  271,  272,   -1,  274,
  275,  276,   -1,  278,  279,  280,   -1,   -1,   -1,   -1,
   -1,  286,   -1,  288,  289,  290,  291,  292,  293,   -1,
   -1,  296,   -1,   -1,   -1,  300,  301,   -1,  303,  304,
  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  315,   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,
  325,  326,  327,  328,  329,  330,  331,  332,  333,  334,
  335,  336,   -1,  338,  256,   -1,  341,   -1,   -1,  448,
   -1,  346,   -1,  265,  266,   -1,  268,   -1,   -1,  271,
  272,   -1,   -1,   -1,  276,   -1,  278,   -1,  280,  364,
   -1,   -1,   -1,  368,  286,   -1,   -1,  289,  373,  374,
  375,  376,  377,   -1,  296,   -1,  286,   -1,  383,  301,
  385,  303,  304,  305,   -1,   -1,   -1,  392,   -1,  394,
   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,
   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,
  332,  333,   -1,  335,   -1,   -1,  338,   -1,  328,   -1,
   -1,   -1,   -1,   -1,  346,   -1,   -1,   -1,   -1,   -1,
   -1,  436,   -1,  438,   -1,  440,   -1,  442,   -1,  444,
   -1,  446,   -1,  448,  449,   -1,  368,   -1,   -1,  454,
   -1,   -1,  374,  375,  376,  377,   -1,   -1,   -1,   -1,
   -1,  383,   -1,  385,  374,  375,  376,  377,   -1,  379,
  392,  381,  394,  383,  384,  385,  386,  387,  388,   -1,
   -1,   -1,  392,   -1,  394,   -1,  396,   -1,  398,   -1,
  400,   -1,  402,   -1,  404,  256,  406,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  265,  266,   -1,  268,   -1,   -1,
  271,  272,   -1,   -1,  436,  276,  438,  278,  440,  280,
  442,   -1,  444,   -1,  446,  286,  448,  449,  289,   -1,
   -1,   -1,  454,   -1,   -1,  296,   -1,   -1,   -1,   -1,
  301,   -1,  303,  304,  305,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,  320,
   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,
  331,  332,  333,   -1,  335,   -1,   -1,  338,  256,   -1,
   -1,   -1,   -1,   -1,   -1,  346,   -1,  265,  266,   -1,
  268,   -1,   -1,  271,  272,   -1,   -1,   -1,  276,   -1,
  278,   -1,  280,   -1,   -1,   -1,   -1,  368,  286,   -1,
  256,  289,   -1,  374,  375,  376,  377,   -1,  296,   -1,
   -1,   -1,  383,  301,  385,  303,  304,  305,   -1,   -1,
   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,
   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,
  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,
  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,  440,
   -1,  442,   -1,  444,   -1,  446,   -1,  448,  449,   -1,
  368,  256,   -1,  454,  340,   -1,  261,  262,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  365,
  285,  367,   -1,  369,   -1,  371,  372,  373,   -1,   -1,
  295,   -1,   -1,  298,  299,   -1,   -1,   -1,  303,   -1,
   -1,  306,   -1,  308,   -1,  310,  311,  312,  313,   -1,
   -1,   -1,   -1,  318,   -1,   -1,   -1,  322,  436,   -1,
  438,  326,  440,   -1,  442,   -1,  444,   -1,  446,  334,
  448,   -1,  337,   -1,  339,  340,  454,   -1,   -1,   -1,
   -1,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,  364,
  365,   -1,  367,  368,  369,  370,  371,  372,  373,   -1,
  375,  376,   -1,  378,  379,   -1,  381,  382,  383,  384,
  385,  386,  387,  388,  389,   -1,   -1,  392,   -1,  394,
   -1,  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,
   -1,  406,   -1,  408,   -1,  410,   -1,  412,   -1,  414,
   -1,  416,   -1,  418,   -1,  420,   -1,  422,   -1,  424,
   -1,  426,   -1,  428,   -1,  430,  256,  432,   -1,  434,
   -1,  261,  262,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  448,  449,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  285,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  295,   -1,   -1,  298,  299,
   -1,   -1,   -1,  303,   -1,   -1,  306,   -1,  308,   -1,
  310,  311,  312,  313,   -1,   -1,   -1,   -1,  318,   -1,
   -1,   -1,  322,   -1,   -1,   -1,  326,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  334,   -1,   -1,  337,   -1,  339,
  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,   -1,   -1,
   -1,   -1,   -1,   -1,  364,  365,  366,  367,  368,  369,
  370,  371,  372,  373,   -1,   -1,   -1,   -1,   -1,  379,
   -1,  381,  382,  383,  384,  385,   -1,   -1,  388,  389,
  256,   -1,   -1,   -1,   -1,  261,  262,   -1,   -1,   -1,
  400,   -1,  402,   -1,  404,   -1,  406,   -1,  408,   -1,
  410,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  285,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  295,
   -1,   -1,  298,  299,  434,   -1,   -1,  303,   -1,   -1,
   -1,   -1,  308,   -1,  310,  311,  312,  313,  448,  449,
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
   -1,  303,   -1,   -1,  306,   -1,  308,   -1,  310,  311,
  312,  313,  448,  449,   -1,   -1,  318,   -1,   -1,   -1,
  322,   -1,   -1,   -1,  326,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  334,   -1,   -1,  337,   -1,  339,  340,   -1,
   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,
   -1,   -1,  364,  365,  366,  367,  368,  369,  256,  371,
  372,  373,  261,   -1,  262,   -1,   -1,  379,   -1,  381,
  382,  383,  384,  385,   -1,   -1,  388,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  285,   -1,  400,   -1,
  402,   -1,  404,   -1,  406,   -1,  408,  295,  410,  298,
   -1,  299,   -1,   -1,  303,   -1,   -1,   -1,   -1,  308,
   -1,  310,  311,  312,  313,   -1,   -1,   -1,   -1,  318,
   -1,   -1,  434,  322,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  334,  448,  449,  337,   -1,
  339,   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
  359,   -1,  361,   -1,   -1,   -1,  364,  365,  366,  367,
  368,  369,  256,  371,  372,  373,  261,   -1,  262,   -1,
   -1,  379,   -1,  381,  382,  383,  384,   -1,   -1,   -1,
  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  285,   -1,  400,   -1,  402,   -1,  404,   -1,  406,   -1,
  408,   -1,  410,  298,   -1,  299,   -1,   -1,  303,   -1,
   -1,   -1,   -1,  308,   -1,  310,  311,  312,  313,   -1,
   -1,   -1,   -1,  318,   -1,   -1,  434,  322,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  334,
  448,  449,  337,   -1,  339,   -1,  340,   -1,   -1,   -1,
   -1,   -1,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,  359,   -1,  361,   -1,   -1,   -1,
  365,  365,  366,  367,  368,  369,  370,  371,  372,  373,
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
  366,  367,  368,  369,  370,  371,  372,  373,  256,  375,
  376,   -1,  378,  379,  262,  381,   -1,  383,  384,  385,
  386,  387,  388,  389,   -1,   -1,  392,   -1,  394,   -1,
  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,   -1,
  406,   -1,  408,   -1,  410,   -1,  412,   -1,  414,   -1,
  416,  299,  418,   -1,  420,   -1,  422,   -1,  424,   -1,
  426,   -1,  428,   -1,  430,   -1,  432,   -1,  434,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  448,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  365,  366,  367,
  368,  369,  370,  371,  372,  373,  256,  375,  376,   -1,
  378,  379,  262,  381,   -1,  383,  384,  385,  386,  387,
  388,  389,   -1,   -1,  392,   -1,  394,   -1,  396,   -1,
  398,   -1,  400,   -1,  402,   -1,  404,   -1,  406,   -1,
  408,   -1,  410,   -1,  412,   -1,  414,   -1,  416,  299,
  418,   -1,  420,   -1,  422,   -1,  424,   -1,  426,   -1,
  428,   -1,  430,   -1,  432,   -1,  434,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  448,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,   -1,   -1,
  256,   -1,   -1,   -1,  364,  365,  262,  367,  368,  369,
  370,  371,  372,  373,   -1,  375,  376,   -1,  378,  379,
   -1,  381,   -1,  383,  384,  385,  386,  387,  388,  389,
   -1,   -1,  392,   -1,  394,   -1,  396,   -1,  398,   -1,
  400,   -1,  402,  299,  404,   -1,  406,   -1,  408,   -1,
  410,   -1,  412,   -1,  414,   -1,  416,   -1,  418,   -1,
  420,   -1,  422,   -1,  424,   -1,  426,   -1,  428,   -1,
  430,   -1,  432,   -1,  434,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,  448,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  365,
  366,  367,  368,  369,  370,  371,  372,  373,  256,  375,
  376,   -1,  378,  379,  262,  381,   -1,  383,  384,  385,
  386,  387,  388,  389,   -1,   -1,  392,   -1,  394,   -1,
  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,   -1,
  406,   -1,  408,   -1,  410,   -1,  412,   -1,  414,   -1,
  416,  299,  418,   -1,  420,   -1,  422,   -1,  424,   -1,
  426,   -1,  428,   -1,  430,   -1,  432,   -1,  434,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  448,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  365,  366,  367,
  368,  369,  370,  371,  372,  373,  256,  375,  376,   -1,
  378,  379,  262,  381,   -1,  383,  384,  385,  386,  387,
  388,  389,   -1,   -1,  392,   -1,  394,   -1,  396,   -1,
  398,   -1,  400,   -1,  402,   -1,  404,   -1,  406,   -1,
  408,   -1,  410,   -1,  412,   -1,  414,   -1,  416,  299,
  418,   -1,  420,   -1,  422,   -1,  424,   -1,  426,   -1,
  428,   -1,  430,   -1,  432,   -1,  434,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  448,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  365,   -1,  367,   -1,  369,
   -1,  371,  372,  373,   -1,  375,  376,   -1,  378,  379,
   -1,  381,   -1,  383,  384,  385,  386,  387,  388,  389,
   -1,   -1,   -1,   -1,   -1,   -1,  396,   -1,  398,   -1,
  400,   -1,  402,   -1,  404,   -1,  406,   -1,  408,   -1,
  410,   -1,  412,   -1,  414,   -1,  416,   -1,  418,   -1,
  420,   -1,  422,   -1,  424,   -1,  426,   -1,  428,   -1,
  430,  257,   -1,   -1,  434,  261,   -1,   -1,   -1,   -1,
  266,   -1,  268,   -1,   -1,  271,   -1,  273,  448,   -1,
  276,   -1,  278,   -1,  280,   -1,  282,   -1,   -1,   -1,
   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,   -1,   -1,
  296,  297,  298,   -1,   -1,  301,  302,  303,   -1,  305,
   -1,   -1,  308,   -1,  310,  311,  312,  313,   -1,   -1,
   -1,  317,  318,  319,   -1,   -1,  322,  323,  324,   -1,
   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,  334,  335,
  336,  337,  338,  339,   -1,   -1,   -1,  343,   -1,   -1,
   -1,   -1,  257,   -1,   -1,   -1,  261,   -1,   -1,   -1,
   -1,  266,   -1,  268,   -1,   -1,  271,   -1,  273,  365,
  366,  276,   -1,  278,   -1,  280,   -1,  282,   -1,   -1,
   -1,   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,   -1,
   -1,  296,  297,  298,   -1,   -1,  301,  302,  303,   -1,
  305,   -1,   -1,  308,   -1,  310,  311,  312,  313,   -1,
   -1,   -1,  317,  318,  319,   -1,   -1,  322,  323,  324,
   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,  334,
  335,  336,  337,  338,  339,   -1,   -1,   -1,  343,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  448,  257,   -1,   -1,   -1,  261,   -1,   -1,
  365,  366,  266,   -1,  268,   -1,   -1,  271,   -1,  273,
   -1,   -1,  276,   -1,  278,   -1,  280,   -1,  282,   -1,
   -1,  285,   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,
   -1,   -1,  296,  297,  298,   -1,   -1,  301,  302,  303,
   -1,  305,   -1,   -1,  308,   -1,  310,  311,  312,  313,
   -1,   -1,   -1,  317,  318,  319,   -1,   -1,  322,  323,
  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,
  334,  335,   -1,  337,  338,  339,   -1,   -1,   -1,  343,
   -1,   -1,   -1,  448,  257,   -1,   -1,   -1,  261,   -1,
   -1,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,   -1,
  273,  365,  366,  276,   -1,  278,   -1,  280,   -1,  282,
   -1,   -1,  285,   -1,   -1,  288,  289,   -1,   -1,   -1,
   -1,   -1,   -1,  296,  297,  298,   -1,   -1,  301,  302,
  303,   -1,  305,   -1,   -1,  308,   -1,  310,  311,  312,
  313,   -1,   -1,   -1,  317,  318,  319,   -1,   -1,  322,
  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,
   -1,  334,  335,   -1,  337,  338,  339,   -1,   -1,   -1,
  343,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  448,  257,   -1,   -1,   -1,  261,
   -1,   -1,  365,  366,  266,   -1,  268,   -1,   -1,  271,
   -1,  273,   -1,   -1,  276,   -1,  278,   -1,  280,   -1,
  282,   -1,   -1,  285,   -1,   -1,  288,  289,   -1,   -1,
   -1,   -1,   -1,   -1,  296,  297,  298,   -1,   -1,  301,
  302,  303,   -1,  305,   -1,   -1,  308,   -1,  310,  311,
  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,   -1,
  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,
  332,   -1,  334,  335,   -1,  337,  338,  339,   -1,  257,
   -1,  343,   -1,  261,   -1,  448,   -1,   -1,  266,   -1,
  268,   -1,   -1,  271,   -1,  273,   -1,   -1,  276,   -1,
  278,   -1,  280,  365,  282,   -1,   -1,  285,   -1,   -1,
  288,  289,   -1,   -1,   -1,   -1,   -1,   -1,  296,  297,
  298,   -1,   -1,  301,  302,  303,   -1,  305,   -1,   -1,
  308,   -1,  310,  311,  312,  313,   -1,   -1,   -1,  317,
  318,  319,   -1,   -1,  322,  323,  324,   -1,   -1,   -1,
   -1,   -1,   -1,  331,  332,   -1,  334,  335,   -1,  337,
  338,  339,   -1,   -1,   -1,  343,   -1,   -1,  265,  266,
  267,  268,   -1,   -1,  271,  272,   -1,  274,  275,  276,
   -1,  278,  279,  280,   -1,   -1,  448,  365,   -1,  286,
   -1,  288,  289,  290,  291,  292,  293,   -1,   -1,  296,
   -1,   -1,   -1,  300,  301,   -1,  303,  304,  305,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  315,   -1,
  317,   -1,  319,  320,   -1,   -1,  323,   -1,  325,  326,
  327,  328,  329,  330,  331,  332,  333,  334,  335,  336,
   -1,  338,   -1,   -1,  341,   -1,   -1,   -1,   -1,  346,
   -1,   -1,  266,   -1,  268,   -1,   -1,  271,   -1,  273,
   -1,   -1,  276,   -1,   -1,   -1,  280,  364,   -1,  283,
  448,  368,   -1,   -1,   -1,  289,  373,  374,  375,  376,
  377,   -1,  296,  297,   -1,   -1,  383,  301,  385,   -1,
   -1,  305,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,  323,
  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,
   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,  265,
  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,  436,
  276,  438,  278,  440,  280,  442,   -1,  444,   -1,  446,
  286,  448,  449,  289,   -1,   -1,   -1,  454,   -1,   -1,
  296,   -1,   -1,   -1,   -1,  301,   -1,  303,  304,  305,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  317,   -1,  319,  320,  321,   -1,  323,   -1,   -1,
  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,
   -1,   -1,  338,   -1,   -1,   -1,  342,   -1,   -1,   -1,
  346,   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,
   -1,   -1,   -1,  276,   -1,  278,   -1,  280,  364,  365,
   -1,   -1,  368,  286,  448,   -1,  289,   -1,  374,  375,
  376,  377,   -1,  296,   -1,   -1,   -1,  383,  301,  385,
  303,  304,  305,   -1,  307,   -1,  392,   -1,  394,   -1,
   -1,  314,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,
  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,
  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,  342,
   -1,   -1,   -1,  346,   -1,   -1,   -1,   -1,   -1,   -1,
  436,   -1,  438,   -1,  440,   -1,  442,   -1,  444,   -1,
  446,   -1,  448,  449,   -1,  368,  369,   -1,  454,   -1,
   -1,  374,  375,  376,  377,   -1,   -1,   -1,   -1,   -1,
  383,   -1,  385,   -1,   -1,   -1,   -1,   -1,   -1,  392,
   -1,  394,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  265,  266,   -1,  268,   -1,   -1,  271,
  272,   -1,   -1,  436,  276,  438,  278,  440,  280,  442,
   -1,  444,   -1,  446,  286,  448,  449,  289,   -1,   -1,
   -1,  454,   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,
   -1,  303,  304,  305,   -1,  307,   -1,   -1,   -1,   -1,
   -1,   -1,  314,   -1,   -1,  317,   -1,  319,  320,   -1,
   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,
  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,
  342,   -1,   -1,   -1,  346,   -1,   -1,  266,   -1,  268,
   -1,   -1,  271,   -1,  273,   -1,   -1,  276,   -1,   -1,
   -1,  280,   -1,   -1,   -1,   -1,  368,  369,   -1,   -1,
  289,   -1,  374,  375,  376,  377,   -1,  296,   -1,   -1,
   -1,  383,  301,  385,  303,   -1,  305,   -1,   -1,   -1,
  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,
  319,   -1,   -1,   -1,  323,  324,   -1,   -1,   -1,   -1,
   -1,   -1,  331,  332,   -1,   -1,  335,   -1,   -1,  338,
   -1,   -1,   -1,   -1,  265,  266,   -1,  268,   -1,   -1,
  271,  272,   -1,   -1,  436,  276,  438,  278,  440,  280,
  442,   -1,  444,   -1,  446,  286,  448,  449,  289,   -1,
   -1,   -1,  454,   -1,   -1,  296,   -1,   -1,   -1,   -1,
  301,   -1,  303,  304,  305,   -1,  307,   -1,   -1,   -1,
   -1,   -1,   -1,  314,   -1,   -1,  317,   -1,  319,  320,
   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,
  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,
   -1,  342,   -1,   -1,   -1,  346,   -1,  265,  266,   -1,
  268,   -1,   -1,  271,  272,   -1,   -1,   -1,  276,   -1,
  278,   -1,  280,   -1,   -1,   -1,   -1,  368,  286,  448,
   -1,  289,   -1,  374,  375,  376,  377,   -1,  296,   -1,
   -1,   -1,  383,  301,  385,  303,  304,  305,   -1,   -1,
   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,
   -1,  319,  320,  321,   -1,  323,   -1,   -1,  326,   -1,
  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,
  338,   -1,   -1,   -1,  342,   -1,   -1,   -1,  346,   -1,
   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,  440,
   -1,  442,   -1,  444,   -1,  446,  364,  448,  449,   -1,
  368,   -1,   -1,  454,   -1,   -1,  374,  375,  376,  377,
   -1,   -1,   -1,   -1,   -1,  383,   -1,  385,   -1,   -1,
   -1,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  265,  266,
   -1,  268,   -1,   -1,  271,  272,   -1,   -1,  436,  276,
  438,  278,  440,  280,  442,   -1,  444,   -1,  446,  286,
  448,  449,  289,   -1,   -1,   -1,  454,   -1,   -1,  296,
   -1,   -1,   -1,   -1,  301,   -1,  303,  304,  305,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,
   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,
   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  346,
   -1,   -1,  266,   -1,  268,   -1,   -1,  271,   -1,   -1,
   -1,   -1,  276,   -1,   -1,   -1,  280,  364,  365,  283,
   -1,  368,   -1,   -1,   -1,  289,   -1,  374,  375,  376,
  377,   -1,  296,   -1,   -1,   -1,  383,  301,  385,   -1,
   -1,  305,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,  323,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,
   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,  265,
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
   -1,   -1,  368,  286,  448,  371,  289,   -1,  374,  375,
  376,  377,   -1,  296,   -1,   -1,   -1,  383,  301,  385,
  303,  304,  305,   -1,   -1,   -1,  392,   -1,  394,   -1,
   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,
  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,
  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  346,   -1,   -1,   -1,   -1,   -1,   -1,
  436,   -1,  438,   -1,  440,   -1,  442,   -1,  444,   -1,
  446,   -1,  448,  449,   -1,  368,  369,   -1,  454,   -1,
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
   -1,   -1,   -1,   -1,  346,   -1,  265,  266,   -1,  268,
   -1,   -1,  271,  272,   -1,   -1,   -1,  276,   -1,  278,
   -1,  280,  364,   -1,   -1,   -1,  368,  286,   -1,   -1,
  289,   -1,  374,  375,  376,  377,   -1,  296,   -1,   -1,
   -1,  383,  301,  385,  303,  304,  305,   -1,   -1,   -1,
  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,
  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,
   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  346,   -1,   -1,
   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,
  442,   -1,  444,   -1,  446,   -1,  448,  449,  367,  368,
   -1,   -1,  454,   -1,   -1,  374,  375,  376,  377,   -1,
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
   -1,  266,   -1,  268,   -1,   -1,  271,   -1,   -1,   -1,
   -1,  276,   -1,   -1,   -1,  280,   -1,   -1,   -1,   -1,
  368,   -1,   -1,   -1,  289,  373,  374,  375,  376,  377,
   -1,  296,   -1,   -1,   -1,  383,  301,  385,   -1,   -1,
  305,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,
   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,  323,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,   -1,
  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,  265,  266,
   -1,  268,   -1,   -1,  271,  272,   -1,   -1,  436,  276,
  438,  278,  440,  280,  442,   -1,  444,   -1,  446,  286,
  448,  449,  289,   -1,   -1,   -1,  454,   -1,   -1,  296,
   -1,   -1,   -1,   -1,  301,   -1,  303,  304,  305,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,
   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,
   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  346,
   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,
   -1,   -1,  276,   -1,  278,   -1,  280,   -1,  365,   -1,
   -1,  368,  286,  448,   -1,  289,   -1,  374,  375,  376,
  377,   -1,  296,   -1,   -1,   -1,  383,  301,  385,  303,
  304,  305,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,
   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,
   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,
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
   -1,   -1,   -1,  346,   -1,  265,  266,   -1,  268,   -1,
   -1,  271,  272,   -1,   -1,   -1,  276,   -1,  278,   -1,
  280,   -1,   -1,   -1,   -1,  368,  286,   -1,   -1,  289,
   -1,  374,  375,  376,  377,   -1,  296,   -1,   -1,   -1,
  383,  301,  385,  303,  304,  305,   -1,   -1,   -1,  392,
   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,
  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,
  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  346,   -1,   -1,   -1,
   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,  442,
   -1,  444,   -1,  446,   -1,  448,  449,   -1,  368,   -1,
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
   -1,   -1,   -1,  265,  266,   -1,  268,   -1,   -1,  271,
  272,   -1,   -1,   -1,  276,   -1,  278,   -1,  280,   -1,
   -1,   -1,   -1,  368,  286,   -1,   -1,  289,   -1,  374,
  375,  376,  377,   -1,  296,   -1,   -1,   -1,  383,  301,
  385,  303,  304,  305,   -1,   -1,   -1,  392,   -1,  394,
   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,
   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,
  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  436,   -1,  438,   -1,  440,   -1,  442,   -1,  444,
   -1,  446,   -1,  448,   -1,   -1,  368,   -1,   -1,  454,
   -1,   -1,  374,  375,  376,  377,   -1,   -1,   -1,   -1,
   -1,  383,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  392,   -1,  394,  265,  266,   -1,  268,   -1,   -1,  271,
  272,   -1,   -1,   -1,  276,   -1,  278,   -1,  280,   -1,
   -1,   -1,   -1,   -1,  286,   -1,   -1,  289,   -1,   -1,
   -1,   -1,   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,
   -1,  303,  304,  305,  436,   -1,  438,   -1,  440,   -1,
  442,   -1,  444,   -1,  446,  317,  448,  319,  320,   -1,
   -1,  323,  454,   -1,  326,   -1,  328,   -1,  330,  331,
  332,  333,   -1,  335,   -1,   -1,  338,   -1,  265,  266,
   -1,  268,   -1,   -1,  271,  272,   -1,   -1,   -1,  276,
   -1,  278,   -1,  280,   -1,   -1,   -1,   -1,   -1,  286,
   -1,   -1,  289,   -1,   -1,   -1,  368,   -1,   -1,  296,
   -1,   -1,  374,   -1,  301,  377,  303,  304,  305,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,
   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,
   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,
  442,  368,  444,   -1,  446,   -1,  448,   -1,   -1,   -1,
   -1,   -1,  454,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  265,  266,
   -1,  268,   -1,   -1,  271,  272,   -1,   -1,   -1,  276,
   -1,  278,   -1,  280,   -1,   -1,   -1,   -1,   -1,  286,
   -1,   -1,  289,   -1,   -1,   -1,   -1,   -1,   -1,  296,
   -1,   -1,   -1,   -1,  301,   -1,  303,  304,  305,  436,
  262,  438,   -1,  440,   -1,  442,   -1,  444,   -1,  446,
  317,  448,  319,  320,   -1,   -1,  323,  454,   -1,  326,
   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,  265,
  266,  338,  268,   -1,   -1,  271,  272,  299,   -1,   -1,
  276,   -1,  278,   -1,  280,   -1,   -1,   -1,   -1,   -1,
  286,   -1,   -1,  289,   -1,   -1,   -1,   -1,   -1,   -1,
  296,  368,   -1,   -1,   -1,  301,   -1,  303,  304,  305,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,
  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,
   -1,   -1,  338,   -1,   -1,   -1,   -1,  369,   -1,  371,
   -1,  373,   -1,  375,  376,   -1,  378,  379,   -1,  381,
   -1,  383,  384,   -1,  386,  387,  388,   -1,   -1,  436,
   -1,  438,  368,  440,  396,  442,  398,  444,  400,  446,
  402,  448,  404,   -1,  406,   -1,  408,  454,  410,   -1,
  412,   -1,  414,   -1,  416,   -1,  418,   -1,  420,   -1,
  422,   -1,  424,   -1,  426,   -1,  428,   -1,  430,   -1,
   -1,   -1,  434,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  448,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  436,   -1,  438,   -1,  440,   -1,  442,   -1,  444,  261,
  446,  263,  448,   -1,  266,   -1,  268,   -1,  454,  271,
   -1,  273,  274,   -1,  276,   -1,  278,   -1,  280,   -1,
  282,  283,  284,  285,   -1,   -1,  288,  289,   -1,   -1,
   -1,   -1,  294,   -1,  296,  297,  298,   -1,   -1,  301,
  302,  303,   -1,  305,   -1,  307,  308,  309,  310,  311,
  312,  313,  314,   -1,  316,  317,  318,  319,   -1,   -1,
  322,  323,  324,   -1,  326,   -1,   -1,   -1,   -1,  331,
  332,   -1,  334,  335,   -1,  337,  338,  339,   -1,   -1,
   -1,  343,   -1,  261,   -1,   -1,   -1,   -1,  266,   -1,
  268,   -1,   -1,  271,   -1,  273,  274,  359,  276,  361,
  278,   -1,  280,   -1,  282,  283,  284,  285,   -1,   -1,
  288,  289,  374,   -1,   -1,   -1,  294,   -1,  296,  297,
  298,   -1,   -1,  301,   -1,  303,   -1,  305,   -1,   -1,
  308,   -1,  310,  311,  312,  313,   -1,   -1,   -1,  317,
  318,  319,   -1,   -1,  322,  323,  324,   -1,   -1,   -1,
   -1,   -1,   -1,  331,  332,   -1,  334,  335,   -1,  337,
  338,  339,   -1,   -1,   -1,  343,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  261,   -1,
   -1,   -1,   -1,  266,   -1,  268,  448,  365,  271,   -1,
  273,  274,   -1,  276,   -1,  278,  374,  280,   -1,  282,
  283,  284,  285,   -1,   -1,  288,  289,   -1,   -1,   -1,
   -1,  294,   -1,  296,  297,  298,   -1,   -1,  301,   -1,
  303,   -1,  305,   -1,   -1,  308,   -1,  310,  311,  312,
  313,   -1,   -1,   -1,  317,  318,  319,   -1,   -1,  322,
  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,
   -1,  334,  335,   -1,  337,  338,  339,   -1,   -1,   -1,
  343,   -1,  261,   -1,   -1,   -1,   -1,  266,   -1,  268,
  448,   -1,  271,   -1,  273,  274,   -1,  276,   -1,  278,
   -1,  280,  365,  282,  283,  284,  285,   -1,   -1,  288,
  289,  374,   -1,   -1,   -1,  294,   -1,  296,  297,  298,
   -1,   -1,  301,   -1,  303,   -1,  305,   -1,   -1,  308,
   -1,  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,
  319,   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,
   -1,   -1,  331,  332,   -1,  334,  335,   -1,  337,  338,
  339,   -1,   -1,   -1,  343,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  261,   -1,   -1,
   -1,   -1,  266,   -1,  268,  448,  365,  271,   -1,  273,
  274,   -1,  276,   -1,  278,  374,  280,   -1,  282,  283,
  284,  285,   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,
  294,   -1,  296,  297,  298,   -1,   -1,  301,   -1,  303,
   -1,  305,   -1,   -1,  308,   -1,  310,  311,  312,  313,
   -1,   -1,   -1,  317,  318,  319,   -1,   -1,  322,  323,
  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,
  334,  335,   -1,  337,  338,  339,   -1,   -1,   -1,  343,
   -1,  261,   -1,   -1,   -1,   -1,  266,   -1,  268,  448,
   -1,  271,   -1,  273,  274,   -1,  276,   -1,  278,   -1,
  280,  365,  282,  283,  284,  285,   -1,   -1,  288,  289,
  374,   -1,   -1,   -1,  294,   -1,  296,  297,  298,   -1,
   -1,  301,   -1,  303,   -1,  305,   -1,   -1,  308,   -1,
  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,
   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,
   -1,  331,  332,   -1,  334,  335,   -1,  337,  338,  339,
   -1,   -1,   -1,  343,  261,   -1,   -1,   -1,   -1,  266,
   -1,  268,   -1,   -1,  271,   -1,  273,  274,   -1,  276,
   -1,  278,   -1,  280,  448,  282,  283,  284,  285,   -1,
   -1,  288,  289,   -1,  374,   -1,   -1,  294,   -1,  296,
  297,  298,   -1,   -1,  301,   -1,  303,   -1,  305,   -1,
   -1,  308,   -1,  310,  311,  312,  313,   -1,   -1,   -1,
  317,  318,  319,   -1,   -1,  322,  323,  324,   -1,   -1,
   -1,   -1,   -1,   -1,  331,  332,   -1,  334,  335,   -1,
  337,  338,  339,  261,   -1,   -1,  343,   -1,  266,   -1,
  268,   -1,   -1,  271,   -1,  273,  274,   -1,  276,   -1,
  278,   -1,  280,   -1,  282,  283,   -1,  285,  448,   -1,
   -1,  289,   -1,   -1,   -1,   -1,   -1,  374,  296,  297,
  298,   -1,   -1,  301,   -1,  303,   -1,  305,   -1,   -1,
  308,   -1,  310,  311,  312,  313,   -1,   -1,   -1,  317,
  318,  319,   -1,   -1,  322,  323,  324,   -1,   -1,   -1,
   -1,   -1,   -1,  331,  332,   -1,  334,  335,   -1,  337,
  338,  339,  261,   -1,   -1,  343,   -1,  266,  262,  268,
   -1,   -1,  271,   -1,  273,  274,   -1,  276,   -1,  278,
   -1,  280,   -1,  282,  283,   -1,  285,  365,   -1,   -1,
  289,  448,   -1,   -1,   -1,   -1,   -1,  296,  297,  298,
   -1,   -1,  301,   -1,  303,  299,  305,   -1,   -1,  308,
  261,  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,
  319,   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,
   -1,   -1,  331,  332,  285,  334,  335,   -1,  337,  338,
  339,   -1,   -1,   -1,  343,   -1,   -1,  298,   -1,   -1,
   -1,  302,  303,   -1,   -1,   -1,   -1,  308,   -1,  310,
  311,  312,  313,   -1,   -1,   -1,  365,  318,   -1,   -1,
  448,  322,  366,   -1,  368,   -1,  370,   -1,   -1,  373,
   -1,  375,  376,  334,  378,  379,  337,  381,  339,  383,
  384,  385,  386,  387,  388,  389,   -1,   -1,  392,   -1,
  394,   -1,  396,   -1,  398,   -1,  400,   -1,  402,   -1,
  404,   -1,  406,   -1,  408,   -1,  410,   -1,  412,   -1,
  414,   -1,  416,   -1,  418,   -1,  420,   -1,  422,   -1,
  424,   -1,  426,   -1,  428,   -1,  430,   -1,  432,   -1,
  434,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,  448,
  273,  274,   -1,  276,  448,  278,   -1,  280,   -1,  282,
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
  296,   -1,   -1,   -1,   -1,  301,   -1,   -1,  448,  305,
   -1,   -1,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,
   -1,  317,   -1,  319,  276,   -1,   -1,  323,  280,   -1,
   -1,   -1,  448,   -1,   -1,  331,  332,  289,  266,  335,
  268,   -1,  338,  271,  296,  256,   -1,   -1,  276,  301,
   -1,  262,  280,  305,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  289,   -1,   -1,   -1,  317,   -1,  319,  296,   -1,
   -1,  323,   -1,  301,   -1,   -1,  448,  305,   -1,  331,
  332,   -1,   -1,  335,   -1,   -1,  338,   -1,  299,  317,
   -1,  319,   -1,   -1,   -1,  323,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  331,  332,   -1,   -1,  335,  448,   -1,
  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  340,
   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,
  351,  352,  353,  354,  355,  356,  357,  256,   -1,   -1,
   -1,   -1,  448,  262,  365,   -1,  367,   -1,  369,   -1,
  371,  372,  373,   -1,  375,  376,   -1,  378,  379,   -1,
  381,   -1,  383,  384,  385,  386,  387,  388,  389,   -1,
   -1,   -1,   -1,   -1,   -1,  396,   -1,  398,   -1,  400,
  299,  402,   -1,  404,   -1,  406,  448,  408,   -1,  410,
   -1,  412,   -1,  414,   -1,  416,   -1,  418,   -1,  420,
   -1,  422,   -1,  424,   -1,  426,   -1,  428,  256,  430,
  448,   -1,   -1,  434,  262,   -1,   -1,   -1,   -1,   -1,
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
  369,   -1,  371,  372,  373,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  383,  384,   -1,   -1,   -1,  388,
  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  256,
   -1,   -1,   -1,   -1,   -1,  404,   -1,  406,   -1,  408,
   -1,  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
   -1,   -1,   -1,   -1,   -1,  434,   -1,  365,   -1,  367,
   -1,  369,   -1,  371,  372,  373,  256,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  383,  384,   -1,   -1,   -1,
  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  404,   -1,  406,   -1,
  408,   -1,  410,  340,   -1,   -1,   -1,   -1,   -1,  346,
  347,  348,  349,  350,  351,  352,  353,  354,  355,  356,
  357,   -1,   -1,   -1,   -1,   -1,  434,   -1,  365,   -1,
  367,   -1,  369,   -1,  371,  372,  373,  256,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  383,  384,   -1,   -1,
  340,  388,  389,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,   -1,   -1,
   -1,  408,   -1,  410,   -1,  365,   -1,  367,   -1,  369,
   -1,  371,  372,  373,  256,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  383,  384,   -1,   -1,  434,  388,  389,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  408,   -1,
  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,   -1,
   -1,   -1,   -1,   -1,  434,   -1,  365,   -1,  367,   -1,
  369,   -1,  371,  372,  373,  256,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  384,   -1,   -1,  340,  388,
  389,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,  408,
   -1,  410,   -1,  365,   -1,  367,   -1,  369,   -1,  371,
  372,  373,  256,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  384,   -1,   -1,  434,  388,  389,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  408,   -1,  410,  340,
   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,
  351,  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,
   -1,   -1,  434,   -1,  365,   -1,  367,   -1,  369,   -1,
  371,  372,  373,  256,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  384,   -1,   -1,  340,   -1,  389,   -1,
   -1,   -1,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,   -1,   -1,   -1,  408,   -1,  410,
   -1,  365,   -1,  367,   -1,  369,   -1,  371,  372,  373,
  256,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  384,   -1,   -1,  434,   -1,  389,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  256,   -1,
   -1,   -1,   -1,   -1,  408,   -1,  410,  340,   -1,   -1,
   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,
  434,   -1,  365,   -1,  367,   -1,  369,   -1,  371,  372,
  373,  256,  256,   -1,   -1,   -1,   -1,  262,   -1,   -1,
   -1,   -1,   -1,   -1,  340,   -1,  389,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,   -1,   -1,   -1,  408,   -1,  410,   -1,  365,
   -1,  367,  340,  369,  299,  371,  372,  373,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
   -1,  434,   -1,  389,   -1,   -1,   -1,  365,  262,  367,
   -1,  369,   -1,  371,  372,  373,   -1,   -1,   -1,   -1,
   -1,   -1,  408,   -1,  410,   -1,  340,   -1,   -1,   -1,
   -1,  389,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,   -1,  299,   -1,   -1,  434,   -1,
   -1,  365,  410,  367,  369,  369,  371,  371,  372,  373,
  375,  376,   -1,   -1,  379,   -1,  381,   -1,  383,  384,
   -1,   -1,   -1,  388,  389,  389,  434,   -1,   -1,   -1,
   -1,  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,
   -1,  406,   -1,  408,   -1,  410,  410,   -1,   -1,  262,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  366,   -1,  368,  369,  370,  371,   -1,  434,
  434,  375,  376,   -1,   -1,  379,   -1,  381,   -1,  383,
  384,  385,  386,  387,  388,  389,  299,   -1,  392,   -1,
  394,   -1,  396,   -1,  398,   -1,  400,   -1,  402,   -1,
  404,   -1,  406,   -1,  408,   -1,  410,   -1,  412,  262,
  414,   -1,  416,   -1,  418,   -1,  420,   -1,  422,   -1,
  424,   -1,  426,   -1,  428,   -1,  430,  340,  432,   -1,
  434,   -1,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,   -1,  299,   -1,   -1,   -1,
   -1,   -1,  365,   -1,  367,   -1,  369,   -1,  371,  372,
  373,   -1,   -1,   -1,   -1,   -1,  379,   -1,  381,   -1,
  383,  384,   -1,   -1,   -1,  388,  389,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  400,   -1,  402,
   -1,  404,   -1,  406,   -1,  408,   -1,  410,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  365,  366,   -1,  368,   -1,  370,  371,   -1,
   -1,  434,  375,  376,   -1,   -1,  379,   -1,  381,   -1,
  383,  384,  385,  386,  387,  388,  389,   -1,  261,  392,
   -1,  394,   -1,  396,   -1,  398,   -1,  400,   -1,  402,
   -1,  404,   -1,  406,   -1,  408,   -1,  410,   -1,   -1,
   -1,   -1,  285,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  261,   -1,  298,   -1,   -1,   -1,  432,
  303,  434,   -1,  306,   -1,  308,   -1,  310,  311,  312,
  313,   -1,   -1,   -1,   -1,  318,   -1,  285,   -1,  322,
   -1,   -1,   -1,  326,   -1,   -1,   -1,   -1,   -1,   -1,
  298,  334,   -1,   -1,  337,  303,  339,   -1,   -1,   -1,
  308,   -1,  310,  311,  312,  313,   -1,   -1,   -1,   -1,
  318,   -1,   -1,  261,  322,  263,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  366,   -1,   -1,  334,   -1,   -1,  337,
   -1,  339,   -1,   -1,   -1,   -1,   -1,  285,   -1,   -1,
   -1,   -1,   -1,  261,   -1,   -1,  389,   -1,   -1,   -1,
  298,  359,   -1,  361,   -1,  303,   -1,  365,   -1,   -1,
  308,   -1,  310,  311,  312,  313,   -1,  285,  316,   -1,
  318,   -1,   -1,  261,  322,  263,   -1,   -1,   -1,   -1,
  298,   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,  316,   -1,
  318,   -1,   -1,  261,  322,  263,   -1,   -1,   -1,   -1,
  298,   -1,   -1,   -1,   -1,  303,  334,  365,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,   -1,   -1,
  318,   -1,   -1,  261,  322,  263,   -1,   -1,   -1,   -1,
  298,   -1,   -1,   -1,   -1,  303,  334,  365,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,  316,   -1,
  318,   -1,   -1,  261,  322,   -1,   -1,   -1,   -1,   -1,
  298,   -1,   -1,   -1,   -1,  303,  334,  365,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,  316,   -1,
  318,   -1,   -1,  261,  322,   -1,   -1,   -1,   -1,   -1,
  298,   -1,   -1,   -1,   -1,  303,  334,   -1,   -1,  337,
  308,  339,  310,  311,  312,  313,   -1,  285,  316,   -1,
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

#line 5624 "cs-parser.jay"

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

		assign = new SimpleAssign (var, expr, decl.Location);

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
