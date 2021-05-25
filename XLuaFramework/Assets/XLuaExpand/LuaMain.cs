using System.IO;
using System.Text;
using UnityEngine;
using XLua;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace XLuaExpand
{
    public class LuaMain : MonoSingleton<LuaMain>
    {
        private LuaEnv lua;
        public static LuaEnv LuaState { get { return instance == null ? null : instance.lua; } }

#region Mono Function
        private void Awake()
        {
            lua = new LuaEnv();
            lua.AddLoader(CustomLoader);
        }

        private void Start()
        {
            DoFile("main");
        }

#endregion

#region Tool Function
        // Lua 调试替换
        private void GetLuaFixBinary(string filename, ref byte[] binaryScript)
        {
            //if (mHasFixFolder && ScriptLocalFixUtil.HasLuaFixFile(filename))
            //{
            //    if (!assetPathScriptCache.TryGetValue(filename, out binaryScript))
            //    {
            //        binaryScript = ScriptLocalFixUtil.GetLuaFixFileBytes(filename);
            //        assetPathScriptCache.Add(filename, binaryScript);
            //    }
            //}
        }

        private byte[] CustomLoader(ref string filename)
        {
            byte[] binaryScript = null;
            string assetPath = filename;
            if (filename.Contains("/"))
            {
                filename = Path.GetFileName(filename);
            }
            GetLuaFixBinary(filename, ref binaryScript);
            if (binaryScript != null)
            {
                return binaryScript;
            }
            filename = filename.ToLower();
            if (!StringUtil.StartsWithIgnoreCase(assetPath, "Lua/"))
            {
                assetPath = string.Format("Lua/{0}", assetPath);
            }
#if UNITY_EDITOR
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine("Assets/XLuaExpand",assetPath)+".txt");// Resources.Load<TextAsset>(assetPath); //AssetManager.Instance.Load<TextAsset>(assetPath);
            if (asset == null)
            {
                Debug.LogErrorFormat("CustomLoader@Load Lua File Failed@{0}", Path.Combine("Assets/XLuaExpand", assetPath));
                return null;
            }
            //Debug.Log("CustomLoader:"+assetPath);
            UnityEngine.Assertions.Assert.IsNotNull(asset);
            binaryScript = asset.bytes;
#else
        assetPath = assetPath.ToLower();
        if (!assetPathScriptCache.TryGetValue(assetPath, out binaryScript))
        {
            bool isLuaPatch = ABManager.CheckIsPatch(assetPath);
            string loadPath = assetPath;
            if (isLuaPatch)
            {
                loadPath = assetPath.Replace("lua/", "lua_patch/");
            }
            UnityEngine.Object asset = null;
            if (!AssetManager.Instance.TryLoad(out asset, loadPath, typeof(TextAsset)))
            {
                ByteDance.Foundation.MyLogger.LogError_(@"Load lua script [{0}] failed, must be preload assetbundle first.", filename);
                binaryScript = null;
            }
            else
            {
                TextAsset txtAst = (TextAsset)asset;
                if (!txtAst)
                {
                    ByteDance.Foundation.MyLogger.LogError_(@"Load lua script [{0}] failed, should be a TextAsset.", filename);
                    binaryScript = null;
                }
                else
                {
                    byte[] bytes_decrypt = txtAst.bytes.DecryptWithTpsAES();
                    byte[] lua_bytes = bytes_decrypt.DecompressBytes();
                    assetPathScriptCache.Add(assetPath, lua_bytes);
                    AssetManager.Instance.Unload(asset);

                    binaryScript = lua_bytes;
                }
            }
        }
#endif
            return binaryScript;
        }
        public object[] DoFile(string filename, LuaTable env = null)
        {
            byte[] buffer = CustomLoader(ref filename);
            string script = Encoding.Default.GetString(buffer);
            return lua.DoString(script, filename, env);
        }
        public LuaTable GetTable(string tableName)
        {
            return lua.Global.Get<LuaTable>(tableName);
        }

        public void SetTable(string tableName, LuaTable table)
        {
            lua.Global.Set<string, LuaTable>(tableName, table);
        }

        public LuaTable NewTable(bool useMeta = true)
        {
            LuaTable table = lua.NewTable();
            if (useMeta)
            {
                LuaTable meta = lua.NewTable();
                meta.Set("__index", lua.Global);
                table.SetMetaTable(meta);
                meta.Dispose();
                meta = null;
            }
            return table;
        }
        private void Close()
        {
            lua.Dispose();
            lua = null;
        }
#endregion
    }
}
