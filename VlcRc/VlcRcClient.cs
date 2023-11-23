using System.Net.Sockets;
using System.Text;

namespace VlcRc;

public class VlcRcClient
{
    private readonly TcpClient Client = new();
    private byte[] Buffer;
    private int BufferSize = 1024;

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

    #region Stream Utils

    public async Task Connect(string host, int port)
    {
        await Client.ConnectAsync(host, port);

        // Initialize buffer and start read
        Buffer = new byte[BufferSize];
        Client.GetStream().BeginRead(Buffer, 0, Buffer.Length, OnReadCallback, null);
    }
    
    private void OnReadCallback(IAsyncResult ar)
    {
        // Process received buffer
        var count = Client.GetStream().EndRead(ar);
        var buffer = new byte[count];
        Array.Copy(Buffer, buffer, count);

        // Reset buffer and continue read
        Buffer = new byte[BufferSize];
        Client.GetStream().BeginRead(Buffer, 0, Buffer.Length, OnReadCallback, null);
    }

    private async Task Write(string text)
    {
        var buffer = Encoding.UTF8.GetBytes($"{text}\n");
        
        await Client.GetStream().WriteAsync(buffer);
        await Client.GetStream().FlushAsync();
    }

    #endregion
}