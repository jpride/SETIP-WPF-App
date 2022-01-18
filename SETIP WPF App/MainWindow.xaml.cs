using System;
using System.Windows;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Windows.Forms;
using System.Windows.Input;
using System.Threading;



namespace SETIP_WPF_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static System.Windows.Forms.Timer _timer;
        private static System.Windows.Forms.Timer _dhcpTimer;

        private NetworkInterface _nic;
        private string _adapter;
        private int _adapterCount = 0;

        private readonly string _appTitle = "IP Address Configurator";
        public bool _messageBoxIsShown = false; //tracks state of message box, in order to stop them from stacking indefinetely

        readonly string _dhcpChoiceContent = "DHCP";
        readonly string _choice2Content = "10.10.1.253/16";
        readonly string _choice3Content = "192.168.1.253/24";
        readonly string _choice4Content = "169.254.1.253/16";
        readonly string _defaultChoice5Content = "Custom (x.x.x.x 255.255.x.x)";

        readonly string _choice2Address = "10.10.1.253";
        readonly string _choice3Address = "192.168.1.253";
        readonly string _choice4Address = "169.254.1.253";

        readonly string _dhcpNetShChoiceString = "dhcp";
        readonly string _choice2NetShString = "static address=10.10.1.253 mask=255.255.0.0 gateway=10.10.1.1";
        readonly string _choice3NetShString = "static address=192.168.1.253 mask=255.255.255.0 gateway=192.168.1.1";
        readonly string _choice4NetShString = "static address=169.254.1.253 mask=255.255.0.0 gateway=169.254.1.1";


        public MainWindow()
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
            Console.WriteLine("AddressChangedDetected");
            if (_nic.OperationalStatus == OperationalStatus.Up)
            {
                Console.WriteLine("_nic: {0}", _nic.Name);
                try
                {
                    UpdateAdapterInfo();
                }
                catch (Exception ex)
                {
                    Console.Write("Error in Callback: {0}", ex.Message);
                }
            }
            else
            {
                adapterName.Text = "waiting for active adapter...";
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
            var props = IPGlobalProperties.GetIPGlobalProperties();

            //loop through list and find interface that is both "Up" and contains the word 'Ethernet' in it
            foreach (NetworkInterface nic in interfaces)
            {
                IPInterfaceProperties adapterProps = nic.GetIPProperties();
                IPv4InterfaceProperties prop = adapterProps.GetIPv4Properties();

                if (nic.OperationalStatus == OperationalStatus.Up & nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    if (nic.Name.Contains("Ethernet"))
                    {
                        if (!nic.Name.Contains("vEthernet") & !nic.Name.Contains("Loopback") & !nic.Name.Contains("Bluetooth"))
                        {
                            //once a valid adapter is found, places the name in the adapterName box and sets the adapter variable used in the processes to the name
                            _nic = nic;
                            _adapter = nic.Name;
                            

                            this.Dispatcher.Invoke(() =>
                                {
                                    adapterName.Text = _adapter;
                                });

                            if (prop.IsDhcpEnabled)
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    Choice1Btn.IsChecked = true;
                                });

                                if (_dhcpTimer != null)
                                {
                                    _dhcpTimer.Stop();
                                }   
                            }

                            _adapterCount++;

                            foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                            {
                                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                {
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        adapterName.Text = String.Format("\"{0}\" || {1} [{2}]", nic.Name, Dns.GetHostName(), ip.Address.ToString());
                                    });

                                    if (ip.Address.ToString() == _choice2Address)
                                    {
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            Choice2Btn.IsChecked = true;
                                        });
                                    }
                                    else if (ip.Address.ToString() == _choice3Address)
                                    {
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            Choice3Btn.IsChecked = true;
                                        });
                                    }
                                    else if (ip.Address.ToString() == _choice4Address)
                                    {
                                        this.Dispatcher.Invoke(() =>
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

            if (_adapterCount == 0)
            {
                //if no active wired adapters found, start a 5 second timer and place message in error box
                //after 5 seconds, run UpdateAdapterInfo() again
                _timer = new System.Windows.Forms.Timer();
                _timer.Tick += new EventHandler(AdapterTimeOut);
                _timer.Interval = 5000;
                _timer.Start();
                adapterName.Text = "No active adapters found...Waiting for active adapter";
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
        private void userEntryTxt_GotFocus(object sender, RoutedEventArgs e)
        {
            userEntryTxt.Text = "";
        }

        private void Choice1Btn_Click(object sender, RoutedEventArgs e)
        {
            userEntryTxt.Text = _defaultChoice5Content;

            if ((bool)Choice1Btn.IsChecked)
            {
                Process p = CreateProcess(_adapter, _dhcpNetShChoiceString);
                ProcessRequest(p);

                //timer to account for delay in grabbing IPA afte DHCP is enabled
                adapterName.Text = "waiting for DHCP...";
                //_dhcpTimer = new System.Windows.Forms.Timer();
                //_dhcpTimer.Tick += ResultTimer_Tick;
                //_dhcpTimer.Interval = 3000;
                //_dhcpTimer.Start();
            }
        }

        private void Choice2Btn_Click(object sender, RoutedEventArgs e)
        {
            userEntryTxt.Text = _defaultChoice5Content;

            if ((bool)Choice2Btn.IsChecked)
            {
                Process p = CreateProcess(_adapter, _choice2NetShString);
                ProcessRequest(p);
                UpdateAdapterInfo();
            }

        }

        private void Choice3Btn_Click(object sender, RoutedEventArgs e)
        {
            userEntryTxt.Text = _defaultChoice5Content;

            if ((bool)Choice3Btn.IsChecked)
            {
                Process p = CreateProcess(_adapter, _choice3NetShString);
                ProcessRequest(p);
                UpdateAdapterInfo();
            }
        }

        private void Choice4Btn_Click(object sender, RoutedEventArgs e)
        {
            userEntryTxt.Text = _defaultChoice5Content;

            if ((bool)Choice4Btn.IsChecked)
            {
                Process p = CreateProcess(_adapter, _choice4NetShString);
                ProcessRequest(p);
                UpdateAdapterInfo();
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
    }
}
