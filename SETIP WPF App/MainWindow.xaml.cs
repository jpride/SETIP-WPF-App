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
        private static System.Windows.Forms.Timer timer;
        private static System.Windows.Forms.Timer resultTimer;
        private static System.Windows.Forms.Timer errorReportTimer;

        string adapter;
        int adapterCount = 0;


        readonly string dhcpChoiceContent = "DHCP";
        readonly string choice2Content = "10.10.1.253/16";
        readonly string choice3Content = "192.168.1.253/24";
        readonly string defaultChoice4String = "Custom (x.x.x.x 255.255.x.x)";

        readonly string choice2Address = "10.10.1.253";
        readonly string choice3Address = "192.168.1.253";


        readonly string dhcpChoiceString = "dhcp";
        readonly string choice2String = "static 10.10.1.253 255.255.0.0";
        readonly string choice3String = "static 192.168.1.253 255.255.255.0";
        //readonly string choice4String = "static 169.254.10.253 255.255.0.0";



        

        public MainWindow()
        {
            InitializeComponent();

            //Initialize button content
            Choice1Btn.Content = dhcpChoiceContent;
            Choice2Btn.Content = choice2Content;
            Choice3Btn.Content = choice3Content;

            //intialize adapter info
            UpdateAdapterInfo();

            if (adapterCount == 0)
            {
                //if no active wired adapters found, start a 10 second timer and place message in error box
                timer = new Timer();
                timer.Tick += new EventHandler(NoAdapterTimeOut);
                timer.Interval = 10000;
                timer.Start();
                ErrorReport.Text = "No active adapters found...\nExiting in 10 seconds";
            }
        }
        
        private static void NoAdapterTimeOut(object sender, EventArgs e)
        {
            //upon expiration of timer, shut the app down
            timer.Stop();
            System.Windows.Application.Current.Shutdown();

        }

        private void ResultTimer_Tick(object sender, EventArgs e)
        {
            UpdateAdapterInfo();
        }

        private void ClearErrorReport(object sender, EventArgs e)
        {
            ErrorReport.Text = "";
        }

        /// <summary>
        /// Method to process the IP change request
        /// </summary>
        /// <param name="p"></param>
        public void ProcessRequest(Process p)
        {
            try
            {
                p.Start();
                p.WaitForExit(30000);
                result = p.StandardOutput.ReadToEnd();
                Console.WriteLine(result);
                ErrorReport.Text = result;

                errorReportTimer = new Timer();
                errorReportTimer.Tick += ClearErrorReport;
                errorReportTimer.Interval = 6000;
                errorReportTimer.Start();
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
                            adapter = nic.Name;

                            if (prop.IsDhcpEnabled)
                            {
                                Choice1Btn.IsChecked = true;
                            }

                            adapterCount++;

                            foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                            {
                                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                {
                                    adapterName.Text = String.Format("{0} ({1})", nic.Name, ip.Address.ToString());

                                    if (ip.Address.ToString() == choice2Address)
                                    {
                                        Choice2Btn.IsChecked = true;
                                    }
                                    else if (ip.Address.ToString() == choice3Address)
                                    {
                                        Choice3Btn.IsChecked = true;
                                    }
                                }
                            }
                        }
                    }
                }
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
                Process p = new Process();
                p.StartInfo.FileName = "netsh.exe";
                p.StartInfo.Arguments = String.Format("interface ipv4 set address name=\"{0}\" {1}", adapter, dhcpChoiceString);
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                ProcessRequest(p);

                //timer to account for delay in grabbing IPA afte DHCP is enabled
                adapterName.Text = "working on it...";
                resultTimer = new Timer();
                resultTimer.Tick += ResultTimer_Tick;
                resultTimer.Interval = 3000;
                resultTimer.Start();
            }
            else if ((bool)Choice2Btn.IsChecked)
            {
                Process p = new Process();
                p.StartInfo.FileName = "netsh.exe";
                p.StartInfo.Arguments = String.Format("interface ipv4 set address name=\"{0}\" {1}", adapter, choice2String);
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
                p.StartInfo.Arguments = String.Format("interface ipv4 set address name=\"{0}\" {1}", adapter, choice3String);
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

                
                try
                {
                    //split user input string into ipa and ipm
                    string[] customIP = userEntryTxt.Text.Split(sep.ToCharArray());

                 
                    bool validIP = IPAddress.TryParse(customIP[0], out IPAddress ip);
                    bool validMask = IPAddress.TryParse(customIP[1], out IPAddress mask);


                    if (!validIP || !validMask)
                    {
                        ErrorReport.Text = "Not a valid IP Addresss! Try Again";
                    }
                    else
                    {
                        Process p = new Process();
                        p.StartInfo.FileName = "netsh.exe";
                        p.StartInfo.Arguments = String.Format("interface ipv4 set address name=\"{0}\" static {1} {2}", adapter, ip, mask);
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.RedirectStandardOutput = true;
                        ProcessRequest(p);
                        UpdateAdapterInfo();
                    }
                }

                catch (IndexOutOfRangeException)
                {
                    ErrorReport.Text = "You must enter an IPAddress followed by a single space, then a Subnet Mask";
                    userEntryTxt.Text = defaultChoice4String;
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
            userEntryTxt.Text = defaultChoice4String;
        }

        private void Choice2Btn_Click(object sender, RoutedEventArgs e)
        {
            userEntryTxt.Text = defaultChoice4String;
        }

        private void Choice3Btn_Click(object sender, RoutedEventArgs e)
        {
            userEntryTxt.Text = defaultChoice4String;
        }
    }
}
