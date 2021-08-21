using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DynamoDBForUnity
{
    public class UiManager : Singleton<UiManager>
    {
        private static TMP_Text _display;

        public TMP_Text TextId;
        public GameObject ContentGo;
        public Button ButtonScanEntries;
        public Button ButtonHighScoreBiggerThanZero;
        public Button ButtonUpdateCreate;
        public GameObject UpdateCreateGo;
        public InputField InputId;
        public InputField InputInitials;
        public InputField InputHighScore;
        public Button UpdateCreateSubmit;

        void Awake()
        {
            ButtonScanEntries.onClick.AddListener(AwsManager.Instance.ScanEntries);
            ButtonHighScoreBiggerThanZero.onClick.AddListener(AwsManager.Instance.HighScoreBiggerThanZero);
            ButtonUpdateCreate.onClick.AddListener(OnButtonUpdateCreate);
        }

        void Start()
        {
            if (TextId == null || ContentGo == null ||
                ButtonScanEntries == null || ButtonHighScoreBiggerThanZero == null || ButtonUpdateCreate == null ||
                UpdateCreateGo == null || InputId == null || InputInitials == null || InputHighScore == null ||
                UpdateCreateSubmit == null || 
                AwsManager.Instance.Player.UserId == null)
                throw new ArgumentNullException();

            UpdateCreateGo.SetActive(false);
            TextId.text = $"UserId: {AwsManager.Instance.Player.UserId}";
            _display = ContentGo.GetComponent<TMP_Text>();

            AwsManager.Instance.ClearDisplay += ClearDisplay;
            AwsManager.Instance.AppendDisplay += AppendToDisplay;
            AwsManager.Instance.ErrorDisplay += DisplayError;
        }

        /// <summary>
        /// Activates update/create game object and sets input fields
        /// Deactivates update/create game object and clears input fields
        /// </summary>
        public void OnButtonUpdateCreate()
        {
            UpdateCreateGo.SetActive(!UpdateCreateGo.activeSelf);

            if (UpdateCreateGo.activeSelf)
            {
                InputId.text = AwsManager.Instance.Player.UserId;
                InputInitials.text = AwsManager.Instance.Player.Initials;
                InputHighScore.text = AwsManager.Instance.Player.HighScore.ToString();
            }
            else
            {
                InputId.text = string.Empty;
                InputInitials.text = string.Empty;
                InputHighScore.text = string.Empty;
            }
        }

        /// <summary>
        /// Updates or create player info
        /// </summary>
        public void OnSubmitUpdateCreate()
        {
            if (!InputId.text.Equals(AwsManager.Instance.Player.UserId))
            {
                var playerInfo = new PlayerInfo
                {
                    UserId = InputId.text,
                    Initials = InputInitials.text,
                    HighScore = int.Parse(InputHighScore.text)
                };

                AwsManager.Instance.CreatePlayerInfo(playerInfo);
            }
            else
            {
                if (!InputInitials.text.Equals(AwsManager.Instance.Player.Initials))
                    AwsManager.Instance.UpdateInitials(InputInitials.text);

                if (int.Parse(InputHighScore.text) != AwsManager.Instance.Player.HighScore)
                    AwsManager.Instance.UpdateHighScore(InputHighScore.text);
            }

            OnButtonUpdateCreate();
        }

        /// <summary>
        /// Enables/disables submit button
        /// </summary>
        public void OnInputUpdateCreateChange()
        {
            if (!string.IsNullOrEmpty(InputId.text) &&
                !string.IsNullOrEmpty(InputInitials.text) &&
                !string.IsNullOrEmpty(InputHighScore.text))
            {
                UpdateCreateSubmit.interactable = true;
            }
            else
                UpdateCreateSubmit.interactable = false;
        }

        /// <summary>
        /// Clears display
        /// </summary>
        public static void ClearDisplay() => _display.text = string.Empty;

        /// <summary>
        /// Sets error to display
        /// </summary>
        /// <param name="s"></param>
        public static void DisplayError(string s)
        {
            ClearDisplay();
            _display.color = Color.red;
            _display.text = s;
        }

        /// <summary>
        /// Sets message to display
        /// </summary>
        /// <param name="s"></param>
        public static void DisplayMessage(string s)
        {
            ClearDisplay();
            _display.color = Color.white;
            _display.text += s;
        }

        /// <summary>
        /// Appends to display
        /// </summary>
        /// <param name="s"></param>
        public static void AppendToDisplay(string s)
        {
            _display.color = Color.white;
            _display.text += s + Environment.NewLine;
        }
    }
}
