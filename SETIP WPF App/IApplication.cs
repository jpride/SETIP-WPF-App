using System.Diagnostics;

namespace SETIP_WPF_App
{
    public interface IApplication
    {
        Process CreateProcess(string adapter, string ipMaskString);
        void InitializeComponent();
        void ProcessRequest(Process p);
        void ShowMessage(bool messageBoxShown, string msg);
        void UpdateAdapterInfo();
    }
}