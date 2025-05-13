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

        private OpcUaClientService() { }

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
        /// OPC UA 서버에 비동기로 연결
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            if (_session != null && _session.Connected)
                return true;

            var config = new ApplicationConfiguration
            {
                ApplicationName = "CenterlessClient",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "CertificateStores/MachineDefault", // 클라이언트 인증서 저장
                        SubjectName = "CenterlessClient"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "CertificateStores/UA Applications"  // 서버 인증서 저장
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "CertificateStores/UA Certificate Authorities"  // 서버 인증서 발급자
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

            // 인증서 자동 수락 처리
            if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += (_, e) =>
                {
                    e.Accept = true;
                };
            }

            // 애플리케이션 인증서 유효성 검사 및 생성
            var app = new ApplicationInstance
            {
                ApplicationName = "CenterlessClient",
                ApplicationConfiguration = config
            };

            await app.CheckApplicationInstanceCertificate(true, 0);

            // 서버 엔드포인트 지정
            string endpointUrl = "opc.tcp://192.168.214.1:4840";
            var endpoint = CoreClientUtils.SelectEndpoint(endpointUrl, true); // 보안 강제
            var endpointConfig = EndpointConfiguration.Create(config);
            var selectedEndpoint = new ConfiguredEndpoint(null, endpoint, endpointConfig);

            // 사용자 로그인 정보
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
        /// 노드 ID 기반 값 읽기
        /// </summary>
        public async Task<string> ReadNodeValueAsync(string nodeId)
        {
            if (_session == null || !_session.Connected)
                throw new InvalidOperationException("OPC UA session is not connected.");

            var value = await Task.Run(() => _session.ReadValue(nodeId));
            return value?.ToString();
        }

        /// <summary>
        /// 세션 연결 상태 반환
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
        }
    }
}
