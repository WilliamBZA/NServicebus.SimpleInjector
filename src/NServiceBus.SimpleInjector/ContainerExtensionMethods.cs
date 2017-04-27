using NServiceBus.SimpleInjector;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NServiceBus.ObjectBuilder
{
    /// <summary>
    /// Extension methods to allow cloning of a SimpleInjector container
    /// </summary>
    public static class ContainerExtensionMethods
    {
        
        
        /// <summary>
        /// Checks if 2 registrations have the same lifestyle and implementation type
        /// </summary>
        /// <param name="registration">The current registration</param>
        /// <param name="otherRegistration">The registration to compare to</param>
        /// <returns>True if the registrations share the same lifestyle and implementation type</returns>
        public static bool IsEqualTo(this Registration registration, Registration otherRegistration)
        {
            return registration.Lifestyle == otherRegistration.Lifestyle
                && registration.ImplementationType == registration.ImplementationType;
        }
    }
}