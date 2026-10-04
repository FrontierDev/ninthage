using UnityEngine;
using PurrNet;
using PurrNet.Transports;
using System.Reflection;
using LiteNetLib;

namespace Game.Shared
{
    public class LatencySimulator : MonoBehaviour
    {
        [Header("Latency Simulation (DEBUG builds only)")]
        [SerializeField] private bool simulateLatency = false;
        [SerializeField] private int minLatencyMs = 50;
        [SerializeField] private int maxLatencyMs = 150;

        [Header("Packet Loss Simulation (DEBUG builds only)")]
        [SerializeField] private bool simulatePacketLoss = false;
        [SerializeField, Range(0, 100)] private int packetLossChance = 5;

        private void Start()
        {
            if (Runtime.IsServer()) return;

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

            netManager.SimulateLatency = simulateLatency;
            netManager.SimulationMinLatency = minLatencyMs;
            netManager.SimulationMaxLatency = maxLatencyMs;
            netManager.SimulatePacketLoss = simulatePacketLoss;
            netManager.SimulationPacketLossChance = packetLossChance;
        }
    }
}