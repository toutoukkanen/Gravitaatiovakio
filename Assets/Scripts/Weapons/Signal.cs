using UnityEngine;

public class Signal : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;


    private void OnEnable() => Invoke(nameof(Die), lifeTime);

    //private void Start() => Invoke(nameof(Die), lifeTime);
    private void Die() => gameObject.SetActive(false);
    
    private void OnDisable() => CancelInvoke();
    
    private void OnCollisionEnter2D() => Die();
}
