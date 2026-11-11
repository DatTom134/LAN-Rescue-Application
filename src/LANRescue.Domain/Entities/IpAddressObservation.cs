using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LANRescue.Domain.Entities;

/// <summary>
/// Một lần quan sát thấy một địa chỉ IPv4 được ánh xạ tới MAC Address.
/// Không đại diện cho bằng chứng xác nhận tuyệt đối rằng IP bị trùng.
/// </summary>
public sealed record IpAddressObservation
{
    public Guid Id { get; init; } = Guid.NewGuid();

    // Adapter trên máy đang chạy LAN Rescue.
    public required string AdapterId { get; init; }

    public required string AdapterName { get; init; }

    // IPv4 nguồn dùng để thực hiện truy vấn.
    public required string SourceAddress { get; init; }

    // IPv4 đang được kiểm tra.
    public required string IpAddress { get; init; }

    // MAC nhận được từ truy vấn ARP.
    public required string MacAddress { get; init; }

    // Ví dụ: Windows SendARP.
    public required string ProbeMethod { get; init; }

    // Lưu UTC để so sánh thời gian chính xác.
    public DateTimeOffset ObservedAt { get; init; }
        = DateTimeOffset.UtcNow;
}