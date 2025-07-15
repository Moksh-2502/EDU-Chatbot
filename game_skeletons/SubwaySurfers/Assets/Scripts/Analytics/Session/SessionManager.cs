using System;
using AIEduChatbot.UnityReactBridge.Handlers;
using Cysharp.Threading.Tasks;
using UnityEngine;
using SubwaySurfers.Analytics.Events.Session;
using SharedCore.Analytics;

namespace SubwaySurfers.Analytics.Session
{
    /// <summary>
    /// Manages game session lifecycle, idle detection, and session metrics tracking
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class SessionManager : MonoBehaviour
    {
        private static SessionManager _instance;
        public static SessionManager Instance => _instance;

        [Header("Session Configuration")]
        [SerializeField] private float idleTimeoutMinutes = 5f;
        [SerializeField] private float sessionPingIntervalSeconds = 30f;
        [SerializeField] private float activityCheckIntervalSeconds = 1f;

        private SessionMetrics _currentSession;
        private bool _isSessionActive;
        private DateTime _lastActivityTime;
        private bool _isIdleDetectionRunning;

        // Activity tracking
        private Vector3 _lastMousePosition;
        private int _lastInputFrameCount;
        private float _sessionStartTime;

        public event Action<SessionMetrics> OnSessionStarted;
        public event Action<SessionMetrics> OnSessionEnded;
        public event Action<SessionMetrics> OnSessionPing;

        public SessionMetrics CurrentSession => _currentSession;
        public bool IsSessionActive => _isSessionActive;

        public string SessionId => _currentSession?.SessionId ?? Guid.NewGuid().ToString();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            _lastMousePosition = Input.mousePosition;
            _lastInputFrameCount = Time.frameCount;
        }

        private void Update()
        {
            DetectActivity();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // App going to background
                RecordActivity();
            }
            else
            {
                // App coming to foreground
                RecordActivity();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                RecordActivity();
            }
        }

        public void StartSession()
        {
            if (_isSessionActive)
            {
                Debug.LogWarning("[SessionManager] Session already active, ending previous session");
                EndSession();
            }

            _sessionStartTime = Time.realtimeSinceStartup;
            _currentSession = new SessionMetrics
            {
                SessionId = IGameSessionProvider.Instance.SessionId,
                PlayerId = SystemInfo.deviceUniqueIdentifier,
                StartTime = DateTime.UtcNow,
                Platform = Application.platform.ToString(),
                AppVersion = Application.version
            };

            _isSessionActive = true;
            _lastActivityTime = DateTime.UtcNow;

            // Start background tasks
            StartIdleDetection().Forget();
            StartSessionPinging().Forget();

            // Fire events
            OnSessionStarted?.Invoke(_currentSession);
            IAnalyticsService.Instance?.TrackEvent(new SessionStartEvent(_currentSession));

            Debug.Log($"[SessionManager] Session started: {_currentSession.SessionId}");
        }

        public void EndSession()
        {
            if (!_isSessionActive || _currentSession == null)
                return;

            _currentSession.EndTime = DateTime.UtcNow;
            _currentSession.DurationMinutes = (float)(_currentSession.EndTime.Value - _currentSession.StartTime).TotalMinutes;

            _isSessionActive = false;
            _isIdleDetectionRunning = false;

            // Fire events
            OnSessionEnded?.Invoke(_currentSession);
            IAnalyticsService.Instance?.TrackEvent(new SessionEndEvent(_currentSession));

            Debug.Log($"[SessionManager] Session ended: {_currentSession.SessionId}, Duration: {_currentSession.DurationMinutes:F2} minutes");

            _currentSession = null;
        }

        public void RecordActivity()
        {
            _lastActivityTime = DateTime.UtcNow;
        }

        private void DetectActivity()
        {
            // Check for mouse movement
            Vector3 currentMousePosition = Input.mousePosition;
            if (Vector3.Distance(_lastMousePosition, currentMousePosition) > 0.1f)
            {
                RecordActivity();
                _lastMousePosition = currentMousePosition;
            }

            // Check for any input
            if (Input.inputString.Length > 0 || Input.anyKeyDown)
            {
                RecordActivity();
            }

            // Check for touch input
            if (Input.touchCount > 0)
            {
                RecordActivity();
            }
        }

        private async UniTaskVoid StartIdleDetection()
        {
            _isIdleDetectionRunning = true;
            
            while (_isIdleDetectionRunning && _isSessionActive)
            {
                await UniTask.WaitForSeconds(activityCheckIntervalSeconds, ignoreTimeScale: true);
                
                if (!_isSessionActive) break;

                var timeSinceLastActivity = DateTime.UtcNow - _lastActivityTime;
                if (timeSinceLastActivity.TotalMinutes >= idleTimeoutMinutes)
                {
                    Debug.Log($"[SessionManager] Session idle timeout reached: {timeSinceLastActivity.TotalMinutes:F2} minutes");
                    EndSession();
                    break;
                }
            }
        }

        private async UniTaskVoid StartSessionPinging()
        {
            while (_isSessionActive)
            {
                await UniTask.WaitForSeconds(sessionPingIntervalSeconds, ignoreTimeScale: true);
                
                if (!_isSessionActive) break;

                SendSessionPing();
            }
        }

        private void SendSessionPing()
        {
            if (!_isSessionActive || _currentSession == null)
                return;

            var currentTime = DateTime.UtcNow;
            var sessionDuration = (float)(currentTime - _currentSession.StartTime).TotalMinutes;
            
            // Update current session metrics
            _currentSession.DurationMinutes = sessionDuration;

            // Fire events
            OnSessionPing?.Invoke(_currentSession);
            IAnalyticsService.Instance?.TrackEvent(new SessionPingEvent(_currentSession, sessionDuration));
        }

        // Metrics tracking methods
        public void RecordQuestionDisplayed() => _currentSession?.RecordQuestionDisplayed();
        public void RecordQuestionAnswered(bool isCorrect) => _currentSession?.RecordQuestionAnswered(isCorrect);
        public void RecordQuestionSkipped() => _currentSession?.RecordQuestionSkipped();
        public void RecordLifeLost() => _currentSession?.RecordLifeLost();
        public void RecordGameOver() => _currentSession?.RecordGameOver();
        public void RecordStreak(int streakLength) => _currentSession?.RecordStreak(streakLength);
        public void RecordFactSetProgression() => _currentSession?.RecordFactSetProgression();

        private void OnDestroy()
        {
            if (_isSessionActive)
            {
                EndSession();
            }
        }
    }
} 