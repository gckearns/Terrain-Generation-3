using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Range(0,0.1f)]
    public float speedScale;
    [Range(0,100)]
    public float shiftBoost;

    [Header("Camera Settings")]
    public float rotateSpeed;
    public float lookAngleLimit;
    public new Camera camera;

    public bool useGravity;
    new Rigidbody rigidbody { get => _rigidbody == null ? _rigidbody = GetComponent<Rigidbody>() : _rigidbody; }
    Rigidbody _rigidbody;
    Vector3 moveAxes = new Vector3();
    Vector2 mouseAxes = new Vector2();
    float yTargetAngle = 0;

    // Start is called before the first frame update
    void Start()
    {

    }

    void OnValidate()
    {
        rigidbody.useGravity = useGravity;
    }

    // Update is called once per frame
    void Update()
    {
        mouseAxes.y = Input.GetAxis("Mouse Y"); //Which way and how far is the mouse moving?
        float yTargetAngleDelta = -mouseAxes.y * Time.deltaTime * rotateSpeed;
        yTargetAngle = Mathf.Clamp(yTargetAngle + yTargetAngleDelta, -lookAngleLimit, lookAngleLimit);
        Quaternion eulerDelta = Quaternion.Euler(yTargetAngle, 0, 0);
        Quaternion targetRotation = Quaternion.LookRotation(transform.forward) * eulerDelta;
        camera.transform.rotation = targetRotation; 
    }

    void FixedUpdate()
    {
        //Input.GetKey(Input.GetButton("Vertical");
        //Debug.LogFormat("Vertical axis: {0}", Input.GetAxis("Vertical"));
        //Debug.LogFormat("Horizontal axis: {0}", Input.GetAxis("Horizontal"));
        moveAxes.x = Input.GetAxis("Horizontal");
        moveAxes.y = Input.GetAxis("Vertical");
        moveAxes.z = Input.GetAxis("Jump") + Input.GetAxis("Crouch");
        float speedMult = Input.GetAxisRaw("Shift") > 0 ? speedScale * shiftBoost : speedScale;
        rigidbody.MovePosition(transform.position + (transform.forward * moveAxes.y * Time.fixedTime * speedMult) + (transform.right * moveAxes.x * Time.fixedTime * speedMult) + (transform.up * moveAxes.z * Time.fixedTime * speedMult));

        mouseAxes.x = Input.GetAxis("Mouse X");
        Quaternion eulerDelta = Quaternion.Euler(0, mouseAxes.x * Time.fixedDeltaTime * rotateSpeed, 0);
        rigidbody.rotation = rigidbody.rotation * eulerDelta;
    }
}
