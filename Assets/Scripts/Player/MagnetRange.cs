using UnityEngine;

public class MagnetRange : MonoBehaviour
{
    [SerializeField] float _rotateSpeed = 180f; // 초당 180도
    private void Update()
    {
        // 일정하게 계속 회전
        transform.Rotate(0, 0, _rotateSpeed * Time.deltaTime);
    }
}
