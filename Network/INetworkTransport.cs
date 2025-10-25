namespace PulseBot.Network;

/// <summary>
/// Transport-agnostic network interface for bot communication.
/// Implementations: TCP (recommended), UDP (legacy), WebSocket (future).
/// </summary>
public interface INetworkTransport
{
    /// <summary>
    /// Server command received with Will identifier and JSON payload.
    /// </summary>
    event Action<string, object?>? OnCommand;

    /// <summary>
    /// Network error occurred (connection failure, send/receive errors).
    /// </summary>
    event Action<Exception>? OnError;

    /// <summary>
    /// Successfully connected to server.
    /// </summary>
    event Action? OnConnected;

    /// <summary>
    /// Disconnected from server (graceful or unexpected).
    /// </summary>
    event Action? OnDisconnected;

    /// <summary>
    /// Current connection state.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Start network connection and begin listening for messages.
    /// </summary>
    void Start();

    /// <summary>
    /// Stop network connection and cleanup resources.
    /// </summary>
    void Stop();

    /// <summary>
    /// Send command to server with optional payload.
    /// </summary>
    /// <param name="will">Command name (e.g., "SEND_MESSAGE", "ADD_REACTION")</param>
    /// <param name="payload">Optional payload object (serialized to JSON)</param>
    void Send(string will, object? payload = null);
}