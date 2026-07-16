using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Be aware this will not prevent a non singleton constructor
///   such as `T myT = new T();`
/// To prevent that, add `protected T () {}` to your singleton class.
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    //private static readonly object _lock = new();
    /// <summary>
    /// 获取单例类的实例. 在OnDestory时单例可能已经被销毁, 有可能返回空!
    /// </summary>
    public static T Instance
    {
        get
        {
            //lock (_lock)
            //{
            if (_instance)
                return _instance;
            var instances = FindObjectsOfType<T>();
            int count = instances.Length;
            if (count == 0)
                return null;
            Assert.IsTrue(count == 1, $"单例类{typeof(T)}实例数量为{count}, 可能出现了严重的bug!");
            _instance = instances[0];
            return _instance;
            //}
        }
    }
}