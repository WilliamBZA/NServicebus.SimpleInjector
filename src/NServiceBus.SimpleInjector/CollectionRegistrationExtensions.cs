using System;
using System.Collections.Generic;
using System.Linq;
using global::SimpleInjector;
using System.Linq.Expressions;
using SimpleInjector.Lifestyles;
using System.Reflection;

namespace NServiceBus.SimpleInjector
{
    /// <summary>
    /// Extension methods for configuration collection defaults on the SimpleInjector container
    /// </summary>
    public static class CollectionRegistrationExtensions
    {
        /// <summary>
        /// Adds support to the container to resolve arrays and lists
        /// </summary>
        /// <param name="container"></param>
        public static void AllowToResolveArraysAndLists(this global::SimpleInjector.Container container)
        {
            container.ResolveUnregisteredType += (sender, e) => {
                var serviceType = e.UnregisteredServiceType;

                if (serviceType.IsArray)
                {
                    RegisterArrayResolver(e, container,
                        serviceType.GetElementType());
                }
                else if (serviceType.IsGenericType &&
                  serviceType.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    RegisterArrayResolver(e, container,
                        serviceType.GetGenericArguments()[0]);
                }
            };
        }

        public static global::SimpleInjector.Container Clone(this global::SimpleInjector.Container parentContainer)
        {
            var clonedContainer = new global::SimpleInjector.Container();
            clonedContainer.AllowToResolveArraysAndLists();
            clonedContainer.Options.AllowOverridingRegistrations = true;
            clonedContainer.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
            clonedContainer.Options.AutoWirePropertiesImplicitly();
            AsyncScopedLifestyle.BeginScope(clonedContainer);
            foreach (var reg in parentContainer.GetCurrentRegistrations())
            {
                if (reg.Lifestyle == Lifestyle.Singleton && !HasComponent(clonedContainer, reg.ServiceType))
                {
                    clonedContainer.Register(reg.ServiceType, reg.GetInstance, reg.Lifestyle);
                }
                else
                {
                    var registration = RegistrationOptions(reg, clonedContainer).First(r => r != null);
                    clonedContainer.AddRegistration(reg.ServiceType, registration);
                }
            }
            return clonedContainer;
        }

        static IEnumerable<Registration> RegistrationOptions(InstanceProducer registrationToCopy, global::SimpleInjector.Container container)
        {
            yield return CreateRegistrationFromPrivateField(registrationToCopy, container, "instanceCreator");
            yield return CreateRegistrationFromPrivateField(registrationToCopy, container, "userSuppliedInstanceCreator");
            yield return registrationToCopy.Lifestyle.CreateRegistration(registrationToCopy.ServiceType, container);
        }

        static object GetPrivateField(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
            var fieldValue = field?.GetValue(obj);
            return fieldValue;
        }

        static Registration CreateRegistrationFromPrivateField(InstanceProducer instanceProducer, global::SimpleInjector.Container container, string privateFieldName)
        {
            var instanceCreator = (Func<object>)GetPrivateField(instanceProducer.Registration, privateFieldName);
            if (instanceCreator != null)
            {
                return instanceProducer.Lifestyle.CreateRegistration(instanceProducer.ServiceType, instanceCreator, container);
            }
            return null;
        }

        static bool HasComponent(global::SimpleInjector.Container container, Type componentType)
        {
            return container.GetCurrentRegistrations().Any(r => r.ServiceType == componentType);
        }

        private static void RegisterArrayResolver(UnregisteredTypeEventArgs e,
            global::SimpleInjector.Container container, Type elementType)
        {
            var producer = container.GetRegistration(typeof(IEnumerable<>)
                .MakeGenericType(elementType));
            var enumerableExpression = producer.BuildExpression();
            var arrayMethod = typeof(Enumerable).GetMethod("ToArray")
                .MakeGenericMethod(elementType);
            var arrayExpression =
                Expression.Call(arrayMethod, enumerableExpression);

            e.Register(arrayExpression);
        }
    }
}
