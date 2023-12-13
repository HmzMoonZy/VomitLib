# Vomit Lib

<font style="background: red">施工中...</font> <font style="background: red">开发中...</font>

个人的基于 [QFramework](https://github.com/liangxiegame/QFramework) + [UniTask](https://github.com/Cysharp/UniTask) + [Addressable](https://docs.unity.cn/Packages/com.unity.addressables@1.14/manual/index.html) + [Luban](https://github.com/focus-creative-games/luban) 的小游戏快速开发框架 

`在开发了多个小游戏的DEMO后根据个人习惯提炼出的框架,目前仍在整合中...`

`本质上是基于 QF 的二次开发,所有设计规范都遵循 QF 的设计.只进行扩展,不修改.确保理解上无偏差.`

`项目名称是对自己的自嘲,对所有前辈和同行保持最大尊重!`

<!-- PROJECT SHIELDS -->

### 使用到的库

- [QFramework](https://github.com/liangxiegame/QFramework)
- [UniTask](https://github.com/Cysharp/UniTask)
- [Addressable](https://docs.unity.cn/Packages/com.unity.addressables@1.14/manual/index.html)
- [Luban](https://github.com/focus-creative-games/luban)

### 提供的功能

##### 可用(待完善)
- 符合 Unity 原生开发习惯的 UI 框架
- 基于 Addressables 的资源框架
- 参考 GF 的 Procedure 框架
- 基于 Luban 本地数据库 API
- 简单的音频系统

###### 不可用
- 客户端网络
- 平台*TapTap\Steam*发布工具
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
- [史莱姆咖啡厅]()  
个人独立开发项目 #模拟经营 #Roguelike #放置
- [剑蛊骰]()      
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

```

## 流程层 Procedure
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

## UI框架 - View
### 为什么还要自己实现一个UI框架? 
- UI框架的实现并不难, 但是大多是UI框架的学习成本却很高.花费时间学习不同的概念去实现同一件事让我感觉非常奇怪.
- 大多数UI框架会联动一套资源框架.

### 提供了什么?
- 整个UI开发体验上遵循原生的开发体验,仅仅提供几个增强选项.
- 自动遮罩 \ 自动切换字体 \ 层级配置 \ 本地化 \ 自动绑定按钮事件
- 没有代码生成, 使用 [SerializeField] 其实也可以相当优雅.

### 配置参数
![ViewConfig](https://github.com/HmzMoonZy/VomitLib/tree/master/Documentation/images/ViewConfig.png)
- ViewAddressable Prefix : View预制体在可寻址地址前缀 `[ViewAddressable Prefix]/ViewLogin.prefab`
- ViewComponent Addressable Prefix : View组件在可寻址地址的前缀 `[ViewComponent Addressable Prefix]/VCBackpackItemToken.prefab`
- Auto Mask Color : 自动生成遮罩的RGBA
- Default Font : 默认字体
- Script Generate Path : UI代码自动生成路径
- View Resolution : View 视图的开发分辨率

### 制作UI 
1. 在 Unity 的 Hierarchy 中选择 `Create-UI-VomitCanvas` 或 `Create-UI-VomitCanvas(No Raycast)` 后者无法做射线检测,性能更优.
2. 将制作好的 UI 做成预制体, 在Project面板中选择`Create-Vomit-View-ViewScript` 自动生成和预制体同名的View代码.

### ViewConfig
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

```csharp
    // 同步打开一个 View
    View.OpenView<ViewTest>();
    // 异步打开一个 View, 可以在 View 的 OnOpen 中实现动画效果.
    await View.OpenViewAsync<ViewTest>();
    // 在打开时通过 QFramework 的事件系统 View 链式传递参数.
    await View.OpenViewAsync<ViewTest>().WithEvent(new ViewTestEvent {Params = "NewTest!"})
        
        
    // 登陆界面   
    public partial class ViewLogin : ViewLogic  // partial 关键字, 另一部分用于绑定UI组件
    {
         public override UniTask OnOpened()
         {
             // Do something...
         }
     
         // 自动绑定 UnityEditor 中的 BtnLogin
         private void __OnClick_BtnLogin()
         {
             LogKit.I("Click BtnLogin");
         }
        
    }
```

## 资源框架-Addr
#### 为什么是 Addressables ?
- Unity 官方库,并且已经更新多年
- 可视化的性能分析
- 通常以文件夹划分Bundle,开发中操作更简单和直观.尤其是规模不大的项目.
- 面向接口, 要封装和替换成其它资源框架都非常简单.
- UniTask 原生支持
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

## 本地数据库-CilentDB
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

[//]: # (```sh)

[//]: # (git clone https://github.com/shaojintian/Best_README_template.git)

[//]: # (```)

[//]: # ()
[//]: # (### 文件目录说明)

[//]: # (eg:)

[//]: # ()
[//]: # (```)

[//]: # (filetree )

[//]: # (├── ARCHITECTURE.md)

[//]: # (├── LICENSE.txt)

[//]: # (├── README.md)

[//]: # (├── /account/)

[//]: # (├── /bbs/)

[//]: # (├── /docs/)

[//]: # (│  ├── /rules/)

[//]: # (│  │  ├── backend.txt)

[//]: # (│  │  └── frontend.txt)

[//]: # (├── manage.py)

[//]: # (├── /oa/)

[//]: # (├── /static/)

[//]: # (├── /templates/)

[//]: # (├── useless.md)

[//]: # (└── /util/)

[//]: # ()
[//]: # (```)

[//]: # ()
[//]: # ()
[//]: # ()
[//]: # ()
[//]: # ()
[//]: # (### 开发的架构)

[//]: # ()
[//]: # (请阅读[ARCHITECTURE.md]&#40;https://github.com/shaojintian/Best_README_template/blob/master/ARCHITECTURE.md&#41; 查阅为该项目的架构。)

[//]: # ()
[//]: # (### 部署)

[//]: # ()
[//]: # (暂无)

[//]: # ()
[//]: # (### 使用到的框架)

[//]: # ()
[//]: # (- [xxxxxxx]&#40;https://getbootstrap.com&#41;)

[//]: # (- [xxxxxxx]&#40;https://jquery.com&#41;)

[//]: # (- [xxxxxxx]&#40;https://laravel.com&#41;)

[//]: # ()
[//]: # (### 贡献者)

[//]: # ()
[//]: # (请阅读**CONTRIBUTING.md** 查阅为该项目做出贡献的开发者。)

[//]: # ()
[//]: # (#### 如何参与开源项目)

[//]: # ()
[//]: # (贡献使开源社区成为一个学习、激励和创造的绝佳场所。你所作的任何贡献都是**非常感谢**的。)

[//]: # ()
[//]: # ()
[//]: # (1. Fork the Project)

[//]: # (2. Create your Feature Branch &#40;`git checkout -b feature/AmazingFeature`&#41;)

[//]: # (3. Commit your Changes &#40;`git commit -m 'Add some AmazingFeature'`&#41;)

[//]: # (4. Push to the Branch &#40;`git push origin feature/AmazingFeature`&#41;)

[//]: # (5. Open a Pull Request)

[//]: # ()
[//]: # ()
[//]: # ()
[//]: # (### 版本控制)

[//]: # ()
[//]: # (该项目使用Git进行版本管理。您可以在repository参看当前可用版本。)

[//]: # ()
[//]: # (### 作者)

[//]: # ()
[//]: # (xxx@xxxx)

[//]: # ()
[//]: # (知乎:xxxx  &ensp; qq:xxxxxx)

[//]: # ()
[//]: # (*您也可以在贡献者名单中参看所有参与该项目的开发者。*)

[//]: # ()
[//]: # (### 版权说明)

[//]: # ()
[//]: # (该项目签署了MIT 授权许可，详情请参阅 [LICENSE.txt]&#40;https://github.com/shaojintian/Best_README_template/blob/master/LICENSE.txt&#41;)

[//]: # ()
[//]: # (### 鸣谢)

[//]: # ()
[//]: # ()
[//]: # (- [GitHub Emoji Cheat Sheet]&#40;https://www.webpagefx.com/tools/emoji-cheat-sheet&#41;)

[//]: # (- [Img Shields]&#40;https://shields.io&#41;)

[//]: # (- [Choose an Open Source License]&#40;https://choosealicense.com&#41;)

[//]: # (- [GitHub Pages]&#40;https://pages.github.com&#41;)

[//]: # (- [Animate.css]&#40;https://daneden.github.io/animate.css&#41;)

[//]: # (- [xxxxxxxxxxxxxx]&#40;https://connoratherton.com/loaders&#41;)

[//]: # ()
[//]: # (<!-- links -->)

[//]: # ([your-project-path]:shaojintian/Best_README_template)

[//]: # ([contributors-shield]: https://img.shields.io/github/contributors/shaojintian/Best_README_template.svg?style=flat-square)

[//]: # ([contributors-url]: https://github.com/shaojintian/Best_README_template/graphs/contributors)

[//]: # ([forks-shield]: https://img.shields.io/github/forks/shaojintian/Best_README_template.svg?style=flat-square)

[//]: # ([forks-url]: https://github.com/shaojintian/Best_README_template/network/members)

[//]: # ([stars-shield]: https://img.shields.io/github/stars/shaojintian/Best_README_template.svg?style=flat-square)

[//]: # ([stars-url]: https://github.com/shaojintian/Best_README_template/stargazers)

[//]: # ([issues-shield]: https://img.shields.io/github/issues/shaojintian/Best_README_template.svg?style=flat-square)

[//]: # ([issues-url]: https://img.shields.io/github/issues/shaojintian/Best_README_template.svg)

[//]: # ([license-shield]: https://img.shields.io/github/license/shaojintian/Best_README_template.svg?style=flat-square)

[//]: # ([license-url]: https://github.com/shaojintian/Best_README_template/blob/master/LICENSE.txt)

[//]: # ([linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=flat-square&logo=linkedin&colorB=555)

[//]: # ([linkedin-url]: https://linkedin.com/in/shaojintian)

[//]: # ()


