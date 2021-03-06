﻿using System;
using System.Linq;
using Mantle.Extensions;
using Mantle.Messaging.Contexts;
using Mantle.Messaging.Interfaces;
using Mantle.Messaging.Messages;

namespace Mantle.Messaging.Subscriptions
{
    public class DefaultSubscription<T> : ISubscription<T>
        where T : class
    {
        public DefaultSubscription(ISubscriptionConfiguration<T> configuration)
        {
            configuration.Require(nameof(configuration));
            configuration.Validate();

            Configuration = configuration;

            Configuration.Subscriber.ErrorOccurred += m => ErrorOccurred.RaiseSafely(m);
            Configuration.Subscriber.MessageOccurred += m => MessageOccurred.RaiseSafely(m);
        }

        public ISubscriptionConfiguration<T> Configuration { get; }

        public event Action<string> ErrorOccurred;
        public event Action<string> MessageOccurred;

        public bool HandleMessage(IMessageContext<MessageEnvelope> messageContext)
        {
            messageContext.Require(nameof(messageContext));

            var body = Configuration.Serializer.Deserialize(messageContext.Message.Body);
            var context = new SubscriptionMessageContext<T>(messageContext, body, this);

            if (Configuration.Constraints.Any(c => (c.Match(context) == false)))
                return false;

            if (Configuration.AutoDeadLetter)
            {
                if ((messageContext.DeliveryCount.HasValue) && (Configuration.DeadLetterDeliveryLimit.HasValue) &&
                    (messageContext.DeliveryCount.Value >= Configuration.DeadLetterDeliveryLimit.Value))
                {
                    Configuration.DeadLetterStrategy.HandleDeadLetterMessage(context);
                    return true;
                }
            }

            try
            {
                if (ExecutePreFilters(context))
                {
                    Configuration.Subscriber.HandleMessage(context);
                    ExecutePostFilters(context);
                }

                if (Configuration.AutoComplete)
                    context.TryToComplete();
            }
            catch when (Configuration.AutoAbandon)
            {
                context.TryToAbandon();

                return false;
            }

            return true;
        }

        private bool ExecutePreFilters(IMessageContext<T> messageContext)
        {
            foreach (var filter in Configuration.Filters)
            {
                if (filter.OnHandlingMessage(messageContext) == false)
                    return false;
            }

            return true;
        }

        private void ExecutePostFilters(IMessageContext<T> messageContext)
        {
            foreach (var filter in Configuration.Filters)
            {
                if (filter.OnHandledMessage(messageContext) == false)
                    return;
            }
        }
    }
}