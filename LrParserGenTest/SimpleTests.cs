using Sunlighter.LrParserGenLib;
using Sunlighter.TypeTraitsLib;
using System.Collections.Immutable;

namespace LrParserGenTest
{
    [TestClass]
    public class SimpleTests
    {
        [TestMethod]
        public void SimpleTest()
        {
            ReflectionResults r1a = typeof(ThingyGrammar).BuildParser(new TypeSymbol(typeof(Thingy)));

            StaticReflectionResults r1 = r1a as StaticReflectionResults ?? throw new InvalidOperationException("r1a is not a StaticReflectionResults");

            Func<string, Token<object?>> tokenizeItem = Tokenization.MakeTokenizer(r1.SpecialTokens);

            ImmutableList<Token<object?>> tokens = Tokenization.Tokenize("( ( a , b ) , c )", tokenizeItem);

            object? result = ParserStateUtility.TryParse(r1.InitialState, tokens, null);

            Assert.IsInstanceOfType<Thingy>(result);

            Thingy t = (Thingy)result;

            Console.WriteLine(t.ToString());
        }
    }

    public abstract class Thingy
    {
        private static readonly Lazy<ITypeTraits<Thingy>> typeTraits = new Lazy<ITypeTraits<Thingy>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ITypeTraits<Thingy> GetTypeTraits()
        {
            RecursiveTypeTraits<Thingy> rec = new RecursiveTypeTraits<Thingy>();

            ITypeTraits<Thingy> tt = new UnionTypeTraits<string, Thingy>
            (
                StringTypeTraits.Value,
                [
                    new UnionCaseTypeTraits2<string, Thingy, TerminalThingy>
                    (
                        "terminal",
                        new ConvertTypeTraits<TerminalThingy, string>
                        (
                            t1 => t1.Value,
                            StringTypeTraits.Value,
                            s => new TerminalThingy(s)
                        )
                    ),
                    new UnionCaseTypeTraits2<string, Thingy, PairThingy>
                    (
                        "pair",
                        new ConvertTypeTraits<PairThingy, Tuple<Thingy, Thingy>>
                        (
                            p => new Tuple<Thingy, Thingy>(p.T1, p.T2),
                            new TupleTypeTraits<Thingy, Thingy>(rec, rec),
                            t2 => new PairThingy(t2.Item1, t2.Item2)
                        )
                    )
                ]
            );

            rec.Set(tt);

            return tt;
        }

        public static ITypeTraits<Thingy> Traits => typeTraits.Value;

        public override string ToString()
        {
            return Traits.ToDebugString(this);
        }
    }

    public sealed class TerminalThingy : Thingy
    {
        private readonly string value;

        public TerminalThingy(string value)
        {
            this.value = value;
        }

        public string Value => value;
    }

    public sealed class PairThingy : Thingy
    {
        private readonly Thingy t1;
        private readonly Thingy t2;

        public PairThingy(Thingy t1, Thingy t2)
        {
            this.t1 = t1;
            this.t2 = t2;
        }

        public Thingy T1 => t1;

        public Thingy T2 => t2;
    }

    public static class ThingyGrammar
    {
        [GrammarRule]
        public static Thingy TerminalRule(string str) => new TerminalThingy(str);

        [GrammarRule]
        public static Thingy PairRule([TokenTypeName("(")] string lParen, Thingy t1, [TokenTypeName(",")] string comma, Thingy t2, [TokenTypeName(")")] string rParen) => new PairThingy(t1, t2);
    }
}