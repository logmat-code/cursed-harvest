using UnityEngine;

namespace CursedHarvest
{
    public interface IGameInputSource
    {
        Vector2 GetMoveAxes();
        bool PollDashPressed();
    }

    public class DesktopInputSource : IGameInputSource
    {
        public Vector2 GetMoveAxes()
        {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 vec = new Vector2(h, v);
            if (vec.sqrMagnitude > 1f) vec.Normalize();
            return vec;
#else
            return Vector2.zero;
#endif
        }

        public bool PollDashPressed()
        {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift);
#else
            return false;
#endif
        }
    }

    public class MobileTouchInputSource : IGameInputSource
    {
        private Vector2 touchMoveAxes = Vector2.zero;
        private bool dashQueued = false;

        public void SetMoveAxes(Vector2 axes)
        {
            touchMoveAxes = Vector2.ClampMagnitude(axes, 1f);
        }

        public void QueueDash()
        {
            dashQueued = true;
        }

        public Vector2 GetMoveAxes() => touchMoveAxes;

        public bool PollDashPressed()
        {
            if (dashQueued)
            {
                dashQueued = false;
                return true;
            }
            return false;
        }

        public void ResetState()
        {
            touchMoveAxes = Vector2.zero;
            dashQueued = false;
        }
    }

    /// <summary>
    /// Unified Input Abstraction layer feeding PlayerController and PlayerDash
    /// without coupling gameplay scripts to raw keyboard or touch UI components.
    /// Supports simultaneous Multitouch (Virtual Joystick + Dash Button) and Editor keyboard fallback.
    /// </summary>
    public class GameInputManager : MonoBehaviour
    {
        private static GameInputManager _instance;
        public static GameInputManager Instance
        {
            get
            {
                if (_instance == null || !_instance)
                {
                    _instance = FindFirstObjectByType<GameInputManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameInputManager");
                        _instance = go.AddComponent<GameInputManager>();
                        if (Application.isPlaying) DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private readonly DesktopInputSource desktopInput = new DesktopInputSource();
        private readonly MobileTouchInputSource mobileInput = new MobileTouchInputSource();

        public MobileTouchInputSource MobileInput => mobileInput;
        public DesktopInputSource DesktopInput => desktopInput;

        public Vector3 MoveVector
        {
            get
            {
                Vector2 mobileVec = mobileInput.GetMoveAxes();
                if (mobileVec.sqrMagnitude > 0.0001f)
                {
                    return new Vector3(mobileVec.x, 0f, mobileVec.y);
                }

                Vector2 desktopVec = desktopInput.GetMoveAxes();
                if (desktopVec.sqrMagnitude > 0.0001f)
                {
                    return new Vector3(desktopVec.x, 0f, desktopVec.y);
                }

                return Vector3.zero;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        public void SetTouchMoveInput(Vector2 moveAxes)
        {
            mobileInput.SetMoveAxes(moveAxes);
        }

        public void RequestTouchDash()
        {
            mobileInput.QueueDash();
        }

        public bool ConsumeDashPressed()
        {
            bool mobileDash = mobileInput.PollDashPressed();
            bool desktopDash = desktopInput.PollDashPressed();
            return mobileDash || desktopDash;
        }

        public void ResetAllInput()
        {
            mobileInput.ResetState();
        }
    }
}
