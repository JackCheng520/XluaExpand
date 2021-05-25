using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XLua;
namespace XLuaExpand
{
    public class LuaBehavior : MonoBehaviour
    {
        public string filename;
        private LuaTable self;
        public delegate void RebindCallback();
        public RebindCallback rebindCallback;
        private bool hasAwake = false;
        private bool hasStart = false;
        private LuaFunction update, fixedUpdate;
        private Dictionary<string, LuaFunction> methodDic = new Dictionary<string, LuaFunction>();
        #region Mono Function

        private void Awake()
        {
            if (string.IsNullOrEmpty(filename))
            {
                Debug.LogError("[LuaBehaviour] filename is null");
                return;
            }
            LuaAwake();
        }

        private void Start()
        {
            LuaStart();
        }

        public void CallUpdate()
        {
            update.Call(self);
        }

        public void CallFixedUpdate()
        {
            fixedUpdate.Call(self);
        }

        private void OnDestroy()
        {
            CallMethod("OnDestroy");
            DisposeComponent();
            if (methodDic != null)
            {
                foreach (var func in methodDic.Values)
                {
                    if (func != null)
                    {
                        func.Dispose();
                    }
                }
            }
            if (self != null)
            {
                self.Dispose();
                self = null;
            }
            rebindCallback = null;
        }
        #endregion

        #region Lua Function
        private void LuaAwake()
        {
            if (hasAwake)
                return;
            InitLuaClass();
            if (rebindCallback != null)
            {
                rebindCallback();
                rebindCallback = null;
            }
            InjectNodes2Lua();
            hasAwake = true;

            LuaBehavior[] subLuaBehaviour = this.GetComponentsInChildren<LuaBehavior>(true);
            if (subLuaBehaviour != null && subLuaBehaviour.Length != 0)
            {
                foreach (var l in subLuaBehaviour)
                {
                    l.LuaAwake();
                }
            }

            if (self != null)
            {
                update = GetMethod("Update");
                fixedUpdate = GetMethod("FixedUpdate");
            }

            CallMethod("Awake");
        }

        private void LuaStart()
        {
            if (hasStart)
                return;
            CallMethod("Start");
            hasStart = true;
        }

        private void InitLuaClass()
        {
            string[] _tempPath = filename.Split(new char[] { '/' });
            string _className = _tempPath[_tempPath.Length - 1];
            LuaTable _lua = LuaMain.instance.GetTable(_className);
            if (_lua == null)
            {
                _lua = LuaMain.instance.NewTable();
                LuaMain.instance.DoFile(_className, _lua);
                LuaMain.instance.SetTable(_className, _lua);
            }

            if (_lua != null)
            {
                // 真正的对应的UI的table
                self = _lua.Get<LuaTable>(_className);
                self.Set("self", self);
                self.Set("scriptName", filename);
                self.Set("gameObject", gameObject);
                self.Set("instID", gameObject.GetInstanceID());
                self.Set("transform", transform);
            }
        }

        private void InjectNodes2Lua()
        {
            ComponentBinder _binder = this.gameObject.GetComponent<ComponentBinder>();
            if (_binder != null)
            {
                _binder.InjectNode(self);
            }
            else
            {
                ComponentBinderUtil.Traverse(gameObject, (key, type, com, index) =>
                {
                    LuaBehavior _lua = com as LuaBehavior;

                    if (index == -1)
                    {
                        SetValue2SelfTable(key, index, _lua == null ? com as object: _lua.GetLuaObject());
                    }
                    else
                    {
                        SetValue2SelfTable(key, _lua == null ? com as object: _lua.GetLuaObject());
                    }
                });
            }
        }
        #endregion

        #region Tool Function

        public object CallMethod(string methodName, params object[] args)
        {
            if (self == null || string.IsNullOrEmpty(methodName))
                return null;

            LuaFunction method = GetMethod(methodName);
            return method != null ? method.Call(self, args) : null;
        }

        public LuaFunction GetMethod(string methodName)
        {
            LuaFunction method;
            if (!methodDic.TryGetValue(methodName, out method))
            {
                LuaTable __class = self.Get<LuaTable>("__class");
                if (__class != null)
                {
                    __class.Get<string, LuaFunction>(methodName, out method);
                    if (!methodName.Equals("Update") && !methodName.Equals("FixedUpdate"))
                    {
                        methodDic.Add(methodName, method);
                    }
                }
            }

            return method;
        }

        public LuaTable GetLuaObject()
        {
            return self;
        }

        private void SetValue2SelfTable(string key, object value)
        {
            if (self != null)
            {
                self.Set(key, value);
            }
        }
        private void SetValue2SelfTable(string key, int index, object value)
        {
            if (index == -1)
            {
                SetValue2SelfTable(key, value);
            }
            else
            {
                LuaTable _array = null;
                self.Get(key, out _array);
                if (_array == null)
                {
                    _array = LuaMain.instance.NewTable();
                    self.Set(key, _array);
                }
                _array.Set(index, value);
            }
        }

        private void DisposeComponent()
        {
            Component[] components = GetComponentsInChildren<Component>(true);
            foreach (var com in components)
            {
                switch (com)
                {
                    case Button btn:
                        btn.onClick = null;
                        break;

                    case Dropdown drop:
                        drop.onValueChanged = null;
                        break;

                    case InputField input:
                        input.onValueChanged = null;
                        input.onEndEdit = null;
                        break;

                    case MaskableGraphic mask:
                        mask.onCullStateChanged = null;
                        break;

                    case Scrollbar scrollbar:
                        scrollbar.onValueChanged = null;
                        break;

                    case ScrollRect scrollRect:
                        scrollRect.onValueChanged = null;
                        break;

                    case Slider slider:
                        slider.onValueChanged = null;
                        break;

                    case Toggle toggle:
                        toggle.onValueChanged = null;
                        break;

                    case EventTrigger eventTrigger:
                        if (eventTrigger.triggers != null)
                        {
                            foreach (var trigger in eventTrigger.triggers)
                            {
                                if (trigger != null && trigger.callback != null)
                                {
                                    trigger.callback = null;
                                }
                            }
                        }

                        break;

                    default:
                        break;
                }
            }
        }
        #endregion
    }
}
