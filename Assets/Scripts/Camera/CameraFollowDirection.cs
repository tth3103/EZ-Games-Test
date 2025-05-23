using UnityEngine;
using Cinemachine;
public class CameraFollowDirection : MonoBehaviour
{
    [SerializeField] CinemachineFreeLook freelookCamera;
    [SerializeField] Transform target;
    [SerializeField] float rotationSpeed = 5f;
    // Start is called before the first frame update
    void Start()
    {
        freelookCamera.m_XAxis.m_InputAxisName = null;
        freelookCamera.m_YAxis.m_InputAxisName = null;
    }

    void LateUpdate()
    {
        if (freelookCamera != null || target != null) return;
        Vector3 targetForward = target.forward;
        targetForward.y = 0;
        if (targetForward != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            freelookCamera.m_XAxis.Value = target.rotation.eulerAngles.y;
        }
    }
}
