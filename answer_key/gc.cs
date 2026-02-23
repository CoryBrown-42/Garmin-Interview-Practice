using UnityEngine;
using TMPro; // Assuming TextMeshPro is used for UI text
using System.Text; // For StringBuilder
using Random = UnityEngine.Random; // To disambiguate from System.Random

// --- Mock Classes for Demonstration ---
// In a real project, these would be your actual game state/player objects.
public class Player
{
    public string Name { get; private set; }
    public Player(string name) { Name = name; }
}

public class GameState
{
    public int CurrentScore { get; set; }
    public float DistanceToHole { get; set; } // in yards
    public string CurrentClubName { get; set; }
    public Vector2 WindDirectionAndSpeed { get; set; } // e.g., (5, 10) for 5 mph from X, 10 mph from Y
    public bool IsPutting { get; set; }
}

/// <summary>
/// Manages the display of golf simulator dashboard elements,
/// optimized for GC avoidance on mobile/embedded targets.
/// </summary>
public class DashboardManager : MonoBehaviour
{
    // --- UI References (Cached via Inspector Assignment) ---
    // Assign these in the Unity Inspector to avoid runtime GetComponent allocations.
    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _distanceText;
    [SerializeField] private TextMeshProUGUI _clubText;
    [SerializeField] private TextMeshProUGUI _windText;
    [SerializeField] private TextMeshProUGUI _playerNameText;

    // --- Data References (Cached via Inspector Assignment or dependency injection) ---
    [Header("Game Data References")]
    [SerializeField] private Player _currentPlayer; // e.g., from a PlayerManager
    [SerializeField] private GameState _gameState;   // e.g., from a GameManager

    // --- GC Avoidance: Cached StringBuilder ---
    // Declared as readonly and initialized once to avoid allocations every frame.
    // Initial capacity can be tuned based on the longest expected string.
    private readonly StringBuilder _stringBuilder = new StringBuilder(128);

    // --- GC Avoidance: Caching Last Known Values for Comparison ---
    // Store the last displayed values. Updates only occur if these values change.
    private int _lastDisplayedScore = int.MinValue; // Use an unlikely initial value to force first update
    private float _lastDisplayedDistance = float.MinValue;
    private string _lastDisplayedClub = string.Empty;
    private Vector2 _lastDisplayedWind = Vector2.one * float.MinValue; // Use a specific large negative vector for wind
    private string _lastDisplayedPlayerName = string.Empty;
    private bool _lastDisplayedIsPutting = false;

    // --- Setup (Called once when the script instance is being loaded) ---
    private void Awake()
    {
        // For demonstration purposes, initialize mock data if not set in Inspector.
        // In a real application, these would be managed by your game's data controllers.
        if (_currentPlayer == null) _currentPlayer = new Player("Garmin Golfer");
        if (_gameState == null) _gameState = new GameState
        {
            CurrentScore = 0,
            DistanceToHole = 250.5f,
            CurrentClubName = "Driver",
            WindDirectionAndSpeed = new Vector2(5f, 3f),
            IsPutting = false
        };

        // Perform an initial update to populate the UI without requiring comparisons.
        // This ensures the dashboard displays correct values immediately.
        ForceUpdateAllDashboardElements();
    }

    /// <summary>
    /// Refactored method to update dashboard UI elements,
    /// critically optimized to eliminate per-frame garbage collection.
    /// This method should ideally be called only when relevant game state changes,
    /// but is robust enough to be called every frame if needed, due to internal comparisons.
    /// </summary>
    public void UpdateDashboard()
    {
        // Early exit if essential data or UI references are missing
        if (_gameState == null || _scoreText == null) return; // Add more null checks as needed

        // --- Player Name (Less frequent updates) ---
        // Player name usually doesn't change per-frame.
        if (_currentPlayer != null && _currentPlayer.Name != _lastDisplayedPlayerName)
        {
            _playerNameText.text = _currentPlayer.Name;
            _lastDisplayedPlayerName = _currentPlayer.Name;
        }

        // --- Score Update ---
        // Only update if the score has actually changed.
        if (_gameState.CurrentScore != _lastDisplayedScore)
        {
            _stringBuilder.Clear(); // Clear the StringBuilder for reuse
            _stringBuilder.Append("Score: ").Append(_gameState.CurrentScore);
            _scoreText.text = _stringBuilder.ToString(); // Allocates a new string, but only when score changes
            _lastDisplayedScore = _gameState.CurrentScore;
        }

        // --- Distance Update ---
        // Use a small tolerance for float comparisons to prevent updates due to tiny precision changes.
        // Also handle the club type impacting distance display (e.g., putting vs. long shots).
        bool isPuttingChanged = _gameState.IsPutting != _lastDisplayedIsPutting;
        if (Mathf.Abs(_gameState.DistanceToHole - _lastDisplayedDistance) > 0.1f || isPuttingChanged)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("Distance: ");
            if (_gameState.IsPutting)
            {
                // For putting, show feet and inches for precision
                float distanceFeet = _gameState.DistanceToHole * 3f; // Assuming 1 yard = 3 feet
                int feet = Mathf.FloorToInt(distanceFeet);
                int inches = Mathf.RoundToInt((distanceFeet - feet) * 12f);
                _stringBuilder.Append(feet).Append("' ").Append(inches).Append("\"");
            }
            else
            {
                // For regular shots, show rounded yards
                _stringBuilder.Append(Mathf.RoundToInt(_gameState.DistanceToHole)).Append(" yd");
            }
            _distanceText.text = _stringBuilder.ToString();
            _lastDisplayedDistance = _gameState.DistanceToHole;
            _lastDisplayedIsPutting = _gameState.IsPutting;
        }

        // --- Club Update ---
        if (_gameState.CurrentClubName != _lastDisplayedClub || _gameState.IsPutting != _lastDisplayedIsPutting)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("Club: ");
            if (_gameState.IsPutting)
            {
                _stringBuilder.Append("Putter"); // Force Putter if putting
            }
            else
            {
                _stringBuilder.Append(_gameState.CurrentClubName);
            }
            _clubText.text = _stringBuilder.ToString();
            _lastDisplayedClub = _gameState.CurrentClubName;
            _lastDisplayedIsPutting = _gameState.IsPutting; // Ensure this also updates if isPutting changes
        }

        // --- Wind Update ---
        // Vector2 comparison is direct, but for performance, comparing individual components or magnitude
        // with a tolerance might be preferred if wind changes very subtly.
        if (_gameState.WindDirectionAndSpeed != _lastDisplayedWind)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("Wind: ");
            if (_gameState.WindDirectionAndSpeed.magnitude > 0.5f) // Display wind only if speed is significant
            {
                // Example: display speed and a simplified direction (e.g., "N", "SW")
                // For a simulator, you'd calculate true bearing. Here, just magnitude.
                int windSpeed = Mathf.RoundToInt(_gameState.WindDirectionAndSpeed.magnitude);
                _stringBuilder.Append(windSpeed).Append(" mph");
                // Optional: Add direction calculation here if needed
                // e.g., if (Vector2.Dot(_gameState.WindDirectionAndSpeed.normalized, Vector2.up) > 0.7f) _stringBuilder.Append(" (N)");
            }
            else
            {
                _stringBuilder.Append("Calm");
            }
            _windText.text = _stringBuilder.ToString();
            _lastDisplayedWind = _gameState.WindDirectionAndSpeed;
        }
    }

    /// <summary>
    /// Forces all dashboard elements to update immediately, regardless of whether their values have changed.
    /// Useful for initial setup or after a major state reset.
    /// </summary>
    public void ForceUpdateAllDashboardElements()
    {
        // Invalidate all last displayed values to trigger a full update
        _lastDisplayedScore = int.MinValue;
        _lastDisplayedDistance = float.MinValue;
        _lastDisplayedClub = string.Empty;
        _lastDisplayedWind = Vector2.one * float.MinValue;
        _lastDisplayedPlayerName = string.Empty;
        _lastDisplayedIsPutting = false;

        UpdateDashboard(); // Trigger the update
    }

    // --- Example of how to simulate updates (for testing) ---
    private float _updateTimer = 0f;
    private void Update()
    {
        // Simulate game state changes for demonstration.
        // In a real game, your game logic would call the public Set methods below.
        if (_gameState != null)
        {
            _updateTimer += Time.deltaTime;
            if (_updateTimer > 1.0f) // Update mock game state every second
            {
                SetScore(_gameState.CurrentScore + Random.Range(0, 3));
                SetDistance(Random.Range(50f, 300f));
                string[] clubs = { "Driver", "Iron 7", "Wedge", "Putter" };
                SetClub(clubs[Random.Range(0, clubs.Length)]);
                SetWind(new Vector2(Random.Range(-10f, 10f), Random.Range(-10f, 10f)));
                SetIsPutting(Random.value < 0.2f); // 20% chance to be putting
                _updateTimer = 0f;
            }
        }

        // Call the refactored dashboard update method.
        // Because of the internal comparisons, this is safe to call every frame.
        // For ultimate optimization, you could make this event-driven, only calling
        // UpdateDashboard() when a relevant game state change occurs.
        UpdateDashboard();
    }

    // --- Public methods to update data from game logic (recommended) ---
    // These methods provide an API for other game systems to update the dashboard's data.
    // They internally trigger an UpdateDashboard call to reflect changes immediately.
    public void SetScore(int newScore)
    {
        if (_gameState.CurrentScore != newScore) // Only update if value truly changes
        {
            _gameState.CurrentScore = newScore;
            // Optionally, call UpdateDashboard() here if you want immediate reflection
            // without waiting for the next Update() cycle.
            // UpdateDashboard();
        }
    }

    public void SetDistance(float newDistance)
    {
        if (Mathf.Abs(_gameState.DistanceToHole - newDistance) > 0.01f)
        {
            _gameState.DistanceToHole = newDistance;
        }
    }

    public void SetClub(string newClub)
    {
        if (_gameState.CurrentClubName != newClub)
        {
            _gameState.CurrentClubName = newClub;
        }
    }

    public void SetWind(Vector2 newWind)
    {
        if (_gameState.WindDirectionAndSpeed != newWind)
        {
            _gameState.WindDirectionAndSpeed = newWind;
        }
    }

    public void SetCurrentPlayer(Player player)
    {
        if (_currentPlayer != player) // Reference comparison
        {
            _currentPlayer = player;
            // UpdateDashboard(); // Player name change might need immediate reflection
        }
    }

    public void SetIsPutting(bool isPutting)
    {
        if (_gameState.IsPutting != isPutting)
        {
            _gameState.IsPutting = isPutting;
        }
    }
}