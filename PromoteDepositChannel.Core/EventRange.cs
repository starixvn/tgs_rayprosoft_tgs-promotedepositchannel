using Tgs.Promotion.Clients;

namespace Tgs.Promotion.PromoteDepositChannel;
public record struct EventRange(DateTime Start, DateTime End)
{
	public readonly bool InTheRange(DateTime time)
		=> time >= Start && time <= End;

	public readonly void Deconstruct(out DateTime start, out DateTime end)
	{
		start = Start;
		end = End;
	}

	public static implicit operator EventRange((DateTime Start, DateTime End) value)
		=> new(value.Start, value.End);

	public static implicit operator EventRange(During during)
		=> new(during.From, during.To);
}
