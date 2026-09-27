using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UnityEngine;
using S1API.Lifecycle;

namespace S1API.ExternalHosting
{
    /// <summary>The original device family of an externally hostable app.</summary>
    public enum ExternalAppFamily
    {
        /// <summary>A registered S1API phone app.</summary>
        Phone,
        /// <summary>A registered S1API TV app.</summary>
        TV
    }

    /// <summary>Detached metadata for one S1API app that permits external hosting.</summary>
    public sealed class ExternalAppRegistration
    {
        internal ExternalAppRegistration(ExternalAppFamily family, string id, string title,
            IExternalAppHost host, Func<Sprite?> icon)
        {
            Family = family;
            Id = id;
            Title = title;
            Host = host;
            ResolveIcon = icon;
        }

        /// <summary>The original device family.</summary>
        public ExternalAppFamily Family { get; }
        /// <summary>A stable desktop identity including family, assembly, type, and native app name.</summary>
        public string Id { get; }
        /// <summary>The display title from the S1API app.</summary>
        public string Title { get; }
        /// <summary>The S1API app that creates independent sessions.</summary>
        public IExternalAppHost Host { get; }
        /// <summary>Returns the mod-owned icon, if available, without transferring ownership.</summary>
        public Func<Sprite?> ResolveIcon { get; }
    }

    /// <summary>An app omitted from external hosting and the reason for omission.</summary>
    public sealed class ExternalAppDiagnostic
    {
        internal ExternalAppDiagnostic(ExternalAppFamily family, object source, string message)
        {
            Family = family;
            Source = source;
            TypeName = source.GetType().FullName ?? source.GetType().Name;
            Message = message;
        }

        /// <summary>The original device family.</summary>
        public ExternalAppFamily Family { get; }
        /// <summary>The app's managed type name.</summary>
        public string TypeName { get; }
        /// <summary>Why the app cannot be independently hosted.</summary>
        public string Message { get; }
        internal object Source { get; }
    }

    /// <summary>
    /// Reports S1API phone and TV apps that explicitly support independent display sessions.
    /// Registrations are process-local and callbacks run on the Unity game thread.
    /// </summary>
    public static class ExternalAppCatalog
    {
        private static readonly Dictionary<string, ExternalAppRegistration> Entries =
            new Dictionary<string, ExternalAppRegistration>(StringComparer.Ordinal);
        private static readonly List<ExternalAppDiagnostic> Diagnostics = new List<ExternalAppDiagnostic>();

        static ExternalAppCatalog()
        {
            GameLifecycle.OnPreSceneChange += ClearForSceneChange;
        }

        /// <summary>Raised after the catalog adds, replaces, or removes an entry.</summary>
        public static event Action? Changed;

        /// <summary>Returns a snapshot of currently eligible apps ordered by identity.</summary>
        public static IReadOnlyList<ExternalAppRegistration> GetAll() =>
            Entries.Values.OrderBy(entry => entry.Id, StringComparer.Ordinal).ToArray();

        /// <summary>Returns a snapshot of apps that lack an independent host contract.</summary>
        public static IReadOnlyList<ExternalAppDiagnostic> GetDiagnostics() =>
            Diagnostics.ToArray();

        internal static void Register(ExternalAppFamily family, object app, string name,
            string title, Func<Sprite?> icon) =>
            Register(family, app, () => name, () => title, icon);

        internal static void Register(ExternalAppFamily family, object app, Func<string> name,
            Func<string> title, Func<Sprite?> icon)
        {
            if (!(app is IExternalAppHost host))
            {
                if (!Diagnostics.Any(entry => ReferenceEquals(entry.Source, app)))
                {
                    Diagnostics.Add(new ExternalAppDiagnostic(family, app,
                        "No independent display session. Implement S1API.ExternalHosting.IExternalAppHost to enable hosting."));
                    NotifyChanged();
                }
                return;
            }

            try
            {
                if (!host.AllowExternalHosting)
                    return;

                string nameValue = name();
                string titleValue = title();
                Type type = app.GetType();
                string assembly = type.Assembly.GetName().Name ?? string.Empty;
                string fullName = type.FullName ?? string.Empty;
                if (string.IsNullOrWhiteSpace(assembly) || string.IsNullOrWhiteSpace(fullName) ||
                    string.IsNullOrWhiteSpace(nameValue) || string.IsNullOrWhiteSpace(titleValue))
                {
                    MelonLogger.Warning($"[S1API] External app registration has incomplete identity: {type}.");
                    return;
                }

                string id = $"s1api.{family.ToString().ToLowerInvariant()}.{assembly}.{fullName}.{nameValue}";
                if (Entries.TryGetValue(id, out ExternalAppRegistration? previous) &&
                    !ReferenceEquals(previous.Host, host) && previous.Host.GetType() != type)
                {
                    MelonLogger.Warning($"[S1API] External app identity collision '{id}' between " +
                        $"{previous.Host.GetType().Assembly.FullName} and {type.Assembly.FullName}.");
                    return;
                }

                if (previous != null && ReferenceEquals(previous.Host, host))
                    return;

                Entries[id] = new ExternalAppRegistration(family, id, titleValue.Trim(), host, icon);
                NotifyChanged();
            }
            catch (Exception exception)
            {
                MelonLogger.Warning($"[S1API] External app registration failed for {app.GetType()}: {exception.Message}");
            }
        }

        internal static void Unregister(object app)
        {
            string[] ids = Entries.Values.Where(entry => ReferenceEquals(entry.Host, app))
                .Select(entry => entry.Id).ToArray();
            foreach (string id in ids)
                Entries.Remove(id);
            int removedDiagnostics = Diagnostics.RemoveAll(entry => ReferenceEquals(entry.Source, app));
            if (ids.Length > 0 || removedDiagnostics > 0)
                NotifyChanged();
        }

        internal static void Clear(ExternalAppFamily family)
        {
            string[] ids = Entries.Values.Where(entry => entry.Family == family)
                .Select(entry => entry.Id).ToArray();
            foreach (string id in ids)
                Entries.Remove(id);
            int removedDiagnostics = Diagnostics.RemoveAll(entry => entry.Family == family);
            if (ids.Length > 0 || removedDiagnostics > 0)
                NotifyChanged();
        }

        internal static void ClearForSceneChange()
        {
            if (Entries.Count == 0 && Diagnostics.Count == 0)
                return;
            Entries.Clear();
            Diagnostics.Clear();
            NotifyChanged();
        }

        private static void NotifyChanged()
        {
            Delegate[] listeners = Changed?.GetInvocationList() ?? Array.Empty<Delegate>();
            foreach (Delegate listener in listeners)
            {
                try { ((Action)listener)(); }
                catch (Exception exception)
                {
                    MelonLogger.Warning($"[S1API] External app listener failed: {exception.Message}");
                }
            }
        }
    }
}
