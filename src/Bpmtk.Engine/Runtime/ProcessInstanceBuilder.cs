﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bpmtk.Engine.Bpmn2;
using Bpmtk.Engine.Models;
using Bpmtk.Engine.Repository;
using Bpmtk.Engine.Utils;
using Bpmtk.Engine.Variables;

namespace Bpmtk.Engine.Runtime
{
    public class ProcessInstanceBuilder : IProcessInstanceBuilder
    {
        protected string key;
        protected string name;
        protected string description;
        protected string initiator;
        protected Token super;
        protected int? processDefinitionId;
        protected string processDefinitionKey;
        protected ProcessDefinition processDefinition;
        protected IDictionary<string, object> variables;
        protected readonly Context context;

        public ProcessInstanceBuilder(Context context)
        {
            this.context = context;
        }

        public virtual async Task<ProcessInstance> BuildAsync()
        {
            var deploymentManager = this.context.DeploymentManager;

            if (this.processDefinition == null)
            {
                if (this.processDefinitionId != null)
                    this.processDefinition = await deploymentManager.FindProcessDefinitionByIdAsync(this.processDefinitionId.Value);
                else if(this.processDefinitionKey != null)
                    this.processDefinition = await deploymentManager.FindProcessDefinitionByKeyAsync(this.processDefinitionKey);

                if (this.processDefinition == null)
                    throw new ArgumentNullException(nameof(processDefinition));
            }

            if (this.processDefinition.State != ProcessDefinitionState.Active)
                throw new RuntimeException("The specified process-defintion is not active.");

            //Load bpmn-model.
            var deploymentId = this.processDefinition.DeploymentId;
            var processDefinitionKey = this.processDefinition.Key;

            var dm = context.DeploymentManager;
            var model = dm.GetBpmnModel(deploymentId);
            var process = model.GetProcess(processDefinitionKey);
            var initialNode = process.InitialNode;
            if (initialNode == null)
                throw new RuntimeException($"The process '{processDefinitionKey}' does not contains any start nodes.");

            var date = Clock.Now;

            //init process-instance.
            var pi = new ProcessInstance();
            
            pi.ProcessDefinition = this.processDefinition;
            pi.Super = this.super;

            if (pi.Super != null)
                pi.Caller = super.ActivityInstance;

            pi.Name = processDefinition.Name;
            if (string.IsNullOrEmpty(pi.Name))
                pi.Name = processDefinition.Key;

            pi.Key = this.key;
            pi.Created = date;
            pi.Modified = date;
            pi.State = ExecutionState.Ready;
            pi.LastStateTime = date;
            pi.Initiator = this.initiator;
            pi.Description = StringHelper.Get(processDefinition.Description, 255);

            if (!string.IsNullOrEmpty(this.name))
                pi.Name = this.name;

            if (!string.IsNullOrEmpty(this.description))
                pi.Description = this.description;

            pi.Variables = new List<Variable>();
            pi.IdentityLinks = new List<IdentityLink>();

            var dataObjects = model.GetProcessDataObjects(processDefinitionKey);

            //initialize process-context.
            this.InitializeProcessContext(pi, dataObjects);

            var session = this.context.DbSession;

            //commit changes.
            await session.SaveAsync(pi);
            await session.FlushAsync();

            //Init root-token.
            var rootToken = new Token(pi);
            rootToken.Node = initialNode;
            pi.Token = rootToken;

            //commit changes.
            await session.SaveAsync(rootToken);
            await session.FlushAsync();

            return pi;
        }

        protected virtual void InitializeProcessContext(ProcessInstance processInstance,
            IList<ValuedDataObject> dataObjects)
        {
            var dataObjectsMap = dataObjects.ToDictionary(x => x.Id);

            IVariableType type = null;
            foreach (var dataObject in dataObjects)
            {
                var value = dataObject.Value;
                type = Variables.VariableType.Resolve(value);

                var variable = new Variable();
                variable.Name = dataObject.Id;
                variable.Type = type.Name;

                type.SetValue(variable, value);

                processInstance.Variables.Add(variable);
            }

            if (variables != null && variables.Count > 0)
            {
                var em = variables.GetEnumerator();
                while (em.MoveNext())
                {
                    processInstance.SetVariable(em.Current.Key, em.Current.Value);
                }
            }
        }

        public virtual ProcessInstanceBuilder SetProcessDefinitionKey(string processDefinitionKey)
        {
            this.processDefinitionKey = processDefinitionKey;
            if(this.processDefinitionKey != null)
            {
                this.processDefinitionId = null;
                this.processDefinition = null;
            }

            return this;
        }

        public virtual ProcessInstanceBuilder SetProcessDefinitionId(int processDefinitionId)
        {
            this.processDefinitionId = processDefinitionId;
            this.processDefinitionKey = null;
            this.processDefinition = null;

            return this;
        }

        public virtual ProcessInstanceBuilder SetProcessDefinition(ProcessDefinition processDefinition)
        {
            this.processDefinition = processDefinition;
            if(this.processDefinition != null)
            {
                this.processDefinitionId = null;
                this.processDefinitionKey = null;
            }

            return this;
        }

        public virtual ProcessInstanceBuilder SetVariables(IDictionary<string, object> variables)
        {
            this.variables = variables;

            return this;
        }

        public virtual ProcessInstanceBuilder SetName(string name)
        {
            this.name = name;

            return this;
        }

        public virtual ProcessInstanceBuilder SetKey(string key)
        {
            this.key = key;

            return this;
        }

        public virtual ProcessInstanceBuilder SetInitiator(string initiator)
        {
            this.initiator = initiator;

            return this;
        }

        public virtual ProcessInstanceBuilder SetDescription(string description)
        {
            this.description = description;

            return this;
        }

        public virtual ProcessInstanceBuilder SetSuper(Token super)
        {
            this.super = super;

            return this;
        }

        IProcessInstanceBuilder IProcessInstanceBuilder.SetProcessDefinitionKey(string processDefinitionKey)
            => this.SetProcessDefinitionKey(processDefinitionKey);

        IProcessInstanceBuilder IProcessInstanceBuilder.SetProcessDefinitionId(int processDefinitionId)
            => this.SetProcessDefinitionId(processDefinitionId);

        IProcessInstanceBuilder IProcessInstanceBuilder.SetVariables(IDictionary<string, object> variables)
            => this.SetVariables(variables);

        IProcessInstanceBuilder IProcessInstanceBuilder.SetName(string name)
            => this.SetName(name);

        IProcessInstanceBuilder IProcessInstanceBuilder.SetKey(string key)
            => this.SetKey(key);

        IProcessInstanceBuilder IProcessInstanceBuilder.SetInitiator(string initiator)
            => this.SetInitiator(initiator);

        IProcessInstanceBuilder IProcessInstanceBuilder.SetDescription(string description)
            => this.SetDescription(description);

        IProcessInstanceBuilder IProcessInstanceBuilder.SetSuper(Token super)
            => this.SetSuper(super);

        IProcessInstance IProcessInstanceBuilder.Build()
            => this.BuildAsync().Result;

        async Task<IProcessInstance> IProcessInstanceBuilder.BuildAsync()
            => await this.BuildAsync();
    }
}
