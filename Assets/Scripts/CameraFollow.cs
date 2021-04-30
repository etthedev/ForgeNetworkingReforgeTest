using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform _followTarget;
    [SerializeField] private float _cameraSpeed;

    #region Monobehaviour
    //! Late update to make sure that the player already moved
    private void LateUpdate()
    {
        MoveToTarget();
    }
    #endregion

    public void MoveToTarget()
    {
        if (_followTarget == null) return;

        this.transform.position = _followTarget.position;//Vector3.MoveTowards(this.transform.position, _followTarget.position, _cameraSpeed * Time.deltaTime);
    }
}
