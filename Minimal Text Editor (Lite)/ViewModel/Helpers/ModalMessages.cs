using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minimal_Text_Editor__Lite_.View.GlobalModals;
using Minimal_Text_Editor__Lite_.Model;

namespace Minimal_Text_Editor__Lite_.ViewModel.Helpers
{
    public class ModalMessages
    {
        public static bool ShowConfirmModal(string title, string description, string boldMessage = "")
        {
            GlobalModalModel globalModalModel = new GlobalModalModel
            {
                ImageSource = "/Assets/Images/icon-eraser.png",
                HeaderContent = title,
                ShowTextField = false,
                LabelTextField = "...",
                TextFieldHint = "...",
                ShowSimpleText = true,
                SimpleTextContent = description,
                BoldTextContent = boldMessage,
                ShowConfirmationCheck = false,
                SaveButton = new ButtonModel { Text = App.Localization.Translate("Button_Reset"), IsVisible = true },
                CancelButton = new ButtonModel { Text = App.Localization.Translate("Button_Cancel"), IsVisible = true },
                OkButton = new ButtonModel { Text = App.Localization.Translate("Button_Ok"), IsVisible = false }
            };

            return OpenModalWithBooleanResult(globalModalModel, "");
        }

        public static void showErrorModal(string description, string boldMessage = "")
        {
            GlobalModalModel globalModalModel = new GlobalModalModel
            {
                ImageSource = "/Assets/Images/icon-close.png",
                HeaderContent = "An Error Ocurred!",
                ShowTextField = false,
                LabelTextField = "...",
                TextFieldHint = "...",
                ShowSimpleText = true,
                SimpleTextContent = description,
                BoldTextContent = boldMessage,
                ShowConfirmationCheck = false,
                SaveButton = new ButtonModel { Text = App.Localization.Translate("Button_Log"), IsVisible = false },
                CancelButton = new ButtonModel { Text = App.Localization.Translate("Button_Close"), IsVisible = true },
                OkButton = new ButtonModel { Text = App.Localization.Translate("Button_Ok"), IsVisible = false }
            };

            OpenModal(globalModalModel);
        }

        public static bool ShowConfirmOpenNoteModal(string title, string description, string boldMessage = "")
        {
            GlobalModalModel globalModalModel = new GlobalModalModel
            {
                ImageSource = "/Assets/Images/icon-notes.png",
                HeaderContent = title,
                ShowTextField = false,
                LabelTextField = "...",
                TextFieldHint = "...",
                ShowSimpleText = true,
                SimpleTextContent = description,
                BoldTextContent = boldMessage,
                ShowConfirmationCheck = true,
                SaveButton = new ButtonModel { Text = App.Localization.Translate("Button_Open"), IsVisible = true },
                CancelButton = new ButtonModel { Text = App.Localization.Translate("Button_Close"), IsVisible = true },
                OkButton = new ButtonModel { Text = App.Localization.Translate("Button_Ok"), IsVisible = false }
            };

            return OpenModalWithBooleanResult(globalModalModel, "OpenNoteMessage");
        }

        public static void showInfoModal(string title, string description, string boldMessage = "")
        {
            GlobalModalModel globalModalModel = new GlobalModalModel
            {
                ImageSource = "/Assets/Images/icon-info.png",
                HeaderContent = title,
                ShowTextField = false,
                LabelTextField = "",
                TextFieldHint = "",
                ShowSimpleText = true,
                SimpleTextContent = description,
                BoldTextContent = boldMessage,
                SaveButton = new ButtonModel { Text = App.Localization.Translate("Button_Save"), IsVisible = false },
                CancelButton = new ButtonModel { Text = App.Localization.Translate("Button_Cancel"), IsVisible = false },
                OkButton = new ButtonModel { Text = App.Localization.Translate("Button_Ok"), IsVisible = true }
            };

            OpenModal(globalModalModel);
        }

        public static void ShowSuccessModal(string title, string description, string boldMessage = "")
        {
            GlobalModalModel globalModalModel = new GlobalModalModel
            {
                ImageSource = "/Assets/Images/icon-check.png",
                HeaderContent = title,
                ShowTextField = false,
                LabelTextField = "...",
                TextFieldHint = "...",
                ShowSimpleText = true,
                SimpleTextContent = description,
                BoldTextContent = boldMessage,
                SaveButton = new ButtonModel { Text = "Reset", IsVisible = false },
                CancelButton = new ButtonModel { Text = "Cancel", IsVisible = false },
                OkButton = new ButtonModel { Text = "OK", IsVisible = true }
            };

            OpenModal(globalModalModel);
        }

        public static bool ShowBackupSizeMessageConfim(string title, string description, string boldMessage = "")
        {
            GlobalModalModel globalModalModel = new GlobalModalModel
            {
                ImageSource = "/Assets/Images/icon-info.png",
                HeaderContent = title,
                ShowTextField = false,
                LabelTextField = "...",
                TextFieldHint = "...",
                ShowSimpleText = true,
                SimpleTextContent = description,
                BoldTextContent = boldMessage,
                ShowConfirmationCheck = true,
                SaveButton = new ButtonModel { Text = App.Localization.Translate("Button_Delete"), IsVisible = false },
                CancelButton = new ButtonModel { Text = App.Localization.Translate("Button_Close"), IsVisible = true },
                OkButton = new ButtonModel { Text = App.Localization.Translate("Button_Ok"), IsVisible = false }
            };

            return OpenModalWithBooleanResult(globalModalModel, "BackupMessage");
        }

        private static void OpenModal(GlobalModalModel gmm)
        {
            GlobalModalsWindow globalModalsWindow = new GlobalModalsWindow(gmm, false, "");
            bool? dialogResult = globalModalsWindow.ShowDialog();
        }

        private static bool OpenModalWithBooleanResult(GlobalModalModel gmm, string typeMessage)
        {
            GlobalModalsWindow globalModalsWindow = new GlobalModalsWindow(gmm, false, typeMessage);
            bool? dialogResult = globalModalsWindow.ShowDialog();

            return (bool)dialogResult;
        }
    }
}
