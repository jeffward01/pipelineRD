﻿using PipelineRD.Sample.Models;
using PipelineRD.Sample.Workflows.Bank.AccountSteps;
using PipelineRD.Sample.Workflows.Bank.SharedSteps;

using Polly;

using System;

namespace PipelineRD.Sample.Workflows.Bank
{
    public class BankPipelineBuilder : IBankPipelineBuilder
    {
        public IPipelineInitializer<BankContext> Pipeline { get; }

        public BankPipelineBuilder(IPipelineInitializer<BankContext> pipeline)
        {
            Pipeline = pipeline;
        }

        public RequestStepResult CreateAccount(CreateAccountModel model)
        {
            var requestKey = Guid.NewGuid().ToString();
            return Pipeline
                .Initialize(requestKey)
                .EnableRecoveryRequestByHash()
                .AddNext<ISearchAccountStep>()
                    .When(b => b.Id == "bla")
                .AddNext<IDepositAccountStep>()
                    .AddRollback<IDepositAccountRollbackStep>()
                    .WithPolicy(Policy.HandleResult<RequestStepResult>(x => !x.Success).Retry(3))
                .AddNext<ICreateAccountStep>()
                    .AddRollback<ICreateAccountRollbackStep>()
                .AddNext<IFinishAccountStep>()
                .Execute(model);
        }

        public RequestStepResult DepositAccount(DepositAccountModel model)
        {
            return Pipeline
                .Initialize()
                .AddNext<ISearchAccountStep>()
                .AddNext<ISearchAccountStep>()
                .AddNext<IDepositAccountStep>()
                    .When(b => b.Id == "test")
                .AddNext<IFinishAccountStep>()
                .Execute(model);
        }
    }

    public interface IBankPipelineBuilder : IPipelineBuilder<BankContext>
    {
        RequestStepResult CreateAccount(CreateAccountModel model);
        RequestStepResult DepositAccount(DepositAccountModel model);
    }
}