using System;
using System.Windows;
using RockGym.Views;

namespace RockGym.Services
{
    public static class CustomMessageBox
    {
        public static MessageBoxResult Show(string messageBoxText)
        {
            return ShowInternal(messageBoxText, "Wiadomość", MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption)
        {
            return ShowInternal(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        {
            return ShowInternal(messageBoxText, caption, button, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return ShowInternal(messageBoxText, caption, button, icon);
        }

        public static MessageBoxResult ShowSuccess(string messageBoxText, string caption = "Sukces")
        {
            return ShowInternal(messageBoxText, caption, MessageBoxButton.OK, (MessageBoxImage)200);
        }

        private static MessageBoxResult ShowInternal(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            MessageBoxResult result = MessageBoxResult.None;

            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    result = ShowWindow(messageBoxText, caption, button, icon);
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        result = ShowWindow(messageBoxText, caption, button, icon);
                    });
                }
            }
            else
            {
                result = ShowWindow(messageBoxText, caption, button, icon);
            }

            return result;
        }

        private static MessageBoxResult ShowWindow(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            CustomMessageBoxWindow dialog = new CustomMessageBoxWindow(messageBoxText, caption, button, icon);
            
            if (Application.Current != null)
            {
                foreach (Window win in Application.Current.Windows)
                {
                    if (win.IsActive && win != dialog)
                    {
                        dialog.Owner = win;
                        break;
                    }
                }
                
                if (dialog.Owner == null && Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible && Application.Current.MainWindow != dialog)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }

            dialog.ShowDialog();
            return dialog.Result;
        }
    }
}
