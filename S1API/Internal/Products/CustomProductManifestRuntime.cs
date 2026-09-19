#if IL2CPPMELON
using FishNetBroadcast = Il2CppFishNet.Broadcast;
using FishNetConnection = Il2CppFishNet.Connection;
using FishNetInstanceFinder = Il2CppFishNet.InstanceFinder;
using FishNetSerializing = Il2CppFishNet.Serializing;
using FishNetTransporting = Il2CppFishNet.Transporting;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
#elif MONOMELON
using FishNetBroadcast = FishNet.Broadcast;
using FishNetConnection = FishNet.Connection;
using FishNetInstanceFinder = FishNet.InstanceFinder;
using FishNetSerializing = FishNet.Serializing;
using FishNetTransporting = FishNet.Transporting;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using S1API.Lifecycle;

namespace S1API.Internal.Products
{
    /// <summary>INTERNAL: Owns the host-authoritative manifest handshake.</summary>
    internal static class CustomProductManifestRuntime
    {
        private const int ClientManifestTimeoutSeconds = 15;
        internal const int HostAcknowledgementTimeoutSeconds = 60;
        private static readonly object Gate = new object();
        private static readonly Dictionary<int, PendingHostData>
            PendingHostDataByConnection =
                new Dictionary<int, PendingHostData>();
        private static readonly Dictionary<int, PendingConnection>
            PendingConnections =
                new Dictionary<int, PendingConnection>();
        private static readonly HashSet<int>
            AcceptedConnections =
                new HashSet<int>();
        private static readonly CustomProductManifestClientGate ClientGate =
            new CustomProductManifestClientGate();

        private static bool _serializersConfigured;
        private static bool _hostSubscribed;
        private static bool _clientSubscribed;
        private static bool _clientLifecycleSubscribed;
        private static bool _clientSessionActive;
        private static bool _clientDefinitionsReady;
        private static bool _clientManifestReceived;
        private static bool _hostActive;
        private static bool _hostManifestReady;
        private static bool _hostRequiresValidation;
        private static DateTime _clientDeadline;
        private static int _hostEntryCount;
        private static string _sessionId = string.Empty;
        private static string _hostPayload = string.Empty;
        private static string _hostHash = string.Empty;
        private static CustomProductManifestData? _pendingClientManifest;
        private static CustomProductManifestData? _localClientManifest;
#if IL2CPPMELON
        private static MethodInfo? _broadcastToConnectionMethod;
        private static Type? _broadcastServerManagerType;
        private static Il2CppSystem.Action<FishNetConnection.NetworkConnection,
            CustomProductManifestAckBroadcast>? _acknowledgementReceiver;
        private static Il2CppSystem.Action<FishNetConnection.NetworkConnection,
            FishNetTransporting.RemoteConnectionStateArgs>? _remoteConnectionStateReceiver;
        private static Il2CppSystem.Action<FishNetConnection.NetworkConnection,
            bool>? _authenticationResultReceiver;
        private static Il2CppSystem.Action<CustomProductManifestBroadcast>? _manifestReceiver;
        private static Il2CppSystem.Action<FishNetTransporting.ClientConnectionStateArgs>? _clientConnectionStateReceiver;
        private static Il2CppSystem.Action<FishNetSerializing.Writer,
            CustomProductManifestBroadcast>? _manifestWriter;
        private static Il2CppSystem.Func<FishNetSerializing.Reader,
            CustomProductManifestBroadcast>? _manifestReader;
        private static Il2CppSystem.Action<FishNetSerializing.Writer,
            CustomProductManifestAckBroadcast>? _acknowledgementWriter;
        private static Il2CppSystem.Func<FishNetSerializing.Reader,
            CustomProductManifestAckBroadcast>? _acknowledgementReader;
#endif

        internal static void BeginHostSession()
        {
            ConfigureSerializers();
#if IL2CPPMELON
            CustomProductManifestIl2CppWire.Reset();
#endif
            lock (Gate)
            {
                _hostActive = true;
                _hostManifestReady = false;
                _sessionId = Guid.NewGuid().ToString("N");
                _hostPayload = string.Empty;
                _hostHash = string.Empty;
                _hostRequiresValidation = false;
                PendingConnections.Clear();
                PendingHostDataByConnection.Clear();
                AcceptedConnections.Clear();
                if (!_hostSubscribed)
                {

#if IL2CPPMELON
                    _acknowledgementReceiver ??= DelegateSupport.ConvertDelegate<
                        Il2CppSystem.Action<FishNetConnection.NetworkConnection,
                            CustomProductManifestAckBroadcast>>(
                        new Action<FishNetConnection.NetworkConnection,
                            CustomProductManifestAckBroadcast>(ReceiveAcknowledgement));
                    _remoteConnectionStateReceiver ??= DelegateSupport.ConvertDelegate<
                        Il2CppSystem.Action<FishNetConnection.NetworkConnection,
                            FishNetTransporting.RemoteConnectionStateArgs>>(
                        new Action<FishNetConnection.NetworkConnection,
                            FishNetTransporting.RemoteConnectionStateArgs>(OnRemoteConnectionState));
                    _authenticationResultReceiver ??= DelegateSupport.ConvertDelegate<
                        Il2CppSystem.Action<FishNetConnection.NetworkConnection, bool>>(
                        new Action<FishNetConnection.NetworkConnection, bool>(OnAuthenticationResult));
                    FishNetInstanceFinder.NetworkManager.ServerManager
                        .RegisterBroadcast<CustomProductManifestAckBroadcast>(
                            _acknowledgementReceiver,
                            requireAuthentication: true);
                    FishNetInstanceFinder.NetworkManager.ServerManager
                        .OnRemoteConnectionState += _remoteConnectionStateReceiver;
                    FishNetInstanceFinder.NetworkManager.ServerManager
                        .OnAuthenticationResult += _authenticationResultReceiver;
#else
                    FishNetInstanceFinder.NetworkManager.ServerManager
                        .RegisterBroadcast<CustomProductManifestAckBroadcast>(
                            ReceiveAcknowledgement,
                            requireAuthentication: true);
                    FishNetInstanceFinder.NetworkManager.ServerManager
                        .OnRemoteConnectionState += OnRemoteConnectionState;
                    FishNetInstanceFinder.NetworkManager.ServerManager
                        .OnAuthenticationResult += OnAuthenticationResult;
#endif
                    _hostSubscribed = true;
                }
            }
        }

        internal static void FinalizeHostManifestAfterDescriptorRestore()
        {
            var releasedData = new List<PendingHostData>();
            bool requiresValidation;
            lock (Gate)
            {
                if (!_hostActive || _hostManifestReady)
                    return;

                try
                {
                    CustomProductManifestData manifest =
                        CustomProductDefinitionRegistry.CreateManifest();
                    _hostPayload = manifest.Serialize(_sessionId);
                    _hostHash = manifest.CompatibilityHash;
                    _hostEntryCount = manifest.Entries.Length;
                    _hostRequiresValidation = RequiresValidation(manifest);
                }
                catch (Exception exception)
                {
                    _hostPayload = string.Empty;
                    _hostHash = string.Empty;
                    _hostEntryCount =
                        CustomProductDefinitionRegistry.GetSaveDescriptors().Length;
                    _hostRequiresValidation =
                        _hostEntryCount != 0;
                    Warn("custom-product manifest is unavailable; multiplayer joins will be " +
                         "rejected while single-player loading continues: " +
                         exception.GetType().Name);
                }

                _hostManifestReady = true;
                Info("host manifest finalized after descriptor restore; entries=" +
                     _hostEntryCount);
                if (!_hostRequiresValidation)
                {
                    releasedData.AddRange(PendingHostDataByConnection.Values);
                    PendingHostDataByConnection.Clear();
                }
                requiresValidation = _hostRequiresValidation;
            }

            if (requiresValidation)
                SendManifestToAuthenticatedClients();
            else
            {
                for (int i = 0; i < releasedData.Count; i++)
                    releasedData[i].Invoke();
            }
        }

        internal static void RefreshHostManifestIfReady()
        {
            lock (Gate)
            {
                if (!_hostActive || !_hostManifestReady)
                    return;

                CustomProductManifestData manifest =
                    CustomProductDefinitionRegistry.CreateManifest();
                _hostPayload = manifest.Serialize(_sessionId);
                _hostHash = manifest.CompatibilityHash;
                _hostEntryCount = manifest.Entries.Length;
                _hostRequiresValidation = RequiresValidation(manifest);
                Info("host manifest refreshed after dynamic custom-product registration; entries=" +
                    _hostEntryCount);
            }
        }

        internal static void BeginClientSession()
        {
            ConfigureSerializers();
#if IL2CPPMELON
            CustomProductManifestIl2CppWire.Reset();
#endif
            lock (Gate)
            {
                _clientSessionActive = true;
                _clientDefinitionsReady = false;
                _clientManifestReceived = false;
                _pendingClientManifest = null;
                _localClientManifest = null;
                ClientGate.Begin(requiresValidation: true);
                _clientDeadline = DateTime.MaxValue;
                if (!_clientLifecycleSubscribed)
                {
                    GameLifecycle.OnPreLoad += OnClientDefinitionsReady;
                    _clientLifecycleSubscribed = true;
                }
                if (_clientSubscribed)
                    return;

#if IL2CPPMELON
                _manifestReceiver ??= DelegateSupport.ConvertDelegate<
                    Il2CppSystem.Action<CustomProductManifestBroadcast>>(
                    new Action<CustomProductManifestBroadcast>(ReceiveManifest));
                _clientConnectionStateReceiver ??= DelegateSupport.ConvertDelegate<
                    Il2CppSystem.Action<FishNetTransporting.ClientConnectionStateArgs>>(
                    new Action<FishNetTransporting.ClientConnectionStateArgs>(OnClientConnectionState));
                FishNetInstanceFinder.NetworkManager.ClientManager
                    .RegisterBroadcast<CustomProductManifestBroadcast>(
                        _manifestReceiver);
                FishNetInstanceFinder.NetworkManager.ClientManager
                    .OnClientConnectionState += _clientConnectionStateReceiver;
#else
                FishNetInstanceFinder.NetworkManager.ClientManager
                    .RegisterBroadcast<CustomProductManifestBroadcast>(ReceiveManifest);
                FishNetInstanceFinder.NetworkManager.ClientManager
                    .OnClientConnectionState += OnClientConnectionState;
#endif
                _clientSubscribed = true;
            }
        }

        internal static void EndHostSession()
        {
#if IL2CPPMELON
            CustomProductManifestIl2CppWire.Reset();
#endif
            lock (Gate)
            {
                _hostActive = false;
                _hostManifestReady = false;
                _hostRequiresValidation = false;
                _sessionId = string.Empty;
                _hostPayload = string.Empty;
                _hostHash = string.Empty;
                _hostEntryCount = 0;
                PendingConnections.Clear();
                PendingHostDataByConnection.Clear();
                AcceptedConnections.Clear();
                if (!_hostSubscribed)
                    return;
                if (FishNetInstanceFinder.NetworkManager == null)
                {
                    _hostSubscribed = false;
                    return;
                }

#if IL2CPPMELON
                FishNetInstanceFinder.NetworkManager.ServerManager
                    .UnregisterBroadcast<CustomProductManifestAckBroadcast>(
                        _acknowledgementReceiver);
                FishNetInstanceFinder.NetworkManager.ServerManager
                    .OnRemoteConnectionState -= _remoteConnectionStateReceiver;
                FishNetInstanceFinder.NetworkManager.ServerManager
                    .OnAuthenticationResult -= _authenticationResultReceiver;
#else
                FishNetInstanceFinder.NetworkManager.ServerManager
                    .UnregisterBroadcast<CustomProductManifestAckBroadcast>(
                        ReceiveAcknowledgement);
                FishNetInstanceFinder.NetworkManager.ServerManager
                    .OnRemoteConnectionState -= OnRemoteConnectionState;
                FishNetInstanceFinder.NetworkManager.ServerManager
                    .OnAuthenticationResult -= OnAuthenticationResult;
#endif
                _hostSubscribed = false;
            }
        }

        internal static void EndClientSession()
        {
#if IL2CPPMELON
            CustomProductManifestIl2CppWire.Reset();
#endif
            lock (Gate)
            {
                _clientSessionActive = false;
                _clientDefinitionsReady = false;
                _clientManifestReceived = false;
                _pendingClientManifest = null;
                _localClientManifest = null;
                ClientGate.End();
                if (_clientLifecycleSubscribed)
                {
                    GameLifecycle.OnPreLoad -= OnClientDefinitionsReady;
                    _clientLifecycleSubscribed = false;
                }
                if (!_clientSubscribed)
                    return;
                if (FishNetInstanceFinder.NetworkManager == null)
                {
                    _clientSubscribed = false;
                    return;
                }

#if IL2CPPMELON
                FishNetInstanceFinder.NetworkManager.ClientManager
                    .UnregisterBroadcast<CustomProductManifestBroadcast>(
                        _manifestReceiver);
                FishNetInstanceFinder.NetworkManager.ClientManager
                    .OnClientConnectionState -= _clientConnectionStateReceiver;
#else
                FishNetInstanceFinder.NetworkManager.ClientManager
                    .UnregisterBroadcast<CustomProductManifestBroadcast>(ReceiveManifest);
                FishNetInstanceFinder.NetworkManager.ClientManager
                    .OnClientConnectionState -= OnClientConnectionState;
#endif
                _clientSubscribed = false;
            }
        }

        internal static void Tick()
        {
            DateTime now = DateTime.UtcNow;
            bool rejectClient;
            var rejectedConnections = new List<PendingConnection>();
            lock (Gate)
            {
                rejectClient = ShouldRejectClientForMissingManifest(
                    ClientGate.IsWaiting,
                    _clientManifestReceived,
                    now,
                    _clientDeadline);
                if (rejectClient)
                    ClientGate.End();

                foreach (KeyValuePair<int, PendingConnection> pair
                    in PendingConnections)
                {
                    if (now >= pair.Value.Deadline)
                        rejectedConnections.Add(pair.Value);
                }

                for (int i = 0; i < rejectedConnections.Count; i++)
                {
                    int connectionId = rejectedConnections[i].ConnectionId;
                    PendingConnections.Remove(connectionId);
                    PendingHostDataByConnection.Remove(connectionId);
                }
            }

            if (rejectClient)
            {
                Warn("custom-product manifest was not received before player data; disconnecting safely");
                FishNetInstanceFinder.NetworkManager?.ClientManager.StopConnection();
            }

            for (int i = 0; i < rejectedConnections.Count; i++)
            {
                Warn("custom-product manifest acknowledgement timed out; rejecting the joining client");
                rejectedConnections[i].Connection.Disconnect(true);
            }
        }

        internal static bool AuthorizeClientPlayerDataRequest(Action request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            lock (Gate)
            {
                if (!FishNetInstanceFinder.IsClient || FishNetInstanceFinder.IsServer)
                {
                    return true;
                }

                bool authorized = ClientGate.AuthorizePlayerDataRequest(request);
                if (ShouldStartClientManifestDeadline(
                        authorized,
                        _clientDeadline))
                {
                    _clientDeadline = DateTime.UtcNow.AddSeconds(ClientManifestTimeoutSeconds);
                    Info("client player-data request deferred until manifest validation");
                }
                return authorized;
            }
        }

        internal static bool ShouldStartClientManifestDeadline(
            bool authorized,
            DateTime currentDeadline) =>
            !authorized && currentDeadline == DateTime.MaxValue;

        internal static bool ShouldRejectClientForMissingManifest(
            bool isWaiting,
            bool manifestReceived,
            DateTime now,
            DateTime deadline) =>
            isWaiting && !manifestReceived && now >= deadline;

        internal static bool AuthorizeHostPlayerData(
            object player,
            FishNetConnection.NetworkConnection connection,
            object[] arguments,
            MethodBase originalMethod)
        {
            if (player == null || connection == null || arguments == null ||
                originalMethod == null)
            {
                return true;
            }

            int connectionId = connection.ClientId;
            lock (Gate)
            {
                if (!_hostActive)
                    return true;
                if (!_hostManifestReady)
                {
                    if (!PendingHostDataByConnection.ContainsKey(connectionId))
                    {
                        PendingHostDataByConnection.Add(
                            connectionId,
                            new PendingHostData(
                                player,
                                (object[])arguments.Clone(),
                                originalMethod));
                    }
                    return false;
                }
                if (!_hostRequiresValidation)
                    return true;

                if (AcceptedConnections.Contains(connectionId))
                    return true;
            }

            bool manifestAvailable = SendManifest(connection);
            lock (Gate)
            {
                if (AcceptedConnections.Contains(connectionId))
                    return true;
                if (!manifestAvailable)
                    return false;

                if (!PendingHostDataByConnection.ContainsKey(connectionId))
                {
                    PendingHostDataByConnection.Add(
                        connectionId,
                        new PendingHostData(
                            player,
                            (object[])arguments.Clone(),
                            originalMethod));
                }
                return false;
            }
        }

        internal static string GetLocalCompatibilityHash() =>
            CustomProductDefinitionRegistry.CreateManifest().CompatibilityHash;

        private static void ConfigureSerializers()
        {
            lock (Gate)
            {
                if (_serializersConfigured)
                    return;

#if IL2CPPMELON
                _manifestWriter ??= DelegateSupport.ConvertDelegate<
                    Il2CppSystem.Action<FishNetSerializing.Writer,
                        CustomProductManifestBroadcast>>(
                    new Action<FishNetSerializing.Writer,
                        CustomProductManifestBroadcast>(WriteManifest));
                _manifestReader ??= DelegateSupport.ConvertDelegate<
                    Il2CppSystem.Func<FishNetSerializing.Reader,
                        CustomProductManifestBroadcast>>(
                    new Func<FishNetSerializing.Reader,
                        CustomProductManifestBroadcast>(ReadManifest));
                _acknowledgementWriter ??= DelegateSupport.ConvertDelegate<
                    Il2CppSystem.Action<FishNetSerializing.Writer,
                        CustomProductManifestAckBroadcast>>(
                    new Action<FishNetSerializing.Writer,
                        CustomProductManifestAckBroadcast>(WriteAcknowledgement));
                _acknowledgementReader ??= DelegateSupport.ConvertDelegate<
                    Il2CppSystem.Func<FishNetSerializing.Reader,
                        CustomProductManifestAckBroadcast>>(
                    new Func<FishNetSerializing.Reader,
                        CustomProductManifestAckBroadcast>(ReadAcknowledgement));
                FishNetSerializing.GenericWriter<CustomProductManifestBroadcast>.Write =
                    _manifestWriter;
                FishNetSerializing.GenericReader<CustomProductManifestBroadcast>.Read =
                    _manifestReader;
                FishNetSerializing.GenericWriter<CustomProductManifestAckBroadcast>.Write =
                    _acknowledgementWriter;
                FishNetSerializing.GenericReader<CustomProductManifestAckBroadcast>.Read =
                    _acknowledgementReader;
#else
                FishNetSerializing.GenericWriter<CustomProductManifestBroadcast>.Write =
                    WriteManifest;
                FishNetSerializing.GenericReader<CustomProductManifestBroadcast>.Read =
                    ReadManifest;
                FishNetSerializing.GenericWriter<CustomProductManifestAckBroadcast>.Write =
                    WriteAcknowledgement;
                FishNetSerializing.GenericReader<CustomProductManifestAckBroadcast>.Read =
                    ReadAcknowledgement;
#endif
                _serializersConfigured = true;
            }
        }

        private static void OnRemoteConnectionState(
            FishNetConnection.NetworkConnection connection,
            FishNetTransporting.RemoteConnectionStateArgs arguments)
        {
            if (arguments.ConnectionState == FishNetTransporting.RemoteConnectionState.Stopped)
            {
                int connectionId = connection.ClientId;
                lock (Gate)
                {
                    PendingConnections.Remove(connectionId);
                    PendingHostDataByConnection.Remove(connectionId);
                    AcceptedConnections.Remove(connectionId);
                }
            }
        }

        private static void OnAuthenticationResult(
            FishNetConnection.NetworkConnection connection,
            bool authenticated)
        {
            if (authenticated)
                SendManifestOrReject(connection);
        }

        private static void SendManifestToAuthenticatedClients()
        {
            if (FishNetInstanceFinder.NetworkManager == null)
                return;

            foreach (var pair in FishNetInstanceFinder.NetworkManager.ServerManager.Clients)
            {
                FishNetConnection.NetworkConnection connection = pair.Value;
                if (connection != null && connection.Authenticated && !connection.IsLocalClient)
                    SendManifestOrReject(connection);
            }
        }

        private static void SendManifestOrReject(
            FishNetConnection.NetworkConnection connection)
        {
            if (SendManifest(connection))
                return;

            bool reject;
            lock (Gate)
                reject = _hostActive && _hostManifestReady && _hostRequiresValidation;
            if (!reject)
                return;

            Warn("custom-product manifest is unavailable; rejecting the joining client");
            connection.Disconnect(true);
        }

        private static bool SendManifest(FishNetConnection.NetworkConnection connection)
        {
            if (connection == null || connection.IsLocalClient || !connection.Authenticated)
                return false;

            int connectionId = connection.ClientId;
            string payload;
            lock (Gate)
            {
                if (!_hostActive || !_hostManifestReady || !_hostRequiresValidation ||
                    string.IsNullOrEmpty(_hostPayload))
                {
                    return false;
                }

                if (PendingConnections.ContainsKey(connectionId) ||
                    AcceptedConnections.Contains(connectionId))
                {
                    return true;
                }

                payload = _hostPayload;
                PendingConnections[connectionId] = new PendingConnection(
                    connectionId,
                    connection,
                    DateTime.UtcNow.AddSeconds(HostAcknowledgementTimeoutSeconds));
            }

            try
            {
#if IL2CPPMELON
                CustomProductManifestIl2CppWire.PrepareOutgoingManifest(payload);
                BroadcastToConnection(
                    FishNetInstanceFinder.NetworkManager.ServerManager,
                    connection,
                    new CustomProductManifestBroadcast());
#else
                FishNetInstanceFinder.NetworkManager.ServerManager.Broadcast(
                    connection,
                    new CustomProductManifestBroadcast { Payload = payload },
                    requireAuthenticated: true,
                    channel: FishNetTransporting.Channel.Reliable);
#endif
                Info("host manifest sent to authenticated client; entries=" +
                     _hostEntryCount);
                return true;
            }
            catch (Exception exception)
            {
#if IL2CPPMELON
                CustomProductManifestIl2CppWire.ClearOutgoingManifest();
#endif
                lock (Gate)
                    PendingConnections.Remove(connectionId);
                Warn("custom-product manifest could not be sent; rejecting the joining client: " +
                     exception.GetType().Name);
                connection.Disconnect(true);
                return false;
            }
        }

#if IL2CPPMELON
        private static void BroadcastToConnection<T>(object serverManager, object connection, T message)
        {
            MethodInfo method = ResolveBroadcastToConnectionMethod(serverManager.GetType());
            method.MakeGenericMethod(typeof(T)).Invoke(serverManager, new object[]
            {
                connection,
                message!,
                true,
                FishNetTransporting.Channel.Reliable
            });
        }

        private static MethodInfo ResolveBroadcastToConnectionMethod(Type serverManagerType)
        {
            lock (Gate)
            {
                if (_broadcastToConnectionMethod != null &&
                    _broadcastServerManagerType == serverManagerType)
                {
                    return _broadcastToConnectionMethod;
                }

                foreach (MethodInfo method in serverManagerType.GetMethods(
                             BindingFlags.Instance | BindingFlags.Public))
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    if (method.Name != "Broadcast" ||
                        !method.IsGenericMethodDefinition ||
                        method.GetGenericArguments().Length != 1 ||
                        parameters.Length != 4 ||
                        parameters[0].ParameterType !=
                        typeof(FishNetConnection.NetworkConnection) ||
                        !parameters[1].ParameterType.IsGenericParameter ||
                        parameters[2].ParameterType != typeof(bool) ||
                        parameters[3].ParameterType !=
                        typeof(FishNetTransporting.Channel))
                    {
                        continue;
                    }

                    _broadcastServerManagerType = serverManagerType;
                    _broadcastToConnectionMethod = method;
                    return method;
                }
            }

            throw new MissingMethodException(
                "FishNet ServerManager.Broadcast<T>(NetworkConnection, T, bool, Channel)");
        }
#endif

        private static void ReceiveManifest(CustomProductManifestBroadcast message)
        {
#if IL2CPPMELON
            string payload = CustomProductManifestIl2CppWire.TakeIncomingManifest();
#else
            string payload = message.Payload;
#endif
            if (!CustomProductManifestData.TryDeserialize(
                    payload,
                    out CustomProductManifestData manifest,
                    out string failure))
            {
                RejectClient("received " + failure);
                return;
            }

            bool conflictingPendingManifest = false;
            bool queuedUntilDefinitionsReady = false;
            lock (Gate)
            {
                if (!_clientSessionActive)
                    return;
                _clientManifestReceived = true;
                if (!_clientDefinitionsReady)
                {
                    if (_pendingClientManifest != null &&
                        (!string.Equals(
                            _pendingClientManifest.SessionId,
                            manifest.SessionId,
                            StringComparison.Ordinal) ||
                         !string.Equals(
                            _pendingClientManifest.CompatibilityHash,
                            manifest.CompatibilityHash,
                            StringComparison.Ordinal)))
                    {
                        conflictingPendingManifest = true;
                    }
                    else
                    {
                        _pendingClientManifest = manifest;
                        queuedUntilDefinitionsReady = true;
                    }
                }
            }

            if (conflictingPendingManifest)
            {
                RejectClient("received conflicting manifests before local definitions were ready");
                return;
            }
            if (queuedUntilDefinitionsReady)
            {
                Info("client manifest queued until pre-load custom definitions are registered");
                return;
            }

            ProcessManifest(manifest);
        }

        private static void OnClientDefinitionsReady()
        {
            CustomProductManifestData localManifest;
            try
            {
                localManifest = CustomProductDefinitionRegistry.CreateManifest();
            }
            catch (Exception exception)
            {
                RejectClient(
                    "local custom-product registrations cannot be represented safely: " +
                    exception.GetType().Name);
                return;
            }

            CustomProductManifestData? pending;
            lock (Gate)
            {
                if (!_clientSessionActive)
                    return;
                _localClientManifest = localManifest;
                _clientDefinitionsReady = true;
                ClientGate.Begin(RequiresValidation(localManifest));
                pending = _pendingClientManifest;
                _pendingClientManifest = null;
            }

            Info("client pre-load custom definitions ready; entries=" +
                 localManifest.Entries.Length);
            if (pending != null)
                ProcessManifest(pending);
        }

        internal static bool RequiresValidation(
            CustomProductManifestData manifest) =>
            manifest.Entries.Length != 0 ||
            manifest.MixingProfiles.Length != 0;

        private static void ProcessManifest(CustomProductManifestData manifest)
        {
            CustomProductManifestData localManifest;
            lock (Gate)
            {
                if (!_clientSessionActive || !_clientDefinitionsReady ||
                    _localClientManifest == null)
                {
                    return;
                }
                localManifest = _localClientManifest;
            }

            for (int i = 0; i < manifest.GeneratedDescriptors.Length; i++)
            {
                if (!CustomProductSavePersistence.TryRestoreGeneratedDescriptorFromNetwork(
                        manifest.GeneratedDescriptors[i]))
                {
                    RejectClient("generated custom-product descriptor could not be restored safely");
                    return;
                }
            }

            try
            {
                localManifest = CustomProductDefinitionRegistry.CreateManifest();
                lock (Gate)
                    _localClientManifest = localManifest;
            }
            catch (Exception exception)
            {
                RejectClient("generated custom-product manifest could not be recreated: " +
                    exception.GetType().Name);
                return;
            }

            string localHash = localManifest.CompatibilityHash;
            Action? deferredRequest;
            CustomProductManifestAcceptance acceptance;
            lock (Gate)
            {
                acceptance = ClientGate.Accept(
                    manifest.CompatibilityHash,
                    localHash,
                    out deferredRequest);
            }

            if (acceptance == CustomProductManifestAcceptance.AlreadyAccepted)
                return;
            if (acceptance == CustomProductManifestAcceptance.Incompatible)
            {
                RejectClient(
                    CustomProductManifestData.DescribeCompatibilityMismatch(
                        manifest,
                        localManifest));
                return;
            }

#if IL2CPPMELON
            try
            {
                CustomProductManifestIl2CppWire.PrepareOutgoingAcknowledgement(
                    manifest.SessionId,
                    manifest.CompatibilityHash);
                FishNetInstanceFinder.NetworkManager.ClientManager.Broadcast(
                    new CustomProductManifestAckBroadcast(),
                    FishNetTransporting.Channel.Reliable);
            }
            catch (Exception exception)
            {
                CustomProductManifestIl2CppWire.ClearOutgoingAcknowledgement();
                RejectClient(
                    "manifest acknowledgement could not be sent safely: " +
                    exception.GetType().Name);
                return;
            }
#else
            FishNetInstanceFinder.NetworkManager.ClientManager.Broadcast(
                new CustomProductManifestAckBroadcast
                {
                    SessionId = manifest.SessionId,
                    CompatibilityHash = manifest.CompatibilityHash
                },
                FishNetTransporting.Channel.Reliable);
#endif
            Info("client manifest accepted before player-data request; entries=" +
                 manifest.Entries.Length);
            deferredRequest?.Invoke();
        }

        private static void ReceiveAcknowledgement(
            FishNetConnection.NetworkConnection connection,
            CustomProductManifestAckBroadcast acknowledgement)
        {
#if IL2CPPMELON
            (string acknowledgementSessionId, string acknowledgementCompatibilityHash) =
                CustomProductManifestIl2CppWire.TakeIncomingAcknowledgement();
#else
            string acknowledgementSessionId = acknowledgement.SessionId;
            string acknowledgementCompatibilityHash = acknowledgement.CompatibilityHash;
#endif
            PendingHostData? pending = null;
            string? rejection = null;
            int connectionId = connection.ClientId;
            lock (Gate)
            {
                if (!CustomProductManifestData.IsSessionId(acknowledgementSessionId) ||
                    !CustomProductManifestData.IsHash(acknowledgementCompatibilityHash))
                {
                    rejection = "manifest acknowledgement bounds are invalid";
                }
                else if (!_hostActive ||
                    !string.Equals(acknowledgementSessionId, _sessionId, StringComparison.Ordinal) ||
                    !string.Equals(
                        acknowledgementCompatibilityHash,
                        _hostHash,
                        StringComparison.Ordinal))
                {
                    rejection = "manifest acknowledgement does not match this host session";
                }
                else if (AcceptedConnections.Contains(connectionId))
                {
                    return;
                }
                else if (!PendingConnections.Remove(connectionId))
                {
                    rejection = "manifest acknowledgement was unsolicited or replayed";
                }
                else
                {
                    AcceptedConnections.Add(connectionId);
                    if (PendingHostDataByConnection.TryGetValue(connectionId, out pending))
                        PendingHostDataByConnection.Remove(connectionId);
                }
            }

            if (rejection != null)
            {
                RejectConnection(connection, rejection);
                return;
            }

            Info("host manifest acknowledgement accepted; deferredPlayerData=" +
                 (pending != null));
            pending?.Invoke();
        }

        private static void OnClientConnectionState(
            FishNetTransporting.ClientConnectionStateArgs arguments)
        {
            if (arguments.ConnectionState == FishNetTransporting.LocalConnectionState.Stopping ||
                arguments.ConnectionState == FishNetTransporting.LocalConnectionState.Stopped)
            {
                EndClientSession();
            }
        }

        private static void RejectClient(string reason)
        {
            Warn("custom-product manifest rejected: " + reason);
            lock (Gate)
            {
                ClientGate.End();
                _clientSessionActive = false;
                _clientDefinitionsReady = false;
                _clientManifestReceived = false;
                _pendingClientManifest = null;
                _localClientManifest = null;
            }
            FishNetInstanceFinder.NetworkManager?.ClientManager.StopConnection();
        }

        private static void RejectConnection(
            FishNetConnection.NetworkConnection connection,
            string reason)
        {
            int connectionId = connection.ClientId;
            lock (Gate)
            {
                PendingConnections.Remove(connectionId);
                PendingHostDataByConnection.Remove(connectionId);
                AcceptedConnections.Remove(connectionId);
            }
            Warn("custom-product manifest acknowledgement rejected: " + reason);
            connection.Disconnect(true);
        }

        private static void WriteManifest(
            FishNetSerializing.Writer writer,
            CustomProductManifestBroadcast value)
        {
#if IL2CPPMELON
            writer.WriteString(CustomProductManifestIl2CppWire.TakeOutgoingManifest());
#else
            string payload = value.Payload;
            writer.WriteString(payload);
#endif
        }

        private static CustomProductManifestBroadcast ReadManifest(
            FishNetSerializing.Reader reader)
        {
#if IL2CPPMELON
            CustomProductManifestIl2CppWire.SetIncomingManifest(reader.ReadString());
            return new CustomProductManifestBroadcast();
#else
            return new CustomProductManifestBroadcast
            {
                Payload = reader.ReadString()
            };
#endif
        }

        private static void WriteAcknowledgement(
            FishNetSerializing.Writer writer,
            CustomProductManifestAckBroadcast value)
        {
#if IL2CPPMELON
            (string sessionId, string compatibilityHash) =
                CustomProductManifestIl2CppWire.TakeOutgoingAcknowledgement();
#else
            string sessionId = value.SessionId;
            string compatibilityHash = value.CompatibilityHash;
#endif
            writer.WriteString(sessionId);
            writer.WriteString(compatibilityHash);
        }

        private static CustomProductManifestAckBroadcast ReadAcknowledgement(
            FishNetSerializing.Reader reader)
        {
#if IL2CPPMELON
            CustomProductManifestIl2CppWire.SetIncomingAcknowledgement(
                reader.ReadString(),
                reader.ReadString());
            return new CustomProductManifestAckBroadcast();
#else
            return new CustomProductManifestAckBroadcast
            {
                SessionId = reader.ReadString(),
                CompatibilityHash = reader.ReadString()
            };
#endif
        }

        private static void Warn(string message)
        {
            try { MelonLoader.MelonLogger.Warning("[CustomProductManifest] " + message); }
            catch (Exception) { }
        }

        private static void Info(string message)
        {
            try { MelonLoader.MelonLogger.Msg("[CustomProductManifest] " + message); }
            catch (Exception) { }
        }

        private sealed class PendingHostData
        {
            private readonly object _player;
            private readonly object[] _arguments;
            private readonly MethodBase _method;

            internal PendingHostData(
                object player,
                object[] arguments,
                MethodBase method)
            {
                _player = player;
                _arguments = arguments;
                _method = method;
            }

            internal void Invoke()
            {
                _method.Invoke(_player, _arguments);
            }
        }

        private sealed class PendingConnection
        {
            internal PendingConnection(
                int connectionId,
                FishNetConnection.NetworkConnection connection,
                DateTime deadline)
            {
                ConnectionId = connectionId;
                Connection = connection;
                Deadline = deadline;
            }

            internal int ConnectionId { get; }
            internal FishNetConnection.NetworkConnection Connection { get; }
            internal DateTime Deadline { get; }
        }
    }

#if IL2CPPMELON
    [RegisterTypeInIl2Cpp]
    [Il2CppImplements(typeof(FishNetBroadcast.IBroadcast))]
    internal sealed class CustomProductManifestBroadcast : Il2CppSystem.Object
    {
        public CustomProductManifestBroadcast(IntPtr pointer)
            : base(pointer)
        {
        }

        public CustomProductManifestBroadcast()
            : base(ClassInjector.DerivedConstructorPointer<CustomProductManifestBroadcast>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }
    }

    [RegisterTypeInIl2Cpp]
    [Il2CppImplements(typeof(FishNetBroadcast.IBroadcast))]
    internal sealed class CustomProductManifestAckBroadcast : Il2CppSystem.Object
    {
        public CustomProductManifestAckBroadcast(IntPtr pointer)
            : base(pointer)
        {
        }

        public CustomProductManifestAckBroadcast()
            : base(ClassInjector.DerivedConstructorPointer<CustomProductManifestAckBroadcast>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }
    }

    internal static class CustomProductManifestIl2CppWire
    {
        private static readonly object Sync = new object();
        private static string? _outgoingManifest;
        private static string? _incomingManifest;
        private static (string SessionId, string CompatibilityHash)? _outgoingAcknowledgement;
        private static (string SessionId, string CompatibilityHash)? _incomingAcknowledgement;

        internal static void PrepareOutgoingManifest(string payload) =>
            Set(ref _outgoingManifest, payload, "outgoing manifest");

        internal static string TakeOutgoingManifest() =>
            Take(ref _outgoingManifest, "outgoing manifest");

        internal static void ClearOutgoingManifest() =>
            Clear(ref _outgoingManifest);

        internal static void SetIncomingManifest(string payload) =>
            Set(ref _incomingManifest, payload, "incoming manifest");

        internal static string TakeIncomingManifest() =>
            Take(ref _incomingManifest, "incoming manifest");

        internal static void PrepareOutgoingAcknowledgement(
            string sessionId,
            string compatibilityHash) =>
            Set(
                ref _outgoingAcknowledgement,
                (sessionId, compatibilityHash),
                "outgoing acknowledgement");

        internal static (string SessionId, string CompatibilityHash)
            TakeOutgoingAcknowledgement() =>
                Take(ref _outgoingAcknowledgement, "outgoing acknowledgement");

        internal static void ClearOutgoingAcknowledgement() =>
            Clear(ref _outgoingAcknowledgement);

        internal static void SetIncomingAcknowledgement(
            string sessionId,
            string compatibilityHash) =>
            Set(
                ref _incomingAcknowledgement,
                (sessionId, compatibilityHash),
                "incoming acknowledgement");

        internal static (string SessionId, string CompatibilityHash)
            TakeIncomingAcknowledgement() =>
                Take(ref _incomingAcknowledgement, "incoming acknowledgement");

        internal static void Reset()
        {
            lock (Sync)
            {
                _outgoingManifest = null;
                _incomingManifest = null;
                _outgoingAcknowledgement = null;
                _incomingAcknowledgement = null;
            }
        }

        private static void Set<T>(ref T? slot, T value, string name)
            where T : struct
        {
            lock (Sync)
            {
                if (slot.HasValue)
                    throw new InvalidOperationException(
                        "IL2CPP custom-product " + name + " overlapped another message.");
                slot = value;
            }
        }

        private static void Set(ref string? slot, string value, string name)
        {
            lock (Sync)
            {
                if (slot != null)
                    throw new InvalidOperationException(
                        "IL2CPP custom-product " + name + " overlapped another message.");
                slot = value;
            }
        }

        private static T Take<T>(ref T? slot, string name)
            where T : struct
        {
            lock (Sync)
            {
                if (!slot.HasValue)
                    throw new InvalidOperationException(
                        "IL2CPP custom-product " + name + " was not prepared.");
                T value = slot.Value;
                slot = null;
                return value;
            }
        }

        private static string Take(ref string? slot, string name)
        {
            lock (Sync)
            {
                string value = slot
                    ?? throw new InvalidOperationException(
                        "IL2CPP custom-product " + name + " was not prepared.");
                slot = null;
                return value;
            }
        }

        private static void Clear<T>(ref T? slot)
            where T : struct
        {
            lock (Sync)
                slot = null;
        }

        private static void Clear(ref string? slot)
        {
            lock (Sync)
                slot = null;
        }
    }
#else
    internal struct CustomProductManifestBroadcast : FishNetBroadcast.IBroadcast
    {
        internal string Payload { get; set; }
    }

    internal struct CustomProductManifestAckBroadcast : FishNetBroadcast.IBroadcast
    {
        internal string SessionId { get; set; }
        internal string CompatibilityHash { get; set; }
    }
#endif
}
