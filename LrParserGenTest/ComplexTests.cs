using Sunlighter.LrParserGenLib;
using Sunlighter.TypeTraitsLib;
using Sunlighter.TypeTraitsLib.Building;
using System.Collections.Immutable;
using System.Numerics;

namespace LrParserGenTest
{
    [TestClass]
    public class ComplexTests
    {
        [TestMethod]
        public void ComplexTest()
        {
            ReflectionResults r1a = typeof(ComplexGrammar).BuildParser(new TypeSymbol(typeof(ImmutableList<TopLevelElement>)));

            StaticReflectionResults r1 = r1a as StaticReflectionResults ?? throw new InvalidOperationException("r1a is not a StaticReflectionResults");

            Func<string, Token<object?>> tokenizeItem = Tokenization.MakeTokenizer(r1.SpecialTokens);

            ImmutableList<Token<object?>> tokens = Tokenization.Tokenize
            (
                """
                function Blah ( int x ) : int
                {
                    return 0 ;
                }

                function Blah2 ( ) : int
                {
                    return 3 ;
                }
                """,
                tokenizeItem
            );

            object? result = ParserStateUtility.TryParse(r1.InitialState, tokens, null);

            Assert.IsInstanceOfType<ImmutableList<TopLevelElement>>(result);

            ImmutableList<TopLevelElement> t = (ImmutableList<TopLevelElement>)result;

            Console.WriteLine(t.Count);

            ITypeTraits<ImmutableList<TopLevelElement>> traits =
                Builder.Instance.GetTypeTraits<ImmutableList<TopLevelElement>>();

            Console.WriteLine(traits.ToDebugString(t));
        }

        [UnionOfDescendants]
        public abstract class TopLevelElement
        {

        }

        [Record]
        [UnionCaseName("function")]
        public sealed class FunctionElement : TopLevelElement
        {
            private readonly string name;
            private readonly ImmutableList<ArgInfo> args;
            private readonly MyType returnType;
            private readonly ImmutableList<Statement> body;

            public FunctionElement
            (
                [Bind("name")] string name,
                [Bind("args")] ImmutableList<ArgInfo> args,
                [Bind("return-type")] MyType returnType,
                [Bind("body")] ImmutableList<Statement> body
            )
            {
                this.name = name;
                this.args = args;
                this.returnType = returnType;
                this.body = body;
            }

            [Bind("name")]
            public string Name => name;

            [Bind("args")]
            public ImmutableList<ArgInfo> Args => args;

            [Bind("return-type")]
            public MyType ReturnType => returnType;

            [Bind("body")]
            public ImmutableList<Statement> Body => body;
        }

        [Record]
        public sealed class ArgInfo
        {
            private readonly MyType type;
            private readonly string name;

            public ArgInfo([Bind("type")] MyType type, [Bind("name")] string name)
            {
                this.type = type;
                this.name = name;
            }

            [Bind("type")]
            public MyType Type => type;

            [Bind("name")]
            public string Name => name;
        }

        [UnionOfDescendants]
        public abstract class MyType
        {

        }

        [Singleton(0x8689D6A3u)]
        [UnionCaseName("int")]
        public sealed class MyIntegerType : MyType
        {
            private static readonly MyIntegerType value = new MyIntegerType();

            private MyIntegerType() { }

            public static MyIntegerType Value => value;
        }

        [UnionOfDescendants]
        public abstract class Statement
        {

        }

        [Record]
        [UnionCaseName("return-statement")]
        public sealed class ReturnStatement : Statement
        {
            private readonly MyExpression value;

            public ReturnStatement([Bind("value")] MyExpression value)
            {
                this.value = value;
            }

            [Bind("value")]
            public MyExpression Value => value;
        }

        [UnionOfDescendants]
        public abstract class MyExpression
        {
            
        }

        [Record]
        [UnionCaseName("literal-integer-expr")]
        public sealed class LiteralIntegerExpression : MyExpression
        {
            private readonly BigInteger value;

            public LiteralIntegerExpression([Bind("value")] BigInteger value)
            {
                this.value = value;
            }

            [Bind("value")]
            public BigInteger Value => value;
        }

        public static class ComplexGrammar
        {
            [GrammarRule]
            public static ImmutableList<TopLevelElement> TopLevelElements_Empty() => ImmutableList<TopLevelElement>.Empty;

            [GrammarRule]
            public static ImmutableList<TopLevelElement> TopLevelElements_Add(ImmutableList<TopLevelElement> items, TopLevelElement item) => items.Add(item);

            [GrammarRule]
            public static TopLevelElement TopLevelElement_Function
            (
                [TokenTypeName("function")] string _func,
                string name,
                [TokenTypeName("(")] string _lparen,
                ImmutableList<ArgInfo> args,
                [TokenTypeName(")")] string _rparen,
                [TokenTypeName(":")] string _colon,
                MyType returnType,
                [TokenTypeName("{")] string _lbrace,
                ImmutableList<Statement> body,
                [TokenTypeName("}")] string _rbrace
            ) => new FunctionElement(name, args, returnType, body);

            [GrammarRule]
            public static ImmutableList<ArgInfo> ArgInfoList_Empty() => ImmutableList<ArgInfo>.Empty;

            [GrammarRule]
            public static ImmutableList<ArgInfo> ArgInfoList_One(ArgInfo item) => ImmutableList<ArgInfo>.Empty.Add(item);

            [GrammarRule]
            public static ImmutableList<ArgInfo> ArgInfoList_Add(ImmutableList<ArgInfo> items, [TokenTypeName(",")] string _comma, ArgInfo item) => items.Add(item);

            [GrammarRule]
            public static ArgInfo ArgInfo(MyType argType, string name) => new ArgInfo(argType, name);

            [GrammarRule]
            public static MyType IntegerType([TokenTypeName("int")] string _int) => MyIntegerType.Value;

            [GrammarRule]
            public static ImmutableList<Statement> StatementList_Empty() => ImmutableList<Statement>.Empty;

            [GrammarRule]
            public static ImmutableList<Statement> StatementList_Add(ImmutableList<Statement> items, Statement item) => items.Add(item);

            [GrammarRule]
            public static Statement ReturnStatement([TokenTypeName("return")] string _return, MyExpression expr, [TokenTypeName(";")] string _semi) => new ReturnStatement(expr);

            [GrammarRule]
            public static MyExpression IntegerExpression(BigInteger b) => new LiteralIntegerExpression(b);
        }
    }
}
