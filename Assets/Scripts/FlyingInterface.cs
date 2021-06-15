using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlyingInterface : MonoBehaviour
{
    public GameObject bat;

    public InputAction setReferencAction;
    public InputAction flyAction;

    private Transform flyingReferencePoint;

    void Awake()
    {
        setReferencAction.performed += ctx => OnSetReference();
        flyAction.performed += ctx => OnFly();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        setReferencAction.Enable();
        flyAction.Enable();
    }

    void OnDisable()
    {
        setReferencAction.Disable();
        flyAction.Disable();
    }

    void OnSetReference()
    {
        flyingReferencePoint = bat.transform;
    }

    void OnFly()
    {
        flyingReferencePoint = bat.transform;
    }
}
