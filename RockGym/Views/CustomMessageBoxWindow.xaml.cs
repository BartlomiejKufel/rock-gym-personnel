using System;
using System.Media;
using System.Windows;
using System.Windows.Input;

namespace RockGym.Views
{
    /// <summary>
    /// Logika interakcji dla klasy CustomMessageBoxWindow.xaml
    /// </summary>
    public partial class CustomMessageBoxWindow : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public CustomMessageBoxWindow(string message, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            InitializeComponent();

            TitleTextBlock.Text = title;
            MessageTextBlock.Text = message;

            SetupIcon(icon);
            SetupButtons(button);
            PlaySound(icon);
        }

        private void SetupIcon(MessageBoxImage icon)
        {
            ErrorIcon.Visibility = Visibility.Collapsed;
            WarningIcon.Visibility = Visibility.Collapsed;
            InfoIcon.Visibility = Visibility.Collapsed;
            QuestionIcon.Visibility = Visibility.Collapsed;
            SuccessIcon.Visibility = Visibility.Collapsed;

            switch (icon)
            {
                case MessageBoxImage.Error:
                    ErrorIcon.Visibility = Visibility.Visible;
                    break;
                case MessageBoxImage.Warning:
                    WarningIcon.Visibility = Visibility.Visible;
                    break;
                case MessageBoxImage.Question:
                    QuestionIcon.Visibility = Visibility.Visible;
                    break;
                case MessageBoxImage.Information:
                    InfoIcon.Visibility = Visibility.Visible;
                    break;
                default:
                    int iconVal = (int)icon;
                    if (iconVal == 16)
                        ErrorIcon.Visibility = Visibility.Visible;
                    else if (iconVal == 48)
                        WarningIcon.Visibility = Visibility.Visible;
                    else if (iconVal == 32)
                        QuestionIcon.Visibility = Visibility.Visible;
                    else if (iconVal == 64)
                        InfoIcon.Visibility = Visibility.Visible;
                    else if (iconVal == 200)
                        SuccessIcon.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void SetupButtons(MessageBoxButton button)
        {
            BtnOk.Visibility = Visibility.Collapsed;
            BtnYes.Visibility = Visibility.Collapsed;
            BtnNo.Visibility = Visibility.Collapsed;
            BtnCancel.Visibility = Visibility.Collapsed;

            switch (button)
            {
                case MessageBoxButton.OK:
                    BtnOk.Visibility = Visibility.Visible;
                    BtnOk.IsDefault = true;
                    break;
                case MessageBoxButton.OKCancel:
                    BtnOk.Visibility = Visibility.Visible;
                    BtnCancel.Visibility = Visibility.Visible;
                    BtnOk.IsDefault = true;
                    BtnCancel.IsCancel = true;
                    break;
                case MessageBoxButton.YesNo:
                    BtnYes.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    BtnYes.IsDefault = true;
                    BtnNo.IsCancel = true;
                    break;
                case MessageBoxButton.YesNoCancel:
                    BtnYes.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    BtnCancel.Visibility = Visibility.Visible;
                    BtnYes.IsDefault = true;
                    BtnCancel.IsCancel = true;
                    break;
            }
        }

        private void PlaySound(MessageBoxImage icon)
        {
            try
            {
                switch (icon)
                {
                    case MessageBoxImage.Error:
                        SystemSounds.Hand.Play();
                        break;
                    case MessageBoxImage.Question:
                        SystemSounds.Question.Play();
                        break;
                    case MessageBoxImage.Warning:
                        SystemSounds.Exclamation.Play();
                        break;
                    case MessageBoxImage.Information:
                        SystemSounds.Asterisk.Play();
                        break;
                    default:
                        if ((int)icon == 200)
                            SystemSounds.Asterisk.Play();
                        else
                            SystemSounds.Beep.Play();
                        break;
                }
            }
            catch
            {

            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (BtnCancel.Visibility == Visibility.Visible)
                Result = MessageBoxResult.Cancel;
            else if (BtnNo.Visibility == Visibility.Visible)
                Result = MessageBoxResult.No;
            else
                Result = MessageBoxResult.OK;

            Close();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            Close();
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = Visibility.Visible == BtnCancel.Visibility ? MessageBoxResult.Cancel : MessageBoxResult.None;
            Result = MessageBoxResult.Cancel;
            Close();
        }
    }
}
