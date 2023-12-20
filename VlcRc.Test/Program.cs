using VlcRc;

var client = new VlcRcClient();
await client.Connect("127.0.0.1", 9999);

client.OnPlayListUpdated += (_, _) =>
{
    foreach (var item in client.PlayList)
    {
        Console.WriteLine($"{item.Name} / {item.Lenght}");
    }
};

await client.FetchPlaylist();

Console.ReadLine();