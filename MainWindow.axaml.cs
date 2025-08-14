using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Layout;

namespace AvaloniaChat
{
    public partial class MainWindow : Window
    {
        private IChatHost? _host;
        private readonly IChatService _chatService;
        private readonly ISecureChannel _secureChannel;
        private IChatClient? _client;

        public MainWindow(IChatService chatService, ISecureChannel secureChannel)
        {
            _chatService = chatService;
            _secureChannel = secureChannel;

            InitializeComponent();

            MessagesList.Items.Clear();
            HostRadio.Checked += (_, __) => IpInput.IsEnabled = false;
            ClientRadio.Checked += (_, __) => IpInput.IsEnabled = true;

            StartButton.Click += async (_, __) => await StartChatAsync();
            SendButton.Click += async (_, __) => await SendMessageAsync();
            DisconnectButton.Click += DisconnectButton_Click;

            SendButton.IsEnabled = false;
            DisconnectButton.IsEnabled = false;

            MessageInput.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    _ = SendMessageAsync();
                    e.Handled = true;
                }
            };

            Dispatcher.UIThread.Post(() => MessageInput.Focus());
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            MyIpText.Text = $"My IP: {NetworkHelper.GetLocalIpAddress()}";
        }

        private void DisconnectButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                if (_host != null)
                {
                    _host.StopHostAsync();
                    _host = null;
                    UpdateStatus("🔌 Host stopped.");
                }
                else if (_client != null)
                {
                    _client.Disconnect();
                    _client = null;
                    UpdateStatus("🔌 Client disconnected.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Error while disconnecting: {ex.Message}");
            }

            StartButton.IsEnabled = true;
            SendButton.IsEnabled = false;
            DisconnectButton.IsEnabled = false;
        }

        private void AddMessage(string text, bool isOwn)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var bubble = new Border
                {
                    Background = isOwn
                        ? new SolidColorBrush(Color.Parse("#0A84FF"))
                        : new SolidColorBrush(Color.Parse("#2D2D2D")),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8),
                    Margin = new Thickness(4),
                    MaxWidth = 300,
                    Child = new TextBlock
                    {
                        Text = text,
                        Foreground = Brushes.White,
                        TextWrapping = TextWrapping.Wrap
                    },
                    HorizontalAlignment = isOwn
                        ? HorizontalAlignment.Right
                        : HorizontalAlignment.Left
                };

                MessagesList.Items.Add(bubble);

                MessagesList.ScrollIntoView(MessagesList.ItemCount - 1);
            });
        }

        private async Task StartChatAsync()
        {
            StartButton.IsEnabled = false;
            SendButton.IsEnabled = false;
            DisconnectButton.IsEnabled = true;
            MessagesList.Items.Clear();

            if (!int.TryParse(PortInput.Text, out int port))
            {
                UpdateStatus("❌ Invalid port");
                StartButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                return;
            }

            if (HostRadio.IsChecked == true)
            {
                _host = new ChatHost(_chatService, _secureChannel);
                _host.OnStatusChanged += UpdateStatus;
                _host.OnMessageReceived += msg => AddMessage(msg, false);
                _host.OnClientConnected += () =>
                    Dispatcher.UIThread.Post(() => SendButton.IsEnabled = true);

                await _host.StartHostAsync(port);
            }
            else
            {
                if (!IPAddress.TryParse(IpInput.Text, out _))
                {
                    UpdateStatus("❌ Invalid IP address");
                    StartButton.IsEnabled = true;
                    DisconnectButton.IsEnabled = false;
                    return;
                }

                _client = new ChatClient(_chatService, _secureChannel);
                _client.OnStatusChanged += UpdateStatus;
                _client.OnMessageReceived += msg => AddMessage(msg, false);

                bool connected = await _client.ConnectToHostAsync(IpInput.Text, port, 10);
                if (!connected)
                {
                    UpdateStatus("❌ Failed to connect");
                    StartButton.IsEnabled = true;
                    DisconnectButton.IsEnabled = false;
                    return;
                }
                SendButton.IsEnabled = true;
            }
        }

        private async Task SendMessageAsync()
        {
            string message = MessageInput.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                if (_host != null)
                    await _host.SendMessageAsync(message);
                else if (_client != null)
                    await _client.SendMessageAsync(message);

                AddMessage(message, true);
                MessageInput.Text = "";
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Sending error: {ex.Message}");
            }
        }

        private void UpdateStatus(string status) =>
            Dispatcher.UIThread.Post(() => StatusText.Text = status);
    }
}