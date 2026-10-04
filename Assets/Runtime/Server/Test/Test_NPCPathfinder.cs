using UnityEngine;
using UnityEngine.AI;

namespace Game.Server
{
    public class Test_NPCPathfinder : MonoBehaviour
    {
        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag("Test_Destination");
            GetComponent<NavMeshAgent>().SetDestination(go.transform.position);
        }
    }
}