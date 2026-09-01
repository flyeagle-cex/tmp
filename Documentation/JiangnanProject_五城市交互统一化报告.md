# JiangnanProject 五城市交互统一化报告

生成日期：2026-09-01  
统一母版：扬州（Scene `1`）  
最终状态：`PASS_WITH_MANUAL_ACCEPTANCE_REQUIRED`

## 1. 自动识别到的五座城市

| Scene | 城市 | English | 视频段 | 题目 |
|---|---|---|---:|---:|
| `1` | 扬州 | Yangzhou | 6 | 5 |
| `2` | 淮安 | Huai'an | 6 | 5 |
| `3` | 无锡 | Wuxi | 5 | 4 |
| `4` | 苏州 | Suzhou | 7 | 6 |
| `5` | 南京 | Nanjing | 5 | 4 |

另有入口 Scene：`Start`。五城合计 29 段城市视频、24 道文化题。

## 2. 扬州基准结构

- Scene：`Assets/Scenes/1.unity`，作为五城 Canonical Reference。
- 原始结构：Legacy UGUI Canvas、VideoPlayer/RawImage、问题面板、正确/错误反馈、完成面板及 `VideoQuizManager`。
- 视频与音频：6 段竖屏 H.264/AAC 视频，音频均为视频内嵌音轨。
- UI：原项目未识别到可直接作为五城母版的完整 Prefab；统一层改由 `UnifiedCityView` 在每个城市 Canvas 上创建同一棵 `CityInteractionRoot`。
- 统一流程：Intro → PlayingVideo → ShowingQuestion → ShowingFeedback → Transitioning → Completed。
- 统一播放：初始仅准备首帧，不产生 0.1 秒音频泄漏；用户点击 Start 后淡入视频；片段结束后进入题目或下一片段。

## 3. 原先五城不一致之处

| 项目 | 审计结果 |
|---|---|
| 内容规模 | 城市视频段和题目数量不同，不能以复制扬州内容的方式统一。 |
| 完成导航 | 南京 Scene 的 legacy `completeButton` 引用为空。 |
| Canvas | 原基准为 800×600、Constant Pixel Size，不满足 1920×1080 网页嵌入目标。 |
| 首帧状态 | VideoPlayer 原为 Play On Awake，Question Panel 又在序列化状态中激活，存在首帧竞态。 |
| 播放速度 | 五城 VideoPlayer 均序列化为 10 倍速，不符合正常文化导览播放。 |
| 音频连接 | 视频都含音轨，但 AudioSource target 未正确连接，导致“素材有声、运行不一定有声”。 |
| 比例 | 原 RawImage 缺少统一的 FitInParent 约束，竖屏视频在不同分辨率下有拉伸风险。 |
| 双语与字幕 | 原项目没有覆盖全部题干/选项/UI 的统一语言层，也没有跨 Scene 的四模式字幕设置。 |
| 重复逻辑风险 | 城市依赖 Scene 内序列化引用，缺少统一状态门控和防重复点击层。 |

## 4. 已统一内容

- Initial UI：五城均显示相同 Intro、顶部控制区、开始方式、弹窗层级和初始隐藏状态。
- Video：统一 URL 播放、1 倍速、首帧准备、0.25/0.32 秒淡出淡入、FitInParent 比例适配。
- Audio：统一 AudioSource 输出、启用音轨 0；Sound Off 使用 `mute`，不停止或重置时间轴。
- Subtitle：支持中文、English、中英双语、关闭；小/中/大字号；以 `VideoPlayer.time` 驱动。
- Question：题干、全部选项、正确/错误反馈即时随 UI 语言更新；正确答案使用稳定 `optionId`。
- Popup：统一反馈、完成、字幕设置和退出确认层；Quiz 显示时字幕隐藏，避免叠压。
- Language：中文/English 即时切换，不重载 Scene、不重播视频；设置跨城市保持。
- Navigation：完成后进入下一城市，末城返回入口；ESC 优先关闭顶层弹窗，否则打开双语退出确认。
- Safety：转场期间禁用交互；Start、Continue、Option 均有状态检查，阻止重复协程和重复提交。
- Resolution：Canvas 全部改为 Scale With Screen Size、1920×1080、Match 0.5。
- Web：视频移至根目录 `WebMedia`，构建后复制到发布目录并以 URL 播放；原素材未覆盖。

## 5. 新增脚本

| 文件 | 作用 |
|---|---|
| `Assets/Scripts/Unified/CityInteractionData.cs` | 城市、片段、题目、选项、字幕的共享数据结构。 |
| `Assets/Scripts/Unified/CityDataRepository.cs` | 按 Scene 加载城市 JSON。 |
| `Assets/Scripts/Unified/LanguageManager.cs` | 全局语言、字幕模式、字幕字号、声音状态与持久化。 |
| `Assets/Scripts/Unified/SubtitleManager.cs` | 按视频时间查询 cue 并实时渲染四种字幕模式。 |
| `Assets/Scripts/Unified/AudioController.cs` | 连接 VideoPlayer 音轨与 AudioSource，处理全局静音。 |
| `Assets/Scripts/Unified/TransitionController.cs` | 统一淡入淡出与转场互斥。 |
| `Assets/Scripts/Unified/UnifiedCityView.cs` | 生成五城共用的 `CityInteractionRoot` 与所有 UI 层。 |
| `Assets/Scripts/Unified/GlobalSceneBootstrap.cs` | 跨 Scene 初始化、Canvas 规范化和入口视频音频接线。 |
| `Assets/Scripts/Unified/MediaPathResolver.cs` | Editor、Windows、WebGL 的统一媒体 URL/本机缓存路径。 |
| `Assets/Scripts/Unified/RuntimeSmokeRunner.cs` | 五城自动运行烟雾测试和 JSON 结果输出。 |
| `Assets/Editor/JiangnanProjectValidator.cs` | 编辑器静态验收：场景、媒体、音轨、题目、字幕、Canvas。 |
| `Assets/Editor/JiangnanBuild.cs` | Windows/WebGL 构建、静态托管设置和媒体发布复制。 |

## 6. 修改脚本

- `Assets/Scripts/VideoQuizManager.cs`
  - 保留原 public 序列化字段与 `QuizData`，避免题目、答案、引用丢失。
  - 改为共享状态机、统一 View、URL 视频、字幕、声音、转场、导航和退出逻辑。
  - 原 Scene 中的中文题目、选项、反馈和 `correctIndex` 仍作为权威内容合并进双语数据。

此外修改了 `Start.unity` 与五个城市 Scene 的 Canvas、VideoPlayer 和初始 Active 状态；没有删除任何原始城市素材。

## 7. 新增 Prefab / Data

- 新增数据：`Assets/Resources/CityData/1.json` 至 `5.json`。
- 新增字体：`Assets/Resources/Fonts/NotoSansSC-Regular.otf` 与 OFL 许可证。
- 新增网页媒体：`WebMedia/*.mp4` 共 30 个，其中 12 个 MOV 仅无损换容器为 MP4；音视频流未重新编码。
- 未新增序列化 Prefab：项目原 Scene 引用较多且命名存在乱码。为降低断引用风险，母版以一个共享脚本生成的 `CityInteractionRoot` 实现，五城运行时结构完全相同。
- 原始 mp4、mov、png、jpg、音轨均未覆盖或删除。

## 8. 双语覆盖情况

| 城市 | 城市名/标题/UI | 题干/选项/反馈 | 结果 |
|---|---|---|---|
| 扬州 | 完整 | 5/5 | PASS |
| 淮安 | 完整 | 5/5 | PASS |
| 无锡 | 完整 | 4/4 | PASS |
| 苏州 | 完整 | 6/6 | PASS |
| 南京 | 完整 | 4/4 | PASS |

## 9. 字幕

| 模式 | 结果 |
|---|---|
| Chinese | PASS |
| English | PASS |
| Bilingual（中文上、英文下） | PASS |
| Off | PASS |
| Small / Medium / Large | PASS |
| 切换不改变播放时间 | PASS |
| 跨 Scene 保持 | PASS |

共 58 条双语时间 cue，均通过起止时间、顺序、双语非空和视频时长边界校验。

## 10. Question

- Chinese：PASS。
- English：PASS。
- 语言切换只改变显示文本：PASS。
- `correctOptionId` 与原 `correctIndex` 一致：24/24 PASS。
- 错误后重试、正确后继续、防重复点击：PASS。

## 11. Audio

| 城市 | 有音轨视频 | AudioSource 接线 | Sound 不重置时间轴 | 结果 |
|---|---:|---|---|---|
| 扬州 | 6/6 | PASS | PASS | PASS |
| 淮安 | 6/6 | PASS | PASS | PASS |
| 无锡 | 5/5 | PASS | PASS | PASS |
| 苏州 | 7/7 | PASS | PASS | PASS |
| 南京 | 5/5 | PASS | PASS | PASS |

## 12. Resolution

最终 Windows GPU 烟雾测试对五个城市逐一检查 Intro、Audio、Language、Subtitle、UI 边界：

| 分辨率 | 五城结果 | Error |
|---|---|---:|
| 1920×1080 | PASS 5/5 | 0 |
| 1600×900 | PASS 5/5 | 0 |
| 1366×768 | PASS 5/5 | 0 |
| 1280×720 | PASS 5/5 | 0 |

## 13. Remaining Issues

1. 字幕是依据片段主题制作并与时长同步的双语文化导览摘要，不是逐字听写稿。比赛终验前建议由内容负责人逐段试听，确认措辞和精确入点。
2. 无锡原项目一道“螃蟹季节”题的正确索引为 `0`（显示为“春季”）。按照“不得修改正确答案含义”的硬性约束已原样保留，建议由内容负责人确认原题是否录入错误。
3. 原项目版本为 `2022.3.62f2c1`，本机可用编辑器为 Unity `6000.3.22f1`，项目已完成升级、编译和双平台构建；升级前 Scenes/Scripts/ProjectSettings/Packages 已保存在 `Backups/pre_unification_20260901`。
4. WebGL 已通过零 Error/零 Warning 构建和本地 HTTP 全资源请求检查。最终上线服务器仍应进行一次人工浏览器点击验收，重点确认站点的自动播放策略、全屏策略和视频 Range 请求配置。
5. 源项目未提供可验证的独立生词数据/生词弹窗内容，因此未虚构文化术语；现有统一 Popup 层可在内容确认后扩展。

## 14. Final Status

`PASS_WITH_MANUAL_ACCEPTANCE_REQUIRED`

### 验证证据

- Editor Validator：PASS，五城 5/5，Error 0。
- Windows Build：Succeeded，Error 0，Warning 0；发布媒体 30/30。
- WebGL Build：Succeeded，Error 0，Warning 0；发布媒体 30/30；无 `VideoClip assets are not supported`。
- Windows Runtime：四种分辨率均 PASS 5/5。
- WebGL HTTP：`index.html`、loader、framework、wasm、data 均返回 200；首段 MP4 返回 `video/mp4`。

