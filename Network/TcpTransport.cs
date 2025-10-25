using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace PulseBot.Network;

public sealed class TcpTransport : INetworkTransport, IDisposable
{
    public event Action<string, object?>? OnCommand;
    public event Action<Exception>? OnError;
    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public event Action<int>? OnReconnecting;
    public event Action? OnReconnected;

    private readonly string _serverIp;
    private readonly int _serverPort;
    private readonly string _apiKey;

    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private bool _running;
    private bool _intentionalDisconnect;
    private DateTime _firstReconnectAttempt;
    private int _reconnectAttempt;

    private static readonly TimeSpan MAX_RECONNECT_WINDOW = TimeSpan.FromHours(6);
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public bool IsConnected { get; private set; }

    public TcpTransport(string serverIp, int serverPort, string apiKey)
    {
        _serverIp = serverIp;
        _serverPort = serverPort;
        _apiKey = apiKey;
    }

    public void Start()
    {
        _intentionalDisconnect = false;
        _reconnectAttempt = 0;
        ConnectInternal();
    }

    private void ConnectInternal()
    {
        if (_running && IsConnected) return;

        try
        {
            _client?.Dispose();
            _stream?.Dispose();

            _client = new TcpClient();
            _client.Connect(_serverIp, _serverPort);
            _stream = _client.GetStream();
            _running = true;
            IsConnected = true;

            Console.WriteLine($"[TCP] Connected to {_serverIp}:{_serverPort}");

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _ = Task.Run(() => ReceiveLoop(_cts.Token));

            if (_reconnectAttempt > 0)
            {
                Console.WriteLine($"[TCP] Reconnected after {_reconnectAttempt} attempts");
                OnReconnected?.Invoke();
            }
            else
            {
                OnConnected?.Invoke();
            }

            _reconnectAttempt = 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TCP] Connection failed: {ex.Message}");
            IsConnected = false;
            _running = false;
            OnError?.Invoke(ex);

            if (!_intentionalDisconnect)
            {
                _ = Task.Run(ReconnectLoop);
            }
        }
    }

    private async Task ReconnectLoop()
    {
        if (_reconnectAttempt == 0)
        {
            _firstReconnectAttempt = DateTime.UtcNow;
        }

        while (!_intentionalDisconnect)
        {
            var elapsed = DateTime.UtcNow - _firstReconnectAttempt;
            if (elapsed > MAX_RECONNECT_WINDOW)
            {
                Console.WriteLine($"[TCP] Max reconnect window (6 hours) exceeded. Giving up.");
                OnError?.Invoke(new Exception("Reconnect window exceeded"));
                return;
            }

            _reconnectAttempt++;

            TimeSpan delay = _reconnectAttempt switch
            {
                1 => TimeSpan.FromSeconds(1),
                2 => TimeSpan.FromSeconds(2),
                3 => TimeSpan.FromSeconds(4),
                4 => TimeSpan.FromSeconds(10),
                _ => TimeSpan.FromSeconds(120)
            };

            Console.WriteLine($"[TCP] Reconnect attempt {_reconnectAttempt} in {delay.TotalSeconds:F0}s...");
            OnReconnecting?.Invoke(_reconnectAttempt);

            await Task.Delay(delay);

            ConnectInternal();

            if (IsConnected)
            {
                return;
            }
        }
    }

    public void Stop()
    {
        _intentionalDisconnect = true;
        StopInternal();
    }

    private void StopInternal()
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
                HttpApiKey = _apiKey,
                Obj = payload
            };

            var json = JsonSerializer.Serialize(request);
            json += "\n";

            var jsonBytes = Utf8NoBom.GetBytes(json);

            if (jsonBytes.Length > 0 && jsonBytes[0] == 0xEF)
            {
                Console.WriteLine("[TCP] BOM detected in output - encoding misconfigured!");
            }

            _stream.Write(jsonBytes, 0, jsonBytes.Length);
            _stream.Flush();

            Console.WriteLine($"[SEND] {will}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[TCP] Send error: {ex.Message}");
            OnError?.Invoke(ex);
            StopInternal();

            if (!_intentionalDisconnect)
            {
                _ = Task.Run(ReconnectLoop);
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[TCP] Socket error: {ex.Message}");
            OnError?.Invoke(ex);
            StopInternal();

            if (!_intentionalDisconnect)
            {
                _ = Task.Run(ReconnectLoop);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TCP] Unexpected send error: {ex.Message}");
            OnError?.Invoke(ex);
        }
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        Console.WriteLine("[TCP] Listening for messages...");

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

                if (string.IsNullOrWhiteSpace(json))
                    continue;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("Will", out var willProp))
                    continue;

                var will = willProp.GetString() ?? "UNKNOWN";

                object? payloadObj = null;
                if (root.TryGetProperty("Obj", out var objProp))
                {
                    payloadObj = objProp;
                }

                Console.WriteLine($"[RECV] {will}");
                OnCommand?.Invoke(will, payloadObj);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[TCP] Receive error: {ex.Message}");
                OnError?.Invoke(ex);
                break;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[TCP] Socket error: {ex.Message}");
                OnError?.Invoke(ex);
                break;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[TCP] JSON parse error: {ex.Message}");
                OnError?.Invoke(ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP] Unexpected receive error: {ex.Message}");
                OnError?.Invoke(ex);
                await Task.Delay(1000, ct);
            }
        }

        Console.WriteLine("[TCP] Receive loop stopped");
        StopInternal();

        if (!_intentionalDisconnect)
        {
            _ = Task.Run(ReconnectLoop);
        }
    }

    public void Dispose()
    {
        _intentionalDisconnect = true;
        StopInternal();
        GC.SuppressFinalize(this);
    }
}