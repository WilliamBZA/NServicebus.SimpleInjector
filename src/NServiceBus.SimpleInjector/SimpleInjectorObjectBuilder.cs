namespace NServiceBus.ObjectBuilder.SimpleInjector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NServiceBus.SimpleInjector;
    using global::SimpleInjector;
    using Janitor;
    using System.Threading;
    using global::SimpleInjector.Lifestyles;

    class SimpleInjectorObjectBuilder : Common.IContainer
    {
        [SkipWeaving]
        Container container;
        readonly bool externalContainer = false;
        bool isBuilt = false;
        Dictionary<Type, IEnumerable<Registration>> collectionRegistrations = new Dictionary<Type, IEnumerable<Registration>>();

        public SimpleInjectorObjectBuilder(Container parentContainer)
        {
            container = parentContainer;

            externalContainer = true;
            AsyncScopedLifestyle.BeginScope(container);
        }

        public SimpleInjectorObjectBuilder()
        {
            container = new Container();
            container.AllowToResolveArraysAndLists();
            container.Options.AllowOverridingRegistrations = true;
            container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
            container.Options.AutoWirePropertiesImplicitly();

            AsyncScopedLifestyle.BeginScope(container);
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        void DisposeManaged()
        {
            var scope = Lifestyle.Scoped.GetCurrentScope(container);

            var scopeTemp = Interlocked.Exchange(ref scope, null);
            if (scopeTemp != null)
            {
                scopeTemp.Dispose();
            }

            if (!externalContainer)
            {
                var temp = Interlocked.Exchange(ref container, null);
                if (temp != null)
                {
                    temp.Dispose();
                }
            }
        }

        public Common.IContainer BuildChildContainer()
        {
            return new SimpleInjectorObjectBuilder(container);
        }

        public object Build(Type typeToBuild)
        {
            isBuilt = true;

            if (!HasComponent(typeToBuild))
            {
                throw new ActivationException("The requested type is not registered yet");
            }
            
            collectionRegistrations = null;

            return container.GetInstance(typeToBuild);
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            isBuilt = true;

            if (HasComponent(typeToBuild))
            {
                collectionRegistrations = null;

                try
                {
                    return container.GetAllInstances(typeToBuild);
                }
                catch (Exception)
                {
                    // Urgh!
                    return new[] { container.GetInstance(typeToBuild) };
                }
            }

            return new object[] { };
        }

        public void Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            if (isBuilt)
            {
                container = container.Clone();
            }

            var registration = GetRegistrationFromDependencyLifecycle(dependencyLifecycle, component);

            foreach (var implementedInterface in component.GetInterfaces())
            {
                if (HasComponent(implementedInterface))
                {
                    var existingRegistration = GetExistingCollectionRegistrationsFor(implementedInterface);

                    RegisterCollection(implementedInterface, existingRegistration.Union(new[] { registration }));
                }
                else
                {
                    container.AddRegistration(implementedInterface, registration);
                }
            }

            if (HasComponent(component))
            {
                var existingRegistration = GetExistingRegistrationsFor(component);

                var first = existingRegistration.First();
                if (!AreRegistrationsEqual(first, registration))
                {
                    container.Collection.Register(component, existingRegistration.Union(new[] { registration }));
                }
            }
            else
            {
                container.AddRegistration(component, registration);
            }
        }

        public void Configure<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            if (isBuilt)
            {
                container = container.Clone();
            }

            var funcType = typeof(T);
            var registration = GetRegistrationFromDependencyLifecycle(dependencyLifecycle, funcType, () => componentFactory());

            foreach (var implementedInterface in funcType.GetInterfaces())
            {
                if (HasComponent(implementedInterface))
                {
                    var existingRegistration = GetExistingCollectionRegistrationsFor(implementedInterface);

                    RegisterCollection(implementedInterface, existingRegistration.Union(new[] { registration }));
                }
                else
                {
                    AddRegistration(componentFactory, dependencyLifecycle, funcType, registration, implementedInterface);
                }
            }

            AddRegistration(componentFactory, dependencyLifecycle, funcType, registration);
        }

        private void AddRegistration<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle, Type funcType, Registration registration)
        {
            try
            {
                container.AddRegistration(funcType, registration);
            }
            catch (ArgumentException)
            {
                container = container.Clone();
                registration = GetRegistrationFromDependencyLifecycle(dependencyLifecycle, funcType, () => componentFactory());

                container.AddRegistration(funcType, registration);
            }
        }

        private void AddRegistration<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle, Type funcType, Registration registration, Type implementedInterface)
        {
            try
            {
                container.AddRegistration(implementedInterface, registration);
            }
            catch (InvalidOperationException)
            {
                container = container.Clone();
                registration = GetRegistrationFromDependencyLifecycle(dependencyLifecycle, funcType, () => componentFactory());

                container.AddRegistration(implementedInterface, registration);
            }
        }

        void RegisterCollection(Type implementedInterface, IEnumerable<Registration> registrations)
        {
            if (isBuilt)
            {
                container = container.Clone();
            }

            container.Collection.Register(implementedInterface, registrations);

            collectionRegistrations[implementedInterface] = registrations;
        }

        IEnumerable<Registration> GetExistingCollectionRegistrationsFor(Type implementedInterface)
        {
            if (collectionRegistrations.ContainsKey(implementedInterface))
            {
                return collectionRegistrations[implementedInterface];
            }

            return GetExistingRegistrationsFor(implementedInterface);
        }

        IEnumerable<Registration> GetExistingRegistrationsFor(Type implementedInterface)
        {
            return container.GetCurrentRegistrations().Where(r => r.ServiceType == implementedInterface).Select(r => r.Registration);
        }

        IEnumerable<Registration> GetExistingRegistrationsFor<TType>()
        {
            return GetExistingRegistrationsFor(typeof(TType));
        }
        
        public void RegisterSingleton(Type lookupType, object instance)
        {
            if (isBuilt)
            {
                container = container.Clone();
            }

            var registration = GetRegistrationFromDependencyLifecycle(DependencyLifecycle.SingleInstance, lookupType, instance);

            foreach (var implementedInterface in lookupType.GetInterfaces())
            {
                if (HasComponent(implementedInterface))
                {
                    var existingRegistrations = GetExistingRegistrationsFor(implementedInterface);

                    container.Collection.Register(implementedInterface, existingRegistrations.Union(new[] { registration }));
                }
                else
                {
                    container.AddRegistration(implementedInterface, registration);
                }
            }

            container.Register(lookupType, () => instance, registration.Lifestyle); // urgh
        }

        public bool HasComponent(Type componentType)
        {
            return GetExistingRegistrationsFor(componentType).Any();
        }

        public void Release(object instance)
        {
        }

        Lifestyle GetLifestyleFromDependencyLifecycle(DependencyLifecycle dependencyLifecycle)
        {
            switch (dependencyLifecycle)
            {
                case DependencyLifecycle.SingleInstance:
                    return Lifestyle.Singleton;

                case DependencyLifecycle.InstancePerUnitOfWork:
                    return Lifestyle.Scoped;

                case DependencyLifecycle.InstancePerCall:
                default:
                    return Lifestyle.Transient;
            }
        }

        Registration GetRegistrationFromDependencyLifecycle(DependencyLifecycle dependencyLifecycle, Type component)
        {
            return GetLifestyleFromDependencyLifecycle(dependencyLifecycle).CreateRegistration(component, container);
        }

        Registration GetRegistrationFromDependencyLifecycle(DependencyLifecycle dependencyLifecycle, Type component, Func<object> creator)
        {
            return GetLifestyleFromDependencyLifecycle(dependencyLifecycle).CreateRegistration(component, creator, container);
        }

        Registration GetRegistrationFromDependencyLifecycle(DependencyLifecycle dependencyLifecycle, Type component, object instance)
        {
            return GetRegistrationFromDependencyLifecycle(dependencyLifecycle, component, () => instance);
        }

        bool AreRegistrationsEqual(Registration registration, Registration otherRegistration)
        {
            return registration.Lifestyle == otherRegistration.Lifestyle
                && registration.ImplementationType == registration.ImplementationType;
        }

        static void SetPropertyValue(object instance, string propertyName, object value)
        {
            instance.GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                .SetValue(instance, value, null);
        }
    }
}