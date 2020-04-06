using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    public abstract class DijkstraBase
    {
        private static readonly V_Number Infinity = new V_Number(9999);
        private static readonly V_Number LeastNot0 = new V_Number(0.0001);

        protected ActionSet actionSet { get; }
        protected Element pathmapObject { get; }
        protected Element Nodes { get; }
        protected Element Segments { get; }
        protected Element position { get; }
        private Element attributes { get; }
        protected bool useAttributes { get; }
        private bool reversed { get; }

        protected IndexReference unvisited { get; private set; }
        private IndexReference current { get; set; }
        private IndexReference parentArray { get; set; }
        private IndexReference parentAttributeInfo { get; set; }

        protected static bool assignExtended = false;
        public DijkstraBase(ActionSet actionSet, Element pathmapObject, Element position, Element attributes, bool reversed)
        {
            this.actionSet = actionSet;
            this.pathmapObject = pathmapObject;
            this.position = position;
            this.attributes = attributes;
            this.useAttributes = attributes != null && attributes is V_EmptyArray == false;
            this.reversed = reversed;

            PathmapClass pathmapClass = actionSet.Translate.DeltinScript.Types.GetCodeType<PathmapClass>();

            Nodes = ((Element)pathmapClass.Nodes.GetVariable())[pathmapObject];
            Segments = ((Element)pathmapClass.Segments.GetVariable())[pathmapObject];
        }

        public void Get()
        {
            var firstNode = ClosestNodeToPosition(Nodes, position);

            Assign();
            
            current                          = actionSet.VarCollection.Assign("Dijkstra: Current", actionSet.IsGlobal, assignExtended);
            IndexReference distances         = actionSet.VarCollection.Assign("Dijkstra: Distances", actionSet.IsGlobal, false);
            unvisited                        = actionSet.VarCollection.Assign("Dijkstra: Unvisited", actionSet.IsGlobal, false);
            IndexReference connectedSegments = actionSet.VarCollection.Assign("Dijkstra: Connected Segments", actionSet.IsGlobal, assignExtended);
            IndexReference neighborIndex     = actionSet.VarCollection.Assign("Dijkstra: Neighbor Index", actionSet.IsGlobal, assignExtended);
            IndexReference neighborDistance  = actionSet.VarCollection.Assign("Dijkstra: Distance", actionSet.IsGlobal, assignExtended);
            parentArray                      = actionSet.VarCollection.Assign("Dijkstra: Parent Array", actionSet.IsGlobal, false);
            if (useAttributes)
                parentAttributeInfo = actionSet.VarCollection.Assign("Dijkstra: Parent Attribute Info", actionSet.IsGlobal, false);

            // Set the current variable as the first node.
            actionSet.AddAction(current.SetVariable(firstNode));
            SetInitialDistances(actionSet, distances, (Element)current.GetVariable());
            SetInitialUnvisited(actionSet, Nodes, unvisited);

            actionSet.AddAction(Element.Part<A_While>(LoopCondition()));

            // !WAIT actionSet.AddAction(A_Wait.MinimumWait);

            // Get neighboring indexes
            actionSet.AddAction(connectedSegments.SetVariable(GetConnectedSegments(
                Nodes,
                Segments,
                (Element)current.GetVariable(),
                reversed
            )));

            // Loop through neighboring indexes
            ForeachBuilder forBuilder = new ForeachBuilder(actionSet, connectedSegments.GetVariable());

            actionSet.AddAction(A_Wait.MinimumWait);

            actionSet.AddAction(ArrayBuilder<Element>.Build(
                // Get the index from the segment data
                neighborIndex.SetVariable(
                    Element.TernaryConditional(
                        new V_Compare(
                            current.GetVariable(),
                            Operators.NotEqual,
                            Node1(forBuilder.IndexValue)
                        ),
                        Node1(forBuilder.IndexValue),
                        Node2(forBuilder.IndexValue)
                    )
                ),

                // Get the distance between the current and the neighbor index.
                neighborDistance.SetVariable(
                    Element.Part<V_DistanceBetween>(
                        Nodes[(Element)neighborIndex.GetVariable()],
                        Nodes[(Element)current.GetVariable()]
                    ) + ((Element)distances.GetVariable())[(Element)current.GetVariable()]
                )
            ));

            // Set the current neighbor's distance if the new distance is less than what it is now.
            actionSet.AddAction(Element.Part<A_If>(
                (Element)neighborDistance.GetVariable()
                <
                WorkingDistance((Element)distances.GetVariable(), (Element)neighborIndex.GetVariable())
            ));

            actionSet.AddAction(distances.SetVariable((Element)neighborDistance.GetVariable(), null, (Element)neighborIndex.GetVariable()));
            actionSet.AddAction(parentArray.SetVariable((Element)current.GetVariable() + 1, null, (Element)neighborIndex.GetVariable()));

            if (useAttributes)
                actionSet.AddAction(parentAttributeInfo.SetVariable(
                    Element.TernaryConditional(
                        new V_Compare(
                            current.GetVariable(),
                            Operators.Equal,
                            Node1(forBuilder.IndexValue)
                        ),
                        Node1Attribute(forBuilder.IndexValue),
                        Node2Attribute(forBuilder.IndexValue)
                    ),
                    null,
                    (Element)neighborIndex.GetVariable()
                ));

            actionSet.AddAction(new A_End());
            forBuilder.Finish();

            actionSet.AddAction(ArrayBuilder<Element>.Build(
                // Add the current to the visited array.
                unvisited.SetVariable(Element.Part<V_RemoveFromArray>(unvisited.GetVariable(), current.GetVariable())),

                // Set the current node as the smallest unvisited.
                current.SetVariable(LowestUnvisited(Nodes, (Element)distances.GetVariable(), (Element)unvisited.GetVariable()))
            ));

            actionSet.AddAction(new A_End());

            GetResult();

            actionSet.AddAction(ArrayBuilder<Element>.Build(
                current.SetVariable(-1),
                distances.SetVariable(-1),
                connectedSegments.SetVariable(-1),
                neighborIndex.SetVariable(-1),
                neighborDistance.SetVariable(-1),
                parentArray.SetVariable(-1),
                parentAttributeInfo.SetVariable(-1)
            ));

            Reset();
        }

        protected abstract void Assign();
        protected abstract Element LoopCondition();
        protected abstract void GetResult();
        protected abstract void Reset();

        protected void Backtrack(Element destination, IndexReference finalPath, IndexReference finalPathAttributes)
        {
            actionSet.AddAction(current.SetVariable(destination));
            actionSet.AddAction(finalPath.SetVariable(new V_EmptyArray()));

            // Get the path.
            actionSet.AddAction(Element.Part<A_While>(new V_Compare(
                current.GetVariable(),
                Operators.GreaterThanOrEqual,
                new V_Number(0)
            )));

            // !WAIT actionSet.AddAction(A_Wait.MinimumWait);

            Element next = Nodes[(Element)current.GetVariable()];
            Element array = (Element)finalPath.GetVariable();
            Element first;
            Element second;

            Element nextAttribute = null; 
            Element attributeArray = null;
            Element firstAttribute = null;
            Element secondAttribute = null;

            if (useAttributes)
            {
                nextAttribute = ((Element)parentAttributeInfo.GetVariable())[(Element)current.GetVariable()];
                attributeArray = (Element)finalPathAttributes.GetVariable();
            }

            if (!reversed)
            {
                first = next;
                second = array;
                if (useAttributes)
                {
                    firstAttribute = nextAttribute;
                    secondAttribute = attributeArray;
                }
            }
            else
            {
                first = array;
                second = next;
                if (useAttributes)
                {
                    firstAttribute = attributeArray;
                    secondAttribute = nextAttribute;
                }
            }

            // For debugging generated path.
            // actionSet.AddAction(Element.Part<A_CreateEffect>(
            //     Element.Part<V_AllPlayers>(),
            //     EnumData.GetEnumValue(Effect.Orb),
            //     EnumData.GetEnumValue(Color.SkyBlue),
            //     next,
            //     new V_Number(0.5),
            //     EnumData.GetEnumValue(EffectRev.VisibleTo)
            // ));

            actionSet.AddAction(finalPath.SetVariable(Element.Part<V_Append>(first, second)));
            if (useAttributes)
                actionSet.AddAction(finalPathAttributes.SetVariable(Element.Part<V_Append>(firstAttribute, secondAttribute)));
            actionSet.AddAction(current.SetVariable(Element.Part<V_ValueInArray>(parentArray.GetVariable(), current.GetVariable()) - 1));
            actionSet.AddAction(new A_End());
        }

        protected static Element ClosestNodeToPosition(Element nodes, Element position)
        {
            return Element.Part<V_IndexOfArrayValue>(
                nodes,
                Element.Part<V_FirstOf>(
                    Element.Part<V_SortedArray>(
                        nodes,
                        Element.Part<V_DistanceBetween>(
                            position,
                            new V_ArrayElement()
                        )
                    )
                )
            );
        }

        private static void SetInitialDistances(ActionSet actionSet, IndexReference distancesVar, Element currentIndex)
        {
            actionSet.AddAction(distancesVar.SetVariable(LeastNot0, null, currentIndex));
        }

        private static void SetInitialUnvisited(ActionSet actionSet, Element nodeArray, IndexReference unvisitedVar)
        {
            // Create an array counting up to the number of values in the nodeArray array.
            // For example, if nodeArray has 6 variables unvisitedVar will be set to [0, 1, 2, 3, 4, 5].

            // Empty the unvisited array.
            actionSet.AddAction(unvisitedVar.SetVariable(new V_EmptyArray()));
            
            IndexReference current = actionSet.VarCollection.Assign("unvisitedBuilder", actionSet.IsGlobal, assignExtended);
            actionSet.AddAction(current.SetVariable(0));

            // While current < the count of the node array.
            actionSet.AddAction(Element.Part<A_While>((Element)current.GetVariable() < Element.Part<V_CountOf>(nodeArray)));

            actionSet.AddAction(unvisitedVar.ModifyVariable(Operation.AppendToArray, (Element)current.GetVariable()));
            actionSet.AddAction(current.ModifyVariable(Operation.Add, 1));

            // End the while.
            actionSet.AddAction(new A_End());
        }

        private Element GetConnectedSegments(Element nodes, Element segments, Element currentIndex, bool reversed)
        {
            Element currentSegmentCheck = new V_ArrayElement();

            Element useAttribute = Element.TernaryConditional(
                new V_Compare(Node1(currentSegmentCheck), Operators.Equal, currentIndex),
                Node1Attribute(currentSegmentCheck),
                Node2Attribute(currentSegmentCheck)
            );

            Element isValid;
            if (useAttributes)
                isValid = Element.Part<V_Or>(
                    new V_Compare(useAttribute, Operators.Equal, new V_Number(0)),
                    Element.Part<V_ArrayContains>(
                        attributes,
                        useAttribute
                    )
                );
            else
                isValid = new V_Compare(useAttribute, Operators.Equal, new V_Number(0));

            return Element.Part<V_FilteredArray>(
                segments,
                Element.Part<V_And>(
                    // Make sure one of the segments nodes is the current node.
                    Element.Part<V_ArrayContains>(
                        BothNodes(currentSegmentCheck),
                        currentIndex
                    ),
                    isValid
                )
            );
        }

        private static Element LowestUnvisited(Element nodes, Element distances, Element unvisited)
        {
            return Element.Part<V_FirstOf>(Element.Part<V_SortedArray>(
                unvisited,
                WorkingDistance(
                    distances,
                    new V_ArrayElement()
                )
            ));
        }

        private static Element WorkingDistance(Element distances, Element index)
        {
            // Return infinity if the distance is unassigned.
            return Element.TernaryConditional(
                new V_Compare(distances[index], Operators.NotEqual, new V_Number(0)),
                distances[index],
                Infinity
            );
        }

        private static Element BothNodes(Element segment)
        {
            return Element.CreateArray(Node1(segment), Node2(segment));
        }
        private static Element Node1(Element segment)
        {
            return Element.Part<V_RoundToInteger>(Element.Part<V_XOf>(segment), EnumData.GetEnumValue(Rounding.Down));
        }
        private static Element Node2(Element segment)
        {
            return Element.Part<V_RoundToInteger>(Element.Part<V_YOf>(segment), EnumData.GetEnumValue(Rounding.Down));
        }
        private static Element Node1Attribute(Element segment)
        {
            return Element.Part<V_RoundToInteger>(
                (Element.Part<V_XOf>(segment) % 1) * 100,
                EnumData.GetEnumValue(Rounding.Nearest)
            );
        }
        private static Element Node2Attribute(Element segment)
        {
            return Element.Part<V_RoundToInteger>(
                (Element.Part<V_YOf>(segment) % 1) * 100,
                EnumData.GetEnumValue(Rounding.Nearest)
            );
        }

        public static void Pathfind(ActionSet actionSet, PathfinderInfo info, Element pathResult, Element target, Element destination, Element pathAttributes)
        {
            actionSet.AddAction(info.Path.SetVariable(
                Element.Part<V_Append>(pathResult, destination),
                target
            ));
            actionSet.AddAction(info.PathAttributes.SetVariable(
                pathAttributes,
                target
            ));
        }
    }

    public class DijkstraNormal : DijkstraBase
    {
        private Element destination { get; }
        private IndexReference finalNode;
        public IndexReference finalPath { get; private set; }
        public IndexReference finalPathAttributes { get; private set; }

        public DijkstraNormal(ActionSet actionSet, Element pathmapObject, Element position, Element destination, Element attributes) : base(actionSet, pathmapObject, position, attributes, false)
        {
            this.destination = destination;
        }

        override protected void Assign()
        {
            var lastNode = ClosestNodeToPosition(Nodes, destination);

            finalNode = actionSet.VarCollection.Assign("Dijkstra: Last", actionSet.IsGlobal, assignExtended);
            finalPath = actionSet.VarCollection.Assign("Dijkstra: Final Path", actionSet.IsGlobal, false);
            if (useAttributes)
                finalPathAttributes = actionSet.VarCollection.Assign("Dijkstra: Final Path Attributes", actionSet.IsGlobal, false);
            actionSet.AddAction(finalNode.SetVariable(lastNode));
            actionSet.AddAction(finalPath.SetVariable(new V_EmptyArray()));
        }

        override protected Element LoopCondition()
        {
            return Element.Part<V_ArrayContains>(
                unvisited.GetVariable(),
                finalNode.GetVariable()
            );
        }

        override protected void GetResult()
        {
            Backtrack((Element)finalNode.GetVariable(), finalPath, finalPathAttributes);
        }

        override protected void Reset()
        {
            actionSet.AddAction(ArrayBuilder<Element>.Build(
                finalNode.SetVariable(-1)
            ));
        }
    }
    
    public class DijkstraMultiSource : DijkstraBase
    {
        private PathfinderInfo pathfinderInfo { get; }
        private Element players { get; }
        private IndexReference closestNodesToPlayers;

        public DijkstraMultiSource(ActionSet actionSet, PathfinderInfo pathfinderInfo, Element pathmapObject, Element players, Element destination, Element attributes) : base(actionSet, pathmapObject, destination, attributes, true)
        {
            this.pathfinderInfo = pathfinderInfo;
            this.players = players;
        }

        override protected void Assign()
        {
            closestNodesToPlayers = actionSet.VarCollection.Assign("Dijkstra: Closest nodes", actionSet.IsGlobal, false);
            actionSet.AddAction(closestNodesToPlayers.SetVariable(Element.Part<V_EmptyArray>()));

            ForeachBuilder getClosestNodes = new ForeachBuilder(actionSet, players);

            actionSet.AddAction(closestNodesToPlayers.SetVariable(
                Element.Part<V_Append>(
                    closestNodesToPlayers.GetVariable(),
                    ClosestNodeToPosition(Nodes, getClosestNodes.IndexValue)
                )
            ));

            getClosestNodes.Finish();
        }

        override protected Element LoopCondition()
        {
            return Element.Part<V_IsTrueForAny>(
                closestNodesToPlayers.GetVariable(),
                Element.Part<V_ArrayContains>(
                    unvisited.GetVariable(),
                    new V_ArrayElement()
                )
            );
        }

        override protected void GetResult()
        {
            ForeachBuilder assignPlayerPaths = new ForeachBuilder(actionSet, players);
            actionSet.AddAction(A_Wait.MinimumWait);

            IndexReference finalPath = actionSet.VarCollection.Assign("Dijkstra: Final Path", actionSet.IsGlobal, false);
            IndexReference finalPathAttributes = actionSet.VarCollection.Assign("Dijkstra: Final Path Attributes", actionSet.IsGlobal, false);

            Backtrack(
                Element.Part<V_ValueInArray>(
                    closestNodesToPlayers.GetVariable(),
                    assignPlayerPaths.Index
                ),
                finalPath,
                finalPathAttributes
            );
            Pathfind(actionSet, pathfinderInfo, (Element)finalPath.GetVariable(), assignPlayerPaths.IndexValue, position, (Element)finalPathAttributes.GetVariable());
            assignPlayerPaths.Finish();
        }

        override protected void Reset()
        {
        }
    }
}