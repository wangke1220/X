﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using NewLife.Exceptions;
using NewLife.Reflection;

namespace NewLife.Model
{
    /// <summary>对象容器</summary>
    /// <remarks>
    /// 1，如果容器里面没有这个类型，则直接的创建对象返回
    /// 2，如果容器里面包含这个类型，并且指向的实例不为空，则返回
    /// 3，如果容器里面包含这个类型，并且指向的实例为空，则创建对象返回
    /// 4，如果有带参数构造函数，则从容器内获取各个参数的实例，最后创建对象返回
    /// 
    /// 这里有一点跟我们以往的想法非常不同，我们都习惯没有对象的时候，创建并加入字典。
    /// 这里采用两种方式，注册类型的时候，如果指定了实例，则表示这个类型对应单一的实例；
    /// 如果不指定实例，则表示支持该类型，每次创建。
    /// </remarks>
    public class ObjectContaner : IObjectContainer
    {
        #region 当前静态对象容器
        private static IObjectContainer _Current = new ObjectContaner();
        /// <summary>当前容器</summary>
        public static IObjectContainer Current
        {
            get { return _Current; }
            set { _Current = value; }
        }
        #endregion

        #region 父容器
        //private IObjectContainer _Parent;
        ///// <summary>父容器</summary>
        //public virtual IObjectContainer Parent
        //{
        //    get { return _Parent; }
        //    protected set { _Parent = value; }
        //}

        //private List<IObjectContainer> _Childs;
        ///// <summary>子容器</summary>
        //protected virtual IList<IObjectContainer> Childs
        //{
        //    get { return _Childs ?? (_Childs = new List<IObjectContainer>()); }
        //}

        ///// <summary>
        ///// 移除所有子容器
        ///// </summary>
        ///// <returns></returns>
        //public virtual IObjectContainer RemoveAllChildContainers()
        //{
        //    if (_Childs != null) _Childs.Clear();

        //    return this;
        //}

        ///// <summary>
        ///// 创建子容器
        ///// </summary>
        ///// <returns></returns>
        //public virtual IObjectContainer CreateChildContainer()
        //{
        //    IObjectContainer container = TypeX.CreateInstance(this.GetType()) as IObjectContainer;
        //    if (container is ObjectContaner)
        //        (container as ObjectContaner).Parent = this;
        //    else
        //    {
        //        PropertyInfo pi = this.GetType().GetProperty("Parent", BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.NonPublic);
        //        if (pi != null && pi.CanWrite) pi.SetValue(container, this, null);

        //    }
        //    Childs.Add(container);

        //    return container;
        //}
        #endregion

        #region 对象字典
        private IDictionary<Type, IDictionary<String, Map>> _stores = null;
        private IDictionary<Type, IDictionary<String, Map>> Stores { get { return _stores ?? (_stores = new Dictionary<Type, IDictionary<String, Map>>()); } }

        private IDictionary<String, Map> Find(Type type, Boolean add)
        {
            IDictionary<String, Map> dic = null;
            if (Stores.TryGetValue(type, out dic)) return dic;

            if (add)
            {
                lock (Stores)
                {
                    if (Stores.TryGetValue(type, out dic)) return dic;

                    dic = new Dictionary<String, Map>();
                    Stores.Add(type, dic);
                    return dic;
                }
            }

            return null;
        }

        class Map
        {
            #region 属性
            private Type _From;
            /// <summary>接口类型</summary>
            public Type From
            {
                get { return _From; }
                set { _From = value; }
            }

            private Type _To;
            /// <summary>实现类型</summary>
            public Type To
            {
                get { return _To; }
                set { _To = value; }
            }

            private String _Name;
            /// <summary>名称</summary>
            public String Name
            {
                get { return _Name; }
                set { _Name = value; }
            }

            private Object _Instance;
            /// <summary>实例</summary>
            public Object Instance
            {
                get { return _Instance; }
                set
                {
                    _Instance = value;
                    if (value != null) To = value.GetType();
                }
            }
            #endregion
        }
        #endregion

        #region 注册核心
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="to">实现类型</param>
        /// <param name="name">名称</param>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        protected virtual IObjectContainer Register(Type from, Type to, String name, Object instance)
        {
            if (from == null) throw new ArgumentNullException("from");
            // 名称不能是null，否则字典里面会报错
            if (name == null) name = String.Empty;

            IDictionary<String, Map> dic = Find(from, true);
            Map map = null;
            if (dic.TryGetValue(name, out map))
            {
                map.To = to;
                map.Instance = instance;
            }
            else
            {
                map = new Map();
                map.To = to;
                map.Instance = instance;
                if (!dic.ContainsKey(name)) dic.Add(name, map);
            }

            return this;
        }
        #endregion

        #region 注册
        /// <summary>
        /// 注册类型
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="to">实现类型</param>
        /// <returns></returns>
        public virtual IObjectContainer Register(Type from, Type to) { return Register(from, to, null); }

        /// <summary>
        /// 注册类型和名称
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="to">实现类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual IObjectContainer Register(Type from, Type to, String name) { return Register(from, to, name, null); }

        /// <summary>
        /// 注册类型
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">实现类型</typeparam>
        /// <returns></returns>
        public virtual IObjectContainer Register<TInterface, TImplement>() { return Register(typeof(TInterface), typeof(TImplement), null); }

        /// <summary>
        /// 注册类型和名称
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">实现类型</typeparam>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual IObjectContainer Register<TInterface, TImplement>(String name) { return Register(typeof(TInterface), typeof(TImplement), name); }

        /// <summary>
        /// 注册类型的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        public virtual IObjectContainer Register(Type from, Object instance) { return Register(from, null, instance); }

        /// <summary>
        /// 注册类型指定名称的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="name">名称</param>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        public virtual IObjectContainer Register(Type from, String name, Object instance) { return Register(from, null, name, instance); }

        /// <summary>
        /// 注册类型的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        public virtual IObjectContainer Register<TInterface>(Object instance) { return Register(typeof(TInterface), null, instance); }

        /// <summary>
        /// 注册类型指定名称的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="name">名称</param>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        public virtual IObjectContainer Register<TInterface>(String name, Object instance) { return Register(typeof(TInterface), name, instance); }
        #endregion

        #region 解析
        /// <summary>
        /// 解析类型的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        public virtual Object Resolve(Type from) { return Resolve(from, null); }

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual Object Resolve(Type from, String name)
        {
            if (from == null) throw new ArgumentNullException("from");
            // 名称不能是null，否则字典里面会报错
            if (name == null) name = String.Empty;

            IDictionary<String, Map> dic = Find(from, false);
            // 1，如果容器里面没有这个类型，则直接的创建对象返回
            //if (dic == null) return Activator.CreateInstance(type, false);
            //if (dic == null) return TypeX.CreateInstance(type);
            // 这个type可能是接口类型
            if (dic == null) return null;

            Map map = null;
            // 2，如果容器里面包含这个类型，并且指向的实例不为空，则返回
            //if (dic.TryGetValue(name, out map) && map != null && map.Instance != null) return map.Instance;
            // 根据名称去找，找不到返回空
            if (!dic.TryGetValue(name, out map) || map == null) return null;
            if (map.Instance != null) return map.Instance;

            // 检查是否指定实现类型
            if (map.To == null) throw new XException("名为{0}的{1}实现未找到！", name, from);

            Object obj = null;
            // 3，如果容器里面包含这个类型，并且指向的实例为空，则创建对象返回
            // 4，如果有带参数构造函数，则从容器内获取各个参数的实例，最后创建对象返回
            ConstructorInfo[] cis = map.To.GetConstructors();
            if (cis.Length <= 0)
                obj = TypeX.CreateInstance(map.To);
            else if (cis.Length == 1)
            {
                List<Object> ps = new List<Object>();
                foreach (ParameterInfo pi in cis[0].GetParameters())
                {
                    dic = Find(pi.ParameterType, false);
                    if (dic != null && dic.Count > 0)
                        ps.Add(Resolve(pi.ParameterType));
                    else
                        ps.Add(null);
                }
                obj = ConstructorInfoX.Create(cis[0]).CreateInstance(ps.ToArray());
            }
            else
                throw new XException("目标对象有多个构造函数，容器无法选择！");

            // 赋值注入
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(obj))
            {
                if (!pd.IsReadOnly && Stores.ContainsKey(pd.PropertyType)) pd.SetValue(obj, Resolve(pd.PropertyType));
            }

            return null;
        }

        /// <summary>
        /// 解析类型的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <returns></returns>
        public virtual TInterface Resolve<TInterface>() { return (TInterface)Resolve(typeof(TInterface), null); }

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual TInterface Resolve<TInterface>(String name) { return (TInterface)Resolve(typeof(TInterface), name); }

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        public virtual IEnumerable<Object> ResolveAll(Type from)
        {
            if (from == null) throw new ArgumentNullException("from");

            IDictionary<String, Map> dic = Find(from, false);
            if (dic == null) yield break;

            foreach (Map item in dic.Values)
            {
                if (item.Instance != null) yield return item.Instance;
            }
        }

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <returns></returns>
        public virtual IEnumerable<TInterface> ResolveAll<TInterface>()
        {
            IDictionary<String, Map> dic = Find(typeof(TInterface), false);
            if (dic == null) yield break;

            foreach (Map item in dic.Values)
            {
                if (item.Instance != null) yield return (TInterface)item.Instance;
            }
        }
        #endregion

        #region 解析类型
        /// <summary>
        /// 解析接口的实现类型
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        public virtual Type ResolveType(Type from) { return ResolveType(from, null); }

        /// <summary>
        /// 解析接口指定名称的实现类型
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual Type ResolveType(Type from, String name)
        {
            if (from == null) throw new ArgumentNullException("from");
            // 名称不能是null，否则字典里面会报错
            if (name == null) name = String.Empty;

            IDictionary<String, Map> dic = Find(from, false);
            if (dic == null) return null;

            Map map = null;
            if (!dic.TryGetValue(name, out map) || map == null) return null;

            return map.To;
        }

        ///// <summary>
        ///// 解析接口的实现类型
        ///// </summary>
        ///// <typeparam name="TInterface">接口类型</typeparam>
        ///// <returns></returns>
        //public virtual Type ResolveType<TInterface>() { return ResolveType(typeof(TInterface), null); }

        ///// <summary>
        ///// 解析接口指定名称的实现类型
        ///// </summary>
        ///// <typeparam name="TInterface">接口类型</typeparam>
        ///// <param name="name">名称</param>
        ///// <returns></returns>
        //public virtual Type ResolveType<TInterface>(String name) { return ResolveType(typeof(TInterface), name); }

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        public virtual IEnumerable<Type> ResolveAllTypes(Type from)
        {
            if (from == null) throw new ArgumentNullException("from");

            IDictionary<String, Map> dic = Find(from, false);
            if (dic == null) yield break;

            foreach (Map item in dic.Values)
            {
                yield return item.To;
            }
        }

        /// <summary>
        /// 解析类型所有已注册指定名称的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        public virtual IEnumerable<KeyValuePair<String, Type>> ResolveAllNameTypes(Type from)
        {
            if (from == null) throw new ArgumentNullException("from");

            IDictionary<String, Map> dic = Find(from, false);
            if (dic == null) yield break;

            foreach (Map item in dic.Values)
            {
                yield return new KeyValuePair<String, Type>(item.Name, item.To);
            }
        }
        #endregion
    }
}