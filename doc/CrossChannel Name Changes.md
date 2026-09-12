# CrossChannel Name Changes

This release renames several public APIs. Behavior is unchanged; only names changed.
No `[Obsolete]` aliases are provided, so code referencing the old names will not compile until updated.

The runtime library (`Arc.CrossChannel`) and its bundled source generator must be the same version:
generated brokers call the new names (`RadioServiceRegistry.Register`, `DangerousGetLinks`, `IsOpen`, `TryGetFirst`, `RadioTask.GetEmptyResultTask`, `RadioTask.AggregateAsync`).
Rebuild after upgrading so the generated code is regenerated.

## Runtime API (breaking)

| Kind | Old | New |
|---|---|---|
| Class | `RadioClass` | `LocalRadio` |
| Class | `ChannelRegistry` | `RadioServiceRegistry` |
| Class | `ChannelRegistration` | `RadioServiceRegistration` |
| Class | `GhostCopy` | `FieldCopier` |
| Class | `ServiceCollectionExtensions` | `CrossChannelServiceCollectionExtensions` |
| Attribute | `CrossChannelGeneratorOptionAttribute` (`[CrossChannelGeneratorOption]`) | `CrossChannelGeneratorOptionsAttribute` (`[CrossChannelGeneratorOptions]`) |
| Method | `RadioResult<T>.TryGetSingleResult(out T)` | `RadioResult<T>.TryGetFirst(out T)` |
| Static method | `RadioResult<T>.Single(T)` | `RadioResult<T>.FromValue(T)` |
| Method | `GhostCopy.CreateDelegate<T>()` | `FieldCopier.GetDelegate<T>()` |
| Method | `Channel<TService>.UnsafeGetLinks()` | `Channel<TService>.DangerousGetLinks()` |
| Method | `RadioTask.EmptyResult<T>()` | `RadioTask.GetEmptyResultTask<T>()` |
| Method | `RadioTask.Aggregate<T>(...)` | `RadioTask.AggregateAsync<T>(...)` |
| Property | `Channel<TService>.Link.IsValid` | `Channel<TService>.Link.IsOpen` |
| Property | `ChannelRegistration.CreateBroker` | `RadioServiceRegistration.BrokerFactory` |
| Property | `ChannelRegistration.CreateChannel` | `RadioServiceRegistration.ChannelFactory` |
| Parameter | `ChannelRegistration(..., createBroker, createChannel, ...)` | `RadioServiceRegistration(..., brokerFactory, channelFactory, ...)` |
| Parameter | `AddCrossChannel(bool useRadioClass)` | `AddCrossChannel(bool useLocalRadio)` |
| Parameter | `weakReference` in `Radio.Open`, `Radio.OpenWithKey`, `RadioClass.Open`, `RadioClass.OpenWithKey`, `IChannel<TService>.Open`, `Channel<TService>.Open` | `useWeakReference` |
| Parameter | `from`, `to` in `GhostCopy.Copy<T>` and `GhostCopy.CopyDelegate<T>` | `source`, `destination` |

Unchanged: `Radio`, `Channel<TService>`, `IChannel<TService>`, `ISender<TService>`, `IRadioService`, `RadioServiceAttribute`, `RadioResult<T>` (type), `RadioTask` (type), `FieldCopier.Copy<T>` (method name), `FieldCopier.CopyDelegate<T>` (delegate name), diagnostic IDs `CCG001`-`CCG004`.

## Migration

Apply these replacements to dependent projects (whole-word, case-sensitive). Order matters where noted.

| # | Find (regex) | Replace |
|---|---|---|
| 1 | `\bRadioClass\b` | `LocalRadio` |
| 2 | `\buseRadioClass:` | `useLocalRadio:` |
| 3 | `\bChannelRegistry\b` | `RadioServiceRegistry` |
| 4 | `\bChannelRegistration\b` | `RadioServiceRegistration` |
| 5 | `\bGhostCopy\.CreateDelegate\b` | `FieldCopier.GetDelegate` (before #6) |
| 6 | `\bGhostCopy\b` | `FieldCopier` |
| 7 | `\bCrossChannelGeneratorOption(?=Attribute\|\b)` | `CrossChannelGeneratorOptions` |
| 8 | `\bServiceCollectionExtensions\.AddCrossChannel\b` | `CrossChannelServiceCollectionExtensions.AddCrossChannel` |
| 9 | `\bTryGetSingleResult\b` | `TryGetFirst` |
| 10 | `(RadioResult<[^<>]*(?:<[^<>]*>)?[^<>]*>)\.Single\(` | `$1.FromValue(` |
| 11 | `\bUnsafeGetLinks\b` | `DangerousGetLinks` |
| 12 | `\bRadioTask\.EmptyResult\b` | `RadioTask.GetEmptyResultTask` |
| 13 | `\bRadioTask\.Aggregate\b` | `RadioTask.AggregateAsync` |
| 14 | `\.CreateBroker\b` (on a registration) | `.BrokerFactory` |
| 15 | `\.CreateChannel\b` (on a registration) | `.ChannelFactory` |
| 16 | `\.IsValid\b` (on a `Channel<TService>.Link`) | `.IsOpen` |
| 17 | `\bweakReference:` (named argument to `Open`/`OpenWithKey`) | `useWeakReference:` |
| 18 | `\bfrom:` / `\bto:` (named arguments to `FieldCopier.Copy`) | `source:` / `destination:` |

Notes:

- #14-#18 are ambiguous names; review each match instead of replacing blindly.
- Positional arguments (e.g. `Radio.Open<IService>(instance, true)`, `AddCrossChannel(false)`) need no change.
- `RadioResult<T>.Single(value)` must become `FromValue(value)`. Calls to LINQ `Enumerable.Single()` on a `RadioResult<T>` are unaffected.
- Extension-method call syntax `services.AddCrossChannel()` needs no change; only direct static calls or `using static` references to the class do.
- DI: `provider.GetRequiredService<RadioClass>()` becomes `GetRequiredService<LocalRadio>()`.

## Source generator (internal; affects only contributors)

| Kind | Old | New |
|---|---|---|
| Class | `CrossChannelGeneratorV2` | `CrossChannelGenerator` |
| Class | `IRadioService` (constant holder) | `IRadioServiceMock` |
| Class | `CrossChannelGeneratorOptionAttributeMock` | `CrossChannelGeneratorOptionsAttributeMock` |
| Enum | `CrossChannelObjectFlag` | `CrossChannelObjectFlags` |
| Enum | `ServiceMethod.Type` | `ServiceMethod.MethodReturnKind` |
| Property | `CrossChannelObject.ObjectFlag` | `CrossChannelObject.ObjectFlags` |
| Property | `CrossChannelObject.RadioServiceInterfaceAttribute` | `CrossChannelObject.RadioServiceAttribute` |
| Property | `CrossChannelObject.ClassName` | `CrossChannelObject.BrokerClassName` |
| Property | `ServiceMethod.ReturnType` | `ServiceMethod.ReturnKind` |
| Constant | `ServiceMethod.TaskRadioResultName` | `ServiceMethod.GenericTaskName` |
| Field | `CrossChannelBody.Error_IRadioService` | `CrossChannelBody.Error_NotRadioService` |
| Method | `ServiceMethod.GetParameters()` | `ServiceMethod.GetParameterDeclarations()` |
| Parameter | `obj` in `ServiceMethod.Create` and the `ServiceMethod` constructor | `serviceObject` |

## Renamed files

| Old | New |
|---|---|
| `CrossChannel/Radio/RadioClass.cs` | `CrossChannel/Radio/LocalRadio.cs` |
| `CrossChannel/Channel/ChannelRegistry.cs` | `CrossChannel/Channel/RadioServiceRegistry.cs` |
| `CrossChannel/Channel/ChannelRegistration.cs` | `CrossChannel/Channel/RadioServiceRegistration.cs` |
| `CrossChannel/GhostCopy.cs` | `CrossChannel/FieldCopier.cs` |
| `CrossChannel/ServiceCollectionExtensions.cs` | `CrossChannel/CrossChannelServiceCollectionExtensions.cs` |
| `CrossChannel/GeneratorShared/CrossChannelGeneratorOptionAttribute.cs` | `CrossChannel/GeneratorShared/CrossChannelGeneratorOptionsAttribute.cs` |
| `CrossChannelGenerator/GeneratorShared/CrossChannelGeneratorOptionAttribute.cs` | `CrossChannelGenerator/GeneratorShared/CrossChannelGeneratorOptionsAttributeMock.cs` |
| `CrossChannelGenerator/GeneratorShared/RadioServiceInterfaceAttribute.cs` | `CrossChannelGenerator/GeneratorShared/RadioServiceAttributeMock.cs` |
| `CrossChannelGenerator/GeneratorShared/IRadioService.cs` | `CrossChannelGenerator/GeneratorShared/IRadioServiceMock.cs` |
| `XUnitTest/Tests/ChannelRegistryTest.cs` | `XUnitTest/Tests/RadioServiceRegistryTest.cs` |
| `XUnitTest/Tests/GhostCopyTest.cs` | `XUnitTest/Tests/FieldCopierTest.cs` |
| `Benchmark/Benchmarks/GhostCopyBenchmark.cs` | `Benchmark/Benchmarks/FieldCopierBenchmark.cs` |
