using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class SmashHitVisuals : MonoBehaviour
{
    public GameObject smashHitPrefab;
    public static SmashHitVisuals instance;

    private ObjectPool<SmashHit> smashHitPool;

    // Start is called before the first frame update
    void Start()
    {
        if (instance != null)
            Debug.LogError("More than one SmashHitVisuals in scene!");
        instance = this;

        // Initialize a new pool for SmashHit objects
        smashHitPool = new ObjectPool<SmashHit>(initialSize: 10);

        // Get a new SmashHit object from the pool
        SmashHit hit = smashHitPool.GetObject();

        //// When done with the object, return it to the pool
        //smashHitPool.ReturnObject(hit);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}



public class SmashHit
{
    GameObject gameObject;

    public SmashHit()
    {
        gameObject = GameObject.Instantiate(SmashHitVisuals.instance.smashHitPrefab);
    }
}



public class ObjectPool<T> where T : new()
{
    private readonly Stack<T> _availableObjects = new Stack<T>();
    private readonly List<T> _allObjects = new List<T>();
    private readonly int _initialSize;

    public ObjectPool(int initialSize = 10)
    {
        _initialSize = initialSize;
        for (int i = 0; i < _initialSize; i++)
        {
            _availableObjects.Push(new T());
        }
    }

    public T GetObject()
    {
        if (_availableObjects.Count == 0)
        {
            // No available object, create a new one
            T newObj = new T();
            _allObjects.Add(newObj);
            return newObj;
        }
        return _availableObjects.Pop();
    }

    public void ReturnObject(T obj)
    {
        _availableObjects.Push(obj);
    }

    public int TotalCount => _allObjects.Count;
    public int AvailableCount => _availableObjects.Count;
}