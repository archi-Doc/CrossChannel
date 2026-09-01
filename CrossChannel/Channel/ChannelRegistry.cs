// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace CrossChannel;

/// <summary>
/// Holds the <see cref="ChannelRegistration"/> of every radio service in the process.<br/>
/// Each assembly registers its services from a generated module initializer, so the registry is
/// already populated by the time user code runs.
/// </summary>
public static class ChannelRegistry
{
    private static readonly ConcurrentDictionary<Type, ChannelRegistration> TypeToRegistration = new();

    private static class RegistrationCache<TService>
        where TService : class, IRadioService
    {
        // A field initializer (instead of a static constructor) keeps the type 'beforefieldinit',
        // so the JIT can elide the class initialization check on the hot path.
        public static readonly ChannelRegistration Registration = GetRegistration(typeof(TService));
    }

    /// <summary>
    /// Gets every registration in the process.
    /// </summary>
    public static ICollection<ChannelRegistration> Registrations => TypeToRegistration.Values;

    /// <summary>
    /// Registers a radio service. Called by the generated module initializer.
    /// </summary>
    /// <param name="registration">The registration to add.</param>
    /// <returns><see langword="true"/> if it was added; <see langword="false"/> if the service type is already registered.</returns>
    public static bool Register(ChannelRegistration registration)
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
    public static ChannelRegistration GetRegistration<TService>()
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
    public static ChannelRegistration GetRegistration(Type serviceType)
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
