using VlcRc;

var client = new VlcRcClient();
await client.Connect("127.0.0.1", 9999);

while (true)
{
    await client.FullScreen(true);
    await client.Pause();
    await Task.Delay(TimeSpan.FromMilliseconds(20));
    await client.FullScreen(false);
    await client.Pause();
    await Task.Delay(TimeSpan.FromMilliseconds(20));
}

Console.ReadLine();