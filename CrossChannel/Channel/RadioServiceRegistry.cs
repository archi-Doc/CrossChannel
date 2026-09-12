// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Threading;

namespace CrossChannel;

/// <summary>
/// Holds the <see cref="RadioServiceRegistration"/> of every radio service in the process.<br/>
/// Generated module initializers add services as their modules initialize.
/// </summary>
public static class RadioServiceRegistry
{
    private static readonly ConcurrentDictionary<Type, RadioServiceRegistration> TypeToRegistration = new();

    private static class RegistrationCache<TService>
        where TService : class, IRadioService
    {
        private static RadioServiceRegistration? registration;

        public static RadioServiceRegistration Registration
        {
            get
            {
                var value = Volatile.Read(ref registration);
                if (value is null)
                {
                    value = GetRegistration(typeof(TService));
                    Volatile.Write(ref registration, value);
                }

                return value;
            }
        }
    }

    /// <summary>
    /// Gets a snapshot of the registrations currently in the process.
    /// </summary>
    public static ICollection<RadioServiceRegistration> Registrations => TypeToRegistration.Values;

    /// <summary>
    /// Registers a radio service. Called by the generated module initializer.
    /// </summary>
    /// <param name="registration">The registration to add.</param>
    /// <returns><see langword="true"/> if it was added; <see langword="false"/> if the service type is already registered.</returns>
    public static bool Register(RadioServiceRegistration registration)
    {
        return TypeToRegistration.TryAdd(registration.ServiceType, registration);
    }

    /// <summary>
    /// Gets the registration of the specified service.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>The registration.</returns>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RadioServiceRegistration GetRegistration<TService>()
        where TService : class, IRadioService
    {
        return RegistrationCache<TService>.Registration;
    }

    /// <summary>
    /// Gets the registration of the specified service.
    /// </summary>
    /// <param name="serviceType">The type of the service.</param>
    /// <returns>The registration.</returns>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    public static RadioServiceRegistration GetRegistration(Type serviceType)
    {
        if (TypeToRegistration.TryGetValue(serviceType, out var registration))
        {
            return registration;
        }
        else
        {
            throw new InvalidOperationException($"The radio service {serviceType.FullName} has not been registered. Make sure the interface has the RadioService attribute and derives from IRadioService.");
        }
    }

    /// <summary>
    /// Gets the shared channel which accepts no link.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>The empty channel.</returns>
    /// <exception cref="InvalidOperationException">The service type is not registered.</exception>
    public static Channel<TService> GetEmptyChannel<TService>()
        where TService : class, IRadioService
        => (Channel<TService>)RegistrationCache<TService>.Registration.EmptyChannel;
}
