using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;
using SimpleInjector.Lifestyles;

namespace NServiceBus.SimpleInjector.AcceptanceTests
{
    class When_using_a_bad_container
    {
        [Test]
        public void Should_throw_argument_exception_if_not_allow_override()
        {
            Assert.ThrowsAsync<ArgumentException>(() => Scenario.Define<Context>()
                .WithEndpoint<Endpoint>().Run());
        }

        public class Context : ScenarioContext
        {
        }
        
        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                var container = new global::SimpleInjector.Container();
                //container.Options.AllowOverridingRegistrations = true;
                //container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
                //container.Options.AutoWirePropertiesImplicitly();

                EndpointSetup<DefaultServer>(config =>
                {
                    config.UseContainer<SimpleInjectorBuilder>(customization =>
                    {
                        customization.UseExistingContainer(container);
                    });
                    config.SendFailedMessagesTo("error");
                });
            }
        }
    }
}
