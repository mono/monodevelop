//
// StyleViewModel.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Refactoring.Options;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Refactoring.Options;

namespace MonoDevelop.CSharp.OptionProvider
{
	internal class CodeStylePage : Ide.Gui.Dialogs.OptionsPanel
	{
		GridOptionPreviewControl widget;

		void EnsureWidget ()
		{
			if (widget != null)
				return;
			widget = new GridOptionPreviewControl (null, (o, s) => new StyleViewModel (o, s));
		}

		public override Control CreatePanelWidget ()
		{
			EnsureWidget ();
			return (Gtk.Widget)Xwt.Toolkit.CurrentEngine.GetNativeWidget (widget);
		}

		public override void ApplyChanges ()
		{
			widget.SaveSettings ();
		}
	}

	/// <summary>
	/// This is the view model for CodeStyle options page.
	/// </summary>
	/// <remarks>
	/// The codestyle options page is defined in <see cref="CodeStylePage"/>
	/// </remarks>
	class StyleViewModel : AbstractOptionPreviewViewModel
	{
		#region "Preview Text"

		private const string s_fieldDeclarationPreviewTrue = @"
class C{
    int capacity;
    void Method()
    {
//[
        this.capacity = 0;
//]
    }
}";

		private const string s_fieldDeclarationPreviewFalse = @"
class C{
    int capacity;
    void Method()
    {
//[
        capacity = 0;
//]
    }
}";

		private const string s_propertyDeclarationPreviewTrue = @"
class C{
    public int Id { get; set; }
    void Method()
    {
//[
        this.Id = 0;
//]
    }
}";

		private const string s_propertyDeclarationPreviewFalse = @"
class C{
    public int Id { get; set; }
    void Method()
    {
//[
        Id = 0;
//]
    }
}";

		private const string s_eventDeclarationPreviewTrue = @"
using System;
class C{
    event EventHandler Elapsed;
    void Handler(object sender, EventArgs args)
    {
//[
        this.Elapsed += Handler;
//]
    }
}";

		private const string s_eventDeclarationPreviewFalse = @"
using System;
class C{
    event EventHandler Elapsed;
    void Handler(object sender, EventArgs args)
    {
//[
        Elapsed += Handler;
//]
    }
}";

		private const string s_methodDeclarationPreviewTrue = @"
using System;
class C{
    void Display()
    {
//[
        this.Display();
//]
    }
}";

		private const string s_methodDeclarationPreviewFalse = @"
using System;
class C{
    void Display()
    {
//[
        Display();
//]
    }
}";

		private const string s_intrinsicPreviewDeclarationTrue = @"
class Program
{
//[
    private int _member;
    static void M(int argument)
    {
        int local;
    }
//]
}";

		private const string s_intrinsicPreviewDeclarationFalse = @"
using System;
class Program
{
//[
    private Int32 _member;
    static void M(Int32 argument)
    {
        Int32 local;
    }
//]
}";

		private const string s_intrinsicPreviewMemberAccessTrue = @"
class Program
{
//[
    static void M()
    {
        var local = int.MaxValue;
    }
//]
}";

		private const string s_intrinsicPreviewMemberAccessFalse = @"
using System;
class Program
{
//[
    static void M()
    {
        var local = Int32.MaxValue;
    }
//]
}";

		private static readonly string s_varForIntrinsicsPreviewFalse = $@"
using System;
class C{{
    void Method()
    {{
//[
        int x = 5; // {GettextCatalog.GetString ("built-in types")}
//]
    }}
}}";
	

		private static readonly string s_varForIntrinsicsPreviewTrue = $@"
using System;
class C{{
    void Method()
    {{
//[
        var x = 5; // {GettextCatalog.GetString ("built-in types")}
//]
    }}
}}";

		private static readonly string s_varWhereApparentPreviewFalse = $@"
using System;
class C{{
    void Method()
    {{
//[
        C cobj = new C(); // {GettextCatalog.GetString ("type is apparent from assignment expression")}
//]
    }}
}}";

		private static readonly string s_varWhereApparentPreviewTrue = $@"
using System;
class C{{
    void Method()
    {{
//[
        var cobj = new C(); // {GettextCatalog.GetString ("type is apparent from assignment expression")}
//]
    }}
}}";

		private static readonly string s_varWherePossiblePreviewFalse = $@"
using System;
class C{{
    void Init()
    {{
//[
        Action f = this.Init(); // {GettextCatalog.GetString ("everywhere else")}
//]
    }}
}}";

		private static readonly string s_varWherePossiblePreviewTrue = $@"
using System;
class C{{
    void Init()
    {{
//[
        var f = this.Init(); // {GettextCatalog.GetString ("everywhere else")}
//]
    }}
}}";

		private static readonly string s_preferThrowExpression = $@"
using System;

class C
{{
    private string s;

    void M1(string s)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        this.s = s ?? throw new ArgumentNullException(nameof(s));
//]
    }}
    void M2(string s)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        if (s == null)
        {{
            throw new ArgumentNullException(nameof(s));
        }}

        this.s = s;
//]
    }}
}}
";
		private static readonly string s_preferCoalesceExpression = $@"
using System;

class C
{{
    private string s;

    void M1(string s)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var v = x ?? y;
//]
    }}
    void M2(string s)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
		var v = x != null ? x : y; // {GettextCatalog.GetString ("or")}
        var v = x == null ? y : x;
//]
    }}
}}
";

		private static readonly string s_preferConditionalDelegateCall = $@"
using System;

class C
{{
    private string s;

    void M1(string s)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        func?.Invoke(args);
//]
    }}
    void M2(string s)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        if (func != null)
        {{
            func(args);
        }}
//]
    }}
}}
";

		private static readonly string s_preferNullPropagation = $@"
using System;

class C
{{
    void M1(object o)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var v = o?.ToString();
//]
    }}
    void M2(object o)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
		var v = o == null ? null : o.ToString(); // {GettextCatalog.GetString ("or")}
        var v = o != null ? o.ToString() : null;
//]
    }}
}}
";

		private static readonly string s_preferPatternMatchingOverAsWithNullCheck = $@"
class C
{{
    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        if (o is string s)
        {{
        }}
//]
    }}
    void M2()
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        var s = o as string;
        if (s != null)
        {{
        }}
//]
    }}
}}
";
	
		private static readonly string s_preferConditionalExpressionOverIfWithAssignments = $@"
class C
{{
    void M()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        string s = expr ? ""hello"" : ""world"";

        // {GettextCatalog.GetString ("Over:")}
        string s;
        if (expr)
        {{
            s = ""hello"";
        }}
        else
        {{
            s = ""world"";
        }}
//]
    }}
}}
";

		private static readonly string s_preferConditionalExpressionOverIfWithReturns = $@"
class C
{{
    void M()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        return expr ? ""hello"" : ""world"";

        // {GettextCatalog.GetString ("Over:")}
        if (expr)
        {{
            return ""hello"";
        }}
        else
        {{
            return ""world"";
        }}
//]
    }}
}}
";

		private static readonly string s_preferPatternMatchingOverIsWithCastCheck = $@"
class C
{{
    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        if (o is int i)
        {{
        }}
//]
    }}
    void M2()
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        if (o is int)
        {{
            var i = (int)o;
        }}
//]
    }}
}}
";

		private static readonly string s_preferObjectInitializer = $@"
using System;

class Customer
{{
    private int Age;

    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var c = new Customer()
        {{
            Age = 21
        }};
//]
    }}
    void M2()
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        var c = new Customer();
        c.Age = 21;
//]
    }}
}}
";

		private static readonly string s_preferCollectionInitializer = $@"
using System.Collections.Generic;

class Customer
{{
    private int Age;

    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var list = new List<int>
        {{
            1,
            2,
            3
        }};
//]
    }}
    void M2()
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        var list = new List<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);
//]
    }}
}}
";

		private static readonly string s_preferExplicitTupleName = $@"
class Customer
{{
    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        (string name, int age) customer = GetCustomer();
        var name = customer.name;
        var age = customer.age;
//]
    }}
    void M2()
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        (string name, int age) customer = GetCustomer();
        var name = customer.Item1;
        var age = customer.Item2;
//]
    }}
}}
";

		private static readonly string s_preferSimpleDefaultExpression = $@"
using System.Threading;

class Customer1
{{
//[
    // {GettextCatalog.GetString ("Prefer:")}
    void DoWork(CancellationToken cancellationToken = default) {{ }}
//]
}}
class Customer2
{{
//[
    // {GettextCatalog.GetString ("Over:")}
    void DoWork(CancellationToken cancellationToken = default(CancellationToken)) {{ }}
//]
}}
";

		private static readonly string s_preferInferredTupleName = $@"
using System.Threading;

class Customer
{{
    void M1(int age, string name)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var tuple = (age, name);
//]
    }}
    void M2(int age, string name)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        var tuple = (age: age, name: name);
//]
    }}
}}
";

		private static readonly string s_preferInferredAnonymousTypeMemberName = $@"
using System.Threading;

class Customer
{{
    void M1(int age, string name)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var anon = new {{ age, name }};
//]
    }}
    void M2(int age, string name)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        var anon = new {{ age = age, name = name }};
//]
    }}
}}
";

		private static readonly string s_preferInlinedVariableDeclaration = $@"
using System;

class Customer
{{
    void M1(string value)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        if (int.TryParse(value, out int i))
        {{
        }}
//]
    }}
    void M2(string value)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        int i;
        if (int.TryParse(value, out i))
        {{
        }}
//]
    }}
}}
";

		private static readonly string s_preferDeconstructedVariableDeclaration = $@"
using System;

class Customer
{{
    void M1(string value)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var (name, age) = GetPersonTuple();
        Console.WriteLine($""{{name}} {{age}}"");

        (int x, int y) = GetPointTuple();
        Console.WriteLine($""{{x}} {{y}}"");
//]
    }}
    void M2(string value)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        var person = GetPersonTuple();
        Console.WriteLine($""{{person.name}} {{person.age}}"");

        (int x, int y) point = GetPointTuple();
        Console.WriteLine($""{{point.x}} {{point.y}}"");
//]
    }}
}}
";

		private static readonly string s_preferBraces = $@"
using System;

class Customer
{{
    private int Age;

    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        if (test)
        {{
            this.Display();
        }}
//]
    }}
    void M2()
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        if (test)
            this.Display();
//]
    }}
}}
";

		private static readonly string s_preferAutoProperties = $@"
using System;

class Customer1
{{
//[
    // {GettextCatalog.GetString ("Prefer:")}
    public int Age {{ get; }}
//]
}}
class Customer2
{{
//[
    // {GettextCatalog.GetString ("Over:")}
    private int age;

    public int Age
    {{
        get
        {{
            return age;
        }}
    }}
//]
}}
";

		private static readonly string s_preferLocalFunctionOverAnonymousFunction = $@"
using System;

class Customer
{{
    void M1(string value)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        int fibonacci(int n)
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }}
//]
    }}
    void M2(string value)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        Func<int, int> fibonacci = null;
        fibonacci = (int n) =>
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }};
//]
    }}
}}
";

		private static readonly string s_preferIsNullOverReferenceEquals = $@"
using System;

class Customer
{{
    void M1(string value1, string value2)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        if (value1 is null)
            return;

        if (value2 is null)
            return;
//]
    }}
    void M2(string value1, string value2)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        if (object.ReferenceEquals(value1, null))
            return;

        if ((object)value2 == null)
            return;
//]
    }}
}}
";

		#region expression and block bodies

		private const string s_preferExpressionBodyForMethods = @"
using System;

//[
class Customer
{
    private int Age;

    public int GetAge() => this.Age;
}
//]
";

		private const string s_preferBlockBodyForMethods = @"
using System;

//[
class Customer
{
    private int Age;

    public int GetAge()
    {
        return this.Age;
    }
}
//]
";

		private const string s_preferExpressionBodyForConstructors = @"
using System;

//[
class Customer
{
    private int Age;

    public Customer(int age) => Age = age;
}
//]
";

		private const string s_preferBlockBodyForConstructors = @"
using System;

//[
class Customer
{
    private int Age;

    public Customer(int age)
    {
        Age = age;
    }
}
//]
";

		private const string s_preferExpressionBodyForOperators = @"
using System;

struct ComplexNumber
{
//[
    public static ComplexNumber operator +(ComplexNumber c1, ComplexNumber c2)
        => new ComplexNumber(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary);
//]
}
";

		private const string s_preferBlockBodyForOperators = @"
using System;

struct ComplexNumber
{
//[
    public static ComplexNumber operator +(ComplexNumber c1, ComplexNumber c2)
    {
        return new ComplexNumber(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary);
    }
//]
}
";

		private const string s_preferExpressionBodyForProperties = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age => _age;
}
//]
";

		private const string s_preferBlockBodyForProperties = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age { get { return _age; } }
}
//]
";

		private const string s_preferExpressionBodyForAccessors = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age
    {
        get => _age;
        set => _age = value;
    }
}
//]
";

		private const string s_preferBlockBodyForAccessors = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age
    {
        get { return _age; }
        set { _age = value; }
    }
}
//]
";

		private const string s_preferExpressionBodyForIndexers = @"
using System;

//[
class List<T>
{
    private T[] _values;
    public T this[int i] => _values[i];
}
//]
";

		private const string s_preferBlockBodyForIndexers = @"
using System;

//[
class List<T>
{
    private T[] _values;
    public T this[int i] { get { return _values[i]; } }
}
//]
";

		private static readonly string s_preferReadonly = $@"
class Customer1
{{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        // '_value' can only be assigned in constructor
        private readonly int _value = 0;
//]
}}
class Customer2
{{
//[
        // {GettextCatalog.GetString ("Over:")}
        // '_value' can be assigned anywhere
        private int _value = 0;
//]
}}
";

		#endregion

		#region arithmetic binary parentheses

		private readonly string s_arithmeticBinaryAlwaysForClarity = $@"
class C
{{
    void M()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var v = a + (b * c);

        // {GettextCatalog.GetString ("Over:")}
        var v = a + b * c;
//]
    }}
}}
";

		private readonly string s_arithmeticBinaryNeverIfUnnecessary = $@"
class C
{{
    void M()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var v = a + b * c;

        // {GettextCatalog.GetString ("Over:")}
        var v = a + (b * c);
//]
    }}
}}
";

		#endregion

		#region relational binary parentheses

		private readonly string s_relationalBinaryIgnore = $@"
class C
{{
    void M()
    {{
//[
        // {GettextCatalog.GetString ("Keep all parentheses in:")}
        var v = (a < b) == (c > d);
//]
    }}
}}
";

		private readonly string s_relationalBinaryNeverIfUnnecessary = $@"
class C
{{
    void M()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var v = a < b == c > d;

        // {GettextCatalog.GetString ("Over:")}
        var v = (a < b) == (c > d);
//]
    }}
}}
";

		#endregion
	
		#region other binary parentheses

		private readonly string s_otherBinaryAlwaysForClarity = $@"
class C
{{
    void M()
    {{
//[
        // {GettextCatalog.GetString("Prefer:")}
        var v = a || (b && c);

        // {GettextCatalog.GetString("Over:")}
        var v = a || b && c;
//]
    }}
}}
";

		private readonly string s_otherBinaryNeverIfUnnecessary = $@"
class C
{{
    void M()
    {{
//[
        // {GettextCatalog.GetString("Prefer:")}
        var v = a || b && c;

        // {GettextCatalog.GetString("Over:")}
        var v = a || (b && c);
//]
    }}
}}
";

		#endregion

		#region other parentheses

		private readonly string s_otherParenthesesIgnore = $@"
class C
{{
    void M()
    {{
//[
        // {GettextCatalog.GetString("Keep all parentheses in:")}
        var v = (a.b).Length;
//]
    }}
}}
";

		private readonly string s_otherParenthesesNeverIfUnnecessary = $@"
class C
{{
    void M()
    {{
//[
        // {GettextCatalog.GetString("Prefer:")}
        var v = a.b.Length;

        // {GettextCatalog.GetString("Over:")}
        var v = (a.b).Length;
//]
    }}
}}
";

		#endregion

		#endregion

		internal StyleViewModel (OptionSet optionSet, IServiceProvider serviceProvider) : base (optionSet, serviceProvider, LanguageNames.CSharp)
		{
			//var collectionView = (ListCollectionView)CollectionViewSource.GetDefaultView (CodeStyleItems);
			//collectionView.GroupDescriptions.Add (new PropertyGroupDescription (nameof (AbstractCodeStyleOptionViewModel.GroupName)));

			var qualifyGroupTitle = GettextCatalog.GetString ("'this.' preferences:");
			var predefinedTypesGroupTitle = GettextCatalog.GetString("predefined type preferences:");
			var varGroupTitle = GettextCatalog.GetString ("'var' preferences:");
			var nullCheckingGroupTitle = GettextCatalog.GetString ("'null' checking:");
			var fieldGroupTitle = GettextCatalog.GetString("Field preferences:");
			var codeBlockPreferencesGroupTitle = GettextCatalog.GetString("Code block preferences:");
			var expressionPreferencesGroupTitle = GettextCatalog.GetString("Expression preferences:");
			var variablePreferencesGroupTitle = GettextCatalog.GetString("Variable preferences:");

			var qualifyMemberAccessPreferences = new List<CodeStylePreference>
			{
				new CodeStylePreference(GettextCatalog.GetString("Prefer 'this.'"), isChecked: true),
				new CodeStylePreference(GettextCatalog.GetString("Do not prefer 'this.'"), isChecked: false),
			};

			var predefinedTypesPreferences = new List<CodeStylePreference>
			{
				new CodeStylePreference(GettextCatalog.GetString("Prefer predefined type"), isChecked: true),
				new CodeStylePreference(GettextCatalog.GetString("Prefer framework type"), isChecked: false),
			};

			var typeStylePreferences = new List<CodeStylePreference>
			{
				new CodeStylePreference(GettextCatalog.GetString("Prefer 'var'"), isChecked: true),
				new CodeStylePreference(GettextCatalog.GetString("Prefer explicit type"), isChecked: false),
			};

			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.QualifyFieldAccess, GettextCatalog.GetString ("Qualify field access with 'this'"), s_fieldDeclarationPreviewTrue, s_fieldDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.QualifyPropertyAccess, GettextCatalog.GetString ("Qualify property access with 'this'"), s_propertyDeclarationPreviewTrue, s_propertyDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.QualifyMethodAccess, GettextCatalog.GetString ("Qualify method access with 'this'"), s_methodDeclarationPreviewTrue, s_methodDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.QualifyEventAccess, GettextCatalog.GetString ("Qualify event access with 'this'"), s_eventDeclarationPreviewTrue, s_eventDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));

			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, GettextCatalog.GetString("For locals, parameters and members"), s_intrinsicPreviewDeclarationTrue, s_intrinsicPreviewDeclarationFalse, this, optionSet, predefinedTypesGroupTitle, predefinedTypesPreferences));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, GettextCatalog.GetString("For member access expressions"), s_intrinsicPreviewMemberAccessTrue, s_intrinsicPreviewMemberAccessFalse, this, optionSet, predefinedTypesGroupTitle, predefinedTypesPreferences));

			// Use var
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, GettextCatalog.GetString("For built-in types"), s_varForIntrinsicsPreviewTrue, s_varForIntrinsicsPreviewFalse, this, optionSet, varGroupTitle, typeStylePreferences));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, GettextCatalog.GetString("When variable type is apparent"), s_varWhereApparentPreviewTrue, s_varWhereApparentPreviewFalse, this, optionSet, varGroupTitle, typeStylePreferences));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CSharpCodeStyleOptions.UseImplicitTypeWherePossible, GettextCatalog.GetString("Elsewhere"), s_varWherePossiblePreviewTrue, s_varWherePossiblePreviewFalse, this, optionSet, varGroupTitle, typeStylePreferences));

			// Code block
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CSharpCodeStyleOptions.PreferBraces, GettextCatalog.GetString("Prefer braces"), s_preferBraces, s_preferBraces, this, optionSet, codeBlockPreferencesGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferAutoProperties, GettextCatalog.GetString("Prefer auto properties"), s_preferAutoProperties, s_preferAutoProperties, this, optionSet, codeBlockPreferencesGroupTitle));

			AddParenthesesOptions (Options);

			// Expression preferences
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferObjectInitializer, GettextCatalog.GetString("Prefer object initializer"), s_preferObjectInitializer, s_preferObjectInitializer, this, optionSet, expressionPreferencesGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferCollectionInitializer, GettextCatalog.GetString("Prefer collection initializer"), s_preferCollectionInitializer, s_preferCollectionInitializer, this, optionSet, expressionPreferencesGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, GettextCatalog.GetString ("Prefer pattern matching over 'is' with 'cast' check"), s_preferPatternMatchingOverIsWithCastCheck, s_preferPatternMatchingOverIsWithCastCheck, this, optionSet, expressionPreferencesGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, GettextCatalog.GetString("Prefer pattern matching over 'as' with 'null' check"), s_preferPatternMatchingOverAsWithNullCheck, s_preferPatternMatchingOverAsWithNullCheck, this, optionSet, expressionPreferencesGroupTitle));
			//CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferConditionalExpressionOverAssignment, GettextCatalog.GetString("Prefer conditional expression over 'if' with assignments"), s_preferConditionalExpressionOverIfWithAssignments, s_preferConditionalExpressionOverIfWithAssignments, this, optionSet, expressionPreferencesGroupTitle));
			//CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferConditionalExpressionOverReturn, GettextCatalog.GetString("Prefer conditional expression over 'if' with returns"), s_preferConditionalExpressionOverIfWithReturns, s_preferConditionalExpressionOverIfWithReturns, this, optionSet, expressionPreferencesGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferExplicitTupleNames, GettextCatalog.GetString("Prefer explicit tuple name"), s_preferExplicitTupleName, s_preferExplicitTupleName, this, optionSet, expressionPreferencesGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CSharpCodeStyleOptions.PreferSimpleDefaultExpression, GettextCatalog.GetString("Prefer simple 'default' expression"), s_preferSimpleDefaultExpression, s_preferSimpleDefaultExpression, this, optionSet, expressionPreferencesGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferInferredTupleNames, GettextCatalog.GetString("Prefer inferred tuple element names"), s_preferInferredTupleName, s_preferInferredTupleName, this, optionSet, expressionPreferencesGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferInferredAnonymousTypeMemberNames, GettextCatalog.GetString("Prefer inferred anonymous type member names"), s_preferInferredAnonymousTypeMemberName, s_preferInferredAnonymousTypeMemberName, this, optionSet, expressionPreferencesGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, GettextCatalog.GetString("Prefer local function over anonymous function"), s_preferLocalFunctionOverAnonymousFunction, s_preferLocalFunctionOverAnonymousFunction, this, optionSet, expressionPreferencesGroupTitle));

			AddExpressionBodyOptions (optionSet, expressionPreferencesGroupTitle);

			// Variable preferences
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferInlinedVariableDeclaration, GettextCatalog.GetString("Prefer inlined variable declaration"), s_preferInlinedVariableDeclaration, s_preferInlinedVariableDeclaration, this, optionSet, variablePreferencesGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferDeconstructedVariableDeclaration, GettextCatalog.GetString("Prefer deconstructed variable declaration"), s_preferDeconstructedVariableDeclaration, s_preferDeconstructedVariableDeclaration, this, optionSet, variablePreferencesGroupTitle));

			// Null preferences.
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferThrowExpression, GettextCatalog.GetString("Prefer throw-expression"), s_preferThrowExpression, s_preferThrowExpression, this, optionSet, nullCheckingGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CSharpCodeStyleOptions.PreferConditionalDelegateCall, GettextCatalog.GetString("Prefer conditional delegate call"), s_preferConditionalDelegateCall, s_preferConditionalDelegateCall, this, optionSet, nullCheckingGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferCoalesceExpression, GettextCatalog.GetString("Prefer coalesce expression"), s_preferCoalesceExpression, s_preferCoalesceExpression, this, optionSet, nullCheckingGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferNullPropagation, GettextCatalog.GetString("Prefer null propagation"), s_preferNullPropagation, s_preferNullPropagation, this, optionSet, nullCheckingGroupTitle));
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferIsNullCheckOverReferenceEqualityMethod, GettextCatalog.GetString("Prefer 'is null' for reference equality checks"), s_preferIsNullOverReferenceEquals, s_preferIsNullOverReferenceEquals, this, optionSet, nullCheckingGroupTitle));

			// Field preferences.
			CodeStyleItems.Add (new BooleanCodeStyleOptionViewModel (CodeStyleOptions.PreferReadonly, GettextCatalog.GetString("Prefer readonly"), s_preferReadonly, s_preferReadonly, this, optionSet, fieldGroupTitle));
		}

		private void AddParenthesesOptions (OptionSet optionSet)
		{ // CodeStyleOptions atm unsupported - implement in one of the next rosyln releases.
			//AddParenthesesOption (
			//	LanguageNames.CSharp, optionSet, CodeStyleOptions.ArithmeticBinaryParentheses,
			//	GettextCatalog.GetString("In arithmetic operators:  *   /   %   +   -   <<   >>   &   ^   |"),
			//	new [] { s_arithmeticBinaryAlwaysForClarity, s_arithmeticBinaryNeverIfUnnecessary },
			//	isIgnoreOption: false);

			//AddParenthesesOption (
			//	LanguageNames.CSharp, optionSet, CodeStyleOptions.OtherBinaryParentheses,
			//	GettextCatalog.GetString("In other binary operators:  &&   ||   ??"),
			//	new [] { s_otherBinaryAlwaysForClarity, s_otherBinaryNeverIfUnnecessary },
			//	isIgnoreOption: false);

			//AddParenthesesOption (
			//	LanguageNames.CSharp, optionSet, CodeStyleOptions.RelationalBinaryParentheses,
			//	GettextCatalog.GetString("In relational operators:  <   >   <=   >=   is   as   ==   !="),
			//	new [] { s_relationalBinaryIgnore, s_relationalBinaryNeverIfUnnecessary },
			//	isIgnoreOption: true);

			//AddParenthesesOption (
				//LanguageNames.CSharp, optionSet, CodeStyleOptions.OtherParentheses,
				//GettextCatalog.GetString("In_other_operators,
				//new [] { s_otherParenthesesIgnore, s_otherParenthesesNeverIfUnnecessary },
				//isIgnoreOption: true);
		}

		private void AddExpressionBodyOptions (OptionSet optionSet, string expressionPreferencesGroupTitle)
		{
			var expressionBodyPreferences = new List<CodeStylePreference>
			{
				new CodeStylePreference(GettextCatalog.GetString("Never"), isChecked: false),
				new CodeStylePreference(GettextCatalog.GetString("When possible"), isChecked: false),
				new CodeStylePreference(GettextCatalog.GetString("When on single line"), isChecked: false),
			};

			var enumValues = new [] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine };

			CodeStyleItems.Add (new EnumCodeStyleOptionViewModel<ExpressionBodyPreference> (
				CSharpCodeStyleOptions.PreferExpressionBodiedMethods,
				GettextCatalog.GetString("Use expression body for methods"),
				enumValues,
				new [] { s_preferBlockBodyForMethods, s_preferExpressionBodyForMethods, s_preferExpressionBodyForMethods },
				this, optionSet, expressionPreferencesGroupTitle, expressionBodyPreferences));

			CodeStyleItems.Add (new EnumCodeStyleOptionViewModel<ExpressionBodyPreference> (
				CSharpCodeStyleOptions.PreferExpressionBodiedConstructors,
				GettextCatalog.GetString("Use expression body for constructors"),
				enumValues,
				new [] { s_preferBlockBodyForConstructors, s_preferExpressionBodyForConstructors, s_preferExpressionBodyForConstructors },
				this, optionSet, expressionPreferencesGroupTitle, expressionBodyPreferences));

			CodeStyleItems.Add (new EnumCodeStyleOptionViewModel<ExpressionBodyPreference> (
				CSharpCodeStyleOptions.PreferExpressionBodiedOperators,
				GettextCatalog.GetString("Use expression body for operators"),
				enumValues,
				new [] { s_preferBlockBodyForOperators, s_preferExpressionBodyForOperators, s_preferExpressionBodyForOperators },
				this, optionSet, expressionPreferencesGroupTitle, expressionBodyPreferences));

			CodeStyleItems.Add (new EnumCodeStyleOptionViewModel<ExpressionBodyPreference> (
				CSharpCodeStyleOptions.PreferExpressionBodiedProperties,
				GettextCatalog.GetString("Use expression body for properties"),
				enumValues,
				new [] { s_preferBlockBodyForProperties, s_preferExpressionBodyForProperties, s_preferExpressionBodyForProperties },
				this, optionSet, expressionPreferencesGroupTitle, expressionBodyPreferences));

			CodeStyleItems.Add (new EnumCodeStyleOptionViewModel<ExpressionBodyPreference> (
				CSharpCodeStyleOptions.PreferExpressionBodiedIndexers,
				GettextCatalog.GetString("Use expression body for indexers"),
				enumValues,
				new [] { s_preferBlockBodyForIndexers, s_preferExpressionBodyForIndexers, s_preferExpressionBodyForIndexers },
				this, optionSet, expressionPreferencesGroupTitle, expressionBodyPreferences));

			CodeStyleItems.Add (new EnumCodeStyleOptionViewModel<ExpressionBodyPreference> (
				CSharpCodeStyleOptions.PreferExpressionBodiedAccessors,
				GettextCatalog.GetString("Use expression body for accessors"),
				enumValues,
				new [] { s_preferBlockBodyForAccessors, s_preferExpressionBodyForAccessors, s_preferExpressionBodyForAccessors },
				this, optionSet, expressionPreferencesGroupTitle, expressionBodyPreferences));
		}
	}
}
