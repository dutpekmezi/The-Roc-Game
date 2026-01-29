using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.Singleton;

namespace Utils.Buttons
{
    public class ButtonManager : Singleton<ButtonManager>
    {
        //public TapTutorialHand TapTutorialHand;
        public static List<BaseButton> AllButtons { get; private set; } = new ();

        public static bool isAllDisabled = false;

        public delegate void ButtonClickedEvent(BaseButton baseButton);
        public ButtonClickedEvent OnButtonClicked;

        public static void RegisterButton(BaseButton button)
        {
            if (!AllButtons.Contains(button))
            {
                if (isAllDisabled)
                {
                    button.Button.interactable = false;
                }

                AllButtons.Add(button);
            }
        }

        public static void UnregisterButton(BaseButton button)
        {
            if (AllButtons.Contains(button))
            {
                AllButtons.Remove(button);
            }
        }

        public static BaseButton GetButtonById(string buttonId)
        {
            return AllButtons.Find(button => button.ButtonId == buttonId);
        }

        public static void DisableAllButtons(BaseButton[] exceptions = null)
        {
            isAllDisabled = true;

            foreach (var button in AllButtons)
            {
                if (exceptions != null && exceptions.Contains(button))
                {
                    button.Button.interactable = true;
                }
                else if ( button.Button != null)
                {
                    button.Button.interactable = false;
                }
            }
        }

        public static void EnableAllButtons(BaseButton[] exceptions = null)
        {
            isAllDisabled = false;

            foreach (var button in AllButtons)
            {
                if (exceptions != null && exceptions.Contains(button))
                {
                    button.Button.interactable = false;
                }
                else if (button.Button != null)
                {
                    button.Button.interactable = true;
                }
            }
        }

        /*public static void ShowTutorialHand(string buttonId)
        {
            var button = GetButtonById(buttonId);
            if (button != null)
            {
                button.ShowTutorialHand();
            }
        }

        public static void ShowTutorialHand(BaseButton baseButton)
        {
            if (baseButton != null)
            {
                baseButton.ShowTutorialHand();
            }
        }

        public static void HideTutorialHand(string buttonId)
        {
            var button = GetButtonById(buttonId);
            if (button != null)
            {
                button.HideTutorialHand();
            }
        }

        public static void HideTutorialHand(BaseButton baseButton)
        {
            if (baseButton != null)
            {
                baseButton.HideTutorialHand();
            }
        }*/

        public void OnButtonClickedHandler(BaseButton baseButton)
        {
            OnButtonClicked?.Invoke(baseButton);
        }
    }
}