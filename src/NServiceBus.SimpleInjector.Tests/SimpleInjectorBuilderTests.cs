using NServiceBus.ObjectBuilder.SimpleInjector;
using NUnit.Framework;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NServiceBus.SimpleInjector.Tests
{
    public class SimpleInjectorBuilderTests
    {
        class TestType { }
        [Test]
        public void Passing_A_Func_Uses_The_Func_To_Resolve()
        {
            var builder = new SimpleInjectorObjectBuilder();
            var funcCalled = false;

            Func<TestType> reg = () =>
            {
                funcCalled = true;
                return new TestType();
            };
            builder.Configure(reg, DependencyLifecycle.InstancePerCall);

            var resolve = builder.Build(typeof(TestType));
            Assert.True(funcCalled);
        }

        [Test]
        public void Passing_A_Func_Uses_The_Func_To_Resolve_In_Child_Container()
        {
            var builder = new SimpleInjectorObjectBuilder();
            var funcCalled = false;

            Func<TestType> reg = () =>
            {
                funcCalled = true;
                return new TestType();
            };
            builder.Configure(reg, DependencyLifecycle.InstancePerCall);

            var childContainer = builder.BuildChildContainer();

            var resolve = childContainer.Build(typeof(TestType));
            Assert.True(funcCalled);
        }

        [Test]
        public void Registering_Multiple_Types_Should_Resolve_All_Types()
        {
            var builder = new SimpleInjectorObjectBuilder();

            builder.Configure(typeof(FirstImplementation), DependencyLifecycle.InstancePerUnitOfWork);
            builder.Configure(typeof(SecondImplementation), DependencyLifecycle.InstancePerUnitOfWork);
            builder.Configure(typeof(ThirdImplementation), DependencyLifecycle.InstancePerUnitOfWork);

            var allTypes = builder.BuildAll(typeof(IInterface));

            Assert.AreEqual(3, allTypes.Count());
        }
    }

    interface IInterface { }
    class FirstImplementation : IInterface { }
    class SecondImplementation : IInterface { }
    class ThirdImplementation : IInterface { }
}