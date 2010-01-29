namespace MassTransit.Services.Routing.Configuration
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Internal;
    using MassTransit.Configuration;
    using Pipeline;

    public class RoutingConfigurator :
        IServiceConfigurator
    {
        readonly IList<Func<IServiceBus, IEndpointFactory, UnsubscribeAction>> _routes = new List<Func<IServiceBus, IEndpointFactory, UnsubscribeAction>>();
        public Type ServiceType
        {
            get { return typeof (RoutingService); }
        }

        public IBusService Create(IServiceBus bus, IObjectBuilder builder)
        {
            var ef = builder.GetInstance<IEndpointFactory>();
            return new RoutingService(_routes, ef);
        }

        public RouteTo<TMessage> Route<TMessage>() where TMessage : class
        {
            return new Router<TMessage>(this);
        }

        class Router<TMessage> :
            RouteTo<TMessage> where TMessage : class
        {
            readonly RoutingConfigurator _boss;

            public Router(RoutingConfigurator boss)
            {
                _boss = boss;
            }

            public void To(Uri address)
            {
                _boss._routes.Add((bus, ef)=>
                {
                    var ep = ef.GetEndpoint(address);
                    return bus.OutboundPipeline.Subscribe<TMessage>(bus, ep);
                });
            }

            public void To(string addressUri)
            {
                To(new Uri(addressUri));
            }
        }
    }

    public interface RouteTo<TMessage> where TMessage : class
    {
        void To(Uri address);
        void To(string addressUri);
    }
}