using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace PulseBot.Network;

/// <summary>
/// UDP-based network transport with best-effort delivery.
/// No guarantees on message ordering or delivery (0.1-1% packet loss typical).
/// Use TCP for production bots - UDP is legacy support only.
/// </summary>
/// <param name="serverIp">Server IP address or hostname</param>
/// <param name="serverPort">Server port number</param>
/// <param name="apiKey">Bot API key for authentication</param>
public sealed class UdpTransport(string serverIp, int serverPort, string apiKey) : INetworkTransport, IDisposable
{
    public event Action<string, object?>? OnCommand;
    public event Action<Exception>? OnError;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    private UdpClient? _client;
    private IPEndPoint? _serverEndpoint;
    private bool _running;

    public bool IsConnected { get; private set; }

    /// <summary>
    /// Start UDP connection to server.
    /// Binds to ephemeral port (OS assigns random available port).
    /// </summary>
    public void Start()
    {
        if (_running) return;

        try
        {
            _client = new UdpClient(0); // Bind to ephemeral port
            _client.Client.ReceiveTimeout = 5000; // 5 second timeout
            _serverEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

            _running = true;
            IsConnected = true;

            var localPort = ((IPEndPoint)_client.Client.LocalEndPoint!).Port;
            Console.WriteLine($"[UDP] Bound to local port {localPort}");
            Console.WriteLine($"[UDP] Targeting server {serverIp}:{serverPort}");

            _ = Task.Run(ReceiveLoop);
            OnConnected?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UDP] Start failed: {ex.Message}");
            OnError?.Invoke(ex);
            _running = false;
            IsConnected = false;
        }
    }

    /// <summary>
    /// Stop UDP connection and cleanup resources.
    /// </summary>
    public void Stop()
    {
        if (!_running) return;

        _running = false;
        IsConnected = false;

        _client?.Close();
        _client?.Dispose();

        OnDisconnected?.Invoke();
        Console.WriteLine("[UDP] Disconnected");
    }

    /// <summary>
    /// Send datagram to server.
    /// No guarantee of delivery - message may be lost in transit.
    /// </summary>
    public void Send(string will, object? payload = null)
    {
        if (!_running || _client is null || _serverEndpoint is null)
        {
            Console.WriteLine("[UDP] Not connected - cannot send");
            return;
        }

        try
        {
            var request = new
            {
                CovenantID = Guid.NewGuid(),
                PacketId = Guid.NewGuid(),
                Will = will,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                HttpApiKey = apiKey,
                Obj = payload
            };

            var json = JsonSerializer.Serialize(request);
            var data = Encoding.UTF8.GetBytes(json);

            // UDP has no built-in send confirmation
            _client.Send(data, data.Length, _serverEndpoint);

            Console.WriteLine($"[SEND] {will}");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[UDP] Send error: {ex.Message}");
            OnError?.Invoke(ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UDP] Unexpected send error: {ex.Message}");
            OnError?.Invoke(ex);
        }
    }

    /// <summary>
    /// Receive loop - waits for UDP datagrams from server.
    /// </summary>
    private async Task ReceiveLoop()
    {
        Console.WriteLine("[UDP] Listening for messages...");

        while (_running)
        {
            try
            {
                if (_client is null) break;

                var result = await _client.ReceiveAsync();
                var json = Encoding.UTF8.GetString(result.Buffer);

                // Parse JSON and extract Will + Obj
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("Will", out var willProp))
                    continue;

                var will = willProp.GetString() ?? "UNKNOWN";

                object? payloadObj = null;
                if (root.TryGetProperty("Obj", out var objProp))
                {
                    // Pass raw JsonElement to handlers
                    payloadObj = objProp;
                }

                Console.WriteLine($"[RECV] {will}");
                OnCommand?.Invoke(will, payloadObj);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                // Timeout is normal - continue waiting
                continue;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[UDP] Socket error: {ex.Message}");
                OnError?.Invoke(ex);
                await Task.Delay(1000);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[UDP] JSON parse error: {ex.Message}");
                OnError?.Invoke(ex);
                // Continue - skip malformed message
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UDP] Unexpected receive error: {ex.Message}");
                OnError?.Invoke(ex);
                await Task.Delay(1000);
            }
        }

        Console.WriteLine("[UDP] Receive loop stopped");
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}