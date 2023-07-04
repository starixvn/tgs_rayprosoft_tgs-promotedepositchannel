using Tgs.Promotion.PromoteDepositChannel;

namespace PromoteDepositChannel.Core.UnitTests;

public class EventRangeTests
{
	[Fact]
	public void 判斷時間在區間內()
	{
		// Arrange
		var sut = new EventRange(
			DateTime.UtcNow.Date.AddDays(-1D),
			DateTime.UtcNow.AddDays(1D));

		// Act
		var actual = sut.InTheRange(DateTime.UtcNow);

		// Assert
		Assert.True(actual);
	}

	[Fact]
	public void 判斷時間在區間內_不同時區()
	{
		// Arrange
		var sut = new EventRange(
			DateTime.UtcNow.Date.AddDays(-1D),
			DateTime.UtcNow.AddDays(1D));

		var time = sut.Start.ToLocalTime();
		// Act
		var actual = sut.InTheRange(time);

		// Assert
		Assert.True(actual);
	}
}
