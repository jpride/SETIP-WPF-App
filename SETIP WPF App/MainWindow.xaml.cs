﻿using System;
using System.Windows;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Windows.Input;
using System.Drawing;
using System.Windows.Forms.VisualStyles;

namespace SETIP_WPF_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Application : Window
    {

        private static System.Windows.Forms.Timer _timer;

        private NetworkInterface _nic;
        private string _adapter;
        private int _adapterCount = 0;
        private NetworkInterfaceType _adapterType = NetworkInterfaceType.Ethernet;

        private readonly string _appTitle = "IP Address Configurator";
        public bool _messageBoxIsShown = false; //tracks state of message box, in order to stop them from stacking indefinetely

        readonly string _dhcpChoiceContent = "DHCP";
        readonly string _choice2Content = "10.10.1.253 / 16";
        readonly string _choice3Content = "192.168.1.253 / 24";
        readonly string _choice4Content = "169.254.1.253 / 16";
        readonly string _defaultChoice5Content = "Enter ip / maskbits";

        readonly string _choice2Address = "10.10.1.253";
        readonly string _choice3Address = "192.168.1.253";
        readonly string _choice4Address = "169.254.1.253";

        readonly string _dhcpNetShChoiceString = "dhcp";
        readonly string _choice2NetShString = "static address=10.10.1.253 mask=255.255.0.0 gateway=10.10.1.1";
        readonly string _choice3NetShString = "static address=192.168.1.253 mask=255.255.255.0 gateway=192.168.1.1";
        readonly string _choice4NetShString = "static address=169.254.1.253 mask=255.255.0.0 gateway=169.254.1.1";


        public Application()
        {
            InitializeComponent();

            //Event handler attached to the Window (looking for Escape Key)
            this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(OnKeyDownInMainWindowHandler);

            //Event handler watching for Network Address Change, triggers the UpdateAdapterInfo() method
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(AddressChangedCallback);

            //Initialize button content
            Choice1Btn.Content = _dhcpChoiceContent;
            Choice2Btn.Content = _choice2Content;
            Choice3Btn.Content = _choice3Content;
            Choice4Btn.Content = _choice4Content;
            userEntryTxt.Text = _defaultChoice5Content;
            adapterLabel.Content = "Adapter:";

            //intialize adapter info
            UpdateAdapterInfo();
        }

        //work in progress
        private void AddressChangedCallback(object sender, EventArgs e)
        {
            //Refresh apapters and find the one that matches _adapter from UpdateAdapterInfo()
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface n in adapters)
            {
                if (n.Name == _adapter)
                { 
                    _nic = n;
                    break;
                }
            }

            Console.WriteLine("Address Changed Detected on adapter: {0}", _nic.Name);
            Console.WriteLine("Adapter {0} is {1}", _nic.Name, _nic.OperationalStatus);

            //Run UpdateAdapterInfo() if the adapter is Up, if not, place message in label that reports it down
            if (_nic.OperationalStatus == OperationalStatus.Up)
            {

                try
                {
                    UpdateAdapterInfo();
                }
                catch (Exception ex)
                {
                    Console.Write("Error in Callback: {0}", ex.Message);
                }
            }
            else {
                this.Dispatcher.Invoke(() =>
                {
                    adapterName.Text = _adapter + " is down...";
                });
            }
        }

        private void AdapterTimeOut(object sender, EventArgs e)
        {
            //upon expiration of timer, Rerun UpdateAdapterInfo()
            _timer.Stop();
            UpdateAdapterInfo();
        }

        private void ResultTimer_Tick(object sender, EventArgs e)
        {
            UpdateAdapterInfo();
        }

        public void ProcessRequest(Process p)
        {
            try
            {
                _ = p.Start();
                _ = p.WaitForExit(10000);
                var _result = p.StandardOutput.ReadToEnd();
            }            
            catch (Exception ex)
            {
                ShowMessage(_messageBoxIsShown, "Error Processing Request:\n" + ex.Message);
                var _result = ex.Message;
            }
        }

        public void UpdateAdapterInfo()
        {
            //grab list of all NetworkInterfaces
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            //loop through list and find interface that is both "Up" and contains the word 'Ethernet' in it
            foreach (NetworkInterface nic in interfaces)
            {

                IPInterfaceProperties adapterProps = nic.GetIPProperties();
                IPv4InterfaceProperties prop = adapterProps.GetIPv4Properties();

                //Console.WriteLine($"Nic Type: {nic.NetworkInterfaceType}");
                //Console.WriteLine($"{NetworkInterfaceType.Wireless80211}");

                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    if (_adapterType == NetworkInterfaceType.Wireless80211) //selected adapter 
                    {
                        if (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                        {
                            if (nic.Name.Contains("Wi-Fi"))
                            {
                                if (!nic.Name.Contains("vEthernet") & !nic.Name.Contains("Loopback") & !nic.Name.Contains("Bluetooth"))
                                {
                                    //once a valid adapter is found, places the name in the adapterName box and sets the adapter variable used in the processes to the name
                                    _nic = nic;
                                    var speed = ((nic.Speed * 10 )/ 1000000000) >= 1 ? "1 Gbps" : "100 Mbps";
                                    _adapter = nic.Name;

                                    //'this' is the Main Window Class and Dispatcher handles updating the UI
                                    Dispatcher.Invoke(() =>
                                    {
                                        adapterName.Text = _adapter + " (" + speed + ")";
                                        this.WifiSelectBtn.IsChecked = true;
                                        this.EthernetSelectBtn.IsChecked = false;
                                    });

                                    if (prop.IsDhcpEnabled)
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            Choice1Btn.IsChecked = true;
                                        });

                                    }

                                    _adapterCount++;

                                    foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                                    {
                                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                        {
                                            Dispatcher.Invoke(() =>
                                            {
                                                adapterName.Text = nic.Name + " (" + speed + ")";
                                                hostName.Text = Dns.GetHostName();
                                                ipAddress.Text = ip.Address.ToString() + "/" + ip.PrefixLength.ToString();
                                            });

                                            if (ip.Address.ToString() == _choice2Address)
                                            {
                                                Dispatcher.Invoke(() =>
                                                {
                                                    Choice2Btn.IsChecked = true;
                                                });
                                            }
                                            else if (ip.Address.ToString() == _choice3Address)
                                            {
                                                Dispatcher.Invoke(() =>
                                                {
                                                    Choice3Btn.IsChecked = true;
                                                });
                                            }
                                            else if (ip.Address.ToString() == _choice4Address)
                                            {
                                                Dispatcher.Invoke(() =>
                                                {
                                                    Choice4Btn.IsChecked = true;
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (_adapterType == NetworkInterfaceType.Ethernet) //selected adapter 
                    {
                        if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                        {
                            if (nic.Name.Contains("Ethernet"))
                            {
                                if (!nic.Name.Contains("vEthernet") & !nic.Name.Contains("Loopback") & !nic.Name.Contains("Bluetooth"))
                                {
                                    //once a valid adapter is found, places the name in the adapterName box and sets the adapter variable used in the processes to the name
                                    _nic = nic;
                                    var speed = ((nic.Speed * 10) / 1000000000) >= 1 ? "1 Gbps" : "100 Mbps";
                                    _adapter = nic.Name;

                                    //'this' is the Main Window Class and Dispatcher handles updating the UI
                                    Dispatcher.Invoke(() =>
                                    {
                                        adapterName.Text = _adapter +  " (" + speed + ")";
                                        EthernetSelectBtn.IsChecked = true;
                                        WifiSelectBtn.IsChecked = false;
                                    });

                                    if (prop.IsDhcpEnabled)
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            Choice1Btn.IsChecked = true;
                                        });

                                    }

                                    _adapterCount++;

                                    foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                                    {
                                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                        {
                                            Dispatcher.Invoke(() =>
                                            {
                                                adapterName.Text = nic.Name + " (" + speed + ")";
                                                hostName.Text = Dns.GetHostName();
                                                ipAddress.Text = ip.Address.ToString() + "/" + ip.PrefixLength.ToString();
                                            });

                                            if (ip.Address.ToString() == _choice2Address)
                                            {
                                                Dispatcher.Invoke(() =>
                                                {
                                                    Choice2Btn.IsChecked = true;
                                                });
                                            }
                                            else if (ip.Address.ToString() == _choice3Address)
                                            {
                                                Dispatcher.Invoke(() =>
                                                {
                                                    Choice3Btn.IsChecked = true;
                                                });
                                            }
                                            else if (ip.Address.ToString() == _choice4Address)
                                            {
                                                Dispatcher.Invoke(() =>
                                                {
                                                    Choice4Btn.IsChecked = true;
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (_adapterCount == 0)
            {
                //if no active wired adapters found, start a 5 second timer and place message in error box
                //after 5 seconds, run UpdateAdapterInfo() again
                _timer = new System.Windows.Forms.Timer();
                _timer.Tick += new EventHandler(AdapterTimeOut);
                _timer.Interval = 5000;
                _timer.Start();
                adapterName.Text = "Waiting for active adapter";
                ShowMessage(_messageBoxIsShown, "No active adapters found...Waiting for active adapter");
            }
        }

        public Process CreateProcess(string adapter, string ipMaskString)
        {
            var p = new Process();
            p.StartInfo.FileName = "netsh.exe";
            p.StartInfo.Arguments = String.Format("interface ipv4 set address name=\"{0}\" {1}", adapter, ipMaskString);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;

            return p;
        }

        public void ShowMessage(bool messageBoxShown, string msg)
        {
            if (!messageBoxShown)
            {
                _messageBoxIsShown = true;
                _ = System.Windows.MessageBox.Show(msg, _appTitle);
            }
        }


        //*******************   UI events   ***************************//


        private void Choice1Btn_Click(object sender, RoutedEventArgs e)
        {
            ResetUserEntryText();

            if ((bool)Choice1Btn.IsChecked)
            {
                Process p = CreateProcess(_adapter, _dhcpNetShChoiceString);
                ProcessRequest(p);

                adapterName.Text = "waiting for DHCP...";
                UpdateAdapterInfo();

            }
        }

        private void Choice2Btn_Click(object sender, RoutedEventArgs e)
        {
            ResetUserEntryText();

            if ((bool)Choice2Btn.IsChecked)
            {
                Process p = CreateProcess(_adapter, _choice2NetShString);
                ProcessRequest(p);
                UpdateAdapterInfo();
            }

        }

        private void Choice3Btn_Click(object sender, RoutedEventArgs e)
        {
            ResetUserEntryText();

            if ((bool)Choice3Btn.IsChecked)
            {
                Process p = CreateProcess(_adapter, _choice3NetShString);
                ProcessRequest(p);
                UpdateAdapterInfo();
            }
        }

        private void Choice4Btn_Click(object sender, RoutedEventArgs e)
        {
            ResetUserEntryText();

            if ((bool)Choice4Btn.IsChecked)
            {
                Process p = CreateProcess(_adapter, _choice4NetShString);
                ProcessRequest(p);
                UpdateAdapterInfo();
            }
        }

        private void userEntryTxt_GotFocus(object sender, RoutedEventArgs e)
        {
            userEntryTxt.Text = "";
            userEntryTxt.Foreground = System.Windows.Media.Brushes.Black;
        }

        private void Choice5Btn_Click(object sender, RoutedEventArgs e)
        {
            //userEntryTxt.Text = "";
            //commented out because the user experience was not ideal. Text would clear out if user selected the button after entering  desired address
        }

        private void ApplyUserEntryBtn_Click(object sender, RoutedEventArgs e)
        {

            if ((bool)Choice5Btn.IsChecked)
            {

                //split character
                string sep = @" ";
                bool validIP;
                bool validMask;


                try
                {
                    if (userEntryTxt.Text.Contains(" "))
                    {
                        //split user input string into ipa and ipm
                        string[] customIP = userEntryTxt.Text.Split(sep.ToCharArray());


                        validIP = IPAddress.TryParse(customIP[0], out IPAddress ip);
                        validMask = IPAddress.TryParse(customIP[1], out IPAddress mask);


                        if (!validIP || !validMask)
                        {
                            ShowMessage(_messageBoxIsShown, "Not a Valid IP Address! Try Again");
                        }
                        else
                        {
                            Process p = new Process();
                            p.StartInfo.FileName = "netsh.exe";
                            p.StartInfo.Arguments = String.Format("interface ipv4 set address name=\"{0}\" static {1} {2}", _adapter, ip, mask);
                            p.StartInfo.UseShellExecute = false;
                            p.StartInfo.CreateNoWindow = true;
                            p.StartInfo.RedirectStandardOutput = true;
                            ProcessRequest(p);
                            UpdateAdapterInfo();
                        }
                    }
                    else if (userEntryTxt.Text.Contains("/"))
                    {
                        sep = @"/";
                        string[] customIP = userEntryTxt.Text.Split(sep.ToCharArray());

                        validIP = IPAddress.TryParse(customIP[0], out IPAddress ip);
                        bool validMaskBits = int.TryParse(customIP[1], out int maskBits);

                        if (validMaskBits)
                        {
                            string mask = null;

                            switch (maskBits)
                            {
                                case 8:
                                    mask = "255.0.0.0";
                                    break;
                                case 16:
                                    mask = "255.255.0.0";
                                    break;
                                case 24:
                                    mask = "255.255.255.0";
                                    break;
                                default:
                                    mask = null;
                                    ShowMessage(_messageBoxIsShown, "Invalid Maskbits! This app only supports '/16' or '/24'");
                                    break;
                            }

                            if (!string.IsNullOrEmpty(mask))
                            {
                                Process p = new Process();
                                p.StartInfo.FileName = "netsh.exe";
                                p.StartInfo.Arguments = String.Format("interface ipv4 set address name=\"{0}\" static {1} {2}", _adapter, ip, mask);
                                p.StartInfo.UseShellExecute = false;
                                p.StartInfo.CreateNoWindow = true;
                                p.StartInfo.RedirectStandardOutput = true;
                                ProcessRequest(p);
                                UpdateAdapterInfo();
                            }
                        }
                        else
                        {
                            ShowMessage(_messageBoxIsShown, "Invalid Maskbits! This app only supports '/16' or '/24'");
                        }
                    }
                    else
                    {
                        ShowMessage(_messageBoxIsShown, "Invalid Entry! Try Again");
                    }
                }

                catch (IndexOutOfRangeException)
                {
                    ShowMessage(_messageBoxIsShown, "You must enter an IPAddress followed by a single space, then a Subnet Mask");
                    userEntryTxt.Text = _defaultChoice5Content;
                }
                catch (Exception)
                {
                    ShowMessage(_messageBoxIsShown, "Invalid! Try Again");
                }
            }
        }

        private void OnKeyDownInUserEntryBoxHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {

                if ((bool)Choice5Btn.IsChecked)
                {
                    
                    //split character
                    string sep = @" ";
                    bool validIP;
                    bool validMask;


                    try
                    {
                        if (userEntryTxt.Text.Contains(" "))
                        {
                            //split user input string into ipa and ipm
                            string[] customIP = userEntryTxt.Text.Split(sep.ToCharArray());


                            validIP = IPAddress.TryParse(customIP[0], out IPAddress ip);
                            validMask = IPAddress.TryParse(customIP[1], out IPAddress mask);


                            if (!validIP || !validMask)
                            {
                                ShowMessage(_messageBoxIsShown, "Not a Valid IP Address! Try Again");
                            }
                            else
                            {
                                Process p = new Process();
                                p.StartInfo.FileName = "netsh.exe";
                                p.StartInfo.Arguments = String.Format("interface ipv4 set address name=\"{0}\" static {1} {2}", _adapter, ip, mask);
                                p.StartInfo.UseShellExecute = false;
                                p.StartInfo.CreateNoWindow = true;
                                p.StartInfo.RedirectStandardOutput = true;
                                ProcessRequest(p);
                                UpdateAdapterInfo();
                            }
                        }
                        else if (userEntryTxt.Text.Contains("/"))
                        {
                            sep = @"/";
                            string[] customIP = userEntryTxt.Text.Split(sep.ToCharArray());

                            validIP = IPAddress.TryParse(customIP[0], out IPAddress ip);
                            bool validMaskBits = int.TryParse(customIP[1], out int maskBits);

                            if (validMaskBits)
                            {
                                string mask = null;

                                switch (maskBits)
                                {
                                    case 8:
                                        mask = "255.0.0.0";
                                        break;
                                    case 16:
                                        mask = "255.255.0.0";
                                        break;
                                    case 24:
                                        mask = "255.255.255.0";
                                        break;
                                    default:
                                        mask = null;
                                        ShowMessage(_messageBoxIsShown, "Invalid Maskbits! This app only supports '/16' or '/24'");
                                        break;
                                }

                                if (!string.IsNullOrEmpty(mask))
                                {
                                    Process p = new Process();
                                    p.StartInfo.FileName = "netsh.exe";
                                    p.StartInfo.Arguments = String.Format("interface ipv4 set address name=\"{0}\" static {1} {2}", _adapter, ip, mask);
                                    p.StartInfo.UseShellExecute = false;
                                    p.StartInfo.CreateNoWindow = true;
                                    p.StartInfo.RedirectStandardOutput = true;
                                    ProcessRequest(p);
                                    UpdateAdapterInfo();
                                }
                            }
                            else
                            {
                                ShowMessage(_messageBoxIsShown, "Invalid Maskbits! This app only supports '/16' or '/24'");
                            }
                        }
                        else
                        {
                            ShowMessage(_messageBoxIsShown, "Invalid Entry! Try Again");
                        }
                    }

                    catch (IndexOutOfRangeException)
                    {
                        ShowMessage(_messageBoxIsShown, "You must enter an IPAddress followed by a single space, then a Subnet Mask");
                        userEntryTxt.Text = _defaultChoice5Content;
                    }
                    catch (Exception)
                    {
                        ShowMessage(_messageBoxIsShown, "Invalid! Try Again");
                    }
                }
            }
        }

        private void OnKeyDownInMainWindowHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                System.Windows.Application.Current.Shutdown();
            }

        }

        private void ResetUserEntryText()
        {
            userEntryTxt.Text = _defaultChoice5Content;
            userEntryTxt.Foreground = System.Windows.Media.Brushes.Tan;
        }

        private void WifiSelectBtn_Click(object sender, RoutedEventArgs e)
        {
            ResetUserEntryText();
            _adapterType = NetworkInterfaceType.Wireless80211;

            Console.WriteLine($"WifiSelectBtn status: {WifiSelectBtn.IsChecked}");

            Dispatcher.Invoke(() =>
            {
                this.WifiSelectBtn.IsChecked = true;
                this.EthernetSelectBtn.IsChecked = false;
            });

            
            if ((bool)WifiSelectBtn.IsChecked)
            {
                UpdateAdapterInfo();
            }
            
        }

        private void EthernetSelectBtn_Click(object sender, RoutedEventArgs e)
        {  
            ResetUserEntryText();
            _adapterType = NetworkInterfaceType.Ethernet;

            Console.WriteLine($"EthernetSelectBtn status: {EthernetSelectBtn.IsChecked}");

            Dispatcher.Invoke(() =>
            {
                this.WifiSelectBtn.IsChecked = false;
                this.EthernetSelectBtn.IsChecked = true;
            });

            
            if ((bool)EthernetSelectBtn.IsChecked)
            {
                UpdateAdapterInfo();
            }
            
        }
    }
}
