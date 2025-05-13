using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using InCenterless.Services;

namespace InCenterless.ViewModels._1.Home
{
    public class MachineConditionViewModel : INotifyPropertyChanged
    {
        private string _conditionValue;

        // 바인딩될 속성
        public string ConditionValue
        {
            get => _conditionValue;
            set { _conditionValue = value; OnPropertyChanged(); }
        }

        // 생성자에서 데이터 로드 시작
        public MachineConditionViewModel()
        {
            LoadDataAsync();
        }

        // OPC UA에서 데이터 받아오기 (비동기)
        private async Task LoadDataAsync()
        {
            try
            {
                var client = OpcUaClientService.Instance;

                // 연결 시도
                bool connected = await client.ConnectAsync();

                if (connected)
                {
                    // 정상 연결 후 데이터 요청
                    ConditionValue = await client.ReadNodeValueAsync("ns=2;s=/Channel/Parameter/R");
                }
                else
                {
                    // 연결 실패 시 사용자에게 안내
                    ConditionValue = "🔌 서버 연결 실패 (Check IP / 인증서 / 계정)";
                }
            }
            catch (Exception ex)
            {
                // 예외 발생 시 예외 메시지를 함께 출력
                ConditionValue = $"❗ 오류 발생: {ex.Message}";
            }
        }

        // MVVM 바인딩용 이벤트 처리
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
