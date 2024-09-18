// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrossChannel;

// Global namespace
[RadioServiceInterface(MaxLinks = 1)]
public interface IConductorPresentationService : IRadioService
{
    void ActivateWindow(bool force = false);
}
