using UnityEngine;
using UnityEngine.AI;

namespace Game.Server
{
    /// <summary>Prototype component that moves a test NPC toward the test destination.</summary>
    public class Test_NPCPathfinder : MonoBehaviour
    {
        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag("Test_Destination");
            GetComponent<NavMeshAgent>().SetDestination(go.transform.position);
        }
    }
}
