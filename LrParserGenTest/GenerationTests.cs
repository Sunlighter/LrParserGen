using Sunlighter.LrParserGenLib;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Sunlighter.TypeTraitsLib;
using Sunlighter.TypeTraitsLib.Building;
using System.Diagnostics;

namespace LrParserGenTest
{
    [TestClass]
    public class GenerationTests
    {
        private static Grammar RunDiagnostics(ImmutableList<Rule> rules, ImmutableList<PrecedenceRule> precedenceRules)
        {
            Debug.WriteLine("Rules:");

            foreach (int i in Enumerable.Range(0, rules.Count))
            {
                Debug.WriteLine("  " + i + ": " + Builder.Instance.GetTypeTraits<Rule>().ToDebugString(rules[i]));
            }

            Grammar g = new Grammar(rules, precedenceRules);

            Debug.WriteLine("Nonterminals:");
            Debug.WriteLine(Grammar.SymbolSetTypeTraits.ToDebugString(g.Nonterminals));

            Debug.WriteLine("Grammar Symbols:");
            Debug.WriteLine(Grammar.SymbolSetTypeTraits.ToDebugString(g.GrammarSymbols));

            Debug.WriteLine("Terminals:");
            Debug.WriteLine(Grammar.SymbolSetTypeTraits.ToDebugString(g.Terminals));

            Assert.IsTrue(g.Terminals.Count > 0);

            Debug.WriteLine("Nullable:");
            Debug.WriteLine(Grammar.SymbolSetTypeTraits.ToDebugString(g.NullableSet));

            Debug.WriteLine("Interminable:");
            Debug.WriteLine(Grammar.SymbolSetTypeTraits.ToDebugString(g.GrammarSymbols.Except(g.TerminableSet)));

            Debug.WriteLine("Unreachable:");
            Debug.WriteLine(Grammar.SymbolSetTypeTraits.ToDebugString(g.GrammarSymbols.Except(g.ReachableSet)));

            Debug.WriteLine("First Table");
            Debug.WriteLine(Grammar.FirstTableTypeTraits.ToDebugString(g.FirstTable));

            Debug.WriteLine("Follow Table");
            Debug.WriteLine(Grammar.FirstTableTypeTraits.ToDebugString(g.FollowTable));

            Debug.WriteLine("Initial State Set");
            Debug.WriteLine(Builder.Instance.GetTypeTraits<ItemSet>().ToDebugString(g.InitialStateSet));

            ITypeTraits<ParseAction<ItemSet>> itemSetParseActionCompareWorker = Builder.Instance.GetTypeTraits<ParseAction<ItemSet>>();

            Debug.WriteLine("Parse action on seeing \"EOF\"");
            Debug.WriteLine(itemSetParseActionCompareWorker.ToDebugString(g.GetParseAction(g.InitialStateSet, EofSymbol.Value)));

            if (g.GrammarSymbols.Contains(new NamedSymbol("begin")))
            {
                Debug.WriteLine("Parse action on seeing \"begin\"");
                Debug.WriteLine(itemSetParseActionCompareWorker.ToDebugString(g.GetParseAction(g.InitialStateSet, new NamedSymbol("begin"))));
            }

            Debug.WriteLine("Item Set Parse Table");

            foreach (KeyValuePair<ItemSet, ImmutableSortedDictionary<Symbol, ParseAction<ItemSet>>> kvp in g.ItemSetParseTable)
            {
                Debug.WriteLine("  " + Builder.Instance.GetTypeTraits<ItemSet>().ToDebugString(kvp.Key));
                foreach (KeyValuePair<Symbol, ParseAction<ItemSet>> kvp2 in kvp.Value)
                {
                    Debug.WriteLine("    " + Builder.Instance.GetTypeTraits<Symbol>().ToDebugString(kvp2.Key) + " " + itemSetParseActionCompareWorker.ToDebugString(kvp2.Value));
                }
            }

            ITypeTraits<ParseAction<int>> intParseActionCompareWorker = Builder.Instance.GetTypeTraits<ParseAction<int>>();

            Debug.WriteLine("Int Parse Table");

            foreach (int i in Enumerable.Range(0, g.IntParseTableData.ParseTable.Count))
            {
                Debug.WriteLine("  " + i);
                foreach (KeyValuePair<Symbol, ParseAction<int>> kvp2 in g.IntParseTableData.ParseTable[i])
                {
                    Debug.WriteLine("    " + Builder.Instance.GetTypeTraits<Symbol>().ToDebugString(kvp2.Key) + " " + intParseActionCompareWorker.ToDebugString(kvp2.Value));
                }
            }

            Debug.WriteLine($"Start State: {g.IntParseTableData.StartState}");

            return g;
        }

        [TestMethod]
        public void TestTableGeneration()
        {
            ImmutableList<Rule> rules =
            [
                MakeStartRule("program"),
                MakeRule("program", "begin programparts end"),
                MakeRule("programparts", "programparts programpart"),
                MakeRule("programparts", ""),
                MakeRule("programpart", "class"),
                MakeRule("programpart", "interface"),
            ];

            RunDiagnostics(rules, ImmutableList<PrecedenceRule>.Empty);
        }

        [TestMethod]
        public void TestLookahead()
        {
            ImmutableList<Rule> rules =
            [
                MakeStartRule("troublesome-phrase"),
                MakeRule("troublesome-phrase", "cart-animal and-cart"),
                MakeRule("troublesome-phrase", "plow-animal and-plow"),
                MakeRule("cart-animal", "horse"),
                MakeRule("cart-animal", "ox"),
                MakeRule("plow-animal", "horse"),
                MakeRule("plow-animal", "ox"),
            ];

            RunDiagnostics(rules, ImmutableList<PrecedenceRule>.Empty);
        }

        [TestMethod]
        public void TestPrecedence()
        {
            ImmutableList<Rule> rules =
            [
                MakeStartRule("expr"),
                MakeRule("expr", "var"),
                MakeRule("expr", "expr + expr"),
                MakeRule("expr", "expr - expr"),
                MakeRule("expr", "expr * expr"),
                MakeRule("expr", "expr to-the expr"),
                MakeRule("expr", "( expr )"),
            ];

            ImmutableList<PrecedenceRule> precedenceRules =
            [
                new PrecedenceRule(Symbol.EmptySet.Add(new NamedSymbol("to-the")), Associativity.RightToLeft),
                new PrecedenceRule(Symbol.EmptySet.Add(new NamedSymbol("*")), Associativity.LeftToRight),
                new PrecedenceRule(Symbol.EmptySet.Add(new NamedSymbol("+")).Add(new NamedSymbol("-")), Associativity.LeftToRight),
            ];

            RunDiagnostics(rules, precedenceRules);
        }

        [TestMethod]
        public void TestLR1()
        {
            ImmutableList<Rule> rules =
            [
                MakeStartRule("S"),
                MakeRule("S", "a A d"),
                MakeRule("S", "b B d"),
                MakeRule("S", "a B e"),
                MakeRule("S", "b A e"),
                MakeRule("A", "c"),
                MakeRule("B", "c"),
            ];

            RunDiagnostics(rules, ImmutableList<PrecedenceRule>.Empty);
        }

        private static readonly Regex rWhiteSpace = new Regex("\\s+", RegexOptions.None);

        private static Rule MakeStartRule(string str)
        {
            return new Rule(StartSymbol.Value, [new NamedSymbol(str)]);
        }

        private static Rule MakeRule(string lhs, string rhs)
        {
            IEnumerable<string> rhsNames = rWhiteSpace.Split(rhs).Where(s => s.Length > 0);

            return new Rule(new NamedSymbol(lhs), rhsNames.Select(n => new NamedSymbol(n)).Cast<Symbol>().ToImmutableList());
        }
    }
}