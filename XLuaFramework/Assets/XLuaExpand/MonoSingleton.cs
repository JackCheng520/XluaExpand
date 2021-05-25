using UnityEngine;

namespace XLuaExpand
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        private static object _lock = new object();

        private static bool destroyed = false;
        private static bool persistent = false;
        private static bool automatic = false;
        private static bool missing = false;

        protected MonoSingleton(bool persistent = false, bool automatic = false)
        {
            MonoSingleton<T>.persistent = persistent;
            MonoSingleton<T>.automatic = automatic;
        }

        public static bool instantiated
        {
            get { return !missing && !destroyed && _instance != null; }
        }

        public static T instance
        {
            get
            {
                if (!Application.isPlaying)
                {
                    T[] instances = FindObjectsOfType<T>();

                    if (instances.Length == 1)
                    {
                        _instance = instances[0];
                    }
                    else if (instances.Length == 0)
                    {
                        throw new UnityException("Missing '" + typeof(T) + "' singleton in the scene.");
                    }
                    else if (instances.Length > 1)
                    {
                        throw new UnityException("More than one '" + typeof(T) + "' singleton in the scene.");
                    }
                }

                if (destroyed)
                {
                    return null;
                }

                if (missing)
                {
                    throw new UnityException("Missing '" + typeof(T) + "' singleton in the scene.");
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        T[] instances = FindObjectsOfType<T>();

                        if (instances.Length == 1)
                        {
                            _instance = instances[0];
                        }
                        else if (instances.Length == 0)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();

                            if (!automatic)
                            {
                                Destroy(singleton);

                                missing = true;

                                throw new UnityException("Missing '" + typeof(T) + "' singleton in the scene.");
                            }

                            singleton.name = "(singleton) " + typeof(T).ToString();

                            if (persistent)
                            {
                                DontDestroyOnLoad(singleton);
                            }
                        }
                        else if (instances.Length > 1)
                        {
                            throw new UnityException("More than one '" + typeof(T) + "' singleton in the scene.");
                        }
                    }

                    return _instance;
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (persistent)
            {
                destroyed = true;
            }
        }
    }
}
