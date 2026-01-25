# Vomit Lib

个人的基于 [QFramework](https://github.com/liangxiegame/QFramework) | [UniTask](https://github.com/Cysharp/UniTask) | [Luban](https://github.com/focus-creative-games/luban) 的小游戏快速开发框架

`在开发了多个小游戏的DEMO后根据个人习惯提炼出的框架,注重易用性和开发效率`

`项目名称是对自己的自嘲,对所有前辈和同行保持最大尊重!`

`VomitLib 是个人项目，主要用于个人独立游戏开发。文档不会及时更新`

<!-- PROJECT SHIELDS -->

### 使用到的库

- [QFramework](https://github.com/liangxiegame/QFramework)
- [UniTask](https://github.com/Cysharp/UniTask)
- [Addressable](https://docs.unity.cn/Packages/com.unity.addressables@1.14/manual/index.html)
- [Luban](https://github.com/focus-creative-games/luban)

### 提供的功能
##### 可用功能
- <a href="#procedure"> 扩展QF, 添加了易用的 Procedure 层 </a>
- <a href="#view"> 符合 Unity 原生开发习惯的 UI框架 </a>
- <a href="#clientdb"> 基于 Luban 本地数据库 API </a>
- <a href="#toolkits"> 丰富的工具包集合 </a>
- <a href="#fluentapi"> 流式API扩展 </a>

###### 不可用/开发中
- 音频系统 (重构中)
- 网络框架 (重构中)
- *TapTap | Steam* 平台发布工具(整合中)

### **使用前**
1. 需要知道 QFramework 的使用方式(仅核心架构)
2. 需要知道 UniTask`await` / `async` 的基本内容
3. (可选) 了解 Luban 的使用方式

### **安装步骤**
1. 在 UPM 中安装 Addressables
2. 在 UPM 中安装 UniTask `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
3. 在 UPM 中安装 VomitLib `https://github.com/HmzMoonZy/VomitLib.git`
4. 在项目合适位置创建 VomitConfig 配置文件

### **项目验证**
- [史莱姆咖啡厅](https://store.steampowered.com/app/2367890/Slime_Cafe)  
个人独立开发项目 #模拟经营 #Roguelike #放置
- [骰子剑](https://github.com/HmzMoonZy/DiceSwordDemo)      
 个人独立开发项目 #回合制 #Roguelike

# 上手指南

## 初始化
- 在合适的时机调用 `Vomit.Init(IArchitecture architecture)`

```csharp
public class V : QFramework.Architecture<V>
{
    protected override void Init(){ }
}

public class Launcher : MonoController, ICanSendEvent
{
    void Start()
    {
        // 初始化框架
        Vomit.Init(V.Interface);
    }
}
```

## 流程层 Procedure
<span id="procedure"></span>
### Procedure 是什么?
- Procedure 是管理程序全局状态的有限状态机 灵感来自 GF 的 Procedure
- Procedure 提供了**超级控制器**的职能,能够调用`SendEvent` 和 `SendCommand`
```csharp
public abstract class ProcedureState<T> : ICanGetModel, ICanGetUtility, ICanGetSystem, ICanRegisterEvent, ICanSendEvent, ICanSendCommand, IState
```
### 为什么需要 Procedure?
- 在QF原本的设计中,将整个 MVC 系统划分为 `System` `Controller` `Model`, 它们各自实现不同的`ICanXXX`接口以实现不同的能力,并遵循调用规范,通过`SendCommand` 和 `SendEvent` 来相互通信.
- 在规范中, `System` 的职能是分担 `Controller` 层的逻辑而无法复用`Command`
- 实际开发中, 我们往往会有一个或多个 `Super Controller` 或 `Super System` 去实现 `ICanSendCommand` 和 `ICanSendEvent`.
- 有了Procedure层, 还能规范全局的 UI调用 BGM调用, 做到谁呼出,谁关闭.
- 可以根据 Procedure 划分 Model 的设计, 避免整个项目只有一个Model用来做全局数据存储的尴尬问题.

### 怎么使用
```csharp
    // 声明Procedure的状态枚举
    public enum GameProcedureState { Launch, Home, Town, Battle,}   // 启动流程, 主页流程, 城镇流程, 战斗流程
    
    // 声明一个启动流程类,并且作为入口流程.
    [Procedure(ProcedureID = GameProcedureState.Launch, IsEntry = true)]
    public class ProcedureLaunch : ProcedureState<GameProcedureState>
    {
        public override bool Condition() => false;  // 不可逆
        
        public override void Enter()
        {
            // Do something...
            ChangeState(JGT.JGTProcedure.Home);     // 进入主界面流程
        }

        public override void Exit()
        {
            Log.I("启动流程结束!");
        }
    }
    
    // 声明一个主页流程类
    [Procedure(ProcedureID = GameProcedureState.Home)]
    public class ProcedureHome : ProcedureState<GameProcedureState>
    {
        public override bool Condition()
        {
            return CurrentProcedure == ProcedureState.Launch;   // 启动流程 => 主页流程
        }
        
        public override void Enter()
        {
            View.Open<ViewHome>();                  // 打开主页UI
            // Do something...   
            ChangeState(JGT.JGTProcedure.Town);     // 切换状态
        }

        public override void Exit()
        {
            View.Close<ViewHome>();                 // 关闭主页UI
            Log.I("主页流程结束!");
        }
    }
    
    // 声明一个城镇流程类
    [Procedure(ProcedureID = GameProcedureState.Town)]
    public class ProcedureTown : ProcedureState<GameProcedureState>
    {
        public override bool Condition()
        {
            // 启动流程 => 城镇流程 ; 战斗流程 => 城镇流程
            return CurrentProcedure == ProcedureState.Home || CurrentProcedure == ProcedureState.Battle;   
        }
        
        public async override  void Enter()
        {   
            // 监听战斗开始事件, 切换状态, 事件自动取消监听
            this.RegisterProcedureEvent<BattleStart> (e => ChangeState(ProcedureState.Battle));
            
            if(PrevProcedure == ProcedureState.Home) {/* 进入游戏逻辑 */}
            
            if(PrevProcedure == ProcedureState.Battle) {/* 战斗归来逻辑 */}
        }
        
        public override void OnUpdate() { }     // 提供 Update 方法
        public override void OnFixUpdate() { }  // 提供 FixUpdate 方法

        public override void Exit() { }
    }
   
```

## 扩展QF
###  优雅的监听仅一次的事件
```csharp
 public class ArenaSystem : AbstractSystem
 {
     
     public async void Continue()
     {
        // ...
        await DrawRewardCard(3);        // 抽取三张奖励卡

        // (int winArgumentIndex, EArena.ConfirmCard result1, EArena.ConfirmCard result2)
        var result = await Event.WaitEvent<EArena.ConfirmCard, EArena.SkipReward>();        // 等待玩家选择奖励或是跳过奖励,支持同时监听多个事件, 相当于 WhenAny()
        
        if(result.winArgumentIndex == 0)
        {
            // 确认奖励
        }
        
        if(result.winArgumentIndex == 1)
        {
            // 跳过奖励
        }
        await ClearAllCards();
        await ChangeArenaState(ArenaState.Rewarded);
        await UniTask.WaitForSeconds(1);
     }
 }

```

###  异步事件
- QF 提供了非常好用的事件系统.
- 实际开发中有时希望等待事件回调.
- 这里扩展更方便的方法.
```csharp
    // 声明一个异步事件
    [AsyncEvent]
    public struct TestEvent { public string Str; }
    
    // ICanSendEvent
    public class Test : MonoController, ICanSendEvent
    {
        async UniTask Delay(string str)
        {
            await UniTask.WaitForSeconds(2);    // 延迟 2s.
            Log.I($"{str} With Async Call");
        }
        
        async void Start()
        {
            // 注册异步任务
            this.RegisterAliveEvent<TestEvent>(e =>
            {
                e.AddTask(Delay(e.Str));
            });
            
            // 也可以注册同步任务
            this.RegisterAliveEvent<TestEvent>(e =>
            {
                Log.I(e.Str);
            });
            
            // 异步任务完成回调事件, 通常多个controller层会监听同一个异步事件,但不一定都提供异步方法.
            this.RegisterAliveEvent<TestAsyncEvent>(e =>
            {
                e.Done(() =>
                {
                    Log.I("I know this event done!");
                });
            });
            
            // 广播异步事件并等待
            await this.SendAsyncEvent(new TestEvent() {Str = "Hi"});
            
            // 所有事件回调执行完毕后调用
            Log.I("Finish!");
            
            // > Hi
            // > Hi With AsyncCall
            // > I know this event done!
            // > Finish
       }
}

```

### 坐标系工具 - CoordinateKit 
- 游戏开发中经常会使用到坐标系的相互转换.
- 通常来说,会涉及 屏幕坐标系 | UI坐标系 | 场景(World)坐标系 | TileMap坐标系(如果你用了)
- CoordinateKit 提供了它们相互转换的方便API.


## View - UI框架
<span id="view"></span>

### 为什么还要自己实现一个UI框架? 
- UI框架的实现并不难, 大多数UI框架实现的功能可以说是大同小异, 但是提供了各种新名词和概念使得学习成本却很高.
- 大多数UI框架会联动一套资源框架.
- 许多UI框架提供了各种组件绑定的代码生成,但实际上,一个UI在开发和设计阶段,往往需要频繁的操作自动生成的配置.
- 这导致了许多在 UnityEditor 中的隐含规则, 比如组件名称不能带下划线, 不能重名, 不能命名为关键字等等...
- 大多数时候使用 `[SerializeField]` 其实也可以相当优雅和方便.

### 提供了什么?
- 整个UI开发体验上遵循原生的开发体验,仅仅提供几个增强选项.
- 自动遮罩 \ 自动切换字体 \ 层级配置 \ 本地化 \ 自动绑定按钮事件
- UI开发中常用的API
- 同步/异步打开关闭面板
- 面板预加载和缓存机制
- 完整的生命周期管理

### 使用示例
```csharp
// UI 面板类
public class ViewSwordDetail : ViewLogic
{
    // 面板打开时调用
    public override void OnOpened(ViewParameterBase param)
    {
        // 初始化面板逻辑
    }

    // 运行时自动绑定按钮事件
    private void __OnClick_BtnLogin()
    {
        Log.I("Click BtnLogin");
    }
}

// 打开面板
public class GameUI
{
    // 同步打开面板
    View.Open<ViewSwordDetail>();

    // 异步打开面板
    await View.OpenAsync<ViewSwordDetail>();

    // 等待面板关闭
    await View.OpenAndWaitClose<ViewSwordDetail>();

    // 关闭面板
    View.Close<ViewSwordDetail>();
}
```

### 配置参数
![ViewConfig](https://github.com/HmzMoonZy/VomitLib/tree/master/Documentation/images/ViewConfig.png)
- ViewAddressable Prefix : View预制体在可寻址地址前缀 `[ViewAddressable Prefix]/ViewLogin.prefab`
- ViewComponent Addressable Prefix : View组件在可寻址地址的前缀 `[ViewComponent Addressable Prefix]/VCBackpackItemToken.prefab`
- Auto Mask Color : 自动生成遮罩的RGBA
- Default Font : 默认字体
- Script Generate Path : UI代码自动生成路径
- View Resolution : View 视图的开发分辨率

### 制作UI - 一个UI是一个Canvas
1. 在 Unity 的 Hierarchy 中选择 `Create-UI-VomitCanvas` 或 `Create-UI-VomitCanvas(No Raycast)` 后者无法做射线检测,性能更优.
2. 将制作好的 UI 做成预制体, 在Project面板中选择`Create-Vomit-View-ViewScript` 自动生成和预制体同名的View代码.

### ViewConfig - 单独控制每个UI
- 每个VomitCanvas都会携带一个通用的 ViewConfig 组件.
- Layer : 层级配置
- EnableAutoMask : 是否自动开启遮罩
- ClickMaskTriggerClose : 点击自动生成的遮罩是否触发关闭面板
- AutoDefaultFont : 是否自动替换默认字体
- EnableLocalization : 是否自动进行本地化
- IsCache : 关闭后是否缓存
- AutoBindButtons : 是否自动绑定按钮事件

### ViewLogic & ViewLogic<T>
- 自动生成的 View 代码继承自 ViewLogic.

## 工具包集合
<span id="toolkits"></span>

VomitLib 提供了丰富的工具包，简化日常开发任务：

### 坐标系工具 - CoordinateKit
游戏开发中经常使用的坐标系相互转换工具，支持：
- 屏幕坐标系 ↔ UI坐标系 ↔ 场景(World)坐标系 ↔ TileMap坐标系
- 一致的API设计，简化坐标转换逻辑

```csharp
// 屏幕坐标转世界坐标
Vector3 worldPos = CoordinateKit.ScreenToWorld(screenPos);

// UI坐标转世界坐标
Vector3 worldPos = CoordinateKit.UIToWorld(uiPos);

// 世界坐标转Tilemap坐标
Vector3Int tilePos = CoordinateKit.WorldToTile(worldPos);
```

### 加密工具 - EncryptKit
提供常用的加密算法支持：
- MurmurHash3 哈希算法
- 数据加密解密功能

### ID生成工具 - IDKit
生成唯一标识符的工具类，支持：
- ULID (Universally Unique Lexicographically Sortable Identifier)
- 分布式友好的ID生成

### 随机数工具 - RandomKit
增强的随机数生成工具：
- 更好的随机数分布
- 种子管理功能
- 各种范围的随机数生成

### 纹理工具 - TextureKit
纹理处理相关工具：
- 纹理压缩
- 格式转换
- 纹理处理辅助功能

### 日志工具 - LogKit
统一的日志管理：
- 分级日志 (Debug, Info, Warning, Error)
- 自定义日志输出
- 运行时日志控制

## 流式API扩展
<span id="fluentapi"></span>

VomitLib 提供了丰富的 C# 和 Unity 流式API扩展，让代码更加简洁优雅：

### C# 基础类型扩展
```csharp
// 字符串扩展
"hello".IsNullOrEmpty();        // 检查字符串是否为空
"test.txt".GetFileExtension();   // 获取文件扩展名

// 集合扩展
list.ForEach(item => Debug.Log(item));
dictionary.GetOrAdd(key, () => defaultValue);

// 反射扩展
type.GetFieldsWithAttribute<SerializeFieldAttribute>();
```

### Unity 对象扩展
```csharp
// GameObject 扩展
gameObject.DestroyChildren();                    // 销毁所有子对象
gameObject.SetActiveWithChildren(true);          // 递归设置激活状态

// Transform 扩展
transform.ResetLocal();                          // 重置本地变换
transform.DestroyChildren();                     // 销毁所有子对象
transform.ChildrenForEach(child => Debug.Log(child.name));

// MonoBehaviour 扩展
monoBehaviour.DestroyAfterSeconds(2f);          // 延迟销毁
monoBehaviour.CancelDelayDestroy();              // 取消延迟销毁

// 颜色扩展
color.WithAlpha(0.5f);                           // 修改透明度
color.ToHex();                                   // 转换为十六进制字符串

// 向量扩展
vector3.WithX(10f);                              // 修改X分量
vector3.Flat();                                  // 转换为2D向量(Y=0)
```

## 框架监控
VomitLib 提供了强大的编辑器内监控工具，帮助开发者调试和分析：

### 启用监控
```csharp
// 在初始化框架后启用监控
Vomit.Init(V.Interface);
Vomit.EnableMonitor();
```

### 监控功能
- **ViewMonitor**: 监控UI面板的打开、关闭、缓存状态
- **ModelMonitor**: 监控数据模型的状态和变化
- **SystemMonitor**: 监控系统方法的调用情况
- **EventMonitor**: 监控事件系统的发送和接收
- **CommandMonitor**: 监控命令的执行情况
- **LubanMonitor**: 监控Luban配置数据的加载

监控工具仅在编辑器模式下可用，提供实时的运行状态反馈，极大提升了开发调试效率。

## 本地数据库-ClientDB
<span id="clientdb"></span>

基于 Luban 的客户端数据库扩展，提供统一的配置数据管理：

### 主要特性
- 支持 Excel 和 JSON 配置文件
- 自动代码生成
- 类型安全的数据访问
- 灵活的数据表扩展
- 本地化数据支持

### 使用示例
```csharp
public class GameDatabase
{
    public static Tables T => ClientDB<Tables>.T;

    public static void Init()
    {
        // Tables 为 Luban 自动生成的数据表类
        ClientDB<Tables>.Init(new Tables(LoadDataFile, true));
    }

    private static JSONNode LoadDataFile(string fileName)
    {
        // 加载配置数据文件
        var asset = Resources.Load<TextAsset>($"Data/{fileName}");
        return JSON.Parse(asset.text);
    }
}

// 使用配置数据
var itemData = GameDatabase.T.TbItem[1001];
Debug.Log($"Item: {itemData.Name}, Price: {itemData.Price}");
```

## 框架设计理念

VomitLib 的设计遵循以下原则：

### 1. 简洁性
- 遵循 Unity 原生开发习惯
- 最小化概念学习成本
- 避免过度封装

### 2. 实用性
- 聚焦实际开发需求
- 提供开箱即用的工具
- 减少样板代码

### 3. 可扩展性
- 基于 QFramework 的架构模式
- 支持自定义扩展
- 模块化设计

### 4. 开发效率
- 丰富的工具包集合
- 强大的编辑器支持
- 实时监控和调试
