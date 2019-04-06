﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Bpmtk.Engine.Runtime;
using Bpmtk.Engine.Stores;

namespace Bpmtk.Engine.Bpmn2
{
    public class MultiInstanceLoopCharacteristics : LoopCharacteristics
    {
        public MultiInstanceLoopCharacteristics()
        {
            this.IsSequential = false;
            this.Behavior = MultiInstanceBehavior.All;
            //this.ComplexBehaviorDefinitions = new List<ComplexBehaviorDefinition>();
        }

        /// <summary>
        /// A numeric Expression that controls the number of Activity instances
        //        that will be created.This Expression MUST evaluate to an integer.
        //This MAY be underspecified, meaning that the modeler MAY simply document
        //        the condition.In such a case the loop cannot be formally
        //        executed.
        //In order to initialize a valid multi-instance, either the loopCardinality
        //        Expression or the loopDataInput MUST be specified.
        /// </summary>
        public virtual Expression LoopCardinality
        {
            get;
            set;
        }

        ///// <summary>
        ///// Gets or sets collection variable id.(extended)
        ///// </summary>
        //public virtual string CollectionRef
        //{
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// Gets or sets element variable id.(extended)
        ///// </summary>
        //public virtual string ElementRef
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// This ItemAwareElement is used to determine the number of Activity
        //        instances, one Activity instance per item in the collection of data stored
        //in that ItemAwareElement element.
        //For Tasks it is a reference to a Data Input which is part of the Activity’s
        //InputOutputSpecification.
        //For Sub-Processes it is a reference to a collection-valued Data Object
        //in the context that is visible to the Sub-Processes.
        //In order to initialize a valid multi-instance, either the loopCardinality
        //Expression or the loopDataInput MUST be specified.
        /// </summary>
        public virtual IItemAwareElement LoopDataInputRef
        {
            get;
            set;
        }

        /// <summary>
        /// This ItemAwareElement specifies the collection of data, which will be
        //  produced by the multi-instance.
        //  For Tasks it is a reference to a Data Output which is part of the
        //  Activity’s InputOutputSpecification.
        //  For Sub-Processes it is a reference to a collection-valued Data Object
        //  in the context that is visible to the Sub-Processes.
        /// </summary>
        public virtual IItemAwareElement LoopDataOutputRef
        {
            get;
            set;
        }

        /// <summary>
        /// A Data Input, representing for every Activity instance the single item of
        //  the collection stored in the loopDataInput.This Data Input can be
        //  the source of DataInputAssociation to a data input of the Activity’s
        //  InputOutputSpecification. The type of this Data Input MUST the
        //  scalar of the type defined for the loopDataInput.
        /// </summary>
        public virtual DataInput InputDataItem
        {
            get;
            set;
        }

        /// <summary>
        /// A Data Output, representing for every Activity instance the single item
        //of the collection stored in the loopDataOutput.This Data Output can
        //be the target of DataOutputAssociation to a data output of the
        //Activity’s InputOutputSpecification. The type of this Data
        //Output MUST the scalar of the type defined for the loopDataOutput
        /// </summary>
        public virtual DataOutput OutputDataItem
        {
            get;
            set;
        }

        /// <summary>
        /// Controls when and which Events are thrown in case behavior is set to
        /// complex.
        /// </summary>
        //public ICollection<ComplexBehaviorDefinition> ComplexBehaviorDefinitions
        //{
        //    get;
        //}

        /// <summary>
        /// This attribute defines a boolean Expression that when evaluated to
        /// true, cancels the remaining Activity instances and produces a token.
        /// </summary>
        public virtual Expression CompletionCondition
        {
            get;
            set;
        }

        /// <summary>
        /// This attribute is a flag that controls whether the Activity instances will
        /// execute sequentially or in parallel.
        /// </summary>
        public virtual bool IsSequential
        {
            get;
            set;
        }

        /// <summary>
        /// The attribute behavior acts as a shortcut for specifying when events
        //        SHALL be thrown from an Activity instance that is about to complete.It
        //        can assume values of None, One, All, and Complex, resulting in the
        //        following behavior:
        //• None: the EventDefinition which is associated through the
        //noneEvent association will be thrown for each instance
        //completing.
        //• One: the EventDefinition referenced through the oneEvent
        //association will be thrown upon the first instance completing.
        //• All: no Event is ever thrown; a token is produced after completion
        //of all instances.
        //• Complex: the complexBehaviorDefinitions are consulted to
        //determine if and which Events to throw.
        //For the behaviors of none and one, a default
        //SignalEventDefinition will be thrown which automatically carries
        //the current runtime attributes of the MI Activity.
        //Any thrown Events can be caught by boundary Events on the Multi-
        //Instance Activity.
        /// </summary>
        public virtual MultiInstanceBehavior Behavior
        {
            get;
            set;
        }

        /// <summary>
        /// The EventDefinition which is thrown when behavior is set to one
        /// and the first internal Activity instance has completed.
        /// </summary>
        public virtual EventDefinition OneBehaviorEventRef
        {
            get;
            set;
        }

        /// <summary>
        /// The EventDefinition which is thrown when the behavior is set to
        /// none and an internal Activity instance has completed.
        /// </summary>
        public virtual EventDefinition NoneBehaviorEventRef
        {
            get;
            set;
        }

        #region Runtime

        public override void Leave(ExecutionContext executionContext)
        {
            //设置活动实例为结束状态
            var context = executionContext.Context;
            var token = executionContext.Token;
            token.Inactivate();

            var parentToken = token.Parent;
            if (parentToken != null)
            {
                var parentExecution = ExecutionContext.Create(context, parentToken);

                var numberOfInstances = parentExecution.GetVariable<int>("numberOfInstances");
                var numberOfCompletedInstances = parentExecution.GetVariable<int>("numberOfCompletedInstances") + 1;
                var numberOfActiveInstances = parentExecution.GetVariable<int>("numberOfActiveInstances") - 1;

                parentExecution.SetVariable("numberOfCompletedInstances", numberOfCompletedInstances);
                parentExecution.SetVariable("numberOfActiveInstances", numberOfActiveInstances);

                //输出变量到集合
                var loopOutputRef = this.LoopDataOutputRef;
                if (loopOutputRef != null)
                {
                    var loopCounter = executionContext.GetVariable<int>("loopCounter");
                    var outputSet = parentExecution.GetVariable<IList>(loopOutputRef.Id);

                    var outputRef = this.OutputDataItem?.Id;
                    if (!string.IsNullOrEmpty(outputRef) && loopCounter < outputSet.Count)
                    {
                        var result = executionContext.GetVariable(outputRef);
                        outputSet[loopCounter] = result;

                        parentExecution.SetVariable(loopOutputRef.Id, outputSet);
                    }
                }

                var node = token.Node;
                var inactivateTokens = parentToken.Children.Where(x => !x.IsActive).ToList();
                if (inactivateTokens.Count() >= numberOfInstances
                    || this.IsCompleted(parentExecution))
                {
                    //remove all child tokens
                    foreach (var item in inactivateTokens)
                        item.Remove(context);

                    base.Leave(parentExecution);
                }
            }
            else
                base.Leave(executionContext);
        }

        protected override int CreateInstances(ExecutionContext executionContext)
        {
            var numberOfInstances = this.ResolveNumberOfInstances(executionContext);
            if (numberOfInstances < 0)
            {
                throw new RuntimeException("Invalid number of instances: must be non-negative integer value"
              + ", but was " + numberOfInstances);
            }

            executionContext.SetVariable("numberOfInstances", numberOfInstances);
            executionContext.SetVariable("numberOfCompletedInstances", 0);
            executionContext.SetVariable("numberOfActiveInstances", numberOfInstances);

            var token = executionContext.Token;
            var node = token.Node;

            var tokens = new List<Token>();
            var context = executionContext.Context;

            for (int loopCounter = 0; loopCounter < numberOfInstances; loopCounter++)
            {
                var instanceToken = token.CreateToken(context);
                instanceToken.Node = node;
                tokens.Add(instanceToken);
            }

            var innerExecutions = new List<ExecutionContext>();

            IList inputSet = null;
            var loopDataInputRef = this.LoopDataInputRef;
            var inputDataItem = this.InputDataItem;
            if (inputDataItem != null)
                inputSet = executionContext.GetVariable<IList>(loopDataInputRef.Id);

            //创建活动实例
            for (int loopCounter = 0; loopCounter < numberOfInstances; loopCounter++)
            {
                var innerToken = tokens[loopCounter];
                innerToken.Activate();

                var innerExecution = ExecutionContext.Create(context, innerToken);

                innerExecution.SetVariable("loopCounter", loopCounter);

                if (inputSet != null && loopCounter < inputSet.Count)
                {
                    var inputValue = inputSet[loopCounter];
                    innerExecution.SetVariable(inputDataItem.Id, inputValue);
                }

                innerExecutions.Add(innerExecution);
            }

            //执行活动实例
            for (int loopCounter = 0; loopCounter < numberOfInstances; loopCounter++)
            {
                var innerExecution = innerExecutions[loopCounter];

                //Execute inner-activity.
                this.Activity.Execute(innerExecution);
            }

            return numberOfInstances;
        }

        protected override int ResolveNumberOfInstances(ExecutionContext executionContext)
        {
            //var expressionText = this.LoopCardinality?.Text;
            var loopDataInputRef = this.LoopDataInputRef;
            int count = -1;
            var elContext = executionContext.CreateEvaluationContext();

            if (this.LoopCardinality != null)
            {
                var value = this.LoopCardinality.GetValue<int?>(elContext);
                if (value != null)
                    count = Convert.ToInt32(value);
            }
            else if (loopDataInputRef != null)
            {
                var loopCollection = executionContext.GetVariable(loopDataInputRef.Id);
                if (loopCollection != null && loopCollection is ICollection)
                    count = ((ICollection)loopCollection).Count;
            }

            return count;
        }

        protected override bool IsCompleted(ExecutionContext executionContext)
        {
            if (this.CompletionCondition == null)
                return false;

            var elContext = executionContext.CreateEvaluationContext();
            //var condition = this.CompletionCondition.Text;

            //var evaluator = executionContext.CreateExpressionEvaluator();
            //var result = evaluator.Evaluate<bool>(condition);

            return this.CompletionCondition.GetValue<bool>(elContext);
        }

        #endregion
    }

    public enum MultiInstanceBehavior
    {

        /// <remarks/>
        None,

        /// <remarks/>
        One,

        /// <remarks/>
        All,

        /// <remarks/>
        Complex,
    }

    //public class ComplexBehaviorDefinition : BaseElement
    //{
    //    /// <remarks/>
    //    //[XmlElement("condition", Order = 0)]
    //    public FormalExpression Condition
    //    {
    //        get;
    //        set;
    //    }

    //    /// <remarks/>
    //    //[XmlElement("event", Order = 1)]
    //    public ImplicitThrowEvent Event
    //    {
    //        get;
    //        set;
    //    }

    //    protected override bool ReadElements(XmlReader reader)
    //    {
    //        switch(reader.LocalName)
    //        {
    //            case "condition":
    //                this.Condition = new FormalExpression();
    //                this.Condition.ReadXml(reader);
    //                break;

    //            case "event":
    //                this.Event = new ImplicitThrowEvent();
    //                this.Event.ReadXml(reader);
    //                break;

    //            default:
    //                return false;
    //        }

    //        return true;
    //    }
    //}
}