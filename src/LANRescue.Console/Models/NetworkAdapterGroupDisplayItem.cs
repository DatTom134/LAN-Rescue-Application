using LANRescue.ConsoleApp.Models;

namespace LANRescue.ConsoleApp.Models;

public sealed class NetworkAdapterGroupDisplayItem
{
    public string Name { get; }

    public int Order { get; }

    public IReadOnlyList<NetworkAdapterDisplayItem> Adapters { get; }

    public NetworkAdapterGroupDisplayItem(
        string name,
        int order,
        IReadOnlyList<NetworkAdapterDisplayItem> adapters)
    {
        Name = name;
        Order = order;
        Adapters = adapters;
    }
}