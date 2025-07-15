using System;
using UnityEngine;

/// <summary>
/// Centralized input controller that manages all game input and fires events for different systems to listen to.
/// This prevents input conflicts between character movement and UI navigation.
/// </summary>
public class GameInputController : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private bool enableInput = true;

        // Generic directional input events
        public static event Action OnLeftInput;
        public static event Action OnRightInput;
        public static event Action OnUpInput;
        public static event Action OnJumpInput;
        public static event Action OnDownInput;
        public static event Action OnConfirmInput; // Enter or Space
        public static event Action OnPauseInput;

        protected Vector2 m_StartingTouch;
        protected bool m_IsSwiping = false;

        private void Update()
        {
            if (!enableInput) return;

            HandleInput();
        }

        private void HandleInput()
        {
            HandleKeyboardInput();
            HandleTouchInput();
        }

        private void HandleKeyboardInput()
        {
            // Arrow keys and WASD
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                OnLeftInput?.Invoke();
            }
            
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                OnRightInput?.Invoke();
            }
            
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                OnUpInput?.Invoke();
                OnJumpInput?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnJumpInput?.Invoke();
            }
            
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                OnDownInput?.Invoke();
            }
            
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                OnConfirmInput?.Invoke();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnPauseInput?.Invoke();
            }
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 1)
            {
                if (m_IsSwiping)
                {
                    Vector2 diff = Input.GetTouch(0).position - m_StartingTouch;

                    // Put difference in Screen ratio, but using only width, so the ratio is the same on both
                    // axes (otherwise we would have to swipe more vertically...)
                    diff = new Vector2(diff.x / Screen.width, diff.y / Screen.width);

                    if (diff.magnitude > 0.01f) //we set the swipe distance to trigger movement to 1% of the screen width
                    {
                        if (Mathf.Abs(diff.y) > Mathf.Abs(diff.x))
                        {
                            if (diff.y < 0)
                            {
                                OnDownInput?.Invoke();
                            }
                            else
                            {
                                OnUpInput?.Invoke();
                            }
                        }
                        else
                        {
                            if (diff.x < 0)
                            {
                                OnLeftInput?.Invoke();
                            }
                            else
                            {
                                OnRightInput?.Invoke();
                            }
                        }
                        m_IsSwiping = false;
                    }
                }

                // Input check is AFTER the swipe test, that way if TouchPhase.Ended happen a single frame after the Began Phase
                // a swipe can still be registered (otherwise, m_IsSwiping will be set to false and the test wouldn't happen for that began-Ended pair)
                if (Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    m_StartingTouch = Input.GetTouch(0).position;
                    m_IsSwiping = true;
                }
                else if (Input.GetTouch(0).phase == TouchPhase.Ended)
                {
                    m_IsSwiping = false;
                }
            }
        }
    } 