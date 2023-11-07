

# Vomit Lib

<font style="background: red">施工中...</font> <font style="background: red">开发中...</font>

个人的基于 [QFramework](https://github.com/liangxiegame/QFramework) + [UniTask](https://github.com/Cysharp/UniTask) + [Addressable](https://docs.unity.cn/Packages/com.unity.addressables@1.14/manual/index.html) + [Luban](https://github.com/focus-creative-games/luban) 的小游戏快速开发框架 

`在开发了多个小游戏的DEMO后根据个人习惯提炼出的框架,目前仍在整合中...`

`本质上是基于 QF 的二次开发,所有设计规范都遵循 QF 的设计. `

`项目名称是对自己的自嘲,对所有前辈和同行保持最大尊重!`

<!-- PROJECT SHIELDS -->
### 使用到的库

- [QFramework](https://github.com/liangxiegame/QFramework)
- [UniTask](https://github.com/Cysharp/UniTask)
- [Addressable](https://docs.unity.cn/Packages/com.unity.addressables@1.14/manual/index.html)
- [Luban](https://github.com/focus-creative-games/luban)

###### **使用前**
1. 需要知道 QFramework 的使用方式(仅核心架构)
2. 需要知道 UniTask`await` / `async` 的基本内容
3. 避免更多的学习成本, 其余部分尽量采用 Unity 原生设计,包括 UI 框架 和 资源框架.
4. (可选) 了解 Luban 的使用方式

###### **安装步骤**
1. 在 UPM 中安装 Addressables
2. 在 UPM 中安装 UniTask `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
3. 在 UPM 中安装 VomitLib `https://github.com/HmzMoonZy/VomitLib.git`

###### **项目验证** <font style="background: red">开发中...</font>
1. [史莱姆咖啡厅]()  
 个人独立开发项目 #模拟经营 #Roguelike #放置
2. [剑蛊骰]()      
   个人独立开发项目 #回合制 #Roguelike
<details>
    <summary>回合制战斗流程代码</summary>  

```csharp
        // 广播事件
        await SetBattleState(BattleLoopState.Before);
        // 回合制循环
        while (true)
        {
            // 回合开始
            await SetBattleState(BattleLoopState.RoundStart);
            await ProcessRoundStartState();
            // AI 行动
            await SetBattleState(BattleLoopState.AI);
            await ProcessAIState();
            // 玩家行动
            await SetBattleState(BattleLoopState.Player);
            await ProcessPlayerState();
            // 处理对抗
            await SetBattleState(BattleLoopState.AD);
            await ProcessADState();
            // 战斗结束.
            if (CheckBattleEnd(out var isWinner))
            {
                battleResult.IsWinner = isWinner;
                break;
            }
            // 回合结束
            await SetBattleState(BattleLoopState.RoundEnd);
            await ProcessRoundEndState();
        }
        // 结算
        await SetBattleState(BattleLoopState.Settle);
        await ProcessSettlementState();
        await this.SendAsyncEvent(battleResult);
```
</details>



### 上手指南

- 在 Assets/Resources/ 下创建 VomitConfig 根据工程配置全局数据
- 拖入 `ViewRoot.prefab`
- 在合适的时机调用 `Vomit.Init(IArchitecture architecture)`
```csharp
public class V : QFramework.Architecture<V>
{
    protected override void Init(){ }
}

public class Test : MonoController, ICanSendEvent
{
    void Start()
    {
        // 初始化框架
        Vomit.Init(V.Interface);
    }

```

### UI框架 - View
- `VomitConfig` 中填入View部分的参数

#### 制作UI 
1. 在 Unity 的 Hierarchy 中选择 `Create-UI-VomitCanvas` 或 `Create-UI-VomitCanvas(No Raycast)` 后者无法做射线检测,性能更优.
2. 将制作好的 UI 做成预制体,在Project面板中选择`Create-Vomit-View-ViewScript` 自动生成和预制体同名的View代码.
3. 在项目中使用View:
```csharp
    // 同步打开一个 View
    View.OpenView<ViewTest>();
    // 异步打开一个 View, 可以在 View 的 OnOpen 中实现动画效果.
    await View.OpenViewAsync<ViewTest>();
    // 在打开时通过 QFramework 的事件系统 View 传递参数.
    await View.OpenViewAsync<ViewTest>().WithEvent(new ViewTestEvent {Params = "NewTest!"})
```

### 扩展QF - 异步事件
QF 提供了非常好用的事件系统. 

实际开发中有时希望等待事件回调.

这里扩展更方便的方法.
```csharp
    // 声明一个事件
    public struct TestEvent
    {
        public string Name;
    }
    
    // IController \ ICanSendEvent
    public class Test : MonoController, ICanSendEvent
    {
        async void Start()
       {
            // 初始化
            Vomit.Init(V.Interface);
           
            // 注册异步任务
            this.RegisterAliveEvent<TestEvent>(e =>
            {
                e.AddTask(UniTask.Create(async () =>
                {
                    await UniTask.WaitForSeconds(2);    // 延迟 2s.
                    LogKit.I(e.Name);
                }));
            });
           
            // 注册同步任务
            this.RegisterAliveEvent<TestEvent>(e =>
            {
                LogKit.I(e.Name);
            });
        
            // 等待事件回调完成
            await this.SendAsyncEvent(new TestEvent() {Name = "Hi"});
            
            // 所有事件回调执行完毕后调用
            LogKit.I("Finish!");
    }
}

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


