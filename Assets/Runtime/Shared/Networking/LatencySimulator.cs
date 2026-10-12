using LiteNetLib;
using PurrNet;
using PurrNet.Transports;
using System.Reflection;
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
        [SerializeField, Range(0, 100), FormerlySerializedAs("packetLossChance")] private int _packetLossChance = 5;

        private void Start()
        {
            if (Game.Shared.Runtime.Runtime.IsServer()) return;

            var networkManager = NetworkManager.main;
            if (!networkManager) return;

            var udpTransport = networkManager.GetComponent<UDPTransport>();
            if (!udpTransport) return;

            // The NetManager fields are private, so we use reflection
            ApplyToNetManager(udpTransport, "_client");
            ApplyToNetManager(udpTransport, "_server");
        }

        private void ApplyToNetManager(UDPTransport transport, string fieldName)
        {
            var field = typeof(UDPTransport).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(transport) is not LiteNetLib.NetManager netManager) return;

            netManager.SimulateLatency = _simulateLatency;
            netManager.SimulationMinLatency = _minLatencyMS;
            netManager.SimulationMaxLatency = _maxLatencyMS;
            netManager.SimulatePacketLoss = _simulatePacketLoss;
            netManager.SimulationPacketLossChance = _packetLossChance;
        }
    }
}
