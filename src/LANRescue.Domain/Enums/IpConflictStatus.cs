namespace LANRescue.Domain.Enums;

public enum IpConflictStatus
{
    // Một IP đã được quan sát thấy có MAC phản hồi.
    Observed,

    // Cùng IP có nhiều MAC được ghi nhận trong thời gian gần nhau.
    Suspected,

    // Có bằng chứng ARP đủ mạnh để xác nhận.
    Confirmed,

    // Chưa có đủ dữ liệu để đưa ra kết luận.
    InsufficientEvidence,

    // Xung đột đã được xử lý hoặc người vận hành xác nhận đã giải quyết.
    Resolved
}
