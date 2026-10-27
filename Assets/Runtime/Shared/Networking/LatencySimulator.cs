using PurrNet;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Networking
{
    /// <summary>Applies optional latency and packet-loss simulation to the client transport.</summary>
    public class LatencySimulator : MonoBehaviour
    {
        [Header("Latency Simulation (DEBUG builds only)")]
        [SerializeField, FormerlySerializedAs("simulateLatency")] private bool _simulateLatency = false;
        [SerializeField, FormerlySerializedAs("minLatencyMs")] private int _minLatencyMS = 50;
        [SerializeField, FormerlySerializedAs("maxLatencyMs")] private int _maxLatencyMS = 150;

        [Header("Packet Loss Simulation (DEBUG builds only)")]
        [SerializeField, FormerlySerializedAs("simulatePacketLoss")] private bool _simulatePacketLoss = false;
        [SerializeField, Range(1, 100), FormerlySerializedAs("packetLossChance")] private int _packetLossChance = 5;

        private void Start()
        {
            if (Game.Shared.Runtime.Runtime.IsServer()) return;

            var networkManager = NetworkManager.main;
            if (!networkManager) return;

            var udpTransport = networkManager.GetComponent<UDPTransport>();
            if (!udpTransport) return;

            var simulation = new NetworkSimulation
            {
                includeInBuild = Debug.isDebugBuild,
                simulateLatency = _simulateLatency,
                minLatency = _minLatencyMS,
                maxLatency = _maxLatencyMS,
                simulatePacketLoss = _simulatePacketLoss,
                packetLossChance = _packetLossChance
            };
            udpTransport.networkSimulation = simulation;
        }

        private void OnValidate()
        {
            _minLatencyMS = Mathf.Max(0, _minLatencyMS);
            _maxLatencyMS = Mathf.Max(_minLatencyMS, _maxLatencyMS);
            _packetLossChance = Mathf.Clamp(_packetLossChance, 1, 100);
        }
    }
}
