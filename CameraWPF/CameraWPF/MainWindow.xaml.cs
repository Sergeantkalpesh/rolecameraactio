using NetSDKCS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CameraWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int m_WaitTime = 5000;

        private static fDisConnectCallBack m_DisConnectCallBack;
        private static fHaveReConnectCallBack m_HaveReConnectCallBack;


        private IntPtr m_LoginID = IntPtr.Zero;
        private NET_DEVICEINFO_Ex m_DeviceInfo;
        private IntPtr m_RealPlayID = IntPtr.Zero;


        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += CameraWindow_Loaded;
        }

        private void CameraWindow_Loaded(object sender, RoutedEventArgs e)
        {
            m_DisConnectCallBack = new fDisConnectCallBack(DisConnectCallBack);
            m_HaveReConnectCallBack = new fHaveReConnectCallBack(ReConnectCallBack);

            try
            {
                NETClient.Init(m_DisConnectCallBack, IntPtr.Zero, null);
                NET_LOG_SET_PRINT_INFO logPrintInfo = new NET_LOG_SET_PRINT_INFO()
                {
                    dwSize = (uint)Marshal.SizeOf(typeof(NET_LOG_SET_PRINT_INFO)),
                };
                NETClient.LogOpen(logPrintInfo);
                NETClient.SetAutoReconnect(m_HaveReConnectCallBack, IntPtr.Zero);
                InitOrLogoutUI();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }
        #region CallBack

        private void DisConnectCallBack(IntPtr lloginID, IntPtr pchDVRIP, int nDVRPort, IntPtr dwUser)
        {
            Dispatcher.BeginInvoke((Action)UpdateDisConnectUI);
        }

        private void UpdateDisConnectUI()
        {
            this.Title = "MainWindow - offline";
        }

        private void ReConnectCallBack(IntPtr lLoginID, IntPtr pchDVRIP, int nDVRPort, IntPtr dwUser)
        {
            Dispatcher.BeginInvoke((Action)UpdateReConnectUI);
        }
        private void UpdateReConnectUI()
        {
            this.Title = "MainWindow Online";
        }



        private void port_textBox_KeyPress(object sender, TextCompositionEventArgs e)
        {
            
            foreach (char c in e.Text)
            {
                if (!Char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void login_button_Click(object sender, RoutedEventArgs e)
        {
            if (IntPtr.Zero == m_LoginID)
            {
                ushort port = 0;
                try
                {
                    port = Convert.ToUInt16(port_textBox.Text.Trim());
                }
                catch
                {
                    System.Windows.MessageBox.Show("Input port error!");
                    return;
                }
                m_DeviceInfo = new NET_DEVICEINFO_Ex();
                m_LoginID = NETClient.LoginWithHighLevelSecurity(ip_textBox.Text.Trim(), port, name_textBox.Text.Trim(), pwd_textBox.Text.Trim(), EM_LOGIN_SPAC_CAP_TYPE.TCP, IntPtr.Zero, ref m_DeviceInfo);
                if (IntPtr.Zero == m_LoginID)
                {
                    System.Windows.MessageBox.Show(this, NETClient.GetLastError());
                    return;
                }
                LoginUI();
            }
            else
            {
                bool result = NETClient.Logout(m_LoginID);
                if (!result)
                {
                    System.Windows.MessageBox.Show(this, NETClient.GetLastError());
                    return;
                }
                m_LoginID = IntPtr.Zero;
                InitOrLogoutUI();
            }
        }

        private void start_realplay_button_Click(object sender, RoutedEventArgs e)
        {
            if (IntPtr.Zero == m_RealPlayID)
            {

                EM_RealPlayType type;
                if (streamtype_comboBox.SelectedIndex == 0)
                {
                    type = EM_RealPlayType.EM_A_RType_Realplay;
                }
                else
                {
                    type = EM_RealPlayType.EM_A_RType_Realplay_1;
                }
                m_RealPlayID = NETClient.RealPlay(m_LoginID, channel_comboBox.SelectedIndex, realplay_pictureBox.Handle, type);
                if (IntPtr.Zero == m_RealPlayID)
                {
                    System.Windows.MessageBox.Show(this, NETClient.GetLastError());
                    return;
                }
                start_realplay_button.Content = "StopReal";
                channel_comboBox.IsEnabled = false;
                streamtype_comboBox.IsEnabled = false;
            }
            else
            {
                bool ret = NETClient.StopRealPlay(m_RealPlayID);
                if (!ret)
                {
                    System.Windows.MessageBox.Show(this, NETClient.GetLastError());
                    return;
                }
                m_RealPlayID = IntPtr.Zero;
                start_realplay_button.Content = "StartReal";
                realplay_pictureBox.Refresh();
                channel_comboBox.IsEnabled = true;
                streamtype_comboBox.IsEnabled = true;
            }
        }
        #endregion

        #region Update UI
        private void InitOrLogoutUI()
        {
           
            login_button.Content = "Login";

            channel_comboBox.Items.Clear();
            channel_comboBox.IsEnabled = false;

            streamtype_comboBox.Items.Clear();
            streamtype_comboBox.IsEnabled = false;


            start_realplay_button.IsEnabled = false;

            m_RealPlayID = IntPtr.Zero;
            start_realplay_button.Content = "StartReal";

            realplay_pictureBox.Refresh();


            this.Title = "MainWindow";

            string path = System.AppDomain.CurrentDomain.BaseDirectory + "savedata";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

        }

        private void LoginUI()
        {
            login_button.Content = "Logout";

            channel_comboBox.IsEnabled = true;
            streamtype_comboBox.IsEnabled = true;

            start_realplay_button.IsEnabled = true;

            channel_comboBox.Items.Clear();
            for (int i = 1; i <= m_DeviceInfo.nChanNum; i++)
            {
                channel_comboBox.Items.Add(i);
            }

            streamtype_comboBox.Items.Clear();
            streamtype_comboBox.Items.Add("Main Stream");
            streamtype_comboBox.Items.Add("Extra Stream");

            if (channel_comboBox.Items.Count > 0) channel_comboBox.SelectedIndex = 0;
            if (streamtype_comboBox.Items.Count > 0) streamtype_comboBox.SelectedIndex = 0;

        }
        #endregion
    }
}