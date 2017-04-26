using NServiceBus.ObjectBuilder;
using NServiceBus.SimpleInjector;
using SimpleInjector.Lifestyles;

namespace NServiceBus
{
    using Container;
    using ObjectBuilder.SimpleInjector;
    using Settings;

    /// <summary>
    /// SimpleInjector Container
    /// </summary>
    public class SimpleInjectorBuilder : ContainerDefinition
    {
        /// <summary>
        ///     Implementers need to new up a new container.
        /// </summary>
        /// <param name="settings">The settings to check if an existing container exists.</param>
        /// <returns>The new container wrapper.</returns>
        public override ObjectBuilder.Common.IContainer CreateContainer(ReadOnlySettings settings)
        {
            ContainerHolder containerHolder;
            if (settings.TryGet(out containerHolder))
            {
                return new SimpleInjectorObjectBuilder(containerHolder.ExistingContainer);
            }

            return new SimpleInjectorObjectBuilder();
        }
    }

    internal class ContainerHolder
    {
        public ContainerHolder(global::SimpleInjector.Container container)
        {
            ExistingContainer = container.Clone();
        }

        public global::SimpleInjector.Container ExistingContainer { get; }
    }
}