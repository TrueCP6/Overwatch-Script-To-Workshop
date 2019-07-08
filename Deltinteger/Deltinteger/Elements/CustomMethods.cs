﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomMethod : Attribute
    {
        public CustomMethod(string elementName, CustomMethodType methodType)
        {
            MethodName = elementName;
            MethodType = methodType;
        }

        public string MethodName { get; private set; }
        public CustomMethodType MethodType { get; private set; }
    }

    public class MethodResult
    {
        public MethodResult(Element[] elements, Element result)
        {
            Elements = elements;
            Result = result;
        }
        public Element[] Elements { get; private set; }
        public Element Result { get; private set; }
    }

    public enum CustomMethodType
    {
        Value,
        MultiAction_Value,
        Action
    }

    public class CustomMethodData : IMethod
    {
        public string Name { get; }
        public ParameterBase[] Parameters { get; }
        public CustomMethodType CustomMethodType { get; }
        public Type Type { get; }
        public WikiMethod Wiki { get; }
        public string GetLabel(bool markdown)
        {
            return Name + "(" + Parameter.ParameterGroupToString(Parameters, markdown) + ")"
            + (markdown && Wiki?.Description != null ? "\n\r" + Wiki.Description : "");
        }

        public CustomMethodData(Type type)
        {
            Type = type;

            CustomMethod data = type.GetCustomAttribute<CustomMethod>();
            Name = data.MethodName;
            CustomMethodType = data.MethodType;

            Parameters = type.GetCustomAttributes<ParameterBase>()
                .ToArray();
            
            Wiki = GetObject().Wiki();
        }

        public CustomMethodBase GetObject(Translate context, ScopeGroup scope, IWorkshopTree[] parameters)
        {
            CustomMethodBase customMethod = GetObject();
            customMethod.TranslateContext = context;
            customMethod.Scope = scope;
            customMethod.Parameters = parameters;
            return customMethod;
        }
        private CustomMethodBase GetObject()
        {
            return (CustomMethodBase)Activator.CreateInstance(Type);
        }

        static CustomMethodData[] _customMethodData = null;
        private static CustomMethodData[] GetCustomMethods()
        {
            if (_customMethodData == null)
            {
                Type[] types = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(type => type.GetCustomAttribute<CustomMethod>() != null)
                    .ToArray();

                _customMethodData = new CustomMethodData[types.Length];
                for (int i = 0; i < _customMethodData.Length; i++)
                    _customMethodData[i] = new CustomMethodData(types[i]);
            }
            return _customMethodData;
        }

        public static CustomMethodData GetCustomMethod(string name)
        {
            return GetCustomMethods().FirstOrDefault(method => method.Name == name);
        }

        public static CompletionItem[] GetCompletion()
        {
            return GetCustomMethods().Select(cm => new CompletionItem(cm.Name) 
            { 
                detail = cm.GetLabel(false),
                kind = CompletionItem.Method,
                documentation = cm.Wiki?.Description
            }).ToArray();
        }
    }

    public abstract class CustomMethodBase
    {
        public Translate TranslateContext { get; set; }
        public IWorkshopTree[] Parameters { get; set; }
        public ScopeGroup Scope { get; set; }

        public MethodResult Result()
        {
            if (TranslateContext == null)
                throw new ArgumentNullException(nameof(TranslateContext));
            
            if (Parameters == null)
                throw new ArgumentNullException(nameof(Parameters));
            
            if (Scope == null)
                throw new ArgumentNullException(nameof(Scope));
            
            return Get();
        }

        protected abstract MethodResult Get();

        public abstract WikiMethod Wiki();

        protected static Element[] ElementBuilder(params ElementBuilderPart[] parts)
        {
            List<Element> elements = new List<Element>();
            foreach(ElementBuilderPart part in parts)
                elements.AddRange(part.Elements);
            return elements.ToArray();
        }

        protected class ElementBuilderPart
        {
            public Element[] Elements;

            public ElementBuilderPart(Element[] elements)
            {
                Elements = elements;
            }

            public static implicit operator ElementBuilderPart(Element element)
            {
                return new ElementBuilderPart(new Element[] { element });
            }

            public static implicit operator ElementBuilderPart(Element[] elements)
            {
                return new ElementBuilderPart(elements);
            }
        }
    }

    [CustomMethod("AngleOfVectors", CustomMethodType.MultiAction_Value)]
    [Parameter("Vector1", ValueType.VectorAndPlayer, null)]
    [Parameter("Vector2", ValueType.VectorAndPlayer, null)]
    [Parameter("Vector3", ValueType.VectorAndPlayer, null)]
    class AngleOfVectors : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            var eventPlayer = new V_EventPlayer();

            Var a      = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: a"     , TranslateContext.IsGlobal);
            Var b      = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: b"     , TranslateContext.IsGlobal);
            Var c      = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: c"     , TranslateContext.IsGlobal);
            Var ab     = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: ab"    , TranslateContext.IsGlobal);
            Var bc     = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: bc"    , TranslateContext.IsGlobal);
            Var abVec  = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: abVec" , TranslateContext.IsGlobal);
            Var bcVec  = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: bcVec" , TranslateContext.IsGlobal);
            Var abNorm = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: abNorm", TranslateContext.IsGlobal);
            Var bcNorm = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: bcNorm", TranslateContext.IsGlobal);

            Element zeroVec = Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(0));

            return new MethodResult
            (
                ElementBuilder
                (
                    // Save A
                    a.SetVariable((Element)Parameters[0], eventPlayer),
                    // Save B
                    b.SetVariable((Element)Parameters[1], eventPlayer),
                    // save C
                    c.SetVariable((Element)Parameters[2], eventPlayer),

                    // get ab
                    // ab[3] = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
                    ab.SetVariable(
                        Element.Part<V_Vector>
                        (
                            Element.Part<V_Subtract>(Element.Part<V_XOf>(b.GetVariable()), Element.Part<V_XOf>(a.GetVariable())),
                            Element.Part<V_Subtract>(Element.Part<V_YOf>(b.GetVariable()), Element.Part<V_YOf>(a.GetVariable())),
                            Element.Part<V_Subtract>(Element.Part<V_ZOf>(b.GetVariable()), Element.Part<V_ZOf>(a.GetVariable()))
                        ), eventPlayer),

                    // get bc
                    // bc[3] = { c[0] - b[0], c[1] - b[1], c[2] - b[2] };
                    bc.SetVariable(
                        Element.Part<V_Vector>
                        (
                            Element.Part<V_Subtract>(Element.Part<V_XOf>(c.GetVariable()), Element.Part<V_XOf>(b.GetVariable())),
                            Element.Part<V_Subtract>(Element.Part<V_YOf>(c.GetVariable()), Element.Part<V_YOf>(b.GetVariable())),
                            Element.Part<V_Subtract>(Element.Part<V_ZOf>(c.GetVariable()), Element.Part<V_ZOf>(b.GetVariable()))
                        ), eventPlayer),

                    // get abVec
                    // abVec = sqrt(ab[0] * ab[0] + ab[1] * ab[1] + ab[2] * ab[2]);
                    abVec.SetVariable(
                        Element.Part<V_DistanceBetween>
                        (
                            ab.GetVariable(),
                            zeroVec
                        ), eventPlayer),

                    // get bcVec
                    // bcVec = sqrt(bc[0] * bc[0] + bc[1] * bc[1] + bc[2] * bc[2]);
                    bcVec.SetVariable(
                        Element.Part<V_DistanceBetween>
                        (
                            bc.GetVariable(),
                            zeroVec
                        ), eventPlayer),

                    // get abNorm
                    // abNorm[3] = {ab[0] / abVec, ab[1] / abVec, ab[2] / abVec};
                    abNorm.SetVariable(
                        Element.Part<V_Vector>
                        (
                            Element.Part<V_Divide>(Element.Part<V_XOf>(ab.GetVariable()), abVec.GetVariable()),
                            Element.Part<V_Divide>(Element.Part<V_YOf>(ab.GetVariable()), abVec.GetVariable()),
                            Element.Part<V_Divide>(Element.Part<V_ZOf>(ab.GetVariable()), abVec.GetVariable())
                        ), eventPlayer),

                    // get bcNorm
                    // bcNorm[3] = {bc[0] / bcVec, bc[1] / bcVec, bc[2] / bcVec};
                    bcNorm.SetVariable(
                        Element.Part<V_Vector>
                        (
                            Element.Part<V_Divide>(Element.Part<V_XOf>(bc.GetVariable()), bcVec.GetVariable()),
                            Element.Part<V_Divide>(Element.Part<V_YOf>(bc.GetVariable()), bcVec.GetVariable()),
                            Element.Part<V_Divide>(Element.Part<V_ZOf>(bc.GetVariable()), bcVec.GetVariable())
                        ), eventPlayer)
                ),
                Element.Part<V_Divide>
                (
                    Element.Part<V_Multiply>
                    (
                        Element.Part<V_ArccosineInRadians>
                        (
                            // get res
                            // res = abNorm[0] * bcNorm[0] + abNorm[1] * bcNorm[1] + abNorm[2] * bcNorm[2];
                            //target.SetVariable(
                            Element.Part<V_Add>
                            (
                                Element.Part<V_Add>
                                (
                                    Element.Part<V_Multiply>(Element.Part<V_XOf>(abNorm.GetVariable()), Element.Part<V_XOf>(bcNorm.GetVariable())),
                                    Element.Part<V_Multiply>(Element.Part<V_YOf>(abNorm.GetVariable()), Element.Part<V_YOf>(bcNorm.GetVariable()))
                                ),
                                Element.Part<V_Multiply>(Element.Part<V_ZOf>(abNorm.GetVariable()), Element.Part<V_ZOf>(bcNorm.GetVariable()))
                            )
                        ),
                        new V_Number(180)
                    ),
                    new V_Number(Math.PI)
                )
            );
        }
    
        public override WikiMethod Wiki()
        {
            return new WikiMethod("AngleOfVectors", "Gets the angle of 3 vectors in 3d space.", null);
        }
    }

    [CustomMethod("AngleOfVectorsCom", CustomMethodType.Value)]
    [Parameter("Vector1", ValueType.VectorAndPlayer, null)]
    [Parameter("Vector2", ValueType.VectorAndPlayer, null)]
    [Parameter("Vector3", ValueType.VectorAndPlayer, null)]
    class AngleOfVectorsCom : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element zeroVec = Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(0));
            Element a = (Element)Parameters[0];
            Element b = (Element)Parameters[1];
            Element c = (Element)Parameters[2];

            Element ab = Element.Part<V_Vector>
            (
                Element.Part<V_Subtract>(Element.Part<V_XOf>(b), Element.Part<V_XOf>(a)),
                Element.Part<V_Subtract>(Element.Part<V_YOf>(b), Element.Part<V_YOf>(a)),
                Element.Part<V_Subtract>(Element.Part<V_ZOf>(b), Element.Part<V_ZOf>(a))
            );
            Element bc = Element.Part<V_Vector>
            (
                Element.Part<V_Subtract>(Element.Part<V_XOf>(c), Element.Part<V_XOf>(b)),
                Element.Part<V_Subtract>(Element.Part<V_YOf>(c), Element.Part<V_YOf>(b)),
                Element.Part<V_Subtract>(Element.Part<V_ZOf>(c), Element.Part<V_ZOf>(b))
            );
            Element abVec = Element.Part<V_DistanceBetween>
            (
                ab,
                zeroVec
            );
            Element bcVec = Element.Part<V_DistanceBetween>
            (
                bc,
                zeroVec
            );
            Element abNorm = Element.Part<V_Vector>
            (
                Element.Part<V_Divide>(Element.Part<V_XOf>(ab), abVec),
                Element.Part<V_Divide>(Element.Part<V_YOf>(ab), abVec),
                Element.Part<V_Divide>(Element.Part<V_ZOf>(ab), abVec)
            );
            Element bcNorm = Element.Part<V_Vector>
            (
                Element.Part<V_Divide>(Element.Part<V_XOf>(bc), bcVec),
                Element.Part<V_Divide>(Element.Part<V_YOf>(bc), bcVec),
                Element.Part<V_Divide>(Element.Part<V_ZOf>(bc), bcVec)
            );
            Element res = Element.Part<V_Add>
            (
                Element.Part<V_Add>
                (
                    Element.Part<V_Multiply>(Element.Part<V_XOf>(abNorm), Element.Part<V_XOf>(bcNorm)),
                    Element.Part<V_Multiply>(Element.Part<V_YOf>(abNorm), Element.Part<V_YOf>(bcNorm))
                ),
                Element.Part<V_Multiply>(Element.Part<V_ZOf>(abNorm), Element.Part<V_ZOf>(bcNorm))
            );
            Element result = Element.Part<V_Divide>
            (
                Element.Part<V_Multiply>
                (
                    Element.Part<V_ArccosineInRadians>(res),
                    new V_Number(180)
                ),
                new V_Number(Math.PI)
            );

            return new MethodResult(null, result);
        }
    
        public override WikiMethod Wiki()
        {
            return new WikiMethod("AngleOfVectorsCom", "Gets the angle of 3 vectors in 3d space.", null);
        }
    }

    [CustomMethod("GetMap", CustomMethodType.MultiAction_Value)]
    class GetMap : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Var temp = TranslateContext.VarCollection.AssignVar(Scope, "GetMap: temp", TranslateContext.IsGlobal);

            Element[] actions = ElementBuilder
            (
                temp.SetVariable(Element.Part<V_RoundToInteger>(
                    Element.Part<V_Add>(
                        Element.Part<V_DistanceBetween>(
                            Element.Part<V_NearestWalkablePosition>(
                                Element.Part<V_Vector>(new V_Number(-500.000), new V_Number(0), new V_Number(0))
                            ),
                            Element.Part<V_NearestWalkablePosition>(Element.Part<V_Vector>(new V_Number(500), new V_Number(0), new V_Number(0)))
                        ),
                        Element.Part<V_DistanceBetween>(
                            Element.Part<V_NearestWalkablePosition>(Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(-500.000))),
                            Element.Part<V_NearestWalkablePosition>(Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(500)))
                        )
                    ),
                    EnumData.GetEnumValue(Rounding.Down)
                )),

                temp.SetVariable(Element.Part<V_IndexOfArrayValue>(
                    Element.Part<V_Append>(
                        Element.Part<V_Append>(
                            Element.Part<V_Append>(
                                Element.Part<V_Append>(
                                    Element.Part<V_Append>(
                                        Element.Part<V_Append>(
                                            Element.Part<V_Append>(
                                                Element.Part<V_Append>(
                                                    Element.Part<V_Append>(
                                                        Element.Part<V_Append>(
                                                            Element.Part<V_Append>(
                                                                Element.Part<V_Append>(
                                                                    Element.Part<V_Append>(
                                                                        Element.Part<V_Append>(
                                                                            Element.Part<V_Append>(
                                                                                Element.Part<V_Append>(
                                                                                    Element.Part<V_Append>(
                                                                                        Element.Part<V_Append>(
                                                                                            Element.Part<V_Append>(
                                                                                                Element.Part<V_Append>(
                                                                                                    Element.Part<V_Append>(
                                                                                                        Element.Part<V_Append>(
                                                                                                            Element.Part<V_Append>(
                                                                                                                Element.Part<V_Append>(
                                                                                                                    Element.Part<V_Append>(
                                                                                                                        Element.Part<V_Append>(
                                                                                                                            Element.Part<V_Append>(
                                                                                                                                Element.Part<V_Append>(
                                                                                                                                    Element.Part<V_Append>(
                                                                                                                                        Element.Part<V_Append>(
                                                                                                                                            Element.Part<V_Append>(
                                                                                                                                                Element.Part<V_Append>(
                                                                                                                                                    Element.Part<V_Append>(
                                                                                                                                                        Element.Part<V_Append>(
                                                                                                                                                            Element.Part<V_Append>(
                                                                                                                                                                Element.Part<V_Append>(
                                                                                                                                                                    Element.Part<V_Append>(
                                                                                                                                                                        Element.Part<V_Append>(
                                                                                                                                                                            Element.Part<V_Append>(
                                                                                                                                                                                Element.Part<V_Append>(
                                                                                                                                                                                    Element.Part<V_Append>(
                                                                                                                                                                                        Element.Part<V_Append>(Element.Part<V_EmptyArray>(), new V_Number(153)),
                                                                                                                                                                                    new V_Number(468)),
                                                                                                                                                                                new V_Number(1196)),
                                                                                                                                                                            new V_Number(135)),
                                                                                                                                                                        new V_Number(139)),
                                                                                                                                                                    new V_Number(477)),
                                                                                                                                                                new V_Number(184)),
                                                                                                                                                                Element.Part<V_FirstOf>(
                                                                                                                                                                    Element.Part<V_FilteredArray>(
                                                                                                                                                                        Element.Part<V_Append>(
                                                                                                                                                                            Element.Part<V_Append>(Element.Part<V_EmptyArray>(), new V_Number(343)),
                                                                                                                                                                        new V_Number(347)),
                                                                                                                                                                        Element.Part<V_Compare>(
                                                                                                                                                                            Element.Part<V_ArrayElement>(),
                                                                                                                                                                            EnumData.GetEnumValue(Operators.Equal),
                                                                                                                                                                            temp.GetVariable()
                                                                                                                                                                        )
                                                                                                                                                                    )
                                                                                                                                                                )
                                                                                                                                                            ),
                                                                                                                                                            new V_Number(366)
                                                                                                                                                        ),
                                                                                                                                                        Element.Part<V_FirstOf>(
                                                                                                                                                            Element.Part<V_FilteredArray>(
                                                                                                                                                                Element.Part<V_Append>(
                                                                                                                                                                    Element.Part<V_Append>(Element.Part<V_EmptyArray>(), new V_Number(433)),
                                                                                                                                                                    new V_Number(436)
                                                                                                                                                                ),
                                                                                                                                                                Element.Part<V_Compare>(
                                                                                                                                                                    Element.Part<V_ArrayElement>(),
                                                                                                                                                                    EnumData.GetEnumValue(Operators.Equal),
                                                                                                                                                                    temp.GetVariable()
                                                                                                                                                                )
                                                                                                                                                            )
                                                                                                                                                        )
                                                                                                                                                    ),
                                                                                                                                                new V_Number(403)),
                                                                                                                                                Element.Part<V_FirstOf>(
                                                                                                                                                    Element.Part<V_FilteredArray>(
                                                                                                                                                        Element.Part<V_Append>(
                                                                                                                                                            Element.Part<V_Append>(Element.Part<V_EmptyArray>(), new V_Number(382)),
                                                                                                                                                            new V_Number(384)
                                                                                                                                                        ),
                                                                                                                                                        Element.Part<V_Compare>(
                                                                                                                                                            Element.Part<V_ArrayElement>(),
                                                                                                                                                            EnumData.GetEnumValue(Operators.Equal),
                                                                                                                                                            temp.GetVariable()
                                                                                                                                                        )
                                                                                                                                                    )
                                                                                                                                                )
                                                                                                                                            ),
                                                                                                                                        new V_Number(993)),
                                                                                                                                    new V_Number(386)),
                                                                                                                                    Element.Part<V_FirstOf>(
                                                                                                                                        Element.Part<V_FilteredArray>(
                                                                                                                                            Element.Part<V_Append>(
                                                                                                                                                Element.Part<V_Append>(Element.Part<V_EmptyArray>(), new V_Number(331)),
                                                                                                                                                new V_Number(348)
                                                                                                                                            ),
                                                                                                                                            Element.Part<V_Compare>(
                                                                                                                                                Element.Part<V_ArrayElement>(),
                                                                                                                                                EnumData.GetEnumValue(Operators.Equal),
                                                                                                                                                temp.GetVariable()
                                                                                                                                            )
                                                                                                                                        )
                                                                                                                                    )
                                                                                                                                ),
                                                                                                                                new V_Number(659)),
                                                                                                                            new V_Number(145)),
                                                                                                                        new V_Number(569)),
                                                                                                                    new V_Number(384)),
                                                                                                                new V_Number(1150)),
                                                                                                            new V_Number(371)),
                                                                                                        new V_Number(179)),
                                                                                                    new V_Number(497)),
                                                                                                new V_Number(374)),
                                                                                            new V_Number(312)),
                                                                                        new V_Number(324)),
                                                                                    new V_Number(434)),
                                                                                new V_Number(297)),
                                                                            new V_Number(276)),
                                                                        new V_Number(330)),
                                                                    new V_Number(376)),
                                                                new V_Number(347)),
                                                            new V_Number(480)),
                                                        new V_Number(310)),
                                                    new V_Number(342)),
                                                new V_Number(360)),
                                            new V_Number(364)),
                                        new V_Number(372)),
                                    new V_Number(370)),
                                new V_Number(450)),
                            new V_Number(356)),
                        new V_Number(305)),
                    temp.GetVariable())
                )
            );

            return new MethodResult(actions, temp.GetVariable());
        }
    
        public override WikiMethod Wiki()
        {
            return new WikiMethod("GetMap", "Gets the current map. The result can be compared with the Map enum.", null);
        }
    }

    [CustomMethod("ChaseVariable", CustomMethodType.Action)]
    [VarRefParameter("Variable")]
    [Parameter("Destination", ValueType.Number | ValueType.Vector, null)]
    [Parameter("Rate", ValueType.Number, null)]
    class ChaseVariable : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            VarRef targetVariable = (VarRef)Parameters[0];
            Element destination = (Element)Parameters[1];
            Element rate = (Element)Parameters[2];
            
            VariableChase chaseData;
            if (targetVariable.Var.IsGlobal)
                chaseData = TranslateContext.ParserData.GlobalLoop.GetChaseData(targetVariable.Var);
            else
                chaseData = TranslateContext.ParserData.PlayerLoop.GetChaseData(targetVariable.Var);
            
            Element[] actions = ElementBuilder
            (
                chaseData.Destination.SetVariable(destination, targetVariable.Target),
                chaseData.Rate.SetVariable(rate, targetVariable.Target)
            );

            return new MethodResult(actions, null);
        }
    
        public override WikiMethod Wiki()
        {
            return new WikiMethod("ChaseVariable", "Chases a variable to a value. Works with numbers and vectors.", 
                new WikiParameter[]
                {
                    new WikiParameter("Variable", "Variable that will chase the destination."),
                    new WikiParameter("Destination", "The final variable destination. Can be a number or vector."),
                    new WikiParameter("Rate", "The chase speed per second.")
                }
            );
        }
    }

    [CustomMethod("MinWait", CustomMethodType.Action)]
    class MinWait : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            return new MethodResult(new Element[] { A_Wait.MinimumWait }, null);
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("MinWait", "Waits for " + Constants.MINIMUM_WAIT + " milliseconds.", null);
        }
    }

    [CustomMethod("RemoveFromArrayAtIndex", CustomMethodType.Value)]
    [Parameter("Array", ValueType.Any, null)]
    [Parameter("Index", ValueType.Number, null)]
    class RemoveFromArrayAtIndex : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element array = (Element)Parameters[0];
            Element index = (Element)Parameters[1];

            return new MethodResult(null, 
                Element.Part<V_Append>(
                    Element.Part<V_ArraySlice>(array, new V_Number(0), index),
                    Element.Part<V_ArraySlice>(array, Element.Part<V_Add>(index, new V_Number(1)), V_Number.LargeArbitraryNumber)
                )
            );
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("RemoveFromArrayAtIndex", "Removes a value from an array by it's index.",
                new WikiParameter[]
                {
                    new WikiParameter("Array", "The array to modify."),
                    new WikiParameter("Index", "The index to remove.")
                }
            );
        }
    }

    [CustomMethod("InsertValueInArray", CustomMethodType.Value)]
    [Parameter("Array", ValueType.Any, null)]
    [Parameter("Index", ValueType.Number, null)]
    [Parameter("Value", ValueType.Any, null)]
    class InsertValueInArray : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element array = (Element)Parameters[0];
            Element index = (Element)Parameters[1];
            Element value = (Element)Parameters[2];

            return new MethodResult(null,
                Element.Part<V_Append>(
                    Element.Part<V_Append>(
                        Element.Part<V_ArraySlice>(array, new V_Number(0), index),
                        value
                    ),
                    Element.Part<V_ArraySlice>(array, index, V_Number.LargeArbitraryNumber)
                )
            );
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("InsertValueInArray", "Inserts a value into an array.",
                new WikiParameter[]
                {
                    new WikiParameter("Array", "The array to modify."),
                    new WikiParameter("Index", "Where to insert the value."),
                    new WikiParameter("Value", "The value to insert.")
                }
            );
        }
    }
}
