// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Bot.Builder
{
    public interface IBotAdapterServiceFactoryCollection : IEnumerable<KeyValuePair<Type, Func<object>>>
    {
        void Add<TService>(Func<TService> serviceFactory) where TService : class;
    }

    public sealed class BotAdapterServiceFactoryCollection : IBotAdapterServiceFactoryCollection, IServiceProvider
    {
        private readonly Dictionary<Type, Func<object>> _serviceFactories = new Dictionary<Type, Func<object>>();

        public void Add<TService>(Func<TService> serviceFactory) where TService : class =>
            _serviceFactories[typeof(TService)] = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));

        public IEnumerator<KeyValuePair<Type, Func<object>>> GetEnumerator() => _serviceFactories.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _serviceFactories.GetEnumerator();

        public object GetService(Type serviceType)
        {
            _serviceFactories.TryGetValue(serviceType, out var serviceFactory);

            return serviceFactory != null ? serviceFactory() : null;
        }
    }

    public static class BotAdapterServiceFactoryCollectionExtensions
    {
        public static void Add<TService>(this IBotAdapterServiceFactoryCollection serviceCollection, TService service) where TService : class =>
            serviceCollection.Add(() => service);

        public static IEnumerable<Func<TService>> GetFactories<TService>(this IBotAdapterServiceFactoryCollection serviceCollection) where TService : class
        {
            foreach (var entry in serviceCollection)
            {
                if (entry.Value is Func<TService> serviceFactory)
                {
                    yield return serviceFactory;
                }
            }
        }
    }
}