namespace Sunlighter.LrParserGenLib
{
    public delegate TValue ReductionFunc<TValue>(ReductionInfo info, ImmutableList<TValue> args);

    public class ImmutableParserState<TValue>
    {
        private readonly IntParseTableData parseTable;
        private readonly ImmutableList<RuleInfo> ruleInfo;
        private readonly ReductionFunc<TValue> reduceFunc;
        private readonly ImmutableList<int> stateStack;
        private readonly ImmutableList<TValue> valueStack;

        private readonly Lazy<ImmutableSortedSet<Symbol>> acceptableSymbols;

        public ImmutableParserState
        (
            IntParseTableData parseTable,
            ImmutableList<RuleInfo> ruleInfo,
            ReductionFunc<TValue> reduceFunc
        )
            : this(parseTable, ruleInfo, reduceFunc, [parseTable.StartState], [])
        {
        }

        private ImmutableParserState
        (
            IntParseTableData parseTable,
            ImmutableList<RuleInfo> ruleInfo,
            ReductionFunc<TValue> reduceFunc,
            ImmutableList<int> stateStack,
            ImmutableList<TValue> valueStack
        )
        {
            this.parseTable = parseTable;
            this.ruleInfo = ruleInfo;
            this.reduceFunc = reduceFunc;
            this.stateStack = stateStack;
            this.valueStack = valueStack;

            acceptableSymbols = new Lazy<ImmutableSortedSet<Symbol>>(GetAcceptableSymbols, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private ImmutableSortedSet<Symbol> GetAcceptableSymbols()
        {
            int currentState = stateStack[^1];
            ImmutableSortedDictionary<Symbol, ParseAction<int>> stateInfo = parseTable.ParseTable[currentState];
            return ImmutableSortedSet<Symbol>.Empty.WithComparer(stateInfo.KeyComparer).Union(stateInfo.Keys);
        }

        public ImmutableSortedSet<Symbol> AcceptableSymbols => acceptableSymbols.Value;

        public bool IsAcceptState => parseTable.ParseTable[stateStack[^1]].TryGetValue(EofSymbol.Value, out ParseAction<int>? pa) && pa is ParseAction_ReduceByRule<int> par && par.RuleNumber == 0;

        public TValue AcceptedValue => IsAcceptState ? valueStack[^1] : throw new InvalidOperationException("Input has not been accepted");

        private static Option<ImmutableParserState<TValue>> StaticShift(ImmutableParserState<TValue> initialState, Symbol terminalType, TValue value)
        {
            ImmutableParserState<TValue> state = initialState;
            bool reductionsOccurred = false;

            while (true)
            {
                int currentState = state.stateStack[^1];
                ImmutableSortedDictionary<Symbol, ParseAction<int>> stateInfo = state.parseTable.ParseTable[currentState];

                if (stateInfo.TryGetValue(terminalType, out ParseAction<int>? action))
                {
                    if (action is ParseAction_Shift<int> shiftAction)
                    {
                        return Option<ImmutableParserState<TValue>>.Some
                        (
                            new ImmutableParserState<TValue>
                            (
                                state.parseTable,
                                state.ruleInfo,
                                state.reduceFunc,
                                state.stateStack.Add(shiftAction.State),
                                state.valueStack.Add(value)
                            )
                        );
                    }
                    else if (action is ParseAction_ReduceByRule<int> reduceByRuleAction)
                    {
                        int ruleNumber = reduceByRuleAction.RuleNumber;

                        if (ruleNumber == 0)
                        {
                            if (Symbol.Traits.Compare(terminalType, EofSymbol.Value) == 0)
                            {
                                // about to accept input
                                return Option<ImmutableParserState<TValue>>.Some(state);
                            }
                            else
                            {
                                throw new InvalidOperationException("Parse table error: attempt to accept before EOF");
                            }
                        }

                        RuleInfo ruleInfo = state.ruleInfo[ruleNumber];
                        int ruleLength = ruleInfo.RuleLength;

                        ImmutableList<TValue> args = state.valueStack.RemoveRange(0, state.valueStack.Count - ruleLength);
                        TValue reductionResult = state.reduceFunc(new ReductionInfo(ruleNumber, ruleLength), args);

                        ImmutableList<TValue> newValueStack = state.valueStack.RemoveRange(state.valueStack.Count - ruleLength, ruleLength);
                        ImmutableList<int> newStateStack = state.stateStack.RemoveRange(state.stateStack.Count - ruleLength, ruleLength);

                        int shiftNonTermState = newStateStack[^1];
                        ImmutableSortedDictionary<Symbol, ParseAction<int>> shiftNonTermStateInfo = state.parseTable.ParseTable[shiftNonTermState];
                        if (shiftNonTermStateInfo.TryGetValue(ruleInfo.LHS, out ParseAction<int>? shiftNonTermParseAction))
                        {
                            if (shiftNonTermParseAction is ParseAction_Shift<int> shiftNonTerm)
                            {
                                newStateStack = newStateStack.Add(shiftNonTerm.State);
                                newValueStack = newValueStack.Add(reductionResult);

                                state = new ImmutableParserState<TValue>
                                (
                                    state.parseTable,
                                    state.ruleInfo,
                                    state.reduceFunc,
                                    newStateStack,
                                    newValueStack
                                );

                                reductionsOccurred = true;

                                // loop back for more
                            }
                            else
                            {
                                throw new InvalidOperationException("Parse table error: must shift nonterminal after reduce (action wasn't a shift)");
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("Parse table error: must shift nonterminal after reduce (action wasn't found)");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Parse table error: unresolved conflict");
                    }
                }
                else
                {
                    if (reductionsOccurred) throw new InvalidOperationException("Parse table error: erroneous reductions detected (shouldn't happen with LR(1))");

                    return Option<ImmutableParserState<TValue>>.None;
                }
            }
        }

        public Option<ImmutableParserState<TValue>> Shift(Symbol terminalType, TValue value) => StaticShift(this, terminalType, value);
    }

    public sealed class ReductionInfo(int ruleNumber, int ruleLength)
    {
        private readonly int ruleNumber = ruleNumber;
        private readonly int ruleLength = ruleLength;

        public int RuleNumber => ruleNumber;
        public int RuleLength => ruleLength;
    }

    public sealed class RuleInfo(Symbol lhs, int ruleLength)
    {
        private readonly Symbol lhs = lhs;
        private readonly int ruleLength = ruleLength;

        public Symbol LHS => lhs;
        public int RuleLength => ruleLength;
    }

    public static class ParserStateUtility
    {
        public static ImmutableList<RuleInfo> GetRuleInfo(this Grammar g)
        {
            return g.Rules.Select(r => new RuleInfo(r.LHS, r.RHS.Count)).ToImmutableList();
        }

        public static TValue TryParse<TValue>(this ImmutableParserState<TValue> state, ImmutableList<Token<TValue>> input, TValue eofValueNotUsed)
        {
            foreach(int i in Enumerable.Range(0, input.Count))
            {
                Token<TValue> token = input[i];
                Option<ImmutableParserState<TValue>> optNextState = state.Shift(token.Type, token.Value);
                if (optNextState.HasValue)
                {
                    state = optNextState.Value;
                }
                else
                {
                    throw new Exception($"Unexpected token at index {i} (type {token.Type} found, expected {string.Join(", ", state.AcceptableSymbols)})");
                }
            }

            Option<ImmutableParserState<TValue>> optFinalState = state.Shift(EofSymbol.Value, eofValueNotUsed);
            if (optFinalState.HasValue)
            {
                state = optFinalState.Value;
            }

            if (state.IsAcceptState)
            {
                return state.AcceptedValue;
            }
            else
            {
                throw new Exception($"Unexpected end of input");
            }
        }
    }
}
