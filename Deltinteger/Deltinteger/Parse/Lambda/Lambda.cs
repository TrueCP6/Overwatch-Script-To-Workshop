using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse.Lambda
{
    public interface IVariableTracker
    {
        void LocalVariableAccessed(IIndexReferencer variable);
    }

    public interface ILambdaApplier : ILabeled
    {
        CallInfo CallInfo { get; }
        IRecursiveCallHandler RecursiveCallHandler { get; }
        IBridgeInvocable[] InvokedState { get; }
        bool ResolvedSource { get; }
        void GetLambdaStatement(PortableLambdaType expecting);
        void GetLambdaStatement();
        IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues);
    }

    public interface IBridgeInvocable
    {
        bool Invoked { get; }
        void WasInvoked();
        void OnInvoke(Action onInvoke);
    }

    public class SubLambdaInvoke : IBridgeInvocable
    {
        public bool Invoked { get; private set; }
        public List<Action> Actions { get; } = new List<Action>();
        
        public void WasInvoked() => Invoked = true;
        public void OnInvoke(Action onInvoke) => Actions.Add(onInvoke);
    }

    public class LambdaContextHandler : IVarContextHandler
    {
        public ParseInfo ParseInfo { get; }
        private readonly LambdaParameter _parameter;

        public LambdaContextHandler(ParseInfo parseInfo, LambdaParameter parameter)
        {
            ParseInfo = parseInfo;
            _parameter = parameter;
        }

        public VarBuilderAttribute[] GetAttributes() => new VarBuilderAttribute[0];
        public IParseType GetCodeType() => _parameter.Type;
        public Location GetDefineLocation() => new Location(ParseInfo.Script.Uri, GetNameRange());
        public string GetName() => _parameter.Identifier.GetText();
        public DocRange GetNameRange() => _parameter.Identifier.GetRange(_parameter.Range);
        public DocRange GetTypeRange() => _parameter.Type?.Range;
        public bool CheckName() => _parameter.Identifier;
    }

    public class ExpectingLambdaInfo
    {
        public PortableLambdaType Type { get; }
        public bool RegisterOccursLater { get; }

        public ExpectingLambdaInfo()
        {
            RegisterOccursLater = true;
        }

        public ExpectingLambdaInfo(PortableLambdaType type)
        {
            RegisterOccursLater = false;
            Type = type;
        }
    }

    public class CheckLambdaContext
    {
        public ParseInfo ParseInfo;
        public ILambdaApplier Applier;
        public string ErrorMessage;
        public DocRange Range;
        public ParameterState ParameterState;

        public CheckLambdaContext(ParseInfo parseInfo, ILambdaApplier applier, string errorMessage, DocRange range, ParameterState parameterState)
        {
            ParseInfo = parseInfo;
            Applier = applier;
            ErrorMessage = errorMessage;
            Range = range;
            ParameterState = parameterState;
        }

        public void Check()
        {
            // If no lambda was expected, throw an error since the parameter types can not be determined.
            if (ParseInfo.ExpectingLambda == null)
            {
                // Parameter data is known.
                if (ParameterState == ParameterState.CountAndTypesKnown)
                    Applier.GetLambdaStatement();
                else
                    ParseInfo.Script.Diagnostics.Error(ErrorMessage, Range);
            }
            
            // The arrow registration occurs now, parse the statement.
            else if (!ParseInfo.ExpectingLambda.RegisterOccursLater)
                Applier.GetLambdaStatement(ParseInfo.ExpectingLambda.Type);
        }
    }

    public enum ParameterState
    {
        Unknown,
        CountKnown,
        CountAndTypesKnown
    }
}