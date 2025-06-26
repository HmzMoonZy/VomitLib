using System;

namespace Twenty2.VomitLib.Procedure
{
    /// <summary>
    /// 流程状态接口 - 专门为Procedure设计的简化接口
    /// </summary>
    public interface IProcedureState
    {
        /// <summary>
        /// 进入状态
        /// </summary>
        void Enter();
        
        /// <summary>
        /// 退出状态
        /// </summary>
        void Exit();
        
        /// <summary>
        /// 更新状态 - 每帧调用
        /// </summary>
        void Update(float deltaTime, float unscaledDeltaTime);
        
        /// <summary>
        /// 固定更新 - FixedUpdate调用
        /// </summary>
        void FixedUpdate();
        
        /// <summary>
        /// 状态切换条件检查
        /// </summary>
        bool CanChangeFrom(IProcedureState fromState);
        
        /// <summary>
        /// 状态名称 - 用于调试
        /// </summary>
        string StateName { get; }
    }
}