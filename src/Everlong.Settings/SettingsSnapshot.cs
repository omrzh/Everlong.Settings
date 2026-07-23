namespace Everlong.Settings;

/// <summary>
/// Pairs a shadow copy of a root settings object with the <see cref="ICommittableSettingsStore"/>
/// that backs it, allowing changes to be inspected, committed, or rolled back atomically.
/// </summary>
/// <typeparam name="TRoot">The root settings type (annotated with <c>[Settings]</c>).</typeparam>
/// <param name="Shadow">
/// A freshly initialized copy of the settings object backed by <paramref name="Store"/>.
/// Mutations on <paramref name="Shadow"/> are buffered in <paramref name="Store"/>'s pending write log.
/// </param>
/// <param name="Store">
/// The committable store that records all writes made to <paramref name="Shadow"/>.
/// Call <see cref="ICommittableSettingsStore.Commit"/> to flush changes to the underlying store,
/// or <see cref="ICommittableSettingsStore.Rollback"/> to discard them.
/// </param>
public sealed record SettingsSnapshot<TRoot>(TRoot Shadow, ICommittableSettingsStore Store);
