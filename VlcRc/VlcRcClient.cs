using System.Net.Sockets;
using System.Text;

namespace VlcRc;

public class VlcRcClient
{
    public readonly List<PlayListItem> PlayList = new();
    public EventHandler OnPlayListUpdated { get; set; }
    public EventHandler<string> OnError { get; set; }
    public bool AutoReconnect { get; set; } = true;
    
    // Networking
    private readonly TcpClient Client = new();
    private byte[] Buffer;
    private int BufferSize = 4096;
    
    // Cache for reconnecting
    private string Host;
    private int Port;

    public async Task Pause() => await Write("pause");

    public async Task FullScreen(bool toggle)
    {
        var status = toggle ? "on" : "off";
        await Write($"f {status}");
    }
    
    public async Task Loop(bool toggle)
    {
        var status = toggle ? "on" : "off";
        await Write($"loop {status}");
    }
    
    public async Task Repeat(bool toggle)
    {
        var status = toggle ? "on" : "off";
        await Write($"repeat {status}");
    }

    public async Task Previous() => await Write("prev");
    public async Task Next() => await Write("next");
    public async Task GoTo(int id) => await Write($"goto {id}");
    public async Task Seek(int seconds) => await Write($"seek {seconds}");
    public async Task Volume(int volume) => await Write($"volume  {volume}");

    public async Task FetchPlaylist()
    {
        lock (PlayList)
            PlayList.Clear();
        
        await Write("playlist");
    }

    #region Stream Utils

    public async Task Connect(string host, int port)
    {
        // Save inputs for auto reconnect
        Host = host;
        Port = port;
        
        await Client.ConnectAsync(host, port);

        // Initialize buffer and start read
        Buffer = new byte[BufferSize];
        Client.GetStream().BeginRead(Buffer, 0, Buffer.Length, OnReadCallback, null);
    }
    
    private void OnReadCallback(IAsyncResult ar)
    {
        try
        {
            // Process received buffer
            var count = Client.GetStream().EndRead(ar);
            var buffer = new byte[count];
            Array.Copy(Buffer, buffer, count);

            // If is not connected, stop reading
            if (!Client.Connected)
                return;

            // Reset buffer and continue read
            Buffer = new byte[BufferSize];

            if (Client.Connected)
                Client.GetStream().BeginRead(Buffer, 0, Buffer.Length, OnReadCallback, null);

            var text = Encoding.UTF8.GetString(buffer);
            var lines = text.Split("\n");

            foreach (var line in lines)
            {
                if (!line.StartsWith("|  - "))
                    continue;

                var lineWithoutStart = line.Replace("|  - ", "");
                var parts = lineWithoutStart.Split("(");

                void AddPlayListItem(string name, string lenght)
                {
                    var item = new PlayListItem()
                    {
                        Name = name,
                        Lenght = lenght
                    };

                    lock (PlayList)
                        PlayList.Add(item);
                }

                if (parts.Length == 1)
                    AddPlayListItem(parts[0], "N/A");
                else
                {
                    var lenght = parts[1].Replace(")", "");

                    AddPlayListItem(parts[0], lenght);
                }
            }

            OnPlayListUpdated?.Invoke(null, null!);
        }
        catch(ObjectDisposedException) { /* ignored */ }
        catch (Exception e)
        {
            OnError?.Invoke(null, $"An unhandled error occured while reading data: {e}");
        }
    }

    private async Task Write(string text)
    {
        try
        {
            if (!Client.Connected && AutoReconnect)
                await Connect(Host, Port);
            
            var buffer = Encoding.UTF8.GetBytes($"{text}\n");

            await Client.GetStream().WriteAsync(buffer);
            await Client.GetStream().FlushAsync();
        }
        catch (Exception e)
        {
            OnError?.Invoke(null, $"An unhandled error occured while writing data: {e}");
        }
    }
    
    public Task Disconnect()
    {
        if(Client.Connected)
            Client.Close();
        
        Client.Dispose();
        
        return Task.CompletedTask;
    }

    #endregion
}