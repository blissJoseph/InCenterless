using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using InCenterless.Services;

using System.Threading.Tasks;
using System.Windows.Input;
using InCenterless.Helpers;
using Opc.Ua.Client;
using Opc.Ua;

namespace InCenterless.ViewModels._1.Home
{
    public class MachineConditionViewModel : INotifyPropertyChanged
    {
        private const string NodeId = "ns=2;s=/Channel/Parameter/R";

        private string _readValue;
        public string ReadValue
        {
            get => _readValue;
            set
            {
                if (_readValue != value)
                {
                    _readValue = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _writeValue;
        public string WriteValue
        {
            get => _writeValue;
            set
            {
                if (_writeValue != value)
                {
                    _writeValue = value;
                    OnPropertyChanged();
                    _ = WriteToServerAsync(value); // 값 변경 시 서버에 자동 쓰기
                }
            }
        }

        public MachineConditionViewModel()
        {
            InitializeAsync();
        }

        /// <summary>
        /// OPC UA 연결 및 구독 초기화
        /// </summary>
        private async void InitializeAsync()
        {
            var client = OpcUaClientService.Instance;

            // OPC 서버 연결
            bool connected = await client.ConnectAsync();
            if (!connected)
            {
                ReadValue = "서버 연결 실패";
                return;
            }

            // 서버로부터 값 구독 (10ms 단위)
            client.SubscribeToNode(NodeId, OnDataChanged, 10);
        }


        /// <summary>
        /// 서버 값 변경 시 호출되는 콜백
        /// </summary>
        private void OnDataChanged(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            var notification = e.NotificationValue as MonitoredItemNotification;
            if (notification?.Value?.Value != null)
            {
                // 읽은 값을 ViewModel 속성에 반영
                ReadValue = notification.Value.Value.ToString();
            }
        }

        /// <summary>
        /// 서버에 값 쓰기 (텍스트 값)
        /// </summary>
        private async Task WriteToServerAsync(string value)
        {
            var client = OpcUaClientService.Instance;

            if (!client.IsConnected) return;

            bool success = await client.WriteNodeValueAsync(NodeId, value);

            if (!success)
            {
                // 쓰기 실패 시 표시 (선택 사항)
                ReadValue = "쓰기 실패";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


    }

}
