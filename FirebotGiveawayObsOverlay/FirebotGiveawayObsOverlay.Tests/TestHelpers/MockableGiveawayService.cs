using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FirebotGiveawayObsOverlay.Tests.TestHelpers;

/// <summary>
/// A testable version of GiveawayService that can be easily mocked
/// </summary>
public class MockableGiveawayService : GiveawayService
{
    private bool _isGiveawayActive = false;
    private string _currentPrize = string.Empty;
    private readonly HashSet<string> _participants = new(StringComparer.OrdinalIgnoreCase);
    private string? _winner = null;

    public MockableGiveawayService()
        : base(
              CreateDefaultOptionsMonitor(),
              CreateDefaultLogger())
    {
    }

    public void SetGiveawayActive(bool isActive)
    {
        _isGiveawayActive = isActive;
    }

    public void SetCurrentPrize(string prize)
    {
        _currentPrize = prize;
    }

    public void SetParticipants(IEnumerable<string> participants)
    {
        _participants.Clear();
        foreach (var participant in participants)
        {
            _participants.Add(participant);
        }
    }

    public void SetWinner(string? winner)
    {
        _winner = winner;
    }

    public new bool IsGiveawayActive => _isGiveawayActive;

    public new string CurrentPrize => _currentPrize;

    public new int EntryCount => _participants.Count;

    public new IEnumerable<string> Entries => _participants.ToList();

    public new bool StartGiveaway(string prize)
    {
        if (_isGiveawayActive)
        {
            return false;
        }

        _currentPrize = prize;
        _participants.Clear();
        _isGiveawayActive = true;
        return true;
    }

    public new bool AddEntry(string username)
    {
        if (!_isGiveawayActive)
        {
            return false;
        }

        return _participants.Add(username);
    }

    public new string? DrawWinner()
    {
        if (!_isGiveawayActive || _participants.Count == 0)
        {
            return null;
        }

        _isGiveawayActive = false;
        
        if (_winner != null)
        {
            return _winner;
        }
        
        // If no winner is pre-set, select a random one
        var random = new Random();
        var winnerIndex = random.Next(_participants.Count);
        return _participants.ElementAt(winnerIndex);
    }

    private static IOptionsMonitor<AppSettings> CreateDefaultOptionsMonitor()
    {
        var settings = new AppSettings
        {
            FireBotFileFolder = "test_folder"
        };

        var optionsMonitor = new TestOptionsMonitor<AppSettings>(settings);
        return optionsMonitor;
    }

    private static ILogger<GiveawayService> CreateDefaultLogger()
    {
        return new TestLogger<GiveawayService>();
    }

    private class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly T _currentValue;

        public TestOptionsMonitor(T currentValue)
        {
            _currentValue = currentValue;
        }

        public T CurrentValue => _currentValue;

        public T Get(string? name) => _currentValue;

        public IDisposable OnChange(Action<T, string> listener)
        {
            return new TestDisposable();
        }

        private class TestDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    private class TestLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => new TestDisposable();

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

        private class TestDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}