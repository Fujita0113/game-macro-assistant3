# GameMacroAssistant - AI開発ワークフロー管理システム

## プロジェクト概要
GameMacroAssistantは、Windows 11マクロ自動化ツールの開発プロジェクトです。AIエージェントによる高速な実装・検証ワークフローにより、効率的な開発サイクルを実現します。

## 技術要件
- **言語**: C# (.NET 8)
- **UI Framework**: WPF with MVVM pattern
- **MVVM Library**: CommunityToolkit.Mvvm
- **テスト**: xUnit, Coverlet, ReportGenerator
- **Windows API**: user32.dll, kernel32.dll, dxgi.dll統合
- **アーキテクチャ**: 疎結合設計、依存性注入、SOLID原則

## ディレクトリ構成
```
/
├── src/                    # アプリケーション本体
│   ├── GameMacroAssistant.Core/      # ビジネスロジック層
│   ├── GameMacroAssistant.Wpf/       # プレゼンテーション層
│   └── GameMacroAssistant.Tests/     # 単体・統合テスト
├── docs/                   # プロジェクトドキュメント
│   ├── pm/                 # プロジェクト管理
│   │   ├── tasks/          # TaskGen-Agent生成タスク仕様
│   │   ├── sprints/        # Planner-Agent生成開発計画
│   │   └── progress/       # 進捗管理JSONファイル
│   ├── user-tests/         # TestDoc-Agent生成ユーザーテスト
│   └── requirements/       # 要件定義・技術仕様
├── worktrees/              # Dev-Agent作業ディレクトリ（Git Worktree）
└── .claude/
    ├── agents/             # サブエージェント定義
    ├── progress.json       # 全体進捗管理
    └── settings.local.json # Claude設定
```

## ブランチ戦略
- **main**: 保護ブランチ、統合済み安定版
- **task-<TaskID>-<description>**: 個別タスク開発用（worktreeと対応）
- **hotfix-<ID>**: 緊急バグ修正用（BugFix-Agent専用）

## 開発制約・設計原則
### アーキテクチャ制約
- **疎結合設計**: 各レイヤー間の依存性を最小化
- **依存性注入**: Microsoft.Extensions.DependencyInjection使用
- **SOLID原則**: 単一責任、開放閉鎖、リスコフ置換、インターフェース分離、依存性逆転
- **テスタビリティ**: 80%以上のテストカバレッジ維持

### 実装制約
- **Windows API統合**: P/Invoke経由でネイティブAPI呼び出し
- **パフォーマンス**: 画面キャプチャ50ms以内、入力精度≤5ms
- **メモリ管理**: using文とIDisposableパターンの徹底
- **スレッドセーフティ**: ConcurrentCollectionとlock文の適切な使用

### ファイル・ディレクトリ制約
- **Claude Code制限**: 上位ディレクトリへの移動不可のため、worktreesはルート配下に配置
- **相対パス使用**: 絶対パスは環境依存回避のため使用禁止
- **ファイル命名**: TaskID-機能名-詳細の形式（例：T-001-core-models）

## ワークフロー管理システム

### 進捗管理JSON構造
```json
{
  "project_id": "GameMacroAssistant",
  "current_phase": "planning|development|review|testing|integration",
  "current_sprint": "sprint-01",
  "completed_sprints": ["sprint-01"],
  "active_tasks": {
    "T-001": {
      "status": "completed|in_progress|blocked|pending",
      "assignee": "dev-agent-1",
      "worktree_path": "worktrees/T-001",
      "branch_name": "task-T-001-core-models",
      "dependencies": ["T-000"],
      "completion_date": "2025-01-15",
      "review_status": "approved|pending|rejected"
    }
  },
  "next_available_tasks": ["T-002", "T-003"],
  "workflow_state": {
    "last_completed_step": "7",
    "next_step": "2",
    "waiting_for_user": false,
    "user_test_pending": []
  }
}
```

## AI開発ワークフロー

### フェーズ1: 要件取得・計画立案
1. **要件定義書取得**: ユーザーから固まった要件定義を受領
2. **進捗状態確認**: `.claude/progress.json`を読み込み、前回中断箇所を特定
3. **要件分析**: Intake-Agentが要件を構造化・分析
4. **技術仕様作成**: DocGen-Agentがclaude.md更新・技術要件定義
5. **開発計画立案**: Planner-Agentが依存関係を分析し実行可能タスクを抽出

### フェーズ2: タスク管理・配備
6. **タスク仕様作成**: TaskGen-Agentが実装手順・背景・テスト要件を含む詳細仕様を生成
7. **開発環境配備**: Dispatcher-Agentがworktree作成・Dev-Agent割り当て

### フェーズ3: 並行開発・品質保証  
8. **並行実装**: 複数Dev-Agentが独立してworktree内で開発・テスト実行（同時実行は最大2つまで）
9. **コードレビュー**: Review-Agentが実装品質・設計適合性を検証・修正指示
10. **ユーザーテスト準備**: TestDoc-Agentがテスト手順書・worktree情報を含むMDファイル作成

### フェーズ4: ユーザー検証・統合
11. **ユーザーテスト**: ユーザーによる機能検証・バグ報告受付
12. **バグ修正サイクル**: BugFix-Agentがログ分析→問題特定→修正実装を反復
13. **統合準備**: 全テスト完了後、main→taskブランチプル・結合テスト実行
14. **本統合**: Integrator-Agentがtask→mainマージ・統合検証

### フェーズ5: 次サイクル移行
15. **進捗更新**: 完了タスクをprogress.jsonに記録
16. **次回計画**: 新たに実行可能になったタスクでフェーズ1に戻る

## メインエージェント統括機能

### ワークフロー制御システム（WorkflowController.py活用）

#### メインエージェント実行ルール
1. **セッション開始時**: 必ず`python src/WorkflowStateMachine.py` 実行 → 現在フェーズ・次アクション自動判定・中断復帰対応
2. **進捗確認要求時**: `python src/ProgressVisualizer.py` 実行 → 視覚的ダッシュボード表示
3. **ユーザー指示受領時**: 状態確認 → 適切なエージェント自動選択・起動
4. **エラー・ブロック検出時**: 状態分析 → 解決策提示・適切エージェント呼び出し
5. **作業中断時**: progress.jsonに中断情報記録 → `current_working_tasks`に詳細保存

#### データ更新責任分担
- **Dev-Agent**: タスク完了時 → `active_tasks[TaskID].status = "completed"`
- **Review-Agent**: レビュー完了時 → `active_tasks[TaskID].review_status = "approved"`
- **Integrator-Agent**: 統合完了時 → `integration_completed[]` 追加・`ready_for_next_sprint = true`
- **Planner-Agent**: 新スプリント作成時 → `current_sprint` 更新・`next_available_tasks` 更新
- **TestDoc-Agent**: テスト準備完了時 → `user_test_pending[]` 追加
- **User-Test-Coordinator**: テスト完了時 → `user_test_pending[]` クリア

#### 自動ワークフロー制御
- **フェーズ自動判定**: progress.json状態分析による5フェーズ自動判定
- **エージェント自動起動**: 現在フェーズ → 最適エージェント自動選択
- **並行処理動的制御**: Dev-Agent最大2同時実行の自動管理
- **品質ゲート自動制御**: カバレッジ80%+基準による統合可否判定

### メインエージェント自動実行ルール

#### 必須実行タイミング
1. **セッション開始時**: 必ず`python src/WorkflowStateMachine.py`実行 → 中断復帰・フェーズ判定
2. **進捗報告時**: 必ず`python src/ProgressVisualizer.py`実行 → ダッシュボード表示
3. **ユーザー質問応答時**: WorkflowStateMachine.py結果に基づいて適切な回答
4. **エージェント完了シグナル検知時**: 
   - `##DEV_DONE##` → Review-Agent自動起動
   - `##REVIEW_PASS##` → TestDoc-Agent自動起動  
   - `##TESTDOC_COMPLETE##` → User-Test-Coordinator自動起動
   - `##PLANNING_COMPLETE##` → TaskGen-Agent→Dispatcher-Agent連鎖起動

#### WorkflowStateMachine活用パターン
```python
# セッション開始時の基本フロー
from WorkflowStateMachine import WorkflowStateMachine
from ProgressVisualizer import ProgressVisualizer

# 1. 状態確認（必須）
workflow = WorkflowStateMachine()
summary = workflow.get_workflow_summary()
current_phase = summary['current_phase']
next_actions = summary['next_actions']

# 2. 進捗可視化（進捗確認時）
visualizer = ProgressVisualizer()
visualizer.show_dashboard()

# 3. フェーズ別アクション自動決定
if current_phase == "resuming_work":
    # 中断復帰処理
    interrupted_tasks = summary.get('interrupted_tasks', {})
    for task_id, task_data in interrupted_tasks.items():
        assignee = task_data.get('assignee')
        launch_agent(assignee, f"resume {task_id}")
elif current_phase == "development":  
    launch_dev_agents(next_actions)
```

### 実行フロー例
```
1. ユーザー指示受領 → WorkflowStateMachine.py実行
2. フェーズ判定: "resuming_work" → 中断タスク: "T-007"
3. 中断復帰情報表示 → ProgressVisualizer.py実行
4. ダッシュボード表示 → Dev-Agent自動アサイン
5. T-007復帰開始 → 並行開発再開
```

### 中断復帰対応構造
```json
{
  "current_working_tasks": {
    "T-007": {
      "status": "in_progress",
      "assignee": "dev-agent-1",
      "current_step": "implementation",
      "current_substep": "writing_unit_tests",
      "interruption_point": {
        "timestamp": "2025-08-05T14:22:00Z",
        "reason": "api_timeout",
        "context": "writing test for MouseHookService.Initialize method",
        "next_action": "complete unit test implementation",
        "files_modified": ["src/Services/MouseHookService.cs"]
      }
    }
  }
}
```

## エージェント間データ連携フロー

### progress.json更新チェーン
```mermaid
graph TD
    A[Planner-Agent] -->|current_sprint, next_available_tasks| B[progress.json]
    C[Dev-Agent] -->|active_tasks.status = completed| B
    D[Review-Agent] -->|active_tasks.review_status = approved| B
    E[TestDoc-Agent] -->|user_test_pending[] += TaskID| B
    F[User-Test-Coordinator] -->|user_test_pending[] = []| B
    G[Integrator-Agent] -->|integration_completed[], ready_for_next_sprint| B
    B -->|State Change| H[WorkflowController監視]
    H -->|Next Action Decision| I[メインエージェント]
```

### データ整合性保証
- **Atomic Updates**: 各エージェントはprogress.json更新を単一操作で実行
- **Version Control**: 各更新で`last_updated`タイムスタンプ更新
- **Validation**: WorkflowController.pyが状態妥当性チェック実行
- **Recovery**: 不整合検出時の自動修復・ユーザー通知機能

## エージェント定義
- **Intake-Agent** (file=".claude/agents/intake-agent.md"): 要件定義の構造化・分析
- **DocGen-Agent** (file=".claude/agents/docgen-agent.md"): 技術仕様・プロジェクト文書生成  
- **Planner-Agent** (file=".claude/agents/planner-agent.md"): 開発計画・依存関係分析
- **TaskGen-Agent** (file=".claude/agents/taskgen-agent.md"): タスク仕様書・実装手順生成
- **Dispatcher-Agent** (file=".claude/agents/dispatcher-agent.md"): worktree配備・Dev-Agent割り当て
- **Dev-Agent** (file=".claude/agents/dev-agent.md", count="最大2", concurrent_limit=2): 並行実装・単体テスト実行
- **Review-Agent** (file=".claude/agents/review-agent.md"): コード品質検証・修正指示
- **TestDoc-Agent** (file=".claude/agents/testdoc-agent.md"): ユーザーテスト手順書生成
- **User-Test-Coordinator** (file=".claude/agents/user-test-coordinator-agent.md"): ユーザーテスト管理・バグ修正サイクル調整
- **BugFix-Agent** (file=".claude/agents/bugfix-agent.md"): バグ分析・修正実装
- **Integrator-Agent** (file=".claude/agents/integrator-agent.md"): ブランチ統合・結合テスト

## 成功基準
- **開発効率**: 従来比3倍の開発速度達成
- **品質基準**: 80%以上のテストカバレッジ・ゼロクリティカルバグ
- **保守性**: SOLID原則準拠・疎結合アーキテクチャによる高保守性
- **ユーザー満足度**: 要件定義との完全適合・直感的UI実現