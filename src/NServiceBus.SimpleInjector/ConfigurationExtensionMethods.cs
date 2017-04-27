using System;
using NServiceBus.SimpleInjector;
using SimpleInjector.Lifestyles;

namespace NServiceBus
{
    using NServiceBus.Container;

    /// <summary>
    /// Extension method class to allow configuration of existing container
    /// </summary>
    public static class ConfigurationExtensionMethods
    {
        /// <summary>
        /// Use the a pre-configured SimpleInjector contains
        /// </summary>
        /// <param name="customizations"></param>
        /// <param name="container">The existing container to use.</param>
        public static void UseExistingContainer(this ContainerCustomizations customizations, global::SimpleInjector.Container container)
        {
            if (!container.Options.AllowOverridingRegistrations)
                throw new ArgumentException("Invalid container - must set AllowOverridingRegistrations");
            if (!(container.Options.DefaultScopedLifestyle is AsyncScopedLifestyle))
                throw new ArgumentException("Invalid container - must set DefaultScopedLifestyle to AsyncScopedLifestyle");
            if (!(container.Options.PropertySelectionBehavior is ImplicitPropertyInjectionExtensions.ImplicitPropertyInjectionBehavior))
                throw new ArgumentException("Invalid container - must set AutoWirePropertiesImplicitly");


            customizations.Settings.Set<ContainerHolder>(new ContainerHolder(container));
        }
    }
}