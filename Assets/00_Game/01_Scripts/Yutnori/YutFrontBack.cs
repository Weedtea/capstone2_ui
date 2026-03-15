using Unity.VisualScripting;
using UnityEngine;

public class YutFrontBack : MonoBehaviour
{
    public bool isFront = true;
    public bool backYut = false;
    public bool isfalling = true;


    /* void OnCollisionStay(Collision collision)
    {
        if(collision.gameObject.CompareTag("Ground"))
        {
            isfalling = false;
            // float dot = Vector3.Dot(transform.up, Vector3.up);
            float dot = Vector3.Dot(transform.forward, Vector3.up);
            if(dot < -0.5f)
            {
                isFront = false;
            }
            else
            {
                isFront = true;
            }
        }
    }
    void OnCollisionExit(Collision collision)
    {
        if(collision.gameObject.CompareTag("Ground"))
        {
            isfalling = true;
        }
    } */
    void OnTriggerStay(Collider other) {
        if(other.gameObject.CompareTag("Ground"))
        {
            isfalling = false;
            // float dot = Vector3.Dot(transform.up, Vector3.up);
            float dot = Vector3.Dot(transform.forward, Vector3.up);
            if(dot < -0.5f)
            {
                isFront = false;
            }
            else
            {
                isFront = true;
            }
        }
    }
    void OnTriggerExit(Collider other)
    {
        if(other.gameObject.CompareTag("Ground"))
        {
            isfalling = true;
        }
    }
}
