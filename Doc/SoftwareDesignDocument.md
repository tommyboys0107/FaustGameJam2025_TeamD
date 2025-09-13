# 《Hide & Seek》軟體設計文件 (SDD)

## 1. 文件概述

### 1.1 目的
本文件描述《Hide & Seek》遊戲的軟體系統架構、模組設計和技術實作細節，為開發團隊提供技術實作指引。

### 1.2 範圍
涵蓋遊戲核心系統、用戶介面、渲染系統、音效系統和網路架構的設計規格。

### 1.3 版本資訊
- 文件版本：1.0
- 建立日期：2025-09-13
- Unity 版本：6000.0.57f1
- 渲染管線：Universal Render Pipeline (URP)

## 2. 系統概述

### 2.1 技術堆疊
- **引擎**：Unity 6000.0.57f1
- **渲染管線**：Universal Render Pipeline (URP)
- **輸入系統**：Unity Input System
- **AI導航**：Unity AI Navigation
- **平台支援**：PC (主要)、Mobile (次要)
- **多人遊戲**：本機雙人對戰

### 2.2 系統架構概覽
```
┌─────────────────────────────────────────────────────────┐
│                    Game Application                     │
├─────────────────────────────────────────────────────────┤
│  Game Manager  │  Player Controller  │  UI Manager     │
├─────────────────────────────────────────────────────────┤
│  Character Sys │  Environment Sys    │  Audio System   │
├─────────────────────────────────────────────────────────┤
│  Lighting Sys  │  Score System       │  Input System   │
├─────────────────────────────────────────────────────────┤
│            Unity Engine (URP)                          │
└─────────────────────────────────────────────────────────┘
```

## 3. 核心系統設計

### 3.1 遊戲管理系統 (GameManager)

#### 3.1.1 功能職責
- 遊戲狀態管理 (開始、進行中、結束)
- 玩家角色分配 (殺手/警察)
- 遊戲時間控制
- 勝負條件判定

#### 3.1.2 主要類別
```csharp
public class GameManager : MonoBehaviour
{
    public enum GameState { Menu, Playing, GameOver }
    public enum PlayerRole { Killer, Police }

    // 遊戲狀態控制
    private GameState currentState;
    private float gameTime;

    // 玩家管理
    private PlayerController killer;
    private PlayerController police;

    // 遊戲設定
    private int killCount;
    private float arrestTime;
}
```

#### 3.1.3 狀態機設計
- **Menu**: 遊戲選單狀態
- **Playing**: 遊戲進行狀態
- **GameOver**: 遊戲結束狀態

### 3.2 角色控制系統 (PlayerController)

#### 3.2.1 功能職責
- 玩家輸入處理
- 角色移動控制
- 動作執行 (跳舞、換裝、互動)
- 角色狀態管理

#### 3.2.2 主要類別
```csharp
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Actions")]
    public float interactionCooldown = 2f; // 殺手: 2秒, 警察: 5秒
    public float disguiseCooldown = 5f;

    private PlayerRole role;
    private CharacterMovement movement;
    private ActionSystem actionSystem;
    private DisguiseSystem disguise;
}
```

#### 3.2.3 輸入映射
- **移動**: WASD / 方向鍵
- **跳舞**: 空白鍵 + 方向鍵 (不同舞姿)
- **換裝**: Q鍵 (5秒冷卻)
- **互動**: E鍵 (殺手殺人/警察逮捕)

### 3.3 角色系統 (CharacterSystem)

#### 3.3.1 偽裝系統 (DisguiseSystem)
```csharp
public class DisguiseSystem : MonoBehaviour
{
    [Header("Disguise Settings")]
    public Material[] availableMaterials;
    public Mesh[] availableMeshes;

    private float lastDisguiseTime;
    private int suspicionLevel; // 可疑度等級

    public void ChangeAppearance()
    public void UpdateSuspicion(float deltaTime)
    public bool IsDetectable()
}
```

#### 3.3.2 動作系統 (ActionSystem)
```csharp
public class ActionSystem : MonoBehaviour
{
    [Header("Action Settings")]
    public AnimationClip[] danceAnimations;
    public AnimationClip killAnimation;
    public AnimationClip arrestAnimation;

    private Animator animator;
    private float lastActionTime;

    public void PerformDance(int danceType)
    public void PerformKill(GameObject target)
    public void PerformArrest(GameObject target)
}
```

### 3.4 環境系統 (EnvironmentSystem)

#### 3.4.1 燈光控制系統 (LightingController)
```csharp
public class LightingController : MonoBehaviour
{
    [Header("Lighting Settings")]
    public Light[] discoLights;
    public AudioSource musicSource;
    public AnimationCurve lightIntensityCurve;

    private float musicTime;
    private bool isLightSyncEnabled = true;

    public void UpdateLightingWithMusic()
    public void SetLightIntensity(float intensity)
    public void RotateLights(float speed)
}
```

### 3.5 NPC系統 (NPCSystem)

#### 3.5.1 NPC控制器
```csharp
public class NPCController : MonoBehaviour
{
    [Header("NPC Behavior")]
    public float danceFrequency = 0.7f;
    public Vector3[] patrolPoints;
    public float moveSpeed = 2f;

    private NavMeshAgent agent;
    private Animator animator;
    private bool isDancing;

    public void StartDancing()
    public void StopDancing()
    public void MoveToRandomPoint()
}
```

#### 3.5.2 群體管理
```csharp
public class CrowdManager : MonoBehaviour
{
    [Header("Crowd Settings")]
    public GameObject npcPrefab;
    public int maxNPCCount = 50;
    public Transform[] spawnPoints;

    private List<NPCController> activeNPCs;
    private ObjectPool npcPool;

    public void SpawnNPCs(int count)
    public void UpdateCrowdBehavior()
}
```

### 3.6 音效系統 (AudioSystem)

#### 3.6.1 音樂管理器
```csharp
public class MusicManager : MonoBehaviour
{
    [Header("Music Settings")]
    public AudioClip backgroundMusic;
    public AudioSource musicSource;
    public float beatThreshold = 0.8f;

    private float[] spectrum;
    private float currentBeat;

    public void AnalyzeSpectrum()
    public bool IsBeatDetected()
    public float GetMusicTime()
}
```

#### 3.6.2 音效管理器
```csharp
public class SFXManager : MonoBehaviour
{
    [Header("Sound Effects")]
    public AudioClip[] footstepSounds;
    public AudioClip killSound;
    public AudioClip arrestSound;
    public AudioClip disguiseSound;

    private AudioSource sfxSource;

    public void PlaySFX(AudioClip clip, float volume = 1f)
    public void PlayFootstep()
}
```

### 3.7 用戶介面系統 (UISystem)

#### 3.7.1 UI管理器
```csharp
public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject gameplayUI;
    public GameObject gameOverPanel;

    [Header("Gameplay UI")]
    public Text killCountText;
    public Text timeText;
    public Text roleText;
    public Slider cooldownSlider;

    public void ShowGameplayUI()
    public void UpdateKillCount(int count)
    public void UpdateTimer(float time)
    public void ShowGameOver(bool killerWins)
}
```

### 3.8 計分系統 (ScoreSystem)

#### 3.8.1 分數管理器
```csharp
public class ScoreManager : MonoBehaviour
{
    [Header("Score Settings")]
    public int killBaseScore = 100;
    public float comboMultiplier = 1.5f;
    public float comboTimeWindow = 3f;

    private int currentScore;
    private int comboCount;
    private float lastKillTime;

    public void AddKillScore()
    public void ResetCombo()
    public int CalculateFinalScore()
}
```

## 4. 資料結構設計

### 4.1 遊戲設定資料
```csharp
[CreateAssetMenu(fileName = "GameSettings", menuName = "Hide&Seek/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Player Settings")]
    public float killerCooldown = 2f;
    public float policeCooldown = 5f;
    public float disguiseCooldown = 5f;

    [Header("Game Balance")]
    public int maxKillsToWin = 10;
    public float maxGameTime = 300f;

    [Header("NPC Settings")]
    public int npcCount = 30;
    public float npcMoveSpeed = 2f;
}
```

### 4.2 場景配置資料
```csharp
[CreateAssetMenu(fileName = "SceneConfig", menuName = "Hide&Seek/Scene Config")]
public class SceneConfiguration : ScriptableObject
{
    [Header("Lighting")]
    public Color[] lightColors;
    public float lightRotationSpeed = 30f;
    public AnimationCurve intensityCurve;

}
```

## 5. 渲染系統設計

### 5.1 URP 配置
- **PC渲染設定**: 高品質燈光、陰影、後處理
- **Mobile渲染設定**: 優化效能、降低品質設定
- **Volume Profile**: 夜店氛圍的後處理效果

### 5.2 燈光系統
- **方向光**: 主要場景照明
- **點光源**: 舞廳彩色燈光效果
- **聚光燈**: 舞台和吧台照明
- **實時陰影**: 增強隱藏效果

### 5.3 材質系統
- **角色材質**: 支援動態換色系統
- **環境材質**: PBR標準材質
- **特效材質**: 燈光和粒子效果

## 6. 效能優化策略

### 6.1 渲染優化
- **批次處理**: 相同材質的NPC使用GPU Instancing
- **LOD系統**: 距離較遠的NPC使用低模
- **遮擋剔除**: 利用Unity Occlusion Culling
- **動態批次**: 小型物件動態批次處理

### 6.2 記憶體優化
- **物件池**: NPC和特效使用物件池管理
- **紋理壓縮**: 不同平台使用適當壓縮格式
- **音效壓縮**: 背景音樂使用壓縮格式

### 6.3 AI優化
- **NavMesh烘焙**: 預先烘焙導航網格
- **更新頻率**: NPC AI邏輯降低更新頻率
- **群體行為**: 使用簡化的群體AI算法

## 7. 除錯和測試策略

### 7.1 除錯工具
- **Debug UI**: 顯示玩家狀態、冷卻時間、可疑度
- **Gizmos**: 場景中顯示偵測範圍、巡邏路徑
- **Console指令**: 快速測試功能的控制台指令

### 7.2 測試計畫
- **單元測試**: 核心邏輯功能測試
- **整合測試**: 系統間互動測試
- **效能測試**: 不同場景下的FPS測試
- **使用者測試**: 遊戲性和平衡性測試

## 8. 部署和維護

### 8.1 建構設定
- **開發版本**: 包含除錯資訊和測試功能
- **發布版本**: 移除除錯程式碼，優化效能
- **平台特定**: PC和Mobile使用不同設定

### 8.2 版本控制
- **Git工作流**: 使用feature分支進行開發
- **提交規範**: 遵循conventional commits
- **發布流程**: 自動化建構和測試流程

## 9. 技術風險和緩解策略

### 9.1 效能風險
- **風險**: 大量NPC可能造成效能問題
- **緩解**: 實作LOD系統和物件池管理

### 9.2 同步風險
- **風險**: 本機多人遊戲的輸入衝突
- **緩解**: 使用Unity Input System的多玩家支援

### 9.3 平衡性風險
- **風險**: 遊戲角色平衡性問題
- **緩解**: 可調整的遊戲參數和廣泛測試

## 10. 未來擴展規劃

### 10.1 網路多人遊戲
- 實作網路同步系統
- 伺服器端權威驗證
- 延遲補償機制

### 10.2 更多遊戲模式
- 團隊對戰模式
- 排行榜系統
- 自定義房間設定

### 10.3 進階功能
- AI自動玩家
- 觀戰模式
- 重播系統

---

**文件版本**: 1.0
**最後更新**: 2025-09-13
**作者**: Team D - FGJ25