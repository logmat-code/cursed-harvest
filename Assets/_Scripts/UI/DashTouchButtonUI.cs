using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CursedHarvest
{
    /// <summary>
    /// Bottom-right Touch Button for Dash ability in Mobile Portrait mode.
    /// Displays radial cooldown fill and fires independently of VirtualJoystickUI pointer events.
    /// </summary>
    public class DashTouchButtonUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Image buttonBgImage;
        [SerializeField] private Image cooldownRadialImage;
        [SerializeField] private Text iconGlyphText;

        private PlayerDash playerDash;
        private bool isPressed;

        public Image CooldownRadialImage => cooldownRadialImage;

        public void SetupReferences(Image bgImg, Image radialImg, Text glyphTxt)
        {
            buttonBgImage = bgImg;
            cooldownRadialImage = radialImg;
            iconGlyphText = glyphTxt;

            Sprite circleSpr = VirtualJoystickUI.GetProceduralCircleSprite(false);
            if (buttonBgImage != null)
            {
                buttonBgImage.sprite = circleSpr;
            }
            if (cooldownRadialImage != null)
            {
                cooldownRadialImage.sprite = circleSpr;
                cooldownRadialImage.type = Image.Type.Filled;
                cooldownRadialImage.fillMethod = Image.FillMethod.Radial360;
                cooldownRadialImage.fillOrigin = (int)Image.Origin360.Top;
                cooldownRadialImage.fillClockwise = false;
                cooldownRadialImage.fillAmount = 0f;
            }
        }

        public void EnsureChildVisuals()
        {
            if (buttonBgImage == null) buttonBgImage = GetComponent<Image>();
            if (buttonBgImage == null) buttonBgImage = gameObject.AddComponent<Image>();

            if (cooldownRadialImage == null)
            {
                Transform rad = transform.Find("CooldownOverlay");
                if (rad == null)
                {
                    GameObject radObj = new GameObject("CooldownOverlay", typeof(RectTransform), typeof(Image));
                    radObj.transform.SetParent(transform, false);
                    RectTransform rtRad = radObj.GetComponent<RectTransform>();
                    rtRad.anchorMin = Vector2.zero;
                    rtRad.anchorMax = Vector2.one;
                    rtRad.offsetMin = Vector2.zero;
                    rtRad.offsetMax = Vector2.zero;
                    rad = radObj.transform;
                }
                cooldownRadialImage = rad.GetComponent<Image>();
                cooldownRadialImage.color = new Color(0.05f, 0.05f, 0.08f, 0.72f);
                cooldownRadialImage.raycastTarget = false;
            }

            if (iconGlyphText == null)
            {
                Transform ic = transform.Find("DashIcon");
                if (ic == null)
                {
                    GameObject icObj = new GameObject("DashIcon", typeof(RectTransform), typeof(Text));
                    icObj.transform.SetParent(transform, false);
                    RectTransform rtIc = icObj.GetComponent<RectTransform>();
                    rtIc.anchorMin = Vector2.zero;
                    rtIc.anchorMax = Vector2.one;
                    rtIc.offsetMin = Vector2.zero;
                    rtIc.offsetMax = Vector2.zero;
                    ic = icObj.transform;
                }
                iconGlyphText = ic.GetComponent<Text>();
                Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
                iconGlyphText.font = f;
                iconGlyphText.fontSize = 28;
                iconGlyphText.fontStyle = FontStyle.Bold;
                iconGlyphText.alignment = TextAnchor.MiddleCenter;
                iconGlyphText.text = "»\nDASH";
                iconGlyphText.color = new Color(1.0f, 0.94f, 0.52f, 1f);
                iconGlyphText.raycastTarget = false;
            }

            SetupReferences(buttonBgImage, cooldownRadialImage, iconGlyphText);
        }

        private void Awake()
        {
            EnsureChildVisuals();
        }

        private void Start()
        {
            FindPlayerDash();
        }

        private void FindPlayerDash()
        {
            if (playerDash == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerDash = player.GetComponent<PlayerDash>();
                }
            }
        }

        private void Update()
        {
            if (playerDash == null) FindPlayerDash();
            if (playerDash == null) return;

            float cdPct = playerDash.CooldownPercent;
            bool canDash = playerDash.CanDash;

            if (cooldownRadialImage != null)
            {
                cooldownRadialImage.fillAmount = cdPct;
                cooldownRadialImage.gameObject.SetActive(cdPct > 0.005f);
            }

            if (buttonBgImage != null)
            {
                if (!canDash)
                {
                    buttonBgImage.color = new Color(0.35f, 0.32f, 0.30f, 0.72f);
                }
                else
                {
                    buttonBgImage.color = isPressed
                        ? new Color(1.0f, 0.90f, 0.45f, 1.0f)
                        : new Color(0.92f, 0.75f, 0.30f, 0.92f);
                }
            }

            if (iconGlyphText != null)
            {
                iconGlyphText.color = canDash
                    ? new Color(1.0f, 0.92f, 0.48f, 1f)
                    : new Color(0.55f, 0.52f, 0.48f, 0.7f);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            TriggerDashInput();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
        }

        public void TriggerDashInput()
        {
            if (GameInputManager.Instance != null)
            {
                GameInputManager.Instance.RequestTouchDash();
            }

            if (playerDash == null) FindPlayerDash();
            if (playerDash != null && playerDash.CanDash)
            {
                playerDash.StartDash();
            }
        }
    }
}
