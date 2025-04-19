using Sunlighter.TypeTraitsLib;

namespace Sunlighter.LrParserGenLib
{
    public sealed class Rule(Symbol lhs, ImmutableList<Symbol> rhs)
    {
        public Symbol LHS => lhs;

        public ImmutableList<Symbol> RHS => rhs;

        private static readonly Lazy<ITypeTraits<Rule>> typeTraits = new Lazy<ITypeTraits<Rule>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ITypeTraits<Rule> GetTypeTraits()
        {
            return new ConvertTypeTraits<Rule, Tuple<Symbol, ImmutableList<Symbol>>>
            (
                r => new Tuple<Symbol, ImmutableList<Symbol>>(r.LHS, r.RHS),
                new TupleTypeTraits<Symbol, ImmutableList<Symbol>>(Symbol.Traits, new ListTypeTraits<Symbol>(Symbol.Traits)),
                t => new Rule(t.Item1, t.Item2)
            );
        }

        public static ITypeTraits<Rule> Traits => typeTraits.Value;

        public override bool Equals(object? obj)
        {
            if (obj is Rule r) return typeTraits.Value.Compare(this, r) == 0;
            else return false;
        }

        public override int GetHashCode() => typeTraits.Value.GetBasicHashCode(this);

        public override string ToString() => typeTraits.Value.ToDebugString(this);
    }

    public sealed class Item(int ruleNumber, int positionOfDot, Symbol follow)
    {
        private readonly int ruleNumber = ruleNumber;
        private readonly int positionOfDot = positionOfDot;
        private readonly Symbol follow = follow;

        public int RuleNumber => ruleNumber;

        public int PositionOfDot => positionOfDot;

        public Symbol Follow => follow;

        private static ITypeTraits<Item> GetTypeTraits()
        {
            return new ConvertTypeTraits<Item, Tuple<int, int, Symbol>>
            (
                i => new Tuple<int, int, Symbol>(i.ruleNumber, i.positionOfDot, i.follow),
                new TupleTypeTraits<int, int, Symbol>
                (
                    Int32TypeTraits.Value,
                    Int32TypeTraits.Value,
                    Symbol.Traits
                ),
                i => new Item(i.Item1, i.Item2, i.Item3)
            );
        }

        private static readonly Lazy<ITypeTraits<Item>> typeTraits = new Lazy<ITypeTraits<Item>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        public static ITypeTraits<Item> TypeTraits => typeTraits.Value;

        private static readonly Lazy<Adapter<Item>> adapter = new Lazy<Adapter<Item>>(() => Adapter<Item>.Create(typeTraits.Value), LazyThreadSafetyMode.ExecutionAndPublication);

        public static Adapter<Item> Adapter => adapter.Value;

        private static readonly Lazy<ImmutableSortedSet<Item>> emptySet =
            new Lazy<ImmutableSortedSet<Item>>(() => ImmutableSortedSet<Item>.Empty.WithComparer(adapter.Value), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ImmutableSortedSet<Item> EmptySet => emptySet.Value;

        public override bool Equals(object? obj)
        {
            if (obj is Item i) return typeTraits.Value.Compare(this, i) == 0;
            else return false;
        }

        public override int GetHashCode() => typeTraits.Value.GetBasicHashCode(this);

        public override string ToString() => typeTraits.Value.ToDebugString(this);
    }

    public sealed class ItemSet(ImmutableSortedSet<Item> items)
    {
        private readonly ImmutableSortedSet<Item> items = items;

        public ImmutableSortedSet<Item> Items => items;

        private static ITypeTraits<ItemSet> GetTypeTraits()
        {
            return new ConvertTypeTraits<ItemSet, ImmutableSortedSet<Item>>
            (
                itemSet => itemSet.items,
                new SetTypeTraits<Item>(Item.TypeTraits),
                i => new ItemSet(i)
            );
        }

        private static readonly Lazy<ITypeTraits<ItemSet>> typeTraits = new Lazy<ITypeTraits<ItemSet>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        public static ITypeTraits<ItemSet> TypeTraits => typeTraits.Value;

        private static readonly Lazy<Adapter<ItemSet>> adapter = new Lazy<Adapter<ItemSet>>(() => Adapter<ItemSet>.Create(typeTraits.Value), LazyThreadSafetyMode.ExecutionAndPublication);

        public static Adapter<ItemSet> Adapter => adapter.Value;

        private static readonly Lazy<ImmutableSortedSet<ItemSet>> emptySet =
            new Lazy<ImmutableSortedSet<ItemSet>>(() => ImmutableSortedSet<ItemSet>.Empty.WithComparer(adapter.Value), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ImmutableSortedSet<ItemSet> EmptySet => emptySet.Value;

        public override string ToString()
        {
            return typeTraits.Value.ToDebugString(this);
        }
    }

    public abstract class ParseAction<S>
    {
        public static ITypeTraits<ParseAction<S>> GetTypeTraits(ITypeTraits<S> stateTypeTraits)
        {
            RecursiveTypeTraits<ParseAction<S>> recurse = new RecursiveTypeTraits<ParseAction<S>>();

            ITypeTraits<ParseAction<S>> cw = new UnionTypeTraits<string, ParseAction<S>>
            (
                StringTypeTraits.Value,
                ImmutableList<IUnionCaseTypeTraits<string, ParseAction<S>>>.Empty.AddRange
                (
                    new IUnionCaseTypeTraits<string, ParseAction<S>>[]
                    {
                        new UnionCaseTypeTraits2<string, ParseAction<S>, ParseAction_Shift<S>>
                        (
                            "shift",
                            new ConvertTypeTraits<ParseAction_Shift<S>, S>
                            (
                                p => p.State,
                                stateTypeTraits,
                                s => new ParseAction_Shift<S>(s)
                            )
                        ),
                        new UnionCaseTypeTraits2<string, ParseAction<S>, ParseAction_ReduceByRule<S>>
                        (
                            "reduce-by-rule",
                            new ConvertTypeTraits<ParseAction_ReduceByRule<S>, int>
                            (
                                p => p.RuleNumber,
                                Int32TypeTraits.Value,
                                r => new ParseAction_ReduceByRule<S>(r)
                            )
                        ),
                        new UnionCaseTypeTraits2<string, ParseAction<S>, ParseAction_Conflict<S>>
                        (
                            "conflict",
                            new ConvertTypeTraits<ParseAction_Conflict<S>, ImmutableList<ParseAction<S>>>
                            (
                                p => p.ParseActions,
                                new ListTypeTraits<ParseAction<S>>(recurse),
                                ls => new ParseAction_Conflict<S>(ls)
                            )
                        ),
                        new UnionCaseTypeTraits2<string, ParseAction<S>, ParseAction_Error<S>>
                        (
                            "error",
                            new ConvertTypeTraits<ParseAction_Error<S>, DBNull>
                            (
                                e => DBNull.Value,
                                new UnitTypeTraits<DBNull>(HashToken.None, DBNull.Value),
                                d => ParseAction_Error<S>.Value
                            )
                        ),
                    }
                )
            );

            recurse.Set(cw);

            return cw;
        }

        public abstract ParseAction<S2> Convert<S2>(Func<S, S2> func);

        public virtual ImmutableSortedSet<S> AddReferencedStates(ImmutableSortedSet<S> set)
        {
            return set;
        }
    }

    public sealed class ParseAction_Shift<S> : ParseAction<S>
    {
        private readonly S state;

        public ParseAction_Shift(S state)
        {
            this.state = state;
        }

        public S State => state;

        public override ParseAction<S2> Convert<S2>(Func<S, S2> func)
        {
            return new ParseAction_Shift<S2>(func(state));
        }

        public override ImmutableSortedSet<S> AddReferencedStates(ImmutableSortedSet<S> set)
        {
            return set.Add(state);
        }
    }

    public sealed class ParseAction_ReduceByRule<S> : ParseAction<S>
    {
        private readonly int ruleNumber;

        public ParseAction_ReduceByRule(int ruleNumber)
        {
            this.ruleNumber = ruleNumber;
        }

        public int RuleNumber => ruleNumber;

        public override ParseAction<S2> Convert<S2>(Func<S, S2> func)
        {
            return new ParseAction_ReduceByRule<S2>(ruleNumber);
        }
    }

    public sealed class ParseAction_Conflict<S> : ParseAction<S>
    {
        private readonly ImmutableList<ParseAction<S>> parseActions;

        public ParseAction_Conflict(ImmutableList<ParseAction<S>> parseActions)
        {
            this.parseActions = parseActions;
        }

        public ImmutableList<ParseAction<S>> ParseActions => parseActions;

        public override ParseAction<S2> Convert<S2>(Func<S, S2> func)
        {
            return new ParseAction_Conflict<S2>(parseActions.Select(p => p.Convert(func)).ToImmutableList());
        }

        public override ImmutableSortedSet<S> AddReferencedStates(ImmutableSortedSet<S> set)
        {
            foreach(ParseAction<S> innerAction in parseActions)
            {
                set = innerAction.AddReferencedStates(set);
            }
            return set;
        }
    }

    public sealed class ParseAction_Error<S> : ParseAction<S>
    {
        private static readonly ParseAction_Error<S> value = new ParseAction_Error<S>();

        private ParseAction_Error() { }

        public static ParseAction_Error<S> Value => value;

        public override ParseAction<S2> Convert<S2>(Func<S, S2> func)
        {
            return ParseAction_Error<S2>.Value;
        }
    }

    public enum Associativity
    {
        LeftToRight,
        RightToLeft,
        NonAssociative
    }

    public sealed class PrecedenceRule(ImmutableSortedSet<Symbol> symbols, Associativity associativity)
    {
        public ImmutableSortedSet<Symbol> Symbols => symbols;
        public Associativity Associativity => associativity;

        private static Lazy<ITypeTraits<PrecedenceRule>> typeTraits =
            new Lazy<ITypeTraits<PrecedenceRule>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ITypeTraits<PrecedenceRule> GetTypeTraits()
        {
            return new ConvertTypeTraits<PrecedenceRule, (ImmutableSortedSet<Symbol>, Associativity)>
            (
                r => (r.Symbols, r.Associativity),
                new ValueTupleTypeTraits<ImmutableSortedSet<Symbol>, Associativity>
                (
                    Grammar.SymbolSetTypeTraits,
                    new ConvertTypeTraits<Associativity, int>
                    (
                        a => (int)a,
                        Int32TypeTraits.Value,
                        i => (Associativity)i
                    )
                ),
                t => new PrecedenceRule(t.Item1, t.Item2)
            );
        }

        public static ITypeTraits<PrecedenceRule> Traits => typeTraits.Value;
    }

    public sealed class Grammar
    {
        private readonly ImmutableList<Rule> rules;
        private readonly ImmutableList<PrecedenceRule> precedenceRules;

        private readonly Lazy<ImmutableSortedSet<Symbol>> nonterminals;
        private readonly Lazy<ImmutableSortedSet<Symbol>> grammarSymbols;
        private readonly Lazy<ImmutableSortedSet<Symbol>> terminals;

        private readonly Lazy<ImmutableSortedSet<Symbol>> nullableSet;
        private readonly Lazy<ImmutableSortedSet<Symbol>> terminableSet;
        private readonly Lazy<ImmutableSortedSet<Symbol>> reachableSet;

        private readonly Lazy<ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>>> firstTable;
        private readonly Lazy<ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>>> followTable;

        private readonly Lazy<ItemSet> initialStateSet;
        private readonly Lazy<ImmutableSortedDictionary<ItemSet, ImmutableSortedDictionary<Symbol, ParseAction<ItemSet>>>> itemSetParseTable;
        private readonly Lazy<IntParseTableData> intParseTableData;

        /// <summary>
        /// Makes a grammar from the augmented list of rules
        /// </summary>
        public Grammar(ImmutableList<Rule> rules, ImmutableList<PrecedenceRule> precedenceRules)
        {
            this.rules = rules;
            this.precedenceRules = precedenceRules;

            nonterminals = new Lazy<ImmutableSortedSet<Symbol>>(GetNonterminals, LazyThreadSafetyMode.ExecutionAndPublication);
            grammarSymbols = new Lazy<ImmutableSortedSet<Symbol>>(GetGrammarSymbols, LazyThreadSafetyMode.ExecutionAndPublication);
            terminals = new Lazy<ImmutableSortedSet<Symbol>>(GetTerminals, LazyThreadSafetyMode.ExecutionAndPublication);

            nullableSet = new Lazy<ImmutableSortedSet<Symbol>>(GetNullableSet, LazyThreadSafetyMode.ExecutionAndPublication);
            terminableSet = new Lazy<ImmutableSortedSet<Symbol>>(GetTerminableSet, LazyThreadSafetyMode.ExecutionAndPublication);
            reachableSet = new Lazy<ImmutableSortedSet<Symbol>>(GetReachableSet, LazyThreadSafetyMode.ExecutionAndPublication);

            firstTable = new Lazy<ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>>>(GetFirstTable, LazyThreadSafetyMode.ExecutionAndPublication);
            followTable = new Lazy<ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>>>(GetFollowTable, LazyThreadSafetyMode.ExecutionAndPublication);

            initialStateSet = new Lazy<ItemSet>(GetInitialStateSet, LazyThreadSafetyMode.ExecutionAndPublication);
            itemSetParseTable = new Lazy<ImmutableSortedDictionary<ItemSet, ImmutableSortedDictionary<Symbol, ParseAction<ItemSet>>>>(GetItemSetParseTable, LazyThreadSafetyMode.ExecutionAndPublication);
            intParseTableData = new Lazy<IntParseTableData>(GetIntParseTableData, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public ImmutableList<Rule> Rules => rules;

        private ImmutableSortedSet<Symbol> GetNonterminals()
        {
            return Symbol.EmptySet.Union(rules.Select(r => r.LHS));
        }

        public ImmutableSortedSet<Symbol> Nonterminals => nonterminals.Value;

        private ImmutableSortedSet<Symbol> GetGrammarSymbols()
        {
            ImmutableSortedSet<Symbol>.Builder b = Symbol.EmptySet.ToBuilder();
            foreach (Rule r in rules)
            {
                b.Add(r.LHS);
                b.UnionWith(r.RHS);
            }
            return b.ToImmutable();
        }

        public ImmutableSortedSet<Symbol> GrammarSymbols => grammarSymbols.Value;

        private ImmutableSortedSet<Symbol> GetTerminals()
        {
            return grammarSymbols.Value.Except(nonterminals.Value);
        }

        public ImmutableSortedSet<Symbol> Terminals => terminals.Value;

        private static readonly Lazy<ITypeTraits<ImmutableSortedSet<Symbol>>> symbolSetTypeTraits =
            new Lazy<ITypeTraits<ImmutableSortedSet<Symbol>>>(() => new SetTypeTraits<Symbol>(Symbol.Traits), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ITypeTraits<ImmutableSortedSet<Symbol>> SymbolSetTypeTraits => symbolSetTypeTraits.Value;

        private static readonly Lazy<Adapter<ImmutableSortedSet<Symbol>>> symbolSetAdapter =
            new Lazy<Adapter<ImmutableSortedSet<Symbol>>>
            (
                () => Adapter<ImmutableSortedSet<Symbol>>.Create(symbolSetTypeTraits.Value),
                LazyThreadSafetyMode.ExecutionAndPublication
            );

        public static Adapter<ImmutableSortedSet<Symbol>> SymbolSetAdapter => symbolSetAdapter.Value;

        private ImmutableSortedSet<Symbol> GetNullableSet()
        {
            ImmutableSortedSet<Symbol> current = Symbol.EmptySet.Union(rules.Where(r => r.RHS.IsEmpty).Select(r => r.LHS));

            while (true)
            {
                ImmutableSortedSet<Symbol> next = Symbol.EmptySet.Union(rules.Where(r => r.RHS.All(rs => current.Contains(rs))).Select(r => r.LHS));
                if (symbolSetTypeTraits.Value.Compare(current, next) == 0) break;
                current = next;
            }

            return current;
        }

        public ImmutableSortedSet<Symbol> NullableSet => nullableSet.Value;

        private ImmutableSortedSet<Symbol> GetTerminableSet()
        {
            ImmutableSortedSet<Symbol> current = terminals.Value; // all terminals are terminable

            while (true)
            {
                ImmutableSortedSet<Symbol> next = current.Union(rules.Where(r => r.RHS.All(rs => current.Contains(rs))).Select(r => r.LHS));
                if (symbolSetTypeTraits.Value.Compare(current, next) == 0) break;
                current = next;
            }

            return current;
        }

        public ImmutableSortedSet<Symbol> TerminableSet => terminableSet.Value;

        private ImmutableSortedSet<Symbol> GetReachableSet()
        {
            ImmutableSortedSet<Symbol> current = Symbol.EmptySet.Add(StartSymbol.Value);

            ImmutableList<Symbol> getSuccessors(Symbol s)
            {
                if (terminals.Value.Contains(s))
                {
                    return ImmutableList<Symbol>.Empty.Add(s);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(nonterminals.Value.Contains(s));
                    ImmutableSortedSet<Symbol> successors = Symbol.EmptySet;
                    foreach(Rule r in rules.Where(r1 => Symbol.Traits.Compare(r1.LHS, s) == 0))
                    {
                        successors = successors.Union(r.RHS);
                    }
                    return successors.ToImmutableList();
                }
            }

            return Closure(current, getSuccessors);
        }

        public ImmutableSortedSet<Symbol> ReachableSet => reachableSet.Value;

        private static readonly Lazy<ITypeTraits<ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>>>> firstTableTypeTraits =
            new Lazy<ITypeTraits<ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>>>>
            (
                () => new DictionaryTypeTraits<Symbol, ImmutableSortedSet<Symbol>>(Symbol.Traits, symbolSetTypeTraits.Value),
                LazyThreadSafetyMode.ExecutionAndPublication
            );

        public static ITypeTraits<ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>>> FirstTableTypeTraits => firstTableTypeTraits.Value;

        private static ImmutableSortedSet<Symbol> FirstOfStringByTable(ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>> table, ImmutableList<Symbol> symbolString)
        {
            ImmutableSortedSet<Symbol> result = Symbol.EmptySet.Add(EpsilonSymbol.Value);

            while (true)
            {
                if (symbolString.IsEmpty) break;
                if (!result.Contains(EpsilonSymbol.Value)) break;
                if (table.TryGetValue(symbolString[0], out ImmutableSortedSet<Symbol>? firstFromTable))
                {
                    result = result.Remove(EpsilonSymbol.Value).Union(firstFromTable);
                }
                else
                {
                    throw new Exception($"Symbol {Symbol.Traits.ToDebugString(symbolString[0])} not in table");
                }
                symbolString = symbolString.RemoveAt(0);
            }

            return result;
        }

        private static readonly Lazy<ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>>> emptyDictionary =
            new Lazy<ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>>>
            (
                () => ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>>.Empty.WithComparers(Symbol.Adapter, symbolSetAdapter.Value),
                LazyThreadSafetyMode.ExecutionAndPublication
            );

        private ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>> GetFirstTable()
        {
            ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>> terminalsOnly = emptyDictionary.Value;

            foreach (Symbol t in terminals.Value)
            {
                terminalsOnly = terminalsOnly.Add(t, Symbol.EmptySet.Add(t));
            }

            ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>> current = terminalsOnly;

            foreach (Symbol nt in nonterminals.Value)
            {
                current = current.Add(nt, Symbol.EmptySet);
            }

            while (true)
            {
                ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>> next = terminalsOnly;

                foreach (Symbol nt in nonterminals.Value)
                {
                    ImmutableSortedSet<Symbol> result = Symbol.EmptySet;
                    foreach (Rule r in rules.Where(r => Symbol.Traits.Compare(r.LHS, nt) == 0))
                    {
                        result = result.Union(FirstOfStringByTable(current, r.RHS));
                    }
                    next = next.Add(nt, result);
                }

                if (firstTableTypeTraits.Value.Compare(current, next) == 0) break;

                current = next;
            }

            return current;
        }

        public ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>> FirstTable => firstTable.Value;

        public ImmutableSortedSet<Symbol> First(Symbol symbol) => firstTable.Value[symbol];

        public ImmutableSortedSet<Symbol> First(ImmutableList<Symbol> symbolString) => FirstOfStringByTable(firstTable.Value, symbolString);

        private sealed class FoundPosition
        {
            private readonly int ruleNumber;
            private readonly int positionInRule;

            public FoundPosition(int ruleNumber, int positionInRule)
            {
                this.ruleNumber = ruleNumber;
                this.positionInRule = positionInRule;
            }

            public int RuleNumber => ruleNumber;

            public int PositionInRule => positionInRule;
        }

        private IEnumerable<FoundPosition> FindAllInstances(Symbol target)
        {
            foreach (int ruleNumber in Enumerable.Range(0, rules.Count))
            {
                Rule r = rules[ruleNumber];
                foreach (int position in Enumerable.Range(0, r.RHS.Count))
                {
                    if (Symbol.Traits.Compare(r.RHS[position], target) == 0)
                    {
                        yield return new FoundPosition(ruleNumber, position);
                    }
                }
            }
        }

        private ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>> GetFollowTable()
        {
            ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>> followTable = emptyDictionary.Value;
            ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>> followDependencies = emptyDictionary.Value;

            foreach (Symbol gs in grammarSymbols.Value)
            {
                foreach (FoundPosition foundPosition in FindAllInstances(gs))
                {
                    Rule r = rules[foundPosition.RuleNumber];
                    ImmutableList<Symbol> tail = r.RHS.RemoveRange(0, foundPosition.PositionInRule + 1);
                    ImmutableSortedSet<Symbol> first = First(tail);
                    if (first.Contains(EpsilonSymbol.Value))
                    {
                        followDependencies = followDependencies.SetItem(gs, followDependencies.GetValueOrDefault(gs, Symbol.EmptySet).Add(r.LHS));
                        followTable = followTable.SetItem(gs, followTable.GetValueOrDefault(gs, Symbol.EmptySet).Union(first.Remove(EpsilonSymbol.Value)));
                    }
                    else
                    {
                        followTable = followTable.SetItem(gs, followTable.GetValueOrDefault(gs, Symbol.EmptySet).Union(first));
                    }
                }
            }

            followTable = followTable.SetItem(StartSymbol.Value, followTable.GetValueOrDefault(StartSymbol.Value, Symbol.EmptySet).Add(EofSymbol.Value));

            while (true)
            {
                ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>> newTable = followTable;
                foreach (Symbol nt in grammarSymbols.Value)
                {
                    ImmutableSortedSet<Symbol> newFollow = followTable[nt];
                    if (followDependencies.TryGetValue(nt, out ImmutableSortedSet<Symbol>? deps))
                    {
                        foreach (Symbol dep in deps)
                        {
                            newFollow = newFollow.Union(followTable[dep]);
                        }
                        newTable = newTable.SetItem(nt, newFollow);
                    }
                }

                if (firstTableTypeTraits.Value.Compare(followTable, newTable) == 0) break;

                followTable = newTable;
            }

            return followTable;
        }

        public ImmutableSortedDictionary<Symbol, ImmutableSortedSet<Symbol>> FollowTable => followTable.Value;

        public ImmutableSortedSet<Symbol> Follow(Symbol s) => followTable.Value.GetValueOrDefault(s, Symbol.EmptySet);

        public ImmutableSortedSet<Symbol> FirstWithFollow(ImmutableList<Symbol> symbolString, Symbol inheritFollow)
        {
            ImmutableSortedSet<Symbol> firstOfSymbolString = First(symbolString);
            if (firstOfSymbolString.Contains(EpsilonSymbol.Value))
            {
                return firstOfSymbolString.Remove(EpsilonSymbol.Value).Union(Follow(inheritFollow));
            }
            else
            {
                return firstOfSymbolString;
            }
        }

        public static ImmutableSortedSet<T> Closure<T>(ImmutableSortedSet<T> initialItems, Func<T, ImmutableList<T>> getSuccessors)
        {
            ImmutableSortedSet<T> result = initialItems;
            ImmutableList<T> todoQueue = ImmutableList<T>.Empty.AddRange(initialItems);
            while (!todoQueue.IsEmpty)
            {
                T item = todoQueue[0];
                todoQueue = todoQueue.RemoveAt(0);

                ImmutableList<T> successors = getSuccessors(item);

                foreach (T successor in successors)
                {
                    if (!result.Contains(successor))
                    {
                        result = result.Add(successor);
                        todoQueue = todoQueue.Add(successor);
                    }
                }
            }
            return result;
        }

        private ImmutableSortedSet<Item> ItemSetClosure(ImmutableSortedSet<Item> itemSet)
        {
            ImmutableList<Item> getSuccessors(Item i)
            {
                ImmutableList<Item> result = ImmutableList<Item>.Empty;
                Rule r = rules[i.RuleNumber];
                if (i.PositionOfDot < r.RHS.Count)
                {
                    Symbol nextGrammarSymbol = r.RHS[i.PositionOfDot];

                    if (nonterminals.Value.Contains(nextGrammarSymbol))
                    {
                        ImmutableList<Symbol> afterNextGrammarSymbol = r.RHS.RemoveRange(0, i.PositionOfDot + 1);
                        ImmutableSortedSet<Symbol> possibleFollows = First(afterNextGrammarSymbol);
                        if (possibleFollows.Contains(EpsilonSymbol.Value))
                        {
                            possibleFollows = possibleFollows.Remove(EpsilonSymbol.Value).Add(i.Follow);
                        }

                        foreach (int r2Index in Enumerable.Range(0, rules.Count).Where(i2 => Symbol.Traits.Compare(rules[i2].LHS, nextGrammarSymbol) == 0))
                        {
                            foreach (Symbol possibleFollow in possibleFollows)
                            {
                                result = result.Add(new Item(r2Index, 0, possibleFollow));
                            }
                        }
                    }
                }
                return result;
            }

            return Closure(itemSet, getSuccessors);
        }

        private ItemSet GetInitialStateSet()
        {
            return new ItemSet(ItemSetClosure(ImmutableSortedSet<Item>.Empty.WithComparer(Item.Adapter).Add(new Item(0, 0, EofSymbol.Value))));
        }

        public ItemSet InitialStateSet => initialStateSet.Value;

        private ParseAction<ItemSet> GetParseAction_NoPrecedence(ItemSet state, Symbol lookAhead)
        {
            ImmutableSortedSet<Item> shiftTargets = Item.EmptySet;
            ImmutableList<ParseAction<ItemSet>> actions = ImmutableList<ParseAction<ItemSet>>.Empty;

            foreach (Item i in state.Items)
            {
                Rule r = rules[i.RuleNumber];

                if (i.PositionOfDot < r.RHS.Count)
                {
                    Symbol nextInRule = r.RHS[i.PositionOfDot];
                    if (Symbol.Traits.Compare(lookAhead, nextInRule) == 0)
                    {
                        shiftTargets = shiftTargets.Add(new Item(i.RuleNumber, i.PositionOfDot + 1, i.Follow));
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(i.PositionOfDot == r.RHS.Count);

                    if (Symbol.Traits.Compare(lookAhead, i.Follow) == 0)
                    {
                        actions = actions.Add(new ParseAction_ReduceByRule<ItemSet>(i.RuleNumber));
                    }
                }
            }

            if (shiftTargets.Count > 0)
            {
                actions = actions.Add(new ParseAction_Shift<ItemSet>(new ItemSet(ItemSetClosure(shiftTargets))));
            }

            if (actions.Count == 0)
            {
                return ParseAction_Error<ItemSet>.Value;
            }
            else if (actions.Count == 1)
            {
                return actions[0];
            }
            else
            {
                return new ParseAction_Conflict<ItemSet>(actions);
            }
        }

        public ParseAction<ItemSet> GetParseAction(ItemSet state, Symbol lookAhead)
        {
            ParseAction<ItemSet> noPrecedenceAction = GetParseAction_NoPrecedence(state, lookAhead);

            if (noPrecedenceAction is ParseAction_Conflict<ItemSet> conflict)
            {
                ImmutableList<ParseAction_Shift<ItemSet>> shifts = conflict.ParseActions.OfType<ParseAction_Shift<ItemSet>>().ToImmutableList();

                ImmutableList<ParseAction_ReduceByRule<ItemSet>> reductions = conflict.ParseActions.OfType<ParseAction_ReduceByRule<ItemSet>>().ToImmutableList();

                // try to resolve reduce-reduce conflicts by favoring the longest reduction

                if (reductions.Count > 1)
                {
                    int maxRuleLength = reductions.Select(reduction => rules[reduction.RuleNumber].RHS.Count).Max();
                    reductions = reductions.Where(reduction => rules[reduction.RuleNumber].RHS.Count == maxRuleLength).ToImmutableList();
                }

                // try to resolve shift-reduce conflicts using precedence rules

                if (shifts.Count == 1 && reductions.Count == 1)
                {
                    Rule reductionRule = rules[reductions[0].RuleNumber];

                    Symbol? leftSymbol = Enumerable.Range(0, reductionRule.RHS.Count).Select(i => reductionRule.RHS[reductionRule.RHS.Count - i - 1]).Where(terminals.Value.Contains).FirstOrDefault();

                    int? PrecedenceRuleForSymbol(Symbol s) => Enumerable.Range(0, precedenceRules.Count).Where(i => precedenceRules[i].Symbols.Contains(s)).Cast<int?>().FirstOrDefault();

                    int? leftPrecedence = (leftSymbol is not null) ? PrecedenceRuleForSymbol(leftSymbol) : null;
                    int? rightPrecedence = PrecedenceRuleForSymbol(lookAhead);

                    if (leftPrecedence.HasValue && rightPrecedence.HasValue)
                    {
                        if (leftPrecedence.Value < rightPrecedence.Value)
                        {
                            return reductions[0];
                        }
                        else if (leftPrecedence.Value > rightPrecedence.Value)
                        {
                            return shifts[0];
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(leftPrecedence.Value == rightPrecedence.Value);
                            PrecedenceRule pRule = precedenceRules[leftPrecedence.Value];
                            if (pRule.Associativity == Associativity.LeftToRight)
                            {
                                return reductions[0];
                            }
                            else if (pRule.Associativity == Associativity.RightToLeft)
                            {
                                return shifts[0];
                            }
                            // else do nothing
                        }
                    }
                }

                ImmutableList<ParseAction<ItemSet>> resolvedParseActions = ImmutableList<ParseAction<ItemSet>>.Empty.AddRange(shifts.Cast<ParseAction<ItemSet>>()).AddRange(reductions.Cast<ParseAction<ItemSet>>());

                if (resolvedParseActions.Count == 1)
                {
                    return resolvedParseActions[0];
                }
                else
                {
                    return new ParseAction_Conflict<ItemSet>(resolvedParseActions);
                }
            }

            return noPrecedenceAction;
        }

        public static ITypeTraits<ImmutableSortedDictionary<S, ImmutableSortedDictionary<Symbol, ParseAction<S>>>> GetParseTableTypeTraits<S>(ITypeTraits<S> stateTraits)
            where S : notnull
        {
            return new DictionaryTypeTraits<S, ImmutableSortedDictionary<Symbol, ParseAction<S>>>
            (
                stateTraits,
                new DictionaryTypeTraits<Symbol, ParseAction<S>>
                (
                    Symbol.Traits,
                    ParseAction<S>.GetTypeTraits(stateTraits)
                )
            );
        }

        private ImmutableSortedDictionary<ItemSet, ImmutableSortedDictionary<Symbol, ParseAction<ItemSet>>> GetItemSetParseTable()
        {
            ImmutableSortedDictionary<ItemSet, ImmutableSortedDictionary<Symbol, ParseAction<ItemSet>>> result =
                ImmutableSortedDictionary<ItemSet, ImmutableSortedDictionary<Symbol, ParseAction<ItemSet>>>.Empty.WithComparers(ItemSet.Adapter);

            ImmutableList<ItemSet> unexplored = ImmutableList<ItemSet>.Empty.Add(initialStateSet.Value);

            ImmutableSortedSet<Symbol> grammarSymbolsPlusEof = grammarSymbols.Value.Add(EofSymbol.Value);

            while(!unexplored.IsEmpty)
            {
                ItemSet itemSet = unexplored[0];
                unexplored = unexplored.RemoveAt(0);

                if (!result.ContainsKey(itemSet))
                {
                    ImmutableSortedDictionary<Symbol, ParseAction<ItemSet>> actions =
                        ImmutableSortedDictionary<Symbol, ParseAction<ItemSet>>.Empty.WithComparers(Symbol.Adapter);

                    ImmutableSortedSet<ItemSet> referencedStates = ItemSet.EmptySet;

                    foreach (Symbol s in grammarSymbolsPlusEof)
                    {
                        ParseAction<ItemSet> p = GetParseAction(itemSet, s);

                        if (p is not ParseAction_Error<ItemSet>)
                        {
                            actions = actions.Add(s, p);
                            referencedStates = p.AddReferencedStates(referencedStates);
                        }
                    }

                    referencedStates = referencedStates.Except(result.Keys).Remove(itemSet);

                    unexplored = unexplored.AddRange(referencedStates);

                    result = result.Add(itemSet, actions);
                }
            }

            return result;
        }

        public ImmutableSortedDictionary<ItemSet, ImmutableSortedDictionary<Symbol, ParseAction<ItemSet>>> ItemSetParseTable => itemSetParseTable.Value;

        private IntParseTableData GetIntParseTableData()
        {
            ImmutableSortedDictionary<ItemSet, ImmutableSortedDictionary<Symbol, ParseAction<ItemSet>>> oldParseTable = itemSetParseTable.Value;

            ImmutableSortedDictionary<ItemSet, int> conversionDict = ImmutableSortedDictionary<ItemSet, int>.Empty.WithComparers(ItemSet.Adapter);

            int i = 0;
            foreach(ItemSet itemSet in oldParseTable.Keys)
            {
                conversionDict = conversionDict.Add(itemSet, i);
                ++i;
            }

            ImmutableList<ImmutableSortedDictionary<Symbol, ParseAction<int>>>.Builder newParseTable = ImmutableList<ImmutableSortedDictionary<Symbol, ParseAction<int>>>.Empty.ToBuilder();

            foreach(KeyValuePair<ItemSet, ImmutableSortedDictionary<Symbol, ParseAction<ItemSet>>> kvp in oldParseTable)
            {
                System.Diagnostics.Debug.Assert(conversionDict[kvp.Key] == newParseTable.Count);

                ImmutableSortedDictionary<Symbol, ParseAction<int>>.Builder b = ImmutableSortedDictionary<Symbol, ParseAction<int>>.Empty.WithComparers(Symbol.Adapter).ToBuilder();
                foreach(KeyValuePair<Symbol, ParseAction<ItemSet>> kvp2 in kvp.Value)
                {
                    b.Add(kvp2.Key, kvp2.Value.Convert(itemSet => conversionDict[itemSet]));
                }

                newParseTable.Add(b.ToImmutable());
            }

            return new IntParseTableData
            (
                newParseTable.ToImmutable(),
                conversionDict,
                conversionDict[initialStateSet.Value]
            );
        }

        public IntParseTableData IntParseTableData => intParseTableData.Value;
    }

    public sealed class IntParseTableData
    {
        private readonly ImmutableList<ImmutableSortedDictionary<Symbol, ParseAction<int>>> parseTable;
        private readonly Option<ImmutableSortedDictionary<ItemSet, int>> conversionDict;
        private readonly int startState;

        public IntParseTableData
        (
            ImmutableList<ImmutableSortedDictionary<Symbol, ParseAction<int>>> parseTable,
            ImmutableSortedDictionary<ItemSet, int> conversionDict,
            int startState
        )
        {
            this.parseTable = parseTable;
            this.conversionDict = Option<ImmutableSortedDictionary<ItemSet, int>>.Some(conversionDict);
            this.startState = startState;
        }

        public IntParseTableData
        (
            ImmutableList<ImmutableSortedDictionary<Symbol, ParseAction<int>>> parseTable,
            int startState
        )
        {
            this.parseTable = parseTable;
            this.conversionDict = Option<ImmutableSortedDictionary<ItemSet, int>>.None;
            this.startState = startState;
        }

        public ImmutableList<ImmutableSortedDictionary<Symbol, ParseAction<int>>> ParseTable => parseTable;

        public Option<ImmutableSortedDictionary<ItemSet, int>> ConversionDictionary => conversionDict;

        public int StartState => startState;

        private static readonly Lazy<ITypeTraits<IntParseTableData>> typeTraits = new Lazy<ITypeTraits<IntParseTableData>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ITypeTraits<IntParseTableData> GetTypeTraits()
        {
            return new ConvertTypeTraits<IntParseTableData, (ImmutableList<ImmutableSortedDictionary<Symbol, ParseAction<int>>>, Option<ImmutableSortedDictionary<ItemSet, int>>, int)>
            (
                p => (p.ParseTable, p.ConversionDictionary, p.StartState),
                new ValueTupleTypeTraits<ImmutableList<ImmutableSortedDictionary<Symbol, ParseAction<int>>>, Option<ImmutableSortedDictionary<ItemSet, int>>, int>
                (
                    new ListTypeTraits<ImmutableSortedDictionary<Symbol, ParseAction<int>>>
                    (
                        new DictionaryTypeTraits<Symbol, ParseAction<int>>(Symbol.Traits, ParseAction<int>.GetTypeTraits(Int32TypeTraits.Value))
                    ),
                    new OptionTypeTraits<ImmutableSortedDictionary<ItemSet, int>>
                    (
                        new DictionaryTypeTraits<ItemSet, int>(ItemSet.TypeTraits, Int32TypeTraits.Value)
                    ),
                    Int32TypeTraits.Value
                ),
                t =>
                {
                    if (t.Item2.HasValue)
                    {
                        return new IntParseTableData(t.Item1, t.Item2.Value, t.Item3);
                    }
                    else
                    {
                        return new IntParseTableData(t.Item1, t.Item3);
                    }
                }
            );
        }

        public static ITypeTraits<IntParseTableData> TypeTraits => typeTraits.Value;

        public IntParseTableData WithoutConversionDictionary()
        {
            return new IntParseTableData(parseTable, startState);
        }
    }
}
