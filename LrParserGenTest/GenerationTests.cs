using Sunlighter.LrParserGenLib;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Sunlighter.LrParserGenLib.TypeTraits;
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
                Debug.WriteLine("  " + i + ": " + Rule.Traits.ItemToString(rules[i]));
            }

            Grammar g = new Grammar(rules, precedenceRules);

            Debug.WriteLine("Nonterminals:");
            Debug.WriteLine(Grammar.SymbolSetTypeTraits.ItemToString(g.Nonterminals));

            Debug.WriteLine("Grammar Symbols:");
            Debug.WriteLine(Grammar.SymbolSetTypeTraits.ItemToString(g.GrammarSymbols));

            Debug.WriteLine("Terminals:");
            Debug.WriteLine(Grammar.SymbolSetTypeTraits.ItemToString(g.Terminals));

            Assert.IsTrue(g.Terminals.Count > 0);

            Debug.WriteLine("Nullable:");
            Debug.WriteLine(Grammar.SymbolSetTypeTraits.ItemToString(g.NullableSet));

            Debug.WriteLine("Interminable:");
            Debug.WriteLine(Grammar.SymbolSetTypeTraits.ItemToString(g.GrammarSymbols.Except(g.TerminableSet)));

            Debug.WriteLine("Unreachable:");
            Debug.WriteLine(Grammar.SymbolSetTypeTraits.ItemToString(g.GrammarSymbols.Except(g.ReachableSet)));

            Debug.WriteLine("First Table");
            Debug.WriteLine(Grammar.FirstTableTypeTraits.ItemToString(g.FirstTable));

            Debug.WriteLine("Follow Table");
            Debug.WriteLine(Grammar.FirstTableTypeTraits.ItemToString(g.FollowTable));

            Debug.WriteLine("Initial State Set");
            Debug.WriteLine(ItemSet.TypeTraits.ItemToString(g.InitialStateSet));

            ITypeTraits<ParseAction<ItemSet>> itemSetParseActionCompareWorker = ParseAction<ItemSet>.GetTypeTraits(ItemSet.TypeTraits);

            Debug.WriteLine("Parse action on seeing \"EOF\"");
            Debug.WriteLine(itemSetParseActionCompareWorker.ItemToString(g.GetParseAction(g.InitialStateSet, EofSymbol.Value)));

            if (g.GrammarSymbols.Contains(new NamedSymbol("begin")))
            {
                Debug.WriteLine("Parse action on seeing \"begin\"");
                Debug.WriteLine(itemSetParseActionCompareWorker.ItemToString(g.GetParseAction(g.InitialStateSet, new NamedSymbol("begin"))));
            }

            Debug.WriteLine("Item Set Parse Table");

            foreach (KeyValuePair<ItemSet, ImmutableSortedDictionary<Symbol, ParseAction<ItemSet>>> kvp in g.ItemSetParseTable)
            {
                Debug.WriteLine("  " + ItemSet.TypeTraits.ItemToString(kvp.Key));
                foreach (KeyValuePair<Symbol, ParseAction<ItemSet>> kvp2 in kvp.Value)
                {
                    Debug.WriteLine("    " + Symbol.Traits.ItemToString(kvp2.Key) + " " + itemSetParseActionCompareWorker.ItemToString(kvp2.Value));
                }
            }

            ITypeTraits<ParseAction<int>> intParseActionCompareWorker = ParseAction<int>.GetTypeTraits(Int32TypeTraits.Value);

            Debug.WriteLine("Int Parse Table");

            foreach (int i in Enumerable.Range(0, g.IntParseTableData.ParseTable.Count))
            {
                Debug.WriteLine("  " + i);
                foreach (KeyValuePair<Symbol, ParseAction<int>> kvp2 in g.IntParseTableData.ParseTable[i])
                {
                    Debug.WriteLine("    " + Symbol.Traits.ItemToString(kvp2.Key) + " " + intParseActionCompareWorker.ItemToString(kvp2.Value));
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