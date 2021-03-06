﻿using System;
using Mantle.Aws;

namespace Mantle.Messaging.Aws
{
    public class SqsSubscriberEndpoint : SqsEndpoint, ISubscriberEndpoint
    {
        private readonly IAwsConfiguration awsConfiguration;

        public SqsSubscriberEndpoint(IAwsConfiguration awsConfiguration)
        {
            if (awsConfiguration == null)
                throw new ArgumentNullException("awsConfiguration");

            awsConfiguration.Validate();

            this.awsConfiguration = awsConfiguration;
        }

        public ISubscriberClient GetClient()
        {
            return new SqsSubscriberClient(this, awsConfiguration);
        }

        public ISubscriberEndpointManager GetManager()
        {
            return new SqsSubscriberEndpointManager(this, awsConfiguration);
        }
    }
}