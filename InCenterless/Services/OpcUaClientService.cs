using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace InCenterless.Services
{
    // 싱글톤 OPC UA 클라이언트 서비스
    public class OpcUaClientService
    {
        private static OpcUaClientService _instance;
        private static readonly object _lock = new object();
        private Session _session;
        private Subscription _subscription;

        private OpcUaClientService() { }

        // 싱글톤 인스턴스
        public static OpcUaClientService Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new OpcUaClientService();
                }
            }
        }

        /// <summary>
        /// OPC UA 서버에 비동기 연결
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            if (_session != null && _session.Connected)
                return true;

            // 클라이언트 설정
            var config = new ApplicationConfiguration
            {
                ApplicationName = "CenterlessClient",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "CertificateStores/MachineDefault",
                        SubjectName = "CenterlessClient"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "CertificateStores/UA Applications"
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "CertificateStores/UA Certificate Authorities"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "CertificateStores/RejectedCertificates"
                    },
                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = true
                },
                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 15000
                },
                ClientConfiguration = new ClientConfiguration
                {
                    DefaultSessionTimeout = 60000
                },
                DisableHiResClock = false
            };

            await config.Validate(ApplicationType.Client);

            // 인증서 자동 수락
            if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += (_, e) =>
                {
                    e.Accept = true;
                };
            }

            // 인증서 생성 또는 확인
            var app = new ApplicationInstance
            {
                ApplicationName = "CenterlessClient",
                ApplicationConfiguration = config
            };

            await app.CheckApplicationInstanceCertificate(true, 0);

            // 엔드포인트 설정 (보안 사용)
            string endpointUrl = "opc.tcp://192.168.214.1:4840";
            var endpoint = CoreClientUtils.SelectEndpoint(endpointUrl, true);
            var endpointConfig = EndpointConfiguration.Create(config);
            var selectedEndpoint = new ConfiguredEndpoint(null, endpoint, endpointConfig);

            // 사용자 로그인 정보 (Secure)
            var identity = new UserIdentity("TEST", "testtest");

            // 세션 생성
            _session = await Session.Create(
                config,
                selectedEndpoint,
                false,
                "CenterlessClient",
                60000,
                identity,
                null
            );

            return _session.Connected;
        }

        /// <summary>
        /// 노드 ID로부터 현재 값을 읽음
        /// </summary>
        public async Task<string> ReadNodeValueAsync(string nodeId)
        {
            if (_session == null || !_session.Connected)
                throw new InvalidOperationException("OPC UA session is not connected.");

            var value = await Task.Run(() => _session.ReadValue(nodeId));
            return value?.ToString();
        }

        /// <summary>
        /// 노드에 값 쓰기
        /// </summary>
        public async Task<bool> WriteNodeValueAsync(string nodeId, object value)
        {
            if (_session == null || !_session.Connected)
                throw new InvalidOperationException("OPC UA session is not connected.");

            return await Task.Run(() =>
            {
                var nodeToWrite = new WriteValue
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value,
                    Value = new DataValue(new Variant(value))
                };

                // ✅ out 변수 선언
                StatusCodeCollection statusCodes;
                DiagnosticInfoCollection diagnosticInfos;

                // ✅ 동기 Write 호출 (Task.Run 안에서)
                _session.Write(
                    null,
                    new WriteValueCollection { nodeToWrite },
                    out statusCodes,
                    out diagnosticInfos
                );

                return StatusCode.IsGood(statusCodes[0]);
            });
        }

        /// <summary>
        /// 노드 변경을 구독 (샘플링 간격 단위: ms)
        /// </summary>
        public void SubscribeToNode(string nodeId, MonitoredItemNotificationEventHandler handler, int samplingInterval = 1000)
        {
            if (_session == null || !_session.Connected)
                throw new InvalidOperationException("OPC UA session is not connected.");

            // 기존 구독 해제
            _subscription?.Delete(true);
            _subscription = new Subscription(_session.DefaultSubscription)
            {
                PublishingInterval = samplingInterval
            };
            _session.AddSubscription(_subscription);
            _subscription.Create();

            // 감시 항목 생성
            var item = new MonitoredItem(_subscription.DefaultItem)
            {
                DisplayName = "SubscribedNode",
                StartNodeId = nodeId,
                SamplingInterval = samplingInterval
            };
            item.Notification += handler;

            _subscription.AddItem(item);
            _subscription.ApplyChanges();
        }

        /// <summary>
        /// 현재 연결 여부
        /// </summary>
        public bool IsConnected => _session != null && _session.Connected;

        /// <summary>
        /// 세션 종료
        /// </summary>
        public void Disconnect()
        {
            _session?.Close();
            _session?.Dispose();
            _session = null;
            _subscription = null;
        }
    }
}
