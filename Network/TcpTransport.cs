using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace PulseBot.Network;

/// <summary>
/// TCP-based network transport with guaranteed delivery and message ordering.
/// Uses newline-delimited JSON framing (no BOM, no length prefix).
/// Recommended for production bots - reliable and performant.
/// </summary>
/// <param name="serverIp">Server IP address or hostname</param>
/// <param name="serverPort">Server port number</param>
/// <param name="apiKey">Bot API key for authentication</param>
public sealed class TcpTransport(string serverIp, int serverPort, string apiKey) : INetworkTransport, IDisposable
{
    public event Action<string, object?>? OnCommand;
    public event Action<Exception>? OnError;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private bool _running;

    // CRITICAL: UTF-8 encoding WITHOUT BOM (Byte Order Mark)
    // Server expects raw JSON without EF BB BF prefix
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public bool IsConnected { get; private set; }

    /// <summary>
    /// Start TCP connection to server.
    /// </summary>
    public void Start()
    {
        if (_running) return;

        try
        {
            _client = new TcpClient();
            _client.Connect(serverIp, serverPort);
            _stream = _client.GetStream();
            _running = true;
            IsConnected = true;

            Console.WriteLine($"[TCP] Connected to {serverIp}:{serverPort}");

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoop(_cts.Token));

            OnConnected?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TCP] Connection failed: {ex.Message}");
            OnError?.Invoke(ex);
            _running = false;
            IsConnected = false;
        }
    }

    /// <summary>
    /// Stop TCP connection and cleanup resources.
    /// </summary>
    public void Stop()
    {
        if (!_running) return;

        _running = false;
        IsConnected = false;

        _cts?.Cancel();
        _stream?.Close();
        _client?.Close();

        _stream?.Dispose();
        _client?.Dispose();
        _cts?.Dispose();

        OnDisconnected?.Invoke();
        Console.WriteLine("[TCP] Disconnected");
    }

    /// <summary>
    /// Send command to server with newline-delimited JSON framing.
    /// </summary>
    public void Send(string will, object? payload = null)
    {
        if (!_running || _stream is null)
        {
            Console.WriteLine("[TCP] Not connected - cannot send");
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
            json += "\n"; // Newline-delimited framing

            // Use UTF-8 WITHOUT BOM to prevent 0xEF parsing errors
            var jsonBytes = Utf8NoBom.GetBytes(json);

            // Debug: Verify no BOM (first byte should be 0x7B = '{')
            if (jsonBytes.Length > 0 && jsonBytes[0] == 0xEF)
            {
                Console.WriteLine("[TCP] ⚠️ BOM detected in output - encoding misconfigured!");
            }

            _stream.Write(jsonBytes, 0, jsonBytes.Length);
            _stream.Flush();

            Console.WriteLine($"[SEND] {will}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[TCP] Send error: {ex.Message}");
            OnError?.Invoke(ex);
            Stop(); // Connection broken
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[TCP] Socket error: {ex.Message}");
            OnError?.Invoke(ex);
            Stop(); // Connection broken
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TCP] Unexpected send error: {ex.Message}");
            OnError?.Invoke(ex);
        }
    }

    /// <summary>
    /// Receive loop - reads newline-delimited JSON messages from server.
    /// </summary>
    private async Task ReceiveLoop(CancellationToken ct)
    {
        Console.WriteLine("[TCP] Listening for messages (newline-delimited)...");

        using var reader = new StreamReader(
            _stream!,
            Utf8NoBom,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true
        );

        while (_running && !ct.IsCancellationRequested)
        {
            try
            {
                var json = await reader.ReadLineAsync(ct);

                if (json is null)
                {
                    Console.WriteLine("[TCP] Connection closed by server");
                    break;
                }

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(json))
                    continue;

                // Parse JSON and extract Will + Obj
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("Will", out var willProp))
                    continue;

                var will = willProp.GetString() ?? "UNKNOWN";

                object? payloadObj = null;
                if (root.TryGetProperty("Obj", out var objProp))
                {
                    // Pass raw JsonElement to handlers (they deserialize as needed)
                    payloadObj = objProp;
                }

                Console.WriteLine($"[RECV] {will}");
                OnCommand?.Invoke(will, payloadObj);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[TCP] Receive error: {ex.Message}");
                OnError?.Invoke(ex);
                break; // Connection broken
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[TCP] Socket error: {ex.Message}");
                OnError?.Invoke(ex);
                break; // Connection broken
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[TCP] JSON parse error: {ex.Message}");
                OnError?.Invoke(ex);
                // Continue - skip malformed message
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP] Unexpected receive error: {ex.Message}");
                OnError?.Invoke(ex);
                await Task.Delay(1000, ct); // Backoff before retry
            }
        }

        Console.WriteLine("[TCP] Receive loop stopped");
        Stop();
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}