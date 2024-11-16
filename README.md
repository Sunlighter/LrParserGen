<!-- -*- coding: utf-8; fill-column: 118 -*- -->

# LrParserGen

An LR(1) Parser Generator that obtains grammar rules by using reflection

## Purpose and Features

This is an LR(1) Parser Generator with a special feature: terminals and nonterminals can be instances of
`System.Type`, and production rules can be written as static methods on a class. For each method, the type of the
return value is the left hand side of the production rule, and the types of the arguments, in order, constitute the
right hand side of the production rule.  (To represent keywords and punctuation, an attribute can be placed on string
arguments.) The method itself is called in order to perform the reduction.

Another interesting feature is that there is an `ImmutableParserState` class. You can offer it a terminal, and if the
terminal is acceptable, it will perform zero or more reductions followed by a shift, and return a new parser state. It
also has a function which will tell you which terminals it will accept. Because it is LR(1), it will not do reductions
if offered an erroneous token. (As you would expect from the &ldquo;immutable&rdquo; name, the old parser state is not
modified, which makes backtracking or &ldquo;undo&rdquo; possible. So you could possibly use this as part of a user
interface, where the user could make various &ldquo;moves,&rdquo; which the machine could parse, and which the user
could undo if needed. Note, however, that this doesn&rsquo;t work if the objects on the parser stack are mutable and
are changed.) To get the state into an &ldquo;accept&rdquo; state, you must &ldquo;offer&rdquo; a
&ldquo;terminal&rdquo; with a type of `EofSymbol`, and this terminal will not actually be shifted.

Please refer to the `LrParserGenTest` project to see how this thing can be used.

## Caveats

This is not by any means &ldquo;production-ready&rdquo; software, so I have not made a NuGet package out of it, but I
thought it had some interesting features so I decided to publish it anyway.

I might change the interface in some ways. Right now I have my eye on `GrammarRuleAttribute`, and I&rsquo;m thinking
of doing away with it; maybe I should put a GrammarRulesAttribute on the *class*, and provide the &ldquo;goal&rdquo;
for parsing there, instead of putting an attribute on every production rule. Also, there is support for constructors
and instance methods to be production rules, but I don&rsquo;t know if that is actually useful, so I may remove it. I
was previously thinking of rounding up production rules from multiple types, but now I think that might be a bad idea.

The main reason why this program appears as a single big commit is because I have a large collection of personal
projects and I copy things out of that collection for publication. (This is also why you will sometimes see code which
has been commented out; there is probably some other copy where that code is used.)

I have included another (but slightly different) copy of the type traits code that appears in Macro Protocol. In some
ways I would like to create an all-purpose type traits library, instead of copying it everywhere, but there are a lot
of variations, some of which incur various forms of overhead, and I am never quite sure which ones to implement.

This implementation of the traits library has traits for `System.Type` and `System.Assembly` which would allow
instances of these types to be serialized and deserialized, but it might be dangerous to deserialize a file where an
attacker could have specified an arbitrary type or assembly to deserialize. It should be noted that I do *not* have
any code that loads assemblies as a result of deserialization. You can only deserialize an assembly if it is already
loaded. If the deserialized data specifies an assembly which is not loaded, an exception will be thrown during
deserialization. But I do not know if this is sufficient to ensure security.

If you have a large grammar, it may take a long time to generate parsing tables for it. There are various ways to use
threads to speed up the generation of parser tables, but I have not implemented that at present. Since the type traits
allow serialization, it should be possible to cache parser tables somewhere, in a file or database for example, but
that also is not implemented at present.

My &ldquo;lexical analyzers&rdquo; currently split on whitespace, which makes the lexers easy to write, but this means
you have to put spaces everywhere in the input to separate the tokens.

If the parser&rsquo;s input has a syntax error, it can be hard to find the error in the input. Right now you get a
&ldquo;token number.&rdquo; It may be necessary to use the Visual Studio debugger to see why the erroneous input is
erroneous.
