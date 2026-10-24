using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Editor.Inspectors
{
    /// <summary>Displays read-only runtime targeting and NavMesh state for an NPC actor.</summary>
    [CustomEditor(typeof(NPCActor))]
    public sealed class NPCActorInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Runtime movement and target data is available in Play Mode.",
                    MessageType.Info);
                return;
            }

            var npcActor = target as NPCActor;
            if (npcActor == null)
            {
                EditorGUILayout.HelpBox("The selected NPCActor is no longer available.", MessageType.Info);
                return;
            }

            DrawTargetingSection(npcActor);
            DrawBehaviourSection(npcActor);
            DrawNavMeshSection(npcActor);

            Repaint();
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        private static void DrawTargetingSection(NPCActor npcActor)
        {
            EditorGUILayout.LabelField("Targeting", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField("Actor GUID", npcActor.ID.ToString());
                EditorGUILayout.LabelField("Actor Name", npcActor.GetName());
                EditorGUILayout.LabelField("Owner", FormatOwner(npcActor));
                EditorGUILayout.LabelField("Current Chunk", npcActor.CurrentChunk ?? "(null)");
                EditorGUILayout.LabelField("Spatial Context", FormatSpatialContext(npcActor.SpatialContext));
                EditorGUILayout.Toggle("isDead", npcActor.isDead);
                EditorGUILayout.ObjectField("Target", npcActor.Target, typeof(Actor), true);

                var targetActor = npcActor.Target;
                if (targetActor == null)
                {
                    EditorGUILayout.LabelField("Target State", "(none)");
                    return;
                }

                EditorGUILayout.LabelField("Target GUID", targetActor.ID.ToString());
                EditorGUILayout.LabelField("Target Name", targetActor.GetName());
                EditorGUILayout.Toggle("Target isDead", targetActor.isDead);
                EditorGUILayout.LabelField("Target Current Chunk", targetActor.CurrentChunk ?? "(null)");
                EditorGUILayout.LabelField(
                    "Target Spatial Context",
                    FormatSpatialContext(targetActor.SpatialContext));
                EditorGUILayout.Toggle(
                    "Contexts Compatible",
                    npcActor.SpatialContext.IsCompatibleWith(targetActor.SpatialContext));

                Vector3 actorPosition = npcActor.transform.position;
                Vector3 targetPosition = targetActor.transform.position;
                EditorGUILayout.Vector3Field("NPC Transform Position", actorPosition);
                EditorGUILayout.Vector3Field("Target Transform Position", targetPosition);
                EditorGUILayout.LabelField(
                    "Centre Distance (local/physical debug)",
                    Vector3.Distance(actorPosition, targetPosition).ToString("F3"));

                try
                {
                    EditorGUILayout.LabelField(
                        "Actor.DistanceTo Surface (local/physical debug)",
                        npcActor.DistanceTo(targetActor).ToString("F3"));
                }
                catch (Exception exception)
                {
                    EditorGUILayout.LabelField(
                        "Actor.DistanceTo Surface (local/physical debug)",
                        $"Unavailable ({exception.GetType().Name})");
                }
            }
        }

        private static void DrawBehaviourSection(NPCActor npcActor)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("NPC Behaviour", EditorStyles.boldLabel);
            var behavior = npcActor.GetComponent<NPCBehavior>();
            if (behavior == null)
            {
                EditorGUILayout.LabelField("NPCBehavior", "missing");
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Aggro Range", behavior.AggroRange);
                EditorGUILayout.FloatField("Aggro Delay", behavior.AggroDelay);
                EditorGUILayout.ObjectField(
                    "Combat Faction",
                    behavior.CombatFaction,
                    typeof(FactionDefinition),
                    false);
            }
        }

        private static void DrawNavMeshSection(NPCActor npcActor)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("NavMesh Movement", EditorStyles.boldLabel);

            if (!npcActor.TryGetComponent<NavMeshAgent>(out var agent) || agent == null)
            {
                EditorGUILayout.LabelField("Component Present", "false");
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Component Present", true);

                if (!TryReadValue(() => agent.enabled, out bool agentEnabled, out string enabledFailure))
                {
                    EditorGUILayout.LabelField("Enabled", $"Unavailable ({enabledFailure})");
                    DrawUnavailableAttachedState("agent enabled state unavailable");
                    DrawStaticAgentValues(agent);
                    return;
                }

                if (!TryReadValue(
                        () => agent.gameObject.activeInHierarchy,
                        out bool activeInHierarchy,
                        out string activeFailure))
                {
                    EditorGUILayout.Toggle("Enabled", agentEnabled);
                    EditorGUILayout.LabelField("GameObject Active", $"Unavailable ({activeFailure})");
                    DrawUnavailableAttachedState("GameObject active state unavailable");
                    DrawStaticAgentValues(agent);
                    return;
                }

                EditorGUILayout.Toggle("Enabled", agentEnabled);
                EditorGUILayout.Toggle("GameObject Active", activeInHierarchy);
                DrawStaticAgentValues(agent);

                if (!agentEnabled || !activeInHierarchy)
                {
                    EditorGUILayout.LabelField("isOnNavMesh", "Not queried (agent disabled or inactive)");
                    DrawUnavailableAttachedState("agent disabled or inactive");
                    return;
                }

                if (!TryReadValue(() => agent.isOnNavMesh, out bool isOnNavMesh, out string navMeshFailure))
                {
                    EditorGUILayout.LabelField("isOnNavMesh", $"Unavailable ({navMeshFailure})");
                    DrawUnavailableAttachedState("NavMesh attachment state unavailable");
                    return;
                }

                EditorGUILayout.Toggle("isOnNavMesh", isOnNavMesh);
                if (!isOnNavMesh)
                {
                    DrawUnavailableAttachedState("agent is not attached to a NavMesh");
                    return;
                }

                DrawReadOnlyValue("isStopped", () => agent.isStopped);
                DrawReadOnlyValue("pathPending", () => agent.pathPending);
                if (TryReadValue(() => agent.hasPath, out bool hasPath, out string pathFailure))
                {
                    EditorGUILayout.Toggle("hasPath", hasPath);
                    EditorGUILayout.LabelField(
                        "pathStatus",
                        hasPath ? ReadPathStatus(agent) : "(no path)");
                }
                else
                {
                    EditorGUILayout.LabelField("hasPath", $"Unavailable ({pathFailure})");
                    EditorGUILayout.LabelField("pathStatus", "Unavailable");
                }

                DrawReadOnlyValue("Next Position", () => agent.nextPosition);
                DrawReadOnlyValue("Destination", () => agent.destination);
                DrawReadOnlyValue("Remaining Distance", () => agent.remainingDistance);
            }
        }

        private static void DrawStaticAgentValues(NavMeshAgent agent)
        {
            DrawReadOnlyValue("Transform Position", () => agent.transform.position);
            DrawReadOnlyValue("Velocity", () => agent.velocity);
            DrawReadOnlyValue("Stopping Distance", () => agent.stoppingDistance);
            DrawReadOnlyValue("Speed", () => agent.speed);
            DrawReadOnlyValue("Agent Type ID", () => agent.agentTypeID);
            DrawReadOnlyValue("Area Mask", () => agent.areaMask);
        }

        private static void DrawUnavailableAttachedState(string reason)
        {
            EditorGUILayout.LabelField("isStopped", $"Not queried ({reason})");
            EditorGUILayout.LabelField("pathPending", $"Not queried ({reason})");
            EditorGUILayout.LabelField("hasPath", $"Not queried ({reason})");
            EditorGUILayout.LabelField("pathStatus", $"Not queried ({reason})");
            EditorGUILayout.LabelField("Next Position", $"Not queried ({reason})");
            EditorGUILayout.LabelField("Destination", $"Not queried ({reason})");
            EditorGUILayout.LabelField("Remaining Distance", $"Not queried ({reason})");
        }

        private static string ReadPathStatus(NavMeshAgent agent)
        {
            try
            {
                return agent.pathStatus.ToString();
            }
            catch (Exception exception)
            {
                return $"Unavailable ({exception.GetType().Name})";
            }
        }

        private static void DrawReadOnlyValue<T>(string label, Func<T> valueReader)
        {
            try
            {
                object value = valueReader();
                EditorGUILayout.LabelField(label, value == null ? "(null)" : value.ToString());
            }
            catch (Exception exception)
            {
                EditorGUILayout.LabelField(label, $"Unavailable ({exception.GetType().Name})");
            }
        }

        private static bool TryReadValue<T>(
            Func<T> valueReader,
            out T value,
            out string failure)
        {
            try
            {
                value = valueReader();
                failure = null;
                return true;
            }
            catch (Exception exception)
            {
                value = default;
                failure = exception.GetType().Name;
                return false;
            }
        }

        private static string FormatOwner(Actor actor)
        {
            return actor.Owner.HasValue ? actor.Owner.Value.ToString() : "(none)";
        }

        private static string FormatSpatialContext(ActorSpatialContext context)
        {
            if (!context.IsValid)
                return "(invalid)";

            return $"World={context.WorldID}, Map={context.MapID}, Instance={context.InstanceID}";
        }
    }
}
