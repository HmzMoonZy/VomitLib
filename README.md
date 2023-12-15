# Vomit Lib

<font style="background: red">施工中...</font> <font style="background: red">开发中...</font>

个人的基于 [QFramework](https://github.com/liangxiegame/QFramework) | [UniTask](https://github.com/Cysharp/UniTask) | [Addressable](https://docs.unity.cn/Packages/com.unity.addressables@1.14/manual/index.html) | [Luban](https://github.com/focus-creative-games/luban) 的小游戏快速开发框架

`在开发了多个小游戏的DEMO后根据个人习惯提炼出的框架,目前仍在整合中...`

`项目名称是对自己的自嘲,对所有前辈和同行保持最大尊重!`

<!-- PROJECT SHIELDS -->

### 使用到的库

- [QFramework](https://github.com/liangxiegame/QFramework)
- [UniTask](https://github.com/Cysharp/UniTask)
- [Addressable](https://docs.unity.cn/Packages/com.unity.addressables@1.14/manual/index.html)
- [Luban](https://github.com/focus-creative-games/luban)

### 提供的功能
##### 可用(待完善)
- <a href="#procedure"> 扩展QF, 添加了易用的 Procedure 层 </a>
- <a href="#view"> 符合 Unity 原生开发习惯的 UI框架 </a>
- <a href="#addr"> 基于 Addressables 的资源框架 </a>
- <a href="#clientdb"> 基于 Luban 本地数据库 API </a>
- <a href="#audio"> 简单的音频系统 </a>
- <a href="#net"> 客户端网络 </a>

###### 不可用
- *TapTap | Steam* 平台发布工具(整合中)
- P2P 网络游戏开发框架(做梦中)

### **使用前**
1. 需要知道 QFramework 的使用方式(仅核心架构)
2. 需要知道 UniTask`await` / `async` 的基本内容
3. (可选) 了解 Luban 的使用方式

### **安装步骤**
1. 在 UPM 中安装 Addressables
2. 在 UPM 中安装 UniTask `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
3. 在 UPM 中安装 VomitLib `https://github.com/HmzMoonZy/VomitLib.git`
4. 在 Assets/Resources/ 下创建 VomitConfig 根据工程配置全局数据

### **项目验证** <font style="background: red">开发中...</font>
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
            LogKit.I("启动流程结束!");
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
            LogKit.I("主页流程结束!");
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
            LogKit.I($"{str} With Async Call");
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
                LogKit.I(e.Str);
            });
            
            // 异步任务完成回调事件, 通常多个controller层会监听同一个异步事件,但不一定都提供异步方法.
            this.RegisterAliveEvent<TestAsyncEvent>(e =>
            {
                e.Done(() =>
                {
                    LogKit.I("I know this event done!");
                });
            });
            
            // 广播异步事件并等待
            await this.SendAsyncEvent(new TestEvent() {Str = "Hi"});
            
            // 所有事件回调执行完毕后调用
            LogKit.I("Finish!");
            
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
- 同于解耦View 面板的 ViewComponent
```csharp
// 当作普通的 MonoBehaviour 去开发
public class VCSwordIcon : ViewComponent
{
    public void Start()
    {
        this.RegisterViewEvent<>    // 监听事件, 组件销毁时自动取消监听.
    }
}

// UI 面板
public class ViewSwordDetail : ViewLogic
{
    public override UniTask OnOpened()
    {
        // 自动加载 ViewComponent 并实例化
        var icon = View.InstantiateVC<VCSwordIcon>(transform);
    }
 
    // 运行时自动绑定 UnityEditor 中的 BtnLogin,无需额外步骤
    private void __OnClick_BtnLogin()
    {
        LogKit.I("Click BtnLogin");
    }
}

public class Launch
{
    void Start()
    {
        // 同步打开一个 View
        View.OpenView<ViewSwordDetail>();
        // 异步打开一个 View, 可以在 View 的 OnOpen 中实现动画效果.
        await View.OpenViewAsync<ViewSwordDetail>();
        // 在打开时通过 QFramework 的事件系统 View 链式传递参数.
        await View.OpenViewAsync<ViewSwordDetail>().WithEvent(new ViewTestEvent {Params = "NewTest!"})
    }
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

## 客户端网络
<span id="net"></span>

- 提供客户端 Socket 封装
- 通过事件传递网络消息
- 简单的参数配置

## 音频系统
<span id="audio"></span>

- 区分 BGM | SFX
- 常用API封装
```csharp
public static void Init(Func<string, AudioClip> onSearchAudioClip = null, float bgmFactors = 0.8f, float bgmVolume = 1f, float seFactors = 1f, float seVolume = 1f)
   
```


## 资源框架-Addr
<span id="addr"></span>
#### 为什么是 Addressables ?
- Unity 官方库,并且已经更新多年.
- 目前开发的是纯单机的游戏,目标平台是Steam,所以对于资源管理的需求非常简单.
- 可视化的性能分析
- 通常以文件夹划分Bundle,开发中操作更简单和直观.尤其是规模不大的项目.
- 面向接口, 要封装和替换成其它资源框架都非常简单.
- UniTask 原生支持.
### 怎么使用
- 按照正常的方式使用Addressable
- AA 在原先的 AB 基础上做了增强,本质上提供了 [通过资源的唯一名称(寻址地址)找到这个资源], 而无需关心资源具体位置.
- 实际使用中我们经常会拼接各种冗长的字符串去确定这个唯一的地址.
- 于是,ADDR则提供,通过[类型]+[资源索引]的方式来自动拼接[唯一的资源名]
- 因为对于同一种类的资源命名规则理应是统一的.
```csharp
    // 注册一类资源的索引拼接规则, 这里是不同骰子点数的Sprite
    Addr.RegisterRule<Sprite>(Constant.AssetType.Sprite.Dice, s => $"Sprites/Dices/Dice{s}@png.png");
    
    // 同步加载点数3的骰子
    Sprite dice3Sprite = Addr.Load<Sprite>(Constant.AssetType.Sprite.Dice, 3);
    
    // 异步加载点数6的骰子
    Sprite dice6Sprite = Addr.LoadAsync<Sprite>(Constant.AssetType.Sprite.Dice, 6).Forget();
    
    // 异步加载点数5的骰子
    Addr.LoadAsync<Sprite>(Constant.AssetType.Sprite.Dice, 5, sprite => {
        image.sprite = sprite;
    });
    
    // 可以同时异步加载所有图标而无需担心重复Load
    for(var itemID in Backpack.List)
    {
        Addr.LoadAsync<GameObejct>(Constant.AssetType.GameObejct.ItemToken, itemID, token => {
            token.Init(itemID);
        });
    }
    
    // 或是借助UniTask
    for(var itemID in Backpack.List)
    {
        var id = itemID;
        UniTask.Create(async () => {await Addr.LoadAsync<GameObejct>(Constant.AssetType.GameObejct.ItemToken, itemID).Init(id)});
    }
    
```

## 本地数据库-ClientDB
<span id="clientdb"></span>
####
- 基于luban的客户端数据库扩展
#### 配置ClientDB Config
- lubandll路径
- lubanconfig 路径
- 自动 C# 代码生成路径
- 数据文件生成路径
- 本地化数据路径
#### 如何使用?
```csharp
    
    public class ClientDB 
    {
        public static Tables T => ClientDB<Tables>.T;
        
        public static void Init()
        {
            // Tables 为鲁班生成代码
            ClientDB<Tables>.Init(new Tables(Loader, true));
        }
        
        private static JSONNode Loader(string fileName)
        {
            // 加载数据文件
            var asset = Addr.Load<TextAsset>(Constant.AssetType.Text.LubanData, fileName);
            string json = asset.text;
            Addressables.Release(asset);
            return JSON.Parse(json);
        }
    }
    
    // 更多时候,我们不是所有数据都通过excel配置.
    // 类似技能数据这种复杂数据,我配置excel会配到头晕,于是更喜欢自己的数据配置器
    public class SkillData : IEditable  // 实现 IEditable 接口
    {
        public int GetID();

        public string GetName();
    }
    
    // 扩展 Tables 
    public partial class Tables
    {
        // CustomTable<T> 提供和Luban生成代码风格一致的数据表
        public CustomTable<SkillData> TbSkill;
        
        public Tables(System.Func<string, JSONNode> loader, bool useSelfData) : this(loader)
        {
            // 加载自己实现的配置文件
            TbSkill = new CustomTable<SkillData>(Addr.Load<TextAsset>(Constant.AssetType.Text.JGTData, nameof(SkillData)).text);
        }
    }
    
    // 正常使用它
    ExecuteSkill(ClientDB.T.TbSkill[1001].Logic);
    
```