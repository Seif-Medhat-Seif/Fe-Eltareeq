using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantOrderingPlatform.Domain.Entities;

public class ProcessedWebhook
{
    public Guid Id { get; set; }

    /// <summary>
    /// مصدر الـ webhook: "Paymob" دلوقتي، ممكن يبقى فيه غيره بعدين.
    /// </summary>
    public string Provider { get; set; } = default!;

    /// <summary>
    /// المعرّف الفريد للحدث من عند المزوّد (Paymob transaction id).
    /// عليه UNIQUE INDEX مركب مع Provider — ده اللي بيمنع المعالجة المكررة.
    /// </summary>
    public string ExternalEventId { get; set; } = default!;

    public Guid? OrderId { get; set; }
    public DateTime ReceivedAt { get; set; }

    /// <summary>نتيجة المعالجة — للتشخيص فقط.</summary>
    public string? Outcome { get; set; }
}
