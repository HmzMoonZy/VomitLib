using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using FluentAPI;
using QFramework;
using Twenty2.VomitLib.Tools;

namespace Twenty2.VomitLib.Procedure
{
    public class Procedure<T> : FSM<T> where T : struct
    {
        private static Procedure<T> _instance;

        public static Procedure<T> Instance => _instance ??= new Procedure<T>();


        /// <summary>
        /// 自动扫描所有可能的流程, 初始化流程系统
        /// </summary>
        public void Launch()
        {
            T start = default;
            
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.HasAttribute<ProcedureAttribute>())
                    {
                        continue;
                    }

                    var attr = type.GetAttribute<ProcedureAttribute>();
                    var id = (T) attr.ProcedureID;
                    var obj = (IState) Activator.CreateInstance(type);
                    // var obj = (IState) Activator.CreateInstance(type, args: new object[] {this});
                    
                    LogKit.I($"创建 Procedure {id}");

                    AddState(id, obj);

                    if (attr.IsEntry)
                    {
                        start = id;
                    }
                }
            }
            
            LogKit.I($"启动 Procedure {start}");

            Launch(start);
        }
        
        /// <summary>
        /// 切换流程
        /// </summary>
        public void Change(T id, IState context)
        {
            ChangeState(id, context);
            
            Vomit.Interface.SendEvent<EProcedure.Changed>();
        }
    }


}