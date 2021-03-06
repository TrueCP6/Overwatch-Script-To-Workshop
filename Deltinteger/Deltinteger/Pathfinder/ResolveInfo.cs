using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    public class ResolveInfoComponent : IComponent
    {
        public const double DefaultMoveToNext = 0.4;

        public DeltinScript DeltinScript { get; set; }
        public bool TrackTimeSinceLastNode { get; set; } // This will be true if the Pathmap.IsPathfindingStuck function is called anywhere in the code.
        public bool SaveLastCurrent { get; set; } // This will be true if the Pathmap.IsPathfindingToSegment function is called anywhere in the code.
        public bool PotentiallyNullNodes { get; set; } // Determines if nodes can potentially be null.
        
        // Class Instances
        private PathResolveClass PathResolveInstance;
        private PathmapClass PathmapInstance;

        // Workshop Variables
        private IndexReference DoGetCurrent { get; set; } // When set to true, the current node is reset to the closest node.
        private IndexReference Current { get; set; } // The index of the current node that the player is walking to.
        private IndexReference PathmapReference { get; set; } // A reference to the pathmap being used to pathfind.
        private IndexReference ParentArray { get; set; } // Stores the parent path array.
        private IndexReference AttributeArray { get; set; } // Stores the parent path attribute array.
        private IndexReference Destination { get; set; } // The destination to walk to after all nodes have been transversed.
        public IndexReference CurrentAttribute { get; set; } // The current pathfinding attribute.
        
        // Stuck dection workshop variables. These are only assigned if 'TrackTimeSinceLastNode' is true.
        private IndexReference DistanceToNextNode { get; set; } // The distance from the player to the next node.
        private IndexReference TimeSinceLastNode { get; set; } // The time since the last node was reached.

        // Hooks to override ostw generated code.
        public LambdaAction OnPathStart { get; set; } // The code to run when a path starts.
        public LambdaAction OnNodeReached { get; set; } // The code to run when a node is reached.
        public LambdaAction OnPathCompleted { get; set; } // The code to run when a path is completed.
        public LambdaAction IsNodeReachedDeterminer { get; set; } // The condition that determines wether or not the current node was reached.
        public LambdaAction ApplicableNodeDeterminer { get; set; } // The function used to get the closest node to the player.

        public void Init()
        {
            bool assignExtended = false;

            // Assign workshop variables.
            DoGetCurrent = DeltinScript.VarCollection.Assign("pathfinderDoGetCurrent", false, assignExtended);
            Current = DeltinScript.VarCollection.Assign("pathfinderCurrent", false, assignExtended);
            PathmapReference = DeltinScript.VarCollection.Assign("pathmapReference", false, assignExtended);
            ParentArray = DeltinScript.VarCollection.Assign("parentArray", false, assignExtended);
            AttributeArray = DeltinScript.VarCollection.Assign("attributeArray", false, assignExtended);
            Destination = DeltinScript.VarCollection.Assign("destination", false, assignExtended);
            CurrentAttribute = DeltinScript.VarCollection.Assign("lastAttribute", false, assignExtended);

            if (TrackTimeSinceLastNode)
            {
                DistanceToNextNode = DeltinScript.VarCollection.Assign("distanceToNextNode", false, assignExtended);
                TimeSinceLastNode = DeltinScript.VarCollection.Assign("timeSinceLastNode", false, assignExtended);
            }

            // Get the PathResolve instance and the Pathmap instance.
            PathResolveInstance = DeltinScript.Types.GetInstance<PathResolveClass>();
            PathmapInstance = DeltinScript.Types.GetInstance<PathmapClass>();

            // Get the resolve subroutine.
            GetResolveRoutine();
        }

        private void GetResolveRoutine()
        {
            // Create the rule that will get the closest node.
            TranslateRule getResolveRule = new TranslateRule(DeltinScript, "Pathfinder: Resolve Current", RuleEvent.OngoingPlayer);
            // The rule will activate when DoGetCurrent is set to true.
            getResolveRule.Conditions.Add(new Condition((Element)DoGetCurrent.GetVariable(), Operators.Equal, new V_True()));
            // Set the Current variable to the closest node.
            getResolveRule.ActionSet.AddAction(Current.SetVariable(ClosestNode(getResolveRule.ActionSet, PlayerPosition())));

            // If the OnPathStart hook is null, do the default which is throttling the player to the next node.
            if (OnPathStart == null)
                // Start throttle to the current node.
                ThrottleEventPlayerToNextNode(getResolveRule.ActionSet);
            // Otherwise, use the hook.
            else
                OnPathStart.Invoke(getResolveRule.ActionSet);
            
            // Update IsPathfindStuck data.
            UpdateStuckDetector(getResolveRule.ActionSet);

            // Reset DoGetCurrent to false.
            getResolveRule.ActionSet.AddAction(DoGetCurrent.SetVariable(new V_False()));

            // Add the rule.
            DeltinScript.WorkshopRules.Add(getResolveRule.GetRule());

            // Resolve the rule that increments the current node.

            // The 'next' rule will set current to the next node index when the current node is reached. 
            TranslateRule next = new TranslateRule(DeltinScript, "Pathfinder: Resolve Next", RuleEvent.OngoingPlayer);
            next.Conditions.Add(NodeReachedCondition(next.ActionSet));
            next.Conditions.Add(new Condition(ParentArray.Get(), Operators.NotEqual, new V_Null()));

            if (OnPathCompleted == null || !OnPathCompleted.EmptyBlock)
                next.ActionSet.AddAction(Element.Part<A_If>(new V_Compare(Current.Get(), Operators.NotEqual, new V_Number(-1))));

            // Get last attribute.
            next.ActionSet.AddAction(CurrentAttribute.SetVariable(NextSegmentAttribute(new V_EventPlayer())));

            // Set current as the current's parent.
            next.ActionSet.AddAction(Current.SetVariable(ParentArray.Get()[Current.Get()] - 1));

            // Update stuck
            UpdateStuckDetector(next.ActionSet);

            // Invoke OnNodeReached
            OnNodeReached?.Invoke(next.ActionSet);

            if (OnPathCompleted == null || !OnPathCompleted.EmptyBlock) next.ActionSet.AddAction(Element.Part<A_Else>());

            if (OnPathCompleted == null)
            {
                next.ActionSet.AddAction(Element.Part<A_StopThrottleInDirection>(new V_EventPlayer()));
                StopPathfinding(next.ActionSet, new V_EventPlayer());
            }
            else if (!OnPathCompleted.EmptyBlock) OnPathCompleted.Invoke(next.ActionSet);

            if (OnPathCompleted == null || !OnPathCompleted.EmptyBlock) next.ActionSet.AddAction(Element.Part<A_End>());

            // Add rule
            DeltinScript.WorkshopRules.Add(next.GetRule());
        }

        /// <summary>Gets the closest node the player is pathfinding with.</summary>
        public void SetCurrent(ActionSet actionSet, Element players)
        {
            actionSet.AddAction(DoGetCurrent.SetVariable(value: new V_True(), targetPlayer: players));
        }

        /// <summary>Gets the closest node from a position.</summary>
        public Element ClosestNode(ActionSet actionSet, Element position)
        {
            // Get the nodes in the pathmap
            Element nodes = Element.Part<V_ValueInArray>(PathmapInstance.Nodes.GetVariable(), PathmapReference.GetVariable());

            // Get the closest node index.
            if (ApplicableNodeDeterminer == null)
                return DijkstraBase.ClosestNodeToPosition(nodes, position, PotentiallyNullNodes);
            else
                return (Element)ApplicableNodeDeterminer.Invoke(actionSet, nodes, position);
        }

        /// <summary>The position of the current node the player is walking towards.</summary>
        public Element CurrentPosition(Element player = null) => PathmapInstance.Nodes.Get()[PathmapReference.Get()][Current.Get(player)];

        public Element CurrentPositionWithDestination(Element player = null) => Element.TernaryConditional(
            // Current will be -1 if the player reached the last node.
            new V_Compare(Current.GetVariable(player), Operators.Equal, new V_Number(-1)),
            // If so, go to the destination.
            Destination.GetVariable(player),
            // Otherwise, go to the current node.
            CurrentPosition(player)
        );

        /// <summary>The position of the current player: `Position Of(Event Player)`</summary>
        private Element PlayerPosition() => Element.Part<V_PositionOf>(Element.Part<V_EventPlayer>());

        /// <summary>Starts pathfinding for the specified players/</summary>
        /// <param name="actionSet">The actionset of the current rule.</param>
        /// <param name="players">The players that will start pathfinding.</param>
        /// <param name="pathmapReference">A reference to the pathmap the players are pathfinding with.</param>
        /// <param name="parentArray">The parent array path.</param>
        /// <param name="attributeArray">The path attributes.</param>
        /// <param name="destination">The destination the players are navigating to.</param>
        public void Pathfind(ActionSet actionSet, Element players, Element pathmapReference, Element parentArray, Element attributeArray, Element destination)
        {
            // Set target's pathmap reference.
            actionSet.AddAction(PathmapReference.SetVariable(
                value: pathmapReference,
                targetPlayer: players
            ));
            
            // Set target's parent array.
            actionSet.AddAction(ParentArray.SetVariable(
                value: parentArray,
                targetPlayer: players
            ));

            // Set target's attribute array.
            actionSet.AddAction(AttributeArray.SetVariable(
                value: attributeArray,
                targetPlayer: players
            ));

            // Set target's destination.
            actionSet.AddAction(Destination.SetVariable(
                value: destination,
                targetPlayer: players
            ));

            // For each of the players, get the current.
            SetCurrent(actionSet, players);
        }

        /// <summary>Stops pathfinding for the specified players.</summary>
        /// <param name="actionSet">The actionset of the current rule.</param>
        /// <param name="players">The players to stop pathfinding for.</param>
        public void StopPathfinding(ActionSet actionSet, Element players) => actionSet.AddAction(ParentArray.SetVariable(value: new V_Null(), targetPlayer: players));

        /// <summary>Stops pathfinding for the players that are pathfinding using the specified pathmap reference.</summary>
        /// <param name="actionSet">The actionset of the current rule.</param>
        /// <param name="pathmapReference">The reference of the pathmap. Any players using this pathmap will stop pathfinding.</param>
        /// <param name="players">The players to stop pathfinding for.</param>
        public void StopPathfindingWithPathmap(ActionSet actionSet, Element pathmapReference, Element players) =>
            StopPathfinding(
                actionSet,
                // Filter players by whos pathfinding reference is equal to pathmapReference.
                Element.Part<V_FilteredArray>(
                    players,
                    new V_Compare(
                        PathmapReference.GetVariable(new V_ArrayElement()),
                        Operators.Equal,
                        pathmapReference
                    )
                )
            );
    
        /// <summary>Determines if the target player is pathfinding.</summary>
        public Element IsPathfinding(Element player) => new V_Compare(ParentArray.GetVariable(player), Operators.NotEqual, new V_Null());

        /// <summary>Returns true if the player takes longer than expected to reach the next node.</summary>
        public Element IsPathfindingStuck(Element player, Element scalar)
        {
            Element leniency = 2;

            Element defaultSpeed = 5.5;
            Element nodeDistance = DistanceToNextNode.Get(player);
            Element timeSinceLastNode = new V_TotalTimeElapsed() - TimeSinceLastNode.Get(player);
            
            Element isStuck = new V_Compare(
                nodeDistance - ((defaultSpeed * scalar * timeSinceLastNode) / leniency),
                Operators.LessThanOrEqual,
                new V_Number(0)
            );
            return Element.Part<V_And>(IsPathfinding(player), isStuck);
        }
    
        /// <summary>Updates stuck detector.</summary>
        private void UpdateStuckDetector(ActionSet actionSet)
        {
            if (!TrackTimeSinceLastNode) return; // Do nothing if TrackTimeSinceLastNode is set to false.
            actionSet.AddAction(TimeSinceLastNode.SetVariable(new V_TotalTimeElapsed()));
            actionSet.AddAction(DistanceToNextNode.SetVariable(Element.Part<V_DistanceBetween>(Element.Part<V_PositionOf>(new V_EventPlayer()), CurrentPositionWithDestination())));
        }
    
        /// <summary>Gets the next pathfinding attribute.</summary>
        public Element NextSegmentAttribute(Element player) => Element.TernaryConditional(
            Element.Part<V_And>(IsPathfinding(player), new V_Compare(Current.GetVariable(player), Operators.NotEqual, new V_Number(-1))),
            AttributeArray.Get(player)[Current.Get(player)],
            new V_Number(-1)
        );
    
        /// <summary>Throttles the event player to the next node.</summary>
        public void ThrottleEventPlayerToNextNode(ActionSet actionSet)
        {
            // Start throttle to the current node.
            actionSet.AddAction(Element.Part<A_StartThrottleInDirection>(
                Element.Part<V_EventPlayer>(),
                Element.Part<V_DirectionTowards>(
                    Element.Part<V_PositionOf>(Element.Part<V_EventPlayer>()),
                    // Go to the destination once the final node is reached.
                    CurrentPositionWithDestination()
                ),
                new V_Number(1), // Magnitude
                EnumData.GetEnumValue(Relative.ToWorld), // Relative
                EnumData.GetEnumValue(ThrottleBehavior.ReplaceExistingThrottle), // Throttle Behavior
                EnumData.GetEnumValue(ThrottleRev.DirectionAndMagnitude) // Throttle Reevaluation
            ));
        }

        /// <summary>The condition to use to determine when the current node is reached.</summary>
        private Condition NodeReachedCondition(ActionSet actionSet)
        {
            // No node reached hook, use default.
            if (IsNodeReachedDeterminer == null)
                return new Condition(
                    Element.Part<V_DistanceBetween>(
                        PlayerPosition(),
                        CurrentPositionWithDestination()
                    ),
                    Operators.LessThanOrEqual,
                    new V_Number(DefaultMoveToNext)
                );
            // Otherwise, use hook.
            else
                return new Condition(
                    (Element)IsNodeReachedDeterminer.Invoke(actionSet, CurrentPositionWithDestination()),
                    Operators.Equal,
                    new V_True()
                );
        }

        /// <summary>Looks at a player's future nodes.</summary>
        public Element IsTravelingToNode(ActionSet actionSet, Element targetPlayer, Element node)
        {
            IndexReference result = actionSet.VarCollection.Assign("Lookahead: Result", actionSet.IsGlobal, true);
            actionSet.AddAction(result.SetVariable(new V_False()));

            IndexReference look = actionSet.VarCollection.Assign("Pathfind: Lookahead", actionSet.IsGlobal, true);
            actionSet.AddAction(look.SetVariable(Current.Get(targetPlayer)));

            // Get the path.
            actionSet.AddAction(Element.Part<A_While>(Element.Part<V_And>(new V_Compare(
                look.GetVariable(),
                Operators.GreaterThanOrEqual,
                new V_Number(0)
            ), !result.Get())));

            //Element currentNode = PathmapInstance.Nodes.Get()[PathmapReference.Get(targetPlayer)][look.Get()];

            actionSet.AddAction(result.SetVariable(new V_Compare(look.Get(), Operators.Equal, node)));

            actionSet.AddAction(look.SetVariable(ParentArray.Get(targetPlayer)[look.Get()] - 1));
            actionSet.AddAction(new A_End());

            return result.Get();
        }

        /// <summary>Looks at a player's future segments.</summary>
        public Element IsTravelingToSegment(ActionSet actionSet, Element targetPlayer, Element segment)
        {
            IndexReference result = actionSet.VarCollection.Assign("Lookahead: Result", actionSet.IsGlobal, true);
            actionSet.AddAction(result.SetVariable(new V_False()));

            IndexReference look = actionSet.VarCollection.Assign("Pathfind: Lookahead", actionSet.IsGlobal, true);
            actionSet.AddAction(look.SetVariable(Current.Get(targetPlayer)));

            // Get the path.
            actionSet.AddAction(Element.Part<A_While>(Element.Part<V_And>(new V_Compare(
                ParentArray.Get(targetPlayer)[look.Get()] - 1,
                Operators.GreaterThanOrEqual,
                new V_Number(0)
            ), !result.Get())));

            Element currentNode = PathmapInstance.Nodes.Get()[PathmapReference.Get(targetPlayer)][look.Get()];
            Element nextNode = PathmapInstance.Nodes.Get()[PathmapReference.Get(targetPlayer)][ParentArray.Get(targetPlayer)[look.Get()] - 1];

            actionSet.AddAction(result.SetVariable(new V_Compare(
                segment,
                Operators.Equal,
                Element.Part<V_FirstOf>(PathmapInstance.SegmentsFromNodes(PathmapReference.Get(targetPlayer), look.Get(), ParentArray.Get(targetPlayer)[look.Get()] - 1))
            )));

            actionSet.AddAction(look.SetVariable(ParentArray.Get(targetPlayer)[look.Get()] - 1));
            actionSet.AddAction(new A_End());

            return result.Get();
        }

        /// <summary>Looks at a player's future nodes.</summary>
        public Element IsTravelingToAttribute(ActionSet actionSet, Element targetPlayer, Element attribute)
        {
            IndexReference result = actionSet.VarCollection.Assign("Lookahead: Result", actionSet.IsGlobal, true);
            actionSet.AddAction(result.SetVariable(new V_False()));

            IndexReference look = actionSet.VarCollection.Assign("Pathfind: Lookahead", actionSet.IsGlobal, true);
            actionSet.AddAction(look.SetVariable(Current.Get(targetPlayer)));

            // Get the path.
            actionSet.AddAction(Element.Part<A_While>(Element.Part<V_And>(new V_Compare(
                look.GetVariable(),
                Operators.GreaterThanOrEqual,
                new V_Number(0)
            ), !result.Get())));

            actionSet.AddAction(result.SetVariable(new V_Compare(attribute, Operators.Equal, AttributeArray.Get(targetPlayer)[look.Get()])));

            actionSet.AddAction(look.SetVariable(ParentArray.Get(targetPlayer)[look.Get()] - 1));
            actionSet.AddAction(new A_End());

            return result.Get();
        }
    }
}