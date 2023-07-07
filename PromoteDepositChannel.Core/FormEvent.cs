using System.Diagnostics;
using Tgs.Payment.Clients;

namespace Tgs.Promotion.PromoteDepositChannel;
public record FormEvent(
	DepositFormInfo FormInfo,
	ActivityTraceId? ParentTraceId,
	ActivitySpanId? ParentSpanId);
