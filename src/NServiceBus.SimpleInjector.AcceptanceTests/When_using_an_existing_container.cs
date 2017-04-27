namespace NServiceBus.SimpleInjector.AcceptanceTests
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using NServiceBus;
    using global::SimpleInjector.Lifestyles;

    public class When_using_an_existing_container : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_values_from_external_container()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((bus, c) => bus.SendLocal(new MyMessage())))
                .Done(c => c.WasCalled)
                .Run();

            Assert.IsTrue(context.PropertyWasInjected);
        }
        

        public class MyService
        {
            public string Id { get; set; }
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public bool PropertyWasInjected { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                var container = new global::SimpleInjector.Container();
                container.Options.AllowOverridingRegistrations = true;
                container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
                container.Options.AutoWirePropertiesImplicitly();

                container.Register(() => new MyService { Id = "Created outside" }, global::SimpleInjector.Lifestyle.Scoped);

                EndpointSetup<DefaultServer>(config =>
                {
                    config.UseContainer<SimpleInjectorBuilder>(customization =>
                    {
                        customization.UseExistingContainer(container);
                    });
                    config.SendFailedMessagesTo("error");
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public MyService MyPropDependency { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.PropertyWasInjected = MyPropDependency != null && MyPropDependency.Id == "Created outside";
                    testContext.WasCalled = true;

                    return Task.FromResult(0);
                }

                Context testContext = new Context();
            }
        }

        public class MyMessage : ICommand
        {
        }
    }
}