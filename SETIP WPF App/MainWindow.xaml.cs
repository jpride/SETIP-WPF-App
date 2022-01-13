using System;
using System.Windows;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Windows.Forms;


namespace SETIP_WPF_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string result;
        private static System.Windows.Forms.Timer _timer;
        private static System.Windows.Forms.Timer _resultTimer;
        private static System.Windows.Forms.Timer _errorReportTimer;

        private string _adapter;
        private int _adapterCount = 0;

        readonly string _dhcpChoiceContent = "DHCP";
        readonly string _choice2Content = "10.10.1.253/16";
        readonly string _choice3Content = "192.168.1.253/24";
        readonly string _defaultChoice4String = "Custom (x.x.x.x 255.255.x.x)";

        readonly string _choice2Address = "10.10.1.253";
        readonly string _choice3Address = "192.168.1.253";

        readonly string _dhcpChoiceString = "dhcp";
        readonly string _choice2String = "static 10.10.1.253 255.255.0.0";
        readonly string _choice3String = "static 192.168.1.253 255.255.255.0";


        public MainWindow()
        {
            InitializeComponent();

            //Initialize button content
            Choice1Btn.Content = _dhcpChoiceContent;
            Choice2Btn.Content = _choice2Content;
            Choice3Btn.Content = _choice3Content;
            userEntryTxt.Text = _defaultChoice4String;

            //intialize adapter info
            UpdateAdapterInfo();

        }

        private void AdapterSearchTimer()
        {
            //if no active wired adapters found, start a 5 second timer and place message in error box
            //after 5 seconds, run UpdateAdapterInfo() again
            _timer = new Timer();
            _timer.Tick += new EventHandler(AdapterTimeOut);
            _timer.Interval = 5000;
            _timer.Start();
            ErrorReport.Text = "No active adapters found...\nWaiting for active adapter";
            adapterLabel.Content = "";
        }

        private void AdapterTimeOut(object sender, EventArgs e)
        {
            //upon expiration of timer, Rerun UpdateAdapterInfo()
            _timer.Stop();
            UpdateAdapterInfo();
        }

        private void ResultTimer_Tick(object sender, EventArgs e)
        {
            ExitBtn.IsEnabled = true;
            UpdateAdapterInfo();
        }
      
        private void ClearErrorReport(object sender, EventArgs e)
        {
            ErrorReport.Text = "";
        }


        public void ProcessRequest(Process p)
        {
            try
            {
                p.Start();
                p.WaitForExit(30000);
                result = p.StandardOutput.ReadToEnd();
                Console.WriteLine(result);
                ErrorReport.Text = result;

                _errorReportTimer = new Timer();
                _errorReportTimer.Tick += ClearErrorReport;
                _errorReportTimer.Interval = 6000;
                _errorReportTimer.Start();
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
        }

        public void UpdateAdapterInfo()
        {
            //This is not as elegant as i would like, but it works.
            //This method reuses code from the MainWindow class above
            //This method is called after an ip address change is processed

       

            //grab list of all NetworkInterfaces
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            IPGlobalProperties props = IPGlobalProperties.GetIPGlobalProperties();

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
                            adapterName.Text = nic.Name;
                            _adapter = nic.Name;

                            if (prop.IsDhcpEnabled)
                            {
                                Choice1Btn.IsChecked = true;
                            }

                            _adapterCount++;

                            foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                            {
                                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                {
                                    adapterName.Text = String.Format("\"{0}\" || {1} [{2}]", nic.Name,Dns.GetHostName(), ip.Address.ToString());
                                    ErrorReport.Text = String.Empty;


                                    if (ip.Address.ToString() == _choice2Address)
                                    {
                                        Choice2Btn.IsChecked = true;
                                    }
                                    else if (ip.Address.ToString() == _choice3Address)
                                    {
                                        Choice3Btn.IsChecked = true;
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
                _timer = new Timer();
                _timer.Tick += new EventHandler(AdapterTimeOut);
                _timer.Interval = 5000;
                _timer.Start();
                ErrorReport.Text = "No active adapters found...\nWaiting for active adapter";
                adapterLabel.Content = "";
            }
            
        }


        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void SetIPBtn_Click(object sender, RoutedEventArgs e)
        {
            
            if ((bool)Choice1Btn.IsChecked)
            {
                ExitBtn.IsEnabled = false; //resolved in resultTimer.Tick eventhandler
                Process p = new Process();
                p.StartInfo.FileName = "netsh.exe";
                p.StartInfo.Arguments = String.Format("interface ipv4 set address name=\"{0}\" {1}", _adapter, _dhcpChoiceString);
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                ProcessRequest(p);

                //timer to account for delay in grabbing IPA afte DHCP is enabled
                adapterName.Text = "waiting for DHCP...";
                _resultTimer = new Timer();
                _resultTimer.Tick += ResultTimer_Tick;
                _resultTimer.Interval = 2500;
                _resultTimer.Start();
            }
            else if ((bool)Choice2Btn.IsChecked)
            {
                Process p = new Process();
                p.StartInfo.FileName = "netsh.exe";
                p.StartInfo.Arguments = String.Format("interface ipv4 set address name=\"{0}\" {1}", _adapter, _choice2String);
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                ProcessRequest(p);
                UpdateAdapterInfo();
            }
            else if ((bool)Choice3Btn.IsChecked)
            {
                Process p = new Process();
                p.StartInfo.FileName = "netsh.exe";
                p.StartInfo.Arguments = String.Format("interface ipv4 set address name=\"{0}\" {1}", _adapter, _choice3String);
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                ProcessRequest(p);
                UpdateAdapterInfo();
            }
            else if ((bool)Choice4Btn.IsChecked)
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
                            ErrorReport.Text = "Not a valid IP Addresss! Try Again";
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
                                    ErrorReport.Text = "Invalid Maskbits! This app only supports '/16' or '/24'";
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
                            ErrorReport.Text = "Invalid Maskbits! This app only supports '/16' or '/24'";
                        }
                    }
                    else 
                    {
                        ErrorReport.Text = "Invalid Entry! Try Again";
                    }
                }

                catch (IndexOutOfRangeException)
                {
                    ErrorReport.Text = "You must enter an IPAddress followed by a single space, then a Subnet Mask";
                    userEntryTxt.Text = _defaultChoice4String;
                }
                catch (Exception)
                {
                    ErrorReport.Text = "Invalid! Try Again";
                }
            }

            
        }

        private void userEntryTxt_GotFocus(object sender, RoutedEventArgs e)
        {
            userEntryTxt.Text = "";
        }

        private void Choice1Btn_Click(object sender, RoutedEventArgs e)
        {
            userEntryTxt.Text = _defaultChoice4String;
        }

        private void Choice2Btn_Click(object sender, RoutedEventArgs e)
        {
            userEntryTxt.Text = _defaultChoice4String;
        }

        private void Choice3Btn_Click(object sender, RoutedEventArgs e)
        {
            userEntryTxt.Text = _defaultChoice4String;
        }
    }
}
