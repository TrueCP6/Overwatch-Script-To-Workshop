using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Antlr4.Runtime.Tree;

namespace Deltin.Deltinteger.Parse
{
    public class ExpressionTree : IExpression, IStatement
    {
        public IExpression[] Tree { get; }
        public IExpression Result { get; }
        public bool Completed { get; } = true;
        public TreeContextPart[] ExprContextTree { get; }

        private ITerminalNode _trailingSeperator = null;

        public ExpressionTree(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_expr_treeContext exprContext, bool usedAsValue)
        {
            ExprContextTree = Flatten(parseInfo.Script, exprContext);

            Tree = new IExpression[ExprContextTree.Length];
            IExpression current = ExprContextTree[0].Parse(parseInfo, scope, scope, true);
            Tree[0] = current;
            if (current != null)
                for (int i = 1; i < ExprContextTree.Length; i++)
                {
                    current = ExprContextTree[i].Parse(parseInfo, current.ReturningScope() ?? new Scope(), scope, i < ExprContextTree.Length - 1 || usedAsValue);

                    Tree[i] = current;

                    if (current == null)
                    {
                        Completed = false;
                        break;
                    }
                }
            else Completed = false;
        
            if (Completed)
                Result = Tree[Tree.Length - 1];
            
            // Get the completion items for each expression in the path.
            GetCompletion(parseInfo.Script, scope);
        }

        private TreeContextPart[] Flatten(ScriptFile script, DeltinScriptParser.E_expr_treeContext exprContext)
        {
            var exprList = new List<TreeContextPart>();
            Flatten(script, exprContext, exprList);
            return exprList.ToArray();

            void Flatten(ScriptFile script, DeltinScriptParser.E_expr_treeContext exprContext, List<TreeContextPart> exprList)
            {
                if (exprContext.expr() is DeltinScriptParser.E_expr_treeContext)
                    Flatten(script, (DeltinScriptParser.E_expr_treeContext)exprContext.expr(), exprList);
                else
                    exprList.Add(new TreeContextPart(exprContext.expr()));

                if (exprContext.method() == null && exprContext.variable() == null)
                {
                    script.Diagnostics.Error("Expected expression.", DocRange.GetRange(exprContext.SEPERATOR()));
                    _trailingSeperator = exprContext.SEPERATOR();
                }
                else
                {
                    if (exprContext.method() != null)
                        exprList.Add(new TreeContextPart(exprContext.method()));
                    if (exprContext.variable() != null)
                        exprList.Add(new TreeContextPart(exprContext.variable()));
                }
            }
        }

        private void GetCompletion(ScriptFile script, Scope scope)
        {
            for (int i = 0; i < Tree.Length; i++)
            if (Tree[i] != null)
            {
                // Get the treescope. Don't get the completion items if it is null.
                var treeScope = Tree[i].ReturningScope();
                if (treeScope != null)
                {
                    DocRange range;
                    if (i < Tree.Length - 1)
                    {
                        range = ExprContextTree[i + 1].CompletionRange;
                    }
                    // Expression path has a trailing '.'
                    else if (_trailingSeperator != null)
                    {
                        range = new DocRange(
                            DocRange.GetRange(_trailingSeperator).end,
                            DocRange.GetRange(script.NextToken(_trailingSeperator)).start
                        );
                    }
                    else continue;

                    script.AddCompletionRange(new CompletionRange(treeScope, scope, range, CompletionRangeKind.ClearRest));
                }
            }
        }

        public Scope ReturningScope()
        {
            if (Completed)
                return Result.ReturningScope();
            else
                return null;
        }

        public CodeType Type() => Result?.Type();

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            return ParseTree(actionSet, true).Result;
        }

        public void Translate(ActionSet actionSet)
        {
            ParseTree(actionSet, false);
        }

        /// <summary>Sets the related output comment. This assumes that the result is both an IExpression and an IStatement.</summary>
        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment) => ((IStatement)Result).OutputComment(diagnostics, range, comment);

        public ExpressionTreeParseResult ParseTree(ActionSet actionSet, bool expectingValue)
        {
            IGettable resultingVariable = null;
            IWorkshopTree target = null;
            IWorkshopTree result = null;
            VarIndexAssigner currentAssigner = actionSet.IndexAssigner;
            IWorkshopTree currentObject = null;
            Element[] resultIndex = new Element[0];

            for (int i = 0; i < Tree.Length; i++)
            {
                bool isLast = i == Tree.Length - 1;
                IWorkshopTree current = null;
                if (Tree[i] is CallVariableAction)
                {
                    var callVariableAction = (CallVariableAction)Tree[i];

                    var reference = currentAssigner[callVariableAction.Calling];
                    current = reference.GetVariable((Element)target);

                    resultIndex = new Element[callVariableAction.Index.Length];
                    for (int ai = 0; ai < callVariableAction.Index.Length; ai++)
                    {
                        var workshopIndex = callVariableAction.Index[ai].Parse(actionSet);
                        resultIndex[ai] = (Element)workshopIndex;
                        current = Element.Part<V_ValueInArray>(current, workshopIndex);
                    }

                    // If this is the last node in the tree, set the resulting variable.
                    if (isLast) resultingVariable = reference;
                }
                else
                {
                    var newCurrent = Tree[i].Parse(actionSet.New(currentAssigner).New(currentObject));
                    if (newCurrent != null)
                    {
                        current = newCurrent;
                        resultIndex = new Element[0];
                    }
                }
                
                if (Tree[i].Type() == null)
                {
                    // If this isn't the last in the tree, set it as the target.
                    if (!isLast)
                        target = current;
                    currentObject = null;
                }
                else
                {
                    var type = Tree[i].Type();

                    currentObject = current;
                    currentAssigner = actionSet.IndexAssigner.CreateContained();
                    type.AddObjectVariablesToAssigner(currentObject, currentAssigner);
                }

                result = current;
            }

            if (result == null && expectingValue) throw new Exception("Expression tree result is null");
            return new ExpressionTreeParseResult(result, resultIndex, target, resultingVariable);
        }
    
        public static IExpression ResultingExpression(IExpression expression)
        {
            if (expression is ExpressionTree expressionTree) return expressionTree.Result;
            return expression;
        }
    }

    public class TreeContextPart
    {
        public DocRange Range { get; }
        public DocRange CompletionRange { get; }
        private readonly DeltinScriptParser.VariableContext variable;
        private readonly DeltinScriptParser.MethodContext method;
        private readonly DeltinScriptParser.ExprContext expression;

        public TreeContextPart(DeltinScriptParser.VariableContext variable)
        {
            this.variable = variable ?? throw new ArgumentNullException(nameof(variable));
            Range = DocRange.GetRange(variable);
            CompletionRange = Range;
        }
        public TreeContextPart(DeltinScriptParser.MethodContext method)
        {
            this.method = method ?? throw new ArgumentNullException(nameof(method));
            Range = DocRange.GetRange(method);
            CompletionRange = DocRange.GetRange(method.PART());
        }
        public TreeContextPart(DeltinScriptParser.ExprContext expression)
        {
            this.expression = expression ?? throw new ArgumentNullException(nameof(expression));
            Range = DocRange.GetRange(expression);
            CompletionRange = Range;
        }

        public IExpression Parse(ParseInfo parseInfo, Scope scope, Scope getter, bool usedAsValue)
        {
            if (variable != null)
                return parseInfo.GetVariable(scope, getter, variable, false);
            if (method != null)
                return new CallMethodAction(parseInfo, scope, method, usedAsValue, getter);
            if (expression != null)
                return parseInfo.GetExpression(scope, expression, false, usedAsValue, getter);
            
            throw new Exception();
        }
    }

    public class ExpressionTreeParseResult
    {
        public IWorkshopTree Result { get; }
        public Element[] ResultingIndex { get; }
        public IWorkshopTree Target { get; }
        public IGettable ResultingVariable { get; }

        public ExpressionTreeParseResult(IWorkshopTree result, Element[] index, IWorkshopTree target, IGettable resultingVariable)
        {
            Result = result;
            ResultingIndex = index;
            Target = target;
            ResultingVariable = resultingVariable;
        }
    }

    public class VariableResolve
    {
        public bool DoesResolveToVariable { get; }

        private DocRange NotAVariableRange { get; }
        private DocRange VariableRange { get; }

        public CallVariableAction SetVariable { get; }
        private ExpressionTree Tree { get; }

        public VariableResolve(VariableResolveOptions options, IExpression expression, DocRange expressionRange, FileDiagnostics diagnostics)
        {
            // The expression is a variable.
            if (expression is CallVariableAction)
            {
                // Get the variable being set and the range.
                SetVariable = (CallVariableAction)expression;
                VariableRange = expressionRange;
            }
            // The expression is an expression tree.
            else if (expression is ExpressionTree tree)
            {
                Tree = tree;
                if (tree.Completed)
                {
                    // If the resulting expression in the tree is not a variable.
                    if (tree.Result is CallVariableAction == false)
                        NotAVariableRange = tree.ExprContextTree.Last().Range;
                    else
                    {
                        // Get the variable and the range.
                        SetVariable = (CallVariableAction)tree.Result;
                        VariableRange = tree.ExprContextTree.Last().Range;
                    }
                }
            }
            // The expression is not a variable.
            else if (expression != null)
                NotAVariableRange = expressionRange;

            // NotAVariableRange will not be null if the resulting expression is a variable.
            if (NotAVariableRange != null)
                diagnostics.Error("Expected a variable.", NotAVariableRange);
            
            // Make sure the variable can be set to.
            if (SetVariable != null)
            {
                // Check if the variable is settable.
                if (options.ShouldBeSettable && !SetVariable.Calling.Settable())
                    diagnostics.Error($"The variable '{SetVariable.Calling.Name}' cannot be set to.", VariableRange);
                
                // Check if the variable is a whole workshop variable.
                if (options.FullVariable)
                {
                    Var asVar = SetVariable.Calling as Var;
                    if (asVar == null || asVar.StoreType != StoreType.FullVariable)
                        diagnostics.Error($"The variable '{SetVariable.Calling.Name}' cannot be indexed.", VariableRange);
                }

                // Check for indexers.
                if (!options.CanBeIndexed && SetVariable.Index.Length != 0)
                    diagnostics.Error($"The variable '{SetVariable.Calling.Name}' cannot be indexed.", VariableRange);
            }
            
            DoesResolveToVariable = SetVariable != null;
        }

        public VariableElements ParseElements(ActionSet actionSet)
        {
            IndexReference var;
            Element target = null;
            Element[] index;

            if (Tree != null)
            {
                // Parse the tree.
                ExpressionTreeParseResult treeParseResult = Tree.ParseTree(actionSet, true);
                // Get the variable.
                var = (IndexReference)treeParseResult.ResultingVariable;
                // Get the target.
                target = (Element)treeParseResult.Target;
                // Get the index.
                index = treeParseResult.ResultingIndex;
            }
            else
            {
                // Get the variable.
                var = (IndexReference)actionSet.IndexAssigner[SetVariable.Calling];
                // Get the index.
                index = Array.ConvertAll(SetVariable.Index, index => (Element)index.Parse(actionSet));
            }

            return new VariableElements(var, target, index);
        }
    }

    public class VariableElements
    {
        public IndexReference IndexReference { get; }
        public Element Target { get; }
        public Element[] Index { get; }

        public VariableElements(IndexReference indexReference, Element target, Element[] index)
        {
            IndexReference = indexReference;
            Target = target;
            Index = index;
        }
    }

    public class VariableResolveOptions
    {
        /// <summary>Determines if a variables needs to be an entire workshop variable.</summary>
        public bool FullVariable = false;
        /// <summary>Determines if the variable can be set to a value in an array.</summary>
        public bool CanBeIndexed = true;
        /// <summary>Determines if the variable should be settable.</summary>
        public bool ShouldBeSettable = true;
    }
}