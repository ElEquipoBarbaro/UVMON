using UnityEngine;

namespace UVG.Classroom2021.Runtime
{
    public class UVGClassroomSimplePlayerHint : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform doorMarker;
        [SerializeField] private float interactionDistance = 1.5f;

        private void Update()
        {
            if (player == null || doorMarker == null) return;

            if (Vector3.Distance(player.position, doorMarker.position) <= interactionDistance &&
                Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("[UVG Classroom 2021] Interacción con la puerta.");
            }
        }
    }
}
