using System;
using System.Configuration;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Sygnia.Wcf.Gateway.Contracts;

namespace Sygnia.WpfClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void GetBalanceButton_Click(object sender, RoutedEventArgs e)
        {
            var accountId = AccountIdTextBox.Text.Trim();
            if (string.IsNullOrEmpty(accountId))
            {
                ShowResult("Enter an account ID.", isError: true);
                return;
            }

            var gatewayAddress = ConfigurationManager.AppSettings["GatewayAddress"] ?? "net.tcp://localhost:8090/BalanceService";
            var factory = new ChannelFactory<IBalanceService>(
                new NetTcpBinding(SecurityMode.None),
                new EndpointAddress(gatewayAddress));

            GetBalanceButton.IsEnabled = false;
            ShowResult("Working...", isError: false);

            try
            {
                var proxy = factory.CreateChannel();
                // ChannelFactory<IBalanceService>.CreateChannel() implements the interface
                // synchronously (no auto-generated async proxy from an interface-based
                // ChannelFactory), so the blocking round trip is pushed off the UI thread here.
                var result = await Task.Run(() => proxy.GetBalance(accountId));
                ShowResult($"{result.AccountId}: {result.Balance}", isError: false);
            }
            catch (FaultException<BalanceFault> fault)
            {
                ShowResult(fault.Detail.Message, isError: true);
            }
            catch (TimeoutException)
            {
                ShowResult("The gateway did not respond in time.", isError: true);
            }
            catch (CommunicationException ex)
            {
                ShowResult($"Could not reach the gateway: {ex.Message}", isError: true);
            }
            catch (Exception ex)
            {
                ShowResult(ex.Message, isError: true);
            }
            finally
            {
                factory.Abort();
                GetBalanceButton.IsEnabled = true;
            }
        }

        private void ShowResult(string text, bool isError)
        {
            ResultTextBlock.Text = text;
            ResultTextBlock.Foreground = isError ? Brushes.Red : Brushes.Black;
        }
    }
}
