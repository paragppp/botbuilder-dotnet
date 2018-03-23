// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Bot.Builder
{
    public interface IBotAdapterServiceFactoryCollection : IEnumerable<KeyValuePair<string, Func<object>>>
    {
        void Add<TService>(string key, Func<TService> serviceFactory) where TService : class;

        TService Get<TService>(string key) where TService : class;
    }

    public sealed class BotAdapterServiceFactoryCollection : IBotAdapterServiceFactoryCollection
    {
        private readonly Dictionary<string, Func<object>> _services = new Dictionary<string, Func<object>>();

        public TService Get<TService>(string key) where TService : class
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _services.TryGetValue(key, out var serviceFactory);

            return serviceFactory != null ? serviceFactory() as TService : default(TService);
        }

        public void Add<TService>(string key, Func<TService> serviceFactory) where TService : class
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (serviceFactory == null) throw new ArgumentNullException(nameof(serviceFactory));

            try
            {
                _services.Add(key, serviceFactory);
            }
            catch (ArgumentException)
            {
                throw new ServiceKeyAlreadyRegisteredException(key);
            }
        }

        public IEnumerator<KeyValuePair<string, Func<object>>> GetEnumerator() => _services.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _services.GetEnumerator();
    }

    public static class BotAdapterCollectionExtensions
    {
        public static void Add<TService>(this IBotAdapterServiceFactoryCollection serviceCollection, TService service) where TService : class =>
            serviceCollection.Add(typeof(TService).FullName, () => service);

        public static TService Get<TService>(this IBotAdapterServiceFactoryCollection serviceCollection) where TService : class =>
            serviceCollection.Get<TService>(typeof(TService).FullName);

        public static IEnumerable<KeyValuePair<string, Func<TService>>> GetServiceFactories<TService>(this IBotAdapterServiceFactoryCollection serviceCollection) where TService : class
        {
            foreach (var entry in serviceCollection)
            {
                if (entry.Value is Func<TService> service)
                {
                    yield return new KeyValuePair<string, Func<TService>>(entry.Key, service);
                }
            }
        }
    }
}