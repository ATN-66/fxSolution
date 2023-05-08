namespace Common.ExtensionsAndHelpers.Test.Unit;

public sealed class DateTimeExtensionsAndHelpersTests
{
    [Theory]
    [InlineData(2021, 0)] //there is no a week number 0
    [InlineData(2022, 0)] //there is no a week number 0
    [InlineData(2020, 54)] //only 53
    [InlineData(2021, 53)] //only 52
    [InlineData(2022, 53)] //only 52
    [InlineData(2023, 53)] //only 52
    public void GetDatesInWeek_ThrowsArgumentOutOfRangeException_WhenWeekOutOfRange(int year, int week)
    {
        // Act
        var e = Record.Exception(() => DateTimeExtensionsAndHelpers.GetDatesInWeek(year, week));

        //Assert
        Assert.IsType<ArgumentOutOfRangeException>(e);
    }

    [Theory]
    [InlineData(2021, 1, "01.04.2021", "01.10.2021")]
    [InlineData(2021, 2, "01.11.2021", "01.17.2021")]
    [InlineData(2021, 52, "12.27.2021", "12.31.2021")]
    [InlineData(2022, 1, "01.03.2022", "01.09.2022")]
    [InlineData(2022, 2, "01.10.2022", "01.16.2022")]
    [InlineData(2022, 52, "12.26.2022", "12.31.2022")]
    [InlineData(2023, 1, "01.02.2023", "01.08.2023")]
    [InlineData(2023, 2, "01.09.2023", "01.15.2023")]
    [InlineData(2023, 52, "12.25.2023", "12.31.2023")]
    public void GetDatesInWeek_ReturnsCorrectDates(int year, int week, string firstDate, string lastDate)
    {
        // Arrange
        var expectedFirstDate = DateTime.Parse(firstDate);
        var expectedLastDate = DateTime.Parse(lastDate);

        // Act
        var datesInWeek = DateTimeExtensionsAndHelpers.GetDatesInWeek(year, week);

        // Assert
        Assert.Equal(expectedFirstDate, datesInWeek.First());
        Assert.Equal(expectedLastDate, datesInWeek.Last());
    }

    [Fact]
    public void DateTimeExtensionsAndHelpers_ElapsedSecondsFromJanuaryFirstOf1970_FromZeroDate_IsZeroSeconds()
    {
        // Arrange
        var zeroDate = new DateTime(1970, 1, 1, 0, 0, 0);

        // Act
        var elapsedSeconds = zeroDate.ElapsedSecondsFromJanuaryFirstOf1970();

        // Assert
        Assert.Equal(0, elapsedSeconds);
    }

    [Fact]
    public void DateTimeExtensionsAndHelpers_ElapsedSecondsOfJanFirstOf2023_Is1672531200()
    {
        // Arrange
        var janFirstOf2023 = new DateTime(2023, 1, 1, 0, 0, 0);
        const int elapsedSecondsExpected = 1672531200;

        // Act
        var actual = janFirstOf2023.ElapsedSecondsFromJanuaryFirstOf1970();

        // Assert 
        Assert.Equal(elapsedSecondsExpected, actual);
    }

    [Fact]
    public void DateTimeExtensionsAndHelpers_ElapsedMinutesSinceTheStartOfTheWeek_SinceTheStartOfTheWeek_IsZeroMinutes()
    {
        // Arrange
        var sunday = new DateTime(2022, 1, 2, 0, 0, 0);

        // Act
        var elapsedMinutes = sunday.ElapsedMinutesSinceTheStartOfTheWeek();

        // Assert
        Assert.Equal(0, elapsedMinutes);
    }

    [Fact]
    public void DateTimeExtensionsAndHelpers_ElapsedMinutesSinceTheStartOfTheWeek_ToTheStartOfTheNextDay_Is1440()
    {
        // Arrange
        var monday = new DateTime(2022, 1, 3, 0, 0, 0);

        // Act
        var elapsedMinutes = monday.ElapsedMinutesSinceTheStartOfTheWeek();

        // Assert
        Assert.Equal(24 * 60, elapsedMinutes);
    }

    [Fact]
    public void DateTimeExtensionsAndHelpers_Jan1Of2022_IsWeekNumber1()
    {
        // Arrange
        var jan1Of2022 = new DateTime(2022, 1, 1, 0, 0, 0);
        const int expected = 52;
        // Act
        var actual = jan1Of2022.WeekOfYear();

        // Assert 
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTimeExtensionsAndHelpers_Nov6Of2022_IsWeekNumber44()
    {
        // Arrange
        var nov6Of2022 = new DateTime(2022, 11, 6, 0, 0, 0);
        const int expected = 44;

        // Act
        var actual = nov6Of2022.WeekOfYear();

        // Assert 
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTimeExtensionsAndHelpers_Dec31Of2022_IsWeekNumber52()
    {
        // Arrange
        var dec316Of2022 = new DateTime(2022, 12, 31, 0, 0, 0);
        const int expected = 52;

        // Act
        var actual = dec316Of2022.WeekOfYear();

        // Assert 
        Assert.Equal(expected, actual);
    }
}