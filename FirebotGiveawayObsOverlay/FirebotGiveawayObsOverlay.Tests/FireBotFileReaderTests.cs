using FirebotGiveawayObsOverlay.WebApp.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace FirebotGiveawayObsOverlay.Tests;

public class FireBotFileReaderTests
{
    private static FireBotFileReader CreateReader() => new(NullLogger<FireBotFileReader>.Instance);

    [Theory]
    [InlineData("", 0)]
    [InlineData("a", 1)]
    [InlineData("a\nb\nc", 3)]
    [InlineData("a\r\nb\r\n", 2)]
    [InlineData("a\n\n\nb\n", 2)]
    [InlineData("a\n   \nb", 2)]
    [InlineData("\r\n\r\n", 0)]
    public void CountNonEmptyLines_HandlesLineEndingsAndBlanks(string text, int expected)
    {
        Assert.Equal(expected, FireBotFileReader.CountNonEmptyLines(text));
    }

    [Fact]
    public void Read_MissingFolder_ReturnsEmpty()
    {
        var data = CreateReader().Read(System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString()));
        Assert.Equal(new FirebotFileData("", "", 0), data);
    }

    [Fact]
    public void Read_ReturnsTrimmedPrizeWinnerAndEntryCount()
    {
        using var dir = new TempFolder();
        dir.Write(FireBotFileReader.PrizeFileName, "Gaming Keyboard\r\n");
        dir.Write(FireBotFileReader.WinnerFileName, "");
        dir.Write(FireBotFileReader.EntriesFileName, "User1\r\nUser2\nUser3\n");

        var data = CreateReader().Read(dir.Path);

        Assert.Equal("Gaming Keyboard", data.Prize);
        Assert.Equal("", data.Winner);
        Assert.Equal(3, data.EntryCount);
    }

    [Fact]
    public void Read_PicksUpChanges()
    {
        using var dir = new TempFolder();
        var reader = CreateReader();
        dir.Write(FireBotFileReader.EntriesFileName, "a\n");
        Assert.Equal(1, reader.Read(dir.Path).EntryCount);

        dir.Write(FireBotFileReader.EntriesFileName, "a\nb\n");
        Assert.Equal(2, reader.Read(dir.Path).EntryCount);

        File.Delete(dir.File(FireBotFileReader.EntriesFileName));
        Assert.Equal(0, reader.Read(dir.Path).EntryCount);
    }

    [Fact]
    public void Read_WhileFileIsOpenForWriting_StillReads()
    {
        using var dir = new TempFolder();
        dir.Write(FireBotFileReader.PrizeFileName, "PS5");

        // Simulate Firebot holding the file open for writing (allowing readers, as most writers do)
        using var writer = new FileStream(dir.File(FireBotFileReader.PrizeFileName), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

        Assert.Equal("PS5", CreateReader().Read(dir.Path).Prize);
    }

    [Fact]
    public void Read_FolderChange_ResetsCache()
    {
        using var a = new TempFolder();
        using var b = new TempFolder();
        a.Write(FireBotFileReader.PrizeFileName, "A");
        b.Write(FireBotFileReader.PrizeFileName, "B");
        var reader = CreateReader();

        Assert.Equal("A", reader.Read(a.Path).Prize);
        Assert.Equal("B", reader.Read(b.Path).Prize);
    }
}
