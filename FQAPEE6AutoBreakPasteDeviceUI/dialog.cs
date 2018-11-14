using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro;
using System.Windows;
using System.IO;

namespace SxjLibrary
{
    public class dialog
    {
        //确认操作框
        public async Task<bool> showconfirm(string msg)
        {
           
            MetroWindow window = (MahApps.Metro.Controls.MetroWindow)System.Windows.Application.Current.MainWindow;
            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "确  认",
                NegativeButtonText = "取  消",
               // ColorScheme = MetroDialogColorScheme.Accented,
            };
            var result = await window.ShowMessageAsync("确认操作框",
                msg,
                MessageDialogStyle.AffirmativeAndNegative, mySettings);

            bool r = result == MessageDialogResult.Affirmative;
            
           
            return r;

        }
        //消息提示框
        public async Task<bool> showmessage(string msg)
        {
            MetroWindow window = (MahApps.Metro.Controls.MetroWindow)System.Windows.Application.Current.MainWindow;
            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "确  定",
               // ColorScheme = MetroDialogColorScheme.Accented,
            };

            var result = await window.ShowMessageAsync("消息提示框",
                msg,
                MessageDialogStyle.Affirmative, mySettings);
            return true;
          
        }
        public async Task<bool> showmessageauto(string msg,int ms)
        {
            MetroWindow window = (MahApps.Metro.Controls.MetroWindow)System.Windows.Application.Current.MainWindow;
            //var dialog = (BaseMetroDialog)window.Resources["CustomDialogTest"];
            CustomDialog cdlg = new CustomDialog();
            cdlg.Title = msg;
       
         
            await window.ShowMetroDialogAsync(cdlg);

            await Task.Delay(ms);

            await window.HideMetroDialogAsync(cdlg);
            return true;
        }
       
        //登陆框
        public async Task<List<string>> showlogin()
        {
            MetroWindow window = (MahApps.Metro.Controls.MetroWindow)System.Windows.Application.Current.MainWindow;
            LoginDialogData result = await window.ShowLoginAsync("用户登录框", "输入你的凭证:", new LoginDialogSettings { ColorScheme = MetroDialogColorScheme.Theme, AffirmativeButtonText = "确  认", PasswordWatermark = "密码提示:初始密码 jsldr", NegativeButtonText = "取  消", InitialUsername = "Leader" });
            if (result == null)
            {
                //User pressed cancel
                List<string> logindata = new List<string>();
                logindata.Add("");
                logindata.Add("");
                return logindata;
            
            }
            else
            {
                List<string> logindata = new List<string>();
                logindata.Add(result.Username);
                logindata.Add(result.Password);
                return logindata;
            }

        }
        public async Task<string> showinput(string msg)
        {
            MetroWindow window = (MahApps.Metro.Controls.MetroWindow)System.Windows.Application.Current.MainWindow;
            var result = await window.ShowInputAsync("输入框", msg, new MetroDialogSettings { ColorScheme = ((MetroWindow)window).MetroDialogOptions.ColorScheme });
            if (result == null) //user pressed cancel
                return "";
            else
            {
                return result;
            }
        }
        public async Task<ProgressDialogController> showShowProgress(string str)
        {
            MetroWindow window = (MahApps.Metro.Controls.MetroWindow)System.Windows.Application.Current.MainWindow;
            var result = await window.ShowProgressAsync("请等待...", str);
            return (ProgressDialogController)result;

        }
        public List<string> getthemes()
        {
            List<string> themes = new List<string>();
            themes.Add("BaseLight");
            themes.Add("BaseDark");
            return themes;
        }
        public List<string> getaccents()
        {
            List<string> accents = new List<string>();
            var a = ThemeManager.Accents;
            foreach (var act in a)
            {
                accents.Add(act.Name);
            }
            return accents;
        }

        public void changetheme(string s)
        {
            var theme = ThemeManager.DetectAppStyle(System.Windows.Application.Current);
            var appTheme = ThemeManager.GetAppTheme(s);
            ThemeManager.ChangeAppStyle(System.Windows.Application.Current, theme.Item2, appTheme);
        }
        public void changeaccent(string s)
        {
            MetroWindow window = (MahApps.Metro.Controls.MetroWindow)System.Windows.Application.Current.MainWindow;
            var theme = ThemeManager.DetectAppStyle(System.Windows.Application.Current);
            var accent = ThemeManager.GetAccent(s);
            ThemeManager.ChangeAppStyle(System.Windows.Application.Current, accent, theme.Item1);

        }








    }
}
