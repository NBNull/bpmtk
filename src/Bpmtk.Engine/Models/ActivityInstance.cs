﻿using System;
using System.Linq;
using System.Collections.Generic;
using Bpmtk.Engine.Utils;
using Bpmtk.Engine.Variables;

namespace Bpmtk.Engine.Models
{
    public class ActivityInstance : ExecutionObject
    {
        //private ICollection<ActivityVariable> variableInstances;
        private IDictionary<string, ActivityVariable> variables;
        protected ICollection<ActivityInstance> children;

        public ActivityInstance()
        {
            //this.Parent = parent;
            //this.variableInstances = new List<ActivityVariable>();
            this.variables = new Dictionary<string, ActivityVariable>();
            this.children = new List<ActivityInstance>();

            //this.ProcessInstance = processInstance;
            //this.activity = activity;

            //this.ActivityId = activity.Id;
            //this.ActivityType = activity.GetType().Name;

            ////Set initial state.
            //this.State = ExecutionState.Ready;
            //this.Created = Clock.Now;
            //this.LastStateTime = this.Created;

            //this.Name = activity.Name;
            //if (string.IsNullOrEmpty(this.Name))
            //    this.Name = activity.Id;

            //if (activity.Documentations.Count > 0)
            //{
            //    var textArray = activity.Documentations.Select(x => x.Text).ToArray();
            //    this.Description = StringHelper.Join(textArray, "\n", 255);
            //}
        }

        public virtual ProcessInstance ProcessInstance
        {
            get;
            set;
        }

        public virtual ActivityInstance Parent
        {
            get;
            set;
        }

        public virtual ProcessInstance SubProcessInstance
        {
            get;
            set;
        }

        public virtual string ActivityId
        {
            get;
            set;
        }

        public virtual string ActivityType
        {
            get;
            set;
        }

        public virtual bool IsMIRoot
        {
            get;
            set;
        }

        //public virtual ActivityInstance MIRoot
        //{
        //    get;
        //    set;
        //}

        public virtual ICollection<ActivityInstance> Children
        {
            get => this.children;
        }

        public virtual long? TokenId
        {
            get;
            set;
        }

        public virtual ICollection<ActivityVariable> Variables
        {
            get;
            set;
        }

        public virtual void Activate()
        {
            this.State = ExecutionState.Active;
            this.StartTime = Clock.Now;
            this.LastStateTime = this.StartTime.Value;
        }

        public virtual void Finish()
        {
            this.State = ExecutionState.Completed;
            this.LastStateTime = Clock.Now;
        }

        //public static ActivityInstance Create(ExecutionContext executionContext)
        //{
        //    var token = executionContext.Token;
        //    var node = token.Node;
        //    ActivityInstance act = null;
        //    var processInstance = executionContext.ProcessInstance;
        //    var scope = executionContext.Scope;
        //    act = new ActivityInstance(processInstance, node, scope);
        //    act.IsMIRoot = token.IsMIRoot;
        //    if (scope != null)
        //        scope.Children.Add(act);

        //    var store = executionContext.Context.GetService<IInstanceStore>();
        //    store.Add(act);

        //    return act;
        //}

        public virtual bool GetVariable(string name, out object value)
        {
            value = null;
            ActivityVariable variable = null;
            if (this.variables.TryGetValue(name, out variable))
            {
                value = variable.GetValue();
                return true;
            }

            return false;
        }

        public override void SetVariable(string name, object value)
        {
            ActivityVariable variable = null;
            if (this.variables.TryGetValue(name, out variable))
                variable.SetValue(value);

            //if (this.Parent != null)
            //{
            //    this.Parent.SetVariable(name, value);
            //    return;
            //}

            //this.ProcessInstance.SetVariable(name, value);
        }

        protected virtual void CreateOrUpdateVariable(string name, object value)
        {
            ActivityVariable variable = null;
            if (this.variables.TryGetValue(name, out variable))
            {
                variable.SetValue(value);
                return;
            }

            this.CreateVariableInstance(name, value);
        }

        //public virtual void InitializeContext(IContext context,
        //    IDictionary<string, object> variables = null)
        //{
        //    var processDefinition = this.ProcessInstance.ProcessDefinition;
        //    var deploymentId = processDefinition.Deployment.Id;
        //    var processDefinitionKey = processDefinition.Key;

        //    var dm = context.GetService<IDeploymentManager>();
        //    var model = dm.GetBpmnModel(deploymentId);
        //    //var dataObjects = model.GetSubProcessDataObjects(this.ActivityId);

        //    //IVariableType type = null;
        //    //foreach (var dataObject in dataObjects)
        //    //{
        //    //    var value = dataObject.Value;
        //    //    type = Variables.VariableType.Resolve(value);

        //    //    this.CreateVariableInstance(dataObject.Id, value);
        //    //}

        //    if (variables != null && variables.Count > 0)
        //    {
        //        var em = variables.GetEnumerator();
        //        while (em.MoveNext())
        //            this.CreateOrUpdateVariable(em.Current.Key, em.Current.Value);
        //    }
        //}

        protected virtual ActivityVariable CreateVariableInstance(string name,
            object initialValue = null)
        {
            var item = new ActivityVariable();
            //this.variableInstances.Add(item);
            this.variables.Add(item.Name, item);

            return item;
        }

        public override Variable GetVariable(string name)
        {
            //object value = null;
            //if (this.GetVariable(name, out value))
            //    return value;

            //if (this.Parent != null)
            //    return this.Parent.GetVariable(name);

            //return this.ProcessInstance.GetVariable(name);
            return null;
        }

        //public override void Terminate(IContext context, string endReason = null)
        //{
        //    this.State = ExecutionState.Terminated;
        //    this.LastStateTime = Clock.Now;
        //}
    }

    public enum ActivityInstanceState
    {
        Ready = 0,

        Active = 1,

        Completed = 2,

        Failed = 4,

        Compensated = 8,

        Terminated = 16
    }
}