<!-- -*- coding: utf-8; fill-column: 118 -*- -->

# LrParserGen

An LR(1) Parser Generator that obtains grammar rules by using reflection.

This is now available as a NuGet package, **Sunlighter.LrParserGenLib**.

## Purpose and Features

This is an LR(1) Parser Generator with a special feature: terminals and nonterminals can be instances of
`System.Type`, and production rules can be written as static methods on a class. For each method, the type of the
return value is the left hand side of the production rule, and the types of the arguments, in order, constitute the
right hand side of the production rule.  (To represent keywords and punctuation, an attribute can be placed on string
arguments.) The method itself is called in order to perform the reduction.

Another interesting feature is that there is an `ImmutableParserState` class. You can offer it a terminal, and if the
terminal is acceptable, it will perform zero or more reductions followed by a shift, and return a new parser state,
leaving the original parser state unmodified. It also has a function which will tell you which terminals it will
accept. Because it is LR(1), it will not do reductions if offered an erroneous token. (As you would expect from the
&ldquo;immutable&rdquo; name, the old parser state is not modified, which makes backtracking or &ldquo;undo&rdquo;
possible, by saving old parser states and returning to them. So you could possibly use this as part of a user
interface, where the user could make various &ldquo;moves,&rdquo; which the machine could parse, and which the user
could undo if needed. Note, however, that this doesn&rsquo;t work if the objects on the parser stack are mutable and
are changed.) To get the state into an &ldquo;accept&rdquo; state, you must &ldquo;offer&rdquo; a
&ldquo;terminal&rdquo; with a type of `EofSymbol`, and this terminal will not actually be shifted. You can then get
the value through the `AcceptedValue` property.

There is a precedence mechanism to resolve possible conflicts in the parsing table, but if after that the parsing
table still has conflicts, it is returned anyway, without warning. A (state, token) pair has a `ParseAction<S>` object
associated with it, where `S` is the type of the parser state (an integer, or a set of `Item` objects which indicate
positions in rules). A parse action can be a shift, a reduction, or a &ldquo;conflict&rdquo; consisting of other parse
actions. There is also an &ldquo;error&rdquo; action which can be returned by functions but is not actually stored.

## Usage

There are two general ways to use this library.

One way is to directly construct an instance of the `Grammar` class. The `Grammar` class has a number of lazy
properties such as the `IntParseTableData` class, which contains the parse table. This table can be serialized. Before
constructing the `Grammar` class, it is important to &ldquo;augment&rdquo; the list of rules by prepending a rule that
reduces the desired start symbol to `StartState.Value`.

The other way is to use the `ReflectionUtility.BuildParser` function. This function takes a start symbol and a type
(which should be a class, struct, or interface), and reflects over the type, and gathers any methods that have a
`[GrammarRule]` attribute. It constructs the appropriate grammar, and either constructs the parse table, or retrieves
it from an optional cache.

You have to implement the appropriate cache logic by implementing the interface `ICacheStorage`, which has only two
simple methods.

(**Warning:** if you retrieve the `IntParseTableData` property of the returned grammar, the retrieval causes the
grammar to compute the parse table *again*, which might be slow. The immutable parser state class also has an
`IntParseTableData` property, which can retrieve the parse table without causing recomputation.)

If all the methods are static, `BuildParser` returns a `StaticReflectionResults` class which has an `InitialState`
property, which is an immutable parser state.

If any of the methods is non-static, `BuildParser` returns an `InstanceReflectionResults` class which has a
`GetInitialState` function. The `GetInitialState` function takes an argument, which is the instance which will carry
out the reductions.  This allows, for example, that you can define an abstract class or an interface with grammar
rules on it, and run the same reductions with different instances.

The parameters and return types of the `[GrammarRule]` functions are used as tokens, with two exceptions. It is
possible to put a `[TokenTypeName("example")]` attribute on a parameter, which will cause tokens of that type to match
that parameter. The *actual* parameter type should usually be a `string` because that is what is returned by the
lexer, but it is possible to use this as a label in cases where two conceptually distinct types have to be represented
by the same C# type. This attribute is also necessary for things like operators, parentheses, commas, and so forth in
the grammar.

There is also a `[TokenType(typeof(Example))]` attribute, which can be used if a function returns a more specific type
than it should, or if it expects a less specific parameter type than it should. In practice I have not found a use for
this and it may be removed from a future version.

## Caveats

This is not &ldquo;production-ready&rdquo; software, but it is getting there. I intend to make a NuGet package out of
it at some point.

This code uses the **Sunlighter.TypeTraitsLib** library, which is now on NuGet.

The &ldquo;lexical analyzers&rdquo; included in this library currently split on whitespace, which makes the lexers
easy to write, but this means you have to put spaces everywhere in the input to separate the tokens. There is a
separate **LexerGen** project, which can be used with this one.

If the parser&rsquo;s input has a syntax error, it can be hard to find the error in the input. Right now you get a
&ldquo;token number.&rdquo; It may be necessary to use the Visual Studio debugger to see why the erroneous input is
erroneous.
